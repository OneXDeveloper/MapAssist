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
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D2RAssist.Types
{
    public static class Globals
    {
        public static MapApiSession MapApiSession { get; set; }
        public static MapData MapData { get; set; }
        public static GameData CurrentGameData { get; set; }
        public static GameData LastGameData { get; set; }
        public static Bitmap MapBackground { get; set; }
        public static Dictionary<Game.Area, MapData> TombData { get; set; }
    }
}
