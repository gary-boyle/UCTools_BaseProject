using UnityEngine;

namespace GameFramework.SaveSystem.Data
{
    /// <summary>
    /// Helper class for serializing Dictionary as List
    /// JsonUtility doesn't serialize Dictionary directly
    /// </summary>
    [System.Serializable]
    public class SavedObjectEntry
    {
        [SerializeField] public string Key;
        [SerializeField] public SavedObjectData Value;
    }
}