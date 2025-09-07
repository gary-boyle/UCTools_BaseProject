using System;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services;
using GameFramework.Services.Interfaces;
using GameFramework.UI.Popups;
using GameFramework.UI.Screens;
using UCTools_Utilities.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Popups
{
    /// <summary>
    /// Pause popup UI that handles pause menu interactions as an overlay
    /// Provides options to resume, load game, access options, or return to main menu
    /// Works within PlayingState rather than as a separate state
    /// </summary>
    public class PausePopup : UIPopup
    {
        #region UI Elements
        private Button _loadButton;
        private Button _saveButton;
        private Button _optionsButton;
        private Button _mainMenuButton;
        private Button _quitButton;
        private Button _closeButton;
        #endregion

        #region Services
        private readonly IUIService _uiService;
        private readonly IEventSystem _eventSystem;
        private readonly IGameDataService _gameDataService;
        #endregion

        public PausePopup(VisualElement rootElement) : base(rootElement)
        {
            // Initialize services via dependency injection
            _uiService = GameManager.GetService<IUIService>() ?? throw new ArgumentNullException(nameof(_uiService));
            _eventSystem = GameManager.GetService<IEventSystem>() ?? throw new ArgumentNullException(nameof(_eventSystem));
            _gameDataService = GameManager.GetService<IGameDataService>() ?? throw new ArgumentNullException(nameof(_gameDataService));
            
            InitializeUI();
            UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.ThrowExceptions);
        }

        #region Initialization
        private void InitializeUI()
        {
            CacheUIElements();
            ConfigureInitialStates();
        }

        private void CacheUIElements()
        {
            _loadButton = RootElement?.Q<Button>("btn_Load");
            _saveButton = RootElement?.Q<Button>("btn_Save");
            _optionsButton = RootElement?.Q<Button>("btn_Options");
            _mainMenuButton = RootElement?.Q<Button>("btn_MainMenu");
            _quitButton = RootElement?.Q<Button>("btn_Quit");
            _closeButton = RootElement?.Q<Button>("btn_Close");
        }

        private void ConfigureInitialStates()
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            // Enable/disable load button based on whether save files exist
            if (_loadButton != null)
            {
                _loadButton.SetEnabled(true); // LoadGamePopup will handle empty state
            }

            // Other buttons are generally always available in pause state
            _optionsButton?.SetEnabled(true);
            _mainMenuButton?.SetEnabled(true);
            _quitButton?.SetEnabled(true);
        }
        #endregion

        #region Event Handlers
        protected override async void OnShow()
        {
            RegisterEventHandlers();
            UpdateButtonStates();
            
            Debug.Log("[PausePopup] Pause popup shown - game should already be paused by PlayingState");
        }

        protected override void OnHide()
        {
            UnregisterEventHandlers();
            Debug.Log("[PausePopup] Pause popup hidden");
        }

        private void RegisterEventHandlers()
        {
            _loadButton?.RegisterCallback<ClickEvent>(OnLoadButtonClicked);
            _saveButton?.RegisterCallback<ClickEvent>(OnSaveButtonClicked);
            _optionsButton?.RegisterCallback<ClickEvent>(OnOptionsButtonClicked);
            _mainMenuButton?.RegisterCallback<ClickEvent>(OnMainMenuButtonClicked);
            _quitButton?.RegisterCallback<ClickEvent>(OnQuitButtonClicked);
            _closeButton?.RegisterCallback<ClickEvent>(OnCloseButtonClicked);
        }

        private void UnregisterEventHandlers()
        {
            _loadButton?.UnregisterCallback<ClickEvent>(OnLoadButtonClicked);
            _saveButton?.UnregisterCallback<ClickEvent>(OnSaveButtonClicked);
            _optionsButton?.UnregisterCallback<ClickEvent>(OnOptionsButtonClicked);
            _mainMenuButton?.UnregisterCallback<ClickEvent>(OnMainMenuButtonClicked);
            _quitButton?.UnregisterCallback<ClickEvent>(OnQuitButtonClicked);
            _closeButton?.UnregisterCallback<ClickEvent>(OnCloseButtonClicked);
        }
        

        private bool CanResumeCurrently()
        {
            // Check if PausePopup is the current active popup
            var isPausePopupCurrent = _uiService?.IsCurrentPopup<PausePopup>() ?? false;
    
            if (!isPausePopupCurrent)
            {
                var currentPopupType = _uiService?.GetCurrentPopupType()?.Name ?? "None";
                Debug.Log($"[PausePopup] Cannot resume - another popup is active: {currentPopupType}");
                return false;
            }
    
            return true;
        }    

        private async void OnCloseButtonClicked(ClickEvent evt)
        {
            if (!CanResumeCurrently())
            {
                Debug.Log("[PausePopup] Resume blocked - close other popups first");
                return;
            }
    
            Debug.Log("[PausePopup] Resume button clicked");
            RequestResume();
        }
        
        private async void OnLoadButtonClicked(ClickEvent evt)
        {
            Debug.Log("[PausePopup] Load button clicked");
            await ShowLoadGamePopup();
        }

        private async void OnSaveButtonClicked(ClickEvent evt)
        {
            Debug.Log("[PausePopup] Load button clicked");
            await ShowSaveGamePopup();
        }
        
        private void OnOptionsButtonClicked(ClickEvent evt)
        {
            Debug.Log("[PausePopup] Options button clicked");
            RequestShowOptions();
        }

        private void OnMainMenuButtonClicked(ClickEvent evt)
        {
            Debug.Log("[PausePopup] Main Menu button clicked");
            RequestReturnToMainMenu();
        }

        private void OnQuitButtonClicked(ClickEvent evt)
        {
            Debug.Log("[PausePopup] Quit button clicked");
            RequestQuitGame();
        }
        #endregion

        #region Action Handlers
        
        /// <summary>
        /// Requests to resume the game
        /// </summary>
        private void RequestResume()
        {
            Debug.Log("[PausePopup] Requesting resume");
            _eventSystem?.Publish(new ResumeRequestedEvent());
        }

        /// <summary>
        /// Shows the Load Game popup on top of pause popup
        /// </summary>
        private async Task ShowLoadGamePopup()
        {
            try
            {
                Debug.Log("[PausePopup] Opening Load Game popup");
                await _uiService.ShowPopupAsync<LoadGamePopup>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PausePopup] Error showing Load Game popup: {ex}");
            }
        }

        /// <summary>
        /// Shows the Save Game popup on top of pause popup
        /// </summary>
        private async Task ShowSaveGamePopup()
        {
            try
            {
                Debug.Log("[PausePopup] Opening Save Game popup");
                await _uiService.ShowPopupAsync<SaveGamePopup>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PausePopup] Error showing Save Game popup: {ex}");
            }
        }
        
        /// <summary>
        /// Shows the Options popup on top of pause popup
        /// </summary>
        private async void RequestShowOptions()
        {
            try
            {
                Debug.Log("[PausePopup] Opening Options popup");
                await _uiService.ShowPopupAsync<OptionsPopup>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PausePopup] Error showing Options popup: {ex}");
            }
        }

        /// <summary>
        /// Requests to return to main menu
        /// </summary>
        private void RequestReturnToMainMenu()
        {
            Debug.Log("[PausePopup] Requesting return to main menu");
            _eventSystem.Publish(new MainMenuRequestedEvent());
        }

        /// <summary>
        /// Requests to quit the game
        /// </summary>
        private void RequestQuitGame()
        {
            Debug.Log("[PausePopup] Requesting quit game");
            _eventSystem.Publish(new QuitRequestedEvent());
        }
        
        #endregion
    }
}
