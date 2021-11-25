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

using System.Drawing;

namespace MapAssist.Settings
{
    public class IconRendering
    {
        public Color IconColor;
        public Shape IconShape;
        public int IconSize;
        public float IconThickness;

        public bool CanDrawIcon()
        {
            return IconShape != Shape.None && IconSize > 0 && IconColor != Color.Transparent;
        }
    }

    public class PointOfInterestRendering : IconRendering
    {
        public Color LineColor;
        public float LineThickness;

        public int ArrowHeadSize;

        public Color LabelColor;
        public string LabelFont;
        public int LabelFontSize;

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

    public class MonsterRendering
    {
        public Color NormalColor;
        public Color EliteColor;
    }
}
