## 6. Examples.md

```markdown
# Examples

Practical examples and usage patterns for the Unity Game Framework.

## 📋 Table of Contents

- [Basic Game Setup](#basic-game-setup)
- [Custom Services](#custom-services)
- [Game State Examples](#game-state-examples)
- [UI Integration](#ui-integration)
- [Event Communication](#event-communication)
- [Configuration Usage](#configuration-usage)
- [Complete Game Systems](#complete-game-systems)

## 🚀 Basic Game Setup

### Minimal Game Implementation

This example shows the absolute minimum to get a working game:

```csharp
// 1. Just add GameManager to your scene - that's it!
// The framework handles everything automatically.

// 2. Optionally, customize the startup in your own script:
public class MyGameBootstrap : MonoBehaviour
{
    private void Start()
    {
        // Access any service after framework initialization
        var audioService = GameManager.GetService<IAudioService>();
        audioService?.PlayMusic("welcome");
        
        // Listen for game events
        var eventSystem = GameManager.GetService<IEventSystem>();
        eventSystem?.Subscribe<GameStartedEvent>(OnGameStarted);
    }
    
    private void OnGameStarted(GameStartedEvent evt)
    {
        Debug.Log("Game has started!");
    }
}
```

### Custom Game Configuration

```csharp
// Define your game's config variables
public static class MyGameConfig
{
    [ConfigVar(Name = "game.difficulty", DefaultValue = "1", 
               Description = "Game difficulty (0=Easy, 1=Normal, 2=Hard)", 
               Flags = ConfigFlags.Save)]
    public static ConfigVar Difficulty;
    
    [ConfigVar(Name = "game.player_speed", DefaultValue = "5.0", 
               Description = "Player movement speed", 
               Flags = ConfigFlags.Save)]
    public static ConfigVar PlayerSpeed;
    
    [ConfigVar(Name = "debug.god_mode", DefaultValue = "0", 
               Description = "Enable invincibility", 
               Flags = ConfigFlags.Cheat)]
    public static ConfigVar GodMode;
}

// Use config in your game logic
public class PlayerController : MonoBehaviour
{
    private void Start()
    {
        // Get movement speed from config
        float speed = MyGameConfig.PlayerSpeed.FloatValue;
        GetComponent<CharacterController>().moveSpeed = speed;
    }
}
```

## 🔧 Custom Services

### Player Management Service

Complete example of a player management system:

```csharp
// Interface
public interface IPlayerService : IGameService
{
    // Properties
    string PlayerName { get; set; }
    int Level { get; }
    int Health { get; }
    int MaxHealth { get; }
    float Experience { get; }
    
    // Methods
    void TakeDamage(int amount);
    void Heal(int amount);
    void GainExperience(float amount);
    void SetPlayerName(string name);
    
    // Events
    event Action<int> OnHealthChanged;
    event Action<int> OnLevelChanged;
    event Action OnPlayerDied;
}

// Implementation
public class PlayerService : IPlayerService
{
    private readonly IEventSystem _eventSystem;
    private readonly IAudioService _audioService;
    private readonly IConfigService _configService;
    private readonly ISaveService _saveService;
    
    // Properties
    public string PlayerName { get; set; } = "Player";
    public int Level { get; private set; } = 1;
    public int Health { get; private set; }
    public int MaxHealth { get; private set; }
    public float Experience { get; private set; }
    
    public bool IsInitialized { get; private set; }
    
    // Events
    public event Action<int> OnHealthChanged;
    public event Action<int> OnLevelChanged;
    public event Action OnPlayerDied;
    
    public PlayerService(
        IEventSystem eventSystem,
        IAudioService audioService,
        IConfigService configService,
        ISaveService saveService)
    {
        _eventSystem = eventSystem;
        _audioService = audioService;
        _configService = configService;
        _saveService = saveService;
    }
    
    public async Task InitializeAsync()
    {
        if (IsInitialized) return;
        
        // Load player data from config
        var startingHealth = _configService.GetConfigValue<int>("player.starting_health");
        Health = MaxHealth = startingHealth;
        
        // Subscribe to game events
        _eventSystem.Subscribe<NewGameStartedEvent>(OnNewGameStarted);
        _eventSystem.Subscribe<SaveGameEvent>(OnSaveGame);
        _eventSystem.Subscribe<LoadGameEvent>(OnLoadGame);
        
        IsInitialized = true;
    }
    
    public void Shutdown()
    {
        _eventSystem.Unsubscribe<NewGameStartedEvent>(OnNewGameStarted);
        _eventSystem.Unsubscribe<SaveGameEvent>(OnSaveGame);
        _eventSystem.Unsubscribe<LoadGameEvent>(OnLoadGame);
        IsInitialized = false;
    }
    
    public void TakeDamage(int amount)
    {
        if (_configService.GetConfigValue<bool>("debug.god_mode"))
            return; // Invincible in god mode
        
        int previousHealth = Health;
        Health = Mathf.Max(0, Health - amount);
        
        // Play sound effect
        _audioService.PlaySound("player_hurt");
        
        // Publish damage event
        _eventSystem.Publish(new PlayerDamagedEvent 
        { 
            PreviousHealth = previousHealth,
            CurrentHealth = Health,
            DamageAmount = amount
        });
        
        // Trigger local event
        OnHealthChanged?.Invoke(Health);
        
        // Check for death
        if (Health <= 0)
        {
            HandlePlayerDeath();
        }
    }
    
    public void Heal(int amount)
    {
        int previousHealth = Health;
        Health = Mathf.Min(MaxHealth, Health + amount);
        
        if (Health > previousHealth)
        {
            _audioService.PlaySound("heal");
            OnHealthChanged?.Invoke(Health);
            
            _eventSystem.Publish(new PlayerHealedEvent
            {
                HealAmount = Health - previousHealth,
                CurrentHealth = Health
            });
        }
    }
    
