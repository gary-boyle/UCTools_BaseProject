using System;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Input.Interfaces;
using GameFramework.Services.Interfaces;

namespace GameFramework.Core
{
    /// <summary>
    /// Centralized context object that provides access to all core game services through dependency injection.
    /// This class serves as a service aggregator that collects all essential game systems into a single,
    /// easily injectable dependency for game states and other complex systems.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The GameContext follows the Context Object pattern, providing a clean way to pass multiple
    /// related services to classes that need access to various game systems. Rather than injecting
    /// each service individually, classes can inject the GameContext and access all services through it.
    /// </para>
    /// <para>
    /// <strong>Benefits:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description>Reduces constructor parameter count in dependent classes</description></item>
    /// <item><description>Provides a stable interface even when service dependencies change</description></item>
    /// <item><description>Centralizes service access for easier testing and mocking</description></item>
    /// <item><description>Maintains clear dependency relationships through constructor injection</description></item>
    /// </list>
    /// <para>
    /// All services are validated for null values during construction, ensuring that the GameContext
    /// is always in a valid state when successfully created.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In a game state constructor
    /// public class PlayingState : IGameState
    /// {
    ///     private readonly GameContext _context;
    ///     
    ///     public PlayingState(GameContext context)
    ///     {
    ///         _context = context;
    ///     }
    ///     
    ///     public void Enter()
    ///     {
    ///         _context.AudioService.PlayMusic("gameplay");
    ///         _context.UIService.ShowScreen("hud");
    ///         _context.EventSystem.Publish(new GameStartedEvent());
    ///     }
    /// }
    /// </code>
    /// </example>
    public class GameContext
    {

        #region Service Properties

        /// <summary>
        /// Gets the event system for publishing and subscribing to game events.
        /// </summary>
        /// <value>The event system service instance.</value>
        /// <remarks>
        /// Use this service to decouple systems through event-driven communication.
        /// Supports both synchronous and asynchronous event handling patterns.
        /// </remarks>
        public IEventSystem EventSystem { get; }

        /// <summary>
        /// Gets the scene management service for loading and transitioning between scenes.
        /// </summary>
        /// <value>The scene service instance.</value>
        /// <remarks>
        /// Provides unified scene loading with progress tracking, async operations,
        /// and integration with the game state system.
        /// </remarks>
        public ISceneService SceneService { get; }

        /// <summary>
        /// Gets the audio service for managing music, sound effects, and audio settings.
        /// </summary>
        /// <value>The audio service instance.</value>
        /// <remarks>
        /// Handles audio playback, volume control, audio mixing, and integration
        /// with the game's configuration system for audio settings.
        /// </remarks>
        public IAudioService AudioService { get; }

        /// <summary>
        /// Gets the input manager for handling player input across different input systems.
        /// </summary>
        /// <value>The input manager instance.</value>
        /// <remarks>
        /// Provides unified input handling that supports multiple input methods
        /// (keyboard, mouse, gamepad) and input contexts (UI, gameplay, console).
        /// </remarks>
        public IInputManager InputManager { get; }

        /// <summary>
        /// Gets the UI service for managing user interface elements and screens.
        /// </summary>
        /// <value>The UI service instance.</value>
        /// <remarks>
        /// Handles UI screen transitions, element management, and integration
        /// with Unity's UI Toolkit system for modern UI development.
        /// </remarks>
        public IUIService UIService { get; }

        /// <summary>
        /// Gets the save service for managing game save data and persistence.
        /// </summary>
        /// <value>The save service instance.</value>
        /// <remarks>
        /// Provides secure, reliable game save functionality with support for
        /// multiple save slots, data validation, and cross-platform compatibility.
        /// </remarks>
        public ISaveService SaveService { get; }
        
        /// <summary>
        /// Gets the game data service for managing runtime game state and shared data.
        /// </summary>
        /// <value>The game data service instance.</value>
        /// <remarks>
        /// Handles runtime game state, player progress, session data, and other
        /// transient game information that needs to be shared across game states.
        /// </remarks>
        public IGameDataService GameDataService { get; }

        public IConsoleService ConsoleService { get; }

        public IGraphicsService GraphicsService { get; }
        
        public ILoadService LoadService { get; }
        
        public IPauseService PauseService { get; }
        
        public ITimeService TimeService { get; }
        
        // ILoadService is not listed as it made a circular dependency with GameStateMachine and it doesn't currently seem needed
        // public ILoadService LoadService { get; }
        
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the GameContext with all required game services.
        /// </summary>
        /// <param name="eventSystem">The event system for game event management.</param>
        /// <param name="sceneService">The scene service for scene loading and management.</param>
        /// <param name="audioService">The audio service for sound and music management.</param>
        /// <param name="inputManager">The input manager for player input handling.</param>
        /// <param name="uiService">The UI service for user interface management.</param>
        /// <param name="saveService">The save service for game data persistence.</param>
        /// <param name="configService">The configuration service for settings management.</param>
        /// <param name="gameDataService">The game data service for runtime game state.</param>
        /// <remarks>
        /// <para>
        /// This constructor uses dependency injection to receive all core game services.
        /// All parameters are validated for null values, ensuring that the GameContext
        /// is always created in a valid state with all required dependencies.
        /// </para>
        /// <para>
        /// The constructor is designed to be called by the dependency injection container
        /// during application startup. Manual instantiation is not recommended as it
        /// requires managing all service dependencies manually.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any of the required service parameters is null.
        /// </exception>
        /// <example>
        /// <code>
        /// // Typically handled by DI container
        /// container.RegisterSingleton&lt;GameContext&gt;();
        /// 
        /// // Container automatically provides all dependencies
        /// var context = container.Resolve&lt;GameContext&gt;();
        /// </code>
        /// </example>
        public GameContext(
            IEventSystem eventSystem,
            ISceneService sceneService, 
            IAudioService audioService,
            IInputManager inputManager,
            IUIService uiService,
            ISaveService saveService,
            IGameDataService gameDataService,
            IConsoleService consoleService,
            IGraphicsService graphicsService,
            IPauseService pauseService,
            ITimeService timeService
        )
        {
            EventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            SceneService = sceneService ?? throw new ArgumentNullException(nameof(sceneService));
            AudioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            InputManager = inputManager ?? throw new ArgumentNullException(nameof(inputManager));
            UIService = uiService ?? throw new ArgumentNullException(nameof(uiService));
            SaveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            GameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
            ConsoleService = consoleService ?? throw new ArgumentNullException(nameof(consoleService));
            GraphicsService = graphicsService ?? throw new ArgumentNullException(nameof(graphicsService));
            PauseService = pauseService ?? throw new ArgumentNullException(nameof(pauseService));
            TimeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
        }

        #endregion
    }
}
