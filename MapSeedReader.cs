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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace D2RAssist
{
    class GameData
    {
        private static readonly HashSet<UInt32> TownIDs = new HashSet<UInt32> {1, 40, 75, 103, 109};

        public UInt16 PlayerX;
        public UInt16 PlayerY;
        public UInt32 MapSeed;
        public UInt16 Difficulty;
        public UInt32 AreaId;

        internal Boolean IsOutsideTown()
        {
            if (TownIDs.Contains(this.AreaId))
            {
                return false;
            }
            return true;
        }

        internal Boolean HasMapSeed()
        {
            if(this.MapSeed != 0)
            {
                return true;
            }
            return false;
        }
    }

    class MapSeedReader
    {
        const int PROCESS_WM_READ = 0x0010;

        public static GameData GetMapSeed()
        {
            try
            {
                Process gameProcess = Process.GetProcessesByName("D2R")[0];
                IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, gameProcess.Id);
                IntPtr processAddress = gameProcess.MainModule.BaseAddress;
                IntPtr pPlayerUnit = IntPtr.Add(processAddress, 0x205fe40);

                byte[] addressBuffer = new byte[8];
                byte[] dwordBuffer = new byte[4];
                byte[] shortBuffer = new byte[2];
                byte[] byteBuffer = new byte[1];
                ReadProcessMemory(processHandle, pPlayerUnit, addressBuffer, addressBuffer.Length, out IntPtr bytesRead);

                IntPtr playerUnit = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);
                IntPtr pPlayer = IntPtr.Add(playerUnit, 0x10);
                IntPtr pAct = IntPtr.Add(playerUnit, 0x20);

                ReadProcessMemory(processHandle, pPlayer, addressBuffer, addressBuffer.Length, out bytesRead);
                IntPtr player = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                // This was for Quest struct not difficulty
                // IntPtr aDifficulty = IntPtr.Add(player, 0x10);

                byte[] playerNameBuffer = new byte[16];
                ReadProcessMemory(processHandle, player, playerNameBuffer, playerNameBuffer.Length, out bytesRead);
                string playerName = Encoding.ASCII.GetString(playerNameBuffer);
                Console.WriteLine("Player name: " + playerName);

                //ReadProcessMemory(processHandle, aDifficulty, buffer, buffer.Length, out bytesRead);
                //ulong difficulty = BitConverter.ToUInt64(buffer, 0);
                //Console.WriteLine("Difficulty: " + difficulty);

                ReadProcessMemory(processHandle, pAct, addressBuffer, addressBuffer.Length, out bytesRead);
                IntPtr aAct = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                IntPtr pActUnk1 = IntPtr.Add(aAct, 0x70);

                ReadProcessMemory(processHandle, pActUnk1, addressBuffer, addressBuffer.Length, out bytesRead);
                IntPtr aActUnk1 = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                IntPtr pGameDifficulty = IntPtr.Add(aActUnk1, 0x830);

                ReadProcessMemory(processHandle, pGameDifficulty, byteBuffer, byteBuffer.Length, out bytesRead);
                UInt16 aGameDifficulty = byteBuffer[0];


                IntPtr aDwAct = IntPtr.Add(aAct, 0x20);
                ReadProcessMemory(processHandle, aDwAct, dwordBuffer, dwordBuffer.Length, out bytesRead);

                uint dwAct = BitConverter.ToUInt32(dwordBuffer, 0);

                IntPtr aMapSeed = IntPtr.Add(aAct, 0x14);

                IntPtr pPath = IntPtr.Add(playerUnit, 0x38);

                ReadProcessMemory(processHandle, pPath, addressBuffer, addressBuffer.Length, out bytesRead);
                IntPtr path = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                IntPtr pRoom1 = IntPtr.Add(path, 0x20);

                ReadProcessMemory(processHandle, pRoom1, addressBuffer, addressBuffer.Length, out bytesRead);
                IntPtr aRoom1 = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                IntPtr pRoom2 = IntPtr.Add(aRoom1, 0x18);
                ReadProcessMemory(processHandle, pRoom2, addressBuffer, addressBuffer.Length, out bytesRead);
                IntPtr aRoom2 = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                IntPtr pLevel = IntPtr.Add(aRoom2, 0x90);
                ReadProcessMemory(processHandle, pLevel, addressBuffer, addressBuffer.Length, out bytesRead);
                IntPtr aLevel = (IntPtr)BitConverter.ToInt64(addressBuffer, 0);

                IntPtr aLevelId = IntPtr.Add(aLevel, 0x1F8);
                ReadProcessMemory(processHandle, aLevelId, dwordBuffer, dwordBuffer.Length, out bytesRead);
                uint dwLevelId = BitConverter.ToUInt32(dwordBuffer, 0);

                /* IntPtr pRoom1 = IntPtr.Add(path, 0x1C);

                 ReadProcessMemory(processHandle, pRoom1, buffer, buffer.Length, out bytesRead);
                 IntPtr aRoom1 = (IntPtr)BitConverter.ToInt64(buffer, 0);
                 IntPtr pRoom2 = IntPtr.Add(aRoom1, 0x10);

                 ReadProcessMemory(processHandle, pRoom2, buffer, buffer.Length, out bytesRead);
                 IntPtr aRoom2 = (IntPtr)BitConverter.ToInt64(buffer, 0);
                 IntPtr pLevel = IntPtr.Add(aRoom2, 0x58);

                 ReadProcessMemory(processHandle, pLevel, buffer, buffer.Length, out bytesRead);
                 IntPtr aLevel = (IntPtr)BitConverter.ToInt64(buffer, 0);

                 IntPtr aLevelNo = IntPtr.Add(aLevel, 0x1D0);

                 byte[] dwordBuffer = new byte[4];


                 ReadProcessMemory(processHandle, aLevelNo, dwordBuffer, dwordBuffer.Length, out bytesRead);

                 uint dwLevelNo = BitConverter.ToUInt32(dwordBuffer, 0);
                 Console.WriteLine("dwLevelNo: " + dwLevelNo); */

                IntPtr posXAddress = IntPtr.Add(path, 0x02);
                IntPtr posYAddress = IntPtr.Add(path, 0x06);

                ReadProcessMemory(processHandle, aMapSeed, dwordBuffer, dwordBuffer.Length, out bytesRead);
                UInt32 mapSeed = BitConverter.ToUInt32(dwordBuffer, 0);

                ReadProcessMemory(processHandle, posXAddress, addressBuffer, addressBuffer.Length, out bytesRead);
                UInt16 playerX = BitConverter.ToUInt16(addressBuffer, 0);

                ReadProcessMemory(processHandle, posYAddress, addressBuffer, addressBuffer.Length, out bytesRead);
                UInt16 playerY = BitConverter.ToUInt16(addressBuffer, 0);

                return new GameData()
                {
                    MapSeed = mapSeed,
                    PlayerX = playerX,
                    PlayerY = playerY,
                    AreaId = dwLevelId,
                    Difficulty = aGameDifficulty
                };
            }
            catch(Exception e)
            {
                return null;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
             uint processAccess,
             bool bInheritHandle,
             int processId
        );
        public static IntPtr OpenProcess(Process proc, ProcessAccessFlags flags)
        {
            return OpenProcess((uint)flags, false, proc.Id);
        }

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out, MarshalAs(UnmanagedType.AsAny)] object lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            IntPtr lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead);
    }

}
