using UnityEngine;

namespace GameFramework.SaveSystem.Data
{
    /// <summary>
    /// Serializable save data for ClickableCube component
    /// Stores the cube's color and integer value for persistence
    /// </summary>
    [System.Serializable]
    public class ClickableCubeSaveData
    {
        [SerializeField] public string uniqueID;
        [SerializeField] public Color cubeColor;
        [SerializeField] public int cubeValue;
        
        public ClickableCubeSaveData()
        {
            cubeColor = Color.white;
            cubeValue = 0;
        }
        
        public ClickableCubeSaveData(string uniqueID, Color color, int value)
        {
            this.uniqueID = uniqueID;
            this.cubeColor = color;
            this.cubeValue = value;
        }
    }
}
