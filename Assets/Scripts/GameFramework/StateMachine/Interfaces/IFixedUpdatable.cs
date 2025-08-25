
namespace GameFramework.StateMachine.Interfaces
{
    /// <summary>
    /// Interface for services that need to update at fixed intervals
    /// </summary>
    public interface IFixedUpdatable
    {
        void FixedUpdate();
    }

}