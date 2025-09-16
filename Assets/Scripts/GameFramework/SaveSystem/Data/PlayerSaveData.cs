using UnityEngine;

namespace GameFramework.SaveSystem.Data
{
    [System.Serializable]
    public class PlayerSaveData
    {
        public string uniqueID;
        public string playerName;
        public Vector3 Position;
        public Vector3 Rotation;
    }

}