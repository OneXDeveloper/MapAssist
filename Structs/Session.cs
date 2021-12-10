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
    public struct Session
    {
        [FieldOffset(0x0)] public IntPtr SessionID; //SessionID = 33 bytes
        [FieldOffset(0x38)] public byte GameNameLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        [FieldOffset(0x48)] public byte[] GameName;
        [FieldOffset(0x68)] public byte GamePassLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        [FieldOffset(0x78)] public byte[] GamePass;
        [FieldOffset(0x98)] public byte RegionLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        [FieldOffset(0xA8)] public byte[] Region;
        [FieldOffset(0x1C0)] public byte GameIPLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        [FieldOffset(0x1D0)] public byte[] GameIP;
    }
}
