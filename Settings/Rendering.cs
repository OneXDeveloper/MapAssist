using MapAssist.Types;
using System;
using System.Drawing;
using YamlDotNet.Serialization;

namespace MapAssist.Settings
{
    public class IconRendering : ICloneable
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

        [YamlMember(Alias = "IconOpacity", ApplyNamingConventions = false)]
        public float IconOpacity { get; set; } = 1;

        public bool CanDrawIcon()
        {
            return IconSize > 0 && IconOpacity > 0 && ((IconColor != Color.Transparent && IconColor != Color.Empty) || (IconOutlineColor != Color.Transparent && IconOutlineColor != Color.Empty));
        }

        public object Clone()
        {
            return (IconRendering)MemberwiseClone();
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
            return LineColor.A > 0 && LineThickness > 0;
        }

        public bool CanDrawArrowHead()
        {
            return CanDrawLine() && ArrowHeadSize > 0;
        }

        public bool CanDrawLabel()
        {
            return LabelColor.A > 0 && !string.IsNullOrWhiteSpace(LabelFont) && LabelFontSize > 0;
        }

        public void ToggleLine()
        {
            if (LineThickness != 0)
            {
                LineThickness = 0;
                return;
            }

            LineThickness = 2;

            if (ArrowHeadSize == 0)
            {
                ArrowHeadSize = 15;
            }

            if (LineColor.A == 0)
            {
                LineColor = IconColor.A > 0
                    ? IconColor
                    : IconOutlineColor.A > 0
                        ? IconOutlineColor
                        : Color.Red;
            }
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
    public enum Shape
    {
        Square,
        Ellipse,
        Polygon,
        Cross,
        Portal,
        Dress,
        Kite,
        Stick,
        Leg,
        Pentagram
    }

    public enum TextAlign
    {
        Left,
        Center,
        Right,
    }
}
