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

                if (process.HasExited) // Game window has exited
                {
                    _log.Info($"Game window has exited (handle: {hwnd})");
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
                    PopulateMissingOffsets();
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

                PopulateMissingOffsets();

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

                PopulateMissingOffsets();

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

                PopulateMissingOffsets();

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

                PopulateMissingOffsets();

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

                PopulateMissingOffsets();

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

                PopulateMissingOffsets();

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

                PopulateMissingOffsets();

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

                PopulateMissingOffsets();

                return _InteractedNpcOffset;
            }
        }

        private static void PopulateMissingOffsets()
        {
            // The fact we are here means we are missing some offset, 
            // which means we will need the buffer.
            using (var processContext = GetProcessContext())
            {
                var buffer = processContext.Read<byte>(processContext.BaseAddr, processContext.ModuleSize);

                if (_UnitHashTableOffset == IntPtr.Zero)
                {
                    _UnitHashTableOffset = processContext.GetUnitHashtableOffset(buffer);
                    _log.Info($"Found offset {nameof(_UnitHashTableOffset)} {_UnitHashTableOffset.ToInt64()-processContext.BaseAddr.ToInt64():X}");
                }

                if (_ExpansionCheckOffset == IntPtr.Zero)
                {
                    _ExpansionCheckOffset = processContext.GetExpansionOffset(buffer);
                    _log.Info($"Found offset {nameof(_ExpansionCheckOffset)} {_ExpansionCheckOffset.ToInt64()-processContext.BaseAddr.ToInt64():X}");
                }

                if (_GameNameOffset == IntPtr.Zero)
                {
                    _GameNameOffset = processContext.GetGameNameOffset(buffer);
                    _log.Info($"Found offset {nameof(_GameNameOffset)} {_GameNameOffset.ToInt64()-processContext.BaseAddr.ToInt64():X}");
                }

                if (_MenuPanelOpenOffset == IntPtr.Zero)
                {
                    _MenuPanelOpenOffset = processContext.GetMenuOpenOffset(buffer);
                    _log.Info($"Found offset {nameof(_MenuPanelOpenOffset)} {_MenuPanelOpenOffset.ToInt64()-processContext.BaseAddr.ToInt64():X}");
                }

                if (_MenuDataOffset == IntPtr.Zero)
                {
                    _MenuDataOffset = processContext.GetMenuDataOffset(buffer);
                    _log.Info($"Found offset {nameof(_MenuDataOffset)} {_MenuDataOffset.ToInt64()-processContext.BaseAddr.ToInt64():X}");
                }

                if (_MapSeedOffset == IntPtr.Zero)
                {
                    _MapSeedOffset = processContext.GetMapSeedOffset(buffer);
                    _log.Info($"Found offset {nameof(_MapSeedOffset)} {_MapSeedOffset.ToInt64()-processContext.BaseAddr.ToInt64():X}");
                }

                if (_RosterDataOffset == IntPtr.Zero)
                {
                    _RosterDataOffset = processContext.GetRosterDataOffset(buffer);
                    _log.Info($"Found offset {nameof(_RosterDataOffset)} {_RosterDataOffset.ToInt64()-processContext.BaseAddr.ToInt64():X}");
                }

                if (_LastHoverDataOffset == IntPtr.Zero)
                {
                    _LastHoverDataOffset = processContext.GetLastHoverObjectOffset(buffer);
                    _log.Info($"Found offset {nameof(_LastHoverDataOffset)} {_LastHoverDataOffset.ToInt64()-processContext.BaseAddr.ToInt64():X}");
                }

                if (_InteractedNpcOffset == IntPtr.Zero)
                {
                    _InteractedNpcOffset = processContext.GetInteractedNpcOffset(buffer);
                    _log.Info($"Found offset {nameof(_InteractedNpcOffset)} {_InteractedNpcOffset.ToInt64()-processContext.BaseAddr.ToInt64():X}");
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
