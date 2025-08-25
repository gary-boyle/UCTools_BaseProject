
## 5. API-Reference.md

```markdown
# API Reference

Complete API documentation for the Unity Game Framework.

## 📋 Table of Contents

- [Core Classes](#core-classes)
- [Services](#services)
- [State Management](#state-management)
- [UI System](#ui-system)
- [Events](#events)
- [Configuration](#configuration)

## 🏗️ Core Classes

### DIContainer

**Purpose**: Dependency injection container for service management.

```csharp
public class DIContainer
{
    // Static access
    public static DIContainer Instance { get; }
    
    // Registration methods
    public void RegisterSingleton<T>(T instance) where T : class
    public void RegisterSingleton<TInterface, TImplementation>() 
        where TImplementation : class, TInterface
    public void RegisterFactory<T>(Func<T> factory) where T : class
    public void RegisterTransient<TInterface, TImplementation>() 
        where TImplementation : class, TInterface
    
    // Resolution methods
    public T Resolve<T>() where T : class
    public object Resolve(Type type)
    
    // Utility methods
    public bool IsRegistered<T>()
    public void Clear()
}
```

**Usage Examples**:
```csharp
// Registration
container.RegisterSingleton<IEventSystem, EventSystem>();
container.RegisterFactory<ILogger>(() => new FileLogger("game.log"));
container.RegisterTransient<IEnemy, Goblin>();

// Resolution
var eventSystem = container.Resolve<IEventSystem>();
var isRegistered = container.IsRegistered<IAudioService>();
```

### GameManager

**Purpose**: Main bootstrap class that initializes the entire framework.

```csharp
public class GameManager : MonoBehaviour
{
    // Static access
    public static GameManager Instance { get; private set; }
    
    // Service access (use sparingly)
    public static T GetService<T>() where T : class
    
    // Unity lifecycle
    private void Awake()
    private void Update()
    private void FixedUpdate()
    private void LateUpdate()
    private void OnApplicationQuit()
}
```

**Usage Examples**:
```csharp
// Access services (prefer constructor injection)
var audioService = GameManager.GetService<IAudioService>();

// The GameManager automatically handles initialization
// Just add it to your scene and it works!
```

### GameContext

**Purpose**: Provides centralized access to all core services.

```csharp
public class GameContext
{
    // Core services (all readonly)
    public IEventSystem EventSystem { get; }
    public ISceneService SceneService { get; }
    public IAudioService AudioService { get; }
    public IInputService InputService { get; }
    public IUIService UIService { get; }
    public ISaveService SaveService { get; }
    public IConfigService ConfigService { get; }
    
    // Constructor (used by DI container)
    public GameContext(/* all services injected */)
}
```

**Usage Examples**:
```csharp
// Passed to game states automatically
public override async Task EnterAsync(GameContext context)
{
    // Access any service through context
    await context.UIService.ShowScreenAsync<MainMenuScreen>();
    context.AudioService.PlayMusic("main_menu");
}
```

## 🔧 Services

### IEventSystem

**Purpose**: Type-safe event publishing and subscription system.

```csharp
public interface IEventSystem : IGameService
{
    // Subscription
    void Subscribe<T>(Action<T> handler) where T : class;
    void Subscribe<T>(Action handler);
    
    // Unsubscription
    void Unsubscribe<T>(Action<T> handler) where T : class;
    void Unsubscribe<T>(Action handler);
    
    // Publishing
    void Publish<T>(T eventData) where T : class;
    void Publish<T>();
    
    // Utility
    void Clear();
}
```

**Usage Examples**:
```csharp
// Subscribe to events with data
eventSystem.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);

// Subscribe to simple events
eventSystem.Subscribe<GameStartedEvent>(OnGameStarted);

// Publish events with data
eventSystem.Publish(new PlayerLevelUpEvent { NewLevel = 5 });

// Publish simple events
eventSystem.Publish<GamePausedEvent>();

// Always unsubscribe in cleanup
eventSystem.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
```

### IAudioService

**Purpose**: Audio playback and volume management.

```csharp
public interface IAudioService : IGameService
{
    // Music control
    void PlayMusic(string musicName);
    void StopMusic();
    
    // Sound effects
    void PlaySound(string soundName);
    void StopSound(string soundName);
    
