using System;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;
using GameFramework.UI.Screens;
using GameFramework.UI.Popups;
using UnityEngine;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Playing state with internal pause handling via popup overlay
    /// Pause is no longer a separate state - it's a mode within this state
    /// </summary>
    public class PlayingState : BaseGameState
    {
        private bool _isPaused = false;
        private float _prePauseTimeScale = 1f;
        private float _prePauseAudioVolume = 1f;

        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public PlayingState(
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputService inputService,
            IConsoleService consoleService,
            IGameDataService gameDataService)  
            : base(GameStateType.Playing, stateMachine, eventSystem, audioService, uiService, inputService, consoleService, gameDataService)
        {
        }
        
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
            // Show game HUD using injected UI service
            await UIService.ShowScreenAsync<GamePlayScreen>();
            
            // Start gameplay music using injected audio service
            AudioService.PlayMusic("gameplay");
            
            // Ensure time is running (in case we came from another state that modified it)
            Time.timeScale = 1f;
            _isPaused = false;
            
            // Subscribe to pause/resume events
            EventSystem.Subscribe<PauseRequestedEvent>(OnPauseRequested);
            EventSystem.Subscribe<ResumeRequestedEvent>(OnResumeRequested);
            
            // Subscribe to game events
            EventSystem.Subscribe<GameOverEvent>(OnGameOver);
            EventSystem.Subscribe<VictoryEvent>(OnVictory);
            EventSystem.Subscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            
            // Subscribe to input events
            EventSystem.Subscribe<UICancelInputEvent>(OnCancelInput);
            EventSystem.Subscribe<PlayerAttackInputEvent>(OnAttackInput);
            EventSystem.Subscribe<PlayerJumpInputEvent>(OnJumpInput);
            EventSystem.Subscribe<PlayerInteractInputEvent>(OnInteractInput);
            EventSystem.Subscribe<PlayerMoveInputEvent>(OnMoveInput);
            EventSystem.Subscribe<PlayerLookInputEvent>(OnLookInput);
            EventSystem.Subscribe<PlayerSprintInputEvent>(OnSprintInput);
            EventSystem.Subscribe<PlayerCrouchInputEvent>(OnCrouchInput);
            
            // Publish game started event
            EventSystem.Publish<GameStartedEvent>();
            
            Debug.Log("[PlayingState] Entered playing state with pause handling");
        }
        
        #region Pause Management
        
        /// <summary>
        /// Pauses the game and shows pause popup
        /// </summary>
        private async Task PauseGame()
        {
            if (_isPaused) return;
            
            Debug.Log("[PlayingState] Pausing game");
            
            _isPaused = true;
            
            // Store current state
            _prePauseTimeScale = Time.timeScale;
            _prePauseAudioVolume = AudioService.GetMasterVolume();
            
            // Apply pause effects
            Time.timeScale = 0f;
            AudioService.SetMasterVolume(0.3f);
            
            // Show pause popup
            await UIService.ShowPopupAsync<PausePopup>();
            
            // Publish pause event for global systems
            EventSystem.Publish<GamePausedEvent>();
        }
        
        /// <summary>
        /// Resumes the game and hides pause popup
        /// </summary>
        private async Task ResumeGame()
        {
            if (!_isPaused) return;
            
            Debug.Log("[PlayingState] Resuming game");
            
            _isPaused = false;
            
            // Restore previous state
            Time.timeScale = _prePauseTimeScale;
            AudioService.SetMasterVolume(_prePauseAudioVolume);
            
            // Hide pause popup
            await UIService.HidePopupAsync<PausePopup>();
            
            // Publish resume event for global systems
            EventSystem.Publish<GameResumedEvent>();
        }
        
        /// <summary>
        /// Checks if the game is currently paused
        /// </summary>
        public bool IsGamePaused() => _isPaused;
        
        #endregion
        
        #region Event Handlers - Pause/Resume
        
        private async void OnPauseRequested(PauseRequestedEvent evt)
        {
            if (!_isPaused)
            {
                await PauseGame();
            }
        }
        
        private async void OnResumeRequested(ResumeRequestedEvent evt)
        {
            if (_isPaused)
            {
                await ResumeGame();
            }
        }
        
        /// <summary>
        /// Handle cancel/escape input for pause/resume toggle
        /// </summary>
        private async void OnCancelInput(UICancelInputEvent evt)
        {
            if (_isPaused)
            {
                await ResumeGame();
            }
            else
            {
                await PauseGame();
            }
        }
        
        #endregion
        
        #region Event Handlers - Game State Changes
        
        /// <summary>
        /// Handle main menu request while paused
        /// </summary>
        private async void OnMainMenuRequested(MainMenuRequestedEvent evt)
        {
            // Save game before leaving if paused
            if (_isPaused)
            {
                await GameDataService.SaveCurrentSessionAsync("BeforeMainMenu");
            }
            
            await TransitionToStateAsync(GameStateType.MainMenu);
        }
        
        private async void OnGameOver(GameOverEvent evt)
        {
            // Ensure we're not paused before transitioning
            if (_isPaused)
            {
                await ResumeGame();
            }
            
            await TransitionToStateAsync(GameStateType.GameOver);
        }
        
        private async void OnVictory(VictoryEvent evt)
        {
            // Ensure we're not paused before transitioning
            if (_isPaused)
            {
                await ResumeGame();
            }
            
            await TransitionToStateAsync(GameStateType.Victory);
        }
        
        #endregion
        
        #region Input Event Handlers (Only processed when not paused)
        
        /// <summary>
        /// Handle player attack input - only when not paused
        /// </summary>
        private void OnAttackInput(PlayerAttackInputEvent evt)
        {
            if (_isPaused) return;
            
            Debug.Log($"[PlayingState] Player attack input: {evt.Phase}");
            // Handle attack logic or publish attack commands
        }
        
        /// <summary>
        /// Handle player jump input - only when not paused
        /// </summary>
        private void OnJumpInput(PlayerJumpInputEvent evt)
        {
            if (_isPaused) return;
            
            Debug.Log("[PlayingState] Player jump input");
            // Handle jump logic or publish jump commands
        }
        
        /// <summary>
        /// Handle player interact input - only when not paused
        /// </summary>
        private void OnInteractInput(PlayerInteractInputEvent evt)
        {
            if (_isPaused) return;
            
            Debug.Log($"[PlayingState] Player interact input: {evt.Phase}");
            // Handle interact logic or publish interact commands
        }
        
        /// <summary>
        /// Handle player movement input - only when not paused
        /// </summary>
        private void OnMoveInput(PlayerMoveInputEvent evt)
        {
            if (_isPaused) return;
            
            // Handle movement (called frequently)
            // Forward to player controller or publish movement commands
        }
        
        /// <summary>
        /// Handle player look input - only when not paused
        /// </summary>
        private void OnLookInput(PlayerLookInputEvent evt)
        {
            if (_isPaused) return;
            
            // Handle camera/look input (called frequently)
            // Forward to camera controller or publish look commands
        }
        
        /// <summary>
        /// Handle player sprint input - only when not paused
        /// </summary>
        private void OnSprintInput(PlayerSprintInputEvent evt)
        {
            if (_isPaused) return;
            
            Debug.Log($"[PlayingState] Player sprint input: {evt.Phase}");
            // Handle sprint logic or publish sprint commands
        }
        
        /// <summary>
        /// Handle player crouch input - only when not paused
        /// </summary>
        private void OnCrouchInput(PlayerCrouchInputEvent evt)
        {
            if (_isPaused) return;
            
            Debug.Log($"[PlayingState] Player crouch input: {evt.Phase}");
            // Handle crouch logic or publish crouch commands
        }
        
        #endregion
        
        public override void Update()
        {
            // Game logic updates are automatically paused via Time.timeScale = 0
            // But you can add custom pause-aware logic here if needed
        }
        
        public override async Task ExitAsync()
        {
            // Ensure we clean up pause state before leaving
            if (_isPaused)
            {
                await ResumeGame();
            }
            
            // Unsubscribe from all events
            EventSystem.Unsubscribe<PauseRequestedEvent>(OnPauseRequested);
            EventSystem.Unsubscribe<ResumeRequestedEvent>(OnResumeRequested);
            EventSystem.Unsubscribe<GameOverEvent>(OnGameOver);
            EventSystem.Unsubscribe<VictoryEvent>(OnVictory);
            EventSystem.Unsubscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            
            EventSystem.Unsubscribe<UICancelInputEvent>(OnCancelInput);
            EventSystem.Unsubscribe<PlayerAttackInputEvent>(OnAttackInput);
            EventSystem.Unsubscribe<PlayerJumpInputEvent>(OnJumpInput);
            EventSystem.Unsubscribe<PlayerInteractInputEvent>(OnInteractInput);
            EventSystem.Unsubscribe<PlayerMoveInputEvent>(OnMoveInput);
            EventSystem.Unsubscribe<PlayerLookInputEvent>(OnLookInput);
            EventSystem.Unsubscribe<PlayerSprintInputEvent>(OnSprintInput);
            EventSystem.Unsubscribe<PlayerCrouchInputEvent>(OnCrouchInput);
            
            await UIService.HideScreenAsync<GamePlayScreen>();
            
            // Publish game ended event
            EventSystem.Publish<GameEndedEvent>();
            
            await base.ExitAsync();
        }
    }
}
