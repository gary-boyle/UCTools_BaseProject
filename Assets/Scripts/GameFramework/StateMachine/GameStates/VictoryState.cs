using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Victory state with constructor injection
    /// </summary>
    public class VictoryState : BaseGameState
    {
        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public VictoryState(
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputService inputService,
            IConsoleService consoleService,
            IGameDataService gameDataService)  
            : base(GameStateType.Victory, stateMachine, eventSystem, audioService, uiService, inputService, consoleService, gameDataService)
        {
        }
    
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            // Implement victory celebration with stats and progression
            // All services available via constructor injection
        }
    }
}