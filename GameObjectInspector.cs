using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UIInspectorMod
{
    public class GameObjectInspector
    {
        private static GameObjectInspector instance;
        
        // Game object tracking
        private GameObject playerObject = null;
        private Dictionary<string, Component> gameComponents = new Dictionary<string, Component>();
        private bool showGameInfo = false;
        
        public static GameObjectInspector Instance
        {
            get
            {
                if (instance == null)
                    instance = new GameObjectInspector();
                return instance;
            }
        }
        
        public bool ShowGameInfo
        {
            get { return showGameInfo; }
            set { showGameInfo = value; }
        }
        
        public void ToggleGameInfo()
        {
            showGameInfo = !showGameInfo;
            MelonLogger.Msg($"Game object inspector visibility: {showGameInfo}");
        }
        
        public void OnSceneLoaded(string sceneName)
        {
            // Reset references when a new scene loads
            playerObject = null;
            gameComponents.Clear();
            
            // Search for important game objects when a scene loads
            SearchForGameObjects();
        }
        
        public void SearchForGameObjects()
        {
            MelonLogger.Msg("Searching for important game objects...");
            
            // Look for player object by common naming patterns
            var allGameObjects = GameObject.FindObjectsOfType<GameObject>();
            
            // Search for player by name
            var playerCandidates = allGameObjects.Where(go => 
                go.name.ToLower().Contains("player") || 
                go.name.ToLower().Contains("character") ||
                go.name.ToLower().Contains("avatar")).ToList();
                
            if (playerCandidates.Count > 0)
            {
                playerObject = playerCandidates.First();
                MelonLogger.Msg($"Found player object: {playerObject.name}");
                
                // Log all components on the player
                Component[] components = playerObject.GetComponents<Component>();
                MelonLogger.Msg($"Player has {components.Length} components:");
                foreach (var component in components)
                {
                    if (component != null)
                    {
                        string typeName = component.GetType().Name;
                        MelonLogger.Msg($"  - {typeName}");
                        gameComponents[typeName] = component;
                    }
                }
            }
            else
            {
                MelonLogger.Msg("Could not find player object. Try using the UI Inspector to locate it manually.");
            }
            
            // Search for other important game controllers
            var controllerCandidates = allGameObjects.Where(go => 
                go.name.ToLower().Contains("manager") || 
                go.name.ToLower().Contains("controller") ||
                go.name.ToLower().Contains("game") ||
                go.name.ToLower().Contains("inventory")).ToList();
                
            if (controllerCandidates.Count > 0)
            {
                MelonLogger.Msg($"Found {controllerCandidates.Count} potential game controllers:");
                foreach (var controller in controllerCandidates.Take(10)) // Limit to first 10 to avoid spam
                {
                    MelonLogger.Msg($"  - {controller.name}");
                    
                    // Store important controller components
                    Component[] components = controller.GetComponents<Component>();
                    foreach (var component in components)
                    {
                        if (component != null && !component.GetType().Name.StartsWith("Unity"))
                        {
                            string typeName = component.GetType().Name;
                            gameComponents[typeName] = component;
                        }
                    }
                }
            }
        }
        
        public void ModifyPlayerData()
        {
            if (playerObject == null)
            {
                MelonLogger.Msg("No player object found. Cannot modify data.");
                return;
            }
            
            MelonLogger.Msg("Attempting to modify player data...");
            
            try
            {
                // Example: Try to modify common player properties
                // The exact properties will depend on the game's implementation
                
                // Example 1: Try to modify player position
                var transform = playerObject.transform;
                Vector3 currentPos = transform.position;
                MelonLogger.Msg($"Current player position: {currentPos}");
                
                // Move the player up slightly
                transform.position = new Vector3(currentPos.x, currentPos.y + 1f, currentPos.z);
                MelonLogger.Msg($"New player position: {transform.position}");
                
                // Example 2: Try to find and modify player stats if they exist
                // This is just an example - actual component names will vary
                foreach (var componentEntry in gameComponents)
                {
                    string typeName = componentEntry.Key;
                    Component component = componentEntry.Value;
                    
                    if (typeName.Contains("Player") || typeName.Contains("Character") || typeName.Contains("Stats"))
                    {
                        MelonLogger.Msg($"Attempting to modify {typeName}...");
                        
                        // Use reflection to find fields/properties we might want to modify
                        var type = component.GetType();
                        var properties = type.GetProperties();
                        var fields = type.GetFields();
                        
                        MelonLogger.Msg($"Found {properties.Length} properties and {fields.Length} fields");
                        
                        // Log some of the properties and fields for discovery
                        foreach (var prop in properties.Take(10)) // Limit to first 10
                        {
                            try
                            {
                                var value = prop.GetValue(component);
                                MelonLogger.Msg($"  Property: {prop.Name} = {value}");
                            }
                            catch (Exception ex)
                            {
                                MelonLogger.Msg($"  Property: {prop.Name} (Error: {ex.Message})");
                            }
                        }
                        
                        foreach (var field in fields.Take(10)) // Limit to first 10
                        {
                            try
                            {
                                var value = field.GetValue(component);
                                MelonLogger.Msg($"  Field: {field.Name} = {value}");
                            }
                            catch (Exception ex)
                            {
                                MelonLogger.Msg($"  Field: {field.Name} (Error: {ex.Message})");
                            }
                        }
                        
                        // Example of trying to modify a common property like "health" or "money"
                        // Again, actual property names will vary by game
                        foreach (var prop in properties)
                        {
                            string propName = prop.Name.ToLower();
                            if (propName.Contains("health") && prop.CanWrite)
                            {
                                try
                                {
                                    var currentValue = prop.GetValue(component);
                                    MelonLogger.Msg($"Found health property: {prop.Name} = {currentValue}");
                                    
                                    // Try to set health to a high value if it's a numeric type
                                    if (prop.PropertyType == typeof(int))
                                    {
                                        prop.SetValue(component, 999);
                                        MelonLogger.Msg($"Modified {prop.Name} to 999");
                                    }
                                    else if (prop.PropertyType == typeof(float))
                                    {
                                        prop.SetValue(component, 999f);
                                        MelonLogger.Msg($"Modified {prop.Name} to 999f");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MelonLogger.Msg($"Failed to modify {prop.Name}: {ex.Message}");
                                }
                            }
                            else if (propName.Contains("money") && prop.CanWrite)
                            {
                                try
                                {
                                    var currentValue = prop.GetValue(component);
                                    MelonLogger.Msg($"Found money property: {prop.Name} = {currentValue}");
                                    
                                    // Try to set money to a high value if it's a numeric type
                                    if (prop.PropertyType == typeof(int))
                                    {
                                        prop.SetValue(component, 10000);
                                        MelonLogger.Msg($"Modified {prop.Name} to 10000");
                                    }
                                    else if (prop.PropertyType == typeof(float))
                                    {
                                        prop.SetValue(component, 10000f);
                                        MelonLogger.Msg($"Modified {prop.Name} to 10000f");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MelonLogger.Msg($"Failed to modify {prop.Name}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error modifying player data: {ex.Message}");
            }
        }
        
        public void OnGUI()
        {
            if (!showGameInfo) return;
            
            // Display basic info about found game objects
            if (playerObject != null)
            {
                GUI.Label(new Rect(10, 10, 300, 20), $"Player Object: {playerObject.name}");
                GUI.Label(new Rect(10, 30, 300, 20), $"Position: {playerObject.transform.position}");
                
                int yPos = 50;
                GUI.Label(new Rect(10, yPos, 300, 20), "Game Components:");
                yPos += 20;
                
                foreach (var componentEntry in gameComponents.Take(10)) // Limit to 10 components
                {
                    GUI.Label(new Rect(10, yPos, 300, 20), $"  - {componentEntry.Key}");
                    yPos += 20;
                }
                
                GUI.Label(new Rect(10, yPos, 300, 20), "Press P to attempt modifying player data");
            }
            else
            {
                GUI.Label(new Rect(10, 10, 300, 20), "No player object found. Press J to search.");
            }
        }
        
        public GameObject GetPlayerObject()
        {
            return playerObject;
        }
        
        public Dictionary<string, Component> GetGameComponents()
        {
            return gameComponents;
        }
    }
} 