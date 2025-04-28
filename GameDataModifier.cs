using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using UnityEngine;

namespace UIInspectorMod
{
    public class GameDataModifier
    {
        private static GameDataModifier instance;
        private bool showModifierUI = false;
        private Vector2 scrollPosition;
        private string saveDirectoryPath;
        private string backupDirectoryPath;
        private List<string> modifiableFiles = new List<string>();
        private Dictionary<string, JsonNode> loadedJsonData = new Dictionary<string, JsonNode>();
        private string selectedFile = "";
        private string modificationMessage = "";
        private bool showSuccessMessage = false;
        private float messageTimer = 0f;

        public static GameDataModifier Instance
        {
            get
            {
                if (instance == null)
                    instance = new GameDataModifier();
                return instance;
            }
        }

        public GameDataModifier()
        {
            // Find the save directory path
            string gameDataPath = Path.Combine(Application.dataPath, "StreamingAssets");
            
            // First check if we have actual save files in a user directory
            string userSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Schedule I", "Saves");
            
            if (Directory.Exists(userSavePath))
            {
                saveDirectoryPath = userSavePath;
                MelonLogger.Msg($"Using user save directory: {saveDirectoryPath}");
            }
            else
            {
                // Fall back to default save template
                saveDirectoryPath = Path.Combine(gameDataPath, "DefaultSave");
                MelonLogger.Msg($"Using default save template: {saveDirectoryPath}");
            }
            
            // Create backup directory
            backupDirectoryPath = Path.Combine(Path.GetDirectoryName(typeof(GameDataModifier).Assembly.Location), "SaveBackups");
            if (!Directory.Exists(backupDirectoryPath))
            {
                Directory.CreateDirectory(backupDirectoryPath);
            }
            
            // Find all modifiable JSON files
            FindModifiableFiles();
        }

        public bool ShowModifierUI
        {
            get { return showModifierUI; }
            set { showModifierUI = value; }
        }

        public void ToggleModifierUI()
        {
            showModifierUI = !showModifierUI;
            if (showModifierUI)
            {
                FindModifiableFiles();
                LoadJsonFiles();
            }
        }

        private void FindModifiableFiles()
        {
            modifiableFiles.Clear();
            
            // List common interesting files first
            string[] interestingFiles = {
                "Money.json",
                "Products.json",
                "Law.json",
                "Time.json",
                "Game.json"
            };
            
            foreach (string file in interestingFiles)
            {
                string fullPath = Path.Combine(saveDirectoryPath, file);
                if (File.Exists(fullPath))
                {
                    modifiableFiles.Add(file);
                }
            }
            
            // Add NPC files
            string npcPath = Path.Combine(saveDirectoryPath, "NPCs");
            if (Directory.Exists(npcPath))
            {
                foreach (string npcDir in Directory.GetDirectories(npcPath))
                {
                    string npcName = Path.GetFileName(npcDir);
                    
                    // Add key NPC files
                    string[] npcFiles = {
                        "CustomerData.json",
                        "Relationship.json"
                    };
                    
                    foreach (string file in npcFiles)
                    {
                        string fullPath = Path.Combine(npcDir, file);
                        if (File.Exists(fullPath))
                        {
                            modifiableFiles.Add($"NPCs/{npcName}/{file}");
                        }
                    }
                }
            }
            
            // Add Business files
            string businessPath = Path.Combine(saveDirectoryPath, "Businesses");
            if (Directory.Exists(businessPath))
            {
                foreach (string businessDir in Directory.GetDirectories(businessPath))
                {
                    string businessName = Path.GetFileName(businessDir);
                    string fullPath = Path.Combine(businessDir, "Business.json");
                    
                    if (File.Exists(fullPath))
                    {
                        modifiableFiles.Add($"Businesses/{businessName}/Business.json");
                    }
                }
            }
            
            MelonLogger.Msg($"Found {modifiableFiles.Count} modifiable files");
        }

        private void LoadJsonFiles()
        {
            loadedJsonData.Clear();
            
            foreach (string file in modifiableFiles)
            {
                try
                {
                    string fullPath = Path.Combine(saveDirectoryPath, file);
                    string jsonContent = File.ReadAllText(fullPath);
                    JsonNode jsonNode = JsonNode.Parse(jsonContent);
                    loadedJsonData[file] = jsonNode;
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Failed to load {file}: {ex.Message}");
                }
            }
        }

