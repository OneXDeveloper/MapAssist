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
using MapAssist.Helpers;
using MapAssist.Interfaces;

namespace MapAssist.Types
{
    public class Path : IUpdatable<Path>
    {
        private readonly IntPtr _pPath = IntPtr.Zero;
        private Structs.Path _path;

        public Path(IntPtr pPath)
        {
            _pPath = pPath;
            Update();
        }

        public Path Update()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                _path = processContext.Read<Structs.Path>(_pPath);
            }

            return this;
        }

        public float CalcFloatPos(ushort DynamicVal, ushort OffsetVal)
        {
            return DynamicVal + ((float)OffsetVal / 65535);
        }

        public float DynamicX => CalcFloatPos(_path.DynamicX, _path.XOffset);
        public float DynamicY => CalcFloatPos(_path.DynamicY, _path.YOffset);
        public ushort StaticX => _path.StaticX;
        public ushort StaticY => _path.StaticY;
        public Room Room => new Room(_path.pRoom);
    }
}
