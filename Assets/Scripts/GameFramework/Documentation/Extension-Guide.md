
## 4. Extension-Guide.md

```markdown
# Extension Guide

Learn how to extend the Unity Game Framework with custom services, states, events, and UI components.

## 🎯 Overview

The framework is designed for extensibility. You can easily add:
- New services and systems
- Custom game states
- Additional events
- New UI screens and popups
- Custom configuration variables

## 🔧 Adding New Services

### Step 1: Define Service Interface

```csharp
/// <summary>
/// Interface for player management service
/// </summary>
public interface IPlayerService : IGameService
{
    // Properties
    int Level { get; }
    float Experience { get; }
    int Health { get; }
    int MaxHealth { get; }
    
    // Methods
    void TakeDamage(int amount);
    void Heal(int amount);
    void GainExperience(float amount);
    void LevelUp();
    
    // Events (optional - you can also use the global event system)
    event Action<int> OnHealthChanged;
    event Action<int> OnLevelUp;
}
```

### Step 2: Implement Service

```csharp
/// <summary>
/// Player management service with constructor injection
/// </summary>
public class PlayerService : IPlayerService
{
    public int Level { get; private set; } = 1;
    public float Experience { get; private set; }
    public int Health { get; private set; }
    public int MaxHealth { get; private set; } = 100;
    
    // Injected dependencies
    private readonly IEventSystem _eventSystem;
    private readonly IAudioService _audioService;
    private readonly ISaveService _saveService;
    private readonly IConfigService _configService;
    
    public bool IsInitialized { get; private set; }
    
    // Events
    public event Action<int> OnHealthChanged;
    public event Action<int> OnLevelUp;
    
    /// <summary>
    /// Constructor injection - specify all dependencies
    /// </summary>
    public PlayerService(
        IEventSystem eventSystem,
        IAudioService audioService,
        ISaveService saveService,
        IConfigService configService)
    {
        _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
        _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
        _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }
    
    public async Task InitializeAsync()
    {
        if (IsInitialized) return;
        
        Debug.Log("[PlayerService] Initializing player service...");
        
        // Load player data from config/save
        LoadPlayerData();
        
        // Subscribe to relevant events
        _eventSystem.Subscribe<GameStartedEvent>(OnGameStarted);
        _eventSystem.Subscribe<SaveGameEvent>(OnSaveGame);
        
        IsInitialized = true;
        await Task.CompletedTask;
    }
    
    public void Shutdown()
    {
        // Unsubscribe from events
        _eventSystem.Unsubscribe<GameStartedEvent>(OnGameStarted);
        _eventSystem.Unsubscribe<SaveGameEvent>(OnSaveGame);
        
        IsInitialized = false;
    }
    
    public void TakeDamage(int amount)
    {
        Health = Mathf.Max(0, Health - amount);
        
        // Play hurt sound using injected audio service
        _audioService.PlaySound("player_hurt");
        
        // Publish global event using injected event system
        _eventSystem.Publish(new PlayerDamagedEvent { Damage = amount, CurrentHealth = Health });
        
        // Trigger local event
        OnHealthChanged?.Invoke(Health);
        
        if (Health <= 0)
        {
            _eventSystem.Publish<PlayerDeathEvent>();
        }
    }
    
    public void Heal(int amount)
    {
        Health = Mathf.Min(MaxHealth, Health + amount);
        _audioService.PlaySound("heal");
        OnHealthChanged?.Invoke(Health);
    }
    
    public void GainExperience(float amount)
    {
        Experience += amount;
        
        // Check for level up
        float expRequired = GetExperienceForLevel(Level + 1);
        if (Experience >= expRequired)
        {
            LevelUp();
        }
    }
    
    public void LevelUp()
    {
        Level++;
        MaxHealth += 10; // Example: gain 10 health per level
        Health = MaxHealth; // Full heal on level up
        
        _audioService.PlaySound("levelup");
        _eventSystem.Publish(new PlayerLevelUpEvent { NewLevel = Level });
        OnLevelUp?.Invoke(Level);
    }
    
    private void LoadPlayerData()
    {
        // Load from config service or save data
        var startingHealth = _configService.GetConfigValue<int>("player.starting_health");
        Health = MaxHealth = startingHealth;
    }
    
