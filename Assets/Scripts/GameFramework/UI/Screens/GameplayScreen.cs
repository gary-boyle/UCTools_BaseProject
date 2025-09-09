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

        // Update tracking
        private float _lastDebugUpdate = 0f;
        private const float DEBUG_UPDATE_INTERVAL = 0.5f; // Update debug info twice per second

        public GamePlayScreen(VisualElement rootElement) : base(rootElement)
        {
            // Get services from DI container
            _gameDataService = GameManager.GetService<IGameDataService>();
            _saveService = GameManager.GetService<ISaveService>();
            _pauseService = GameManager.GetService<IPauseService>();

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

            //_eventSystem?.Publish(new GameplayTestEvent());
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
            
            // Publish pause event
            //_eventSystem?.Publish(new SaveGameEvent());
            _saveService.PerformRegularSaveAsync();
        }
        #endregion
        
        #region Debug Label Updates
        

        /// <summary>
        /// Updates all debug labels with current game state information
        /// Now respects global pause state - playtime will stop incrementing when paused
        /// </summary>
        protected override void OnUpdate(float deltaTime)
        {
            // Respect global pause state - this stops the playtime counter!
            if (_pauseService.IsPaused)
            {
                return; // Don't update UI when paused - this stops the time counter
            }
    
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
                UpdateDebugLabel4(session); // This will now pause when game is paused
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
        /// Updates debug label 4 with scene and playtime information
        /// Playtime is now updated every frame for smooth display
        /// </summary>
        private void UpdateDebugLabel4(DataStructures.GameSession session)
        {
            // Get real-time playtime (this will now increment smoothly)
            var currentPlayTime = session.GetCurrentPlayTime();
            var playTime = TimeSpan.FromSeconds(currentPlayTime);
            var formattedTime = $"{playTime.Hours:D2}:{playTime.Minutes:D2}:{playTime.Seconds:D2}";
            var text = $"Scene: {session.currentScene} | Time: {formattedTime}";
            SetDebugLabel(_debugLabel4, text);
        }
        
        private void SetDebugLabelsNoSession()
        {
            SetDebugLabel(_debugLabel1, "No Active Session");
            SetDebugLabel(_debugLabel2, "---");
            SetDebugLabel(_debugLabel3, "---");
            SetDebugLabel(_debugLabel4, "---");
        }
        
        private void SetDebugLabelsError()
        {
            SetDebugLabel(_debugLabel1, "Error Loading Data");
            SetDebugLabel(_debugLabel2, "Check Console");
            SetDebugLabel(_debugLabel3, "---");
            SetDebugLabel(_debugLabel4, "---");
        }
        
        private void SetDebugLabel(Label label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }
        
        #endregion
        
        #region Public API for External Updates
        
        /// <summary>
        /// Manually set debug label text (useful for custom debug info)
        /// </summary>
        public void SetDebugLabel1(string text) => SetDebugLabel(_debugLabel1, text);
        public void SetDebugLabel2(string text) => SetDebugLabel(_debugLabel2, text);
        public void SetDebugLabel3(string text) => SetDebugLabel(_debugLabel3, text);
        public void SetDebugLabel4(string text) => SetDebugLabel(_debugLabel4, text);
        
        #endregion
    }
}
