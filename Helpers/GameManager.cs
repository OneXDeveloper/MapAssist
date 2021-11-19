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
using System.Diagnostics;
using System.Linq;
using System.Text;
using MapAssist.Structs;
using MapAssist.Types;

namespace MapAssist.Helpers
{
    public static class GameManager
    {
        private static readonly string ProcessName = Encoding.UTF8.GetString(new byte[] { 68, 50, 82 });
        private static IntPtr PlayerUnitPtr = IntPtr.Zero;
        private static Types.UnitAny _PlayerUnit = default;
        private static int LastProcessId = 0;
        private static IntPtr _MainWindowHandle = IntPtr.Zero;
        private static ProcessContext _ProcessContext;
        private static Process GameProcess;

        public static ProcessContext GetProcessContext()
        {
            if (_ProcessContext != null && _ProcessContext.OpenContextCount > 0)
            {
                IntPtr windowInFocus = WindowsExternal.GetForegroundWindow();
                if (_MainWindowHandle == windowInFocus)
                {
                    _ProcessContext.OpenContextCount++;
                    return _ProcessContext;
                } 
                else
                {
                    GameProcess = null;
                }
            }

            if (GameProcess == null)
            {
                Process[] processes = Process.GetProcessesByName(ProcessName);

                Process gameProcess = null;

                IntPtr windowInFocus = WindowsExternal.GetForegroundWindow();
                if (windowInFocus == IntPtr.Zero)
                {
                    gameProcess = processes.FirstOrDefault();
                }
                else
                {
                    gameProcess = processes.FirstOrDefault(p => p.MainWindowHandle == windowInFocus);
                }

                if (gameProcess == null)
                {
                    gameProcess = processes.FirstOrDefault();
                }

                if (gameProcess == null)
                {
                    throw new Exception("Game process not found.");
                }

                // If changing processes we need to re-find the player
                if (gameProcess.Id != LastProcessId)
                {
                    ResetPlayerUnit();
                }

                GameProcess = gameProcess;
            }

            LastProcessId = GameProcess.Id;
            _MainWindowHandle = GameProcess.MainWindowHandle;
            _ProcessContext = new ProcessContext(GameProcess);
            
            //Start
            IntPtr processHandle = IntPtr.Zero;
            processHandle =
                    WindowsExternal.OpenProcess((uint)WindowsExternal.ProcessAccessFlags.VirtualMemoryRead, false, GameProcess.Id);

            if (PlayerUnitPtr == IntPtr.Zero)
            {
                var Sigscan1 = new SigScanSharp(GameProcess.Handle);
                Sigscan1.SelectModule(GameProcess.MainModule);
                IntPtr processAddress = GameProcess.MainModule.BaseAddress;

                Sigscan1.AddPattern("Pattern1", "48 8D 05 ? ? ? ? 8B D1 44 8B C1 49 8B C9 83 E2 7F 48 C1 E1 0A 48 03 C8");
                Sigscan1.AddPattern("UnitTableOffset", "8B D1 44 8B C1 49 8B C9 83 E2 7F 48 C1 E1 0A 48 03 C8");
                Sigscan1.AddPattern("Pattern2", "C6 05 ? ? ? ? 00 40 84 F6 74 ? B9 0A 00 00 00 E8 ? ? ? ? B9 0A 00 00 00 E8 ? ? ? ? 48");
                Sigscan1.AddPattern("UISettingOffset", "40 84 F6 74 ? B9 0A 00 00 00 E8 ? ? ? ? B9 0A 00 00 00 E8 ? ? ? ? 48");
                Sigscan1.AddPattern("Pattern3", "0F B6 2D ? ? ? ? C7 05 ? ? ? ? 00 00 00 00 48 85 C0 0F 84 ? ? ? ? 83 78 5C 00 0F 84 ? ? ? ? 33 D2 41");
                Sigscan1.AddPattern("ExpansionOffset", "C7 05 ? ? ? ? 00 00 00 00 48 85 C0 0F 84 ? ? ? ? 83 78 5C 00 0F 84 ? ? ? ? 33 D2 41");
                Sigscan1.AddPattern("Pattern4", "48 03 3D ? ? ? ? 74 ? 49 8B CE E8 ? ? ? ? 4C 8B E0 48 C7 C1 FF FF FF FF");
                Sigscan1.AddPattern("DataTableOffset", "? ? ? ? 74 ? 49 8B CE E8 ? ? ? ? 4C 8B E0 48 C7 C1 FF FF FF FF");

                long lTime;
                var BasePointer = processAddress;
                var sResult = Sigscan1.FindPatterns(out lTime);
                var sc = (long)sResult["Pattern1"];
                var sx = ProcessContext.Reads<int>(processHandle, (IntPtr)(long)sc + 3);
                var sLabel = (long)sResult["UnitTableOffset"];
                var sOffset = sLabel + sx;
                var UnitOffset = (long)sOffset - (long)BasePointer;
                Offsets.UnitHashTable = (int)UnitOffset;

                var ac = (long)sResult["Pattern2"];
                var ax = ProcessContext.Reads<int>(processHandle, (IntPtr)(long)ac + 2);
                var aLabel = (long)sResult["UISettingOffset"];
                var aOffset = aLabel + ax;
                var UiOffset = (long)aOffset - (long)BasePointer;
                Offsets.UiSettings = (int)UiOffset;

                var ec = (long)sResult["Pattern3"];
                var ex = ProcessContext.Reads<int>(processHandle, (IntPtr)(long)ec + 3);
                var eLabel = (long)sResult["ExpansionOffset"];
                var eOffset = eLabel + ex;
                var ExpansionOffset = (long)eOffset - (long)BasePointer;
                Offsets.ExpansionCheck = (int)ExpansionOffset;

                var I_table1 = (long)sResult["Pattern4"];
                var I_table2 = ProcessContext.Reads<int>(processHandle, (IntPtr)(long)I_table1 + 3);
                var I_table3 = (long)sResult["DataTableOffset"];
                var I_table4 = I_table3 + I_table2;
                var DataTableOffset = (long)I_table4 - (long)BasePointer;
                Offsets.DataTable = (int)DataTableOffset;
            }
            //End
            return _ProcessContext;
        }

