/**
 *   Copyright (C) 2021 okaygo
 *
 *   https://github.com/misterokaygo/MapAssist/
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <https://www.gnu.org/licenses/>.
 **/

using MapAssist.Helpers;
using MapAssist.Settings;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using YamlDotNet.Serialization;

namespace MapAssist.Types
{
    public class Items
    {
        public static Dictionary<int, HashSet<string>> ItemUnitHashesSeen = new Dictionary<int, HashSet<string>>();
        public static Dictionary<int, HashSet<uint>> ItemUnitIdsSeen = new Dictionary<int, HashSet<uint>>();
        public static Dictionary<int, HashSet<uint>> ItemUnitIdsToSkip = new Dictionary<int, HashSet<uint>>();
        public static Dictionary<int, HashSet<uint>> InventoryItemUnitIdsToSkip = new Dictionary<int, HashSet<uint>>();
        public static Dictionary<int, Dictionary<uint, Npc>> ItemVendors = new Dictionary<int, Dictionary<uint, Npc>>();
        public static Dictionary<int, List<ItemLogEntry>> ItemLog = new Dictionary<int, List<ItemLogEntry>>();
        public static Dictionary<string, LocalizedObj> LocalizedItems = new Dictionary<string, LocalizedObj>();

        public static void LogItem(UnitItem item, int processId)
        {
            if (CheckDroppedItem(item, processId) || CheckInventoryItem(item, processId) || CheckVendorItem(item, processId))
            {
                if (item.IsInStore)
                {
                    InventoryItemUnitIdsToSkip[processId].Add(item.UnitId);
                }
                else
                {
                    ItemUnitHashesSeen[processId].Add(item.HashString);
                }

                if (item.IsPlayerOwned && item.IsIdentified)
                {
                    InventoryItemUnitIdsToSkip[processId].Add(item.UnitId);
                    ItemUnitIdsToSkip[processId].Add(item.UnitId);
                }

                ItemUnitIdsSeen[processId].Add(item.UnitId);

                var (logItem, rule) = LootFilter.Filter(item);
                if (!logItem) return;

                if (MapAssistConfiguration.Loaded.ItemLog.PlaySoundOnDrop && (rule == null || rule.PlaySoundOnDrop))
                {
                    AudioPlayer.PlayItemAlert();
                }

                ItemLog[processId].Add(new ItemLogEntry()
                {
                    Text = ItemLogDisplayName(item, rule),
                    Color = item.ItemBaseColor,
                    ItemHashString = item.HashString,
                    UnitItem = item
                });
            }
        }

        private static bool CheckInventoryItem(UnitItem item, int processId) =>
            item.IsIdentified && item.IsPlayerOwned &&
            !InventoryItemUnitIdsToSkip[processId].Contains(item.UnitId);

        private static bool CheckDroppedItem(UnitItem item, int processId) =>
            !ItemUnitHashesSeen[processId].Contains(item.HashString) &&
            !ItemUnitIdsSeen[processId].Contains(item.UnitId) &&
            !ItemUnitIdsToSkip[processId].Contains(item.UnitId);

        private static bool CheckVendorItem(UnitItem item, int processId) =>
            item.IsInStore &&
            !ItemUnitIdsSeen[processId].Contains(item.UnitId) &&
            !ItemUnitIdsToSkip[processId].Contains(item.UnitId);

        public static string ItemLogDisplayName(UnitItem item, ItemFilter rule)
        {
            var statsProcessed = new List<Stats.Stat>();
            var itemBaseName = GetItemName(item);
            var itemSpecialName = "";
            var itemPrefix = "";
            var itemSuffix = "";

            if (item.IsInStore && item.VendorOwner != Npc.Invalid)
            {
                var vendorLabel = item.VendorOwner != Npc.Unknown ? NpcExtensions.Name(item.VendorOwner) : "Vendor";
                itemPrefix += $"[{vendorLabel}] ";
            }
            else if (item.IsIdentified)
            {
                itemPrefix += "[Identified] ";
            }

            if (rule == null) return itemPrefix + itemBaseName;

            if ((item.ItemData.ItemFlags & ItemFlags.IFLAG_ETHEREAL) == ItemFlags.IFLAG_ETHEREAL)
            {
                itemPrefix += "[Eth] ";
            }

            if (item.Stats.TryGetValue(Stats.Stat.NumSockets, out var numSockets))
            {
                itemPrefix += "[" + numSockets + " S] ";
            }

            if (item.ItemData.ItemQuality == ItemQuality.SUPERIOR)
            {
                itemPrefix += "Sup. ";
            }

            if (rule.AllResist != null)
            {
                var itemAllRes = GetItemStatResists(item, false);
                if (itemAllRes > 0)
                {
                    itemSuffix += $" ({itemAllRes} all res)";
                }
            }

            if (rule.SumResist != null)
            {
                var itemSumRes = GetItemStatResists(item, true);
                if (itemSumRes > 0)
                {
                    itemSuffix += $" ({itemSumRes} sum res)";
                }
            }

            if (rule.AllSkills != null)
            {
                var itemAllSkills = GetItemStat(item, Stats.Stat.AllSkills);
                if (itemAllSkills > 0)
                {
                    itemSuffix += $" (+{itemAllSkills} all skills)";
                }
                statsProcessed.Add(Stats.Stat.AllSkills);
            }

            if (rule.ClassSkills != null)
            {
                foreach (var subrule in rule.ClassSkills)
                {
                    var (className, classSkills) = GetItemStatAddClassSkills(item, subrule.Key);
                    if (classSkills > 0)
                    {
                        itemSuffix += $" (+{classSkills} {className} skills)";
                    }
                }
            }

            if (rule.SkillTrees != null)
            {
                foreach (var subrule in rule.SkillTrees)
                {
                    var (skillTreeName, skillTrees) = GetItemStatAddSkillTreeSkills(item, subrule.Key);
                    if (skillTrees > 0)
                    {
                        itemSuffix += $" (+{skillTrees} {skillTreeName.Name()} skills)";
                    }
                }
            }

            if (rule.Skills != null)
            {
                foreach (var subrule in rule.Skills)
                {
                    var (skill, singleSkills) = GetItemStatAddSingleSkills(item, subrule.Key);
                    if (singleSkills > 0)
                    {
                        itemSuffix += $" (+{singleSkills} {skill.Name()})";
                    }
                }
            }

            if (rule.SkillCharges != null)
            {
                foreach (var subrule in rule.SkillCharges)
                {
                    var (skillLevel, currentCharges, maxCharges) = GetItemStatAddSkillCharges(item, subrule.Key);
                    if (skillLevel > 0)
                    {
                        var charges = "";
                        if (currentCharges > 0 && maxCharges > 0)
                        {
                            charges = $"{currentCharges}/{maxCharges}";
                        }
                        itemSuffix += $" (+{skillLevel} {subrule.Key.Name()} {charges} charges)";
                    }
                }
            }

            foreach (var (stat, shift) in Stats.StatShifts.Select(x => (x.Key, x.Value)))
            {
                var property = rule.GetType().GetProperty(stat.ToString());
                var propertyValue = property.GetValue(rule, null);
                if (propertyValue == null) continue;

                var yamlAttribute = property.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(YamlMemberAttribute));
                var propName = property.Name;

                if (yamlAttribute != null) propName = yamlAttribute.NamedArguments.FirstOrDefault(x => x.MemberName == "Alias").TypedValue.Value.ToString();

                var statValue = GetItemStatShifted(item, stat);
                if (statValue > 0)
                {
                    itemSuffix += $" ({statValue} {propName})";
                }
                statsProcessed.Add(stat);
            }

            foreach (var property in rule.GetType().GetProperties())
            {
                var yamlAttribute = property.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(YamlMemberAttribute));
                var propName = property.Name;

                if (yamlAttribute != null) propName = yamlAttribute.NamedArguments.FirstOrDefault(x => x.MemberName == "Alias").TypedValue.Value.ToString();

                if (property.PropertyType == typeof(int?) && Enum.TryParse<Stats.Stat>(property.Name, out var stat))
                {
                    if (statsProcessed.Contains(stat)) continue;

                    var propertyValue = rule.GetType().GetProperty(property.Name).GetValue(rule, null);
                    if (propertyValue == null) continue;

                    var statValue = GetItemStat(item, stat);
                    if (statValue > 0)
                    {
                        itemSuffix += $" ({statValue} {propName})";
                    }
                }
            }

            switch (item.ItemData.ItemQuality)
            {
                case ItemQuality.UNIQUE:
                    itemSpecialName = GetUniqueName(item) + " ";
                    break;

                case ItemQuality.SET:
                    itemSpecialName = GetSetName(item) + " ";
                    break;
            }

