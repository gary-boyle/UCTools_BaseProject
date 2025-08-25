## 2. Setup-Guide.md

```markdown
# Setup Guide

Complete guide to setting up the Unity Game Framework in your project.

## 📋 Prerequisites

- Unity 2021.3 LTS or newer
- UI Toolkit package installed
- Basic C# and Unity knowledge

## 🚀 Step-by-Step Setup

### Step 1: Install Framework Files

1. **Copy Framework Scripts**
   ```
   Assets/
   └── Scripts/
       └── Framework/
           ├── Core/
           │   ├── DIContainer.cs
           │   ├── GameManager.cs
           │   └── GameContext.cs
           ├── StateMachine/
           │   ├── GameStateMachine.cs
           │   ├── BaseGameState.cs
           │   └── GameStates/
           ├── Services/
           │   ├── EventSystem.cs
           │   ├── AudioService.cs
           │   ├── UIService.cs
           │   ├── SaveService.cs
           │   ├── ConfigService.cs
           │   ├── InputService.cs
           │   └── SceneService.cs
           ├── UI/
           │   ├── UIScreen.cs
           │   ├── UIPopup.cs
           │   └── Screens/
           └── Config/
               └── DefaultConfigVars.cs
   ```

2. **Verify All Dependencies**
   - Ensure all scripts compile without errors
   - Check that UI Toolkit is installed via Package Manager

### Step 2: Create UI Document

1. **Create UI Document Asset**
   ```
   Assets/
   └── UI/
       ├── MainUI.uxml (UI Document)
       └── MainUI.uss (StyleSheet - optional)
   ```

2. **Basic UXML Structure**
   ```xml
   <ui:UXML xmlns:ui="UnityEngine.UIElements">
       <!-- Splash Screen -->
       <ui:VisualElement name="SplashScreen" style="display: none;">
           <ui:Label name="VersionLabel" text="Version 1.0" />
       </ui:VisualElement>
       
       <!-- Main Menu -->
       <ui:VisualElement name="MainMenuScreen" style="display: none;">
           <ui:Button name="NewGameButton" text="New Game" />
           <ui:Button name="ContinueButton" text="Continue" />
           <ui:Button name="OptionsButton" text="Options" />
           <ui:Button name="CreditsButton" text="Credits" />
           <ui:Button name="QuitButton" text="Quit" />
       </ui:VisualElement>
       
       <!-- Gameplay HUD -->
       <ui:VisualElement name="GameplayHUD" style="display: none;">
           <ui:Label name="ScoreLabel" text="Score: 0" />
           <ui:Label name="HealthLabel" text="Health: 100/100" />
           <ui:Label name="TimeLabel" text="Time: 00:00" />
           <ui:Button name="PauseButton" text="Pause" />
       </ui:VisualElement>
       
       <!-- Pause Screen -->
       <ui:VisualElement name="PauseScreen" style="display: none;">
           <ui:Button name="ResumeButton" text="Resume" />
           <ui:Button name="OptionsButton" text="Options" />
           <ui:Button name="MainMenuButton" text="Main Menu" />
       </ui:VisualElement>
       
       <!-- Options Screen -->
       <ui:VisualElement name="OptionsScreen" style="display: none;">
           <ui:SliderInt name="MasterVolumeSlider" label="Master Volume" 
                        low-value="0" high-value="100" value="100" />
           <ui:SliderInt name="MusicVolumeSlider" label="Music Volume" 
                        low-value="0" high-value="100" value="80" />
           <ui:SliderInt name="SfxVolumeSlider" label="SFX Volume" 
                        low-value="0" high-value="100" value="100" />
           <ui:Button name="ResetDefaultsButton" text="Reset to Defaults" />
           <ui:Button name="BackButton" text="Back" />
       </ui:VisualElement>
       
       <!-- Loading Screen -->
       <ui:VisualElement name="LoadingScreen" style="display: none;">
           <ui:Label text="Loading..." />
       </ui:VisualElement>
       
       <!-- Additional Screens -->
       <ui:VisualElement name="NewGameScreen" style="display: none;" />
       <ui:VisualElement name="CreditsScreen" style="display: none;" />
       <ui:VisualElement name="GameOverScreen" style="display: none;" />
       <ui:VisualElement name="VictoryScreen" style="display: none;" />
       
       <!-- Popups -->
       <ui:VisualElement name="ConfirmationPopup" style="display: none;" />
       <ui:VisualElement name="ErrorPopup" style="display: none;" />
   </ui:UXML>
   ```

### Step 3: Scene Setup

1. **Create Main Scene**
   - Create new scene: `Assets/Scenes/Main.unity`
   - Save as your main scene

2. **Add GameManager**
   - Create empty GameObject named "GameManager"
   - Add `GameManager` component
   - Configure settings in inspector

3. **Add UI Document Component**
   - Create empty GameObject named "UI"
   - Add `UIDocument` component
   - Assign your UXML file to the component
   - Ensure this GameObject persists (GameManager will handle this)

### Step 4: Audio Setup (Optional)

1. **Create Audio Resources**
   ```
   Assets/
   └── Audio/
       ├── Music/
       │   ├── main_menu.mp3
       │   ├── gameplay.mp3
       │   └── splash_music.mp3
       └── SFX/
           ├── click.wav
           ├── levelup.wav
           └── error.wav
   ```

2. **Update AudioService** (if needed)
   ```csharp
   // In AudioService.LoadAudioClips()
   private async Task LoadAudioClips()
   {
       // Load music
       _musicClips["main_menu"] = Resources.Load<AudioClip>("Audio/Music/main_menu");
       _musicClips["gameplay"] = Resources.Load<AudioClip>("Audio/Music/gameplay");
       
       // Load SFX
       _sfxClips["click"] = Resources.Load<AudioClip>("Audio/SFX/click");
       _sfxClips["levelup"] = Resources.Load<AudioClip>("Audio/SFX/levelup");
   }
   ```

### Step 5: Configuration Setup

1. **Initialize ConfigVars**
   ```csharp
   // DefaultConfigVars are automatically registered
   // Add custom config vars as needed:
   
   [ConfigVar(Name = "game.player_name", DefaultValue = "Player", 
              Description = "Player display name", Flags = ConfigFlags.Save)]
   public static ConfigVar PlayerName;
   ```

2. **Test Configuration**
   ```csharp
   // In any service or game logic:
   var configService = GameManager.GetService<IConfigService>();
   var playerName = configService.GetConfigValue<string>("game.player_name");
   ```

### Step 6: Build Settings

1. **Add Scenes to Build**
   - Add your main scene to Build Settings
   - Set as Scene 0 (first scene)

2. **Configure Player Settings**
   - Set appropriate .NET compatibility level
   - Configure target platform settings

## ✅ Verification Checklist

After setup, verify everything works:

- [ ] **Framework Compiles** - No compiler errors
- [ ] **GameManager Starts** - Console shows initialization messages
- [ ] **UI Displays** - Splash screen appears on play
- [ ] **State Transitions** - Can navigate between screens
- [ ] **Audio Works** - Sounds and music play (if implemented)
- [ ] **Config Saves** - Options persist between sessions
- [ ] **Input Responds** - Keyboard/mouse input works

## 🐛 Common Issues

### UI Elements Not Found
```csharp
// Problem: UI elements return null
// Solution: Check UXML element names match exactly

