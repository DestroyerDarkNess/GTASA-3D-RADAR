using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.D3D9;
using Hexa.NET.ImGui.Backends.Win32;
using RenderSpy.Graphics.d3d9;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using ImGui = Hexa.NET.ImGui.ImGui;

namespace Samp.Radar.Backends
{
    public sealed unsafe class D3D9Backend : ImGuiBackendBase
    {
        private IDirect3DDevice9Ptr _devicePtr;
        private bool _frameStarted = false;
        private string _tmpDir, _cimguiPath, _implPath;
        private IntPtr _cimguiHandle = IntPtr.Zero, _implHandle = IntPtr.Zero, _windowHandle = IntPtr.Zero;

        public override bool Initialize(IntPtr devicePtr, IntPtr windowHandle)
        {
            if (IsInitialized) return true;

            //2 - ImGui Context
            Context = ImGui.CreateContext();
            ImGui.SetCurrentContext(Context);
            IO = ImGui.GetIO();

            var io = IO;
            io.IniFilename = null;

            RaiseOnCreateContext();

            // Win32 backend
            ImGuiImplWin32.SetCurrentContext(Context);
            ImGuiImplWin32.Init(windowHandle);

            // D3D9 backend
            _devicePtr = new IDirect3DDevice9Ptr((IDirect3DDevice9*)devicePtr);
            ImGuiImplD3D9.SetCurrentContext(Context);
            if (!ImGuiImplD3D9.Init(_devicePtr))
                return false;

            IsInitialized = true;

            return IsInitialized;
        }

        public override void NewFrame()
        {
            if (!IsInitialized) return;

            if (_frameStarted) return;

            ImGuiImplWin32.NewFrame();
            ImGuiImplD3D9.NewFrame();
            ImGui.NewFrame();

            _frameStarted = true;
        }

        public override void Render()
        {
            if (!IsInitialized) return;

            if (!_frameStarted) return;

            RaiseRender();

            ImGui.Render();

            if ((IO.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
            {
                ImGui.UpdatePlatformWindows();
                ImGui.RenderPlatformWindowsDefault();
            }

            ImGuiImplD3D9.RenderDrawData(ImGui.GetDrawData());

            _frameStarted = false;
        }

        public override void OnLostDevice()
        {
            if (!IsInitialized) return;
            ImGuiImplD3D9.InvalidateDeviceObjects();
        }

        public override void OnResetDevice()
        {
            if (!IsInitialized) return;
            ImGuiImplD3D9.CreateDeviceObjects();
        }

        public override void Dispose()
        {
            if (!IsInitialized) return;

            ImGuiImplD3D9.Shutdown();
            ImGuiImplWin32.Shutdown();
            ImGui.DestroyContext(Context);

            void FreeIfLoaded(ref IntPtr handle)
            {
                if (handle != IntPtr.Zero)
                {
                    RenderSpy.Globals.WinApi.FreeLibrary(handle);
                    handle = IntPtr.Zero;
                }
            }

            FreeIfLoaded(ref _implHandle);
            FreeIfLoaded(ref _cimguiHandle);

            try
            {
                if (Directory.Exists(_tmpDir))
                    Directory.Delete(_tmpDir, true);
            }
            catch { /* Swallow – directorio temp */ }

            IsInitialized = false;
        }
    }
}