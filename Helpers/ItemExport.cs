using MapAssist.Structs;
using MapAssist.Types;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MapAssist.Helpers
{
    public class ItemExport
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private static string itemTemplate = "<div class=\"item\"><div class=\"item-name\" style=\"color: {{color}}\">{{name}}</div>{{stats}}</div>";
        private static string statTemplate = "<div class=\"stat\" style=\"color:#4169E1\">{{text}}</div>";

        public static void ExportPlayerInventory(UnitPlayer player, UnitItem[] itemAry)
        {
            var itemsExport = GetItemsExport(player, itemAry);

            ExportPlayerInventoryHTML(player, itemsExport);
            ExportPlayerInventoryJSON(player, itemsExport);
        }

        public static ExportedItems GetItemsExport(UnitPlayer player, UnitItem[] itemAry)
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                var items = itemAry.Select(item => { item.IsCached = false; return item.Update(); }).ToList();

                var equippedItems = items.Where(x => x.ItemData.dwOwnerID == player.UnitId && x.ItemData.InvPage == InvPage.NULL && x.ItemData.BodyLoc != BodyLoc.NONE).ToList();
                var inventoryItems = items.Where(x => x.ItemData.dwOwnerID == player.UnitId && x.ItemData.InvPage == InvPage.INVENTORY).ToList();
                var mercItems = items.Where(x => x.ItemMode == ItemMode.EQUIP && x.ItemModeMapped == ItemModeMapped.Mercenary).ToList();
                var stashPersonalItems = items.Where(x => x.ItemModeMapped == ItemModeMapped.Stash && x.StashTab == StashTab.Personal).ToList();
                var stashShared1Items = items.Where(x => x.ItemModeMapped == ItemModeMapped.Stash && x.StashTab == StashTab.Shared1).ToList();
                var stashShared2Items = items.Where(x => x.ItemModeMapped == ItemModeMapped.Stash && x.StashTab == StashTab.Shared2).ToList();
                var stashShared3Items = items.Where(x => x.ItemModeMapped == ItemModeMapped.Stash && x.StashTab == StashTab.Shared3).ToList();
                var cubeItems = items.Where(x => x.ItemData.dwOwnerID == player.UnitId && x.ItemModeMapped == ItemModeMapped.Cube).ToList();

                return new ExportedItems()
                {
                    equipped = equippedItems,
                    inventory = inventoryItems,
                    mercenary = mercItems,
                    cube = cubeItems,
                    personalStash = stashPersonalItems,
                    sharedStashTab1 = stashShared1Items,
                    sharedStashTab2 = stashShared2Items,
                    sharedStashTab3 = stashShared3Items,
                };
            }
        }

        public static void ExportPlayerInventoryJSON(UnitPlayer player, ExportedItems items)
        {
            var outputfile = player.Name + ".json";

            var json = new Dictionary<string, object>
            {
                { "items",  new Dictionary<string, List<JSONItem>> {
                    { "equipped", ItemsToList(items.equipped)},
                    { "inventory", ItemsToList(items.inventory) },
                    { "mercenary", ItemsToList(items.mercenary) },
                    { "cube", ItemsToList(items.cube) },
                    { "personalStash", ItemsToList(items.personalStash) },
                    { "sharedStashTab1", ItemsToList(items.sharedStashTab1) },
                    { "sharedStashTab2", ItemsToList(items.sharedStashTab2) },
                    { "sharedStashTab3", ItemsToList(items.sharedStashTab3) },
                }}
            };
            var finalJSONstr = JsonConvert.SerializeObject(json);

            File.WriteAllText(outputfile, finalJSONstr);
            _log.Info($"Created JSON item file {outputfile}");
        }

        public static void ExportPlayerInventoryHTML(UnitPlayer player, ExportedItems items)
        {
            var finalHTMLstr = Properties.Resources.InventoryExportTemplate;
            var outputfile = player.Name + ".html";

            finalHTMLstr = finalHTMLstr.Replace("{{player-name}}", player.Name);

            if (items.equipped.Count() > 0)
            {
                finalHTMLstr = finalHTMLstr.Replace("{{show-equipped}}", "show");
                finalHTMLstr = finalHTMLstr.Replace("{{equipped-items}}", GetItemHtmlList(items.equipped));
            }

            if (items.inventory.Count() > 0)
            {
                finalHTMLstr = finalHTMLstr.Replace("{{show-inventory}}", "show");
                finalHTMLstr = finalHTMLstr.Replace("{{inventory-items}}", GetItemHtmlList(items.inventory));
            }

            if (items.mercenary.Count() > 0)
            {
                finalHTMLstr = finalHTMLstr.Replace("{{show-merc}}", "show");
                finalHTMLstr = finalHTMLstr.Replace("{{merc-items}}", GetItemHtmlList(items.mercenary));
            }

            if (items.personalStash.Count() > 0)
            {
                finalHTMLstr = finalHTMLstr.Replace("{{show-stash-personal}}", "show");
                finalHTMLstr = finalHTMLstr.Replace("{{stash-personal-items}}", GetItemHtmlList(items.personalStash));
            }

            if (items.sharedStashTab1.Count() > 0)
            {
                finalHTMLstr = finalHTMLstr.Replace("{{show-stash-shared1}}", "show");
                finalHTMLstr = finalHTMLstr.Replace("{{stash-shared1-items}}", GetItemHtmlList(items.sharedStashTab1));
            }

            if (items.sharedStashTab2.Count() > 0)
            {
                finalHTMLstr = finalHTMLstr.Replace("{{show-stash-shared2}}", "show");
                finalHTMLstr = finalHTMLstr.Replace("{{stash-shared2-items}}", GetItemHtmlList(items.sharedStashTab2));
            }

            if (items.sharedStashTab3.Count() > 0)
            {
                finalHTMLstr = finalHTMLstr.Replace("{{show-stash-shared3}}", "show");
                finalHTMLstr = finalHTMLstr.Replace("{{stash-shared3-items}}", GetItemHtmlList(items.sharedStashTab3));
            }

            if (items.cube.Count() > 0)
            {
                finalHTMLstr = finalHTMLstr.Replace("{{show-cube}}", "show");
                finalHTMLstr = finalHTMLstr.Replace("{{cube-items}}", GetItemHtmlList(items.cube));
            }

            File.WriteAllText(outputfile, finalHTMLstr);
            _log.Info($"Created HTML item file {outputfile}");
        }

        public static List<JSONItem> ItemsToList(List<UnitItem> filteredItems)
        {
            var itemJSONarr = new List<JSONItem>();
            foreach (var item in filteredItems)
            {
                item.Stats.TryGetValue(Stats.Stat.NumSockets, out var numSockets);
                var thisItem = new JSONItem()
                {
                    txtFileNo = item.TxtFileNo,
                    baseName = item.ItemBaseName,
                    quality = item.ItemData.ItemQuality.ToString(),
                    fullName = Items.ItemFullName(item),
                    ethereal = ((item.ItemData.ItemFlags & ItemFlags.IFLAG_ETHEREAL) == ItemFlags.IFLAG_ETHEREAL),
                    identified = item.IsIdentified,
                    numSockets = numSockets,
                    position = new Position() { x = (uint)item.Position.X, y = (uint)item.Position.Y },
                    bodyLoc = item.ItemData.BodyLoc.ToString(),
                    affixes = GetAffixes(item)
                };
                itemJSONarr.Add(thisItem);
            }
            return itemJSONarr;
        }

        public static List<Affix> GetAffixes(UnitItem item)
        {
            var affixes = new List<Affix>();
            foreach (var (stat, values) in item.StatLayers.Select(x => (x.Key, x.Value)))
            {
                var name = AddSpaces(stat.ToString());

                foreach (var (layer, value) in values.Select(x => (x.Key, x.Value)))
                {
                    var finalValue = value.ToString();

                    if (Stats.StatShifts.ContainsKey(stat))
                    {
                        finalValue = Items.GetItemStatShifted(item, stat).ToString();
                    }
                    else if (Stats.StatDivisors.ContainsKey(stat) || Stats.StatInvertDivisors.ContainsKey(stat))
                    {
                        finalValue = Items.GetItemStatDecimal(item, stat).ToString();
                    }
                    else if (Stats.NegativeValueStats.Contains(stat))
                    {
                        finalValue = (-value).ToString();
                    }
                    else if (stat == Stats.Stat.AddClassSkills)
                    {
                        var (classSkills, points) = Items.GetItemStatAddClassSkills(item, (PlayerClass)layer);
                        name = classSkills[0].ToString() + " Skills";
                        finalValue = points.ToString();
                    }
                    else if (stat == Stats.Stat.AddSkillTab)
                    {
                        var (skillTrees, points) = Items.GetItemStatAddSkillTreeSkills(item, (SkillTree)layer, false);
                        name = AddSpaces(skillTrees[0].ToString());
                        finalValue = points.ToString();
                    }
                    else if (stat == Stats.Stat.SingleSkill || stat == Stats.Stat.NonClassSkill)
                    {
                        var (skills, points) = Items.GetItemStatAddSingleSkills(item, (Skill)layer, false);
                        name = AddSpaces(skills[0].ToString());
                        finalValue = points.ToString();
                    }
                    else if (stat == Stats.Stat.ItemChargedSkill)
                    {
                        var skill = (Skill)(layer >> 6);

                        var (skillLevel, currentCharges, maxCharges) = Items.GetItemStatAddSkillCharges(item, skill);
                        name = AddSpaces(skill.ToString()) + " Charges";

                        var chargesText = "";
                        if (currentCharges > 0 && maxCharges > 0)
                        {
                            chargesText = $"{currentCharges}/{maxCharges}";
                        }

                        finalValue = $"Level {skillLevel} ({chargesText})";
                    }
                    else if (stat.ToString().StartsWith("SkillOn"))
                    {
                        var skill = (Skill)(layer >> 6);
                        var level = layer % (1 << 6);

                        name = AddSpaces(skill.ToString()) + " On " + AddSpaces(stat.ToString().Replace("SkillOn", ""));

                        finalValue = $"Level {level} ({value}% chance)";
                    }
                    else if (stat == Stats.Stat.Aura)
                    {
                        var skill = (Skill)layer;

                        name = AddSpaces(skill.ToString()) + " Aura";

                        finalValue = $"Level {value}";
                    }

                    var thisAffix = new Affix()
                    {
                        name = name,
                        value = finalValue
                    };
                    affixes.Add(thisAffix);
                }
            }
            return affixes;
        }

        private static string GetItemHtmlList(IEnumerable<UnitItem> items)
        {
            var result = "";

            foreach (var item in items.OrderBy(x => x.TxtFileNo))
            {
                result += GetItemHtml(item);
            }

            return result;
        }

        private static string GetItemHtml(UnitItem item)
        {
            var itemName = Items.ItemFullName(item);

            if (item.ItemData.ItemQuality > ItemQuality.SUPERIOR && item.IsIdentified)
            {
                itemName = itemName.Replace("[Identified] ", "");
            }

            var itemText = itemTemplate.Replace("{{color}}", ColorTranslator.ToHtml(item.ItemBaseColor)).Replace("{{name}}", itemName);
            var statText = "";

            if (item.ItemData.ItemQuality > ItemQuality.SUPERIOR && !item.IsIdentified)
            {
                statText += statTemplate.Replace("{{text}}", "Unidentified").Replace("4169E1", "DD0000");

                if (item.Stats.TryGetValue(Stats.Stat.Defense, out var defense))
                {
                    statText += statTemplate.Replace("{{text}}", AddSpaces(Stats.Stat.Defense.ToString()) + ": " + defense);
                }
            }
            else
            {
                var affixes = GetAffixes(item);
                foreach (var affix in affixes)
                {
                    statText += statTemplate.Replace("{{text}}", affix.name + ": " + affix.value);
                }
            }

            itemText = itemText.Replace("{{stats}}", statText);

            return itemText;
        }

        public static string AddSpaces(string text) => Regex.Replace(text.ToString(), "(\\B[A-Z][a-z])", " $1");
    }
}
