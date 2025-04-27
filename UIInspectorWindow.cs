using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace UIInspectorMod
{
    public class UIInspectorWindow
    {
        private static UIInspectorWindow instance;
        
        // Window properties
        private bool isVisible = false;
        private Vector2 scrollPosition;
        private Dictionary<int, bool> expandedObjects = new Dictionary<int, bool>();
        private GameObject selectedGameObject;
        private Component selectedComponent;
        private float windowWidth = 600f;
        private float windowHeight = 500f;
        private Rect windowRect;
        private bool isDragging = false;
        private Vector2 dragOffset;
        
        // Inspector data
        private Dictionary<string, List<GameObject>> inspectedElements = new Dictionary<string, List<GameObject>>();
        
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
            // Initialize window position (right side of the screen)
            windowRect = new Rect(Screen.width - windowWidth - 20, 20, windowWidth, windowHeight);
        }
        
        public bool IsVisible 
        { 
            get { return isVisible; }
            set { isVisible = value; }
        }
        
        public void ToggleVisibility()
        {
            isVisible = !isVisible;
            MelonLoader.MelonLogger.Msg($"UI Inspector visibility toggled: {isVisible}");
        }
        
        public void SetInspectedElements(Dictionary<string, List<GameObject>> elements)
        {
            inspectedElements = elements;
        }
        
        public void OnGUI()
        {
            if (!isVisible) return;
            
            // Draw our custom window
            DrawCustomWindow();
        }
        
        private void DrawCustomWindow()
        {
            // Draw window background
            GUI.Box(windowRect, "");
            
            // Window header
            GUI.Box(new Rect(windowRect.x, windowRect.y, windowRect.width, 25), "UI Inspector");
            
            // Close button
            if (GUI.Button(new Rect(windowRect.x + windowRect.width - 25, windowRect.y + 2, 20, 20), "X"))
            {
                ToggleVisibility();
                return;
            }
            
            // Handle window dragging
            Rect dragArea = new Rect(windowRect.x, windowRect.y, windowRect.width - 25, 25);
            if (Event.current.type == EventType.MouseDown && dragArea.Contains(Event.current.mousePosition))
            {
                isDragging = true;
                dragOffset = Event.current.mousePosition - new Vector2(windowRect.x, windowRect.y);
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                isDragging = false;
            }
            
            if (isDragging && Event.current.type == EventType.MouseDrag)
            {
                windowRect.x = Event.current.mousePosition.x - dragOffset.x;
                windowRect.y = Event.current.mousePosition.y - dragOffset.y;
                Event.current.Use();
            }
            
            // Content area
            Rect contentRect = new Rect(windowRect.x + 5, windowRect.y + 30, windowRect.width - 10, windowRect.height - 35);
            GUI.BeginGroup(contentRect);
            
            // Left panel - hierarchy tree
            Rect leftPanelRect = new Rect(0, 0, contentRect.width * 0.4f, contentRect.height);
            GUI.Box(leftPanelRect, "");
            GUI.Label(new Rect(5, 5, leftPanelRect.width - 10, 20), "UI Elements:");
            
            Rect scrollViewRect = new Rect(5, 30, leftPanelRect.width - 10, leftPanelRect.height - 35);
            scrollPosition = GUI.BeginScrollView(scrollViewRect, scrollPosition, new Rect(0, 0, scrollViewRect.width - 20, Mathf.Max(500, inspectedElements.Count * 300)));
            
            float yPos = 5;
            // Display canvases and their children
            foreach (var kvp in inspectedElements)
            {
                string canvasName = kvp.Key;
                List<GameObject> elements = kvp.Value;
                
                if (elements.Count > 0 && elements[0] != null)
                {
                    // Canvas header
                    bool expanded = GetExpanded(elements[0].GetInstanceID());
                    if (GUI.Button(new Rect(5, yPos, scrollViewRect.width - 30, 25), canvasName))
                    {
                        SetExpanded(elements[0].GetInstanceID(), !expanded);
                    }
                    yPos += 30;
                    
                    // Draw children if expanded
                    if (expanded)
                    {
                        foreach (var element in elements)
                        {
                            if (element != null && element != elements[0])
                            {
                                yPos = DrawElementSimple(element, elements[0].transform, 1, yPos, scrollViewRect.width - 30);
                            }
                        }
                    }
                }
            }
            
            GUI.EndScrollView();
            
            // Right panel - details view
            Rect rightPanelRect = new Rect(contentRect.width * 0.4f + 5, 0, contentRect.width * 0.6f - 5, contentRect.height);
            GUI.Box(rightPanelRect, "");
            
            // Show details of selected object
            if (selectedGameObject != null)
            {
                GUI.Label(new Rect(rightPanelRect.x + 5, 5, rightPanelRect.width - 10, 25), $"Selected: {selectedGameObject.name}");
                
                // Display basic properties
                float detailsYPos = 35;
                GUI.Label(new Rect(rightPanelRect.x + 5, detailsYPos, rightPanelRect.width - 10, 20), $"Active: {selectedGameObject.activeSelf}");
                detailsYPos += 25;
                
                if (selectedGameObject.GetComponent<RectTransform>() is RectTransform rt)
                {
                    GUI.Label(new Rect(rightPanelRect.x + 5, detailsYPos, rightPanelRect.width - 10, 20), $"Position: {rt.anchoredPosition}");
                    detailsYPos += 25;
                    GUI.Label(new Rect(rightPanelRect.x + 5, detailsYPos, rightPanelRect.width - 10, 20), $"Size: {rt.sizeDelta}");
                    detailsYPos += 25;
                }
                
                // Components list
                GUI.Label(new Rect(rightPanelRect.x + 5, detailsYPos, rightPanelRect.width - 10, 25), "Components:");
                detailsYPos += 30;
                
                Component[] components = selectedGameObject.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null) continue;
                    
                    GUI.color = selectedComponent == component ? Color.green : Color.white;
                    if (GUI.Button(new Rect(rightPanelRect.x + 5, detailsYPos, rightPanelRect.width - 10, 25), component.GetType().Name))
                    {
                        selectedComponent = component;
                    }
                    GUI.color = Color.white;
                    detailsYPos += 30;
                }
                
                // Component details
                if (selectedComponent != null)
                {
                    GUI.Label(new Rect(rightPanelRect.x + 5, detailsYPos, rightPanelRect.width - 10, 25), $"Component: {selectedComponent.GetType().Name}");
                    detailsYPos += 30;
                    
                    // Get public properties
                    PropertyInfo[] properties = selectedComponent.GetType().GetProperties(
                        BindingFlags.Public | BindingFlags.Instance);
                    
                    foreach (var prop in properties)
                    {
                        try
                        {
                            object value = prop.GetValue(selectedComponent);
                            string valueStr = value != null ? value.ToString() : "null";
                            GUI.Label(new Rect(rightPanelRect.x + 5, detailsYPos, rightPanelRect.width - 10, 20), $"{prop.Name}: {valueStr}");
                            detailsYPos += 25;
                        }
                        catch
                        {
                            GUI.Label(new Rect(rightPanelRect.x + 5, detailsYPos, rightPanelRect.width - 10, 20), $"{prop.Name}: [Error reading]");
                            detailsYPos += 25;
                        }
                    }
                    
                    // Get public methods
                    MethodInfo[] methods = selectedComponent.GetType().GetMethods(
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    
                    if (methods.Length > 0)
                    {
                        GUI.Label(new Rect(rightPanelRect.x + 5, detailsYPos, rightPanelRect.width - 10, 25), "Methods:");
                        detailsYPos += 30;
                        
                        foreach (var method in methods)
                        {
                            string parameters = "";
                            foreach (var param in method.GetParameters())
                            {
                                if (parameters.Length > 0) parameters += ", ";
                                parameters += $"{param.ParameterType.Name} {param.Name}";
                            }
                            
                            GUI.Label(new Rect(rightPanelRect.x + 5, detailsYPos, rightPanelRect.width - 10, 20), $"{method.ReturnType.Name} {method.Name}({parameters})");
                            detailsYPos += 25;
                        }
                    }
                }
            }
            else
            {
                GUI.Label(new Rect(rightPanelRect.x + 5, 5, rightPanelRect.width - 10, 25), "Select an element to view details");
            }
            
            GUI.EndGroup();
        }
        
        private float DrawElementSimple(GameObject element, Transform rootTransform, int indentLevel, float yPos, float width)
        {
            if (element == null) return yPos;
            
            GUI.color = selectedGameObject == element ? Color.cyan : Color.white;
            
            if (GUI.Button(new Rect(5 + indentLevel * 20, yPos, width - indentLevel * 20, 25), element.name))
            {
                selectedGameObject = element;
                selectedComponent = null;
            }
            
            GUI.color = Color.white;
            yPos += 30;
            
            // Check if expanded
            bool expanded = GetExpanded(element.GetInstanceID());
            if (expanded && element.transform.childCount > 0)
            {
                // Draw children
                for (int i = 0; i < element.transform.childCount; i++)
                {
                    Transform child = element.transform.GetChild(i);
                    yPos = DrawElementSimple(child.gameObject, rootTransform, indentLevel + 1, yPos, width);
                }
            }
            
            return yPos;
        }
        
        private bool GetExpanded(int id)
        {
            if (!expandedObjects.ContainsKey(id))
            {
                expandedObjects[id] = false;
            }
            return expandedObjects[id];
        }
        
        private void SetExpanded(int id, bool expanded)
        {
            expandedObjects[id] = expanded;
        }
    }
} 