        public static IntPtr MainWindowHandle { get => _MainWindowHandle; }

        public static Types.UnitAny PlayerUnit
        {
            get
            {
                using (var processContext = GetProcessContext())
                {
                    if (Equals(_PlayerUnit, default(Types.UnitAny)))
                    {
                        foreach (var pUnitAny in UnitHashTable.UnitTable)
                        {
                            var unitAny = new Types.UnitAny(pUnitAny);

                            while (unitAny.IsValid())
                            {
                                if (unitAny.IsPlayerUnit())
                                {
                                    PlayerUnitPtr = pUnitAny;
                                    _PlayerUnit = unitAny;
                                    return _PlayerUnit;
                                }
                                unitAny = unitAny.ListNext;
                            }
                        }
                    } else
                    {
                        return _PlayerUnit;
                    }
                    throw new Exception("Player unit not found.");
                }
            }
        }

        public static UnitHashTable UnitHashTable
        {
            get
            {
                using (var processContext = GetProcessContext())
                {
                    return processContext.Read<UnitHashTable>(processContext.FromOffset(Offsets.UnitHashTable));
                }
            }
        }

        public static Types.UiSettings UiSettings
        {
            get
            {
                using (var processContext = GetProcessContext())
                {
                    return new Types.UiSettings(processContext.FromOffset(Offsets.UiSettings));
                }
            }
        }

        public static void ResetPlayerUnit()
        {
            _PlayerUnit = default;
            PlayerUnitPtr = IntPtr.Zero;
        }
    }
}
