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
            _handle = WindowsExternal.OpenProcess((uint)WindowsExternal.ProcessAccessFlags.VirtualMemoryRead, false, _process.Id);
            _baseAddr = _process.MainModule.BaseAddress;
            _moduleSize = _process.MainModule.ModuleMemorySize;
        }

        public IntPtr Handle { get => _handle; }
        public IntPtr BaseAddr { get => _baseAddr; }
        public int ModuleSize { get => _moduleSize; }
        public int ProcessId { get => _process.Id; }

        public IntPtr GetUnitHashtableOffset(byte[] buffer)
        {
            var pattern = new Pattern("48 8D 0D ? ? ? ? 48 C1 E0 0A 48 03 C1 C3 CC");
            var patternAddress = FindPattern(buffer, pattern);

            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 3);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info($"Failed to find pattern {pattern}");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - _baseAddr.ToInt64();
            return IntPtr.Add(_baseAddr, (int)(delta + 7 + offsetAddressToInt));
        }

        public IntPtr GetExpansionOffset(byte[] buffer)
        {
            var pattern = new Pattern("48 8B 05 ? ? ? ? 48 8B D9 F3 0F 10 50");
            var patternAddress = FindPattern(buffer, pattern);
            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 3);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info($"Failed to find pattern {pattern}");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - _baseAddr.ToInt64();
            return IntPtr.Add(_baseAddr, (int)(delta + 7 + offsetAddressToInt));
        }

        public IntPtr GetGameNameOffset(byte[] buffer)
        {
            var pattern = new Pattern("44 88 25 ? ? ? ? 66 44 89 25");
            var patternAddress = FindPattern(buffer, pattern);

            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 3);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info($"Failed to find pattern {pattern}");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - _baseAddr.ToInt64();
            return IntPtr.Add(_baseAddr, (int)(delta - 0x121 + offsetAddressToInt));
        }

        public IntPtr GetMenuOpenOffset(byte[] buffer)
        {
            var pattern = new Pattern("8B 05 ? ? ? ? 89 44 24 20 74 07");
            var patternAddress = FindPattern(buffer, pattern);

            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 2);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info($"Failed to find pattern {pattern}");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - _baseAddr.ToInt64();
            return IntPtr.Add(_baseAddr, (int)(delta + 6 + offsetAddressToInt));
        }

        public IntPtr GetMenuDataOffset(byte[] buffer)
        {
            var pattern = new Pattern("45 8B D7 4C 8D 05 ? ? ? ?");
            var patternAddress = FindPattern(buffer, pattern);

            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 6);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info($"Failed to find pattern {pattern}");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - _baseAddr.ToInt64();
            return IntPtr.Add(_baseAddr, (int)(delta + 10 + offsetAddressToInt));
        }

        public IntPtr GetMapSeedOffset(byte[] buffer)
        {
            var pattern = new Pattern("41 8B F9 48 8D 0D ? ? ? ?");
            var patternAddress = FindPattern(buffer, pattern);

            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 6);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info($"Failed to find pattern {pattern}");
                return IntPtr.Zero;
            }

            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - _baseAddr.ToInt64();
            return IntPtr.Add(_baseAddr, (int)(delta + 0xEA + offsetAddressToInt));
        }

        public IntPtr GetRosterDataOffset(byte[] buffer)
        {
            var pattern = new Pattern("02 45 33 D2 4D 8B");
            var patternAddress = FindPattern(buffer, pattern);

            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, -3);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info($"Failed to find pattern {pattern}");
                return IntPtr.Zero;
            }
            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            var delta = patternAddress.ToInt64() - _baseAddr.ToInt64();
            return IntPtr.Add(_baseAddr, (int)(delta + 1 + offsetAddressToInt));
        }

        public IntPtr GetInteractedNpcOffset(byte[] buffer)
        {
            var pattern = new Pattern("43 01 84 31 ? ? ? ?");
            var patternAddress = FindPattern(buffer, pattern);

            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 4);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info($"Failed to find pattern {pattern}");
                return IntPtr.Zero;
            }
            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0);
            return IntPtr.Add(_baseAddr, (int)(offsetAddressToInt + 0x1D4));
        }

        public IntPtr GetLastHoverObjectOffset(byte[] buffer)
        {
            var pattern = new Pattern("C6 84 C2 ? ? ? ? ? 48 8B 74 24");
            IntPtr patternAddress = FindPattern(buffer, pattern);

            var offsetBuffer = new byte[4];
            var resultRelativeAddress = IntPtr.Add(patternAddress, 3);
            if (!WindowsExternal.ReadProcessMemory(_handle, resultRelativeAddress, offsetBuffer, sizeof(int), out _))
            {
                _log.Info($"Failed to find pattern {pattern}");
                return IntPtr.Zero;
            }
            var offsetAddressToInt = BitConverter.ToInt32(offsetBuffer, 0) - 1;
            return IntPtr.Add(_baseAddr, offsetAddressToInt);
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

                // Optimisation when reading byte sized things.
                if (sz == 1)
                {
                    Buffer.BlockCopy(buf, 0, result, 0, buf.Length);
                    return result;
                }

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

        public IntPtr FindPattern(byte[] buffer, Pattern pattern)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                if (pattern.Match(buffer, i))
                {
                    return IntPtr.Add(_baseAddr, i);
                }
            }
            return IntPtr.Zero;
        }

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
