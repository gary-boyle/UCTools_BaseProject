using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;
using GameFramework.UI.Screens;
using GameFramework.UI.Popups;
using UnityEngine;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Main menu state with constructor injection for all dependencies
    /// </summary>
    public class MainMenuState : BaseGameState
    {
        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public MainMenuState(
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputService inputService,
            IConsoleService consoleService)  
            : base(GameStateType.MainMenu, stateMachine, eventSystem, audioService, uiService, inputService, consoleService)
        {
        }
        
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
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
            // Load the most recent save using context services
            await Context.SaveService.LoadMostRecentSaveAsync();
            await TransitionToStateAsync(GameStateType.Loading);
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
