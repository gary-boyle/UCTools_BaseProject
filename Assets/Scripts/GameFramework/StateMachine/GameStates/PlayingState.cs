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
    /// Simplified PlayingState - no direct input handling
    /// Just declares what input context it needs
    /// Now properly handles cleanup when returning to main menu
    /// </summary>
    public class PlayingState : BaseGameState
    {
        private readonly IPauseService _pauseService;
        private readonly IInputManager _inputManager;
        private readonly IEventSystem _eventSystem;

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
        
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
    
            // Simply declare that we need player input
            InputManager.SetInputContext(InputContext.Player);
    
            // **FIX: Auto-resume if game is paused when entering PlayingState**
            // This handles the case where user loaded a game from a paused state
            if (_pauseService.IsPaused)
            {
                Debug.Log("[PlayingState] Auto-resuming game after entering PlayingState (likely from loading)");
                _pauseService.ResumeGame();
            }
    
            // Subscribe only to high-level game events, not input events
            _eventSystem.Subscribe<PauseRequestedEvent>(OnPauseRequested);
            _eventSystem.Subscribe<ResumeRequestedEvent>(OnResumeRequested);
            _eventSystem.Subscribe<GameOverEvent>(OnGameOver);
            _eventSystem.Subscribe<MainMenuRequestedEvent>(OnMainMenuRequested);

            await UIService.ShowScreenAsync<GamePlayScreen>();
            AudioService.PlayMusic("gameplay");
        }        
        
        public override async Task ExitAsync()
        {
            // **ENHANCED: Ensure all popups are closed and game is unpaused when exiting**
            Debug.Log("[PlayingState] Exiting - cleaning up popups and pause state");
            
            // Close all popups (including pause popup)
            await UIService.CloseAllPopupsAsync();
            
            // Resume game if it's paused
            if (_pauseService.IsPaused)
            {
                Debug.Log("[PlayingState] Resuming game during exit");
                _pauseService.ResumeGame();
            }
            
            // Reset input context
            _inputManager.SetInputContext(InputContext.UI);
            
            // Unsubscribe from events
            _eventSystem.Unsubscribe<PauseRequestedEvent>(OnPauseRequested);
            _eventSystem.Unsubscribe<ResumeRequestedEvent>(OnResumeRequested);
            _eventSystem.Unsubscribe<GameOverEvent>(OnGameOver);
            _eventSystem.Unsubscribe<MainMenuRequestedEvent>(OnMainMenuRequested);

            await UIService.HideScreenAsync<GamePlayScreen>();
            await base.ExitAsync();
        }
        
        private async void OnPauseRequested(PauseRequestedEvent evt)
        {
            if (!_pauseService.IsPaused)
            {
                // Pause game - but only if no other popups are open
                if (!UIService.HasOpenPopups())
                {
                    _pauseService.PauseGame("Player requested pause");
                    await UIService.ShowPopupAsync<PausePopup>();
                    _inputManager.SetInputContext(InputContext.Mixed); // Allow UI + player input for pause menu
                }
                else
                {
                    Debug.Log("[PlayingState] Cannot pause - other popups are open");
                }
            }
        }

        private async void OnResumeRequested(ResumeRequestedEvent evt)
        {
            if (_pauseService.IsPaused)
            {
                // Resume game
                _pauseService.ResumeGame();
                await UIService.HidePopupAsync<PausePopup>();
                _inputManager.SetInputContext(InputContext.Player); // Back to player input
            }
        }
        
        /// <summary>
        /// FIXED: Handles main menu requests with proper cleanup
        /// Closes all popups, resumes game, then transitions to main menu
        /// </summary>
        private async void OnMainMenuRequested(MainMenuRequestedEvent evt)
        {
            Debug.Log("[PlayingState] Main menu requested - performing cleanup before transition");
            
            // Close all popups (including pause popup) before transitioning
            await UIService.CloseAllPopupsAsync();
            
            // Resume game if it's paused
            if (_pauseService.IsPaused)
            {
                Debug.Log("[PlayingState] Resuming game before returning to main menu");
                _pauseService.ResumeGame();
            }
            
            // Transition to main menu (ExitAsync will handle additional cleanup)
            await TransitionToStateAsync(GameStateType.MainMenu);
        }
        
        private async void OnGameOver(GameOverEvent evt)
        {
            // Close popups and resume game before transitioning to game over
            await UIService.CloseAllPopupsAsync();
            
            if (_pauseService.IsPaused)
            {
                _pauseService.ResumeGame();
            }
            
            await TransitionToStateAsync(GameStateType.GameOver);
        }
    }
}
