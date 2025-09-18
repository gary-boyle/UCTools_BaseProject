# SaveableBase Usage Guide

The `SaveableBase` class is an abstract MonoBehaviour that implements the `ISaveable` interface and provides common functionality for objects that need to be saved and loaded. It handles automatic save system registration, UniqueID management, and provides extension points for custom behavior.

## Basic Usage

### 1. Inherit from SaveableBase

```csharp
using GameFramework.SaveSystem;

public class MyCustomSaveable : SaveableBase
{
    [SerializeField] private int _myValue;
    [SerializeField] private string _myString;
    
    // Required: Implement GetSaveData
    public override object GetSaveData()
    {
        return new MyCustomSaveData
        {
            uniqueID = UniqueID,
            myValue = _myValue,
            myString = _myString
        };
    }
    
    // Required: Implement LoadSaveData
    public override void LoadSaveData(object data)
    {
        if (data is MyCustomSaveData saveData)
        {
            SetUniqueID(saveData.uniqueID);
            _myValue = saveData.myValue;
            _myString = saveData.myString;
        }
    }
}

[System.Serializable]
public class MyCustomSaveData
{
    public string uniqueID;
    public int myValue;
    public string myString;
}
```

### 2. Attach to GameObject

- Add your custom component to any GameObject
- The component will automatically:
  - Generate a unique ID **at runtime** when instantiated in a scene (not in prefab assets)
  - Register with the save system when the scene starts (if UniqueID exists)
  - Unregister when the object is destroyed

**Important:** UniqueIDs are only generated for actual runtime instances, not prefab assets. This prevents prefab variants from sharing the same ID.

## Extension Points

SaveableBase provides several virtual methods you can override for custom behavior:

### Lifecycle Extension Points

```csharp
protected override void OnAwakeCustom()
{
    // Custom logic during Awake, after UniqueID generation
    Debug.Log("Custom Awake logic");
}

protected override void OnStartCustom()
{
    // Custom logic during Start, before save system registration
    InitializeCustomProperties();
}

protected override void OnDestroyCustom()
{
    // Custom cleanup logic during OnDestroy
    CleanupResources();
}
```

### Save/Load Extension Points

```csharp
protected override void OnBeforeSave()
{
    // Logic before saving (e.g., update cached values)
    UpdateCachedPosition();
}

protected override void OnAfterLoad()
{
    // Logic after loading (e.g., apply loaded state)
    ApplyLoadedState();
    UpdateVisuals();
}

protected override void OnSaveError(System.Exception exception)
{
    // Custom save error handling
    Debug.LogError($"Custom save error: {exception.Message}");
    
    // Call base for standard logging
    base.OnSaveError(exception);
}

protected override void OnLoadError(System.Exception exception)
{
    // Custom load error handling
    ResetToDefaults();
    
    // Call base for standard logging
    base.OnLoadError(exception);
}
```

### Save System Integration Extension Points

```csharp
protected override void OnSaveSystemRegistered()
{
    // Called when successfully registered with save system
    Debug.Log("Successfully registered with save system");
}

protected override void OnSaveSystemRegistrationFailed()
{
    // Called when registration fails
    Debug.LogError("Failed to register with save system");
}

protected override void OnSaveSystemUnregistered()
{
    // Called when unregistered from save system
    Debug.Log("Unregistered from save system");
}
```

## Customizing UniqueID Generation

```csharp
protected override string GetUniqueIdPrefix()
{
    // Return custom prefix for UniqueID generation
    return "weapon"; // Results in IDs like "weapon_1234567890_5678"
}
```

## Public API

### Properties
- `string UniqueID { get; }` - The unique identifier for this object
- `string SaveKey { get; }` - Key used in save file (can be overridden)
- `string TypeName { get; }` - Type name for deserialization (can be overridden)
- `bool IsRegisteredWithSaveSystem { get; }` - Registration status

### Methods
- `void SetUniqueID(string uniqueId)` - Manually set UniqueID (use carefully!)
- `Task ForceReregisterWithSaveSystem()` - Force re-registration with save system

