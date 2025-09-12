using System;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Input.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;
using GameFramework.StateMachine.Interfaces;
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

        // Keep StateMachine separate since it's not in GameContext
        protected readonly IGameStateMachine StateMachine;
    
        // Optional: Keep ConsoleService separate if not in GameContext
        //protected readonly IConsoleService ConsoleService;

        protected IAudioService AudioService => Context.AudioService;
        protected IConsoleService ConsoleService => Context.ConsoleService;
        protected IEventSystem EventSystem => Context.EventSystem;
        protected IGameDataService GameDataService => Context.GameDataService;
        protected IGraphicsService GraphicsService => Context.GraphicsService;
        protected IInputManager InputManager => Context.InputManager;
        protected ILoadService LoadService => Context.LoadService;
        protected IPauseService PauseService => Context.PauseService;
        protected ISaveService SaveService => Context.SaveService;
        protected ISceneService SceneService => Context.SceneService;
        protected ITimeService TimeService => Context.TimeService;
        protected IUIService UIService => Context.UIService;

        
        protected BaseGameState(
            GameStateType stateType,
            GameContext context,
            IGameStateMachine stateMachine) // Optional for when console is disabled
        {
            StateType = stateType;
            Context = context ?? throw new ArgumentNullException(nameof(context));
            StateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
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
            Debug.Log($"[BaseGameState] Exiting {StateType}");
            IsActive = false;
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Helper method for state transitions using injected state machine
        /// </summary>
        protected async Task TransitionToStateAsync(GameStateType newStateType)
        {
            await StateMachine.ChangeStateAsync(newStateType);
        }
    }
}
