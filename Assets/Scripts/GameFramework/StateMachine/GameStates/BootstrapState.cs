using System;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Input.Interfaces;
using GameFramework.Services;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;
using UnityEngine;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Bootstrap state handles initial game setup using constructor injection for all dependencies.
    /// All required services are injected via constructor rather than resolved at runtime.
    /// </summary>
    public class BootstrapState : BaseGameState
    {
        private readonly IConfigService _configService;
        
        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public BootstrapState(
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputManager inputManager,
            IConfigService configService,
            IConsoleService consoleService,
            IGameDataService gameDataService) 
            : base(GameStateType.Bootstrap, stateMachine, eventSystem, audioService, uiService, inputManager, consoleService, gameDataService)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }
        
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            
            Debug.Log("[Bootstrap] Initializing core systems...");
            
            // Initialize all core services
            await InitializeCoreServices();
            
            // Load initial configuration using injected service
            await _configService.LoadConfigAsync();
            

            // Automatically transition to splash screen using injected state machine
            //await Task.Delay(1); // Brief pause for initialization
            await TransitionToStateAsync(GameStateType.Splash);
        }
        
        private async Task InitializeCoreServices()
        {
            // Services should already be registered, just ensure they're initialized
            var services = new IGameService[]
            {
                EventSystem,
                AudioService,
                InputManager,
                UIService,
                ConsoleService, 
                Context.SaveService,
                _configService,
                Context.SceneService
            };
            
            foreach (var service in services)
            {
                if (!service.IsInitialized)
                    await service.InitializeAsync();
            }
        }
    }

}