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
using System.Text;
using MapAssist.Structs;

namespace MapAssist.Helpers
{
    public class GameManager
    {
        private static readonly string ProcessName = Encoding.UTF8.GetString(new byte[] { 68, 50, 82 });
        private static IntPtr _winHook;
        private static int _foregroundProcessId = 0;

        private static IntPtr _lastGameHwnd = IntPtr.Zero;
        private static Process _lastGameProcess;
        private static int _lastGameProcessId = 0;
        private static ProcessContext _processContext;

        private static Types.UnitAny _PlayerUnit = default;
        private static IntPtr _UnitHashTableOffset;
        private static IntPtr _ExpansionCheckOffset;
        private static IntPtr _GameIPOffset;
        private static IntPtr _MenuPanelOpenOffset;
        private static IntPtr _MenuDataOffset;
        private static IntPtr _RosterDataOffset;

        private static WindowsExternal.WinEventDelegate _eventDelegate = null;

        private static bool _playerNotFoundErrorThrown = false;

        public static void MonitorForegroundWindow()
        {
            _eventDelegate = new WindowsExternal.WinEventDelegate(WinEventProc);
            _winHook = WindowsExternal.SetWinEventHook(WindowsExternal.EVENT_SYSTEM_FOREGROUND, WindowsExternal.EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _eventDelegate, 0, 0, WindowsExternal.WINEVENT_OUTOFCONTEXT);

            SetActiveWindow(WindowsExternal.GetForegroundWindow()); // Needed once to start, afterwards the hook will provide updates
        }

        private static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            SetActiveWindow(hwnd);
        }

        private static void SetActiveWindow(IntPtr hwnd)
        {
            if (!WindowsExternal.HandleExists(hwnd)) // Process doesn't exist
            {
                return;
            }

            uint processId;
            WindowsExternal.GetWindowThreadProcessId(hwnd, out processId);

            _foregroundProcessId = (int)processId;

            if (_lastGameProcessId == _foregroundProcessId) // Process is the last found valid game process
            {
                return;
            }

            Process process;
            try // The process can end before this block is done, hence wrap it in a try catch
            {
                process = Process.GetProcessById(_foregroundProcessId); // If closing another non-foreground window, Process.GetProcessById can fail

                if (process.ProcessName != ProcessName) // Not a valid game process
                {
                    ClearLastGameProcess();
                    return;
                }
            }
            catch
            {
                ClearLastGameProcess();
                return;
            }

            // is a new game process
            ResetPlayerUnit();

            _lastGameHwnd = hwnd;
            _lastGameProcess = process;
            _lastGameProcessId = _foregroundProcessId;
        }

        public static ProcessContext GetProcessContext()
        {
            if (_processContext != null && _processContext.OpenContextCount > 0)
            {
                _processContext.OpenContextCount += 1;
                return _processContext;
            }
            else if (_lastGameProcess != null && WindowsExternal.HandleExists(_lastGameHwnd))
            {
                _processContext = new ProcessContext(_lastGameProcess);
                return _processContext;
            }

            return null;
        }

        private static void ClearLastGameProcess()
        {
            if (_processContext != null && _processContext.OpenContextCount == 0 && _lastGameProcess != null) // Prevent disposing the process when the context is open
            {
                _lastGameProcess.Dispose();
            }

            _lastGameHwnd = IntPtr.Zero;
            _lastGameProcess = null;
            _lastGameProcessId = 0;
        }

        public static IntPtr MainWindowHandle { get => _lastGameHwnd; }
        public static bool IsGameInForeground { get => _lastGameProcessId == _foregroundProcessId; }

        public static Types.UnitAny PlayerUnit
        {
            get
            {
                if (Equals(_PlayerUnit, default(Types.UnitAny)))
                {
                    foreach (var pUnitAny in UnitHashTable().UnitTable)
                    {
                        var unitAny = new Types.UnitAny(pUnitAny);

                        while (unitAny.IsValidUnit())
                        {
                            if (unitAny.IsPlayerUnit())
                            {
                                _playerNotFoundErrorThrown = false;
                                _PlayerUnit = unitAny;
                                return _PlayerUnit;
                            }

                            unitAny = unitAny.ListNext;
                        }
                    }
                }
                else
                {
                    _playerNotFoundErrorThrown = false;
                    return _PlayerUnit;
                }

                if (!_playerNotFoundErrorThrown)
                {
                    _playerNotFoundErrorThrown = true;
                    throw new Exception("Player unit not found.");
                }
                else
                {
                    return default(Types.UnitAny);
                }
            }
        }

        public static UnitHashTable UnitHashTable(int offset = 0)
        {
            using (var processContext = GetProcessContext())
            {
                if (_UnitHashTableOffset == IntPtr.Zero)
                {

                    _UnitHashTableOffset = processContext.GetUnitHashtableOffset();
                }

                return processContext.Read<UnitHashTable>(IntPtr.Add(_UnitHashTableOffset, offset));
            }
        }

        public static IntPtr ExpansionCheckOffset
        {
            get
            {
                if (_ExpansionCheckOffset != IntPtr.Zero)
                {
                    return _ExpansionCheckOffset;
                }

                using (var processContext = GetProcessContext())
                {
                    _ExpansionCheckOffset = processContext.GetExpansionOffset();
                }

                return _ExpansionCheckOffset;
            }
        }
        public static IntPtr GameIPOffset
        {
            get
            {
                if (_GameIPOffset != IntPtr.Zero)
                {
                    return _GameIPOffset;
                }

                using (var processContext = GetProcessContext())
                {
                    _GameIPOffset = (IntPtr)processContext.GetGameIPOffset();

                }

                return _GameIPOffset;
            }
        }
        public static IntPtr MenuOpenOffset
        {
            get
            {
                if (_MenuPanelOpenOffset != IntPtr.Zero)
                {
                    return _MenuPanelOpenOffset;
                }

                using (var processContext = GetProcessContext())
                {
                    _MenuPanelOpenOffset = (IntPtr)processContext.GetMenuOpenOffset();
                }

                return _MenuPanelOpenOffset;
            }
        }
        public static IntPtr MenuDataOffset
        {
            get
            {
                if (_MenuDataOffset != IntPtr.Zero)
                {
                    return _MenuDataOffset;
                }

                using (var processContext = GetProcessContext())
                {
                    _MenuDataOffset = (IntPtr)processContext.GetMenuDataOffset();
                }

                return _MenuDataOffset;
            }
        }
        public static IntPtr RosterDataOffset
        {
            get
            {
                if (_RosterDataOffset != IntPtr.Zero)
                {
                    return _RosterDataOffset;
                }

                using (var processContext = GetProcessContext())
                {
                    _RosterDataOffset = processContext.GetRosterDataOffset();
                }

                return _RosterDataOffset;
            }
        }

        public static void ResetPlayerUnit()
        {
            _PlayerUnit = default;
            using (var processContext = GetProcessContext())
            {
                if (processContext == null) { return; }
                var processId = processContext.ProcessId;
                if(GameMemory.PlayerUnits.TryGetValue(processId, out var playerUnit))
                {
                    GameMemory.PlayerUnits[processId] = default;
                }
            }
        }
        
        public static void Dispose()
        {
            if (_lastGameProcess != null)
            {
                _lastGameProcess.Dispose();
            }
            WindowsExternal.UnhookWinEvent(_winHook);
        }
    }
}
