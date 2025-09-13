using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GameFramework.DataStructures;
using GameFramework.Services;
using GameFramework.Services.Interfaces;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor
{
    /// <summary>
    /// Unity Editor tool for inspecting and managing save files
    /// 
    /// Intent: Provide developers with a comprehensive interface for save file management
    /// 
    /// Design:
    /// - Uses reflection-based field display for flexibility with changing save structures
    /// - Configurable display properties through ScriptableObject settings
    /// - Handles both runtime and edit-mode operations safely
    /// - Provides basic CRUD operations (View, Load, Delete) for save files
    /// 
    /// Pros:
    /// - Adapts automatically to changes in save file structure
    /// - Configurable display without code changes
    /// - Safe operation in both play and edit modes
    /// - Comprehensive error handling and validation
    /// 
    /// Cons:
    /// - Reflection usage has slight performance overhead
    /// - Requires understanding of save file structure for configuration
    /// - Some operations only available during play mode
    /// </summary>
    public class SaveFileManagerWindow : EditorWindow
    {
        #region Configuration and Settings
        
        [SerializeField] private SaveFileDisplayConfig _displayConfig;
        [SerializeField] private Vector2 _scrollPosition;
        [SerializeField] private int _selectedSaveIndex = -1;
        [SerializeField] private bool _showRawData = false;
        [SerializeField] private bool _autoRefresh = true;
        [SerializeField] private float _lastRefreshTime = 0f;
        
        private SaveFileInfo[] _saveFiles = Array.Empty<SaveFileInfo>();
        private string _statusMessage = "Ready";
        private MessageType _statusType = MessageType.Info;
        private bool _isRefreshing = false;
        
        // Refresh interval in seconds
        private const float REFRESH_INTERVAL = 2f;
        
        #endregion
        
        #region Unity Editor Menu Integration
        
        [MenuItem("UCTools/Game Framework/Save File Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<SaveFileManagerWindow>("Save File Manager");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnEnable()
        {
            LoadConfiguration();
            _ = RefreshSaveFilesAsync();
        }
        
        private void Update()
        {
            // Auto-refresh save files periodically if enabled
            if (_autoRefresh && Time.realtimeSinceStartup - _lastRefreshTime > REFRESH_INTERVAL)
            {
                _lastRefreshTime = Time.realtimeSinceStartup;
                _ = RefreshSaveFilesAsync();
            }
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            DrawMainContent();
            DrawStatusBar();
        }
        
        #endregion
        
        #region UI Drawing Methods
        
        /// <summary>
        /// Draws the main toolbar with refresh and configuration options
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // Refresh button
            GUI.enabled = !_isRefreshing;
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                _ = RefreshSaveFilesAsync();
            }
            GUI.enabled = true;
            
            GUILayout.Space(10);
            
            // Auto-refresh toggle
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);
            
            GUILayout.Space(10);
            
            // Show raw data toggle
            _showRawData = GUILayout.Toggle(_showRawData, "Show Raw Data", EditorStyles.toolbarButton);
            
            GUILayout.FlexibleSpace();
            
            // Configuration button
            if (GUILayout.Button("Config", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                ShowConfigurationWindow();
            }
            
            // Save count display
            GUILayout.Label($"Files: {_saveFiles.Length}", EditorStyles.toolbarButton);
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Draws the main content area with save file list and details
        /// </summary>
        private void DrawMainContent()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Left panel - Save file list
            DrawSaveFileList();
            
            // Right panel - Selected save details
            DrawSaveFileDetails();
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Draws the scrollable list of save files
        /// </summary>
        private void DrawSaveFileList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            
            EditorGUILayout.LabelField("Save Files", EditorStyles.boldLabel);
            
            if (_saveFiles.Length == 0)
            {
                EditorGUILayout.HelpBox("No save files found", MessageType.Info);
            }
            else
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                
                for (int i = 0; i < _saveFiles.Length; i++)
                {
                    DrawSaveFileListItem(i, _saveFiles[i]);
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draws an individual save file list item with selection handling
        /// </summary>
        private void DrawSaveFileListItem(int index, SaveFileInfo saveInfo)
        {
            var isSelected = index == _selectedSaveIndex;
            var style = isSelected ? EditorStyles.selectionRect : GUIStyle.none;
            
            EditorGUILayout.BeginVertical(style);
            
            if (GUILayout.Button(GUIContent.none, GUIStyle.none, GUILayout.Height(50)))
            {
                _selectedSaveIndex = isSelected ? -1 : index;
            }
            
            var rect = GUILayoutUtility.GetLastRect();
            
            // Draw save file info
            var labelRect = new Rect(rect.x + 5, rect.y + 5, rect.width - 10, 16);
            var fileName = saveInfo.IsAutoSave ? $"[AUTO] {saveInfo.PlayerName}" : saveInfo.FileName;
            GUI.Label(labelRect, fileName, EditorStyles.boldLabel);
            
            var detailRect = new Rect(rect.x + 5, rect.y + 22, rect.width - 10, 14);
            var details = $"{saveInfo.PlayerName} • Lv.{saveInfo.PlayerLevel} • {saveInfo.FormattedDate}";
            GUI.Label(detailRect, details, EditorStyles.miniLabel);
            
            var timeRect = new Rect(rect.x + 5, rect.y + 36, rect.width - 10, 12);
            GUI.Label(timeRect, $"Playtime: {saveInfo.FormattedPlayTime}", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
            
            // Add separator
            var separatorRect = GUILayoutUtility.GetRect(0, 1);
            EditorGUI.DrawRect(separatorRect, Color.gray * 0.3f);
        }
        
        /// <summary>
        /// Draws detailed information for the selected save file
        /// </summary>
        private void DrawSaveFileDetails()
        {
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.LabelField("Save File Details", EditorStyles.boldLabel);
            
            if (_selectedSaveIndex >= 0 && _selectedSaveIndex < _saveFiles.Length)
            {
                var selectedSave = _saveFiles[_selectedSaveIndex];
                DrawSaveFileDetailContent(selectedSave);
            }
            else
            {
                EditorGUILayout.HelpBox("Select a save file to view details", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draws the detailed content for a selected save file using flexible field display
        /// </summary>
        private void DrawSaveFileDetailContent(SaveFileInfo saveInfo)
        {
            EditorGUILayout.BeginVertical("box");
            
            // Action buttons
            DrawActionButtons(saveInfo);
            
            GUILayout.Space(10);
            
            // Display configured fields
            if (_displayConfig != null)
            {
                DrawConfiguredFields(saveInfo);
            }
            else
            {
                DrawDefaultFields(saveInfo);
            }
            
            GUILayout.Space(10);
            
            // Raw data section
            if (_showRawData)
            {
                DrawRawDataSection(saveInfo);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draws action buttons for the selected save file
        /// </summary>
        private void DrawActionButtons(SaveFileInfo saveInfo)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Load button (only available in play mode)
            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Load Game", GUILayout.Height(30)))
            {
                LoadSelectedSave(saveInfo);
            }
            GUI.enabled = true;
            
            // Delete button
            GUI.backgroundColor = Color.red * 0.8f;
            if (GUILayout.Button("Delete", GUILayout.Height(30), GUILayout.Width(80)))
            {
                DeleteSelectedSave(saveInfo);
            }
            GUI.backgroundColor = Color.white;
            
            // Open file location
            if (GUILayout.Button("Show in Explorer", GUILayout.Height(30), GUILayout.Width(120)))
            {
                ShowSaveFileInExplorer(saveInfo);
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to load save files", MessageType.Warning);
            }
        }
        
        /// <summary>
        /// Draws fields based on display configuration
        /// </summary>
        private void DrawConfiguredFields(SaveFileInfo saveInfo)
        {
            foreach (var fieldConfig in _displayConfig.DisplayFields)
            {
                DrawFieldValue(saveInfo, fieldConfig.FieldName, fieldConfig.DisplayName, fieldConfig.IsReadOnly);
            }
        }
        
        /// <summary>
        /// Draws default set of fields when no configuration is available
        /// </summary>
        private void DrawDefaultFields(SaveFileInfo saveInfo)
        {
            DrawFieldValue(saveInfo, nameof(SaveFileInfo.FileName), "File Name");
            DrawFieldValue(saveInfo, nameof(SaveFileInfo.PlayerName), "Player Name");
            DrawFieldValue(saveInfo, nameof(SaveFileInfo.Difficulty), "Difficulty");
            DrawFieldValue(saveInfo, nameof(SaveFileInfo.CurrentScene), "Current Scene");
            DrawFieldValue(saveInfo, nameof(SaveFileInfo.PlayerLevel), "Player Level");
            DrawFieldValue(saveInfo, nameof(SaveFileInfo.Score), "Score");
            DrawFieldValue(saveInfo, nameof(SaveFileInfo.FormattedPlayTime), "Play Time");
            DrawFieldValue(saveInfo, nameof(SaveFileInfo.FormattedDate), "Last Save");
            DrawFieldValue(saveInfo, nameof(SaveFileInfo.IsAutoSave), "Auto Save");
        }
        
        /// <summary>
        /// Draws a field value using reflection for flexibility
        /// </summary>
        private void DrawFieldValue(SaveFileInfo saveInfo, string fieldName, string displayName, bool isReadOnly = true)
        {
            try
            {
                var field = typeof(SaveFileInfo).GetField(fieldName);
                var property = typeof(SaveFileInfo).GetProperty(fieldName);
                
                object value = null;
                Type fieldType = null;
                
                if (field != null)
                {
                    value = field.GetValue(saveInfo);
                    fieldType = field.FieldType;
                }
                else if (property != null)
                {
                    value = property.GetValue(saveInfo);
                    fieldType = property.PropertyType;
                }
                else
                {
                    EditorGUILayout.LabelField(displayName, "Field not found");
                    return;
                }
                
                DrawTypedFieldValue(displayName, value, fieldType, isReadOnly);
            }
            catch (Exception ex)
            {
                EditorGUILayout.LabelField(displayName, $"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Draws a field value with appropriate UI control based on type
        /// </summary>
        private void DrawTypedFieldValue(string label, object value, Type fieldType, bool isReadOnly)
        {
            GUI.enabled = !isReadOnly;
            
            if (fieldType == typeof(string))
            {
                EditorGUILayout.TextField(label, value?.ToString() ?? "");
            }
            else if (fieldType == typeof(int))
            {
                EditorGUILayout.IntField(label, (int)(value ?? 0));
            }
            else if (fieldType == typeof(float))
            {
                EditorGUILayout.FloatField(label, (float)(value ?? 0f));
            }
            else if (fieldType == typeof(bool))
            {
                EditorGUILayout.Toggle(label, (bool)(value ?? false));
            }
            else if (fieldType == typeof(DateTime))
            {
                EditorGUILayout.TextField(label, ((DateTime)(value ?? DateTime.MinValue)).ToString());
            }
            else
            {
                EditorGUILayout.TextField(label, value?.ToString() ?? "null");
            }
            
            GUI.enabled = true;
        }
        
        /// <summary>
        /// Draws raw JSON data section for debugging
        /// </summary>
        private void DrawRawDataSection(SaveFileInfo saveInfo)
        {
            EditorGUILayout.LabelField("Raw Save Data", EditorStyles.boldLabel);
            
            try
            {
                var savePath = GetSaveFilePath(saveInfo.FileName);
                if (File.Exists(savePath))
                {
                    var jsonContent = File.ReadAllText(savePath);
                    
                    EditorGUILayout.BeginVertical("textArea", GUILayout.Height(200));
                    var scrollPos = EditorGUILayout.BeginScrollView(Vector2.zero);
                    EditorGUILayout.SelectableLabel(jsonContent, GUILayout.ExpandHeight(true));
                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("Save file not found", MessageType.Warning);
                }
            }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox($"Error reading save file: {ex.Message}", MessageType.Error);
            }
        }
        
        /// <summary>
        /// Draws the status bar at the bottom of the window
        /// </summary>
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            var statusContent = new GUIContent(_statusMessage);
            var statusStyle = EditorStyles.toolbarButton;
            
            // Change color based on message type
            switch (_statusType)
            {
                case MessageType.Error:
                    GUI.contentColor = Color.red;
                    break;
                case MessageType.Warning:
                    GUI.contentColor = Color.yellow;
                    break;
                case MessageType.Info:
                    GUI.contentColor = Color.white;
                    break;
            }
            
            GUILayout.Label(statusContent, statusStyle);
            GUI.contentColor = Color.white;
            
            GUILayout.FlexibleSpace();
            
            if (_isRefreshing)
            {
                GUILayout.Label("Refreshing...", EditorStyles.toolbarButton);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        #endregion
        
        #region Save File Operations
        
        /// <summary>
        /// Refreshes the save file list asynchronously
        /// </summary>
        private async Task RefreshSaveFilesAsync()
        {
            if (_isRefreshing) return;
            
            _isRefreshing = true;
            SetStatus("Refreshing save files...", MessageType.Info);
            
            try
            {
                // Load directly from file system in edit mode
                _saveFiles = await LoadSaveFilesDirectly();
                
                SetStatus($"Loaded {_saveFiles.Length} save files", MessageType.Info);
            }
            catch (Exception ex)
            {
                SetStatus($"Error refreshing saves: {ex.Message}", MessageType.Error);
                Debug.LogError($"[SaveFileManager] Error refreshing save files: {ex}");
            }
            finally
            {
                _isRefreshing = false;
            }
        }
        
        /// <summary>
        /// Loads save files directly from the file system (for edit mode)
        /// </summary>
        private async Task<SaveFileInfo[]> LoadSaveFilesDirectly()
        {
            var saveDirectory = Application.persistentDataPath + "/Saves/";
            
            if (!Directory.Exists(saveDirectory))
            {
                return Array.Empty<SaveFileInfo>();
            }
            
            var saveFiles = Directory.GetFiles(saveDirectory, "*.gamesave");
            var saveInfos = new List<SaveFileInfo>();
            
            foreach (var filePath in saveFiles)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var jsonContent = await File.ReadAllTextAsync(filePath);
                    var session = JsonUtility.FromJson<GameSession>(jsonContent);
                    
                    if (session != null)
                    {
                        var saveInfo = new SaveFileInfo(fileName, session);
                        saveInfos.Add(saveInfo);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveFileManager] Failed to load save file {filePath}: {ex.Message}");
                }
            }
            
            // Sort by last save time (most recent first)
            return saveInfos.OrderByDescending(s => s.LastSaveTime).ToArray();
        }
        
        /// <summary>
        /// Loads the selected save file into the game (play mode only)
        /// </summary>
        private async void LoadSelectedSave(SaveFileInfo saveInfo)
        {
            //TODO Implement 
            // if (!Application.isPlaying)
            // {
            //     SetStatus("Cannot load save file - not in play mode", MessageType.Warning);
            //     return;
            // }
            //
            // try
            // {
            //     SetStatus($"Loading {saveInfo.FileName}...", MessageType.Info);
            //     
            //     var loadService = UnityEngine.Object.FindObjectOfType<LoadService>();
            //     if (loadService != null)
            //     {
            //         var success = await loadService.LoadGameAsync(saveInfo);
            //         if (success)
            //         {
            //             SetStatus($"Successfully loaded {saveInfo.FileName}", MessageType.Info);
            //         }
            //         else
            //         {
            //             SetStatus($"Failed to load {saveInfo.FileName}", MessageType.Error);
            //         }
            //     }
            //     else
            //     {
            //         SetStatus("LoadService not found", MessageType.Error);
            //     }
            // }
            // catch (Exception ex)
            // {
            //     SetStatus($"Error loading save: {ex.Message}", MessageType.Error);
            // }
        }
        
        /// <summary>
        /// Deletes the selected save file with confirmation
        /// </summary>
        private async void DeleteSelectedSave(SaveFileInfo saveInfo)
        {
            var confirmed = EditorUtility.DisplayDialog(
                "Delete Save File",
                $"Are you sure you want to delete '{saveInfo.FileName}'?\n\nThis action cannot be undone.",
                "Delete",
                "Cancel"
            );
            
            if (!confirmed) return;
            
            try
            {
                SetStatus($"Deleting {saveInfo.FileName}...", MessageType.Info);

                // Delete directly in edit mode
                var filePath = GetSaveFilePath(saveInfo.FileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    SetStatus($"Deleted {saveInfo.FileName}", MessageType.Info);
                }

                // Clear selection and refresh
                _selectedSaveIndex = -1;
                await RefreshSaveFilesAsync();
            }
            catch (Exception ex)
            {
                SetStatus($"Error deleting save: {ex.Message}", MessageType.Error);
            }
        }
        
        /// <summary>
        /// Opens the save file location in the system file explorer
        /// </summary>
        private void ShowSaveFileInExplorer(SaveFileInfo saveInfo)
        {
            var filePath = GetSaveFilePath(saveInfo.FileName);
            
            if (File.Exists(filePath))
            {
                EditorUtility.RevealInFinder(filePath);
            }
            else
            {
                SetStatus("Save file not found", MessageType.Error);
            }
        }
        
        /// <summary>
        /// Gets the full file path for a save file name
        /// </summary>
        private string GetSaveFilePath(string fileName)
        {
            return Application.persistentDataPath + "/Saves/" + fileName + ".gamesave";
        }
        
        #endregion
        
        #region Configuration Management
        
        /// <summary>
        /// Loads the display configuration ScriptableObject
        /// </summary>
        private void LoadConfiguration()
        {
            var configAssets = AssetDatabase.FindAssets("t:SaveFileDisplayConfig");
            if (configAssets.Length > 0)
            {
                var configPath = AssetDatabase.GUIDToAssetPath(configAssets[0]);
                _displayConfig = AssetDatabase.LoadAssetAtPath<SaveFileDisplayConfig>(configPath);
            }
        }
        
        /// <summary>
        /// Shows the configuration window for customizing the display
        /// </summary>
        private void ShowConfigurationWindow()
        {
            SaveFileDisplayConfigWindow.ShowWindow();
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Sets the status message and type for display in the status bar
        /// </summary>
        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            Repaint();
        }
        
        #endregion
    }
    
    /// <summary>
    /// Configuration ScriptableObject for customizing save file display
    /// Allows adding new fields without modifying the editor code
    /// </summary>
    [CreateAssetMenu(fileName = "SaveFileDisplayConfig", menuName = "Game Framework/Save File Display Config")]
    public class SaveFileDisplayConfig : ScriptableObject
    {
        [System.Serializable]
        public class FieldDisplayConfig
        {
            public string FieldName;
            public string DisplayName;
            public bool IsReadOnly = true;
            
            public FieldDisplayConfig(string fieldName, string displayName, bool isReadOnly = true)
            {
                FieldName = fieldName;
                DisplayName = displayName;
                IsReadOnly = isReadOnly;
            }
        }
        
        [Header("Field Display Configuration")]
        [SerializeField] public List<FieldDisplayConfig> DisplayFields = new List<FieldDisplayConfig>
        {
            new FieldDisplayConfig("FileName", "File Name"),
            new FieldDisplayConfig("PlayerName", "Player Name"),
            new FieldDisplayConfig("Difficulty", "Difficulty"),
            new FieldDisplayConfig("CurrentScene", "Current Scene"),
            new FieldDisplayConfig("PlayerLevel", "Player Level"),
            new FieldDisplayConfig("Score", "Score"),
            new FieldDisplayConfig("FormattedPlayTime", "Play Time"),
            new FieldDisplayConfig("FormattedDate", "Last Save"),
            new FieldDisplayConfig("IsAutoSave", "Auto Save")
        };
    }
    
    /// <summary>
    /// Configuration window for editing save file display settings
    /// </summary>
    public class SaveFileDisplayConfigWindow : EditorWindow
    {
        private SaveFileDisplayConfig _config;
        private Vector2 _scrollPosition;
        
        public static void ShowWindow()
        {
            var window = GetWindow<SaveFileDisplayConfigWindow>("Save File Display Config");
            window.minSize = new Vector2(400, 300);
            window.LoadOrCreateConfig();
            window.Show();
        }
        
        private void LoadOrCreateConfig()
        {
            var configAssets = AssetDatabase.FindAssets("t:SaveFileDisplayConfig");
            if (configAssets.Length > 0)
            {
                var configPath = AssetDatabase.GUIDToAssetPath(configAssets[0]);
                _config = AssetDatabase.LoadAssetAtPath<SaveFileDisplayConfig>(configPath);
            }
            else
            {
                // Create new config
                _config = CreateInstance<SaveFileDisplayConfig>();
                AssetDatabase.CreateAsset(_config, "Assets/SaveFileDisplayConfig.asset");
                AssetDatabase.SaveAssets();
            }
        }
        
        private void OnGUI()
        {
            if (_config == null)
            {
                EditorGUILayout.HelpBox("No configuration loaded", MessageType.Error);
                return;
            }
            
            EditorGUILayout.LabelField("Save File Display Configuration", EditorStyles.boldLabel);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            var serializedObject = new SerializedObject(_config);
            var displayFieldsProp = serializedObject.FindProperty("DisplayFields");
            
            EditorGUILayout.PropertyField(displayFieldsProp, true);
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add Field"))
            {
                _config.DisplayFields.Add(new SaveFileDisplayConfig.FieldDisplayConfig("", ""));
            }
            
            if (GUILayout.Button("Save"))
            {
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
