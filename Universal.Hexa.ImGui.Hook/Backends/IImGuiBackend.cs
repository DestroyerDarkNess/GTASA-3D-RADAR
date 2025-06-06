using System;
using Hexa.NET.ImGui;

namespace Samp.Radar.Backends
{
    /// <summary>
    /// Delegate que el usuario registra para dibujar su UI.
    /// </summary>
    public delegate void ImGuiRenderDelegate();

    /// <summary>
    /// Delegate que el usuario registra para dibujar su UI.
    /// </summary>
    public delegate void ImGuiOnContextDelegate();

    /// <summary>
    /// Todos los back-ends deben exponer este contrato.
    /// </summary>
    public interface IImGuiBackend : IDisposable
    {
        ImGuiContextPtr Context { get; }
        ImGuiIOPtr IO { get; }
        bool IsInitialized { get; }

        /// <summary>Inicializa el back-end con los manejadores nativos necesarios.</summary>
        bool Initialize(IntPtr devicePtr, IntPtr windowHandle);

        /// <summary>Llamado una vez por frame antes de que el juego llame a Present / SwapBuffers.</summary>
        void NewFrame();

        /// <summary>Llamado inmediatamente antes de Present / SwapBuffers para emitir el draw-list.</summary>
        void Render();

        /// <summary>Llamado justo antes de un Reset / DeviceLost.</summary>
        void OnLostDevice();

        /// <summary>Llamado después de un Reset / DeviceReset.</summary>
        void OnResetDevice();

        /// <summary>Evento para que el usuario dibuje su UI.</summary>
        event ImGuiRenderDelegate OnImGuiRender;

        /// <summary>Evento para que el usuario cree su contexto.</summary>
        event ImGuiOnContextDelegate OnImGuiCreateContext;
    }

    /// <summary>
    /// Factor común entre back-ends: contexto, IO y disparo del evento de renderizado.
    /// </summary>
    public abstract class ImGuiBackendBase : IImGuiBackend
    {
        public ImGuiContextPtr Context { get; protected set; }
        public ImGuiIOPtr IO { get; protected set; }

        public bool IsInitialized { get; protected set; }

        public event ImGuiRenderDelegate OnImGuiRender = null;

        public event ImGuiOnContextDelegate OnImGuiCreateContext = null;

        protected void RaiseRender()
        { if (OnImGuiRender != null) OnImGuiRender.Invoke(); }

        protected void RaiseOnCreateContext()
        { if (OnImGuiCreateContext != null) OnImGuiCreateContext.Invoke(); }

        public abstract bool Initialize(IntPtr devicePtr, IntPtr windowHandle);

        public abstract void NewFrame();

        public abstract void Render();

        public abstract void OnLostDevice();

        public abstract void OnResetDevice();

        public abstract void Dispose();
    }
}