    public void GainExperience(float amount)
    {
        Experience += amount;
        
        // Check for level up
        while (Experience >= GetExperienceForLevel(Level + 1))
        {
            LevelUp();
        }
        
        _eventSystem.Publish(new ExperienceGainedEvent { Amount = amount });
    }
    
    public void SetPlayerName(string name)
    {
        string oldName = PlayerName;
        PlayerName = name;
        
        _eventSystem.Publish(new PlayerNameChangedEvent 
        { 
            OldName = oldName, 
            NewName = name 
        });
    }
    
    private void LevelUp()
    {
        Level++;
        
        // Increase max health
        MaxHealth += 10;
        Health = MaxHealth; // Full heal on level up
        
        // Play level up sound
        _audioService.PlaySound("level_up");
        
        // Publish events
        OnLevelChanged?.Invoke(Level);
        _eventSystem.Publish(new PlayerLevelUpEvent { NewLevel = Level });
    }
    
    private void HandlePlayerDeath()
    {
        _audioService.PlaySound("player_death");
        OnPlayerDied?.Invoke();
        _eventSystem.Publish<PlayerDeathEvent>();
    }
    
    private float GetExperienceForLevel(int level)
    {
        return level * 100f; // Simple XP curve
    }
    
    private void OnNewGameStarted(NewGameStartedEvent evt)
    {
        // Reset player stats for new game
        Level = 1;
        Experience = 0;
        var startingHealth = _configService.GetConfigValue<int>("player.starting_health");
        Health = MaxHealth = startingHealth;
        PlayerName = "Player";
    }
    
    private void OnSaveGame(SaveGameEvent evt)
    {
        // Save player data - implement ISaveable interface for automatic saving
    }
    
    private void OnLoadGame(LoadGameEvent evt)
    {
        // Load player data from save file
    }
}

// Register in GameManager
private void RegisterGameSystems()
{
    _container.RegisterSingleton<IPlayerService, PlayerService>();
}
```

### Inventory System

```csharp
public interface IInventorySystem : IGameService
{
    bool AddItem(string itemId, int quantity = 1);
    bool RemoveItem(string itemId, int quantity = 1);
    bool HasItem(string itemId, int quantity = 1);
    InventoryItem GetItem(string itemId);
    List<InventoryItem> GetAllItems();
    int GetItemCount(string itemId);
    bool IsInventoryFull();
}

public class InventorySystem : IInventorySystem
{
    private readonly IEventSystem _eventSystem;
    private readonly IConfigService _configService;
    private readonly Dictionary<string, InventoryItem> _items = new();
    
    public bool IsInitialized { get; private set; }
    
    public InventorySystem(IEventSystem eventSystem, IConfigService configService)
    {
        _eventSystem = eventSystem;
        _configService = configService;
    }
    
    public async Task InitializeAsync()
    {
        if (IsInitialized) return;
        
        _eventSystem.Subscribe<ItemPickupRequestEvent>(OnItemPickupRequest);
        _eventSystem.Subscribe<ItemUseRequestEvent>(OnItemUseRequest);
        
        IsInitiized = true;
    }
    
    public void Shutdown()
    {
        _eventSystem.Unsubscribe<ItemPickupRequestEvent>(OnItemPickupRequest);
        _eventSystem.Unsubscribe<ItemUseRequestEvent>(OnItemUseRequest);
        IsInitialized = false;
    }
    
    public bool AddItem(string itemId, int quantity = 1)
    {
        if (IsInventoryFull()) return false;
        
        if (_items.ContainsKey(itemId))
        {
            _items[itemId].Quantity += quantity;
        }
        else
        {
            _items[itemId] = new InventoryItem
            {
                Id = itemId,
                Name = GetItemName(itemId),
                Quantity = quantity
            };
        }
        
        _eventSystem.Publish(new ItemAddedToInventoryEvent
        {
            ItemId = itemId,
            Quantity = quantity,
            TotalQuantity = _items[itemId].Quantity
        });
        
        return true;
    }
    
    public bool RemoveItem(string itemId, int quantity = 1)
    {
        if (!HasItem(itemId, quantity)) return false;
        
        _items[itemId].Quantity -= quantity;
        
        if (_items[itemId].Quantity <= 0)
        {
            _items.Remove(itemId);
        }
        
        _eventSystem.Publish(new ItemRemovedFromInventoryEvent
        {
            ItemId = itemId,
            Quantity = quantity
        });
        
        return true;
    }
    
    public bool HasItem(string itemId, int quantity = 1)
    {
        return _items.ContainsKey(itemId) && _items[itemId].Quantity >= quantity;
    }
    
    public bool IsInventoryFull()
    {
        var maxItems = _configService.GetConfigValue<int>("inventory.max_items");
        return _items.Count >= maxItems;
    }
    
    private void OnItemPickupRequest(ItemPickupRequestEvent evt)
    {
        if (AddItem(evt.ItemId, evt.Quantity))
        {
            _eventSystem.Publish(new ItemPickedUpEvent
            {
                ItemId = evt.ItemId,
                Quantity = evt.Quantity,
                PickupLocation = evt.PickupLocation
            });
        }
    }
    
