using Hexa.NET.ImGui;
using RenderSpy.Inputs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Samp.Radar.Backends;
using Samp.Radar.Core;
using Samp.Radar.Core.Theme;
using Samp.Radar.SDK;
using Samp.Radar.SDK.Radar3D;
using System.Runtime;
using System.Reflection;
using System.Security.Claims;

namespace Samp.Radar
{
    public class dllmain
    {
        public static IntPtr GameHandle = IntPtr.Zero;
        public static bool Show = false;
        public static bool Logger = false;
        public static bool Runtime = true;
        public static Size Gui_Size = new System.Drawing.Size(800, 600);

        // Settings
        public static SDK.ConfigManager Settings = SDK.ConfigManager.LoadConfig();

        public static string fileVer = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;

        // ImGui Backend
        public static IImGuiBackend ImGuiBackend = null;

        public static InputImguiEmu InputImguiEmu = null;
        public static Radar3D radar = null;

        public static void EntryPoint()
        {
            while (GameHandle.ToInt32() == 0)
            {
                GameHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;  // Get Main Game Window Handle
            }

            Logger = Settings.DebugEnabled;

            if (Logger == true)
            {
                RenderSpy.Globals.WinApi.AllocConsole();
                bool result = Diagnostic.RunDiagnostic();

                if (result)
                {
                    Console.WriteLine("All diagnostics passed. The system is ready.");
                }
                else
                {
                    Console.WriteLine("Some diagnostics failed. Please resolve the missing libraries, Press any key to continue.");
                    Console.ReadKey();
                }
            }

            try
            {
                var A = Core.Cimgui.CimguiRuntime.Handle;
            }
            catch
            {
                return;
            }

            var radar_config = new Radar3DSettings();

            radar_config.PolyCount = Settings.PolyCount;

            radar_config.SkipFactor = Settings.SkipFactor;

            radar_config.ZScaleDown = Settings.ZScaleDown;

            radar_config.Scale = Settings.Scale;

            radar_config.Offset = Settings.Offset;

            radar_config.UpdateRate = Settings.UpdateRate;

            radar_config.BaseColor = Settings.BaseColor;
            radar_config.DirectionColor = Settings.DirectionColor;
            radar_config.CenterColor = Settings.CenterColor;
            radar_config.MainColor = Settings.MainColor;

            radar = new Radar3D(radar_config);
            radar.LoadHeightMap();

            RenderSpy.Graphics.d3d9.Present PresentHook_9 = new RenderSpy.Graphics.d3d9.Present();
            PresentHook_9.Install();
            PresentHook_9.PresentEvent += (IntPtr device, IntPtr sourceRect, IntPtr destRect, IntPtr hDestWindowOverride, IntPtr dirtyRegion) =>
            {
                try
                {
                    if (ImGuiBackend == null)
                    {
                        ImGuiBackend = new D3D9Backend();
                        ImGuiBackend.OnImGuiCreateContext += HandleImGuiCreateContext;
                        ImGuiBackend.OnImGuiRender += HandleImGuiRender;

                        ImGuiBackend.Initialize(device, dllmain.GameHandle);
                    }

                    ImGuiBackend.NewFrame();
                    ImGuiBackend.Render();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error in Present Event: {ex.Message} {Environment.NewLine} {Environment.NewLine} {ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                return PresentHook_9.Present_orig(device, sourceRect, destRect, hDestWindowOverride, dirtyRegion);
            };

            RenderSpy.Graphics.d3d9.Reset ResetHook_9 = new RenderSpy.Graphics.d3d9.Reset();
            ResetHook_9.Install();
            ResetHook_9.Reset_Event += (IntPtr device, ref SharpDX.Direct3D9.PresentParameters presentParameters) =>
            {
                if (ImGuiBackend != null) ImGuiBackend.OnLostDevice();

                int Reset = ResetHook_9.Reset_orig(device, ref presentParameters);

                if (ImGuiBackend != null) ImGuiBackend.OnResetDevice();

                return Reset;
            };

            DirectInputHook DirectInputHook_Hook = new DirectInputHook();

            IntPtr already = RenderSpy.Globals.WinApi.GetModuleHandle("dinput8.dll");

            if (already != IntPtr.Zero)
            {
                DirectInputHook_Hook.WindowHandle = GameHandle;
                DirectInputHook_Hook.Install();
                DirectInputHook_Hook.GetDeviceState += (IntPtr hDevice, int cbData, IntPtr lpvData) =>
                {
                    if (Show) return 0;
                    return DirectInputHook_Hook.Hook_orig(hDevice, cbData, lpvData);
                };
            }

            SetCursorPos NewHookCursor = new SetCursorPos();
            NewHookCursor.Install();
            NewHookCursor.SetCursorPos_Event += (int x, int y) =>
            {
                NewHookCursor.BlockInput = Show;
                return false;
            };

            while (Runtime) { }

            if (already != IntPtr.Zero) DirectInputHook_Hook.Uninstall();
            NewHookCursor.Uninstall();
            PresentHook_9.Uninstall();
            ResetHook_9.Uninstall();

            if (ImGuiBackend != null) ImGuiBackend.Dispose();
        }

        public static void HandleImGuiCreateContext()
        {
            Dark.Apply();
            if (radar != null) radar.RadarEnabled = Settings.RadarEnabled;

            InputImguiEmu = new InputImguiEmu(ImGuiBackend.IO);

            // Menu Key
            InputImguiEmu.AddEvent(Keys.F2, () =>
            {
                Show = !Show;

                InputImguiEmu.Enabled = Show;
                ImGuiBackend.IO.MouseDrawCursor = Show;
                Core.WinApi.ShowCursor(Show);

                if (!Show) SDK.ConfigManager.SaveConfig(Settings);
            });

            //Panic Key
            InputImguiEmu.AddEvent(Keys.F3, () =>
            {
                if (!Show) SDK.ConfigManager.SaveConfig(Settings);
                Runtime = false;
            });
        }

        public static void HandleImGuiRender()
        {
            if (!Runtime) return;

            if (SDK.API.IsEscMenuOpen()) return;

            if (SDK.API.IsPlayerInsideInterior()) return;

            if (InputImguiEmu != null) InputImguiEmu.UpdateKeyboardState();

            Vector3 playerPos = Vector3.Zero;
            if (SDK.API.TryGetPlayerPosition(out playerPos))
            {
                radar.PlayerPosition = playerPos;
                radar.PlayerHeading = SDK.API.GetRadarRotation();
            }

            if (Show)
            {
                if (InputImguiEmu != null) InputImguiEmu.UpdateMouseState();

                RenderRadarMenu();
            }

            if (radar != null && radar.RadarEnabled)
            {
                List<PlayerInfo> players = new List<PlayerInfo>();
                players.Add(new PlayerInfo
                {
                    Id = 0,
                    Position = new Vector3(radar.PlayerPosition.X, radar.PlayerPosition.Y, radar.PlayerPosition.Z),
                    Heading = radar.PlayerHeading,
                    Color = Settings.CurrentPlayerColor
                });
                radar.UpdatePlayerList(players);
                radar.Render();
            }
        }

        private static void RenderRadarMenu()
        {
            ImGui.SetNextWindowSize(new Vector2(430, 500));
            ImGui.Begin("Radar 3D – Settings", ref Show);

            // -- General ------------------------------------------------------------------
            bool enabled = Settings.RadarEnabled;
            if (ImGui.Checkbox("##enableRadar", ref enabled))
            {
                Settings.RadarEnabled = enabled;
                radar.RadarEnabled = enabled;
            }
            ImGui.SameLine();
            ImGui.Text("3D Radar");

            ImGui.SameLine(0.0f, 32.0f);

            bool vanilla = Settings.VanillaRadarEnabled;
            if (ImGui.Checkbox("##enableVanillaRadar", ref vanilla))
            {
                Settings.VanillaRadarEnabled = vanilla;
                SDK.API.SetVanillaRadarVisible(vanilla);
            }
            ImGui.SameLine();
            ImGui.Text("Vanilla Radar");

            // -- Geometry -----------------------------------------------------------------
            int poly = Settings.PolyCount;
            if (ImGui.SliderInt("Poly-count", ref poly, 5, 60))
            {
                Settings.PolyCount = poly; radar.Settings.PolyCount = poly;
            }

            int skip = Settings.SkipFactor;
            if (ImGui.SliderInt("Skip factor", ref skip, 1, 8))
            {
                Settings.SkipFactor = skip; radar.Settings.SkipFactor = skip;
            }

            float zsd = Settings.ZScaleDown;
            if (ImGui.SliderFloat("Z-scale-down", ref zsd, 0.5f, 7.0f, "%.2f"))
            {
                Settings.ZScaleDown = zsd; radar.Settings.ZScaleDown = zsd;
            }

            Vector2 scale = Settings.Scale;
            if (ImGui.DragFloat2("Scale  XY", ref scale, 0.1f, 0.5f, 10.0f, "%.2f"))
            {
                Settings.Scale = scale; radar.Settings.Scale = scale;
            }

            Vector2 offset = Settings.Offset;
            if (ImGui.DragFloat2("Screen offset", ref offset, 1f, -4000, 4000, "%.0f"))
            {
                Settings.Offset = offset; radar.Settings.Offset = offset;
            }

            int upd = Settings.UpdateRate;
            if (ImGui.SliderInt("Update (ms)", ref upd, 0, 50))
            {
                Settings.UpdateRate = upd; radar.Settings.UpdateRate = upd;
            }

            ImGui.Separator();

            // -- Colors -------------------------------------------------------------------
            Vector4 pBaseColor = Settings.CurrentPlayerColor;
            Vector4 cBase = Settings.BaseColor;
            Vector4 cDir = Settings.DirectionColor;
            Vector4 cCent = Settings.CenterColor;
            Vector4 cMain = Settings.MainColor;

            if (ImGui.ColorEdit4("Current Player", ref pBaseColor))
            {
                Settings.CurrentPlayerColor = pBaseColor;
            }

            if (ImGui.ColorEdit4("Base", ref cBase))
            {
                Settings.BaseColor = cBase; radar.Settings.BaseColor = Settings.BaseColor;
            }
            if (ImGui.ColorEdit4("Direction", ref cDir))
            {
                Settings.DirectionColor = cDir; radar.Settings.DirectionColor = Settings.DirectionColor;
            }
            if (ImGui.ColorEdit4("Center", ref cCent))
            {
                Settings.CenterColor = cCent;
                radar.Settings.CenterColor = Settings.CenterColor;
            }
            if (ImGui.ColorEdit4("Main lines", ref cMain))
            {
                Settings.MainColor = cMain; radar.Settings.MainColor = Settings.MainColor;
            }

            // -- Buttons ------------------------------------------------------------------
            if (ImGui.Button("Reset##radar"))
            {
                Settings = new ConfigManager();

                var radar_config = new Radar3DSettings();

                radar_config.PolyCount = Settings.PolyCount;

                radar_config.SkipFactor = Settings.SkipFactor;

                radar_config.ZScaleDown = Settings.ZScaleDown;

                radar_config.Scale = Settings.Scale;

                radar_config.Offset = Settings.Offset;

                radar_config.UpdateRate = Settings.UpdateRate;

                radar_config.BaseColor = Settings.BaseColor;
                radar_config.DirectionColor = Settings.DirectionColor;
                radar_config.CenterColor = Settings.CenterColor;
                radar_config.MainColor = Settings.MainColor;
                radar.Settings = radar_config;
            }

            // -- Footer ------------------------------------------------------------------
            var style = ImGui.GetStyle();

            float footerH = ImGui.GetTextLineHeightWithSpacing();

            float contentMaxY = ImGui.GetWindowHeight() - style.WindowPadding.Y;

            float cursorY = contentMaxY - footerH;

            Vector2 winPos = ImGui.GetWindowPos();
            Vector2 footerMin = new Vector2(winPos.X,
                                    winPos.Y + cursorY);
            Vector2 footerMax = new Vector2(winPos.X + ImGui.GetWindowWidth(),
                                    winPos.Y + contentMaxY);

            ImGui.GetWindowDrawList().AddRectFilled(
                footerMin, footerMax,
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.13f, 0.13f, 0.13f, 1f)));

            ImGui.SetCursorPosY(cursorY);
            ImGui.Text($"3D Radar v{fileVer} | by https://github.com/DestroyerDarkNess");

            ImGui.End();
        }
    }
}