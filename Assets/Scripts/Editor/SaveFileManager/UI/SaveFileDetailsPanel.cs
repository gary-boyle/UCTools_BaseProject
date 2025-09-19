// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Reflection;
// using GameFramework.DataStructures;
// using GameFramework.Editor.SaveFileManager.ScriptableObjects;
// using GameFramework.SaveSystem.Data;
// using GameFramework.SaveSystem.Utilities;
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.UIElements;
//
// namespace GameFramework.Editor.SaveFileManager.UI
// {
//     /// <summary>
//     /// Enhanced panel for displaying detailed information about a selected save file
//     /// 
//     /// Features:
//     /// - Loads full SaveFileData directly from JSON for complete information access
//     /// - Displays nested objects (GameSessionData, PlayerData) with all their fields
//     /// - Supports Vector3, DateTime, and other Unity/complex types with proper formatting  
//     /// - Configurable field display through SaveFileDisplayConfig (supports nested field paths)
//     /// - Dynamic field discovery to automatically show all available data (extensible for future arbitrary data)
//     /// - Uses reflection-based field display for maximum flexibility with changing save structures
//     /// 
//     /// Configuration:
//     /// - Use dot notation in SaveFileDisplayConfig for nested fields (e.g., "PlayerData.uniqueID")
//     /// - Toggle ShowDynamicFieldDiscovery to show/hide complete data structure discovery
//     /// - System automatically handles new fields added to SaveFileData structure
//     /// </summary>
//     public class SaveFileDetailsPanel : VisualElement
//     {
//         private const string UXMLPath = "Assets/Scripts/Editor/SaveFileManager/UI/UXML/SaveFileDetailsPanel.uxml";
//         private const string FieldItemUXMLPath = "Assets/Scripts/Editor/SaveFileManager/UI/UXML/FieldDisplayItem.uxml";
//         
//         // UI References
//         private VisualElement _noSelection;
//         private VisualElement _detailsContent;
//         private Button _loadButton;
//         private Button _deleteButton;
//         private Button _showExplorerButton;
//         private VisualElement _playModeWarning;
//         private ScrollView _fieldsContainer;
//         private VisualElement _rawDataSection;
//         private ScrollView _rawDataContainer;
//         private Label _rawDataContent;
//         
//         // Data
//         private SaveFileInfo _currentSave;
//         private SaveFileData _currentSaveData; // Full save data for enhanced display
//         private ScriptableObjects.SaveFileDisplayConfig _displayConfig;
//         private VisualTreeAsset _fieldItemTemplate; 
//         
//         // Events
//         public event Action<SaveFileInfo> OnLoadRequested;
//         public event Action<SaveFileInfo> OnDeleteRequested;
//         public event Action<SaveFileInfo> OnShowInExplorerRequested;
//         
//         public SaveFileDetailsPanel()
//         {
//             LoadUI();
//         }
//         
//         private void LoadUI()
//         {
//             var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXMLPath);
//             if (uxml == null)
//             {
//                 Debug.LogError($"Could not load UXML file at {UXMLPath}");
//                 return;
//             }
//             
//             // Load field item template
//             _fieldItemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(FieldItemUXMLPath);
//             if (_fieldItemTemplate == null)
//             {
//                 Debug.LogError($"Could not load field item template at {FieldItemUXMLPath}");
//                 return;
//             }
//             
//             uxml.CloneTree(this);
//             
//             GetUIReferences();
//             SetupEventHandlers();
//             UpdatePlayModeWarning();
//         }
//         
//         private void GetUIReferences()
//         {
//             _noSelection = this.Q("no-selection");
//             _detailsContent = this.Q("details-content");
//             _loadButton = this.Q<Button>("load-button");
//             _deleteButton = this.Q<Button>("delete-button");
//             _showExplorerButton = this.Q<Button>("show-explorer-button");
//             _playModeWarning = this.Q("play-mode-warning");
//             _fieldsContainer = this.Q<ScrollView>("fields-container");
//             _rawDataSection = this.Q("raw-data-section");
//             _rawDataContainer = this.Q<ScrollView>("raw-data-container");
//             _rawDataContent = this.Q<Label>("raw-data-content");
//         }
//         
//         private void SetupEventHandlers()
//         {
//             _loadButton.clicked += () => OnLoadRequested?.Invoke(_currentSave);
//             _deleteButton.clicked += () => OnDeleteRequested?.Invoke(_currentSave);
//             _showExplorerButton.clicked += () => OnShowInExplorerRequested?.Invoke(_currentSave);
//         }
//         
//         public void LoadSaveFile(SaveFileInfo saveInfo, ScriptableObjects.SaveFileDisplayConfig displayConfig, bool showRawData)
//         {
//             _currentSave = saveInfo;
//             _displayConfig = displayConfig;
//             _currentSaveData = null; // Reset full save data
//             
//             if (saveInfo == null)
//             {
//                 ShowNoSelection();
//                 return;
//             }
//             
//             // Load full save data for enhanced display
//             LoadFullSaveData();
//             
//             ShowDetailsContent();
//             UpdateFieldsDisplay();
//             UpdateRawDataDisplay(showRawData);
//             UpdatePlayModeWarning();
//         }
//         
//         private void ShowNoSelection()
//         {
//             _noSelection.style.display = DisplayStyle.Flex;
//             _detailsContent.style.display = DisplayStyle.None;
//         }
//         
//         private void ShowDetailsContent()
//         {
//             _noSelection.style.display = DisplayStyle.None;
//             _detailsContent.style.display = DisplayStyle.Flex;
//         }
//         
//         private void LoadFullSaveData()
//         {
//             if (_currentSave == null) return;
//             
//             try
//             {
//                 var savePath = GetSaveFilePath(_currentSave.FileName);
//                 if (File.Exists(savePath))
//                 {
//                     var jsonContent = File.ReadAllText(savePath);
//                     _currentSaveData = JsonSerializationHelper.DeserializeFromJson<SaveFileData>(jsonContent);
//                 }
//                 else
//                 {
//                     Debug.LogWarning($"[SaveFileDetailsPanel] Save file not found: {savePath}");
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"[SaveFileDetailsPanel] Failed to load full save data: {ex.Message}");
//             }
//         }
//         
//         private void UpdateFieldsDisplay()
//         {
//             _fieldsContainer.Clear();
//             
//             if (_currentSave == null) return;
//             
//             if (_displayConfig?.DisplayFields != null && _displayConfig.DisplayFields.Count > 0)
//             {
//                 // Use configured fields - but now support nested paths
//                 foreach (var fieldConfig in _displayConfig.DisplayFields)
//                 {
//                     CreateFieldDisplayFromConfig(fieldConfig);
//                 }
//             }
//             else
//             {
//                 // Use enhanced default fields showing full save data
//                 CreateEnhancedDefaultFieldDisplays();
//             }
//             
//             // Show dynamic field discovery if enabled (for debugging and future extensibility)
//             if (_displayConfig?.ShowDynamicFieldDiscovery == true)
//             {
//                 CreateSectionHeader("Complete Save Data (All Fields)");
//                 CreateDynamicFieldDiscovery();
//             }
//         }
//         
//         private void CreateEnhancedDefaultFieldDisplays()
//         {
//             // Basic save file info
//             CreateSectionHeader("Save File Information");
//             CreateFieldDisplay(nameof(SaveFileInfo.FileName), "File Name", true, _currentSave);
//             CreateFieldDisplay(nameof(SaveFileInfo.WasAutoSaved), "Auto Save", true, _currentSave);
//             CreateFieldDisplay(nameof(SaveFileInfo.LastSaveTime), "Last Save Time", true, _currentSave);
//             
//             if (_currentSaveData != null)
//             {
//                 // Player Data section
//                 if (_currentSaveData.PlayerData != null)
//                 {
//                     CreateSectionHeader("Player Data");
//                     CreateNestedFieldDisplay("PlayerData.uniqueID", "Player Unique ID", _currentSaveData.PlayerData.uniqueID);
//                     CreateNestedFieldDisplay("PlayerData.playerName", "Player Name", _currentSaveData.PlayerData.playerName);
//                     CreateVector3FieldDisplay("PlayerData.Position", "Player Position", _currentSaveData.PlayerData.Position);
//                     CreateVector3FieldDisplay("PlayerData.Rotation", "Player Rotation", _currentSaveData.PlayerData.Rotation);
//                 }
//                 
//                 // Game Session Data section
//                 if (_currentSaveData.GameSessionData != null)
//                 {
//                     CreateSectionHeader("Game Session Data");
//                     CreateNestedFieldDisplay("GameSessionData.uniqueID", "Session Unique ID", _currentSaveData.GameSessionData.uniqueID);
//                     CreateNestedFieldDisplay("GameSessionData.difficulty", "Difficulty", _currentSaveData.GameSessionData.difficulty);
//                     CreateNestedFieldDisplay("GameSessionData.currentScene", "Current Scene", _currentSaveData.GameSessionData.currentScene);
//                     CreateNestedFieldDisplay("GameSessionData.gameTime", "Game Time", _currentSaveData.GameSessionData.gameTime);
//                 }
//             }
//             else
//             {
//                 // Fallback to basic info if full data couldn't be loaded
//                 CreateFieldDisplay(nameof(SaveFileInfo.PlayerName), "Player Name", true, _currentSave);
//                 CreateFieldDisplay(nameof(SaveFileInfo.CurrentScene), "Current Scene", true, _currentSave);
//                 CreateFieldDisplay(nameof(SaveFileInfo.GameTime), "Game Time", true, _currentSave);
//             }
//         }
//         
//         private void CreateDefaultFieldDisplays()
//         {
//             CreateFieldDisplay(nameof(SaveFileInfo.FileName), "File Name");
//             CreateFieldDisplay(nameof(SaveFileInfo.PlayerName), "Player Name");
//             CreateFieldDisplay(nameof(SaveFileInfo.CurrentScene), "Current Scene");
//             CreateFieldDisplay(nameof(SaveFileInfo.WasAutoSaved), "Auto Save");
//             CreateFieldDisplay(nameof(SaveFileInfo.GameTime), "Game Time");
//             CreateFieldDisplay(nameof(SaveFileInfo.LastSaveTime), "Last Save time");
//         }
//         
//         private void CreateSectionHeader(string sectionTitle)
//         {
//             var headerElement = new Label(sectionTitle);
//             headerElement.AddToClassList("section-header");
//             headerElement.style.fontSize = 14;
//             headerElement.style.unityFontStyleAndWeight = FontStyle.Bold;
//             headerElement.style.marginTop = 10;
//             headerElement.style.marginBottom = 5;
//             headerElement.style.color = new StyleColor(new Color(0.8f, 0.8f, 0.8f));
//             _fieldsContainer.Add(headerElement);
//         }
//         
//         private void CreateNestedFieldDisplay(string fieldPath, string displayName, object value)
//         {
//             try
//             {
//                 var fieldItem = _fieldItemTemplate.CloneTree();
//                 var fieldLabel = fieldItem.Q<Label>("field-label");
//                 fieldLabel.text = displayName;
//                 
//                 // Display the value directly
//                 DisplayFieldValue(fieldItem, value, value?.GetType() ?? typeof(string), true);
//                 
//                 _fieldsContainer.Add(fieldItem);
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"Error creating nested field display for {fieldPath}: {ex.Message}");
//             }
//         }
//         
//         private void CreateVector3FieldDisplay(string fieldPath, string displayName, Vector3 vector)
//         {
//             try
//             {
//                 var fieldItem = _fieldItemTemplate.CloneTree();
//                 var fieldLabel = fieldItem.Q<Label>("field-label");
//                 fieldLabel.text = displayName;
//                 
//                 // Format Vector3 as a readable string
//                 var vectorString = $"({vector.x:F3}, {vector.y:F3}, {vector.z:F3})";
//                 DisplayFieldValue(fieldItem, vectorString, typeof(string), true);
//                 
//                 _fieldsContainer.Add(fieldItem);
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"Error creating Vector3 field display for {fieldPath}: {ex.Message}");
//             }
//         }
//         
//         private void CreateFieldDisplayFromConfig(SaveFileDisplayConfig.FieldDisplayConfig fieldConfig)
//         {
//             // Enhanced version that supports nested field paths
//             if (fieldConfig.FieldName.Contains("."))
//             {
//                 // Handle nested field path (e.g., "PlayerData.uniqueID")
//                 var value = GetNestedFieldValue(fieldConfig.FieldName);
//                 CreateNestedFieldDisplay(fieldConfig.FieldName, fieldConfig.DisplayName, value);
//             }
//             else
//             {
//                 // Handle simple field from SaveFileInfo
//                 CreateFieldDisplay(fieldConfig.FieldName, fieldConfig.DisplayName, fieldConfig.IsReadOnly, _currentSave);
//             }
//         }
//         
//         private void CreateDynamicFieldDiscovery()
//         {
//             if (_currentSaveData == null) return;
//             
//             try
//             {
//                 // Discover all fields and properties in SaveFileData
//                 var saveDataType = typeof(SaveFileData);
//                 var fields = saveDataType.GetFields(BindingFlags.Public | BindingFlags.Instance);
//                 var properties = saveDataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
//                 
//                 // Display root level fields
//                 CreateSubSectionHeader("Root Level Fields");
//                 foreach (var field in fields)
//                 {
//                     if (field.IsPublic)
//                     {
//                         var value = field.GetValue(_currentSaveData);
//                         CreateDynamicFieldDisplay($"SaveFileData.{field.Name}", GetFriendlyName(field.Name), value, field.FieldType);
//                     }
//                 }
//                 
//                 foreach (var property in properties)
//                 {
//                     if (property.CanRead)
//                     {
//                         try
//                         {
//                             var value = property.GetValue(_currentSaveData);
//                             CreateDynamicFieldDisplay($"SaveFileData.{property.Name}", GetFriendlyName(property.Name), value, property.PropertyType);
//                         }
//                         catch (Exception ex)
//                         {
//                             Debug.LogWarning($"Could not read property {property.Name}: {ex.Message}");
//                         }
//                     }
//                 }
//                 
//                 // Discover nested objects
//                 foreach (var field in fields)
//                 {
//                     var value = field.GetValue(_currentSaveData);
//                     if (value != null && IsComplexType(field.FieldType))
//                     {
//                         CreateSubSectionHeader($"{GetFriendlyName(field.Name)} Fields");
//                         DiscoverNestedFields(field.Name, value);
//                     }
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"Error in dynamic field discovery: {ex.Message}");
//             }
//         }
//         
//         private void CreateSubSectionHeader(string title)
//         {
//             var headerElement = new Label($"• {title}");
//             headerElement.AddToClassList("subsection-header");
//             headerElement.style.fontSize = 12;
//             headerElement.style.unityFontStyleAndWeight = FontStyle.Bold;
//             headerElement.style.marginTop = 8;
//             headerElement.style.marginBottom = 3;
//             headerElement.style.marginLeft = 10;
//             headerElement.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
//             _fieldsContainer.Add(headerElement);
//         }
//         
//         private void CreateDynamicFieldDisplay(string fullPath, string displayName, object value, Type fieldType)
//         {
//             try
//             {
//                 var fieldItem = _fieldItemTemplate.CloneTree();
//                 var fieldLabel = fieldItem.Q<Label>("field-label");
//                 fieldLabel.text = displayName;
//                 fieldLabel.style.marginLeft = 15; // Indent to show it's dynamic
//                 
//                 DisplayFieldValue(fieldItem, value, fieldType, true);
//                 _fieldsContainer.Add(fieldItem);
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"Error creating dynamic field display for {fullPath}: {ex.Message}");
//             }
//         }
//         
//         private void DiscoverNestedFields(string parentName, object parentObject)
//         {
//             if (parentObject == null) return;
//             
//             try
//             {
//                 var objectType = parentObject.GetType();
//                 var fields = objectType.GetFields(BindingFlags.Public | BindingFlags.Instance);
//                 var properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
//                 
//                 foreach (var field in fields)
//                 {
//                     var value = field.GetValue(parentObject);
//                     var fullPath = $"{parentName}.{field.Name}";
//                     CreateDynamicFieldDisplay(fullPath, GetFriendlyName(field.Name), value, field.FieldType);
//                 }
//                 
//                 foreach (var property in properties)
//                 {
//                     if (property.CanRead)
//                     {
//                         try
//                         {
//                             var value = property.GetValue(parentObject);
//                             var fullPath = $"{parentName}.{property.Name}";
//                             CreateDynamicFieldDisplay(fullPath, GetFriendlyName(property.Name), value, property.PropertyType);
//                         }
//                         catch (Exception ex)
//                         {
//                             Debug.LogWarning($"Could not read nested property {property.Name}: {ex.Message}");
//                         }
//                     }
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"Error discovering nested fields for {parentName}: {ex.Message}");
//             }
//         }
//         
//         private bool IsComplexType(Type type)
//         {
//             // Check if it's a complex type that should have nested fields discovered
//             if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || 
//                 type == typeof(Vector3) || type == typeof(Vector2) || type == typeof(Quaternion))
//             {
//                 return false;
//             }
//             
//             // Check if it's a Unity serializable type or custom class
//             return type.IsClass && !type.IsArray;
//         }
//         
//         private string GetFriendlyName(string fieldName)
//         {
//             // Convert field names to friendly display names
//             return fieldName switch
//             {
//                 "uniqueID" => "Unique ID",
//                 "playerName" => "Player Name",
//                 "currentScene" => "Current Scene",
//                 "gameTime" => "Game Time",
//                 "SaveTimeTicks" => "Save Time (Ticks)",
//                 "WasAutoSave" => "Was Auto Save",
//                 "PlayerData" => "Player Data",
//                 "GameSessionData" => "Game Session Data",
//                 "Position" => "Position",
//                 "Rotation" => "Rotation",
//                 _ => fieldName.Replace("_", " ").Replace("Data", "").Trim()
//             };
//         }
//         
//         private object GetNestedFieldValue(string fieldPath)
//         {
//             try
//             {
//                 if (_currentSaveData == null) return null;
//                 
//                 var parts = fieldPath.Split('.');
//                 if (parts.Length != 2) return null;
//                 
//                 var objectName = parts[0];
//                 var fieldName = parts[1];
//                 
//                 object targetObject = null;
//                 switch (objectName)
//                 {
//                     case "PlayerData":
//                         targetObject = _currentSaveData.PlayerData;
//                         break;
//                     case "GameSessionData":
//                         targetObject = _currentSaveData.GameSessionData;
//                         break;
//                     default:
//                         return null;
//                 }
//                 
//                 if (targetObject == null) return null;
//                 
//                 var field = targetObject.GetType().GetField(fieldName);
//                 var property = targetObject.GetType().GetProperty(fieldName);
//                 
//                 if (field != null)
//                     return field.GetValue(targetObject);
//                 else if (property != null)
//                     return property.GetValue(targetObject);
//                 
//                 return null;
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"Error getting nested field value for {fieldPath}: {ex.Message}");
//                 return null;
//             }
//         }
//         
//         private void CreateFieldDisplay(string fieldName, string displayName, bool isReadOnly = true, object sourceObject = null)
//         {
//             try
//             {
//                 // Default to _currentSave if no sourceObject provided
//                 var targetObject = sourceObject ?? _currentSave;
//                 if (targetObject == null) return;
//                 
//                 var fieldItem = _fieldItemTemplate.CloneTree();
//                 var fieldLabel = fieldItem.Q<Label>("field-label");
//                 fieldLabel.text = displayName;
//                 
//                 // Get field value using reflection
//                 var objectType = targetObject.GetType();
//                 var field = objectType.GetField(fieldName);
//                 var property = objectType.GetProperty(fieldName);
//                 
//                 object value = null;
//                 Type fieldType = null;
//                 
//                 if (field != null)
//                 {
//                     value = field.GetValue(targetObject);
//                     fieldType = field.FieldType;
//                 }
//                 else if (property != null)
//                 {
//                     value = property.GetValue(targetObject);
//                     fieldType = property.PropertyType;
//                 }
//                 else
//                 {
//                     // Field not found, show error
//                     var textField = fieldItem.Q<TextField>("field-value-text");
//                     textField.style.display = DisplayStyle.Flex;
//                     textField.value = "Field not found";
//                     textField.SetEnabled(false);
//                     _fieldsContainer.Add(fieldItem);
//                     return;
//                 }
//                 
//                 // Display appropriate control based on type
//                 DisplayFieldValue(fieldItem, value, fieldType, isReadOnly);
//                 
//                 _fieldsContainer.Add(fieldItem);
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"Error creating field display for {fieldName}: {ex.Message}");
//             }
//         }
//         
//         private void DisplayFieldValue(VisualElement fieldItem, object value, Type fieldType, bool isReadOnly)
//         {
//             // Hide all field value controls first
//             fieldItem.Q("field-value-text").style.display = DisplayStyle.None;
//             fieldItem.Q("field-value-int").style.display = DisplayStyle.None;
//             fieldItem.Q("field-value-float").style.display = DisplayStyle.None;
//             fieldItem.Q("field-value-bool").style.display = DisplayStyle.None;
//             
//             if (fieldType == typeof(int))
//             {
//                 var intField = fieldItem.Q<IntegerField>("field-value-int");
//                 intField.style.display = DisplayStyle.Flex;
//                 intField.value = (int)(value ?? 0);
//                 intField.SetEnabled(!isReadOnly);
//             }
//             else if (fieldType == typeof(long))
//             {
//                 var textField = fieldItem.Q<TextField>("field-value-text");
//                 textField.style.display = DisplayStyle.Flex;
//                 
//                 // Format long values nicely (especially for game time)
//                 long longValue = (long)(value ?? 0);
//                 if (longValue > 100000) // Likely ticks or milliseconds
//                 {
//                     // Try to format as time if it looks like game time
//                     var timeSpan = TimeSpan.FromMilliseconds(longValue);
//                     if (timeSpan.TotalDays < 365) // Reasonable game time
//                     {
//                         textField.value = $"{longValue:N0} ({timeSpan:hh\\:mm\\:ss})";
//                     }
//                     else
//                     {
//                         textField.value = longValue.ToString("N0");
//                     }
//                 }
//                 else
//                 {
//                     textField.value = longValue.ToString();
//                 }
//                 textField.SetEnabled(!isReadOnly);
//             }
//             else if (fieldType == typeof(float))
//             {
//                 var floatField = fieldItem.Q<FloatField>("field-value-float");
//                 floatField.style.display = DisplayStyle.Flex;
//                 floatField.value = (float)(value ?? 0f);
//                 floatField.SetEnabled(!isReadOnly);
//             }
//             else if (fieldType == typeof(bool))
//             {
//                 var boolField = fieldItem.Q<Toggle>("field-value-bool");
//                 boolField.style.display = DisplayStyle.Flex;
//                 boolField.value = (bool)(value ?? false);
//                 boolField.SetEnabled(!isReadOnly);
//             }
//             else if (fieldType == typeof(DateTime))
//             {
//                 var textField = fieldItem.Q<TextField>("field-value-text");
//                 textField.style.display = DisplayStyle.Flex;
//                 if (value is DateTime dateTime)
//                 {
//                     textField.value = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
//                 }
//                 else
//                 {
//                     textField.value = "Invalid Date";
//                 }
//                 textField.SetEnabled(!isReadOnly);
//             }
//             else if (fieldType == typeof(Vector3))
//             {
//                 var textField = fieldItem.Q<TextField>("field-value-text");
//                 textField.style.display = DisplayStyle.Flex;
//                 if (value is Vector3 vector)
//                 {
//                     textField.value = $"({vector.x:F3}, {vector.y:F3}, {vector.z:F3})";
//                 }
//                 else
//                 {
//                     textField.value = "(0, 0, 0)";
//                 }
//                 textField.SetEnabled(!isReadOnly);
//             }
//             else
//             {
//                 // Default to text field for everything else
//                 var textField = fieldItem.Q<TextField>("field-value-text");
//                 textField.style.display = DisplayStyle.Flex;
//                 
//                 // Handle null values gracefully
//                 if (value == null)
//                 {
//                     textField.value = "<null>";
//                     textField.style.color = new StyleColor(Color.gray);
//                 }
//                 else
//                 {
//                     textField.value = value.ToString();
//                     textField.style.color = StyleKeyword.Initial; // Reset color
//                 }
//                 textField.SetEnabled(!isReadOnly);
//             }
//         }
//         
//         private void UpdateRawDataDisplay(bool showRawData)
//         {
//             _rawDataSection.style.display = showRawData ? DisplayStyle.Flex : DisplayStyle.None;
//             
//             if (!showRawData || _currentSave == null) return;
//             
//             try
//             {
//                 var savePath = GetSaveFilePath(_currentSave.FileName);
//                 if (File.Exists(savePath))
//                 {
//                     var jsonContent = File.ReadAllText(savePath);
//                     _rawDataContent.text = jsonContent;
//                 }
//                 else
//                 {
//                     _rawDataContent.text = "Save file not found";
//                 }
//             }
//             catch (Exception ex)
//             {
//                 _rawDataContent.text = $"Error reading save file: {ex.Message}";
//             }
//         }
//         
//         private void UpdatePlayModeWarning()
//         {
//             _playModeWarning.style.display = Application.isPlaying ? DisplayStyle.None : DisplayStyle.Flex;
//             _loadButton.SetEnabled(Application.isPlaying);
//         }
//         
//         private string GetSaveFilePath(string fileName)
//         {
//             // fileName now includes the extension, so just join with directory
//             return Path.Combine(Application.persistentDataPath, "Saves", fileName);
//         }
//     }
// }
