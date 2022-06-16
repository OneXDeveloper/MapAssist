using MapAssist.Helpers;
using MapAssist.Settings;
using MapAssist.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Stat = MapAssist.Types.Stats.Stat;

namespace MapAssist.Types
{
    public static class StatReader
    {
        private class StatLayerValue
        {
            public ushort Layer;
            public int Value;
        }

        public static List<string> GetStatsText(UnitItem item, UnitPlayer player)
        {
            var statsText = new List<string>();

            var blueStats = new Dictionary<Stat, StatLayerValue[]>();

            void AddBlueStats(Dictionary<Stat, StatLayerValue[]> statsList)
            {
                foreach (var stat in statsList.Keys)
                {
                    if (blueStats.ContainsKey(stat))
                    {
                        var layers = blueStats[stat].Select(x => x.Layer).Concat(statsList[stat].Select(x => x.Layer).Reverse()).Distinct().ToArray();

                        blueStats[stat] = layers.Select(layer =>
                        {
                            var existingStat = blueStats[stat].FirstOrDefault(x => x.Layer == layer);
                            var addStat = statsList[stat].FirstOrDefault(x => x.Layer == layer);

                            return new StatLayerValue { Layer = layer, Value = (existingStat?.Value ?? 0) + (addStat?.Value ?? 0) };
                        }).Where(x => x.Value != 0).ToArray();
                    }
                    else
                    {
                        blueStats.Add(stat, statsList[stat].Reverse().ToArray());
                    }
                }
            }

            string GetTextFormat(string key)
            {
                if (!LocalizedStatText.TryGetValue(key, out var localItem)) return "";

                var lang = MapAssistConfiguration.Loaded.LanguageCode;
                var prop = localItem.GetType().GetProperty(lang.ToString()).GetValue(localItem, null);

                return prop.ToString();
            }

            bool TryFormatGroupedStat(string formatTextKey, Stat[] statsInGroup)
            {
                var propertyStatsArgs = statsInGroup
                        .Select(x => blueStats.TryGetValue(x, out var statsLayers) ? statsLayers.Select(y => new { stat = x, y.Layer, y.Value }).ToArray() : null)
                        .Where(x => x != null)
                        .ToArray();

                if (statsInGroup.Length == propertyStatsArgs.Length)
                {
                    var args = propertyStatsArgs.Select(x => StatArgs(item, player, x[0].stat, x[0].Layer, x[0].Value)).SelectMany(x => x).ToArray();

                    if (formatTextKey == "strModPoisonDamageRange")
                    {
                        var duration = (int)args[2] / 25;
                        var multiplier = duration / 10.24; // Extra decimals to fix stupid rounding bugs in the next lines

                        var min = Math.Round(((int)args[0]) * multiplier, 0, MidpointRounding.AwayFromZero);
                        var max = Math.Round(((int)args[1]) * multiplier, 0, MidpointRounding.AwayFromZero);

                        args = new object[] { (int)min, (int)max, duration };
                    }

                    if (formatTextKey.EndsWith("Range") && (int)args[0] == (int)args[1])
                    {
                        formatTextKey = formatTextKey.Replace("Range", "");
                        args = args.Skip(1).ToArray();
                    }

                    var formatText = GetTextFormat(formatTextKey);
                    formatText = new Regex(@"\%\d").Replace(formatText, "$0s");

                    var text = CTools.sprintf(formatText, args);
                    statsText.Add(text);

                    foreach (var groupStat in statsInGroup) blueStats.Remove(groupStat);
                    return true;
                }

                return false;
            }

            bool TryFormatStat(Stat stat)
            {
                if (!blueStats.TryGetValue(stat, out var statsList)) return false;

                var propertyStat = StatCost[stat];

                if (propertyStat.DescGrp != null) // All stats in group have the same value (all attributes or all resistances)
                {
                    var statsInGroup = StatCost.Where(x => x.Value.DescGrp == propertyStat.DescGrp).Select(x => x.Key).ToArray();

                    if (TryFormatGroupedStat(propertyStat.DescGrp, statsInGroup)) return true;
                }

                foreach (var customGroupedStat in CustomGroupedStats) // Defined groups like "Add x - y damage"
                {
                    foreach (var statsInGroup in customGroupedStat.Value)
                    {
                        if (!statsInGroup.Contains(stat)) continue;

                        if (TryFormatGroupedStat(customGroupedStat.Key, statsInGroup)) return true;
                    }
                }

                if (propertyStat.DescStrPos != null)
                {
                    foreach (var stats in statsList)
                    {
                        var args = StatArgs(item, player, stat, stats.Layer, stats.Value).ToList();

                        var isNegative = decimal.TryParse(args[0].ToString(), out var parseValue) && parseValue < 0;

                        var formatText = isNegative ? propertyStat.DescStrNeg : propertyStat.DescStrPos;

                        switch (stat)
                        {
                            case Stat.ReplenishDurability:
                                args = new object[] { 1 }.Concat(args).ToList();
                                formatText = "ModStre9u"; // Data file seems wrong, this is actually what's in game
                                break;

                            case Stat.AddSkillTab:
                                var skillTree = (SkillTree)args.FirstOrDefault(x => x is SkillTree);
                                var skillTreePlayerClass = skillTree.GetPlayerClass();

                                formatText = CharStats[skillTreePlayerClass].StrSkillTabs[skillTree];
                                break;

                            case Stat.AddClassSkills:
                                var playerClass = (PlayerClass)args.FirstOrDefault(x => x is PlayerClass);

                                formatText = CharStats[playerClass].StrAllSkills;
                                break;
                        }

                        formatText = GetTextFormat(formatText) + (propertyStat.DescStr2 != null ? " " + GetTextFormat(propertyStat.DescStr2) : "");
                        formatText = new Regex(@"\%\d").Replace(formatText, "$0s");

                        switch (stat)
                        {
                            case Stat.SingleSkill:
                                var skill = (Skill)args.FirstOrDefault(x => x is Skill);
                                var skillPlayerClass = skill.GetPlayerClass();

                                args.Add(GetTextFormat(CharStats[skillPlayerClass].StrClassOnly));
                                break;

                            case Stat.AddSkillTab:
                                var skillTree = (SkillTree)args.FirstOrDefault(x => x is SkillTree);
                                var skillTreePlayerClass = skillTree.GetPlayerClass();

                                formatText += " " + GetTextFormat(CharStats[skillTreePlayerClass].StrClassOnly);
                                break;
                        }

                        args = args.Select(x => x.GetType().IsEnum ? x.ToString().AddSpaces() : x).ToList<object>();
                        var text = CTools.sprintf(formatText, args.ToArray());
                        statsText.Add(text);
                    }

                    blueStats.Remove(stat);
                    return true;
                }

                return false;
            }

            void AddSocketEthSuffix()
            {
                var hasSockets = item.Stats.TryGetValue(Stat.NumSockets, out var numSockets);

                if (hasSockets && item.IsEthereal)
                {
                    var formatText = GetTextFormat("strItemModEtherealSocketed");

                    var text = CTools.sprintf(formatText, new object[] { numSockets });
                    statsText.Add(text);
                }
                else if (hasSockets)
                {
                    var formatText = GetTextFormat("Socketable");

                    var text = CTools.sprintf(formatText, new object[] { numSockets });
                    statsText.Add(text);
                }
                else if (item.IsEthereal)
                {
                    var formatText = GetTextFormat("strethereal");

                    var text = CTools.sprintf(formatText, new object[] { });
                    statsText.Add(text);
                }
            }

            foreach (var socketItem in item.Sockets)
            {
                var socketItemStats = socketItem.StatLayers?.ToDictionary(x => x.Key, x => x.Value.Select(y => new StatLayerValue { Layer = y.Key, Value = y.Value }).ToArray());

                AddBlueStats(socketItemStats);
            }

            var addedStats = item.StatLayersAdded.ToDictionary(x => x.Key, x => x.Value.Select(y => new StatLayerValue { Layer = y.Key, Value = y.Value }).ToArray());
            AddBlueStats(addedStats);

            var staffMods = item.StaffModsLayers.ToDictionary(x => x.Key, x => x.Value.Select(y => new StatLayerValue { Layer = y.Key, Value = y.Value }).ToArray());
            AddBlueStats(staffMods);

            var orderedStats = StatCost.Select(x => new { x.Key, x.Value })
                .OrderByDescending(x => x.Value.DescPriority)
                .ThenBy(x => blueStats.Keys.ToList().IndexOf(x.Key))
                .Select(x => x.Key)
                .ToArray();

            foreach (var stat in orderedStats)
            {
                TryFormatStat(stat);
            }

            AddSocketEthSuffix();

            return statsText;
        }

