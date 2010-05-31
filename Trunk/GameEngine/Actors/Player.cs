using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Magecrawl.GameEngine.Armor;
using Magecrawl.GameEngine.Effects;
using Magecrawl.GameEngine.Interfaces;
using Magecrawl.GameEngine.Items;
using Magecrawl.GameEngine.Magic;
using Magecrawl.GameEngine.SaveLoad;
using Magecrawl.GameEngine.Skills;
using Magecrawl.Utilities;

namespace Magecrawl.GameEngine.Actors
{
    internal sealed class Player : Character, IPlayer, IXmlSerializable
    {
        public IArmor ChestArmor { get; internal set; }
        public IArmor Headpiece { get; internal set; }
        public IArmor Gloves { get; internal set; }
        public IArmor Boots { get; internal set; }

        public int SkillPoints { get; internal set; }

        private List<Item> m_itemList;
        private List<Skill> m_skills;

        public int LastTurnSeenAMonster { get; set; }

        public Player() : base()
        {
            m_itemList = null;
            m_skills = null;
            m_currentStamina = 0;
            m_baseMaxStamina = 0;
            m_currentHealth = 0;
            m_baseMaxHealth = 0;
            m_baseCurrentMP = 0;
            m_baseMaxStamina = 0;
            LastTurnSeenAMonster = 0;
            SkillPoints = 0;
        }

        public Player(string name, Point p) : base(name, p, 6)
        {
            m_itemList = new List<Item>();
            m_skills = new List<Skill>();

            m_baseMaxStamina = 8;
            m_currentStamina = m_baseMaxStamina;

            m_baseMaxHealth = 12;
            m_currentHealth = m_baseMaxHealth;

            m_baseMaxMP = 10;
            m_baseCurrentMP = m_baseMaxMP;
            
            SkillPoints = 5;

            LastTurnSeenAMonster = 0;

            m_itemList.Add(CoreGameEngine.Instance.ItemFactory.CreateItem("Minor Health Potion"));
            m_itemList.Add(CoreGameEngine.Instance.ItemFactory.CreateItem("Minor Mana Potion"));                        
            Equip(CoreGameEngine.Instance.ItemFactory.CreateItem("Wooden Cudgel"));
            Equip(CoreGameEngine.Instance.ItemFactory.CreateItem("Robe"));
            Equip(CoreGameEngine.Instance.ItemFactory.CreateItem("Sandles"));

            // Since we're equiping equipment here, reset m_currentStamina to new total
            m_currentStamina = MaxStamina;
        }

        #region HP/MP
        
        private int m_currentStamina;
        public int CurrentStamina
        {
            get
            {
                return m_currentStamina;
            }
        }
        
        private int m_currentHealth;
        public int CurrentHealth
        {
            get
            {
                return m_currentHealth;
            }
        }

        public override int CurrentHP 
        {
            get
            {
                return CurrentHealth + CurrentStamina;
            }
        }
        
        private int m_baseMaxStamina;
        public int MaxStamina
        {
            get
            {
                int staminaSkillBonus = 0;
                foreach (Skill s in Skills)
                    staminaSkillBonus += s.HPBonus;
                int baseMaxStamWithSkills = m_baseMaxStamina + staminaSkillBonus;
                return baseMaxStamWithSkills + CombatDefenseCalculator.CalculateStaminaBonus(this);
            }
        }

        private int m_baseMaxHealth;
        public int MaxHealth
        {
            get
            {
                return m_baseMaxHealth;
            }
        }

        public override int MaxHP
        {
            get
            {
                return MaxHealth + MaxStamina;
            }
        }

        private int m_baseCurrentMP;
        public int CurrentMP 
        {
            get
            {
                return m_baseCurrentMP;
            }
        }

        public int MaxMP
        {
            get
            {
                return MaxPossibleMP - m_effects.OfType<PositiveEffect>().Sum(x => x.MPCost);
            }
        }

        private int m_baseMaxMP;
        public int MaxPossibleMP
        {
            get
            {
                int baseMax = m_baseMaxMP;

                int percentageBonus = 0;
                foreach (Skill s in Skills)
                {
                    percentageBonus += s.MPBonus;
                }
                baseMax = (int)(baseMax * (1.0 + ((float)percentageBonus / 100.0f)));

                return baseMax;
            }
        }

