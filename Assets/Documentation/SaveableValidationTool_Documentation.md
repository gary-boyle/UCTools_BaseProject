# Saveable Validation Tool Documentation

## Overview

The Saveable Validation Tool is a comprehensive Unity Editor utility for checking and fixing UniqueID issues in SaveableBase objects within your scenes. It helps ensure your save/load system will work correctly by identifying and resolving duplicate or missing UniqueIDs.

## Menu Location

All tools are located under: **`UCTools/Game Framework/Saveable/`**

## Available Tools

### 🔍 **Validate Scene UniqueIDs**
**Path**: `UCTools/Game Framework/Saveable/Validate Scene UniqueIDs`

- **Purpose**: Validates all SaveableBase objects in the currently active scene
- **Checks**: Empty UniqueIDs and duplicate UniqueIDs
- **Scope**: Current active scene only

### 🔍 **Validate All Loaded Scenes**
**Path**: `UCTools/Game Framework/Saveable/Validate All Loaded Scenes`

- **Purpose**: Validates all SaveableBase objects across all currently loaded scenes
- **Checks**: Empty UniqueIDs and duplicate UniqueIDs
- **Scope**: All loaded scenes (useful for multi-scene setups)

### 📊 **Show Scene Saveable Info**
**Path**: `UCTools/Game Framework/Saveable/Show Scene Saveable Info`

- **Purpose**: Displays detailed information about SaveableBase objects in the current scene
- **Shows**: Object counts by type, total objects, and ID status
- **Scope**: Current active scene only

---

## Features

### 🔍 **Comprehensive Validation**

#### **Empty UniqueID Detection**
- Finds objects with missing or empty UniqueIDs
- Lists affected objects with names and types
- Provides automatic fixing capability