    private void OnItemUseRequest(ItemUseRequestEvent evt)
    {
        if (RemoveItem(evt.ItemId, 1))
        {
            _eventSystem.Publish(new ItemUsedEvent
            {
                ItemId = evt.ItemId,
                Effect = GetItemEffect(evt.ItemId)
            });
        }
    }
}
```

## 🎮 Game State Examples

### Custom Inventory State

```csharp
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
        _inventorySystem = inventorySystem;
        _playerService = playerService;
    }
    
    public override async Task EnterAsync(GameContext context)
    {
        await base.EnterAsync(context);
        
        Debug.Log("Opening inventory...");
        
        // Show inventory UI
        await UIService.ShowScreenAsync<InventoryScreen>();
        
        // Pause game
        Time.timeScale = 0f;
        
        // Darken background
        AudioService.SetMasterVolume(0.3f);
        
        // Subscribe to inventory events
        EventSystem.Subscribe<ItemSelectedEvent>(OnItemSelected);
        EventSystem.Subscribe<ItemUsedEvent>(OnItemUsed);
        EventSystem.Subscribe<CloseInventoryEvent>(OnCloseInventory);
        
        // Play open sound
        AudioService.PlaySound("inventory_open");
        
        // Update inventory display
        UpdateInventoryDisplay();
    }
    
    public override void Update()
    {
        // Handle close input
        if (InputService.GetKeyDown("Inventory") || InputService.GetKeyDown("Escape"))
        {
            EventSystem.Publish<CloseInventoryEvent>();
        }
        
        // Handle item shortcuts
        for (int i = 1; i <= 9; i++)
        {
            if (InputService.GetKeyDown($"Alpha{i}"))
            {
                UseItemAtSlot(i - 1);
            }
        }
    }
    
    public override async Task ExitAsync()
    {
        Debug.Log("Closing inventory...");
        
        // Unsubscribe from events
        EventSystem.Unsubscribe<ItemSelectedEvent>(OnItemSelected);
        EventSystem.Unsubscribe<ItemUsedEvent>(OnItemUsed);
        EventSystem.Unsubscribe<CloseInventoryEvent>(OnCloseInventory);
        
        // Hide inventory UI
        await UIService.HideScreenAsync<InventoryScreen>();
        
        // Resume game
        Time.timeScale = 1f;
        AudioService.SetMasterVolume(1f);
        
        // Play close sound
        AudioService.PlaySound("inventory_close");
        
        await base.ExitAsync();
    }
    
    private void OnItemSelected(ItemSelectedEvent evt)
    {
        // Update item description display
        var inventoryScreen = UIService.GetScreen<InventoryScreen>();
        inventoryScreen?.ShowItemDetails(evt.ItemId);
    }
    
    private void OnItemUsed(ItemUsedEvent evt)
    {
        // Apply item effect to player
        switch (evt.Effect)
        {
            case "heal_small":
                _playerService.Heal(25);
                break;
            case "heal_large":
                _playerService.Heal(100);
                break;
            // Add more item effects
        }
        
        // Refresh inventory display
        UpdateInventoryDisplay();
    }
    
    private async void OnCloseInventory(CloseInventoryEvent evt)
    {
        await TransitionToStateAsync(GameStateType.Playing);
    }
    
    private void UpdateInventoryDisplay()
    {
        var inventoryScreen = UIService.GetScreen<InventoryScreen>();
        var items = _inventorySystem.GetAllItems();
        inventoryScreen?.UpdateItemList(items);
    }
    
    private void UseItemAtSlot(int slot)
    {
        var items = _inventorySystem.GetAllItems();
        if (slot < items.Count)
        {
            var item = items[slot];
            EventSystem.Publish(new ItemUseRequestEvent { ItemId = item.Id });
        }
    }
}
```

### Shop State

```csharp
public class ShopState : BaseGameState
{
    private readonly IInventorySystem _inventorySystem;
    private readonly IPlayerService _playerService;
    private readonly ICurrencyService _currencyService;
    private readonly IShopService _shopService;
    
    public ShopState(
        IGameStateMachine stateMachine,
        IEventSystem eventSystem,
        IAudioService audioService,
        IUIService uiService,
        IInputService inputService,
        IInventorySystem inventorySystem,
        IPlayerService playerService,
        ICurrencyService currencyService,
        IShopService shopService)
        : base(GameStateType.Shop, stateMachine, eventSystem, audioService, uiService, inputService)
    {
        _inventorySystem = inventorySystem;
        _playerService = playerService;
        _currencyService = currencyService;
        _shopService = shopService;
    }
    
    public override async Task EnterAsync(GameContext context)
    {
        await base.EnterAsync(context);
        
        // Show shop UI
        await UIService.ShowScreenAsync<ShopScreen>();
        
        // Play shop music
        AudioService.PlayMusic("shop_theme");
        
        // Subscribe to shop events
        EventSystem.Subscribe<ItemPurchaseRequestEvent>(OnItemPurchaseRequest);
        EventSystem.Subscribe<ItemSellRequestEvent>(OnItemSellRequest);
        EventSystem.Subscribe<CloseShopEvent>(OnCloseShop);
        
        // Update shop display
        UpdateShopDisplay();
    }
    
    public override async Task ExitAsync()
    {
        // Unsubscribe from events
        EventSystem.Unsubscribe<ItemPurchaseRequestEvent>(OnItemPurchaseRequest);
        EventSystem.Unsubscribe<ItemSellRequestEvent>(OnItemSellRequest);
        EventSystem.Unsubscribe<CloseShopEvent>(OnCloseShop);
        
        // Hide shop UI
        await UIService.HideScreenAsync<ShopScreen>();
        
        // Resume previous music
        AudioService.PlayMusic("overworld");
        
        await base.ExitAsync();
    }
    
    private void OnItemPurchaseRequest(ItemPurchaseRequestEvent evt)
    {
        var shopItem = _shopService.GetShopItem(evt.ItemId);
        
        if (_currencyService.HasCurrency(shopItem.Price) && !_inventorySystem.IsInventoryFull())
        {
            _currencyService.SpendCurrency(shopItem.Price);
            _inventorySystem.AddItem(evt.ItemId, 1);
            
            AudioService.PlaySound("purchase_success");
            
            EventSystem.Publish(new ItemPurchasedEvent
            {
                ItemId = evt.ItemId,
                Price = shopItem.Price
            });
            
            UpdateShopDisplay();
        }
        else
        {
            AudioService.PlaySound("purchase_failed");
            
            if (!_currencyService.HasCurrency(shopItem.Price))
            {
                ShowMessage("Not enough gold!");
            }
            else
            {
                ShowMessage("Inventory is full!");
            }
        }
    }
    
