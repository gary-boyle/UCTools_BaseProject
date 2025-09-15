using System;
using UnityEngine;

/// <summary>
/// Wrapper for individual saved object data with type information
/// Enables proper deserialization of different object types
/// </summary>
[System.Serializable]
public class SavedObjectData
{
    #region Serialized Fields
    [SerializeField] private string typeName;
    [SerializeField] private string dataJson; // Store as JSON string
    #endregion

    #region Public Properties
    /// <summary>
    /// Type name for deserialization
    /// </summary>
    public string TypeName 
    { 
        get => typeName; 
        set => typeName = value; 
    }
    
    /// <summary>
    /// Raw JSON data - consumers should deserialize this themselves
    /// </summary>
    public string DataJson
    {
        get => dataJson;
        set => dataJson = value;
    }
    #endregion

    public SavedObjectData() { }

    public SavedObjectData(string typeName, string dataJson)
    {
        this.typeName = typeName;
        this.dataJson = dataJson;
    }

    /// <summary>
    /// Constructor that serializes object data to JSON
    /// </summary>
    public SavedObjectData(string typeName, object data)
    {
        this.typeName = typeName;
        this.dataJson = data != null ? JsonUtility.ToJson(data) : null;
    }

    /// <summary>
    /// Deserializes the stored JSON data to the specified type
    /// </summary>
    public T GetData<T>() where T : class
    {
        if (string.IsNullOrEmpty(dataJson))
            return null;
            
        try
        {
            return JsonUtility.FromJson<T>(dataJson);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SavedObjectData] Failed to deserialize {typeName}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize the stored JSON data to the specified type
    /// Returns success status and result via out parameter
    /// </summary>
    public bool TryGetData<T>(out T result) where T : class
    {
        result = null;
        
        if (string.IsNullOrEmpty(dataJson))
            return false;
            
        try
        {
            result = JsonUtility.FromJson<T>(dataJson);
            return result != null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SavedObjectData] Failed to deserialize {typeName}: {ex.Message}");
            return false;
        }
    }
}
