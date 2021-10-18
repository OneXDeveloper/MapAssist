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

using System.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;

// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace D2RAssist.Types
{
    public enum MapPosition
    {
        TopLeft,
        TopRight
    }

    public enum Shape
    {
        None,
        Rectangle,
        Ellipse
    }

    public static class Settings
    {
        public static class Rendering
        {
            public static PointOfInterestRenderingSettings
                NextArea = Utils.GetRenderingSettingsForPrefix("NextArea");

            public static PointOfInterestRenderingSettings PreviousArea =
                Utils.GetRenderingSettingsForPrefix("PreviousArea");

            public static PointOfInterestRenderingSettings Waypoint = Utils.GetRenderingSettingsForPrefix("Waypoint");
            public static PointOfInterestRenderingSettings Quest = Utils.GetRenderingSettingsForPrefix("Quest");
            public static PointOfInterestRenderingSettings Player = Utils.GetRenderingSettingsForPrefix("Player");

            public static PointOfInterestRenderingSettings SuperChest =
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

            public static bool AlwaysOnTop = Convert.ToBoolean(ConfigurationManager.AppSettings["AlwaysOnTop"]);
            public static bool HideInTown = Convert.ToBoolean(ConfigurationManager.AppSettings["HideInTown"]);

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

            public static bool ClearPrefetchedOnAreaChange =
                Convert.ToBoolean(ConfigurationManager.AppSettings["ClearPrefetchedOnAreaChange"]);
        }

        public static class Api
        {
            public static string Endpoint = ConfigurationManager.AppSettings["ApiEndpoint"];
        }
    }


    public class PointOfInterestRenderingSettings
    {
        public Color IconColor;
        public Shape IconShape;
        public int IconSize;

        public Color LineColor;
        public float LineThickness;

        public int ArrowHeadSize;

        public Color LabelColor;
        public string LabelFont;
        public int LabelFontSize;

        public bool CanDrawIcon()
        {
            return IconShape != Shape.None && IconSize > 0 && IconColor != Color.Transparent;
        }

        public bool CanDrawLine()
        {
            return LineColor != Color.Transparent && LineThickness > 0;
        }

        public bool CanDrawArrowHead()
        {
            return CanDrawLine() && ArrowHeadSize > 0;
        }

        public bool CanDrawLabel()
        {
            return LabelColor != Color.Transparent && !string.IsNullOrWhiteSpace(LabelFont) &&
                   LabelFontSize > 0;
        }
    }

    public static class Utils
    {
        public static Area[] ParseCommaSeparatedAreasByName(string areas)
        {
            return areas
                .Split(',')
                .Select(o => LookupAreaByName(o.Trim()))
                .Where(o => o != Area.None)
                .ToArray();
        }

        private static Area LookupAreaByName(string name)
        {
            return Enum.GetValues(typeof(Area)).Cast<Area>().FirstOrDefault(area => area.Name() == name);
        }

        private static T GetConfigValue<T>(string key, Func<string, T> converter, T fallback = default)
        {
            string valueString = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(valueString) ? fallback : converter.Invoke(valueString);
        }

        public static Color ParseColor(string value)
        {
            if (value.StartsWith("#"))
            {
                return ColorTranslator.FromHtml(value);
            }

            if (!value.Contains(","))
            {
                return Color.FromName(value);
            }

            int[] ints = value.Split(',').Select(o => int.Parse(o.Trim())).ToArray();
            switch (ints.Length)
            {
                case 4:
                    return Color.FromArgb(ints[0], ints[1], ints[2], ints[3]);
                case 3:
                    return Color.FromArgb(ints[0], ints[1], ints[2]);
            }

            return Color.FromName(value);
        }

        public static PointOfInterestRenderingSettings GetRenderingSettingsForPrefix(string name)
        {
            return new PointOfInterestRenderingSettings
            {
                IconColor = GetConfigValue($"{name}.IconColor", ParseColor, Color.Transparent),
                IconShape = GetConfigValue($"{name}.IconShape", t => (Shape)Enum.Parse(typeof(Shape), t, true)),
                IconSize = GetConfigValue($"{name}.IconSize", Convert.ToInt32),
                LineColor = GetConfigValue($"{name}.LineColor", ParseColor, Color.Transparent),
                LineThickness = GetConfigValue($"{name}.LineThickness", Convert.ToSingle, 1),
                ArrowHeadSize = GetConfigValue($"{name}.ArrowHeadSize", Convert.ToInt32),
                LabelColor = GetConfigValue($"{name}.LabelColor", ParseColor, Color.Transparent),
                LabelFont = GetConfigValue($"{name}.LabelFont", t => t, "Arial"),
                LabelFontSize = GetConfigValue($"{name}.LabelFontSize", Convert.ToInt32, 8),
            };
        }
    }
}
