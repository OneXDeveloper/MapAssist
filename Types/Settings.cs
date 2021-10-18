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
                public static readonly Color DoorNext = Color.FromArgb(237, 107, 0);
                public static readonly Color DoorPrevious = Color.FromArgb(255, 0, 149);
                public static readonly Color Waypoint = Color.FromArgb(16, 140, 235);
                public static readonly Color Player = Color.FromArgb(255, 255, 0);
                public static readonly Color Chest = Color.FromArgb(17, 255, 0);
                public static readonly Color SuperChest = Color.FromArgb(255, 128, 0);
                public static readonly Color ArrowExit = Color.FromArgb(0,72,186);
                public static readonly Color ArrowQuest = Color.FromArgb(255, 255, 255);
                public static readonly Color ArrowWaypoint = Color.FromArgb(0, 204, 153);
                public static readonly Color LabelColor = Color.FromArgb(255, 246, 0);
            }

            public static readonly double Opacity = Convert.ToDouble(ConfigurationManager.AppSettings["Opacity"], System.Globalization.CultureInfo.InvariantCulture);
            public static bool AlwaysOnTop = true;
            public static bool HideInTown = Convert.ToBoolean(ConfigurationManager.AppSettings["HideInTown"]);
            public static bool ToggleViaInGameMap = Convert.ToBoolean(ConfigurationManager.AppSettings["ToggleViaInGameMap"]);
            public static int Size = 450;
            public static MapPosition Position = MapPosition.TopRight;
            public static int UpdateTime = Convert.ToInt16(ConfigurationManager.AppSettings["UpdateTime"]);
            public static bool Rotate = true;
            public static string LabelFont = "BD Megalona";
        }

        public static class Api
        {
            public static string Endpoint = ConfigurationManager.AppSettings["ApiEndpoint"];
        }
    }
}