        private static object[] StatArgs(UnitItem item, UnitPlayer player, Stat stat, ushort layer, int value)
        {
            value = (int)GetAdjustedStatValue(stat, value, player.Level);

            switch (StatCost[stat].DescFunc)
            {
                case 5:
                    return new object[] { (int)(value / 1.28) };

                case 11:
                    return new object[] { (int)Math.Round(100.0 / value) };

                case 13:
                    var (classSkills, classPoints) = GetItemStatAddClassSkills(item, (Structs.PlayerClass)layer, false);

                    return new object[] { classPoints, classSkills[0] };

                case 14:
                    var (skillTrees, skillPoints) = GetItemStatAddSkillTreeSkills(item, (SkillTree)layer, false);

                    return new object[] { skillPoints, skillTrees[0] };

                case 15:
                    var skill = (Skill)(layer >> 6);
                    var level = layer % (1 << 6);

                    return new object[] { value, level, skill };

                case 16:
                    return new object[] { value, (Skill)layer };

                case 22:
                    return new object[] { value, (Npc)layer };

                case 23:
                    return new object[] { value, (Npc)layer }; // Unsure about the NPC type here
                case 24:
                    var chargesSkill = (Skill)(layer >> 6);
                    var (skillLevel, currentCharges, maxCharges) = GetItemStatAddSkillCharges(item, chargesSkill);

                    return new object[] { skillLevel, chargesSkill, currentCharges, maxCharges };

                case 27:
                case 28:
                    var (singleSkills, points) = GetItemStatAddSingleSkills(item, (Skill)layer, false);

                    return new object[] { points, singleSkills[0] };
            }

