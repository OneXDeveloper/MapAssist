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

using MapAssist.Types;
using System;
using System.Runtime.InteropServices;

namespace MapAssist.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct UnitAny
    {
        [FieldOffset(0x00)] public UnitType UnitType;
        [FieldOffset(0x04)] public uint TxtFileNo;
        [FieldOffset(0x08)] public uint UnitId;
        [FieldOffset(0x0C)] public uint Mode;
        [FieldOffset(0x10)] public IntPtr pUnitData;
        [FieldOffset(0x20)] public IntPtr pAct;
        [FieldOffset(0x38)] public IntPtr pPath;
        [FieldOffset(0x88)] public IntPtr pStatsListEx;
        [FieldOffset(0x90)] public IntPtr pInventory;

        //[MarshalAs(UnmanagedType.U1)]
        //[FieldOffset(0xB9)] public bool isAlive1; // ?
        [FieldOffset(0xC4)] public ushort X;

        [FieldOffset(0xC6)] public ushort Y;
        [FieldOffset(0x100)] public IntPtr pSkills;
        [FieldOffset(0x150)] public IntPtr pListNext;
        [FieldOffset(0x158)] public IntPtr pRoomNext;
        [FieldOffset(0x174)] public PlayerClass playerClass;

        //[FieldOffset(0x194)] public ushort unk1;
        //[FieldOffset(0x19D)] public byte isAlive2; //could be = 16 for alive players
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x1A6)] public bool isCorpse;

        //[FieldOffset(0x1B8)] public uint unk2;
        //[FieldOffset(0x1BC)] public uint unk3;
        //[FieldOffset(0x1C0)] public uint unk4;

        public override bool Equals(object obj) => obj is UnitAny other && Equals(other);

        public bool Equals(UnitAny unit) => UnitId == unit.UnitId;

        public override int GetHashCode() => UnitId.GetHashCode();

        public static bool operator ==(UnitAny unit1, UnitAny unit2) => unit1.Equals(unit2);

        public static bool operator !=(UnitAny unit1, UnitAny unit2) => !(unit1 == unit2);
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct StatValue
    {
        [FieldOffset(0x0)] public readonly ushort Layer;
        [FieldOffset(0x2)] public readonly Stats.Stat Stat;
        [FieldOffset(0x4)] public readonly int Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct StatArrayStruct
    {
        [FieldOffset(0x0)] public readonly IntPtr pFirstStat;
        [FieldOffset(0x8)] public readonly ulong Size;
        [FieldOffset(0x10)] public readonly ulong Capacity;
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly struct StatListStruct
    {
        [FieldOffset(0x8)] public readonly uint OwnerType;
        [FieldOffset(0xC)] public readonly uint OwnerId;
        [FieldOffset(0x1C)] public readonly uint Flags;
        [FieldOffset(0x30)] public readonly StatArrayStruct BaseStats;
        [FieldOffset(0x80)] public readonly StatArrayStruct Stats;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        [FieldOffset(0xAC8)] public readonly uint[] StateFlags;
    }
}
