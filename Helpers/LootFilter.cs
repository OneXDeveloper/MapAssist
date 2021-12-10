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

using System.Linq;
using MapAssist.Types;
using MapAssist.Settings;

namespace MapAssist.Helpers
{
    public static class LootFilter
    {
        public static bool Filter(UnitAny unitAny)
        {
            var baseName = Items.ItemName(unitAny.TxtFileNo);
            var itemQuality = unitAny.ItemData.ItemQuality;
            var isEth = (unitAny.ItemData.ItemFlags & ItemFlags.IFLAG_ETHEREAL) == ItemFlags.IFLAG_ETHEREAL;
            var lowQuality = (unitAny.ItemData.ItemFlags & ItemFlags.IFLAG_LOWQUALITY) == ItemFlags.IFLAG_LOWQUALITY;
            var hasSockets = unitAny.Stats.TryGetValue(Stat.STAT_ITEM_NUMSOCKETS, out var numSockets);
            if (!hasSockets)
            {
                numSockets = 0;
            }

            return Filter(baseName, itemQuality, isEth, numSockets, lowQuality);
        }

        private static bool Filter(string baseName, ItemQuality itemQuality, bool isEth, int numSockets,
            bool lowQuality)
        {
            if (lowQuality)
            {
                return false;
            }

            //populate a list of filter rules by combining rules from "Any" and the item base name
            //use only one list or the other depending on if "Any" exists
            var matches =
                LootLogConfiguration.Filters
                    .Where(f => f.Key == "Any" || f.Key == baseName).ToList();

            // Early breakout
            // We know that there is an item in here without any actual filters
            // So we know that simply having the name match means we can return true
            if (matches.Any(kv => kv.Value == null))
            {
                return true;
            }

            //scan the list of rules
            foreach (var item in matches.SelectMany(kv => kv.Value))
            {
                var qualityReqMet = item.Qualities == null || item.Qualities.Length == 0 ||
                                    item.Qualities.Contains(itemQuality);
                var socketReqMet = item.Sockets == null || item.Sockets.Length == 0 ||
                                   item.Sockets.Contains(numSockets);

                var ethReqMet = (item.Ethereal == null || item.Ethereal == isEth);
                if (qualityReqMet && socketReqMet && ethReqMet) { return true; }
            }

            return false;
        }
    }
}
