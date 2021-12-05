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
using System.Text;
using MapAssist.Types;

namespace MapAssist.Helpers
{
    public static class GameMemory
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private static Dictionary<int, uint> _lastMapSeed = new Dictionary<int, uint>();
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

                    if (!playerUnit.IsValidUnit())
                    {
                        throw new Exception("Player unit not found");
                    }

                    var mapSeed = playerUnit.Act.MapSeed;

                    if (mapSeed <= 0 || mapSeed > 0xFFFFFFFF)
                    {
                        throw new Exception("Map seed is out of bounds.");
                    }
                    if (!_lastMapSeed.TryGetValue(_currentProcessId, out var _))
                    {
                        _lastMapSeed.Add(_currentProcessId, 0);
                    }
                    if (mapSeed != _lastMapSeed[_currentProcessId])
                    {
                        _lastMapSeed[_currentProcessId] = mapSeed;
                        if (!Items.ItemUnitHashesSeen.TryGetValue(_currentProcessId, out var _))
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

                    var gameIPLength = processContext.Read<byte>(GameManager.GameIPOffset - 16);
                    var gameIP = Encoding.ASCII.GetString(processContext.Read<byte>(GameManager.GameIPOffset, gameIPLength));

                    var menuOpen = processContext.Read<byte>(GameManager.MenuOpenOffset);
                    var menuData = processContext.Read<Structs.MenuData>(GameManager.MenuDataOffset);

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

                    var monsterList = new List<UnitAny>();
                    var itemList = new List<UnitAny>();
                    GetUnits(ref monsterList, ref itemList);
                    Items.CurrentItemLog = Items.ItemLog[_currentProcessId];

                    return new GameData
                    {
                        PlayerPosition = playerUnit.Position,
                        MapSeed = mapSeed,
                        Area = levelId,
                        Difficulty = gameDifficulty,
                        MainWindowHandle = GameManager.MainWindowHandle,
                        PlayerName = playerUnit.Name,
                        Monsters = monsterList,
                        Items = itemList,
                        GameIP = gameIP,
                        PlayerUnit = playerUnit,
                        MenuOpen = menuData,
                        MenuPanelOpen = menuOpen
                    };
                }
            }
            catch (Exception exception)
            {
                if (exception.Message == "Game process not found.")
                {
                    _log.Debug(exception);
                }
                else
                {
                    _log.Error(exception);
                }
                GameManager.ResetPlayerUnit();
                return null;
            }
        }
        private static void GetUnits(ref List<UnitAny> monsterList, ref List<UnitAny> itemList)
        {
            for (var i = 0; i <= 5; i++)
            {
                var unitHashTable = GameManager.UnitHashTable(128 * 8 * i);
                var unitType = (UnitType)i;
                foreach (var pUnitAny in unitHashTable.UnitTable)
                {
                    var unitAny = new UnitAny(pUnitAny);
                    while (unitAny.IsValidUnit())
                    {
                        switch (unitType)
                        {
                            case UnitType.Monster:
                                if (!monsterList.Contains(unitAny) && unitAny.IsMonster())
                                {
                                    monsterList.Add(unitAny);
                                }
                                break;
                            case UnitType.Item:
                                if (!itemList.Contains(unitAny))
                                {
                                    itemList.Add(unitAny);
                                }
                                break;
                        }
                        unitAny = unitAny.ListNext;
                    }
                }
            }
        }
        private static void GetUnits(HashSet<Room> rooms, ref List<UnitAny> monsterList, ref List<UnitAny> itemList)
        {
            foreach (var room in rooms)
            {
                var unitAny = room.UnitFirst;
                while (unitAny.IsValidUnit())
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
                            if (!itemList.Contains(unitAny))
                            {
                                itemList.Add(unitAny);
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
