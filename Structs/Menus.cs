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
    [StructLayout(LayoutKind.Explicit), ]
    public struct MenuData
    {
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x09)] public bool Inventory;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x0A)] public bool Character;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x0B)] public bool SkillSelect;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x0C)] public bool SkillTree;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x0D)] public bool Chat;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x10)] public bool NpcInteract;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x11)] public bool EscMenu;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x12)] public bool Map;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x13)] public bool NpcShop;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x16)] public bool QuestLog;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x1B)] public bool Waypoint;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x1D)] public bool Party;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x20)] public bool Stash;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x21)] public bool Cube;
        [MarshalAs(UnmanagedType.U1)]
        [FieldOffset(0x26)] public bool MercenaryInventory;
    }
}
