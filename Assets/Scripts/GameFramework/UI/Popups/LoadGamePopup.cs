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
    /// Load game popup that extends the base save file list functionality
    /// Provides double-click loading and load button functionality
    /// Works with FileService for file operations and LoadService for game state loading
    /// 
    /// INTENT: Specialized popup for loading saved game files with enhanced UX
    /// DESIGN: Leverages base class infrastructure and new FileService/LoadService separation
    /// PROS: Minimal code duplication, consistent interaction patterns, clean separation of concerns
    /// CONS: Dependent on base class implementation and service availability
    /// </summary>
    public class LoadGamePopup : SaveFileListPopup
    {
        #region UI Elements
        
        private Button _loadGameButton;
        
        #endregion

        #region Constructor
        
        public LoadGamePopup(VisualElement rootElement) : base(rootElement)
        {
            InitializeBaseUI();
            UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.ThrowExceptions);
        }
        
        #endregion

        #region Base Class Implementation
        
        protected override void CacheSpecificUIElements()
        {
            _loadGameButton = RootElement?.Q<Button>("btn_Load");
            
            if (_loadGameButton == null)
            {
                Debug.LogError("[LoadGamePopup] Load button not found in UI");
            }
        }

        protected override void SetupSpecificFunctionality()
        {
            // Subscribe to load-related events to update UI state
            if (_eventSystem != null)
            {
                _eventSystem.Subscribe<LoadingStartedEvent>(OnLoadStarted);
                _eventSystem.Subscribe<LoadingCompletedEvent>(OnLoadCompleted);
                _eventSystem.Subscribe<LoadingFailedEvent>(OnLoadFailed);
            }
        }

        protected override void RegisterSpecificEventHandlers()
        {
            _loadGameButton?.RegisterCallback<ClickEvent>(OnLoadButtonClicked);
        }

        protected override void UnregisterSpecificEventHandlers()
        {
            _loadGameButton?.UnregisterCallback<ClickEvent>(OnLoadButtonClicked);
            
            // Unsubscribe from load events
            if (_eventSystem != null)
            {
                _eventSystem.Unsubscribe<LoadingStartedEvent>(OnLoadStarted);
                _eventSystem.Unsubscribe<LoadingCompletedEvent>(OnLoadCompleted);
                _eventSystem.Unsubscribe<LoadingFailedEvent>(OnLoadFailed);
            }
        }

        protected override void UpdateSpecificButtonStates()
        {
            if (!IsVisible) return;
            
            bool canLoad = CanLoadSelectedFile();
            _loadGameButton?.SetEnabled(canLoad);
            
            // Update button text/appearance based on loading state
            if (_loadGameButton != null)
            {
                if (_loadService?.IsLoading == true)
                {
                    _loadGameButton.text = "Loading...";
                }
                else
                {
                    _loadGameButton.text = "Load Game";
                }
            }
        }

        protected override async Task ClosePopup()
        {
            await _uiService?.HidePopupAsync<LoadGamePopup>();
        }

        /// <summary>
        /// Implements double-click action for loading - called by base class
        /// </summary>
        protected override async Task OnDoubleClickAction(SaveFileInfo selectedSaveFile)
        {
            await RequestLoadSelectedFile();
        }

        /// <summary>
        /// Implements double-click validation - called by base class
        /// </summary>
        protected override bool CanPerformDoubleClickAction()
        {
            return CanLoadSelectedFile();
        }
        
        #endregion

        #region Load-Specific Functionality
        
        /// <summary>
        /// Validates if the currently selected file can be loaded
        /// Checks both file service and load service states
        /// </summary>
        private bool CanLoadSelectedFile()
        {
            return IsVisible &&
                   _selectedSaveFile != null &&
                   _selectedSaveFile.IsValid() &&
                   !_isLoadingData &&
                   !_isDeletingFile &&
                   _loadService?.IsLoading != true &&
                   _fileService?.IsInitialized == true &&
                   _loadService?.IsInitialized == true;
        }

        /// <summary>
        /// Handles load button click with comprehensive validation
        /// </summary>
        private async void OnLoadButtonClicked(ClickEvent evt)
        {
            if (!IsVisible) return;
            
            if (!CanLoadSelectedFile())
            {
                string reason = GetCannotLoadReason();
                Debug.LogWarning($"[LoadGamePopup] Cannot load - {reason}");
                return;
            }

            await RequestLoadSelectedFile();
        }

        /// <summary>
        /// Gets a descriptive reason why loading cannot proceed (for debugging)
        /// </summary>
        private string GetCannotLoadReason()
        {
            if (!IsVisible) return "Popup not visible";
            if (_selectedSaveFile == null) return "No save file selected";
            if (!_selectedSaveFile.IsValid()) return "Selected save file is invalid/corrupted";
            if (_isLoadingData) return "Currently loading save file data";
            if (_isDeletingFile) return "Currently deleting a file";
            if (_loadService?.IsLoading == true) return "Load service is busy";
            if (_fileService?.IsInitialized != true) return "File service not initialized";
            if (_loadService?.IsInitialized != true) return "Load service not initialized";
            return "Unknown reason";
        }

        /// <summary>
        /// Requests loading of selected save file and transitions to loading state
        /// </summary>
        private async Task RequestLoadSelectedFile()
        {
            if (!IsVisible || _selectedSaveFile == null)
            {
                Debug.LogWarning($"[LoadGamePopup] Cannot request load - Visible: {IsVisible}, Selected: {_selectedSaveFile != null}");
                return;
            }
    
            try
            {
                Debug.Log($"[LoadGamePopup] Requesting load of save file: {_selectedSaveFile.FileName}");
        
                // Publish begin load event - LoadService will handle the actual loading
                var beginLoadEvent = new BeginLoadGameEvent(_selectedSaveFile);
                _eventSystem.Publish(beginLoadEvent);
        
                // Close popup immediately - we're transitioning to loading state
                await ClosePopupAsync();
        
                Debug.Log("[LoadGamePopup] Load request published successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoadGamePopup] Error requesting load: {ex}");
                SetStatusMessage("Error requesting load", true);
        
                // Clear error message after delay
                await Task.Delay(3000);
                SetStatusMessage("", false);
            }
        }
        /// <summary>
        /// Safely closes the popup with proper error handling
        /// </summary>
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

        #region Load Event Handlers
        
        /// <summary>
        /// Handles load started event to update UI state
        /// </summary>
        private void OnLoadStarted(LoadingStartedEvent evt)
        {
            if (!IsVisible) return;
            
            UpdateButtonStates();
            SetStatusMessage("Loading game...", true);
        }

        /// <summary>
        /// Handles load completed event - closes popup on successful load
        /// </summary>
        private async void OnLoadCompleted(LoadingCompletedEvent evt)
        {
            if (!IsVisible) return;
            
            try
            {
                // Close popup after successful load
                await ClosePopupAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoadGamePopup] Error closing popup after successful load: {ex}");
            }
        }

        /// <summary>
        /// Handles load failed event to update UI and show error
        /// </summary>
        private async void OnLoadFailed(LoadingFailedEvent evt)
        {
            if (!IsVisible) return;
            
            UpdateButtonStates();
            SetStatusMessage($"Load failed: {evt.Exception.Message}", true);
            
            // Clear error message after delay
            await Task.Delay(5000);
            if (IsVisible)
            {
                SetStatusMessage("", false);
            }
        }
        
        #endregion

        #region Validation Override
        
        /// <summary>
        /// Override to provide more comprehensive validation for load operations
        /// </summary>
        protected override bool CanDeleteSelectedFile()
        {
            // Can't delete while loading
            return base.CanDeleteSelectedFile() && _loadService?.IsLoading != true;
        }
        
        #endregion
    }
}
