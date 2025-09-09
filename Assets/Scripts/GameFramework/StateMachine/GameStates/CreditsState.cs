using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Input;
using GameFramework.Input.Interfaces;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Credits state with constructor injection
    /// </summary>
    public class CreditsState : BaseGameState
    {
        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public CreditsState(
            IGameStateMachine stateMachine,
            IEventSystem eventSystem,
            IAudioService audioService,
            IUIService uiService,
            IInputManager inputManager,
            IConsoleService consoleService,
            IGameDataService gameDataService)  
            : base(GameStateType.Credits, stateMachine, eventSystem, audioService, uiService, inputManager, consoleService, gameDataService)
        {
        }
    
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);
            // Implement credits roll with music and scrolling text
            // All services available via constructor injection
            InputManager.SetInputContext(InputContext.UI);

        }
    }
}