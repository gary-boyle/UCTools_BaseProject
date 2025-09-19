using System;

namespace GameFramework.SaveSystem.Attributes
{
    /// <summary>
    /// Attribute to specify the RuntimeObjectSaveData type associated with a SaveableBase class.
    /// This enables automatic type discovery and eliminates the need for manual registration.
    /// 
    /// Usage:
    /// [SaveableType(typeof(MyObjectRuntimeSaveData))]
    /// public class MyObject : SaveableBase
    /// {
    ///     // Implementation...
    /// }
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SaveableTypeAttribute : Attribute
    {
        /// <summary>
        /// The Type of RuntimeObjectSaveData associated with this SaveableBase class
        /// </summary>
        public Type SaveDataType { get; }
        
        /// <summary>
        /// Optional display name for this saveable type (defaults to SaveDataType.Name without "RuntimeSaveData" suffix)
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// Creates a new SaveableType attribute
        /// </summary>
        /// <param name="saveDataType">The Type of RuntimeObjectSaveData for this SaveableBase class</param>
        public SaveableTypeAttribute(Type saveDataType)
        {
            if (saveDataType == null)
                throw new ArgumentNullException(nameof(saveDataType));
                
            if (!typeof(GameFramework.SaveSystem.Data.RuntimeObjectSaveData).IsAssignableFrom(saveDataType))
                throw new ArgumentException($"SaveDataType must inherit from RuntimeObjectSaveData. Got: {saveDataType.Name}");
                
            SaveDataType = saveDataType;
        }
    }
}
