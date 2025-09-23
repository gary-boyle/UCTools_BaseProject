namespace GameFramework.Components.Controllers.Enum
{
    /// <summary>
    /// Enum for different player controller types available in the game framework.
    /// Maps to prefabs in the Prefabs/Player folder and determines interaction behavior.
    /// </summary>
    public enum PlayerPrefabType
    {
        /// <summary>
        /// First-person shooter style controller with direct camera control.
        /// Maps to Player_FPS.prefab
        /// Uses distance and facing detection for interactions.
        /// </summary>
        FPS = 0,
        
        /// <summary>
        /// Third-person controller with orbital camera control.
        /// Maps to Player_3rdPerson.prefab
        /// Uses distance and facing detection for interactions.
        /// </summary>
        ThirdPerson = 1,
        
        /// <summary>
        /// Real-time strategy style controller with camera control.
        /// Maps to Player_RTS.prefab
        /// Uses mouse hover detection for interactions.
        /// </summary>
        RTS = 2,
        
        /// <summary>
        /// Isometric top-down controller with fixed camera angle.
        /// Maps to Player_Isometric.prefab
        /// Uses distance and facing detection for interactions.
        /// </summary>
        Isometric = 3
    }
}
