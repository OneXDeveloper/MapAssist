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
    public struct Room
    {
        [FieldOffset(0x00)] public IntPtr pRoomsNear;
        [FieldOffset(0x18)] public IntPtr pRoomEx;
        [FieldOffset(0x40)] public uint numRoomsNear;
        [FieldOffset(0x48)] public IntPtr pAct;
        [FieldOffset(0xA8)] public IntPtr pUnitFirst;
        [FieldOffset(0xB0)] public IntPtr pRoomNext;
    }
}
