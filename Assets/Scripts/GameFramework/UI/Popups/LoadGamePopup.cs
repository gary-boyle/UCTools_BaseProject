using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.UI.Popups;
using UCTools_Utilities.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Popup for selecting and loading save files. Handles only UI concerns.
    /// </summary>
    public class LoadGamePopup : UIPopup
    {
        #region UI Elements
        private ListView _loadGameList;
        private Button _loadGameButton;
        private Button _closeButton;
        private Label _loadingLabel;
        #endregion

        #region Services
        private readonly IUIService _uiService;
        private readonly ISaveService _saveService;
        private readonly IEventSystem _eventSystem;
        #endregion

        #region State
        private List<SaveFileInfo> _saveFiles = new();
        private SaveFileInfo _selectedSaveFile;
        private bool _isLoadingData;
        #endregion

        #region Constants
        private const string NO_SAVE_FILES_MESSAGE = "No save files found";
        private const string LOADING_SAVE_FILES_MESSAGE = "Loading save files...";
        private const string ERROR_LOADING_MESSAGE = "Error loading save files";
        #endregion

        public LoadGamePopup(VisualElement rootElement) : base(rootElement)
        {
            _uiService = GameManager.GetService<IUIService>() ?? throw new ArgumentNullException(nameof(_uiService));
            _saveService = GameManager.GetService<ISaveService>() ?? throw new ArgumentNullException(nameof(_saveService));
            _eventSystem = GameManager.GetService<IEventSystem>() ?? throw new ArgumentNullException(nameof(_eventSystem));
            
            InitializeUI();
            UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.ThrowExceptions);
        }

        #region Initialization

        private void InitializeUI()
        {
            CacheUIElements();
            ConfigureInitialStates();
            SetupListView();
        }

        private void CacheUIElements()
        {
            _loadGameList = RootElement?.Q<ListView>("list_LoadFiles");
            _loadGameButton = RootElement?.Q<Button>("btn_Load");
            _closeButton = RootElement?.Q<Button>("btn_Close");
            _loadingLabel = RootElement?.Q<Label>("lbl_Loading");
        }

        private void ConfigureInitialStates()
        {
            _loadGameButton?.SetEnabled(false);
            SetStatusMessage("", false);
        }

        private void SetupListView()
        {
            if (_loadGameList == null) return;

            //_loadGameList.makeItem = CreateListItem;
            _loadGameList.bindItem = BindListItem;
            _loadGameList.selectionChanged += OnSelectionChanged;
            _loadGameList.selectionType = SelectionType.Single;
        }
        #endregion

        #region ListView Implementation
        // private VisualElement CreateListItem()
        // {
        //     // Return a new instance of your save file item template
        //     // This should match your Widget_SaveFileInfo structure
        //     return new VisualElement();
        // }

        private void BindListItem(VisualElement element, int index)
        {
            if (!IsValidBinding(element, index)) return;

            var saveFileInfo = _saveFiles[index];
            BindSaveFileData(element, saveFileInfo);
            UpdateSelectionVisuals(element, saveFileInfo);
        }

        private bool IsValidBinding(VisualElement element, int index)
        {
            return element != null && 
                   _saveFiles != null && 
                   index >= 0 && 
                   index < _saveFiles.Count &&
                   _saveFiles[index] != null;
        }

        private void BindSaveFileData(VisualElement element, SaveFileInfo saveFileInfo)
        {
            var bindings = new Dictionary<string, string>
            {
                ["lbl_PlayerName"] = saveFileInfo.playerName ?? "",
                ["lbl_Difficulty"] = saveFileInfo.difficulty ?? "",
                ["lbl_Scene"] = saveFileInfo.currentScene ?? "",
                ["lbl_PlayTime"] = $"Play Time: {saveFileInfo.formattedPlayTime ?? "00:00:00"}",
                ["lbl_SaveDate"] = $"Saved: {saveFileInfo.formattedDate ?? "Unknown"}"
            };

            foreach (var (elementName, text) in bindings)
            {
                element.Q<Label>(elementName)?.SetText(text);
            }
        }

        private void UpdateSelectionVisuals(VisualElement element, SaveFileInfo saveFileInfo)
        {
            element.EnableInClassList("selected", _selectedSaveFile == saveFileInfo);
        }
        #endregion

        #region Event Handlers
        protected override async void OnShow()
        {
            RegisterEventHandlers();
            ResetUIState();
            await LoadSaveFilesList();
        }

        protected override void OnHide()
        {
            UnregisterEventHandlers();
        }

        private void RegisterEventHandlers()
        {
            _loadGameButton?.RegisterCallback<ClickEvent>(OnLoadButtonClicked);
            _closeButton?.RegisterCallback<ClickEvent>(OnCloseButtonClicked);
        }

        private void UnregisterEventHandlers()
        {
            _loadGameButton?.UnregisterCallback<ClickEvent>(OnLoadButtonClicked);
            _closeButton?.UnregisterCallback<ClickEvent>(OnCloseButtonClicked);
        }

        private void OnSelectionChanged(IEnumerable<object> selectedItems)
        {
            var selectedSaveFile = selectedItems.FirstOrDefault() as SaveFileInfo;
            UpdateSelection(selectedSaveFile);
        }

        private void OnLoadButtonClicked(ClickEvent evt)
        {
            if (_selectedSaveFile == null || _isLoadingData)
            {
                Debug.LogWarning("Cannot load: No save file selected or data is loading");
                return;
            }

            LoadSelectedSaveFile();
        }

        private async void OnCloseButtonClicked(ClickEvent evt)
        {
            await ClosePopup();
        }
        #endregion

        #region UI State Management
        private void ResetUIState()
        {
            _selectedSaveFile = null;
            _isLoadingData = false;
            UpdateLoadButtonState();
            _loadGameList?.ClearSelection();
        }

        private void UpdateSelection(SaveFileInfo selectedSaveFile)
        {
            _selectedSaveFile = selectedSaveFile;
            UpdateLoadButtonState();
            
            if (selectedSaveFile != null)
            {
                Debug.Log($"[LoadGamePopup] Selected save file: {selectedSaveFile.fileName}");
            }
        }

        private void UpdateLoadButtonState()
        {
            bool canLoad = _selectedSaveFile != null && !_isLoadingData;
            _loadGameButton?.SetEnabled(canLoad);
        }

        private void SetDataLoadingState(bool isLoading)
        {
            _isLoadingData = isLoading;
            UpdateLoadButtonState();
        }

        private void SetStatusMessage(string message, bool show)
        {
            if (_loadingLabel == null) return;

            _loadingLabel.text = message;
            _loadingLabel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
        #endregion

        #region Save Files Data Loading
        private async Task LoadSaveFilesList()
        {
            try
            {
                SetDataLoadingState(true);
                SetStatusMessage(LOADING_SAVE_FILES_MESSAGE, true);

                await LoadSaveFilesData();
                RefreshListView();
                HandleEmptyListState();
            }
            catch (Exception ex)
            {
                HandleSaveFilesLoadError(ex);
            }
            finally
            {
                SetDataLoadingState(false);
            }
        }

        private async Task LoadSaveFilesData()
        {
            _saveFiles.Clear();
            var saveFileNames = await _saveService.GetSaveFilesAsync();

            // Load save file info in parallel for better performance
            var loadTasks = saveFileNames.Select(LoadSingleSaveFileInfo);
            var results = await Task.WhenAll(loadTasks);

            _saveFiles = results
                .Where(info => info != null)
                .OrderByDescending(info => info.lastSaveTime)
                .ToList();

            Debug.Log($"[LoadGamePopup] Loaded {_saveFiles.Count} save files");
        }

        private async Task<SaveFileInfo> LoadSingleSaveFileInfo(string fileName)
        {
            try
            {
                var gameSession = await _saveService.LoadGameSessionAsync(fileName);
                return gameSession != null ? new SaveFileInfo(fileName, gameSession) : null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LoadGamePopup] Failed to load save file '{fileName}': {ex.Message}");
                return null;
            }
        }

        private void RefreshListView()
        {
            if (_loadGameList == null) return;

            _loadGameList.itemsSource = _saveFiles;
            _loadGameList.RefreshItems();
        }

        private void HandleEmptyListState()
        {
            if (_saveFiles.Count == 0)
            {
                SetStatusMessage(NO_SAVE_FILES_MESSAGE, true);
            }
            else
            {
                SetStatusMessage("", false);
            }
        }

        private void HandleSaveFilesLoadError(Exception ex)
        {
            Debug.LogError($"[LoadGamePopup] Error loading save files: {ex}");
            SetStatusMessage(ERROR_LOADING_MESSAGE, true);
            _saveFiles.Clear();
            RefreshListView();
        }
        #endregion

        #region Game Loading Request
        private void LoadSelectedSaveFile()
        {
            if (_selectedSaveFile == null) return;

            Debug.Log($"[LoadGamePopup] Requesting load of save file: {_selectedSaveFile.fileName}");
            
            // Fire event to request save file loading - other systems will handle the actual loading
            var loadEvent = new LoadSaveFileEvent(_selectedSaveFile);
            _eventSystem.Publish(loadEvent);

            // Close the popup after firing the event
            _ = ClosePopupAsync();
        }

        private async Task ClosePopupAsync()
        {
            try
            {
                await ClosePopup();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoadGamePopup] Error closing popup: {ex}");
            }
        }
        #endregion

        #region Utility Methods
        private async Task ClosePopup()
        {
            if (_uiService != null)
            {
                await _uiService.HidePopupAsync<LoadGamePopup>();
            }
        }

        /// <summary>
        /// Public method to refresh the save files list (can be called externally if needed)
        /// </summary>
        public async Task RefreshSaveFiles()
        {
            await LoadSaveFilesList();
        }

        /// <summary>
        /// Get the currently selected save file info (for external access if needed)
        /// </summary>
        public SaveFileInfo GetSelectedSaveFile() => _selectedSaveFile;

        /// <summary>
        /// Get all loaded save files (for external access if needed)
        /// </summary>
        public IReadOnlyList<SaveFileInfo> GetSaveFiles() => _saveFiles.AsReadOnly();
        #endregion
    }

    #region Extensions
    public static class LabelExtensions
    {
        public static void SetText(this Label label, string text)
        {
            if (label != null) label.text = text;
        }
    }
    #endregion
}
