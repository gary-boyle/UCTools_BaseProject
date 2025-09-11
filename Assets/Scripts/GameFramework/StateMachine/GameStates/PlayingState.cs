using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Input;
using GameFramework.Input.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;
using GameFramework.StateMachine.Interfaces;
using GameFramework.UI.Popups;
using GameFramework.UI.Screens;
using UnityEngine;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Playing state - reacts to pause state changes from PauseService
    /// Manages UI in response to actual pause/resume events
    /// 
    /// Intent: Manage the main gameplay state by reacting to pause state changes
    /// Design: Reactive event-driven architecture - responds to state changes rather than intercepting requests
    /// Pros: Cleaner separation, more reactive, simpler flow
    /// Cons: Less control over when pausing occurs
    /// </summary>
    public class PlayingState : BaseGameState
    {
        private readonly IPauseService _pauseService; // Only for read-only state checking
        private readonly IInputManager _inputManager;
        private readonly IEventSystem _eventSystem;

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public PlayingState(
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputManager inputManager,
            IConsoleService consoleService,
            IGameDataService gameDataService,
            IPauseService pauseService)
            : base(GameStateType.Playing, stateMachine, eventSystem, audioService, uiService, inputManager, consoleService, gameDataService)
        {
            _pauseService = pauseService; // Only for read-only access
            _inputManager = inputManager;
            _eventSystem = eventSystem;
        }
        
        /// <summary>
        /// Enter playing state - setup UI, audio, input, and event subscriptions
        /// </summary>
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
    
            // Configure input for gameplay
            InputManager.SetInputContext(InputContext.Player);
    
            // Subscribe to state transition events
            _eventSystem.Subscribe<GameOverEvent>(OnGameOver);
            _eventSystem.Subscribe<VictoryEvent>(OnVictory);
            _eventSystem.Subscribe<CreditsRequestedEvent>(OnCreditsRequested);
            _eventSystem.Subscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            _eventSystem.Subscribe<QuitRequestedEvent>(OnQuitRequested);

            // Subscribe to pause state change events - this is where the magic happens
            _eventSystem.Subscribe<GamePausedEvent>(OnGamePaused);
            _eventSystem.Subscribe<GameResumedEvent>(OnGameResumed);
            
            // Auto-resume if game is paused when entering (via event)
            if (_pauseService.IsPaused)
            {
                Debug.Log("[PlayingState] Game was paused on entry - requesting resume");
                _eventSystem.Publish(new ResumeRequestedEvent());
            }

            // State is responsible for showing its UI
            await UIService.ShowScreenAsync<GamePlayScreen>();
            AudioService.PlayMusic("gameplay");
        }        
        
        /// <summary>
        /// Exit playing state - cleanup UI, pause state, input, and unsubscribe from events
        /// </summary>
        public override async Task ExitAsync()
        {
            // State handles cleanup of all UI elements it may have shown
            await UIService.CloseAllPopupsAsync();
            
            // Ensure game is not paused when leaving state (via event)
            if (_pauseService.IsPaused)
            {
                Debug.Log("[PlayingState] Requesting resume before state exit");
                _eventSystem.Publish(new ResumeRequestedEvent());
                
                // Wait a frame for the resume to process
                await Task.Yield();
            }
            
            // Reset input context
            _inputManager.SetInputContext(InputContext.UI);
            
            // Unsubscribe from all events to prevent memory leaks
            _eventSystem.Unsubscribe<GameOverEvent>(OnGameOver);
            _eventSystem.Unsubscribe<VictoryEvent>(OnVictory);
            _eventSystem.Unsubscribe<CreditsRequestedEvent>(OnCreditsRequested);
            _eventSystem.Unsubscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            _eventSystem.Unsubscribe<QuitRequestedEvent>(OnQuitRequested);

            // Unsubscribe from pause state change events
            _eventSystem.Unsubscribe<GamePausedEvent>(OnGamePaused);
            _eventSystem.Unsubscribe<GameResumedEvent>(OnGameResumed);

            // State is responsible for hiding its UI
            await UIService.HideScreenAsync<GamePlayScreen>();
            await base.ExitAsync();
        }
        
        #region Event Handlers - Reactive to State Changes
        
        /// <summary>
        /// React to game being paused - show pause UI if appropriate
        /// If pause is invalid for current UI state, immediately request resume
        /// </summary>
        private async void OnGamePaused(GamePausedEvent evt)
        {
            // Check if pause is valid for current UI state
            if (UIService.HasOpenPopups())
            {
                Debug.Log("[PlayingState] Game paused but popups are open - requesting immediate resume");
                _eventSystem.Publish(new ResumeRequestedEvent());
                return;
            }
            
            // Pause is valid - show pause UI
            await UIService.ShowPopupAsync<PausePopup>();
            _inputManager.SetInputContext(InputContext.Mixed);
        }

        /// <summary>
        /// React to game being resumed - hide pause UI
        /// </summary>
        private async void OnGameResumed(GameResumedEvent evt)
        {
            Debug.Log("[PlayingState] Game resumed");
            
            // Hide pause popup if it's showing
            await UIService.HidePopupAsync<PausePopup>();
            _inputManager.SetInputContext(InputContext.Player);
        }
        
        /// <summary>
        /// Handle game over event - transition to GameOver state
        /// Ensures clean state before transitioning
        /// </summary>
        private async void OnGameOver(GameOverEvent evt)
        {
            Debug.Log("[PlayingState] Game Over triggered - transitioning to GameOver state");
            
            // Request resume via event if paused
            if (_pauseService.IsPaused)
            {
                _eventSystem.Publish(new ResumeRequestedEvent());
            }
            
            await TransitionToStateAsync(GameStateType.GameOver);
        }

        /// <summary>
        /// Handle victory event - transition to Victory state
        /// Ensures clean state before transitioning
        /// </summary>
        private async void OnVictory(VictoryEvent evt)
        {
            Debug.Log("[PlayingState] Victory triggered - transitioning to Victory state");
            
            // Request resume via event if paused
            if (_pauseService.IsPaused)
            {
                _eventSystem.Publish(new ResumeRequestedEvent());
            }
            
            await TransitionToStateAsync(GameStateType.Victory);
        }

        /// <summary>
        /// Handle credits request - transition to Credits state
        /// Ensures clean state before transitioning
        /// </summary>
        private async void OnCreditsRequested(CreditsRequestedEvent evt)
        {
            Debug.Log("[PlayingState] Credits requested - transitioning to Credits state");
            
            // Request resume via event if paused
            if (_pauseService.IsPaused)
            {
                _eventSystem.Publish(new ResumeRequestedEvent());
            }
            
            await TransitionToStateAsync(GameStateType.Credits);
        }
        
        /// <summary>
        /// Handle main menu request - state manages all cleanup before transition
        /// </summary>
        private async void OnMainMenuRequested(MainMenuRequestedEvent evt)
        {
            Debug.Log("[PlayingState] Main Menu requested - transitioning to Main Menu state");
            
            // Request resume via event if paused
            if (_pauseService.IsPaused)
            {
                _eventSystem.Publish(new ResumeRequestedEvent());
            }
            
            await TransitionToStateAsync(GameStateType.MainMenu);
        }
        
        /// <summary>
        /// Handle quit request - state manages UI transition
        /// </summary>
        private async void OnQuitRequested(QuitRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.Quit);
        }
        #endregion
    }
}