        public void OnGUI()
        {
            if (!showModifierUI) return;
            
            // Update message timer
            if (showSuccessMessage)
            {
                messageTimer -= Time.deltaTime;
                if (messageTimer <= 0f)
                {
                    showSuccessMessage = false;
                }
            }
            
            // Calculate window position (left side of the screen)
            float windowWidth = 500f;
            float windowHeight = 400f;
            float x = 20f;
            float y = 20f;
            
            GUI.Box(new Rect(x, y, windowWidth, windowHeight), "Game Data Modifier");
            
            // Files list panel
            GUI.Label(new Rect(x + 10, y + 30, 150, 20), "Select a file to modify:");
            
            // File list scrollable area
            scrollPosition = GUI.BeginScrollView(
                new Rect(x + 10, y + 50, 150, windowHeight - 70),
                scrollPosition,
                new Rect(0, 0, 130, modifiableFiles.Count * 25)
            );
            
            for (int i = 0; i < modifiableFiles.Count; i++)
            {
                string file = modifiableFiles[i];
                bool isSelected = file == selectedFile;
                
                GUI.backgroundColor = isSelected ? Color.cyan : Color.white;
                if (GUI.Button(new Rect(5, i * 25, 120, 20), Path.GetFileName(file)))
                {
                    selectedFile = file;
                }
                GUI.backgroundColor = Color.white;
            }
            
            GUI.EndScrollView();
            
            // Data editor panel
            if (!string.IsNullOrEmpty(selectedFile) && loadedJsonData.ContainsKey(selectedFile))
            {
                JsonNode data = loadedJsonData[selectedFile];
                float editorX = x + 170;
                float editorY = y + 30;
                float editorWidth = windowWidth - 180;
                
                GUI.Box(new Rect(editorX, editorY, editorWidth, windowHeight - 40), selectedFile);
                
                // Command buttons
                if (GUI.Button(new Rect(editorX + 10, editorY + 30, 100, 25), "Reload File"))
                {
                    try
                    {
                        string fullPath = Path.Combine(saveDirectoryPath, selectedFile);
                        string jsonContent = File.ReadAllText(fullPath);
                        loadedJsonData[selectedFile] = JsonNode.Parse(jsonContent);
                        
                        modificationMessage = "File reloaded successfully.";
                        showSuccessMessage = true;
                        messageTimer = 3f;
                    }
                    catch (Exception ex)
                    {
                        modificationMessage = $"Error reloading: {ex.Message}";
                        showSuccessMessage = true;
                        messageTimer = 3f;
                    }
                }
                
                if (GUI.Button(new Rect(editorX + 120, editorY + 30, 100, 25), "Create Backup"))
                {
                    BackupFile(selectedFile);
                }
                
                // Common modification buttons
                if (selectedFile == "Money.json")
                {
                    if (GUI.Button(new Rect(editorX + 10, editorY + 70, 150, 25), "Add $1000"))
                    {
                        ModifyMoney(1000);
                    }
                    
                    if (GUI.Button(new Rect(editorX + 10, editorY + 100, 150, 25), "Add $10,000"))
                    {
                        ModifyMoney(10000);
                    }
                    
                    if (GUI.Button(new Rect(editorX + 10, editorY + 130, 150, 25), "Add $100,000"))
                    {
                        ModifyMoney(100000);
                    }
                }
                else if (selectedFile == "Law.json")
                {
                    if (GUI.Button(new Rect(editorX + 10, editorY + 70, 150, 25), "Set Law Intensity Low"))
                    {
                        ModifyLawIntensity(0.1f);
                    }
                    
                    if (GUI.Button(new Rect(editorX + 10, editorY + 100, 150, 25), "Set Law Intensity High"))
                    {
                        ModifyLawIntensity(0.75f);
                    }
                }
                else if (selectedFile == "Products.json")
                {
                    if (GUI.Button(new Rect(editorX + 10, editorY + 70, 150, 25), "Unlock All Products"))
                    {
                        UnlockAllProducts();
                    }
                    
                    if (GUI.Button(new Rect(editorX + 10, editorY + 100, 150, 25), "Lower All Prices"))
                    {
                        ModifyProductPrices(0.5f);
                    }
                    
                    if (GUI.Button(new Rect(editorX + 10, editorY + 130, 150, 25), "Increase All Prices"))
                    {
                        ModifyProductPrices(1.5f);
                    }
                }
                else if (selectedFile.Contains("CustomerData.json"))
                {
                    if (GUI.Button(new Rect(editorX + 10, editorY + 70, 150, 25), "Increase Dependence"))
                    {
                        ModifyCustomerDependence(0.8f);
                    }
                    
                    if (GUI.Button(new Rect(editorX + 10, editorY + 100, 150, 25), "Unlock All Products"))
                    {
                        UnlockCustomerProducts();
                    }
                }
                else if (selectedFile.Contains("Business.json"))
                {
                    if (GUI.Button(new Rect(editorX + 10, editorY + 70, 150, 25), "Make Owned"))
                    {
                        SetBusinessOwned(true);
                    }
                }
                
                // Display JSON data
                string jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                GUI.Label(new Rect(editorX + 10, editorY + 170, editorWidth - 20, windowHeight - 180), jsonString);
            }
            
            // Display modification message
            if (showSuccessMessage)
            {
                GUI.backgroundColor = Color.green;
                GUI.Box(new Rect(x + 20, y + windowHeight - 30, windowWidth - 40, 20), modificationMessage);
                GUI.backgroundColor = Color.white;
            }
            
            // Close button
            if (GUI.Button(new Rect(x + windowWidth - 30, y + 5, 25, 20), "X"))
            {
                showModifierUI = false;
            }
        }

