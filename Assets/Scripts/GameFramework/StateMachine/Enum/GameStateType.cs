namespace GameFramework.StateMachine.Enum
{
    /// <summary>
    /// Enumeration of all possible game states
    /// </summary>
    public enum GameStateType
    {
        Bootstrap,    // Initial loading and setup
        Splash,      // Company/game logos
        MainMenu,    // Main menu navigation
        Loading,     // Loading screens between transitions  
        NewGame,     // New game setup and character creation
        Playing,     // Active gameplay
        //Paused,      // Game paused overlay
        //Options,     // Settings and configuration
        Credits,     // Credits roll
        GameOver,    // Game over screen
        Victory,     // Victory/completion screen
        Quit         // Shutting down
    }
}