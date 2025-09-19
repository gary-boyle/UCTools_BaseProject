using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GameFramework.DataStructures;
using GameFramework.Editor.SaveFileManager.ScriptableObjects;
using GameFramework.SaveSystem.Data;
using GameFramework.SaveSystem.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.Editor.SaveFileManager.UI
{
    /// <summary>
    /// Enhanced panel for displaying detailed information about a selected save file
    /// 
    /// Features:
    /// - Loads full SaveFileData directly from JSON for complete information access
    /// - Displays nested objects (GameSessionData, PlayerData) with all their fields
    /// - Supports Vector3, DateTime, and other Unity/complex types with proper formatting  
    /// - Configurable field display through SaveFileDisplayConfig (supports nested field paths)
    /// - Dynamic field discovery to automatically show all available data (extensible for future arbitrary data)
    /// - Uses reflection-based field display for maximum flexibility with changing save structures
    /// 
    /// Configuration:
    /// - Use dot notation in SaveFileDisplayConfig for nested fields (e.g., "PlayerData.uniqueID")
    /// - Toggle ShowDynamicFieldDiscovery to show/hide complete data structure discovery
    /// - System automatically handles new fields added to SaveFileData structure
    /// </summary>
    public class SaveFileDetailsPanel : VisualElement
    {
        private const string UXMLPath = "Assets/Scripts/Editor/SaveFileManager/UI/UXML/SaveFileDetailsPanel.uxml";
        private const string FieldItemUXMLPath = "Assets/Scripts/Editor/SaveFileManager/UI/UXML/FieldDisplayItem.uxml";
        
        // UI References
        private VisualElement _noSelection;
        private VisualElement _detailsContent;
        private Button _loadButton;
        private Button _deleteButton;
        private Button _showExplorerButton;
        private Button _saveChangesButton;
        private Button _cancelChangesButton;
        private VisualElement _editModeButtons;
        private Label _unsavedChangesLabel;
        private VisualElement _playModeWarning;
        private ScrollView _fieldsContainer;
        private VisualElement _rawDataSection;
        private ScrollView _rawDataContainer;
        private Label _rawDataContent;
        
        // Data
        private SaveFileInfo _currentSave;
        private SaveFileData _currentSaveData; // Full save data for enhanced display
        private SaveFileData _editedSaveData; // Copy for editing
        private ScriptableObjects.SaveFileDisplayConfig _displayConfig;
        private VisualTreeAsset _fieldItemTemplate;
        private bool _isEditMode = false;
        private bool _hasUnsavedChanges = false;
        
        // Events
        public event Action<SaveFileInfo> OnLoadRequested;
        public event Action<SaveFileInfo> OnDeleteRequested;
        public event Action<SaveFileInfo> OnShowInExplorerRequested;
        public event Action<SaveFileInfo> OnSaveRequested;
        
        public SaveFileDetailsPanel()
        {
            LoadUI();
        }
        
        private void LoadUI()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXMLPath);
            if (uxml == null)
            {
                Debug.LogError($"Could not load UXML file at {UXMLPath}");
                return;
            }
            
            // Load field item template
            _fieldItemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(FieldItemUXMLPath);
            if (_fieldItemTemplate == null)
            {
                Debug.LogError($"Could not load field item template at {FieldItemUXMLPath}");
                return;
            }
            
            uxml.CloneTree(this);
            
            GetUIReferences();
            SetupEventHandlers();
            UpdatePlayModeWarning();
        }
        
        private void GetUIReferences()
        {
            _noSelection = this.Q("no-selection");
            _detailsContent = this.Q("details-content");
            _loadButton = this.Q<Button>("load-button");
            _deleteButton = this.Q<Button>("delete-button");
            _showExplorerButton = this.Q<Button>("show-explorer-button");
            
            // Edit mode UI elements (may not exist in UXML yet)
            _saveChangesButton = this.Q<Button>("save-changes-button");
            _cancelChangesButton = this.Q<Button>("cancel-changes-button");
            _editModeButtons = this.Q("edit-mode-buttons");
            _unsavedChangesLabel = this.Q<Label>("unsaved-changes-label");
            
            _playModeWarning = this.Q("play-mode-warning");
            _fieldsContainer = this.Q<ScrollView>("fields-container");
            _rawDataSection = this.Q("raw-data-section");
            _rawDataContainer = this.Q<ScrollView>("raw-data-container");
            _rawDataContent = this.Q<Label>("raw-data-content");
            
            // Log missing edit mode UI elements for debugging
            if (_saveChangesButton == null)
                Debug.LogWarning("[SaveFileDetailsPanel] save-changes-button not found in UXML - edit mode save functionality disabled");
            if (_cancelChangesButton == null)
                Debug.LogWarning("[SaveFileDetailsPanel] cancel-changes-button not found in UXML - edit mode cancel functionality disabled");
            if (_editModeButtons == null)
                Debug.LogWarning("[SaveFileDetailsPanel] edit-mode-buttons container not found in UXML - edit mode buttons will not be shown");
            if (_unsavedChangesLabel == null)
                Debug.LogWarning("[SaveFileDetailsPanel] unsaved-changes-label not found in UXML - unsaved changes indicator disabled");
        }
        
        private void SetupEventHandlers()
        {
            // Setup handlers for core UI elements (should always exist)
            _loadButton.clicked += () => OnLoadRequested?.Invoke(_currentSave);
            _deleteButton.clicked += () => OnDeleteRequested?.Invoke(_currentSave);
            _showExplorerButton.clicked += () => OnShowInExplorerRequested?.Invoke(_currentSave);
            
            // Setup handlers for edit mode UI elements (may not exist yet)
            if (_saveChangesButton != null)
                _saveChangesButton.clicked += SaveChanges;
            if (_cancelChangesButton != null)
                _cancelChangesButton.clicked += CancelChanges;
        }
        
        public void LoadSaveFile(SaveFileInfo saveInfo, ScriptableObjects.SaveFileDisplayConfig displayConfig, bool showRawData, bool isEditMode = false)
        {
            _currentSave = saveInfo;
            _displayConfig = displayConfig;
            _isEditMode = isEditMode;
            _currentSaveData = null; // Reset full save data
            _editedSaveData = null; // Reset edited data
            _hasUnsavedChanges = false;
            
            if (saveInfo == null)
            {
                ShowNoSelection();
                return;
            }
            
            // Load full save data for enhanced display
            LoadFullSaveData();
            
            // Create a copy for editing if in edit mode
            if (_isEditMode && _currentSaveData != null)
            {
                CreateEditCopy();
            }
            
            ShowDetailsContent();
            UpdateFieldsDisplay();
            UpdateRawDataDisplay(showRawData);
            UpdateEditModeUI();
            UpdatePlayModeWarning();
        }
        
        private void ShowNoSelection()
        {
            _noSelection.style.display = DisplayStyle.Flex;
            _detailsContent.style.display = DisplayStyle.None;
        }
        
        private void ShowDetailsContent()
        {
            _noSelection.style.display = DisplayStyle.None;
            _detailsContent.style.display = DisplayStyle.Flex;
        }
        
        private void LoadFullSaveData()
        {
            if (_currentSave == null) return;
            
            try
            {
                var savePath = GetSaveFilePath(_currentSave.FileName);
                if (File.Exists(savePath))
                {
                    var jsonContent = File.ReadAllText(savePath);
                    _currentSaveData = JsonSerializationHelper.DeserializeFromJson<SaveFileData>(jsonContent);
                    
                    if (_currentSaveData == null)
                    {
                        Debug.LogWarning($"[SaveFileDetailsPanel] Failed to deserialize save data, attempting to create corrupted data for display");
                        _currentSaveData = JsonSerializationHelper.CreateCorruptedSaveData(_currentSave.FileName);
                    }
                }
                else
                {
                    Debug.LogWarning($"[SaveFileDetailsPanel] Save file not found: {savePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileDetailsPanel] Failed to load full save data: {ex.Message}");
                // Create corrupted save data for display purposes
                _currentSaveData = JsonSerializationHelper.CreateCorruptedSaveData(_currentSave.FileName);
            }
        }
        
        private void UpdateFieldsDisplay()
        {
            Debug.Log($"[SaveFileDetailsPanel] UpdateFieldsDisplay called - EditMode: {_isEditMode}, CurrentSave: {_currentSave?.FileName}");
            _fieldsContainer.Clear();
            
            if (_currentSave == null) return;
            
            if (_displayConfig?.DisplayFields != null && _displayConfig.DisplayFields.Count > 0)
            {
                // Use configured fields - but now support nested paths
                foreach (var fieldConfig in _displayConfig.DisplayFields)
                {
                    CreateFieldDisplayFromConfig(fieldConfig);
                }
            }
            else
            {
                // Use enhanced default fields showing full save data
                CreateEnhancedDefaultFieldDisplays();
            }
            
            // Show dynamic field discovery if enabled (for debugging and future extensibility)
            if (_displayConfig?.ShowDynamicFieldDiscovery == true)
            {
                CreateSectionHeader("Complete Save Data (All Fields)");
                CreateDynamicFieldDiscovery();
            }
        }
        
        private void CreateEnhancedDefaultFieldDisplays()
        {
            // Basic save file info
            CreateSectionHeader("Save File Information");
            CreateFieldDisplay(nameof(SaveFileInfo.FileName), "File Name", true, _currentSave);
            CreateFieldDisplay(nameof(SaveFileInfo.WasAutoSaved), "Auto Save", true, _currentSave);
            CreateFieldDisplay(nameof(SaveFileInfo.LastSaveTime), "Last Save Time", true, _currentSave);
            
            if (_currentSaveData != null)
            {
                // Use edited data when in edit mode for consistent display
                var saveDataForDisplay = _isEditMode && _editedSaveData != null ? _editedSaveData : _currentSaveData;
                
                // Player Data section
                if (saveDataForDisplay.PlayerData != null)
                {
                    CreateSectionHeader("Player Data");
                    CreateNestedFieldDisplay("PlayerData.uniqueID", "Player Unique ID", saveDataForDisplay.PlayerData.uniqueID);
                    CreateNestedFieldDisplay("PlayerData.playerName", "Player Name", saveDataForDisplay.PlayerData.playerName);
                    CreateVector3FieldDisplay("PlayerData.Position", "Player Position", saveDataForDisplay.PlayerData.Position);
                    CreateVector3FieldDisplay("PlayerData.Rotation", "Player Rotation", saveDataForDisplay.PlayerData.Rotation);
                }
                
                // Game Session Data section
                if (saveDataForDisplay.GameSessionData != null)
                {
                    CreateSectionHeader("Game Session Data");
                    CreateNestedFieldDisplay("GameSessionData.uniqueID", "Session Unique ID", saveDataForDisplay.GameSessionData.uniqueID);
                    CreateNestedFieldDisplay("GameSessionData.difficulty", "Difficulty", saveDataForDisplay.GameSessionData.difficulty);
                    CreateNestedFieldDisplay("GameSessionData.currentScene", "Current Scene", saveDataForDisplay.GameSessionData.currentScene);
                    CreateNestedFieldDisplay("GameSessionData.gameTime", "Game Time", saveDataForDisplay.GameSessionData.gameTime);
                }
                
                // Runtime Objects section
                var saveDataToCheck = _isEditMode && _editedSaveData != null ? _editedSaveData : _currentSaveData;
                if (saveDataToCheck.RuntimeObjects != null && saveDataToCheck.RuntimeObjects.Count > 0)
                {
                    CreateRuntimeObjectsSection();
                }
            }
            else
            {
                // Fallback to basic info if full data couldn't be loaded
                CreateFieldDisplay(nameof(SaveFileInfo.PlayerName), "Player Name", true, _currentSave);
                CreateFieldDisplay(nameof(SaveFileInfo.CurrentScene), "Current Scene", true, _currentSave);
                CreateFieldDisplay(nameof(SaveFileInfo.GameTime), "Game Time", true, _currentSave);
            }
        }
        
        private void CreateDefaultFieldDisplays()
        {
            CreateFieldDisplay(nameof(SaveFileInfo.FileName), "File Name");
            CreateFieldDisplay(nameof(SaveFileInfo.PlayerName), "Player Name");
            CreateFieldDisplay(nameof(SaveFileInfo.CurrentScene), "Current Scene");
            CreateFieldDisplay(nameof(SaveFileInfo.WasAutoSaved), "Auto Save");
            CreateFieldDisplay(nameof(SaveFileInfo.GameTime), "Game Time");
            CreateFieldDisplay(nameof(SaveFileInfo.LastSaveTime), "Last Save time");
        }
        
        private void CreateSectionHeader(string sectionTitle)
        {
            var headerElement = new Label(sectionTitle);
            headerElement.AddToClassList("section-header");
            headerElement.style.fontSize = 14;
            headerElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerElement.style.marginTop = 10;
            headerElement.style.marginBottom = 5;
            headerElement.style.color = new StyleColor(new Color(0.8f, 0.8f, 0.8f));
            _fieldsContainer.Add(headerElement);
        }
        
        private void CreateNestedFieldDisplay(string fieldPath, string displayName, object value)
        {
            try
            {
                var fieldItem = _fieldItemTemplate.CloneTree();
                var fieldLabel = fieldItem.Q<Label>("field-label");
                fieldLabel.text = displayName;
                
                // Store the field path in userData for editing
                fieldItem.userData = fieldPath;
                Debug.Log($"[SaveFileDetailsPanel] CreateNestedFieldDisplay - Setting userData to: '{fieldPath}' for field: '{displayName}', EditMode: {_isEditMode}");
                
                // Display the value directly
                DisplayFieldValue(fieldItem, value, value?.GetType() ?? typeof(string), false); // Allow editing in edit mode
                
                _fieldsContainer.Add(fieldItem);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating nested field display for {fieldPath}: {ex.Message}");
            }
        }
        
        private void CreateVector3FieldDisplay(string fieldPath, string displayName, Vector3 vector)
        {
            try
            {
                var fieldItem = _fieldItemTemplate.CloneTree();
                var fieldLabel = fieldItem.Q<Label>("field-label");
                fieldLabel.text = displayName;
                
                // Format Vector3 as a readable string
                var vectorString = $"({vector.x:F3}, {vector.y:F3}, {vector.z:F3})";
                DisplayFieldValue(fieldItem, vectorString, typeof(string), true);
                
                _fieldsContainer.Add(fieldItem);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating Vector3 field display for {fieldPath}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Creates the Runtime Objects section displaying detailed information about serialized runtime objects
        /// </summary>
        private void CreateRuntimeObjectsSection()
        {
            CreateSectionHeader("Runtime Objects");
            
            // Use edited data when in edit mode to reflect current changes (like deletions)
            var saveDataToUse = _isEditMode && _editedSaveData != null ? _editedSaveData : _currentSaveData;
            var runtimeObjects = saveDataToUse.RuntimeObjects;
            var totalObjects = runtimeObjects.Count;
            
            Debug.Log($"[SaveFileDetailsPanel] CreateRuntimeObjectsSection - EditMode: {_isEditMode}, Using edited data: {_isEditMode && _editedSaveData != null}, Total objects: {totalObjects}");
            
            // Display summary information
            CreateNestedFieldDisplay("RuntimeObjects.Count", "Total Runtime Objects", totalObjects);
            
            // Group objects by type for summary
            var typeGroups = runtimeObjects.GroupBy(obj => obj.typeName)
                                           .ToDictionary(g => g.Key, g => g.Count());
            
            CreateSubSectionHeader("Objects by Type");
            foreach (var typeGroup in typeGroups.OrderByDescending(kvp => kvp.Value))
            {
                CreateNestedFieldDisplay($"RuntimeObjects.{typeGroup.Key}", $"{typeGroup.Key} Objects", typeGroup.Value);
            }
            
            // Display individual objects
            CreateSubSectionHeader("Individual Objects");
            
            // Use configuration for max display objects
            int maxDisplayObjects = _displayConfig?.MaxRuntimeObjectsDisplay ?? 20;
            maxDisplayObjects = Math.Min(totalObjects, maxDisplayObjects);
            bool hasMoreObjects = totalObjects > maxDisplayObjects;
            
            for (int i = 0; i < maxDisplayObjects; i++)
            {
                var obj = runtimeObjects[i];
                CreateRuntimeObjectDisplay(obj, i);
            }
            
            if (hasMoreObjects)
            {
                var moreObjectsLabel = new Label($"... and {totalObjects - maxDisplayObjects} more objects (showing first {maxDisplayObjects})");
                moreObjectsLabel.AddToClassList("info-message");
                moreObjectsLabel.style.fontSize = 11;
                moreObjectsLabel.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
                moreObjectsLabel.style.marginLeft = 15;
                moreObjectsLabel.style.marginTop = 5;
                _fieldsContainer.Add(moreObjectsLabel);
            }
        }
        
        /// <summary>
        /// Creates a display for an individual runtime object
        /// </summary>
        private void CreateRuntimeObjectDisplay(SerializedRuntimeObject obj, int index)
        {
            try
            {
                // Object header with type, ID, and delete button
                var headerContainer = new VisualElement();
                headerContainer.style.flexDirection = FlexDirection.Row;
                headerContainer.style.alignItems = Align.Center;
                headerContainer.style.marginTop = 8;
                headerContainer.style.marginBottom = 3;
                headerContainer.style.marginLeft = 20;
                
                var headerLabel = new Label($"[{index + 1}] {obj.typeName}: {obj.uniqueID}");
                headerLabel.AddToClassList("runtime-object-header");
                headerLabel.style.fontSize = 12;
                headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                headerLabel.style.color = new StyleColor(new Color(0.9f, 0.7f, 0.4f)); // Orange-ish color for objects
                headerLabel.style.flexGrow = 1; // Take up remaining space
                headerContainer.Add(headerLabel);
                
                // Delete button (only show in edit mode)
                var deleteButton = new Button(() => DeleteRuntimeObject(index, obj.uniqueID))
                {
                    text = "🗑",
                    tooltip = $"Delete runtime object: {obj.uniqueID}"
                };
                deleteButton.AddToClassList("delete-runtime-object-button");
                deleteButton.style.width = 24;
                deleteButton.style.height = 20;
                deleteButton.style.fontSize = 12;
                deleteButton.style.marginLeft = 10;
                deleteButton.style.backgroundColor = new StyleColor(new Color(0.8f, 0.3f, 0.3f, 0.8f)); // Red background
                deleteButton.style.color = Color.white;
                deleteButton.style.borderTopLeftRadius = 3;
                deleteButton.style.borderTopRightRadius = 3;
                deleteButton.style.borderBottomLeftRadius = 3;
                deleteButton.style.borderBottomRightRadius = 3;
                deleteButton.style.display = _isEditMode ? DisplayStyle.Flex : DisplayStyle.None;
                headerContainer.Add(deleteButton);
                
                _fieldsContainer.Add(headerContainer);
                
                // Object details
                CreateRuntimeObjectField($"Object[{index}].typeName", "Type Name", obj.typeName);
                CreateRuntimeObjectField($"Object[{index}].saveDataTypeName", "Save Data Type", obj.saveDataTypeName);
                CreateRuntimeObjectField($"Object[{index}].dataLength", "Data Length", $"{obj.dataLength} bytes");
                
                // Try to deserialize and show key fields from the jsonData
                CreateDeserializedObjectFields(obj, index);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating runtime object display for index {index}: {ex.Message}");
                
                // Create error display
                var errorLabel = new Label($"[ERROR] Failed to display object {index}: {ex.Message}");
                errorLabel.style.color = Color.red;
                errorLabel.style.marginLeft = 20;
                errorLabel.style.fontSize = 11;
                _fieldsContainer.Add(errorLabel);
            }
        }
        
        /// <summary>
        /// Creates a field display for runtime object properties
        /// </summary>
        private void CreateRuntimeObjectField(string fieldPath, string displayName, object value)
        {
            try
            {
                var fieldItem = _fieldItemTemplate.CloneTree();
                var fieldLabel = fieldItem.Q<Label>("field-label");
                fieldLabel.text = displayName;
                fieldLabel.style.marginLeft = 25; // Extra indent for runtime object fields
                
                // Set the field path in userData for editing
                fieldItem.userData = fieldPath;
                
                // Most runtime object fields should be editable in edit mode
                bool isReadOnly = fieldPath.Contains("typeName") || fieldPath.Contains("saveDataTypeName") || fieldPath.Contains("dataLength");
                
                Debug.Log($"[SaveFileDetailsPanel] Creating runtime object field: {fieldPath}, isReadOnly: {isReadOnly}");
                DisplayFieldValue(fieldItem, value, value?.GetType() ?? typeof(string), isReadOnly);
                _fieldsContainer.Add(fieldItem);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating runtime object field display for {fieldPath}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Creates a field display for runtime object properties with raw values for editing
        /// </summary>
        private void CreateRuntimeObjectFieldWithRawValue(string fieldPath, string displayName, object value)
        {
            try
            {
                var fieldItem = _fieldItemTemplate.CloneTree();
                var fieldLabel = fieldItem.Q<Label>("field-label");
                fieldLabel.text = displayName;
                fieldLabel.style.marginLeft = 25; // Extra indent for runtime object fields
                
                // Set the field path in userData for editing
                fieldItem.userData = fieldPath;
                
                // Most runtime object fields should be editable in edit mode
                bool isReadOnly = fieldPath.Contains("typeName") || fieldPath.Contains("saveDataTypeName") || fieldPath.Contains("dataLength") || fieldPath.Contains("prefabGUID");
                
                Debug.Log($"[SaveFileDetailsPanel] Creating runtime object field with raw value: {fieldPath}, type: {value?.GetType()?.Name}, isReadOnly: {isReadOnly}");
                DisplayFieldValue(fieldItem, value, value?.GetType() ?? typeof(string), isReadOnly);
                _fieldsContainer.Add(fieldItem);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating runtime object field display for {fieldPath}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Attempts to deserialize the jsonData and display key fields
        /// </summary>
        private void CreateDeserializedObjectFields(SerializedRuntimeObject obj, int index)
        {
            try
            {
                if (string.IsNullOrEmpty(obj.jsonData))
                {
                    CreateRuntimeObjectField($"Object[{index}].jsonData", "JSON Data", "<empty>");
                    return;
                }
                
                // Try to deserialize the runtime object
                var deserializedObj = obj.Deserialize();
                if (deserializedObj != null)
                {
                    // Display transform information if enabled in config
                    if (_displayConfig?.ShowRuntimeObjectTransforms ?? true)
                    {
                        CreateRuntimeObjectFieldWithRawValue($"Object[{index}].position", "Position", deserializedObj.position);
                        CreateRuntimeObjectFieldWithRawValue($"Object[{index}].rotation", "Rotation", deserializedObj.rotation);
                        CreateRuntimeObjectFieldWithRawValue($"Object[{index}].scale", "Scale", deserializedObj.scale);
                        CreateRuntimeObjectFieldWithRawValue($"Object[{index}].isActive", "Is Active", deserializedObj.isActive);
                    }
                    
                    CreateRuntimeObjectFieldWithRawValue($"Object[{index}].prefabGUID", "Prefab GUID", deserializedObj.prefabGUID);
                    
                    // Try to display type-specific fields if enabled in config
                    if (_displayConfig?.ShowRuntimeObjectSpecificFields ?? true)
                    {
                        DisplayTypeSpecificFields(deserializedObj, index);
                    }
                }
                else
                {
                    // Show raw JSON data if deserialization failed
                    CreateRuntimeObjectField($"Object[{index}].jsonData", "Raw JSON", 
                        obj.jsonData.Length > 100 ? obj.jsonData.Substring(0, 100) + "..." : obj.jsonData);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to deserialize runtime object {obj.uniqueID}: {ex.Message}");
                CreateRuntimeObjectField($"Object[{index}].jsonData", "Raw JSON (Parse Failed)", 
                    obj.jsonData.Length > 100 ? obj.jsonData.Substring(0, 100) + "..." : obj.jsonData);
            }
        }
        
        /// <summary>
        /// Displays type-specific fields for specialized runtime save data types
        /// </summary>
        private void DisplayTypeSpecificFields(RuntimeObjectSaveData saveData, int index)
        {
            try
            {
                var objectType = saveData.GetType();
                
                // Get all public fields that are not part of the base RuntimeObjectSaveData
                var baseFields = typeof(RuntimeObjectSaveData).GetFields(BindingFlags.Public | BindingFlags.Instance);
                var allFields = objectType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                var specificFields = allFields.Where(f => !baseFields.Any(bf => bf.Name == f.Name)).ToArray();
                
                if (specificFields.Length > 0)
                {
                    // Create sub-header for type-specific fields
                    var specificHeader = new Label($"• {objectType.Name} Specific Fields");
                    specificHeader.style.fontSize = 11;
                    specificHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                    specificHeader.style.marginTop = 5;
                    specificHeader.style.marginLeft = 25;
                    specificHeader.style.color = new StyleColor(new Color(0.6f, 0.8f, 0.9f)); // Light blue for specific fields
                    _fieldsContainer.Add(specificHeader);
                    
                    foreach (var field in specificFields)
                    {
                        var value = field.GetValue(saveData);
                        CreateRuntimeObjectFieldWithRawValue($"Object[{index}].{field.Name}", GetFriendlyFieldName(field.Name), value);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to display type-specific fields for {saveData.GetType().Name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Formats a field value for display
        /// </summary>
        private string FormatFieldValue(object value, Type fieldType)
        {
            if (value == null) return "<null>";
            
            if (fieldType == typeof(Color) || fieldType == typeof(Color32))
            {
                var color = (Color)value;
                return $"RGBA({color.r:F2}, {color.g:F2}, {color.b:F2}, {color.a:F2})";
            }
            
            if (fieldType == typeof(Vector3))
            {
                var vector = (Vector3)value;
                return $"({vector.x:F2}, {vector.y:F2}, {vector.z:F2})";
            }
            
            if (fieldType == typeof(Vector2))
            {
                var vector = (Vector2)value;
                return $"({vector.x:F2}, {vector.y:F2})";
            }
            
            if (fieldType == typeof(float))
            {
                return ((float)value).ToString("F3");
            }
            
            if (fieldType == typeof(double))
            {
                return ((double)value).ToString("F3");
            }
            
            return value.ToString();
        }
        
        /// <summary>
        /// Converts field names to friendly display names
        /// </summary>
        private string GetFriendlyFieldName(string fieldName)
        {
            return fieldName switch
            {
                "cubeColor" => "Cube Color",
                "cubeValue" => "Cube Value",
                "healthPoints" => "Health Points",
                "maxHealth" => "Max Health",
                "playerLevel" => "Player Level",
                "experiencePoints" => "Experience Points",
                _ => fieldName.Replace("_", " ")
                              .Replace("Data", "")
                              .Trim()
            };
        }
        
        #region Edit Mode Methods
        
        /// <summary>
        /// Creates a deep copy of the current save data for editing
        /// </summary>
        private void CreateEditCopy()
        {
            try
            {
                if (_currentSaveData == null) return;
                
                // Serialize to JSON and deserialize back to create a deep copy
                var json = JsonSerializationHelper.SerializeToJson(_currentSaveData);
                _editedSaveData = JsonSerializationHelper.DeserializeFromJson<SaveFileData>(json);
                
                if (_editedSaveData == null)
                {
                    Debug.LogError("[SaveFileDetailsPanel] Failed to create edit copy of save data");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileDetailsPanel] Error creating edit copy: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Updates the edit mode UI elements visibility and state
        /// </summary>
        private void UpdateEditModeUI()
        {
            Debug.Log($"[SaveFileDetailsPanel] UpdateEditModeUI called - EditMode: {_isEditMode}, HasUnsavedChanges: {_hasUnsavedChanges}");
            
            if (_editModeButtons != null)
            {
                _editModeButtons.style.display = _isEditMode ? DisplayStyle.Flex : DisplayStyle.None;
                Debug.Log($"[SaveFileDetailsPanel] Edit mode buttons visibility: {(_isEditMode ? "Visible" : "Hidden")}");
            }
            else
            {
                Debug.LogWarning($"[SaveFileDetailsPanel] _editModeButtons is null");
            }
            
            if (_unsavedChangesLabel != null)
            {
                _unsavedChangesLabel.style.display = (_isEditMode && _hasUnsavedChanges) ? DisplayStyle.Flex : DisplayStyle.None;
                if (_hasUnsavedChanges)
                {
                    _unsavedChangesLabel.text = "⚠ You have unsaved changes";
                    _unsavedChangesLabel.style.color = new StyleColor(Color.yellow);
                    Debug.Log($"[SaveFileDetailsPanel] Unsaved changes label shown");
                }
            }
            else
            {
                Debug.LogWarning($"[SaveFileDetailsPanel] _unsavedChangesLabel is null");
            }
            
            if (_saveChangesButton != null)
            {
                bool shouldEnable = _isEditMode && _hasUnsavedChanges;
                _saveChangesButton.SetEnabled(shouldEnable);
                Debug.Log($"[SaveFileDetailsPanel] Save button enabled: {shouldEnable}");
            }
            else
            {
                Debug.LogWarning($"[SaveFileDetailsPanel] _saveChangesButton is null");
            }
            
            if (_cancelChangesButton != null)
            {
                bool shouldEnable = _isEditMode && _hasUnsavedChanges;
                _cancelChangesButton.SetEnabled(shouldEnable);
                Debug.Log($"[SaveFileDetailsPanel] Cancel button enabled: {shouldEnable}");
            }
            else
            {
                Debug.LogWarning($"[SaveFileDetailsPanel] _cancelChangesButton is null");
            }
            
            // Update delete button visibility for runtime objects
            UpdateRuntimeObjectDeleteButtonsVisibility();
        }
        
        /// <summary>
        /// Updates the visibility of delete buttons for runtime objects
        /// </summary>
        private void UpdateRuntimeObjectDeleteButtonsVisibility()
        {
            // Find all delete buttons in the fields container
            var deleteButtons = _fieldsContainer.Query<Button>(className: "delete-runtime-object-button").ToList();
            foreach (var deleteButton in deleteButtons)
            {
                deleteButton.style.display = _isEditMode ? DisplayStyle.Flex : DisplayStyle.None;
            }
            
            Debug.Log($"[SaveFileDetailsPanel] Updated {deleteButtons.Count} runtime object delete buttons visibility - EditMode: {_isEditMode}");
        }
        
        /// <summary>
        /// Saves the edited changes back to the JSON file
        /// </summary>
        private void SaveChanges()
        {
            if (!_isEditMode || _editedSaveData == null || _currentSave == null)
            {
                Debug.LogWarning("[SaveFileDetailsPanel] Cannot save - not in edit mode or no edited data");
                return;
            }
            
            try
            {
                var filePath = GetSaveFilePath(_currentSave.FileName);
                
                // Create backup of original file
                var backupPath = filePath + ".backup." + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.Copy(filePath, backupPath);
                
                // Serialize the edited data to JSON
                var json = JsonSerializationHelper.SerializeToJson(_editedSaveData);
                if (string.IsNullOrEmpty(json))
                {
                    throw new Exception("Failed to serialize edited save data to JSON");
                }
                
                // Write the new JSON to the file
                File.WriteAllText(filePath, json);
                
                // Update current data to match edited data
                _currentSaveData = _editedSaveData;
                CreateEditCopy(); // Create new edit copy
                
                _hasUnsavedChanges = false;
                UpdateEditModeUI();
                
                Debug.Log($"[SaveFileDetailsPanel] Successfully saved changes to {_currentSave.FileName}. Backup created at {Path.GetFileName(backupPath)}");
                
                // Trigger save event so the main window can refresh
                OnSaveRequested?.Invoke(_currentSave);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileDetailsPanel] Error saving changes: {ex.Message}");
                EditorUtility.DisplayDialog("Save Error", 
                    $"Failed to save changes to {_currentSave.FileName}:\n\n{ex.Message}", 
                    "OK");
            }
        }
        
        /// <summary>
        /// Cancels the current edits and reloads the original data
        /// </summary>
        private void CancelChanges()
        {
            if (!_isEditMode)
            {
                return;
            }
            
            bool shouldCancel = true;
            
            if (_hasUnsavedChanges)
            {
                shouldCancel = EditorUtility.DisplayDialog("Cancel Changes", 
                    "Are you sure you want to discard your changes?", 
                    "Discard Changes", 
                    "Keep Editing");
            }
            
            if (shouldCancel)
            {
                // Reset to original data
                CreateEditCopy();
                _hasUnsavedChanges = false;
                UpdateEditModeUI();
                UpdateFieldsDisplay();
                
                Debug.Log("[SaveFileDetailsPanel] Cancelled edit changes, reverted to original data");
            }
        }
        
        /// <summary>
        /// Marks that changes have been made and updates the UI
        /// </summary>
        private void MarkAsChanged()
        {
            if (!_isEditMode) 
            {
                Debug.LogWarning($"[SaveFileDetailsPanel] MarkAsChanged called but not in edit mode");
                return;
            }
            
            Debug.Log($"[SaveFileDetailsPanel] MarkAsChanged called - HasUnsavedChanges: {_hasUnsavedChanges} -> true");
            _hasUnsavedChanges = true;
            UpdateEditModeUI();
        }
        
        #endregion
        
        private void CreateFieldDisplayFromConfig(SaveFileDisplayConfig.FieldDisplayConfig fieldConfig)
        {
            // Enhanced version that supports nested field paths
            if (fieldConfig.FieldName.Contains("."))
            {
                // Handle nested field path (e.g., "PlayerData.uniqueID")
                var value = GetNestedFieldValue(fieldConfig.FieldName);
                CreateNestedFieldDisplay(fieldConfig.FieldName, fieldConfig.DisplayName, value);
            }
            else
            {
                // Handle simple field from SaveFileInfo
                CreateFieldDisplay(fieldConfig.FieldName, fieldConfig.DisplayName, fieldConfig.IsReadOnly, _currentSave);
            }
        }
        
        private void CreateDynamicFieldDiscovery()
        {
            if (_currentSaveData == null) return;
            
            try
            {
                // Discover all fields and properties in SaveFileData
                var saveDataType = typeof(SaveFileData);
                var fields = saveDataType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                var properties = saveDataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                // Display root level fields
                CreateSubSectionHeader("Root Level Fields");
                foreach (var field in fields)
                {
                    if (field.IsPublic)
                    {
                        var value = field.GetValue(_currentSaveData);
                        CreateDynamicFieldDisplay($"SaveFileData.{field.Name}", GetFriendlyName(field.Name), value, field.FieldType);
                    }
                }
                
                foreach (var property in properties)
                {
                    if (property.CanRead)
                    {
                        try
                        {
                            var value = property.GetValue(_currentSaveData);
                            CreateDynamicFieldDisplay($"SaveFileData.{property.Name}", GetFriendlyName(property.Name), value, property.PropertyType);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Could not read property {property.Name}: {ex.Message}");
                        }
                    }
                }
                
                // Discover nested objects
                foreach (var field in fields)
                {
                    var value = field.GetValue(_currentSaveData);
                    if (value != null && IsComplexType(field.FieldType))
                    {
                        CreateSubSectionHeader($"{GetFriendlyName(field.Name)} Fields");
                        DiscoverNestedFields(field.Name, value);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in dynamic field discovery: {ex.Message}");
            }
        }
        
        private void CreateSubSectionHeader(string title)
        {
            var headerElement = new Label($"• {title}");
            headerElement.AddToClassList("subsection-header");
            headerElement.style.fontSize = 12;
            headerElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerElement.style.marginTop = 8;
            headerElement.style.marginBottom = 3;
            headerElement.style.marginLeft = 10;
            headerElement.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            _fieldsContainer.Add(headerElement);
        }
        
        private void CreateDynamicFieldDisplay(string fullPath, string displayName, object value, Type fieldType)
        {
            try
            {
                var fieldItem = _fieldItemTemplate.CloneTree();
                var fieldLabel = fieldItem.Q<Label>("field-label");
                fieldLabel.text = displayName;
                fieldLabel.style.marginLeft = 15; // Indent to show it's dynamic
                
                DisplayFieldValue(fieldItem, value, fieldType, true);
                _fieldsContainer.Add(fieldItem);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating dynamic field display for {fullPath}: {ex.Message}");
            }
        }
        
        private void DiscoverNestedFields(string parentName, object parentObject)
        {
            if (parentObject == null) return;
            
            try
            {
                var objectType = parentObject.GetType();
                var fields = objectType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                var properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                foreach (var field in fields)
                {
                    var value = field.GetValue(parentObject);
                    var fullPath = $"{parentName}.{field.Name}";
                    CreateDynamicFieldDisplay(fullPath, GetFriendlyName(field.Name), value, field.FieldType);
                }
                
                foreach (var property in properties)
                {
                    if (property.CanRead)
                    {
                        try
                        {
                            var value = property.GetValue(parentObject);
                            var fullPath = $"{parentName}.{property.Name}";
                            CreateDynamicFieldDisplay(fullPath, GetFriendlyName(property.Name), value, property.PropertyType);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Could not read nested property {property.Name}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error discovering nested fields for {parentName}: {ex.Message}");
            }
        }
        
        private bool IsComplexType(Type type)
        {
            // Check if it's a complex type that should have nested fields discovered
            if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || 
                type == typeof(Vector3) || type == typeof(Vector2) || type == typeof(Quaternion))
            {
                return false;
            }
            
            // Check if it's a Unity serializable type or custom class
            return type.IsClass && !type.IsArray;
        }
        
        private string GetFriendlyName(string fieldName)
        {
            // Convert field names to friendly display names
            return fieldName switch
            {
                "uniqueID" => "Unique ID",
                "playerName" => "Player Name",
                "currentScene" => "Current Scene",
                "gameTime" => "Game Time",
                "SaveTimeTicks" => "Save Time (Ticks)",
                "WasAutoSave" => "Was Auto Save",
                "PlayerData" => "Player Data",
                "GameSessionData" => "Game Session Data",
                "Position" => "Position",
                "Rotation" => "Rotation",
                _ => fieldName.Replace("_", " ").Replace("Data", "").Trim()
            };
        }
        
        private object GetNestedFieldValue(string fieldPath)
        {
            try
            {
                if (_currentSaveData == null) return null;
                
                var parts = fieldPath.Split('.');
                if (parts.Length != 2) return null;
                
                var objectName = parts[0];
                var fieldName = parts[1];
                
                object targetObject = null;
                switch (objectName)
                {
                    case "PlayerData":
                        targetObject = _currentSaveData.PlayerData;
                        break;
                    case "GameSessionData":
                        targetObject = _currentSaveData.GameSessionData;
                        break;
                    default:
                        return null;
                }
                
                if (targetObject == null) return null;
                
                var field = targetObject.GetType().GetField(fieldName);
                var property = targetObject.GetType().GetProperty(fieldName);
                
                if (field != null)
                    return field.GetValue(targetObject);
                else if (property != null)
                    return property.GetValue(targetObject);
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error getting nested field value for {fieldPath}: {ex.Message}");
                return null;
            }
        }
        
        private void CreateFieldDisplay(string fieldName, string displayName, bool isReadOnly = true, object sourceObject = null)
        {
            try
            {
                // Default to _currentSave if no sourceObject provided
                var targetObject = sourceObject ?? _currentSave;
                if (targetObject == null) return;
                
                var fieldItem = _fieldItemTemplate.CloneTree();
                var fieldLabel = fieldItem.Q<Label>("field-label");
                fieldLabel.text = displayName;
                
                // Get field value using reflection
                var objectType = targetObject.GetType();
                var field = objectType.GetField(fieldName);
                var property = objectType.GetProperty(fieldName);
                
                object value = null;
                Type fieldType = null;
                
                if (field != null)
                {
                    value = field.GetValue(targetObject);
                    fieldType = field.FieldType;
                }
                else if (property != null)
                {
                    value = property.GetValue(targetObject);
                    fieldType = property.PropertyType;
                }
                else
                {
                    // Field not found, show error
                    var textField = fieldItem.Q<TextField>("field-value-text");
                    textField.style.display = DisplayStyle.Flex;
                    textField.value = "Field not found";
                    textField.SetEnabled(false);
                    _fieldsContainer.Add(fieldItem);
                    return;
                }
                
                // Store the field path in userData for editing
                fieldItem.userData = fieldName;
                Debug.Log($"[SaveFileDetailsPanel] CreateFieldDisplay - Setting userData to: '{fieldName}' for field: '{displayName}', EditMode: {_isEditMode}");
                
                // Display appropriate control based on type
                DisplayFieldValue(fieldItem, value, fieldType, isReadOnly);
                
                _fieldsContainer.Add(fieldItem);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating field display for {fieldName}: {ex.Message}");
            }
        }
        
        private void DisplayFieldValue(VisualElement fieldItem, object value, Type fieldType, bool isReadOnly)
        {
            // Fields should ONLY be editable when in edit mode AND not explicitly marked read-only
            bool isActuallyReadOnly = !_isEditMode || isReadOnly;
            
            Debug.Log($"[SaveFileDetailsPanel] DisplayFieldValue called - EditMode: {_isEditMode}, isReadOnly: {isReadOnly}, isActuallyReadOnly: {isActuallyReadOnly}, fieldType: {fieldType.Name}, value: {value}, fieldPath: {fieldItem.userData}");
            
            // Hide all field value controls first
            fieldItem.Q("field-value-text").style.display = DisplayStyle.None;
            fieldItem.Q("field-value-int").style.display = DisplayStyle.None;
            fieldItem.Q("field-value-float").style.display = DisplayStyle.None;
            fieldItem.Q("field-value-bool").style.display = DisplayStyle.None;
            
            if (fieldType == typeof(int))
            {
                var intField = fieldItem.Q<IntegerField>("field-value-int");
                intField.style.display = DisplayStyle.Flex;
                intField.value = (int)(value ?? 0);
                intField.SetEnabled(!isActuallyReadOnly);
                intField.isReadOnly = isActuallyReadOnly;
                
                if (_isEditMode && !isReadOnly)
                {
                    Debug.Log($"[SaveFileDetailsPanel] Registering callback for INTEGER field with path: {fieldItem.userData}");
                    intField.RegisterValueChangedCallback(evt => 
                    {
                        Debug.Log($"[SaveFileDetailsPanel] Integer field changed: {evt.newValue}, fieldPath: {fieldItem.userData}");
                        UpdateFieldValue(fieldItem, evt.newValue, fieldType);
                        MarkAsChanged();
                    });
                }
                else
                {
                    Debug.Log($"[SaveFileDetailsPanel] NOT registering callback for INTEGER field - EditMode: {_isEditMode}, isReadOnly: {isReadOnly}");
                }
            }
            else if (fieldType == typeof(long))
            {
                var textField = fieldItem.Q<TextField>("field-value-text");
                textField.style.display = DisplayStyle.Flex;
                
                // Format long values nicely (especially for game time)
                long longValue = (long)(value ?? 0);
                if (_isEditMode && !isReadOnly)
                {
                    // In edit mode, show raw value for easier editing
                    textField.value = longValue.ToString();
                }
                else if (longValue > 100000) // Likely ticks or milliseconds
                {
                    // Try to format as time if it looks like game time
                    var timeSpan = TimeSpan.FromMilliseconds(longValue);
                    if (timeSpan.TotalDays < 365) // Reasonable game time
                    {
                        textField.value = $"{longValue:N0} ({timeSpan:hh\\:mm\\:ss})";
                    }
                    else
                    {
                        textField.value = longValue.ToString("N0");
                    }
                }
                else
                {
                    textField.value = longValue.ToString();
                }
                textField.SetEnabled(!isActuallyReadOnly);
                textField.isReadOnly = isActuallyReadOnly;
                
                if (_isEditMode && !isReadOnly)
                {
                    textField.RegisterValueChangedCallback(evt => 
                    {
                        Debug.Log($"[SaveFileDetailsPanel] Long field changed: {evt.newValue}, fieldPath: {fieldItem.userData}");
                        if (long.TryParse(evt.newValue, out long newValue))
                        {
                            UpdateFieldValue(fieldItem, newValue, fieldType);
                            MarkAsChanged();
                        }
                        else
                        {
                            Debug.LogWarning($"[SaveFileDetailsPanel] Invalid long value: {evt.newValue}");
                        }
                    });
                }
            }
            else if (fieldType == typeof(float))
            {
                var floatField = fieldItem.Q<FloatField>("field-value-float");
                floatField.style.display = DisplayStyle.Flex;
                floatField.value = (float)(value ?? 0f);
                floatField.SetEnabled(!isActuallyReadOnly);
                floatField.isReadOnly = isActuallyReadOnly;
                
                if (_isEditMode && !isReadOnly)
                {
                    floatField.RegisterValueChangedCallback(evt => 
                    {
                        Debug.Log($"[SaveFileDetailsPanel] Float field changed: {evt.newValue}, fieldPath: {fieldItem.userData}");
                        UpdateFieldValue(fieldItem, evt.newValue, fieldType);
                        MarkAsChanged();
                    });
                }
            }
            else if (fieldType == typeof(bool))
            {
                var boolField = fieldItem.Q<Toggle>("field-value-bool");
                boolField.style.display = DisplayStyle.Flex;
                boolField.value = (bool)(value ?? false);
                boolField.SetEnabled(!isActuallyReadOnly);
                // Note: Toggle doesn't have isReadOnly property, so we only use SetEnabled
                
                if (_isEditMode && !isReadOnly)
                {
                    Debug.Log($"[SaveFileDetailsPanel] Registering callback for BOOLEAN field with path: {fieldItem.userData}");
                    boolField.RegisterValueChangedCallback(evt => 
                    {
                        Debug.Log($"[SaveFileDetailsPanel] Bool field changed: {evt.newValue}, fieldPath: {fieldItem.userData}");
                        UpdateFieldValue(fieldItem, evt.newValue, fieldType);
                        MarkAsChanged();
                    });
                }
                else
                {
                    Debug.Log($"[SaveFileDetailsPanel] NOT registering callback for BOOLEAN field - EditMode: {_isEditMode}, isReadOnly: {isReadOnly}");
                }
            }
            else if (fieldType == typeof(DateTime))
            {
                var textField = fieldItem.Q<TextField>("field-value-text");
                textField.style.display = DisplayStyle.Flex;
                if (value is DateTime dateTime)
                {
                    textField.value = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    textField.value = "Invalid Date";
                }
                textField.SetEnabled(!isActuallyReadOnly);
                textField.isReadOnly = isActuallyReadOnly;
                
                if (_isEditMode && !isReadOnly)
                {
                    textField.RegisterValueChangedCallback(evt => 
                    {
                        Debug.Log($"[SaveFileDetailsPanel] DateTime field changed: {evt.newValue}, fieldPath: {fieldItem.userData}");
                        if (DateTime.TryParse(evt.newValue, out DateTime newValue))
                        {
                            UpdateFieldValue(fieldItem, newValue, fieldType);
                            MarkAsChanged();
                        }
                        else
                        {
                            Debug.LogWarning($"[SaveFileDetailsPanel] Invalid DateTime value: {evt.newValue}");
                        }
                    });
                }
            }
            else if (fieldType == typeof(Vector3))
            {
                var textField = fieldItem.Q<TextField>("field-value-text");
                textField.style.display = DisplayStyle.Flex;
                if (value is Vector3 vector)
                {
                    if (_isEditMode && !isReadOnly)
                    {
                        // In edit mode, show as editable format
                        textField.value = $"{vector.x:F3},{vector.y:F3},{vector.z:F3}";
                    }
                    else
                    {
                        textField.value = $"({vector.x:F3}, {vector.y:F3}, {vector.z:F3})";
                    }
                }
                else
                {
                    textField.value = _isEditMode ? "0,0,0" : "(0, 0, 0)";
                }
                textField.SetEnabled(!isActuallyReadOnly);
                textField.isReadOnly = isActuallyReadOnly;
                
                if (_isEditMode && !isReadOnly)
                {
                    textField.RegisterValueChangedCallback(evt => 
                    {
                        Debug.Log($"[SaveFileDetailsPanel] Vector3 field changed: {evt.newValue}, fieldPath: {fieldItem.userData}");
                        if (TryParseVector3(evt.newValue, out Vector3 newVector))
                        {
                            UpdateFieldValue(fieldItem, newVector, fieldType);
                            MarkAsChanged();
                        }
                        else
                        {
                            Debug.LogWarning($"[SaveFileDetailsPanel] Invalid Vector3 value: {evt.newValue}");
                        }
                    });
                }
            }
            else if (fieldType == typeof(Color))
            {
                var textField = fieldItem.Q<TextField>("field-value-text");
                textField.style.display = DisplayStyle.Flex;
                if (value is Color color)
                {
                    if (_isEditMode && !isReadOnly)
                    {
                        // In edit mode, show as editable format
                        textField.value = $"{color.r:F3},{color.g:F3},{color.b:F3},{color.a:F3}";
                    }
                    else
                    {
                        textField.value = $"RGBA({color.r:F2}, {color.g:F2}, {color.b:F2}, {color.a:F2})";
                    }
                }
                else
                {
                    textField.value = _isEditMode ? "1,1,1,1" : "RGBA(1.00, 1.00, 1.00, 1.00)";
                }
                textField.SetEnabled(!isActuallyReadOnly);
                textField.isReadOnly = isActuallyReadOnly;
                
                if (_isEditMode && !isReadOnly)
                {
                    textField.RegisterValueChangedCallback(evt => 
                    {
                        Debug.Log($"[SaveFileDetailsPanel] Color field changed: {evt.newValue}, fieldPath: {fieldItem.userData}");
                        if (TryParseColor(evt.newValue, out Color newColor))
                        {
                            UpdateFieldValue(fieldItem, newColor, fieldType);
                            MarkAsChanged();
                        }
                        else
                        {
                            Debug.LogWarning($"[SaveFileDetailsPanel] Invalid Color value: {evt.newValue}");
                        }
                    });
                }
            }
            else
            {
                // Default to text field for everything else
                var textField = fieldItem.Q<TextField>("field-value-text");
                textField.style.display = DisplayStyle.Flex;
                
                // Handle null values gracefully
                if (value == null)
                {
                    textField.value = "<null>";
                    textField.style.color = new StyleColor(Color.gray);
                }
                else
                {
                    textField.value = value.ToString();
                    textField.style.color = StyleKeyword.Initial; // Reset color
                }
                textField.SetEnabled(!isActuallyReadOnly);
                textField.isReadOnly = isActuallyReadOnly;
                
                if (_isEditMode && !isReadOnly)
                {
                    Debug.Log($"[SaveFileDetailsPanel] Registering callback for TEXT field with path: {fieldItem.userData}");
                    textField.RegisterValueChangedCallback(evt => 
                    {
                        Debug.Log($"[SaveFileDetailsPanel] Text field changed: '{evt.newValue}', fieldPath: '{fieldItem.userData}'");
                        UpdateFieldValue(fieldItem, evt.newValue, fieldType);
                        MarkAsChanged();
                    });
                }
                else
                {
                    Debug.Log($"[SaveFileDetailsPanel] NOT registering callback for TEXT field - EditMode: {_isEditMode}, isReadOnly: {isReadOnly}");
                }
            }
        }
        
        /// <summary>
        /// Updates a field value in the edited save data
        /// </summary>
        private void UpdateFieldValue(VisualElement fieldItem, object newValue, Type fieldType)
        {
            if (!_isEditMode || _editedSaveData == null) 
            {
                Debug.LogWarning($"[SaveFileDetailsPanel] Cannot update field - EditMode: {_isEditMode}, EditedData: {_editedSaveData != null}");
                return;
            }
            
            // Get the field path from the fieldItem's data
            var fieldPath = fieldItem.userData as string;
            if (string.IsNullOrEmpty(fieldPath)) 
            {
                Debug.LogWarning($"[SaveFileDetailsPanel] No field path found in userData");
                return;
            }
            
            Debug.Log($"[SaveFileDetailsPanel] Updating field '{fieldPath}' with value '{newValue}' (type: {fieldType.Name})");
            
            try
            {
                // Update the value in the edited save data using reflection
                SetNestedFieldValue(fieldPath, newValue);
                Debug.Log($"[SaveFileDetailsPanel] Successfully updated field '{fieldPath}'");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileDetailsPanel] Failed to update field {fieldPath}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sets a nested field value using dot notation (e.g., "PlayerData.playerName")
        /// </summary>
        private void SetNestedFieldValue(string fieldPath, object value)
        {
            if (_editedSaveData == null) 
            {
                Debug.LogWarning($"[SaveFileDetailsPanel] EditedSaveData is null");
                return;
            }
            
            Debug.Log($"[SaveFileDetailsPanel] SetNestedFieldValue called with path: '{fieldPath}', value: '{value}'");
            
            // Handle runtime object paths like "Object[0].position"
            if (fieldPath.StartsWith("Object["))
            {
                SetRuntimeObjectFieldValue(fieldPath, value);
                return;
            }
            
            var parts = fieldPath.Split('.');
            if (parts.Length == 1)
            {
                // Direct field on SaveFileData
                Debug.Log($"[SaveFileDetailsPanel] Setting direct field '{fieldPath}' on SaveFileData");
                SetFieldValue(_editedSaveData, fieldPath, value);
            }
            else if (parts.Length == 2)
            {
                // Nested field (e.g., PlayerData.playerName)
                var objectName = parts[0];
                var fieldName = parts[1];
                
                Debug.Log($"[SaveFileDetailsPanel] Setting nested field '{fieldName}' on '{objectName}'");
                
                object targetObject = null;
                switch (objectName)
                {
                    case "PlayerData":
                        targetObject = _editedSaveData.PlayerData;
                        Debug.Log($"[SaveFileDetailsPanel] Found PlayerData: {targetObject != null}");
                        break;
                    case "GameSessionData":
                        targetObject = _editedSaveData.GameSessionData;
                        Debug.Log($"[SaveFileDetailsPanel] Found GameSessionData: {targetObject != null}");
                        break;
                    default:
                        Debug.LogWarning($"[SaveFileDetailsPanel] Unknown object name: {objectName}");
                        break;
                }
                
                if (targetObject != null)
                {
                    SetFieldValue(targetObject, fieldName, value);
                }
                else
                {
                    Debug.LogError($"[SaveFileDetailsPanel] Target object '{objectName}' is null");
                }
            }
            else
            {
                Debug.LogWarning($"[SaveFileDetailsPanel] Unsupported field path format: '{fieldPath}'");
            }
        }
        
        /// <summary>
        /// Deletes a runtime object from the list
        /// </summary>
        private void DeleteRuntimeObject(int index, string uniqueID)
        {
            if (!_isEditMode || _editedSaveData == null)
            {
                Debug.LogWarning($"[SaveFileDetailsPanel] Cannot delete runtime object - not in edit mode or no edited data");
                return;
            }

            if (_editedSaveData.RuntimeObjects == null || index >= _editedSaveData.RuntimeObjects.Count)
            {
                Debug.LogError($"[SaveFileDetailsPanel] Runtime object index {index} out of range");
                return;
            }

            // Show confirmation dialog
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Runtime Object", 
                $"Are you sure you want to delete runtime object:\n\nType: {_editedSaveData.RuntimeObjects[index].typeName}\nID: {uniqueID}\n\nThis action cannot be undone.",
                "Delete", 
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            try
            {
                // Remove the object from the list
                _editedSaveData.RuntimeObjects.RemoveAt(index);
                
                // Mark as changed and refresh the display
                MarkAsChanged();
                
                // Refresh the fields display to show updated list
                UpdateFieldsDisplay();
                
                Debug.Log($"[SaveFileDetailsPanel] Successfully deleted runtime object at index {index} with ID: {uniqueID}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileDetailsPanel] Failed to delete runtime object: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to delete runtime object:\n{ex.Message}", "OK");
            }
        }
        
        /// <summary>
        /// Sets a runtime object field value (e.g., "Object[0].position")
        /// </summary>
        private void SetRuntimeObjectFieldValue(string fieldPath, object value)
        {
            Debug.Log($"[SaveFileDetailsPanel] SetRuntimeObjectFieldValue called with path: '{fieldPath}', value: '{value}'");
            
            try
            {
                // Parse the path: Object[index].fieldName
                var match = System.Text.RegularExpressions.Regex.Match(fieldPath, @"Object\[(\d+)\]\.(.+)");
                if (!match.Success)
                {
                    Debug.LogError($"[SaveFileDetailsPanel] Invalid runtime object path format: '{fieldPath}'");
                    return;
                }
                
                int index = int.Parse(match.Groups[1].Value);
                string fieldName = match.Groups[2].Value;
                
                Debug.Log($"[SaveFileDetailsPanel] Parsed runtime object path - Index: {index}, Field: '{fieldName}'");
                
                // Get the runtime objects list
                var runtimeObjects = _editedSaveData.RuntimeObjects;
                if (runtimeObjects == null || index >= runtimeObjects.Count)
                {
                    Debug.LogError($"[SaveFileDetailsPanel] Runtime object index {index} out of range (count: {runtimeObjects?.Count ?? 0})");
                    return;
                }
                
                var serializedRuntimeObject = runtimeObjects[index];
                
                // Deserialize the runtime object
                var deserializedObject = serializedRuntimeObject.Deserialize();
                if (deserializedObject == null)
                {
                    Debug.LogError($"[SaveFileDetailsPanel] Failed to deserialize runtime object at index {index}");
                    return;
                }
                
                Debug.Log($"[SaveFileDetailsPanel] Successfully deserialized runtime object: {deserializedObject.GetType().Name}");
                
                // Update the field value
                SetFieldValue(deserializedObject, fieldName, value);
                
                // Re-serialize and update the serialized runtime object
                var updatedJsonData = JsonUtility.ToJson(deserializedObject);
                var updatedSerializedObject = new SerializedRuntimeObject
                {
                    uniqueID = serializedRuntimeObject.uniqueID,
                    typeName = serializedRuntimeObject.typeName,
                    saveDataTypeName = serializedRuntimeObject.saveDataTypeName,
                    jsonData = updatedJsonData,
                    dataLength = updatedJsonData.Length
                };
                
                // Replace the serialized object in the list
                runtimeObjects[index] = updatedSerializedObject;
                
                Debug.Log($"[SaveFileDetailsPanel] Successfully updated runtime object field '{fieldName}' at index {index}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveFileDetailsPanel] Failed to set runtime object field value: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Sets a field value on an object using reflection
        /// </summary>
        private void SetFieldValue(object targetObject, string fieldName, object value)
        {
            Debug.Log($"[SaveFileDetailsPanel] SetFieldValue called - Object: {targetObject.GetType().Name}, Field: '{fieldName}', Value: '{value}'");
            
            var objectType = targetObject.GetType();
            var field = objectType.GetField(fieldName);
            var property = objectType.GetProperty(fieldName);
            
            if (field != null)
            {
                Debug.Log($"[SaveFileDetailsPanel] Found field '{fieldName}', setting value");
                field.SetValue(targetObject, value);
            }
            else if (property != null && property.CanWrite)
            {
                Debug.Log($"[SaveFileDetailsPanel] Found property '{fieldName}', setting value");
                property.SetValue(targetObject, value);
            }
            else
            {
                Debug.LogError($"[SaveFileDetailsPanel] Field or property '{fieldName}' not found on type {objectType.Name}");
            }
        }
        
        /// <summary>
        /// Tries to parse a Vector3 from a string in format "x,y,z"
        /// </summary>
        private bool TryParseVector3(string input, out Vector3 result)
        {
            result = Vector3.zero;
            
            if (string.IsNullOrEmpty(input)) return false;
            
            var parts = input.Split(',');
            if (parts.Length != 3) return false;
            
            if (float.TryParse(parts[0].Trim(), out float x) &&
                float.TryParse(parts[1].Trim(), out float y) &&
                float.TryParse(parts[2].Trim(), out float z))
            {
                result = new Vector3(x, y, z);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Tries to parse a Color from a string in format "r,g,b,a"
        /// </summary>
        private bool TryParseColor(string input, out Color result)
        {
            result = Color.white;
            
            if (string.IsNullOrEmpty(input)) return false;
            
            var parts = input.Split(',');
            if (parts.Length != 4) return false;
            
            if (float.TryParse(parts[0].Trim(), out float r) &&
                float.TryParse(parts[1].Trim(), out float g) &&
                float.TryParse(parts[2].Trim(), out float b) &&
                float.TryParse(parts[3].Trim(), out float a))
            {
                result = new Color(r, g, b, a);
                return true;
            }
            
            return false;
        }
        
        private void UpdateRawDataDisplay(bool showRawData)
        {
            _rawDataSection.style.display = showRawData ? DisplayStyle.Flex : DisplayStyle.None;
            
            if (!showRawData || _currentSave == null) return;
            
            try
            {
                var savePath = GetSaveFilePath(_currentSave.FileName);
                if (File.Exists(savePath))
                {
                    var jsonContent = File.ReadAllText(savePath);
                    _rawDataContent.text = jsonContent;
                }
                else
                {
                    _rawDataContent.text = "Save file not found";
                }
            }
            catch (Exception ex)
            {
                _rawDataContent.text = $"Error reading save file: {ex.Message}";
            }
        }
        
        private void UpdatePlayModeWarning()
        {
            _playModeWarning.style.display = Application.isPlaying ? DisplayStyle.None : DisplayStyle.Flex;
            _loadButton.SetEnabled(Application.isPlaying);
        }
        
        private string GetSaveFilePath(string fileName)
        {
            // fileName now includes the extension, so just join with directory
            return Path.Combine(Application.persistentDataPath, "Saves", fileName);
        }
    }
}
