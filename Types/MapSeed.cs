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



                    var qword_7FF7328DEA40 = 0x0C727A64824745172ul;
                    var a5 = 0xB133105CF0F31CD6ul;
                    var a6 = 0x176E2BC088CB2DA9ul;
                    var a8 = dwInitSeedHash; // 0x6AC690C5 * dwInitSeedHash + 666;

                    a5 ^= 0x770AFD680A3D2D6D;
                    a6 = a6 ^ (ulong)ldr.ToInt64() ^ 0xA23D40A5FD70B4A4;

                    var asdf1 = processContext.Read<ulong>(IntPtr.Add(processContext.BaseAddr, (int)(0x20DEA40 + (a5 >> 52))));
                    var asdf2 = processContext.Read<ulong>(IntPtr.Add(processContext.BaseAddr, (int)(0x20DEA40 + (a5 & 0xFFF))));

                    var seed = a6 ^ dwInitSeedHash | (a6 ^ dwInitSeedHash | (a6 ^ (dwInitSeedHash | (dwInitSeedHash | a8 & 0xFFFFFFFF00000000 ^ ((dwInitSeedHash ^ ~HiDWord(asdf1)) << 32)) & 0xFFFFFFFF00000000 ^ ((dwInitSeedHash ^ 0x7734D256) << 32))) & 0xFFFFFFFF00000000 ^ ((ror4(HiDWord(asdf2), 11) ^ ~(a6 ^ dwInitSeedHash)) << 32)) & 0xFFFFFFFF00000000 ^ ((a6 ^ dwInitSeedHash ^ ~asdf1) << 32);



                    Seed = (uint)(int)seed;

                    var sdfsfd = 1;
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
