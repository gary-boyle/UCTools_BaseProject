using System;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Input;
using GameFramework.Input.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Data;
using GameFramework.StateMachine.Enum;
using GameFramework.UI.Screens;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// New Game state - fully responsible for its UI lifecycle
    /// Handles all UI transitions based on user interactions reported by the screen
    /// </summary>
    public class NewGameState : BaseGameState
    {
        protected readonly IGameDataService GameDataService;

        public NewGameState(
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputManager inputManager,
            IConsoleService consoleService,
            IGameDataService gameDataService)  
            : base(GameStateType.NewGame, stateMachine, eventSystem, audioService, uiService, inputManager, consoleService, gameDataService)
        {
            GameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
        }
    
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            InputManager.SetInputContext(InputContext.UI);

            // Subscribe to user interaction events from UI
            EventSystem.Subscribe<NewGameRequestedEvent>(OnNewGameRequested);
            EventSystem.Subscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            
            // State is responsible for showing its UI
            await UIService.ShowScreenAsync<NewGameScreen>();
        }
        
        /// <summary>
        /// Handle new game creation - state manages UI transition to loading
        /// </summary>
        private async void OnNewGameRequested(NewGameRequestedEvent evt)
        {
            // Create loading configuration
            var loadingConfig = LoadingConfiguration.NewGame(evt.StartingScene, evt.PlayerName);
            loadingConfig.GameData["difficulty"] = evt.Difficulty;
            
            foreach (var kvp in evt.CustomData)
            {
                loadingConfig.GameData[kvp.Key] = kvp.Value;
            }
            
            GameDataService.CurrentLoadingConfig = loadingConfig;
            await TransitionToStateAsync(GameStateType.Loading);
        }
        
        /// <summary>
        /// Handle back to main menu - state manages UI transition
        /// </summary>
        private async void OnMainMenuRequested(MainMenuRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.MainMenu);
        }
        
        public override async Task ExitAsync()
        {
            // Unsubscribe from events
            EventSystem.Unsubscribe<NewGameRequestedEvent>(OnNewGameRequested);
            EventSystem.Unsubscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            
            // State is responsible for cleaning up its UI
            await UIService.HideScreenAsync<NewGameScreen>();
            
            await base.ExitAsync();
        }
    }
}
