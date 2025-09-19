using System;
using UnityEngine;

namespace GameFramework.SaveSystem.Data
{
    /// <summary>
    /// Clean save data structure for runtime objects that need to be instantiated or modified.
    /// Replaces the nested JSON string approach with direct field storage.
    /// Each object type will have its own specific data fields instead of being serialized as JSON.
    /// </summary>
    [System.Serializable]
    public class RuntimeObjectSaveData
    {
        [Header("Object Identity")]
        public string uniqueID;                // Runtime unique identifier
        public string prefabGUID;             // GUID of the source prefab (for instantiation)
        public string typeName;               // Type name for validation
        
        [Header("Transform Data")]
        public Vector3 position;
        public Vector3 rotation;              // Euler angles for easier editing
        public Vector3 scale = Vector3.one;
        public bool isActive = true;
        
        [Header("Object-Specific Data")]
        // This will be populated by individual object types
        // using their own serializable data structures
        public string objectDataJson;         // Temporary - will be replaced by specific fields
        
        public RuntimeObjectSaveData()
        {
            scale = Vector3.one;
            isActive = true;
        }
        
        public RuntimeObjectSaveData(string uniqueID, string prefabGUID, string typeName)
        {
            this.uniqueID = uniqueID;
            this.prefabGUID = prefabGUID;
            this.typeName = typeName;
            this.scale = Vector3.one;
            this.isActive = true;
        }
    }
    
    /// <summary>
    /// Specialized save data for ClickableCube objects with clean structure
    /// </summary>
    [System.Serializable]
    public class ClickableCubeRuntimeSaveData : RuntimeObjectSaveData
    {
        [Header("ClickableCube Data")]
        public Color cubeColor = Color.white;
        public int cubeValue = 0;
        
        public ClickableCubeRuntimeSaveData() : base() { }
        
        public ClickableCubeRuntimeSaveData(string uniqueID, string prefabGUID) 
            : base(uniqueID, prefabGUID, "ClickableCube")
        {
        }
    }
    
    /// <summary>
    /// Specialized save data for TestGenericSaveable objects
    /// </summary>
    [System.Serializable]
    public class TestGenericRuntimeSaveData : RuntimeObjectSaveData
    {
        [Header("TestGeneric Data")]
        public int testValue = 42;
        public string testString = "Hello World";
        public bool testBool = true;
        
        public TestGenericRuntimeSaveData() : base() { }
        
        public TestGenericRuntimeSaveData(string uniqueID, string prefabGUID) 
            : base(uniqueID, prefabGUID, "TestGenericSaveable")
        {
        }
    }
}
