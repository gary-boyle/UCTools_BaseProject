using System;
using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Input.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Quit state with constructor injection
    /// </summary>
    public class QuitState : BaseGameState
    {
        private readonly ISaveService _saveService;
    
        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public QuitState(
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputManager inputManager,
            ISaveService saveService,
            IConsoleService consoleService,
            IGameDataService gameDataService)  
            : base(GameStateType.Quit, stateMachine, eventSystem, audioService, uiService, inputManager, consoleService, gameDataService)
        {
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
        }
    
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            // Handle application shutdown with save confirmation using injected save service
            // All services available via constructor injection
        }
    }
}