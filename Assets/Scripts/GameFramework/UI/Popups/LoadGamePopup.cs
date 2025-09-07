using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using UCTools_Utilities.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Pure UI popup for selecting and loading save files
    /// All business logic is handled by LoadService - this only manages UI state and user interactions
    /// Supports single-click selection, double-click loading, and delete functionality
    /// </summary>
    public class LoadGamePopup : UIPopup
    {
        #region UI Elements

        private ListView _loadGameList;
        private Button _loadGameButton;
        private Button _deleteButton;
        private Button _closeButton;
        private Label _loadingLabel;

        #endregion

        #region Services

        private readonly IUIService _uiService;
        private readonly ILoadService _loadService;
        private readonly ISaveService _saveService;
        private readonly IEventSystem _eventSystem;

        #endregion

        #region UI State

        private SaveFileInfo[] _saveFiles = new SaveFileInfo[0];
        private SaveFileInfo _selectedSaveFile;
        private bool _isLoadingData;
        private bool _isDeletingFile;

        #endregion

        #region Constants

        private const string NO_SAVE_FILES_MESSAGE = "No save files found";
        private const string LOADING_SAVE_FILES_MESSAGE = "Loading save files...";
        private const string DELETING_SAVE_FILE_MESSAGE = "Deleting save file...";
        private const string ERROR_LOADING_MESSAGE = "Error loading save files";
        private const string ERROR_DELETING_MESSAGE = "Error deleting save file";

        #endregion

        public LoadGamePopup(VisualElement rootElement) : base(rootElement)
        {
            _uiService = GameManager.GetService<IUIService>() ?? throw new ArgumentNullException(nameof(_uiService));
            _loadService = GameManager.GetService<ILoadService>() ?? throw new ArgumentNullException(nameof(_loadService));
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
            SetupDoubleClickLoading();
        }

        private void CacheUIElements()
        {
            _loadGameList = RootElement?.Q<ListView>("list_LoadFiles");
            _loadGameButton = RootElement?.Q<Button>("btn_Load");
            _deleteButton = RootElement?.Q<Button>("btn_DeleteSelected"); // Your delete button
            _closeButton = RootElement?.Q<Button>("btn_Close");
            _loadingLabel = RootElement?.Q<Label>("lbl_Loading");
        }

        private void ConfigureInitialStates()
        {
            _loadGameButton?.SetEnabled(false);
            _deleteButton?.SetEnabled(false);
            SetStatusMessage("", false);
        }

        private void SetupListView()
        {
            if (_loadGameList == null) return;

            _loadGameList.bindItem = BindListItem;
            _loadGameList.selectionChanged += OnSelectionChanged;
            _loadGameList.selectionType = SelectionType.Single;
        }

        /// <summary>
        /// Sets up double-click functionality on the ListView
        /// </summary>
        private void SetupDoubleClickLoading()
        {
            if (_loadGameList == null) return;

            _loadGameList.RegisterCallback<MouseDownEvent>(OnListViewMouseDown);
        }

        #endregion

        #region ListView Implementation

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
                   index < _saveFiles.Length &&
                   _saveFiles[index] != null;
        }

        private void BindSaveFileData(VisualElement element, SaveFileInfo saveFileInfo)
        {
            var bindings = new Dictionary<string, string>
            {
                ["lbl_PlayerName"] = saveFileInfo.playerName ?? "Unknown Player",
                ["lbl_Difficulty"] = saveFileInfo.difficulty ?? "Normal",
                ["lbl_Scene"] = saveFileInfo.currentScene ?? "Unknown Scene",
                ["lbl_PlayTime"] = $"Play Time: {saveFileInfo.formattedPlayTime ?? "00:00:00"}",
                ["lbl_SaveDate"] = $"Saved: {saveFileInfo.formattedDate ?? "Unknown Date"}",
                ["lbl_PlayerLevel"] = $"Level {saveFileInfo.playerLevel}",
                ["lbl_Score"] = $"Score: {saveFileInfo.score:N0}"
            };

            foreach (var (elementName, text) in bindings)
            {
                var label = element.Q<Label>(elementName);
                if (label != null)
                {
                    label.text = text;
                }
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
            await RefreshSaveFilesList();
        }

        protected override void OnHide()
        {
            UnregisterEventHandlers();
        }

        private void RegisterEventHandlers()
        {
            _loadGameButton?.RegisterCallback<ClickEvent>(OnLoadButtonClicked);
            _deleteButton?.RegisterCallback<ClickEvent>(OnDeleteButtonClicked);
            _closeButton?.RegisterCallback<ClickEvent>(OnCloseButtonClicked);
        }

        private void UnregisterEventHandlers()
        {
            _loadGameButton?.UnregisterCallback<ClickEvent>(OnLoadButtonClicked);
            _deleteButton?.UnregisterCallback<ClickEvent>(OnDeleteButtonClicked);
            _closeButton?.UnregisterCallback<ClickEvent>(OnCloseButtonClicked);

            // Unregister double-click handler
            _loadGameList?.UnregisterCallback<MouseDownEvent>(OnListViewMouseDown);
        }

        private void OnSelectionChanged(IEnumerable<object> selectedItems)
        {
            var selectedSaveFile = selectedItems.FirstOrDefault() as SaveFileInfo;
            UpdateSelection(selectedSaveFile);
        }

        private void OnLoadButtonClicked(ClickEvent evt)
        {
            Debug.Log("[LoadGamePopup] Load button clicked");

            if (!CanLoadSelectedFile())
            {
                Debug.LogWarning(
                    $"[LoadGamePopup] Cannot load - Selected: {_selectedSaveFile != null}, Loading: {_isLoadingData}");
                return;
            }

            RequestLoadSelectedFile();
        }

        private async void OnDeleteButtonClicked(ClickEvent evt)
        {
            Debug.Log("[LoadGamePopup] Delete button clicked");

            if (!CanDeleteSelectedFile())
            {
                Debug.LogWarning(
                    $"[LoadGamePopup] Cannot delete - Selected: {_selectedSaveFile != null}, Deleting: {_isDeletingFile}");
                return;
            }

            await DeleteSelectedFile();
        }

        private async void OnCloseButtonClicked(ClickEvent evt)
        {
            await ClosePopup();
        }

        /// <summary>
        /// Handles double-click on ListView - loads selected save file immediately
        /// </summary>
        private void OnListViewMouseDown(MouseDownEvent evt)
        {
            if (evt.clickCount == 2 && evt.button == 0) // Left mouse button double-click
            {
                Debug.Log("[LoadGamePopup] Double-click detected on ListView");

                if (_selectedSaveFile != null && CanLoadSelectedFile())
                {
                    Debug.Log($"[LoadGamePopup] Double-click loading save file: {_selectedSaveFile.fileName}");
                    RequestLoadSelectedFile();
                    evt.StopPropagation();
                }
                else
                {
                    Debug.LogWarning(
                        $"[LoadGamePopup] Double-click ignored - Selected: {_selectedSaveFile != null}, CanLoad: {CanLoadSelectedFile()}");
                }
            }
        }

        #endregion

        #region UI State Management

        private void ResetUIState()
        {
            _selectedSaveFile = null;
            _saveFiles = new SaveFileInfo[0];
            _isLoadingData = false;
            _isDeletingFile = false;
            UpdateButtonStates();
            _loadGameList?.ClearSelection();
        }

        private void UpdateSelection(SaveFileInfo selectedSaveFile)
        {
            _selectedSaveFile = selectedSaveFile;
            UpdateButtonStates();

            Debug.Log($"[LoadGamePopup] Selected save file: {selectedSaveFile?.fileName ?? "None"}");
        }

        private void UpdateButtonStates()
        {
            bool canLoad = CanLoadSelectedFile();
            bool canDelete = CanDeleteSelectedFile();

            _loadGameButton?.SetEnabled(canLoad);
            _deleteButton?.SetEnabled(canDelete);
        }

        private bool CanLoadSelectedFile()
        {
            return _selectedSaveFile != null &&
                   !_isLoadingData &&
                   !_isDeletingFile &&
                   !_loadService.IsLoading;
        }

        private bool CanDeleteSelectedFile()
        {
            return _selectedSaveFile != null &&
                   !_isLoadingData &&
                   !_isDeletingFile &&
                   !_loadService.IsLoading;
        }

        private void SetDataLoadingState(bool isLoading)
        {
            _isLoadingData = isLoading;
            UpdateButtonStates();
        }

        private void SetDeletingState(bool isDeleting)
        {
            _isDeletingFile = isDeleting;
            UpdateButtonStates();
        }

        private void SetStatusMessage(string message, bool show)
        {
            if (_loadingLabel == null) return;

            _loadingLabel.text = message;
            _loadingLabel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        #endregion

        #region Data Loading (Delegated to LoadService)

        /// <summary>
        /// Refreshes the save files list using LoadService
        /// </summary>
        private async Task RefreshSaveFilesList()
        {
            try
            {
                SetDataLoadingState(true);
                SetStatusMessage(LOADING_SAVE_FILES_MESSAGE, true);

                // Delegate data loading to LoadService
                _saveFiles = await _loadService.GetLoadableSaveFilesAsync();

                RefreshListView();
                HandleEmptyListState();

                Debug.Log($"[LoadGamePopup] Loaded {_saveFiles.Length} save files for display");
            }
            catch (Exception ex)
            {
                HandleDataLoadError(ex);
            }
            finally
            {
                SetDataLoadingState(false);
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
            if (_saveFiles.Length == 0)
            {
                SetStatusMessage(NO_SAVE_FILES_MESSAGE, true);
            }
            else
            {
                SetStatusMessage("", false);
            }
        }

        private void HandleDataLoadError(Exception ex)
        {
            Debug.LogError($"[LoadGamePopup] Error loading save files: {ex}");
            SetStatusMessage(ERROR_LOADING_MESSAGE, true);
            _saveFiles = new SaveFileInfo[0];
            RefreshListView();
        }

        #endregion

        #region Delete Functionality

        /// <summary>
        /// Deletes the selected save file and refreshes the UI
        /// </summary>
        private async Task DeleteSelectedFile()
        {
            if (_selectedSaveFile == null) return;

            var saveFileToDelete = _selectedSaveFile;
            Debug.Log($"[LoadGamePopup] Deleting save file: {saveFileToDelete.fileName}");

            try
            {
                SetDeletingState(true);
                SetStatusMessage(DELETING_SAVE_FILE_MESSAGE, true);

                // Use SaveService to delete the file
                bool deleteSuccess = await _saveService.DeleteSaveFileByInfoAsync(saveFileToDelete);

                if (deleteSuccess)
                {
                    Debug.Log($"[LoadGamePopup] Successfully deleted save file: {saveFileToDelete.fileName}");

                    // Clear selection since the file no longer exists
                    _selectedSaveFile = null;
                    _loadGameList?.ClearSelection();

                    // Refresh the save files list
                    await RefreshSaveFilesList();
                }
                else
                {
                    Debug.LogError($"[LoadGamePopup] Failed to delete save file: {saveFileToDelete.fileName}");
                    SetStatusMessage(ERROR_DELETING_MESSAGE, true);

                    // Clear error message after a delay
                    await Task.Delay(3000);
                    if (_saveFiles.Length > 0)
                    {
                        SetStatusMessage("", false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoadGamePopup] Error deleting save file: {ex}");
                SetStatusMessage(ERROR_DELETING_MESSAGE, true);

                // Clear error message after a delay
                await Task.Delay(3000);
                if (_saveFiles.Length > 0)
                {
                    SetStatusMessage("", false);
                }
            }
            finally
            {
                SetDeletingState(false);
            }
        }

        #endregion

        #region Load Request (Simplified - Just Publishes Event)

        /// <summary>
        /// Publishes event to request loading of selected save file
        /// LoadService handles all the loading logic
        /// </summary>
        private void RequestLoadSelectedFile()
        {
            if (_selectedSaveFile == null)
            {
                Debug.LogWarning("[LoadGamePopup] No save file selected");
                return;
            }

            Debug.Log($"[LoadGamePopup] Requesting load of save file: {_selectedSaveFile.fileName}");
            Debug.Log(
                $"[LoadGamePopup] Save file info - Player: {_selectedSaveFile.playerName}, Scene: {_selectedSaveFile.currentScene}");

            // Publish the event - LoadService is subscribed and will handle everything
            var loadEvent = new LoadSaveFileEvent(_selectedSaveFile);
            _eventSystem.Publish(loadEvent);

            Debug.Log($"[LoadGamePopup] Published LoadSaveFileEvent for {_selectedSaveFile.fileName}");

            // Close popup after requesting load
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


        private async Task ClosePopup()
        {
            if (_uiService != null)
            {
                await _uiService.HidePopupAsync<LoadGamePopup>();
            }
        }
    }
    
}
