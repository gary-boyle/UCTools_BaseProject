using GameFramework.EventSystem.Events;
using UCTools_Utilities.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Game Over screen - displays game over message and provides player options
    /// Follows established pattern of publishing events rather than managing state directly
    /// </summary>
    public class GameOverScreen : UIScreen
    {
        // UI Elements
        private Button _tryAgainButton;
        private Button _loadGameButton;
        private Button _mainMenuButton;
        
        // Labels for potential dynamic content updates
        private Label _gameOverLabel;
        private Label _messageLabel;

        /// <summary>
        /// Initialize game over screen with UI element references
        /// </summary>
        public GameOverScreen(VisualElement rootElement) : base(rootElement)
        {
            InitializeUI();
            UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.LogWarnings);
        }
        
        /// <summary>
        /// Subscribe to button events when screen is shown
        /// </summary>
        protected override void OnShow()
        {
            _tryAgainButton?.RegisterCallback<ClickEvent>(OnTryAgainClicked);
            _loadGameButton?.RegisterCallback<ClickEvent>(OnLoadGameClicked);
            _mainMenuButton?.RegisterCallback<ClickEvent>(OnMainMenuClicked);
        }
        
        /// <summary>
        /// Unsubscribe from button events when screen is hidden
        /// </summary>
        protected override void OnHide()
        {
            _tryAgainButton?.UnregisterCallback<ClickEvent>(OnTryAgainClicked);
            _loadGameButton?.UnregisterCallback<ClickEvent>(OnLoadGameClicked);
            _mainMenuButton?.UnregisterCallback<ClickEvent>(OnMainMenuClicked);
        }
        
        /// <summary>
        /// Cache references to UI elements from UXML
        /// </summary>
        private void InitializeUI()
        {
            // Get button references
            _tryAgainButton = RootElement?.Q<Button>("btn_TryAgain");
            _loadGameButton = RootElement?.Q<Button>("btn_LoadGame");
            _mainMenuButton = RootElement?.Q<Button>("btn_MainMenu");
            
            // Cache label references for potential dynamic updates
            _gameOverLabel = RootElement?.Q<Label>(className: "text--hero");
            _messageLabel = RootElement?.Q<Label>(className: "text--xl");
            
            // Set initial focus to Try Again button for better UX
            _tryAgainButton?.Focus();
        }
        
        #region Button Event Handlers
        
        /// <summary>
        /// Handle try again button click - start a new game with current player settings
        /// </summary>
        private void OnTryAgainClicked(ClickEvent evt)
        {
            _eventSystem?.Publish(new NewGameRequestedEvent());
        }
        
        /// <summary>
        /// Handle load game button click - show load game interface
        /// </summary>
        private void OnLoadGameClicked(ClickEvent evt)
        {
            _eventSystem?.Publish(new LoadWindowRequestedEvent());
        }
        
        /// <summary>
        /// Handle main menu button click - return to main menu
        /// </summary>
        private void OnMainMenuClicked(ClickEvent evt)
        {
            _eventSystem?.Publish(new MainMenuRequestedEvent());
        }
        
        #endregion
    }
}
