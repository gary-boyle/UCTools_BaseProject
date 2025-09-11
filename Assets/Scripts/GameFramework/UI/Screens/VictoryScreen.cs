using GameFramework.EventSystem.Events;
using UCTools_Utilities.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Victory screen - displays victory celebration and provides player options
    /// Follows established pattern of publishing events rather than managing state directly
    /// 
    /// Intent: Celebrate player victory and provide options for continued play
    /// Design: Event-driven UI component with clear separation of concerns
    /// Pros: Maintainable, testable, follows established patterns, celebratory UX
    /// Cons: Requires event system for interactions
    /// </summary>
    public class VictoryScreen : UIScreen
    {
        // UI Elements
        private Button _playAgainButton;
        private Button _mainMenuButton;
        
        // Labels for potential dynamic content updates
        private Label _victoryLabel;
        private Label _wellDoneLabel;
        private Label _congratulationsLabel;

        /// <summary>
        /// Initialize victory screen with UI element references
        /// </summary>
        public VictoryScreen(VisualElement rootElement) : base(rootElement)
        {
            InitializeUI();
            UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.LogWarnings);
        }
        
        /// <summary>
        /// Subscribe to button events when screen is shown
        /// </summary>
        protected override void OnShow()
        {
            _playAgainButton?.RegisterCallback<ClickEvent>(OnPlayAgainClicked);
            _mainMenuButton?.RegisterCallback<ClickEvent>(OnMainMenuClicked);
            
            // Optional: Add celebration effects
            StartCelebrationEffects();
        }
        
        /// <summary>
        /// Unsubscribe from button events when screen is hidden
        /// </summary>
        protected override void OnHide()
        {
            _playAgainButton?.UnregisterCallback<ClickEvent>(OnPlayAgainClicked);
            _mainMenuButton?.UnregisterCallback<ClickEvent>(OnMainMenuClicked);
            
            StopCelebrationEffects();
        }
        
        /// <summary>
        /// Cache references to UI elements from UXML
        /// </summary>
        private void InitializeUI()
        {
            // Get button references
            _playAgainButton = RootElement?.Q<Button>("btn_PlayAgain");
            _mainMenuButton = RootElement?.Q<Button>("btn_MainMenu");
            
            // Cache label references for potential dynamic updates
            _victoryLabel = RootElement?.Q<Label>(className: "text--hero");
            _wellDoneLabel = RootElement?.Q<Label>(className: "text--display");
            _congratulationsLabel = RootElement?.Q<Label>(className: "text--xl");
            
            // Set initial focus to Play Again button for better UX
            _playAgainButton?.Focus();
        }
        
        #region Button Event Handlers
        
        /// <summary>
        /// Handle play again button click - start a new game
        /// </summary>
        private void OnPlayAgainClicked(ClickEvent evt)
        {
            _eventSystem?.Publish(new NewGameRequestedEvent());
        }
        
        /// <summary>
        /// Handle main menu button click - return to main menu
        /// </summary>
        private void OnMainMenuClicked(ClickEvent evt)
        {
            _eventSystem?.Publish(new MainMenuRequestedEvent());
        }
        
        #endregion
        
        #region Celebration Effects
        
        /// <summary>
        /// Start visual celebration effects (placeholder for future animation system)
        /// </summary>
        private void StartCelebrationEffects()
        {
            // Placeholder for future particle effects, animations, etc.
            Debug.Log("[VictoryScreen] Starting celebration effects");
            
            // Could add:
            // - Particle effects
            // - Screen animations
            // - UI element scaling/pulsing
            // - Color transitions
        }
        
        /// <summary>
        /// Stop celebration effects
        /// </summary>
        private void StopCelebrationEffects()
        {
            // Placeholder for stopping celebration effects
            Debug.Log("[VictoryScreen] Stopping celebration effects");
        }
        
        #endregion
    }
}
