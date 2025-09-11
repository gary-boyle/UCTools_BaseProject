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
    /// 
    /// INTENT: Specialized popup for loading saved game files with enhanced UX
    /// DESIGN: Leverages base class double-click infrastructure for consistent UX
    /// PROS: Minimal code duplication, consistent interaction patterns, clean separation
    /// CONS: Dependent on base class implementation
    /// </summary>
    public class LoadGamePopup : SaveFileListPopup
    {
        #region UI Elements
        
        private Button _loadGameButton;
        
        #endregion

        public LoadGamePopup(VisualElement rootElement) : base(rootElement)
        {
            InitializeBaseUI();
            UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.ThrowExceptions);
        }

        #region Base Class Implementation
        
        protected override void CacheSpecificUIElements()
        {
            _loadGameButton = RootElement?.Q<Button>("btn_Load");
        }

        protected override void SetupSpecificFunctionality()
        {
        }

        protected override void RegisterSpecificEventHandlers()
        {
            _loadGameButton?.RegisterCallback<ClickEvent>(OnLoadButtonClicked);
        }

        protected override void UnregisterSpecificEventHandlers()
        {
            _loadGameButton?.UnregisterCallback<ClickEvent>(OnLoadButtonClicked);
        }

        protected override void UpdateSpecificButtonStates()
        {
            if (!IsVisible) return;
            
            bool canLoad = CanLoadSelectedFile();
            _loadGameButton?.SetEnabled(canLoad);
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
        /// </summary>
        private bool CanLoadSelectedFile()
        {
            return IsVisible &&
                   _selectedSaveFile != null &&
                   !_isLoadingData &&
                   !_isDeletingFile &&
                   !_loadService.IsLoading;
        }

        /// <summary>
        /// Handles load button click with validation
        /// </summary>
        private async void OnLoadButtonClicked(ClickEvent evt)
        {
            if (!IsVisible) return;
            
            if (!CanLoadSelectedFile())
            {
                Debug.LogWarning($"[LoadGamePopup] Cannot load - Selected: {_selectedSaveFile != null}, Loading: {_isLoadingData}, Visible: {IsVisible}");
                return;
            }

            await RequestLoadSelectedFile();
        }

        /// <summary>
        /// Requests loading of selected save file with comprehensive error handling
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
                var loadEvent = new LoadSaveFileEvent(_selectedSaveFile);
                _eventSystem.Publish(loadEvent);
                
                // Close popup after requesting load
                await ClosePopupAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoadGamePopup] Error requesting load: {ex}");
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
    }
}
