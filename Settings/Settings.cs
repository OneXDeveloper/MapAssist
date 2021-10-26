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
using System.Configuration;
using System.Drawing;
using MapAssist.Types;

// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace MapAssist.Settings
{
    public static class Rendering
    {
        public static PointOfInterestRendering
            NextArea = Utils.GetRenderingSettingsForPrefix("NextArea");

        public static PointOfInterestRendering PreviousArea =
            Utils.GetRenderingSettingsForPrefix("PreviousArea");

        public static PointOfInterestRendering Waypoint = Utils.GetRenderingSettingsForPrefix("Waypoint");
        public static PointOfInterestRendering Quest = Utils.GetRenderingSettingsForPrefix("Quest");
        public static PointOfInterestRendering Player = Utils.GetRenderingSettingsForPrefix("Player");

        public static PointOfInterestRendering SuperChest =
            Utils.GetRenderingSettingsForPrefix("SuperChest");
    }

    public static class Map
    {
        public static readonly Dictionary<int, Color?> MapColors = new Dictionary<int, Color?>();

        public static void InitMapColors()
        {
            for (var i = -1; i < 600; i++)
            {
                LookupMapColor(i);
            }
        }

        public static Color? LookupMapColor(int type)
        {
            string key = "MapColor[" + type + "]";

            if (!MapColors.ContainsKey(type))
            {
                string mapColorString = ConfigurationManager.AppSettings[key];
                if (!String.IsNullOrEmpty(mapColorString))
                {
                    MapColors[type] = Utils.ParseColor(mapColorString);
                }
                else
                {
                    MapColors[type] = null;
                }
            }

            return MapColors[type];
        }

        public static double Opacity = Convert.ToDouble(ConfigurationManager.AppSettings["Opacity"],
            System.Globalization.CultureInfo.InvariantCulture);
            
        public static bool OverlayMode = Convert.ToBoolean(ConfigurationManager.AppSettings["OverlayMode"]);

        public static bool AlwaysOnTop = Convert.ToBoolean(ConfigurationManager.AppSettings["AlwaysOnTop"]);

        public static bool ToggleViaInGameMap =
            Convert.ToBoolean(ConfigurationManager.AppSettings["ToggleViaInGameMap"]);

        public static int Size = Convert.ToInt16(ConfigurationManager.AppSettings["Size"]);

        public static MapPosition Position =
            (MapPosition)Enum.Parse(typeof(MapPosition), ConfigurationManager.AppSettings["MapPosition"], true);

        public static int UpdateTime = Convert.ToInt16(ConfigurationManager.AppSettings["UpdateTime"]);
        public static bool Rotate = Convert.ToBoolean(ConfigurationManager.AppSettings["Rotate"]);
        public static char ToggleKey = Convert.ToChar(ConfigurationManager.AppSettings["ToggleKey"]);

        public static Area[] PrefetchAreas =
            Utils.ParseCommaSeparatedAreasByName(ConfigurationManager.AppSettings["PrefetchAreas"]);

        public static Area[] HiddenAreas =
            Utils.ParseCommaSeparatedAreasByName(ConfigurationManager.AppSettings["HiddenAreas"]);

        public static bool ClearPrefetchedOnAreaChange =
            Convert.ToBoolean(ConfigurationManager.AppSettings["ClearPrefetchedOnAreaChange"]);
    }

    public static class Api
    {
        public static string Endpoint = ConfigurationManager.AppSettings["ApiEndpoint"];
    }
}
