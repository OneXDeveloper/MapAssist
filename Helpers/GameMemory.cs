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
using System.Media;
using System.Text;
using MapAssist.Settings;
using MapAssist.Types;

namespace MapAssist.Helpers
{
    public static class GameMemory
    {
        private static uint _lastMapSeed;
        private static int _currentProcessId;
        public static GameData GetGameData()
        {
            try
            {
                using (var processContext = GameManager.GetProcessContext())
                {
                    _currentProcessId = processContext.ProcessId;
                    var playerUnit = GameManager.PlayerUnit;
                    playerUnit.Update();

                    var mapSeed = playerUnit.Act.MapSeed;

                    if (mapSeed <= 0 || mapSeed > 0xFFFFFFFF)
                    {
                        throw new Exception("Map seed is out of bounds.");
                    }
                    if (mapSeed != _lastMapSeed)
                    {
                        _lastMapSeed = mapSeed;
                        if(!Items.ItemUnitHashesSeen.TryGetValue(_currentProcessId, out var _))
                        {
                            Items.ItemUnitHashesSeen.Add(_currentProcessId, new HashSet<string>());
                            Items.ItemUnitIdsSeen.Add(_currentProcessId, new HashSet<uint>());
                            Items.ItemLog.Add(_currentProcessId, new List<UnitAny>());
                        } else
                        {
                            Items.ItemUnitHashesSeen[_currentProcessId].Clear();
                            Items.ItemUnitIdsSeen[_currentProcessId].Clear();
                            Items.ItemLog[_currentProcessId].Clear();
                        }
                    }

                    var gameIP = Encoding.ASCII.GetString(processContext.Read<byte>(GameManager.GameIPOffset, 15)).TrimEnd((char)0);

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
                    var itemList = new List<UnitAny>();
                    GetUnits(rooms, ref monsterList, ref itemList);
                    Items.CurrentItemLog = Items.ItemLog[_currentProcessId];

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
                        Items = itemList,
                        GameIP = gameIP,
                        PlayerUnit = playerUnit
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
        private static void GetUnits(HashSet<Room> rooms, ref List<UnitAny> monsterList, ref List<UnitAny> itemList)
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
                        case UnitType.Item:
                            if (!itemList.Contains(unitAny) && unitAny.IsDropped())
                            {
                                itemList.Add(unitAny);
                                if ((!Items.ItemUnitHashesSeen[_currentProcessId].Contains(unitAny.ItemHash()) && !Items.ItemUnitIdsSeen[_currentProcessId].Contains(unitAny.UnitId)) && LootFilter.Filter(unitAny))
                                {
                                    if (MapAssistConfiguration.Loaded.ItemLog.PlaySoundOnDrop)
                                    {
                                        var player = new SoundPlayer(Properties.Resources.ching);
                                        player.Play();
                                    }
                                    Items.ItemUnitHashesSeen[_currentProcessId].Add(unitAny.ItemHash());
                                    Items.ItemUnitIdsSeen[_currentProcessId].Add(unitAny.UnitId);
                                    if (Items.ItemLog.Count == MapAssistConfiguration.Loaded.ItemLog.MaxSize)
                                    {
                                        Items.ItemLog[_currentProcessId].RemoveAt(0);
                                        Items.ItemLog[_currentProcessId].Add(unitAny);
                                    }
                                    else
                                    {
                                        Items.ItemLog[_currentProcessId].Add(unitAny);
                                    }
                                }
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