            return new object[] { value };
        }

        private static Dictionary<string, Stat[][]> CustomGroupedStats = new Dictionary<string, Stat[][]>()
        {
            { "strModMinDamageRange", new[] {
                new[] { Stat.MinDamage, Stat.MaxDamage, Stat.TwoHandedMinDamage, Stat.TwoHandedMaxDamage },
                new[] { Stat.MinDamage, Stat.MaxDamage }
            }},
            { "ModStr1g", new[] { new[] { Stat.MinDamage, Stat.TwoHandedMinDamage } } },
            { "ModStr1f", new[] { new[] { Stat.MaxDamage, Stat.TwoHandedMaxDamage } } },
            { "strModEnhancedDamage", new[] { new[] { Stat.EnhancedDamage, Stat.EnhancedDamageMax } } },
            { "strModFireDamageRange", new[] { new[] { Stat.FireMinDamage, Stat.FireMaxDamage } } },
            { "strModLightningDamageRange", new[] { new[] { Stat.LightningMinDamage, Stat.LightningMaxDamage } } },
            { "strModColdDamageRange", new[] { new[] { Stat.ColdMinDamage, Stat.ColdMaxDamage } } },
            { "strModPoisonDamageRange", new[] { new[] { Stat.PoisonMinDamage, Stat.PoisonMaxDamage, Stat.PoisonLength } } },
            { "strModMagicDamageRange", new[] { new[] { Stat.MagicMinDamage, Stat.MagicMaxDamage } } },
        };

        public static int? GetQualityLevel(UnitItem item)
        {
            string itemCode;
            if (!Items._ItemCodes.TryGetValue(item.TxtFileNo, out itemCode))
            {
                return null;
            }

            string namedCode;
            switch (item.ItemData.ItemQuality)
            {
                case ItemQuality.UNIQUE:
                    if (Items._UniqueFromCode.TryGetValue(itemCode, out namedCode) && namedCode != "Unique")
                    {
                        itemCode = namedCode;
                    }

                    break;

                case ItemQuality.SET:
                    if (Items._SetFromCode.TryGetValue(itemCode, out namedCode) && namedCode != "Set")
                    {
                        itemCode = namedCode;
                    }
                    break;
            }

            QualityLevelsObj qualityLevel;
            if (!Items.QualityLevels.TryGetValue(itemCode, out qualityLevel))
            {
                return null;
            }

            return qualityLevel.qlvl;
        }

        public static ItemTier GetItemTier(UnitItem item)
        {
            return GetItemTier(item.Item);
        }

