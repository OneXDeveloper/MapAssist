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
        private Process _process;
        private IntPtr _handle;
        private bool _disposedValue;
        public int OpenContextCount = 1;

        public ProcessContext(Process process)
        {
            _process = process;
            _handle = WindowsExternal.OpenProcess((uint)WindowsExternal.ProcessAccessFlags.VirtualMemoryRead, false, process.Id);
        }

        public IntPtr Handle { get => _handle; }

        public IntPtr FromOffset(int offset)
        {
            IntPtr processAddress = _process.MainModule.BaseAddress;
            return IntPtr.Add(processAddress, offset);
        }

        public T[] Read<T>(IntPtr address, int count) where T : struct
        {
            var sz = Marshal.SizeOf<T>();
            var buf = new byte[sz * count];
            var handle = GCHandle.Alloc(buf, GCHandleType.Pinned);
            try
            {
                IntPtr processAddress = _process.MainModule.BaseAddress;
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
    }
}
