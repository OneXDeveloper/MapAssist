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

    }
}
