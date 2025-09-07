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
    /// Pause is no longer a separate state - it's a mode within this state
    /// Handles loading from save games and restoring player state
    /// </summary>
    public class PlayingState : BaseGameState
    {
        #region Private Fields
        private bool _isPaused = false;
        private float _prePauseTimeScale = 1f;
        private float _prePauseAudioVolume = 1f;
        private bool _isTransitioning = false;
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
            IGameDataService gameDataService)  
            : base(GameStateType.Playing, stateMachine, eventSystem, audioService, uiService, inputService, consoleService, gameDataService)
        {
        }
        #endregion
        
        #region State Lifecycle
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
            Debug.Log("[PlayingState] Entering playing state...");
            
            // ✅ CRITICAL: Reset all state flags when entering
            _isPaused = false;
            _isTransitioning = false;
            _prePauseTimeScale = 1f;
            _prePauseAudioVolume = 1f;
            
            // ✅ Ensure time scale is correct
            Time.timeScale = 1f;
            
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
            
            // ✅ CRITICAL: Always unsubscribe on exit
            UnsubscribeFromEvents();
            
            // Clean up pause state
            if (_isPaused)
            {
                Debug.Log("[PlayingState] Cleaning up pause state before exit");
                Time.timeScale = 1f; // Don't leave the game paused
                _isPaused = false;
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
            // Game logic updates are automatically paused via Time.timeScale = 0
            // But you can add custom pause-aware logic here if needed
            
            // Only update session if game is not paused and not transitioning
            if (!_isPaused && !_isTransitioning && GameDataService.HasActiveSession())
            {
                // Let GameDataService handle session updates (it checks pause state internally)
                // GameDataService.Update() is called by the framework
            }
        }
        #endregion

        #region Event Management
        /// <summary>
        /// ✅ Centralized event subscription
        /// </summary>
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


        /// <summary>
        /// ✅ Centralized event unsubscription
        /// </summary>
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
        
        #region Pause Management
        /// <summary>
        /// Pauses the game and shows pause popup
        /// </summary>
        private async Task PauseGame()
        {
            if (_isPaused || _isTransitioning) return;
            
            Debug.Log("[PlayingState] Pausing game");
            
            _isPaused = true;
            
            // Store current state
            _prePauseTimeScale = Time.timeScale;
            _prePauseAudioVolume = AudioService.GetMasterVolume();
            
            // Apply pause effects
            Time.timeScale = 0f;
            AudioService.SetMasterVolume(_prePauseAudioVolume * 0.3f); // Reduce volume, don't mute completely
            
            try
            {
                // Show pause popup
                await UIService.ShowPopupAsync<PausePopup>();
                Debug.Log("[PlayingState] Pause popup shown");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayingState] Error showing pause popup: {ex}");
                // If popup fails, still publish pause event
            }
            
            // Publish pause event for global systems
            EventSystem.Publish(new GamePausedEvent());
            
            Debug.Log("[PlayingState] Game successfully paused");
        }
        
        /// <summary>
        /// Resumes the game and hides pause popup
        /// </summary>
        private async Task ResumeGame()
        {
            if (!_isPaused || _isTransitioning) return;
            
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
                // Continue with resume anyway
            }
            
            _isPaused = false;
            
            // Restore previous state
            Time.timeScale = _prePauseTimeScale;
            AudioService.SetMasterVolume(_prePauseAudioVolume);
            
            // Publish resume event for global systems
            EventSystem.Publish(new GameResumedEvent());
            
            Debug.Log("[PlayingState] Game successfully resumed");
        }
        
        /// <summary>
        /// Checks if the game is currently paused
        /// </summary>
        public bool IsGamePaused() => _isPaused;
        #endregion

        #region Player State Restoration
        /// <summary>
        /// ✅ Restore player state when loading from save
        /// </summary>
        private async Task RestorePlayerStateFromSession(GameSession session)
        {
            try
            {
                Debug.Log($"[PlayingState] Restoring player state from session...");
                Debug.Log($"[PlayingState] Player Data - Level: {session.player.level}, Health: {session.player.health}, Position: {session.player.position}");
                
                // ✅ Wait a frame to ensure scene is fully loaded
                await Task.Delay(100);
                
                // ✅ Find player GameObject and restore state
                var player = GameObject.FindObjectOfType<UnityEngine.MonoBehaviour>(); // Replace with your actual player controller type
                
                // Example restoration logic - replace with your actual player controller
                /*
                var playerController = GameObject.FindObjectOfType<PlayerController>();
                if (playerController != null)
                {
                    Debug.Log($"[PlayingState] Found player controller, restoring state...");
                    
                    // Restore position
                    playerController.transform.position = session.player.position;
                    
                    // Restore rotation if needed
                    if (session.player.rotation != Vector3.zero)
                    {
                        playerController.transform.eulerAngles = session.player.rotation;
                    }
                    
                    // Restore health
                    var healthComponent = playerController.GetComponent<HealthComponent>();
                    if (healthComponent != null)
                    {
                        healthComponent.SetMaxHealth(session.player.maxHealth);
                        healthComponent.SetCurrentHealth(session.player.health);
                    }
                    
                    // Restore level/experience
                    var levelComponent = playerController.GetComponent<LevelComponent>();
                    if (levelComponent != null)
                    {
                        levelComponent.SetLevel(session.player.level);
                        levelComponent.SetExperience(session.player.experience);
                    }
                    
                    // Restore inventory
                    var inventory = playerController.GetComponent<InventoryComponent>();
                    if (inventory != null)
                    {
                        inventory.LoadInventoryFromData(session.player.inventory);
                    }
                    
                    // Restore abilities
                    var abilitySystem = playerController.GetComponent<AbilitySystem>();
                    if (abilitySystem != null)
                    {
                        abilitySystem.UnlockAbilities(session.player.unlockedAbilities);
                    }
                    
                    Debug.Log("[PlayingState] Player state restoration complete");
                }
                else
                {
                    Debug.LogWarning("[PlayingState] Player controller not found, cannot restore player state");
                }
                */
                
                // ✅ For now, just log the restoration (replace above with your actual logic)
                Debug.Log($"[PlayingState] Player state restoration simulated - implement actual restoration logic");
                Debug.Log($"[PlayingState] Would restore: Pos={session.player.position}, HP={session.player.health}, Lvl={session.player.level}");
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayingState] Error restoring player state: {ex}");
                // Don't fail the entire loading process because of player restoration issues
            }
        }
        #endregion

        #region UI Management
        /// <summary>
        /// ✅ Ensure all popups are closed when entering play state
        /// </summary>
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
                // Don't fail state entry because of popup issues
            }
        }
        #endregion
        
        #region Event Handlers - Pause/Resume
        private async void OnPauseRequested(PauseRequestedEvent evt)
        {
            Debug.Log($"[PlayingState] OnPauseRequested - Current state: Paused={_isPaused}, Transitioning={_isTransitioning}");
            
            if (!_isPaused && !_isTransitioning)
            {
                await PauseGame();
            }
            else
            {
                Debug.LogWarning($"[PlayingState] Pause request ignored - Paused={_isPaused}, Transitioning={_isTransitioning}");
            }
        }
        
        private async void OnResumeRequested(ResumeRequestedEvent evt)
        {
            Debug.Log($"[PlayingState] OnResumeRequested - Current state: Paused={_isPaused}, Transitioning={_isTransitioning}");
            
            if (_isPaused && !_isTransitioning)
            {
                await ResumeGame();
            }
            else
            {
                Debug.LogWarning($"[PlayingState] Resume request ignored - Paused={_isPaused}, Transitioning={_isTransitioning}");
            }
        }        
        /// <summary>
        /// Handle cancel/escape input for pause/resume toggle
        /// </summary>
        private async void OnCancelInput(UICancelInputEvent evt)
        {
            Debug.Log($"[PlayingState] OnCancelInput - Current state: Paused={_isPaused}, Transitioning={_isTransitioning}");
            
            if (_isTransitioning) return;
            
            if (_isPaused)
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
            Debug.Log($"[PlayingState] OnPlayerPauseInput - Current state: Paused={_isPaused}, Transitioning={_isTransitioning}");
            
            if (_isTransitioning) return;
            
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
            if (_isTransitioning) return;
            
            Debug.Log("[PlayingState] Main menu requested");
            
            // // Save game before leaving if we have an active session
            // if (GameDataService.HasActiveSession())
            // {
            //     try
            //     {
            //         Debug.Log("[PlayingState] Auto-saving before returning to main menu...");
            //         await SaveService.SaveCurrentSessionAsync("BeforeMainMenu");
            //     }
            //     catch (Exception ex)
            //     {
            //         Debug.LogWarning($"[PlayingState] Failed to auto-save before main menu: {ex}");
            //         // Don't block transition if save fails
            //     }
            // }
            
            await TransitionToStateAsync(GameStateType.MainMenu);
        }
        
        private async void OnGameOver(GameOverEvent evt)
        {
            if (_isTransitioning) return;
            
            Debug.Log("[PlayingState] Game over event received");
            
            // Ensure we're not paused before transitioning
            if (_isPaused)
            {
                await ResumeGame();
            }
            
            await TransitionToStateAsync(GameStateType.GameOver);
        }
        
        private async void OnVictory(VictoryEvent evt)
        {
            if (_isTransitioning) return;
            
            Debug.Log("[PlayingState] Victory event received");
            
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
            if (_isPaused || _isTransitioning) return;
            
            Debug.Log($"[PlayingState] Player attack input: {evt.Phase}");
            
            // ✅ Forward to game systems or publish attack commands
            // Example: EventSystem.Publish(new PlayerAttackCommand { Phase = evt.Phase });
        }
        
        /// <summary>
        /// Handle player jump input - only when not paused
        /// </summary>
        private void OnJumpInput(PlayerJumpInputEvent evt)
        {
            if (_isPaused || _isTransitioning) return;
            
            Debug.Log("[PlayingState] Player jump input");
            
            // ✅ Forward to game systems or publish jump commands
            // Example: EventSystem.Publish(new PlayerJumpCommand());
        }
        
        /// <summary>
        /// Handle player interact input - only when not paused
        /// </summary>
        private void OnInteractInput(PlayerInteractInputEvent evt)
        {
            if (_isPaused || _isTransitioning) return;
            
            Debug.Log($"[PlayingState] Player interact input: {evt.Phase}");
            
            // ✅ Forward to game systems or publish interact commands
            // Example: EventSystem.Publish(new PlayerInteractCommand { Phase = evt.Phase });
        }
        
        /// <summary>
        /// Handle player movement input - only when not paused
        /// </summary>
        private void OnMoveInput(PlayerMoveInputEvent evt)
        {
            if (_isPaused || _isTransitioning) return;
            
            // Handle movement (called frequently, so no debug log)
            // ✅ Forward to player controller or publish movement commands
            // Example: EventSystem.Publish(new PlayerMoveCommand { MovementVector = evt.MovementVector });
        }
        
        /// <summary>
        /// Handle player look input - only when not paused
        /// </summary>
        private void OnLookInput(PlayerLookInputEvent evt)
        {
            if (_isPaused || _isTransitioning) return;
            
            // Handle camera/look input (called frequently, so no debug log)
            // ✅ Forward to camera controller or publish look commands
            // Example: EventSystem.Publish(new PlayerLookCommand { LookDelta = evt.LookDelta });
        }
        
        /// <summary>
        /// Handle player sprint input - only when not paused
        /// </summary>
        private void OnSprintInput(PlayerSprintInputEvent evt)
        {
            if (_isPaused || _isTransitioning) return;
            
            Debug.Log($"[PlayingState] Player sprint input: {evt.Phase}");
            
            // ✅ Forward to game systems or publish sprint commands
            // Example: EventSystem.Publish(new PlayerSprintCommand { Phase = evt.Phase });
        }
        
        /// <summary>
        /// Handle player crouch input - only when not paused
        /// </summary>
        private void OnCrouchInput(PlayerCrouchInputEvent evt)
        {
            if (_isPaused || _isTransitioning) return;
            
            Debug.Log($"[PlayingState] Player crouch input: {evt.Phase}");
            
            // ✅ Forward to game systems or publish crouch commands
            // Example: EventSystem.Publish(new PlayerCrouchCommand { Phase = evt.Phase });
        }

        /// <summary>
        /// Handle player next input (e.g., next weapon, next item)
        /// </summary>
        private void OnNextInput(PlayerNextInputEvent evt)
        {
            if (_isPaused || _isTransitioning) return;
            
            Debug.Log("[PlayingState] Player next input");
            
            // ✅ Forward to appropriate systems
            // Example: EventSystem.Publish(new PlayerNextCommand());
        }

        /// <summary>
        /// Handle player previous input (e.g., previous weapon, previous item)
        /// </summary>
        private void OnPreviousInput(PlayerPreviousInputEvent evt)
        {
            if (_isPaused || _isTransitioning) return;
            
            Debug.Log("[PlayingState] Player previous input");
            
            // ✅ Forward to appropriate systems
            // Example: EventSystem.Publish(new PlayerPreviousCommand());
        }
        #endregion
        
        #region Public Properties
        /// <summary>
        /// Public property to check pause state from external systems
        /// </summary>
        public bool IsPaused => _isPaused;
        
        /// <summary>
        /// Public property to check if state is transitioning
        /// </summary>
        public bool IsTransitioning => _isTransitioning;
        #endregion
    }
}