    private void OnItemSellRequest(ItemSellRequestEvent evt)
    {
        if (_inventorySystem.HasItem(evt.ItemId))
        {
            var sellPrice = _shopService.GetSellPrice(evt.ItemId);
            _inventorySystem.RemoveItem(evt.ItemId, 1);
            _currencyService.AddCurrency(sellPrice);
            
            AudioService.PlaySound("sell_success");
            
            EventSystem.Publish(new ItemSoldEvent
            {
                ItemId = evt.ItemId,
                Price = sellPrice
            });
            
            UpdateShopDisplay();
        }
    }
    
    private async void OnCloseShop(CloseShopEvent evt)
    {
        await TransitionToStateAsync(GameStateType.Playing);
    }
    
    private void UpdateShopDisplay()
    {
        var shopScreen = UIService.GetScreen<ShopScreen>();
        var shopItems = _shopService.GetAvailableItems();
        var playerGold = _currencyService.GetCurrency();
        var playerItems = _inventorySystem.GetAllItems();
        
        shopScreen?.UpdateShopItems(shopItems);
        shopScreen?.UpdatePlayerGold(playerGold);
        shopScreen?.UpdatePlayerInventory(playerItems);
    }
    
    private void ShowMessage(string message)
    {
        var shopScreen = UIService.GetScreen<ShopScreen>();
        shopScreen?.ShowMessage(message);
    }
}
```

## 🎨 UI Integration

### Advanced Gameplay HUD

```csharp
public class GameplayHUD : UIScreen
{
    // Health bar
    private VisualElement _healthBar;
    private VisualElement _healthFill;
    private Label _healthText;
    
    // Experience bar
    private VisualElement _expBar;
    private VisualElement _expFill;
    private Label _levelText;
    
    // Currency display
    private Label _goldLabel;
    
    // Mini-map
    private VisualElement _miniMap;
    
    // Item hotbar
    private VisualElement _hotbar;
    private List<Button> _hotbarSlots;
    
    // Status effects
    private VisualElement _statusEffects;
    
    public GameplayHUD(VisualElement rootElement) : base(rootElement)
    {
        InitializeElements();
        SetupHotbar();
    }
    
    private void InitializeElements()
    {
        // Health elements
        _healthBar = RootElement?.Q<VisualElement>("HealthBar");
        _healthFill = _healthBar?.Q<VisualElement>("HealthFill");
        _healthText = _healthBar?.Q<Label>("HealthText");
        
        // Experience elements
        _expBar = RootElement?.Q<VisualElement>("ExpBar");
        _expFill = _expBar?.Q<VisualElement>("ExpFill");
        _levelText = RootElement?.Q<Label>("LevelText");
        
        // Currency
        _goldLabel = RootElement?.Q<Label>("GoldLabel");
        
        // Mini-map
        _miniMap = RootElement?.Q<VisualElement>("MiniMap");
        
        // Hotbar
        _hotbar = RootElement?.Q<VisualElement>("Hotbar");
        
        // Status effects
        _statusEffects = RootElement?.Q<VisualElement>("StatusEffects");
    }
    
    private void SetupHotbar()
    {
        _hotbarSlots = new List<Button>();
        
        for (int i = 0; i < 10; i++)
        {
            var slot = _hotbar?.Q<Button>($"HotbarSlot{i}");
            if (slot != null)
            {
                _hotbarSlots.Add(slot);
                
                // Capture index for lambda
                int slotIndex = i;
                slot.RegisterCallback<ClickEvent>(evt => OnHotbarSlotClicked(slotIndex));
            }
        }
    }
    
    protected override void OnShow()
    {
        base.OnShow();
        
        // Subscribe to player events
        var eventSystem = GameManager.GetService<IEventSystem>();
        eventSystem?.Subscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
        eventSystem?.Subscribe<PlayerExperienceChangedEvent>(OnPlayerExperienceChanged);
        eventSystem?.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        eventSystem?.Subscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        eventSystem?.Subscribe<ItemAddedToInventoryEvent>(OnItemAddedToInventory);
        eventSystem?.Subscribe<StatusEffectAppliedEvent>(OnStatusEffectApplied);
        eventSystem?.Subscribe<StatusEffectRemovedEvent>(OnStatusEffectRemoved);
        
        // Initial update
        RefreshAllDisplays();
    }
    
    protected override void OnHide()
    {
        // Unsubscribe from events
        var eventSystem = GameManager.GetService<IEventSystem>();
        eventSystem?.Unsubscribe<PlayerHealthChangedEvent>(OnPlayerHealthChanged);
        eventSystem?.Unsubscribe<PlayerExperienceChangedEvent>(OnPlayerExperienceChanged);
        eventSystem?.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        eventSystem?.Unsubscribe<CurrencyChangedEvent>(OnCurrencyChanged);
        eventSystem?.Unsubscribe<ItemAddedToInventoryEvent>(OnItemAddedToInventory);
        eventSystem?.Unsubscribe<StatusEffectAppliedEvent>(OnStatusEffectApplied);
        eventSystem?.Unsubscribe<StatusEffectRemovedEvent>(OnStatusEffectRemoved);
        
        base.OnHide();
    }
    
    private void OnPlayerHealthChanged(PlayerHealthChangedEvent evt)
    {
        UpdateHealthDisplay(evt.CurrentHealth, evt.MaxHealth);
    }
    
    private void OnPlayerExperienceChanged(PlayerExperienceChangedEvent evt)
    {
        UpdateExperienceDisplay(evt.CurrentExperience, evt.ExperienceToNext);
    }
    
    private void OnPlayerLevelUp(PlayerLevelUpEvent evt)
    {
        UpdateLevelDisplay(evt.NewLevel);
        
        // Show level up effect
        ShowLevelUpEffect();
    }
    
    private void OnCurrencyChanged(CurrencyChangedEvent evt)
    {
        UpdateCurrencyDisplay(evt.NewAmount);
    }
    
