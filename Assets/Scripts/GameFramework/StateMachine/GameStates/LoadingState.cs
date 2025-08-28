using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;

namespace GameFramework.StateMachine.GameStates
{
    
    /// <summary>
    /// Loading state with constructor injection
    /// </summary>
    public class LoadingState : BaseGameState
    {
        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public LoadingState(
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputService inputService,
            IConsoleService consoleService)  
            : base(GameStateType.Loading, stateMachine, eventSystem, audioService, uiService, inputService, consoleService)
        {
        }
    
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            // Implement loading logic with progress bars, asset loading, etc.
            // All services available via constructor injection
        }
    }
}