    // Volume control
    void SetMasterVolume(float volume);
    void SetMusicVolume(float volume);
    void SetSFXVolume(float volume);
    
    // Volume queries
    float GetMasterVolume();
    float GetMusicVolume();
    float GetSFXVolume();
}
```

**Usage Examples**:
```csharp
// Play music
audioService.PlayMusic("main_menu");
audioService.PlayMusic("battle_theme");

// Play sound effects
audioService.PlaySound("button_click");
audioService.PlaySound("player_hurt");

// Control volume
audioService.SetMasterVolume(0.8f);
audioService.SetMusicVolume(0.6f);

// Query current volume
float currentVolume = audioService.GetMasterVolume();
```

### IUIService

**Purpose**: UI screen and popup management.

```csharp
public interface IUIService : IGameService
{
    // Screen management
    Task ShowScreenAsync<T>() where T : UIScreen;
    Task HideScreenAsync<T>() where T : UIScreen;
    
    // Popup management
    Task ShowPopupAsync<T>() where T : UIPopup;
    Task HidePopupAsync<T>() where T : UIPopup;
    
    // Registration (called automatically)
    void RegisterScreen<T>(T screen) where T : UIScreen;
    void RegisterPopup<T>(T popup) where T : UIPopup;
    
    // Access
    T GetScreen<T>() where T : UIScreen;
    T GetPopup<T>() where T : UIPopup;
}
```

**Usage Examples**:
```csharp
// Show/hide screens
await uiService.ShowScreenAsync<MainMenuScreen>();
await uiService.HideScreenAsync<GameplayHUD>();

// Show/hide popups
await uiService.ShowPopupAsync<ConfirmationPopup>();
await uiService.HidePopupAsync<ErrorPopup>();

// Get screen instance for direct manipulation
var hud = uiService.GetScreen<GameplayHUD>();
hud?.UpdateScore(1250);
```

### ISaveService

**Purpose**: Game save and load functionality.

```csharp
public interface ISaveService : IGameService
{
    // Save operations
    Task SaveGameAsync(string saveName = null);
    
    // Load operations
    Task<bool> LoadGameAsync(string saveName);
    Task<bool> LoadMostRecentSaveAsync();
    
    // Save management
    Task<string[]> GetSaveFilesAsync();
    Task<bool> DeleteSaveAsync(string saveName);
    
    // Save queries
    bool HasAnySaves();
    string GetMostRecentSaveName();
}
```

**Usage Examples**:
```csharp
// Save game (auto-generates name if null)
await saveService.SaveGameAsync();
await saveService.SaveGameAsync("checkpoint_level_5");

// Load game
bool loadSuccess = await saveService.LoadGameAsync("my_save");
bool loadedRecent = await saveService.LoadMostRecentSaveAsync();

// Manage saves
string[] saveFiles = await saveService.GetSaveFilesAsync();
bool deleted = await saveService.DeleteSaveAsync("old_save");

// Check save status
bool hasSaves = saveService.HasAnySaves();
string recentSave = saveService.GetMostRecentSaveName();
```

### IConfigService

**Purpose**: Configuration and settings management.

```csharp
public interface IConfigService : IGameService
{
    // Config file operations
    Task LoadConfigAsync();
    Task SaveConfigAsync();
    
    // Value access
    T GetConfigValue<T>(string configName);
    void SetConfigValue<T>(string configName, T value);
    
    // Config management
    void ResetToDefaults();
    void RegisterConfigVar(UCTools_ConfigVariables.ConfigVar configVar);
}
```

**Usage Examples**:
```csharp
// Load/save config
await configService.LoadConfigAsync();
await configService.SaveConfigAsync();

// Get config values
float masterVolume = configService.GetConfigValue<float>("audio.master_volume");
bool fullscreen = configService.GetConfigValue<bool>("graphics.fullscreen");
string playerName = configService.GetConfigValue<string>("player.name");

// Set config values
configService.SetConfigValue("audio.master_volume", 0.8f);
configService.SetConfigValue("graphics.quality_level", 3);

// Reset to defaults
configService.ResetToDefaults();
```

### IInputService

**Purpose**: Input handling and key mapping.

```csharp
public interface IInputService : IGameService, IUpdatable
{
    // Key input
    bool GetKeyDown(string keyName);
    bool GetKey(string keyName);
    bool GetKeyUp(string keyName);
    bool GetAnyKeyDown();
    
