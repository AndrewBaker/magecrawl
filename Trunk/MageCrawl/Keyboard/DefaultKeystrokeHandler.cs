﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using Magecrawl.GameEngine.Interfaces;
using Magecrawl.Utilities;

namespace Magecrawl.Keyboard
{
    internal enum ChordKeystrokeStatus
    {
        None,
        Operate,
        Attack,
    }

    internal class DefaultKeystrokeHandler : IKeystrokeHandler
    {
        private IGameEngine m_engine;
        private GameInstance m_gameInstance;
        private ChordKeystrokeStatus m_chordKeystroke;
        private Dictionary<NamedKey, MethodInfo> m_keyMappings;

        public Point SelectionPoint { get; set; }

        public bool InSelectionMode { get; set; }

        public DefaultKeystrokeHandler(IGameEngine engine, GameInstance instance)
        {
            m_engine = engine;
            m_gameInstance = instance;
            m_chordKeystroke = ChordKeystrokeStatus.None;
            m_keyMappings = null;
        }

        public void HandleKeystroke(NamedKey keystroke)
        {
            MethodInfo action;
            m_keyMappings.TryGetValue(keystroke, out action);
            if (action != null)
            {
                action.Invoke(this, null);
            }
        }

        public void LoadKeyMappings()
        {
            m_keyMappings = new Dictionary<NamedKey, MethodInfo>();

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            settings.IgnoreComments = true;
            XmlReader reader = XmlReader.Create(new StreamReader("KeyMappings.xml"), settings);
            reader.Read();  // XML declaration
            reader.Read();  // KeyMappings element
            if (reader.LocalName != "KeyMappings")
            {
                throw new InvalidOperationException("Bad key mappings file");
            }
            while (true)
            {
                reader.Read();
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "KeyMappings")
                {
                    break;
                }
                if (reader.LocalName == "KeyMapping")
                {
                    string key = reader.GetAttribute("Key");
                    string actionName = reader.GetAttribute("Action");
                    MethodInfo action = this.GetType().GetMethod(actionName, BindingFlags.Instance | BindingFlags.NonPublic);
                    if (action != null)
                    {
                        NamedKey namedKey = NamedKey.FromName(key);
                        m_keyMappings.Add(namedKey, action);
                    }
                    else
                    {
                        throw new InvalidOperationException(String.Format("Could not find a mappable operation named {0}.", actionName));
                    }
                }
            }
            reader.Close();
        }

        private void HandleDirection(Direction direction)
        {
            if (m_chordKeystroke == ChordKeystrokeStatus.Operate)
            {
                m_engine.Operate(direction);
            }
            else if (m_chordKeystroke == ChordKeystrokeStatus.Attack)
            {
                Point pointWantToGoTo = PointDirectionUtils.ConvertDirectionToDestinationPoint(SelectionPoint, direction);
                Point resultPoint = AttackKeystrokeHelper.MoveSelectionToNewPoint(m_engine, pointWantToGoTo, direction);
                if (resultPoint != Point.Invalid)
                    SelectionPoint = resultPoint;
                m_gameInstance.SendPaintersRequest("MapCursorPositionChanged", SelectionPoint);
                m_gameInstance.UpdatePainters();
                return;
            }
            else
            {
                m_engine.MovePlayer(direction);
            }
            m_chordKeystroke = ChordKeystrokeStatus.None;
            m_gameInstance.UpdatePainters();
        }

        #region Mappable key commands

        /*
         * BCL: see file MageCrawl/dist/KeyMappings.xml. To add a new mappable action, define a private method for it here,
         * then map it to an unused key in KeyMappings.xml. The action should take no parameters and should return nothing.
         * 
         */

        private void North()
        {
            HandleDirection(Direction.North);
        }

        private void South()
        {
            HandleDirection(Direction.South);
        }

        private void East()
        {
            HandleDirection(Direction.East);
        }

        private void West()
        {
            HandleDirection(Direction.West);
        }

        private void Northeast()
        {
            HandleDirection(Direction.Northeast);
        }

        private void Northwest()
        {
            HandleDirection(Direction.Northwest);
        }

        private void Southeast()
        {
            HandleDirection(Direction.Southeast);
        }

        private void Southwest()
        {
            HandleDirection(Direction.Southwest);
        }

        private void Quit()
        {
            m_gameInstance.IsQuitting = true;
        }

        private void Operate()
        {
            m_chordKeystroke = ChordKeystrokeStatus.Operate;
        }

        private void Save()
        {
            m_engine.Save();
            m_gameInstance.UpdatePainters();
        }

        private void Load()
        {
            try
            {
                m_engine.Load();
                m_gameInstance.UpdatePainters();
            }
            catch (System.IO.FileNotFoundException)
            {
                // TODO: Inform user somehow
                m_gameInstance.UpdatePainters();
            }
        }

        private void MoveableOnOff()
        {
            m_gameInstance.SendPaintersRequest("DebuggingMoveableOnOff", m_engine);
        }

        private void DebuggingFOVOnOff()
        {
            m_gameInstance.SendPaintersRequest("DebuggingFOVOnOff", m_engine);
        }

        private void Wait()
        {
            m_engine.PlayerWait();
            m_gameInstance.UpdatePainters();
        }

        private void Attack()
        {
            if (m_chordKeystroke == ChordKeystrokeStatus.Attack)
            {
                if (SelectionPoint != m_engine.Player.Position)
                    m_engine.PlayerAttack(SelectionPoint);
                InSelectionMode = false;
                m_chordKeystroke = ChordKeystrokeStatus.None;
                m_gameInstance.SendPaintersRequest("MapCursorDisabled", null);
                m_gameInstance.SendPaintersRequest("RangedAttackDisabled", null);
                m_gameInstance.UpdatePainters();
            }
            else
            {
                m_chordKeystroke = ChordKeystrokeStatus.Attack;
                SelectionPoint = AttackKeystrokeHelper.SetAttackInitialSpot(m_engine, m_engine.Player.CurrentWeapon);
                InSelectionMode = true;
                List<WeaponPoint> listOfSelectablePoints = m_engine.Player.CurrentWeapon.CalculateTargetablePoints(m_engine.Player.Position);
                m_gameInstance.SendPaintersRequest("RangedAttackEnabled", listOfSelectablePoints);
                m_gameInstance.SendPaintersRequest("MapCursorEnabled", SelectionPoint);
                m_gameInstance.UpdatePainters();
            }
        }

        private void ChangeWeapon()
        {
            m_engine.IterateThroughWeapons();
        }

        private void TextBoxPageUp()
        {
            m_gameInstance.TextBox.TextBoxScrollUp();
        }

        private void TextBoxPageDown()
        {
            m_gameInstance.TextBox.TextBoxScrollDown();
        }

        private void TextBoxClear()
        {
            m_gameInstance.TextBox.Clear();
        }

        #endregion
    }
}