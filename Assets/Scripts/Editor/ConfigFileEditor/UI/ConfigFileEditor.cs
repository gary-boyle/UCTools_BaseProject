using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Diagnostics;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace GameFramework.Editor
{
    /// <summary>
    /// Advanced Unity Editor tool for viewing and editing JSON config files
    /// 
    /// Design:
    /// - UIToolkit-based interface with dynamic JSON property display
    /// - Supports both structured editing and raw JSON editing
    /// - Real-time validation and auto-save capabilities
    /// - File management operations (create, delete, open externally)
    /// 
    /// Pros:
    /// - Dual editing modes (structured/raw JSON)
    /// - Deferred value updates for better performance
    /// - Comprehensive validation and error handling
    /// - Auto-save functionality
    /// - File management integration
    /// 
    /// Cons:
    /// - Complex UI state management
    /// - Requires specific JSON structure for config entries
    /// - Heavy dependency on UIToolkit assets
    /// </summary>
    public class ConfigFileEditorWindow : EditorWindow
    {
        #region Constants and Configuration
        
        private const string UXMLPath = "Assets/Scripts/Editor/ConfigFileEditor/UI/UXML/ConfigFileEditorWindow.uxml";
        private const string PropertyItemUXMLPath = "Assets/Scripts/Editor/ConfigFileEditor/UI/UXML/ConfigPropertyItem.uxml";
        private const string CONFIG_FILE_NAME = "config.json";
        
        /// <summary>
        /// Supported configuration value types for the dropdown
        /// </summary>
        public enum ConfigValueType
        {
            Boolean,
            Single,
            Int32,
            String,
            ResolutionOption,
            QualityOption
        }
        
        #endregion
        
        #region UI Element References
        
        // Toolbar Elements
        private Button _refreshButton;
        private Button _saveButton;
        private Button _revertButton;
        private Toggle _autoSaveToggle;
        private Toggle _rawJsonToggle;
        private Button _actionsButton;
        
        // Left Panel - File Information
        private Label _statusIndicator;
        private Label _statusText;
        private Label _fileSize;
        private Label _fileModified;
        private Label _entryCount;
        private Button _openExternalButton;
        private Button _revealButton;
        private Button _copyPathButton;
        private Button _createFileButton;
        private Button _deleteFileButton;
        private Label _validationIndicator;
        private Label _validationText;
        private Button _validateButton;
        
        // Right Panel - Editor Content
        private VisualElement _noFileMessage;
        private Button _createFilePromptButton;
        private VisualElement _configContent;
        private VisualElement _structuredEditor;
        private VisualElement _rawJsonEditor;
        private ScrollView _configPropertiesContainer;
        private Button _addEntryButton;
        private TextField _jsonTextField;
        private Button _formatJsonButton;
        
        // Status Bar Elements
        private Label _statusMessage;
        private Label _changesIndicator;
        private Label _saveIndicator;
        
        #endregion
        
        #region Data Management and State
        
        private string ConfigFilePath => Path.Combine(Application.persistentDataPath, CONFIG_FILE_NAME);
        private JObject _configData;
        private JObject _originalConfigData;
        private bool _hasUnsavedChanges = false;
        private bool _isValidJson = true;
        private VisualTreeAsset _propertyItemTemplate;
        private Dictionary<string, VisualElement> _propertyElements = new Dictionary<string, VisualElement>();
        
        #endregion
        
        #region Unity Editor Integration
        
        /// <summary>
        /// Opens the Config File Editor window from Unity's menu
        /// </summary>
        [MenuItem("UCTools/Game Framework/Config File Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<ConfigFileEditorWindow>("Config File Editor");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        #endregion
        
        #region Unity Lifecycle Methods
        
        /// <summary>
        /// Initializes the UI when the window is created
        /// </summary>
        public void CreateGUI()
        {
            LoadUIAssets();
            RefreshConfigFile();
        }
        
        /// <summary>
        /// Auto-saves if enabled when the window is closed
        /// </summary>
        private void OnDestroy()
        {
            if (_hasUnsavedChanges && _autoSaveToggle?.value == true)
            {
                SaveConfigFile();
            }
        }
        
        #endregion
        
        #region UI Initialization and Setup
        
        /// <summary>
        /// Loads UXML assets and initializes the UI
        /// </summary>
        private void LoadUIAssets()
        {
            // Load main UXML template
            var mainUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXMLPath);
            if (mainUxml == null)
            {
                UnityEngine.Debug.LogError($"Could not load UXML file at {UXMLPath}");
                return;
            }
            
            // Load property item template
            _propertyItemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PropertyItemUXMLPath);
            if (_propertyItemTemplate == null)
            {
                UnityEngine.Debug.LogError($"Could not load property item template at {PropertyItemUXMLPath}");
                return;
            }
            
            mainUxml.CloneTree(rootVisualElement);
            
            GetUIReferences();
            SetupEventHandlers();
            UpdateUIState();
        }
        
        /// <summary>
        /// Caches references to all UI elements using UQuery
        /// </summary>
        private void GetUIReferences()
        {
            // Toolbar elements
            _refreshButton = rootVisualElement.Q<Button>("refresh-button");
            _saveButton = rootVisualElement.Q<Button>("save-button");
            _revertButton = rootVisualElement.Q<Button>("revert-button");
            _autoSaveToggle = rootVisualElement.Q<Toggle>("auto-save-toggle");
            _rawJsonToggle = rootVisualElement.Q<Toggle>("raw-json-toggle");
            _actionsButton = rootVisualElement.Q<Button>("actions-button");
            
            // Left panel file information
            _statusIndicator = rootVisualElement.Q<Label>("status-indicator");
            _statusText = rootVisualElement.Q<Label>("status-text");
            _fileSize = rootVisualElement.Q<Label>("file-size");
            _fileModified = rootVisualElement.Q<Label>("file-modified");
            _entryCount = rootVisualElement.Q<Label>("entry-count");
            _openExternalButton = rootVisualElement.Q<Button>("open-external-button");
            _revealButton = rootVisualElement.Q<Button>("reveal-button");
            _copyPathButton = rootVisualElement.Q<Button>("copy-path-button");
            _createFileButton = rootVisualElement.Q<Button>("create-file-button");
            _deleteFileButton = rootVisualElement.Q<Button>("delete-file-button");
            _validationIndicator = rootVisualElement.Q<Label>("validation-indicator");
            _validationText = rootVisualElement.Q<Label>("validation-text");
            _validateButton = rootVisualElement.Q<Button>("validate-button");
            
            // Right panel editor content
            _noFileMessage = rootVisualElement.Q("no-file-message");
            _createFilePromptButton = rootVisualElement.Q<Button>("create-file-prompt-button");
            _configContent = rootVisualElement.Q("config-content");
            _structuredEditor = rootVisualElement.Q("structured-editor");
            _rawJsonEditor = rootVisualElement.Q("raw-json-editor");
            _configPropertiesContainer = rootVisualElement.Q<ScrollView>("config-properties-container");
            _addEntryButton = rootVisualElement.Q<Button>("add-entry-button");
            _jsonTextField = rootVisualElement.Q<TextField>("json-text-field");
            _formatJsonButton = rootVisualElement.Q<Button>("format-json-button");
            
            // Status bar elements
            _statusMessage = rootVisualElement.Q<Label>("status-message");
            _changesIndicator = rootVisualElement.Q<Label>("changes-indicator");
            _saveIndicator = rootVisualElement.Q<Label>("save-indicator");
        }
        
        /// <summary>
        /// Registers event handlers for all interactive UI elements
        /// </summary>
        private void SetupEventHandlers()
        {
            // Toolbar event handlers
            _refreshButton.clicked += RefreshConfigFile;
            _saveButton.clicked += SaveConfigFile;
            _revertButton.clicked += RevertChanges;
            _autoSaveToggle.RegisterValueChangedCallback(OnAutoSaveToggled);
            _rawJsonToggle.RegisterValueChangedCallback(OnRawJsonToggled);
            _actionsButton.clicked += ShowActionsMenu;
            
            // Left panel action handlers
            _openExternalButton.clicked += OpenConfigFileExternal;
            _revealButton.clicked += RevealConfigFileInExplorer;
            _copyPathButton.clicked += CopyConfigFilePath;
            _createFileButton.clicked += CreateConfigFile;
            _deleteFileButton.clicked += DeleteConfigFile;
            _validateButton.clicked += ValidateJson;
            
            // Right panel editor handlers
            _createFilePromptButton.clicked += CreateConfigFile;
            _addEntryButton.clicked += AddNewEntry;
            _formatJsonButton.clicked += FormatJsonText;
            _jsonTextField.RegisterValueChangedCallback(OnJsonTextChanged);
        }
        
        #endregion
        
        #region File Operations and Data Management
        
        /// <summary>
        /// Refreshes the config file data and updates the UI
        /// </summary>
        private void RefreshConfigFile()
        {
            try
            {
                bool fileExists = File.Exists(ConfigFilePath);
                
                UpdateFileInfo(fileExists);
                
                if (fileExists)
                {
                    string jsonContent = File.ReadAllText(ConfigFilePath);
                    LoadJsonContent(jsonContent);
                    ShowConfigContent();
                }
                else
                {
                    _configData = null;
                    _originalConfigData = null;
                    ShowNoFileMessage();
                }
                
                _hasUnsavedChanges = false;
                UpdateUIState();
                SetStatus("Config file refreshed", "info");
            }
            catch (Exception ex)
            {
                SetStatus($"Error refreshing config: {ex.Message}", "error");
                UnityEngine.Debug.LogError($"[ConfigFileEditor] Error refreshing config file: {ex}");
            }
        }
        
        /// <summary>
        /// Parses and loads JSON content into the editor
        /// </summary>
        private void LoadJsonContent(string jsonContent)
        {
            try
            {
                _configData = JObject.Parse(jsonContent);
                _originalConfigData = (JObject)_configData.DeepClone();
                _isValidJson = true;
        
                UpdateStructuredEditor();
                UpdateRawJsonEditor();
                UpdateValidation(true, "JSON is valid");
            }
            catch (JsonException ex)
            {
                _isValidJson = false;
                UpdateValidation(false, $"Invalid JSON: {ex.Message}");
        
                // Show raw editor for fixing invalid JSON
                _jsonTextField.value = jsonContent;
                _rawJsonToggle.value = true;
                UpdateEditorMode(true);
            }
        }
        
        /// <summary>
        /// Saves the current config data to file
        /// </summary>
        private void SaveConfigFile()
        {
            try
            {
                if (_configData == null)
                {
                    SetStatus("No config data to save", "warning");
                    return;
                }
                
                // Get JSON content based on current editing mode
                string jsonContent;
                if (_rawJsonToggle.value)
                {
                    jsonContent = _jsonTextField.value;
                    // Validate before saving
                    try
                    {
                        JObject.Parse(jsonContent);
                    }
                    catch (JsonException ex)
                    {
                        SetStatus($"Cannot save invalid JSON: {ex.Message}", "error");
                        return;
                    }
                }
                else
                {
                    jsonContent = _configData.ToString(Formatting.Indented);
                }
                
                // Ensure directory exists and write file
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));
                File.WriteAllText(ConfigFilePath, jsonContent);
                
                _originalConfigData = (JObject)_configData.DeepClone();
                _hasUnsavedChanges = false;
                
                UpdateUIState();
                UpdateFileInfo(true);
                SetStatus("Config file saved successfully", "info");
                
                // Show brief save indicator
                _saveIndicator.text = "Saved";
                EditorApplication.delayCall += () => {
                    if (_saveIndicator != null)
                        _saveIndicator.text = "";
                };
            }
            catch (Exception ex)
            {
                SetStatus($"Error saving config: {ex.Message}", "error");
                UnityEngine.Debug.LogError($"[ConfigFileEditor] Error saving config file: {ex}");
            }
        }
        
        /// <summary>
        /// Creates a new config file with proper structure
        /// </summary>
        private void CreateConfigFile()
        {
            try
            {
                // Create config structure with entries array
                var emptyConfig = new JObject
                {
                    ["entries"] = new JArray()
                };
        
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));
                File.WriteAllText(ConfigFilePath, emptyConfig.ToString(Formatting.Indented));
        
                SetStatus("Config file created with proper structure", "info");
                RefreshConfigFile();
            }
            catch (Exception ex)
            {
                SetStatus($"Error creating config file: {ex.Message}", "error");
            }
        }
        
        /// <summary>
        /// Deletes the config file after user confirmation
        /// </summary>
        private void DeleteConfigFile()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Config File",
                $"Are you sure you want to permanently delete the config file?\n\n{ConfigFilePath}\n\nThis will reset all settings to defaults on next game launch.",
                "Delete",
                "Cancel"
            );
            
            if (!confirmed) return;
            
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    File.Delete(ConfigFilePath);
                    SetStatus("Config file deleted", "info");
                    RefreshConfigFile();
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Error deleting config file: {ex.Message}", "error");
            }
        }
        
        #endregion
        
        #region UI State Updates and Display
        
        /// <summary>
        /// Updates file information display in the left panel
        /// </summary>
        private void UpdateFileInfo(bool fileExists)
        {
            if (fileExists)
            {
                var fileInfo = new FileInfo(ConfigFilePath);
                _statusIndicator.text = "●";
                _statusIndicator.RemoveFromClassList("missing");
                _statusIndicator.AddToClassList(_hasUnsavedChanges ? "modified" : "exists");
                _statusText.text = _hasUnsavedChanges ? "Modified" : "File exists";
                _fileSize.text = FormatFileSize(fileInfo.Length);
                _fileModified.text = fileInfo.LastWriteTime.ToString("MMM dd, HH:mm");
                
                // Count total entries in JSON structure
                int entryCount = 0;
                if (_configData != null)
                {
                    CountJsonEntries(_configData, ref entryCount);
                }
                _entryCount.text = entryCount.ToString();
            }
            else
            {
                _statusIndicator.text = "●";
                _statusIndicator.RemoveFromClassList("exists");
                _statusIndicator.RemoveFromClassList("modified");
                _statusIndicator.AddToClassList("missing");
                _statusText.text = "File not found";
                _fileSize.text = "N/A";
                _fileModified.text = "N/A";
                _entryCount.text = "0";
            }
        }
        
        /// <summary>
        /// Rebuilds the structured editor with current config data
        /// </summary>
        private void UpdateStructuredEditor()
        {
            _configPropertiesContainer.Clear();
            _propertyElements.Clear();
            
            if (_configData == null) return;
            
            foreach (var property in _configData)
            {
                CreatePropertyElement(property.Key, property.Value, _configPropertiesContainer, 0);
            }
        }
        
        /// <summary>
        /// Updates the raw JSON editor to reflect current config data
        /// </summary>
        private void UpdateRawJsonEditor()
        {
            try
            {
                if (_configData != null && _jsonTextField != null)
                {
                    // Temporarily unregister callback to prevent infinite loops
                    _jsonTextField.UnregisterValueChangedCallback(OnJsonTextChanged);
                    _jsonTextField.value = _configData.ToString(Formatting.Indented);
                    _jsonTextField.RegisterValueChangedCallback(OnJsonTextChanged);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ConfigFileEditor] Error updating raw JSON editor: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Updates JSON validation status display
        /// </summary>
        private void UpdateValidation(bool isValid, string message)
        {
            _isValidJson = isValid;
            _validationIndicator.RemoveFromClassList("valid");
            _validationIndicator.RemoveFromClassList("invalid");
            _validationIndicator.RemoveFromClassList("unknown");
            
            if (isValid)
            {
                _validationIndicator.AddToClassList("valid");
                _validationIndicator.text = "●";
            }
            else
            {
                _validationIndicator.AddToClassList("invalid");
                _validationIndicator.text = "●";
            }
            
            _validationText.text = message;
        }
        
        /// <summary>
        /// Updates UI element states based on current conditions
        /// </summary>
        private void UpdateUIState()
        {
            bool fileExists = File.Exists(ConfigFilePath);
            bool hasValidData = _configData != null && _isValidJson;
            
            // Update panel visibility
            _noFileMessage.style.display = fileExists ? DisplayStyle.None : DisplayStyle.Flex;
            _configContent.style.display = fileExists ? DisplayStyle.Flex : DisplayStyle.None;
            
            // Update button enabled states
            _saveButton.SetEnabled(_hasUnsavedChanges && hasValidData);
            _revertButton.SetEnabled(_hasUnsavedChanges);
            _deleteFileButton.SetEnabled(fileExists);
            _openExternalButton.SetEnabled(fileExists);
            _addEntryButton.SetEnabled(hasValidData && !_rawJsonToggle.value);
            
            // Update changes indicator
            _changesIndicator.text = _hasUnsavedChanges ? "●" : "";
            _changesIndicator.tooltip = _hasUnsavedChanges ? "Unsaved changes" : "";
        }
        
        /// <summary>
        /// Shows the no-file message panel
        /// </summary>
        private void ShowNoFileMessage()
        {
            _noFileMessage.style.display = DisplayStyle.Flex;
            _configContent.style.display = DisplayStyle.None;
        }
        
        /// <summary>
        /// Shows the config content panel with appropriate editor
        /// </summary>
        private void ShowConfigContent()
        {
            _noFileMessage.style.display = DisplayStyle.None;
            _configContent.style.display = DisplayStyle.Flex;
            UpdateEditorMode(_rawJsonToggle.value);
        }
        
        #endregion
        
        #region Dynamic Property UI Creation
        
        /// <summary>
        /// Creates a property UI element for a JSON key-value pair
        /// </summary>
        private void CreatePropertyElement(string key, JToken value, VisualElement parent, int depth)
        {
            if (_propertyItemTemplate == null)
            {
                UnityEngine.Debug.LogError("[ConfigFileEditor] Property item template is null. Cannot create property elements.");
                SetStatus("Property template not loaded", "error");
                return;
            }

            var propertyElement = _propertyItemTemplate.CloneTree();
            var propertyItem = propertyElement.Q("property-item");
            
            if (propertyItem == null)
            {
                UnityEngine.Debug.LogError("[ConfigFileEditor] Could not find 'property-item' in template.");
                return;
            }

            // Setup key field with edit restrictions for structural keys
            var keyField = propertyElement.Q<TextField>("key-field");
            if (keyField != null)
            {
                keyField.value = key;
                
                bool isStructuralKey = key == "entries" || 
                                      key.StartsWith("[") && key.EndsWith("]") || 
                                      key == "Value" || 
                                      key == "Type" ||
                                      key == "value";
                
                if (isStructuralKey)
                {
                    keyField.SetEnabled(false);
                }
                else
                {
                    SetupDeferredKeyFieldUpdate(keyField, key);
                }
            }

            // Setup value field based on JSON token type
            SetupValueField(propertyElement, value, key);

            // Setup expand/collapse for complex types
            SetupPropertyExpansion(propertyElement, value, depth);

            // Setup delete button for deletable entries
            SetupDeleteButton(propertyElement, key, parent, depth);

            parent.Add(propertyElement);
            _propertyElements[key] = propertyElement;
        }
        
        /// <summary>
        /// Sets up the appropriate value field based on JSON token type
        /// </summary>
        private void SetupValueField(VisualElement propertyElement, JToken value, string key = "")
        {
            // Hide all value fields initially
            propertyElement.Q("string-field").style.display = DisplayStyle.None;
            propertyElement.Q("int-field").style.display = DisplayStyle.None;
            propertyElement.Q("float-field").style.display = DisplayStyle.None;
            propertyElement.Q("bool-field").style.display = DisplayStyle.None;
            propertyElement.Q("type-dropdown").style.display = DisplayStyle.None;

            // Handle Type field with special dropdown
            if (key == "Type" && value.Type == JTokenType.String)
            {
                SetupTypeDropdown(propertyElement, value);
            }
            // Handle Value field - always string input with validation
            else if (key == "Value")
            {
                SetupValueStringField(propertyElement, value);
            }
            else
            {
                // Standard field types
                SetupStandardValueField(propertyElement, value, key);
            }
        }
        
        /// <summary>
        /// Sets up expand/collapse functionality for objects and arrays
        /// </summary>
        private void SetupPropertyExpansion(VisualElement propertyElement, JToken value, int depth)
        {
            var expandButton = propertyElement.Q<Button>("expand-button");
            var childrenContainer = propertyElement.Q("property-children");

            if (expandButton != null && childrenContainer != null && 
                (value.Type == JTokenType.Object || value.Type == JTokenType.Array))
            {
                expandButton.style.display = DisplayStyle.Flex;
                expandButton.clicked += () => TogglePropertyExpansion(propertyElement, childrenContainer);

                // Add child elements
                if (value.Type == JTokenType.Object)
                {
                    foreach (var child in ((JObject)value))
                    {
                        CreatePropertyElement(child.Key, child.Value, childrenContainer, depth + 1);
                    }
                }
                else if (value.Type == JTokenType.Array)
                {
                    var array = (JArray)value;
                    for (int i = 0; i < array.Count; i++)
                    {
                        CreatePropertyElement($"[{i}]", array[i], childrenContainer, depth + 1);
                    }
                }
            }
            else if (expandButton != null)
            {
                expandButton.style.display = DisplayStyle.None;
                if (childrenContainer != null)
                    childrenContainer.style.display = DisplayStyle.None;
            }
        }
        
        /// <summary>
        /// Sets up delete button for appropriate entries
        /// </summary>
        private void SetupDeleteButton(VisualElement propertyElement, string key, VisualElement parent, int depth)
        {
            var deleteButton = propertyElement.Q<Button>("delete-button");
            if (deleteButton != null)
            {
                // Show delete button only for entries in the entries array
                bool canDelete = key.StartsWith("[") && key.EndsWith("]") && depth == 1;
                
                if (canDelete)
                {
                    deleteButton.style.display = DisplayStyle.Flex;
                    deleteButton.clicked += () => DeleteProperty(key, parent, propertyElement);
                }
                else
                {
                    deleteButton.style.display = DisplayStyle.None;
                }
            }
        }
        
        #endregion
        
        #region Value Field Setup Methods
        
        /// <summary>
        /// Sets up the Type dropdown field for config entries
        /// </summary>
        private void SetupTypeDropdown(VisualElement propertyElement, JToken value)
        {
            var typeDropdown = propertyElement.Q<EnumField>("type-dropdown");
            if (typeDropdown != null)
            {
                typeDropdown.style.display = DisplayStyle.Flex;
                typeDropdown.Init(ConfigValueType.String);
                
                // Set current value if it matches enum values
                string currentType = value.ToString();
                if (System.Enum.TryParse<ConfigValueType>(currentType, out ConfigValueType enumValue))
                {
                    typeDropdown.value = enumValue;
                }
                else
                {
                    typeDropdown.value = ConfigValueType.String;
                    ((JValue)value).Value = "String";
                }
                
                // Handle dropdown changes
                typeDropdown.RegisterValueChangedCallback(evt => {
                    try 
                    {
                        ((JValue)value).Value = evt.newValue.ToString();
                        UpdateRawJsonEditor();
                        OnValueChanged();
                        UpdateCorrespondingValueField(propertyElement, evt.newValue.ToString());
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[ConfigFileEditor] Error updating Type field: {ex.Message}");
                    }
                });
            }
        }
        
        /// <summary>
        /// Sets up the Value string field with type validation
        /// </summary>
        private void SetupValueStringField(VisualElement propertyElement, JToken value)
        {
            var stringField = propertyElement.Q<TextField>("string-field");
            if (stringField != null)
            {
                stringField.style.display = DisplayStyle.Flex;
                stringField.value = value.ToString();
                
                // Get type from sibling Type field for validation
                var parentObject = value.Parent?.Parent as JObject;
                var typeField = parentObject?["Type"]?.ToString();
                
                stringField.label = GetPlaceholderForType(typeField);
                SetupDeferredTextFieldUpdate(stringField, value, typeField, "Value");
            }
        }
        
        /// <summary>
        /// Sets up standard value fields for primitive types
        /// </summary>
        private void SetupStandardValueField(VisualElement propertyElement, JToken value, string key)
        {
            switch (value.Type)
            {
                case JTokenType.String:
                    var stringField = propertyElement.Q<TextField>("string-field");
                    stringField.style.display = DisplayStyle.Flex;
                    stringField.value = value.ToString();
                    SetupDeferredTextFieldUpdate(stringField, value, null, key);
                    break;
                    
                case JTokenType.Integer:
                    var intField = propertyElement.Q<IntegerField>("int-field");
                    intField.style.display = DisplayStyle.Flex;
                    intField.value = value.Value<int>();
                    SetupDeferredIntFieldUpdate(intField, value, key);
                    break;
                    
                case JTokenType.Float:
                    var floatField = propertyElement.Q<FloatField>("float-field");
                    floatField.style.display = DisplayStyle.Flex;
                    floatField.value = value.Value<float>();
                    SetupDeferredFloatFieldUpdate(floatField, value, key);
                    break;
                    
                case JTokenType.Boolean:
                    var boolField = propertyElement.Q<Toggle>("bool-field");
                    boolField.style.display = DisplayStyle.Flex;
                    boolField.value = value.Value<bool>();
                    
                    // Boolean changes immediately
                    boolField.RegisterValueChangedCallback(evt => {
                        try
                        {
                            ((JValue)value).Value = evt.newValue;
                            UpdateRawJsonEditor();
                            OnValueChanged();
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogError($"[ConfigFileEditor] Error updating bool field: {ex.Message}");
                        }
                    });
                    break;
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handles auto-save toggle changes
        /// </summary>
        private void OnAutoSaveToggled(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                SetStatus("Auto-save enabled", "info");
            }
        }
        
        /// <summary>
        /// Handles raw JSON toggle changes
        /// </summary>
        private void OnRawJsonToggled(ChangeEvent<bool> evt)
        {
            UpdateEditorMode(evt.newValue);
        }
        
        /// <summary>
        /// Switches between structured and raw JSON editing modes
        /// </summary>
        private void UpdateEditorMode(bool showRawJson)
        {
            if (showRawJson)
            {
                _structuredEditor.style.display = DisplayStyle.None;
                _rawJsonEditor.style.display = DisplayStyle.Flex;
                UpdateRawJsonEditor();
            }
            else
            {
                _structuredEditor.style.display = DisplayStyle.Flex;
                _rawJsonEditor.style.display = DisplayStyle.None;
            }
        }
        
        /// <summary>
        /// Handles raw JSON text changes with validation
        /// </summary>
        private void OnJsonTextChanged(ChangeEvent<string> evt)
        {
            try
            {
                var parsed = JObject.Parse(evt.newValue);
                _configData = parsed;
                _hasUnsavedChanges = true;
                UpdateValidation(true, "JSON is valid");
                UpdateStructuredEditor();
                UpdateUIState();
        
                if (_autoSaveToggle?.value == true)
                {
                    SaveConfigFile();
                }
            }
            catch (JsonException ex)
            {
                UpdateValidation(false, $"Invalid JSON: {ex.Message}");
                UnityEngine.Debug.LogWarning($"[ConfigFileEditor] Invalid JSON in text field: {ex.Message}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ConfigFileEditor] Error processing JSON text change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles value changes and triggers auto-save if enabled
        /// </summary>
        private void OnValueChanged()
        {
            try
            {
                _hasUnsavedChanges = true;
                UpdateUIState();
        
                // Auto-save if enabled
                if (_autoSaveToggle?.value == true)
                {
                    SaveConfigFile();
                }
        
                UpdateFileInfo(File.Exists(ConfigFilePath));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ConfigFileEditor] Error in OnValueChanged: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Adds a new config entry to the entries array
        /// </summary>
        private void AddNewEntry()
        {
            if (_configData == null)
            {
                _configData = new JObject();
            }
    
            // Ensure entries array exists
            if (_configData["entries"] == null)
            {
                _configData["entries"] = new JArray();
            }
    
            var entriesArray = _configData["entries"] as JArray;
            if (entriesArray == null)
            {
                SetStatus("Invalid entries structure - expected array", "error");
                return;
            }
    
            // Find unique key name
            string newKey = "new.setting";
            int counter = 1;
            while (EntryKeyExists(entriesArray, newKey))
            {
                newKey = $"new.setting{counter++}";
            }
    
            // Create new entry with proper structure
            var newEntry = new JObject
            {
                ["key"] = newKey,
                ["value"] = new JObject
                {
                    ["Value"] = "DefaultValue",
                    ["Type"] = "String"
                }
            };
    
            entriesArray.Add(newEntry);
            UpdateStructuredEditor();
            OnValueChanged();
            SetStatus($"Added new entry: {newKey}", "info");
        }
        
        /// <summary>
        /// Deletes a property from the config
        /// </summary>
        private void DeleteProperty(string key, VisualElement parent, VisualElement element)
        {
            try
            {
                parent.Remove(element);
        
                if (_propertyElements.ContainsKey(key))
                {
                    _propertyElements.Remove(key);
                }
        
                // Handle array index deletion
                if (key.StartsWith("[") && key.EndsWith("]"))
                {
                    if (int.TryParse(key.Trim('[', ']'), out int index))
                    {
                        var entriesArray = _configData?["entries"] as JArray;
                        if (entriesArray != null && index >= 0 && index < entriesArray.Count)
                        {
                            entriesArray.RemoveAt(index);
                            UpdateStructuredEditor();
                            UpdateRawJsonEditor();
                        }
                    }
                }
                else if (_configData?.ContainsKey(key) == true)
                {
                    _configData.Remove(key);
                    UpdateRawJsonEditor();
                }
        
                OnValueChanged();
                SetStatus($"Deleted property: {key}", "info");
            }
            catch (Exception ex)
            {
                SetStatus($"Error deleting property: {ex.Message}", "error");
                UnityEngine.Debug.LogError($"[ConfigFileEditor] Error deleting property '{key}': {ex.Message}");
            }
        }
        
        /// <summary>
        /// Toggles property expansion for objects and arrays
        /// </summary>
        private void TogglePropertyExpansion(VisualElement propertyItem, VisualElement childrenContainer)
        {
            bool isCollapsed = propertyItem.ClassListContains("collapsed");
            
            if (isCollapsed)
            {
                propertyItem.RemoveFromClassList("collapsed");
                propertyItem.AddToClassList("expanded");
                childrenContainer.style.display = DisplayStyle.Flex;
            }
            else
            {
                propertyItem.RemoveFromClassList("expanded");
                propertyItem.AddToClassList("collapsed");
                childrenContainer.style.display = DisplayStyle.None;
            }
        }
        
        /// <summary>
        /// Reverts all changes to the original config data
        /// </summary>
        private void RevertChanges()
        {
            if (_originalConfigData != null)
            {
                _configData = (JObject)_originalConfigData.DeepClone();
                _hasUnsavedChanges = false;
                UpdateStructuredEditor();
                UpdateRawJsonEditor();
                UpdateUIState();
                SetStatus("Changes reverted", "info");
            }
        }
        
        /// <summary>
        /// Validates the current JSON structure
        /// </summary>
        private void ValidateJson()
        {
            try
            {
                if (_rawJsonToggle.value)
                {
                    JObject.Parse(_jsonTextField.value);
                }
                
                UpdateValidation(true, "JSON validation passed");
                SetStatus("JSON is valid", "info");
            }
            catch (JsonException ex)
            {
                UpdateValidation(false, $"JSON validation failed: {ex.Message}");
                SetStatus($"JSON validation failed: {ex.Message}", "error");
            }
        }
        
        /// <summary>
        /// Formats the raw JSON text with proper indentation
        /// </summary>
        private void FormatJsonText()
        {
            try
            {
                var parsed = JObject.Parse(_jsonTextField.value);
                _jsonTextField.value = parsed.ToString(Formatting.Indented);
                SetStatus("JSON formatted", "info");
            }
            catch (JsonException ex)
            {
                SetStatus($"Cannot format invalid JSON: {ex.Message}", "error");
            }
        }
        
        /// <summary>
        /// Shows the actions context menu
        /// </summary>
        private void ShowActionsMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Open in External Editor"), false, OpenConfigFileExternal);
            menu.AddItem(new GUIContent("Reveal in Explorer"), false, RevealConfigFileInExplorer);
            menu.AddItem(new GUIContent("Copy File Path"), false, CopyConfigFilePath);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Create Backup"), false, CreateBackup);
            menu.AddItem(new GUIContent("Restore from Backup"), false, RestoreFromBackup);
            menu.ShowAsContext();
        }
        
        #endregion
        
        #region File System Operations
        
        /// <summary>
        /// Opens the config file in the default external editor
        /// </summary>
        private void OpenConfigFileExternal()
        {
            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    Process.Start(ConfigFilePath);
                    SetStatus("Opened config file in external editor", "info");
                }
                catch (Exception ex)
                {
                    SetStatus($"Failed to open external editor: {ex.Message}", "error");
                }
            }
        }
        
        /// <summary>
        /// Reveals the config file location in the system file explorer
        /// </summary>
        private void RevealConfigFileInExplorer()
        {
            string folderPath = Path.GetDirectoryName(ConfigFilePath);
            if (Directory.Exists(folderPath))
            {
                EditorUtility.RevealInFinder(File.Exists(ConfigFilePath) ? ConfigFilePath : folderPath);
                SetStatus("Revealed config file location", "info");
            }
        }
        
        /// <summary>
        /// Copies the config file path to the system clipboard
        /// </summary>
        private void CopyConfigFilePath()
        {
            EditorGUIUtility.systemCopyBuffer = ConfigFilePath;
            SetStatus("Config file path copied to clipboard", "info");
        }
        
        /// <summary>
        /// Creates a timestamped backup of the config file
        /// </summary>
        private void CreateBackup()
        {
            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    string backupPath = ConfigFilePath + ".backup." + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    File.Copy(ConfigFilePath, backupPath);
                    SetStatus($"Backup created: {Path.GetFileName(backupPath)}", "info");
                }
                catch (Exception ex)
                {
                    SetStatus($"Failed to create backup: {ex.Message}", "error");
                }
            }
        }
        
        /// <summary>
        /// Restores the config file from a selected backup
        /// </summary>
        private void RestoreFromBackup()
        {
            string backupPath = EditorUtility.OpenFilePanel("Select Backup File", 
                Path.GetDirectoryName(ConfigFilePath), "");
                
            if (!string.IsNullOrEmpty(backupPath) && File.Exists(backupPath))
            {
                try
                {
                    File.Copy(backupPath, ConfigFilePath, true);
                    SetStatus("Backup restored successfully", "info");
                    RefreshConfigFile();
                }
                catch (Exception ex)
                {
                    SetStatus($"Failed to restore backup: {ex.Message}", "error");
                }
            }
        }
        
        #endregion
        
        #region Deferred Update System
        
        /// <summary>
        /// Sets up deferred update handling for text fields (commits on focus loss, Enter, or Tab)
        /// </summary>
        private void SetupDeferredTextFieldUpdate(TextField textField, JToken value, string valueType, string key)
        {
            string lastCommittedValue = textField.value;
            
            textField.RegisterCallback<FocusOutEvent>(evt => {
                CommitTextFieldValue(textField, value, valueType, key, ref lastCommittedValue);
            });
            
            textField.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Tab)
                {
                    CommitTextFieldValue(textField, value, valueType, key, ref lastCommittedValue);
                    
                    if (evt.keyCode == KeyCode.Tab)
                    {
                        evt.StopPropagation();
                        FocusNextField(textField);
                    }
                }
            });
        }
        
        /// <summary>
        /// Sets up deferred update handling for integer fields
        /// </summary>
        private void SetupDeferredIntFieldUpdate(IntegerField intField, JToken value, string key)
        {
            int lastCommittedValue = intField.value;
            
            intField.RegisterCallback<FocusOutEvent>(evt => {
                if (intField.value != lastCommittedValue)
                {
                    CommitIntFieldValue(intField, value, key, ref lastCommittedValue);
                }
            });
            
            intField.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Tab)
                {
                    if (intField.value != lastCommittedValue)
                    {
                        CommitIntFieldValue(intField, value, key, ref lastCommittedValue);
                    }
                    
                    if (evt.keyCode == KeyCode.Tab)
                    {
                        evt.StopPropagation();
                        FocusNextField(intField);
                    }
                }
            });
        }
        
        /// <summary>
        /// Sets up deferred update handling for float fields
        /// </summary>
        private void SetupDeferredFloatFieldUpdate(FloatField floatField, JToken value, string key)
        {
            float lastCommittedValue = floatField.value;
            
            floatField.RegisterCallback<FocusOutEvent>(evt => {
                if (Math.Abs(floatField.value - lastCommittedValue) > float.Epsilon)
                {
                    CommitFloatFieldValue(floatField, value, key, ref lastCommittedValue);
                }
            });
            
            floatField.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Tab)
                {
                    if (Math.Abs(floatField.value - lastCommittedValue) > float.Epsilon)
                    {
                        CommitFloatFieldValue(floatField, value, key, ref lastCommittedValue);
                    }
                    
                    if (evt.keyCode == KeyCode.Tab)
                    {
                        evt.StopPropagation();
                        FocusNextField(floatField);
                    }
                }
            });
        }
        
        /// <summary>
        /// Sets up deferred update handling for key fields
        /// </summary>
        private void SetupDeferredKeyFieldUpdate(TextField keyField, string originalKey)
        {
            string lastCommittedKey = originalKey;
            
            keyField.RegisterCallback<FocusOutEvent>(evt => {
                if (keyField.value != lastCommittedKey)
                {
                    OnPropertyKeyChanged(lastCommittedKey, keyField.value);
                    lastCommittedKey = keyField.value;
                }
            });
            
            keyField.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Tab)
                {
                    if (keyField.value != lastCommittedKey)
                    {
                        OnPropertyKeyChanged(lastCommittedKey, keyField.value);
                        lastCommittedKey = keyField.value;
                    }
                    
                    if (evt.keyCode == KeyCode.Tab)
                    {
                        evt.StopPropagation();
                        FocusNextField(keyField);
                    }
                }
            });
        }
        
        #endregion
        
        #region Value Commit Methods
        
        /// <summary>
        /// Commits text field value changes to JSON with validation
        /// </summary>
        private void CommitTextFieldValue(TextField textField, JToken value, string valueType, string key, ref string lastCommittedValue)
        {
            try
            {
                string currentValue = textField.value;
                
                if (currentValue == lastCommittedValue)
                    return;
                    
                // Validate if type constraint exists
                if (!string.IsNullOrEmpty(valueType))
                {
                    if (ValidateValueForType(currentValue, valueType))
                    {
                        textField.RemoveFromClassList("invalid-input");
                    }
                    else
                    {
                        textField.AddToClassList("invalid-input");
                        SetStatus($"Invalid value for type {valueType}: {currentValue}", "warning");
                        return;
                    }
                }
                
                ((JValue)value).Value = currentValue;
                lastCommittedValue = currentValue;
                
                UpdateRawJsonEditor();
                OnValueChanged();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ConfigFileEditor] Error committing text field value: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Commits integer field value changes to JSON
        /// </summary>
        private void CommitIntFieldValue(IntegerField intField, JToken value, string key, ref int lastCommittedValue)
        {
            try
            {
                ((JValue)value).Value = intField.value;
                lastCommittedValue = intField.value;
                
                UpdateRawJsonEditor();
                OnValueChanged();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ConfigFileEditor] Error committing int field value: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Commits float field value changes to JSON
        /// </summary>
        private void CommitFloatFieldValue(FloatField floatField, JToken value, string key, ref float lastCommittedValue)
        {
            try
            {
                ((JValue)value).Value = floatField.value;
                lastCommittedValue = floatField.value;
                
                UpdateRawJsonEditor();
                OnValueChanged();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ConfigFileEditor] Error committing float field value: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Handles property key changes
        /// </summary>
        private void OnPropertyKeyChanged(string oldKey, string newKey)
        {
            try
            {
                OnValueChanged();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ConfigFileEditor] Error changing property key: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Validation and Type Handling
        
        /// <summary>
        /// Updates the corresponding Value field when Type changes
        /// </summary>
        private void UpdateCorrespondingValueField(VisualElement typePropertyElement, string newType)
        {
            try
            {
                var entryContainer = typePropertyElement.parent;
                while (entryContainer != null && !entryContainer.ClassListContains("property-item"))
                {
                    entryContainer = entryContainer.parent;
                }
        
                if (entryContainer != null)
                {
                    var valueFields = entryContainer.Query<TextField>("string-field").ToList();
                    foreach (var valueField in valueFields)
                    {
                        var keyField = valueField.parent?.parent?.parent?.Q<TextField>("key-field");
                        if (keyField != null && keyField.value == "Value")
                        {
                            valueField.label = GetPlaceholderForType(newType);
                            valueField.RemoveFromClassList("invalid-input");
                    
                            if (!ValidateValueForType(valueField.value, newType))
                            {
                                valueField.AddToClassList("invalid-input");
                                SetStatus($"Current value '{valueField.value}' is invalid for type {newType}", "warning");
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ConfigFileEditor] Error updating corresponding value field: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets placeholder text for different value types
        /// </summary>
        private string GetPlaceholderForType(string type)
        {
            return type switch
            {
                "Boolean" => "true or false",
                "Single" => "e.g., 1.5, 2.0",
                "Int32" => "e.g., 1, 42, -5",
                "String" => "Enter text value",
                "ResolutionOption" => "e.g., 1920x1080",
                "QualityOption" => "e.g., High, Medium, Low",
                _ => "Enter value"
            };
        }
        
        /// <summary>
        /// Validates input value against the specified type
        /// </summary>
        private bool ValidateValueForType(string inputValue, string type)
        {
            if (string.IsNullOrEmpty(inputValue))
                return true;
                
            try
            {
                return type switch
                {
                    "Boolean" => bool.TryParse(inputValue, out _),
                    "Single" => float.TryParse(inputValue, out _),
                    "Int32" => int.TryParse(inputValue, out _),
                    "String" => true,
                    "ResolutionOption" => ValidateResolutionFormat(inputValue),
                    "QualityOption" => ValidateQualityFormat(inputValue),
                    _ => true
                };
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Validates resolution format (e.g., "1920x1080")
        /// </summary>
        private bool ValidateResolutionFormat(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            
            string[] parts = value.Split('x');
            if (parts.Length != 2) return false;
            
            return int.TryParse(parts[0], out _) && int.TryParse(parts[1], out _);
        }
        
        /// <summary>
        /// Validates quality option format
        /// </summary>
        private bool ValidateQualityFormat(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            
            string[] validQualities = { "Low", "Medium", "High", "Ultra", "Very Low", "Very High" };
            return Array.Exists(validQualities, q => q.Equals(value, StringComparison.OrdinalIgnoreCase));
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Sets status message with appropriate styling
        /// </summary>
        private void SetStatus(string message, string type)
        {
            _statusMessage.text = message;
            
            _statusMessage.RemoveFromClassList("error");
            _statusMessage.RemoveFromClassList("warning");
            _statusMessage.RemoveFromClassList("info");
            _statusMessage.AddToClassList(type);
        }
        
        /// <summary>
        /// Formats file size in human-readable format
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
        
        /// <summary>
        /// Recursively counts JSON entries for statistics
        /// </summary>
        private void CountJsonEntries(JToken token, ref int count)
        {
            if (token.Type == JTokenType.Object)
            {
                foreach (var property in ((JObject)token))
                {
                    count++;
                    CountJsonEntries(property.Value, ref count);
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (var item in ((JArray)token))
                {
                    CountJsonEntries(item, ref count);
                }
            }
        }
        
        /// <summary>
        /// Checks if a key already exists in the entries array
        /// </summary>
        private bool EntryKeyExists(JArray entriesArray, string key)
        {
            foreach (var entry in entriesArray)
            {
                if (entry is JObject obj && obj["key"]?.ToString() == key)
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Moves focus to the next focusable field
        /// </summary>
        private void FocusNextField(VisualElement currentField)
        {
            try
            {
                var focusableElements = rootVisualElement.Query<VisualElement>()
                    .Where(e => e.canGrabFocus && e.enabledSelf && e.style.display != DisplayStyle.None)
                    .ToList();
                
                int currentIndex = focusableElements.IndexOf(currentField);
                if (currentIndex >= 0 && currentIndex < focusableElements.Count - 1)
                {
                    focusableElements[currentIndex + 1].Focus();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[ConfigFileEditor] Error focusing next field: {ex.Message}");
            }
        }
        
        #endregion
    }
}
