using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Cinemachine;
using GameFramework.Components.Controllers.Camera;
using GameFramework.Core;
using GameFramework.EventSystem.Events;
using GameFramework.Input;
using GameFramework.Services.Interfaces;
using UnityEngine.InputSystem;

namespace GameFramework.Components.Controllers
{
    /// <summary>
    /// Real-Time Strategy controller that manages camera movement and unit selection.
    /// Uses a single RTSCameraControl component for all camera functionality.
    /// Handles unit selection with mouse clicks and selection boxes.
    /// </summary>
    public class RTSController : BasePlayerController
    {
        #region Serialized Fields
        [Header("Camera Component")]
        [SerializeField] private RTSCameraControl _cameraControl;
        
        [Header("Unit Selection")]
        [SerializeField] private bool _enableUnitSelection = true;
        [SerializeField] private LayerMask _selectableLayerMask = -1;
        [SerializeField] private Color _selectionBoxColor = new Color(0, 1, 0, 0.3f);
        
        [Header("Camera Focus")]
        [SerializeField] private bool _enableCameraFocus = true;
        [SerializeField] private float _focusSpeed = 5.0f;
        #endregion

        #region Private Fields
        private UnityEngine.Camera _mainCamera;
        
        // Unit selection
        private List<ISelectableUnit> _selectedUnits = new List<ISelectableUnit>();
        private Vector2 _selectionStart;
        private Vector2 _selectionEnd;
        private bool _isSelecting = false;
        private Rect _selectionRect;
        private Vector2 _currentMousePosition;
        private bool _isShiftPressed = false;
        
        // Camera focus
        private bool _isFocusing = false;
        private Vector3 _focusTarget;
        private float _focusStartTime;
        
        // UI and visual feedback
        private Texture2D _selectionBoxTexture;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            
            _mainCamera = GameManager.GetService<IGameDataService>().GetMainCamera();

            // Override input context for RTS
            _requiredInputContext = InputContext.Mixed; // RTS needs both camera and UI input
            
            // Find camera control component if not assigned
            if (_cameraControl == null)
            {
                _cameraControl = GetComponent<RTSCameraControl>();
            }
            
            // Create selection box texture
            CreateSelectionBoxTexture();
            
            // Subscribe to UI events for selection
            if (_eventSystem != null)
            {
                _eventSystem.Subscribe<UIClickInputEvent>(OnUIClick);
                _eventSystem.Subscribe<UIPointInputEvent>(OnMousePosition);
            }
        }

        protected override void Update()
        {
            base.Update();
            
            if (_isInitialized)
            {
                HandleSelectionInput();
                HandleCameraFocus();
            }
        }

        private void OnGUI()
        {
            if (_isSelecting && _selectionBoxTexture != null)
            {
                DrawSelectionBox();
            }
        }
        
        protected override void OnDestroy()
        {
            // Unsubscribe from UI events
            if (_eventSystem != null)
            {
                _eventSystem.Unsubscribe<UIClickInputEvent>(OnUIClick);
                _eventSystem.Unsubscribe<UIPointInputEvent>(OnMousePosition);
            }
            
            base.OnDestroy();
        }
        #endregion

        #region Component Creation
        protected override void CreateComponents()
        {
            // Camera control component is assigned from inspector or found in Awake()
            if (_showDebugInfo)
                Debug.Log($"[RTSController] RTS Controller initialized - Camera: {(_cameraControl != null ? "Found" : "Missing")}");
        }


        private void CreateSelectionBoxTexture()
        {
            _selectionBoxTexture = new Texture2D(1, 1);
            _selectionBoxTexture.SetPixel(0, 0, _selectionBoxColor);
            _selectionBoxTexture.Apply();
        }
        #endregion

        #region Input Handling
        private void HandleSelectionInput()
        {
            if (!_enableUnitSelection) return;
            
            // Update selection rectangle if selecting
            if (_isSelecting)
            {
                UpdateSelection();
            }
        }