    private void OnItemAddedToInventory(ItemAddedToInventoryEvent evt)
    {
        // Update hotbar if it's a usable item
        RefreshHotbar();
    }
    
    private void OnStatusEffectApplied(StatusEffectAppliedEvent evt)
    {
        AddStatusEffectIcon(evt.EffectId, evt.Duration);
    }
    
    private void OnStatusEffectRemoved(StatusEffectRemovedEvent evt)
    {
        RemoveStatusEffectIcon(evt.EffectId);
    }
    
    public void UpdateHealthDisplay(int currentHealth, int maxHealth)
    {
        if (_healthFill != null && _healthText != null)
        {
            float healthPercent = (float)currentHealth / maxHealth;
            _healthFill.style.width = Length.Percent(healthPercent * 100);
            _healthText.text = $"{currentHealth}/{maxHealth}";
            
            // Change color based on health level
            if (healthPercent < 0.25f)
                _healthFill.style.backgroundColor = Color.red;
            else if (healthPercent < 0.5f)
                _healthFill.style.backgroundColor = Color.yellow;
            else
                _healthFill.style.backgroundColor = Color.green;
        }
    }
    
    public void UpdateExperienceDisplay(float currentExp, float expToNext)
    {
        if (_expFill != null)
        {
            float expPercent = currentExp / expToNext;
            _expFill.style.width = Length.Percent(expPercent * 100);
        }
    }
    
    public void UpdateLevelDisplay(int level)
    {
        if (_levelText != null)
            _levelText.text = $"Level {level}";
    }
    
    public void UpdateCurrencyDisplay(int gold)
    {
        if (_goldLabel != null)
            _goldLabel.text = $"Gold: {gold}";
    }
    
    private void RefreshHotbar()
    {
        var inventoryService = GameManager.GetService<IInventorySystem>();
        var usableItems = inventoryService?.GetUsableItems() ?? new List<InventoryItem>();
        
        for (int i = 0; i < _hotbarSlots.Count && i < usableItems.Count; i++)
        {
            var item = usableItems[i];
            var slot = _hotbarSlots[i];
            
            // Update slot appearance
            slot.text = item.Quantity.ToString();
            // Set item icon, tooltip, etc.
        }
        
        // Clear unused slots
        for (int i = usableItems.Count; i < _hotbarSlots.Count; i++)
        {
            _hotbarSlots[i].text = "";
        }
    }
    
    private void OnHotbarSlotClicked(int slotIndex)
    {
        // Use item in hotbar slot
        var eventSystem = GameManager.GetService<IEventSystem>();
        eventSystem?.Publish(new HotbarItemUseEvent { SlotIndex = slotIndex });
    }
    
    private void ShowLevelUpEffect()
    {
        // Implement level up visual effect
        // Could use DOTween, Animation, or UI Toolkit animations
    }
    
    private void AddStatusEffectIcon(string effectId, float duration)
    {
        // Add status effect icon to display
        var icon = new VisualElement();
        icon.name = $"StatusEffect_{effectId}";
        icon.AddToClassList("status-effect-icon");
        _statusEffects?.Add(icon);
        
        // Start timer to remove icon after duration
    }
    
    private void RemoveStatusEffectIcon(string effectId)
    {
        var icon = _statusEffects?.Q<VisualElement>($"StatusEffect_{effectId}");
        icon?.RemoveFromHierarchy();
    }
    
    private void RefreshAllDisplays()
    {
        var playerService = GameManager.GetService<IPlayerService>();
        var currencyService = GameManager.GetService<ICurrencyService>();
        
        if (playerService != null)
        {
            UpdateHealthDisplay(playerService.Health, playerService.MaxHealth);
            UpdateLevelDisplay(playerService.Level);
        }
        
        if (currencyService != null)
        {
            UpdateCurrencyDisplay(currencyService.GetCurrency());
        }
        
        RefreshHotbar();
    }
}
```

## 📨 Event Communication

### Quest System Integration

```csharp
// Quest-related events
public class QuestStartedEvent
{
    public string QuestId { get; set; }
    public string QuestTitle { get; set; }
    public string Description { get; set; }
}

public class QuestCompletedEvent
{
    public string QuestId { get; set; }
    public int ExperienceReward { get; set; }
    public int GoldReward { get; set; }
    public List<string> ItemRewards { get; set; }
}

public class QuestObjectiveUpdatedEvent
{
    public string QuestId { get; set; }
    public string ObjectiveId { get; set; }
    public int CurrentProgress { get; set; }
    public int RequiredProgress { get; set; }
    public bool IsCompleted { get; set; }
}

// Quest system that responds to game events
public class QuestService : IQuestService
{
    private readonly IEventSystem _eventSystem;
    private readonly IPlayerService _playerService;
    private readonly Dictionary<string, Quest> _activeQuests = new();
    
    public QuestService(IEventSystem eventSystem, IPlayerService playerService)
    {
        _eventSystem = eventSystem;
        _playerService = playerService;
    }
    
    public async Task InitializeAsync()
    {
        // Subscribe to game events that can progress quests
        _eventSystem.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
        _eventSystem.Subscribe<ItemCollectedEvent>(OnItemCollected);
        _eventSystem.Subscribe<LocationVisitedEvent>(OnLocationVisited);
        _eventSystem.Subscribe<NPCTalkedToEvent>(OnNPCTalkedTo);
    }
    
    private void OnEnemyKilled(EnemyKilledEvent evt)
    {
        // Check all active quests for "kill enemy" objectives
        foreach (var quest in _activeQuests.Values)
        {
            foreach (var objective in quest.Objectives)
            {
                if (objective.Type == QuestObjectiveType.KillEnemy && 
                    objective.TargetId == evt.EnemyType)
                {
                    UpdateObjectiveProgress(quest.Id, objective.Id, 1);
                }
            }
        }
    }
    
