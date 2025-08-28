using System;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Data;

namespace GameFramework.Core
{
    /// <summary>
    /// Context object containing all core game services.
    /// Uses constructor injection to receive all dependencies.
    /// Provides centralized access to services for game states and other systems.
    /// </summary>
    public class GameContext
    {
        public IEventSystem EventSystem { get; }
        public ISceneService SceneService { get; }
        public IAudioService AudioService { get; }
        public IInputService InputService { get; }
        public IUIService UIService { get; }
        public ISaveService SaveService { get; }
        public IConfigService ConfigService { get; }
        public IGameDataService GameDataService { get; } // New service for game data
    
        public GameContext(
            IEventSystem eventSystem,
            ISceneService sceneService, 
            IAudioService audioService,
            IInputService inputService,
            IUIService uiService,
            ISaveService saveService,
            IConfigService configService,
            IGameDataService gameDataService)
        {
            EventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            SceneService = sceneService ?? throw new ArgumentNullException(nameof(sceneService));
            AudioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            InputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
            UIService = uiService ?? throw new ArgumentNullException(nameof(uiService));
            SaveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            ConfigService = configService ?? throw new ArgumentNullException(nameof(configService));
            GameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
        }
    }
}