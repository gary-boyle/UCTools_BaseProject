using GameFramework.EventSystem.Events;
using UCTools_Utilities.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Credits screen - displays game credits and handles user interactions
    /// Follows established pattern of publishing events rather than managing state directly
    /// 
    /// Intent: Display game credits and provide navigation back to main menu
    /// Design: Event-driven UI component with clear separation of concerns
    /// Pros: Maintainable, testable, follows established patterns
    /// Cons: Requires event system for interactions
    /// </summary>
    public class CreditsScreen : UIScreen
    {
        // UI Elements
        private Button _backToMenuButton;
        
        // Labels for potential dynamic content updates
        private Label _titleLabel;
        private Label _gameDevLabel;
        private Label _madeByLabel;
        private Label _specialThanksLabel;
        private Label _unityLabel;
        private Label _communityLabel;

        /// <summary>
        /// Initialize credits screen with UI element references
        /// </summary>
        public CreditsScreen(VisualElement rootElement) : base(rootElement)
        {
            InitializeUI();
            UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.LogWarnings);
        }
        
        /// <summary>
        /// Subscribe to button events when screen is shown
        /// </summary>
        protected override void OnShow()
        {
            _backToMenuButton?.RegisterCallback<ClickEvent>(OnBackToMenuClicked);
            
            // Optional: Add keyboard navigation support
            RegisterKeyboardNavigation();
        }
        
        /// <summary>
        /// Unsubscribe from button events when screen is hidden
        /// </summary>
        protected override void OnHide()
        {
            _backToMenuButton?.UnregisterCallback<ClickEvent>(OnBackToMenuClicked);
            UnregisterKeyboardNavigation();
        }
        
        /// <summary>
        /// Cache references to UI elements from UXML
        /// </summary>
        private void InitializeUI()
        {
            // Get button reference
            _backToMenuButton = RootElement?.Q<Button>("btn_BackToMenu");
            
            // Cache label references for potential future dynamic updates
            _titleLabel = RootElement?.Q<Label>(className: "text--hero");
            _gameDevLabel = RootElement?.Q<Label>(className: "text--display");
            _madeByLabel = RootElement?.Q<Label>(className: "text--xl");
            _specialThanksLabel = RootElement?.Q<Label>(className: "text--lg");
            
            // Could cache other labels if needed for dynamic content
            var allLabels = RootElement?.Query<Label>(className: "text").ToList();
            if (allLabels != null && allLabels.Count >= 2)
            {
                _unityLabel = allLabels[0];
                _communityLabel = allLabels[1];
            }
            
            // Ensure back button has focus for keyboard navigation
            _backToMenuButton?.Focus();
        }
        
        /// <summary>
        /// Handle back to menu button click - publish event for state to handle
        /// </summary>
        private void OnBackToMenuClicked(ClickEvent evt)
        {
            Debug.Log("[CreditsScreen] Back to Menu button clicked - requesting main menu");
            _eventSystem?.Publish(new MainMenuRequestedEvent());
        }
        
        /// <summary>
        /// Register keyboard navigation for accessibility
        /// </summary>
        private void RegisterKeyboardNavigation()
        {
            if (RootElement != null)
            {
                RootElement.RegisterCallback<KeyDownEvent>(OnKeyDown);
            }
        }
        
        /// <summary>
        /// Unregister keyboard navigation
        /// </summary>
        private void UnregisterKeyboardNavigation()
        {
            if (RootElement != null)
            {
                RootElement.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            }
        }
        
        /// <summary>
        /// Handle keyboard input for navigation
        /// </summary>
        private void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                case KeyCode.Backspace:
                    Debug.Log("[CreditsScreen] Escape/Backspace pressed - requesting main menu");
                    _eventSystem?.Publish(new MainMenuRequestedEvent());
                    evt.StopPropagation();
                    break;
                    
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                case KeyCode.Space:
                    // Trigger back button if focused
                    if (_backToMenuButton != null && _backToMenuButton == evt.target)
                    {
                        OnBackToMenuClicked(null);
                        evt.StopPropagation();
                    }
                    break;
            }
        }
    }
}
