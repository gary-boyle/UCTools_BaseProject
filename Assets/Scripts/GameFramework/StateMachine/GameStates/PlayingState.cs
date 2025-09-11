using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Input;
using GameFramework.Input.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;
using GameFramework.UI.Popups;
using GameFramework.UI.Screens;
using UnityEngine;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Playing state - fully responsible for its UI lifecycle
    /// Handles all UI transitions including pause popup management and state transitions
    /// 
    /// Intent: Manage the main gameplay state and handle all user-initiated state changes
    /// Design: Event-driven architecture with clear separation of concerns
    /// Pros: Centralized state management, clean event handling, maintainable
    /// Cons: Requires careful event subscription management
    /// </summary>
    public class PlayingState : BaseGameState
    {
        private readonly IPauseService _pauseService;
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
            _pauseService = pauseService;
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
    
            // Auto-resume if game is paused when entering
            if (_pauseService.IsPaused)
            {
                _pauseService.ResumeGame();
            }
    
            // Subscribe to all user interaction events from UI
            _eventSystem.Subscribe<PauseRequestedEvent>(OnPauseRequested);
            _eventSystem.Subscribe<ResumeRequestedEvent>(OnResumeRequested);
            _eventSystem.Subscribe<GameOverEvent>(OnGameOver);
            _eventSystem.Subscribe<VictoryEvent>(OnVictory);
            _eventSystem.Subscribe<CreditsRequestedEvent>(OnCreditsRequested);
            _eventSystem.Subscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            _eventSystem.Subscribe<QuitRequestedEvent>(OnQuitRequested);

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
            
            // Ensure game is not paused when leaving state
            if (_pauseService.IsPaused)
            {
                _pauseService.ResumeGame();
            }
            
            // Reset input context
            _inputManager.SetInputContext(InputContext.UI);
            
            // Unsubscribe from all events to prevent memory leaks
            _eventSystem.Unsubscribe<PauseRequestedEvent>(OnPauseRequested);
            _eventSystem.Unsubscribe<ResumeRequestedEvent>(OnResumeRequested);
            _eventSystem.Unsubscribe<GameOverEvent>(OnGameOver);
            _eventSystem.Unsubscribe<VictoryEvent>(OnVictory);
            _eventSystem.Unsubscribe<CreditsRequestedEvent>(OnCreditsRequested);
            _eventSystem.Unsubscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            _eventSystem.Unsubscribe<QuitRequestedEvent>(OnQuitRequested);

            // State is responsible for hiding its UI
            await UIService.HideScreenAsync<GamePlayScreen>();
            await base.ExitAsync();
        }
        
        #region Event Handlers - UI Interaction Responses
        
        /// <summary>
        /// Handle pause request - state manages showing pause popup
        /// Only allows pause if not already paused and no popups are open
        /// </summary>
        private async void OnPauseRequested(PauseRequestedEvent evt)
        {
            if (!_pauseService.IsPaused && !UIService.HasOpenPopups())
            {
                _pauseService.PauseGame("Player requested pause");
                await UIService.ShowPopupAsync<PausePopup>();
                _inputManager.SetInputContext(InputContext.Mixed);
            }
            else
            {
                Debug.Log("[PlayingState] Cannot pause - game already paused or popups open");
            }
        }

        /// <summary>
        /// Handle resume request - state manages hiding pause popup
        /// </summary>
        private async void OnResumeRequested(ResumeRequestedEvent evt)
        {
            if (_pauseService.IsPaused)
            {
                _pauseService.ResumeGame();
                await UIService.HidePopupAsync<PausePopup>();
                _inputManager.SetInputContext(InputContext.Player);
            }
        }
        
        /// <summary>
        /// Handle game over event - transition to GameOver state
        /// Ensures clean state before transitioning
        /// </summary>
        private async void OnGameOver(GameOverEvent evt)
        {
            Debug.Log("[PlayingState] Game Over triggered - transitioning to GameOver state");
            
            if (_pauseService.IsPaused)
            {
                _pauseService.ResumeGame();
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
            
            if (_pauseService.IsPaused)
            {
                _pauseService.ResumeGame();
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
            
            if (_pauseService.IsPaused)
            {
                _pauseService.ResumeGame();
            }
            
            await TransitionToStateAsync(GameStateType.Credits);
        }
        
        /// <summary>
        /// Handle main menu request - state manages all cleanup before transition
        /// </summary>
        private async void OnMainMenuRequested(MainMenuRequestedEvent evt)
        {
            Debug.Log("[PlayingState] Main Menu requested - transitioning to Main Menu state");
            
            if (_pauseService.IsPaused)
            {
                _pauseService.ResumeGame();
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
