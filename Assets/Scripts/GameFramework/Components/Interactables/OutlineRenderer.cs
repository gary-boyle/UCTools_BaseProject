using UnityEngine;

namespace GameFramework.Components.Interactables
{
    /// <summary>
    /// Simple outline renderer for interactable objects.
    /// Uses material swapping for basic outline effect.
    /// Can be extended with more sophisticated outline methods.
    /// </summary>
    [System.Serializable]
    public class OutlineRenderer
    {
        [Header("Outline Settings")]
        [SerializeField] private Material _outlineMaterial;
        [SerializeField] private Color _outlineColor = Color.yellow;
        [SerializeField] private bool _useOutlineMaterial = true;
        
        private Renderer _renderer;
        private Material _originalMaterial;
        private bool _isOutlined = false;
        
        /// <summary>
        /// Initialize the outline renderer with the target renderer
        /// </summary>
        public void Initialize(Renderer targetRenderer)
        {
            _renderer = targetRenderer;
            if (_renderer != null)
            {
                _originalMaterial = _renderer.material;
                
                // Create outline material if not provided
                if (_outlineMaterial == null && _useOutlineMaterial)
                {
                    CreateDefaultOutlineMaterial();
                }
            }
        }
        
        /// <summary>
        /// Show the outline effect
        /// </summary>
        public void ShowOutline()
        {
            if (_renderer == null || _isOutlined) return;
            
            if (_useOutlineMaterial && _outlineMaterial != null)
            {
                _renderer.material = _outlineMaterial;
            }
            else
            {
                // Fallback: modify material color
                _renderer.material.color = _outlineColor;
            }
            
            _isOutlined = true;
        }
        
        /// <summary>
        /// Hide the outline effect
        /// </summary>
        public void HideOutline()
        {
            if (_renderer == null || !_isOutlined) return;
            
            _renderer.material = _originalMaterial;
            _isOutlined = false;
        }
        
        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Cleanup()
        {
            if (_isOutlined)
            {
                HideOutline();
            }
        }
        
        private void CreateDefaultOutlineMaterial()
        {
            if (_originalMaterial == null) return;
            
            // Create a simple outline material based on the original
            _outlineMaterial = new Material(_originalMaterial);
            _outlineMaterial.color = _outlineColor;
            
            // If the material supports emission, use it for outline effect
            if (_outlineMaterial.HasProperty("_EmissionColor"))
            {
                _outlineMaterial.EnableKeyword("_EMISSION");
                _outlineMaterial.SetColor("_EmissionColor", _outlineColor * 0.5f);
            }
        }
    }
}