        public static ItemTier GetItemTier(Item item)
        {
            var itemClasses = Items.ItemClasses.Where(x => x.Value.Contains(item));
            if (itemClasses.Count() == 0) return ItemTier.NotApplicable;

            var itemClass = itemClasses.First();
            if (itemClass.Key == Item.ClassCirclets) return ItemTier.NotApplicable;

            return (ItemTier)(Array.IndexOf(itemClass.Value, item) * 3 / itemClass.Value.Length); // All items within each class (except circlets) come in equal amounts within each tier
        }

        public static double GetAdjustedStatValue(Stat stat, int value, int playerLevel = 1, bool adjustNegativeStats = false)
        {
            var property = StatCost[stat];

            double newValue = value >> property.ValShift;

            if (StatPerLevelDivisors.TryGetValue(stat, out var divisor))
            {
                newValue = newValue / (double)divisor * playerLevel;
            }

            if (adjustNegativeStats && NegativeValueStats.Contains(stat)) newValue *= -1;

            return newValue;
        }

        public static int GetStatValue(UnitItem item, Stat stat)
        {
            return item.Stats.TryGetValue(stat, out var statValue) ? statValue :
                item.StatsAdded != null && item.StatsAdded.TryGetValue(stat, out var statAddedValue) ? statAddedValue :
                item.StaffMods != null && item.StaffMods.TryGetValue(stat, out var staffModValue) ? staffModValue : 0;
        }

        public static int GetStatAllResists(UnitItem item, bool sumOfEach)
        {
            item.Stats.TryGetValue(Stat.FireResist, out var fireRes);
            item.Stats.TryGetValue(Stat.LightningResist, out var lightRes);
            item.Stats.TryGetValue(Stat.ColdResist, out var coldRes);
            item.Stats.TryGetValue(Stat.PoisonResist, out var psnRes);
            var resistances = new[] { fireRes, lightRes, coldRes, psnRes };
            return sumOfEach ? resistances.Sum() : resistances.Min();
        }

        public static int GetStatAllAttributes(UnitItem item)
        {
            item.Stats.TryGetValue(Stat.Strength, out var strength);
            item.Stats.TryGetValue(Stat.Dexterity, out var dexterity);
            item.Stats.TryGetValue(Stat.Vitality, out var vitality);
            item.Stats.TryGetValue(Stat.Energy, out var energy);
            return new[] { strength, dexterity, vitality, energy }.Min();
        }

        public static (Structs.PlayerClass[], int) GetItemStatAddClassSkills(UnitItem item, Structs.PlayerClass playerClass, bool addAllSkills = true)
        {
            var allSkills = GetStatValue(item, Stat.AllSkills);

            if (playerClass == Structs.PlayerClass.Any)
            {
                var maxClassSkills = 0;
                var maxClasses = new List<Structs.PlayerClass>();

                for (var classId = Structs.PlayerClass.Amazon; classId <= Structs.PlayerClass.Assassin; classId++)
                {
                    if (item.StatLayers.TryGetValue(Stat.AddClassSkills, out var anyItemStats) &&
                        anyItemStats.TryGetValue((ushort)classId, out var anyClassSkills))
                    {
                        if (anyClassSkills > maxClassSkills)
                        {
                            maxClasses = new List<Structs.PlayerClass>() { classId };
                            maxClassSkills = anyClassSkills;
                        }
                        else if (anyClassSkills == maxClassSkills)
                        {
                            maxClasses.Add(classId);
                        }
                    }
                }

                return (maxClasses.ToArray(), allSkills + maxClassSkills);
            }

            if (item.StatLayers.TryGetValue(Stat.AddClassSkills, out var itemStats) &&
                itemStats.TryGetValue((ushort)playerClass, out var addClassSkills))
            {
                return (new Structs.PlayerClass[] { playerClass }, allSkills + addClassSkills);
            }

            return (new Structs.PlayerClass[] { playerClass }, allSkills);
        }

