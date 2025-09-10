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
        // UI Elements
        private Button _testButton;
        private Button _pauseButton;
        private Button _saveButton;

        // Debug Labels
        private Label _debugLabel1;
        private Label _debugLabel2;
        private Label _debugLabel3;
        private Label _debugLabel4;
        
        // Services for data display
        private IGameDataService _gameDataService;
        private ISaveService _saveService;
        private IPauseService _pauseService;
        private ITimeService _timeService;

        public GamePlayScreen(VisualElement rootElement) : base(rootElement)
        {
            // Get services for data display
            _gameDataService = GameManager.GetService<IGameDataService>();
            _saveService = GameManager.GetService<ISaveService>();
            _pauseService = GameManager.GetService<IPauseService>();
            _timeService = GameManager.GetService<ITimeService>();

            EnableFrameUpdates();
            InitializeUI();
            UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.ThrowExceptions);
        }
        
        protected override void OnShow()
        {
            // Subscribe to button events
            _testButton?.RegisterCallback<ClickEvent>(OnTestButtonClicked);
            _pauseButton?.RegisterCallback<ClickEvent>(OnPauseButtonClicked);
            _saveButton?.RegisterCallback<ClickEvent>(OnSaveButtonClicked);
        }
        
        protected override void OnHide()
        {
            // Clean up event subscriptions
            _testButton?.UnregisterCallback<ClickEvent>(OnTestButtonClicked);
            _pauseButton?.UnregisterCallback<ClickEvent>(OnPauseButtonClicked);
            _saveButton?.UnregisterCallback<ClickEvent>(OnSaveButtonClicked);
        }
        
        private void InitializeUI()
        {
            // Get button references
            _testButton = RootElement?.Q<Button>("btn_Test");
            _pauseButton = RootElement?.Q<Button>("btn_Pause");
            _saveButton = RootElement?.Q<Button>("btn_Save");

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
            Debug.Log("[GamePlayScreen] Test button clicked - performing auto-save");
            _saveService?.PerformAutoSaveAsync();
        }
        
        /// <summary>
        /// Report pause request - state will handle popup management
        /// </summary>
        private void OnPauseButtonClicked(ClickEvent evt)
        {
            Debug.Log("[GamePlayScreen] Pause button clicked - reporting to state");
            _eventSystem?.Publish(new PauseRequestedEvent());
        }
        
        /// <summary>
        /// Report save action - doesn't control UI, just performs action
        /// </summary>
        private void OnSaveButtonClicked(ClickEvent evt)
        {
            Debug.Log("[GamePlayScreen] Save button clicked - performing regular save");
            _saveService?.PerformRegularSaveAsync();
        }
        
        #endregion
        
        #region Debug Label Updates - Pure Data Display
        
        /// <summary>
        /// Updates debug labels with current game state - pure data display function
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
                var playerState = _gameDataService.GetPlayerState();
                var progress = _gameDataService.GetGameProgress();
        
                UpdateDebugLabel1(session, playerState);
                UpdateDebugLabel2(playerState);
                UpdateDebugLabel3(progress);
                UpdateDebugLabel4WithTimeService();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GamePlayScreen] Error updating debug labels: {e.Message}");
                SetDebugLabelsError();
            }
        }
        
        private void UpdateDebugLabel1(DataStructures.GameSession session, 
                                     DataStructures.PlayerState playerState)
        {
            var text = $"Player: {session.playerName} | Level: {playerState.level}";
            SetDebugLabel(_debugLabel1, text);
        }
        
        private void UpdateDebugLabel2(DataStructures.PlayerState playerState)
        {
            var text = $"Health: {playerState.health}/{playerState.maxHealth}";
            SetDebugLabel(_debugLabel2, text);
        }
        
        private void UpdateDebugLabel3(DataStructures.GameProgress progress)
        {
            var completedLevels = progress.completedLevels.Count;
            var text = $"Score: {progress.score} | Levels: {completedLevels}";
            SetDebugLabel(_debugLabel3, text);
        }
        
        private void UpdateDebugLabel4WithTimeService()
        {
            var session = _gameDataService.CurrentSession;
            
            if (_timeService != null && session != null)
            {
                var formattedGameTime = _timeService.GetFormattedGameTime();
                var formattedSessionTime = _timeService.GetFormattedSessionTime();
                var isTracking = _timeService.IsTrackingGameTime;
                
                var trackingIndicator = isTracking ? "⏱️" : "⏸️";
                var text = $"Scene: {session.currentScene} | Game: {formattedGameTime} | Session: {formattedSessionTime} {trackingIndicator}";
                
                SetDebugLabel(_debugLabel4, text);
            }
            else
            {
                var currentPlayTime = session?.TotalPlayTimeSeconds ?? 0f;
                var playTime = TimeSpan.FromSeconds(currentPlayTime);
                var formattedTime = $"{playTime.Hours:D2}:{playTime.Minutes:D2}:{playTime.Seconds:D2}";
                var text = $"Scene: {session?.currentScene ?? "Unknown"} | Time: {formattedTime} (Fallback)";
                
                SetDebugLabel(_debugLabel4, text);
            }
        }
        
        private void SetDebugLabelsNoSession()
        {
            SetDebugLabel(_debugLabel1, "No Active Session");
            SetDebugLabel(_debugLabel2, "---");
            SetDebugLabel(_debugLabel3, "---");
            
            if (_timeService != null)
            {
                var sessionTime = _timeService.GetFormattedSessionTime();
                SetDebugLabel(_debugLabel4, $"Session Time: {sessionTime} (No Game Session)");
            }
            else
            {
                SetDebugLabel(_debugLabel4, "TimeService Unavailable");
            }
        }
        
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
