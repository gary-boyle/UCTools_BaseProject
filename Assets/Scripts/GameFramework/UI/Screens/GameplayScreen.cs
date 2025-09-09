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
    /// Main gameplay screen with HUD elements and debug information display
    /// Provides access to game state information and gameplay controls
    /// Uses TimeService for accurate playtime display with proper pause handling
    /// </summary>
    public class GamePlayScreen : UIScreen
    {
        // UI Elements
        private Button _testButton;
        private Button _pauseButton;
        private Button _saveButton;

        // Debug Labels
        private Label _debugLabel1; // First label (no name in UXML)
        private Label _debugLabel2; // lbl_Debug2
        private Label _debugLabel3; // lbl_Debug3
        private Label _debugLabel4; // lbl_Debug4
        
        // Services
        private IGameDataService _gameDataService;
        private ISaveService _saveService;
        private IPauseService _pauseService;
        private ITimeService _timeService;

        public GamePlayScreen(VisualElement rootElement) : base(rootElement)
        {
            // Get services from DI container
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

            // Set initial button states
            SetupButtonStates();
        }
        
        private void SetupButtonStates()
        {
        }
        
        #region Button Event Handlers
        
        private void OnTestButtonClicked(ClickEvent evt)
        {
            Debug.Log("[GamePlayScreen] Test button clicked");
            
            // Publish test event for other systems
            _saveService.PerformAutoSaveAsync();
        }
        
        private void OnPauseButtonClicked(ClickEvent evt)
        {
            Debug.Log("[GamePlayScreen] Pause button clicked");
            
            // Publish pause event
            _eventSystem?.Publish(new PauseRequestedEvent());
        }
        
        private void OnSaveButtonClicked(ClickEvent evt)
        {
            Debug.Log("[GamePlayScreen] Save button clicked");
            
            // Perform regular save
            _saveService.PerformRegularSaveAsync();
        }
        #endregion
        
        #region Debug Label Updates
        
        /// <summary>
        /// Updates all debug labels with current game state information
        /// Uses TimeService for accurate playtime display that automatically pauses
        /// </summary>
        protected override void OnUpdate(float deltaTime)
        {
            // TimeService automatically handles pause state - no need to check here
            // The playtime will pause automatically when game is paused
            
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
        
                // Update each label with different information
                UpdateDebugLabel1(session, playerState);
                UpdateDebugLabel2(playerState);
                UpdateDebugLabel3(progress);
                UpdateDebugLabel4WithTimeService(); // Now uses TimeService directly
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
        
        /// <summary>
        /// Updates debug label 4 with scene and playtime from TimeService
        /// TimeService automatically handles pause state - no manual checking needed
        /// </summary>
        private void UpdateDebugLabel4WithTimeService()
        {
            var session = _gameDataService.CurrentSession;
            
            if (_timeService != null && session != null)
            {
                // Get real-time playtime from TimeService (automatically pauses when game is paused)
                var formattedGameTime = _timeService.GetFormattedGameTime();
                var formattedSessionTime = _timeService.GetFormattedSessionTime();
                var isTracking = _timeService.IsTrackingGameTime;
                
                // Show both game time and session time, with tracking indicator
                var trackingIndicator = isTracking ? "⏱️" : "⏸️";
                var text = $"Scene: {session.currentScene} | Game: {formattedGameTime} | Session: {formattedSessionTime} {trackingIndicator}";
                
                SetDebugLabel(_debugLabel4, text);
            }
            else
            {
                // Fallback to session-based time if TimeService unavailable
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
            
            // Show TimeService status even without session
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
            
            // Show TimeService status on error
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