        public static (SkillTree[], int) GetItemStatAddSkillTreeSkills(UnitItem item, SkillTree skillTree, bool addClassSkills = true)
        {
            if (skillTree == SkillTree.Any)
            {
                var maxSkillTreeQuantity = 0;
                var maxSkillTrees = new List<SkillTree>();

                foreach (var skillTreeId in Enum.GetValues(typeof(SkillTree)).Cast<SkillTree>().Where(x => x != SkillTree.Any).ToList())
                {
                    if (item.StatLayers.TryGetValue(Stat.AddSkillTab, out var anyItemStats) &&
                        anyItemStats.TryGetValue((ushort)skillTreeId, out var anyTabSkills))
                    {
                        anyTabSkills += addClassSkills ? GetItemStatAddClassSkills(item, skillTreeId.GetPlayerClass()).Item2 : 0; // This adds the +class skill points and +all skills points

                        if (anyTabSkills > maxSkillTreeQuantity)
                        {
                            maxSkillTrees = new List<SkillTree>() { skillTreeId };
                            maxSkillTreeQuantity = anyTabSkills;
                        }
                        else if (anyTabSkills == maxSkillTreeQuantity)
                        {
                            maxSkillTrees.Add(skillTreeId);
                        }
                    }
                }

                return (maxSkillTrees.ToArray(), maxSkillTreeQuantity);
            }

            var baseAddSkills = addClassSkills ? GetItemStatAddClassSkills(item, skillTree.GetPlayerClass()).Item2 : 0; // This adds the +class skill points and +all skills points

            if (item.StatLayers.TryGetValue(Stat.AddSkillTab, out var itemStats) &&
                itemStats.TryGetValue((ushort)skillTree, out var addSkillTab))
            {
                return (new SkillTree[] { skillTree }, baseAddSkills + addSkillTab);
            }

            return (new SkillTree[] { skillTree }, baseAddSkills);
        }

        public static (Skill[], int) GetItemStatAddSingleSkills(UnitItem item, Skill skill, bool addSkillTree = true)
        {
            var itemSkillsStats = new List<Stat>()
            {
                Stat.SingleSkill,
                Stat.NonClassSkill,
            };

            if (skill == Skill.Any)
            {
                var maxSkillQuantity = 0;
                var maxSkills = new List<Skill>();

                foreach (var statType in itemSkillsStats)
                {
                    foreach (var skillId in SkillExtensions.SkillTreeToSkillDict.SelectMany(x => x.Value).ToList())
                    {
                        if (item.StatLayers.TryGetValue(statType, out var anyItemStats) &&
                            anyItemStats.TryGetValue((ushort)skillId, out var anySkillLevel))
                        {
                            anySkillLevel += (addSkillTree && statType == Stat.SingleSkill ? GetItemStatAddSkillTreeSkills(item, skillId.GetSkillTree()).Item2 : 0); // This adds the +skill tree points, +class skill points and +all skills points

                            if (anySkillLevel > maxSkillQuantity)
                            {
                                maxSkills = new List<Skill>() { skillId };
                                maxSkillQuantity = anySkillLevel;
                            }
                            else if (anySkillLevel == maxSkillQuantity)
                            {
                                maxSkills.Add(skillId);
                            }
                        }
                    }
                }

                return (maxSkills.ToArray(), maxSkillQuantity);
            }

            var baseAddSkills = addSkillTree ? GetItemStatAddSkillTreeSkills(item, skill.GetSkillTree()).Item2 : 0; // This adds the +skill tree points, +class skill points and +all skills points

            foreach (var statType in itemSkillsStats)
            {
                if (item.StatLayers.TryGetValue(statType, out var itemStats) &&
                    itemStats.TryGetValue((ushort)skill, out var skillLevel))
                {
                    return (new Skill[] { skill }, (statType == Stat.SingleSkill ? baseAddSkills : 0) + skillLevel);
                }
            }

            return (new Skill[] { skill }, baseAddSkills);
        }

        public static (int, int, int) GetItemStatAddSkillCharges(UnitItem item, Skill skill)
        {
            if (item.StatLayers.TryGetValue(Stat.ItemChargedSkill, out var itemStats))
            {
                foreach (var stat in itemStats)
                {
                    var skillId = stat.Key >> 6;
                    var level = stat.Key % (1 << 6);

                    if (skillId == (int)skill && itemStats.TryGetValue(stat.Key, out var data))
                    {
                        var maxCharges = data >> 8;
                        var currentCharges = data % (1 << 8);

                        return (level, currentCharges, maxCharges);
                    }
                }
            }
            return (0, 0, 0);
        }

        //public static Dictionary<Stat, double> StatDivisors = new Dictionary<Stat, double>()
        //{
        //    [Stat.PoisonLength] = 25,
        //};

        private static List<Stat> NegativeValueStats = new List<Stat>()
        {
            Stat.EnemyFireResist,
            Stat.EnemyLightningResist,
            Stat.EnemyColdResist,
            Stat.EnemyPoisonResist,
            Stat.TargetDefense,
        };

