namespace GameFramework.Components.Controllers.Enum
{
    /// <summary>
    /// Defines cursor locking requirements for different player controller types
    /// </summary>
    public enum CursorLockRequirement
    {
        /// <summary>
        /// Never lock the cursor (e.g., RTS, isometric controllers)
        /// </summary>
        Never,
        
        /// <summary>
        /// Lock cursor only during gameplay state (e.g., first-person controllers)
        /// </summary>
        DuringGameplay,
        
        /// <summary>
        /// Lock cursor during gameplay but allow temporary unlocking for UI interactions
        /// (e.g., third-person controllers with camera control)
        /// </summary>
        DuringGameplayWithUIExceptions
    }
}
