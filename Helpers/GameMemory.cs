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
using System.Collections.Generic;
using System.Diagnostics;
using MapAssist.Types;

namespace MapAssist.Helpers
{
    public static class GameMemory
    {
        public static GameData GetGameData()
        {
            try
            {
                using (var processContext = GameManager.GetProcessContext())
                {
                    var playerUnit = GameManager.PlayerUnit;
                    playerUnit.Update();

                    var mapSeed = playerUnit.Act.MapSeed;

                    if (mapSeed <= 0 || mapSeed > 0xFFFFFFFF)
                    {
                        throw new Exception("Map seed is out of bounds.");
                    }

                    var actId = playerUnit.Act.ActId;

                    var gameDifficulty = playerUnit.Act.ActMisc.GameDifficulty;

                    if (!gameDifficulty.IsValid())
                    {
                        throw new Exception("Game difficulty out of bounds.");
                    }

                    var levelId = playerUnit.Path.Room.RoomEx.Level.LevelId;

                    if (!levelId.IsValid())
                    {
                        throw new Exception("Level id out of bounds.");
                    }

                    var mapShown = GameManager.UiSettings.MapShown;

                    var rooms = new HashSet<Room>() {playerUnit.Path.Room};
                    rooms = GetRooms(playerUnit.Path.Room, ref rooms);
                    foreach (var room in rooms)
                    {
                        room.Update();
                    }

                    var monsterList = new List<UnitAny>();
                    GetUnits(rooms, ref monsterList);

                    return new GameData
                    {
                        PlayerPosition = playerUnit.Position,
                        MapSeed = mapSeed,
                        Area = levelId,
                        Difficulty = gameDifficulty,
                        MapShown = mapShown,
                        MainWindowHandle = GameManager.MainWindowHandle,
                        PlayerName = playerUnit.Name,
                        Monsters = monsterList,
                    };
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                GameManager.ResetPlayerUnit();
                return null;
            }
        }

        private static void GetUnits(HashSet<Room> rooms, ref List<UnitAny> monsterList)
        {
            foreach (var room in rooms)
            {
                var unitAny = room.UnitFirst;
                while (unitAny.IsValid())
                {
                    switch (unitAny.UnitType)
                    {
                        case UnitType.Monster:
                            if (!monsterList.Contains(unitAny) && unitAny.IsMonster())
                            {
                                monsterList.Add(unitAny);
                            }

                            break;
                    }

                    unitAny = unitAny.RoomNext;
                }
            }
        }

        private static HashSet<Room> GetRooms(Room startingRoom, ref HashSet<Room> roomsList)
        {
            var roomsNear = startingRoom.RoomsNear;
            foreach (var roomNear in roomsNear)
            {
                if (!roomsList.Contains(roomNear))
                {
                    roomsList.Add(roomNear);
                    GetRooms(roomNear, ref roomsList);
                }
            }

            if (!roomsList.Contains(startingRoom.RoomNextFast))
            {
                roomsList.Add(startingRoom.RoomNextFast);
                GetRooms(startingRoom.RoomNextFast, ref roomsList);
            }

            return roomsList;
        }
    }
}
