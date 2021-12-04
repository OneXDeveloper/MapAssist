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

using MapAssist.Helpers;
using MapAssist.Interfaces;
using System;

namespace MapAssist.Types
{
    public class RoomEx : IUpdatable<RoomEx>
    {
        private readonly IntPtr _pRoomEx = IntPtr.Zero;
        private Structs.RoomEx _roomEx;

        public RoomEx(IntPtr pRoomEx)
        {
            _pRoomEx = pRoomEx;
            Update();
        }

        public RoomEx Update()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                _roomEx = processContext.Read<Structs.RoomEx>(_pRoomEx);
            }

            return this;
        }

        public Level Level => new Level(_roomEx.pLevel);
    }
}
