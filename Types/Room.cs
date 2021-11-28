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
using System.Linq;
using System.Collections.Generic;

namespace MapAssist.Types
{
    public class Room : IUpdatable<Room>
    {
        private readonly IntPtr _pRoom = IntPtr.Zero;
        private Structs.Room _room;

        public Room(IntPtr pRoom, bool update = true)
        {
            _pRoom = pRoom;
            if (update) { Update(); }
        }

        public Room Update()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                _room = processContext.Read<Structs.Room>(_pRoom);
            }

            return this;
        }

        public Room[] RoomsNear
        {
            get
            {
                using (var processContext = GameManager.GetProcessContext())
                {
                    var addrBuf = new byte[8];
                    var uintBuf = new byte[4];
                    WindowsExternal.ReadProcessMemory(processContext.Handle, IntPtr.Add(_pRoom, 0x40), uintBuf,
                        uintBuf.Length, out _);
                    var numRoomsNear = BitConverter.ToUInt32(uintBuf, 0);
                    if (numRoomsNear > 9)
                    {
                        numRoomsNear = 9;
                    }

                    var roomList = new Room[numRoomsNear];
                    for (var p = 0; p < numRoomsNear; p++)
                    {
                        WindowsExternal.ReadProcessMemory(processContext.Handle, IntPtr.Add(_room.pRoomsNear, p * 8),
                            addrBuf, addrBuf.Length, out _);
                        var pRoom = (IntPtr)BitConverter.ToInt64(addrBuf, 0);
                        roomList[p] = new Room(pRoom, false);
                    }

                    return roomList;
                }
            }
        }

        public override bool Equals(object obj) => obj is Room other && Equals(other);
        public bool Equals(Room room) => _pRoom == room._pRoom;
        public override int GetHashCode() => _pRoom.GetHashCode();
        public RoomEx RoomEx => new RoomEx(_room.pRoomEx);
        public uint NumRoomsNear => _room.numRoomsNear;
        public Act Act => new Act(_room.pAct);
        public UnitAny UnitFirst => new UnitAny(_room.pUnitFirst);
        public Room RoomNext => new Room(_room.pRoomNext);
        public Room RoomNextFast => new Room(_room.pRoomNext, false);
    }
}