    private void OnGameStarted(GameStartedEvent evt)
    {
        // Reset or initialize player for new game
        Debug.Log("[PlayerService] Game started - player ready");
    }
    
    private void OnSaveGame(SaveGameEvent evt)
    {
        // Save player data
        // The SaveService will automatically include this data if you implement ISaveable
    }
    
    private float GetExperienceForLevel(int level)
    {
        // Example XP curve
        return level * 100f;
    }
}

// Custom events for this service
public class PlayerDamagedEvent
{
    public int Damage { get; set; }
    public int CurrentHealth { get; set; }
}

public class PlayerLevelUpEvent
{
    public int NewLevel { get; set; }
}

public class PlayerDeathEvent { }
```

### Step 3: Register Service

```csharp
// In GameManager.RegisterGameSystems()
private void RegisterGameSystems()
{
    Debug.Log("[GameManager] Registering game systems...");
    
    // Register your new service
    _container.RegisterSingleton<IPlayerService, PlayerService>();
    
    // Register other game-specific systems
    _container.RegisterSingleton<IInventorySystem, InventorySystem>();
    _container.RegisterSingleton<IEnemyManager, EnemyManager>();
}
```

### Step 4: Use Service in Game States

```csharp
public class PlayingState : BaseGameState
{
    private readonly IPlayerService _playerService; // Add as dependency
    
    public PlayingState(
        IGameStateMachine stateMachine,
        IEventSystem eventSystem,
        IAudioService audioService,
        IUIService uiService,
        IInputService inputService,
        IPlayerService playerService) // ← Add to constructor
        : base(GameStateType.Playing, stateMachine, eventSystem, audioService, uiService, inputService)
    {
        _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
    }
    
    public override async Task EnterAsync(GameContext context)
    {
        await base.EnterAsync(context);
        
        // Subscribe to player events
        _playerService.OnHealthChanged += OnPlayerHealthChanged;
        _playerService.OnLevelUp += OnPlayerLevelUp;
        
        // Update UI with current player stats
        var hud = UIService.GetScreen<GameplayHUD>();
        hud?.UpdateHealth(_playerService.Health, _playerService.MaxHealth);
    }
    
    public override async Task ExitAsync()
    {
        // Unsubscribe from player events
        _playerService.OnHealthChanged -= OnPlayerHealthChanged;
        _playerService.OnLevelUp -= OnPlayerLevelUp;
        
        await base.ExitAsync();
    }
    
    private void OnPlayerHealthChanged(int newHealth)
    {
        var hud = UIService.GetScreen<GameplayHUD>();
        hud?.UpdateHealth(newHealth, _playerService.MaxHealth);
    }
    
    private void OnPlayerLevelUp(int newLevel)
    {
        // Show level up effect
        AudioService.PlaySound("fanfare");
    }
}
```

## 🎮 Adding Custom Game States

### Step 1: Define State Class

```csharp
/// <summary>
/// Custom inventory management state
/// </summary>
public class InventoryState : BaseGameState
{
    private readonly IInventorySystem _inventorySystem;
    private readonly IPlayerService _playerService;
    
    public InventoryState(
        IGameStateMachine stateMachine,
        IEventSystem eventSystem,
        IAudioService audioService,
        IUIService uiService,
        IInputService inputService,
        IInventorySystem inventorySystem,
        IPlayerService playerService)
        : base(GameStateType.Inventory, stateMachine, eventSystem, audioService, uiService, inputService)
    {
        _inventorySystem = inventorySystem ?? throw new ArgumentNullException(nameof(inventorySystem));
        _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
    }
    
    public override async Task EnterAsync(GameContext context)
    {
        await base.EnterAsync(context);
        
        Debug.Log("[InventoryState] Opening inventory...");
        
        // Show inventory screen
        await UIService.ShowScreenAsync<InventoryScreen>();
        
        // Pause game time (like pause state)
        Time.timeScale = 0f;
        
        // Subscribe to inventory events
        EventSystem.Subscribe<ItemUsedEvent>(OnItemUsed);
        EventSystem.Subscribe<ItemDroppedEvent>(OnItemDropped);
        EventSystem.Subscribe<CloseInventoryEvent>(OnCloseInventory);
        
        // Play inventory open sound
        AudioService.PlaySound("inventory_open");
    }
    
