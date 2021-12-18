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
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x10)]
        [FieldOffset(0x0)] public string Name;
        [FieldOffset(0x18)] public uint UnitId;
        [FieldOffset(0x24)] public PlayerClass PlayerClass;
        [FieldOffset(0x28)] public ushort PlayerLevel;
        [FieldOffset(0x2A)] public ushort PartyID;
        [FieldOffset(0x2C)] public Area Area; //partyid must match your players to update
        [FieldOffset(0x30)] public uint PosX; //partyid must match your players to update
        [FieldOffset(0x34)] public uint PosY; //partyid must match your players to update
        [FieldOffset(0x38)] public uint PartyFlags; //01 = normal, 02 = invited
        [FieldOffset(0x40)] public IntPtr pHostileInfo; //ptr that leads to another ptr that gets first HostileInfo
        [FieldOffset(0x108)] public IntPtr pNext;
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
        AMAZON,
        SORCERESS,
        NECROMANCER,
        PALADIN,
        BARBARIAN,
        DRUID,
        ASSASSIN,
        EVILFORCE,
        INVALID = 7,
        NUMBER_OF_PLAYERCLASSES = 7,
    };
}
