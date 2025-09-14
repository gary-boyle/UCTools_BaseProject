using System;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.Services;
using GameFramework.Services.Interfaces;
using UCTools_Utilities.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.UI.Screens
{
    /// <summary>
    /// Game play screen - pure UI component that reports user interactions and displays game data
    /// Does not handle its own lifecycle - that's managed by the PlayingState
    /// </summary>
    public class GamePlayScreen : UIScreen
    {
        // UI Elements - Core Gameplay
        private Button _testButton;
        private Button _pauseButton;
        private Button _saveButton;

        // UI Elements - State Transition Buttons (for testing/development)
        private Button _victoryButton;
        private Button _gameOverButton;

        // Debug Labels
        private Label _debugLabel1;
        private Label _debugLabel2;
        private Label _debugLabel3;
        private Label _debugLabel4;
        
        // Services for data display
        private IGameDataService _gameDataService;
        private ITimeService _timeService;

        /// <summary>
        /// Initialize screen with dependency injection of services
        /// </summary>
        public GamePlayScreen(VisualElement rootElement) : base(rootElement)
        {
            // Get services for data display
            _gameDataService = GameManager.GetService<IGameDataService>();
            _timeService = GameManager.GetService<ITimeService>();

            EnableFrameUpdates();
            InitializeUI();
            UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.ThrowExceptions);
        }
        
        /// <summary>
        /// Subscribe to all button events when screen is shown
        /// </summary>
        protected override void OnShow()
        {
            // Core gameplay buttons
            _testButton?.RegisterCallback<ClickEvent>(OnTestButtonClicked);
            _pauseButton?.RegisterCallback<ClickEvent>(OnPauseButtonClicked);
            _saveButton?.RegisterCallback<ClickEvent>(OnSaveButtonClicked);

            // State transition buttons (for testing/development)
            _victoryButton?.RegisterCallback<ClickEvent>(OnVictoryButtonClicked);
            _gameOverButton?.RegisterCallback<ClickEvent>(OnGameOverButtonClicked);
        }
        
        /// <summary>
        /// Unsubscribe from all button events when screen is hidden
        /// </summary>
        protected override void OnHide()
        {
            // Core gameplay buttons
            _testButton?.UnregisterCallback<ClickEvent>(OnTestButtonClicked);
            _pauseButton?.UnregisterCallback<ClickEvent>(OnPauseButtonClicked);
            _saveButton?.UnregisterCallback<ClickEvent>(OnSaveButtonClicked);

            // State transition buttons
            _victoryButton?.UnregisterCallback<ClickEvent>(OnVictoryButtonClicked);
            _gameOverButton?.UnregisterCallback<ClickEvent>(OnGameOverButtonClicked);
        }
        
        /// <summary>
        /// Cache references to all UI elements from the UXML
        /// </summary>
        private void InitializeUI()
        {
            // Core gameplay buttons
            _testButton = RootElement?.Q<Button>("btn_Test");
            _pauseButton = RootElement?.Q<Button>("btn_Pause");
            _saveButton = RootElement?.Q<Button>("btn_Save");

            // State transition buttons
            _victoryButton = RootElement?.Q<Button>("btn_Victory");
            _gameOverButton = RootElement?.Q<Button>("btn_GameOver");

            // Debug labels
            _debugLabel1 ??= RootElement?.Q<Label>("lbl_Debug1");
            _debugLabel2 ??= RootElement?.Q<Label>("lbl_Debug2");
            _debugLabel3 ??= RootElement?.Q<Label>("lbl_Debug3");  
            _debugLabel4 ??= RootElement?.Q<Label>("lbl_Debug4");
        }
        
        #region Button Event Handlers - Only Report User Interactions
        
        /// <summary>
        /// Report test action - doesn't control UI transitions, just performs action
        /// </summary>
        private void OnTestButtonClicked(ClickEvent evt)
        {
            _eventSystem.Publish(SaveRequestedEvent.CreateAutoSave());
        }
        
        /// <summary>
        /// Report pause request - state will handle popup management
        /// </summary>
        private void OnPauseButtonClicked(ClickEvent evt)
        {
            _eventSystem?.Publish(new PauseRequestedEvent());
        }
        
        /// <summary>
        /// Report save action - doesn't control UI, just performs action
        /// </summary>
        private void OnSaveButtonClicked(ClickEvent evt)
        {
            _eventSystem.Publish(SaveRequestedEvent.CreateRegularSave());
        }

        /// <summary>
        /// Report victory condition - state will handle transition to VictoryState
        /// </summary>
        private void OnVictoryButtonClicked(ClickEvent evt)
        {
            _eventSystem?.Publish(new VictoryEvent());
        }

        /// <summary>
        /// Report game over condition - state will handle transition to GameOverState
        /// </summary>
        private void OnGameOverButtonClicked(ClickEvent evt)
        {
            _eventSystem?.Publish(new GameOverEvent());
        }   

        #endregion
        
        #region Debug Label Updates - Pure Data Display
        
        /// <summary>
        /// Updates debug labels with current game state - pure data display function
        /// Called every frame while screen is active
        /// </summary>
        protected override void OnUpdate(float deltaTime)
        {
            if (!_gameDataService.HasActiveSession())
            {
                SetDebugLabelsNoSession();
                return;
            }
            
            try
            {
                var session = _gameDataService.CurrentSession;
        
                UpdateDebugLabel1(session);
                //UpdateDebugLabel2(playerState);
                //UpdateDebugLabel3(progress);
                UpdateDebugLabel4WithTimeService();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GamePlayScreen] Error updating debug labels: {e.Message}");
                SetDebugLabelsError();
            }
        }
        
        /// <summary>
        /// Update first debug label with player and level information
        /// </summary>
        private void UpdateDebugLabel1(DataStructures.GameSession session)
        {
            var text = $"Player: {session.PlayerName}";
            SetDebugLabel(_debugLabel1, text);
        }
        
        /// <summary>
        /// Update second debug label with health information
        /// </summary>
        // private void UpdateDebugLabel2(DataStructures.PlayerState playerState)
        // {
        //     var text = $"Health: {playerState.Health}/{playerState.MaxHealth}";
        //     SetDebugLabel(_debugLabel2, text);
        // }
        
        /// <summary>
        /// Update third debug label with score and progress information
        /// </summary>
        // private void UpdateDebugLabel3()
        // {
        // }
        
        /// <summary>
        /// Update fourth debug label with time and scene information
        /// Uses TimeService if available, falls back to session data
        /// </summary>
        private void UpdateDebugLabel4WithTimeService()
        {
            var session = _gameDataService.CurrentSession;
            
            if (_timeService != null && session != null)
            {
                var formattedGameTime = _timeService.GetFormattedGameTime();
                var isTracking = _timeService.IsTrackingGameTime;
                
                var trackingIndicator = isTracking ? "⏱️" : "⏸️";
                var text = $"Scene: {session.CurrentScene} | Game: {formattedGameTime} | {trackingIndicator}";
                
                SetDebugLabel(_debugLabel4, text);
            }
            else
            {
                var currentPlayTime = session?.SavedGameTime ?? 0f;
                var playTime = TimeSpan.FromSeconds(currentPlayTime);
                var formattedTime = $"{playTime.Hours:D2}:{playTime.Minutes:D2}:{playTime.Seconds:D2}";
                var text = $"Scene: {session?.CurrentScene ?? "Unknown"} | Time: {formattedTime} (Fallback)";
                
                SetDebugLabel(_debugLabel4, text);
            }
        }
        
        /// <summary>
        /// Set debug labels when no active game session exists
        /// </summary>
        private void SetDebugLabelsNoSession()
        {
            SetDebugLabel(_debugLabel1, "No Active Session");
            SetDebugLabel(_debugLabel2, "---");
            SetDebugLabel(_debugLabel3, "---");
            SetDebugLabel(_debugLabel4, "---");

        }
        
        /// <summary>
        /// Set debug labels when an error occurs loading game data
        /// </summary>
        private void SetDebugLabelsError()
        {
            SetDebugLabel(_debugLabel1, "Error Loading Data");
            SetDebugLabel(_debugLabel2, "Check Console");
            SetDebugLabel(_debugLabel3, "---");
            
            if (_timeService != null)
            {
                var timeStats = _timeService.GetTimeStatistics();
                SetDebugLabel(_debugLabel4, $"TimeService: {timeStats.IsTrackingGameTime} | {_timeService.GetFormattedGameTime()}");
            }
            else
            {
                SetDebugLabel(_debugLabel4, "TimeService Error");
            }
        }
        
        /// <summary>
        /// Safely set text on a label if it exists
        /// </summary>
        private void SetDebugLabel(Label label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }
        
        #endregion
    }
}