    private void OnItemCollected(ItemCollectedEvent evt)
    {
        // Update "collect item" quest objectives
        foreach (var quest in _activeQuests.Values)
        {
            foreach (var objective in quest.Objectives)
            {
                if (objective.Type == QuestObjectiveType.CollectItem && 
                    objective.TargetId == evt.ItemId)
                {
                    UpdateObjectiveProgress(quest.Id, objective.Id, evt.Quantity);
                }
            }
        }
    }
    
    public void StartQuest(string questId)
    {
        var quest = LoadQuestData(questId);
        _activeQuests[questId] = quest;
        
        _eventSystem.Publish(new QuestStartedEvent
        {
            QuestId = questId,
            QuestTitle = quest.Title,
            Description = quest.Description
        });
    }
    
    private void UpdateObjectiveProgress(string questId, string objectiveId, int amount)
    {
        if (!_activeQuests.TryGetValue(questId, out var quest))
            return;
            
        var objective = quest.Objectives.FirstOrDefault(o => o.Id == objectiveId);
        if (objective == null) return;
        
        objective.CurrentProgress += amount;
        bool wasCompleted = objective.IsCompleted;
        objective.IsCompleted = objective.CurrentProgress >= objective.RequiredProgress;
        
        // Publish objective update
        _eventSystem.Publish(new QuestObjectiveUpdatedEvent
        {
            QuestId = questId,
            ObjectiveId = objectiveId,
            CurrentProgress = objective.CurrentProgress,
            RequiredProgress = objective.RequiredProgress,
            IsCompleted = objective.IsCompleted
        });
        
        // Check if quest is now complete
        if (!wasCompleted && objective.IsCompleted && quest.IsComplete())
        {
            CompleteQuest(questId);
        }
    }
    
    private void CompleteQuest(string questId)
    {
        if (!_activeQuests.TryGetValue(questId, out var quest))
            return;
            
        // Give rewards
        _playerService.GainExperience(quest.ExperienceReward);
        
        var currencyService = GameManager.GetService<ICurrencyService>();
        currencyService?.AddCurrency(quest.GoldReward);
        
        var inventoryService = GameManager.GetService<IInventorySystem>();
        foreach (var itemReward in quest.ItemRewards)
        {
            inventoryService?.AddItem(itemReward.ItemId, itemReward.Quantity);
        }
        
        // Remove from active quests
        _activeQuests.Remove(questId);
        
        // Publish completion event
        _eventSystem.Publish(new QuestCompletedEvent
        {
            QuestId = questId,
            ExperienceReward = quest.ExperienceReward,
            GoldReward = quest.GoldReward,
            ItemRewards = quest.ItemRewards.Select(r => r.ItemId).ToList()
        });
    }
}
```

## ⚙️ Configuration Usage

### Graphics Settings Integration

```csharp
// Graphics configuration variables
public static class GraphicsConfig
{
    [ConfigVar(Name = "graphics.resolution_width", DefaultValue = "1920", 
               Description = "Screen resolution width", Flags = ConfigFlags.Save)]
    public static ConfigVar ResolutionWidth;
    
    [ConfigVar(Name = "graphics.resolution_height", DefaultValue = "1080", 
               Description = "Screen resolution height", Flags = ConfigFlags.Save)]
    public static ConfigVar ResolutionHeight;
    
    [ConfigVar(Name = "graphics.fullscreen", DefaultValue = "1", 
               Description = "Fullscreen mode", Flags = ConfigFlags.Save)]
    public static ConfigVar Fullscreen;
    
    [ConfigVar(Name = "graphics.quality_level", DefaultValue = "2", 
               Description = "Graphics quality (0-5)", Flags = ConfigFlags.Save)]
    public static ConfigVar QualityLevel;
    
    [ConfigVar(Name = "graphics.vsync", DefaultValue = "1", 
               Description = "Vertical sync", Flags = ConfigFlags.Save)]
    public static ConfigVar VSync;
}

// Graphics service that applies settings
public class GraphicsService : IGraphicsService
{
    private readonly IEventSystem _eventSystem;
    private readonly IConfigService _configService;
    
    public GraphicsService(IEventSystem eventSystem, IConfigService configService)
    {
        _eventSystem = eventSystem;
        _configService = configService;
    }
    
    public async Task InitializeAsync()
    {
        // Subscribe to config changes
        _eventSystem.Subscribe<OptionsChangedEvent>(OnOptionsChanged);
        
        // Apply current settings
        ApplyGraphicsSettings();
    }
    
    private void OnOptionsChanged(OptionsChangedEvent evt)
    {
        ApplyGraphicsSettings();
    }
    
    private void ApplyGraphicsSettings()
    {
        // Resolution
        int width = _configService.GetConfigValue<int>("graphics.resolution_width");
        int height = _configService.GetConfigValue<int>("graphics.resolution_height");
        bool fullscreen = _configService.GetConfigValue<bool>("graphics.fullscreen");
        
        Screen.SetResolution(width, height, fullscreen);
        
        // Quality level
        int quality = _configService.GetConfigValue<int>("graphics.quality_level");
        QualitySettings.SetQualityLevel(quality);
        
        // VSync
        int vsync = _configService.GetConfigValue<int>("graphics.vsync");
        QualitySettings.vSyncCount = vsync;
        
        Debug.Log($"Applied graphics settings: {width}x{height}, " +
                 $"Fullscreen: {fullscreen}, Quality: {quality}, VSync: {vsync}");
    }
    
    public void SetResolution(int width, int height, bool fullscreen)
    {
        _configService.SetConfigValue("graphics.resolution_width", width);
        _configService.SetConfigValue("graphics.resolution_height", height);
        _configService.SetConfigValue("graphics.fullscreen", fullscreen);
        
        ApplyGraphicsSettings();
    }
    
    public void SetQualityLevel(int level)
    {
        _configService.SetConfigValue("graphics.quality_level", level);
        ApplyGraphicsSettings();
    }
}

// Options screen that uses graphics service
public class OptionsScreen : UIScreen
{
    private DropdownField _resolutionDropdown;
    private Toggle _fullscreenToggle;
    private SliderInt _qualitySlider;
    private Toggle _vsyncToggle;
    
