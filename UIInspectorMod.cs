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
        private static Dictionary<string, List<GameObject>> inspectedElements = new Dictionary<string, List<GameObject>>();
        
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
                InspectUI();
            }
        }
        
        public override void OnGUI()
        {
            // Call the inspector's OnGUI method
            UIInspectorWindow.Instance.OnGUI();
        }

        private void InspectUI()
        {
            MelonLogger.Msg("Taking UI snapshot...");
            outputBuffer.Clear();
            LogToBuffer("Taking UI snapshot...");
            
            // Find UI elements and populate the dictionary
            inspectedElements.Clear();
            FindUIElements();
            SaveToTempFile();
            
            // Toggle UI visibility
            UIInspectorWindow.Instance.ToggleVisibility();
            UIInspectorWindow.Instance.SetInspectedElements(inspectedElements);
            
            if (UIInspectorWindow.Instance.IsVisible)
                MelonLogger.Msg("UI Inspector window opened");
            else
                MelonLogger.Msg("UI Inspector window closed");
        }
        
        private void FindUIElements()
        {
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
                    
                    // Store canvas and all its children in our dictionary
                    List<GameObject> canvasElements = new List<GameObject>();
                    canvasElements.Add(canvas.gameObject);
                    
                    // Add all children that are relevant
                    Component[] allChildren = canvas.GetComponentsInChildren<Component>(true);
                    foreach (var child in allChildren)
                    {
                        if (child != null && child.gameObject != canvas.gameObject)
                        {
                            if (!canvasElements.Contains(child.gameObject))
                            {
                                canvasElements.Add(child.gameObject);
                            }
                        }
                    }
                    
                    inspectedElements[canvasName] = canvasElements;
                    
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
                            if (!string.IsNullOrWhiteSpace(text.text) && 
                                text.gameObject.activeInHierarchy &&
                                !string.IsNullOrEmpty(text.text.Trim()))
                            {
                                importantTexts.Add(text);
                            }
                        }
                        
                        if (importantTexts.Count > 0)
                        {
                            MelonLogger.Msg($"  Important Texts ({importantTexts.Count}):");
                            LogToBuffer($"  Important Texts ({importantTexts.Count}):");
                            foreach (var text in importantTexts)
                            {
                                string relativePath = GetRelativePath(text.transform, canvas.transform);
                                MelonLogger.Msg($"  - {relativePath}: \"{text.text}\"");
                                LogToBuffer($"  - {relativePath}: \"{text.text}\"");
                            }
                        }
                    }
                    
                    // Find any interesting MonoBehaviours
                    FindInterestingScripts(canvas.gameObject);
                }
            }
        }

        private void FindInterestingScripts(GameObject parentObject)
        {
            // Find all MonoBehaviours in the parent object
            var allScripts = parentObject.GetComponentsInChildren<MonoBehaviour>(true);
            
            // Filter to potentially interesting scripts
            List<MonoBehaviour> interestingScripts = new List<MonoBehaviour>();
            foreach (var script in allScripts)
            {
                string typeName = script.GetType().Name;
                if (typeName.Contains("Message") || 
                    typeName.Contains("Phone") || 
                    typeName.Contains("SMS") || 
                    typeName.Contains("Contact") || 
                    typeName.Contains("Chat") ||
                    typeName.Contains("ATM") ||
                    typeName.Contains("Bank"))
                {
                    interestingScripts.Add(script);
                }
            }
            
            if (interestingScripts.Count > 0)
            {
                MelonLogger.Msg($"  Interesting Scripts ({interestingScripts.Count}):");
                LogToBuffer($"  Interesting Scripts ({interestingScripts.Count}):");
                foreach (var script in interestingScripts)
                {
                    MelonLogger.Msg($"  - {script.GetType().Name} on {script.gameObject.name}");
                    LogToBuffer($"  - {script.GetType().Name} on {script.gameObject.name}");
                }
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
                // Get current scene name
                string sceneName = SceneManager.GetActiveScene().name;
                
                // Create a timestamp for the filename
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                
                // Path is relative to the mod directory
                string modDir = Path.GetDirectoryName(typeof(UIInspectorMod).Assembly.Location);
                string tempDir = Path.Combine(modDir, "tmp");
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }
                
                string filePath = Path.Combine(tempDir, $"UIInspection_{sceneName}_{timestamp}.txt");
                File.WriteAllText(filePath, outputBuffer.ToString());
                
                MelonLogger.Msg($"UI Inspection saved to: {filePath}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to save UI inspection to file: {ex.Message}");
            }
        }
        
        private string GetRelativePath(Transform child, Transform parent)
        {
            string path = child.name;
            Transform current = child.parent;
            
            while (current != null && current != parent)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            
            return path;
        }
        
        public GameObject FindUIElement(string canvasName, string elementPath)
        {
            var canvases = GameObject.FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                if (canvas.name == canvasName && canvas.gameObject.activeInHierarchy)
                {
                    Transform current = canvas.transform;
                    string[] pathParts = elementPath.Split('/');
                    
                    foreach (string part in pathParts)
                    {
                        bool found = false;
                        for (int i = 0; i < current.childCount; i++)
                        {
                            if (current.GetChild(i).name == part)
                            {
                                current = current.GetChild(i);
                                found = true;
                                break;
                            }
                        }
                        
                        if (!found)
                        {
                            MelonLogger.Warning($"Could not find UI element: {part} in path {elementPath}");
                            return null;
                        }
                    }
                    
                    return current.gameObject;
                }
            }
            
            return null;
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