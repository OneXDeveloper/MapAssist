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
using System.Collections.Generic;

namespace MapAssist.Types
{
    public enum ShrineType : byte
    {
        None,
        Refill,
        Health,
        Mana,
        HPXChange,
        ManaXChange,
        Armor,
        Combat,
        ResistFire,
        ResistCold,
        ResistLight,
        ResistPoison,
        Skill,
        ManaRegen,
        Stamina,
        Experience,
        Shrine,
        Portal,
        Gem,
        Fire,
        Monster,
        Explosive,
        Poison
    };

    public static class Shrine
    {
        public static Dictionary<string, LocalizedObj> LocalizedShrines = new Dictionary<string, LocalizedObj>();

        public static string ShrineDisplayName(UnitObject obj)
        {
            if (obj.IsWell)
            {
                return WellDisplayName();
            }

            var shrineId = obj.ObjectData.InteractType;
            var itemCode = $"ShrId{shrineId}";

            LocalizedObj localItem;
            if (!LocalizedShrines.TryGetValue(itemCode, out localItem))
            {
                return "Shrine";
            }

            var lang = MapAssistConfiguration.Loaded.LanguageCode;
            var prop = localItem.GetType().GetProperty(lang.ToString()).GetValue(localItem, null);
            var label = prop.ToString();

            if (lang == Locale.enUS)
            {
                var trim = " Shrine";
                if (label.Contains(trim))
                {
                    return label.Replace(trim, "");
                }
            }

            return label;
        }

        public static string WellDisplayName()
        {
            var itemCode = "Well";
            LocalizedObj localItem;
            if (!LocalizedShrines.TryGetValue(itemCode, out localItem))
            {
                return itemCode;
            }

            var lang = MapAssistConfiguration.Loaded.LanguageCode;
            var prop = localItem.GetType().GetProperty(lang.ToString()).GetValue(localItem, null);
            return prop.ToString();
        }
    }
}
