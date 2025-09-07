using System;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events;
using GameFramework.Services.Interfaces;
using UCTools_Utilities.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Save game popup that extends the base save file list functionality
    /// Delegates all business logic to SaveService for clean separation of concerns
    /// Provides both regular saves and autosaves through simple UI interactions
    /// </summary>
    public class SaveGamePopup : SaveFileListPopup
    {
        #region UI Elements
        
        private Button _saveButton;
        private Button _autoSaveButton;
        
        #endregion

        #region Save State
        
        private bool _isSaving;
        
        #endregion

        #region Constants
        
        private const string SAVING_MESSAGE = "Saving game...";
        private const string SAVE_SUCCESS_MESSAGE = "Game saved successfully!";
        private const string SAVE_ERROR_MESSAGE = "Error saving game";
        private const string AUTOSAVE_SUCCESS_MESSAGE = "Auto-save completed!";
        private const string AUTOSAVE_ERROR_MESSAGE = "Error auto-saving game";
        
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
        }

        protected override void SetupSpecificFunctionality()
        {
            // No additional setup needed - business logic is handled by SaveService
        }

        protected override void RegisterSpecificEventHandlers()
        {
            _saveButton?.RegisterCallback<ClickEvent>(OnSaveButtonClicked);
            _autoSaveButton?.RegisterCallback<ClickEvent>(OnAutoSaveButtonClicked);
        }

        protected override void UnregisterSpecificEventHandlers()
        {
            _saveButton?.UnregisterCallback<ClickEvent>(OnSaveButtonClicked);
            _autoSaveButton?.UnregisterCallback<ClickEvent>(OnAutoSaveButtonClicked);
        }

        protected override void UpdateSpecificButtonStates()
        {
            bool canSave = CanSaveGame();
            _saveButton?.SetEnabled(canSave);
            _autoSaveButton?.SetEnabled(canSave);
        }

        protected override void ResetUIState()
        {
            base.ResetUIState();
            _isSaving = false;
        }
        
        #endregion

        #region Save Functionality
        
        /// <summary>
        /// Checks if the game can currently be saved by delegating to SaveService
        /// </summary>
        private bool CanSaveGame()
        {
            return !_isSaving &&
                   !_isLoadingData &&
                   !_isDeletingFile &&
                   _saveService.CanSaveGame();
        }

        /// <summary>
        /// Handles regular save button click
        /// </summary>
        private async void OnSaveButtonClicked(ClickEvent evt)
        {
            Debug.Log("[SaveGamePopup] Save button clicked");

            if (!CanSaveGame())
            {
                Debug.LogWarning($"[SaveGamePopup] Cannot save - Saving: {_isSaving}");
                return;
            }

            await PerformSaveOperation(false);
        }

        /// <summary>
        /// Handles auto-save button click
        /// </summary>
        private async void OnAutoSaveButtonClicked(ClickEvent evt)
        {
            Debug.Log("[SaveGamePopup] Auto-save button clicked");

            if (!CanSaveGame())
            {
                Debug.LogWarning($"[SaveGamePopup] Cannot auto-save - Saving: {_isSaving}");
                return;
            }

            await PerformSaveOperation(true);
        }

        /// <summary>
        /// Performs the save operation by delegating to SaveService
        /// </summary>
        private async Task PerformSaveOperation(bool isAutoSave)
        {
            try
            {
                _isSaving = true;
                
                SetStatusMessage(SAVING_MESSAGE, true);
                UpdateButtonStates();

                // Delegate save operation to SaveService
                var (success, saveName) = isAutoSave 
                    ? await _saveService.PerformAutoSaveAsync()
                    : await _saveService.PerformRegularSaveAsync();

                if (success)
                {
                    await HandleSaveSuccess(saveName, isAutoSave);
                }
                else
                {
                    HandleSaveError("Save operation failed", isAutoSave);
                }
            }
            catch (Exception ex)
            {
                HandleSaveError(ex.Message, isAutoSave);
                Debug.LogError($"[SaveGamePopup] Error saving game: {ex}");
            }
            finally
            {
                _isSaving = false;
                UpdateButtonStates();
            }
        }

        /// <summary>
        /// Handles successful save operation
        /// </summary>
        private async Task HandleSaveSuccess(string saveName, bool isAutoSave)
        {
            Debug.Log($"[SaveGamePopup] Successfully saved game as '{saveName}' (AutoSave: {isAutoSave})");
            
            string successMessage = isAutoSave ? AUTOSAVE_SUCCESS_MESSAGE : SAVE_SUCCESS_MESSAGE;
            SetStatusMessage(successMessage, true);
            
            // Publish save event
            _eventSystem.Publish(new SaveGameEvent());
            
            // Refresh the save files list to show the new save
            await RefreshSaveFilesList();
            
            // Clear success message after a delay, then close
            await Task.Delay(1500);
            await ClosePopup();
        }

        /// <summary>
        /// Handles save operation errors
        /// </summary>
        private void HandleSaveError(string errorMessage, bool isAutoSave)
        {
            Debug.LogError($"[SaveGamePopup] Save failed: {errorMessage}");
            
            string errorMsg = isAutoSave ? AUTOSAVE_ERROR_MESSAGE : SAVE_ERROR_MESSAGE;
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
        
        protected override async Task ClosePopup()
        {
            await _uiService?.HidePopupAsync<SaveGamePopup>();
        }
        
        #endregion
    }
}
