using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.EventSystem.Interfaces;
using GameFramework.StateMachine.Enum;
using GameFramework.StateMachine.GameStates;
using GameFramework.StateMachine.Interfaces;
using UnityEngine;
using IFixedUpdatable = GameFramework.StateMachine.Interfaces.IFixedUpdatable;
using IUpdatable = GameFramework.StateMachine.Interfaces.IUpdatable;

namespace GameFramework.StateMachine
{
    /// <summary>
    /// Game State Machine manages game state transitions with proper event publishing
    /// Ensures TimeService and other systems receive correct state change notifications
    /// 
    /// Design: Uses dependency injection and factory pattern for state creation
    /// Pros: Clean separation of concerns, proper event handling, extensible state creation
    /// Cons: Requires all states to be registered in factory
    /// </summary>
    public class GameStateMachine : IGameStateMachine, IUpdatable, IFixedUpdatable
    {
        public GameStateType CurrentStateType => CurrentState?.StateType ?? GameStateType.Bootstrap;
        public BaseGameState CurrentState { get; private set; }
        public bool IsInitialized { get; private set; }
        
        private readonly Dictionary<GameStateType, BaseGameState> _states = new Dictionary<GameStateType, BaseGameState>();
        private readonly Stack<GameStateType> _stateHistory = new Stack<GameStateType>();
        private readonly HashSet<(GameStateType from, GameStateType to)> _validTransitions = new HashSet<(GameStateType, GameStateType)>();
        private readonly GameContext _context;
        private readonly IEventSystem _eventSystem;
        private readonly DiContainer _container;
        
        /// <summary>
        /// All possible game states defined explicitly for Unity compatibility
        /// This avoids using Enum.GetValues which may not be available in all Unity .NET versions
        /// </summary>
        private static readonly GameStateType[] AllGameStates = new GameStateType[]
        {
            GameStateType.Bootstrap,
            GameStateType.Splash,
            GameStateType.MainMenu,
            GameStateType.Loading,
            GameStateType.NewGame,
            GameStateType.Playing,
            GameStateType.Credits,
            GameStateType.GameOver,
            GameStateType.Victory,
            GameStateType.Quit
        };
        
        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public GameStateMachine(GameContext context, IEventSystem eventSystem, DiContainer container)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }
        
        public async Task InitializeAsync()
        {
            if (IsInitialized) return;
            
            // Register all states using DI container for constructor injection
            RegisterStates();
            
            // Define valid state transitions
            DefineStateTransitions();
            IsInitialized = true;
            
            // Start with bootstrap state
            await ChangeStateAsync(GameStateType.Bootstrap);
        }
        
        public void Shutdown()
        {
            CurrentState?.ExitAsync();
            
            _states.Clear();
            _stateHistory.Clear();
            IsInitialized = false;
        }
        
        /// <summary>
        /// Changes game state and publishes GameStateChangeEvent for TimeService and other systems
        /// This is the KEY method that was missing the event publication
        /// </summary>
        public async Task ChangeStateAsync(GameStateType newStateType)
        {
            if (!CanTransitionTo(newStateType))
            {
                Debug.LogError($"[GameStateMachine] Invalid transition from {CurrentStateType} to {newStateType}");
                return;
            }
            
            // Store the previous state for the event
            var previousStateType = CurrentState?.StateType ?? GameStateType.Bootstrap;
            
            try
            {
                // Exit current state
                if (CurrentState != null)
                {
                    await CurrentState.ExitAsync();
                    _stateHistory.Push(previousStateType);
                }
                
                // This ensures TimeService gets notified of the state change
                _eventSystem.Publish(new GameStateChangeEvent 
                { 
                    PreviousState = previousStateType,
                    NewState = newStateType, 
                    Context = _context 
                });
                
                // Enter new state
                if (_states.TryGetValue(newStateType, out var newState))
                {
                    CurrentState = newState;
                    await CurrentState.EnterAsync(_context);
                }
                else
                {
                    Debug.LogError($"[GameStateMachine] State {newStateType} not registered!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameStateMachine] Error during state transition from {previousStateType} to {newStateType}: {e}");
                throw;
            }
        }
        
        public async Task ChangeStateAsync<T>() where T : BaseGameState
        {
            var state = _states.Values.FirstOrDefault(s => s is T);
            if (state != null)
            {
                await ChangeStateAsync(state.StateType);
            }
            else
            {
                Debug.LogError($"[GameStateMachine] State of type {typeof(T).Name} not found!");
            }
        }
        
        public bool CanTransitionTo(GameStateType stateType)
        {
            if (CurrentState == null) return stateType == GameStateType.Bootstrap;
            return _validTransitions.Contains((CurrentStateType, stateType));
        }
        
        public void RegisterState(BaseGameState state)
        {
            _states[state.StateType] = state;
        }
        
        public void Update()
        {
            CurrentState?.Update();
        }
        
        public void FixedUpdate()
        {
            CurrentState?.FixedUpdate();
        }
        
        /// <summary>
        /// Register all available game states using DI container for constructor injection
        /// </summary>
        private void RegisterStates()
        {
            try
            {
                // Use DI container to create states with all dependencies injected
                RegisterState(_container.Resolve<BootstrapState>());
                RegisterState(_container.Resolve<SplashState>());
                RegisterState(_container.Resolve<MainMenuState>());
                RegisterState(_container.Resolve<LoadingState>());
                RegisterState(_container.Resolve<NewGameState>());
                RegisterState(_container.Resolve<PlayingState>());
                RegisterState(_container.Resolve<CreditsState>());
                RegisterState(_container.Resolve<GameOverState>());
                RegisterState(_container.Resolve<VictoryState>());
                RegisterState(_container.Resolve<QuitState>());
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameStateMachine] Error registering states: {e}");
                throw;
            }
        }
        