    // Mouse input
    Vector2 GetMousePosition();
    bool GetMouseButtonDown(int button);
    bool GetMouseButton(int button);
    bool GetMouseButtonUp(int button);
    
    // Axis input
    float GetAxis(string axisName);
}
```

**Usage Examples**:
```csharp
// Key input (uses mapped keys)
if (inputService.GetKeyDown("Pause"))
    PauseGame();

if (inputService.GetKey("MoveLeft"))
    MovePlayerLeft();

// Mouse input
Vector2 mousePos = inputService.GetMousePosition();
if (inputService.GetMouseButtonDown(0))
    HandleLeftClick();

// Any key detection
if (inputService.GetAnyKeyDown())
    SkipCutscene();

// Axis input
float horizontal = inputService.GetAxis("Horizontal");
float vertical = inputService.GetAxis("Vertical");
```

### ISceneService

**Purpose**: Scene loading and management.

```csharp
public interface ISceneService : IGameService
{
    // Scene loading
    Task LoadSceneAsync(string sceneName);
    Task LoadSceneAdditiveAsync(string sceneName);
    Task UnloadSceneAsync(string sceneName);
    
    // Scene queries
    string GetCurrentSceneName();
    bool IsSceneLoaded(string sceneName);
}
```

**Usage Examples**:
```csharp
// Load scenes
await sceneService.LoadSceneAsync("Level1");
await sceneService.LoadSceneAdditiveAsync("UI_Overlay");
await sceneService.UnloadSceneAsync("MainMenu");

// Query scene status
string currentScene = sceneService.GetCurrentSceneName();
bool isLoaded = sceneService.IsSceneLoaded("Level2");
```

## 🎮 State Management

### IGameStateMachine

**Purpose**: Game state management and transitions.

```csharp
public interface IGameStateMachine : IGameService
{
    // State properties
    GameStateType CurrentStateType { get; }
    BaseGameState CurrentState { get; }
    
    // State transitions
    Task ChangeStateAsync(GameStateType newStateType);
    Task ChangeStateAsync<T>() where T : BaseGameState;
    
    // Transition validation
    bool CanTransitionTo(GameStateType stateType);
    
    // State registration (internal)
    void RegisterState(BaseGameState state);
}
```

**Usage Examples**:
```csharp
// Transition by enum
await stateMachine.ChangeStateAsync(GameStateType.MainMenu);
await stateMachine.ChangeStateAsync(GameStateType.Playing);

// Transition by type
await stateMachine.ChangeStateAsync<OptionsState>();

// Check current state
GameStateType current = stateMachine.CurrentStateType;
bool isPlaying = current == GameStateType.Playing;

// Validate transitions
bool canPause = stateMachine.CanTransitionTo(GameStateType.Paused);
```

### BaseGameState

**Purpose**: Base class for all game states.

```csharp
public abstract class BaseGameState
{
    // Properties
    public GameStateType StateType { get; }
    public GameContext Context { get; protected set; }
    public bool IsActive { get; private set; }
    
    // Injected services
    protected readonly IGameStateMachine StateMachine;
    protected readonly IEventSystem EventSystem;
    protected readonly IAudioService AudioService;
    protected readonly IUIService UIService;
    protected readonly IInputService InputService;
    
    // Lifecycle methods (override as needed)
    public virtual async Task EnterAsync(GameContext context);
    public virtual void Update();
    public virtual void FixedUpdate();
    public virtual async Task ExitAsync();
    public virtual void HandleInput();
    
    // Helper method
    protected async Task TransitionToStateAsync(GameStateType newStateType);
}
```

**Usage Examples**:
```csharp
public class CustomState : BaseGameState
{
    public CustomState(/* dependencies */) : base(GameStateType.Custom, /* dependencies */) { }
    
    public override async Task EnterAsync(GameContext context)
    {
        await base.EnterAsync(context); // Always call base
        
        // Show UI
        await UIService.ShowScreenAsync<CustomScreen>();
        
        // Play music
        AudioService.PlayMusic("custom_theme");
        
        // Subscribe to events
        EventSystem.Subscribe<CustomEvent>(OnCustomEvent);
    }
    
