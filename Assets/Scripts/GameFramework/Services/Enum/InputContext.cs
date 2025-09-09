namespace GameFramework.Input
{
    /// <summary>
    /// Defines the different input contexts for the game
    /// </summary>
    public enum InputContext
    {
        /// <summary>
        /// No input handling (except console)
        /// </summary>
        None = 0,
        
        /// <summary>
        /// UI input only (menus, popups, etc.)
        /// </summary>
        UI = 1,
        
        /// <summary>
        /// Player input only (gameplay)
        /// </summary>
        Player = 2,
        
        /// <summary>
        /// Both UI and Player input active (pause menu during gameplay, inventory, etc.)
        /// </summary>
        Mixed = 3
    }
}