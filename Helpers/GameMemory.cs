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

                    return new GameData
                    {
                        PlayerPosition = playerUnit.Position,
                        MapSeed = mapSeed,
                        Area = levelId,
                        Difficulty = gameDifficulty,
                        MapShown = mapShown,
                        MainWindowHandle = GameManager.MainWindowHandle,
                        PlayerName = playerUnit.Name
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
    }
}
