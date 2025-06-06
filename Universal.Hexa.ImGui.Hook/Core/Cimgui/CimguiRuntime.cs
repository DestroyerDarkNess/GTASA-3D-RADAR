using System;

namespace Samp.Radar.Core.Cimgui
{
    internal static class CimguiRuntime
    {
        public static IntPtr Handle => _handle.Value;

        private static readonly Lazy<IntPtr> _handle = new Lazy<IntPtr>(() =>
        {
            try
            {
                return NativeLibLoader.LoadCimgui();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CimguiRuntime] Error cargando cimgui.dll: {ex}");
                throw;
            }
        }, isThreadSafe: true);
    }
}