    public override void Update()
    {
        // Handle inventory-specific input
        if (InputService.GetKeyDown("Escape") || InputService.GetKeyDown("Inventory"))
        {
            EventSystem.Publish<CloseInventoryEvent>();
        }
    }
    
    public override async Task ExitAsync()
    {
        Debug.Log("[InventoryState] Closing inventory...");
        
        // Unsubscribe from events
        EventSystem.Unsubscribe<ItemUsedEvent>(OnItemUsed);
        EventSystem.Unsubscribe<ItemDroppedEvent>(OnItemDropped);
        EventSystem.Unsubscribe<CloseInventoryEvent>(OnCloseInventory);
        
        // Hide inventory screen
        await UIService.HideScreenAsync<InventoryScreen>();
        
        // Resume game time
        Time.timeScale = 1f;
        
        // Play close sound
        AudioService.PlaySound("inventory_close");
        
        await base.ExitAsync();
    }
    
    private void OnItemUsed(ItemUsedEvent evt)
    {
        // Handle item usage
        AudioService.PlaySound("item_use");
    }
    
    private void OnItemDropped(ItemDroppedEvent evt)
    {
        // Handle item dropping
        AudioService.PlaySound("item_drop");
    }
    
    private async void OnCloseInventory(CloseInventoryEvent evt)
    {
        // Return to previous state (usually Playing)
        await TransitionToStateAsync(GameStateType.Playing);
    }
}
```

### Step 2: Add State to Enum

```csharp
// Add to GameStateType enum
public enum GameStateType
{
    Bootstrap,
    Splash,
    MainMenu,
    Loading,
    NewGame,
    Playing,
    Paused,
    Options,
    Credits,
    GameOver,
    Victory,
    Quit,
    Inventory  // ← Add your new state
}
```

### Step 3: Register State

```csharp
// In GameManager.RegisterGameStates()
private void RegisterGameStates()
{
    // ... existing registrations
    
    _container.RegisterTransient<InventoryState, InventoryState>();
}

// In GameStateMachine.RegisterStates()
private void RegisterStates()
{
    // ... existing states
    
    RegisterState(_container.Resolve<InventoryState>());
}

// In GameStateMachine.AllGameStates array
private static readonly GameStateType[] AllGameStates = new GameStateType[]
{
    // ... existing states
    GameStateType.Inventory
};
```

### Step 4: Define State Transitions

```csharp
// In GameStateMachine.DefineStateTransitions()
private void DefineStateTransitions()
{
    // ... existing transitions
    
    // Playing can go to Inventory
    _validTransitions.Add((GameStateType.Playing, GameStateType.Inventory));
    
    // Inventory can return to Playing
    _validTransitions.Add((GameStateType.Inventory, GameStateType.Playing));
    
    // Inventory can go to Options
    _validTransitions.Add((GameStateType.Inventory, GameStateType.Options));
}
```

## 📨 Adding Custom Events

### Step 1: Define Event Classes

```csharp
// Simple events (no data)
public class InventoryOpenedEvent { }
public class InventoryClosedEvent { }

// Events with data
public class ItemPickedUpEvent
{
    public string ItemId { get; set; }
    public string ItemName { get; set; }
    public int Quantity { get; set; }
    public Vector3 PickupLocation { get; set; }
}

public class ItemUsedEvent
{
    public string ItemId { get; set; }
    public string ItemName { get; set; }
    public ItemType ItemType { get; set; }
    public int RemainingQuantity { get; set; }
}

public class QuestCompletedEvent
{
    public string QuestId { get; set; }
    public string QuestName { get; set; }
    public int ExperienceReward { get; set; }
    public List<string> ItemRewards { get; set; }
}

// Complex events with nested data
public class CombatEncounterEvent
{
    public CombatData Combat { get; set; }
    public List<ParticipantData> Participants { get; set; }
}

public class CombatData
{
    public float Duration { get; set; }
    public int TotalDamage { get; set; }
    public bool PlayerVictory { get; set; }
}
```

### Step 2: Publish Events

```csharp
// From any service or state
public class InventorySystem : IInventorySystem
{
    private readonly IEventSystem _eventSystem;
    
