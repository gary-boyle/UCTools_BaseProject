using UnityEngine;
using GameFramework.SaveSystem;
using GameFramework.SaveSystem.Data;

namespace GameFramework.SaveSystem.Examples
{
    /// <summary>
    /// Simple test class to verify the new clean save system works properly.
    /// Uses SaveableBaseV2 with direct field storage instead of nested JSON strings.
    /// </summary>
    public class TestGenericSaveable : SaveableBase
    {
        [Header("Test Generic Saveable")]
        [SerializeField] private int _testValue = 42;
        [SerializeField] private string _testString = "Hello World";
        [SerializeField] private bool _testBool = true;
        
        protected override string GetUniqueIdPrefix()
        {
            return "genericsaveable";
        }
        
        #region New Save System Implementation
        protected override RuntimeObjectSaveData CreateSpecificRuntimeSaveData()
        {
            Debug.Log($"[TestGenericSaveable] Creating runtime save data for {gameObject.name} with SaveKey: {SaveKey}");
            
            return new TestGenericRuntimeSaveData(UniqueID, PrefabGUID)
            {
                testValue = _testValue,
                testString = _testString,
                testBool = _testBool
            };
        }
        
        protected override void LoadSpecificRuntimeSaveData(RuntimeObjectSaveData saveData)
        {
            if (saveData is TestGenericRuntimeSaveData genericData)
            {
                _testValue = genericData.testValue;
                _testString = genericData.testString;
                _testBool = genericData.testBool;
                
                Debug.Log($"[TestGenericSaveable] Loaded runtime save data - Value: {_testValue}, String: {_testString}, Bool: {_testBool}");
            }
            else
            {
                Debug.LogWarning($"[TestGenericSaveable] Expected TestGenericRuntimeSaveData but got: {saveData?.GetType().Name}");
            }
        }
        #endregion
        
        // Public methods for testing
        public void SetTestValue(int value) => _testValue = value;
        public void SetTestString(string str) => _testString = str;
        public void SetTestBool(bool value) => _testBool = value;
        
        public int GetTestValue() => _testValue;
        public string GetTestString() => _testString;
        public bool GetTestBool() => _testBool;
    }

    // Legacy save data structure - kept for reference but no longer used
    // The new system uses TestGenericRuntimeSaveData from RuntimeObjectSaveData.cs
    [System.Serializable]
    public class TestGenericSaveData
    {
        public string uniqueID;
        public int testValue;
        public string testString;
        public bool testBool;
    }
}
