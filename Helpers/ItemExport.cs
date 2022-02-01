using MapAssist.Structs;
using MapAssist.Types;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MapAssist.Helpers
{
    public class ItemExport
    {
        private static string itemTemplate = "<div class=\"item\"><div class=\"item-name\" style=\"color: {{color}}\">{{name}}</div>{{stats}}</div>";
        private static string statTemplate = "<div class=\"stat\" style=\"color:#4169E1\">{{text}}</div>";

        public static void ExportPlayerInventory(UnitPlayer player, UnitItem[] itemAry)
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                var template = Properties.Resources.InventoryExportTemplate;
                var outputfile = player.Name + ".html";

                template = template.Replace("{{player-name}}", player.Name);

                var items = itemAry.Select(item => item.Update()).ToList();

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

                var stashItems = items.Where(x => x.ItemData.dwOwnerID == player.UnitId && x.ItemData.InvPage == InvPage.STASH);

                if (stashItems.Count() > 0)
                {
                    template = template.Replace("{{show-stash}}", "show");
                    template = template.Replace("{{stash-items}}", GetItemList(stashItems));
                }

                var cubeItems = items.Where(x => x.ItemData.dwOwnerID == player.UnitId && x.ItemModeMapped == ItemModeMapped.Cube);

                if (cubeItems.Count() > 0)
                {
                    template = template.Replace("{{show-cube}}", "show");
                    template = template.Replace("{{cube-items}}", GetItemList(cubeItems));
                }

                File.WriteAllText(outputfile, template);
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

                        statText += statTemplate.Replace("{{text}}", name + ": " + finalValue);
                    }
                }
            }

            itemText = itemText.Replace("{{stats}}", statText);

            return itemText;
        }

        public static string AddSpaces(string text) => Regex.Replace(text.ToString(), "(\\B[A-Z][a-z])", " $1");
    }
}
