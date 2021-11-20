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
