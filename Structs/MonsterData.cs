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
using System.Runtime.InteropServices;

namespace MapAssist.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct MonsterData
    {
        [FieldOffset(0x0)] public IntPtr pMonStats;
        [FieldOffset(0x8)] public ulong ShrineType;
        [FieldOffset(0x1A)] public MonsterTypeFlags MonsterType;
        [FieldOffset(0x2A)] public ushort BossLineID;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MonStats
    {
        private ushort Id;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Name;
    }

    [Flags]
    public enum MonsterTypeFlags : byte
    {
        None = 0,
        Other = 1,
        SuperUnique = 1 << 1,
        Champion = 1 << 2,
        Unique = 1 << 3,
        Minion = 1 << 4,
        Possessed = 1 << 5,
        Ghostly = 1 << 6,
        Multishot = 1 << 7,
    }
}