    private readonly List<Resolution> _supportedResolutions;
    
    public OptionsScreen(VisualElement rootElement) : base(rootElement)
    {
        _supportedResolutions = Screen.resolutions.ToList();
        InitializeControls();
    }
    
    private void InitializeControls()
    {
        _resolutionDropdown = RootElement?.Q<DropdownField>("ResolutionDropdown");
        _fullscreenToggle = RootElement?.Q<Toggle>("FullscreenToggle");
        _qualitySlider = RootElement?.Q<SliderInt>("QualitySlider");
        _vsyncToggle = RootElement?.Q<Toggle>("VsyncToggle");
        
        // Setup resolution dropdown
        if (_resolutionDropdown != null)
        {
            _resolutionDropdown.choices = _supportedResolutions
                .Select(r => $"{r.width}x{r.height}")
                .ToList();
            _resolutionDropdown.RegisterValueChangedCallback(OnResolutionChanged);
        }
        
        // Setup other controls
        _fullscreenToggle?.RegisterValueChangedCallback(OnFullscreenChanged);
        _qualitySlider?.RegisterValueChangedCallback(OnQualityChanged);
        _vsyncToggle?.RegisterValueChangedCallback(OnVSyncChanged);
    }
    
    protected override void OnShow()
    {
        base.OnShow();
        
        // Load current settings
        LoadCurrentSettings();
    }
    
    private void LoadCurrentSettings()
    {
        var configService = GameManager.GetService<IConfigService>();
        
        // Resolution
        int width = configService.GetConfigValue<int>("graphics.resolution_width");
        int height = configService.GetConfigValue<int>("graphics.resolution_height");
        string resString = $"{width}x{height}";
        _resolutionDropdown.value = resString;
        
        // Other settings
        _fullscreenToggle.value = configService.GetConfigValue<bool>("graphics.fullscreen");
        _qualitySlider.value = configService.GetConfigValue<int>("graphics.quality_level");
        _vsyncToggle.value = configService.GetConfigValue<bool>("graphics.vsync");
    }
    
    private void OnResolutionChanged(ChangeEvent<string> evt)
    {
        var parts = evt.newValue.Split('x');
        if (parts.Length == 2 && 
            int.TryParse(parts[0], out int width) && 
            int.TryParse(parts[1], out int height))
        {
            var graphicsService = GameManager.GetService<IGraphicsService>();
            graphicsService?.SetResolution(width, height, _fullscreenToggle.value);
        }
    }
    
    private void OnFullscreenChanged(ChangeEvent<bool> evt)
    {
        var configService = GameManager.GetService<IConfigService>();
        configService?.SetConfigValue("graphics.fullscreen", evt.newValue);
    }
    
    private void OnQualityChanged(ChangeEvent<int> evt)
    {
        var graphicsService = GameManager.GetService<IGraphicsService>();
        graphicsService?.SetQualityLevel(evt.newValue);
    }
    
    private void OnVSyncChanged(ChangeEvent<bool> evt)
    {
        var configService = GameManager.GetService<IConfigService>();
        configService?.SetConfigValue("graphics.vsync", evt.newValue ? 1 : 0);
    }
}
```

## 🎯 Complete Game Systems

### Simple RPG Combat System

```csharp
// Combat service that integrates with the framework
public class CombatService : ICombatService
{
    private readonly IEventSystem _eventSystem;
    private readonly IAudioService _audioService;
    private readonly IPlayerService _playerService;
    private bool _isInCombat;
    
    public bool IsInCombat => _isInCombat;
    
    public CombatService(
        IEventSystem eventSystem, 
        IAudioService audioService, 
        IPlayerService playerService)
    {
        _eventSystem = eventSystem;
        _audioService = audioService;
        _playerService = playerService;
    }
    
    public async Task InitializeAsync()
    {
        _eventSystem.Subscribe<CombatStartRequestEvent>(OnCombatStartRequest);
        _eventSystem.Subscribe<PlayerAttackEvent>(OnPlayerAttack);
        _eventSystem.Subscribe<EnemyAttackEvent>(OnEnemyAttack);
        _eventSystem.Subscribe<CombatEndRequestEvent>(OnCombatEndRequest);
    }
    
    public void Shutdown()
    {
        _eventSystem.Unsubscribe<CombatStartRequestEvent>(OnCombatStartRequest);
        _eventSystem.Unsubscribe<PlayerAttackEvent>(OnPlayerAttack);
        _eventSystem.Unsubscribe<EnemyAttackEvent>(OnEnemyAttack);
        _eventSystem.Unsubscribe<CombatEndRequestEvent>(OnCombatEndRequest);
    }
    
    private void OnCombatStartRequest(CombatStartRequestEvent evt)
    {
        StartCombat(evt.Enemy);
    }
    
    public void StartCombat(EnemyData enemy)
    {
        if (_isInCombat) return;
        
        _isInCombat = true;
        
        // Play combat music
        _audioService.PlayMusic("combat_theme");
        
        // Publish combat started event
        _eventSystem.Publish(new CombatStartedEvent
        {
            Enemy = enemy,
            PlayerHealth = _playerService.Health,
            EnemyHealth = enemy.MaxHealth
        });
        
        // Transition to combat state
        var stateMachine = GameManager.GetService<IGameStateMachine>();
        stateMachine?.ChangeStateAsync(GameStateType.Combat);
    }
    
    private void OnPlayerAttack(PlayerAttackEvent evt)
    {
        if (!_isInCombat) return;
        
        // Calculate damage
        int damage = CalculatePlayerDamage();
        
        // Apply damage to enemy
        evt.Enemy.CurrentHealth -= damage;
        
        // Play attack sound
        _audioService.PlaySound("player_attack");
        
        // Publish damage event
        _eventSystem.Publish(new EnemyDamagedEvent
        {
            Enemy = evt.Enemy,
            Damage = damage,
            RemainingHealth = evt.Enemy.CurrentHealth
        });
        
        // Check if enemy is defeated
        if (evt.Enemy.CurrentHealth <= 0)
        {
            EndCombat(true, evt.Enemy);
        }
    }
    
