using MapAssist.Settings;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapAssist.Constants
{
    class Config
    {
        public const int UpdateTime = 30;
        public const bool ClearPrefetchedOnAreaChange = true;

        public const float ZoomLevel = 1;
        public const bool OverlayMode = true;
        public const int RenderingSize = 450;
        public const int RenderingBuffSize = 1;
        public const bool ToggleViaIngameMap = false;

        public const int IconSize = 6;
        public const Shape IconShape = Shape.SquareOutline;
        public static readonly Color IconColor = Color.Yellow;
        public const float IconThickness = 2;
        public static readonly Color LineColor = Color.White;
        public const float LineThickness = 2;
        public const int ArrowHeadSize = 10;
        public static readonly Color LabelColor = Color.Chartreuse;
        public const int LabelFontSize = 8;

        public const bool GameInfoEnabled = true;
        public const bool GameInfoShowFps = false;

        public const bool ItemLogEnabled = true;
        public const bool PlaySoundOnDrop = true;
        public const double DisplayForSeconds = 45;
        public const int ItemLogLabelFontSize = 14;

    }
}
