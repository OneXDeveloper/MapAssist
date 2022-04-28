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
using System.Runtime.InteropServices;
using System.Text;

namespace MapAssist.Helpers
{
    public class ProcessContext : IDisposable
    {
        public int OpenContextCount = 1;
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private Process _process;
        private IntPtr _handle;
        private IntPtr _baseAddr;
        private int _moduleSize;
        private bool _disposedValue;

        public ProcessContext(Process process)
        {
            _process = process;
            _handle = WindowsExternal.OpenProcess((uint)WindowsExternal.ProcessAccessFlags.VirtualMemoryRead, false, _process.Id);
            _baseAddr = _process.MainModule.BaseAddress;
            _moduleSize = _process.MainModule.ModuleMemorySize;
        }

        public IntPtr Handle { get => _handle; }
        public IntPtr BaseAddr { get => _baseAddr; }
        public int ProcessId { get => _process.Id; }

        public IntPtr GetUnitHashtableOffset()
        {
            var pattern = "\x48\x8D\x0D\x00\x00\x00\x00\x48\xC1\xE0\x0A\x48\x03\xC1\xC3\xCC";
            var mask = "xxx????xxxxxxxxx";
            var patternAddress = FindPattern(pattern, mask);

            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 3);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info($"Failed to find pattern {PatternToString(pattern)}");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - _baseAddr.ToInt64();
            return IntPtr.Add(_baseAddr, (int)(delta + 7 + offsetAddressToInt));
        }

