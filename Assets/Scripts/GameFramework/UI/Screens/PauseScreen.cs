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

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Pause screen UI that handles pause menu interactions
    /// Provides options to resume, load game, access options, or return to main menu
    /// </summary>
    public class PauseScreen : UIScreen
    {
        #region UI Elements
        private Button _loadButton;
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

        public PauseScreen(VisualElement rootElement) : base(rootElement)
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
            // You might want to check this asynchronously in a more complete implementation
            if (_loadButton != null)
            {
                _loadButton.SetEnabled(true); // Always enabled for now, LoadGamePopup will handle empty state
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
            
            // Pause the game when showing pause screen
            PublishPauseEvent();
        }

        protected override void OnHide()
        {
            UnregisterEventHandlers();
        }

        private void RegisterEventHandlers()
        {
            _loadButton?.RegisterCallback<ClickEvent>(OnLoadButtonClicked);
            _optionsButton?.RegisterCallback<ClickEvent>(OnOptionsButtonClicked);
            _mainMenuButton?.RegisterCallback<ClickEvent>(OnMainMenuButtonClicked);
            _quitButton?.RegisterCallback<ClickEvent>(OnQuitButtonClicked);
            _closeButton?.RegisterCallback<ClickEvent>(OnCloseButtonClicked);
        }

        private void UnregisterEventHandlers()
        {
            _loadButton?.UnregisterCallback<ClickEvent>(OnLoadButtonClicked);
            _optionsButton?.UnregisterCallback<ClickEvent>(OnOptionsButtonClicked);
            _mainMenuButton?.UnregisterCallback<ClickEvent>(OnMainMenuButtonClicked);
            _quitButton?.UnregisterCallback<ClickEvent>(OnQuitButtonClicked);
            _closeButton?.UnregisterCallback<ClickEvent>(OnCloseButtonClicked);
        }

        private async void OnCloseButtonClicked(ClickEvent evt)
        {
            await ClosePopup();
        }

        private async void OnLoadButtonClicked(ClickEvent evt)
        {
            Debug.Log("[PauseScreen] Load button clicked");
            await ShowLoadGamePopup();
        }

        private void OnOptionsButtonClicked(ClickEvent evt)
        {
            Debug.Log("[PauseScreen] Options button clicked");
            RequestShowOptions();
        }

        private void OnMainMenuButtonClicked(ClickEvent evt)
        {
            Debug.Log("[PauseScreen] Main Menu button clicked");
            RequestReturnToMainMenu();
        }

        private void OnQuitButtonClicked(ClickEvent evt)
        {
            Debug.Log("[PauseScreen] Quit button clicked");
            RequestQuitGame();
        }
        #endregion

        #region Action Handlers
        private async Task ClosePopup()
        {
            await _uiService?.HidePopupAsync<LoadGamePopup>();
            await _uiService?.HidePopupAsync<OptionsPopup>();

        }

        /// <summary>
        /// Shows the Load Game popup
        /// </summary>
        private async Task ShowLoadGamePopup()
        {
            try
            {
                Debug.Log("[PauseScreen] Opening Load Game popup");
                await _uiService.ShowPopupAsync<LoadGamePopup>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PauseScreen] Error showing Load Game popup: {ex}");
            }
        }

        /// <summary>
        /// Requests to show options screen
        /// </summary>
        private void RequestShowOptions()
        {
            Debug.Log("[PauseScreen] Requesting options screen");
            _eventSystem?.Publish(new OptionsRequestedEvent());
            
        }


        
        /// <summary>
        /// Requests to return to main menu
        /// </summary>
        private void RequestReturnToMainMenu()
        {
            Debug.Log("[PauseScreen] Requesting return to main menu");
            _eventSystem.Publish(new MainMenuRequestedEvent());
        }

        /// <summary>
        /// Requests to quit the game
        /// </summary>
        private void RequestQuitGame()
        {
            Debug.Log("[PauseScreen] Requesting quit game");
            _eventSystem.Publish(new QuitRequestedEvent());
        }

        /// <summary>
        /// Publishes pause event when screen is shown
        /// </summary>
        private void PublishPauseEvent()
        {
            Debug.Log("[PauseScreen] Publishing pause event");
            _eventSystem.Publish(new PauseRequestedEvent());
        }
        #endregion
        
    }
}
