﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Magecrawl.GameEngine.Actors;
using Magecrawl.GameEngine.Interfaces;

namespace Magecrawl.GameEngine.Magic
{
    internal sealed class Spell
    {
        private string m_name;
        private string m_effectType;
        private int m_cost;
        private int m_strength;

        internal Spell(string name, string effectType, int cost, int strength)
        {
            m_name = name;
            m_effectType = effectType;
            m_cost = cost;
            m_strength = strength;
        }

        internal string Name
        {
            get
            {
                return m_name;
            }
        }

        internal string EffectType
        {
            get
            {
                return m_effectType;
            }
        }

        internal int Cost
        {
            get
            {
                return m_cost;
            }
        }

        internal int Strength
        {
            get
            {
                return m_strength;
            }
        }
    }
}