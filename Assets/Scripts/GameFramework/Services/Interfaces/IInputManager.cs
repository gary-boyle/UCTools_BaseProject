using System.Threading.Tasks;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Interfaces;

namespace GameFramework.Input.Interfaces
{
    /// <summary>
    /// Interface for the central input manager that coordinates all input handlers
    /// Manages handler activation/deactivation based on game context
    /// </summary>
    public interface IInputManager : IUpdatable, IGameService
    {
        /// <summary>
        /// Gets whether the input manager has been initialized
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// Initialize the input manager and all registered handlers
        /// </summary>
        Task InitializeAsync();
        
        /// <summary>
        /// Set the input context for the current game state
        /// This will automatically activate/deactivate appropriate handlers
        /// </summary>
        /// <param name="context">Input context to set</param>
        void SetInputContext(InputContext context);

        /// <summary>
        /// Clean shutdown of the input manager
        /// </summary>
        void Shutdown();
    }
}
