# Third-Person Controller Setup Guide

This guide provides step-by-step instructions for setting up a third-person controller prefab that works correctly with mouse look and character rotation.

## Issues This Guide Addresses

- Camera not following when character rotates
- Character continuously moving when no input is given
- Mouse input not properly controlling character rotation
- Camera orbit behavior not working correctly

## Required GameObject Hierarchy

```
ThirdPersonPlayer (Root)
├── ThirdPersonController (Component)
├── ThirdPersonMovement (Component)  
├── ThirdPersonCameraControl (Component)
├── Rigidbody (Component)
├── CapsuleCollider (Component)
├── CameraLookAtTarget (Child GameObject) - IMPORTANT!
└── [Optional] Character Model with Animator

ThirdPersonCamera (Separate GameObject - NOT a child!)
├── CinemachineCamera (Component)
├── CinemachineOrbitalFollow (Component) - AUTO-ADDED
└── CinemachineRotationComposer (Component) - AUTO-ADDED
```

## Step-by-Step Setup

### 1. Create the Player GameObject

1. Create an empty GameObject and name it **"ThirdPersonPlayer"**
2. Position: (0, 0, 0)
3. Rotation: (0, 0, 0) 
4. Scale: (1, 1, 1)

### 2. Add Required Components to Player

Add these components **IN THIS ORDER**:

1. **ThirdPersonController**
2. **ThirdPersonMovement** 
3. **ThirdPersonCameraControl**
4. **Rigidbody**
5. **CapsuleCollider**

### 3. Create Camera Look-At Target (CRITICAL!)

1. **Right-click** on ThirdPersonPlayer → Create Empty
2. Name it **"CameraLookAtTarget"**
3. Set **local position**: `(0, 1.2, 0)` (chest level)
4. Set **local rotation**: `(0, 0, 0)`
5. Set **local scale**: `(1, 1, 1)`

> ⚠️ **WARNING**: This target is essential for proper camera behavior!

### 4. Create the Cinemachine Camera (Separate GameObject!)

1. **In the root scene** (NOT as child of player), create empty GameObject
2. Name it **"ThirdPersonCamera"**
3. Position: `(0, 5, -5)` (initial position - Cinemachine will take over)
4. Add **CinemachineCamera** component

### 5. Configure Cinemachine Camera

**CinemachineCamera Settings:**
- **Priority**: `10` (higher than other cameras)
- **Follow**: Drag **ThirdPersonPlayer** (the root player object)
- **Look At**: Drag **CameraLookAtTarget** (the child object)
- **Lens → Field of View**: `60°`

**Auto-Added Components** (Should appear automatically):
- **CinemachineOrbitalFollow** - handles orbiting around player
- **CinemachineRotationComposer** - handles look-at behavior

### 6. Configure CinemachineOrbitalFollow

This component should auto-configure, but verify these settings:

- **Horizontal Axis**:
  - Value: `0`
  - Range: `(-180, 180)`
  - Wrap: `True`
  - Max Speed: `300`

- **Vertical Axis**:
  - Value: `30` (initial angle)
  - Range: `(-30, 60)` 
  - Wrap: `False`
  - Max Speed: `2`

- **Radius**: `5` (distance from character)

### 7. Configure Player Components

#### ThirdPersonController:
- **Movement Component**: Drag ThirdPersonMovement from same GameObject
- **Camera Component**: Drag ThirdPersonCameraControl from same GameObject
- **Cinemachine Camera**: Drag the ThirdPersonCamera GameObject
- **Camera Look At Target**: Drag the CameraLookAtTarget child GameObject
- **Show Debug Info**: Enable for testing

#### ThirdPersonMovement:
- **Move Speed**: `5.0`
- **Sprint Multiplier**: `1.5`
- **Jump Force**: `5.0`
- **Ground Layer Mask**: Set to ground layers only
- **Rotate Towards Movement**: Choose based on control scheme (see below)
- **Rotation Speed**: `10.0`
- **Enable Mouse Rotation**: `True` ⚠️ **NEW: Enables mouse-controlled character rotation**
- **Mouse Sensitivity Multiplier**: `1.0`
- **Show Debug Info**: Enable for testing

> **Control Scheme Options**:
> - **Mouse-Controlled Rotation**: Set **Enable Mouse Rotation** to `True`, **Rotate Towards Movement** to `False`
> - **Movement-Based Rotation**: Set **Enable Mouse Rotation** to `False`, **Rotate Towards Movement** to `True`

#### ThirdPersonCameraControl:
- **Cinemachine Camera**: Drag the ThirdPersonCamera GameObject
- **Follow Target**: Drag the ThirdPersonPlayer (root object)
- **Look At Target**: Drag the CameraLookAtTarget child GameObject
- **Mouse Sensitivity Multiplier**: `1.0`
- **Orbit Speed**: `2.0`
- **Min/Max Vertical Angle**: `(-30, 60)`
- **Follow Distance**: `5.0`
- **Lock Cursor**: `True`
- **Hide Cursor**: `True`
- **Show Debug Info**: Enable for testing

#### Rigidbody:
- **Mass**: `1`
- **Drag**: `0`
- **Angular Drag**: `0.05`
- **Use Gravity**: `True`
- **Is Kinematic**: `False`
- **Freeze Position**: None
- **Freeze Rotation**: `X, Z` (Y should be unchecked to allow turning)

