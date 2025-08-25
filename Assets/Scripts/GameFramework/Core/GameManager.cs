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
    /// </summary>
    public class GameManager : MonoBehaviour // Remove Singleton<GameManager> inheritance
    {
        [Header("Debug Settings")]
        [SerializeField] private bool _enableDebugConsole = true;
        [SerializeField] private bool _enableVerboseLogging = false;
        
        [Header("Prefabs")]
        [SerializeField] private UIDocument _UIPrefab;

        // Singleton implementation
        private static GameManager _instance;
        public static GameManager Instance 
        { 
            get 
            {
                if (_instance == null)
                {
                    Debug.LogError("[GameManager] Instance accessed before initialization!");
                }
                return _instance;
            }
        }

        // Core systems
        private DIContainer _container;
        private IGameStateMachine _stateMachine;
        private List<IUpdatable> _updatables = new();
        private List<IFixedUpdatable> _fixedUpdatables = new();
        private List<ILateUpdatable> _lateUpdatables = new();
        
        // Initialization tracking
        private bool _isInitialized = false;
        private bool _servicesRegistered = false;
        private TaskCompletionSource<bool> _initializationComplete = new();
        
        /// <summary>
        /// Initialize the singleton and start the dependency injection setup
        /// </summary>
        private void Awake() 
        {
            // Singleton pattern implementation
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[GameManager] Singleton instance created");
                
                // Start initialization
                _ = InitializeFrameworkAsync();
            }
            else if (_instance != this)
            {
                Debug.LogWarning("[GameManager] Another instance detected, destroying duplicate");
                Destroy(gameObject);
            }
        }
        
        private void Update() 
        {
            // Only update if initialized
            if (!_isInitialized) return;
            
            // Update all systems that implement IUpdatable
            foreach (var updatable in _updatables)
            {
                updatable.Update();
            }
        }
        
        private void FixedUpdate()
        {
            // Only update if initialized
            if (!_isInitialized) return;
            
            // Update all systems that implement IFixedUpdatable
            foreach (var fixedUpdatable in _fixedUpdatables)
            {
                fixedUpdatable.FixedUpdate();
            }
        }
        
        private void LateUpdate()
        {
            // Only update if initialized
            if (!_isInitialized) return;
            
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
            if (_isInitialized) return;
            
            Debug.Log("[GameManager] Initializing game framework with dependency injection...");
            
            try
            {
                // Initialize DI container
                _container = DIContainer.Instance;
                Debug.Log("[GameManager] DI Container created");
                
                // Register all services in dependency order
                RegisterCoreServices();
                RegisterGameSystems();
                RegisterGameStates();
                
                _servicesRegistered = true; // Mark that services are registered
                Debug.Log("[GameManager] Services registered, container ready");
                
                // Initialize ConfigVar system
                UCTools_ConfigVariables.ConfigVar.Init();
                
                // INITIALIZE ALL SERVICES AFTER REGISTRATION
                await InitializeServicesAsync();
                
                // Create and initialize the state machine
                _stateMachine = _container.Resolve<IGameStateMachine>();
                await _stateMachine.InitializeAsync();
                
                // Collect updatable systems
                CollectUpdatableSystems();
                
                _isInitialized = true;
                _initializationComplete.SetResult(true);
                
                Debug.Log("[GameManager] Framework initialization complete!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameManager] Framework initialization failed: {e}");
                _initializationComplete.SetException(e);
            }
        }
        
        /// <summary>
        /// Initialize all registered services in the correct order
        /// </summary>
        private async Task InitializeServicesAsync()
        {
            Debug.Log("[GameManager] Initializing services...");
            
            // Initialize services in dependency order
            var eventSystem = _container.Resolve<IEventSystem>();
            await eventSystem.InitializeAsync();
            Debug.Log("[GameManager] EventSystem initialized");
            
            var audioService = _container.Resolve<IAudioService>();
            await audioService.InitializeAsync();
            
            var inputService = _container.Resolve<IInputService>();
            await inputService.InitializeAsync();
            
            var sceneService = _container.Resolve<ISceneService>();
            await sceneService.InitializeAsync();
            
            var configService = _container.Resolve<IConfigService>();
            await configService.InitializeAsync();
            
            var saveService = _container.Resolve<ISaveService>();
            await saveService.InitializeAsync();
            
            // Initialize UI service LAST since it depends on other services
            var uiService = _container.Resolve<IUIService>();
            await uiService.InitializeAsync();
            
            Debug.Log("[GameManager] All services initialized!");
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
            Debug.Log("[GameManager] Registering EventSystem...");
            _container.RegisterSingleton<IEventSystem, EventSystem.EventSystem>();
            Debug.Log($"[GameManager] EventSystem registered: {_container.IsRegistered<IEventSystem>()}");
            
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
            
            Debug.Log("[GameManager] Core services registration complete");
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
        }
        
        /// <summary>
        /// Get access to a service from anywhere in the game - with proper null checks
        /// </summary>
        public static T GetService<T>() where T : class
        {
            return Instance?._container?.Resolve<T>();
        }
        
        /// <summary>
        /// Async version for waiting until services are ready
        /// </summary>
        public static async Task<T> GetServiceAsync<T>() where T : class
        {
            if (_instance == null)
            {
                Debug.LogError($"[GameManager] Instance is null when requesting {typeof(T).Name}");
                return null;
            }
            
            // Wait for initialization to complete
            await _instance._initializationComplete.Task;
            
            return GetService<T>();
        }
        
        /// <summary>
        /// Check if the GameManager is ready
        /// </summary>
        public static bool IsReady => _instance != null && _instance._servicesRegistered;
        
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
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
