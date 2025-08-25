namespace GameFramework.StateMachine.Interfaces
{
    /// <summary>
    /// Interface for services that handle late updates
    /// </summary>
    public interface ILateUpdatable
    {
        void LateUpdate();
    }
}