    private void OnEnemyAttack(EnemyAttackEvent evt)
    {
        if (!_isInCombat) return;
        
        // Calculate damage
        int damage = CalculateEnemyDamage(evt.Enemy);
        
        // Apply damage to player
        _playerService.TakeDamage(damage);
        
        // Play enemy attack sound
        _audioService.PlaySound("enemy_attack");
        
        // Check if player is defeated
        if (_playerService.Health <= 0)
        {
            EndCombat(false, evt.Enemy);
        }
    }
    
    public void EndCombat(bool playerWon, EnemyData enemy)
    {
        if (!_isInCombat) return;
        
        _isInCombat = false;
        
        if (playerWon)
        {
            // Player victory
            _audioService.PlaySound("victory");
            
            // Give rewards
            _playerService.GainExperience(enemy.ExperienceReward);
            
            var currencyService = GameManager.GetService<ICurrencyService>();
            currencyService?.AddCurrency(enemy.GoldReward);
            
            _eventSystem.Publish(new CombatEndedEvent
            {
                PlayerWon = true,
                Enemy = enemy,
                ExperienceGained = enemy.ExperienceReward,
                GoldGained = enemy.GoldReward
            });
        }
        else
        {
            // Player defeat
            _audioService.PlaySound("defeat");
            
            _eventSystem.Publish(new CombatEndedEvent
            {
                PlayerWon = false,
                Enemy = enemy,
                ExperienceGained = 0,
                GoldGained = 0
            });
            
            // Transition to game over
            var stateMachine = GameManager.GetService<IGameStateMachine>();
            stateMachine?.ChangeStateAsync(GameStateType.GameOver);
            return;
        }
        
        // Return to playing state
        var gameStateMachine = GameManager.GetService<IGameStateMachine>();
        gameStateMachine?.ChangeStateAsync(GameStateType.Playing);
        
        // Resume overworld music
        _audioService.PlayMusic("overworld");
    }
    
    private int CalculatePlayerDamage()
    {
        // Simple damage calculation
        var baseAttack = 10;
        var levelBonus = _playerService.Level * 2;
        var randomFactor = UnityEngine.Random.Range(0.8f, 1.2f);
        
        return Mathf.RoundToInt((baseAttack + levelBonus) * randomFactor);
    }
    
    private int CalculateEnemyDamage(EnemyData enemy)
    {
        var randomFactor = UnityEngine.Random.Range(0.8f, 1.2f);
        return Mathf.RoundToInt(enemy.AttackPower * randomFactor);
    }
}

// Combat state
public class CombatState : BaseGameState
{
    private readonly ICombatService _combatService;
    private readonly IPlayerService _playerService;
    
    public CombatState(
        IGameStateMachine stateMachine,
        IEventSystem eventSystem,
        IAudioService audioService,
        IUIService uiService,
        IInputService inputService,
        ICombatService combatService,
        IPlayerService playerService)
        : base(GameStateType.Combat, stateMachine, eventSystem, audioService, uiService, inputService)
    {
        _combatService = combatService;
        _playerService = playerService;
    }
    
    public override async Task EnterAsync(GameContext context)
    {
        await base.EnterAsync(context);
        
        // Show combat UI
        await UIService.ShowScreenAsync<CombatScreen>();
        
        // Subscribe to combat events
        EventSystem.Subscribe<CombatStartedEvent>(OnCombatStarted);
        EventSystem.Subscribe<EnemyDamagedEvent>(OnEnemyDamaged);
        EventSystem.Subscribe<CombatEndedEvent>(OnCombatEnded);
    }
    
    public override void Update()
    {
        if (!_combatService.IsInCombat) return;
        
        // Handle combat input
        if (InputService.GetKeyDown("Space") || InputService.GetKeyDown("Attack"))
        {
            // Player attacks
            EventSystem.Publish(new PlayerAttackEvent());
        }
        
        if (InputService.GetKeyDown("Escape"))
        {
            // Try to flee
            EventSystem.Publish(new CombatFleeAttemptEvent());
        }
    }
    
    public override async Task ExitAsync()
    {
        // Unsubscribe from events
        EventSystem.Unsubscribe<CombatStartedEvent>(OnCombatStarted);
        EventSystem.Unsubscribe<EnemyDamagedEvent>(OnEnemyDamaged);
        EventSystem.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
        
        // Hide combat UI
        await UIService.HideScreenAsync<CombatScreen>();
        
        await base.ExitAsync();
    }
    
    private void OnCombatStarted(CombatStartedEvent evt)
    {
        var combatScreen = UIService.GetScreen<CombatScreen>();
        combatScreen?.SetupCombat(evt.Enemy, _playerService.Health);
    }
    
    private void OnEnemyDamaged(EnemyDamagedEvent evt)
    {
        var combatScreen = UIService.GetScreen<CombatScreen>();
        combatScreen?.UpdateEnemyHealth(evt.RemainingHealth, evt.Enemy.MaxHealth);
        combatScreen?.ShowDamageNumber(evt.Damage);
    }
    
    private void OnCombatEnded(CombatEndedEvent evt)
    {
        var combatScreen = UIService.GetScreen<CombatScreen>();
        combatScreen?.ShowCombatResult(evt.PlayerWon, evt.ExperienceGained, evt.GoldGained);
    }
}
```

This completes the comprehensive examples section. Each example shows:

- **Real-world usage patterns**
- **Proper dependency injection**
- **Event-driven communication**  
- **UI integration**
- **Configuration management**
- **Service interaction**

The examples progress from simple to complex, showing how to build complete game systems using the framework's architecture.

---

**Next**: Check the remaining guides for specialized topics!
```