using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Samp.Radar.Core
{
    public static class OverlayStyleManager
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const int GWL_EXSTYLE = -20;
        private const int GWL_STYLE = -16;

        // Estilos Extended
        private const int WS_EX_LAYERED = 0x00080000;

        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOPMOST = 0x00000008;

        // Estilos normales
        private const int WS_POPUP = unchecked((int)0x80000000);

        private const int WS_VISIBLE = 0x10000000;
        private const int WS_CLIPSIBLINGS = 0x04000000;
        private const int WS_CLIPCHILDREN = 0x02000000;

        // SetWindowPos flags
        private const uint SWP_NOMOVE = 0x0002;

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;

        public static void ConfigureOverlayWindow(IntPtr hwnd)
        {
            Console.WriteLine("[ConfigureOverlayWindow] Configurando estilos de overlay...");

            // Obtener estilos actuales
            int currentStyle = RenderSpy.Globals.WinApi.GetWindowLongPtr(hwnd, GWL_STYLE).ToInt32();
            int currentExStyle = RenderSpy.Globals.WinApi.GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt32();

            Console.WriteLine($"[ConfigureOverlayWindow] Style actual: {currentStyle:X}");
            Console.WriteLine($"[ConfigureOverlayWindow] ExStyle actual: {currentExStyle:X}");

            // Configurar estilo normal (popup window)
            int newStyle = WS_POPUP | WS_VISIBLE | WS_CLIPSIBLINGS | WS_CLIPCHILDREN;
            RenderSpy.Globals.WinApi.SetWindowLongPtr(hwnd, GWL_STYLE, (IntPtr)newStyle);

            //// Configurar estilos extendidos
            //int newExStyle = WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TRANSPARENT;
            //RenderSpy.Globals.WinApi.SetWindowLongPtr(hwnd, GWL_EXSTYLE, (IntPtr)newExStyle);

            //// Aplicar cambios
            //SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
            //    SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

            //// Verificar que se aplicaron
            //int finalStyle = RenderSpy.Globals.WinApi.GetWindowLongPtr(hwnd, GWL_STYLE).ToInt32();
            //int finalExStyle = RenderSpy.Globals.WinApi.GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt32();

            //Console.WriteLine($"[ConfigureOverlayWindow] Style final: {finalStyle:X}");
            //Console.WriteLine($"[ConfigureOverlayWindow] ExStyle final: {finalExStyle:X}");
            //Console.WriteLine($"[ConfigureOverlayWindow] Es transparente: {(finalExStyle & WS_EX_TRANSPARENT) != 0}");
        }

        public static void SetInteractive(IntPtr hwnd, bool interactive)
        {
            int currentExStyle = RenderSpy.Globals.WinApi.GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt32();
            int newExStyle;

            if (interactive)
            {
                // REMOVER WS_EX_TRANSPARENT para hacer interactivo
                newExStyle = currentExStyle & ~WS_EX_TRANSPARENT;
            }
            else
            {
                // AGREGAR WS_EX_TRANSPARENT para hacer transparente
                newExStyle = currentExStyle | WS_EX_TRANSPARENT;
            }

            RenderSpy.Globals.WinApi.SetWindowLongPtr(hwnd, GWL_EXSTYLE, (IntPtr)newExStyle);
        }
    }
}