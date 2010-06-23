using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using libtcod;
using Magecrawl.Exceptions;
using Magecrawl.Interfaces;
using Magecrawl.GameUI;
using Magecrawl.GameUI.Dialogs;
using Magecrawl.GameUI.Map.Requests;
using Magecrawl.Keyboard;
using Magecrawl.Keyboard.Debug;
using Magecrawl.Keyboard.Dialogs;
using Magecrawl.Keyboard.Effects;
using Magecrawl.Keyboard.Inventory;
using Magecrawl.Keyboard.Magic;
using Magecrawl.Keyboard.SkillTree;
using Magecrawl.Utilities;

namespace Magecrawl
{
    internal sealed class GameInstance : IDisposable
    {
        internal bool IsQuitting { get; set; }
        internal TextBox TextBox { get; set; }
        private TCODConsole m_console;

        [Import]
        private IGameEngine m_engine;

        [ImportMany]
        private Lazy<IKeystrokeHandler, IDictionary<string, object>>[] m_keystrokeImports;

        private CompositionContainer m_container;

        private KeystrokeManager m_keystroke;
        private PaintingCoordinator m_painters;

        public bool ShouldSaveOnClose
        {
            get;
            set;
        }

        internal GameInstance()
        {
            m_console = TCODConsole.root;

            Compose();

            TextBox = new TextBox();
            m_painters = new PaintingCoordinator();

            // Most of the time while debugging, we don't want to save on window close
            ShouldSaveOnClose = !Preferences.Instance.DebuggingMode;
        }

        public void Compose()
        {
            using (LoadingScreen loadingScreen = new LoadingScreen(m_console, "Loading Game..."))
            {
                m_container = new CompositionContainer(
                    new AggregateCatalog(new AssemblyCatalog(System.Reflection.Assembly.GetExecutingAssembly()),
                    new DirectoryCatalog(".")));
                m_container.ComposeParts(this);
            }
        }

        public void Dispose()
        {
            m_container.Dispose();
            m_engine = null;
            if (m_painters != null)
                m_painters.Dispose();
            m_painters = null;
        }

        private void ShowWelcomeMessage(bool firstTime)
        {
            if (firstTime)
                TextBox.AddText(string.Format("If this is your first time, press '{0}' for help.", m_keystroke.DefaultHandler.GetCommandKey("Help")));
            if (m_engine.Player.SkillPoints > 0)
                TextBox.AddText(string.Format("You have skill points to spent. Press '{0}' to open the skill tree.", m_keystroke.DefaultHandler.GetCommandKey("ShowSkillTree")));
            if (firstTime)
                TextBox.AddText("Welcome To Magecrawl.");
            else
                TextBox.AddText("Welcome Back To Magecrawl.");
        }
        
