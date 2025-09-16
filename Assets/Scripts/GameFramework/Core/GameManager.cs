using System.Collections.Generic;
using System.Threading.Tasks;
using GameFramework.Audio;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine;
using GameFramework.StateMachine.GameStates;
using GameFramework.StateMachine.Interfaces;
using GameFramework.ConsoleTool;
using GameFramework.Input;
using GameFramework.Input.Handlers;
using GameFramework.Input.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using GameFramework.Config.ScriptableObjects;
using GameFramework.FileSystem.Interfaces;
using GameFramework.FileSystem.Services;
using GameFramework.GameData.Services;
using GameFramework.LoadSystem.Interfaces;
using GameFramework.LoadSystem.Services;
using GameFramework.SaveSystem.Interfaces;
using GameFramework.SaveSystem.Services;

namespace GameFramework.Core
{
    /// <summary>
    /// Main game manager that bootstraps the entire game framework using dependency injection.
    /// This singleton class initializes all core services, manages the game state machine, 
    /// and coordinates frame updates across all systems.
    /// </summary>
    /// <remarks>
    /// The GameManager follows this initialization sequence:
    /// 1. Creates DI container and registers all services
    /// 2. Initializes services in dependency order
    /// 3. Sets up the game state machine
    /// 4. Collects updatable systems for frame updates
    /// 5. Begins normal game operation
    /// </remarks>
    public class GameManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Debug Settings")]
        [SerializeField] private bool _enableDebugConsole = true;
        
        [Header("Prefabs")]
        [SerializeField] private UIDocument _UIPrefab;
        [SerializeField] private ConsoleGUI _consoleGUIPrefab;
        [SerializeField] private AudioManager _audioManagerPrefab;

        [Header("Configuration Settings")]
        [SerializeField] private AudioSettings_SO _audioSettingsSo;
        [SerializeField] private GraphicsSettings_SO _graphicsSettingsSo;
        [SerializeField] private GameplaySettings_SO _gameplaySettingsSo;
        [SerializeField] private InputSettings_SO _inputSettingsSo;
        [SerializeField] private DebugSettings_SO _debugSettingsSo;

        #endregion

        #region Singleton Implementation

        /// <summary>
        /// Private static instance of the GameManager singleton.
        /// </summary>
        private static GameManager _instance;
        
        /// <summary>
        /// Gets the singleton instance of the GameManager.
        /// </summary>
        /// <value>The GameManager instance, or null if not yet initialized.</value>
        /// <remarks>
        /// Logs an error if accessed before initialization. Use <see cref="IsReady"/> to check availability.
        /// </remarks>
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

        #endregion

        #region Private Fields

        /// <summary>
        /// The dependency injection container that manages all service instances.
        /// </summary>
        private DiContainer _container;
        
        /// <summary>
        /// The main game state machine that controls game flow.
        /// </summary>
        private IGameStateMachine _stateMachine;
        
        /// <summary>
        /// Collection of systems that need Update() calls every frame.
        /// </summary>
        private List<IUpdatable> _updatables = new();
        
        /// <summary>
        /// Collection of systems that need FixedUpdate() calls for physics updates.
        /// </summary>
        private List<IFixedUpdatable> _fixedUpdatables = new();
        
        /// <summary>
        /// Collection of systems that need LateUpdate() calls after all other updates.
        /// </summary>
        private List<ILateUpdatable> _lateUpdatables = new();
        
        /// <summary>
        /// Flag indicating whether the framework has completed initialization.
        /// </summary>
        private bool _isInitialized;
        
        /// <summary>
        /// Flag indicating whether all services have been registered in the DI container.
        /// </summary>
        private bool _servicesRegistered;
        
