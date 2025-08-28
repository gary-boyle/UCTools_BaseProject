using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Data;
using GameFramework.StateMachine.Enum;
using GameFramework.UI.Screens;

namespace GameFramework.StateMachine.GameStates
{
    public class NewGameState : BaseGameState
    {
        private NewGameRequestedEvent _pendingNewGame;
        protected readonly IGameDataService GameDataService;

        public NewGameState(
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputService inputService,
            IConsoleService consoleService,
            IGameDataService gameDataService)  
            : base(GameStateType.NewGame, stateMachine, eventSystem, audioService, uiService, inputService, consoleService, gameDataService)
        {
            GameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
        }
    
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
            // Subscribe to new game requests
            EventSystem.Subscribe<NewGameRequestedEvent>(OnNewGameRequested);
            
            // Show new game UI
            await UIService.ShowScreenAsync<NewGameScreen>();
        }
        
        private async void OnNewGameRequested(NewGameRequestedEvent evt)
        {
            // Hide the new game screen
            await UIService.HideScreenAsync<NewGameScreen>();
            
            // Create loading configuration
            var loadingConfig = LoadingConfiguration.NewGame(evt.StartingScene, evt.PlayerName);
            loadingConfig.GameData["difficulty"] = evt.Difficulty;
            
            foreach (var kvp in evt.CustomData)
            {
                loadingConfig.GameData[kvp.Key] = kvp.Value;
            }
            
            // Set loading configuration in the data service
            GameDataService.CurrentLoadingConfig = loadingConfig;
            
            await TransitionToStateAsync(GameStateType.Loading);
        }
        
        public override async Task ExitAsync()
        {
            // Unsubscribe from events
            EventSystem.Unsubscribe<NewGameRequestedEvent>(OnNewGameRequested);
            
            await base.ExitAsync();
        }
    }
}
