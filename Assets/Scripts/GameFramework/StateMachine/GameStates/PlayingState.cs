using System;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.DataStructures;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Data;
using GameFramework.StateMachine.Enum;
using GameFramework.UI.Screens;
using GameFramework.UI.Popups;
using UnityEngine;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Playing state with internal pause handling via popup overlay
    /// Uses PauseService as single source of truth for pause state
    /// </summary>
    public class PlayingState : BaseGameState
    {
        #region Private Fields
        private bool _isTransitioning = false;
        private readonly IPauseService _pauseService;
        #endregion

        #region Constructor
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
            IGameDataService gameDataService,
            IPauseService pauseService)  // NEW: Added PauseService dependency
            : base(GameStateType.Playing, stateMachine, eventSystem, audioService, uiService, inputService, consoleService, gameDataService)
        {
            _pauseService = pauseService ?? throw new ArgumentNullException(nameof(pauseService));
        }
        #endregion
        
        #region State Lifecycle
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
            Debug.Log("[PlayingState] Entering playing state...");
            
            // ✅ SIMPLIFIED: Reset state flags when entering
            _isTransitioning = false;
            
            // ✅ Ensure game is not paused using PauseService
            _pauseService.ResumeGame();
            
            // ✅ Ensure all popups are closed first
            await CloseAllPopups();
            
            // Check if we're resuming from a loaded session
            var gameSession = GameDataService.CurrentSession;
            if (gameSession != null)
            {
                Debug.Log($"[PlayingState] Resuming game for {gameSession.playerName} at {gameSession.currentScene}");
                await RestorePlayerStateFromSession(gameSession);
            }
            
            // Show game HUD
            await UIService.ShowScreenAsync<GamePlayScreen>();
            
            // Start gameplay music
            AudioService.PlayMusic("gameplay");
            
            // ✅ CRITICAL: Always subscribe to events on enter
            SubscribeToEvents();
            
            // Publish game started event
            EventSystem.Publish(new GameStartedEvent());
            
            Debug.Log("[PlayingState] Successfully entered playing state - pause should work");
        }

        public override async Task ExitAsync()
        {
            Debug.Log("[PlayingState] Exiting playing state...");
            
            _isTransitioning = true;
            
            // CRITICAL: Always unsubscribe on exit
            UnsubscribeFromEvents();
            
            // Clean up pause state using PauseService
            if (_pauseService.IsPaused)
            {
                Debug.Log("[PlayingState] Cleaning up pause state before exit");
                _pauseService.ResumeGame(); // This will restore time scale and audio
            }
            
            // Hide UI
            await UIService.HideScreenAsync<GamePlayScreen>();
            await CloseAllPopups();
            
            // Publish game ended event
            EventSystem.Publish(new GameEndedEvent());
            
            Debug.Log("[PlayingState] Successfully exited playing state");
            
            await base.ExitAsync();
        }

        public override void Update()
        {
            // Only update session if game is not paused and not transitioning
            if (!_pauseService.IsPaused && !_isTransitioning && GameDataService.HasActiveSession())
            {
                // GameDataService.Update() is called by the framework automatically
                // Individual game systems should check _pauseService.IsPaused for their own logic
            }
        }
        #endregion

        #region Event Management
        private void SubscribeToEvents()
        {
            Debug.Log("[PlayingState] Subscribing to events...");
            
            if (EventSystem == null)
            {
                Debug.LogError("[PlayingState] EventSystem is null - cannot subscribe to events!");
                return;
            }
            
            try
            {
                // Pause/Resume events - MOST IMPORTANT
                EventSystem.Subscribe<PauseRequestedEvent>(OnPauseRequested);
                EventSystem.Subscribe<ResumeRequestedEvent>(OnResumeRequested);
                
                // Game state events
                EventSystem.Subscribe<GameOverEvent>(OnGameOver);
                EventSystem.Subscribe<VictoryEvent>(OnVictory);
                EventSystem.Subscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
                
                // Input events
                EventSystem.Subscribe<UICancelInputEvent>(OnCancelInput);
                EventSystem.Subscribe<PlayerPauseInputEvent>(OnPlayerPauseInput);
                EventSystem.Subscribe<PlayerAttackInputEvent>(OnAttackInput);
                EventSystem.Subscribe<PlayerJumpInputEvent>(OnJumpInput);
                EventSystem.Subscribe<PlayerInteractInputEvent>(OnInteractInput);
                EventSystem.Subscribe<PlayerMoveInputEvent>(OnMoveInput);
                EventSystem.Subscribe<PlayerLookInputEvent>(OnLookInput);
                EventSystem.Subscribe<PlayerSprintInputEvent>(OnSprintInput);
                EventSystem.Subscribe<PlayerCrouchInputEvent>(OnCrouchInput);
                EventSystem.Subscribe<PlayerNextInputEvent>(OnNextInput);
                EventSystem.Subscribe<PlayerPreviousInputEvent>(OnPreviousInput);
                
                Debug.Log("[PlayingState] Event subscriptions complete - pause should work now");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayingState] Error subscribing to events: {ex}");
            }
        }

        private void UnsubscribeFromEvents()
        {
            Debug.Log("[PlayingState] Unsubscribing from events...");
            
            if (EventSystem == null)
            {
                Debug.LogWarning("[PlayingState] EventSystem is null during unsubscribe");
                return;
            }
            
            try
            {
                // Pause/Resume events
                EventSystem.Unsubscribe<PauseRequestedEvent>(OnPauseRequested);
                EventSystem.Unsubscribe<ResumeRequestedEvent>(OnResumeRequested);
                
                // Game state events
                EventSystem.Unsubscribe<GameOverEvent>(OnGameOver);
                EventSystem.Unsubscribe<VictoryEvent>(OnVictory);
                EventSystem.Unsubscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
                
                // Input events
                EventSystem.Unsubscribe<UICancelInputEvent>(OnCancelInput);
                EventSystem.Unsubscribe<PlayerPauseInputEvent>(OnPlayerPauseInput);
                EventSystem.Unsubscribe<PlayerAttackInputEvent>(OnAttackInput);
                EventSystem.Unsubscribe<PlayerJumpInputEvent>(OnJumpInput);
                EventSystem.Unsubscribe<PlayerInteractInputEvent>(OnInteractInput);
                EventSystem.Unsubscribe<PlayerMoveInputEvent>(OnMoveInput);
                EventSystem.Unsubscribe<PlayerLookInputEvent>(OnLookInput);
                EventSystem.Unsubscribe<PlayerSprintInputEvent>(OnSprintInput);
                EventSystem.Unsubscribe<PlayerCrouchInputEvent>(OnCrouchInput);
                EventSystem.Unsubscribe<PlayerNextInputEvent>(OnNextInput);
                EventSystem.Unsubscribe<PlayerPreviousInputEvent>(OnPreviousInput);
                
                Debug.Log("[PlayingState] Event unsubscriptions complete");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayingState] Error unsubscribing from events: {ex}");
            }
        }
        #endregion
        
        #region Pause Management - SIMPLIFIED WITH PAUSESERVICE!
        /// <summary>
        /// Pauses the game and shows pause popup
        /// Uses PauseService for all pause logic
        /// </summary>
        private async Task PauseGame()
        {
            if (_pauseService.IsPaused || _isTransitioning) return;
            
            Debug.Log("[PlayingState] Pausing game");
            
            // Use PauseService to handle all pause logic (time scale, audio, events)
            _pauseService.PauseGame("Player paused via PlayingState");
            
            try
            {
                // Show pause popup
                await UIService.ShowPopupAsync<PausePopup>();
                Debug.Log("[PlayingState] Pause popup shown");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayingState] Error showing pause popup: {ex}");
            }
            
            Debug.Log("[PlayingState] Game successfully paused");
        }
        
        /// <summary>
        /// Resumes the game and hides pause popup
        /// Uses PauseService for all pause logic
        /// </summary>
        private async Task ResumeGame()
        {
            if (!_pauseService.IsPaused || _isTransitioning) return;
            
            Debug.Log("[PlayingState] Resuming game");
            
            try
            {
                // Hide pause popup first
                await UIService.HidePopupAsync<PausePopup>();
                Debug.Log("[PlayingState] Pause popup hidden");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayingState] Error hiding pause popup: {ex}");
            }
            
            // Use PauseService to handle all resume logic (time scale, audio, events)
            _pauseService.ResumeGame();
            
            Debug.Log("[PlayingState] Game successfully resumed");
        }
        #endregion

        #region Player State Restoration
        private async Task RestorePlayerStateFromSession(GameSession session)
        {
            try
            {
                Debug.Log($"[PlayingState] Restoring player state from session...");
                Debug.Log($"[PlayingState] Player Data - Level: {session.player.level}, Health: {session.player.health}, Position: {session.player.position}");
                
                await Task.Delay(100);
                
                // Your player restoration logic here...
                Debug.Log($"[PlayingState] Player state restoration simulated - implement actual restoration logic");
                Debug.Log($"[PlayingState] Would restore: Pos={session.player.position}, HP={session.player.health}, Lvl={session.player.level}");
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayingState] Error restoring player state: {ex}");
            }
        }
        #endregion

        #region UI Management
        private async Task CloseAllPopups()
        {
            try
            {
                Debug.Log("[PlayingState] Closing all popups...");
                
                await UIService.HidePopupAsync<LoadGamePopup>();
                await UIService.HidePopupAsync<PausePopup>();
                await UIService.HidePopupAsync<OptionsPopup>();
                await UIService.HidePopupAsync<SaveGamePopup>();
                
                Debug.Log("[PlayingState] All popups closed");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayingState] Error closing popups: {ex.Message}");
            }
        }
        #endregion
        
        #region Event Handlers - Pause/Resume - SIMPLIFIED!
        private async void OnPauseRequested(PauseRequestedEvent evt)
        {
            Debug.Log($"[PlayingState] OnPauseRequested - Current state: Paused={_pauseService.IsPaused}, Transitioning={_isTransitioning}");
            
            if (!_pauseService.IsPaused && !_isTransitioning)
            {
                await PauseGame();
            }
            else
            {
                Debug.LogWarning($"[PlayingState] Pause request ignored - Paused={_pauseService.IsPaused}, Transitioning={_isTransitioning}");
            }
        }
        
        private async void OnResumeRequested(ResumeRequestedEvent evt)
        {
            Debug.Log($"[PlayingState] OnResumeRequested - Current state: Paused={_pauseService.IsPaused}, Transitioning={_isTransitioning}");
            
            if (_pauseService.IsPaused && !_isTransitioning)
            {
                await ResumeGame();
            }
            else
            {
                Debug.LogWarning($"[PlayingState] Resume request ignored - Paused={_pauseService.IsPaused}, Transitioning={_isTransitioning}");
            }
        }
        
        /// <summary>
        /// Handle cancel/escape input for pause/resume toggle
        /// </summary>
        private async void OnCancelInput(UICancelInputEvent evt)
        {
            Debug.Log($"[PlayingState] OnCancelInput - Current state: Paused={_pauseService.IsPaused}, Transitioning={_isTransitioning}");
            
            if (_isTransitioning) return;
            
            if (_pauseService.IsPaused)
            {
                await ResumeGame();
            }
            else
            {
                await PauseGame();
            }
        }

        /// <summary>
        /// Handle dedicated pause input
        /// </summary>
        private async void OnPlayerPauseInput(PlayerPauseInputEvent evt)
        {
            Debug.Log($"[PlayingState] OnPlayerPauseInput - Current state: Paused={_pauseService.IsPaused}, Transitioning={_isTransitioning}");
            
            if (_isTransitioning) return;
            
            if (_pauseService.IsPaused)
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
        private async void OnMainMenuRequested(MainMenuRequestedEvent evt)
        {
            if (_isTransitioning) return;
            
            Debug.Log("[PlayingState] Main menu requested");
            await TransitionToStateAsync(GameStateType.MainMenu);
        }
        
        private async void OnGameOver(GameOverEvent evt)
        {
            if (_isTransitioning) return;
            
            Debug.Log("[PlayingState] Game over event received");
            
            if (_pauseService.IsPaused)
            {
                await ResumeGame();
            }
            
            await TransitionToStateAsync(GameStateType.GameOver);
        }
        
        private async void OnVictory(VictoryEvent evt)
        {
            if (_isTransitioning) return;
            
            Debug.Log("[PlayingState] Victory event received");
            
            if (_pauseService.IsPaused)
            {
                await ResumeGame();
            }
            
            await TransitionToStateAsync(GameStateType.Victory);
        }
        #endregion
        
        #region Input Event Handlers - SIMPLIFIED!
        private void OnAttackInput(PlayerAttackInputEvent evt)
        {
            if (_pauseService.IsPaused || _isTransitioning) return;
            
            Debug.Log($"[PlayingState] Player attack input: {evt.Phase}");
            // Forward to game systems...
        }
        
        private void OnJumpInput(PlayerJumpInputEvent evt)
        {
            if (_pauseService.IsPaused || _isTransitioning) return;
            
            Debug.Log("[PlayingState] Player jump input");
            // Forward to game systems...
        }
        
        private void OnInteractInput(PlayerInteractInputEvent evt)
        {
            if (_pauseService.IsPaused || _isTransitioning) return;
            
            Debug.Log($"[PlayingState] Player interact input: {evt.Phase}");
            // Forward to game systems...
        }
        
        private void OnMoveInput(PlayerMoveInputEvent evt)
        {
            if (_pauseService.IsPaused || _isTransitioning) return;
            
            // Handle movement (no debug log - called frequently)
        }
        
        private void OnLookInput(PlayerLookInputEvent evt)
        {
            if (_pauseService.IsPaused || _isTransitioning) return;
            
            // Handle camera/look input (no debug log - called frequently)
        }
        
        private void OnSprintInput(PlayerSprintInputEvent evt)
        {
            if (_pauseService.IsPaused || _isTransitioning) return;
            
            Debug.Log($"[PlayingState] Player sprint input: {evt.Phase}");
            // Forward to game systems...
        }
        
        private void OnCrouchInput(PlayerCrouchInputEvent evt)
        {
            if (_pauseService.IsPaused || _isTransitioning) return;
            
            Debug.Log($"[PlayingState] Player crouch input: {evt.Phase}");
            // Forward to game systems...
        }

        private void OnNextInput(PlayerNextInputEvent evt)
        {
            if (_pauseService.IsPaused || _isTransitioning) return;
            
            Debug.Log("[PlayingState] Player next input");
            // Forward to game systems...
        }

        private void OnPreviousInput(PlayerPreviousInputEvent evt)
        {
            if (_pauseService.IsPaused || _isTransitioning) return;
            
            Debug.Log("[PlayingState] Player previous input");
            // Forward to game systems...
        }
        #endregion
    }
}
