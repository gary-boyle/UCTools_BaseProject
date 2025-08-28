using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;
using GameFramework.UI.Screens;
using UnityEngine;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Paused state with constructor injection and event-driven input handling
    /// </summary>
    public class PausedState : BaseGameState
    {
        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public PausedState(
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputService inputService,
            IConsoleService consoleService,
            IGameDataService gameDataService)  
            : base(GameStateType.Paused, stateMachine, eventSystem, audioService, uiService, inputService, consoleService, gameDataService)
        {
        }
        
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
            // Pause game time
            Time.timeScale = 0f;
            
            // Show pause overlay using injected UI service
            await UIService.ShowScreenAsync<PauseScreen>();
            
            // Lower game audio volume using injected audio service
            AudioService.SetMasterVolume(0.3f);
            
            // Subscribe to pause menu events using injected event system
            EventSystem.Subscribe<ResumeRequestedEvent>(OnResumeRequested);
            EventSystem.Subscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            EventSystem.Subscribe<OptionsRequestedEvent>(OnOptionsRequested);
            EventSystem.Subscribe<GamePausedEvent>(OnPauseInput);

            // Subscribe to input events for resume functionality
            EventSystem.Subscribe<UICancelInputEvent>(OnCancelInput);
            // If you add a dedicated Pause action, subscribe to it here:
            // EventSystem.Subscribe<PlayerPauseInputEvent>(OnPauseInput);
            
            // Publish pause event using injected event system
            EventSystem.Publish<GamePausedEvent>();
        }
        
        /// <summary>
        /// Handle cancel/escape input for resuming
        /// </summary>
        private async void OnCancelInput(UICancelInputEvent evt)
        {
            await TransitionToStateAsync(GameStateType.Playing);
        }
        
        /// <summary>
        /// Handle dedicated pause input (if you add a Pause action)
        /// </summary>
        // private async void OnPauseInput(PlayerPauseInputEvent evt)
        // {
        //     await TransitionToStateAsync(GameStateType.Playing);
        // }
        
        private async void OnResumeRequested(ResumeRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.Playing);
        }
        
        private async void OnMainMenuRequested(MainMenuRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.MainMenu);
        }
        
        private async void OnOptionsRequested(OptionsRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.Options);
        }
        
        private async void OnPauseInput(GamePausedEvent evt)
        {
            // Not sure if I should actually do anythign here.
            //await TransitionToStateAsync(GameStateType.Playing);
        }
        
        public override async Task ExitAsync()
        {
            // Resume game time
            Time.timeScale = 1f;
            
            // Restore audio volume using injected audio service
            AudioService.SetMasterVolume(1f);
            
            // Unsubscribe from events using injected event system
            EventSystem.Unsubscribe<ResumeRequestedEvent>(OnResumeRequested);
            EventSystem.Unsubscribe<MainMenuRequestedEvent>(OnMainMenuRequested);
            EventSystem.Unsubscribe<OptionsRequestedEvent>(OnOptionsRequested);
            EventSystem.Unsubscribe<GamePausedEvent>(OnPauseInput);

            // Unsubscribe from input events
            EventSystem.Unsubscribe<UICancelInputEvent>(OnCancelInput);
            // EventSystem.Unsubscribe<PlayerPauseInputEvent>(OnPauseInput);
            
            await UIService.HideScreenAsync<PauseScreen>();
            
            // Publish resume event using injected event system
            EventSystem.Publish<GameResumedEvent>();
            
            await base.ExitAsync();
        }
    }
}
