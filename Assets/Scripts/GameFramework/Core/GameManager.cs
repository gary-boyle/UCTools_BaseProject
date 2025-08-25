using System.Collections.Generic;
using System.Threading.Tasks;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine;
using GameFramework.StateMachine.GameStates;
using GameFramework.StateMachine.Interfaces;
using UCTools_Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameFramework.Core
{
    /// <summary>
    /// Main game manager that bootstraps the entire game framework using dependency injection.
    /// Registers all services, initializes the DI container, and starts the game state machine.
    /// 
    /// Design: Bootstrap pattern with dependency injection container setup
    /// Pros: Clean initialization, all dependencies properly registered, easy to modify service registrations
    /// Cons: Single point of failure, requires careful initialization order
    /// </summary>
    public class GameManager : Singleton<GameManager> 
    {
        [Header("Debug Settings")]
        [SerializeField] private bool _enableDebugConsole = true;
        [SerializeField] private bool _enableVerboseLogging = false;
        
        [Header("Prefabs")]
        [SerializeField] private UIDocument _UIPrefab;

        // Core systems
        private DIContainer _container;
        private IGameStateMachine _stateMachine;
        private List<IUpdatable> _updatables = new();
        private List<IFixedUpdatable> _fixedUpdatables = new();
        private List<ILateUpdatable> _lateUpdatables = new();
        
        // Singleton pattern implementation
        public static GameManager Instance { get; private set; }
        
        /// <summary>
        /// Initialize the singleton and start the dependency injection setup
        /// </summary>
        private async void Awake() 
        {
            base.Awake();
            await InitializeFrameworkAsync();
        }
        
        private void Update() 
        {
            // Update all systems that implement IUpdatable
            foreach (var updatable in _updatables)
            {
                updatable.Update();
            }
        }
        
        private void FixedUpdate()
        {
            // Update all systems that implement IFixedUpdatable
            foreach (var fixedUpdatable in _fixedUpdatables)
            {
                fixedUpdatable.FixedUpdate();
            }
        }
        
        private void LateUpdate()
        {
            // Update all systems that implement ILateUpdatable
            foreach (var lateUpdatable in _lateUpdatables)
            {
                lateUpdatable.LateUpdate();
            }
        }
        
        /// <summary>
        /// Complete framework initialization using dependency injection
        /// </summary>
        private async Task InitializeFrameworkAsync()
        {
            Debug.Log("[GameManager] Initializing game framework with dependency injection...");
            
            // Initialize DI container
            _container = DIContainer.Instance;
            
            // Register all services in dependency order
            RegisterCoreServices();
            RegisterGameSystems();
            RegisterGameStates();
            
            // Initialize ConfigVar system
            UCTools_ConfigVariables.ConfigVar.Init();
            
            // Create and initialize the state machine
            _stateMachine = _container.Resolve<IGameStateMachine>();
            await _stateMachine.InitializeAsync();
            
            // Collect updatable systems
            CollectUpdatableSystems();
            
            Debug.Log("[GameManager] Framework initialization complete!");
        }
        
        /// <summary>
        /// Register core services that other systems depend on
        /// Order matters for dependency resolution
        /// </summary>
        private void RegisterCoreServices()
        {
            Debug.Log("[GameManager] Registering core services...");
            
            // Register the DI container itself (for cases where services need to resolve other services)
            _container.RegisterSingleton(_container);
            
            // Register leaf services first (no dependencies)
            _container.RegisterSingleton<IEventSystem, EventSystem.EventSystem>();
            
            // Register services with minimal dependencies
            _container.RegisterSingleton<IAudioService, AudioService>();
            _container.RegisterSingleton<IInputService, InputService>();
            _container.RegisterSingleton<ISceneService, SceneService>();
            
            // CREATE AND REGISTER UI DOCUMENT BEFORE UI SERVICE
            RegisterUIDocument();
            
            // Register services that might depend on the above
            _container.RegisterSingleton<IUIService, UIService>();
            _container.RegisterSingleton<ISaveService, SaveService>();
            _container.RegisterSingleton<IConfigService, ConfigService>();
            
            // Register GameContext (depends on all other services)
            _container.RegisterSingleton<GameContext>();
            
            // Register state machine (depends on GameContext)
            _container.RegisterSingleton<IGameStateMachine, StateMachine.GameStateMachine>();
        }
        
        
        /// <summary>
        /// Create UIDocument instance from prefab and register it in the DI container
        /// </summary>
        private void RegisterUIDocument()
        {
            Debug.Log("[GameManager] Setting up UI Document from prefab...");
    
            if (_UIPrefab == null)
            {
                Debug.LogError("[GameManager] UI Prefab is not assigned! Please assign it in the inspector.");
                return;
            }
    
            try
            {
                // Instantiate the UI prefab
                var uiInstance = Instantiate(_UIPrefab);
                uiInstance.name = "UI Document (Runtime)";
        
                // Make it persist across scene loads
                DontDestroyOnLoad(uiInstance.gameObject);
        
                // Register the UIDocument instance directly in the container
                _container.RegisterSingleton(uiInstance);
        
                Debug.Log("[GameManager] UIDocument registered successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameManager] Failed to create UIDocument from prefab: {e.Message}");
            }
        }
        
        /// <summary>
        /// Register additional game systems and managers
        /// </summary>
        private void RegisterGameSystems()
        {
            Debug.Log("[GameManager] Registering game systems...");
            
            // Register any additional game-specific systems here
            // Example:
            // _container.RegisterSingleton<IPlayerManager, PlayerManager>();
            // _container.RegisterSingleton<IEnemyManager, EnemyManager>();
            // _container.RegisterSingleton<IInventorySystem, InventorySystem>();
            
            if (_enableDebugConsole)
            {
                // Register console system for debugging
                // _container.RegisterSingleton<IConsoleSystem, ConsoleSystem>();
            }
        }
        
        /// <summary>
        /// Register all game states for DI container to create with proper injection
        /// </summary>
        private void RegisterGameStates()
        {
            Debug.Log("[GameManager] Registering game states...");
            
            // Register all game states as transient (new instance each time, but they're cached by state machine)
            _container.RegisterTransient<BootstrapState, BootstrapState>();
            _container.RegisterTransient<SplashState, SplashState>();
            _container.RegisterTransient<MainMenuState, MainMenuState>();
            _container.RegisterTransient<LoadingState, LoadingState>();
            _container.RegisterTransient<NewGameState, NewGameState>();
            _container.RegisterTransient<PlayingState, PlayingState>();
            _container.RegisterTransient<PausedState, PausedState>();
            _container.RegisterTransient<OptionsState, OptionsState>();
            _container.RegisterTransient<CreditsState, CreditsState>();
            _container.RegisterTransient<GameOverState, GameOverState>();
            _container.RegisterTransient<VictoryState, VictoryState>();
            _container.RegisterTransient<QuitState, QuitState>();
        }
        
        /// <summary>
        /// Collect all systems that need frame updates
        /// </summary>
        private void CollectUpdatableSystems()
        {
            Debug.Log("[GameManager] Collecting updatable systems...");
            
            // Add state machine to updatables
            if (_stateMachine is IUpdatable updatable)
                _updatables.Add(updatable);
                
            if (_stateMachine is IFixedUpdatable fixedUpdatable)
                _fixedUpdatables.Add(fixedUpdatable);
            
            // Add other systems that need updates
            var inputService = _container.Resolve<IInputService>();
            if (inputService is IUpdatable inputUpdatable)
                _updatables.Add(inputUpdatable);
                
            // Add more systems as needed
        }
        
        /// <summary>
        /// Get access to a service from anywhere in the game
        /// Use sparingly - prefer constructor injection where possible
        /// </summary>
        public static T GetService<T>() where T : class
        {
            return Instance?._container?.Resolve<T>();
        }
        
        /// <summary>
        /// Shutdown the entire framework cleanly
        /// </summary>
        private void OnApplicationQuit()
        {
            Debug.Log("[GameManager] Shutting down game framework...");
            
            _stateMachine?.Shutdown();
            
            // Shutdown all registered services
            var eventSystem = _container?.Resolve<IEventSystem>();
            eventSystem?.Shutdown();
            
            _container?.Clear();
        }
        
        /// <summary>
        /// Debug information for inspector
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying && _stateMachine != null)
            {
                // Draw debug info in scene view
                var currentState = _stateMachine.CurrentStateType.ToString();
                UnityEditor.Handles.Label(transform.position, $"Current State: {currentState}");
            }
        }
    }
}