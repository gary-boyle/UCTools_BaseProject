# Modular Controller System - Prefab Setup Instructions

This guide explains how to set up prefabs with the new modular controller system where Camera and Movement components are independent MonoBehaviours that can be configured in the inspector.

## Overview

The controller system now consists of:
- **Controller Classes** (IsometricController, FirstPersonController, etc.) - Main controller logic
- **Movement Components** (IsometricMovement, FirstPersonMovement, etc.) - Handle movement logic
- **Camera Components** (IsometricCameraControl, FirstPersonCameraControl, etc.) - Handle camera behavior

## Isometric Controller Setup

### Required GameObject Hierarchy

```
Player (Root)
├── IsometricController (Component)
├── IsometricMovement (Component)  
├── IsometricCameraControl (Component)
├── Rigidbody (Component)
├── CapsuleCollider (Component)
└── [Optional] Character Model
    ├── Animator (Component)
    └── [Optional] SpriteRenderer (Component)

CinemachineCamera (Separate GameObject)
└── CinemachineCamera (Component)
```

### Step-by-Step Setup

#### 1. Create the Player GameObject

1. Create an empty GameObject and name it "IsometricPlayer"
2. Add the required components in this order:
   - `IsometricController`
   - `IsometricMovement` 
   - `IsometricCameraControl`
   - `Rigidbody`
   - `CapsuleCollider`

#### 2. Configure the Components

**IsometricController:**
- **Movement Component**: Drag the IsometricMovement component from the same GameObject
- **Camera Component**: Drag the IsometricCameraControl component from the same GameObject
- **Character Model**: (Optional) Assign character model GameObject
- **Animator**: (Optional) Assign if using 3D character animations
- **Character Sprite**: (Optional) Assign if using 2D sprite-based character
- **Use Grid Movement**: Enable for grid-based movement games
- **Grid Size**: Set to 1.0 for standard grid size
- **Grid Move Speed**: Adjust movement speed for grid transitions
- **Use Sprite Renderer**: Enable if using 2D sprites
- **Flip Sprite With Movement**: Enable to flip sprite based on movement direction

**IsometricMovement:**
- **Move Speed**: Base movement speed (default: 5.0)
- **Sprint Multiplier**: Speed multiplier when sprinting (default: 1.5)
- **Crouch Multiplier**: Speed multiplier when crouching (default: 0.5)
- **Normalize Diagonal Movement**: Ensure consistent speed in all directions
- **Rotate Towards Movement**: Enable character rotation based on movement
- **Rotation Speed**: Speed of rotation animation
- **Use Physics**: Enable physics-based movement (recommended)
- **Acceleration/Deceleration**: Control movement responsiveness
- **Ground Layer Mask**: Set layers that count as ground
- **Show Debug Info**: Enable for debugging movement

**IsometricCameraControl:**
- **Cinemachine Camera**: Drag the CinemachineCamera GameObject (see step 3)
- **Follow Target**: Usually the player transform (assigned automatically)
- **Isometric Angle**: Camera X rotation (default: 30°)
- **Camera Rotation Y**: Camera Y rotation (default: 45°)
- **Follow Offset**: Camera position offset from target
- **Enable Manual Pan**: Allow camera panning with mouse
- **Enable Zoom**: Allow zooming with scroll wheel
- **Min/Max Zoom**: Set zoom limits for orthographic size
- **Orthographic Projection**: Enable for true isometric view
- **Show Debug Info**: Enable for debugging camera

**Rigidbody:**
- **Use Gravity**: Disable (handled by movement component)
- **Freeze Rotation**: Enable X, Y, Z to prevent physics rotation
- **Mass**: 1.0 (default)
- **Drag**: 0 (movement handles deceleration)

**CapsuleCollider:**
- **Height**: 2.0 (standard character height)
- **Radius**: 0.5 (standard character width)
- **Center Y**: 1.0 (half the height)

#### 3. Create the Cinemachine Camera

1. Create a new GameObject and name it "IsometricCamera"
2. Add the `CinemachineCamera` component
3. Configure the CinemachineCamera:
   - **Priority**: 10 (higher than default cameras)
   - **Follow**: Assign the player GameObject
   - **Look At**: Assign the player GameObject
   - **Lens > Orthographic**: Enable in camera component (set at runtime by controller)
   - **Lens > Orthographic Size**: 8.0 (adjusted by zoom controls)