    public void PickupItem(string itemId, int quantity, Vector3 location)
    {
        // Add item to inventory logic...
        
        // Publish event
        _eventSystem.Publish(new ItemPickedUpEvent
        {
            ItemId = itemId,
            ItemName = GetItemName(itemId),
            Quantity = quantity,
            PickupLocation = location
        });
    }
}
```

### Step 3: Subscribe to Events

```csharp
// In any service or state
public class PlayerService : IPlayerService
{
    public async Task InitializeAsync()
    {
        // Subscribe to relevant events
        _eventSystem.Subscribe<ItemUsedEvent>(OnItemUsed);
        _eventSystem.Subscribe<QuestCompletedEvent>(OnQuestCompleted);
    }
    
    private void OnItemUsed(ItemUsedEvent evt)
    {
        // React to item usage
        if (evt.ItemType == ItemType.HealthPotion)
        {
            Heal(25);
        }
    }
    
    private void OnQuestCompleted(QuestCompletedEvent evt)
    {
        // Give rewards
        GainExperience(evt.ExperienceReward);
        
        foreach (var itemReward in evt.ItemRewards)
        {
            // Add items to inventory
        }
    }
}
```

## 🎨 Adding Custom UI Screens

### Step 1: Define Screen Class

```csharp
/// <summary>
/// Custom inventory screen
/// </summary>
public class InventoryScreen : UIScreen
{
    // UI Elements
    private ListView _itemList;
    private Label _selectedItemName;
    private Label _selectedItemDescription;
    private Button _useItemButton;
    private Button _dropItemButton;
    private Button _closeButton;
    
    // Data
    private List<InventoryItem> _items = new List<InventoryItem>();
    private InventoryItem _selectedItem;
    
    public InventoryScreen(VisualElement rootElement) : base(rootElement)
    {
        InitializeElements();
        SetupEventHandlers();
    }
    
    private void InitializeElements()
    {
        _itemList = RootElement?.Q<ListView>("ItemList");
        _selectedItemName = RootElement?.Q<Label>("SelectedItemName");
        _selectedItemDescription = RootElement?.Q<Label>("SelectedItemDescription");
        _useItemButton = RootElement?.Q<Button>("UseItemButton");
        _dropItemButton = RootElement?.Q<Button>("DropItemButton");
        _closeButton = RootElement?.Q<Button>("CloseButton");
        
        // Setup ListView
        if (_itemList != null)
        {
            _itemList.makeItem = MakeItem;
            _itemList.bindItem = BindItem;
            _itemList.onSelectionChange += OnItemSelectionChanged;
        }
    }
    
    private void SetupEventHandlers()
    {
        _useItemButton?.RegisterCallback<ClickEvent>(OnUseItemClicked);
        _dropItemButton?.RegisterCallback<ClickEvent>(OnDropItemClicked);
        _closeButton?.RegisterCallback<ClickEvent>(OnCloseClicked);
    }
    
    protected override void OnShow()
    {
        base.OnShow();
        
        // Load current inventory data
        RefreshInventory();
        
        // Subscribe to inventory events
        var eventSystem = GameManager.GetService<IEventSystem>();
        eventSystem?.Subscribe<ItemPickedUpEvent>(OnItemPickedUp);
        eventSystem?.Subscribe<ItemUsedEvent>(OnItemUsed);
    }
    
    protected override void OnHide()
    {
        // Unsubscribe from events
        var eventSystem = GameManager.GetService<IEventSystem>();
        eventSystem?.Unsubscribe<ItemPickedUpEvent>(OnItemPickedUp);
        eventSystem?.Unsubscribe<ItemUsedEvent>(OnItemUsed);
        
        base.OnHide();
    }
    
    private VisualElement MakeItem()
    {
        var item = new VisualElement();
        item.AddToClassList("inventory-item");
        
        var icon = new VisualElement();
        icon.AddToClassList("item-icon");
        
        var nameLabel = new Label();
        nameLabel.AddToClassList("item-name");
        
        var quantityLabel = new Label();
        quantityLabel.AddToClassList("item-quantity");
        
        item.Add(icon);
        item.Add(nameLabel);
        item.Add(quantityLabel);
        
        return item;
    }
    
    private void BindItem(VisualElement element, int index)
    {
        if (index >= _items.Count) return;
        
        var item = _items[index];
        var nameLabel = element.Q<Label>(className: "item-name");
        var quantityLabel = element.Q<Label>(className: "item-quantity");
        
        nameLabel.text = item.Name;
        quantityLabel.text = item.Quantity.ToString();
    }
    
