using System;
using System.Threading.Tasks;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events;
using UCTools_Utilities.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Save game popup with enhanced progress indication using ProgressBar and proper popup stack management
    /// Delegates all business logic to SaveService for clean separation of concerns
    /// Provides both regular saves, autosaves, and quick-save via double-click
    /// 
    /// INTENT: Specialized popup for saving game files with comprehensive progress feedback
    /// DESIGN: Enhanced with modal progress overlay using ProgressBar and proper popup stack management
    /// PROS: Clear visual feedback with standard ProgressBar, proper pause state management, multiple save options
    /// CONS: More complex UI state management, additional visual elements
    /// </summary>
    public class SaveGamePopup : SaveFileListPopup
    {
        #region UI Elements
        
        private Button _saveButton;
        private Button _autoSaveButton;
        
        // Progress overlay elements - using ProgressBar like LoadingScreen
        private VisualElement _savingProgressOverlay;
        private VisualElement _mainContent;
        private Label _savingTitle;
        private Label _savingStatus;
        private ProgressBar _progressBar; // Changed from VisualElement to ProgressBar
        
        #endregion

        #region Save State
        
        private bool _isSaving;
        
        #endregion

        #region Constants
        
        private const string SAVE_SUCCESS_MESSAGE = "Game saved successfully!";
        private const string SAVE_ERROR_MESSAGE = "Error saving game";
        private const string AUTOSAVE_SUCCESS_MESSAGE = "Auto-save completed!";
        private const string AUTOSAVE_ERROR_MESSAGE = "Error auto-saving game";
        private const string OVERWRITE_SUCCESS_MESSAGE = "Save file overwritten successfully!";
        private const string OVERWRITE_ERROR_MESSAGE = "Error overwriting save file";
        
        // Progress overlay messages
        private const string PROGRESS_SAVING = "Saving game...";
        private const string PROGRESS_AUTOSAVING = "Creating auto-save...";
        private const string PROGRESS_OVERWRITING = "Overwriting save file...";
        private const string PROGRESS_SUCCESS = "Save completed!";
        
        #endregion

        public SaveGamePopup(VisualElement rootElement) : base(rootElement)
        {
            InitializeBaseUI();
            UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.ThrowExceptions);
        }

        #region Base Class Implementation
        
        protected override void CacheSpecificUIElements()
        {
            _saveButton = RootElement?.Q<Button>("btn_Save");
            _autoSaveButton = RootElement?.Q<Button>("btn_AutoSave");
            
            // Cache progress overlay elements - using ProgressBar query like LoadingScreen
            _savingProgressOverlay = RootElement?.Q<VisualElement>("overlay_SavingProgress");
            _mainContent = RootElement?.Q<VisualElement>("content_Main");
            _savingTitle = RootElement?.Q<Label>("lbl_SavingTitle");
            _savingStatus = RootElement?.Q<Label>("lbl_SavingStatus");
            _progressBar = RootElement?.Q<ProgressBar>("progress_Bar"); // Query ProgressBar by name
        }

        protected override void SetupSpecificFunctionality()
        {
            // Ensure progress overlay is initially hidden
            HideProgressOverlay();
        }

        protected override void RegisterSpecificEventHandlers()
        {
            _saveButton?.RegisterCallback<ClickEvent>(OnSaveButtonClicked);
            _autoSaveButton?.RegisterCallback<ClickEvent>(OnAutoSaveButtonClicked);
            _eventSystem?.Subscribe<SaveCompletedEvent>(OnSaveCompleted);
            _eventSystem?.Subscribe<SaveFailedEvent>(OnSaveFailed);
        }

        protected override void UnregisterSpecificEventHandlers()
        {
            _saveButton?.UnregisterCallback<ClickEvent>(OnSaveButtonClicked);
            _autoSaveButton?.UnregisterCallback<ClickEvent>(OnAutoSaveButtonClicked);
            _eventSystem?.Unsubscribe<SaveCompletedEvent>(OnSaveCompleted);
            _eventSystem?.Unsubscribe<SaveFailedEvent>(OnSaveFailed);
        }

        protected override void UpdateSpecificButtonStates()
        {
            if (!IsVisible) return;
            
            bool canSave = CanSaveGame();
            _saveButton?.SetEnabled(canSave);
            _autoSaveButton?.SetEnabled(canSave);
        }

        protected override void ResetUIState()
        {
            base.ResetUIState();
            _isSaving = false;
            HideProgressOverlay();
        }

        protected override async Task ClosePopup()
        {
            // Instead of directly hiding, ensure we return to the pause popup properly
            await _uiService?.HidePopupAsync<SaveGamePopup>();
        }

        /// <summary>
        /// Implements double-click action for overwriting - called by base class
        /// Double-clicking on a save file will overwrite it with current game state
        /// </summary>
        protected override async Task OnDoubleClickAction(SaveFileInfo selectedSaveFile)
        {
            await PerformOverwriteSaveOperation(selectedSaveFile);
        }

        /// <summary>
        /// Implements double-click validation - called by base class
        /// </summary>
        protected override bool CanPerformDoubleClickAction()
        {
            return CanSaveGame() && _selectedSaveFile != null;
        }
        
        #endregion

        #region Progress Overlay Management - Using ProgressBar like LoadingScreen
        
        /// <summary>
        /// Shows the modal progress overlay with specified title and status
        /// </summary>
        private void ShowProgressOverlay(string title, string status)
        {
            if (_savingProgressOverlay == null) return;
            
            UpdateSaveTitle(title);
            UpdateSaveStatus(status);
            UpdateProgressBar(0f); // Reset progress bar using ProgressBar.value
            
            _savingProgressOverlay.style.display = DisplayStyle.Flex;
            _mainContent?.SetEnabled(false); // Disable interaction with main content
        }
        
        /// <summary>
        /// Updates the progress overlay with new status and progress
        /// Uses the same pattern as LoadingScreen
        /// </summary>
        private void UpdateProgressOverlay(string status, float progress = -1f)
        {
            UpdateSaveStatus(status);
            
            if (progress >= 0f && progress <= 1f)
            {
                UpdateProgressBar(progress);
            }
        }
        
        /// <summary>
        /// Updates just the progress bar value - same logic as LoadingScreen
        /// </summary>
        private void UpdateProgressBar(float progress)
        {
            if (_progressBar != null)
            {
                _progressBar.value = Mathf.Clamp01(progress);
            }
        }
        
        /// <summary>
        /// Updates the save title text
        /// </summary>
        private void UpdateSaveTitle(string title)
        {
            if (_savingTitle != null)
            {
                _savingTitle.text = title;
            }
        }
        
        /// <summary>
        /// Updates the save status text
        /// </summary>
        private void UpdateSaveStatus(string status)
        {
            if (_savingStatus != null)
            {
                _savingStatus.text = status;
            }
        }
        
        /// <summary>
        /// Hides the progress overlay and re-enables main content
        /// </summary>
        private void HideProgressOverlay()
        {
            if (_savingProgressOverlay == null) return;
            
            _savingProgressOverlay.style.display = DisplayStyle.None;
            _mainContent?.SetEnabled(true);
        }
        
        #endregion

        #region Save Functionality
        
        /// <summary>
        /// Checks if the game can currently be saved by delegating to SaveService
        /// </summary>
        private bool CanSaveGame()
        {
            return IsVisible &&
                   !_isSaving &&
                   !_isLoadingData &&
                   !_isDeletingFile &&
                   _saveService.CanSaveGame();
        }

        /// <summary>
        /// Handles regular save button click
        /// </summary>
        private async void OnSaveButtonClicked(ClickEvent evt)
        {
            if (!IsVisible) return;
            
            if (!CanSaveGame())
            {
                Debug.LogWarning($"[SaveGamePopup] Cannot save - Saving: {_isSaving}, Visible: {IsVisible}");
                return;
            }

            await PerformSaveOperation(false);
        }

        /// <summary>
        /// Handles auto-save button click
        /// </summary>
        private async void OnAutoSaveButtonClicked(ClickEvent evt)
        {
            if (!IsVisible) return;
            
            if (!CanSaveGame())
            {
                Debug.LogWarning($"[SaveGamePopup] Cannot auto-save - Saving: {_isSaving}, Visible: {IsVisible}");
                return;
            }

            await PerformSaveOperation(true);
        }

        /// <summary>
        /// Performs a new save operation with enhanced progress feedback using ProgressBar
        /// Now uses event system for clean separation of concerns
        /// </summary>
        private async Task PerformSaveOperation(bool isAutoSave)
        {
            try
            {
                _isSaving = true;
        
                // Show progress overlay
                string progressTitle = isAutoSave ? PROGRESS_AUTOSAVING : PROGRESS_SAVING;
                ShowProgressOverlay(progressTitle, "Preparing save data...");
        
                UpdateButtonStates();

                // Simulate progress updates using ProgressBar.value
                UpdateProgressOverlay("Requesting save operation...", 0.3f);
                await Task.Delay(200); // Small delay for visual feedback

                // Publish save request event instead of calling service directly
                if (isAutoSave)
                {
                    _eventSystem.Publish(new AutoSaveRequestedEvent());
                }
                else
                {
                    _eventSystem.Publish(new RegularSaveRequestedEvent());
                }

                UpdateProgressOverlay("Save request sent...", 0.6f);
        
                // Note: The actual save completion will be handled by event subscribers
                // The SaveService will publish SaveCompletedEvent or SaveFailedEvent
                // which we should subscribe to for UI feedback
            }
            catch (Exception ex)
            {
                HandleSaveError(ex.Message, isAutoSave, false);
            }
        }

        /// <summary>
        /// Performs an overwrite save operation (from double-click) with progress feedback
        /// Now uses event system for clean separation of concerns
        /// </summary>
        private async Task PerformOverwriteSaveOperation(SaveFileInfo targetSaveFile)
        {
            try
            {
                _isSaving = true;
        
                // Show progress overlay for overwrite
                ShowProgressOverlay(PROGRESS_OVERWRITING, $"Overwriting {targetSaveFile.fileName}...");
        
                UpdateButtonStates();
        
                UpdateProgressOverlay("Requesting overwrite operation...", 0.6f);
                await Task.Delay(200);

                // Publish overwrite request event instead of calling service directly
                _eventSystem.Publish(new OverwriteSaveRequestedEvent(targetSaveFile));

                UpdateProgressOverlay("Overwrite request sent...", 0.9f);
            }
            catch (Exception ex)
            {
                HandleSaveError(ex.Message, false, true);
            }
        }


        /// <summary>
        /// Handles successful save operation with proper popup management
        /// </summary>
        private async Task HandleSaveSuccess(string saveName, bool isAutoSave, bool isOverwrite)
        {
            // Update progress to completion
            UpdateProgressOverlay(PROGRESS_SUCCESS, 1.0f);
            await Task.Delay(800); // Show completion state
            
            // Publish save event
            _eventSystem.Publish(new SaveGameEvent());
            
            // Refresh the save files list to show the new/updated save
            await RefreshSaveFilesList();
            
            // Hide progress overlay
            HideProgressOverlay();
            
            // Show brief success message
            string successMessage = isOverwrite ? OVERWRITE_SUCCESS_MESSAGE :
                                  isAutoSave ? AUTOSAVE_SUCCESS_MESSAGE : 
                                  SAVE_SUCCESS_MESSAGE;
            SetStatusMessage(successMessage, true);
            
            await ClosePopupAsync();
        }

        /// <summary>
        /// Handles save operation errors with proper UI feedback
        /// </summary>
        private void HandleSaveError(string errorMessage, bool isAutoSave, bool isOverwrite)
        {
            // Hide progress overlay first
            HideProgressOverlay();
            
            string errorMsg = isOverwrite ? OVERWRITE_ERROR_MESSAGE :
                            isAutoSave ? AUTOSAVE_ERROR_MESSAGE : 
                            SAVE_ERROR_MESSAGE;
            SetStatusMessage(errorMsg, true);
            
            // Clear error message after a delay
            _ = Task.Delay(3000).ContinueWith(_ => 
            {
                if (_saveFiles.Length > 0)
                {
                    SetStatusMessage("", false);
                }
            });
        }

        /// <summary>
        /// Safely closes the popup with proper error handling
        /// This should return to the PausePopup that's in the popup stack
        /// </summary>
        private async Task ClosePopupAsync()
        {
            try
            {
                await ClosePopup();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveGamePopup] Error closing popup: {ex}");
            }
        }
        
        /// <summary>
        /// Handles successful save completion from the event system
        /// </summary>
        private async void OnSaveCompleted(SaveCompletedEvent saveEvent)
        {
            if (!IsVisible || !_isSaving) return;
    
            try
            {
                await HandleSaveSuccess(saveEvent.SaveFileName, saveEvent.IsAutoSave, saveEvent.IsOverwrite);
            }
            finally
            {
                _isSaving = false;
                UpdateButtonStates();
            }
        }

        /// <summary>
        /// Handles save failure from the event system
        /// </summary>
        private void OnSaveFailed(SaveFailedEvent saveEvent)
        {
            if (!IsVisible || !_isSaving) return;
    
            try
            {
                HandleSaveError(saveEvent.ErrorMessage, saveEvent.IsAutoSave, saveEvent.IsOverwrite);
            }
            finally
            {
                _isSaving = false;
                UpdateButtonStates();
            }
        }
        
        #endregion
    }
}
