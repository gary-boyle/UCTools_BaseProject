using System;
using GameFramework.EventSystem.Events;
using UCTools_Utilities.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Quit screen - displays shutdown progress and handles graceful application exit
    /// Follows established pattern of publishing events rather than managing state directly
    /// 
    /// Intent: Provide visual feedback during shutdown process and allow cancellation when safe
    /// Design: Event-driven UI component with progress tracking capabilities
    /// Pros: Clear user feedback, graceful shutdown UX, cancellation support
    /// Cons: Requires event system for interactions, complex progress management
    /// </summary>
    public class QuitScreen : UIScreen
    {
        // UI Elements
        private Button _cancelButton;
        
        // Labels
        private Label _shutdownMessageLabel;
        private Label _progressLabel;
        private Label _progressPercentLabel;
        private Label _currentActionLabel;
        
        // Progress Bar Elements
        private VisualElement _progressContainer;
        private VisualElement _progressBar;
        
        // State tracking
        private float _currentProgress = 0f;
        private bool _canCancel = true;
        private bool _isShuttingDown = false;

        /// <summary>
        /// Initialize quit screen with UI element references
        /// </summary>
        public QuitScreen(VisualElement rootElement) : base(rootElement)
        {
            InitializeUI();
            UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.LogWarnings);
        }
        
        /// <summary>
        /// Subscribe to button events when screen is shown
        /// </summary>
        protected override void OnShow()
        {
            _cancelButton?.RegisterCallback<ClickEvent>(OnCancelClicked);
            
            // Add keyboard navigation support
            RegisterKeyboardNavigation();
            
            // Initialize progress display
            ResetProgress();
        }
        
        /// <summary>
        /// Unsubscribe from button events when screen is hidden
        /// </summary>
        protected override void OnHide()
        {
            _cancelButton?.UnregisterCallback<ClickEvent>(OnCancelClicked);
            UnregisterKeyboardNavigation();
        }
        
        /// <summary>
        /// Cache references to UI elements from UXML
        /// </summary>
        private void InitializeUI()
        {
            // Get button reference
            _cancelButton = RootElement?.Q<Button>("btn_Cancel");
            
            // Get label references
            _shutdownMessageLabel = RootElement?.Q<Label>("lbl_ShutdownMessage");
            _progressLabel = RootElement?.Q<Label>("lbl_ProgressLabel");
            _progressPercentLabel = RootElement?.Q<Label>("lbl_ProgressPercent");
            _currentActionLabel = RootElement?.Q<Label>("lbl_CurrentAction");
            
            // Get progress bar elements
            _progressContainer = RootElement?.Q<VisualElement>("progress_Container");
            _progressBar = RootElement?.Q<VisualElement>("progress_Bar");
        }
        
        #region Progress Management
        
        /// <summary>
        /// Update shutdown progress with current status
        /// </summary>
        public void UpdateProgress(float progress, string currentAction, bool canCancel = true)
        {
            _currentProgress = Mathf.Clamp01(progress);
            _canCancel = canCancel;
            
            // Update progress bar visual
            if (_progressBar != null)
            {
                _progressBar.style.width = new StyleLength(new Length(_currentProgress * 100, LengthUnit.Percent));
            }
            
            // Update progress percentage text
            if (_progressPercentLabel != null)
            {
                _progressPercentLabel.text = $"{_currentProgress * 100:F0}%";
            }
            
            // Update current action text
            if (_currentActionLabel != null && !string.IsNullOrEmpty(currentAction))
            {
                _currentActionLabel.text = currentAction;
            }
            
            // Update cancel button state
            UpdateCancelButtonState();
            
            Debug.Log($"[QuitScreen] Progress updated: {_currentProgress * 100:F0}% - {currentAction}");
        }
        
        /// <summary>
        /// Reset progress to initial state
        /// </summary>
        public void ResetProgress()
        {
            UpdateProgress(0f, "Initializing shutdown...", true);
        }
        
        /// <summary>
        /// Mark shutdown as complete
        /// </summary>
        public void MarkShutdownComplete()
        {
            UpdateProgress(1f, "Shutdown complete. Goodbye!", false);
            _isShuttingDown = false;
        }
        
        /// <summary>
        /// Update cancel button based on current state
        /// </summary>
        private void UpdateCancelButtonState()
        {
            if (_cancelButton != null)
            {
                _cancelButton.SetEnabled(_canCancel && !_isShuttingDown);
                _cancelButton.style.opacity = _canCancel ? 1f : 0.5f;
                
                if (!_canCancel)
                {
                    _cancelButton.tooltip = "Cannot cancel during critical shutdown operations";
                }
                else
                {
                    _cancelButton.tooltip = "Cancel shutdown and return to game";
                }
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle cancel button click - attempt to cancel shutdown if safe
        /// </summary>
        private void OnCancelClicked(ClickEvent evt)
        {
            if (_canCancel && !_isShuttingDown)
            {
                Debug.Log("[QuitScreen] Cancel button clicked - requesting shutdown cancellation");
                _eventSystem?.Publish(new MainMenuRequestedEvent()); // Return to main menu
            }
            else
            {
                Debug.Log("[QuitScreen] Cannot cancel shutdown at this time");
            }
        }
        
        #endregion
        
        #region Keyboard Navigation
        
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
        /// Handle keyboard input for navigation and shortcuts
        /// </summary>
        private void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    if (_canCancel && !_isShuttingDown)
                    {
                        Debug.Log("[QuitScreen] Escape pressed - cancelling shutdown");
                        OnCancelClicked(null);
                        evt.StopPropagation();
                    }
                    break;
                    
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                case KeyCode.Space:
                    // Trigger cancel button if focused and available
                    if (evt.target == _cancelButton && _canCancel && !_isShuttingDown)
                    {
                        OnCancelClicked(null);
                        evt.StopPropagation();
                    }
                    break;
            }
        }
        
        #endregion
        
        #region Content Updates
        
        /// <summary>
        /// Update shutdown message text
        /// </summary>
        public void UpdateShutdownMessage(string message)
        {
            if (_shutdownMessageLabel != null && !string.IsNullOrEmpty(message))
            {
                _shutdownMessageLabel.text = message;
            }
        }
        
        /// <summary>
        /// Set shutdown as in progress (disables cancellation during critical operations)
        /// </summary>
        public void SetShuttingDown(bool isShuttingDown)
        {
            _isShuttingDown = isShuttingDown;
            UpdateCancelButtonState();
        }
        
        #endregion
    }
}