    private void OnItemSelectionChanged(IEnumerable<object> selectedItems)
    {
        var selectedIndices = _itemList.selectedIndices.ToArray();
        if (selectedIndices.Length > 0)
        {
            var index = selectedIndices[0];
            if (index < _items.Count)
            {
                _selectedItem = _items[index];
                UpdateSelectedItemInfo();
            }
        }
    }
    
    private void UpdateSelectedItemInfo()
    {
        if (_selectedItem != null)
        {
            _selectedItemName.text = _selectedItem.Name;
            _selectedItemDescription.text = _selectedItem.Description;
            _useItemButton.SetEnabled(_selectedItem.IsUsable);
            _dropItemButton.SetEnabled(true);
        }
        else
        {
            _selectedItemName.text = "No item selected";
            _selectedItemDescription.text = "";
            _useItemButton.SetEnabled(false);
            _dropItemButton.SetEnabled(false);
        }
    }
    
    private void OnUseItemClicked(ClickEvent evt)
    {
        if (_selectedItem != null)
        {
            var eventSystem = GameManager.GetService<IEventSystem>();
            eventSystem?.Publish(new ItemUsedEvent
            {
                ItemId = _selectedItem.Id,
                ItemName = _selectedItem.Name,
                ItemType = _selectedItem.Type,
                RemainingQuantity = _selectedItem.Quantity - 1
            });
        }
    }
    
    private void OnDropItemClicked(ClickEvent evt)
    {
        if (_selectedItem != null)
        {
            var eventSystem = GameManager.GetService<IEventSystem>();
            eventSystem?.Publish(new ItemDroppedEvent
            {
                ItemId = _selectedItem.Id,
                ItemName = _selectedItem.Name
            });
        }
    }
    
    private void OnCloseClicked(ClickEvent evt)
    {
        var eventSystem = GameManager.GetService<IEventSystem>();
        eventSystem?.Publish<CloseInventoryEvent>();
    }
    
    private void OnItemPickedUp(ItemPickedUpEvent evt)
    {
        // Refresh inventory when items are picked up
        RefreshInventory();
    }
    
    private void OnItemUsed(ItemUsedEvent evt)
    {
        // Refresh inventory when items are used
        RefreshInventory();
    }
    
    private void RefreshInventory()
    {
        // Get current inventory from inventory service
        var inventoryService = GameManager.GetService<IInventorySystem>();
        _items = inventoryService?.GetAllItems() ?? new List<InventoryItem>();
        
        // Update ListView
        _itemList.itemsSource = _items;
        _itemList.RefreshItems();
        
        // Clear selection
        _selectedItem = null;
        UpdateSelectedItemInfo();
    }
}

// Supporting classes
public class InventoryItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public ItemType Type { get; set; }
    public int Quantity { get; set; }
    public bool IsUsable { get; set; }
}

public enum ItemType
{
    Weapon,
    Armor,
    HealthPotion,
    ManaPotion,
    QuestItem,
    Consumable
}

// Additional events
public class ItemDroppedEvent
{
    public string ItemId { get; set; }
    public string ItemName { get; set; }
}

public class CloseInventoryEvent { }
```

### Step 2: Add to UXML

```xml
<!-- Add to your main UXML file -->
<ui:VisualElement name="InventoryScreen" style="display: none;" class="full-screen">
    <ui:VisualElement class="inventory-panel">
        <ui:Label text="Inventory" class="panel-title" />
        
        <ui:VisualElement class="inventory-content">
            <!-- Item list -->
            <ui:VisualElement class="item-list-container">
                <ui:ListView name="ItemList" class="item-list" />
            </ui:VisualElement>
            
            <!-- Item details -->
            <ui:VisualElement class="item-details">
                <ui:Label name="SelectedItemName" text="No item selected" class="item-title" />
                <ui:Label name="SelectedItemDescription" text="" class="item-description" />
                
                <ui:VisualElement class="item-actions">
                    <ui:Button name="UseItemButton" text="Use" />
                    <ui:Button name="DropItemButton" text="Drop" />
                </ui:VisualElement>
            </ui:VisualElement>
        </ui:VisualElement>
        
        <ui:Button name="CloseButton" text="Close" class="close-button" />
    </ui:VisualElement>
