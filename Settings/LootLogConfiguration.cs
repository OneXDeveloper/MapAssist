using MapAssist.Files;
using MapAssist.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace MapAssist.Settings
{
    public class LootLogConfiguration
    {
        public static Dictionary<Item, List<ItemFilter>> Filters { get; set; }

        public static void Load()
        {
            Filters = ConfigurationParser<Dictionary<Item, List<ItemFilter>>>.ParseConfigurationFile($"./{MapAssistConfiguration.Loaded.ItemLog.FilterFileName}");

            for (var itemClass = Item.ClassAxes; ; itemClass += 1)
            {
                if (!Enum.IsDefined(typeof(Item), itemClass)) break;
                if (!Filters.ContainsKey(itemClass)) continue;

                Action<ItemFilter> assignRule = (rule) =>
                {
                    var classItems = Items.ItemClasses[itemClass].Where(item => itemClass == Item.ClassCirclets || rule == null || rule.Tiers == null || rule.Tiers.Contains(Items.GetItemTier(item))).ToArray();

                    foreach (var item in classItems)
                    {
                        if (Filters.ContainsKey(item) && Filters[item] == null) continue; // null rule so everything is already being returned

                        if (Filters.ContainsKey(item))
                        {
                            if (rule == null) Filters[item] = null; // replace with a null rule
                            else Filters[item].Add(rule);
                        }
                        else
                        {
                            if (rule == null) Filters.Add(item, null);
                            else Filters.Add(item, new List<ItemFilter> { rule });
                        }
                    }
                };

                if (Filters[itemClass] == null)
                {
                    assignRule(null);
                }
                else
                {
                    foreach (var rule in Filters[itemClass])
                    {
                        assignRule(rule);
                    }
                }
            }

            // Validation for Enhanced Damage
            foreach (var item in Filters.Where(kv => kv.Value != null && kv.Value.Exists(x => x.EnhancedDamage != null)))
            {
                if (item.Key != Item.Any && item.Key != Item.Jewel && item.Key != Item.ClassPaladinShields && Items.ItemClasses.FirstOrDefault(x => x.Value.Contains(item.Key)).Key != Item.ClassPaladinShields)
                {
                    throw new Exception($"Enhanced Damage was found on an ItemFilter rule for {item.Key}.\n\nIt is currently supported for Jewels and Paladin Shields.");
                }
            }
        }
    }

    public class ItemFilter
    {
        public object this[Stats.Stat stat]
        {
            get { return GetType().GetProperty(stat.ToString()).GetValue(this, null); }
            set { GetType().GetProperty(stat.ToString()).SetValue(this, value, null); }
        }

        public ItemTier[] Tiers { get; set; }
        public bool PlaySoundOnDrop { get; set; } = true;
        public ItemQuality[] Qualities { get; set; }
        public int[] Sockets { get; set; }
        public bool? Ethereal { get; set; }
        public int? Defense { get; set; }
        public int? Strength { get; set; }
        public int? Dexterity { get; set; }
        public int? Vitality { get; set; }
        public int? Energy { get; set; }

        [YamlMember(Alias = "All Attributes")]
        public int? AllAttributes { get; set; }

        [YamlMember(Alias = "Max Life")]
        public int? MaxLife { get; set; }

        [YamlMember(Alias = "Max Mana")]
        public int? MaxMana { get; set; }

        [YamlMember(Alias = "Attack Rating")]
        public int? AttackRating { get; set; }

        [YamlMember(Alias = "Min Damage")]
        public int? MinDamage { get; set; }

        [YamlMember(Alias = "Max Damage")]
        public int? MaxDamage { get; set; }

        [YamlMember(Alias = "Damage Reduced")]
        public int? DamageReduced { get; set; }

        [YamlMember(Alias = "Life Steal")]
        public int? LifeSteal { get; set; }

        [YamlMember(Alias = "Mana Steal")]
        public int? ManaSteal { get; set; }

        [YamlMember(Alias = "Cold Skill Damage")]
        public int? ColdSkillDamage { get; set; }

        [YamlMember(Alias = "Lightning Skill Damage")]
        public int? LightningSkillDamage { get; set; }

        [YamlMember(Alias = "Fire Skill Damage")]
        public int? FireSkillDamage { get; set; }

        [YamlMember(Alias = "Poison Skill Damage")]
        public int? PoisonSkillDamage { get; set; }

        [YamlMember(Alias = "Increased Attack Speed")]
        public int? IncreasedAttackSpeed { get; set; }

        [YamlMember(Alias = "Faster Run Walk")]
        public int? FasterRunWalk { get; set; }

        [YamlMember(Alias = "Faster Hit Recovery")]
        public int? FasterHitRecovery { get; set; }

        [YamlMember(Alias = "Faster Cast Rate")]
        public int? FasterCastRate { get; set; }

        [YamlMember(Alias = "Magic Find")]
        public int? MagicFind { get; set; }

        [YamlMember(Alias = "Gold Find")]
        public int? GoldFind { get; set; }

        [YamlMember(Alias = "Cold Resist")]
        public int? ColdResist { get; set; }

        [YamlMember(Alias = "Lightning Resist")]
        public int? LightningResist { get; set; }

        [YamlMember(Alias = "Fire Resist")]
        public int? FireResist { get; set; }

        [YamlMember(Alias = "Poison Resist")]
        public int? PoisonResist { get; set; }

        [YamlMember(Alias = "Sum Resist")]
        public int? SumResist { get; set; }

        [YamlMember(Alias = "All Resist")]
        public int? AllResist { get; set; }

        [YamlMember(Alias = "All Skills")]
        public int? AllSkills { get; set; }

        [YamlMember(Alias = "Class Skills")]
        public Dictionary<Structs.PlayerClass, int?> ClassSkills { get; set; } = new Dictionary<Structs.PlayerClass, int?>();

        [YamlMember(Alias = "Class Skill Tree")]
        public Dictionary<SkillTree, int?> SkillTrees { get; set; } = new Dictionary<SkillTree, int?>();

        [YamlMember(Alias = "Skills")]
        public Dictionary<Skill, int?> Skills { get; set; } = new Dictionary<Skill, int?>();

        [YamlMember(Alias = "Skill Charges")]
        public Dictionary<Skill, int?> SkillCharges { get; set; } = new Dictionary<Skill, int?>();

        [YamlMember(Alias = "Faster Block Rate")]
        public int? FasterBlockRate { get; set; }

        [YamlMember(Alias = "Deadly Strike")]
        public int? DeadlyStrike { get; set; }

        [YamlMember(Alias = "Crushing Blow")]
        public int? CrushingBlow { get; set; }

        [YamlMember(Alias = "Open Wounds")]
        public int? OpenWounds { get; set; }

        [YamlMember(Alias = "Cannot Be Frozen")]
        public int? CannotBeFrozen { get; set; }

        [YamlMember(Alias = "Slain Monsters Rest In Peace")]
        public int? SlainMonstersRestInPeace { get; set; }

        [YamlMember(Alias = "Prevent Monster Heal")]
        public int? PreventMonsterHeal { get; set; }

        [YamlMember(Alias = "Absorb Cold Percent")]
        public int? AbsorbColdPercent { get; set; }

        [YamlMember(Alias = "Absorb Fire Percent")]
        public int? AbsorbFirePercent { get; set; }

        [YamlMember(Alias = "Absorb Lightning Percent")]
        public int? AbsorbLightningPercent { get; set; }

        [YamlMember(Alias = "Max Cold Resist")]
        public int? MaxColdResist { get; set; }

        [YamlMember(Alias = "Max Lightning Resist")]
        public int? MaxLightningResist { get; set; }

        [YamlMember(Alias = "Max Fire Resist")]
        public int? MaxFireResist { get; set; }

        [YamlMember(Alias = "Max Poison Resist")]
        public int? MaxPoisonResist { get; set; }

        [YamlMember(Alias = "Enemy Fire Resist")]
        public int? EnemyFireResist { get; set; }

        [YamlMember(Alias = "Enemy Lightning Resist")]
        public int? EnemyLightningResist { get; set; }

        [YamlMember(Alias = "Enemy Cold Resist")]
        public int? EnemyColdResist { get; set; }

        [YamlMember(Alias = "Enemy Poison Resist")]
        public int? EnemyPoisonResist { get; set; }

        [YamlMember(Alias = "Max Life Percent")]
        public int? MaxLifePercent { get; set; }

        [YamlMember(Alias = "Max Mana Percent")]
        public int? MaxManaPercent { get; set; }

        [YamlMember(Alias = "Damage Taken Goes To Mana")]
        public int? DamageTakenGoesToMana { get; set; }

        [YamlMember(Alias = "Enhanced Damage")]
        public int? EnhancedDamage { get; set; }

        [YamlMember(Alias = "Min Area Level")]
        public int? MinAreaLevel { get; set; }

        [YamlMember(Alias = "Max Area Level")]
        public int? MaxAreaLevel { get; set; }

        [YamlMember(Alias = "Min Player Level")]
        public int? MinPlayerLevel { get; set; }

        [YamlMember(Alias = "Max Player Level")]
        public int? MaxPlayerLevel { get; set; }
    }

    public static class ItemFilterExtensions
    {
        public static bool TargetsUnidItem(this ItemFilter rule)
        {
            if (rule == null) return true;

            foreach (var property in rule.GetType().GetProperties())
            {
                if (property.Name == "Defense") continue;

                var propType = property.PropertyType;
                if (propType == typeof(object)) continue;

                var propertyValue = rule.GetType().GetProperty(property.Name).GetValue(rule, null);
                if (propertyValue != null && propType == typeof(int?) && (int)propertyValue > 0)
                {
                    return false;
                }
            }

            if (rule.Skills.Where(subrule => subrule.Value != null).Count() > 0) return false;
            if (rule.SkillCharges.Where(subrule => subrule.Value != null).Count() > 0) return false;
            if (rule.ClassSkills.Where(subrule => subrule.Value != null).Count() > 0) return false;
            if (rule.SkillTrees.Where(subrule => subrule.Value != null).Count() > 0) return false;

            return true;
        }
    }
}