4. Position the camera:
   - **Position**: (0, 10, -10) relative to player
   - **Rotation**: (30, 45, 0) for standard isometric angle

5. In the IsometricCameraControl component on the player:
   - **Cinemachine Camera**: Drag the CinemachineCamera GameObject here

### Optional Components

#### Character Model (3D)
If using a 3D character model:
1. Create a child GameObject under the player
2. Add your 3D model as a child of this GameObject
3. Add an `Animator` component with your character's Animator Controller
4. In IsometricController, assign:
   - **Character Model**: The parent GameObject containing the model
   - **Animator**: The Animator component

#### Sprite Character (2D)
If using a 2D sprite character:
1. Create a child GameObject under the player
2. Add a `SpriteRenderer` component
3. Assign your character sprite
4. In IsometricController, assign:
   - **Character Sprite**: The SpriteRenderer component
   - **Use Sprite Renderer**: Enable
   - **Flip Sprite With Movement**: Enable for automatic sprite flipping

## First Person Controller Setup

### Required GameObject Hierarchy

```
Player (Root)
├── FirstPersonController (Component)
├── FirstPersonMovement (Component)
├── FirstPersonCameraControl (Component)
├── Rigidbody (Component)
└── CapsuleCollider (Component)

CinemachineCamera (Child of Player)
├── CinemachineCamera (Component)
└── Position at head height (0, 1.7, 0)
```

### Configuration Notes
- **FirstPersonMovement**: Configure jump height, ground detection, and physics
- **FirstPersonCameraControl**: Set mouse sensitivity and look constraints
- **CinemachineCamera**: Position at eye level, typically (0, 1.7, 0) local position

## Third Person Controller Setup

### Required GameObject Hierarchy

```
Player (Root)
├── ThirdPersonController (Component)
├── ThirdPersonMovement (Component)
├── ThirdPersonCameraControl (Component)
├── Rigidbody (Component)
├── CapsuleCollider (Component)
└── [Optional] Character Model with Animator

CinemachineCamera (Separate GameObject)
└── CinemachineCamera (Component) with Orbital Follow
```

### Configuration Notes
- **ThirdPersonCameraControl**: Configure orbital distance and collision settings
- **CinemachineCamera**: Add `CinemachineOrbitalFollow` component for third-person behavior

## RTS Controller Setup

### Required GameObject Hierarchy

```
RTS Camera Controller (Root)
├── RTSController (Component)
├── RTSMovement (Component)
└── RTSCameraControl (Component)

CinemachineCamera (Separate GameObject)
└── CinemachineCamera (Component) - Orthographic, top-down view
```

### Configuration Notes
- No Rigidbody/Collider needed (camera-only controller)
- Set up selection and command systems separately
- **RTSCameraControl**: Configure pan limits, zoom range, and movement speed

## General Tips

### Input System Integration
- Controllers automatically handle input events
- Make sure your Input Actions are properly configured
- Test all movement and camera inputs after setup

### Testing Your Prefab
1. **Movement**: Test WASD movement, sprinting, crouching
2. **Camera**: Test mouse look, zoom (if applicable)
3. **Grid Movement**: Test grid snapping and transitions (isometric only)
4. **Animations**: Verify character animations respond to movement
5. **Debug Gizmos**: Enable debug info to visualize ranges and states

### Performance Considerations
- Use physics-based movement for most scenarios
- Disable physics for grid-based movement to reduce overhead
- Optimize camera settings based on your game's requirements
- Consider object pooling for frequently spawned controllers

### Common Issues

**Controller Not Responding:**
- Verify all component references are assigned
- Check that input events are being published
- Ensure GameManager services are initialized

**Camera Not Following:**
- Verify CinemachineCamera is assigned to camera component
- Check Follow and LookAt targets are set
- Ensure camera priority is higher than other cameras

**Movement Issues:**
- Check Rigidbody settings (gravity, constraints)
- Verify ground layer mask settings
- Test with debug info enabled

**Animation Not Playing:**
- Ensure Animator is assigned and has valid Animator Controller
- Check animation parameter names match the expected values
- Verify animation states and transitions are set up correctly

## Prefab Variants

