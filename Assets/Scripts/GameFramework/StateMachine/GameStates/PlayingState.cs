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
            _inputManager.SetInputContext(InputContext.Player);
            
            // Subscribe only to high-level game events, not input events
            _eventSystem.Subscribe<PauseRequestedEvent>(OnPauseRequested);
            _eventSystem.Subscribe<ResumeRequestedEvent>(OnResumeRequested);
            _eventSystem.Subscribe<GameOverEvent>(OnGameOver);
            
            
            await UIService.ShowScreenAsync<GamePlayScreen>();
            AudioService.PlayMusic("gameplay");
        }
        
        public override async Task ExitAsync()
        {
            _eventSystem.Unsubscribe<PauseRequestedEvent>(OnPauseRequested);
            _eventSystem.Unsubscribe<ResumeRequestedEvent>(OnResumeRequested);
            _eventSystem.Unsubscribe<GameOverEvent>(OnGameOver);
            
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
        
        private async void OnGameOver(GameOverEvent evt)
        {
            await TransitionToStateAsync(GameStateType.GameOver);
        }
    }
}
