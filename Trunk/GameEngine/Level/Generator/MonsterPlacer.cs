﻿using System;
using System.Collections.Generic;
using System.Linq;
using Magecrawl.GameEngine.Actors;
using Magecrawl.GameEngine.MapObjects;
using Magecrawl.Utilities;

namespace Magecrawl.GameEngine.Level.Generator
{
    internal static class MonsterPlacer
    {
        // Priority is how dangerous this room is: 0 - unguarded, 10 - Must protect with everything
        internal static void PlaceMonster(Map map, Point upperLeft, Point lowerRight, List<Point> pointsNotToPlaceOn, int priority)
        {
            List<Point> pointsWithClearTerrain = MapGeneratorBase.GetClearPointListInRange(map, upperLeft, lowerRight);

            // We don't want monsters on top of map objects
            foreach (MapObject o in map.MapObjects)
                pointsWithClearTerrain.Remove(o.Position);

            // We need to remove seams since they might become walls later
            if (pointsNotToPlaceOn != null)
            {
                foreach (Point p in pointsNotToPlaceOn)
                    pointsWithClearTerrain.Remove(p);
            }

            pointsWithClearTerrain = pointsWithClearTerrain.Randomize();

            // Right now since we only have a single monster type, add 1 monster for every 2 levels of priority
            int numberOfMonstersToAdd = priority / 2;
            for (int i = 0; i < numberOfMonstersToAdd; ++i)
            {
                if (pointsWithClearTerrain.Count > 0)
                {
                    Point position = pointsWithClearTerrain[0];
                    pointsWithClearTerrain.RemoveAt(0);
                    Monster newMonster = CoreGameEngine.Instance.MonsterFactory.CreateRandomMonster(position);
                    map.AddMonster(newMonster);
                }
            }
        }
    }
}