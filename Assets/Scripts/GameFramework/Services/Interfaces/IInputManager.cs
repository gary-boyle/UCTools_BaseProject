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
        /// Register an input handler with the manager
        /// </summary>
        /// <param name="handler">Input handler to register</param>
        void RegisterHandler(InputHandlerBase handler);
        
        /// <summary>
        /// Activate a specific input handler type
        /// </summary>
        /// <typeparam name="T">Type of input handler to activate</typeparam>
        void ActivateHandler<T>() where T : InputHandlerBase;
        
        /// <summary>
        /// Deactivate a specific input handler type
        /// </summary>
        /// <typeparam name="T">Type of input handler to deactivate</typeparam>
        void DeactivateHandler<T>() where T : InputHandlerBase;
        
        /// <summary>
        /// Set the input context for the current game state
        /// This will automatically activate/deactivate appropriate handlers
        /// </summary>
        /// <param name="context">Input context to set</param>
        void SetInputContext(InputContext context);
        
        /// <summary>
        /// Check if a specific input handler type is currently active
        /// </summary>
        /// <typeparam name="T">Type of input handler to check</typeparam>
        /// <returns>True if the handler is active</returns>
        bool IsHandlerActive<T>() where T : InputHandlerBase;
        
        /// <summary>
        /// Get the current input context
        /// </summary>
        /// <returns>Current input context</returns>
        InputContext GetCurrentContext();
        
        /// <summary>
        /// Clean shutdown of the input manager
        /// </summary>
        void Shutdown();
    }
}
