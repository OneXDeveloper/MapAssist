/**
 *   Copyright (C) 2021 okaygo, OneXDeveloper
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
using MapAssist.Types;

namespace MapAssist.Helpers
{
    public static class Extensions
    {
        public static bool IsWaypoint(this GameObject obj) => obj.ToString().Contains("Waypoint");

        public static Point OffsetFrom(this Point point, Point offset)
        {
            return new Point(point.X - offset.X, point.Y - offset.Y);
        }
    }
}