        public static Dictionary<string, LocalizedObj> LocalizedStatText = new Dictionary<string, LocalizedObj>();

        private static Dictionary<Stat, StatMetaData> StatCost = ExcelDataLoader.Parse(Properties.Resources.ItemStatCost).ToDictionary(x => (Stat)int.Parse(x["_index"]), x => new StatMetaData(x));

        private static Dictionary<PlayerClass, CharStatsData> CharStats = ExcelDataLoader.Parse(Properties.Resources.CharStats).Select(x => new CharStatsData(x)).ToDictionary(x => x.PlayerClass, x => x);

        public static Dictionary<Stat, int> StatPerLevelDivisors = ExcelDataLoader.Parse(Properties.Resources.Properties)
            .Select(x => new { Key = StatProperty.GetStat(x), Value = StatProperty.GetDivisor(x) })
            .Where(x => x.Key != Stat.Invalid && x.Value != 0)
            .ToDictionary(x => x.Key, x => x.Value);

        public class StatMetaData
        {
            public string Stat { get; private set; }
            public int Encode { get; private set; }
            public int ValShift { get; private set; }
            public int DescPriority { get; private set; }
            public int DescFunc { get; private set; }
            public string DescStrPos { get; private set; }
            public string DescStrNeg { get; private set; }
            public string DescGrp { get; private set; }
            public string DescStr2 { get; private set; }

            public StatMetaData(Dictionary<string, string> data)
            {
                Stat = string.IsNullOrWhiteSpace(data["Stat"]) ? null : data["Stat"];
                Encode = string.IsNullOrWhiteSpace(data["Encode"]) ? 0 : int.Parse(data["Encode"]);
                ValShift = string.IsNullOrWhiteSpace(data["ValShift"]) ? 0 : int.Parse(data["ValShift"]);
                DescPriority = string.IsNullOrWhiteSpace(data["descpriority"]) ? 0 : int.Parse(data["descpriority"]);
                DescFunc = string.IsNullOrWhiteSpace(data["descfunc"]) ? 0 : int.Parse(data["descfunc"]);
                DescStrPos = string.IsNullOrWhiteSpace(data["descstrpos"]) ? null : data["descstrpos"];
                DescStrNeg = string.IsNullOrWhiteSpace(data["descstrneg"]) ? null : data["descstrneg"];
                DescGrp = string.IsNullOrWhiteSpace(data["dgrpstrpos"]) ? null : data["dgrpstrpos"];
                DescStr2 = string.IsNullOrWhiteSpace(data["descstr2"]) ? null : data["descstr2"];
            }
        }

        public class CharStatsData
        {
            public PlayerClass PlayerClass { get; private set; }
            public string StrAllSkills { get; private set; }
            public Dictionary<SkillTree, string> StrSkillTabs { get; private set; }
            public string StrClassOnly { get; private set; }

            public CharStatsData(Dictionary<string, string> data)
            {
                var skillTrees = Enum.GetValues(typeof(SkillTree)).Cast<SkillTree>().ToArray();
                Enum.TryParse<PlayerClass>(data["class"], out var playerClass);

                PlayerClass = playerClass;
                StrAllSkills = data["StrAllSkills"];
                StrSkillTabs = Enumerable.Range(0, 3).ToDictionary(x => skillTrees[(int)PlayerClass * 3 + x], x => data[$"StrSkillTab{x + 1}"]);
                StrClassOnly = data["StrClassOnly"];
            }
        }

        private static class StatProperty
        {
            public static Stat GetStat(Dictionary<string, string> data)
            {
                var stat1 = string.IsNullOrWhiteSpace(data[$"stat1"]) ? "" : data[$"stat1"];
                var stat = StatCost.Select(p => new { p.Key, p.Value }).FirstOrDefault(y => y.Value.Stat == stat1)?.Key;

                return stat ?? Stat.Invalid;
            }

            public static int GetDivisor(Dictionary<string, string> data)
            {
                var text = string.IsNullOrWhiteSpace(data["*Parameter"]) ? null : data["*Parameter"];

                if (text != null)
                {
                    if (text.EndsWith(" per Level"))
                    {
                        return int.Parse(text.Replace("#/", "").Replace(" per Level", ""));
                    }
                    else if (text.StartsWith("ac/lvl "))
                    {
                        return int.Parse(text.Replace("ac/lvl (", "").Replace("ths)", ""));
                    }
                }

                return 0;
            }
        }
    }
}
