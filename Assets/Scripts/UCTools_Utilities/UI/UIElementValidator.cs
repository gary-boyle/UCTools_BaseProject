using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace UCTools_Utilities.UI
{
    /// <summary>
    /// Simple validation helper for UI Toolkit elements
    /// </summary>
    public static class UIElementValidator
    {
        public enum ValidationMode
        {
            LogWarnings,
            ThrowExceptions,
            Silent
        }
        
        /// <summary>
        /// Validates a collection of UI elements - just pass the elements directly
        /// Note: For detailed error reporting with field names, use ValidateElementsWithNames instead
        /// </summary>
        /// <param name="contextName">Name of the UI class for error messages</param>
        /// <param name="mode">How to handle validation failures</param>
        /// <param name="elements">The UI elements to validate</param>
        /// <returns>True if all elements are valid</returns>
        public static bool ValidateElements(
            string contextName,
            ValidationMode mode = ValidationMode.LogWarnings,
            params VisualElement[] elements)
        {
            var nullCount = elements.Count(e => e == null);
            
            if (nullCount > 0)
            {
                string message;
                
                if (mode == ValidationMode.ThrowExceptions)
                {
                    // For exceptions, provide more detailed info about which positions are null
                    var nullPositions = elements
                        .Select((element, index) => new { element, index })
                        .Where(x => x.element == null)
                        .Select(x => $"element[{x.index}]")
                        .ToList();
                    
                    message = $"[{contextName}] The following UI elements are null: {string.Join(", ", nullPositions)}";
                }
                else
                {
                    message = $"[{contextName}] {nullCount} out of {elements.Length} UI elements are null";
                }
                
                switch (mode)
                {
                    case ValidationMode.LogWarnings:
                        Debug.LogWarning(message);
                        break;
                    case ValidationMode.ThrowExceptions:
                        throw new InvalidOperationException(message);
                    case ValidationMode.Silent:
                        break;
                }
                return false;
            }
            
            if (mode != ValidationMode.Silent)
            {
                Debug.Log($"[{contextName}] All {elements.Length} UI elements validated successfully");
            }
            
            return true;
        }
        
        /// <summary>
        /// Validates elements and gets field names via reflection for detailed error reporting
        /// This is the recommended method for detailed validation feedback
        /// </summary>
        public static bool ValidateElementsWithNames<T>(T instance, ValidationMode mode = ValidationMode.LogWarnings)
        {
            var type = typeof(T);
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                           .Where(f => typeof(VisualElement).IsAssignableFrom(f.FieldType))
                           .ToList();
            
            var nullFields = new List<string>();
            
            foreach (var field in fields)
            {
                var value = field.GetValue(instance) as VisualElement;
                if (value == null)
                {
                    nullFields.Add(field.Name);
                }
            }
            
            if (nullFields.Any())
            {
                var message = $"[{type.Name}] The following UI elements are null: {string.Join(", ", nullFields)}";
                
                switch (mode)
                {
                    case ValidationMode.LogWarnings:
                        Debug.LogWarning(message);
                        break;
                    case ValidationMode.ThrowExceptions:
                        throw new InvalidOperationException(message);
                    case ValidationMode.Silent:
                        break;
                }
                return false;
            }
            
            if (mode != ValidationMode.Silent)
            {
                Debug.Log($"[{type.Name}] All {fields.Count} UI elements validated successfully");
            }
            
            return true;
        }
    }
}
