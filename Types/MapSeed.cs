using MapAssist.Helpers;
using MapAssist.Interfaces;
using System;
using System.Runtime.InteropServices;

namespace MapAssist.Types
{
    public class MapSeed : IUpdatable<MapSeed>
    {
        private readonly ulong _seedHash;
        public uint Seed { get; private set; }

        public MapSeed(ulong SeedHash)
        {
            _seedHash = SeedHash;
            Update();
        }

        public MapSeed Update()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                try
                {
                    var ldr = GetLdrAddress();
                    var dwInitSeedHash = _seedHash;
                    var k0 = 0xB133105CF0F31CD6 ^ 0x770AFD680A3D2D6D;
                    var k1 = 0x176E2BC088CB2DA9ul ^ (ulong)ldr.ToInt64() ^ 0xA23D40A5FD70B4A4;
                    var t1 = IntPtr.Add(processContext.BaseAddr, (int)(0x20DEA40 + (k0 & 0xFFF)));
                    var t2 = IntPtr.Add(processContext.BaseAddr, (int)(0x20DEA40 + (k0 >> 52)));
                    var k2 = processContext.Read<ulong>(t1);
                    var k3 = processContext.Read<ulong>(t2);
                    var seed = k1 ^ dwInitSeedHash | (k1 ^ dwInitSeedHash | (k1 ^ (dwInitSeedHash | (dwInitSeedHash | dwInitSeedHash & 0xFFFFFFFF00000000 ^ ((dwInitSeedHash ^ ~HiDWord(k2)) << 32)) & 0xFFFFFFFF00000000 ^ ((dwInitSeedHash ^ 0x7734D256) << 32))) & 0xFFFFFFFF00000000 ^ ((ror4(HiDWord(k3), 11) ^ ~(k1 ^ dwInitSeedHash)) << 32)) & 0xFFFFFFFF00000000 ^ ((k1 ^ dwInitSeedHash ^ ~k2) << 32);

                    Seed = (uint)(int)seed;
                }
                catch (Exception) { }
            }
            return this;
        }

        public static IntPtr GetLdrAddress()
        {
            using (var processContext = GameManager.GetProcessContext())
            {
                IntPtr hProc = WindowsExternal.OpenProcess(0x001F0FFF, false, processContext.ProcessId);
                //Allocate memory for a new PROCESS_BASIC_INFORMATION structure
                IntPtr pbi = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION)));
                //Allocate memory for a long
                IntPtr outLong = Marshal.AllocHGlobal(sizeof(long));
                IntPtr outPtr = IntPtr.Zero;

                //Store API call success in a boolean
                var queryStatus = NtQueryInformationProcess(hProc, 0, pbi, (uint)Marshal.SizeOf(typeof(PROCESS_BASIC_INFORMATION)), outLong);

                //STATUS_SUCCESS = 0, so if API call was successful querySuccess should contain 0 ergo we reverse the check.
                if (queryStatus == 0)
                {
                    var info = (PROCESS_BASIC_INFORMATION)Marshal.PtrToStructure(pbi, typeof(PROCESS_BASIC_INFORMATION));
                    var pebPtr = info.PebBaseAddress;

                    var peb = processContext.Read<PEB_32>(pebPtr);
                    outPtr = peb.Ldr;
                }

                //Free allocated space
                Marshal.FreeHGlobal(pbi);

                //Return pointer to PEB base address
                return outPtr;
            }
        }

        private static uint HiDWord(ulong number)
        {
            return (uint)(number >> 32);
        }

        private static uint ror4(uint value, int count)
        {
            var nbits = sizeof(uint) * 8;

            count = -count % nbits;
            var low = value << (nbits - count);
            value >>= count;
            value |= low;

            return value;
        }

        [DllImport("ntdll.dll", SetLastError = true)]
        static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, IntPtr processInformation, uint processInformationLength, IntPtr returnLength);


        private struct PROCESS_BASIC_INFORMATION
        {
            public uint ExitStatus;
            public IntPtr PebBaseAddress;
            public UIntPtr AffinityMask;
            public int BasePriority;
            public UIntPtr UniqueProcessId;
            public UIntPtr InheritedFromUniqueProcessId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public partial struct PEB_32
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] Reserved1;
            public byte BeingDebugged;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public byte[] Reserved2;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public IntPtr[] Reserved3;
            public IntPtr Ldr;
            public IntPtr ProcessParameters;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public IntPtr[] Reserved4;
            public IntPtr AtlThunkSListPtr;
            public IntPtr Reserved5;
            public uint Reserved6;
            public IntPtr Reserved7;
            public uint Reserved8;
            public uint AtlThunkSListPtr32;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 45)]
            public IntPtr[] Reserved9;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)]
            public byte[] Reserved10;
            public IntPtr PostProcessInitRoutine;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            public byte[] Reserved11;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public IntPtr[] Reserved12;
            public uint SessionId;
        }
    }
}
