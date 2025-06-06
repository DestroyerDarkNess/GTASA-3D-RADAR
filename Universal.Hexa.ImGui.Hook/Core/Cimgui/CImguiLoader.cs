using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Samp.Radar.Core;

namespace Samp.Radar.Core.Cimgui
{
    internal static class NativeLibLoader
    {
        private static readonly byte[] CimguiX86 = Properties.Resources.win_x86_cimgui;
        private static readonly byte[] CimguiX64 = Properties.Resources.win_x64_cimgui;

        private static readonly byte[] Backend_imguiX86 = Properties.Resources.win_x86_ImGuiImpl;
        private static readonly byte[] Backend_CimguiX64 = Properties.Resources.win_x64_ImGuiImpl;

        private static readonly List<TempItem> _items = new List<TempItem>();

        public static IntPtr LoadCimgui()
        {
            IntPtr already = WinApi.GetModuleHandle("cimgui.dll");
            if (already != IntPtr.Zero) return already;

            bool is64 = IntPtr.Size == 8;

            byte[] dllBytes = is64 ? CimguiX64 : CimguiX86;
            byte[] dllBytesImpl = is64 ? Backend_CimguiX64 : Backend_imguiX86;

            string tempDir = Path.Combine(Path.GetTempPath(),
                                           $"cimgui_{Process.GetCurrentProcess().Id}");

            if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

            string tempPath = Path.Combine(tempDir, "cimgui.dll");

            try { if (File.Exists(tempPath)) File.Delete(tempPath); File.WriteAllBytes(tempPath, dllBytes); } catch { /* ignore */ }

            string tempPathImpl = Path.Combine(tempDir, "ImGuiImpl.dll");

            try { if (File.Exists(tempPathImpl)) File.Delete(tempPathImpl); File.WriteAllBytes(tempPathImpl, dllBytesImpl); } catch { /* ignore */ }

            //var hFile = WinApi.CreateFile(
            //    tempPath,
            //    WinApi.FileAccess.GenericRead,                   // solo lectura
            //    WinApi.FileShare.Read | WinApi.FileShare.Write | // permite que Loader abra el fichero
            //    WinApi.FileShare.Delete,
            //    IntPtr.Zero,
            //    WinApi.CreationDisposition.OpenExisting,
            //    WinApi.FileFlags.FileFlagDeleteOnClose,
            //    IntPtr.Zero);

            //if (hFile.IsInvalid)
            //    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

            IntPtr hMod = WinApi.LoadLibrary(tempPath);
            if (hMod == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

            IntPtr hModImp = WinApi.LoadLibrary(tempPathImpl);
            if (hModImp == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

            //_items.Add(new TempItem(hFile, hMod, tempDir));
            return hMod;
        }

        public static void FreeAll()
        {
            foreach (var item in _items)
            {
                WinApi.FreeLibrary(item.ModuleHandle);
                item.FileHandle.Dispose();            // ➜ borra cimgui.dll

                try { Directory.Delete(item.TempDir, false); }
                catch { /* carpeta en uso o ya eliminada */ }
            }
            _items.Clear();
        }

        private sealed class TempItem
        {
            public SafeFileHandle FileHandle { get; }
            public IntPtr ModuleHandle { get; }
            public string TempDir { get; }

            public TempItem(SafeFileHandle fh, IntPtr mod, string dir)
            {
                FileHandle = fh;
                ModuleHandle = mod;
                TempDir = dir;
            }
        }
    }
}