    public override void Update()
    {
        if (InputService.GetKeyDown("Escape"))
            TransitionToStateAsync(GameStateType.MainMenu);
    }
    
    public override async Task ExitAsync()
    {
        // Cleanup
        EventSystem.Unsubscribe<CustomEvent>(OnCustomEvent);
        await UIService.HideScreenAsync<CustomScreen>();
        
        await base.ExitAsync(); // Always call base
    }
}
```

### GameStateType

**Purpose**: Enumeration of all available game states.

```csharp
public enum GameStateType
{
    Bootstrap,    // Initial loading and setup
    Splash,      // Company/game logos
    MainMenu,    // Main menu navigation
    Loading,     // Loading screens
    NewGame,     // New game setup
    Playing,     // Active gameplay
    Paused,      // Game paused
    Options,     // Settings menu
    Credits,     // Credits screen
    GameOver,    // Game over screen
    Victory,     // Victory screen
    Quit         // Shutting down
}
```

## 🎨 UI System

### UIScreen

**Purpose**: Base class for full-screen UI elements.

```csharp
public abstract class UIScreen
{
    // Properties
    protected VisualElement RootElement { get; private set; }
    public bool IsVisible { get; protected set; }
    
    // Constructor
    protected UIScreen(VisualElement rootElement);
    
    // Visibility control
    public virtual void Show();
    public virtual void Hide();
    
    // Override points
    protected virtual void OnShow();
    protected virtual void OnHide();
}
```

**Usage Examples**:
```csharp
public class CustomScreen : UIScreen
{
    private Button _actionButton;
    private Label _statusLabel;
    
    public CustomScreen(VisualElement rootElement) : base(rootElement)
    {
        // Initialize UI elements
        _actionButton = rootElement?.Q<Button>("ActionButton");
        _statusLabel = rootElement?.Q<Label>("StatusLabel");
        
        // Setup event handlers
        _actionButton?.RegisterCallback<ClickEvent>(OnActionClicked);
    }
    
    protected override void OnShow()
    {
        // Setup when screen becomes visible
        UpdateStatus("Screen is now visible");
    }
    
    protected override void OnHide()
    {
        // Cleanup when screen is hidden
        _statusLabel.text = "";
    }
    
    public void UpdateStatus(string status)
    {
        if (_statusLabel != null)
            _statusLabel.text = status;
    }
    
    private void OnActionClicked(ClickEvent evt)
    {
        // Handle button click
        var eventSystem = GameManager.GetService<IEventSystem>();
        eventSystem?.Publish(new ActionPerformedEvent());
    }
}
```

### UIPopup

**Purpose**: Base class for popup/overlay UI elements.

```csharp
public abstract class UIPopup : UIScreen
{
    // Inherits all UIScreen functionality
    protected UIPopup(VisualElement rootElement) : base(rootElement) { }
}
```

**Usage Examples**:
```csharp
public class ConfirmationPopup : UIPopup
{
    private Button _yesButton;
    private Button _noButton;
    private Label _messageLabel;
    
    public ConfirmationPopup(VisualElement rootElement) : base(rootElement)
    {
        _yesButton = rootElement?.Q<Button>("YesButton");
        _noButton = rootElement?.Q<Button>("NoButton");
        _messageLabel = rootElement?.Q<Label>("MessageLabel");
        
        _yesButton?.RegisterCallback<ClickEvent>(OnYesClicked);
        _noButton?.RegisterCallback<ClickEvent>(OnNoClicked);
    }
    
    public void SetMessage(string message)
    {
        if (_messageLabel != null)
            _messageLabel.text = message;
    }
    
    private void OnYesClicked(ClickEvent evt)
    {
        var eventSystem = GameManager.GetService<IEventSystem>();
        eventSystem?.Publish(new ConfirmationYesEvent());
        Hide();
    }
    
    private void OnNoClicked(ClickEvent evt)
    {
        var eventSystem = GameManager.GetService<IEventSystem>();
        eventSystem?.Publish(new ConfirmationNoEvent());
        Hide();
    }
}
```

## 📨 Events

### Event Naming Convention

Events should be named descriptively and end with "Event":

```csharp
// ✅ Good event names
public class PlayerLevelUpEvent { }
public class GamePausedEvent { }
public class ItemPickedUpEvent { }
public class QuestCompletedEvent { }

