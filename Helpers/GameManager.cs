using MapAssist.Settings;
using MapAssist.Structs;
using System;
using System.Collections.Generic;
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

        private static Dictionary<int, IntPtr> _UnitHashTableOffset = new Dictionary<int, IntPtr>();
        private static Dictionary<int, IntPtr> _ExpansionCheckOffset = new Dictionary<int, IntPtr>();
        private static Dictionary<int, IntPtr> _GameNameOffset = new Dictionary<int, IntPtr>();
        private static Dictionary<int, IntPtr> _MenuDataOffset = new Dictionary<int, IntPtr>();
        private static Dictionary<int, IntPtr> _RosterDataOffset = new Dictionary<int, IntPtr>();
        private static Dictionary<int, IntPtr> _InteractedNpcOffset = new Dictionary<int, IntPtr>();
        private static Dictionary<int, IntPtr> _LastHoverDataOffset = new Dictionary<int, IntPtr>();
        private static Dictionary<int, IntPtr> _PetsOffsetOffset = new Dictionary<int, IntPtr>();

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

            WindowsExternal.GetWindowThreadProcessId(hwnd, out var processId);

            _foregroundProcessId = (int)processId;

            if (_lastGameProcessId == _foregroundProcessId) // Process is the last found valid game process
            {
                _log.Info($"Active window changed to last game process (handle: {hwnd})");
                return;
            }

            Process process;
            try // The process can end before this block is done, hence wrap it in a try catch
            {
                try
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

                    if (process.HasExited) // Game window has exited
                    {
                        _log.Info($"Game window has exited (handle: {hwnd})");
                        ClearLastGameProcess();
                        return;
                    }

                    using (var _ = new ProcessContext(process)) { } // Read memory test to see if game is running as an admin
                }
                catch (Win32Exception ex)
                {
                    if (ex.Message == "Access is denied")
                    {
                        _log.Info($"Active window changed a game window that requires admin rights (handle: {hwnd})");
                        OnGameAccessDenied(null, null);
                        return;
                    }
                    else
                    {
                        throw ex;
                    }
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

            _lastGameHwnd = hwnd;
            _lastGameProcess = process;
            _lastGameProcessId = _foregroundProcessId;

            if (!_UnitHashTableOffset.ContainsKey(process.Id)) _UnitHashTableOffset[process.Id] = IntPtr.Zero;
            if (!_ExpansionCheckOffset.ContainsKey(process.Id)) _ExpansionCheckOffset[process.Id] = IntPtr.Zero;
            if (!_GameNameOffset.ContainsKey(process.Id)) _GameNameOffset[process.Id] = IntPtr.Zero;
            if (!_MenuDataOffset.ContainsKey(process.Id)) _MenuDataOffset[process.Id] = IntPtr.Zero;
            if (!_RosterDataOffset.ContainsKey(process.Id)) _RosterDataOffset[process.Id] = IntPtr.Zero;
            if (!_InteractedNpcOffset.ContainsKey(process.Id)) _InteractedNpcOffset[process.Id] = IntPtr.Zero;
            if (!_LastHoverDataOffset.ContainsKey(process.Id)) _LastHoverDataOffset[process.Id] = IntPtr.Zero;
            if (!_PetsOffsetOffset.ContainsKey(process.Id)) _PetsOffsetOffset[process.Id] = IntPtr.Zero;
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
                var pid = processContext.ProcessId;

                if (_UnitHashTableOffset[pid] == IntPtr.Zero)
                {
                    PopulateMissingOffsets();
                }

                return processContext.Read<UnitHashTable>(IntPtr.Add(_UnitHashTableOffset[pid], offset));
            }
        }

        public static IntPtr ExpansionCheckOffset
        {
            get
            {
                if (_ExpansionCheckOffset[_lastGameProcessId] != IntPtr.Zero)
                {
                    return _ExpansionCheckOffset[_lastGameProcessId];
                }

                PopulateMissingOffsets();

                return _ExpansionCheckOffset[_lastGameProcessId];
            }
        }

        public static IntPtr GameNameOffset
        {
            get
            {
                if (_GameNameOffset[_lastGameProcessId] != IntPtr.Zero)
                {
                    return _GameNameOffset[_lastGameProcessId];
                }

                PopulateMissingOffsets();

                return _GameNameOffset[_lastGameProcessId];
            }
        }

        public static IntPtr MenuDataOffset
        {
            get
            {
                if (_MenuDataOffset[_lastGameProcessId] != IntPtr.Zero)
                {
                    return _MenuDataOffset[_lastGameProcessId];
                }

                PopulateMissingOffsets();

                return _MenuDataOffset[_lastGameProcessId];
            }
        }

        public static IntPtr RosterDataOffset
        {
            get
            {
                if (_RosterDataOffset[_lastGameProcessId] != IntPtr.Zero)
                {
                    return _RosterDataOffset[_lastGameProcessId];
                }

                PopulateMissingOffsets();

                return _RosterDataOffset[_lastGameProcessId];
            }
        }

        public static IntPtr LastHoverDataOffset
        {
            get
            {
                if (_LastHoverDataOffset[_lastGameProcessId] != IntPtr.Zero)
                {
                    return _LastHoverDataOffset[_lastGameProcessId];
                }

                PopulateMissingOffsets();

                return _LastHoverDataOffset[_lastGameProcessId];
            }
        }

        public static IntPtr InteractedNpcOffset
        {
            get
            {
                if (_InteractedNpcOffset[_lastGameProcessId] != IntPtr.Zero)
                {
                    return _InteractedNpcOffset[_lastGameProcessId];
                }

                PopulateMissingOffsets();

                return _InteractedNpcOffset[_lastGameProcessId];
            }
        }

        public static IntPtr PetsOffset
        {
            get
            {
                if (_PetsOffsetOffset[_lastGameProcessId] != IntPtr.Zero)
                {
                    return _PetsOffsetOffset[_lastGameProcessId];
                }

                PopulateMissingOffsets();

                return _PetsOffsetOffset[_lastGameProcessId];
            }
        }

        private static void PopulateMissingOffsets()
        {
            // The fact we are here means we are missing some offset,
            // which means we will need the buffer.
            using (var processContext = GetProcessContext())
            {
                var pid = processContext.ProcessId;
                var buffer = processContext.Read<byte>(processContext.BaseAddr, processContext.ModuleSize);

                if (_UnitHashTableOffset[pid] == IntPtr.Zero)
                {
                    _UnitHashTableOffset[pid] = processContext.GetUnitHashtableOffset(buffer);
                    _log.Info($"Found offset {nameof(_UnitHashTableOffset)} 0x{_UnitHashTableOffset[pid].ToInt64() - processContext.BaseAddr.ToInt64():X}");
                }

                if (_ExpansionCheckOffset[pid] == IntPtr.Zero)
                {
                    _ExpansionCheckOffset[pid] = processContext.GetExpansionOffset(buffer);
                    _log.Info($"Found offset {nameof(_ExpansionCheckOffset)} 0x{_ExpansionCheckOffset[pid].ToInt64() - processContext.BaseAddr.ToInt64():X}");
                }

                if (_GameNameOffset[pid] == IntPtr.Zero)
                {
                    _GameNameOffset[pid] = processContext.GetGameNameOffset(buffer);
                    _log.Info($"Found offset {nameof(_GameNameOffset)} 0x{_GameNameOffset[pid].ToInt64() - processContext.BaseAddr.ToInt64():X}");
                }

                if (_MenuDataOffset[pid] == IntPtr.Zero)
                {
                    _MenuDataOffset[pid] = processContext.GetMenuDataOffset(buffer);
                    _log.Info($"Found offset {nameof(_MenuDataOffset)} 0x{_MenuDataOffset[pid].ToInt64() - processContext.BaseAddr.ToInt64():X}");
                }

                if (_RosterDataOffset[pid] == IntPtr.Zero)
                {
                    _RosterDataOffset[pid] = processContext.GetRosterDataOffset(buffer);
                    _log.Info($"Found offset {nameof(_RosterDataOffset)} 0x{_RosterDataOffset[pid].ToInt64() - processContext.BaseAddr.ToInt64():X}");
                }

                if (_LastHoverDataOffset[pid] == IntPtr.Zero)
                {
                    _LastHoverDataOffset[pid] = processContext.GetLastHoverObjectOffset(buffer);
                    _log.Info($"Found offset {nameof(_LastHoverDataOffset)} 0x{_LastHoverDataOffset[pid].ToInt64() - processContext.BaseAddr.ToInt64():X}");
                }

                if (_InteractedNpcOffset[pid] == IntPtr.Zero)
                {
                    _InteractedNpcOffset[pid] = processContext.GetInteractedNpcOffset(buffer);
                    _log.Info($"Found offset {nameof(_InteractedNpcOffset)} 0x{_InteractedNpcOffset[pid].ToInt64() - processContext.BaseAddr.ToInt64():X}");
                }

                if (_PetsOffsetOffset[pid] == IntPtr.Zero)
                {
                    _PetsOffsetOffset[pid] = processContext.GetPetsOffset(buffer);
                    _log.Info($"Found offset {nameof(_PetsOffsetOffset)} 0x{_PetsOffsetOffset[pid].ToInt64() - processContext.BaseAddr.ToInt64():X}");
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
