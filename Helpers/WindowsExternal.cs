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
using System.Runtime.InteropServices;

namespace MapAssist.Helpers
{
    public static class WindowsExternal
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
            uint processAccess,
            bool bInheritHandle,
            int processId
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(
            IntPtr hObject
        );

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead);

        // This helper static method is required because the 32-bit version of user32.dll does not contain this API
        // (on any versions of Windows), so linking the method will fail at run-time. The bridge dispatches the request
        // to the correct function (GetWindowLong in 32-bit mode and GetWindowLongPtr in 64-bit mode)

        // If that doesn't work, the following signature can be used alternatively.
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        // This static method is required because Win32 does not support
        // GetWindowLongPtr directly
        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return GetWindowLongPtr32(hWnd, nIndex);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy,
            uint uFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
    }
    
    //SigScan
    public class SigScanSharp
    {
        private IntPtr g_hProcess { get; set; }
        private byte[] g_arrModuleBuffer { get; set; }
        private ulong g_lpModuleBase { get; set; }

        private System.Collections.Generic.Dictionary<string, string> g_dictStringPatterns { get; }

        public SigScanSharp(IntPtr hProc)
        {
            g_hProcess = hProc;
            g_dictStringPatterns = new System.Collections.Generic.Dictionary<string, string>();
        }

        public bool SelectModule(System.Diagnostics.ProcessModule targetModule)
        {
            g_lpModuleBase = (ulong)targetModule.BaseAddress;
            g_arrModuleBuffer = new byte[targetModule.ModuleMemorySize];

            g_dictStringPatterns.Clear();

            return Win32.ReadProcessMemory(g_hProcess, g_lpModuleBase, g_arrModuleBuffer, targetModule.ModuleMemorySize);
        }

        public void AddPattern(string szPatternName, string szPattern)
        {
            g_dictStringPatterns.Add(szPatternName, szPattern);
        }

        private bool PatternCheck(int nOffset, byte[] arrPattern)
        {
            for (int i = 0; i < arrPattern.Length; i++)
            {
                if (arrPattern[i] == 0x0)
                    continue;

                if (arrPattern[i] != g_arrModuleBuffer[nOffset + i])
                    return false;
            }

            return true;
        }

        public ulong FindPattern(string szPattern, out long lTime)
        {
            if (g_arrModuleBuffer == null || g_lpModuleBase == 0)
                throw new Exception("Selected module is null");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            byte[] arrPattern = ParsePatternString(szPattern);

            for (int nModuleIndex = 0; nModuleIndex < g_arrModuleBuffer.Length; nModuleIndex++)
            {
                if (g_arrModuleBuffer[nModuleIndex] != arrPattern[0])
                    continue;

                if (PatternCheck(nModuleIndex, arrPattern))
                {
                    lTime = stopwatch.ElapsedMilliseconds;
                    return g_lpModuleBase + (ulong)nModuleIndex;
                }
            }

            lTime = stopwatch.ElapsedMilliseconds;
            return 0;
        }
        public System.Collections.Generic.Dictionary<string, ulong> FindPatterns(out long lTime)
        {
            if (g_arrModuleBuffer == null || g_lpModuleBase == 0)
                throw new Exception("Selected module is null");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            byte[][] arrBytePatterns = new byte[g_dictStringPatterns.Count][];
            ulong[] arrResult = new ulong[g_dictStringPatterns.Count];

            // PARSE PATTERNS
            for (int nIndex = 0; nIndex < g_dictStringPatterns.Count; nIndex++)
                arrBytePatterns[nIndex] = ParsePatternString(g_dictStringPatterns.ElementAt(nIndex).Value);

            // SCAN FOR PATTERNS
            for (int nModuleIndex = 0; nModuleIndex < g_arrModuleBuffer.Length; nModuleIndex++)
            {
                for (int nPatternIndex = 0; nPatternIndex < arrBytePatterns.Length; nPatternIndex++)
                {
                    if (arrResult[nPatternIndex] != 0)
                        continue;

                    if (PatternCheck(nModuleIndex, arrBytePatterns[nPatternIndex]))
                        arrResult[nPatternIndex] = g_lpModuleBase + (ulong)nModuleIndex;
                }
            }

            var dictResultFormatted = new System.Collections.Generic.Dictionary<string, ulong>();

            // FORMAT PATTERNS
            for (int nPatternIndex = 0; nPatternIndex < arrBytePatterns.Length; nPatternIndex++)
                dictResultFormatted[g_dictStringPatterns.ElementAt(nPatternIndex).Key] = arrResult[nPatternIndex];

            lTime = stopwatch.ElapsedMilliseconds;
            return dictResultFormatted;
        }

        private byte[] ParsePatternString(string szPattern)
        {
            var patternbytes = new System.Collections.Generic.List<byte>();

            foreach (var szByte in szPattern.Split(' '))
                patternbytes.Add(szByte == "?" ? (byte)0x0 : Convert.ToByte(szByte, 16));

            return patternbytes.ToArray();
        }

        private static class Win32
        {
            [DllImport("kernel32.dll")]
            public static extern bool ReadProcessMemory(IntPtr hProcess, ulong lpBaseAddress, byte[] lpBuffer, int dwSize, int lpNumberOfBytesRead = 0);
        }
    }
}
