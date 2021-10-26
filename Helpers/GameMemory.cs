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

using MapAssist.Types;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Numerics;

namespace MapAssist.Helpers
{
    class GameMemory
    {
        private static string processName = Encoding.UTF8.GetString(new byte[] { 68, 50, 82 });
        public static IntPtr? ProcessHandle = null;
        public static bool foundcheck = false;
        public static IntPtr? SaveAddress = null;
        public static IntPtr? CheckAddress = null;

        public static GameData GetGameData()
        {
            // Clean up and organize, add better exception handeling.
            try
            {
                Process[] process = Process.GetProcessesByName(processName);
                Process gameProcess = process.Length > 0 ? process[0] : null;
                if (gameProcess == null)
                {
                    ProcessHandle = null;
                    return null;
                }

                if (ProcessHandle == null)
                {
                    ProcessHandle =
                        WindowsExternal.OpenProcess((uint)WindowsExternal.ProcessAccessFlags.VirtualMemoryRead, false, gameProcess.Id);
                }
                IntPtr processAddress = gameProcess.MainModule.BaseAddress;
                IntPtr pPlayerUnit = IntPtr.Add(processAddress, Offsets.PlayerUnit);

                var addressBuffer = new byte[8];
                var addressBuffer1 = new byte[8];
                var addressBuffer2 = new byte[8];
                var dwordBuffer = new byte[4];
                var byteBuffer = new byte[1];

                //QQlol's code fix joker
                if (foundcheck == false)
                {
                    for (int i = 0; i < 128; i++)
                    {
                        IntPtr decryptpPlayer = IntPtr.Add(pPlayerUnit, (int)i * 8);

                        WindowsExternal.ReadProcessMemory((IntPtr)ProcessHandle, decryptpPlayer, addressBuffer1, addressBuffer1.Length, out _);

                        IntPtr tplayerUnit = (IntPtr)BitConverter.ToInt64(addressBuffer1, 0);

                        if (tplayerUnit.ToInt64() != 0)
                        {
                            IntPtr decryptpPlayerCheck = IntPtr.Add(tplayerUnit, (int)0xB8);

                            WindowsExternal.ReadProcessMemory((IntPtr)ProcessHandle, decryptpPlayerCheck, addressBuffer2, addressBuffer2.Length, out _);
                            IntPtr CheckplayerUnit = (IntPtr)BitConverter.ToInt64(addressBuffer2, 0);
                            if (CheckplayerUnit.ToInt64() == 0x0000000000000100)
                            {
                                //Console.WriteLine("Successfully finding the player pointer");
                                SaveAddress = tplayerUnit;
                                CheckAddress = decryptpPlayer;
                                foundcheck = true;
                                break;
                            }
                            else
                            {
                                //Console.WriteLine("Failed to find player pointer");
                            }
                        }
                    }
                }

                var playerUnit = (IntPtr)SaveAddress;
                var CheckUnit = (IntPtr)CheckAddress;

                WindowsExternal.ReadProcessMemory((IntPtr)ProcessHandle, CheckUnit, addressBuffer, addressBuffer.Length, out _);

                IntPtr CleanPointer = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                if (CleanPointer.ToInt64() == 0)
                {
                    //Console.WriteLine("Clean player pointer");
                    foundcheck = false;
                    return null;
                }

                IntPtr pPlayer = IntPtr.Add(playerUnit, 0x10);
                IntPtr pAct = IntPtr.Add(playerUnit, 0x20);

                WindowsExternal.ReadProcessMemory((IntPtr)ProcessHandle, pPlayer, addressBuffer, addressBuffer.Length, out _);
                var player = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                var playerNameBuffer = new byte[16];
                WindowsExternal.ReadProcessMemory((IntPtr)ProcessHandle, player, playerNameBuffer, playerNameBuffer.Length,
                    out _);
                string playerName = Encoding.ASCII.GetString(playerNameBuffer);

                WindowsExternal.ReadProcessMemory((IntPtr)ProcessHandle, pAct, addressBuffer, addressBuffer.Length, out _);
                var aAct = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                IntPtr pActUnk1 = IntPtr.Add(aAct, 0x70);

                WindowsExternal.ReadProcessMemory((IntPtr)ProcessHandle, pActUnk1, addressBuffer, addressBuffer.Length, out _);
                var aActUnk1 = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                IntPtr pGameDifficulty = IntPtr.Add(aActUnk1, 0x830);

                WindowsExternal.ReadProcessMemory((IntPtr)ProcessHandle, pGameDifficulty, byteBuffer, byteBuffer.Length, out _);
                ushort aGameDifficulty = byteBuffer[0];

                IntPtr aDwAct = IntPtr.Add(aAct, 0x20);
                WindowsExternal.ReadProcessMemory((IntPtr)ProcessHandle, aDwAct, dwordBuffer, dwordBuffer.Length, out _);

                IntPtr aMapSeed = IntPtr.Add(aAct, 0x14);

                IntPtr pPath = IntPtr.Add(playerUnit, 0x38);

                WindowsExternal.ReadProcessMemory((IntPtr)ProcessHandle, pPath, addressBuffer, addressBuffer.Length, out _);
                var path = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                IntPtr pRoom1 = IntPtr.Add(path, 0x20);

                WindowsExternal.ReadProcessMemory((IntPtr)ProcessHandle, pRoom1, addressBuffer, addressBuffer.Length, out _);
                var aRoom1 = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                IntPtr pRoom2 = IntPtr.Add(aRoom1, 0x18);
                WindowsExternal.ReadProcessMemory((IntPtr)ProcessHandle, pRoom2, addressBuffer, addressBuffer.Length, out _);
                var aRoom2 = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                IntPtr pLevel = IntPtr.Add(aRoom2, 0x90);
                WindowsExternal.ReadProcessMemory((IntPtr)ProcessHandle, pLevel, addressBuffer, addressBuffer.Length, out _);
                var aLevel = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                if (addressBuffer.All(o => o == 0))
                    return null;

                IntPtr aLevelId = IntPtr.Add(aLevel, 0x1F8);
                WindowsExternal.ReadProcessMemory((IntPtr)ProcessHandle, aLevelId, dwordBuffer, dwordBuffer.Length, out _);
                var dwLevelId = BitConverter.ToUInt32(dwordBuffer, 0);

                IntPtr posXAddress = IntPtr.Add(path, 0x02);
                IntPtr posYAddress = IntPtr.Add(path, 0x06);

                WindowsExternal.ReadProcessMemory((IntPtr)ProcessHandle, aMapSeed, dwordBuffer, dwordBuffer.Length, out _);
                var mapSeed = BitConverter.ToUInt32(dwordBuffer, 0);

                WindowsExternal.ReadProcessMemory((IntPtr)ProcessHandle, posXAddress, addressBuffer, addressBuffer.Length,
                    out _);
                var playerX = BitConverter.ToUInt16(addressBuffer, 0);

                WindowsExternal.ReadProcessMemory((IntPtr)ProcessHandle, posYAddress, addressBuffer, addressBuffer.Length,
                    out _);
                var playerY = BitConverter.ToUInt16(addressBuffer, 0);

                IntPtr uiSettingsPath = IntPtr.Add(processAddress, Offsets.InGameMap);
                WindowsExternal.ReadProcessMemory((IntPtr)ProcessHandle, uiSettingsPath, byteBuffer, byteBuffer.Length,
                    out _);
                var mapShown = BitConverter.ToBoolean(byteBuffer, 0);

                return new GameData
                {
                    PlayerPosition = new Point(playerX, playerY),
                    MapSeed = mapSeed,
                    Area = (Area)dwLevelId,
                    Difficulty = (Difficulty)aGameDifficulty,
                    MapShown = mapShown,
                    MainWindowHandle = gameProcess.MainWindowHandle
                };
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
