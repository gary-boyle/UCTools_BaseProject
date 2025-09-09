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
    /// Updated NewGameState that properly initializes the central GameDataManager
    /// Creates unified game session from new game parameters
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

            // Subscribe to new game requests
            EventSystem.Subscribe<NewGameRequestedEvent>(OnNewGameRequested);
            
            // Show new game UI
            await UIService.ShowScreenAsync<NewGameScreen>();
        }
        
        /// <summary>
        /// Handles new game creation by setting up unified loading configuration
        /// </summary>
        private async void OnNewGameRequested(NewGameRequestedEvent evt)
        {
            // Hide the new game screen
            await UIService.HideScreenAsync<NewGameScreen>();
            
            // Create loading configuration that will be used by GameDataManager
            var loadingConfig = LoadingConfiguration.NewGame(evt.StartingScene, evt.PlayerName);
            loadingConfig.GameData["difficulty"] = evt.Difficulty;
            
            // Add all custom data from the event
            foreach (var kvp in evt.CustomData)
            {
                loadingConfig.GameData[kvp.Key] = kvp.Value;
            }
            
            // Set loading configuration - GameDataManager will use this to create the session
            GameDataService.CurrentLoadingConfig = loadingConfig;
            
            await TransitionToStateAsync(GameStateType.Loading);
        }
        
        public override async Task ExitAsync()
        {
            EventSystem.Unsubscribe<NewGameRequestedEvent>(OnNewGameRequested);
            await base.ExitAsync();
        }
    }
}
