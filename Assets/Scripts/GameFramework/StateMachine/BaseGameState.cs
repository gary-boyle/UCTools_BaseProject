using System;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Interfaces;
using GameFramework.EventSystem.Events;
using GameFramework.Input.Interfaces;
using GameFramework.LoadSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;
using GameFramework.StateMachine.Interfaces;
using UnityEngine;

namespace GameFramework.StateMachine
{
    /// <summary>
    /// Base class for all game states with constructor injection support.
    /// All dependencies are provided via constructor rather than service locator pattern.
    /// Includes universal load game functionality that can be overridden by derived states.
    /// </summary>
    public abstract class BaseGameState
    {
        public GameStateType StateType { get; }
        public GameContext Context { get; protected set; }
        public bool IsActive { get; private set; }

        // Keep StateMachine separate since it's not in GameContext
        protected readonly IGameStateMachine StateMachine;

        // Service shortcuts
        protected IConsoleService ConsoleService => Context.ConsoleService;
        protected IEventSystem EventSystem => Context.EventSystem;
        protected IGameDataService GameDataService => Context.GameDataService;
        protected IGraphicsService GraphicsService => Context.GraphicsService;
        protected IInputManager InputManager => Context.InputManager;
        protected ILoadService LoadService => Context.LoadService;
        protected IPauseService PauseService => Context.PauseService;
        protected ISceneService SceneService => Context.SceneService;
        protected ITimeService TimeService => Context.TimeService;
        protected IUIService UIService => Context.UIService;
        protected IInstantiationService InstantiationService => Context.InstantiationService;

        protected BaseGameState(
            GameStateType stateType,
            GameContext context,
            IGameStateMachine stateMachine)
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

            // Subscribe to universal load game events
            SubscribeToLoadGameEvents();

            if (StateType != GameStateType.Bootstrap)
            {
                UIService.SetDebugPopupText(StateType.ToString());
            }

            Debug.Log($"[BaseGameState] Entered {StateType}");
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
            
            // Unsubscribe from universal load game events
            UnsubscribeFromLoadGameEvents();
            
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

        #region Universal Load Game Functionality

        /// <summary>
        /// Subscribes to load game events - called automatically in EnterAsync
        /// </summary>
        private void SubscribeToLoadGameEvents()
        {
            EventSystem?.Subscribe<BeginLoadGameEvent>(OnBeginLoadGameRequested);
        }

        /// <summary>
        /// Unsubscribes from load game events - called automatically in ExitAsync
        /// </summary>
        private void UnsubscribeFromLoadGameEvents()
        {
            EventSystem?.Unsubscribe<BeginLoadGameEvent>(OnBeginLoadGameRequested);
        }

        /// <summary>
        /// Handles begin load game requests - can be overridden by derived states
        /// Default behavior is to transition to Loading state
        /// </summary>
        protected virtual async void OnBeginLoadGameRequested(BeginLoadGameEvent evt)
        {
            if (evt?.SaveFileInfo == null)
            {
                Debug.LogError($"[{StateType}] Received load event with null save file info");
                return;
            }

            // Check if loading is allowed from current state
            if (!CanLoadFromCurrentState())
            {
                Debug.LogWarning($"[{StateType}] Load game not allowed from current state");
                return;
            }

            Debug.Log($"[{StateType}] Load game requested for: {evt.SaveFileInfo.FileName}, transitioning to loading state...");
            
            try
            {
                await UIService.CloseAllPopupsAsync();
                // Default behavior: transition to loading state
                await TransitionToStateAsync(GameStateType.Loading);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{StateType}] Failed to transition to loading state: {ex.Message}");
            }
        }

        /// <summary>
        /// Determines if loading is allowed from the current state
        /// Can be overridden by derived states to implement custom logic
        /// </summary>
        protected virtual bool CanLoadFromCurrentState()
        {
            // Don't allow loading if already in loading state
            if (StateType == GameStateType.Loading)
            {
                Debug.LogWarning($"[{StateType}] Cannot load game - already in loading state");
                return false;
            }

            // Don't allow loading if LoadService is already busy
            if (LoadService?.IsLoading == true)
            {
                Debug.LogWarning($"[{StateType}] Cannot load game - LoadService is already loading");
                return false;
            }

            // Default: allow loading from any other state
            return true;
        }

        #endregion
    }
}
