using System;
using GameFramework.Core;
using UnityEngine;
using UnityEngine.UIElements;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.StateMachine.Enum;
using UCTools_Utilities.UI;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Loading screen that displays loading progress and messages
    /// 
    /// INTENT: Real-time loading progress display with event-driven updates
    /// DESIGN: Event-driven architecture using consolidated LoadingProgressEvent
    /// PROS: Single event subscription, clean separation of concerns, responsive UI
    /// CONS: Dependent on EventSystem for all updates
    /// </summary>
    public class LoadingScreen : UIScreen
    {
        // UI Element References
        private Label _loadingTextLabel;
        private Label _loadingMessageLabel;
        private VisualElement _progressContainer;
        private ProgressBar _progressBar;
        
        // Dependencies
        private IEventSystem _eventSystem;
        
        public LoadingScreen(VisualElement rootElement) : base(rootElement)
        {
            _eventSystem = GameManager.GetService<IEventSystem>();

            InitializeUIElements();
            UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.ThrowExceptions);
        }

        /// <summary>
        /// Initialize references to UI elements from UXML
        /// </summary>
        private void InitializeUIElements()
        {
            // Get existing elements from UXML
            _loadingTextLabel = RootElement?.Q<Label>("lbl_LoadingText");
            _progressContainer = RootElement?.Q<VisualElement>(className: "progress-container");
            _progressBar = RootElement?.Q<ProgressBar>(className: "loading-progress");
            // Find the secondary text label (currently shows "Please wait...")
            _loadingMessageLabel = RootElement?.Q<Label>(className: "text--secondary");
            
            // Set initial state
            UpdateLoadingText("Loading...");
            UpdateLoadingMessage("Initializing...");
        }
        

        /// <summary>
        /// Subscribe to loading progress events
        /// </summary>
        private void SubscribeToLoadingEvents()
        {
            _eventSystem = GameManager.GetService<IEventSystem>();

            // Subscribe to consolidated loading events
            _eventSystem.Subscribe<LoadingProgressEvent>(OnLoadingProgress);
            _eventSystem.Subscribe<LoadingMessageChangedEvent>(OnLoadingMessageChanged);
            _eventSystem.Subscribe<LoadingFailedEvent>(OnLoadingFailed);
            _eventSystem.Subscribe<LoadingCompletedEvent>(OnLoadingCompleted);
        }

        /// <summary>
        /// Unsubscribe from loading progress events
        /// </summary>
        private void UnsubscribeFromLoadingEvents()
        {
            if (_eventSystem == null) return;

            _eventSystem.Unsubscribe<LoadingProgressEvent>(OnLoadingProgress);
            _eventSystem.Unsubscribe<LoadingMessageChangedEvent>(OnLoadingMessageChanged);
            _eventSystem.Unsubscribe<LoadingFailedEvent>(OnLoadingFailed);
            _eventSystem.Unsubscribe<LoadingCompletedEvent>(OnLoadingCompleted);
        }

        #region Event Handlers

        /// <summary>
        /// Handles consolidated loading progress events from both LoadService and LoadingState
        /// </summary>
        private void OnLoadingProgress(LoadingProgressEvent evt)
        {
            UpdateProgress(evt.Progress, evt.Message);
        }

        /// <summary>
        /// Handles standalone loading message changed events
        /// </summary>
        private void OnLoadingMessageChanged(LoadingMessageChangedEvent evt)
        {
            UpdateLoadingMessage(evt.Message);
        }

        /// <summary>
        /// Handles loading failed events
        /// </summary>
        private void OnLoadingFailed(LoadingFailedEvent evt)
        {
            ShowError($"Loading failed: {evt.Exception.Message}");
        }

        /// <summary>
        /// Handles loading completed events
        /// </summary>
        private void OnLoadingCompleted(LoadingCompletedEvent evt)
        {
            UpdateProgress(1.0f, "Loading complete!");
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Updates both progress and loading message
        /// </summary>
        public void UpdateProgress(float progress, string message)
        {
            UpdateProgressBar(progress);
            UpdateLoadingMessage(message);
        }

        /// <summary>
        /// Updates just the progress bar value
        /// </summary>
        public void UpdateProgressBar(float progress)
        {
            if (_progressBar != null)
            {
                _progressBar.value = Mathf.Clamp01(progress);
            }
        }

        /// <summary>
        /// Updates the main loading text (the hero text)
        /// </summary>
        public void UpdateLoadingText(string text)
        {
            if (_loadingTextLabel != null)
            {
                _loadingTextLabel.text = text;
            }
        }

        /// <summary>
        /// Updates the loading message (the secondary text)
        /// </summary>
        public void UpdateLoadingMessage(string message)
        {
            if (_loadingMessageLabel != null)
            {
                _loadingMessageLabel.text = message;
            }
        }

        /// <summary>
        /// Shows an error message on the loading screen
        /// </summary>
        public void ShowError(string errorMessage)
        {
            UpdateLoadingText("Loading Failed");
            UpdateLoadingMessage(errorMessage);
            
            // Hide progress bar and spinner on error
            if (_progressContainer != null)
            {
                _progressContainer.style.display = DisplayStyle.None;
            }

            // Add error styling
            _loadingTextLabel?.AddToClassList("text--error");
            _loadingMessageLabel?.AddToClassList("text--error");
            
            Debug.LogError($"[LoadingScreen] Error displayed: {errorMessage}");
        }

        /// <summary>
        /// Sets the loading type for context-specific messaging
        /// </summary>
        public void SetLoadingType(LoadingType loadingType)
        {
            string loadingText = loadingType switch
            {
                LoadingType.NewGame => "Starting New Game...",
                LoadingType.LoadSave => "Loading Game...",
                LoadingType.SceneTransition => "Loading...",
                LoadingType.GameRestart => "Restarting Game...",
                _ => "Loading..."
            };
            
            UpdateLoadingText(loadingText);
        }

        #endregion

        #region Lifecycle

        public override void Show()
        {
            base.Show();
            
            SubscribeToLoadingEvents();
            
            // Reset to initial state when showing
            UpdateProgressBar(0f);
            UpdateLoadingText("Loading...");
            UpdateLoadingMessage("Initializing...");
    
            // Show all elements
            if (_progressContainer != null)
            {
                _progressContainer.style.display = DisplayStyle.Flex;
            }
            
            // Remove any error styling
            _loadingTextLabel?.RemoveFromClassList("text--error");
            _loadingMessageLabel?.RemoveFromClassList("text--error");
        }

        public override void Hide()
        {
            UnsubscribeFromLoadingEvents();
            base.Hide();
        }
        
        #endregion
    }
}