        /// <summary>
        /// Define which state transitions are valid to prevent invalid state changes.
        /// Uses explicit state definitions for maximum Unity compatibility.
        /// This approach avoids potential issues with Enum.GetValues in older Unity versions.
        /// </summary>
        private void DefineStateTransitions()
        {
            // Bootstrap transition
            _validTransitions.Add((GameStateType.Bootstrap, GameStateType.Splash));
            
            // Splash screen transitions
            _validTransitions.Add((GameStateType.Splash, GameStateType.MainMenu));
            _validTransitions.Add((GameStateType.Splash, GameStateType.Loading));
            
            // Main Menu transitions
            _validTransitions.Add((GameStateType.MainMenu, GameStateType.NewGame));
            _validTransitions.Add((GameStateType.MainMenu, GameStateType.Loading));
            _validTransitions.Add((GameStateType.MainMenu, GameStateType.Credits));
            _validTransitions.Add((GameStateType.MainMenu, GameStateType.Quit));
            
            // Loading screen transitions
            _validTransitions.Add((GameStateType.Loading, GameStateType.Playing));
            _validTransitions.Add((GameStateType.Loading, GameStateType.MainMenu));
            _validTransitions.Add((GameStateType.Loading, GameStateType.GameOver));
            
            // New Game setup transitions
            _validTransitions.Add((GameStateType.NewGame, GameStateType.Loading));
            _validTransitions.Add((GameStateType.NewGame, GameStateType.MainMenu));
            _validTransitions.Add((GameStateType.NewGame, GameStateType.Playing));
            
            // Playing game state transitions
            _validTransitions.Add((GameStateType.Playing, GameStateType.GameOver));
            _validTransitions.Add((GameStateType.Playing, GameStateType.Victory));
            _validTransitions.Add((GameStateType.Playing, GameStateType.MainMenu));
            _validTransitions.Add((GameStateType.Playing, GameStateType.Loading));
            _validTransitions.Add((GameStateType.Playing, GameStateType.Quit));

            // Credits transitions
            _validTransitions.Add((GameStateType.Credits, GameStateType.MainMenu));
            _validTransitions.Add((GameStateType.Credits, GameStateType.Quit));
            
            // Game Over transitions
            _validTransitions.Add((GameStateType.GameOver, GameStateType.MainMenu));
            _validTransitions.Add((GameStateType.GameOver, GameStateType.NewGame));
            _validTransitions.Add((GameStateType.GameOver, GameStateType.Loading));
            _validTransitions.Add((GameStateType.GameOver, GameStateType.Quit));
            
            // Victory transitions
            _validTransitions.Add((GameStateType.Victory, GameStateType.MainMenu));
            _validTransitions.Add((GameStateType.Victory, GameStateType.Credits));
            _validTransitions.Add((GameStateType.Victory, GameStateType.NewGame));
            _validTransitions.Add((GameStateType.Victory, GameStateType.Quit));
        }
    }
}