// ❌ Avoid generic names
public class DataEvent { }
public class UpdateEvent { }
public class Event { }
```

### Event Data Patterns

**Simple Events** (no data):
```csharp
public class GameStartedEvent { }
public class GamePausedEvent { }
public class MenuOpenedEvent { }
```

**Events with Data**:
```csharp
public class PlayerLevelUpEvent
{
    public int NewLevel { get; set; }
    public int PreviousLevel { get; set; }
    public float ExperienceGained { get; set; }
}

public class ItemPickedUpEvent
{
    public string ItemId { get; set; }
    public string ItemName { get; set; }
    public int Quantity { get; set; }
    public Vector3 PickupLocation { get; set; }
}
```

**Complex Events**:
```csharp
public class CombatResultEvent
{
    public CombatParticipant Winner { get; set; }
    public CombatParticipant Loser { get; set; }
    public float CombatDuration { get; set; }
    public List<CombatAction> Actions { get; set; }
    public Dictionary<string, object> Rewards { get; set; }
}
```

## ⚙️ Configuration

### ConfigVar Attributes

**Purpose**: Declarative configuration variable definition.

```csharp
[ConfigVar(
    Name = "variable.name",           // Variable identifier
    DefaultValue = "default",         // Default value as string
    Description = "Description text", // Help text
    Flags = ConfigFlags.Save         // Behavior flags
)]
public static ConfigVar VariableName;
```

**ConfigFlags Options**:
```csharp
[Flags]
public enum ConfigFlags
{
    None = 0,
    Save = 1,      // Persist to config file
    Cheat = 2,     // Mark as cheat/debug variable
    ReadOnly = 4   // Cannot be modified at runtime
}
```

**Usage Examples**:
```csharp
// Audio settings
[ConfigVar(Name = "audio.master_volume", DefaultValue = "1.0", 
           Description = "Master volume (0.0-1.0)", Flags = ConfigFlags.Save)]
public static ConfigVar MasterVolume;

// Debug settings
[ConfigVar(Name = "debug.show_fps", DefaultValue = "0", 
           Description = "Show FPS counter", Flags = ConfigFlags.Cheat)]
public static ConfigVar ShowFPS;

// Game settings
[ConfigVar(Name = "player.name", DefaultValue = "Player", 
           Description = "Player display name", Flags = ConfigFlags.Save)]
public static ConfigVar PlayerName;
```

### ConfigVar Access

**Direct Access**:
```csharp
// Get values
string stringValue = MyConfigVar.Value;
int intValue = MyConfigVar.IntValue;
float floatValue = MyConfigVar.FloatValue;

// Set values
MyConfigVar.Value = "new_value";

// Change detection
if (MyConfigVar.ChangeCheck())
{
    // Variable was modified since last check
    ApplyNewSetting();
}
```

**Service Access**:
```csharp
// Through ConfigService
var configService = GameManager.GetService<IConfigService>();

// Get typed values
float volume = configService.GetConfigValue<float>("audio.master_volume");
bool fullscreen = configService.GetConfigValue<bool>("graphics.fullscreen");
string playerName = configService.GetConfigValue<string>("player.name");

// Set values
configService.SetConfigValue("audio.master_volume", 0.8f);
configService.SetConfigValue("graphics.fullscreen", true);

// Save/load
await configService.SaveConfigAsync();
await configService.LoadConfigAsync();
```

## 🔍 Debug and Utility Methods

### GameStateMachine Debug Methods

```csharp
// Get valid transitions from current state
GameStateType[] validTransitions = stateMachine.GetValidTransitionsFromCurrentState();

// Check specific transition
bool isValid = stateMachine.IsTransitionValid(GameStateType.MainMenu, GameStateType.Playing);

// Navigation history
GameStateType? previous = stateMachine.GetPreviousState();
await stateMachine.GoBackToPreviousStateAsync();
stateMachine.ClearStateHistory();
```

### Service Registration Check

```csharp
// Check if service is registered
bool isRegistered = container.IsRegistered<IAudioService>();

// Get service if available
if (container.IsRegistered<ICustomService>())
{
    var customService = container.Resolve<ICustomService>();
}
```

---

**Next**: Check out [Examples](Examples.md) for practical usage scenarios!
```