        private void HandleCameraFocus()
        {
            if (!_enableCameraFocus || !_isFocusing || _cameraControl == null) return;
            
            float elapsedTime = Time.time - _focusStartTime;
            float t = elapsedTime * _focusSpeed;
            
            if (t >= 1.0f)
            {
                _cameraControl.FocusOnPosition(_focusTarget);
                _isFocusing = false;
            }
            else
            {
                // Smoothly interpolate camera position using the camera control component
                Vector3 currentPos = _cameraControl.GetCameraTransform().position;
                Vector3 targetPos = Vector3.Lerp(currentPos, _focusTarget, t);
                _cameraControl.FocusOnPosition(targetPos);
            }
        }
        #endregion

        #region Unit Selection
        private void StartSelection()
        {
            _selectionStart = _currentMousePosition;
            _selectionEnd = _selectionStart;
            _isSelecting = true;
            
            // Clear previous selection if not holding Shift
            if (!_isShiftPressed)
            {
                ClearSelection();
            }
        }

        private void UpdateSelection()
        {
            _selectionEnd = _currentMousePosition;
            _selectionRect = GetSelectionRect();
        }

        private void EndSelection()
        {
            _isSelecting = false;
            
            // Perform selection
            if (_selectionRect.width > 5f && _selectionRect.height > 5f)
            {
                // Box selection
                SelectUnitsInRect(_selectionRect);
            }
            else
            {
                // Single click selection
                SelectUnitAtPoint(_selectionStart);
            }
        }

