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
using System.Collections.Generic;
using System.Drawing;
using MapAssist.Structs;

namespace MapAssist.Types
{
    public class GameData
    {
        public Point PlayerPosition;
        public uint MapSeed;
        public Difficulty Difficulty;
        public Area Area;
        public IntPtr MainWindowHandle;
        public string PlayerName;
        public List<UnitAny> Monsters;
        public List<UnitAny> Items;
        public string GameIP;
        public UnitAny PlayerUnit;
        public byte MenuPanelOpen;
        public MenuData MenuOpen;

        public bool HasGameChanged(GameData other)
        {
            if (other == null) return true;
            if (MapSeed != other.MapSeed) return true;
            if (Difficulty != other.Difficulty) return true;
            if (PlayerName != other.PlayerName) return true;
            return false;
        }

        public bool HasMapChanged(GameData other)
        {
            return HasGameChanged(other) || Area != other.Area;
        }

        public override string ToString()
        {
            return
                $"{nameof(PlayerPosition)}: {PlayerPosition}, {nameof(MapSeed)}: {MapSeed}, {nameof(Difficulty)}: {Difficulty}, {nameof(Area)}: {Area}, {nameof(MenuOpen.Map)}: {MenuOpen.Map}";
        }
    }
}
