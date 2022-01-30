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
    public struct Path
    {
        [FieldOffset(0x00)] public ushort XOffset;
        [FieldOffset(0x02)] public ushort DynamicX;
        [FieldOffset(0x04)] public ushort YOffset;
        [FieldOffset(0x06)] public ushort DynamicY;
        [FieldOffset(0x10)] public ushort StaticX;
        [FieldOffset(0x14)] public ushort StaticY;
        [FieldOffset(0x20)] public IntPtr pRoom;
    }
}