        /// <summary>
        /// Task completion source for async initialization tracking.
        /// </summary>
        private readonly TaskCompletionSource<bool> _initializationComplete = new();

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Unity Awake callback. Initializes the singleton pattern and starts framework initialization.
        /// </summary>
        /// <remarks>
        /// Creates the singleton instance, marks it as DontDestroyOnLoad, and begins async initialization.
        /// Destroys duplicate instances if they exist.
        /// </remarks>
        private void Awake() 
        {
            // Singleton pattern implementation
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Start initialization
                _ = InitializeFrameworkAsync();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return; // Exit early since we're destroying this object
            }
        }
        
        /// <summary>
        /// Unity Update callback. Calls Update() on all registered updatable systems.
        /// </summary>
        /// <remarks>
        /// Only executes if the framework is fully initialized. Systems are updated in registration order.
        /// </remarks>
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
        
        /// <summary>
        /// Unity FixedUpdate callback. Calls FixedUpdate() on all registered fixed updatable systems.
        /// </summary>
        /// <remarks>
        /// Only executes if the framework is fully initialized. Used for physics-related updates.
        /// </remarks>
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
        
        /// <summary>
        /// Unity LateUpdate callback. Calls LateUpdate() on all registered late updatable systems.
        /// </summary>
        /// <remarks>
        /// Only executes if the framework is fully initialized. Used for updates that should happen 
        /// after all other systems have updated.
        /// </remarks>
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
        /// Unity OnApplicationQuit callback. Cleanly shuts down all framework systems.
        /// </summary>
        private void OnApplicationQuit()
        {
            Debug.Log("[GameManager] Shutting down game framework...");
    
            _stateMachine?.Shutdown();
    
            // Shutdown console service if it exists
            if (_enableDebugConsole && _container?.IsRegistered<IConsoleService>() == true)
            {
                var consoleService = _container.Resolve<IConsoleService>();
                consoleService?.Shutdown();
            }
    
            // Shutdown all registered services
            var eventSystem = _container?.Resolve<IEventSystem>();
            eventSystem?.Shutdown();
    
            _container?.Clear();
        }
        
        /// <summary>
        /// Unity OnDestroy callback. Cleans up singleton reference.
        /// </summary>
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Framework Initialization

        /// <summary>
        /// Asynchronously initializes the entire game framework using dependency injection.
        /// </summary>
        /// <returns>A task that completes when initialization is finished.</returns>
        /// <remarks>
        /// This method orchestrates the complete framework startup sequence:
        /// 1. Creates the DI container
        /// 2. Registers all services in dependency order
        /// 3. Initializes all services
        /// 4. Sets up the state machine
        /// 5. Collects updatable systems
        /// </remarks>
        private async Task InitializeFrameworkAsync()
        {
            if (_isInitialized) return;
            
            try
            {
                // Initialize DI container
                _container = DiContainer.Instance;
                
                // Register all services in dependency order
                RegisterCoreServices();
                RegisterGameSystems();
                RegisterGameStates();
                
                _servicesRegistered = true; // Mark that services are registered
                
                // Initialize all services after registration
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
        /// Initializes all registered services in the correct dependency order.
        /// </summary>
        /// <returns>A task that completes when all services are initialized.</returns>
        /// <remarks>
        /// Services are initialized in dependency order to ensure all required dependencies 
        /// are available when each service starts up.
        /// </remarks>
        private async Task InitializeServicesAsync()
        {
            Debug.Log("[GameManager] Initializing services...");
 
            // Initialize services in dependency order
            var eventSystem = _container.Resolve<IEventSystem>();
            await eventSystem.InitializeAsync();

            var fileService = _container.Resolve<IFileService>();
            await fileService.InitializeAsync();
            
            SettingsRegistry.Initialize();
            if (!_servicesRegistered)
            {
                Debug.LogError("[GameManager] Cannot initialize services before they are registered!");
                return;
            }
            
            await SettingsRegistry.LoadAllSettingsAsync();
          

            var audioService = _container.Resolve<IAudioService>();
            await audioService.InitializeAsync(RegisterAudioManager());

            var graphicsService = _container.Resolve<IGraphicsService>();
            await graphicsService.InitializeAsync();
            
            var timeService = _container.Resolve<ITimeService>();
            await timeService.InitializeAsync();
            
            var inputManager = _container.Resolve<IInputManager>();
            await inputManager.InitializeAsync();
            
            if (_container.IsRegistered<IConsoleService>())
            {
                var consoleService = _container.Resolve<IConsoleService>();
                await consoleService.InitializeAsync();
                Debug.Log("[GameManager] Console service initialized");
            }
            
            var sceneService = _container.Resolve<ISceneService>();
            await sceneService.InitializeAsync();
            
            var pauseService = _container.Resolve<IPauseService>();
            await pauseService.InitializeAsync();
            
            var profilingService = _container.Resolve<IProfilingService>();
            await profilingService.InitializeAsync();

            var gameDataService = _container.Resolve<IGameDataService>();
            await gameDataService.InitializeAsync();
            
            var loadService = _container.Resolve<ILoadService>();
            await loadService.InitializeAsync();
            
            var saveService = _container.Resolve<ISaveService>();
            await saveService.InitializeAsync();
            
            var saveDataRegistry = _container.Resolve<ISaveDataRegistry>();
            await saveDataRegistry.InitializeAsync();
            
            // Initialize UI service LAST since it depends on other services
            var uiService = _container.Resolve<IUIService>();
            await uiService.InitializeAsync();
            
            Debug.Log("[GameManager] All services initialized!");
        }

        #endregion

        #region Service Registration

        /// <summary>
        /// Registers core framework services in the dependency injection container.
        /// </summary>
        /// <remarks>
        /// Services are registered in dependency order where services with no dependencies 
        /// are registered first, followed by services that depend on them.
        /// Registration order is critical for proper dependency resolution.
        /// </remarks>
        private void  RegisterCoreServices()
        {
            Debug.Log("[GameManager] Registering core services...");
    
            // Register the DI container itself (for cases where services need to resolve other services)
            _container.RegisterSingleton(_container);
    
            // Register leaf services first (no dependencies)
            _container.RegisterSingleton<IEventSystem, EventSystem.EventSystem>();
            _container.RegisterSingleton<IProfilingService, ProfilingService>();
            _container.RegisterSingleton<IFileService, FileService>();

            // Register services with minimal dependencies
            _container.RegisterSingleton<ITimeService, TimeService>();
            _container.RegisterSingleton<IAudioService, AudioService>();
            _container.RegisterSingleton<IGraphicsService, GraphicsService>();
            _container.RegisterSingleton<ISceneService, SceneService>();
            _container.RegisterSingleton<IPauseService, PauseService>();

            // Register input handlers
            _container.RegisterSingleton<ConsoleInputHandler>();
            _container.RegisterSingleton<UIInputHandler>();
            _container.RegisterSingleton<PlayerInputHandler>();
    
            // Register input manager with interface
            _container.RegisterSingleton<IInputManager, InputManager>();
            
            // Create and register UI document before UI service
            RegisterUIDocument();
            
            // Create and register console-related services (if debug console is enabled)
            if (_enableDebugConsole)
            {
                RegisterConsoleGUI();
                _container.RegisterSingleton<IConsoleService, ConsoleService>();
                _container.RegisterSingleton<ConsoleInputHandler>(); // Only register if console service exists
                Debug.Log("[GameManager] Console services registered");
            }
            
            // Register services that might depend on the above
            _container.RegisterSingleton<IUIService, UIService>();
            _container.RegisterSingleton<ILoadService, LoadService>();
            _container.RegisterSingleton<ISaveService, SaveService>();
            _container.RegisterSingleton<IGameDataService, GameDataService>();
            _container.RegisterSingleton<ISaveDataRegistry, SaveDataRegistry>();
            
            // Register GameContext (depends on all other services)
            _container.RegisterSingleton<GameContext>();
    
            
            // Register state machine (depends on GameContext)
            _container.RegisterSingleton<IGameStateMachine, GameStateMachine>();
    
            Debug.Log("[GameManager] Core services registration complete");
        }
        
        /// <summary>
        /// Creates and registers the ConsoleGUI instance from the assigned prefab.
        /// </summary>
        /// <remarks>
        /// The ConsoleGUI is instantiated from the prefab, marked as DontDestroyOnLoad,
        /// and registered directly in the DI container for injection into the ConsoleService.
        /// </remarks>
        private void RegisterConsoleGUI()
        {
            if (_consoleGUIPrefab == null)
            {
                Debug.LogError("[GameManager] ConsoleGUI Prefab is not assigned! Please assign it in the inspector.");
                return;
            }

            try
            {
                // Instantiate the ConsoleGUI prefab
                var consoleInstance = Instantiate(_consoleGUIPrefab);
                consoleInstance.name = "ConsoleGUI (Runtime)";

                // Make it persist across scene loads
                DontDestroyOnLoad(consoleInstance.gameObject);

                // Register the ConsoleGUI instance directly in the container
                _container.RegisterSingleton(consoleInstance);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameManager] Failed to create ConsoleGUI from prefab: {e.Message}");
            }
        }
        
        /// <summary>
        /// Creates and registers the UIDocument instance from the assigned prefab.
        /// </summary>
        /// <remarks>
        /// The UIDocument is instantiated from the prefab, marked as DontDestroyOnLoad,
        /// and registered directly in the DI container for injection into the UIService.
        /// </remarks>
        private void RegisterUIDocument()
        {
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
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameManager] Failed to create UIDocument from prefab: {e.Message}");
            }
        }
        
        private AudioManager RegisterAudioManager()
        {
            if (_audioManagerPrefab == null)
            {
                Debug.LogError("[GameManager] AudioManager Prefab is not assigned! Please assign it in the inspector.");
                return null;
            }
    
            try
            {
                // Instantiate the Audio prefab
                var audioManager = Instantiate(_audioManagerPrefab);
                audioManager.name = "Audio Manager (Runtime)";
        
                // Make it persist across scene loads
                DontDestroyOnLoad(audioManager.gameObject);
        
                // Register the UIDocument instance directly in the container
                _container.RegisterSingleton(audioManager);

                return audioManager;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameManager] Failed to create AudioManager from prefab: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Registers additional game-specific systems and managers.
        /// </summary>
        /// <remarks>
        /// This method is available for registering custom game systems that extend 
        /// the core framework functionality.
        /// </remarks>
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
        /// Registers all game states as transient services in the DI container.
        /// </summary>
        /// <remarks>
        /// Game states are registered as transient to allow the state machine to create 
        /// fresh instances with proper dependency injection. The state machine handles 
        /// caching and lifecycle management of state instances.
        /// </remarks>
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
            _container.RegisterTransient<CreditsState, CreditsState>();
            _container.RegisterTransient<GameOverState, GameOverState>();
            _container.RegisterTransient<VictoryState, VictoryState>();
            _container.RegisterTransient<QuitState, QuitState>();
        }

        #endregion
        
        #region System Collection

        /// <summary>
        /// Collects all systems that implement update interfaces for frame-based updates.
        /// </summary>
        /// <remarks>
        /// This method scans all registered services for IUpdatable, IFixedUpdatable, and 
        /// ILateUpdatable implementations and adds them to the appropriate update collections.
        /// The GameManager will then call their update methods each frame.
        /// </remarks>
        private void CollectUpdatableSystems()
        {
            Debug.Log("[GameManager] Collecting updatable systems...");



            // Add state machine to updatables
            if (_stateMachine is IUpdatable updatable)
                _updatables.Add(updatable);
    
            if (_stateMachine is IFixedUpdatable fixedUpdatable)
                _fixedUpdatables.Add(fixedUpdatable);
            
            // Add AudioService for FadeIn/FadeOut
            var audioService = _container.Resolve<IAudioService>();
            if (audioService is IUpdatable audioServiceUpdatable)
                _updatables.Add(audioServiceUpdatable);

            var timeService = _container.Resolve<ITimeService>();
            if (timeService is IUpdatable timeUpdatable)
                _updatables.Add(timeUpdatable);
            
            var profilingService = _container.Resolve<IProfilingService>();
            if (profilingService is IUpdatable profilingUpdatable)
                _updatables.Add(profilingUpdatable);
            
            // Add InputManager for updates
            var inputManager = _container.Resolve<IInputManager>();
            if (inputManager is IUpdatable inputManagerUpdatable)
                _updatables.Add(inputManagerUpdatable);
            
            var pauseService = _container.Resolve<IPauseService>();
            if (pauseService is IUpdatable pauseUpdatable)
                _updatables.Add(pauseUpdatable);
            
            // Add UI service for screen updates
            var uiService = _container.Resolve<IUIService>();
            if (uiService is IUpdatable uiUpdatable)
            {
                _updatables.Add(uiUpdatable);
            }

            // Add console service if enabled and registered
            if (_enableDebugConsole && _container.IsRegistered<IConsoleService>())
            {
                var consoleService = _container.Resolve<IConsoleService>();
                if (consoleService is IUpdatable consoleUpdatable)
                    _updatables.Add(consoleUpdatable);
                if (consoleService is ILateUpdatable consoleLateUpdatable)
                    _lateUpdatables.Add(consoleLateUpdatable);
            }
    
            Debug.Log($"[GameManager] Collected {_updatables.Count} updatable systems, {_fixedUpdatables.Count} fixed updatable systems, {_lateUpdatables.Count} late updatable systems");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets a service instance from the dependency injection container.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <returns>The service instance, or null if not found or GameManager not initialized.</returns>
        /// <remarks>
        /// This method provides global access to registered services. Use <see cref="IsReady"/> 
        /// to check if services are available before calling this method.
        /// </remarks>
        /// <example>
        /// <code>
        /// var audioService = GameManager.GetService&lt;IAudioService&gt;();
        /// if (audioService != null)
        /// {
        ///     audioService.PlaySound("buttonClick");
        /// }
        /// </code>
        /// </example>
        public static T GetService<T>() where T : class
        {
            return Instance?._container?.Resolve<T>();
        }
        
        /// <summary>
        /// Asynchronously gets a service instance, waiting for initialization to complete if necessary.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <returns>A task that resolves to the service instance, or null if GameManager instance is null.</returns>
        /// <remarks>
        /// This method is useful for getting services during early initialization phases where 
        /// you need to wait for the framework to be ready.
        /// </remarks>
        /// <example>
        /// <code>
        /// var audioService = await GameManager.GetServiceAsync&lt;IAudioService&gt;();
        /// if (audioService != null)
        /// {
        ///     audioService.PlaySound("gameStart");
        /// }
        /// </code>
        /// </example>
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
        
        public Dictionary<Type, ConfigCategoryBase>  GetConfigurationSettings()
        {
            return new Dictionary<Type, ConfigCategoryBase> 
            {
                { typeof(AudioSettings_SO), _audioSettingsSo },
                { typeof(GraphicsSettings_SO), _graphicsSettingsSo },
                { typeof(GameplaySettings_SO), _gameplaySettingsSo },
                { typeof(InputSettings_SO), _inputSettingsSo },
                { typeof(DebugSettings_SO), _debugSettingsSo }
            };
        }
        
        #endregion
    }
}
