using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Input;
using GameFramework.Input.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;
using GameFramework.UI.Screens;
using UnityEngine;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Game Over state - manages the game over screen and handles player choices
    /// Responsible for its UI lifecycle and event handling
    /// 
    /// Intent: Present game over state with options to restart, load game, or return to menu
    /// Design: Event-driven state management with clear separation of concerns
    /// Pros: Clear responsibility separation, multiple player options, maintainable
    /// Cons: Requires careful event subscription management
    /// </summary>
    public class GameOverState : BaseGameState
    {
        private readonly IEventSystem _eventSystem;
        private readonly ISaveService _saveService;
        
        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public GameOverState(
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputManager inputManager,
            IConsoleService consoleService,
            IGameDataService gameDataService)  
            : base(GameStateType.GameOver, stateMachine, eventSystem, audioService, uiService, inputManager, consoleService, gameDataService)
        {
            _eventSystem = eventSystem;
            _saveService = GameManager.GetService<ISaveService>();
        }
        
        /// <summary>
        /// Enter game over state - setup UI, audio, input, and event subscriptions
        /// </summary>
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
            // Set input context for UI navigation
            InputManager.SetInputContext(InputContext.UI);
            
            // Subscribe to user interaction events from UI
            _eventSystem.Subscribe<NewGameRequestedEvent>(OnNewGameRequested);
            _eventSystem.Subscribe<LoadWindowRequestedEvent>(OnLoadWindowRequested);
            _eventSystem.Subscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            
            // Show the game over screen
            await UIService.ShowScreenAsync<GameOverScreen>();
            
            // Play appropriate music/sound for game over
            AudioService.PlayMusic("gameover");
        }
        
        /// <summary>
        /// Exit game over state - cleanup UI, audio, and unsubscribe from events
        /// </summary>
        public override async Task ExitAsync()
        {
            Debug.Log("[GameOverState] Exiting Game Over state");
            
            // Unsubscribe from events to prevent memory leaks
            _eventSystem.Unsubscribe<NewGameRequestedEvent>(OnNewGameRequested);
            _eventSystem.Unsubscribe<LoadWindowRequestedEvent>(OnLoadWindowRequested);
            _eventSystem.Unsubscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            
            // Hide the game over screen
            await UIService.HideScreenAsync<GameOverScreen>();
            
            // Stop game over music
            AudioService.StopMusic();
            
            await base.ExitAsync();
        }
        
        #region Event Handlers
        
        /// <summary>
        /// Handle new game request - restart the game
        /// </summary>
        private async void OnNewGameRequested(NewGameRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.NewGame);
        }
        
        /// <summary>
        /// Handle load window request - show load game interface
        /// </summary>
        private async void OnLoadWindowRequested(LoadWindowRequestedEvent evt)
        {
            await UIService.ShowPopupAsync<LoadGamePopup>();
        }
        
        /// <summary>
        /// Handle main menu request - return to main menu
        /// </summary>
        private async void OnMainMenuRequested(MainMenuRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.MainMenu);
        }
        
        #endregion
    }
}