        private void SelectUnitAtPoint(Vector2 screenPoint)
        {
            
            Ray ray = _mainCamera.ScreenPointToRay(screenPoint);
            
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _selectableLayerMask))
            {
                var selectableUnit = hit.collider.GetComponent<ISelectableUnit>();
                if (selectableUnit != null)
                {
                    if (!_isShiftPressed)
                    {
                        ClearSelection();
                    }
                    
                    AddToSelection(selectableUnit);
                }
            }
        }

        private void SelectUnitsInRect(Rect selectionRect)
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameManager.GetService<IGameDataService>().GetMainCamera();
            }
            
            // Find all selectable units
            var allSelectables = FindObjectsOfType<MonoBehaviour>().OfType<ISelectableUnit>();
            
            foreach (var selectable in allSelectables)
            {
                if (selectable is MonoBehaviour mb)
                {
                    Vector3 screenPos = _mainCamera.WorldToScreenPoint(mb.transform.position);
                    
                    // Check if unit is within selection rectangle
                    if (selectionRect.Contains(new Vector2(screenPos.x, Screen.height - screenPos.y)))
                    {
                        AddToSelection(selectable);
                    }
                }
            }
        }

        private void AddToSelection(ISelectableUnit unit)
        {
            if (!_selectedUnits.Contains(unit))
            {
                _selectedUnits.Add(unit);
                unit.OnSelected();
                
                // Publish selection event
                _eventSystem?.Publish(new UnitSelectedEvent(unit));
            }
        }

        private void ClearSelection()
        {
            foreach (var unit in _selectedUnits)
            {
                unit.OnDeselected();
            }
            
            _selectedUnits.Clear();
            _eventSystem?.Publish(new SelectionClearedEvent());
        }

        private Rect GetSelectionRect()
        {
            float left = Mathf.Min(_selectionStart.x, _selectionEnd.x);
            float right = Mathf.Max(_selectionStart.x, _selectionEnd.x);
            float bottom = Mathf.Min(_selectionStart.y, _selectionEnd.y);
            float top = Mathf.Max(_selectionStart.y, _selectionEnd.y);
            
            return new Rect(left, Screen.height - top, right - left, top - bottom);
        }

        private void DrawSelectionBox()
        {
            Rect rect = GetSelectionRect();
            GUI.color = _selectionBoxColor;
            GUI.DrawTexture(rect, _selectionBoxTexture);
            GUI.color = Color.white;
        }
        #endregion


        #region Input Event Handlers - Override for RTS-specific behavior
        protected override void OnPlayerMoveInput(PlayerMoveInputEvent inputEvent)
        {
            // In RTS, move input controls camera movement (WASD)
            if (_cameraControl != null)
            {
                _cameraControl.HandleMoveInput(inputEvent);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Focus the camera on a specific world position
        /// </summary>
        public void FocusOnPosition(Vector3 worldPosition)
        {
            if (!_enableCameraFocus || _cameraControl == null) return;
            
            _focusTarget = worldPosition;
            _focusStartTime = Time.time;
            _isFocusing = true;
        }

        /// <summary>
        /// Focus the camera on the currently selected units
        /// </summary>
        public void FocusOnSelection()
        {
            if (_selectedUnits.Count == 0) return;
            
            Vector3 center = Vector3.zero;
            foreach (var unit in _selectedUnits)
            {
                if (unit is MonoBehaviour mb)
                {
                    center += mb.transform.position;
                }
            }
            
            center /= _selectedUnits.Count;
            FocusOnPosition(center);
        }

        /// <summary>
        /// Get the current selection
        /// </summary>
        public List<ISelectableUnit> GetSelectedUnits()
        {
            return new List<ISelectableUnit>(_selectedUnits);
        }

        /// <summary>
        /// Get the camera control component
        /// </summary>
        public RTSCameraControl GetCameraControl()
        {
            return _cameraControl;
        }

        /// <summary>
        /// Set selection box color
        /// </summary>
        public void SetSelectionBoxColor(Color color)
        {
            _selectionBoxColor = color;
            CreateSelectionBoxTexture();
        }

        /// <summary>
        /// Enable or disable unit selection
        /// </summary>
        public void SetUnitSelectionEnabled(bool enabled)
        {
            _enableUnitSelection = enabled;
            if (!enabled)
            {
                ClearSelection();
            }
        }

        /// <summary>
        /// Enable or disable camera focus functionality
        /// </summary>
        public void SetCameraFocusEnabled(bool enabled)
        {
            _enableCameraFocus = enabled;
            if (!enabled)
            {
                _isFocusing = false;
            }
        }
        #endregion

        #region Debug
        protected override void OnDrawGizmos()
        {

            base.OnDrawGizmos();
            
            if (!_showDebugInfo) return;
            
            // Draw selection area during selection
            if (_isSelecting)
            {
                Gizmos.color = Color.green;
                Vector3 start = _mainCamera.ScreenToWorldPoint(new Vector3(_selectionStart.x, _selectionStart.y, 10f));
                Vector3 end = _mainCamera.ScreenToWorldPoint(new Vector3(_selectionEnd.x, _selectionEnd.y, 10f));
                Gizmos.DrawLine(start, end);
            }
            
            // Draw focus target
            if (_isFocusing)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_focusTarget, 1f);
            }
        }
        #endregion

        #region UI Event Handlers
        private void OnUIClick(UIClickInputEvent clickEvent)
        {
            if (!_enableUnitSelection) return;
            
            if (clickEvent.Phase == UnityEngine.InputSystem.InputActionPhase.Started)
            {
                StartSelection();
            }
            else if (clickEvent.Phase == UnityEngine.InputSystem.InputActionPhase.Canceled && _isSelecting)
            {
                EndSelection();
            }
        }


        private void OnMousePosition(UIPointInputEvent pointEvent)
        {
            _currentMousePosition = pointEvent.Position;
        }
        #endregion
    }

    #region RTS Interfaces
    /// <summary>
    /// Interface for units that can be selected
    /// </summary>
    public interface ISelectableUnit
    {
        void OnSelected();
        void OnDeselected();
        bool IsSelected { get; }
    }

    #endregion

    #region RTS Events
    public class UnitSelectedEvent
    {
        public ISelectableUnit Unit { get; }
        
        public UnitSelectedEvent(ISelectableUnit unit)
        {
            Unit = unit;
        }
    }

    public class SelectionClearedEvent
    {
        // Empty event for when selection is cleared
    }

    #endregion
}
