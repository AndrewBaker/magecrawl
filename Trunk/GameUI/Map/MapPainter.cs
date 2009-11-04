﻿using System.Collections.Generic;
using libtcodWrapper;
using Magecrawl.GameEngine.Interfaces;
using Magecrawl.Utilities;

namespace Magecrawl.GameUI.Map
{
    internal sealed class MapPainter : MapPainterBase
    {
        private Console m_offscreenConsole;
        private bool m_honorFOV;

        public MapPainter()
        {
            m_offscreenConsole = RootConsole.GetNewConsole(OffscreenWidth, OffscreenHeight);
            m_honorFOV = true;
        }

        internal bool HonorFOV
        {
            get { return m_honorFOV; }
            set { m_honorFOV = value; }
        }

        public override void UpdateFromNewData(IGameEngine engine, Point mapUpCorner)
        {
            TileVisibility[,] tileVisibility = engine.CalculateTileVisibility();

            m_offscreenConsole.Clear();

            m_offscreenConsole.DrawFrame(0, 0, MapDrawnWidth + 1, MapDrawnHeight + 1, true, "Map");

            for (int i = 0; i < engine.Map.Width; ++i)
            {
                for (int j = 0; j < engine.Map.Height; ++j)
                {
                    DrawThing(mapUpCorner, new Point(i, j), m_offscreenConsole, ConvertTerrianToChar(engine.Map[i, j].Terrain));
                }
            }

            foreach (IMapObject obj in engine.Map.MapObjects)
            {
                DrawThing(mapUpCorner, obj.Position, m_offscreenConsole, ConvertMapObjectToChar(obj.Type));
            }

            foreach (Pair<IItem, Point> obj in engine.Map.Items)
            {
                TileVisibility visibility = tileVisibility[obj.Second.X, obj.Second.Y];
                if (!m_honorFOV || visibility == TileVisibility.Visible)
                {
                    DrawThingIfMultipleSpecialSymbol(mapUpCorner, obj.Second, m_offscreenConsole, '&', '%');
                }
            }

            foreach (ICharacter obj in engine.Map.Monsters)
            {
                TileVisibility visibility = tileVisibility[obj.Position.X, obj.Position.Y];
                if (!m_honorFOV || visibility == TileVisibility.Visible)
                    DrawThing(mapUpCorner, obj.Position, m_offscreenConsole, 'M');
            }

            DrawThing(mapUpCorner, engine.Player.Position, m_offscreenConsole, '@');
        }

        public override void DrawNewFrame(Console screen)
        {
            m_offscreenConsole.Blit(0, 0, OffscreenWidth, OffscreenHeight, screen, 0, 0);
        }

        public override void Dispose()
        {
            if (m_offscreenConsole != null)
                m_offscreenConsole.Dispose();
            m_offscreenConsole = null;
        }

        private static void DrawThing(Point mapUpCorner, Point position, Console screen, char symbol)
        {
            Point screenPlacement = new Point(mapUpCorner.X + position.X + 1, mapUpCorner.Y + position.Y + 1);

            if (IsDrawableTile(screenPlacement))
            {
                screen.PutChar(screenPlacement.X, screenPlacement.Y, symbol);
            }
        }

        private static void DrawThingIfMultipleSpecialSymbol(Point mapUpCorner, Point position, Console screen, char symbol, char multipleSymbol)
        {
            Point screenPlacement = new Point(mapUpCorner.X + position.X + 1, mapUpCorner.Y + position.Y + 1);

            if (IsDrawableTile(screenPlacement))
            {
                char currentChar = screen.GetChar(screenPlacement.X, screenPlacement.Y);

                // If we already have one of those, or the multipleSymbol, draw the multipleSymbole, else draw normal.
                if (currentChar == symbol || currentChar == multipleSymbol)
                    screen.PutChar(screenPlacement.X, screenPlacement.Y, multipleSymbol);
                else 
                    screen.PutChar(screenPlacement.X, screenPlacement.Y, symbol);
            }
        }
    }
}