            return itemPrefix + itemSpecialName + itemBaseName + itemSuffix;
        }

        public static string GetItemNameFromKey(string key)
        {
            LocalizedObj localItem;
            if (!LocalizedItems.TryGetValue(key, out localItem))
            {
                return "ItemNotFound";
            }

            var lang = MapAssistConfiguration.Loaded.LanguageCode;
            var prop = localItem.GetType().GetProperty(lang.ToString()).GetValue(localItem, null);

            return prop.ToString();
        }

        public static string GetItemBaseName(UnitItem item)
        {
            string itemCode;
            if (!_ItemCodes.TryGetValue(item.TxtFileNo, out itemCode))
            {
                return "ItemNotFound";
            }

            LocalizedObj localItem;
            if (!LocalizedItems.TryGetValue(itemCode, out localItem))
            {
                return "ItemNotFound";
            }

            var lang = MapAssistConfiguration.Loaded.LanguageCode;
            var prop = localItem.GetType().GetProperty(lang.ToString()).GetValue(localItem, null);

            return prop.ToString();
        }

        public static string GetItemName(UnitItem item)
        {
            string itemCode;
            if (!_ItemCodes.TryGetValue(item.TxtFileNo, out itemCode))
            {
                return "ItemNotFound";
            }

            LocalizedObj localItem;
            if (!LocalizedItems.TryGetValue(itemCode, out localItem))
            {
                return "ItemNotFound";
            }

            return localItem.enUS;
        }

        public static string GetUniqueName(UnitItem item)
        {
            string itemCode;
            if (!_ItemCodes.TryGetValue(item.TxtFileNo, out itemCode))
            {
                return "Unique";
            }

            if (!_UniqueFromCode.TryGetValue(itemCode, out itemCode))
            {
                return "Unique";
            }

            LocalizedObj localItem;
            if (!LocalizedItems.TryGetValue(itemCode, out localItem))
            {
                return "Unique";
            }

            var lang = MapAssistConfiguration.Loaded.LanguageCode;
            var prop = localItem.GetType().GetProperty(lang.ToString()).GetValue(localItem, null);

            return prop.ToString();
        }

        public static string GetSetName(UnitItem item)
        {
            string itemCode;
            if (!_ItemCodes.TryGetValue(item.TxtFileNo, out itemCode))
            {
                return "Set";
            }

            if (!_SetFromCode.TryGetValue(itemCode, out itemCode))
            {
                return "Set";
            }

            LocalizedObj localItem;
            if (!LocalizedItems.TryGetValue(itemCode, out localItem))
            {
                return "Set";
            }

            var lang = MapAssistConfiguration.Loaded.LanguageCode;
            var prop = localItem.GetType().GetProperty(lang.ToString()).GetValue(localItem, null);

            return prop.ToString();
        }

        public static Color GetItemBaseColor(UnitItem unit)
        {
            Color fontColor;
            if (unit == null || !ItemColors.TryGetValue(unit.ItemData.ItemQuality, out fontColor))
            {
                // Invalid item quality
                return Color.Empty;
            }

            var isEth = (unit.ItemData.ItemFlags & ItemFlags.IFLAG_ETHEREAL) == ItemFlags.IFLAG_ETHEREAL;
            if (isEth && fontColor == Color.White)
            {
                return ItemColors[ItemQuality.SUPERIOR];
            }

            if (unit.Stats.ContainsKey(Stats.Stat.NumSockets) && fontColor == Color.White)
            {
                return ItemColors[ItemQuality.SUPERIOR];
            }

            if (unit.TxtFileNo >= 610 && unit.TxtFileNo <= 642)
            {
                // Runes
                return ItemColors[ItemQuality.CRAFT];
            }

            switch (unit.TxtFileNo)
            {
                case 647: // Key of Terror
                case 648: // Key of Hate
                case 649: // Key of Destruction
                case 653: // Token of Absolution
                case 654: // Twisted Essence of Suffering
                case 655: // Charged Essense of Hatred
                case 656: // Burning Essence of Terror
                case 657: // Festering Essence of Destruction
                    return ItemColors[ItemQuality.CRAFT];
            }

            return fontColor;
        }

        public static ItemTier GetItemTier(UnitItem item)
        {
            return GetItemTier(item.Item);
        }

        public static ItemTier GetItemTier(Item item)
        {
            var itemClasses = ItemClasses.Where(x => x.Value.Contains(item));
            if (itemClasses.Count() == 0) return ItemTier.NotApplicable;

            var itemClass = itemClasses.First();
            if (itemClass.Key == Item.ClassCirclets) return ItemTier.NotApplicable;

            return (ItemTier)(Array.IndexOf(itemClass.Value, item) * 3 / itemClass.Value.Length); // All items with each class (except circlets) come in equal amounts within each tier
        }

        public static int GetItemStat(UnitItem item, Stats.Stat stat)
        {
            return item.Stats.TryGetValue(stat, out var statValue) ? statValue : 0;
        }

        public static int GetItemStatShifted(UnitItem item, Stats.Stat stat)
        {
            return item.Stats.TryGetValue(stat, out var statValue) && Stats.StatShifts.TryGetValue(stat, out var shift) ? statValue >> shift : 0;
        }

        public static double GetItemStatDecimal(UnitItem item, Stats.Stat stat)
        {
            return item.Stats.TryGetValue(stat, out var statValue) && Stats.StatDivisors.TryGetValue(stat, out var divisor) ? statValue / divisor : 0;
        }

        public static int GetItemStatResists(UnitItem item, bool sumOfEach)
        {
            item.Stats.TryGetValue(Stats.Stat.FireResist, out var fireRes);
            item.Stats.TryGetValue(Stats.Stat.LightningResist, out var lightRes);
            item.Stats.TryGetValue(Stats.Stat.ColdResist, out var coldRes);
            item.Stats.TryGetValue(Stats.Stat.PoisonResist, out var psnRes);
            var resistances = new[] { fireRes, lightRes, coldRes, psnRes };
            return sumOfEach ? resistances.Sum() : resistances.Min();
        }

        public static int GetItemStatAllAttributes(UnitItem item)
        {
            item.Stats.TryGetValue(Stats.Stat.Strength, out var strength);
            item.Stats.TryGetValue(Stats.Stat.Dexterity, out var dexterity);
            item.Stats.TryGetValue(Stats.Stat.Vitality, out var vitality);
            item.Stats.TryGetValue(Stats.Stat.Energy, out var energy);
            return new[] { strength, dexterity, vitality, energy }.Min();
        }

        public static (Structs.PlayerClass, int) GetItemStatAddClassSkills(UnitItem item, Structs.PlayerClass playerClass)
        {
            var allSkills = GetItemStat(item, Stats.Stat.AllSkills);

            if (playerClass == Structs.PlayerClass.Any)
            {
                var maxClassSkills = 0;
                var maxClass = playerClass;

                for (var classId = Structs.PlayerClass.Amazon; classId <= Structs.PlayerClass.Assassin; classId++)
                {
                    if (item.StatLayers.TryGetValue(Stats.Stat.AddClassSkills, out var anyItemStats) &&
                        anyItemStats.TryGetValue((ushort)classId, out var anyClassSkills))
                    {
                        if (anyClassSkills > maxClassSkills)
                        {
                            maxClassSkills = anyClassSkills;
                            maxClass = classId;
                        }
                    }
                }

                return (maxClass, allSkills + maxClassSkills);
            }

            if (item.StatLayers.TryGetValue(Stats.Stat.AddClassSkills, out var itemStats) &&
                itemStats.TryGetValue((ushort)playerClass, out var addClassSkills))
            {
                return (playerClass, allSkills + addClassSkills);
            }

            return (playerClass, allSkills);
        }

        public static (SkillTree, int) GetItemStatAddSkillTreeSkills(UnitItem item, SkillTree skillTree)
        {
            if (skillTree == SkillTree.Any)
            {
                var maxSkillTreeQuantity = 0;
                var maxSkillTree = skillTree;

                foreach (var skillTreeId in Enum.GetValues(typeof(SkillTree)).Cast<SkillTree>().Where(x => x != SkillTree.Any).ToList())
                {
                    if (item.StatLayers.TryGetValue(Stats.Stat.AddSkillTab, out var anyItemStats) &&
                        anyItemStats.TryGetValue((ushort)skillTreeId, out var anyTabSkills))
                    {
                        anyTabSkills += GetItemStatAddClassSkills(item, skillTreeId.GetPlayerClass()).Item2; // This adds the +class skill points and +all skills points

                        if (anyTabSkills > maxSkillTreeQuantity)
                        {
                            maxSkillTree = skillTreeId;
                            maxSkillTreeQuantity = anyTabSkills;
                        }
                    }
                }

                return (maxSkillTree, maxSkillTreeQuantity);
            }

            var baseAddSkills = GetItemStatAddClassSkills(item, skillTree.GetPlayerClass()).Item2; // This adds the +class skill points and +all skills points

            if (item.StatLayers.TryGetValue(Stats.Stat.AddSkillTab, out var itemStats) &&
                itemStats.TryGetValue((ushort)skillTree, out var addSkillTab))
            {
                return (skillTree, baseAddSkills + addSkillTab);
            }

            return (skillTree, baseAddSkills);
        }

        public static (Skill, int) GetItemStatAddSingleSkills(UnitItem item, Skill skill)
        {
            var itemSkillsStats = new List<Stats.Stat>()
            {
                Stats.Stat.SingleSkill,
                Stats.Stat.NonClassSkill,
            };

            if (skill == Skill.Any)
            {
                var maxSkillQuantity = 0;
                var maxSkill = skill;

                foreach (var statType in itemSkillsStats)
                {
                    foreach (var skillId in SkillExtensions.SkillTreeToSkillDict.SelectMany(x => x.Value).ToList())
                    {
                        if (item.StatLayers.TryGetValue(statType, out var anyItemStats) &&
                            anyItemStats.TryGetValue((ushort)skillId, out var anySkillLevel))
                        {
                            anySkillLevel += (statType == Stats.Stat.SingleSkill ? GetItemStatAddSkillTreeSkills(item, skillId.GetSkillTree()).Item2 : 0); // This adds the +skill tree points, +class skill points and +all skills points

                            if (anySkillLevel > maxSkillQuantity)
                            {
                                maxSkill = skillId;
                                maxSkillQuantity = anySkillLevel;
                            }
                        }
                    }
                }

                return (maxSkill, maxSkillQuantity);
            }

            var baseAddSkills = GetItemStatAddSkillTreeSkills(item, skill.GetSkillTree()).Item2; // This adds the +skill tree points, +class skill points and +all skills points

            foreach (var statType in itemSkillsStats)
            {
                if (item.StatLayers.TryGetValue(statType, out var itemStats) &&
                    itemStats.TryGetValue((ushort)skill, out var skillLevel))
                {
                    return (skill, (statType == Stats.Stat.SingleSkill ? baseAddSkills : 0) + skillLevel);
                }
            }

            return (skill, baseAddSkills);
        }

        public static (int, int, int) GetItemStatAddSkillCharges(UnitItem item, Skill skill)
        {
            if (item.StatLayers.TryGetValue(Stats.Stat.ItemChargedSkill, out var itemStats))
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

        public static readonly Dictionary<ItemQuality, Color> ItemColors = new Dictionary<ItemQuality, Color>()
        {
            {ItemQuality.INFERIOR, Color.White},
            {ItemQuality.NORMAL, Color.White},
            {ItemQuality.SUPERIOR, Color.Gray},
            {ItemQuality.MAGIC, ColorTranslator.FromHtml("#4169E1")},
            {ItemQuality.SET, ColorTranslator.FromHtml("#00FF00")},
            {ItemQuality.RARE, ColorTranslator.FromHtml("#FFFF00")},
            {ItemQuality.UNIQUE, ColorTranslator.FromHtml("#A59263")},
            {ItemQuality.CRAFT, ColorTranslator.FromHtml("#FFAE00")},
        };

        public static readonly Dictionary<string, string> _SetFromCode = new Dictionary<string, string>()
        {
            {"lrg", "Civerb's Ward"},
            {"amu", "Set"}, //Amulet
            {"gsc", "Civerb's Cudgel"},
            {"mbt", "Hsarus' Iron Heel"},
            {"buc", "Hsarus' Iron Fist"},
            {"mbl", "Set"}, //Belt
            {"lsd", "Cleglaw's Tooth"},
            {"sml", "Cleglaw's Claw"},
            {"mgl", "Cleglaw's Pincers"},
            {"tgl", "Set"}, //Light Gauntlets
            {"crn", "Set"}, //Crown
            {"tbl", "Set"}, //Heavy Belt
            {"bsd", "Isenhart's Lightbrand"},
            {"gts", "Isenhart's Parry"},
            {"brs", "Isenhart's Case"},
            {"fhl", "Isenhart's Horns"},
            {"lbb", "Vidala's Barb"},
            {"tbt", "Vidala's Fetlock"},
            {"lea", "Vidala's Ambush"},
            {"kit", "Milabrega's Orb"},
            {"wsp", "Milabrega's Rod"},
            {"aar", "Milabrega's Robe"},
            {"bst", "Cathan's Rule"},
            {"chn", "Cathan's Mesh"},
            {"msk", "Cathan's Visage"},
            {"rin", "Set"}, //Ring
            {"mpi", "Tancred's Crowbill"},
            {"ful", "Tancred's Spine"},
            {"lbt", "Tancred's Hobnails"},
            {"bhm", "Tancred's Skull"},
            {"hgl", "Sigon's Gage"},
            {"ghm", "Sigon's Visor"},
            {"gth", "Sigon's Shelter"},
            {"hbt", "Sigon's Sabot"},
            {"hbl", "Sigon's Wrap"},
            {"tow", "Sigon's Guard"},
            {"cap", "Set"}, //Cap
            {"gwn", "Infernal Torch"},
            {"hlm", "Berserker's Headgear"},
            {"spl", "Berserker's Hauberk"},
            {"2ax", "Berserker's Hatchet"},
            {"lgl", "Death's Hand"},
            {"lbl", "Death's Guard"},
            {"wsd", "Death's Touch"},
            {"sbr", "Angelic Sickle"},
            {"rng", "Angelic Mantle"},
            {"swb", "Arctic Horn"},
            {"qui", "Arctic Furs"},
            {"vbl", "Arctic Binding"},
            {"wst", "Arcanna's Deathwand"},
            {"skp", "Arcanna's Head"},
            {"ltp", "Arcanna's Flesh"},
            {"xh9", "Natalya's Totem"},
            {"7qr", "Natalya's Mark"},
            {"ucl", "Natalya's Shadow"},
            {"xmb", "Natalya's Soul"},
            {"dr8", "Aldur's Stony Gaze"},
            {"uul", "Aldur's Deception"},
            {"9mt", "Aldur's Gauntlet"},
            {"xtb", "Aldur's Advance"},
            {"ba5", "Immortal King's Will"},
            {"uar", "Immortal King's Soul Cage"},
            {"zhb", "Immortal King's Detail"},
            {"xhg", "Immortal King's Forge"},
            {"xhb", "Immortal King's Pillar"},
            {"7m7", "Immortal King's Stone Crusher"},
            {"zmb", "Tal Rasha's Fire-Spun Cloth"},
            {"oba", "Tal Rasha's Lidless Eye"},
            {"uth", "Tal Rasha's Howling Wind"},
            {"xsk", "Tal Rasha's Horadric Crest"},
            {"urn", "Griswold's Valor"},
            {"xar", "Griswold's Heart"},
            {"7ws", "Griswolds's Redemption"},
            {"paf", "Griswold's Honor"},
            {"uh9", "Trang-Oul's Guise"},
            {"xul", "Trang-Oul's Scales"},
            {"ne9", "Trang-Oul's Wing"},
            {"xmg", "Trang-Oul's Claws"},
            {"utc", "Trang-Oul's Girth"},
            {"ci3", "M'avina's True Sight"},
            {"uld", "M'avina's Embrace"},
            {"xtg", "M'avina's Icy Clutch"},
            {"zvb", "M'avina's Tenet"},
            {"amc", "M'avina's Caster"},
            {"ulg", "Laying of Hands"},
            {"xlb", "Rite of Passage"},
            {"uui", "Spiritual Custodian"},
            {"umc", "Credendum"},
            {"7ma", "Dangoon's Teaching"},
            {"uts", "Heaven's Taebaek"},
            {"xrs", "Haemosu's Adament"},
            {"uhm", "Ondal's Almighty"},
            {"xhm", "Guillaume's Face"},
            {"ztb", "Wilhelm's Pride"},
            {"xvg", "Magnus' Skin"},
            {"xml", "Wihtstan's Guard"},
            {"xrn", "Hwanin's Splendor"},
            {"xcl", "Hwanin's Refuge"},
            {"9vo", "Hwanin's Justice"},
            {"7ls", "Sazabi's Cobalt Redeemer"},
            {"upl", "Sazabi's Ghost Liberator"},
            {"xhl", "Sazabi's Mental Sheath"},
            {"7gd", "Bul-Kathos' Sacred Charge"},
            {"7wd", "Bul-Kathos' Tribal Guardian"},
            {"xap", "Cow King's Horns"},
            {"stu", "Cow King's Hide"},
            {"vbt", "Set"}, //Heavy Boots
            {"6cs", "Naj's Puzzler"},
            {"ult", "Naj's Light Plate"},
            {"ci0", "Naj's Circlet"},
            {"vgl", "McAuley's Taboo"},
            {"bwn", "McAuley's Superstition"}
        };

        public static readonly Dictionary<string, string> _UniqueFromCode = new Dictionary<string, string>()
        {
            {"hax", "The Gnasher"},
            {"axe", "Deathspade"},
            {"2ax", "Bladebone"},
            {"mpi", "Mindrend"},
            {"wax", "Rakescar"},
            {"lax", "Fechmars Axe"},
            {"bax", "Goreshovel"},
            {"btx", "The Chieftan"},
            {"gax", "Brainhew"},
            {"gix", "The Humongous"},
            {"wnd", "Iros Torch"},
            {"ywn", "Maelstromwrath"},
            {"bwn", "Gravenspine"},
            {"gwn", "Umes Lament"},
            {"clb", "Felloak"},
            {"scp", "Knell Striker"},
            {"gsc", "Rusthandle"},
            {"wsp", "Stormeye"},
            {"spc", "Stoutnail"},
            {"mac", "Crushflange"},
            {"mst", "Bloodrise"},
            {"fla", "The Generals Tan Do Li Ga"},
            {"whm", "Ironstone"},
            {"mau", "Bonesob"},
            {"gma", "Steeldriver"},
            {"ssd", "Rixots Keen"},
            {"scm", "Blood Crescent"},
            {"sbr", "Krintizs Skewer"},
            {"flc", "Gleamscythe"},
            {"crs", "Azurewrath"},
            {"bsd", "Griswolds Edge"},
            {"lsd", "Hellplague"},
            {"wsd", "Culwens Point"},
            {"2hs", "Shadowfang"},
            {"clm", "Soulflay"},
            {"gis", "Kinemils Awl"},
            {"bsw", "Blacktongue"},
            {"flb", "Ripsaw"},
            {"gsd", "The Patriarch"},
            {"dgr", "Gull"},
            {"dir", "The Diggler"},
            {"kri", "The Jade Tan Do"},
            {"bld", "Irices Shard"},
            {"spr", "The Dragon Chang"},
            {"tri", "Razortine"},
            {"brn", "Bloodthief"},
            {"spt", "Lance of Yaggai"},
            {"pik", "The Tannr Gorerod"},
            {"bar", "Dimoaks Hew"},
            {"vou", "Steelgoad"},
            {"scy", "Soul Harvest"},
            {"pax", "The Battlebranch"},
            {"hal", "Woestave"},
            {"wsc", "The Grim Reaper"},
            {"sst", "Bane Ash"},
            {"lst", "Serpent Lord"},
            {"cst", "Lazarus Spire"},
            {"bst", "The Salamander"},
            {"wst", "The Iron Jang Bong"},
            {"sbw", "Pluckeye"},
            {"hbw", "Witherstring"},
            {"lbw", "Rimeraven"},
            {"cbw", "Piercerib"},
            {"sbb", "Pullspite"},
            {"lbb", "Wizendraw"},
            {"swb", "Hellclap"},
            {"lwb", "Blastbark"},
            {"lxb", "Leadcrow"},
            {"mxb", "Ichorsting"},
            {"hxb", "Hellcast"},
            {"rxb", "Doomspittle"},
            {"cap", "Biggin's Bonnet"},
            {"skp", "Tarnhelm"},
            {"hlm", "Coif of Glory"},
            {"fhl", "Duskdeep"},
            {"bhm", "Wormskull"},
            {"ghm", "Howltusk"},
            {"crn", "Undead Crown"},
            {"msk", "The Face of Horror"},
            {"qui", "Greyform"},
            {"lea", "Blinkbats Form"},
            {"hla", "The Centurion"},
            {"stu", "Twitchthroe"},
            {"rng", "Darkglow"},
            {"scl", "Hawkmail"},
            {"chn", "Sparking Mail"},
            {"brs", "Venomsward"},
            {"spl", "Iceblink"},
            {"plt", "Boneflesh"},
            {"fld", "Rockfleece"},
            {"gth", "Rattlecage"},
            {"ful", "Goldskin"},
            {"aar", "Victors Silk"},
            {"ltp", "Heavenly Garb"},
            {"buc", "Pelta Lunata"},
            {"sml", "Umbral Disk"},
            {"lrg", "Stormguild"},
            {"bsh", "Wall of the Eyeless"},
            {"spk", "Swordback Hold"},
            {"kit", "Steelclash"},
            {"tow", "Bverrit Keep"},
            {"gts", "The Ward"},
            {"lgl", "The Hand of Broc"},
            {"vgl", "Bloodfist"},
            {"mgl", "Chance Guards"},
            {"tgl", "Magefist"},
            {"hgl", "Frostburn"},
            {"lbt", "Hotspur"},
            {"vbt", "Gorefoot"},
            {"mbt", "Treads of Cthon"},
            {"tbt", "Goblin Toe"},
            {"hbt", "Tearhaunch"},
            {"lbl", "Lenyms Cord"},
            {"vbl", "Snakecord"},
            {"mbl", "Nightsmoke"},
            {"tbl", "Goldwrap"},
            {"hbl", "Bladebuckle"},
            {"amu", "Unique"}, //Amulet
            {"rin", "Unique"}, //Ring
            {"vip", "Amulet of the Viper"},
            {"msf", "Staff of Kings"},
            {"hst", "Horadric Staff"},
            {"hfh", "Hell Forge Hammer"},
            {"qf1", "KhalimFlail"},
            {"qf2", "SuperKhalimFlail"},
            {"9ha", "Coldkill"},
            {"9ax", "Butcher's Pupil"},
            {"92a", "Islestrike"},
            {"9mp", "Pompe's Wrath"},
            {"9wa", "Guardian Naga"},
            {"9la", "Warlord's Trust"},
            {"9ba", "Spellsteel"},
            {"9bt", "Stormrider"},
            {"9ga", "Boneslayer Blade"},
            {"9gi", "The Minataur"},
            {"9wn", "Suicide Branch"},
            {"9yw", "Carin Shard"},
            {"9bw", "Arm of King Leoric"},
            {"9gw", "Blackhand Key"},
            {"9cl", "Dark Clan Crusher"},
            {"9sc", "Zakarum's Hand"},
            {"9qs", "The Fetid Sprinkler"},
            {"9ws", "Hand of Blessed Light"},
            {"9sp", "Fleshrender"},
            {"9ma", "Sureshrill Frost"},
            {"9mt", "Moonfall"},
            {"9fl", "Baezil's Vortex"},
            {"9wh", "Earthshaker"},
            {"9m9", "Bloodtree Stump"},
            {"9gm", "The Gavel of Pain"},
            {"9ss", "Bloodletter"},
            {"9sm", "Coldsteel Eye"},
            {"9sb", "Hexfire"},
            {"9fc", "Blade of Ali Baba"},
            {"9cr", "Ginther's Rift"},
            {"9bs", "Headstriker"},
            {"9ls", "Plague Bearer"},
            {"9wd", "The Atlantian"},
            {"92h", "Crainte Vomir"},
            {"9cm", "Bing Sz Wang"},
            {"9gs", "The Vile Husk"},
            {"9b9", "Cloudcrack"},
            {"9fb", "Todesfaelle Flamme"},
            {"9gd", "Swordguard"},
            {"9dg", "Spineripper"},
            {"9di", "Heart Carver"},
            {"9kr", "Blackbog's Sharp"},
            {"9bl", "Stormspike"},
            {"9sr", "The Impaler"},
            {"9tr", "Kelpie Snare"},
            {"9br", "Soulfeast Tine"},
            {"9st", "Hone Sundan"},
            {"9p9", "Spire of Honor"},
            {"9b7", "The Meat Scraper"},
            {"9vo", "Blackleach Blade"},
            {"9s8", "Athena's Wrath"},
            {"9pa", "Pierre Tombale Couant"},
            {"9h9", "Husoldal Evo"},
            {"9wc", "Grim's Burning Dead"},
            {"8ss", "Razorswitch"},
            {"8ls", "Ribcracker"},
            {"8cs", "Chromatic Ire"},
            {"8bs", "Warpspear"},
            {"8ws", "Skullcollector"},
            {"8sb", "Skystrike"},
            {"8hb", "Riphook"},
            {"8lb", "Kuko Shakaku"},
            {"8cb", "Endlesshail"},
            {"8s8", "Whichwild String"},
            {"8l8", "Cliffkiller"},
            {"8sw", "Magewrath"},
            {"8lw", "Godstrike Arch"},
            {"8lx", "Langer Briser"},
            {"8mx", "Pus Spiter"},
            {"8hx", "Buriza-Do Kyanon"},
            {"8rx", "Demon Machine"},
            {"xap", "Peasent Crown"},
            {"xkp", "Rockstopper"},
            {"xlm", "Stealskull"},
            {"xhl", "Darksight Helm"},
            {"xhm", "Valkiry Wing"},
            {"xrn", "Crown of Thieves"},
            {"xsk", "Blackhorn's Face"},
            {"xh9", "Vampiregaze"},
            {"xui", "The Spirit Shroud"},
            {"xea", "Skin of the Vipermagi"},
            {"xla", "Skin of the Flayerd One"},
            {"xtu", "Ironpelt"},
            {"xng", "Spiritforge"},
            {"xcl", "Crow Caw"},
            {"xhn", "Shaftstop"},
            {"xrs", "Duriel's Shell"},
            {"xpl", "Skullder's Ire"},
            {"xlt", "Guardian Angel"},
            {"xld", "Toothrow"},
            {"xth", "Atma's Wail"},
            {"xul", "Black Hades"},
            {"xar", "Corpsemourn"},
            {"xtp", "Que-Hegan's Wisdon"},
            {"xuc", "Visceratuant"},
            {"xml", "Mosers Blessed Circle"},
            {"xrg", "Stormchaser"},
            {"xit", "Tiamat's Rebuke"},
            {"xow", "Kerke's Sanctuary"},
            {"xts", "Radimant's Sphere"},
            {"xsh", "Lidless Wall"},
            {"xpk", "Lance Guard"},
            {"xlg", "Venom Grip"},
            {"xvg", "Gravepalm"},
            {"xmg", "Ghoulhide"},
            {"xtg", "Lavagout"},
            {"xhg", "Hellmouth"},
            {"xlb", "Infernostride"},
            {"xvb", "Waterwalk"},
            {"xmb", "Silkweave"},
            {"xtb", "Wartraveler"},
            {"xhb", "Gorerider"},
            {"zlb", "String of Ears"},
            {"zvb", "Razortail"},
            {"zmb", "Gloomstrap"},
            {"ztb", "Snowclash"},
            {"zhb", "Thudergod's Vigor"},
            {"uap", "Harlequin Crest"},
            {"uhm", "Unique"}, //Spired Helm
            {"utu", "The Gladiator's Bane"},
            {"upl", "Arkaine's Valor"},
            {"uml", "Blackoak Shield"},
            {"uit", "Stormshield"},
            {"7bt", "Hellslayer"},
            {"7ga", "Messerschmidt's Reaver"},
            {"7mt", "Baranar's Star"},
            {"7b7", "Doombringer"},
            {"7gd", "The Grandfather"},
            {"7dg", "Wizardspike"},
            {"7wc", "Stormspire"},
            {"6l7", "Eaglehorn"},
            {"6lw", "Windforce"},
            {"baa", "Arreat's Face"},
            {"nea", "Homunculus"},
            {"ama", "Titan's Revenge"},
            {"am7", "Lycander's Aim"},
            {"am9", "Lycander's Flank"},
            {"oba", "The Oculus"},
            {"pa9", "Herald of Zakarum"},
            {"9tw", "Cutthroat1"},
            {"dra", "Jalal's Mane"},
            {"9ta", "The Scalper"},
            {"7sb", "Bloodmoon"},
            {"7sm", "Djinnslayer"},
            {"9tk", "Deathbit"},
            {"7bk", "Warshrike"},
            {"6rx", "Gutsiphon"},
            {"7ha", "Razoredge"},
            {"7sp", "Demonlimb"},
            {"ulm", "Unique"}, //Armet
            {"7pa", "Tomb Reaver"},
            {"7gw", "Deaths's Web"},
            {"7cr", "Unique"}, //Phase Blade
            {"7kr", "Fleshripper"},
            {"7fl", "Unique"}, //Scourge
            {"7wh", "Unique"}, //Legendary Mallet
            {"7wb", "Jadetalon"},
            {"uhb", "Shadowdancer"},
            {"drb", "Cerebus"},
            {"uar", "Unique"}, //Sacred Armor
            {"umg", "Souldrain"},
            {"72a", "Runemaster"},
            {"7wa", "Deathcleaver"},
            {"7gi", "Executioner's Justice"},
            {"amd", "Stoneraven"},
            {"uld", "Leviathan"},
            {"7ts", "Gargoyle's Bite"},
            {"7b8", "Lacerator"},
            {"6ws", "Mang Song's Lesson"},
            {"7br", "Viperfork"},
            {"7ba", "Ethereal Edge"},
            {"bad", "Demonhorn's Edge"},
            {"7s8", "The Reaper's Toll"},
            {"drd", "Spiritkeeper"},
            {"6hx", "Hellrack"},
            {"pac", "Alma Negra"},
            {"nef", "Darkforge Spawn"},
            {"6sw", "Widowmaker"},
            {"amb", "Bloodraven's Charge"},
            {"7bl", "Ghostflame"},
            {"7cs", "Shadowkiller"},
            {"7ta", "Gimmershred"},
            {"ci3", "Griffon's Eye"},
            {"7m7", "Windhammer"},
            {"amf", "Thunderstroke"},
            {"7s7", "Demon's Arch"},
            {"nee", "Boneflame"},
            {"7p7", "Steelpillar"},
            {"urn", "Crown of Ages"},
            {"usk", "Andariel's Visage"},
            {"pae", "Dragonscale"},
            {"uul", "Steel Carapice"},
            {"uow", "Medusa's Gaze"},
            {"dre", "Ravenlore"},
            {"7bw", "Boneshade"},
            {"7gs", "Flamebellow"},
            {"obf", "Fathom"},
            {"bac", "Wolfhowl"},
            {"uts", "Spirit Ward"},
            {"ci2", "Kira's Guardian"},
            {"uui", "Ormus' Robes"},
            {"cm3", "Gheed's Fortune"},
            {"bae", "Halaberd's Reign"},
            {"upk", "Spike Thorn"},
            {"uvg", "Dracul's Grasp"},
            {"7ls", "Frostwind"},
            {"obc", "Eschuta's temper"},
            {"7lw", "Firelizard's Talons"},
            {"uvb", "Sandstorm Trek"},
            {"umb", "Marrowwalk"},
            {"ulc", "Arachnid Mesh"},
            {"uvc", "Nosferatu's Coil"},
            {"umc", "Verdugo's Hearty Cord"},
            {"uh9", "Giantskull"},
            {"7ws", "Ironward"},
            {"cm1", "Annihilus"},
            {"7sr", "Arioc's Needle"},
            {"7mp", "Cranebeak"},
            {"7cl", "Nord's Tenderizer"},
            {"7gm", "Unique"}, //Thunder Maul
            {"7gl", "Wraithflight"},
            {"7o7", "Bonehew"},
            {"6cs", "Ondal's Wisdom"},
            {"7sc", "Unique"}, //Mighty Scepter
            {"ush", "Headhunter's Glory"},
            {"uhg", "Steelrend"},
            {"jew", "Rainbow Facet"},
            {"cm2", "Hellfire Torch"}
        };

        public static readonly Dictionary<uint, string> _ItemCodes = new Dictionary<uint, string>()
        {
            {0, "hax"},
            {1, "axe"},
            {2, "2ax"},
            {3, "mpi"},
            {4, "wax"},
            {5, "lax"},
            {6, "bax"},
            {7, "btx"},
            {8, "gax"},
            {9, "gix"},
            {10, "wnd"},
            {11, "ywn"},
            {12, "bwn"},
            {13, "gwn"},
            {14, "clb"},
            {15, "scp"},
            {16, "gsc"},
            {17, "wsp"},
            {18, "spc"},
            {19, "mac"},
            {20, "mst"},
            {21, "fla"},
            {22, "whm"},
            {23, "mau"},
            {24, "gma"},
            {25, "ssd"},
            {26, "scm"},
            {27, "sbr"},
            {28, "flc"},
            {29, "crs"},
            {30, "bsd"},
            {31, "lsd"},
            {32, "wsd"},
            {33, "2hs"},
            {34, "clm"},
            {35, "gis"},
            {36, "bsw"},
            {37, "flb"},
            {38, "gsd"},
            {39, "dgr"},
            {40, "dir"},
            {41, "kri"},
            {42, "bld"},
            {43, "tkf"},
            {44, "tax"},
            {45, "bkf"},
            {46, "bal"},
            {47, "jav"},
            {48, "pil"},
            {49, "ssp"},
            {50, "glv"},
            {51, "tsp"},
            {52, "spr"},
            {53, "tri"},
            {54, "brn"},
            {55, "spt"},
            {56, "pik"},
            {57, "bar"},
            {58, "vou"},
            {59, "scy"},
            {60, "pax"},
            {61, "hal"},
            {62, "wsc"},
            {63, "sst"},
            {64, "lst"},
            {65, "cst"},
            {66, "bst"},
            {67, "wst"},
            {68, "sbw"},
            {69, "hbw"},
            {70, "lbw"},
            {71, "cbw"},
            {72, "sbb"},
            {73, "lbb"},
            {74, "swb"},
            {75, "lwb"},
            {76, "lxb"},
            {77, "mxb"},
            {78, "hxb"},
            {79, "rxb"},
            {80, "gps"},
            {81, "ops"},
            {82, "gpm"},
            {83, "opm"},
            {84, "gpl"},
            {85, "opl"},
            {86, "d33"},
            {87, "g33"},
            {88, "leg"},
            {89, "hdm"},
            {90, "hfh"},
            {91, "hst"},
            {92, "msf"},
            {93, "9ha"},
            {94, "9ax"},
            {95, "92a"},
            {96, "9mp"},
            {97, "9wa"},
            {98, "9la"},
            {99, "9ba"},
            {100, "9bt"},
            {101, "9ga"},
            {102, "9gi"},
            {103, "9wn"},
            {104, "9yw"},
            {105, "9bw"},
            {106, "9gw"},
            {107, "9cl"},
            {108, "9sc"},
            {109, "9qs"},
            {110, "9ws"},
            {111, "9sp"},
            {112, "9ma"},
            {113, "9mt"},
            {114, "9fl"},
            {115, "9wh"},
            {116, "9m9"},
            {117, "9gm"},
            {118, "9ss"},
            {119, "9sm"},
            {120, "9sb"},
            {121, "9fc"},
            {122, "9cr"},
            {123, "9bs"},
            {124, "9ls"},
            {125, "9wd"},
            {126, "92h"},
            {127, "9cm"},
            {128, "9gs"},
            {129, "9b9"},
            {130, "9fb"},
            {131, "9gd"},
            {132, "9dg"},
            {133, "9di"},
            {134, "9kr"},
            {135, "9bl"},
            {136, "9tk"},
            {137, "9ta"},
            {138, "9bk"},
            {139, "9b8"},
            {140, "9ja"},
            {141, "9pi"},
            {142, "9s9"},
            {143, "9gl"},
            {144, "9ts"},
            {145, "9sr"},
            {146, "9tr"},
            {147, "9br"},
            {148, "9st"},
            {149, "9p9"},
            {150, "9b7"},
            {151, "9vo"},
            {152, "9s8"},
            {153, "9pa"},
            {154, "9h9"},
            {155, "9wc"},
            {156, "8ss"},
            {157, "8ls"},
            {158, "8cs"},
            {159, "8bs"},
            {160, "8ws"},
            {161, "8sb"},
            {162, "8hb"},
            {163, "8lb"},
            {164, "8cb"},
            {165, "8s8"},
            {166, "8l8"},
            {167, "8sw"},
            {168, "8lw"},
            {169, "8lx"},
            {170, "8mx"},
            {171, "8hx"},
            {172, "8rx"},
            {173, "qf1"},
            {174, "qf2"},
            {175, "ktr"},
            {176, "wrb"},
            {177, "axf"},
            {178, "ces"},
            {179, "clw"},
            {180, "btl"},
            {181, "skr"},
            {182, "9ar"},
            {183, "9wb"},
            {184, "9xf"},
            {185, "9cs"},
            {186, "9lw"},
            {187, "9tw"},
            {188, "9qr"},
            {189, "7ar"},
            {190, "7wb"},
            {191, "7xf"},
            {192, "7cs"},
            {193, "7lw"},
            {194, "7tw"},
            {195, "7qr"},
            {196, "7ha"},
            {197, "7ax"},
            {198, "72a"},
            {199, "7mp"},
            {200, "7wa"},
            {201, "7la"},
            {202, "7ba"},
            {203, "7bt"},
            {204, "7ga"},
            {205, "7gi"},
            {206, "7wn"},
            {207, "7yw"},
            {208, "7bw"},
            {209, "7gw"},
            {210, "7cl"},
            {211, "7sc"},
            {212, "7qs"},
            {213, "7ws"},
            {214, "7sp"},
            {215, "7ma"},
            {216, "7mt"},
            {217, "7fl"},
            {218, "7wh"},
            {219, "7m7"},
            {220, "7gm"},
            {221, "7ss"},
            {222, "7sm"},
            {223, "7sb"},
            {224, "7fc"},
            {225, "7cr"},
            {226, "7bs"},
            {227, "7ls"},
            {228, "7wd"},
            {229, "72h"},
            {230, "7cm"},
            {231, "7gs"},
            {232, "7b7"},
            {233, "7fb"},
            {234, "7gd"},
            {235, "7dg"},
            {236, "7di"},
            {237, "7kr"},
            {238, "7bl"},
            {239, "7tk"},
            {240, "7ta"},
            {241, "7bk"},
            {242, "7b8"},
            {243, "7ja"},
            {244, "7pi"},
            {245, "7s7"},
            {246, "7gl"},
            {247, "7ts"},
            {248, "7sr"},
            {249, "7tr"},
            {250, "7br"},
            {251, "7st"},
            {252, "7p7"},
            {253, "7o7"},
            {254, "7vo"},
            {255, "7s8"},
            {256, "7pa"},
            {257, "7h7"},
            {258, "7wc"},
            {259, "6ss"},
            {260, "6ls"},
            {261, "6cs"},
            {262, "6bs"},
            {263, "6ws"},
            {264, "6sb"},
            {265, "6hb"},
            {266, "6lb"},
            {267, "6cb"},
            {268, "6s7"},
            {269, "6l7"},
            {270, "6sw"},
            {271, "6lw"},
            {272, "6lx"},
            {273, "6mx"},
            {274, "6hx"},
            {275, "6rx"},
            {276, "ob1"},
            {277, "ob2"},
            {278, "ob3"},
            {279, "ob4"},
            {280, "ob5"},
            {281, "am1"},
            {282, "am2"},
            {283, "am3"},
            {284, "am4"},
            {285, "am5"},
            {286, "ob6"},
            {287, "ob7"},
            {288, "ob8"},
            {289, "ob9"},
            {290, "oba"},
            {291, "am6"},
            {292, "am7"},
            {293, "am8"},
            {294, "am9"},
            {295, "ama"},
            {296, "obb"},
            {297, "obc"},
            {298, "obd"},
            {299, "obe"},
            {300, "obf"},
            {301, "amb"},
            {302, "amc"},
            {303, "amd"},
            {304, "ame"},
            {305, "amf"},
            {306, "cap"},
            {307, "skp"},
            {308, "hlm"},
            {309, "fhl"},
            {310, "ghm"},
            {311, "crn"},
            {312, "msk"},
            {313, "qui"},
            {314, "lea"},
            {315, "hla"},
            {316, "stu"},
            {317, "rng"},
            {318, "scl"},
            {319, "chn"},
            {320, "brs"},
            {321, "spl"},
            {322, "plt"},
            {323, "fld"},
            {324, "gth"},
            {325, "ful"},
            {326, "aar"},
            {327, "ltp"},
            {328, "buc"},
            {329, "sml"},
            {330, "lrg"},
            {331, "kit"},
            {332, "tow"},
            {333, "gts"},
            {334, "lgl"},
            {335, "vgl"},
            {336, "mgl"},
            {337, "tgl"},
            {338, "hgl"},
            {339, "lbt"},
            {340, "vbt"},
            {341, "mbt"},
            {342, "tbt"},
            {343, "hbt"},
            {344, "lbl"},
            {345, "vbl"},
            {346, "mbl"},
            {347, "tbl"},
            {348, "hbl"},
            {349, "bhm"},
            {350, "bsh"},
            {351, "spk"},
            {352, "xap"},
            {353, "xkp"},
            {354, "xlm"},
            {355, "xhl"},
            {356, "xhm"},
            {357, "xrn"},
            {358, "xsk"},
            {359, "xui"},
            {360, "xea"},
            {361, "xla"},
            {362, "xtu"},
            {363, "xng"},
            {364, "xcl"},
            {365, "xhn"},
            {366, "xrs"},
            {367, "xpl"},
            {368, "xlt"},
            {369, "xld"},
            {370, "xth"},
            {371, "xul"},
            {372, "xar"},
            {373, "xtp"},
            {374, "xuc"},
            {375, "xml"},
            {376, "xrg"},
            {377, "xit"},
            {378, "xow"},
            {379, "xts"},
            {380, "xlg"},
            {381, "xvg"},
            {382, "xmg"},
            {383, "xtg"},
            {384, "xhg"},
            {385, "xlb"},
            {386, "xvb"},
            {387, "xmb"},
            {388, "xtb"},
            {389, "xhb"},
            {390, "zlb"},
            {391, "zvb"},
            {392, "zmb"},
            {393, "ztb"},
            {394, "zhb"},
            {395, "xh9"},
            {396, "xsh"},
            {397, "xpk"},
            {398, "dr1"},
            {399, "dr2"},
            {400, "dr3"},
            {401, "dr4"},
            {402, "dr5"},
            {403, "ba1"},
            {404, "ba2"},
            {405, "ba3"},
            {406, "ba4"},
            {407, "ba5"},
            {408, "pa1"},
            {409, "pa2"},
            {410, "pa3"},
            {411, "pa4"},
            {412, "pa5"},
            {413, "ne1"},
            {414, "ne2"},
            {415, "ne3"},
            {416, "ne4"},
            {417, "ne5"},
            {418, "ci0"},
            {419, "ci1"},
            {420, "ci2"},
            {421, "ci3"},
            {422, "uap"},
            {423, "ukp"},
            {424, "ulm"},
            {425, "uhl"},
            {426, "uhm"},
            {427, "urn"},
            {428, "usk"},
            {429, "uui"},
            {430, "uea"},
            {431, "ula"},
            {432, "utu"},
            {433, "ung"},
            {434, "ucl"},
            {435, "uhn"},
            {436, "urs"},
            {437, "upl"},
            {438, "ult"},
            {439, "uld"},
            {440, "uth"},
            {441, "uul"},
            {442, "uar"},
            {443, "utp"},
            {444, "uuc"},
            {445, "uml"},
            {446, "urg"},
            {447, "uit"},
            {448, "uow"},
            {449, "uts"},
            {450, "ulg"},
            {451, "uvg"},
            {452, "umg"},
            {453, "utg"},
            {454, "uhg"},
            {455, "ulb"},
            {456, "uvb"},
            {457, "umb"},
            {458, "utb"},
            {459, "uhb"},
            {460, "ulc"},
            {461, "uvc"},
            {462, "umc"},
            {463, "utc"},
            {464, "uhc"},
            {465, "uh9"},
            {466, "ush"},
            {467, "upk"},
            {468, "dr6"},
            {469, "dr7"},
            {470, "dr8"},
            {471, "dr9"},
            {472, "dra"},
            {473, "ba6"},
            {474, "ba7"},
            {475, "ba8"},
            {476, "ba9"},
            {477, "baa"},
            {478, "pa6"},
            {479, "pa7"},
            {480, "pa8"},
            {481, "pa9"},
            {482, "paa"},
            {483, "ne6"},
            {484, "ne7"},
            {485, "ne8"},
            {486, "ne9"},
            {487, "nea"},
            {488, "drb"},
            {489, "drc"},
            {490, "drd"},
            {491, "dre"},
            {492, "drf"},
            {493, "bab"},
            {494, "bac"},
            {495, "bad"},
            {496, "bae"},
            {497, "baf"},
            {498, "pab"},
            {499, "pac"},
            {500, "pad"},
            {501, "pae"},
            {502, "paf"},
            {503, "neb"},
            {504, "neg"},
            {505, "ned"},
            {506, "nee"},
            {507, "nef"},
            {508, "elx"},
            {509, "hpo"},
            {510, "mpo"},
            {511, "hpf"},
            {512, "mpf"},
            {513, "vps"},
            {514, "yps"},
            {515, "rvs"},
            {516, "rvl"},
            {517, "wms"},
            {518, "tbk"},
            {519, "ibk"},
            {520, "amu"},
            {521, "vip"},
            {522, "rin"},
            {523, "gld"},
            {524, "bks"},
            {525, "bkd"},
            {526, "aqv"},
            {527, "tch"},
            {528, "cqv"},
            {529, "tsc"},
            {530, "isc"},
            {531, "hrt"},
            {532, "brz"},
            {533, "jaw"},
            {534, "eyz"},
            {535, "hrn"},
            {536, "tal"},
            {537, "flg"},
            {538, "fng"},
            {539, "qll"},
            {540, "sol"},
            {541, "scz"},
            {542, "spe"},
            {543, "key"},
            {544, "luv"},
            {545, "xyz"},
            {546, "j34"},
            {547, "g34"},
            {548, "bbb"},
            {549, "box"},
            {550, "tr1"},
            {551, "mss"},
            {552, "ass"},
            {553, "qey"},
            {554, "qhr"},
            {555, "qbr"},
            {556, "ear"},
            {557, "gcv"},
            {558, "gfv"},
            {559, "gsv"},
            {560, "gzv"},
            {561, "gpv"},
            {562, "gcy"},
            {563, "gfy"},
            {564, "gsy"},
            {565, "gly"},
            {566, "gpy"},
            {567, "gcb"},
            {568, "gfb"},
            {569, "gsb"},
            {570, "glb"},
            {571, "gpb"},
            {572, "gcg"},
            {573, "gfg"},
            {574, "gsg"},
            {575, "glg"},
            {576, "gpg"},
            {577, "gcr"},
            {578, "gfr"},
            {579, "gsr"},
            {580, "glr"},
            {581, "gpr"},
            {582, "gcw"},
            {583, "gfw"},
            {584, "gsw"},
            {585, "glw"},
            {586, "gpw"},
            {587, "hp1"},
            {588, "hp2"},
            {589, "hp3"},
            {590, "hp4"},
            {591, "hp5"},
            {592, "mp1"},
            {593, "mp2"},
            {594, "mp3"},
            {595, "mp4"},
            {596, "mp5"},
            {597, "skc"},
            {598, "skf"},
            {599, "sku"},
            {600, "skl"},
            {601, "skz"},
            {602, "hrb"},
            {603, "cm1"},
            {604, "cm2"},
            {605, "cm3"},
            {606, "rps"},
            {607, "rpl"},
            {608, "bps"},
            {609, "bpl"},
            {610, "r01"},
            {611, "r02"},
            {612, "r03"},
            {613, "r04"},
            {614, "r05"},
            {615, "r06"},
            {616, "r07"},
            {617, "r08"},
            {618, "r09"},
            {619, "r10"},
            {620, "r11"},
            {621, "r12"},
            {622, "r13"},
            {623, "r14"},
            {624, "r15"},
            {625, "r16"},
            {626, "r17"},
            {627, "r18"},
            {628, "r19"},
            {629, "r20"},
            {630, "r21"},
            {631, "r22"},
            {632, "r23"},
            {633, "r24"},
            {634, "r25"},
            {635, "r26"},
            {636, "r27"},
            {637, "r28"},
            {638, "r29"},
            {639, "r30"},
            {640, "r31"},
            {641, "r32"},
            {642, "r33"},
            {643, "jew"},
            {644, "ice"},
            {645, "0sc"},
            {646, "tr2"},
            {647, "pk1"},
            {648, "pk2"},
            {649, "pk3"},
            {650, "dhn"},
            {651, "bey"},
            {652, "mbr"},
            {653, "toa"},
            {654, "tes"},
            {655, "ceh"},
            {656, "bet"},
            {657, "fed"},
            {658, "std"}
        };

        public static Dictionary<Item, Item[]> ItemClasses = new Dictionary<Item, Item[]>()
        {
            [Item.ClassAxes] = new Item[] { Item.HandAxe, Item.Axe, Item.DoubleAxe, Item.MilitaryPick, Item.WarAxe, Item.LargeAxe, Item.BroadAxe, Item.BattleAxe, Item.GreatAxe, Item.GiantAxe, Item.Hatchet, Item.Cleaver, Item.TwinAxe, Item.Crowbill, Item.Naga, Item.MilitaryAxe, Item.BeardedAxe, Item.Tabar, Item.GothicAxe, Item.AncientAxe, Item.Tomahawk, Item.SmallCrescent, Item.EttinAxe, Item.WarSpike, Item.BerserkerAxe, Item.FeralAxe, Item.SilverEdgedAxe, Item.Decapitator, Item.ChampionAxe, Item.GloriousAxe },
            [Item.ClassWands] = new Item[] { Item.Wand, Item.YewWand, Item.BoneWand, Item.GrimWand, Item.BurntWand, Item.PetrifiedWand, Item.TombWand, Item.GraveWand, Item.PolishedWand, Item.GhostWand, Item.LichWand, Item.UnearthedWand },
            [Item.ClassClubs] = new Item[] { Item.Club, Item.SpikedClub, Item.Cudgel, Item.BarbedClub, Item.Truncheon, Item.TyrantClub },
            [Item.ClassScepters] = new Item[] { Item.Scepter, Item.GrandScepter, Item.WarScepter, Item.RuneScepter, Item.HolyWaterSprinkler, Item.DivineScepter, Item.MightyScepter, Item.SeraphRod, Item.Caduceus },
            [Item.ClassMaces] = new Item[] { Item.Mace, Item.MorningStar, Item.Flail, Item.FlangedMace, Item.JaggedStar, Item.Knout, Item.ReinforcedMace, Item.DevilStar, Item.Scourge },
            [Item.ClassHammers] = new Item[] { Item.WarHammer, Item.Maul, Item.GreatMaul, Item.BattleHammer, Item.WarClub, Item.MartelDeFer, Item.LegendaryMallet, Item.OgreMaul, Item.ThunderMaul },
            [Item.ClassSwords] = new Item[] { Item.ShortSword, Item.Scimitar, Item.Sabre, Item.Falchion, Item.CrystalSword, Item.BroadSword, Item.LongSword, Item.WarSword, Item.TwoHandedSword, Item.Claymore, Item.GiantSword, Item.BastardSword, Item.Flamberge, Item.GreatSword, Item.Gladius, Item.Cutlass, Item.Shamshir, Item.Tulwar, Item.DimensionalBlade, Item.BattleSword, Item.RuneSword, Item.AncientSword, Item.Espandon, Item.DacianFalx, Item.TuskSword, Item.GothicSword, Item.Zweihander, Item.ExecutionerSword, Item.Falcata, Item.Ataghan, Item.ElegantBlade, Item.HydraEdge, Item.PhaseBlade, Item.ConquestSword, Item.CrypticSword, Item.MythicalSword, Item.LegendSword, Item.HighlandBlade, Item.BalrogBlade, Item.ChampionSword, Item.ColossusSword, Item.ColossusBlade },
            [Item.ClassDaggers] = new Item[] { Item.Dagger, Item.Dirk, Item.Kris, Item.Blade, Item.Poignard, Item.Rondel, Item.Cinquedeas, Item.Stiletto, Item.BoneKnife, Item.MithrilPoint, Item.FangedKnife, Item.LegendSpike },
            [Item.ClassThrowingKnifes] = new Item[] { Item.ThrowingKnife, Item.BalancedKnife, Item.BattleDart, Item.WarDart, Item.FlyingKnife, Item.WingedKnife },
            [Item.ClassThrowingAxes] = new Item[] { Item.ThrowingAxe, Item.BalancedAxe, Item.Francisca, Item.Hurlbat, Item.FlyingAxe, Item.WingedAxe },
            [Item.ClassJavelins] = new Item[] { Item.Javelin, Item.Pilum, Item.ShortSpear, Item.Glaive, Item.ThrowingSpear, Item.WarJavelin, Item.GreatPilum, Item.Simbilan, Item.Spiculum, Item.Harpoon, Item.HyperionJavelin, Item.StygianPilum, Item.BalrogSpear, Item.GhostGlaive, Item.WingedHarpoon },
            [Item.ClassSpears] = new Item[] { Item.Spear, Item.Trident, Item.Brandistock, Item.Spetum, Item.Pike, Item.WarSpear, Item.Fuscina, Item.WarFork, Item.Yari, Item.Lance, Item.HyperionSpear, Item.StygianPike, Item.Mancatcher, Item.GhostSpear, Item.WarPike },
            [Item.ClassPolearms] = new Item[] { Item.Bardiche, Item.Voulge, Item.Scythe, Item.Poleaxe, Item.Halberd, Item.WarScythe, Item.LochaberAxe, Item.Bill, Item.BattleScythe, Item.Partizan, Item.BecDeCorbin, Item.GrimScythe, Item.OgreAxe, Item.ColossusVoulge, Item.Thresher, Item.CrypticAxe, Item.GreatPoleaxe, Item.GiantThresher },
            [Item.ClassStaves] = new Item[] { Item.ShortStaff, Item.LongStaff, Item.GnarledStaff, Item.BattleStaff, Item.WarStaff, Item.JoStaff, Item.QuarterStaff, Item.CedarStaff, Item.GothicStaff, Item.RuneStaff, Item.WalkingStick, Item.Stalagmite, Item.ElderStaff, Item.Shillelagh, Item.ArchonStaff },
            [Item.ClassBows] = new Item[] { Item.ShortBow, Item.HuntersBow, Item.LongBow, Item.CompositeBow, Item.ShortBattleBow, Item.LongBattleBow, Item.ShortWarBow, Item.LongWarBow, Item.EdgeBow, Item.RazorBow, Item.CedarBow, Item.DoubleBow, Item.ShortSiegeBow, Item.LargeSiegeBow, Item.RuneBow, Item.GothicBow, Item.SpiderBow, Item.BladeBow, Item.ShadowBow, Item.GreatBow, Item.DiamondBow, Item.CrusaderBow, Item.WardBow, Item.HydraBow },
            [Item.ClassCrossbows] = new Item[] { Item.LightCrossbow, Item.Crossbow, Item.HeavyCrossbow, Item.RepeatingCrossbow, Item.Arbalest, Item.SiegeCrossbow, Item.Ballista, Item.ChuKoNu, Item.PelletBow, Item.GorgonCrossbow, Item.ColossusCrossbow, Item.DemonCrossBow },

            [Item.ClassHelms] = new Item[] { Item.Cap, Item.SkullCap, Item.Helm, Item.FullHelm, Item.GreatHelm, Item.Crown, Item.Mask, Item.BoneHelm, Item.WarHat, Item.Sallet, Item.Casque, Item.Basinet, Item.WingedHelm, Item.GrandCrown, Item.DeathMask, Item.GrimHelm, Item.Shako, Item.Hydraskull, Item.Armet, Item.GiantConch, Item.SpiredHelm, Item.Corona, Item.DemonHead, Item.BoneVisage },
            [Item.ClassArmors] = new Item[] { Item.QuiltedArmor, Item.LeatherArmor, Item.HardLeatherArmor, Item.StuddedLeather, Item.RingMail, Item.ScaleMail, Item.ChainMail, Item.BreastPlate, Item.SplintMail, Item.PlateMail, Item.FieldPlate, Item.GothicPlate, Item.FullPlateMail, Item.AncientArmor, Item.LightPlate, Item.GhostArmor, Item.SerpentskinArmor, Item.DemonhideArmor, Item.TrellisedArmor, Item.LinkedMail, Item.TigulatedMail, Item.MeshArmor, Item.Cuirass, Item.RussetArmor, Item.TemplarCoat, Item.SharktoothArmor, Item.EmbossedPlate, Item.ChaosArmor, Item.OrnatePlate, Item.MagePlate, Item.DuskShroud, Item.Wyrmhide, Item.ScarabHusk, Item.WireFleece, Item.DiamondMail, Item.LoricatedMail, Item.Boneweave, Item.GreatHauberk, Item.BalrogSkin, Item.HellforgePlate, Item.KrakenShell, Item.LacqueredPlate, Item.ShadowPlate, Item.SacredArmor, Item.ArchonPlate },
            [Item.ClassShields] = new Item[] { Item.Buckler, Item.SmallShield, Item.LargeShield, Item.KiteShield, Item.TowerShield, Item.GothicShield, Item.BoneShield, Item.SpikedShield, Item.Defender, Item.RoundShield, Item.Scutum, Item.DragonShield, Item.Pavise, Item.AncientShield, Item.GrimShield, Item.BarbedShield, Item.Heater, Item.Luna, Item.Hyperion, Item.Monarch, Item.Aegis, Item.Ward, Item.TrollNest, Item.BladeBarrier },
            [Item.ClassGloves] = new Item[] { Item.LeatherGloves, Item.HeavyGloves, Item.ChainGloves, Item.LightGauntlets, Item.Gauntlets, Item.DemonhideGloves, Item.SharkskinGloves, Item.HeavyBracers, Item.BattleGauntlets, Item.WarGauntlets, Item.BrambleMitts, Item.VampireboneGloves, Item.Vambraces, Item.CrusaderGauntlets, Item.OgreGauntlets },
            [Item.ClassBoots] = new Item[] { Item.Boots, Item.HeavyBoots, Item.ChainBoots, Item.LightPlatedBoots, Item.Greaves, Item.DemonhideBoots, Item.SharkskinBoots, Item.MeshBoots, Item.BattleBoots, Item.WarBoots, Item.WyrmhideBoots, Item.ScarabshellBoots, Item.BoneweaveBoots, Item.MirroredBoots, Item.MyrmidonGreaves },
            [Item.ClassBelts] = new Item[] { Item.Sash, Item.LightBelt, Item.Belt, Item.HeavyBelt, Item.PlatedBelt, Item.DemonhideSash, Item.SharkskinBelt, Item.MeshBelt, Item.BattleBelt, Item.WarBelt, Item.SpiderwebSash, Item.VampirefangBelt, Item.MithrilCoil, Item.TrollBelt, Item.ColossusGirdle },
            [Item.ClassCirclets] = new Item[] { Item.Circlet, Item.Coronet, Item.Tiara, Item.Diadem },

            [Item.ClassAssassinKatars] = new Item[] { Item.Katar, Item.WristBlade, Item.HatchetHands, Item.Cestus, Item.Claws, Item.BladeTalons, Item.ScissorsKatar, Item.Quhab, Item.WristSpike, Item.Fascia, Item.HandScythe, Item.GreaterClaws, Item.GreaterTalons, Item.ScissorsQuhab, Item.Suwayyah, Item.WristSword, Item.WarFist, Item.BattleCestus, Item.FeralClaws, Item.RunicTalons, Item.ScissorsSuwayyah },
            [Item.ClassSorceressOrbs] = new Item[] { Item.EagleOrb, Item.SacredGlobe, Item.SmokedSphere, Item.ClaspedOrb, Item.JaredsStone, Item.GlowingOrb, Item.CrystallineGlobe, Item.CloudySphere, Item.SparklingBall, Item.SwirlingCrystal, Item.HeavenlyStone, Item.EldritchOrb, Item.DemonHeart, Item.VortexOrb, Item.DimensionalShard },
            [Item.ClassAmazonBows] = new Item[] { Item.StagBow, Item.ReflexBow, Item.AshwoodBow, Item.CeremonialBow, Item.MatriarchalBow, Item.GrandMatronBow },
            [Item.ClassAmazonSpears] = new Item[] { Item.MaidenSpear, Item.MaidenPike, Item.CeremonialSpear, Item.CeremonialPike, Item.MatriarchalSpear, Item.MatriarchalPike },
            [Item.ClassAmazonJavelins] = new Item[] { Item.MaidenJavelin, Item.CeremonialJavelin, Item.MatriarchalJavelin },
            [Item.ClassDruidHelms] = new Item[] { Item.WolfHead, Item.HawkHelm, Item.Antlers, Item.FalconMask, Item.SpiritMask, Item.AlphaHelm, Item.GriffonHeaddress, Item.HuntersGuise, Item.SacredFeathers, Item.TotemicMask, Item.BloodSpirit, Item.SunSpirit, Item.EarthSpirit, Item.SkySpirit, Item.DreamSpirit },
            [Item.ClassBarbarianHelms] = new Item[] { Item.JawboneCap, Item.FangedHelm, Item.HornedHelm, Item.AssaultHelmet, Item.AvengerGuard, Item.JawboneVisor, Item.LionHelm, Item.RageMask, Item.SavageHelmet, Item.SlayerGuard, Item.CarnageHelm, Item.FuryVisor, Item.DestroyerHelm, Item.ConquerorCrown, Item.GuardianCrown },
            [Item.ClassPaladinShields] = new Item[] { Item.Targe, Item.Rondache, Item.HeraldicShield, Item.AerinShield, Item.CrownShield, Item.AkaranTarge, Item.AkaranRondache, Item.ProtectorShield, Item.GildedShield, Item.RoyalShield, Item.SacredTarge, Item.SacredRondache, Item.KurastShield, Item.ZakarumShield, Item.VortexShield },
            [Item.ClassNecromancerShields] = new Item[] { Item.PreservedHead, Item.ZombieHead, Item.UnravellerHead, Item.GargoyleHead, Item.DemonHeadShield, Item.MummifiedTrophy, Item.FetishTrophy, Item.SextonTrophy, Item.CantorTrophy, Item.HierophantTrophy, Item.MinionSkull, Item.HellspawnSkull, Item.OverseerSkull, Item.SuccubusSkull, Item.BloodlordSkull },
        };
    }

    public class ItemLogEntry
    {
        public string Text { get; set; }
        public Color Color { get; set; }
        public DateTime LogDate { get; private set; } = DateTime.Now;
        public bool ItemLogExpired { get => DateTime.Now.Subtract(LogDate).TotalSeconds > MapAssistConfiguration.Loaded.ItemLog.DisplayForSeconds; }
        public string ItemHashString { get; set; }
        public string ShowOnMap { get; set; }
        public UnitItem UnitItem { get; set; }
        public ItemFilter Rule { get; set; }
    }

    [Flags]
    public enum ItemFlags : uint
    {
        IFLAG_NEWITEM = 0x00000001,
        IFLAG_TARGET = 0x00000002,
        IFLAG_TARGETING = 0x00000004,
        IFLAG_DELETED = 0x00000008,
        IFLAG_IDENTIFIED = 0x00000010,
        IFLAG_QUANTITY = 0x00000020,
        IFLAG_SWITCHIN = 0x00000040,
        IFLAG_SWITCHOUT = 0x00000080,
        IFLAG_BROKEN = 0x00000100,
        IFLAG_REPAIRED = 0x00000200,
        IFLAG_UNK1 = 0x00000400,
        IFLAG_SOCKETED = 0x00000800,
        IFLAG_NOSELL = 0x00001000,
        IFLAG_INSTORE = 0x00002000,
        IFLAG_NOEQUIP = 0x00004000,
        IFLAG_NAMED = 0x00008000,
        IFLAG_ISEAR = 0x00010000,
        IFLAG_STARTITEM = 0x00020000,
        IFLAG_UNK2 = 0x00040000,
        IFLAG_INIT = 0x00080000,
        IFLAG_UNK3 = 0x00100000,
        IFLAG_COMPACTSAVE = 0x00200000,
        IFLAG_ETHEREAL = 0x00400000,
        IFLAG_JUSTSAVED = 0x00800000,
        IFLAG_PERSONALIZED = 0x01000000,
        IFLAG_LOWQUALITY = 0x02000000,
        IFLAG_RUNEWORD = 0x04000000,
        IFLAG_ITEM = 0x08000000
    }

    public enum ItemQuality : uint
    {
        INFERIOR = 0x01, //0x01 Inferior
        NORMAL = 0x02, //0x02 Normal
        SUPERIOR = 0x03, //0x03 Superior
        MAGIC = 0x04, //0x04 Magic
        SET = 0x05, //0x05 Set
        RARE = 0x06, //0x06 Rare
        UNIQUE = 0x07, //0x07 Unique
        CRAFT = 0x08, //0x08 Crafted
        TEMPERED = 0x09 //0x09 Tempered
    }

    public enum InvPage : byte
    {
        INVENTORY = 0,
        EQUIP = 1,
        TRADE = 2,
        CUBE = 3,
        STASH = 4,
        BELT = 5,
        NULL = 255,
    }

    public enum StashType : byte
    {
        Body = 0,
        Personal = 1,
        Shared1 = 2,
        Shared2 = 3,
        Shared3 = 4,
        Belt = 5
    }

    public enum BodyLoc : byte
    {
        NONE, //Not Equipped
        HEAD, //Helm
        NECK, //Amulet
        TORSO, //Body Armor
        RARM, //Right-Hand
        LARM, //Left-Hand
        RRIN, //Right Ring
        LRIN, //Left Ring
        BELT, //Belt
        FEET, //Boots
        GLOVES, //Gloves
        SWRARM, //Right-Hand on Switch
        SWLARM //Left-Hand on Switch
    };

    public enum ItemMode : uint
    {
        STORED, //Item is in Storage (inventory, cube, Stash?)
        EQUIP, //Item is Equippped
        INBELT, //Item is in Belt Rows
        ONGROUND, //Item is on Ground
        ONCURSOR, //Item is on Cursor
        DROPPING, //Item is Being Dropped
        SOCKETED //Item is Socketed in another Item
    };

    public enum ItemModeMapped // Provides more detail over ItemMode
    {
        Player,
        Inventory,
        Belt,
        Cube,
        Stash,
        Vendor,
        Trade,
        Mercenary,
        Socket,
        Ground,
        Unknown
    };

    public enum Item : uint
    {
        HandAxe,
        Axe,
        DoubleAxe,
        MilitaryPick,
        WarAxe,
        LargeAxe,
        BroadAxe,
        BattleAxe,
        GreatAxe,
        GiantAxe,
        Wand,
        YewWand,
        BoneWand,
        GrimWand,
        Club,
        Scepter,
        GrandScepter,
        WarScepter,
        SpikedClub,
        Mace,
        MorningStar,
        Flail,
        WarHammer,
        Maul,
        GreatMaul,
        ShortSword,
        Scimitar,
        Sabre,
        Falchion,
        CrystalSword,
        BroadSword,
        LongSword,
        WarSword,
        TwoHandedSword,
        Claymore,
        GiantSword,
        BastardSword,
        Flamberge,
        GreatSword,
        Dagger,
        Dirk,
        Kris,
        Blade,
        ThrowingKnife,
        ThrowingAxe,
        BalancedKnife,
        BalancedAxe,
        Javelin,
        Pilum,
        ShortSpear,
        Glaive,
        ThrowingSpear,
        Spear,
        Trident,
        Brandistock,
        Spetum,
        Pike,
        Bardiche,
        Voulge,
        Scythe,
        Poleaxe,
        Halberd,
        WarScythe,
        ShortStaff,
        LongStaff,
        GnarledStaff,
        BattleStaff,
        WarStaff,
        ShortBow,
        HuntersBow,
        LongBow,
        CompositeBow,
        ShortBattleBow,
        LongBattleBow,
        ShortWarBow,
        LongWarBow,
        LightCrossbow,
        Crossbow,
        HeavyCrossbow,
        RepeatingCrossbow,
        RancidGasPotion,
        OilPotion,
        ChokingGasPotion,
        ExplodingPotion,
        StranglingGasPotion,
        FulminatingPotion,
        DecoyGidbinn,
        TheGidbinn,
        WirtsLeg,
        HoradricMalus,
        HellforgeHammer,
        HoradricStaff,
        StaffOfKings,
        Hatchet,
        Cleaver,
        TwinAxe,
        Crowbill,
        Naga,
        MilitaryAxe,
        BeardedAxe,
        Tabar,
        GothicAxe,
        AncientAxe,
        BurntWand,
        PetrifiedWand,
        TombWand,
        GraveWand,
        Cudgel,
        RuneScepter,
        HolyWaterSprinkler,
        DivineScepter,
        BarbedClub,
        FlangedMace,
        JaggedStar,
        Knout,
        BattleHammer,
        WarClub,
        MartelDeFer,
        Gladius,
        Cutlass,
        Shamshir,
        Tulwar,
        DimensionalBlade,
        BattleSword,
        RuneSword,
        AncientSword,
        Espandon,
        DacianFalx,
        TuskSword,
        GothicSword,
        Zweihander,
        ExecutionerSword,
        Poignard,
        Rondel,
        Cinquedeas,
        Stiletto,
        BattleDart,
        Francisca,
        WarDart,
        Hurlbat,
        WarJavelin,
        GreatPilum,
        Simbilan,
        Spiculum,
        Harpoon,
        WarSpear,
        Fuscina,
        WarFork,
        Yari,
        Lance,
        LochaberAxe,
        Bill,
        BattleScythe,
        Partizan,
        BecDeCorbin,
        GrimScythe,
        JoStaff,
        QuarterStaff,
        CedarStaff,
        GothicStaff,
        RuneStaff,
        EdgeBow,
        RazorBow,
        CedarBow,
        DoubleBow,
        ShortSiegeBow,
        LargeSiegeBow,
        RuneBow,
        GothicBow,
        Arbalest,
        SiegeCrossbow,
        Ballista,
        ChuKoNu,
        KhalimsFlail,
        KhalimsWill,
        Katar,
        WristBlade,
        HatchetHands,
        Cestus,
        Claws,
        BladeTalons,
        ScissorsKatar,
        Quhab,
        WristSpike,
        Fascia,
        HandScythe,
        GreaterClaws,
        GreaterTalons,
        ScissorsQuhab,
        Suwayyah,
        WristSword,
        WarFist,
        BattleCestus,
        FeralClaws,
        RunicTalons,
        ScissorsSuwayyah,
        Tomahawk,
        SmallCrescent,
        EttinAxe,
        WarSpike,
        BerserkerAxe,
        FeralAxe,
        SilverEdgedAxe,
        Decapitator,
        ChampionAxe,
        GloriousAxe,
        PolishedWand,
        GhostWand,
        LichWand,
        UnearthedWand,
        Truncheon,
        MightyScepter,
        SeraphRod,
        Caduceus,
        TyrantClub,
        ReinforcedMace,
        DevilStar,
        Scourge,
        LegendaryMallet,
        OgreMaul,
        ThunderMaul,
        Falcata,
        Ataghan,
        ElegantBlade,
        HydraEdge,
        PhaseBlade,
        ConquestSword,
        CrypticSword,
        MythicalSword,
        LegendSword,
        HighlandBlade,
        BalrogBlade,
        ChampionSword,
        ColossusSword,
        ColossusBlade,
        BoneKnife,
        MithrilPoint,
        FangedKnife,
        LegendSpike,
        FlyingKnife,
        FlyingAxe,
        WingedKnife,
        WingedAxe,
        HyperionJavelin,
        StygianPilum,
        BalrogSpear,
        GhostGlaive,
        WingedHarpoon,
        HyperionSpear,
        StygianPike,
        Mancatcher,
        GhostSpear,
        WarPike,
        OgreAxe,
        ColossusVoulge,
        Thresher,
        CrypticAxe,
        GreatPoleaxe,
        GiantThresher,
        WalkingStick,
        Stalagmite,
        ElderStaff,
        Shillelagh,
        ArchonStaff,
        SpiderBow,
        BladeBow,
        ShadowBow,
        GreatBow,
        DiamondBow,
        CrusaderBow,
        WardBow,
        HydraBow,
        PelletBow,
        GorgonCrossbow,
        ColossusCrossbow,
        DemonCrossBow,
        EagleOrb,
        SacredGlobe,
        SmokedSphere,
        ClaspedOrb,
        JaredsStone,
        StagBow,
        ReflexBow,
        MaidenSpear,
        MaidenPike,
        MaidenJavelin,
        GlowingOrb,
        CrystallineGlobe,
        CloudySphere,
        SparklingBall,
        SwirlingCrystal,
        AshwoodBow,
        CeremonialBow,
        CeremonialSpear,
        CeremonialPike,
        CeremonialJavelin,
        HeavenlyStone,
        EldritchOrb,
        DemonHeart,
        VortexOrb,
        DimensionalShard,
        MatriarchalBow,
        GrandMatronBow,
        MatriarchalSpear,
        MatriarchalPike,
        MatriarchalJavelin,
        Cap,
        SkullCap,
        Helm,
        FullHelm,
        GreatHelm,
        Crown,
        Mask,
        QuiltedArmor,
        LeatherArmor,
        HardLeatherArmor,
        StuddedLeather,
        RingMail,
        ScaleMail,
        ChainMail,
        BreastPlate,
        SplintMail,
        PlateMail,
        FieldPlate,
        GothicPlate,
        FullPlateMail,
        AncientArmor,
        LightPlate,
        Buckler,
        SmallShield,
        LargeShield,
        KiteShield,
        TowerShield,
        GothicShield,
        LeatherGloves,
        HeavyGloves,
        ChainGloves,
        LightGauntlets,
        Gauntlets,
        Boots,
        HeavyBoots,
        ChainBoots,
        LightPlatedBoots,
        Greaves,
        Sash,
        LightBelt,
        Belt,
        HeavyBelt,
        PlatedBelt,
        BoneHelm,
        BoneShield,
        SpikedShield,
        WarHat,
        Sallet,
        Casque,
        Basinet,
        WingedHelm,
        GrandCrown,
        DeathMask,
        GhostArmor,
        SerpentskinArmor,
        DemonhideArmor,
        TrellisedArmor,
        LinkedMail,
        TigulatedMail,
        MeshArmor,
        Cuirass,
        RussetArmor,
        TemplarCoat,
        SharktoothArmor,
        EmbossedPlate,
        ChaosArmor,
        OrnatePlate,
        MagePlate,
        Defender,
        RoundShield,
        Scutum,
        DragonShield,
        Pavise,
        AncientShield,
        DemonhideGloves,
        SharkskinGloves,
        HeavyBracers,
        BattleGauntlets,
        WarGauntlets,
        DemonhideBoots,
        SharkskinBoots,
        MeshBoots,
        BattleBoots,
        WarBoots,
        DemonhideSash,
        SharkskinBelt,
        MeshBelt,
        BattleBelt,
        WarBelt,
        GrimHelm,
        GrimShield,
        BarbedShield,
        WolfHead,
        HawkHelm,
        Antlers,
        FalconMask,
        SpiritMask,
        JawboneCap,
        FangedHelm,
        HornedHelm,
        AssaultHelmet,
        AvengerGuard,
        Targe,
        Rondache,
        HeraldicShield,
        AerinShield,
        CrownShield,
        PreservedHead,
        ZombieHead,
        UnravellerHead,
        GargoyleHead,
        DemonHeadShield,
        Circlet,
        Coronet,
        Tiara,
        Diadem,
        Shako,
        Hydraskull,
        Armet,
        GiantConch,
        SpiredHelm,
        Corona,
        DemonHead,
        DuskShroud,
        Wyrmhide,
        ScarabHusk,
        WireFleece,
        DiamondMail,
        LoricatedMail,
        Boneweave,
        GreatHauberk,
        BalrogSkin,
        HellforgePlate,
        KrakenShell,
        LacqueredPlate,
        ShadowPlate,
        SacredArmor,
        ArchonPlate,
        Heater,
        Luna,
        Hyperion,
        Monarch,
        Aegis,
        Ward,
        BrambleMitts,
        VampireboneGloves,
        Vambraces,
        CrusaderGauntlets,
        OgreGauntlets,
        WyrmhideBoots,
        ScarabshellBoots,
        BoneweaveBoots,
        MirroredBoots,
        MyrmidonGreaves,
        SpiderwebSash,
        VampirefangBelt,
        MithrilCoil,
        TrollBelt,
        ColossusGirdle,
        BoneVisage,
        TrollNest,
        BladeBarrier,
        AlphaHelm,
        GriffonHeaddress,
        HuntersGuise,
        SacredFeathers,
        TotemicMask,
        JawboneVisor,
        LionHelm,
        RageMask,
        SavageHelmet,
        SlayerGuard,
        AkaranTarge,
        AkaranRondache,
        ProtectorShield,
        GildedShield,
        RoyalShield,
        MummifiedTrophy,
        FetishTrophy,
        SextonTrophy,
        CantorTrophy,
        HierophantTrophy,
        BloodSpirit,
        SunSpirit,
        EarthSpirit,
        SkySpirit,
        DreamSpirit,
        CarnageHelm,
        FuryVisor,
        DestroyerHelm,
        ConquerorCrown,
        GuardianCrown,
        SacredTarge,
        SacredRondache,
        KurastShield,
        ZakarumShield,
        VortexShield,
        MinionSkull,
        HellspawnSkull,
        OverseerSkull,
        SuccubusSkull,
        BloodlordSkull,
        Elixir,
        INVALID509,
        INVALID510,
        INVALID511,
        INVALID512,
        StaminaPotion,
        AntidotePotion,
        RejuvenationPotion,
        FullRejuvenationPotion,
        ThawingPotion,
        TomeOfTownPortal,
        TomeOfIdentify,
        Amulet,
        AmuletOfTheViper,
        Ring,
        Gold,
        ScrollOfInifuss,
        KeyToTheCairnStones,
        Arrows,
        Torch,
        Bolts,
        ScrollOfTownPortal,
        ScrollOfIdentify,
        Heart,
        Brain,
        Jawbone,
        Eye,
        Horn,
        Tail,
        Flag,
        Fang,
        Quill,
        Soul,
        Scalp,
        Spleen,
        Key,
        TheBlackTowerKey,
        PotionOfLife,
        AJadeFigurine,
        TheGoldenBird,
        LamEsensTome,
        HoradricCube,
        HoradricScroll,
        MephistosSoulstone,
        BookOfSkill,
        KhalimsEye,
        KhalimsHeart,
        KhalimsBrain,
        Ear,
        ChippedAmethyst,
        FlawedAmethyst,
        Amethyst,
        FlawlessAmethyst,
        PerfectAmethyst,
        ChippedTopaz,
        FlawedTopaz,
        Topaz,
        FlawlessTopaz,
        PerfectTopaz,
        ChippedSapphire,
        FlawedSapphire,
        Sapphire,
        FlawlessSapphire,
        PerfectSapphire,
        ChippedEmerald,
        FlawedEmerald,
        Emerald,
        FlawlessEmerald,
        PerfectEmerald,
        ChippedRuby,
        FlawedRuby,
        Ruby,
        FlawlessRuby,
        PerfectRuby,
        ChippedDiamond,
        FlawedDiamond,
        Diamond,
        FlawlessDiamond,
        PerfectDiamond,
        MinorHealingPotion,
        LightHealingPotion,
        HealingPotion,
        GreaterHealingPotion,
        SuperHealingPotion,
        MinorManaPotion,
        LightManaPotion,
        ManaPotion,
        GreaterManaPotion,
        SuperManaPotion,
        ChippedSkull,
        FlawedSkull,
        Skull,
        FlawlessSkull,
        PerfectSkull,
        Herb,
        SmallCharm,
        LargeCharm,
        GrandCharm,
        INVALID606,
        INVALID607,
        INVALID608,
        INVALID609,
        ElRune,
        EldRune,
        TirRune,
        NefRune,
        EthRune,
        IthRune,
        TalRune,
        RalRune,
        OrtRune,
        ThulRune,
        AmnRune,
        SolRune,
        ShaelRune,
        DolRune,
        HelRune,
        IoRune,
        LumRune,
        KoRune,
        FalRune,
        LemRune,
        PulRune,
        UmRune,
        MalRune,
        IstRune,
        GulRune,
        VexRune,
        OhmRune,
        LoRune,
        SurRune,
        BerRune,
        JahRune,
        ChamRune,
        ZodRune,
        Jewel,
        MalahsPotion,
        ScrollOfKnowledge,
        ScrollOfResistance,
        KeyOfTerror,
        KeyOfHate,
        KeyOfDestruction,
        DiablosHorn,
        BaalsEye,
        MephistosBrain,
        TokenofAbsolution,
        TwistedEssenceOfSuffering,
        ChargedEssenceOfHatred,
        BurningEssenceOfTerror,
        FesteringEssenceOfDestruction,
        StandardOfHeroes,

        // Used only for item filter
        ClassAxes = 0xFFD0,

        ClassWands,
        ClassClubs,
        ClassScepters,
        ClassMaces,
        ClassHammers,
        ClassSwords,
        ClassDaggers,
        ClassThrowingKnifes,
        ClassThrowingAxes,
        ClassJavelins,
        ClassSpears,
        ClassPolearms,
        ClassStaves,
        ClassBows,
        ClassCrossbows,

        ClassHelms,
        ClassArmors,
        ClassShields,
        ClassGloves,
        ClassBoots,
        ClassBelts,
        ClassCirclets,

        ClassAssassinKatars,
        ClassSorceressOrbs,
        ClassAmazonBows,
        ClassAmazonSpears,
        ClassAmazonJavelins,
        ClassDruidHelms,
        ClassBarbarianHelms,
        ClassPaladinShields,
        ClassNecromancerShields,

        xBases = 0xFFFF - 0x1,
        Any = 0xFFFF
    };

    public enum ItemTier
    {
        Normal,
        Exceptional,
        Elite,
        NotApplicable
    }
}
