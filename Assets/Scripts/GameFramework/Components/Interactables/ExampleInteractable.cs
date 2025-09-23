using UnityEngine;
using GameFramework.Components.Controllers.Enum;

namespace GameFramework.Components.Interactables
{
    /// <summary>
    /// Example interactable object for testing the interaction system.
    /// Demonstrates basic interaction counting and debug output.
    /// </summary>
    public class ExampleInteractable : BaseInteractable
    {
        [Header("Example Settings")]
        [SerializeField] private string _interactableName = "Example Object";
        
        public override void OnInteract(PlayerPrefabType controllerType)
        {
            base.OnInteract(controllerType);
            
            Debug.Log($"[ExampleInteractable] '{_interactableName}' was interacted with via {controllerType} controller! (Total interactions: {InteractionCount})");
        }
        
        public override void OnInteractionAvailable(PlayerPrefabType controllerType)
        {
            base.OnInteractionAvailable(controllerType);
            
            Debug.Log($"[ExampleInteractable] '{_interactableName}' is now available for {controllerType} interaction");
        }
        
        public override void OnInteractionUnavailable(PlayerPrefabType controllerType)
        {
            base.OnInteractionUnavailable(controllerType);
            
            Debug.Log($"[ExampleInteractable] '{_interactableName}' is no longer available for {controllerType} interaction");
        }
    }
}
