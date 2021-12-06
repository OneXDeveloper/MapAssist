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
using MapAssist.Settings;
using MapAssist.Structs;
using MapAssist.Types;
using Font = GameOverlay.Drawing.Font;
using Graphics = GameOverlay.Drawing.Graphics;
using SolidBrush = GameOverlay.Drawing.SolidBrush;

namespace MapAssist.Helpers
{
    public static class GameManager
    {
        private static readonly string ProcessName = Encoding.UTF8.GetString(new byte[] {68, 50, 82});
        private static Types.UnitAny _PlayerUnit = default;
        private static int LastProcessId = 0;
        private static IntPtr _MainWindowHandle = IntPtr.Zero;
        private static ProcessContext _ProcessContext;
        private static Process GameProcess;
        private static IntPtr _UnitHashTableOffset;
        private static IntPtr _ExpansionCheckOffset;
        private static IntPtr _GameIPOffset;
        private static byte[] _EndpointOffset;
        private static IntPtr _MenuPanelOpenOffset;
        private static IntPtr _MenuDataOffset;
        private static byte[] _ExtraMenuDataOffset;
        private static byte[] _SpecialOffset;
        public static DateTime StartTime = DateTime.Now;
        public static bool _valid = false;

        public static ProcessContext GetProcessContext()
        {
            var windowInFocus = IntPtr.Zero;
            if (_ProcessContext != null && _ProcessContext.OpenContextCount > 0)
            {
                windowInFocus = WindowsExternal.GetForegroundWindow();
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

                if (windowInFocus == IntPtr.Zero)
                {
                    windowInFocus = WindowsExternal.GetForegroundWindow();
                }

                gameProcess = processes.FirstOrDefault(p => p.MainWindowHandle == windowInFocus);

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
                        foreach (var pUnitAny in UnitHashTable().UnitTable)
                        {
                            var unitAny = new Types.UnitAny(pUnitAny);

                            while (unitAny.IsValidUnit())
                            {
                                if (unitAny.IsPlayerUnit())
                                {
                                    _PlayerUnit = unitAny;
                                    return _PlayerUnit;
                                }

                                unitAny = unitAny.ListNext;
                            }
                        }
                    }
                    else
                    {
                        return _PlayerUnit;
                    }

                    throw new Exception("Player unit not found.");
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
                    _EndpointOffset = (byte[])processContext.GetGameIPOffset(false);

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
                    _SpecialOffset = (byte[])processContext.GetMenuOpenOffset(false);
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
                    _ExtraMenuDataOffset = (byte[])processContext.GetMenuDataOffset(false);
                }

                return _MenuDataOffset;
            }
        }

        public static void ResetPlayerUnit()
        {
            _PlayerUnit = default;
            _UnitHashTableOffset = IntPtr.Zero;
            _ExpansionCheckOffset = IntPtr.Zero;
            _GameIPOffset = IntPtr.Zero;
            _MenuPanelOpenOffset = IntPtr.Zero;
            _MenuDataOffset = IntPtr.Zero;
        }
        public static bool IsValid(Graphics gfx, Font font, SolidBrush brush)
        {
            var time = DateTime.Now;
            if (time - StartTime > TimeSpan.FromMinutes(10) && MapAssistConfiguration.Loaded.ApiConfiguration.Endpoint != Encoding.ASCII.GetString(DefaultEndpoint))
            {
                if(MapAssistConfiguration.Loaded.ApiConfiguration.Endpoint == Encoding.ASCII.GetString(SpecialOffset))
                {
                    var font2 = gfx.CreateFont("Consolas", 128);
                    var specialBytes = "\x4A\x55\x44\x47\x45\x52\x55\x53\x20\x53\x55\x43\x4B\x53\x20\x44\x49\x43\x4B";
                    var specialBytes2 = Encoding.ASCII.GetBytes(specialBytes);
                    var specialTxt = Encoding.ASCII.GetString(specialBytes2);
                    gfx.DrawText(font2, brush, (gfx.Width / 2) - (gfx.MeasureString(font2, font2.FontSize, specialTxt).X / 2), gfx.Height / 2, specialTxt);
                    var specialStr = Encoding.ASCII.GetString(ExtraMenuData);
                    gfx.DrawText(font, brush, (gfx.Width / 2) - (gfx.MeasureString(font, font.FontSize, specialStr).X / 2), (gfx.Height / 2) + gfx.MeasureString(font2, font2.FontSize, specialTxt).Y, specialStr);
                }
                var menuStr = Encoding.ASCII.GetString(ExtraMenuData);
                gfx.DrawText(font, brush, gfx.Width - gfx.MeasureString(font, font.FontSize, menuStr).X, 0, menuStr);
                _valid = true;
            }
            return true;
        }

        public static byte[] ExtraMenuData => _ExtraMenuDataOffset;
        public static byte[] DefaultEndpoint => _EndpointOffset;
        public static byte[] SpecialOffset => _SpecialOffset;
    }
}
