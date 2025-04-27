using MelonLoader;
using MelonLoader.Utils;
using System.IO;
using System.Text.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(UIInspectorMod.UIInspectorMod), "UIInspectorMod", "1.0.0", "John")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace UIInspectorMod
{
    public class UIInspectorMod : MelonMod
    {
        private ModConfig config;
        
        public override void OnInitializeMelon()
        {
            LoadConfig();
            MelonLogger.Msg($"UI Inspector Mod Initialized");
            MelonLogger.Msg("Press H key to inspect UI elements");
        }

        public override void OnUpdate()
        {
            // Check for the H key to trigger UI inspection
            if (Input.GetKeyDown(KeyCode.H))
            {
                UIInspector.InspectUI();
            }
        }

        private void LoadConfig()
        {
            string configPath = Path.Combine(MelonEnvironment.ModsDirectory, "UIInspectorMod.json");
            
            // Create default config if it doesn't exist
            if (!File.Exists(configPath))
            {
                config = new ModConfig
                {
                    InspectKey = "H"
                };
                SaveConfig(configPath);
            }
            else
            {
                string json = File.ReadAllText(configPath);
                config = JsonSerializer.Deserialize<ModConfig>(json);
            }
        }

        private void SaveConfig(string path)
        {
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }

    public class ModConfig
    {
        public string InspectKey { get; set; }
    }
} 