

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameFramework.DataStructures;
using GameFramework.SaveSystem.Data;
using GameFramework.SaveSystem.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.Editor.SaveFileManager.UI
{
    /// <summary>
    /// Unity Editor tool for inspecting and managing save files
    /// Now uses UIToolkit for modern, flexible UI
    /// 
    /// Design:
    /// - UIToolkit-based interface with UXML/USS styling
    /// - Three-panel layout similar to profiling session viewer
    /// - Reflection-based field display for flexibility
    /// - Configurable through ScriptableObject settings
    /// </summary>
    public class SaveFileManagerWindow : EditorWindow
    {
        #region Constants and Paths
        
        private const string UXMLPath = "Assets/Scripts/Editor/SaveFileManager/UI/UXML/SaveFileManagerWindow.uxml";
        private const string SaveItemUXMLPath = "Assets/Scripts/Editor/SaveFileManager/UI/UXML/SaveFileListItem.uxml";
        private const float REFRESH_INTERVAL = 10f;
        
        #endregion
        
        #region UI References
        
        private ListView _savesList;
        private SaveFileDetailsPanel _detailsPanel;
        private Button _refreshButton;
        private Toggle _autoRefreshToggle;
        private Toggle _showRawToggle;
        private Button _configButton;
        private Button _fileCountButton;
        private Label _statusMessage;
        private Label _refreshIndicator;
        private VisualElement _noSavesMessage;
        
        #endregion
        
        #region Data and State
        
        private ScriptableObjects.SaveFileDisplayConfig _displayConfig;
        private SaveFileInfo[] _saveFiles = Array.Empty<SaveFileInfo>();
        private SaveFileInfo _selectedSave;
        private int _selectedIndex = -1;
        private bool _isRefreshing = false;
        private float _lastRefreshTime = 0f;
        
        // Templates
        private VisualTreeAsset _saveItemTemplate;
        
        #endregion
        
        #region Unity Editor Integration
        
        [MenuItem("UCTools/Game Framework/Saveable/Save File Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<SaveFileManagerWindow>("Save File Manager");
            window.minSize = new Vector2(700, 500);
            window.Show();
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        public void CreateGUI()
        {
            LoadUIAssets();
            LoadConfiguration();
            _ = RefreshSaveFilesAsync();
        }
        
        private void Update()
        {
            // Auto-refresh if enabled
            if (_autoRefreshToggle?.value == true && 
                Time.realtimeSinceStartup - _lastRefreshTime > REFRESH_INTERVAL)
            {
                _lastRefreshTime = Time.realtimeSinceStartup;
                _ = RefreshSaveFilesAsync();
            }
        }
        
        #endregion
        
        #region UI Setup and Asset Loading
        
        private void LoadUIAssets()
        {
            // Load main UXML
            var mainUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXMLPath);
            if (mainUxml == null)
            {
                Debug.LogError($"Could not load UXML file at {UXMLPath}");
                return;
            }
            
            // Load save item template
            _saveItemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SaveItemUXMLPath);
            if (_saveItemTemplate == null)
            {
                Debug.LogError($"Could not load save item template at {SaveItemUXMLPath}");
                return;
            }
            
            // Clone the main template
            mainUxml.CloneTree(rootVisualElement);
            
            // Get UI references
            GetUIReferences();
            
            // Setup event handlers
            SetupEventHandlers();
            
            // Setup saves list
            SetupSavesList();
            
            // Create details panel
            CreateDetailsPanel();
            
            // Initialize UI state
            UpdateUIState();
        }
        
        private void GetUIReferences()
        {
            _refreshButton = rootVisualElement.Q<Button>("refresh-button");
            _autoRefreshToggle = rootVisualElement.Q<Toggle>("auto-refresh-toggle");
            _showRawToggle = rootVisualElement.Q<Toggle>("show-raw-toggle");
            _configButton = rootVisualElement.Q<Button>("config-button");
            _fileCountButton = rootVisualElement.Q<Button>("file-count-button");
            _savesList = rootVisualElement.Q<ListView>("saves-list");
            _statusMessage = rootVisualElement.Q<Label>("status-message");
            _refreshIndicator = rootVisualElement.Q<Label>("refresh-indicator");
            _noSavesMessage = rootVisualElement.Q("no-saves-message");
        }
        
        private void SetupEventHandlers()
        {
            _refreshButton.clicked += () => _ = RefreshSaveFilesAsync();
            _configButton.clicked += ShowConfigurationWindow;
            _autoRefreshToggle.RegisterValueChangedCallback(OnAutoRefreshToggled);
            _showRawToggle.RegisterValueChangedCallback(OnShowRawToggled);
        }
        
        private void SetupSavesList()
        {
            var saveFilesList = new List<SaveFileInfo>();
            _savesList.itemsSource = saveFilesList;
            _savesList.makeItem = MakeSaveListItem;
            _savesList.bindItem = BindSaveListItem;
            _savesList.itemHeight = 70;
            _savesList.selectionType = SelectionType.Single;
            _savesList.onSelectionChange += OnSaveSelected;
        }
        
        private void CreateDetailsPanel()
        {
            var detailsContainer = rootVisualElement.Q("details-container");
            _detailsPanel = new SaveFileDetailsPanel();
            _detailsPanel.OnLoadRequested += LoadSelectedSave;
            _detailsPanel.OnDeleteRequested += DeleteSelectedSave;
            _detailsPanel.OnShowInExplorerRequested += ShowSaveFileInExplorer;
            detailsContainer.Add(_detailsPanel);
        }
        
        #endregion
        
        #region ListView Implementation
        
        private VisualElement MakeSaveListItem()
        {
            return _saveItemTemplate.CloneTree();
        }
        
        private void BindSaveListItem(VisualElement element, int index)
        {
            if (index >= _saveFiles.Length) return;
            
            var save = _saveFiles[index];
            
            // Update file name and auto-save badge
            var fileName = save.WasAutoSaved ? save.PlayerName : save.GetDisplayName();
            element.Q<Label>("file-name").text = fileName;
            
            var autoSaveBadge = element.Q<Label>("auto-save-badge");
            if (save.WasAutoSaved)
            {
                autoSaveBadge.RemoveFromClassList("hidden");
                autoSaveBadge.text = "AUTO";
            }
            else
            {
                autoSaveBadge.AddToClassList("hidden");
            }
            
            // Update save info
            element.Q<Label>("player-name").text = save.PlayerName ?? "Unknown";
            element.Q<Label>("save-date").text = save.GetFormattedSaveTime("yyyy/MM/dd HH:mm");
            element.Q<Label>("play-time").text = save.GetFormattedGameTime();
            
            // Update additional info
            element.Q<Label>("current-scene").text = $"Scene: {save.CurrentScene ?? "Unknown"}";
        }
        
        private void OnSaveSelected(IEnumerable<object> selectedItems)
        {
            var selectedSave = selectedItems.FirstOrDefault() as SaveFileInfo;
            _selectedSave = selectedSave;
            _selectedIndex = selectedSave != null ? Array.IndexOf(_saveFiles, selectedSave) : -1;
            
            _detailsPanel.LoadSaveFile(selectedSave, _displayConfig, _showRawToggle?.value == true);
        }
        
        #endregion
        
        #region Save File Operations
        
        private async Task RefreshSaveFilesAsync()
        {
            if (_isRefreshing) return;
            
            _isRefreshing = true;
            SetStatus("Refreshing save files...", "info");
            _refreshIndicator.text = "Refreshing...";
            
            try
            {
                _saveFiles = await LoadSaveFilesDirectly();
                
                // Update UI
                (_savesList.itemsSource as List<SaveFileInfo>).Clear();
                (_savesList.itemsSource as List<SaveFileInfo>).AddRange(_saveFiles);
                _savesList.Rebuild();
                
                // Update file count
                _fileCountButton.text = $"Files: {_saveFiles.Length}";
                
                // Show/hide no saves message
                _noSavesMessage.style.display = _saveFiles.Length == 0 ? DisplayStyle.Flex : DisplayStyle.None;
                _savesList.style.display = _saveFiles.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                
                SetStatus($"Loaded {_saveFiles.Length} save files", "info");
            }
            catch (Exception ex)
            {
                SetStatus($"Error refreshing saves: {ex.Message}", "error");
                Debug.LogError($"[SaveFileManager] Error refreshing save files: {ex}");
            }
            finally
            {
                _isRefreshing = false;
                _refreshIndicator.text = "";
            }
        }
        
        private async Task<SaveFileInfo[]> LoadSaveFilesDirectly()
        {
            var saveDirectory = Application.persistentDataPath + "/Saves/";
            
            if (!Directory.Exists(saveDirectory))
            {
                return Array.Empty<SaveFileInfo>();
            }
            
            var saveFiles = Directory.GetFiles(saveDirectory, "*.json");
            var saveInfos = new List<SaveFileInfo>();
            
            foreach (var filePath in saveFiles)
            {
                try
                {
                    var fileName = Path.GetFileName(filePath); // Keep full filename with extension
                    var jsonContent = await File.ReadAllTextAsync(filePath);
                    var saveData = JsonSerializationHelper.DeserializeFromJson<SaveFileData>(jsonContent);
                    
                    if (saveData != null)
                    {
                        var saveInfo = new SaveFileInfo(fileName, saveData);
                        saveInfos.Add(saveInfo);
                    }
                    else
                    {
                        Debug.LogWarning($"[SaveFileManager] Failed to deserialize save file {fileName} - creating corrupted save info");
                        var corruptedSaveInfo = SaveFileInfo.CreateFromFile(filePath);
                        if (corruptedSaveInfo != null)
                        {
                            saveInfos.Add(corruptedSaveInfo);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveFileManager] Failed to load save file {filePath}: {ex.Message}");
                    // Try to create corrupted save info as fallback
                    try
                    {
                        var fileName = Path.GetFileName(filePath);
                        var corruptedSaveInfo = SaveFileInfo.CreateFromFile(filePath);
                        if (corruptedSaveInfo != null)
                        {
                            saveInfos.Add(corruptedSaveInfo);
                        }
                    }
                    catch (Exception fallbackEx)
                    {
                        Debug.LogError($"[SaveFileManager] Even corrupted save creation failed for {filePath}: {fallbackEx.Message}");
                    }
                }
            }
            
            return saveInfos.OrderByDescending(s => s.LastSaveTime).ToArray();
        }
        
        private async void LoadSelectedSave(SaveFileInfo saveInfo)
        {
            if (!Application.isPlaying)
            {
                SetStatus("Cannot load save file - not in play mode", "warning");
                return;
            }
            
            SetStatus($"Loading {saveInfo.GetDisplayName()}...", "info");
            
            // TODO: Implement actual loading
            // This would typically involve finding your save/load service and calling it
            
            SetStatus($"Load functionality not implemented yet", "warning");
        }
        
        private async void DeleteSelectedSave(SaveFileInfo saveInfo)
        {
            var confirmed = EditorUtility.DisplayDialog(
                "Delete Save File",
                $"Are you sure you want to delete '{saveInfo.GetDisplayName()}'?\n\nThis action cannot be undone.",
                "Delete",
                "Cancel"
            );
            
            if (!confirmed) return;
            
            try
            {
                SetStatus($"Deleting {saveInfo.GetDisplayName()}...", "info");
                
                var filePath = GetSaveFilePath(saveInfo.FileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    SetStatus($"Deleted {saveInfo.GetDisplayName()}", "info");
                }
                
                // Clear selection and refresh
                _selectedSave = null;
                _selectedIndex = -1;
                _detailsPanel.LoadSaveFile(null, _displayConfig, false);
                await RefreshSaveFilesAsync();
            }
            catch (Exception ex)
            {
                SetStatus($"Error deleting save: {ex.Message}", "error");
            }
        }
        
        private void ShowSaveFileInExplorer(SaveFileInfo saveInfo)
        {
            var filePath = GetSaveFilePath(saveInfo.FileName);
            
            if (File.Exists(filePath))
            {
                EditorUtility.RevealInFinder(filePath);
            }
            else
            {
                SetStatus("Save file not found", "error");
            }
        }
        
        private string GetSaveFilePath(string fileName)
        {
            // fileName now includes the extension, so just join with directory
            return Path.Combine(Application.persistentDataPath, "Saves", fileName);
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnAutoRefreshToggled(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                _lastRefreshTime = Time.realtimeSinceStartup;
                SetStatus("Auto-refresh enabled", "info");
            }
            else
            {
                SetStatus("Auto-refresh disabled", "info");
            }
        }
        
        private void OnShowRawToggled(ChangeEvent<bool> evt)
        {
            _detailsPanel?.LoadSaveFile(_selectedSave, _displayConfig, evt.newValue);
        }
        
        #endregion
        
        #region Configuration Management
        
        private void LoadConfiguration()
        {
            var configAssets = AssetDatabase.FindAssets("t:SaveFileDisplayConfig");
            if (configAssets.Length > 0)
            {
                var configPath = AssetDatabase.GUIDToAssetPath(configAssets[0]);
                _displayConfig = AssetDatabase.LoadAssetAtPath<ScriptableObjects.SaveFileDisplayConfig>(configPath);
            }
        }
        
        private void ShowConfigurationWindow()
        {
            //SaveFileDisplayConfigWindow.ShowWindow();
        }
        
        #endregion
        
        #region Utility Methods
        
        private void SetStatus(string message, string type)
        {
            _statusMessage.text = message;
            
            // Remove existing type classes
            _statusMessage.RemoveFromClassList("error");
            _statusMessage.RemoveFromClassList("warning");
            _statusMessage.RemoveFromClassList("info");
            
            // Add new type class
            _statusMessage.AddToClassList(type);
            
            Debug.Log($"[SaveFileManager] {message}");
        }
        
        private void UpdateUIState()
        {
            // Set initial toggle states - disable auto-refresh by default
            _autoRefreshToggle.value = false;
            _showRawToggle.value = false;
        }
        
        #endregion
    }
}