#### **Duplicate UniqueID Detection**
- Identifies groups of objects sharing the same UniqueID
- Shows all objects in each duplicate group
- Fixes duplicates by generating new IDs (keeps first object's ID)

#### **Cross-Scene Validation**
- Can validate multiple scenes simultaneously
- Useful for additive scene loading scenarios
- Comprehensive project-wide validation

### 🛠️ **Automatic Fixing**

#### **One-Click Fixes**
- Automatically generates new UniqueIDs for problematic objects
- Uses proper naming convention: `{classname}_{guid}`
- Safe SerializedObject-based field modification

#### **Smart Duplicate Resolution**
- Preserves the first object's UniqueID in duplicate groups
- Generates new IDs only for subsequent duplicates
- Maintains referential integrity where possible

### 📊 **Detailed Reporting**

#### **Visual Dialogs**
- Clear, user-friendly validation results
- Progress bars during fixing operations
- Success/failure confirmations

#### **Console Logging**
- Detailed logs with object references for easy location
- Clickable references to problematic objects
- Comprehensive validation summaries

---

## Usage Guide

### **Basic Validation Workflow**

#### 1. **Quick Scene Check**
```
1. Open your scene
2. Go to UCTools/Game Framework/Saveable/Validate Scene UniqueIDs
3. Review the results dialog
4. Click "Fix Issues" if problems are found
```

#### 2. **Multi-Scene Validation**
```
1. Load all scenes you want to validate (additively)
2. Go to UCTools/Game Framework/Saveable/Validate All Loaded Scenes  
3. Review cross-scene validation results
4. Fix any issues found
```

#### 3. **Information Gathering**
```
1. Go to UCTools/Game Framework/Saveable/Show Scene Saveable Info
2. Review object counts and types
3. Identify areas with many SaveableBase objects
```

### **Typical Results Dialog**

```
UniqueID Validation Results

Context: MyGameScene
Total SaveableBase Objects: 45
Valid Objects: 42
Objects with Empty IDs: 2
Duplicate ID Groups: 1

❌ Objects with Empty UniqueIDs:
• Player_Spawner (PlayerSpawner)
• Collectible_001 (CollectibleItem)

❌ Duplicate UniqueID Groups:
• ID 'enemy_abc123' used by 2 objects:
  - Enemy (Enemy)
  - Enemy (1) (Enemy)

Would you like to automatically fix these issues?
[Fix Issues] [Just Report]
```

---

## When to Use

### **Development Workflow**

#### **Scene Building**
- After adding many new SaveableBase objects
- Before committing scene changes
- When preparing scenes for testing

#### **Asset Integration**  
- After importing new prefabs with SaveableBase components
- When integrating work from team members
- Before building releases

#### **Debugging Save Issues**
- When save/load operations fail unexpectedly
- When objects don't restore properly from saves
- During save system troubleshooting

### **Team Workflows**

#### **Pre-Commit Validation**
```bash
# Good practice: validate before committing scenes
1. Open scenes to be committed
2. Run "Validate All Loaded Scenes"
3. Fix any issues found
4. Commit clean scenes
```

#### **Integration Testing**
- Validate scenes after merging branches
- Check for ID conflicts after team integration
- Ensure consistent UniqueID usage across team

---

## Technical Details

### **Performance Characteristics**

#### **Validation Speed**
- **Small Scenes** (< 100 SaveableBase objects): Instant
- **Medium Scenes** (100-500 objects): < 1 second  
- **Large Scenes** (500+ objects): 1-3 seconds
- **Multi-Scene**: Scales linearly with total object count

#### **Memory Usage**
- Minimal memory footprint during validation
- Temporary collections cleaned up automatically
- No persistent memory impact

#### **Safety Features**
- **Read-Only Analysis**: Validation doesn't modify scenes
- **Optional Fixing**: User confirms before any changes
- **Undo Support**: Changes can be undone via Ctrl+Z
- **Error Recovery**: Graceful handling of corrupted objects

### **Integration with Existing Systems**

#### **Works With**
- All SaveableBase implementations
- Single and multi-scene setups
- Prefab variants and overrides
- Nested SaveableBase objects

#### **Compatibility**
- **Unity 2021.3+**: Full compatibility
- **All Render Pipelines**: No dependencies
- **All Platforms**: Editor-only tool
- **Version Control**: Git/Perforce friendly

---

## Troubleshooting

### **Common Issues**

#### **"No Active Scene" Error**
- **Cause**: No scene is currently open
- **Solution**: Open a scene file first
- **Prevention**: Always work with open scenes

#### **"Cannot Validate During Play" Error**
- **Cause**: Trying to validate while in Play Mode
- **Solution**: Stop Play Mode first
- **Reason**: Validation modifies editor objects

#### **"No SaveableBase Objects Found"**
- **Cause**: Scene contains no objects with SaveableBase components
- **Status**: Not an error - just informational
- **Action**: No action needed

#### **Validation Never Completes**
- **Cause**: Very large number of objects (10,000+)
- **Solution**: Use Progress Bar, be patient
- **Prevention**: Consider scene organization

### **Edge Cases**

#### **Prefab Variants with Same IDs**
- **Issue**: Prefab variants might share base prefab's ID
- **Detection**: Tool identifies these as duplicates
- **Resolution**: Generate new IDs for variants

#### **Cross-Scene ID Conflicts**
- **Issue**: Different scenes using same UniqueIDs
- **Detection**: Multi-scene validation catches these
- **Resolution**: Generate new IDs to ensure global uniqueness

#### **Corrupted or Missing Components**
- **Issue**: SaveableBase components with missing scripts
- **Handling**: Gracefully skipped during validation
- **Logging**: Warnings logged to console

---

## Best Practices

### **Regular Validation Schedule**

#### **Daily Development**
- Quick scene validation before major changes
- Validate after importing new assets
- Check before committing to version control

#### **Weekly Maintenance**
- Comprehensive multi-scene validation
- Review validation logs for patterns
- Clean up any recurring issues

#### **Release Preparation**
- Full project validation before builds
- Document any known issues
- Ensure clean state for release

### **Team Coordination**

#### **Shared Workflows**
- Establish team validation standards
- Include validation in code review process  
- Use validation results in team communications

#### **Issue Tracking**
- Log recurring validation issues
- Track patterns across different team members
- Address systemic problems in workflows

---

## Integration Examples

### **Build Pipeline Integration**

```csharp
// Example: Pre-build validation check
[MenuItem("Build/Validate and Build")]
public static void ValidateAndBuild()
{
    // Run validation programmatically
    if (SaveableValidationTool.ValidateAllLoadedScenes())
    {
        // Proceed with build
        BuildPipeline.BuildPlayer(settings);
    }
    else
    {
        Debug.LogError("Build cancelled: UniqueID validation failed");
    }
}
```

### **Automated Testing**

```csharp
// Example: Unit test integration
[Test]
public void TestSceneUniqueIDValidity()
{
    // Load test scene
    SceneManager.LoadScene("TestScene");
    
    // Validate programmatically
    var result = SaveableValidationTool.ValidateCurrentScene();
    
    // Assert no issues
    Assert.AreEqual(0, result.EmptyIds.Count);
    Assert.AreEqual(0, result.DuplicateGroups.Count);
}
```

---

## Conclusion

The Saveable Validation Tool provides comprehensive UniqueID validation for your SaveableBase objects, ensuring your save/load system works reliably. Its automated fixing capabilities and detailed reporting make it an essential tool for maintaining clean, functional save systems in Unity projects.

**Key Benefits:**
- ✅ Automated UniqueID validation
- ✅ One-click issue fixing  
- ✅ Detailed reporting and logging
- ✅ Multi-scene support
- ✅ Team workflow integration
- ✅ Safe, reliable operation
