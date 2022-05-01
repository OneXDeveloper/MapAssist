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
    public struct RosterMember
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x40)]
        [FieldOffset(0x0)] public byte[] Name;

        [FieldOffset(0x48)] public uint UnitId;
        [FieldOffset(0x54)] public PlayerClass PlayerClass;
        [FieldOffset(0x58)] public ushort PlayerLevel;
        [FieldOffset(0x5A)] public ushort PartyID;
        [FieldOffset(0x5C)] public Area Area; //partyid must match your players to update
        [FieldOffset(0x60)] public uint PosX; //partyid must match your players to update
        [FieldOffset(0x64)] public uint PosY; //partyid must match your players to update
        [FieldOffset(0x68)] public uint PartyFlags; //01 = normal, 02 = invited
        [FieldOffset(0x70)] public IntPtr pHostileInfo; //ptr that leads to another ptr that gets first HostileInfo
        [FieldOffset(0x148)] public IntPtr pNext;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct HostileInfo
    {
        [FieldOffset(0x0)] public uint UnitId;
        [FieldOffset(0x04)] public uint HostileFlag;
        [FieldOffset(0x08)] public IntPtr NextHostileInfo;
    }

    public enum PlayerClass : uint
    {
        Amazon,
        Sorceress,
        Necromancer,
        Paladin,
        Barbarian,
        Druid,
        Assassin,
        EvilForce,
        Invalid = 7,
        NumberOfPlayerClasses = 7,

        Any = 0xFFFF
    };
}
