using UnityEngine;
using System;

namespace GameFramework.Components.Controllers
{
    /// <summary>
    /// Controller types supported by the system
    /// </summary>
    public enum ControllerType
    {
        FirstPerson,
        ThirdPerson,
        RTS,
        Isometric
    }

    /// <summary>
    /// DEPRECATED: This factory class is no longer used in the new prefab-based controller system.
    /// 
    /// The new system uses pre-configured prefabs with MonoBehaviour components that can be 
    /// configured directly in the inspector. Use ControllerManager with prefab variants instead.
    /// 
    /// Migration Guide:
    /// 1. Create prefabs for each controller type following the documentation
    /// 2. Assign prefabs to ControllerManager inspector slots  
    /// 3. Use ControllerManager.SwitchToController() instead of factory methods
    /// 
    /// This class is kept for compatibility but all methods are deprecated.
    /// </summary>
    [System.Obsolete(
        "ControllerFactory is deprecated. Use prefab-based controllers with ControllerManager instead. See PREFAB_SETUP_INSTRUCTIONS.md for migration guide.",
        false)]
    public static class ControllerFactory
    {
        #region Deprecated Factory Methods

        [System.Obsolete(
            "Use prefab-based controllers with ControllerManager instead. See PREFAB_SETUP_INSTRUCTIONS.md")]
        public static BasePlayerController CreateController(ControllerType type, GameObject target,
            ControllerConfiguration config = null)
        {
            Debug.LogWarning(
                $"[ControllerFactory] CreateController() is deprecated. Use prefab-based controllers with ControllerManager.SwitchToController() instead. See PREFAB_SETUP_INSTRUCTIONS.md for migration guide.");
            return null;
        }

        [System.Obsolete(
            "Use prefab-based controllers with ControllerManager instead. See PREFAB_SETUP_INSTRUCTIONS.md")]
        public static BasePlayerController SwitchController(GameObject target, ControllerType newType,
            ControllerConfiguration config = null)
        {
            Debug.LogWarning(
                $"[ControllerFactory] SwitchController() is deprecated. Use ControllerManager.SwitchToController() instead. See PREFAB_SETUP_INSTRUCTIONS.md for migration guide.");
            return null;
        }

        [System.Obsolete(
            "Use prefab-based controllers with ControllerManager instead. See PREFAB_SETUP_INSTRUCTIONS.md")]
        public static void RemoveAllControllers(GameObject target)
        {
            Debug.LogWarning(
                $"[ControllerFactory] RemoveAllControllers() is deprecated. Controllers are now managed via prefab switching in ControllerManager.");
        }

        #endregion

        #region Utility Methods (Still Useful)

        /// <summary>
        /// Get all available controller types
        /// </summary>
        public static ControllerType[] GetAvailableControllerTypes()
        {
            return (ControllerType[])Enum.GetValues(typeof(ControllerType));
        }

        /// <summary>
        /// Check if a GameObject has any controller
        /// </summary>
        public static bool HasController(GameObject target)
        {
            return target != null && target.GetComponent<BasePlayerController>() != null;
        }

        /// <summary>
        /// Get the current controller type on a GameObject
        /// </summary>
        public static ControllerType? GetCurrentControllerType(GameObject target)
        {
            if (target == null) return null;

            var controller = target.GetComponent<BasePlayerController>();
            if (controller == null) return null;

            switch (controller)
            {
                case FirstPersonController _:
                    return ControllerType.FirstPerson;
                case ThirdPersonController _:
                    return ControllerType.ThirdPerson;
                case RTSController _:
                    return ControllerType.RTS;
                case IsometricController _:
                    return ControllerType.Isometric;
                default:
                    return null;
            }
        }

        #endregion


        /// <summary>
        /// DEPRECATED: Configuration class for the old factory-based controller creation.
        /// In the new prefab-based system, all configuration is done in the inspector on the prefabs themselves.
        /// </summary>
        [System.Serializable]
        [System.Obsolete(
            "ControllerConfiguration is deprecated. Configure controller settings directly in prefab inspector instead.")]
        public class ControllerConfiguration
        {
            // Keeping a minimal version for compatibility
            public bool StartEnabled = true;

            /// <summary>
            /// Create a default configuration (deprecated - use prefab inspector configuration instead)
            /// </summary>
            [System.Obsolete("Use prefab inspector configuration instead")]
            public static ControllerConfiguration CreateDefault(ControllerType controllerType)
            {
                Debug.LogWarning(
                    "ControllerConfiguration.CreateDefault() is deprecated. Configure settings directly on prefabs instead.");
                return new ControllerConfiguration();
            }
        }
    }
}
