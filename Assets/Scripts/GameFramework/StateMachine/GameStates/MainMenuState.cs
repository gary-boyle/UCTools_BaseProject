using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Input;
using GameFramework.Input.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;
using GameFramework.StateMachine.Interfaces;
using GameFramework.UI.Screens;
using GameFramework.UI.Popups;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Main menu state - fully responsible for its UI lifecycle
    /// Handles all UI transitions based on user interactions reported by the screen
    /// </summary>
    public class MainMenuState : BaseGameState
    {
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
        }
        
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
            InputManager.SetInputContext(InputContext.UI);
            
            // Subscribe to user interaction events from UI
            EventSystem.Subscribe<NewGameRequestedEvent>(OnNewGameRequested);
            EventSystem.Subscribe<OptionsRequestedEvent>(OnOptionsRequested);
            EventSystem.Subscribe<LoadWindowRequestedEvent>(OnLoadWindowRequested);
            EventSystem.Subscribe<CreditsRequestedEvent>(OnCreditsRequested);
            EventSystem.Subscribe<QuitRequestedEvent>(OnQuitRequested);
            
            // State is responsible for showing its UI
            await UIService.ShowScreenAsync<MainMenuScreen>();
            AudioService.PlayMusic("main_menu");
        }
        
        /// <summary>
        /// Handle new game request - state manages UI transition
        /// </summary>
        private async void OnNewGameRequested(NewGameRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.NewGame);
        }
        
        /// <summary>
        /// Handle options request - state decides to show popup instead of state transition
        /// </summary>
        private async void OnOptionsRequested(OptionsRequestedEvent evt)
        {
            await UIService.ShowPopupAsync<OptionsPopup>();
        }
        
        /// <summary>
        /// Handle load window request - state decides to show popup
        /// </summary>
        private async void OnLoadWindowRequested(LoadWindowRequestedEvent evt)
        {
            await UIService.ShowPopupAsync<LoadGamePopup>();
        }
        
        /// <summary>
        /// Handle credits request - state manages UI transition
        /// </summary>
        private async void OnCreditsRequested(CreditsRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.Credits);
        }
        
        /// <summary>
        /// Handle quit request - state manages UI transition
        /// </summary>
        private async void OnQuitRequested(QuitRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.Quit);
        }
        
        public override async Task ExitAsync()
        {
            // Unsubscribe from events
            EventSystem.Unsubscribe<NewGameRequestedEvent>(OnNewGameRequested);
            EventSystem.Unsubscribe<OptionsRequestedEvent>(OnOptionsRequested);
            EventSystem.Unsubscribe<LoadWindowRequestedEvent>(OnLoadWindowRequested);
            EventSystem.Unsubscribe<CreditsRequestedEvent>(OnCreditsRequested);
            EventSystem.Unsubscribe<QuitRequestedEvent>(OnQuitRequested);
            
            // State is responsible for cleaning up its UI
            await UIService.HidePopupAsync<OptionsPopup>();
            await UIService.HidePopupAsync<LoadGamePopup>();
            await UIService.HideScreenAsync<MainMenuScreen>();
            
            await base.ExitAsync();
        }
    }
}
