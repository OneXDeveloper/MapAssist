﻿/**
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
        public static string GetAreaLabel(Area area, Difficulty difficulty, bool prefix = false)
        {
            var label = area.Name();
            var level = area.Level(difficulty);
            if (level > 0)
            {
                label += " (";
                if (prefix)
                {
                    label += "Level: ";
                }
                label += level + ")";
            }
            return label;
        }
    }
}
