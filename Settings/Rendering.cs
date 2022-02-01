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

using MapAssist.Types;
using System.Drawing;
using YamlDotNet.Serialization;

namespace MapAssist.Settings
{
    public class IconRendering
    {
        [YamlMember(Alias = "IconColor", ApplyNamingConventions = false)]
        public Color IconColor { get; set; }

        [YamlMember(Alias = "IconOutlineColor", ApplyNamingConventions = false)]
        public Color IconOutlineColor { get; set; }

        [YamlMember(Alias = "IconShape", ApplyNamingConventions = false)]
        public Shape IconShape { get; set; }

        [YamlMember(Alias = "IconSize", ApplyNamingConventions = false)]
        public float IconSize { get; set; }

        [YamlMember(Alias = "IconThickness", ApplyNamingConventions = false)]
        public float IconThickness { get; set; }

        public bool CanDrawIcon()
        {
            return IconSize > 0 && (IconColor != Color.Transparent || IconOutlineColor != Color.Transparent);
        }
    }

    public class PointOfInterestRendering : IconRendering
    {
        [YamlMember(Alias = "LineColor", ApplyNamingConventions = false)]
        public Color LineColor { get; set; }

        [YamlMember(Alias = "LineThickness", ApplyNamingConventions = false)]
        public float LineThickness { get; set; }

        [YamlMember(Alias = "ArrowHeadSize", ApplyNamingConventions = false)]
        public int ArrowHeadSize { get; set; }

        [YamlMember(Alias = "LabelColor", ApplyNamingConventions = false)]
        public Color LabelColor { get; set; }

        [YamlMember(Alias = "LabelFont", ApplyNamingConventions = false)]
        public string LabelFont { get; set; }

        [YamlMember(Alias = "LabelFontSize", ApplyNamingConventions = false)]
        public double LabelFontSize { get; set; }

        [YamlMember(Alias = "LabelTextShadow", ApplyNamingConventions = false)]
        public bool LabelTextShadow { get; set; }

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

    public class PortalRendering : PointOfInterestRendering
    {
        public bool CanDrawLabel(Area area)
        {
            return CanDrawLabel() && area != Area.Tristram;  // Skip drawing tristram label since we have a Cairn Stone as the quest destination already and can be seen from further away
        }
    }

    public enum MapPosition
    {
        TopLeft,
        TopRight,
        Center
    }

    public enum MapLinesMode
    {
        All,
        PVE,
        PVP,
    }

    public enum BuffPosition
    {
        Top,
        Player,
        Bottom
    }

    public enum GameInfoPosition
    {
        TopLeft,
        TopRight
    }
    public enum ResistancesPosition
    {
        Left,
        Right
    }

    public enum Shape
    {
        Square,
        Ellipse,
        Polygon,
        Cross,
        Portal,
        Dress,
        Kite,
    }

    public enum TextAlign
    {
        Left,
        Center,
        Right,
    }
}
