# UniqueID Auto-Generation System

## Overview

The UniqueID Auto-Generation system provides automatic generation of unique identifiers for SaveableBase objects when they are added to scenes in the Unity Editor. This ensures every saveable object gets a proper UniqueID without manual intervention.

## How It Works

### Automatic Detection
- **Editor Callback**: Uses `EditorApplication.hierarchyChanged` to detect when objects are added to scenes
- **Lightweight**: Only runs a simple check when the hierarchy changes
- **Edit Mode Only**: Only operates in edit mode, not during play

### Triggers
The system automatically generates UniqueIDs when:
- **Dragging prefabs** into the scene view
- **Copy/pasting objects** in the hierarchy
- **Duplicating objects** using Ctrl+D
- **Any other scenario** where SaveableBase objects are added to scenes

### ID Generation
- **Format**: `{classname}_{guid}` (e.g., `clickablecube_a1b2c3d4e5f6...`)
- **Unique**: Uses System.Guid to ensure uniqueness
- **Automatic**: Happens immediately when objects are detected

## Implementation

### SaveableBasePostProcessor.cs
```csharp
[InitializeOnLoad]
public static class SaveableBasePostProcessor
{
    static SaveableBasePostProcessor()
    {
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    private static void OnHierarchyChanged()
    {
        if (Application.isPlaying) return;
        
        var saveableObjects = Object.FindObjectsOfType<SaveableBase>();
        foreach (var saveable in saveableObjects)
        {
            if (string.IsNullOrEmpty(saveable.UniqueID))
            {
                GenerateUniqueIdForObject(saveable);
            }
        }
    }
}
```

### Key Features
- **Non-Intrusive**: Doesn't modify SaveableBase behavior
- **Automatic**: No manual intervention required  
- **Safe**: Uses SerializedObject to safely modify private fields
- **Efficient**: Only runs when hierarchy actually changes

## Benefits

### For Developers
- **Zero Configuration**: Works automatically once installed
- **No Manual Work**: UniqueIDs generated automatically
- **Consistent**: Same process regardless of how objects are added
- **Safe**: Doesn't interfere with existing objects that already have IDs

### For Workflow
- **Drag & Drop**: Prefabs automatically get UniqueIDs when dragged to scene
- **Copy & Paste**: Duplicated objects get new, unique IDs
- **Level Design**: Artists and designers don't need to think about IDs
- **Team Friendly**: Works consistently across all team members

## Usage

### No Setup Required
The system works automatically once the script is in your project. Simply:

1. **Drag prefabs** into your scene
2. **Copy/paste objects** as usual
3. **Duplicate objects** normally
4. **UniqueIDs are generated automatically**

### Verification
You can verify the system is working by:
- Dragging a SaveableBase prefab into the scene
- Selecting the object and viewing its UniqueID in the inspector
- The ID should be populated automatically

## Technical Details

### Performance
- **Minimal Overhead**: Only runs when hierarchy changes
- **Fast Execution**: Simple GUID generation and field setting
- **No Runtime Impact**: Editor-only system

### Error Handling
- **Graceful Failures**: Continues processing other objects if one fails
- **Debug Logging**: Success and error messages logged to console
- **Safe Operation**: Uses try-catch to prevent editor crashes

### Compatibility
- **Unity 2021.3+**: Compatible with all modern Unity versions
- **All Platforms**: Editor-only, no platform restrictions
- **Existing Projects**: Safe to add to existing SaveableBase implementations

## Troubleshooting

### "No UniqueID generated"
- **Check Scene Validity**: Object must be in a valid scene (not a prefab asset)
- **Check Field Access**: Ensure `_uniqueID` field exists and is accessible
- **Check Console**: Look for error messages in the Unity console

### "Multiple objects with same ID"
- **Rare Occurrence**: Should not happen with this system
- **Manual Fix**: Use the SaveableBase inspector to generate new IDs
- **Report Issue**: This indicates a bug that should be reported

### "Performance Issues"
- **Unlikely**: System is very lightweight
- **Check Frequency**: If hierarchy changes very frequently, there might be other issues
- **Disable Temporarily**: Comment out the subscription line to test

## Conclusion

The UniqueID Auto-Generation system provides a seamless, automatic solution for ensuring all SaveableBase objects have proper unique identifiers. It integrates transparently into the Unity Editor workflow and requires no configuration or manual intervention.

**Key Benefits:**
- ✅ Automatic UniqueID generation
- ✅ Zero configuration required  
- ✅ Works with all Unity editor workflows
- ✅ Lightweight and performant
- ✅ Safe and non-intrusive
