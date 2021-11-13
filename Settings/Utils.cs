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
using System.Configuration;
using System.Drawing;
using System.Linq;
using MapAssist.Types;

namespace MapAssist.Settings
{
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
            var valueString = ConfigurationManager.AppSettings[key];
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

            var ints = value.Split(',').Select(o => int.Parse(o.Trim())).ToArray();
            switch (ints.Length)
            {
                case 4:
                    return Color.FromArgb(ints[0], ints[1], ints[2], ints[3]);
                case 3:
                    return Color.FromArgb(ints[0], ints[1], ints[2]);
            }

            return Color.FromName(value);
        }

        public static PointOfInterestRendering GetRenderingSettingsForPrefix(string name)
        {
            return new PointOfInterestRendering
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
