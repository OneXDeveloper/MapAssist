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
    public struct MapSeed
    {
        [FieldOffset(0x110)] public uint check; // User feedback that this check worked 100% of the time from the people that tried it out
        //[FieldOffset(0x124)] public uint check; // User feedback that this check worked 100% of the time from the people that tried it out
        //[FieldOffset(0x830)] public uint check; // User feedback that this check worked most of the time from the people that tried it out

        [FieldOffset(0x840)] public uint mapSeed1;
        [FieldOffset(0x10C0)] public uint mapSeed2;
    }
}