        // Returns amount actually healed by
        public override int Heal(int toHeal, bool magical)
        {
            if (toHeal < 0)
                throw new InvalidOperationException("Heal with < 0.");

            int amountOfDamageToHeal = toHeal;
            int amountInTotalHealed = 0;
            if (magical)
            {
                int amountOfHealthMissing = MaxHealth - CurrentHealth;
                if (amountOfHealthMissing > 0)
                {
                    int amountOfHealthToHeal = Math.Min(amountOfDamageToHeal, amountOfHealthMissing);
                    m_currentHealth += amountOfHealthToHeal;
                    amountOfDamageToHeal -= amountOfHealthToHeal;
                    amountInTotalHealed = amountOfHealthToHeal;
                }
            }
            if (amountOfDamageToHeal > 0)
            {
                int amountOfStaminaMissing = MaxStamina - CurrentStamina;
                if (amountOfStaminaMissing > 0)
                {
                    int amountOfStaminaToHeal = Math.Min(amountOfDamageToHeal, amountOfStaminaMissing);
                    m_currentStamina += amountOfStaminaToHeal;
                    amountInTotalHealed += amountOfStaminaToHeal;
                }
            }
            return amountInTotalHealed;

        }

        public override void Damage(int dmg)
        {
            int amountOfDamageLeftToDo = dmg;
            int amountOfDamageToStamina = Math.Min(m_currentStamina, amountOfDamageLeftToDo);
            m_currentStamina -= amountOfDamageToStamina;
            amountOfDamageLeftToDo -= amountOfDamageToStamina;

            if (amountOfDamageLeftToDo > 0)
            {
                int amountOfDamageToHealth = Math.Min(m_currentHealth, amountOfDamageLeftToDo);
                m_currentHealth -= amountOfDamageToHealth;
            }
        }

        public void GainMP(int amount)
        {
            m_baseCurrentMP += amount;
            if (m_baseCurrentMP > MaxMP)
                m_baseCurrentMP = MaxMP;
        }

        public void SpendMP(int amount)
        {
            m_baseCurrentMP -= amount;
        }

        #endregion

        public IEnumerable<ISpell> Spells
        {
            get 
            {
                List<ISpell> returnList = new List<ISpell>();
                foreach (Skill skill in Skills)
                {
                    if (skill.NewSpell)
                        returnList.Add(SpellFactory.CreateSpell(skill.AddSpell));
                }
                return returnList;
            }
        }

        public int SpellStrength(string spellType)
        {
            return CalculateSpellStrengthFromPassiveSkills(spellType);
        }

        private int CalculateSpellStrengthFromPassiveSkills(string typeName)
        {
            int strength = 1;
            foreach (Skill s in Skills)
            {
                if (s.Proficiency == typeName)
                    strength++;
            }
            return strength;
        }

        public IEnumerable<IItem> Items
        {
            get
            {
                return m_itemList.ConvertAll<IItem>(i => i).ToList();
            }
        }

        public IEnumerable<string> StatusEffects
        {
            get
            {
                return m_effects.Select(a => a.Name).ToList();
            }
        }

        public IEnumerable<ISkill> Skills
        {
            get
            {
                return m_skills.ConvertAll<ISkill>(x => x).ToList();
            }
        }

        public void AddSkill(ISkill skill)
        {
             m_skills.Add((Skill)skill);
        }

        internal override IItem Equip(IItem item)
        {
            if (item is ChestArmor)
            {
                IItem previousArmor = ChestArmor;
                ChestArmor = (IArmor)item;
                return previousArmor;
            }
            if (item is Headpiece)
            {
                IItem previousArmor = Headpiece;
                Headpiece = (IArmor)item;
                return previousArmor;
            }
            if (item is Gloves)
            {
                IItem previousArmor = Gloves;
                Gloves = (IArmor)item;
                return previousArmor;
            }
            if (item is Boots)
            {
                IItem previousArmor = Boots;
                Boots = (IArmor)item;
                return previousArmor;
            }

            return base.Equip(item);
        }

        private void ResetMaxStaminaIfNowOver()
        {
            if (CurrentStamina > MaxStamina)
                m_currentStamina = MaxStamina;
        }

