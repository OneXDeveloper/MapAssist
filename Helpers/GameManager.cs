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

using MapAssist.Settings;
using MapAssist.Structs;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MapAssist.Helpers
{
    public class GameManager
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        public static readonly string ProcessName = Encoding.UTF8.GetString(new byte[] { 68, 50, 82 });
        private static IntPtr _winHook;
        private static int _foregroundProcessId = 0;

        private static IntPtr _lastGameHwnd = IntPtr.Zero;
        private static Process _lastGameProcess;
        private static int _lastGameProcessId = 0;
        private static ProcessContext _processContext;

        public delegate void StatusUpdateHandler(object sender, EventArgs e);

        public static event StatusUpdateHandler OnGameAccessDenied;

        private static IntPtr _UnitHashTableOffset;
        private static IntPtr _ExpansionCheckOffset;
        private static IntPtr _GameNameOffset;
        private static IntPtr _MenuPanelOpenOffset;
        private static IntPtr _MenuDataOffset;
        private static IntPtr _MapSeedOffset;
        private static IntPtr _RosterDataOffset;
        private static IntPtr _InteractedNpcOffset;
        private static IntPtr _LastHoverDataOffset;

        private static WindowsExternal.WinEventDelegate _eventDelegate = null;

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
            if (!WindowsExternal.HandleExists(hwnd)) // Handle doesn't exist
            {
                _log.Info($"Active window changed to another process (handle: {hwnd})");
                return;
            }

            uint processId;
            WindowsExternal.GetWindowThreadProcessId(hwnd, out processId);

            _foregroundProcessId = (int)processId;

            if (_lastGameProcessId == _foregroundProcessId) // Process is the last found valid game process
            {
                _log.Info($"Active window changed to last game process (handle: {hwnd})");
                return;
            }

            Process process;
            try // The process can end before this block is done, hence wrap it in a try catch
            {
                process = Process.GetProcessById(_foregroundProcessId); // If closing another non-foreground window, Process.GetProcessById can fail

                // Skip process by window title
                if (MapAssistConfiguration.Loaded.AuthorizedWindowTitles.Length != 0 && !MapAssistConfiguration.Loaded.AuthorizedWindowTitles.Any(process.MainWindowTitle.Contains))
                {
                    _log.Info($"Skipping window because of title (handle: {hwnd})");
                    return;
                }

                if (process.ProcessName != ProcessName) // Not a valid game process
                {
                    _log.Info($"Active window changed to a non-game window (handle: {hwnd})");
                    ClearLastGameProcess();
                    return;
                }
            }
            catch
            {
                _log.Info($"Active window changed to a now closed window (handle: {hwnd})");
                ClearLastGameProcess();
                return;
            }

            // is a new game process
            _log.Info($"Active window changed to a game window (handle: {hwnd})");

            try
            {
                using (var _ = new ProcessContext(process)) { } // Read memory test to see if game is running as an admin
            }
            catch (Win32Exception ex)
            {
                if (ex.Message == "Access is denied")
                {
                    OnGameAccessDenied(null, null);
                    return;
                }
                else
                {
                    throw ex;
                }
            }

            _UnitHashTableOffset = IntPtr.Zero;
            _ExpansionCheckOffset = IntPtr.Zero;
            _GameNameOffset = IntPtr.Zero;
            _MenuPanelOpenOffset = IntPtr.Zero;
            _MenuDataOffset = IntPtr.Zero;
            _MapSeedOffset = IntPtr.Zero;
            _RosterDataOffset = IntPtr.Zero;
            _InteractedNpcOffset = IntPtr.Zero;
            _LastHoverDataOffset = IntPtr.Zero;

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
                _processContext = new ProcessContext(_lastGameProcess); // Rarely, the VirtualMemoryRead will cause an error, in that case return a null instead of a runtime error. The next frame will try again.
                return _processContext;
            }

            return null;
        }

        private static void ClearLastGameProcess()
        {
            if (MapAssistConfiguration.Loaded.RenderingConfiguration.StickToLastGameWindow) return;

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

        public static IntPtr GameNameOffset
        {
            get
            {
                if (_GameNameOffset != IntPtr.Zero)
                {
                    return _GameNameOffset;
                }

                using (var processContext = GetProcessContext())
                {
                    _GameNameOffset = (IntPtr)processContext.GetGameNameOffset();
                }

                return _GameNameOffset;
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

        public static IntPtr MapSeedOffset
        {
            get
            {
                if (_MapSeedOffset != IntPtr.Zero)
                {
                    return _MapSeedOffset;
                }

                using (var processContext = GetProcessContext())
                {
                    _MapSeedOffset = (IntPtr)processContext.GetMapSeedOffset();
                }

                return _MapSeedOffset;
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

        public static IntPtr LastHoverDataOffset
        {
            get
            {
                if (_LastHoverDataOffset != IntPtr.Zero)
                {
                    return _LastHoverDataOffset;
                }

                using (var processContext = GetProcessContext())
                {
                    _LastHoverDataOffset = processContext.GetLastHoverObjectOffset();
                }

                return _LastHoverDataOffset;
            }
        }

        public static IntPtr InteractedNpcOffset
        {
            get
            {
                if (_InteractedNpcOffset != IntPtr.Zero)
                {
                    return _InteractedNpcOffset;
                }

                using (var processContext = GetProcessContext())
                {
                    _InteractedNpcOffset = processContext.GetInteractedNpcOffset();
                }

                return _InteractedNpcOffset;
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
