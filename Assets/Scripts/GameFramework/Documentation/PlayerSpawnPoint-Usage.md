# PlayerSpawnPoint Usage Guide

## Overview

The `PlayerSpawnPoint` component is used to mark specific positions in your game scenes where the player should spawn during **new games**. When loading saved games, the player will spawn at their saved position instead.

## How It Works

### New Games
- The `InstantiationService` searches the scene for `PlayerSpawnPoint` components
- Uses the position and rotation of the spawn point to instantiate the player
- Falls back to default spawn settings if no spawn point is found

### Loaded Games
- Uses the saved position and rotation from the save file
- Ignores `PlayerSpawnPoint` components completely

## Setup Instructions

### 1. Add PlayerSpawnPoint to Scene

1. Create an empty GameObject in your scene (or use an existing one)
2. Add the `PlayerSpawnPoint` component
3. Position and rotate the GameObject where you want the player to spawn
4. Configure the component settings in the inspector

### 2. Configure PlayerSpawnPoint

**Inspector Properties:**
- **Spawn Point Name**: Descriptive name for this spawn point
- **Is Active**: Whether this spawn point should be used
- **Gizmo Color**: Color of the visual gizmo in the editor
- **Gizmo Size**: Size of the spawn point gizmo
- **Show Direction Arrow**: Whether to show which direction the player will face
- **Arrow Length**: Length of the direction arrow

### 3. Scene Validation

**Important Rules:**
- ⚠️ **Only one active PlayerSpawnPoint per scene**
- Multiple inactive spawn points are allowed
- If multiple active spawn points exist, an error is logged and the first one is used

### 4. Visual Editor Feedback

The component provides visual feedback in the editor:
- **Green sphere**: Shows spawn position
- **Blue arrow**: Shows spawn direction (player facing)
- **Yellow wireframe**: When selected
- **RGB axes**: When selected (Red=Right, Green=Up, Blue=Forward)

## Example Setup

```csharp
// In your scene setup script (optional)
public class SceneSetup : MonoBehaviour 
{
    [SerializeField] private PlayerSpawnPoint spawnPoint;
    
    void Start() 
    {
        // Configure spawn point programmatically if needed
        spawnPoint.SetSpawnPointName("Main Entrance");
        spawnPoint.SetActive(true);
    }
}
```

## Best Practices

### Scene Organization
- Name your spawn point GameObjects clearly: "PlayerSpawn_MainEntrance", "PlayerSpawn_SecretPath", etc.
- Use only one active spawn point per scene
- Keep inactive spawn points for different game modes or story branches

### Multi-Scene Games
- Each scene should have its own `PlayerSpawnPoint`
- Use descriptive names to identify spawn points across different scenes
- Consider the narrative flow when placing spawn points

### Testing
- Use the editor gizmos to verify spawn position and rotation
- Test both new game and save game loading in your scenes
- Check the console for any multiple spawn point warnings

## Troubleshooting

### Common Issues

**Player not spawning at spawn point:**
- Check that `PlayerSpawnPoint` component is active
- Verify it's a **new game** (loaded games use saved position)
- Check console for warnings about multiple spawn points

**Multiple spawn point error:**
- Only one `PlayerSpawnPoint` should be active per scene
- Disable extra spawn points by unchecking "Is Active"
- Use GameObject.SetActive(false) to completely disable unused spawn points

**Spawn point not visible in editor:**
- Check "Show Direction Arrow" is enabled
- Adjust "Gizmo Size" to make it more visible
- Select the GameObject to see the enhanced selection gizmo

### Debug Information

The system logs helpful information to the console:
- `[InstantiationService] Found PlayerSpawnPoint: ...` - Successful spawn point detection
- `[InstantiationService] No PlayerSpawnPoint found in scene...` - Fallback to default position
- `[InstantiationService] Multiple active PlayerSpawnPoint components found...` - Error condition

## Integration with Game Flow

```
New Game Flow:
User clicks "New Game" → BeginNewGameLoadEvent → LoadingState → 
LoadingCompletedEvent → InstantiationService finds PlayerSpawnPoint → 
Player instantiated at spawn point position

Loaded Game Flow:
User clicks "Load Game" → LoadSaveFileEvent → LoadingState → 
LoadingCompletedEvent → InstantiationService uses saved position → 
Player instantiated at saved position (ignores PlayerSpawnPoint)
```
