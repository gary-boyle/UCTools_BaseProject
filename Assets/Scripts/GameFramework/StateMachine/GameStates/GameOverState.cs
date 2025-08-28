using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Game over state with constructor injection
    /// </summary>
    public class GameOverState : BaseGameState
    {
        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public GameOverState(
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputService inputService,
            IConsoleService consoleService)  
            : base(GameStateType.GameOver, stateMachine, eventSystem, audioService, uiService, inputService, consoleService)
        {
        }
    
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            // Implement game over screen with restart options
            // All services available via constructor injection
        }
    }
}