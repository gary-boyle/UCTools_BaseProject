using System;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Data;
using GameFramework.StateMachine.Enum;
using GameFramework.UI.Screens;
using GameFramework.UI.Popups;
using UnityEngine;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Main menu state using unified GameSession system for save/load operations
    /// </summary>
    public class MainMenuState : BaseGameState
    {
        protected readonly IGameDataService GameDataService;
        protected readonly ISaveService SaveService;

        public MainMenuState(
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputService inputService,
            IConsoleService consoleService,
            IGameDataService gameDataService,
            ISaveService saveService)  
            : base(GameStateType.MainMenu, stateMachine, eventSystem, audioService, uiService, inputService, consoleService, gameDataService)
        {
            GameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
            SaveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        }
        
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
            // Clear any existing session when returning to main menu
            GameDataService.ClearSession();
            
            // Show main menu UI using injected service
            await UIService.ShowScreenAsync<MainMenuScreen>();
            
            // Play main menu music using injected service
            AudioService.PlayMusic("main_menu");
            
            // Subscribe to menu events using injected event system
            EventSystem.Subscribe<NewGameRequestedEvent>(OnNewGameRequested);
            EventSystem.Subscribe<LoadRequestedEvent>(OnContinueGameRequested);
            EventSystem.Subscribe<OptionsRequestedEvent>(OnOptionsRequested);
            EventSystem.Subscribe<CreditsRequestedEvent>(OnCreditsRequested);
            EventSystem.Subscribe<QuitRequestedEvent>(OnQuitRequested);
        }
        
        private async void OnNewGameRequested(NewGameRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.NewGame);
        }
        
        private async void OnContinueGameRequested(LoadRequestedEvent evt)
        {
            // Load the most recent save using the new session-based system
            var mostRecentSave = SaveService.GetMostRecentSaveName();
            
            if (string.IsNullOrEmpty(mostRecentSave))
            {
                Debug.LogWarning("[MainMenuState] No save files found to continue");
                // Optionally show a message to the user
                return;
            }
            
            // Load the session using GameDataService
            var loadSuccess = await GameDataService.LoadSessionAsync(mostRecentSave);
            
            if (loadSuccess)
            {
                // Create loading configuration from the loaded session
                var loadingConfig = GameDataService.CurrentSession.ToLoadingConfiguration();
                GameDataService.CurrentLoadingConfig = loadingConfig;
                
                await TransitionToStateAsync(GameStateType.Loading);
            }
            else
            {
                Debug.LogError("[MainMenuState] Failed to load most recent save");
                // Optionally show error message to user
            }
        }
        
        private async void OnOptionsRequested(OptionsRequestedEvent evt)
        {
            Debug.Log("OnOptionsRequested firing");
            // Show options as popup instead of transitioning to options state
            await UIService.ShowPopupAsync<OptionsPopup>();
        }
        
        private async void OnCreditsRequested(CreditsRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.Credits);
        }
        
        private async void OnQuitRequested(QuitRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.Quit);
        }
        
        public override async Task ExitAsync()
        {
            // Unsubscribe from events using injected event system
            EventSystem.Unsubscribe<NewGameRequestedEvent>(OnNewGameRequested);
            EventSystem.Unsubscribe<LoadRequestedEvent>(OnContinueGameRequested);
            EventSystem.Unsubscribe<OptionsRequestedEvent>(OnOptionsRequested);
            EventSystem.Unsubscribe<CreditsRequestedEvent>(OnCreditsRequested);
            EventSystem.Unsubscribe<QuitRequestedEvent>(OnQuitRequested);
            
            // Hide any open popups
            await UIService.HidePopupAsync<OptionsPopup>();
            
            await UIService.HideScreenAsync<MainMenuScreen>();
            await base.ExitAsync();
        }
    }
}
