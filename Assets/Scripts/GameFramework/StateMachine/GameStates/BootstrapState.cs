using System.Threading.Tasks;
using GameFramework.Core;
using GameFramework.Input;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;
using GameFramework.StateMachine.Interfaces;

namespace GameFramework.StateMachine.GameStates
{
    /// <summary>
    /// Bootstrap state handles initial game setup using constructor injection for all dependencies.
    /// All required services are injected via constructor rather than resolved at runtime.
    /// </summary>
    public class BootstrapState : BaseGameState
    {
        /// <summary>
        /// Constructor injection - all dependencies provided by DI container
        /// </summary>
        public BootstrapState(
            GameContext context,
            IGameStateMachine stateMachine)
            : base(GameStateType.Bootstrap, context, stateMachine)
        {
        }
        
        public override async Task EnterAsync(GameContext context)
        {
            await base.EnterAsync(context);

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
                Context.SceneService
            };
            
            foreach (var service in services)
            {
                if (!service.IsInitialized)
                    await service.InitializeAsync();
            }
            
            InputManager.SetInputContext(InputContext.UI);

        }
    }

}