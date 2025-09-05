# Game Data Management System Guide

## Table of Contents
1. [Overview](#overview)
2. [System Architecture](#system-architecture)
3. [Data Flow Diagrams](#data-flow-diagrams)
4. [Core Components](#core-components)
5. [Data Storage Structure](#data-storage-structure)
6. [Accessing Game Data](#accessing-game-data)
7. [Save/Load Process](#saveload-process)
8. [Adding New Data Types](#adding-new-data-types)
9. [Glossary](#glossary)
10. [Best Practices](#best-practices)

## Overview

This system provides a unified approach to managing all game state data in Unity. Instead of scattered data storage across multiple systems, everything is centralized in a **GameSession** object that serves as the single source of truth for your game's state.

### Key Benefits
- **Single Source of Truth**: All game data lives in one place
- **Automatic Save/Load**: Seamless persistence without manual data mapping
- **Type Safety**: Strongly typed data structures prevent runtime errors
- **Extensible**: Easy to add new data types without breaking existing systems
- **Clean Architecture**: Clear separation between data management and business logic

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Game States                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │ MainMenu    │  │ NewGame     │  │ Loading     │        │
│  │ State       │  │ State       │  │ State       │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                GameDataService                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │              GameSession                            │    │
│  │  ┌─────────────┐ ┌─────────────┐ ┌──────────────┐  │    │
│  │  │ PlayerState │ │GameProgress │ │ CustomData   │  │    │
│  │  │             │ │             │ │ Dictionary   │  │    │
│  │  └─────────────┘ └─────────────┘ └──────────────┘  │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                  SaveService                                │
│              (File System I/O)                             │
└─────────────────────────────────────────────────────────────┘
```

## Data Flow Diagrams

### New Game Creation Flow
```
User Clicks "New Game" → NewGameState → LoadingState → GameDataService
                                           │
                                           ▼
                        Creates GameSession with default values
                                           │
                                           ▼
                              Game systems access session data
```

### Save Game Flow
```
Game Requests Save → GameDataService → SaveService → File System
     │                     │              │
     │                     │              ▼
     │                     │        *.gamesave file
     │                     │
     │                     ▼
     │            Updates session timestamps
     │
     ▼
Auto-save every 5 minutes
```

### Load Game Flow
```
User Selects Save → MainMenuState → GameDataService → SaveService
                         │               │              │
                         │               │              ▼
                         │               │        Read *.gamesave
                         │               │
                         │               ▼
                         │        Creates GameSession from file
                         │
                         ▼
                   LoadingState applies session data
```

## Core Components

### GameSession
The central data container that holds all game state information.

**Contains:**
- Basic session info (player name, difficulty, timestamps)
- PlayerState (health, level, position, inventory)
- GameProgress (score, completed levels, achievements)
- CustomData dictionary for game-specific data

### GameDataService
The service that manages the active GameSession and provides access methods.

**Responsibilities:**
- Creating new game sessions
- Loading existing sessions from saves
- Providing data access methods
- Coordinating with SaveService
- Managing session lifecycle

### SaveService
Handles file I/O operations for persisting GameSession data.

**Responsibilities:**
- Serializing GameSession to JSON
- Writing/reading save files
- Managing save file listing
- File cleanup operations

## Data Storage Structure

### In Memory (GameSession)
```
GameSession
├── playerName: string
├── difficulty: string  
├── currentScene: string
├── sessionStartTime: DateTime
├── totalPlayTimeSeconds: float
├── PlayerState
│   ├── level: int
│   ├── health: int
│   ├── position: Vector3
│   ├── inventory: Dictionary<string, int>
│   └── unlockedAbilities: List<string>
├── GameProgress
│   ├── score: int
│   ├── completedLevels: List<string>
│   ├── unlockedLevels: List<string>
│   └── achievements: Dictionary<string, bool>
└── customData: Dictionary<string, object>
```

### On Disk (JSON File)
Save files are stored as JSON in: `Application.persistentDataPath/Saves/`
- Filename format: `PlayerName_SaveType_YYYY-MM-DD_HH-mm-ss.gamesave`
- Content: Serialized GameSession object
- Automatic compression and validation

## Accessing Game Data

### From Any Game System
```csharp
// Get the service (usually via dependency injection)
var gameData = GameManager.GetService<IGameDataService>();

// Check if there's an active session
if (gameData.HasActiveSession())
{
    // Access player data
    var player = gameData.GetPlayerState();
    var currentLevel = player.level;
    
    // Access progress data
    var progress = gameData.GetGameProgress();
    var currentScore = progress.score;
    
    // Access custom data
    var questCompleted = gameData.GetCustomData<bool>("quest1_completed");
}
```

### Data Access Patterns
| Pattern | Use Case | Example |
|---------|----------|---------|
| Direct Property Access | Frequently used data | `player.health`, `progress.score` |
| Custom Data Dictionary | Game-specific flags | `GetCustomData<bool>("tutorial_complete")` |
| Structured Data | Complex nested data | `player.inventory["sword"]` |

## Save/Load Process

### Automatic Operations
- **Auto-save**: Every 5 minutes during gameplay
- **Scene transitions**: Session updated with new scene name
- **Data synchronization**: Real-time updates to session

### Manual Operations
- **Quick Save**: `gameData.SaveCurrentSessionAsync("QuickSave")`
- **Load Game**: `gameData.LoadSessionAsync("PlayerSave_2024-01-15")`
- **Session Management**: Create, load, clear sessions via GameDataService

### Save File Management
```
Saves/
├── Player_AutoSave_2024-01-15_14-30-00.gamesave
├── Player_QuickSave_2024-01-15_15-45-00.gamesave
└── John_ManualSave_2024-01-15_16-00-00.gamesave
```

## Adding New Data Types

### Step 1: Determine Data Category
Choose the appropriate container:
- **PlayerState**: Character-specific data (stats, inventory, abilities)
- **GameProgress**: Meta-progression (achievements, unlocks, scores)
- **CustomData**: Game-specific flags and temporary data

### Step 2: Add to Data Structure
For structured data, modify the appropriate class:

```csharp
// Add to PlayerState for character data
public class PlayerState
{
    // Existing fields...
    public List<string> collectedItems = new List<string>(); // NEW
    public Dictionary<string, float> skillLevels = new Dictionary<string, float>(); // NEW
}
```

### Step 3: Create Helper Methods (Optional)
Add convenience methods to GameDataService:

```csharp
public bool HasCollectedItem(string itemId)
{
    return GetPlayerState().collectedItems.Contains(itemId);
}

public void CollectItem(string itemId)
{
    GetPlayerState().collectedItems.Add(itemId);
}
```

### Step 4: Use Custom Data for Simple Values
For simple flags or temporary data:

```csharp
// Setting data
gameData.SetCustomData("boss_defeated", true);
gameData.SetCustomData("current_weapon", "sword_of_fire");

// Reading data  
bool bossDefeated = gameData.GetCustomData<bool>("boss_defeated");
string currentWeapon = gameData.GetCustomData<string>("current_weapon", "basic_sword");
```

## Glossary

| Term | Definition |
|------|------------|
| **GameSession** | The root data container holding all game state for a single playthrough |
| **PlayerState** | Character-specific data like health, level, inventory, and abilities |
| **GameProgress** | Meta-progression data like achievements, unlocked content, and scores |
| **CustomData** | Flexible key-value storage for game-specific data and flags |
| **GameDataService** | The service managing the active GameSession and providing data access |
| **SaveService** | File I/O service handling serialization and persistence of GameSession |
| **LoadingConfiguration** | Temporary data structure used during state transitions |
| **Session Lifecycle** | The process of creating, managing, and destroying GameSession objects |
| **Auto-save** | Automatic periodic saving of the current session (every 5 minutes) |
| **Single Source of Truth** | Design principle where all game data comes from one authoritative source |

## Best Practices

### Data Organization
- **Use PlayerState** for data that belongs to the character/player
- **Use GameProgress** for meta-game progression and achievements
- **Use CustomData** for temporary flags, settings, and game-specific data
- **Avoid deep nesting** in data structures for better serialization

### Performance Considerations
- **Cache frequently accessed data** locally in performance-critical systems
- **Update session periodically** rather than every frame
- **Use events** to notify systems of important data changes
- **Batch related updates** together when possible

### Error Handling
- **Always check** `HasActiveSession()` before accessing session data
- **Use default values** in GetCustomData calls
- **Handle save/load failures** gracefully with user feedback
- **Validate data** after loading from disk

### Testing
- **Mock GameDataService** in unit tests using the interface
- **Test with invalid save files** to ensure robust error handling
- **Verify auto-save behavior** doesn't impact performance
- **Test data persistence** across game sessions

This system provides a robust, scalable foundation for managing game data while maintaining clean architecture and excellent developer experience.