        internal override IItem Unequip(IItem item)
        {
            if (item is ChestArmor)
            {
                IItem previousArmor = ChestArmor;
                ChestArmor = null;
                ResetMaxStaminaIfNowOver();
                return previousArmor;
            }
            if (item is Headpiece)
            {
                IItem previousArmor = Headpiece;
                Headpiece = null;
                ResetMaxStaminaIfNowOver();
                return previousArmor;
            }
            if (item is Gloves)
            {
                IItem previousArmor = Gloves;
                Gloves = null;
                ResetMaxStaminaIfNowOver();
                return previousArmor;
            }
            if (item is Boots)
            {
                IItem previousArmor = Boots;
                Boots = null;
                ResetMaxStaminaIfNowOver();
                return previousArmor;
            }

            return base.Unequip(item);
        }

        internal void TakeItem(Item i)
        {
            m_itemList.Add(i);
        }

        internal void RemoveItem(Item i)
        {
            m_itemList.Remove(i);
        }

        public override DiceRoll MeleeDamage
        {
            get
            {
                return new DiceRoll(1, 2);
            }
        }
        
        public override double MeleeSpeed
        {
            get
            {
                return 1.0;
            }
        }

        public override double Evade
        {
            get
            {
                return CombatDefenseCalculator.CalculateEvade(this);
            }
        }

        #region SaveLoad

        public override void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement();
            base.ReadXml(reader);

            m_currentHealth = reader.ReadElementContentAsInt();
            m_baseMaxHealth = reader.ReadElementContentAsInt();

            m_currentStamina = reader.ReadElementContentAsInt();
            m_baseMaxStamina = reader.ReadElementContentAsInt();

            m_baseCurrentMP = reader.ReadElementContentAsInt();
            m_baseMaxMP = reader.ReadElementContentAsInt();

            SkillPoints = reader.ReadElementContentAsInt();

            LastTurnSeenAMonster = reader.ReadElementContentAsInt();

            ChestArmor = (IArmor)Item.ReadXmlEntireNode(reader, this);
            Headpiece = (IArmor)Item.ReadXmlEntireNode(reader, this);
            Gloves = (IArmor)Item.ReadXmlEntireNode(reader, this);
            Boots = (IArmor)Item.ReadXmlEntireNode(reader, this);

            m_itemList = new List<Item>();
            ReadListFromXMLCore readItemDelegate = new ReadListFromXMLCore(delegate
            {
                string typeString = reader.ReadElementContentAsString();
                Item newItem = CoreGameEngine.Instance.ItemFactory.CreateItem(typeString); 
                newItem.ReadXml(reader);
                m_itemList.Add(newItem);
            });
            ListSerialization.ReadListFromXML(reader, readItemDelegate);

            m_skills = new List<Skill>();
            ReadListFromXMLCore readSkillDelegate = new ReadListFromXMLCore(delegate
            {
                string skillName = reader.ReadElementContentAsString();
                m_skills.Add(SkillFactory.CreateSkill(skillName));
            });
            ListSerialization.ReadListFromXML(reader, readSkillDelegate);
            reader.ReadEndElement();
        }

        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Player");
            base.WriteXml(writer);

            writer.WriteElementString("BaseCurrentHealth", m_currentHealth.ToString());
            writer.WriteElementString("BaseMaxHealth", m_baseMaxHealth.ToString());

            writer.WriteElementString("BaseCurrentStamina", m_currentStamina.ToString());
            writer.WriteElementString("BaseMaxStamina", m_baseMaxStamina.ToString());

            writer.WriteElementString("BaseCurrentMagic", m_baseCurrentMP.ToString());
            writer.WriteElementString("BaseMaxMagic", m_baseMaxMP.ToString());

            writer.WriteElementString("SkillPoints", SkillPoints.ToString());

            writer.WriteElementString("LastTurnSeenAMonster", LastTurnSeenAMonster.ToString());

            Item.WriteXmlEntireNode((Item)ChestArmor, "ChestArmor", writer);
            Item.WriteXmlEntireNode((Item)Headpiece, "Headpiece", writer);
            Item.WriteXmlEntireNode((Item)Gloves, "Gloves", writer);
            Item.WriteXmlEntireNode((Item)Boots, "Boots", writer);

            ListSerialization.WriteListToXML(writer, m_itemList, "Items");
            ListSerialization.WriteListToXML(writer, m_skills, "Skills");

            writer.WriteEndElement();
        }

        #endregion
    }
}
