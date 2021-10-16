/**
 *   Copyright (C) 2021 okaygo
 *
 *   https://github.com/misterokaygo/D2RAssist/
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
using D2RAssist.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static D2RAssist.Types.Game;

namespace D2RAssist.Helpers
{
    class GameMemory
    {
        public static GameData GetGameData()
        {
            // Clean up and organize, add better exception handeling.
            try
            {
                Process gameProcess = Process.GetProcessesByName("D2R")[0];
                IntPtr processHandle = WindowsExternal.OpenProcess((uint)WindowsExternal.ProcessAccessFlags.VirtualMemoryRead, false, gameProcess.Id);
                IntPtr processAddress = gameProcess.MainModule.BaseAddress;
                IntPtr pPlayerUnit = IntPtr.Add(processAddress, Offsets.PlayerUnit);

                byte[] addressBuffer = new byte[8];
                byte[] dwordBuffer = new byte[4];
                byte[] shortBuffer = new byte[2];
                byte[] byteBuffer = new byte[1];
                WindowsExternal.ReadProcessMemory(processHandle, pPlayerUnit, addressBuffer, addressBuffer.Length, out IntPtr bytesRead);

                IntPtr playerUnit = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);
                IntPtr pPlayer = IntPtr.Add(playerUnit, 0x10);
                IntPtr pAct = IntPtr.Add(playerUnit, 0x20);

                WindowsExternal.ReadProcessMemory(processHandle, pPlayer, addressBuffer, addressBuffer.Length, out bytesRead);
                IntPtr player = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                byte[] playerNameBuffer = new byte[16];
                WindowsExternal.ReadProcessMemory(processHandle, player, playerNameBuffer, playerNameBuffer.Length, out bytesRead);
                string playerName = Encoding.ASCII.GetString(playerNameBuffer);
 
                WindowsExternal.ReadProcessMemory(processHandle, pAct, addressBuffer, addressBuffer.Length, out bytesRead);
                IntPtr aAct = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                IntPtr pActUnk1 = IntPtr.Add(aAct, 0x70);

                WindowsExternal.ReadProcessMemory(processHandle, pActUnk1, addressBuffer, addressBuffer.Length, out bytesRead);
                IntPtr aActUnk1 = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                IntPtr pGameDifficulty = IntPtr.Add(aActUnk1, 0x830);

                WindowsExternal.ReadProcessMemory(processHandle, pGameDifficulty, byteBuffer, byteBuffer.Length, out bytesRead);
                UInt16 aGameDifficulty = byteBuffer[0];


                IntPtr aDwAct = IntPtr.Add(aAct, 0x20);
                WindowsExternal.ReadProcessMemory(processHandle, aDwAct, dwordBuffer, dwordBuffer.Length, out bytesRead);

                uint dwAct = BitConverter.ToUInt32(dwordBuffer, 0);

                IntPtr aMapSeed = IntPtr.Add(aAct, 0x14);

                IntPtr pPath = IntPtr.Add(playerUnit, 0x38);

                WindowsExternal.ReadProcessMemory(processHandle, pPath, addressBuffer, addressBuffer.Length, out bytesRead);
                IntPtr path = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                IntPtr pRoom1 = IntPtr.Add(path, 0x20);

                WindowsExternal.ReadProcessMemory(processHandle, pRoom1, addressBuffer, addressBuffer.Length, out bytesRead);
                IntPtr aRoom1 = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                IntPtr pRoom2 = IntPtr.Add(aRoom1, 0x18);
                WindowsExternal.ReadProcessMemory(processHandle, pRoom2, addressBuffer, addressBuffer.Length, out bytesRead);
                IntPtr aRoom2 = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                IntPtr pLevel = IntPtr.Add(aRoom2, 0x90);
                WindowsExternal.ReadProcessMemory(processHandle, pLevel, addressBuffer, addressBuffer.Length, out bytesRead);
                IntPtr aLevel = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                IntPtr aLevelId = IntPtr.Add(aLevel, 0x1F8);
                WindowsExternal.ReadProcessMemory(processHandle, aLevelId, dwordBuffer, dwordBuffer.Length, out bytesRead);
                uint dwLevelId = BitConverter.ToUInt32(dwordBuffer, 0);

                IntPtr posXAddress = IntPtr.Add(path, 0x02);
                IntPtr posYAddress = IntPtr.Add(path, 0x06);

                WindowsExternal.ReadProcessMemory(processHandle, aMapSeed, dwordBuffer, dwordBuffer.Length, out bytesRead);
                UInt32 mapSeed = BitConverter.ToUInt32(dwordBuffer, 0);

                WindowsExternal.ReadProcessMemory(processHandle, posXAddress, addressBuffer, addressBuffer.Length, out bytesRead);
                UInt16 playerX = BitConverter.ToUInt16(addressBuffer, 0);

                WindowsExternal.ReadProcessMemory(processHandle, posYAddress, addressBuffer, addressBuffer.Length, out bytesRead);
                UInt16 playerY = BitConverter.ToUInt16(addressBuffer, 0);

                IntPtr uiSettingsPath = IntPtr.Add(processAddress, Offsets.InGameMap);
                WindowsExternal.ReadProcessMemory(processHandle, uiSettingsPath, byteBuffer, byteBuffer.Length,
                    out bytesRead);
                Boolean mapShown = BitConverter.ToBoolean(byteBuffer, 0);

                return new GameData()
                {
                    MapSeed = mapSeed,
                    PlayerX = playerX,
                    PlayerY = playerY,
                    AreaId = (Area)dwLevelId,
                    Difficulty = aGameDifficulty,
                    MapShown = mapShown,
                    MainWindowHandle = gameProcess.MainWindowHandle,
                };
            }
            catch(Exception)
            {
                return null;
            }
        }
    }

}
