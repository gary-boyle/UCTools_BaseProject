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
    /// Handles all UI transitions including pause popup management
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
    
            InputManager.SetInputContext(InputContext.Player);
    
            // Auto-resume if game is paused when entering
            if (_pauseService.IsPaused)
            {
                _pauseService.ResumeGame();
            }
    
            // Subscribe to user interaction events from UI
            _eventSystem.Subscribe<PauseRequestedEvent>(OnPauseRequested);
            _eventSystem.Subscribe<ResumeRequestedEvent>(OnResumeRequested);
            _eventSystem.Subscribe<GameOverEvent>(OnGameOver);
            _eventSystem.Subscribe<MainMenuRequestedEvent>(OnMainMenuRequested);

            // State is responsible for showing its UI
            await UIService.ShowScreenAsync<GamePlayScreen>();
            AudioService.PlayMusic("gameplay");
        }        
        
        public override async Task ExitAsync()
        {
            // State handles cleanup of all UI elements it may have shown
            await UIService.CloseAllPopupsAsync();
            
            if (_pauseService.IsPaused)
            {
                _pauseService.ResumeGame();
            }
            
            _inputManager.SetInputContext(InputContext.UI);
            
            // Unsubscribe from events
            _eventSystem.Unsubscribe<PauseRequestedEvent>(OnPauseRequested);
            _eventSystem.Unsubscribe<ResumeRequestedEvent>(OnResumeRequested);
            _eventSystem.Unsubscribe<GameOverEvent>(OnGameOver);
            _eventSystem.Unsubscribe<MainMenuRequestedEvent>(OnMainMenuRequested);

            // State is responsible for hiding its UI
            await UIService.HideScreenAsync<GamePlayScreen>();
            await base.ExitAsync();
        }
        
        /// <summary>
        /// Handle pause request - state manages showing pause popup
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
        /// Handle main menu request - state manages all cleanup before transition
        /// </summary>
        private async void OnMainMenuRequested(MainMenuRequestedEvent evt)
        {
            if (_pauseService.IsPaused)
            {
                _pauseService.ResumeGame();
            }
            
            await TransitionToStateAsync(GameStateType.MainMenu);
        }
        
        /// <summary>
        /// Handle game over - state manages UI transition
        /// </summary>
        private async void OnGameOver(GameOverEvent evt)
        {
            if (_pauseService.IsPaused)
            {
                _pauseService.ResumeGame();
            }
            
            await TransitionToStateAsync(GameStateType.GameOver);
        }
    }
}
