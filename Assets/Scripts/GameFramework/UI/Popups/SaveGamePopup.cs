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
    /// Provides save functionality with new file creation and overwrite capabilities
    /// Supports both creating new saves and overwriting existing ones
    /// </summary>
    public class SaveGamePopup : SaveFileListPopup
    {
        #region UI Elements
        
        private Button _saveButton;
        private TextField _saveNameField;
        private Label _overwriteWarningLabel;
        
        #endregion

        #region Save State
        
        private bool _isSaving;
        private string _pendingSaveName;
        
        #endregion

        #region Constants
        
        private const string SAVING_MESSAGE = "Saving game...";
        private const string SAVE_SUCCESS_MESSAGE = "Game saved successfully!";
        private const string SAVE_ERROR_MESSAGE = "Error saving game";
        private const string OVERWRITE_WARNING_MESSAGE = "This will overwrite an existing save file";
        private const string DEFAULT_SAVE_NAME_PREFIX = "Save";
        
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
            _saveNameField = RootElement?.Q<TextField>("txt_SaveName");
            _overwriteWarningLabel = RootElement?.Q<Label>("lbl_OverwriteWarning");
        }

        protected override void SetupSpecificFunctionality()
        {
            GenerateDefaultSaveName();
            SetupSaveNameValidation();
            HideOverwriteWarning();
        }

        protected override void RegisterSpecificEventHandlers()
        {
            _saveButton?.RegisterCallback<ClickEvent>(OnSaveButtonClicked);
            _saveNameField?.RegisterCallback<ChangeEvent<string>>(OnSaveNameChanged);
            
            // Double-click to select existing save for overwriting
            _saveFileList?.RegisterCallback<MouseDownEvent>(OnListViewMouseDown);
        }

        protected override void UnregisterSpecificEventHandlers()
        {
            _saveButton?.UnregisterCallback<ClickEvent>(OnSaveButtonClicked);
            _saveNameField?.UnregisterCallback<ChangeEvent<string>>(OnSaveNameChanged);
            _saveFileList?.UnregisterCallback<MouseDownEvent>(OnListViewMouseDown);
        }

        protected override void UpdateSpecificButtonStates()
        {
            bool canSave = CanSaveGame();
            _saveButton?.SetEnabled(canSave);
        }

        protected override void UpdateSelection(SaveFileInfo selectedSaveFile)
        {
            base.UpdateSelection(selectedSaveFile);
            
            // Auto-populate save name field when selecting existing save
            if (selectedSaveFile != null && _saveNameField != null)
            {
                _saveNameField.value = selectedSaveFile.fileName;
                UpdateOverwriteWarning();
            }
        }

        protected override void ResetUIState()
        {
            base.ResetUIState();
            _isSaving = false;
            _pendingSaveName = null;
            GenerateDefaultSaveName();
            HideOverwriteWarning();
        }
        
        #endregion

        #region Save-Specific Functionality
        
        private void GenerateDefaultSaveName()
        {
            if (_saveNameField == null) return;

            // Get current game data for better naming
            var gameDataService = GameManager.GetService<IGameDataService>();
            string playerName = gameDataService?.CurrentSession?.playerName ?? "Player";
            
            // Generate a unique default name
            string baseName = $"{playerName}_{DEFAULT_SAVE_NAME_PREFIX}";
            string uniqueName = GenerateUniqueSaveName(baseName);
            
            _saveNameField.value = uniqueName;
        }

        private string GenerateUniqueSaveName(string baseName)
        {
            string testName = baseName;
            int counter = 1;
            
            // Keep incrementing until we find a unique name
            while (SaveFileExists(testName))
            {
                testName = $"{baseName}_{counter:D2}";
                counter++;
            }
            
            return testName;
        }

        private bool SaveFileExists(string saveName)
        {
            foreach (var saveFile in _saveFiles)
            {
                if (saveFile.fileName.Equals(saveName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private void SetupSaveNameValidation()
        {
            if (_saveNameField == null) return;
            
            // Set reasonable character limit
            _saveNameField.maxLength = 50;
            
            // Initial validation
            ValidateSaveName(_saveNameField.value);
        }

        private bool CanSaveGame()
        {
            return !string.IsNullOrWhiteSpace(_saveNameField?.value) &&
                   !_isSaving &&
                   !_isLoadingData &&
                   !_isDeletingFile &&
                   IsValidSaveFileName(_saveNameField?.value);
        }

        private bool IsValidSaveFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return false;
            
            // Check for invalid characters
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                if (fileName.Contains(c)) return false;
            }
            
            return true;
        }

        private void OnSaveNameChanged(ChangeEvent<string> evt)
        {
            ValidateSaveName(evt.newValue);
            UpdateOverwriteWarning();
            UpdateButtonStates();
        }

        private void ValidateSaveName(string saveName)
        {
            // Visual feedback for invalid names could be added here
            // For now, just update button states
        }

        private void UpdateOverwriteWarning()
        {
            string saveName = _saveNameField?.value;
            bool willOverwrite = !string.IsNullOrEmpty(saveName) && SaveFileExists(saveName);
            
            if (willOverwrite)
            {
                ShowOverwriteWarning();
            }
            else
            {
                HideOverwriteWarning();
            }
        }

        private void ShowOverwriteWarning()
        {
            if (_overwriteWarningLabel == null) return;
            
            _overwriteWarningLabel.text = OVERWRITE_WARNING_MESSAGE;
            _overwriteWarningLabel.style.display = DisplayStyle.Flex;
            _overwriteWarningLabel.AddToClassList("warning-text");
        }

        private void HideOverwriteWarning()
        {
            if (_overwriteWarningLabel == null) return;
            
            _overwriteWarningLabel.style.display = DisplayStyle.None;
            _overwriteWarningLabel.RemoveFromClassList("warning-text");
        }

        /// <summary>
        /// Handles double-click on ListView - populates save name for overwriting
        /// </summary>
        private void OnListViewMouseDown(MouseDownEvent evt)
        {
            if (evt.clickCount == 2 && evt.button == 0) // Left mouse button double-click
            {
                Debug.Log("[SaveGamePopup] Double-click detected on ListView");

                if (_selectedSaveFile != null && _saveNameField != null)
                {
                    Debug.Log($"[SaveGamePopup] Double-click selecting save file for overwrite: {_selectedSaveFile.fileName}");
                    _saveNameField.value = _selectedSaveFile.fileName;
                    _saveNameField.Focus(); // Focus the text field for easy editing
                    evt.StopPropagation();
                }
            }
        }

        private async void OnSaveButtonClicked(ClickEvent evt)
        {
            Debug.Log("[SaveGamePopup] Save button clicked");

            if (!CanSaveGame())
            {
                Debug.LogWarning($"[SaveGamePopup] Cannot save - Name: '{_saveNameField?.value}', Saving: {_isSaving}");
                return;
            }

            string saveName = _saveNameField.value.Trim();
            await PerformSaveOperation(saveName);
        }

        /// <summary>
        /// Performs the actual save operation
        /// </summary>
        private async Task PerformSaveOperation(string saveName)
        {
            try
            {
                _isSaving = true;
                _pendingSaveName = saveName;
                
                SetStatusMessage(SAVING_MESSAGE, true);
                UpdateButtonStates();

                Debug.Log($"[SaveGamePopup] Saving game as '{saveName}'");

                // Get the game data service to save current session
                var gameDataService = GameManager.GetService<IGameDataService>();
                if (gameDataService?.CurrentSession == null)
                {
                    throw new InvalidOperationException("No active game session to save");
                }

                // Perform the save operation
                bool saveSuccess = await gameDataService.SaveCurrentSessionAsync(saveName);

                if (saveSuccess)
                {
                    await HandleSaveSuccess();
                }
                else
                {
                    HandleSaveError("Save operation returned false");
                }
            }
            catch (Exception ex)
            {
                HandleSaveError(ex.Message);
                Debug.LogError($"[SaveGamePopup] Error saving game: {ex}");
            }
            finally
            {
                _isSaving = false;
                _pendingSaveName = null;
                UpdateButtonStates();
            }
        }

        private async Task HandleSaveSuccess()
        {
            Debug.Log($"[SaveGamePopup] Successfully saved game as '{_pendingSaveName}'");
            
            SetStatusMessage(SAVE_SUCCESS_MESSAGE, true);
            
            // Publish save event
            _eventSystem.Publish(new SaveGameEvent());
            
            // Refresh the save files list to show the new/updated save
            await RefreshSaveFilesList();
            
            // Clear success message after a delay, then close
            await Task.Delay(1500);
            await ClosePopup();
        }

        private void HandleSaveError(string errorMessage)
        {
            Debug.LogError($"[SaveGamePopup] Save failed: {errorMessage}");
            SetStatusMessage(SAVE_ERROR_MESSAGE, true);
            
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