// In UXML:
<ui:Button name="NewGameButton" text="New Game" />

// In Code:
_newGameButton = RootElement?.Q<Button>("NewGameButton"); // Exact match required
```

### Services Not Resolving
```csharp
// Problem: Service resolution fails
// Solution: Check registration order in GameManager

// Services with dependencies must be registered after their dependencies
container.RegisterSingleton<IEventSystem, EventSystem>(); // No dependencies first
container.RegisterSingleton<IAudioService, AudioService>(); // Depends on IEventSystem
```

### State Transitions Fail
```csharp
// Problem: Invalid state transitions
// Solution: Check DefineStateTransitions() in GameStateMachine

// Ensure transition is defined:
_validTransitions.Add((GameStateType.MainMenu, GameStateType.NewGame));
```

## 🚀 Next Steps

1. **Read [Architecture Guide](Architecture-Guide.md)** - Understand the system
2. **Check [Examples](Examples.md)** - See practical usage
3. **Review [Extension Guide](Extension-Guide.md)** - Add custom features
4. **Start building your game!**

## 💡 Tips

- **Start Simple** - Begin with basic screens, add complexity gradually
- **Use Events** - Prefer event communication over direct service calls
- **Test Early** - Verify each system works before adding complexity
- **Follow Patterns** - Use the established patterns for consistency

---

Need help? Check the [API Reference](API-Reference.md) or [Examples](Examples.md)!
```