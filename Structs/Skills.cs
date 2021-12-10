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
    public struct SkillList
    {
        [FieldOffset(0x0)] public IntPtr pFirstSkill; //pointer to a Skill
        [FieldOffset(0x08)] public IntPtr pLeftSkill;
        [FieldOffset(0x10)] public IntPtr pRightSkill;
        [FieldOffset(0x18)] public IntPtr pUsedSkill;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct SkillStrc
    {
        [FieldOffset(0x0)] public IntPtr SkillTxt;
        [FieldOffset(0x08)] public IntPtr pNextSkill;
        [FieldOffset(0x34)] public uint HardPoints;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct SkillTxt
    {
        [FieldOffset(0x0)] public ushort Id;
    }
}