        private void BackupFile(string file)
        {
            try
            {
                string fullPath = Path.Combine(saveDirectoryPath, file);
                if (File.Exists(fullPath))
                {
                    // Create timestamp
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    
                    // Create directory structure in backups if needed
                    string relativeDir = Path.GetDirectoryName(file);
                    string backupDir = backupDirectoryPath;
                    
                    if (!string.IsNullOrEmpty(relativeDir))
                    {
                        backupDir = Path.Combine(backupDirectoryPath, relativeDir);
                        if (!Directory.Exists(backupDir))
                        {
                            Directory.CreateDirectory(backupDir);
                        }
                    }
                    
                    // Create backup filename with timestamp
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string extension = Path.GetExtension(file);
                    string backupFileName = $"{fileName}_{timestamp}{extension}";
                    string backupPath = Path.Combine(backupDir, backupFileName);
                    
                    // Copy file
                    File.Copy(fullPath, backupPath);
                    
                    modificationMessage = $"Backup created: {backupFileName}";
                    showSuccessMessage = true;
                    messageTimer = 3f;
                }
            }
            catch (Exception ex)
            {
                modificationMessage = $"Backup failed: {ex.Message}";
                showSuccessMessage = true;
                messageTimer = 3f;
            }
        }

        private void SaveFile(string file)
        {
            try
            {
                if (loadedJsonData.ContainsKey(file))
                {
                    // Create backup first
                    BackupFile(file);
                    
                    // Save the modified JSON
                    string fullPath = Path.Combine(saveDirectoryPath, file);
                    string jsonString = JsonSerializer.Serialize(loadedJsonData[file], new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(fullPath, jsonString);
                    
                    modificationMessage = "File saved successfully.";
                    showSuccessMessage = true;
                    messageTimer = 3f;
                }
            }
            catch (Exception ex)
            {
                modificationMessage = $"Save failed: {ex.Message}";
                showSuccessMessage = true;
                messageTimer = 3f;
            }
        }

        private void ModifyMoney(float amount)
        {
            try
            {
                if (loadedJsonData.ContainsKey("Money.json"))
                {
                    JsonNode moneyData = loadedJsonData["Money.json"];
                    
                    // Get current balance
                    float currentBalance = 0;
                    if (moneyData["OnlineBalance"] != null)
                    {
                        currentBalance = (float)moneyData["OnlineBalance"].GetValue<double>();
                    }
                    
                    // Add the amount
                    moneyData["OnlineBalance"] = currentBalance + amount;
                    
                    // Save the file
                    SaveFile("Money.json");
                    
                    modificationMessage = $"Added ${amount} to balance.";
                    showSuccessMessage = true;
                    messageTimer = 3f;
                }
            }
            catch (Exception ex)
            {
                modificationMessage = $"Failed to modify money: {ex.Message}";
                showSuccessMessage = true;
                messageTimer = 3f;
            }
        }

        private void ModifyLawIntensity(float intensity)
        {
            try
            {
                if (loadedJsonData.ContainsKey("Law.json"))
                {
                    JsonNode lawData = loadedJsonData["Law.json"];
                    
                    // Set law intensity (0 = none, 1 = maximum)
                    lawData["InternalLawIntensity"] = intensity;
                    
                    // Save the file
                    SaveFile("Law.json");
                    
                    modificationMessage = $"Set law intensity to {intensity}.";
                    showSuccessMessage = true;
                    messageTimer = 3f;
                }
            }
            catch (Exception ex)
            {
                modificationMessage = $"Failed to modify law intensity: {ex.Message}";
                showSuccessMessage = true;
                messageTimer = 3f;
            }
        }

        private void UnlockAllProducts()
        {
            try
            {
                if (loadedJsonData.ContainsKey("Products.json"))
                {
                    JsonNode productsData = loadedJsonData["Products.json"];
                    
                    // List of product IDs to unlock (from what we saw in the file)
                    string[] allProducts = {
                        "ogkush", "sourdiesel", "greencrack", "granddaddypurple", 
                        "meth", "cocaine"
                    };
                    
                    // Get discovered products array
                    JsonArray discoveredProducts = productsData["DiscoveredProducts"].AsArray();
                    
                    // Add missing products
                    foreach (string product in allProducts)
                    {
                        bool found = false;
                        foreach (JsonNode node in discoveredProducts)
                        {
                            if (node.GetValue<string>() == product)
                            {
                                found = true;
                                break;
                            }
                        }
                        
                        if (!found)
                        {
                            discoveredProducts.Add(product);
                        }
                    }
                    
                    // Save the file
                    SaveFile("Products.json");
                    
                    modificationMessage = "All products unlocked.";
                    showSuccessMessage = true;
                    messageTimer = 3f;
                }
            }
            catch (Exception ex)
            {
                modificationMessage = $"Failed to unlock products: {ex.Message}";
                showSuccessMessage = true;
                messageTimer = 3f;
            }
        }

        private void ModifyProductPrices(float multiplier)
        {
            try
            {
                if (loadedJsonData.ContainsKey("Products.json"))
                {
                    JsonNode productsData = loadedJsonData["Products.json"];
                    
                    // Get product prices array
                    JsonArray productPrices = productsData["ProductPrices"].AsArray();
                    
                    // Modify each price
                    foreach (JsonNode priceNode in productPrices)
                    {
                        int currentPrice = priceNode["Int"].GetValue<int>();
                        int newPrice = (int)(currentPrice * multiplier);
                        priceNode["Int"] = newPrice;
                    }
                    
                    // Save the file
                    SaveFile("Products.json");
                    
                    modificationMessage = $"Product prices modified by {multiplier}x.";
                    showSuccessMessage = true;
                    messageTimer = 3f;
                }
            }
            catch (Exception ex)
            {
                modificationMessage = $"Failed to modify product prices: {ex.Message}";
                showSuccessMessage = true;
                messageTimer = 3f;
            }
        }

        private void ModifyCustomerDependence(float value)
        {
            try
            {
                if (selectedFile.Contains("CustomerData.json") && loadedJsonData.ContainsKey(selectedFile))
                {
                    JsonNode customerData = loadedJsonData[selectedFile];
                    
                    // Set dependence (higher value = more dependence)
                    customerData["Dependence"] = value;
                    
                    // Save the file
                    SaveFile(selectedFile);
                    
                    modificationMessage = $"Set customer dependence to {value}.";
                    showSuccessMessage = true;
                    messageTimer = 3f;
                }
            }
            catch (Exception ex)
            {
                modificationMessage = $"Failed to modify customer dependence: {ex.Message}";
                showSuccessMessage = true;
                messageTimer = 3f;
            }
        }

        private void UnlockCustomerProducts()
        {
            try
            {
                if (selectedFile.Contains("CustomerData.json") && loadedJsonData.ContainsKey(selectedFile))
                {
                    JsonNode customerData = loadedJsonData[selectedFile];
                    
                    // List of product IDs to unlock
                    string[] allProducts = {
                        "ogkush", "sourdiesel", "greencrack", "granddaddypurple", 
                        "meth", "cocaine"
                    };
                    
                    // Get purchaseable products array
                    JsonArray purchaseableProducts = customerData["PurchaseableProducts"].AsArray();
                    
                    // Clear existing and add all products
                    purchaseableProducts.Clear();
                    foreach (string product in allProducts)
                    {
                        purchaseableProducts.Add(product);
                    }
                    
                    // Save the file
                    SaveFile(selectedFile);
                    
                    modificationMessage = "All products unlocked for customer.";
                    showSuccessMessage = true;
                    messageTimer = 3f;
                }
            }
            catch (Exception ex)
            {
                modificationMessage = $"Failed to unlock customer products: {ex.Message}";
                showSuccessMessage = true;
                messageTimer = 3f;
            }
        }

        private void SetBusinessOwned(bool owned)
        {
            try
            {
                if (selectedFile.Contains("Business.json") && loadedJsonData.ContainsKey(selectedFile))
                {
                    JsonNode businessData = loadedJsonData[selectedFile];
                    
                    // Set IsOwned property
                    businessData["IsOwned"] = owned;
                    
                    // Save the file
                    SaveFile(selectedFile);
                    
                    modificationMessage = owned ? "Business now owned." : "Business now not owned.";
                    showSuccessMessage = true;
                    messageTimer = 3f;
                }
            }
            catch (Exception ex)
            {
                modificationMessage = $"Failed to change business ownership: {ex.Message}";
                showSuccessMessage = true;
                messageTimer = 3f;
            }
        }
    }
} 