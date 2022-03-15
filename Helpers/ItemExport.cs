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
            ExportPlayerInventoryHTML(player, itemAry);
            ExportPlayerInventoryJSON(player, itemAry);
        }

        public static void ExportPlayerInventoryJSON(UnitPlayer player, UnitItem[] itemAry)
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                var outputfile = player.Name + ".json";

                var items = itemAry.Select(item => { item.IsCached = false; return item.Update(); }).ToList();

                var equippedItems = items.Where(x => x.ItemData.dwOwnerID == player.UnitId && x.ItemData.InvPage == InvPage.NULL && x.ItemData.BodyLoc != BodyLoc.NONE).ToList();
                var inventoryItems = items.Where(x => x.ItemData.dwOwnerID == player.UnitId && x.ItemData.InvPage == InvPage.INVENTORY).ToList();
                var mercItems = items.Where(x => x.ItemMode == ItemMode.EQUIP && x.ItemModeMapped == ItemModeMapped.Mercenary).ToList();
                var stashPersonalItems = items.Where(x => x.ItemModeMapped == ItemModeMapped.Stash && x.StashTab == StashTab.Personal).ToList();
                var stashShared1Items = items.Where(x => x.ItemModeMapped == ItemModeMapped.Stash && x.StashTab == StashTab.Shared1).ToList();
                var stashShared2Items = items.Where(x => x.ItemModeMapped == ItemModeMapped.Stash && x.StashTab == StashTab.Shared2).ToList();
                var stashShared3Items = items.Where(x => x.ItemModeMapped == ItemModeMapped.Stash && x.StashTab == StashTab.Shared3).ToList();
                //var cubeItems = items.Where(x => x.ItemData.dwOwnerID == player.UnitId && x.ItemModeMapped == ItemModeMapped.Cube).ToList();

                // initial object to be serialized to JSON
                var json = new ItemsExport()
                {
                    items = new JSONItems()
                    {
                        equipped = ItemsToList(equippedItems),
                        inventory = ItemsToList(inventoryItems),
                        mercenary = ItemsToList(mercItems),
                        personalStash = ItemsToList(stashPersonalItems),
                        sharedStashTab1 = ItemsToList(stashShared1Items),
                        sharedStashTab2 = ItemsToList(stashShared2Items),
                        sharedStashTab3 = ItemsToList(stashShared3Items),
                    }
                };

                var finalJSONstr = JsonConvert.SerializeObject(json);
                File.WriteAllText(outputfile, finalJSONstr);
                _log.Info($"Created JSON item file {outputfile}");

            }
        }

        public static List<JSONItem> ItemsToList(List<UnitItem> filteredItems)
        {
            var itemJSONarr = new List<JSONItem>();
            foreach (var item in filteredItems)
            {
                item.Stats.TryGetValue(Stats.Stat.NumSockets, out var numSockets);
                var itemName = Items.ItemFullName(item);
                var thisItem = new JSONItem()
                {
                    txtFileNo = item.TxtFileNo,
                    baseName = item.ItemBaseName,
                    quality = item.ItemData.ItemQuality.ToString(),
                    fullName = itemName,
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
                    else if (Stats.StatDivisors.ContainsKey(stat))
                    {
                        finalValue = Items.GetItemStatDecimal(item, stat).ToString();
                    }
                    else if (stat == Stats.Stat.AddClassSkills)
                    {
                        var (classSkills, points) = Items.GetItemStatAddClassSkills(item, (PlayerClass)layer);
                        name = classSkills[0].ToString() + " Skills";
                        finalValue = points.ToString();
                    }
                    else if (stat == Stats.Stat.AddSkillTab)
                    {
                        var (skillTrees, points) = Items.GetItemStatAddSkillTreeSkills(item, (SkillTree)layer);
                        name = AddSpaces(skillTrees[0].ToString());
                        finalValue = points.ToString();
                    }
                    else if (stat == Stats.Stat.SingleSkill || stat == Stats.Stat.NonClassSkill)
                    {
                        var (skills, points) = Items.GetItemStatAddSingleSkills(item, (Skill)layer);
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
                        var chance = layer % (1 << 6);

                        name = AddSpaces(skill.ToString()) + " On " + AddSpaces(stat.ToString().Replace("SkillOn", ""));

                        finalValue = $"Level {value} ({chance}% chance)";
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

        public static void ExportPlayerInventoryHTML(UnitPlayer player, UnitItem[] itemAry)
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                var template = Properties.Resources.InventoryExportTemplate;
                var outputfile = player.Name + ".html";

                template = template.Replace("{{player-name}}", player.Name);

                var items = itemAry.Select(item => { item.IsCached = false; return item.Update(); }).ToList();

                var equippedItems = items.Where(x => x.ItemData.dwOwnerID == player.UnitId && x.ItemData.InvPage == InvPage.NULL && x.ItemData.BodyLoc != BodyLoc.NONE);

                if (equippedItems.Count() > 0)
                {
                    template = template.Replace("{{show-equipped}}", "show");
                    template = template.Replace("{{equipped-items}}", GetItemList(equippedItems));
                }

                var inventoryItems = items.Where(x => x.ItemData.dwOwnerID == player.UnitId && x.ItemData.InvPage == InvPage.INVENTORY);

                if (inventoryItems.Count() > 0)
                {
                    template = template.Replace("{{show-inventory}}", "show");
                    template = template.Replace("{{inventory-items}}", GetItemList(inventoryItems));
                }

                var mercItems = items.Where(x => x.ItemMode == ItemMode.EQUIP && x.ItemModeMapped == ItemModeMapped.Mercenary);

                if (mercItems.Count() > 0)
                {
                    template = template.Replace("{{show-merc}}", "show");
                    template = template.Replace("{{merc-items}}", GetItemList(mercItems));
                }

                var stashPersonalItems = items.Where(x => x.ItemModeMapped == ItemModeMapped.Stash && x.StashTab == StashTab.Personal);

                if (stashPersonalItems.Count() > 0)
                {
                    template = template.Replace("{{show-stash-personal}}", "show");
                    template = template.Replace("{{stash-personal-items}}", GetItemList(stashPersonalItems));
                }

                var stashShared1Items = items.Where(x => x.ItemModeMapped == ItemModeMapped.Stash && x.StashTab == StashTab.Shared1);

                if (stashShared1Items.Count() > 0)
                {
                    template = template.Replace("{{show-stash-shared1}}", "show");
                    template = template.Replace("{{stash-shared1-items}}", GetItemList(stashShared1Items));
                }

                var stashshared2Items = items.Where(x => x.ItemModeMapped == ItemModeMapped.Stash && x.StashTab == StashTab.Shared2);

                if (stashshared2Items.Count() > 0)
                {
                    template = template.Replace("{{show-stash-shared2}}", "show");
                    template = template.Replace("{{stash-shared2-items}}", GetItemList(stashshared2Items));
                }

                var stashshared3Items = items.Where(x => x.ItemModeMapped == ItemModeMapped.Stash && x.StashTab == StashTab.Shared3);

                if (stashshared3Items.Count() > 0)
                {
                    template = template.Replace("{{show-stash-shared3}}", "show");
                    template = template.Replace("{{stash-shared3-items}}", GetItemList(stashshared3Items));
                }

                var cubeItems = items.Where(x => x.ItemData.dwOwnerID == player.UnitId && x.ItemModeMapped == ItemModeMapped.Cube);

                if (cubeItems.Count() > 0)
                {
                    template = template.Replace("{{show-cube}}", "show");
                    template = template.Replace("{{cube-items}}", GetItemList(cubeItems));
                }

                File.WriteAllText(outputfile, template);
                _log.Info($"Created HTML item file {outputfile}");
            }
        }

        private static string GetItemList(IEnumerable<UnitItem> items)
        {
            var result = "";

            foreach (UnitItem item in items.OrderBy(x => x.TxtFileNo))
            {
                result += GetItemHtml(item);
            }

            return result;
        }

        private static string GetItemHtml(UnitItem item)
        {
            var itemName = Items.ItemLogDisplayName(item, new Settings.ItemFilter());

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
