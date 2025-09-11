using System.Threading.Tasks;
using GameFramework.Services.Interfaces;
using GameFramework.StateMachine.Enum;

namespace GameFramework.StateMachine.Interfaces
{
    /// <summary>
    /// Advanced state machine with constructor injection support.
    /// All dependencies are injected rather than resolved from service locator.
    /// Uses explicit state definitions for maximum Unity compatibility.
    /// 
    /// Design: State machine pattern with async support and transition validation
    /// Pros: Robust state management, prevents invalid transitions, supports complex state logic, Unity compatible
    /// Cons: More complex than simple state switching, requires careful transition planning, explicit state definitions
    /// </summary>
    public interface IGameStateMachine : IGameService
    {
        Task ChangeStateAsync(GameStateType newStateType);
    }
}