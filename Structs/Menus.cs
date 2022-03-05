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
    public struct MenuData
    {
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x0)] public bool InGame;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x01)] public bool Inventory;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x02)] public bool Character;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x03)] public bool SkillSelect;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x04)] public bool SkillTree;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x05)] public bool Chat;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x08)] public bool NpcInteract;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x09)] public bool EscMenu;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x0A)] public bool Map;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x0B)] public bool NpcShop;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x0C)] public bool GroundItems;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x0D)] public bool Anvil; // Imbue and sockets
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x0E)] public bool QuestLog;
        //missing 4
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x13)] public bool Waypoint;
        //missing 1
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x15)] public bool Party;
        //missing 2
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x18)] public bool Stash;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x19)] public bool Cube;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x1A)] public bool PotionBelt;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x1B)] public bool Help;
        //missing 1
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x1D)] public bool Portraits;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x1E)] public bool MercenaryInventory;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct HoverData
    {
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x00)] public bool IsHovered;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x01)] public bool IsItemTooltip;
        [FieldOffset(0x04)] public UnitType UnitType;
        [FieldOffset(0x08)] public uint UnitId;
    }
}
