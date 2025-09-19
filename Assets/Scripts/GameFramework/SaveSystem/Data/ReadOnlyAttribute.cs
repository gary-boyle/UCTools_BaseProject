using UnityEngine;

namespace GameFramework.SaveSystem.Data
{
    /// <summary>
    /// Custom attribute to mark fields as read-only in the Inspector
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute
    {
        public ReadOnlyAttribute() { }
    }
}
