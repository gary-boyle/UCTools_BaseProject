using UnityEngine;
using GameFramework.SaveSystem;

namespace GameFramework.SaveSystem.Examples
{
    /// <summary>
    /// Simple test class to verify SaveableBase works with the fixed save system
    /// This should reproduce the SaveKey that was failing: "GenericSaveable_genericsaveable_..."
    /// </summary>
    public class TestGenericSaveable : SaveableBase
    {
        [Header("Test Generic Saveable")]
        [SerializeField] private int _testValue = 42;
        [SerializeField] private string _testString = "Hello World";
        [SerializeField] private bool _testBool = true;
        
        protected override string GetUniqueIdPrefix()
        {
            return "genericsaveable"; // This matches the failing SaveKey prefix
        }
        
        public override object GetSaveData()
        {
            Debug.Log($"[TestGenericSaveable] Creating save data for {gameObject.name} with SaveKey: {SaveKey}");
            
            return new TestGenericSaveData
            {
                uniqueID = UniqueID,
                testValue = _testValue,
                testString = _testString,
                testBool = _testBool
            };
        }

        public override void LoadSaveData(object data)
        {
            if (data == null)
            {
                Debug.LogWarning($"[TestGenericSaveable] Cannot load null save data for {gameObject.name}");
                return;
            }

            TestGenericSaveData saveData;
            
            if (data is TestGenericSaveData directData)
            {
                saveData = directData;
            }
            else
            {
                try
                {
                    var json = JsonUtility.ToJson(data);
                    saveData = JsonUtility.FromJson<TestGenericSaveData>(json);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[TestGenericSaveable] Failed to deserialize save data: {ex.Message}");
                    return;
                }
            }

            SetUniqueID(saveData.uniqueID);
            _testValue = saveData.testValue;
            _testString = saveData.testString;
            _testBool = saveData.testBool;
            
            Debug.Log($"[TestGenericSaveable] Loaded save data for {gameObject.name} - Value: {_testValue}, String: {_testString}, Bool: {_testBool}");
        }
        
        // Public methods for testing
        public void SetTestValue(int value) => _testValue = value;
        public void SetTestString(string str) => _testString = str;
        public void SetTestBool(bool value) => _testBool = value;
        
        public int GetTestValue() => _testValue;
        public string GetTestString() => _testString;
        public bool GetTestBool() => _testBool;
    }

    [System.Serializable]
    public class TestGenericSaveData
    {
        public string uniqueID;
        public int testValue;
        public string testString;
        public bool testBool;
    }
}
