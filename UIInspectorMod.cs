using MelonLoader;
using MelonLoader.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(UIInspectorMod.UIInspectorMod), "UIInspectorMod", "1.0.0", "John")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace UIInspectorMod
{
    public class UIInspectorMod : MelonMod
    {
        private ModConfig config;
        private static StringBuilder outputBuffer = new StringBuilder();
        
        public override void OnInitializeMelon()
        {
            LoadConfig();
            MelonLogger.Msg($"UI Inspector Mod Initialized");
            MelonLogger.Msg("Press SHIFT+H key to toggle UI Inspector");
            MelonLogger.Msg("Press SHIFT+J key to search for player and important game objects");
            MelonLogger.Msg("Press SHIFT+P key to attempt modifying player data");
            MelonLogger.Msg("Press SHIFT+M key to open game data modifier");
        }
        
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            MelonLogger.Msg($"Scene loaded: {sceneName} (Index: {buildIndex})");
            
            // Notify the game object inspector about the scene change
            GameObjectInspector.Instance.OnSceneLoaded(sceneName);
            
            // Notify the UI inspector window about the scene change
            UIInspectorWindow.Instance.OnSceneLoaded(sceneName);
        }

        public override void OnUpdate()
        {
            bool isShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            
            // Check for SHIFT+H to toggle UI inspector
            if (isShiftPressed && Input.GetKeyDown(KeyCode.H))
            {
                ToggleUIInspector();
            }
            
            // Check for SHIFT+J to search for player and game objects
            if (isShiftPressed && Input.GetKeyDown(KeyCode.J))
            {
                MelonLogger.Msg("SHIFT+J key pressed - searching for game objects");
                GameObjectInspector.Instance.SearchForGameObjects();
                GameObjectInspector.Instance.ToggleGameInfo();
            }
            
            // Check for SHIFT+P to attempt modifying player data
            if (isShiftPressed && Input.GetKeyDown(KeyCode.P))
            {
                GameObjectInspector.Instance.ModifyPlayerData();
            }
            
            // Check for SHIFT+M to open game data modifier
            if (isShiftPressed && Input.GetKeyDown(KeyCode.M))
            {
                MelonLogger.Msg("SHIFT+M key pressed - opening game data modifier");
                GameDataModifier.Instance.ToggleModifierUI();
            }
        }
        
        public override void OnGUI()
        {
            // Call the inspector's OnGUI method
            UIInspectorWindow.Instance.OnGUI();
            
            // Call the game object inspector's OnGUI method
            GameObjectInspector.Instance.OnGUI();
            
            // Call the game data modifier's OnGUI method
            GameDataModifier.Instance.OnGUI();
        }

        private void ToggleUIInspector()
        {
            try
            {
                MelonLogger.Msg("Toggling UI Inspector...");
                
                // Toggle the UI Inspector window visibility
                UIInspectorWindow.Instance.ToggleVisibility();
                
                if (UIInspectorWindow.Instance.WindowVisible)
                {
                    MelonLogger.Msg("UI Inspector window opened");
                    LogActiveUI(); // Log UI info to console for reference
                }
                else
                {
                    MelonLogger.Msg("UI Inspector window closed");
                }
            }
            catch (Exception ex)
            {
                // If the UI window is causing errors, fallback to just logging the UI structure
                MelonLogger.Error($"Error with UI Inspector window: {ex.Message}");
                MelonLogger.Msg("Falling back to console-only UI logging...");
                LogActiveUI();
            }
        }
        
        private void LogActiveUI()
        {
            try
            {
                outputBuffer.Clear();
                LogToBuffer("UI Structure Summary:");
                
                // Find all Canvas objects
                var canvases = GameObject.FindObjectsOfType<Canvas>();
                
                MelonLogger.Msg($"Found {canvases.Length} Canvas objects");
                LogToBuffer($"Found {canvases.Length} Canvas objects");
                
                foreach (var canvas in canvases)
                {
                    if (canvas.gameObject.activeInHierarchy)
                    {
                        string canvasName = canvas.name;
                        MelonLogger.Msg($"Active Canvas: {canvasName}");
                        LogToBuffer($"Active Canvas: {canvasName}");
                        
                        // Find all buttons in this canvas
                        var buttons = canvas.GetComponentsInChildren<Button>(true);
                        if (buttons.Length > 0)
                        {
                            MelonLogger.Msg($"  Buttons ({buttons.Length}):");
                            LogToBuffer($"  Buttons ({buttons.Length}):");
                            
                            foreach (var button in buttons)
                            {
                                // Get button path within canvas
                                string relativePath = GetRelativePath(button.transform, canvas.transform);
                                MelonLogger.Msg($"  - {relativePath}");
                                LogToBuffer($"  - {relativePath}");
                                
                                // Get button text
                                var buttonText = button.GetComponentInChildren<Text>();
                                if (buttonText != null && !string.IsNullOrEmpty(buttonText.text))
                                {
                                    MelonLogger.Msg($"      Text: \"{buttonText.text}\"");
                                    LogToBuffer($"      Text: \"{buttonText.text}\"");
                                }
                                
                                // Log components
                                var components = button.GetComponents<Component>();
                                List<string> componentNames = new List<string>();
                                foreach (var component in components)
                                {
                                    if (component != null && 
                                        !component.GetType().Name.StartsWith("Unity") &&
                                        component.GetType().Name != "Button" &&
                                        component.GetType().Name != "Image" &&
                                        component.GetType().Name != "RectTransform")
                                    {
                                        componentNames.Add(component.GetType().Name);
                                    }
                                }
                                
                                if (componentNames.Count > 0)
                                {
                                    MelonLogger.Msg($"      Components: {string.Join(", ", componentNames)}");
                                    LogToBuffer($"      Components: {string.Join(", ", componentNames)}");
                                }
                            }
                        }
                        
                        // Find all text elements that might be interesting
                        var texts = canvas.GetComponentsInChildren<Text>(true);
                        if (texts.Length > 0)
                        {
                            // Filter texts to only show ones that might be important (non-empty)
                            List<Text> importantTexts = new List<Text>();
                            foreach (var text in texts)
                            {
                                try
                                {
                                    if (!string.IsNullOrWhiteSpace(text.text) && 
                                        text.gameObject.activeInHierarchy &&
                                        !string.IsNullOrEmpty(text.text.Trim()))
                                    {
                                        importantTexts.Add(text);
                                    }
                                }
                                catch (Exception)
                                {
                                    // Skip if there's an error getting text
                                }
                            }
                            
                            MelonLogger.Msg($"  Important Text Elements ({importantTexts.Count}):");
                            LogToBuffer($"  Important Text Elements ({importantTexts.Count}):");
                            
                            foreach (var text in importantTexts)
                            {
                                try
                                {
                                    string relativePath = GetRelativePath(text.transform, canvas.transform);
                                    MelonLogger.Msg($"  - {relativePath}: \"{text.text}\"");
                                    LogToBuffer($"  - {relativePath}: \"{text.text}\"");
                                }
                                catch (Exception ex)
                                {
                                    MelonLogger.Warning($"Error getting text info: {ex.Message}");
                                }
                            }
                        }
                        
                        // Find interesting components directly on the canvas
                        FindInterestingScripts(canvas.gameObject);
                    }
                }
                
                SaveToTempFile();
                
                // Log a tree view of the hierarchy for easier navigation
                MelonLogger.Msg("=== UI HIERARCHY ===");
                foreach (var canvas in canvases)
                {
                    if (canvas != null && canvas.gameObject.activeInHierarchy)
                    {
                        LogHierarchyToConsole(canvas.gameObject, 0);
                    }
                }
                MelonLogger.Msg("=== END UI HIERARCHY ===");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error logging UI: {ex.Message}");
            }
        }
        
        private void LogHierarchyToConsole(GameObject obj, int depth)
        {
            try
            {
                if (obj == null) return;
                
                string indent = new string(' ', depth * 2);
                MelonLogger.Msg($"{indent}└─ {obj.name}");
                
                // Get direct children safely without using foreach on transform
                int childCount = obj.transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    try
                    {
                        Transform childTransform = obj.transform.GetChild(i);
                        if (childTransform != null)
                        {
                            LogHierarchyToConsole(childTransform.gameObject, depth + 1);
                        }
                    }
                    catch (Exception)
                    {
                        // Skip problematic children
                    }
                }
            }
            catch (Exception ex)
            {
                // Just skip this object silently
            }
        }
        
        private void FindInterestingScripts(GameObject parentObject)
        {
            try
            {
                var components = parentObject.GetComponents<Component>();
                bool foundInteresting = false;
                
                List<string> interestingTypes = new List<string>();
                foreach (var component in components)
                {
                    if (component == null)
                        continue;
                        
                    string typeName = component.GetType().Name;
                    
                    // Skip standard Unity components
                    if (typeName.StartsWith("Unity") || 
                        typeName == "Canvas" || 
                        typeName == "CanvasScaler" || 
                        typeName == "GraphicRaycaster" ||
                        typeName == "RectTransform" ||
                        typeName == "CanvasRenderer" ||
                        typeName == "Text" ||
                        typeName == "Image" ||
                        typeName == "Button")
                        continue;
                        
                    interestingTypes.Add(typeName);
                    foundInteresting = true;
                }
                
                if (foundInteresting)
                {
                    string path = GetGameObjectPath(parentObject);
                    MelonLogger.Msg($"  Interesting Components on {path}: {string.Join(", ", interestingTypes)}");
                    LogToBuffer($"  Interesting Components on {path}: {string.Join(", ", interestingTypes)}");
                }
                
                // MODIFIED: Safely handle IL2CPP Transform iteration
                // Don't recursively check children - IL2CPP issues with transform enumeration
                // This avoids the casting exception
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Error inspecting {parentObject.name}: {ex.Message}");
            }
        }
        
        private void LogToBuffer(string message)
        {
            outputBuffer.AppendLine(message);
        }
        
        private void SaveToTempFile()
        {
            try
            {
                // Create a temporary file to save the output
                string tempPath = Path.Combine(Path.GetTempPath(), "UIInspectorOutput.txt");
                File.WriteAllText(tempPath, outputBuffer.ToString());
                
                MelonLogger.Msg($"UI inspection data saved to: {tempPath}");
                
                // Try to get a path within the game directory as well
                try
                {
                    string localPath = Path.Combine(MelonEnvironment.GameRootDirectory, "UIInspectorOutput.txt");
                    File.WriteAllText(localPath, outputBuffer.ToString());
                    MelonLogger.Msg($"UI inspection data also saved to: {localPath}");
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"Could not save to game directory: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to save inspection data: {ex.Message}");
            }
        }
        
        private string GetRelativePath(Transform child, Transform parent)
        {
            if (child == parent) return parent.name;
            
            StringBuilder path = new StringBuilder(child.name);
            Transform current = child.parent;
            
            while (current != null && current != parent)
            {
                path.Insert(0, current.name + "/");
                current = current.parent;
            }
            
            return path.ToString();
        }
        
        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
        
        public GameObject FindUIElement(string canvasName, string elementPath)
        {
            // Find the canvas
            Canvas[] canvases = GameObject.FindObjectsOfType<Canvas>();
            Canvas targetCanvas = null;
            
            foreach (var canvas in canvases)
            {
                if (canvas.name == canvasName)
                {
                    targetCanvas = canvas;
                    break;
                }
            }
            
            if (targetCanvas == null)
            {
                MelonLogger.Warning($"Canvas {canvasName} not found");
                return null;
            }
            
            // Split the path
            string[] pathParts = elementPath.Split('/');
            Transform current = targetCanvas.transform;
            
            // Navigate the path
            for (int i = 0; i < pathParts.Length; i++)
            {
                Transform next = current.Find(pathParts[i]);
                if (next == null)
                {
                    MelonLogger.Warning($"Could not find {pathParts[i]} in {current.name}");
                    return null;
                }
                current = next;
            }
            
            return current.gameObject;
        }
        
        private void LoadConfig()
        {
            string configPath = Path.Combine(MelonEnvironment.UserDataDirectory, "UIInspectorMod.cfg");
            
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    config = JsonSerializer.Deserialize<ModConfig>(json);
                    MelonLogger.Msg("Config loaded");
                }
                else
                {
                    config = new ModConfig
                    {
                        InspectKey = "H"
                    };
                    
                    SaveConfig(configPath);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to load config: {ex.Message}");
                config = new ModConfig
                {
                    InspectKey = "H"
                };
            }
        }
        
        private void SaveConfig(string path)
        {
            try
            {
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
                MelonLogger.Msg("Config saved");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to save config: {ex.Message}");
            }
        }
    }
    
    public class ModConfig
    {
        public string InspectKey { get; set; }
    }
} 
