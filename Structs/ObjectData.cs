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
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ObjectData
    {
        public IntPtr pObjectTxt;
        public byte InteractType;
        public byte PortalFlags;
        private short unk1;
        public IntPtr pShrineTxt;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        private byte[] unk2;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        public byte[] Owner;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ObjectTxt
    {
        public ushort Id;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x40)]
        public string ObjectName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x40)]
        public string ObjectType;
    }
}
