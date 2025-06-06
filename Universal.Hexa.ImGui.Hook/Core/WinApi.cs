using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Samp.Radar.Core
{
    internal static class WinApi
    {
        [DllImport("user32.dll")] public static extern int ShowCursor(bool show);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeFileHandle CreateFile(
            string lpFileName,
            FileAccess dwDesiredAccess,
            FileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            CreationDisposition dwCreationDisposition,
            FileFlags dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process(IntPtr hProcess, out bool isWow64);

        [Flags]
        internal enum FileAccess : uint
        {
            GenericRead = 0x80000000,
            GenericWrite = 0x40000000,
            GenericExecute = 0x20000000,
            GenericAll = 0x10000000
        }

        [Flags]
        internal enum FileShare : uint
        {
            None = 0x0,
            Read = 0x1,
            Write = 0x2,
            Delete = 0x4
        }

        internal enum CreationDisposition : uint
        {
            New = 1,
            CreateAlways = 2,
            OpenExisting = 3,
            OpenAlways = 4,
            TruncateExisting = 5
        }

        [Flags]
        internal enum FileFlags : uint
        {
            FileFlagDeleteOnClose = 0x04000000,
            FileFlagSequentialScan = 0x08000000,
            FileAttributeNormal = 0x00000080
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;

        public static void Interactive(IntPtr hwnd, bool status)
        {
            int extendedStyle = (int)RenderSpy.Globals.WinApi.GetWindowLongPtr(hwnd, GWL_EXSTYLE);

            if (status)
            {
                RenderSpy.Globals.WinApi.SetWindowLongPtr(hwnd, GWL_EXSTYLE, (IntPtr)(extendedStyle & ~WS_EX_TRANSPARENT));
            }
            else
            {
                RenderSpy.Globals.WinApi.SetWindowLongPtr(hwnd, GWL_EXSTYLE, (IntPtr)(extendedStyle | WS_EX_TRANSPARENT));
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private const uint LWA_COLORKEY = 0x1;
        private const uint LWA_ALPHA = 0x2;

        public static void EnableTransparency(IntPtr hwnd, byte alpha = 255)
        {
            SetLayeredWindowAttributes(hwnd, 0, alpha, LWA_ALPHA);
        }
    }
}