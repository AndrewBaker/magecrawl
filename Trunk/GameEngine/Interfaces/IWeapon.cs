﻿using System;
using System.Collections.Generic;
using libtcodWrapper;
using Magecrawl.Utilities;

namespace Magecrawl.GameEngine.Interfaces
{
    public struct WeaponPoint
    {
        public Point Position;

        // On scale of .01 to 1, lower is less damage.
        public float EffectiveStrength;

        public WeaponPoint(Point p, float str)
        {
            Position = p;
            EffectiveStrength = str;
        }
    }

    public interface IWeapon
    {
        DiceRoll Damage
        {
            get;
        }

        string Name
        {
            get;
        }

        List<WeaponPoint> CalculateTargetablePoints(Point characterPosition);

        // This version is faster since we don't have to calculate targetablePoints over and over again.
        bool PositionInTargetablePoints(Point characterCenter, Point pointOfInterest, List<WeaponPoint> targetablePoints);
        bool PositionInTargetablePoints(Point characterCenter, Point pointOfInterest);

        float EffectiveStrengthAtPoint(Point characterCenter, Point pointOfInterest);
    }
}