        internal void Go(string playerName, bool loadFromFile)
        {
            SetupEngineOutputDelegate();

            if (loadFromFile)
            {
                string saveFilePath = playerName + ".sav";
 
                using (LoadingScreen loadingScreen = new LoadingScreen(m_console, "Loading..."))
                {
                    m_engine.LoadSaveFile(saveFilePath);
                }

                SetupKeyboardHandlers();  // Requires game engine.
                SetHandlerName("Default");

                ShowWelcomeMessage(false);
            }
            else
            {
                using (LoadingScreen loadingScreen = new LoadingScreen(m_console, "Generating World..."))
                {
                    m_engine.CreateNewWorld(playerName);
                }

                SetupKeyboardHandlers();  // Requires game engine.
                if (!Preferences.Instance.DebuggingMode)
                    SetHandlerName("Welcome");
                else
                    SetHandlerName("Default");

                ShowWelcomeMessage(true);
            }

            // First update before event loop so we have a map to display
            m_painters.UpdateFromNewData(m_engine);

            do
            {
                try
                {
                    HandleKeyboard();
                    DrawFrame();
                }
                catch (PlayerDiedException)
                {
                    HandleException(true);
                }
                catch (PlayerWonException)
                {
                    HandleException(false);
                }
                catch (System.Reflection.TargetInvocationException e)
                {
                    if (e.InnerException is PlayerDiedException)
                    {
                        HandleException(true);
                    }
                    else if (e.InnerException is PlayerWonException)
                    {
                        HandleException(false);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            while (!TCODConsole.isWindowClosed() && !IsQuitting);
            
            // User closed the window, save and bail.
            if (TCODConsole.isWindowClosed() && !IsQuitting && ShouldSaveOnClose)
            {
                m_engine.Save();
            }
        }

        private void SetupEngineOutputDelegate()
        {
            m_engine.PlayerDiedEvent += HandlePlayerDied;
            m_engine.RangedAttackEvent += HandleRangedAttack;
            m_engine.TextOutputEvent += TextBox.AddText;
        }

        public void DrawFrame()
        {
            m_console.clear();
            TextBox.Draw(m_console);
            m_painters.DrawNewFrame(m_console);
            TCODConsole.flush();
        }

        private void HandleRangedAttack(object attackingMethod, ShowRangedAttackType type, object data, bool targetAtEndPoint)
        {
            UpdatePainters();
            ResetHandlerName();
            SendPaintersRequest(new DisableAllOverlays());
            
            TCODColor colorOfBolt = ColorPresets.White;
            if (attackingMethod is ISpell)
                colorOfBolt = SpellGraphicsInfo.GetColorOfSpellFromSchool(((ISpell)attackingMethod).School);
            else if (attackingMethod is IItem)
                colorOfBolt = SpellGraphicsInfo.GetColorOfSpellFromSchool(((IItem)attackingMethod).ItemEffectSchool);

            if (type == ShowRangedAttackType.RangedBoltOrBlast)
            {
                bool drawLastFrame = !targetAtEndPoint;
                int tailLength = 1;

                if (attackingMethod is ISpell)
                {
                    ISpell attackingSpell = (ISpell)attackingMethod;
                    drawLastFrame |= SpellGraphicsInfo.DrawLastFrame(attackingSpell);  // Draw the last frame if we wouldn't otherwise and the spell asks us to.
                    tailLength = SpellGraphicsInfo.GetTailLength(attackingSpell);
                }
                m_painters.HandleRequest(new ShowRangedBolt(null, (List<Point>)data, colorOfBolt, drawLastFrame, tailLength));
                m_painters.DrawAnimationSynchronous(m_console);
            }
            else if (type == ShowRangedAttackType.Cone)
            {
                m_painters.HandleRequest(new ShowConeBlast(null, (List<Point>)data, colorOfBolt));
                m_painters.DrawAnimationSynchronous(m_console);
            }
            else if (type == ShowRangedAttackType.RangedExplodingPoint)
            {
                var animationData = (Pair<List<Point>, List<List<Point>>>)data;
                m_painters.HandleRequest(new ShowExploadingPoint(null, animationData.First, animationData.Second, colorOfBolt));
                m_painters.DrawAnimationSynchronous(m_console);
            }
        }

        private void HandleException(bool death)
        {
            HandleGameOver(death ? "Player has died." : "Player has won.");
            IsQuitting = true;
        }

        private void HandleGameOver(string textToDisplay)
        {
            // Put death information out here.
            UpdatePainters();
            SendPaintersRequest(new DisableAllOverlays());
            m_painters.DrawNewFrame(m_console);
            TextBox.AddText(textToDisplay);
            TextBox.AddText("Press 'q' to exit.");
            TextBox.Draw(m_console);
            TCODConsole.flush();

            while (!TCODConsole.isWindowClosed())
            {
                if (TCODConsole.checkForKeypress((int)TCODKeyStatus.KeyPressed).Character == 'q')
                    break;
            }
        }

        private void SetupKeyboardHandlers()
        {
            m_keystroke = new KeystrokeManager(m_engine);

            foreach (var keystrokeHandlerImport in m_keystrokeImports)
            {
                bool requiresAllActionsMapped = Boolean.Parse((string)keystrokeHandlerImport.Metadata["RequireAllActionsMapped"]);
                string handlerName = (string)keystrokeHandlerImport.Metadata["HandlerName"];
                BaseKeystrokeHandler handler = (BaseKeystrokeHandler)keystrokeHandlerImport.Value;
                handler.Init(m_engine, this, requiresAllActionsMapped);
                m_keystroke.Handlers.Add(handlerName, handler);
            }            

            if (BaseKeystrokeHandler.ErrorsParsingKeymapFiles != "")
            {
                TextBox.AddText("");
                TextBox.AddText(BaseKeystrokeHandler.ErrorsParsingKeymapFiles);
                TextBox.AddText("");
            }
        }

        internal void SetHandlerName(string s)
        {
            SetHandlerName(s, null);
        }

        internal void SetHandlerName(string s, object request)
        {
            m_keystroke.CurrentHandlerName = s;
            m_keystroke.CurrentHandler.NowPrimaried(request);
        }

        internal void ResetHandlerName()
        {
            m_keystroke.CurrentHandlerName = "Default";
        }

        private void HandlePlayerDied()
        {
            // So we want player dead to hault pretty much everything. While an exception is 
            // probally not the 'best' solution, since we're in a callback from the engine, made from a request from HandleKeyboard
            // it's easy.
            throw new PlayerDiedException();
        }

        internal void SendPaintersRequest(RequestBase request)
        {
            m_painters.HandleRequest(request);
        }

        internal void UpdatePainters()
        {
            m_painters.UpdateFromNewData(m_engine);
        }

        private void HandleKeyboard()
        {
            m_keystroke.HandleKeyStroke();
        }
    }
}