## Advanced Usage

### Custom SaveKey

```csharp
public override string SaveKey => $"CustomPrefix_{UniqueID}_{SomeOtherIdentifier}";
```

### Custom TypeName

```csharp
public override string TypeName => "MyCustomType";
```

### Handling Complex Save Data

```csharp
public override void LoadSaveData(object data)
{
    SaveableExampleData saveData;
    
    if (data is SaveableExampleData directData)
    {
        saveData = directData;
    }
    else
    {
        // Handle JSON conversion
        try
        {
            var json = JsonUtility.ToJson(data);
            saveData = JsonUtility.FromJson<SaveableExampleData>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to deserialize: {ex.Message}");
            return;
        }
    }
    
    // Apply loaded data
    ApplyData(saveData);
}
```

## Prefab Considerations

### ⚠️ UniqueID Generation Timing
UniqueIDs are **only generated at runtime** when objects are instantiated in scenes. This design prevents several common issues:

**✅ What Works:**
- Prefab instances in scenes get unique IDs when the scene loads
- Instantiated prefabs at runtime get unique IDs when created
- Prefab variants each get their own unique IDs

**❌ What Doesn't Work:**
- Prefab assets don't have UniqueIDs (by design)
- Objects that are never instantiated won't get IDs

### 🔧 Working with Prefabs
```csharp
// ✅ Good: Runtime instantiation
var instance = Instantiate(myPrefab);
// UniqueID is generated automatically

// ✅ Good: Scene objects
// Objects placed directly in scenes get IDs when scene loads

// ⚠️ Note: Prefab assets have empty UniqueIDs until instantiated
```

### 🛠️ Editor Testing
For editor testing only, you can manually generate a UniqueID:
```csharp
#if UNITY_EDITOR
myComponent.EditorGenerateUniqueID(); // For testing only!
#endif
```

## Best Practices

1. **Always create separate save data classes** - Don't save the MonoBehaviour directly
2. **Handle null data gracefully** - The LoadSaveData method should be defensive
3. **Include UniqueID in save data** - This helps with debugging and data integrity
4. **Use SerializeField for private fields** - Ensures they're saved in prefabs/scenes
5. **Call base methods when overriding** - Unless you specifically don't want the base behavior
6. **Test save/load extensively** - Different data types and edge cases
7. **Use try-catch for complex loading** - Prevent save data corruption from breaking the game
8. **Don't rely on UniqueIDs in prefab assets** - They're generated at runtime only

## Save System Architecture

### How SaveFileData Handles Different Object Types

The save system uses a **hybrid approach** to store different types of saveable data:

#### **Static Fields (Core Game Data)**
- Fixed SaveKeys like `"GameSessionData"`, `"PlayerData"`  
- Stored as direct fields in `SaveFileData`
- Use reflection for field assignment

#### **Dynamic Objects (SaveableBase Instances)**
- Generated SaveKeys like `"ClickableCube_cube_1234567890_1234"`
- Stored in `SaveFileData.DynamicSaveableObjects[]` array
- Converted to `SavedObjectEntry` with Key-Value pairs

```csharp
// Save file structure
{
  "SaveTimeTicks": 638012345678901234,
  "GameSessionData": { "uniqueID": "session_123", ... },
  "PlayerData": { "uniqueID": "player_456", ... },
  "DynamicSaveableObjects": [
    {
      "Key": "ClickableCube_cube_1234567890_1234",
      "Value": { "typeName": "ClickableCubeSaveData", "dataJson": "{...}" }
    },
    {
      "Key": "SaveableExample_example_1234567890_5678", 
      "Value": { "typeName": "SaveableExampleData", "dataJson": "{...}" }
    }
  ]
}
```

This design allows unlimited dynamic saveable objects while maintaining clean structure for core game data.

## Example Implementation

See `SaveableExample.cs` in the Examples folder for a complete implementation example that demonstrates all the extension points and best practices.
