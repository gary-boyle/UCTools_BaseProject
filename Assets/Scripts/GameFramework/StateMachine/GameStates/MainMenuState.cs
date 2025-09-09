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
        private readonly IInputManager _inputManager;

        public MainMenuState(
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputManager inputManager,
            IConsoleService consoleService,
            IGameDataService gameDataService)
            : base(GameStateType.MainMenu, stateMachine, eventSystem, audioService, uiService, inputManager, consoleService, gameDataService)
        {
            _inputManager = inputManager;
        }
        
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
            // Declare that we need UI input
            _inputManager.SetInputContext(InputContext.UI);
            
            // Subscribe to menu events using injected event system
            EventSystem.Subscribe<NewGameRequestedEvent>(OnNewGameRequested);
            //EventSystem.Subscribe<LoadWindowLoadRequestedEvent>(OnContinueGameRequested);
            EventSystem.Subscribe<OptionsRequestedEvent>(OnOptionsRequested);
            EventSystem.Subscribe<LoadWindowRequestedEvent>(OnLoadWindowRequested);
            EventSystem.Subscribe<CreditsRequestedEvent>(OnCreditsRequested);
            EventSystem.Subscribe<QuitRequestedEvent>(OnQuitRequested);
            
            await UIService.ShowScreenAsync<MainMenuScreen>();
            AudioService.PlayMusic("main_menu");
        }
        
        private async void OnNewGameRequested(NewGameRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.NewGame);
        }
        
        private async void OnContinueGameRequested(LoadWindowRequestedEvent evt)
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
        
        private async void OnLoadWindowRequested(LoadWindowRequestedEvent evt)
        {
            Debug.Log("OnOptionsRequested firing");
            // Show options as popup instead of transitioning to options state
            await UIService.ShowPopupAsync<LoadGamePopup>();
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
            //EventSystem.Unsubscribe<LoadWindowRequestedEvent>(OnContinueGameRequested);
            EventSystem.Unsubscribe<OptionsRequestedEvent>(OnOptionsRequested);
            EventSystem.Unsubscribe<LoadWindowRequestedEvent>(OnLoadWindowRequested);
            EventSystem.Unsubscribe<CreditsRequestedEvent>(OnCreditsRequested);
            EventSystem.Unsubscribe<QuitRequestedEvent>(OnQuitRequested);
            
            // Hide any open popups
            await UIService.HidePopupAsync<OptionsPopup>();
            
            await UIService.HideScreenAsync<MainMenuScreen>();
            await base.ExitAsync();
        }
    }
}
