namespace GameFramework.Services
{
    /// <summary>
    /// Input context types for managing which input maps are active
    /// </summary>
    public enum InputContext
    {
        None,
        UI,        // For menus, UI navigation
        Player,    // For gameplay
        Mixed      // For states that need both UI and Player input
    }
}