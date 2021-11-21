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
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Drawing;
using ColorConverter = MapAssist.Helpers.ColorConverter;

namespace MapAssist.Settings
{
    public class IconRendering
    {
        [JsonProperty("IconColor")]
        [JsonConverter(typeof(ColorConverter))]
        public Color IconColor { get; set; }

        [JsonConverter(typeof(StringEnumConverter<Shape>))]
        public Shape IconShape { get; set; }

        [JsonProperty("IconSize")]
        public int IconSize { get; set; }

        [JsonProperty("IconThickness")]
        public float IconThickness;

        public bool CanDrawIcon()
        {
            return IconShape != Shape.None && IconSize > 0 && IconColor != Color.Transparent;
        }
    }

    public class PointOfInterestRendering : IconRendering
    {
        [JsonProperty("LineColor")]
        [JsonConverter(typeof(ColorConverter))]
        public Color LineColor { get; set; }
        
        [JsonProperty("LineThickness")]
        public float LineThickness { get; set; }

        [JsonProperty("ArrowHeadSize")]
        public int ArrowHeadSize { get; set; }

        [JsonProperty("LabelColor")]
        [JsonConverter(typeof(ColorConverter))]
        public Color LabelColor { get; set; }
        
        [JsonProperty("LabelFont")]
        public string LabelFont { get; set; }
        
        [JsonProperty("LabelFontSize")]
        public int LabelFontSize { get; set; }

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

}
