using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Samp.Radar.SDK
{
    public class ConfigManager
    {
        [JsonProperty("Debug")]
        public bool DebugEnabled { get; set; } = false;

        [JsonProperty("VanillaRadarEnabled")]
        public bool VanillaRadarEnabled { get; set; } = true;

        [JsonProperty("RadarEnabled")]
        public bool RadarEnabled { get; set; } = false;

        [JsonProperty("PolyCount")]
        public int PolyCount { get; set; } = 30;

        [JsonProperty("SkipFactor")]
        public int SkipFactor { get; set; } = 3;

        [JsonProperty("Offset")]
        public Vector2 Offset { get; set; } = new Vector2(100, 100);

        [JsonProperty("Scale")]
        public Vector2 Scale { get; set; } = new Vector2(4, 4);

        [JsonProperty("ZScaleDown")]
        public float ZScaleDown { get; set; } = 2.0f;

        [JsonProperty("UpdateRate")]
        public int UpdateRate { get; set; } = 5;

        [JsonProperty("BaseColor")]
        public Vector4 BaseColor { get; set; } = new Vector4(0.00f, 0.00f, 0.00f, 0.63f); // A0-000000

        [JsonProperty("DirectionColor")]
        public Vector4 DirectionColor { get; set; } = new Vector4(1.00f, 1.00f, 1.00f, 0.19f); // 30-FFFFFF

        [JsonProperty("CenterColor")]
        public Vector4 CenterColor { get; set; } = new Vector4(0.00f, 1.00f, 1.00f, 1.00f); // FF-00FFFF

        [JsonProperty("MainColor")]
        public Vector4 MainColor { get; set; } = new Vector4(0.00f, 1.00f, 0.00f, 1.00f); // FF-00FF00

        [JsonProperty("CurrentPlayerColor")]
        public Vector4 CurrentPlayerColor { get; set; } = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);

        public static void SaveConfig(ConfigManager config, string CONFIG_FILE = "3dRadar_config.json")
        {
            string json = JsonConvert.SerializeObject(config);
            File.WriteAllText(CONFIG_FILE, json);
        }

        public static ConfigManager LoadConfig(string CONFIG_FILE = "3dRadar_config.json")
        {
            if (File.Exists(CONFIG_FILE))
            {
                string json = File.ReadAllText(CONFIG_FILE);
                return (ConfigManager)JsonConvert.DeserializeObject(json, typeof(ConfigManager));
            }
            else
            {
                return new ConfigManager();
            }
        }
    }
}