</ui:VisualElement>
```

### Step 3: Register Screen

```csharp
// In UIService.InitializeScreensAndPopups()
private void InitializeScreensAndPopups()
{
    var root = _uiDocument.rootVisualElement;
    
    // ... existing screens
    
    // Register your new screen
    RegisterScreen(new InventoryScreen(root.Q<VisualElement>("InventoryScreen")));
}
```

## ⚙️ Adding Configuration Variables

### Step 1: Define ConfigVars

```csharp
// Add to DefaultConfigVars or create your own class
public static class GameplayConfigVars
{
    [ConfigVar(Name = "player.starting_health", DefaultValue = "100", 
               Description = "Player starting health points", Flags = ConfigFlags.Save)]
    public static ConfigVar StartingHealth;
    
    [ConfigVar(Name = "player.max_level", DefaultValue = "50", 
               Description = "Maximum player level", Flags = ConfigFlags.Save)]
    public static ConfigVar MaxLevel;
    
    [ConfigVar(Name = "inventory.max_items", DefaultValue = "50", 
               Description = "Maximum items in inventory", Flags = ConfigFlags.Save)]
    public static ConfigVar MaxInventoryItems;
    
    [ConfigVar(Name = "combat.damage_multiplier", DefaultValue = "1.0", 
               Description = "Global damage multiplier", Flags = ConfigFlags.Save)]
    public static ConfigVar DamageMultiplier;
    
    [ConfigVar(Name = "debug.show_collision_bounds", DefaultValue = "0", 
               Description = "Show collision boundaries in debug mode")]
    public static ConfigVar ShowCollisionBounds;
}
```

### Step 2: Use ConfigVars in Services

```csharp
public class PlayerService : IPlayerService
{
    private readonly IConfigService _configService;
    
    private void LoadPlayerData()
    {
        // Use config service to get values
        var startingHealth = _configService.GetConfigValue<int>("player.starting_health");
        var maxLevel = _configService.GetConfigValue<int>("player.max_level");
        
        Health = MaxHealth = startingHealth;
        
        // Or use the ConfigVar directly
        var maxItems = GameplayConfigVars.MaxInventoryItems.IntValue;
    }
    
    public void TakeDamage(int baseDamage)
    {
        // Apply damage multiplier from config
        var multiplier = _configService.GetConfigValue<float>("combat.damage_multiplier");
        var actualDamage = Mathf.RoundToInt(baseDamage * multiplier);
        
        Health = Mathf.Max(0, Health - actualDamage);
    }
}
```

## 📋 Extension Checklist

When adding new components, use this checklist:

### ✅ New Service Checklist
- [ ] Define interface inheriting from `IGameService`
- [ ] Implement service with constructor injection
- [ ] Add async `InitializeAsync()` and `Shutdown()` methods
- [ ] Register in DI container (`GameManager.RegisterGameSystems()`)
- [ ] Add to service dependencies where needed
- [ ] Write unit tests

### ✅ New State Checklist
- [ ] Add to `GameStateType` enum
- [ ] Create state class inheriting from `BaseGameState`
- [ ] Implement constructor with all needed dependencies
- [ ] Override `EnterAsync()`, `Update()`, and `ExitAsync()`
- [ ] Register state in `GameStateMachine.RegisterStates()`
- [ ] Add to `AllGameStates` array
- [ ] Define valid transitions in `DefineStateTransitions()`
- [ ] Test state transitions

### ✅ New Event Checklist
- [ ] Create event class (can be empty for simple events)
- [ ] Add relevant properties for event data
- [ ] Publish from appropriate services/states
- [ ] Subscribe in systems that need to react
- [ ] Ensure proper unsubscription in cleanup methods

### ✅ New UI Screen Checklist
- [ ] Create UXML elements in UI document
- [ ] Create screen class inheriting from `UIScreen` or `UIPopup`
- [ ] Initialize UI elements in constructor
- [ ] Override `OnShow()` and `OnHide()` for setup/cleanup
- [ ] Register screen in `UIService.InitializeScreensAndPopups()`
- [ ] Add CSS classes if needed
- [ ] Test show/hide functionality

---

**Next**: Check out [Examples](Examples.md) for practical implementation examples!
```