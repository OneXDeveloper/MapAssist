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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using MapAssist.Types;
using MapAssist.Settings;

namespace MapAssist.Helpers
{
    public class ItemFilter
    {
        public string Quality { get; set; }

        public bool? Ethereal { get; set; }

        public string Sockets { get; set; }
    }
    public static class LootFilter
    {
        public static Dictionary<string, List<ItemFilter>> yaml;
        private static bool LoadFilter()
        {
            if (yaml == null)
            {
                var deserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
                var yamlFileLocation = System.AppContext.BaseDirectory + @"\" + Rendering.ItemFilterFileName;
                var rawYaml = "";
                try
                {
                    rawYaml = File.ReadAllText(yamlFileLocation);
                    yaml = deserializer.Deserialize<Dictionary<string, List<ItemFilter>>>(rawYaml);
                }
                catch(Exception e)
                {
                    Console.WriteLine("Error reading from {0}. Message = {1}", yamlFileLocation, e.Message);
                    return false;
                }
            }
            if (yaml != null)
            {
                return true;
            }
            return false;
        }
        public static bool Filter(UnitAny unitAny)
        {
            var baseName = Items.ItemNames[unitAny.TxtFileNo];
            var itemQuality = Enum.GetName(typeof(ItemQuality), unitAny.ItemData.ItemQuality).ToLower();
            var isEth = (unitAny.ItemData.ItemFlags & ItemFlags.IFLAG_ETHEREAL) == ItemFlags.IFLAG_ETHEREAL;
            var lowQuality = (unitAny.ItemData.ItemFlags & ItemFlags.IFLAG_LOWQUALITY) == ItemFlags.IFLAG_LOWQUALITY;
            var hasSockets = unitAny.Stats.TryGetValue(Stat.STAT_ITEM_NUMSOCKETS, out var numSockets);
            if (!hasSockets)
            {
                numSockets = 0;
            }
            return Filter(baseName, itemQuality, isEth, numSockets, lowQuality);
        }
        private static bool Filter(string baseName, string itemQuality, bool isEth, int numSockets, bool lowQuality)
        {
            if (!LoadFilter())
            {
                return false;
            }
            if (lowQuality)
            {
                return false;
            }
            var qualityReqMet = false;
            //populate a list of filter rules by combining rules from "Any" and the item base name
            //use only one list or the other depending on if "Any" exists
            var fullFilterList = new List<ItemFilter>();
            yaml.TryGetValue("Any", out var filterlist);
            if (yaml.TryGetValue(baseName, out var filterlist2) && filterlist != null)
            {
                if (filterlist2 != null)
                {
                    fullFilterList = filterlist.Concat(filterlist2).ToList();
                } else
                {
                    //no rules were specified for baseName item, so add null rules
                    filterlist2 = new List<ItemFilter>();
                    var nullRule = new ItemFilter();
                    nullRule.Quality = null;
                    nullRule.Ethereal = null;
                    nullRule.Sockets = null;
                    filterlist2.Add(nullRule);
                    fullFilterList = filterlist.Concat(filterlist2).ToList();
                }
            }
            else
            {
                if (filterlist != null)
                {
                    fullFilterList = filterlist;
                }
            }
            if (fullFilterList.Count == 0 && filterlist2 != null)
            {
                fullFilterList = filterlist2;
            }
            //scan the list of rules
            foreach (var item in fullFilterList)
            {
                if (item.Quality == null)
                {
                    //if a quality is not specified in this rule, any quality is ok
                    qualityReqMet = true;
                }
                else
                {
                    //if a quality is specified, try splitting by / to see if multiple qualities are specified
                    var qualities = item.Quality.Split('/');
                    if (qualities.Length > 0)
                    {
                        foreach (var quality in qualities)
                        {
                            if (quality == itemQuality.ToLower())
                            {
                                qualityReqMet = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        qualityReqMet = item.Quality == itemQuality.ToLower();
                    }
                }
                var socketReqMet = false;
                if (item.Sockets != null)
                {
                    //also split by / to see if multiple sockets are specified
                    var socketsAllowed = item.Sockets.Split('/');
                    foreach (var socketNum in socketsAllowed)
                    {
                        if (int.Parse(socketNum) == numSockets)
                        {
                            socketReqMet = true;
                            break;
                        }
                    }
                }
                var otherReqsMet = (item.Ethereal == null || item.Ethereal == isEth) && (item.Sockets == null || socketReqMet);
                if (qualityReqMet && otherReqsMet) { return true; }
            }
            if (fullFilterList.Count == 0 && yaml.TryGetValue(baseName, out var _))
            {
                //there are no rules
                return true;
            }
            return false;
        }
    }
}
