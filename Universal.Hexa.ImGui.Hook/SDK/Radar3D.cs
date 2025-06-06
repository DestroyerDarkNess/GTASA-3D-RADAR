using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;

namespace Samp.Radar.SDK
{
    namespace Radar3D
    {
        public class PlayerInfo
        {
            public int Id { get; set; }
            public Vector3 Position { get; set; }
            public float Heading { get; set; }
            public Vector4 Color { get; set; }
        }

        public class Radar3DSettings
        {
            public int PolyCount { get; set; } = 30;
            public int SkipFactor { get; set; } = 3;
            public Vector2 Offset { get; set; } = new Vector2(100, 100);
            public Vector2 Scale { get; set; } = new Vector2(4, 4);
            public float ZScaleDown { get; set; } = 2.0f;
            public Vector4 BaseColor { get; set; } = new Vector4(0.00f, 0.00f, 0.00f, 0.63f); // A0-000000
            public Vector4 DirectionColor { get; set; } = new Vector4(1.00f, 1.00f, 1.00f, 0.19f); // 30-FFFFFF
            public Vector4 CenterColor { get; set; } = new Vector4(0.00f, 1.00f, 1.00f, 1.00f); // FF-00FFFF
            public Vector4 MainColor { get; set; } = new Vector4(0.00f, 1.00f, 0.00f, 1.00f); // FF-00FF00
            public int UpdateRate { get; set; } = 5;
        }

        public class Radar3D : IDisposable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static uint U32(Vector4 v) => ImGui.ColorConvertFloat4ToU32(v);

            private const int MAP_MAX = 3000;
            private int mapSize = 0;
            private const float WORLD_SIZE = 6000f;

            private float[,] heightMap;
            private bool mapLoaded = false;
            private bool disposed = false;
            private DateTime lastUpdate = DateTime.Now;

            private List<List<Vector2>> polylines = new List<List<Vector2>>();
            private List<PlayerMarker> playerMarkers = new List<PlayerMarker>();
            private Vector2[] baseCorners = new Vector2[5];

            public Radar3DSettings Settings { get; set; }
            public Vector3 PlayerPosition { get; set; }
            public float PlayerHeading { get; set; }
            public bool RadarEnabled { get; set; } = false;
            public Vector2 ScreenResolution { get; set; } = new Vector2(640, 480);

            private struct PlayerMarker
            {
                public Vector2 Position;
                public Vector2 BasePosition;
                public float Heading;
                public int Id;
                public uint Color;
            }

            private struct DirectionEdge
            {
                public int Corner1, Corner2;
                public float Start, End;
                public bool Reversed;
            }

            private Dictionary<int, DirectionEdge> directionTable = new Dictionary<int, DirectionEdge>
        {
            {1, new DirectionEdge { Corner1 = 1, Corner2 = 2, Start = 0.75f, End = 0.25f, Reversed = false }},
            {2, new DirectionEdge { Corner1 = 1, Corner2 = 2, Start = 0.75f, End = 0.25f, Reversed = false }},
            {0, new DirectionEdge { Corner1 = 2, Corner2 = 4, Start = -0.25f, End = 0.25f, Reversed = true }},
            {-1, new DirectionEdge { Corner1 = 2, Corner2 = 4, Start = -0.25f, End = 0.25f, Reversed = true }},
            {-2, new DirectionEdge { Corner1 = 3, Corner2 = 4, Start = -0.75f, End = -0.25f, Reversed = true }},
            {-3, new DirectionEdge { Corner1 = 3, Corner2 = 4, Start = -0.75f, End = -0.25f, Reversed = true }},
            {-4, new DirectionEdge { Corner1 = 3, Corner2 = 1, Start = -0.75f, End = -1.25f, Reversed = false }},
            {3, new DirectionEdge { Corner1 = 3, Corner2 = 1, Start = 1.25f, End = 0.75f, Reversed = false }}
        };

            public Radar3D()
            {
                Settings = new Radar3DSettings();
                InitializeBaseCorners();
            }

