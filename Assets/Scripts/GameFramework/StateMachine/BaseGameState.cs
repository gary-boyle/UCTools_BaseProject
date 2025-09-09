using System;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Input.Interfaces;
using GameFramework.Services;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;
using UnityEngine;

namespace GameFramework.StateMachine
{
    /// <summary>
    /// Base class for all game states with constructor injection support.
    /// All dependencies are provided via constructor rather than service locator pattern.
    /// </summary>
    public abstract class BaseGameState
    {
        public GameStateType StateType { get; }
        public GameContext Context { get; protected set; }
        public bool IsActive { get; private set; }
        
        // Injected dependencies
        protected readonly IGameStateMachine StateMachine;
        protected readonly IEventSystem EventSystem;
        protected readonly IAudioService AudioService;
        protected readonly IUIService UIService;
        protected readonly IInputManager InputManager;
        protected readonly IConsoleService ConsoleService;
        protected readonly IGameDataService GameDataService;

        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        protected BaseGameState(
            GameStateType stateType,
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputManager inputService,
            IConsoleService consoleService,
            IGameDataService gameDataService)
        {
            StateType = stateType;
            StateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            EventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            AudioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            UIService = uiService ?? throw new ArgumentNullException(nameof(uiService));
            InputManager = inputService ?? throw new ArgumentNullException(nameof(inputService));
            ConsoleService = consoleService ?? throw new ArgumentNullException(nameof(consoleService));
            GameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
        }
        
        /// <summary>
        /// Called when entering this state. Setup UI, subscribe to events, initialize state-specific systems.
        /// </summary>
        public virtual async Task EnterAsync(GameContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            IsActive = true;

            if (StateType != GameStateType.Bootstrap)
            {
                UIService.SetDebugPopupText(StateType.ToString());
            }
            
            // Publish state change event using injected event system
            EventSystem.Publish(new GameStateChangeEvent 
            { 
                NewState = StateType, 
                Context = context 
            });
        }
        
        /// <summary>
        /// Called every frame while this state is active
        /// </summary>
        public virtual void Update() { }
        
        /// <summary>
        /// Called at fixed intervals while this state is active
        /// </summary>
        public virtual void FixedUpdate() { }
        
        /// <summary>
        /// Called when exiting this state. Cleanup UI, unsubscribe from events, save state if needed.
        /// </summary>
        public virtual async Task ExitAsync()
        {
            Debug.Log($"[GameState] Exiting {StateType}");
            IsActive = false;
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Handle input events specific to this state
        /// </summary>
        public virtual void HandleInput() { }
        
        /// <summary>
        /// Helper method for state transitions using injected state machine
        /// </summary>
        protected async Task TransitionToStateAsync(GameStateType newStateType)
        {
            await StateMachine.ChangeStateAsync(newStateType);
        }
    }
}