You can create multiple prefab variants for different scenarios:
- **IsometricPlayer_Grid**: For grid-based movement games
- **IsometricPlayer_Free**: For free movement games  
- **IsometricPlayer_Sprite**: For 2D sprite-based games
- **IsometricPlayer_3D**: For 3D model-based games
- **FirstPersonPlayer**: For first-person shooters and exploration games
- **ThirdPersonPlayer**: For action-adventure and platformer games
- **RTSController**: For real-time strategy and simulation games

Each variant can have different component configurations while sharing the same base setup.

---

## First Person Controller Setup

### Required GameObject Hierarchy

```
Player (Root)
├── FirstPersonController (Component)
├── FirstPersonMovement (Component)  
├── FirstPersonCameraControl (Component)
├── Rigidbody (Component)
├── CapsuleCollider (Component)
├── CameraMount (Child GameObject)
└── [Optional] Character Model

CinemachineCamera (Separate GameObject or Child of CameraMount)
└── CinemachineCamera (Component)
```

### Step-by-Step Setup

#### 1. Create the Player GameObject

1. Create an empty GameObject and name it "FirstPersonPlayer"
2. Add the required components in this order:
   - `FirstPersonController`
   - `FirstPersonMovement` 
   - `FirstPersonCameraControl`
   - `Rigidbody`
   - `CapsuleCollider`

#### 2. Create Camera Mount

1. Create child GameObject under the player and name it "CameraMount"
2. Position at eye level: typically (0, 1.7, 0) local position

#### 3. Configure the Components

**FirstPersonController:**
- **Movement Component**: Drag the FirstPersonMovement component from the same GameObject
- **Camera Component**: Drag the FirstPersonCameraControl component from the same GameObject  
- **Cinemachine Camera**: Drag the CinemachineCamera GameObject
- **Camera Mount**: Drag the CameraMount child GameObject

**FirstPersonMovement:**
- **Move Speed**: Base movement speed (default: 5.0)
- **Sprint Multiplier**: Speed multiplier when sprinting (default: 1.5)
- **Jump Force**: Force applied when jumping (default: 5.0)
- **Ground Layer Mask**: Set layers that count as ground
- **Crouch Settings**: Configure crouching height and transition speed
- **Show Debug Info**: Enable for debugging movement

**FirstPersonCameraControl:**
- **Cinemachine Camera**: Drag the CinemachineCamera GameObject
- **Follow Target**: Usually the CameraMount transform
- **Mouse Sensitivity Multiplier**: Adjust look sensitivity (default: 1.0)
- **Vertical Angle Limits**: Min/Max look angles (default: -80° to 80°)
- **Cursor Settings**: Configure cursor lock and visibility
- **Show Debug Info**: Enable for debugging camera

#### 4. Create the Cinemachine Camera

1. Create a new GameObject and name it "FirstPersonCamera"
2. Add the `CinemachineCamera` component
3. Configure the CinemachineCamera:
   - **Priority**: 10 (higher than default cameras)
   - **Follow**: Assign the CameraMount GameObject
   - **Look At**: Assign the CameraMount GameObject
   - **Lens > Field of View**: 60° (standard FPS FOV)

---

## Third Person Controller Setup

### Required GameObject Hierarchy

```
Player (Root)
├── ThirdPersonController (Component)
├── ThirdPersonMovement (Component)
├── ThirdPersonCameraControl (Component)
├── Rigidbody (Component)
├── CapsuleCollider (Component)
├── CameraLookAtTarget (Child GameObject)
└── [Optional] Character Model with Animator

CinemachineCamera (Separate GameObject)
└── CinemachineCamera (Component) with Orbital Follow
```

### Step-by-Step Setup

#### 1. Create the Player GameObject

1. Create an empty GameObject and name it "ThirdPersonPlayer"
2. Add the required components in this order:
   - `ThirdPersonController`
   - `ThirdPersonMovement`
   - `ThirdPersonCameraControl`
   - `Rigidbody`
   - `CapsuleCollider`

#### 2. Create Camera Look-At Target

1. Create child GameObject under the player and name it "CameraLookAtTarget"
2. Position at chest level: typically (0, 1.2, 0) local position

#### 3. Configure the Components

**ThirdPersonController:**
- **Movement Component**: Drag the ThirdPersonMovement component from the same GameObject
- **Camera Component**: Drag the ThirdPersonCameraControl component from the same GameObject
- **Cinemachine Camera**: Drag the CinemachineCamera GameObject
- **Camera Look At Target**: Drag the CameraLookAtTarget child GameObject
- **Character Model**: (Optional) Assign character model GameObject
- **Animator**: (Optional) Assign if using character animations

