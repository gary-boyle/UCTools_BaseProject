
using UnityEngine;

namespace GameFramework.Components.Saveable
{
    /// <summary>
    /// Serializable save data class for GenericSaveable.
    /// This is what gets saved to/loaded from the save file.
    /// </summary>
    [System.Serializable]
    public class GenericSaveableData
    {
        public string uniqueID;
        public Vector3 Position;
        public Vector3 Rotation;

    }
}