#### CapsuleCollider:
- **Center**: `(0, 1, 0)`
- **Radius**: `0.5`
- **Height**: `2`

## Expected Behavior

After proper setup, you should have:

✅ **Mouse Horizontal**: Rotates character left/right (handled by **ThirdPersonMovement**)  
✅ **Mouse Vertical**: Moves camera up/down around character (handled by **ThirdPersonCameraControl**)  
✅ **WASD**: Moves character relative to camera direction  
✅ **Camera**: Follows character smoothly via Cinemachine, maintains relative position when character rotates  
✅ **No Input**: Character stops moving, camera stops rotating  

## Architecture Overview

**NEW ARCHITECTURE** (Cleaner separation of concerns):

- **ThirdPersonMovement**: Handles all character rotation (both mouse-based and movement-based)
- **ThirdPersonCameraControl**: Handles only camera behavior (vertical orbit, following, zoom)
- **ThirdPersonController**: Routes input appropriately (look input goes to both movement and camera)

This design is more logical because:
- Movement component controls the character
- Camera component only manages camera behavior  
- No conflicts between movement and camera rotation systems  

## Common Issues & Solutions

### Issue: Camera doesn't follow character rotation
**Root Cause**: Improper Cinemachine setup or missing components

**Solutions**: 
- Ensure Rigidbody **Freeze Rotation Y** is **UNCHECKED**
- Verify CinemachineOrbitalFollow is present and configured
- Make sure camera Follow target is the player root, not a child
- Check that CameraLookAtTarget exists and is positioned correctly
- Verify ThirdPersonCamera is **NOT** a child of the player object

### Issue: Character keeps moving without input  
**Root Cause**: Input conflicts or continuous input events

**Solutions**:
- **Check rotation system conflicts**: Ensure only ONE rotation mode is enabled in ThirdPersonMovement
- Enable debug info on ThirdPersonMovement to check input values  
- Enable debug info on BasePlayerController to verify input events
- Verify **movement input (WASD)** and **look input (mouse)** are properly separated
- Check that Input Manager isn't sending continuous movement events
- Ensure no gamepad is connected with stick drift
- **NEW**: Verify **Enable Mouse Rotation** setting matches your intended control scheme

### Issue: Character rotates unexpectedly or conflicts with camera
**Root Cause**: Multiple rotation systems active simultaneously

**Solutions**:
- Choose **ONE** rotation mode in ThirdPersonMovement:
  - **Mouse-Controlled**: **Enable Mouse Rotation** = `True`, **Rotate Towards Movement** = `False`
  - **Movement-Based**: **Enable Mouse Rotation** = `False`, **Rotate Towards Movement** = `True`
- **Do NOT** enable both systems simultaneously

### Issue: Camera jumps or doesn't orbit smoothly
**Root Cause**: Incorrect Cinemachine configuration

**Solutions**:
- Verify CameraLookAtTarget is at chest level `(0, 1.2, 0)`
- Check CinemachineOrbitalFollow axis ranges are correct
- Ensure camera is NOT a child of the player
- Reset CinemachineOrbitalFollow if it becomes corrupted
- Check that Follow and LookAt targets are assigned correctly

### Issue: Mouse sensitivity too high/low
**Root Cause**: Incorrect sensitivity settings

**Solutions**:
- Adjust **Mouse Sensitivity Multiplier** on ThirdPersonCameraControl
- Adjust **Orbit Speed** for camera rotation speed
- Check global input settings in InputSettings_SO
- Try starting with lower values like `0.5` and adjust up

### Issue: Camera doesn't move behind character when rotating
**Root Cause**: Orbital follow system not maintaining relative position

**Solutions**:
- Verify CinemachineOrbitalFollow **Horizontal Axis** is set to `0` and **Range** is `(-180, 180)` with **Wrap** enabled
- Ensure the Follow target is the player root GameObject
- Check that orbital follow is not being overridden by manual camera positioning

## Testing Checklist

- [ ] Character moves with WASD
- [ ] Character stops when no WASD input
- [ ] Mouse horizontal rotates character
- [ ] Mouse vertical moves camera up/down
- [ ] Camera follows character smoothly
- [ ] Camera maintains position when character rotates
- [ ] Character faces movement direction
- [ ] No continuous movement without input
- [ ] Cursor locks in play mode
- [ ] Jump works
- [ ] Sprint works (Shift + WASD)

## Debug Mode

Enable **Show Debug Info** on all components to see:
- Movement input values
- Camera rotation values  
- Character rotation
- Ground detection
- Input event logging

This will help identify where the setup might be incorrect.

---

## Summary of Changes

This guide has been **updated** to reflect the new cleaner architecture:

### What Changed:
- **Character rotation** moved from camera component to movement component
- **Mouse horizontal input** now processed by ThirdPersonMovement (not ThirdPersonCameraControl)
- **Camera** only handles vertical orbit and following behavior
- **Clear separation** of concerns between movement and camera systems

### Migration from Old Architecture:
If you were using the previous system:
1. **Enable Mouse Rotation** = `True` in ThirdPersonMovement
2. **Rotate Towards Movement** = `False` in ThirdPersonMovement  
3. Camera component now only handles vertical camera orbit
4. Horizontal mouse input automatically routes to character rotation

---

If issues persist after following this guide, check:
1. Input Manager configuration
2. Event system setup  
3. Service manager initialization
4. Input context assignment
5. **NEW**: Ensure correct rotation mode is selected in ThirdPersonMovement
