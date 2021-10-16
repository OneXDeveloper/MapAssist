/**
 *   Copyright (C) 2021 okaygo
 *
 *   https://github.com/misterokaygo/D2RAssist/
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2RAssist.Types
{
    public enum MapPosition
    {
        TopLeft,
        TopRight
    }
    public static class Settings
    {
        public static class Map
        {
            public static class Colors
            {
                public static Dictionary<int, Color?> MapColors = new Dictionary<int, Color?>();

                public static readonly Color DoorNext = ColorTranslator.FromHtml(ConfigurationManager.AppSettings["DoorNext"]);
                public static readonly Color DoorPrevious = ColorTranslator.FromHtml(ConfigurationManager.AppSettings["DoorPrevious"]);
                public static readonly Color Waypoint = ColorTranslator.FromHtml(ConfigurationManager.AppSettings["Waypoint"]);
                public static readonly Color Player = ColorTranslator.FromHtml(ConfigurationManager.AppSettings["Player"]);
                public static readonly Color SuperChest = ColorTranslator.FromHtml(ConfigurationManager.AppSettings["SuperChest"]);
                public static readonly Color ArrowExit = ColorTranslator.FromHtml(ConfigurationManager.AppSettings["ArrowExit"]);
                public static readonly Color ArrowQuest = ColorTranslator.FromHtml(ConfigurationManager.AppSettings["ArrowQuest"]);
                public static readonly Color ArrowWaypoint = ColorTranslator.FromHtml(ConfigurationManager.AppSettings["ArrowWaypoint"]);
                public static readonly Color LabelColor = ColorTranslator.FromHtml(ConfigurationManager.AppSettings["LabelColor"]);

                public static Color? LookupMapColor(int type)
                {
                    string key = "MapColor[" + type + "]";

                    if (!MapColors.ContainsKey(type))
                    {
                        string mapColorString = ConfigurationManager.AppSettings[key];
                        if (!String.IsNullOrEmpty(mapColorString))
                        {
                            MapColors[type] = ColorTranslator.FromHtml(mapColorString);
                        }
                        else
                        {
                            MapColors[type] = null;
                        }
                    }

                    return MapColors[type];
                }
            }

            public static readonly double Opacity = Convert.ToDouble(ConfigurationManager.AppSettings["Opacity"], System.Globalization.CultureInfo.InvariantCulture);
            public static bool AlwaysOnTop = Convert.ToBoolean(ConfigurationManager.AppSettings["AlwaysOnTop"]);
            public static bool HideInTown = Convert.ToBoolean(ConfigurationManager.AppSettings["HideInTown"]);
            public static bool ToggleViaInGameMap = Convert.ToBoolean(ConfigurationManager.AppSettings["ToggleViaInGameMap"]);
            public static int Size = Convert.ToInt16(ConfigurationManager.AppSettings["Size"]);
            public static MapPosition MapPosition = (MapPosition)Convert.ToInt16(ConfigurationManager.AppSettings["MapPosition"]);
            public static int UpdateTime = Convert.ToInt16(ConfigurationManager.AppSettings["UpdateTime"]);
            public static bool Rotate = Convert.ToBoolean(ConfigurationManager.AppSettings["Rotate"]);
            public static string LabelFont = ConfigurationManager.AppSettings["LabelFont"];
            public static int ArrowThickness = Convert.ToInt16(ConfigurationManager.AppSettings["ArrowThickness"]);
            public static bool DrawExitArrow = Convert.ToBoolean(ConfigurationManager.AppSettings["DrawExitArrow"]);
            public static bool DrawQuestArrow = Convert.ToBoolean(ConfigurationManager.AppSettings["DrawQuestArrow"]);
            public static bool DrawWaypointArrow = Convert.ToBoolean(ConfigurationManager.AppSettings["DrawWaypointArrow"]);
        }

        public static class Api
        {
            public static string Endpoint = ConfigurationManager.AppSettings["ApiEndpoint"];
        }
    }
}
