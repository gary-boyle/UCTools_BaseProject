using System;
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
    /// Playing state with constructor injection and event-driven input handling
    /// </summary>
    public class PlayingState : BaseGameState
    {
        protected readonly IGameDataService GameDataService;

        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public PlayingState(
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputService inputService,
            IConsoleService consoleService,
            IGameDataService gameDataService)  
            : base(GameStateType.Playing, stateMachine, eventSystem, audioService, uiService, inputService, consoleService, gameDataService)
        {
            GameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));

        }
        
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
            // Show game HUD using injected UI service
            await UIService.ShowScreenAsync<GamePlayScreen>();
            
            // Start gameplay music using injected audio service
            AudioService.PlayMusic("gameplay");
            
            // Resume time if it was paused
            Time.timeScale = 1f;
            
            // Subscribe to game events using injected event system
            EventSystem.Subscribe<PauseRequestedEvent>(OnPauseRequested);
            EventSystem.Subscribe<GameOverEvent>(OnGameOver);
            EventSystem.Subscribe<VictoryEvent>(OnVictory);
            
            // Subscribe to input events for pause functionality
            EventSystem.Subscribe<UICancelInputEvent>(OnCancelInput);
            
            // If you add a dedicated Pause action, subscribe to it here:
            // EventSystem.Subscribe<PlayerPauseInputEvent>(OnPauseInput);
            
            // Subscribe to other gameplay input events as needed
            EventSystem.Subscribe<PlayerAttackInputEvent>(OnAttackInput);
            EventSystem.Subscribe<PlayerJumpInputEvent>(OnJumpInput);
            EventSystem.Subscribe<PlayerInteractInputEvent>(OnInteractInput);
            EventSystem.Subscribe<PlayerMoveInputEvent>(OnMoveInput);
            EventSystem.Subscribe<PlayerLookInputEvent>(OnLookInput);
            EventSystem.Subscribe<PlayerSprintInputEvent>(OnSprintInput);
            EventSystem.Subscribe<PlayerCrouchInputEvent>(OnCrouchInput);
            
            // Publish game started event using injected event system
            EventSystem.Publish<GameStartedEvent>();
        }
        
        public override void Update()
        {
            // No longer needed! Input is handled via events
            // The event-driven InputService will automatically handle input and publish events
        }
        
        #region Input Event Handlers
        
        /// <summary>
        /// Handle cancel/escape input for pausing
        /// </summary>
        private async void OnCancelInput(UICancelInputEvent evt)
        {
            EventSystem.Publish(new PauseRequestedEvent());
        }
        
        /// <summary>
        /// Handle dedicated pause input (if you add a Pause action)
        /// </summary>
        // private async void OnPauseInput(PlayerPauseInputEvent evt)
        // {
        //     if (evt.Phase == InputActionPhase.Performed)
        //     {
        //         EventSystem.Publish(new PauseRequestedEvent());
        //     }
        // }
        
        /// <summary>
        /// Handle player attack input
        /// </summary>
        private void OnAttackInput(PlayerAttackInputEvent evt)
        {
            // Handle attack logic here or publish more specific events
            Debug.Log($"[PlayingState] Player attack input: {evt.Phase}");
            
            // Example: Publish attack command to game systems
            // EventSystem.Publish(new PlayerAttackCommandEvent());
        }
        
        /// <summary>
        /// Handle player jump input
        /// </summary>
        private void OnJumpInput(PlayerJumpInputEvent evt)
        {
            Debug.Log("[PlayingState] Player jump input");
            
            // Example: Publish jump command to player controller
            // EventSystem.Publish(new PlayerJumpCommandEvent());
        }
        
        /// <summary>
        /// Handle player interact input
        /// </summary>
        private void OnInteractInput(PlayerInteractInputEvent evt)
        {
            Debug.Log($"[PlayingState] Player interact input: {evt.Phase}");
            
            // Example: Check for nearby interactables and interact
            // EventSystem.Publish(new PlayerInteractCommandEvent());
        }
        
        /// <summary>
        /// Handle player movement input
        /// </summary>
        private void OnMoveInput(PlayerMoveInputEvent evt)
        {
            // Handle movement (this will be called frequently)
            // Example: Update player controller with movement vector
            // EventSystem.Publish(new PlayerMoveCommandEvent(evt.MovementVector));
        }
        
        /// <summary>
        /// Handle player look input
        /// </summary>
        private void OnLookInput(PlayerLookInputEvent evt)
        {
            // Handle camera/look input (this will be called frequently)
            // Example: Update camera controller with look delta
            // EventSystem.Publish(new PlayerLookCommandEvent(evt.LookDelta));
        }
        
        /// <summary>
        /// Handle player sprint input
        /// </summary>
        private void OnSprintInput(PlayerSprintInputEvent evt)
        {
            Debug.Log($"[PlayingState] Player sprint input: {evt.Phase}");
            
            // Example: Toggle sprint mode
            // EventSystem.Publish(new PlayerSprintCommandEvent(evt.Phase == InputActionPhase.Started));
        }
        
        /// <summary>
        /// Handle player crouch input
        /// </summary>
        private void OnCrouchInput(PlayerCrouchInputEvent evt)
        {
            Debug.Log($"[PlayingState] Player crouch input: {evt.Phase}");
            
            // Example: Toggle crouch mode
            // EventSystem.Publish(new PlayerCrouchCommandEvent(evt.Phase == InputActionPhase.Started));
        }
        
        #endregion
        
        #region Game Event Handlers
        
        private async void OnPauseRequested(PauseRequestedEvent evt)
        {
            await TransitionToStateAsync(GameStateType.Paused);
        }
        
        private async void OnGameOver(GameOverEvent evt)
        {
            await TransitionToStateAsync(GameStateType.GameOver);
        }
        
        private async void OnVictory(VictoryEvent evt)
        {
            await TransitionToStateAsync(GameStateType.Victory);
        }
        
        #endregion
        
        public override async Task ExitAsync()
        {
            // Unsubscribe from game events using injected event system
            EventSystem.Unsubscribe<PauseRequestedEvent>(OnPauseRequested);
            EventSystem.Unsubscribe<GameOverEvent>(OnGameOver);
            EventSystem.Unsubscribe<VictoryEvent>(OnVictory);
            
            // Unsubscribe from input events
            EventSystem.Unsubscribe<UICancelInputEvent>(OnCancelInput);
            // EventSystem.Unsubscribe<PlayerPauseInputEvent>(OnPauseInput);
            
            EventSystem.Unsubscribe<PlayerAttackInputEvent>(OnAttackInput);
            EventSystem.Unsubscribe<PlayerJumpInputEvent>(OnJumpInput);
            EventSystem.Unsubscribe<PlayerInteractInputEvent>(OnInteractInput);
            EventSystem.Unsubscribe<PlayerMoveInputEvent>(OnMoveInput);
            EventSystem.Unsubscribe<PlayerLookInputEvent>(OnLookInput);
            EventSystem.Unsubscribe<PlayerSprintInputEvent>(OnSprintInput);
            EventSystem.Unsubscribe<PlayerCrouchInputEvent>(OnCrouchInput);
            
            await UIService.HideScreenAsync<GamePlayScreen>();
            
            // Publish game ended event using injected event system
            EventSystem.Publish<GameEndedEvent>();
            
            await base.ExitAsync();
        }
    }
}