            public Radar3D(Radar3DSettings settings)
            {
                Settings = settings ?? new Radar3DSettings();
                InitializeBaseCorners();
            }

            public bool LoadHeightMap()
            {
                try
                {
                    string fileContent = Properties.Resources.heightmap;

                    string[] lines = fileContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                    mapSize = Math.Min(lines.Length, MAP_MAX);
                    heightMap = new float[mapSize, mapSize];

                    for (int y = 0; y < mapSize; y++)
                    {
                        string[] values = lines[y].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int x = 0; x < mapSize && x < values.Length; x++)
                        {
                            if (float.TryParse(values[x], out float height))
                                heightMap[y, x] = float.Parse(values[x]);
                        }
                    }

                    mapLoaded = true;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            public void UpdatePlayerList(List<PlayerInfo> players)
            {
                if (!mapLoaded || (DateTime.Now - lastUpdate).TotalMilliseconds < Settings.UpdateRate)
                    return;

                lastUpdate = DateTime.Now;
                UpdateRadarData(players);
            }

            private void UpdateRadarData(List<PlayerInfo> players)
            {
                polylines.Clear();
                playerMarkers.Clear();
                InitializeBaseCorners();

                var normalizedPlayerPos = NormalizePosition(PlayerPosition);
                int playerX = (int)normalizedPlayerPos.X;
                int playerY = (int)normalizedPlayerPos.Y;

                float playerZ = Math.Max(0, Math.Min(510, PlayerPosition.Z));

                GeneratePolylines(playerX, playerY);

                GeneratePlayerMarkers(players, normalizedPlayerPos);
            }

            private void GeneratePolylines(int playerX, int playerY)
            {
                int np = Settings.PolyCount * Settings.SkipFactor;
                var edges = new Vector2[]
                {
                new Vector2(Settings.SkipFactor, 0),
                new Vector2(0, Settings.SkipFactor),
                new Vector2(-Settings.SkipFactor, 0),
                new Vector2(0, -Settings.SkipFactor)
                };

                for (int poly = 1; poly <= Settings.PolyCount; poly++)
                {
                    var polyline = new List<Vector2>();
                    int xArr = playerX - poly * Settings.SkipFactor;
                    int yArr = playerY - poly * Settings.SkipFactor;

                    for (int edge = 0; edge < 4; edge++)
                    {
                        for (int k = 0; k < poly * 2; k++)
                        {
                            float height = GetMapHeight(xArr, yArr);
                            var screenPoint = WorldToScreen(playerX - xArr, playerY - yArr, height);
                            polyline.Add(screenPoint);

                            xArr += (int)edges[edge].X;
                            yArr += (int)edges[edge].Y;
                        }
                    }

                    float finalHeight = GetMapHeight(xArr, yArr - Settings.SkipFactor);
                    var finalPoint = WorldToScreen(playerX - xArr, playerY - (yArr - Settings.SkipFactor), finalHeight);
                    polyline.Add(finalPoint);

                    polylines.Add(polyline);
                }
            }

            private void GeneratePlayerMarkers(List<PlayerInfo> players, Vector2 normalizedPlayerPos)
            {
                int np = Settings.PolyCount * Settings.SkipFactor;

                var bounds = new
                {
                    XMin = normalizedPlayerPos.X - np,
                    XMax = normalizedPlayerPos.X + np,
                    YMin = normalizedPlayerPos.Y - np,
                    YMax = normalizedPlayerPos.Y + np
                };

                foreach (var player in players)
                {
                    var normalizedPos = NormalizePositionFloat(player.Position);

                    if (normalizedPos.X >= bounds.XMin && normalizedPos.X <= bounds.XMax &&
                        normalizedPos.Y >= bounds.YMin && normalizedPos.Y <= bounds.YMax)
                    {
                        float pz = Math.Max(0, Math.Min(510, player.Position.Z));

                        var marker = new PlayerMarker
                        {
                            Position = WorldToScreen(normalizedPlayerPos.X - normalizedPos.X,
                                                   normalizedPlayerPos.Y - normalizedPos.Y, pz / 2f),
                            BasePosition = WorldToScreen(normalizedPlayerPos.X - normalizedPos.X,
                                                       normalizedPlayerPos.Y - normalizedPos.Y, 0),
                            Heading = player.Heading + 45f,
                            Id = player.Id,
                            Color = U32(player.Color)
                        };

                        playerMarkers.Add(marker);
                    }
                }
            }

            public void Render()
            {
                if (!mapLoaded || polylines.Count == 0)
                    return;

                if (!RadarEnabled) return;

                var drawList = ImGui.GetBackgroundDrawList();

                RenderRadarBase(drawList);

                RenderDirectionIndicator(drawList);

                RenderPolylines(drawList);

                RenderPlayerMarkers(drawList);
            }

            private void RenderRadarBase(ImDrawListPtr drawList)
            {
                Span<Vector2> points = stackalloc Vector2[4]
                {
                 new Vector2(baseCorners[3].X + Settings.Offset.X, baseCorners[3].Y - Settings.Offset.Y), // UL
                 new Vector2(baseCorners[1].X + Settings.Offset.X, baseCorners[1].Y - Settings.Offset.Y), // UR
                 new Vector2(baseCorners[2].X + Settings.Offset.X, baseCorners[2].Y - Settings.Offset.Y), // LR
                 new Vector2(baseCorners[4].X + Settings.Offset.X, baseCorners[4].Y - Settings.Offset.Y)  // LL
                };

                drawList.AddConvexPolyFilled(ref points[0], points.Length, U32(Settings.BaseColor));
            }

            private void RenderDirectionIndicator(ImDrawListPtr drawList)
            {
                var dirMarkers = GetDirectionMarkers();
                if (dirMarkers != null)
                {
                    var points = new Vector2[]
                    {
                    new Vector2(dirMarkers.Value.Item1.X + Settings.Offset.X, dirMarkers.Value.Item1.Y - Settings.Offset.Y),
                    new Vector2(dirMarkers.Value.Item2.X + Settings.Offset.X, dirMarkers.Value.Item2.Y - Settings.Offset.Y),
                    new Vector2(baseCorners[0].X + Settings.Offset.X, baseCorners[0].Y - Settings.Offset.Y)
                    };

                    drawList.AddTriangleFilled(points[0], points[1], points[2], U32(Settings.DirectionColor));
                }
            }

            private void RenderPolylines(ImDrawListPtr drawList)
            {
                for (int k = 0; k < polylines.Count; k++)
                {
                    var polyline = polylines[k];
                    uint color = (k < 2) ? U32(Settings.CenterColor) : U32(Settings.MainColor);
                    color -= (uint)(0x8000000 * k); // Aplicar fade

                    for (int p = 0; p < polyline.Count - 1; p++)
                    {
                        var p1 = new Vector2(polyline[p].X + Settings.Offset.X, polyline[p].Y - Settings.Offset.Y);
                        var p2 = new Vector2(polyline[p + 1].X + Settings.Offset.X, polyline[p + 1].Y - Settings.Offset.Y);
                        drawList.AddLine(p1, p2, color, 1.0f);
                    }
                }
            }

            private void RenderPlayerMarkers(ImDrawListPtr drawList)
            {
                foreach (var marker in playerMarkers)
                {
                    var pos = new Vector2(marker.Position.X + Settings.Offset.X, marker.Position.Y - Settings.Offset.Y);
                    var basePos = new Vector2(marker.BasePosition.X + Settings.Offset.X, marker.BasePosition.Y - Settings.Offset.Y);

                    // Línea vertical del jugador
                    drawList.AddLine(pos, basePos, marker.Color, 1.0f);

                    // Triángulo indicando dirección
                    DrawPlayerTriangle(drawList, pos, marker.Heading, marker.Color);

                    // ID del jugador
                    drawList.AddText(new Vector2(pos.X + 5, pos.Y + 5), marker.Color, marker.Id.ToString());
                }
            }

            private void DrawPlayerTriangle(ImDrawListPtr drawList, Vector2 center, float heading, uint color)
            {
                float rad = heading * (float)Math.PI / 180f;
                float size = 10f;

                var p1 = new Vector2(
                    center.X + (float)Math.Cos(rad) * size,
                    center.Y + (float)Math.Sin(rad) * size
                );
                var p2 = new Vector2(
                    center.X + (float)Math.Cos(rad + 2.094f) * size * 0.6f,
                    center.Y + (float)Math.Sin(rad + 2.094f) * size * 0.6f
                );
                var p3 = new Vector2(
                    center.X + (float)Math.Cos(rad - 2.094f) * size * 0.6f,
                    center.Y + (float)Math.Sin(rad - 2.094f) * size * 0.6f
                );

                drawList.AddTriangleFilled(p1, p2, p3, color);
            }

            private (Vector2, Vector2)? GetDirectionMarkers()
            {
                float dconv = PlayerHeading / (float)Math.PI;   //float dconv = (PlayerHeading * ((float)Math.PI / 180f)) / (float)Math.PI;

                int dval = (int)Math.Floor(dconv / 0.25f);

                if (!directionTable.TryGetValue(dval, out DirectionEdge dtbl))
                    return null;

                var c1 = baseCorners[dtbl.Corner1];
                var c2 = baseCorners[dtbl.Corner2];

                float step = (dconv - dtbl.Start) / (dtbl.End - dtbl.Start);
                float p1 = Math.Max(0, step - 0.06f);
                float p2 = Math.Min(1, step + 0.06f);

                Vector2 mark1, mark2;
                float range = Math.Abs(c1.X - c2.X);

                if (c1.Y == c2.Y)
                {
                    mark1 = new Vector2(Math.Min(c1.X, c2.X) + p1 * range, c1.Y);
                    mark2 = new Vector2(Math.Min(c1.X, c2.X) + p2 * range, c2.Y);
                }
                else
                {
                    mark1 = new Vector2(
                        Math.Max(c1.X, c2.X) - p1 * range,
                        Math.Min(c1.Y, c2.Y) + Math.Abs(c1.Y - c2.Y) * p1
                    );
                    mark2 = new Vector2(
                        Math.Max(c1.X, c2.X) - p2 * range,
                        Math.Min(c1.Y, c2.Y) + Math.Abs(c1.Y - c2.Y) * p2
                    );
                }

                return dtbl.Reversed ? (mark2, mark1) : (mark1, mark2);
            }

            private void InitializeBaseCorners()
            {
                int np = Settings.PolyCount * Settings.SkipFactor;
                baseCorners[1] = WorldToScreen(np, np, 0);
                baseCorners[2] = WorldToScreen(np, -np, 0);
                baseCorners[3] = WorldToScreen(-np, np, 0);
                baseCorners[4] = WorldToScreen(-np, -np, 0);
                baseCorners[0] = WorldToScreen(0, 0, 0);
            }

            private Vector2 NormalizePosition(Vector3 worldPos)
            {
                return new Vector2(
                    (float)Math.Floor(worldPos.X * (mapSize / WORLD_SIZE) + mapSize / 2),
                    (float)Math.Floor(worldPos.Y * (mapSize / WORLD_SIZE) + mapSize / 2)
                );
            }

            private Vector2 NormalizePositionFloat(Vector3 worldPos)
            {
                return new Vector2(
                    worldPos.X * (mapSize / WORLD_SIZE) + mapSize / 2,
                    worldPos.Y * (mapSize / WORLD_SIZE) + mapSize / 2
                );
            }

            private Vector2 WorldToScreen(float x, float y, float z)
            {
                float xpos = y / 4.24f + x;
                float ypos = y / 4.24f + z / Settings.ZScaleDown;

                return new Vector2(
                    xpos * Settings.Scale.X,
                    ScreenResolution.Y - ypos * Settings.Scale.Y
                );
            }

            private float GetMapHeight(int x, int y)
            {
                if (x < 0 || y < 0 || x >= mapSize || y >= mapSize) return 0;
                return heightMap[x, y];
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        heightMap = null;
                        polylines?.Clear();
                        playerMarkers?.Clear();
                    }
                    disposed = true;
                }
            }
        }
    }
}