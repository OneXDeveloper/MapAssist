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
            _handle = WindowsExternal.OpenProcess((uint)WindowsExternal.ProcessAccessFlags.VirtualMemoryRead, false,
                process.Id);
            _baseAddr = process.MainModule.BaseAddress;
            _moduleSize = _process.MainModule.ModuleMemorySize;
        }

        public IntPtr Handle { get => _handle; }

        public IntPtr FromOffset(int offset)
        {
            return IntPtr.Add(_baseAddr, offset);
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

        public T Read<T>(IntPtr address) where T : struct
        {
            return Read<T>(address, 1)[0];
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                if (_handle != IntPtr.Zero)
                {
                    WindowsExternal.CloseHandle(_handle);
                }

                _process = null;
                _handle = IntPtr.Zero;
                _disposedValue = true;
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
            GC.SuppressFinalize(this);
        }

        public IntPtr GetUnitHashtableOffset()
        {
            var buffer = GetProcessMemory();
            IntPtr patternAddress = FindPatternEx(ref buffer, _baseAddr, _moduleSize,
                "\x48\x8d\x00\x00\x00\x00\x00\x8b\xd1", "xx?????xx");
            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 3);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info("We failed to read the process memory");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - _baseAddr.ToInt64();
            return IntPtr.Add(_baseAddr, (int)(delta + 7 + offsetAddressToInt));
        }

        public IntPtr GetExpansionOffset()
        {
            var buffer = GetProcessMemory();
            IntPtr patternAddress = FindPatternEx(ref buffer, _baseAddr, _moduleSize,
                "\xC7\x05\x00\x00\x00\x00\x00\x00\x00\x00\x48\x85\xC0\x0F\x84\x00\x00\x00\x00\x83\x78\x5C\x00\x0F\x84\x00\x00\x00\x00\x33\xD2\x41",
                "xx????xxxxxxxxx????xxxxxx????xxx");
            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, -4);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info("We failed to read the process memory");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - _baseAddr.ToInt64();
            return IntPtr.Add(_baseAddr, (int)(delta + offsetAddressToInt));
        }
        public IntPtr GetGameIPOffset()
        {
            var buffer = GetProcessMemory();
            IntPtr patternAddress = FindPatternEx(ref buffer, _baseAddr, _moduleSize,
                "\x48\x8D\x0D\x00\x00\x00\x00\x44\x88\x2D", 
                "xxx????xxx");
            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 3);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info("We failed to read the process memory");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - _baseAddr.ToInt64();
            return IntPtr.Add(_baseAddr, (int)(delta + 7 + 208 + offsetAddressToInt));
        }
        public IntPtr GetMenuOpenOffset()
        {
            var buffer = GetProcessMemory();
            IntPtr patternAddress = FindPatternEx(ref buffer, _baseAddr, _moduleSize,
                "\x8B\x05\x00\x00\x00\x00\x89\x44\x24\x20\x74\x07",
                "xx????xxxxxx");
            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 2);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info("We failed to read the process memory");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - _baseAddr.ToInt64();
            return IntPtr.Add(_baseAddr, (int)(delta + 6 + offsetAddressToInt));
        }
        public IntPtr GetMenuDataOffset()
        {
            var buffer = GetProcessMemory();
            IntPtr patternAddress = FindPatternEx(ref buffer, _baseAddr, _moduleSize,
                "\x0f\x84\x00\x00\x00\x00\xff\x05\x00\x00\x00\x00\x48\x8b",
                "xx????xx????xx");
            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 8);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info("We failed to read the process memory");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - _baseAddr.ToInt64();
            return IntPtr.Add(_baseAddr, (int)(delta + 12 + offsetAddressToInt));
        }

        private static int FindPattern(ref byte[] buffer, ref int size, ref string pattern, ref string mask)
        {
            var patternLength = pattern.Length;
            for (var i = 0; i < size - patternLength; i++)
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
                    return i;
                }
            }

            return 0;
        }

        private byte[] GetProcessMemory()
        {
            var memoryBuffer = new byte[_moduleSize];
            if (WindowsExternal.ReadProcessMemory(_handle, _baseAddr, memoryBuffer, _moduleSize, out _) == false)
            {
                _log.Info("We failed to read the process memory");
                return null;
            }

            return memoryBuffer;
        }

        private IntPtr FindPatternEx(ref byte[] buffer, IntPtr baseAddr, int size, string pattern, string mask)
        {
            var offset = FindPattern(ref buffer, ref size, ref pattern, ref mask);
            return offset != 0 ? IntPtr.Add(baseAddr, offset) : IntPtr.Zero;
        }

        private IntPtr FindPatternEx(IntPtr baseAddr, int size, string pattern, string mask)
        {
            var buffer = new byte[size];
            if (!WindowsExternal.ReadProcessMemory(_handle, baseAddr, buffer, size, out _))
            {
                return IntPtr.Zero;
            }

            var offset = FindPattern(ref buffer, ref size, ref pattern, ref mask);
            return offset != 0 ? IntPtr.Add(baseAddr, offset) : IntPtr.Zero;
        }

        public int ProcessId => _process.Id;
    }
}