**ThirdPersonMovement:**
- **Move Speed**: Base movement speed (default: 5.0)
- **Sprint Multiplier**: Speed multiplier when sprinting (default: 1.5)
- **Jump Force**: Force applied when jumping (default: 5.0)
- **Rotation Settings**: Configure character rotation behavior
- **Ground Detection**: Set ground layer mask and check distance
- **Show Debug Info**: Enable for debugging movement

**ThirdPersonCameraControl:**
- **Cinemachine Camera**: Drag the CinemachineCamera GameObject
- **Follow Target**: Usually the player transform
- **Look At Target**: Usually the CameraLookAtTarget transform
- **Orbit Settings**: Configure mouse sensitivity and angle limits
- **Distance Settings**: Set follow distance and zoom limits
- **Collision**: Enable collision detection and set layer mask
- **Show Debug Info**: Enable for debugging camera

#### 4. Create the Cinemachine Camera

1. Create a new GameObject and name it "ThirdPersonCamera"
2. Position behind and above the player: (0, 3, -5)
3. Add the `CinemachineCamera` component
4. Configure the CinemachineCamera:
   - **Priority**: 10 (higher than default cameras)  
   - **Follow**: Assign the player GameObject
   - **Look At**: Assign the CameraLookAtTarget GameObject
   - **Lens > Field of View**: 50° (standard third-person FOV)

The system will automatically add required Cinemachine components:
- `CinemachineOrbitalFollow` for orbital behavior
- `CinemachineRotationComposer` for smooth look-at
- `CinemachineDeoccluder` for collision detection

---

## RTS Controller Setup

### Required GameObject Hierarchy

```
RTS Camera Controller (Root)
├── RTSController (Component)
├── RTSMovement (Component)
├── RTSCameraControl (Component)
└── CameraRig (Child GameObject)

CinemachineCamera (Separate GameObject or Child of CameraRig)
└── CinemachineCamera (Component) - Orthographic, top-down view
```

### Step-by-Step Setup

#### 1. Create the RTS Controller GameObject

1. Create an empty GameObject and name it "RTSController"
2. Add the required components in this order:
   - `RTSController`
   - `RTSMovement`
   - `RTSCameraControl`

Note: No Rigidbody/Collider needed (camera-only controller)

#### 2. Create Camera Rig

1. Create child GameObject under the controller and name it "CameraRig"
2. Position at elevated location: typically (0, 15, 0)
3. Rotate for RTS angle: typically (45, 0, 0)

#### 3. Configure the Components

**RTSController:**
- **Movement Component**: Drag the RTSMovement component from the same GameObject
- **Camera Component**: Drag the RTSCameraControl component from the same GameObject
- **Cinemachine Camera**: Drag the CinemachineCamera GameObject
- **Camera Rig**: Drag the CameraRig child GameObject
- **Unit Selection**: Configure selection layer mask and colors
- **Camera Focus**: Enable focus capabilities and set speed

**RTSMovement:**
- **Pan Speed**: Camera panning speed (default: 5.0)
- **Pan Acceleration/Deceleration**: Control camera responsiveness
- **Edge Scrolling**: Configure edge scrolling settings
- **Zoom Settings**: Set zoom limits and speed
- **Boundaries**: Define camera movement limits
- **Show Debug Info**: Enable for debugging movement

**RTSCameraControl:**
- **Cinemachine Camera**: Drag the CinemachineCamera GameObject
- **Camera Rig**: Drag the CameraRig GameObject (can be auto-created)
- **Pan Settings**: Configure panning speed and sensitivity
- **Zoom Settings**: Set orthographic size limits
- **Rotation Settings**: Enable rotation and set speed
- **Height Settings**: Configure camera height based on zoom
- **Show Debug Info**: Enable for debugging camera

#### 4. Create the Cinemachine Camera

1. Create a new GameObject and name it "RTSCamera"
2. Position at camera rig or as child of CameraRig
3. Set rotation for top-down view: (45, 0, 0)
4. Add the `CinemachineCamera` component
5. Configure the CinemachineCamera:
   - **Priority**: 10 (higher than default cameras)
   - **Follow**: Assign the CameraRig GameObject
   - **Lens > Orthographic**: Enable in Unity Camera component (set at runtime by controller)
   - **Lens > Orthographic Size**: 10.0 (adjusted by zoom controls)
