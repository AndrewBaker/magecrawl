﻿using System;
using System.Collections.Generic;
using System.Linq;
using Magecrawl.GameEngine.Actors;
using Magecrawl.GameEngine.Affects;
using Magecrawl.GameEngine.Interfaces;
using Magecrawl.GameEngine.Items;
using Magecrawl.GameEngine.Magic;
using Magecrawl.GameEngine.MapObjects;
using Magecrawl.Utilities;

namespace Magecrawl.GameEngine
{
    // So in the current archtecture, each public method should do the action requested,
    // and _then_ call the CoreTimingEngine somehow to let others have their time slice before returning
    // This is very synchronous, but easy to do.
    public class PublicGameEngine : IGameEngine
    {
        private CoreGameEngine m_engine;

        public PublicGameEngine(TextOutputFromGame textOutput, PlayerDiedDelegate playerDiedDelegate)
        {
            // This is a singleton accessable from anyone in GameEngine, but stash a copy since we use it alot
            m_engine = new CoreGameEngine(textOutput, playerDiedDelegate);
        }

        public PublicGameEngine(TextOutputFromGame textOutput, PlayerDiedDelegate playerStateChanged, string saveGameName)
        {
            // This is a singleton accessable from anyone in GameEngine, but stash a copy since we use it alot
            m_engine = new CoreGameEngine(textOutput, playerStateChanged, saveGameName);
        }

        public void Dispose()
        {
            if (m_engine != null)
                m_engine.Dispose();
            m_engine = null;
        }

        public Point TargetSelection
        {
            get;
            set;
        }

        public bool SelectingTarget
        {
            get;
            set;
        }

        public IPlayer Player
        {
            get
            {
                return m_engine.Player;
            }
        }

        public IMap Map
        {
            get
            {
                return m_engine.Map;
            }
        }

        public int CurrentLevel
        {
            get
            {
                return m_engine.CurrentLevel;
            }
        }

        public bool MovePlayer(Direction direction)
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.Move(m_engine.Player, direction);
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;
        }

        public bool Operate(Point pointToOperateAt)
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.Operate(m_engine.Player, pointToOperateAt);
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;
        }

        public bool PlayerWait()
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.Wait(m_engine.Player);
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;
        }

        public bool PlayerAttack(Point target)
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.Attack(m_engine.Player, target);
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;            
        }

        public bool PlayerCouldCastSpell(ISpell spell)
        {
            return m_engine.Player.CurrentMP >= ((Spell)spell).Cost;
        }

        public bool PlayerCastSpell(ISpell spell, Point target)
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.CastSpell(m_engine.Player, (Spell)spell, target);
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;
        }

        public bool PlayerGetItem()
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.PlayerGetItem();
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;            
        }

        public void Save()
        {
            m_engine.Save();
        }

        public List<Point> PlayerPathToPoint(Point dest)
        {
            return m_engine.PathToPoint(m_engine.Player, dest, true, true, false);
        }

        // For the IsPathable debugging mode, show if player could walk there.
        public bool[,] PlayerMoveableToEveryPoint()
        {
            return m_engine.PlayerMoveableToEveryPoint();
        }

        public List<Point> CellsInPlayersFOV()
        {
            return GenerateFOVListForCharacter(m_engine.Player);
        }

        private List<Point> GenerateFOVListForCharacter(ICharacter c)
        {
            List<Point> returnList = new List<Point>();

            m_engine.FOVManager.CalculateForMultipleCalls(m_engine.Map, c.Position, c.Vision);

            for (int i = 0; i < m_engine.Map.Width; ++i)
            {
                for (int j = 0; j < m_engine.Map.Height; ++j)
                {
                    Point currentPosition = new Point(i, j);
                    if (m_engine.FOVManager.Visible(currentPosition))
                    {
                        returnList.Add(currentPosition);
                    }
                }
            }
            return returnList;
        }

        public Dictionary<ICharacter, List<Point>> CellsInAllMonstersFOV()
        {
            Dictionary<ICharacter, List<Point>> returnValue = new Dictionary<ICharacter, List<Point>>();

            foreach (ICharacter c in m_engine.Map.Monsters)
            {
                returnValue[c] = GenerateFOVListForCharacter(c);
            }

            return returnValue;
        }

        public TileVisibility[,] CalculateTileVisibility()
        {
            return m_engine.CalculateTileVisibility();
        }

        public List<ItemOptions> GetOptionsForInventoryItem(IItem item)
        {
            return m_engine.GetOptionsForInventoryItem(item as Item);
        }

        public List<ItemOptions> GetOptionsForEquipmentItem(IItem item)
        {
            return m_engine.GetOptionsForEquipmentItem(item as Item);
        }

        public bool PlayerSelectedItemOption(IItem item, string option)
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.PlayerSelectedItemOption(item, option);
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;
        }

        public void FilterNotTargetablePointsFromList(List<EffectivePoint> pointList, bool needsToBeVisible)
        {
            m_engine.FilterNotTargetablePointsFromList(pointList, m_engine.Player.Position, m_engine.Player.Vision, needsToBeVisible);
        }

        public bool PlayerMoveDownStairs()
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.PlayerMoveDownStairs();
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;
        }

        public bool PlayerMoveUpStairs()
        {
            m_engine.BeforePlayerAction();
            bool didAnything = m_engine.PlayerMoveUpStairs();
            if (didAnything)
                m_engine.AfterPlayerAction();
            return didAnything;
        }

        public StairMovmentType IsStairMovementSpecial(bool headingUp)
        {
            Stairs s = m_engine.Map.MapObjects.OfType<Stairs>().Where(x => x.Position == m_engine.Player.Position).SingleOrDefault();
            if (s != null)
            {
                if (s.Type == MapObjectType.StairsUp && m_engine.CurrentLevel == 0 && headingUp)
                    return StairMovmentType.QuitGame;
                else if (s.Type == MapObjectType.StairsDown && m_engine.CurrentLevel == (m_engine.NumberOfLevels - 1) && !headingUp)
                    return StairMovmentType.WinGame;
            }
            return StairMovmentType.None;
        }

        public bool DangerInLOS()
        {
            m_engine.FOVManager.CalculateForMultipleCalls(m_engine.Map, m_engine.Player.Position, m_engine.Player.Vision);

            foreach (Monster m in m_engine.Map.Monsters)
            {
                if (m_engine.FOVManager.Visible(m.Position))
                    return true;
            }
            return false;
        }

        public List<ICharacter> MonstersInPlayerLOS()
        {
            return m_engine.MonstersInPlayerLOS();
        }
    }
}