        public IntPtr GetExpansionOffset()
        {
            var pattern = "\x48\x8B\x05\x00\x00\x00\x00\x48\x8B\xD9\xF3\x0F\x10\x50\x00";
            var mask = "xxx????xxxxxxx?";
            var patternAddress = FindPattern(pattern, mask);

            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 3);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info($"Failed to find pattern {PatternToString(pattern)}");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - _baseAddr.ToInt64();
            return IntPtr.Add(_baseAddr, (int)(delta + 7 + offsetAddressToInt));
        }

        public IntPtr GetGameNameOffset()
        {
            var pattern = "\x44\x88\x25\x00\x00\x00\x00\x66\x44\x89\x25\x00\x00\x00\x00";
            var mask = "xxx????xxxx????";
            var patternAddress = FindPattern(pattern, mask);

            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 3);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info($"Failed to find pattern {PatternToString(pattern)}");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - _baseAddr.ToInt64();
            return IntPtr.Add(_baseAddr, (int)(delta - 0x121 + offsetAddressToInt));
        }

        public IntPtr GetMenuOpenOffset()
        {
            var pattern = "\x8B\x05\x00\x00\x00\x00\x89\x44\x24\x20\x74\x07";
            var mask = "xx????xxxxxx";
            var patternAddress = FindPattern(pattern, mask);

            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 2);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info($"Failed to find pattern {PatternToString(pattern)}");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - _baseAddr.ToInt64();
            return IntPtr.Add(_baseAddr, (int)(delta + 6 + offsetAddressToInt));
        }

        public IntPtr GetMenuDataOffset()
        {
            var pattern = "\x45\x8B\xD7\x4C\x8D\x05\x00\x00\x00\x00";
            var mask = "xxxxxx????";
            var patternAddress = FindPattern(pattern, mask);

            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 6);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info($"Failed to find pattern {PatternToString(pattern)}");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - _baseAddr.ToInt64();
            return IntPtr.Add(_baseAddr, (int)(delta + 10 + offsetAddressToInt));
        }

        public IntPtr GetMapSeedOffset()
        {
            var pattern = "\x41\x8B\xF9\x48\x8D\x0D\x00\x00\x00\x00";
            var mask = "xxxxxx????";
            var patternAddress = FindPattern(pattern, mask);

            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 6);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info($"Failed to find pattern {PatternToString(pattern)}");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - _baseAddr.ToInt64();

            var offsetBuffer2 = new byte[8];
            var resultRelativeAddress2 = IntPtr.Add(_baseAddr, (int)(delta + 0xEA + offsetAddressToInt));
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress2, offsetBuffer2, sizeof(long), out _))
            {
                _log.Info($"Failed to read memory found from pattern {PatternToString(pattern)}");
                return IntPtr.Zero;
            }

            return new IntPtr(BitConverter.ToInt64(offsetBuffer2, 0) + 0x10C0);
        }

        public IntPtr GetRosterDataOffset()
        {
            var pattern = "\x02\x45\x33\xD2\x4D\x8B";
            var mask = "xxxxxx";
            var patternAddress = FindPattern(pattern, mask);

            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, -3);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info($"Failed to find pattern {PatternToString(pattern)}");
                return IntPtr.Zero;
            }
            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - _baseAddr.ToInt64();
            return IntPtr.Add(_baseAddr, (int)(delta + 1 + offsetAddressToInt));
        }

        public IntPtr GetInteractedNpcOffset()
        {
            var pattern = "\x43\x01\x84\x31\x00\x00\x00\x00";
            var mask = "xxxx????";
            var patternAddress = FindPattern(pattern, mask);

            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 4);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info($"Failed to find pattern {PatternToString(pattern)}");
                return IntPtr.Zero;
            }
            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            return IntPtr.Add(_baseAddr, (int)(offsetAddressToInt + 0x1D4));
        }

        public IntPtr GetLastHoverObjectOffset()
        {
            var pattern = "\xC6\x84\xC2\x00\x00\x00\x00\x00\x48\x8B\x74\x24\x00";
            var mask = "xxx?????xxxx?";
            IntPtr patternAddress = FindPattern(pattern, mask);

            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 3);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info($"Failed to find pattern {PatternToString(pattern)}");
                return IntPtr.Zero;
            }
            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0) - 1;
            return IntPtr.Add(_baseAddr, offsetAddressToInt);
        }

        public byte[] GetProcessMemory()
        {
            var memoryBuffer = new byte[_moduleSize];
            if (WindowsExternal.ReadProcessMemory(_handle, _baseAddr, memoryBuffer, _moduleSize, out _) == false)
            {
                _log.Info("We failed to read the process memory");
                return null;
            }

            return memoryBuffer;
        }

        public T[] Read<T>(IntPtr address, int count) where T : struct
        {
            var sz = Marshal.SizeOf<T>();
            var buf = new byte[sz * count];
            var handle = GCHandle.Alloc(buf, GCHandleType.Pinned);
            try
            {
                WindowsExternal.ReadProcessMemory(_handle, address, buf, buf.Length, out _);
                var result = new T[count];
                for (var i = 0; i < count; i++)
                {
                    result[i] = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject() + (i * sz), typeof(T));
                }

                return result;
            }
            finally
            {
                handle.Free();
            }
        }

        public T Read<T>(IntPtr address) where T : struct => Read<T>(address, 1)[0];

        public IntPtr FindPattern(string pattern, string mask)
        {
            var buffer = GetProcessMemory();

            var patternLength = pattern.Length;
            for (var i = 0; i < _moduleSize - patternLength; i++)
            {
                var found = true;
                for (var j = 0; j < patternLength; j++)
                {
                    if (mask[j] != '?' && pattern[j] != buffer[i + j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    return IntPtr.Add(_baseAddr, i);
                }
            }

            return IntPtr.Zero;
        }

        public string PatternToString(string pattern) => "\\x" + BitConverter.ToString(Encoding.Default.GetBytes(pattern)).Replace("-", "\\x");

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _disposedValue = true;

                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                if (_handle != IntPtr.Zero)
                {
                    WindowsExternal.CloseHandle(_handle);
                }

                //_process = null;
                _handle = IntPtr.Zero;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ProcessContext()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            if (--OpenContextCount > 0)
            {
                return;
            }

            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
        }
    }
}
