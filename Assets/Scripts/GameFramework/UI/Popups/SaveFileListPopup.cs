using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using UCTools_Utilities.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Base class for popups that display and manage save file lists
    /// Provides common functionality for save file display, selection, deletion, and double-click handling
    /// Derived classes implement specific load/save behaviors
    /// 
    /// INTENT: Centralized save file list management with extensible interaction patterns
    /// DESIGN: Template method pattern with double-click infrastructure and proper lifecycle management
    /// PROS: DRY principle, centralized ListView logic, reusable double-click handling
    /// CONS: More complex base class, requires careful abstract method implementation
    /// </summary>
    public abstract class SaveFileListPopup : UIPopup
    {
        #region Protected UI Elements
        
        protected ListView _saveFileList;
        protected Button _deleteButton;
        protected Button _closeButton;
        protected Label _statusLabel;
        
        #endregion

        #region Protected Services
        
        protected readonly IUIService _uiService;
        protected readonly ILoadService _loadService;
        protected readonly ISaveService _saveService;
        protected readonly IEventSystem _eventSystem;
        
        #endregion

        #region Protected State
        
        protected SaveFileInfo[] _saveFiles = new SaveFileInfo[0];
        protected SaveFileInfo _selectedSaveFile;
        protected bool _isLoadingData;
        protected bool _isDeletingFile;
        
        #endregion

        #region Double-Click Management
        
        private float _lastClickTime = 0f;
        private const float DOUBLE_CLICK_THRESHOLD = 0.5f;
        private bool _listViewEventHandlersRegistered = false;
        
        #endregion

        #region Constants
        
        protected const string NO_SAVE_FILES_MESSAGE = "No save files found";
        protected const string LOADING_SAVE_FILES_MESSAGE = "Loading save files...";
        protected const string DELETING_SAVE_FILE_MESSAGE = "Deleting save file...";
        protected const string ERROR_LOADING_MESSAGE = "Error loading save files";
        protected const string ERROR_DELETING_MESSAGE = "Error deleting save file";
        
        #endregion

        protected SaveFileListPopup(VisualElement rootElement) : base(rootElement)
        {
            _uiService = GameManager.GetService<IUIService>() ?? throw new ArgumentNullException(nameof(_uiService));
            _loadService = GameManager.GetService<ILoadService>() ?? throw new ArgumentNullException(nameof(_loadService));
            _saveService = GameManager.GetService<ISaveService>() ?? throw new ArgumentNullException(nameof(_saveService));
            _eventSystem = GameManager.GetService<IEventSystem>() ?? throw new ArgumentNullException(nameof(_eventSystem));
        }

        #region Abstract Methods - Implemented by derived classes
        
        /// <summary>
        /// Cache UI elements specific to the derived popup
        /// </summary>
        protected abstract void CacheSpecificUIElements();
        
        /// <summary>
        /// Setup derived class specific functionality
        /// </summary>
        protected abstract void SetupSpecificFunctionality();
        
        /// <summary>
        /// Register event handlers specific to derived class
        /// </summary>
        protected abstract void RegisterSpecificEventHandlers();
        
        /// <summary>
        /// Unregister event handlers specific to derived class
        /// </summary>
        protected abstract void UnregisterSpecificEventHandlers();
        
        /// <summary>
        /// Update button states based on current selection and derived class needs
        /// </summary>
        protected abstract void UpdateSpecificButtonStates();
        
        /// <summary>
        /// Handle double-click action - implemented by derived classes
        /// </summary>
        /// <param name="selectedSaveFile">The save file that was double-clicked</param>
        protected abstract Task OnDoubleClickAction(SaveFileInfo selectedSaveFile);
        
        /// <summary>
        /// Check if double-click action can be performed - implemented by derived classes
        /// </summary>
        /// <returns>True if double-click action is allowed</returns>
        protected abstract bool CanPerformDoubleClickAction();
        
        #endregion

        #region Popup Lifecycle Override
        
        /// <summary>
        /// Override Show to ensure proper setup each time popup is displayed
        /// </summary>
        public override void Show()
        {
            Debug.Log($"[{GetType().Name}] Show() called - setting up for display");
            
            // Clean up any existing handlers first
            CleanupListViewEventHandlers();
            
            // Call base class show
            base.Show();
        }

        /// <summary>
        /// Override Hide to ensure proper cleanup each time popup is hidden
        /// </summary>
        public override void Hide()
        {
            Debug.Log($"[{GetType().Name}] Hide() called - cleaning up");
            
            // Clean up ListView handlers
            CleanupListViewEventHandlers();
            
            // Call base class hide
            base.Hide();
        }
        
        #endregion

        #region Initialization
        
        protected void InitializeBaseUI()
        {
            CacheCommonUIElements();
            CacheSpecificUIElements();
            ConfigureInitialStates();
            SetupListView();
            SetupSpecificFunctionality();
            
            // Validation should be done by derived class after all elements are cached
        }

        private void CacheCommonUIElements()
        {
            _saveFileList = RootElement?.Q<ListView>("list_SaveFiles");
            _deleteButton = RootElement?.Q<Button>("btn_DeleteSelected");
            _closeButton = RootElement?.Q<Button>("btn_Close");
            _statusLabel = RootElement?.Q<Label>("lbl_Status");
            
            Debug.Log($"[{GetType().Name}] Common UI elements cached - SaveFileList: {_saveFileList != null}");
        }

        private void ConfigureInitialStates()
        {
            _deleteButton?.SetEnabled(false);
            SetStatusMessage("", false);
        }

        private void SetupListView()
        {
            if (_saveFileList == null) return;

            _saveFileList.bindItem = BindListItem;
            _saveFileList.selectionType = SelectionType.Single;
            
            Debug.Log($"[{GetType().Name}] ListView basic setup completed");
        }
        
        #endregion

        #region ListView Event Management
        
        /// <summary>
        /// Sets up ListView event handlers including double-click functionality
        /// </summary>
        private void SetupListViewEventHandlers()
        {
            if (_saveFileList == null || _listViewEventHandlersRegistered) return;

            try
            {
                // Clear any existing selection
                _saveFileList.ClearSelection();
                
                // Register selection change callback
                _saveFileList.selectionChanged += OnSelectionChanged;
                
                // Register pointer events for double-click detection
                _saveFileList.RegisterCallback<PointerDownEvent>(OnListViewPointerDown, TrickleDown.TrickleDown);
                _saveFileList.RegisterCallback<MouseDownEvent>(OnListViewMouseDown, TrickleDown.NoTrickleDown);
                
                _listViewEventHandlersRegistered = true;
                Debug.Log($"[{GetType().Name}] ListView event handlers registered");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error setting up ListView event handlers: {ex}");
            }
        }

        /// <summary>
        /// Cleans up ListView event handlers
        /// </summary>
        private void CleanupListViewEventHandlers()
        {
            if (_saveFileList == null || !_listViewEventHandlersRegistered) return;

            try
            {
                _saveFileList.selectionChanged -= OnSelectionChanged;
                _saveFileList.UnregisterCallback<PointerDownEvent>(OnListViewPointerDown);
                _saveFileList.UnregisterCallback<MouseDownEvent>(OnListViewMouseDown);
                
                _listViewEventHandlersRegistered = false;
                Debug.Log($"[{GetType().Name}] ListView event handlers cleaned up");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error cleaning up ListView event handlers: {ex}");
            }
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
    
            // Handle autosave indicator visibility
            var autoSaveIndicator = element.Q<Label>("lbl_AutoSaveIndicator");
            if (autoSaveIndicator != null)
            {
                autoSaveIndicator.style.display = saveFileInfo.isAutoSave ? DisplayStyle.Flex : DisplayStyle.None;
                autoSaveIndicator.EnableInClassList("autosave-active", saveFileInfo.isAutoSave);
            }
    
            // Add autosave class to the entire container for additional styling
            element.EnableInClassList("is-autosave", saveFileInfo.isAutoSave);
        }

        private void UpdateSelectionVisuals(VisualElement element, SaveFileInfo saveFileInfo)
        {
            element.EnableInClassList("selected", _selectedSaveFile == saveFileInfo);
        }
        
        #endregion

        #region Event Handlers
        
        protected override async void OnShow()
        {
            RegisterCommonEventHandlers();
            RegisterSpecificEventHandlers();
            SetupListViewEventHandlers(); // Set up ListView handlers when showing
            ResetUIState();
            await RefreshSaveFilesList();
        }

        protected override void OnHide()
        {
            UnregisterCommonEventHandlers();
            UnregisterSpecificEventHandlers();
            CleanupListViewEventHandlers(); // Clean up ListView handlers when hiding
        }

        private void RegisterCommonEventHandlers()
        {
            _deleteButton?.RegisterCallback<ClickEvent>(OnDeleteButtonClicked);
            _closeButton?.RegisterCallback<ClickEvent>(OnCloseButtonClicked);
        }

        private void UnregisterCommonEventHandlers()
        {
            _deleteButton?.UnregisterCallback<ClickEvent>(OnDeleteButtonClicked);
            _closeButton?.UnregisterCallback<ClickEvent>(OnCloseButtonClicked);
        }

        private void OnSelectionChanged(IEnumerable<object> selectedItems)
        {
            if (!IsVisible) return;

            try
            {
                var selectedSaveFile = selectedItems.FirstOrDefault() as SaveFileInfo;
                UpdateSelection(selectedSaveFile);
                Debug.Log($"[{GetType().Name}] Selection changed to: {selectedSaveFile?.fileName ?? "None"}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error handling selection change: {ex}");
            }
        }

        /// <summary>
        /// Handles pointer down events for reliable timing
        /// </summary>
        private void OnListViewPointerDown(PointerDownEvent evt)
        {
            if (!IsVisible) return;
            
            _lastClickTime = Time.unscaledTime;
            Debug.Log($"[{GetType().Name}] Pointer down at {_lastClickTime}");
        }

        /// <summary>
        /// Handles mouse down events for double-click detection
        /// </summary>
        private void OnListViewMouseDown(MouseDownEvent evt)
        {
            if (!IsVisible || evt.button != 0) return;
            
            if (evt.clickCount == 2)
            {
                float timeSinceLastClick = Time.unscaledTime - _lastClickTime;
                
                Debug.Log($"[{GetType().Name}] Double-click detected, time since last: {timeSinceLastClick}");
                
                if (timeSinceLastClick <= DOUBLE_CLICK_THRESHOLD && 
                    _selectedSaveFile != null && 
                    CanPerformDoubleClickAction())
                {
                    Debug.Log($"[{GetType().Name}] Performing double-click action on: {_selectedSaveFile.fileName}");
                    
                    // Delegate to derived class implementation
                    _ = OnDoubleClickAction(_selectedSaveFile);
                    
                    evt.StopPropagation();
                    evt.PreventDefault();
                }
                else
                {
                    Debug.LogWarning($"[{GetType().Name}] Double-click failed - Time: {timeSinceLastClick}, Selected: {_selectedSaveFile != null}, CanPerform: {CanPerformDoubleClickAction()}");
                }
            }
        }

        private async void OnDeleteButtonClicked(ClickEvent evt)
        {
            Debug.Log($"[{GetType().Name}] Delete button clicked");

            if (!CanDeleteSelectedFile())
            {
                Debug.LogWarning($"[{GetType().Name}] Cannot delete - Selected: {_selectedSaveFile != null}, Deleting: {_isDeletingFile}");
                return;
            }

            await DeleteSelectedFile();
        }

        private async void OnCloseButtonClicked(ClickEvent evt)
        {
            await ClosePopup();
        }
        
        #endregion

        #region UI State Management
        
        protected virtual void ResetUIState()
        {
            _selectedSaveFile = null;
            _saveFiles = new SaveFileInfo[0];
            _isLoadingData = false;
            _isDeletingFile = false;
            UpdateButtonStates();
            _saveFileList?.ClearSelection();
        }

        protected virtual void UpdateSelection(SaveFileInfo selectedSaveFile)
        {
            _selectedSaveFile = selectedSaveFile;
            UpdateButtonStates();

            Debug.Log($"[{GetType().Name}] Selected save file: {selectedSaveFile?.fileName ?? "None"}");
        }

        protected virtual void UpdateButtonStates()
        {
            bool canDelete = CanDeleteSelectedFile();
            _deleteButton?.SetEnabled(canDelete);
            
            // Let derived classes update their specific buttons
            UpdateSpecificButtonStates();
        }

        protected virtual bool CanDeleteSelectedFile()
        {
            return _selectedSaveFile != null &&
                   !_isLoadingData &&
                   !_isDeletingFile &&
                   !_loadService.IsLoading;
        }

        protected void SetDataLoadingState(bool isLoading)
        {
            _isLoadingData = isLoading;
            UpdateButtonStates();
        }

        protected void SetDeletingState(bool isDeleting)
        {
            _isDeletingFile = isDeleting;
            UpdateButtonStates();
        }

        protected void SetStatusMessage(string message, bool show)
        {
            if (_statusLabel == null) return;

            _statusLabel.text = message;
            _statusLabel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        #endregion

        #region Data Loading
        
        /// <summary>
        /// Refreshes the save files list using LoadService
        /// </summary>
        protected async Task RefreshSaveFilesList()
        {
            try
            {
                SetDataLoadingState(true);
                SetStatusMessage(LOADING_SAVE_FILES_MESSAGE, true);

                // Delegate data loading to LoadService
                _saveFiles = await _loadService.GetLoadableSaveFilesAsync();

                RefreshListView();
                HandleEmptyListState();

                Debug.Log($"[{GetType().Name}] Loaded {_saveFiles.Length} save files for display");
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
            if (_saveFileList == null) return;

            _saveFileList.itemsSource = _saveFiles;
            _saveFileList.RefreshItems();
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
            Debug.LogError($"[{GetType().Name}] Error loading save files: {ex}");
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
            Debug.Log($"[{GetType().Name}] Deleting save file: {saveFileToDelete.fileName}");

            try
            {
                SetDeletingState(true);
                SetStatusMessage(DELETING_SAVE_FILE_MESSAGE, true);

                // Use SaveService to delete the file
                bool deleteSuccess = await _saveService.DeleteSaveFileByInfoAsync(saveFileToDelete);

                if (deleteSuccess)
                {
                    Debug.Log($"[{GetType().Name}] Successfully deleted save file: {saveFileToDelete.fileName}");

                    // Clear selection since the file no longer exists
                    _selectedSaveFile = null;
                    _saveFileList?.ClearSelection();

                    // Refresh the save files list
                    await RefreshSaveFilesList();
                }
                else
                {
                    Debug.LogError($"[{GetType().Name}] Failed to delete save file: {saveFileToDelete.fileName}");
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
                Debug.LogError($"[{GetType().Name}] Error deleting save file: {ex}");
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

        #region Utility Methods

        /// <summary>
        /// Abstract method for closing the popup - derived classes implement with their specific type
        /// </summary>
        protected abstract Task ClosePopup();

        #endregion
    }
}
