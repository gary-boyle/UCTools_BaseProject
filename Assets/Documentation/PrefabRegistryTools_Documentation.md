# PrefabRegistry Management Tools Documentation

## Overview

The PrefabRegistry Management Tools provide a comprehensive suite of editor utilities for automatically managing the PrefabRegistry used by the Save/Load system. These tools eliminate the manual work of registering SaveableBase prefabs and ensure your registry stays synchronized with your project.

## Tools Overview

### 🔧 **Menu Tools** (`UCTools/Game Framework/`)

1. **Auto-Populate PrefabRegistry** - Automatically finds and registers all SaveableBase prefabs
2. **Validate PrefabRegistry** - Checks registry integrity and reports issues  
3. **Clear PrefabRegistry** - Removes all entries from the registry
4. **Show PrefabRegistry Info** - Displays detailed registry statistics
5. **Select PrefabRegistry** - Opens the registry asset in Inspector
6. **Open PrefabRegistry Window** - Opens the visual management window

### 🖼️ **Visual Window** (`PrefabRegistryWindow`)

A comprehensive GUI interface for managing the PrefabRegistry with:
- Visual prefab browser with search and filtering
- One-click registration/deregistration
- Invalid entry detection and cleanup
- Unregistered prefab discovery

### 🔍 **Automatic Validation** (`PrefabRegistryValidator`)

Background validation that automatically:
- Monitors prefab changes in your project
- Removes invalid registry entries
- Warns about unregistered SaveableBase prefabs
- Keeps the registry synchronized

---

## Quick Start Guide

### 1. Initial Setup

When you first use the save system, create and populate your PrefabRegistry:

1. Go to `UCTools/Game Framework/Auto-Populate PrefabRegistry`
2. Click to automatically find and register all SaveableBase prefabs
3. Review the results dialog to see what was registered

### 2. Daily Workflow

The tools integrate seamlessly into your workflow:

- **Adding New Prefabs**: The system automatically detects new SaveableBase prefabs and suggests registration
- **Removing Prefabs**: Invalid entries are automatically cleaned up when prefabs are deleted
- **Validation**: Run validation periodically to ensure registry integrity

### 3. Visual Management

For hands-on management, use the PrefabRegistry Window:

1. Open `UCTools/Game Framework/Open PrefabRegistry Window`  
2. Browse registered/unregistered prefabs
3. Use search and filters to find specific prefabs
4. Click buttons to register/unregister prefabs individually

---

## Tool Details

### Auto-Populate PrefabRegistry

**Purpose**: Scans your entire project for prefabs containing SaveableBase components and automatically registers them.

**Features**:
- Progress bar for large projects
- Detailed results reporting
- Skips already-registered prefabs
- Error handling for problematic prefabs

**When to Use**:
- Initial setup of PrefabRegistry
- After adding multiple new SaveableBase prefabs
- When you suspect prefabs are missing from the registry

### Validate PrefabRegistry

**Purpose**: Checks the integrity of your PrefabRegistry and reports issues.

**What it Checks**:
- Missing prefab references
- Prefabs without SaveableBase components  
- GUID mismatches
- Registry consistency

**Output**: Detailed report with issue counts and descriptions

### PrefabRegistry Window

**Purpose**: Provides a visual interface for managing prefabs.

**Key Features**:

#### Registry View
- Lists all registered prefabs with status indicators
- Shows prefab names, types, and GUIDs
- Color-coded validity indicators
- One-click selection and removal

#### Unregistered View  
- Shows SaveableBase prefabs not yet in registry
- One-click registration
- Type and path information

#### Search & Filtering
- Text search by name, type, or GUID
- Filter to show only invalid entries
- Filter to show only unregistered prefabs

#### Actions
- **Select**: Highlights prefab in Project window
- **Register**: Adds prefab to registry
- **Remove**: Removes prefab from registry
- **Auto-Populate**: Runs automatic population
- **Validate**: Runs validation check

### Automatic Validator

**Purpose**: Runs automatically in the background to keep your registry synchronized.

**Triggers**:
- Prefab assets imported/deleted/moved
- PrefabRegistry asset modified
- Project asset database refreshed

**Actions**:
- Removes entries for deleted prefabs
- Removes entries for prefabs that lost SaveableBase components
- Warns about new unregistered SaveableBase prefabs
- Logs cleanup actions

---

## Best Practices

### 1. Regular Maintenance

- Run **Auto-Populate** after adding new SaveableBase prefabs
- Run **Validate** periodically to catch issues
- Use the **Window** for detailed management when needed

### 2. Team Workflows

- Include PrefabRegistry.asset in version control
- Run Auto-Populate after pulling changes
- Validate after large refactoring operations

### 3. Performance Tips

- The automatic validator is lightweight and runs only when needed
- Large projects may take a few seconds for Auto-Populate
- Use search/filters in the window for large registries

### 4. Troubleshooting

**"No SaveableBase component found"**
- Prefab lost its SaveableBase component
- Check if component was accidentally removed
- Re-add component or remove from registry

**"GUID mismatch"**
- Prefab was duplicated or moved in a way that changed its GUID
- Usually harmless but indicates the prefab reference changed
- Remove and re-register if problematic

**"Missing prefab reference"**
- Prefab was deleted from project
- Entry will be automatically cleaned up
- Check if prefab was moved instead of deleted

---

## Integration with Save/Load System

### Automatic Discovery

The tools work seamlessly with your SaveableBase prefabs:

```csharp
[SaveableType(typeof(MyObjectRuntimeSaveData))]
public class MyObject : SaveableBase
{
    // Your implementation
}
```

When you create a prefab with this component:
1. **Automatic Validator** detects the new prefab
2. **Auto-Populate** can register it
3. **Window** shows it in the unregistered list
4. **Save/Load System** can now instantiate it

### Registry Structure

Each registry entry contains:
- **GUID**: Unity's asset GUID for the prefab
- **Prefab Reference**: Direct reference to the prefab asset
- **Name**: Display name for editor tools

### Runtime Usage

The Save/Load system uses the registry like this:
```csharp
// During saving: SaveableBase records its prefab GUID
var saveData = new MyObjectRuntimeSaveData(UniqueID, PrefabGUID);

// During loading: RuntimeObjectInstantiator looks up prefab by GUID
GameObject prefab = _prefabRegistry.GetPrefab(saveData.prefabGUID);
GameObject instance = Instantiate(prefab);
```

---

## Error Reference

### Common Warnings

**"Found X unregistered SaveableBase prefabs"**
- New prefabs detected that aren't in registry
- **Solution**: Run Auto-Populate or register manually

**"Cleaned up X invalid entries"**  
- Registry had entries for deleted/invalid prefabs
- **Solution**: No action needed, automatic cleanup performed

### Common Errors

**"Could not find or create PrefabRegistry"**
- Registry asset missing or corrupt
- **Solution**: Check Resources folder, or create new registry

**"Failed to register prefab"**
- Registration failed due to conflicts or errors
- **Solution**: Check console for specific error details

---

## Conclusion

The PrefabRegistry Management Tools provide a complete solution for managing your Save/Load system's prefab registry. They eliminate manual work, prevent errors, and keep your registry synchronized with your project automatically.

**Key Benefits**:
- **Zero Manual Work**: Automatic discovery and registration
- **Error Prevention**: Validation catches issues before runtime
- **Team Friendly**: Keeps registries synchronized across team members
- **Visual Management**: Easy-to-use interface for manual management
- **Background Monitoring**: Automatic maintenance as you work

Use these tools to focus on building your game instead of managing save system configuration!
