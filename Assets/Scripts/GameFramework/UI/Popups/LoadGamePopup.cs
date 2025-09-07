using System;
using System.Threading.Tasks;
using GameFramework.EventSystem.Events;
using UCTools_Utilities.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Load game popup that extends the base save file list functionality
    /// Provides double-click loading and load button functionality
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
            SetupDoubleClickLoading();
        }

        protected override void RegisterSpecificEventHandlers()
        {
            _loadGameButton?.RegisterCallback<ClickEvent>(OnLoadButtonClicked);
        }

        protected override void UnregisterSpecificEventHandlers()
        {
            _loadGameButton?.UnregisterCallback<ClickEvent>(OnLoadButtonClicked);
            _saveFileList?.UnregisterCallback<MouseDownEvent>(OnListViewMouseDown);
        }

        protected override void UpdateSpecificButtonStates()
        {
            bool canLoad = CanLoadSelectedFile();
            _loadGameButton?.SetEnabled(canLoad);
        }

        protected override async Task ClosePopup()
        {
            await _uiService?.HidePopupAsync<LoadGamePopup>(); // ✅ Fixed: Use correct type
        }
        
        #endregion

        #region Load-Specific Functionality
        
        /// <summary>
        /// Sets up double-click functionality on the ListView
        /// </summary>
        private void SetupDoubleClickLoading()
        {
            if (_saveFileList == null) return;
            _saveFileList.RegisterCallback<MouseDownEvent>(OnListViewMouseDown);
        }

        private bool CanLoadSelectedFile()
        {
            return _selectedSaveFile != null &&
                   !_isLoadingData &&
                   !_isDeletingFile &&
                   !_loadService.IsLoading;
        }

        private void OnLoadButtonClicked(ClickEvent evt)
        {
            Debug.Log("[LoadGamePopup] Load button clicked");

            if (!CanLoadSelectedFile())
            {
                Debug.LogWarning($"[LoadGamePopup] Cannot load - Selected: {_selectedSaveFile != null}, Loading: {_isLoadingData}");
                return;
            }

            RequestLoadSelectedFile();
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
            }
        }

        /// <summary>
        /// Publishes event to request loading of selected save file
        /// </summary>
        private void RequestLoadSelectedFile()
        {
            if (_selectedSaveFile == null)
            {
                Debug.LogWarning("[LoadGamePopup] No save file selected");
                return;
            }

            Debug.Log($"[LoadGamePopup] Requesting load of save file: {_selectedSaveFile.fileName}");

            // Publish the event - LoadService handles everything
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
    }
}
