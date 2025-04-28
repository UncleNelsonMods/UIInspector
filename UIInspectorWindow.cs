using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace UIInspectorMod
{
    public class UIInspectorWindow
    {
        private static UIInspectorWindow instance;
        
        // Window state
        private bool windowVisible = false;
        private Rect windowRect = new Rect(20, 20, 800, 600);
        private Vector2 hierarchyScrollPosition = Vector2.zero;
        private Vector2 detailsScrollPosition = Vector2.zero;
        private Vector2 methodsScrollPosition = Vector2.zero;
        private bool isDragging = false;
        private Vector2 dragOffset = Vector2.zero;
        
        // Object selection tracking
        private GameObject selectedObject;
        private Component selectedComponent;
        private bool staySelectedOnSceneChange = false;
        private string selectedObjectPath = "";
        
        // Inspector state
        private string searchText = "";
        private bool showInactiveObjects = true;
        private bool lockSelection = false;
        
        // Utilities
        private string tempFilePath;
        
        public static UIInspectorWindow Instance
        {
            get
            {
                if (instance == null)
                    instance = new UIInspectorWindow();
                return instance;
            }
        }
        
        public UIInspectorWindow()
        {
            // Set up temp file path for object export
            tempFilePath = Path.Combine(Path.GetTempPath(), "UIInspectorExport.txt");
        }
        
        public bool WindowVisible
        {
            get { return windowVisible; }
            set { windowVisible = value; }
        }
        
        public void ToggleVisibility()
        {
            windowVisible = !windowVisible;
            MelonLogger.Msg($"UI Inspector window visibility: {windowVisible}");
        }
        
        public void OnSceneLoaded(string sceneName)
        {
            if (!staySelectedOnSceneChange)
            {
                // Reset selection when scene changes
                selectedObject = null;
                selectedComponent = null;
                selectedObjectPath = "";
            }
        }
        
        public void OnGUI()
        {
            try 
            {
                if (!windowVisible) return;
                
                // Very basic window to avoid unstripping errors
                GUI.Box(windowRect, "UI Inspector (Use SHIFT+H to Toggle)");
                
                // Simple message
                GUI.Label(new Rect(windowRect.x + 20, windowRect.y + 30, windowRect.width - 40, 50),
                    "Due to Unity method stripping, the interactive UI viewer cannot be displayed.\n" +
                    "Please check the console logs for UI hierarchy information.");
                
                // Close button
                if (GUI.Button(new Rect(windowRect.x + windowRect.width - 60, windowRect.y + 30, 40, 25), "Close"))
                {
                    windowVisible = false;
                }
                
                // Export button
                if (GUI.Button(new Rect(windowRect.x + windowRect.width - 60, windowRect.y + 65, 40, 25), "Log"))
                {
                    LogUIHierarchyToConsole();
                }
            }
            catch (Exception ex)
            {
                // Silently fail if there are more GUI errors
                // We'll still have the console logging as fallback
            }
        }
        
        private void LogUIHierarchyToConsole()
        {
            MelonLogger.Msg("=== UI HIERARCHY ===");
            
            Canvas[] canvases = GameObject.FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                if (canvas == null || !canvas.gameObject.activeInHierarchy) continue;
                
                MelonLogger.Msg($"Canvas: {canvas.name}");
                LogChildrenToConsole(canvas.transform, 1);
            }
            
            MelonLogger.Msg("=== END UI HIERARCHY ===");
        }
        
        private void LogChildrenToConsole(Transform parent, int depth)
        {
            if (parent == null) return;
            
            string indent = new string(' ', depth * 2);
            
            foreach (Transform child in parent)
            {
                if (child == null) continue;
                
                string activeStatus = child.gameObject.activeSelf ? "(active)" : "(inactive)";
                MelonLogger.Msg($"{indent}└─ {child.name} {activeStatus}");
                
                // Log components
                Component[] components = child.GetComponents<Component>();
                if (components.Length > 0)
                {
                    foreach (Component component in components)
                    {
                        if (component == null) continue;
                        
                        string typeName = component.GetType().Name;
                        // Skip common Unity components to reduce clutter
                        if (typeName != "RectTransform" && 
                            typeName != "CanvasRenderer" && 
                            !typeName.StartsWith("Unity"))
                        {
                            MelonLogger.Msg($"{indent}  └─ Component: {typeName}");
                        }
                    }
                }
                
                // Recursively log children
                LogChildrenToConsole(child, depth + 1);
            }
        }
        
        private void DrawHeaderControls(Rect rect)
        {
            float buttonWidth = 110f;
            
            // Remove the search functionality since TextField is causing errors
            // searchText remains as a variable but we don't provide UI for modifying it
            
            // Show inactive toggle
            showInactiveObjects = GUI.Toggle(
                new Rect(rect.x, rect.y, 120, 20), 
                showInactiveObjects, 
                "Show Inactive"
            );
            
            // Lock selection toggle
            lockSelection = GUI.Toggle(
                new Rect(rect.x + 120, rect.y, 120, 20), 
                lockSelection, 
                "Lock Selection"
            );
            
            // Stay selected on scene change
            staySelectedOnSceneChange = GUI.Toggle(
                new Rect(rect.x + 240, rect.y, 120, 20),
                staySelectedOnSceneChange,
                "Stay Selected On Scene Change"
            );
            
            // Export button
            if (GUI.Button(new Rect(rect.xMax - buttonWidth, rect.y, buttonWidth, 20), "Export"))
            {
                ExportSelectedObject();
            }
            
            // Refresh button (optional)
            if (GUI.Button(new Rect(rect.xMax - buttonWidth * 2 - 5, rect.y, buttonWidth, 20), "Refresh"))
            {
                // Force refresh of hierarchy (optional)
            }
        }
        
        private void DrawHierarchyPanel(Rect rect)
        {
            // Draw panel background
            GUI.Box(rect, "Object Hierarchy");
            
            // Adjust rect for scrolling content
            Rect contentRect = new Rect(rect.x + 5, rect.y + 20, rect.width - 10, rect.height - 25);
            
            // Start scrollable area for hierarchy
            hierarchyScrollPosition = GUI.BeginScrollView(
                contentRect,
                hierarchyScrollPosition,
                new Rect(0, 0, contentRect.width - 20, GetHierarchyHeight())
            );
            
            // Display root canvas objects
            Canvas[] canvases = GameObject.FindObjectsOfType<Canvas>();
            int yPos = 0;
            
            foreach (Canvas canvas in canvases.OrderBy(c => c.name))
            {
                // Skip inactive objects if not showing them
                if (!canvas.gameObject.activeInHierarchy && !showInactiveObjects)
                    continue;
                
                // Skip objects that don't match search
                if (!string.IsNullOrEmpty(searchText) && !canvas.name.ToLower().Contains(searchText.ToLower()))
                    continue;
                
                DrawObjectInHierarchy(canvas.gameObject, 0, ref yPos);
            }
            
            GUI.EndScrollView();
        }
        
        private void DrawObjectInHierarchy(GameObject obj, int depth, ref int yPos)
        {
            // Calculate indentation
            float indent = depth * 20f;
            Rect objRect = new Rect(indent, yPos, 300 - indent, 20);
            
            // Set different color for inactive objects
            Color originalColor = GUI.color;
            if (!obj.activeInHierarchy)
            {
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
            }
            
            // Highlight selected object
            if (obj == selectedObject)
            {
                GUI.color = Color.cyan;
            }
            
            // Draw the object button
            if (GUI.Button(objRect, obj.name))
            {
                if (!lockSelection)
                {
                    selectedObject = obj;
                    selectedComponent = null;
                    selectedObjectPath = GetGameObjectPath(obj);
                }
            }
            
            // Reset color
            GUI.color = originalColor;
            
            yPos += 20;
            
            // Draw children if this object matches search, or if search is empty
            bool shouldDrawChildren = string.IsNullOrEmpty(searchText) || 
                                      obj.name.ToLower().Contains(searchText.ToLower());
            
            if (shouldDrawChildren)
            {
                // Recursively draw children
                foreach (Transform child in obj.transform)
                {
                    // Skip inactive objects if not showing them
                    if (!child.gameObject.activeInHierarchy && !showInactiveObjects)
                        continue;
                    
                    DrawObjectInHierarchy(child.gameObject, depth + 1, ref yPos);
                }
            }
        }
        
        private void DrawDetailsPanel(Rect rect)
        {
            // Draw panel background
            GUI.Box(rect, "Object Details");
            
            // Adjust rect for scrolling content
            Rect contentRect = new Rect(rect.x + 5, rect.y + 20, rect.width - 10, rect.height - 25);
            
            // If no object is selected, show a message
            if (selectedObject == null)
            {
                GUI.Label(new Rect(contentRect.x, contentRect.y, contentRect.width, 20), 
                    "Select an object to view details");
                return;
            }
            
            // Start scrollable area for details
            detailsScrollPosition = GUI.BeginScrollView(
                contentRect,
                detailsScrollPosition,
                new Rect(0, 0, contentRect.width - 20, GetDetailsHeight())
            );
            
            int yPos = 0;
            
            // Show object name and path
            GUI.Label(new Rect(0, yPos, contentRect.width - 20, 20), 
                $"Name: {selectedObject.name}");
            yPos += 20;
            
            GUI.Label(new Rect(0, yPos, contentRect.width - 20, 20), 
                $"Path: {selectedObjectPath}");
            yPos += 20;
            
            GUI.Label(new Rect(0, yPos, contentRect.width - 20, 20), 
                $"Active: {selectedObject.activeSelf} (in hierarchy: {selectedObject.activeInHierarchy})");
            yPos += 20;
            
            // Show components list
            GUI.Label(new Rect(0, yPos, contentRect.width - 20, 20), 
                "Components:");
            yPos += 25;
            
            Component[] components = selectedObject.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component == null) continue;
                
                Rect compRect = new Rect(20, yPos, contentRect.width - 40, 20);
                
                // Highlight selected component
                Color originalColor = GUI.color;
                if (component == selectedComponent)
                {
                    GUI.color = Color.cyan;
                }
                
                string componentName = component.GetType().Name;
                if (GUI.Button(compRect, componentName))
                {
                    selectedComponent = component;
                }
                
                GUI.color = originalColor;
                yPos += 25;
            }
            
            // Draw horizontal line
            GUI.Box(new Rect(0, yPos, contentRect.width - 20, 2), "");
            yPos += 10;
            
            // Show component details
            if (selectedComponent != null)
            {
                DrawComponentDetails(contentRect.width - 20, ref yPos);
            }
            
            GUI.EndScrollView();
        }
        
        private void DrawComponentDetails(float width, ref int yPos)
        {
            GUI.Label(new Rect(0, yPos, width, 20), 
                $"Component: {selectedComponent.GetType().Name}");
            yPos += 25;
            
            Type type = selectedComponent.GetType();
            
            // Properties section
            GUI.Label(new Rect(0, yPos, width, 20), "Properties:");
            yPos += 25;
            
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                try
                {
                    string propName = property.Name;
                    string propValue = "null";
                    
                    // Skip specific Unity properties that often cause issues
                    if (propName == "rigidbody" || propName == "camera" || propName == "audio" || 
                        propName == "particleSystem" || propName == "renderer" || propName == "networkView")
                        continue;
                    
                    try
                    {
                        object value = property.GetValue(selectedComponent, null);
                        propValue = value != null ? value.ToString() : "null";
                    }
                    catch (Exception)
                    {
                        propValue = "[Error getting value]";
                    }
                    
                    GUI.Label(new Rect(20, yPos, width - 20, 20), $"{propName}: {propValue}");
                    yPos += 20;
                }
                catch (Exception)
                {
                    // Skip properties that throw errors
                }
            }
            
            // Fields section
            GUI.Label(new Rect(0, yPos, width, 20), "Fields:");
            yPos += 25;
            
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                try
                {
                    string fieldName = field.Name;
                    string fieldValue = "null";
                    
                    try
                    {
                        object value = field.GetValue(selectedComponent);
                        fieldValue = value != null ? value.ToString() : "null";
                    }
                    catch (Exception)
                    {
                        fieldValue = "[Error getting value]";
                    }
                    
                    GUI.Label(new Rect(20, yPos, width - 20, 20), $"{fieldName}: {fieldValue}");
                    yPos += 20;
                }
                catch (Exception)
                {
                    // Skip fields that throw errors
                }
            }
            
            // Methods section with scrolling
            GUI.Label(new Rect(0, yPos, width, 20), "Methods:");
            yPos += 25;
            
            // Get method height to determine scrollview size
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => !m.IsSpecialName) // Skip property getter/setters
                .ToArray();
            
            // Calculate methods area height and create a scrollview just for methods
            int methodsHeight = methods.Length * 20 + 10;
            Rect methodsScrollRect = new Rect(0, yPos, width, 200); // Fixed height for methods section
            Rect methodsContentRect = new Rect(0, 0, width - 20, methodsHeight);
            
            // Begin scrollview just for methods
            methodsScrollPosition = GUI.BeginScrollView(
                methodsScrollRect,
                methodsScrollPosition,
                methodsContentRect
            );
            
            int methodYPos = 0;
            foreach (MethodInfo method in methods)
            {
                try
                {
                    string methodName = method.Name;
                    
                    // Skip common Unity methods to reduce clutter
                    if (methodName.StartsWith("get_") || methodName.StartsWith("set_") ||
                        methodName == "ToString" || methodName == "GetHashCode" || 
                        methodName == "GetType" || methodName == "Equals")
                        continue;
                    
                    // Format parameters
                    string parameters = "";
                    foreach (var param in method.GetParameters())
                    {
                        if (!string.IsNullOrEmpty(parameters))
                            parameters += ", ";
                        parameters += $"{param.ParameterType.Name} {param.Name}";
                    }
                    
                    GUI.Label(new Rect(20, methodYPos, width - 40, 20), 
                        $"{methodName}({parameters})");
                    methodYPos += 20;
                }
                catch (Exception)
                {
                    // Skip methods that throw errors
                }
            }
            
            GUI.EndScrollView();
            
            // Update yPos to continue after the methods section
            yPos += 210; // Fixed height + small margin
        }
        
        private float GetHierarchyHeight()
        {
            // Estimate the total height of hierarchy
            Canvas[] canvases = GameObject.FindObjectsOfType<Canvas>();
            int count = 0;
            
            foreach (Canvas canvas in canvases)
            {
                if (!canvas.gameObject.activeInHierarchy && !showInactiveObjects)
                    continue;
                
                if (!string.IsNullOrEmpty(searchText) && !canvas.name.ToLower().Contains(searchText.ToLower()))
                    continue;
                
                count += CountHierarchyItems(canvas.gameObject);
            }
            
            return count * 20f;
        }
        
        private int CountHierarchyItems(GameObject obj)
        {
            int count = 1; // This object
            
            foreach (Transform child in obj.transform)
            {
                if (!child.gameObject.activeInHierarchy && !showInactiveObjects)
                    continue;
                
                count += CountHierarchyItems(child.gameObject);
            }
            
            return count;
        }
        
        private float GetDetailsHeight()
        {
            if (selectedObject == null)
                return 20f;
            
            float height = 100f; // Base height for object info
            
            // Add height for components
            Component[] components = selectedObject.GetComponents<Component>();
            height += components.Length * 25f;
            
            // Add height for component details
            if (selectedComponent != null)
            {
                Type type = selectedComponent.GetType();
                
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                height += properties.Length * 20f + 25f;
                
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                height += fields.Length * 20f + 25f;
                
                // Methods are in a separate scrollview now, so we don't need to add their height here
                height += 250f; // Fixed height for methods section including label
            }
            
            return height;
        }
        
        private string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return "";
            
            string path = obj.name;
            Transform parent = obj.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
        
        private void ExportSelectedObject()
        {
            if (selectedObject == null) return;
            
            try
            {
                using (StreamWriter writer = new StreamWriter(tempFilePath))
                {
                    writer.WriteLine($"Object: {selectedObject.name}");
                    writer.WriteLine($"Path: {selectedObjectPath}");
                    writer.WriteLine($"Active: {selectedObject.activeSelf} (in hierarchy: {selectedObject.activeInHierarchy})");
                    writer.WriteLine();
                    
                    writer.WriteLine("Components:");
                    Component[] components = selectedObject.GetComponents<Component>();
                    foreach (Component component in components)
                    {
                        if (component == null) continue;
                        
                        writer.WriteLine($"  - {component.GetType().Name}");
                    }
                    
                    writer.WriteLine();
                    
                    if (selectedComponent != null)
                    {
                        Type type = selectedComponent.GetType();
                        writer.WriteLine($"Selected Component: {type.Name}");
                        writer.WriteLine();
                        
                        writer.WriteLine("Properties:");
                        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        foreach (PropertyInfo property in properties)
                        {
                            try
                            {
                                string propName = property.Name;
                                string propValue = "null";
                                
                                try
                                {
                                    object value = property.GetValue(selectedComponent, null);
                                    propValue = value != null ? value.ToString() : "null";
                                }
                                catch (Exception)
                                {
                                    propValue = "[Error getting value]";
                                }
                                
                                writer.WriteLine($"  {propName}: {propValue}");
                            }
                            catch (Exception)
                            {
                                // Skip properties that throw errors
                            }
                        }
                        
                        writer.WriteLine();
                        
                        writer.WriteLine("Fields:");
                        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                        foreach (FieldInfo field in fields)
                        {
                            try
                            {
                                string fieldName = field.Name;
                                string fieldValue = "null";
                                
                                try
                                {
                                    object value = field.GetValue(selectedComponent);
                                    fieldValue = value != null ? value.ToString() : "null";
                                }
                                catch (Exception)
                                {
                                    fieldValue = "[Error getting value]";
                                }
                                
                                writer.WriteLine($"  {fieldName}: {fieldValue}");
                            }
                            catch (Exception)
                            {
                                // Skip fields that throw errors
                            }
                        }
                        
                        writer.WriteLine();
                        
                        writer.WriteLine("Methods:");
                        MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                        List<MethodInfo> filteredMethods = new List<MethodInfo>();
                        
                        foreach (MethodInfo m in methods)
                        {
                            if (!m.IsSpecialName)
                                filteredMethods.Add(m);
                        }
                        
                        foreach (MethodInfo method in filteredMethods)
                        {
                            try
                            {
                                string methodName = method.Name;
                                
                                // Skip common Unity methods to reduce clutter
                                if (methodName.StartsWith("get_") || methodName.StartsWith("set_") ||
                                    methodName == "ToString" || methodName == "GetHashCode" || 
                                    methodName == "GetType" || methodName == "Equals")
                                    continue;
                                
                                // Format parameters
                                string parameters = "";
                                foreach (var param in method.GetParameters())
                                {
                                    if (!string.IsNullOrEmpty(parameters))
                                        parameters += ", ";
                                    parameters += $"{param.ParameterType.Name} {param.Name}";
                                }
                                
                                writer.WriteLine($"  {methodName}({parameters})");
                            }
                            catch (Exception)
                            {
                                // Skip methods that throw errors
                            }
                        }
                    }
                }
                
                MelonLogger.Msg($"Exported UI object data to: {tempFilePath}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to export object data: {ex.Message}");
            }
        }
    }
} 
