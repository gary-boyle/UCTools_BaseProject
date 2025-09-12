using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Profiling;
using GameFramework.UI.Utilities;
using UCTools_Utilities.UI;

namespace GameFramework.UI.Popups
{
    /// <summary>
    /// Enhanced debug popup with real-time performance graphs
    /// Displays FPS, memory consumption, and historical graphs of both metrics
    /// 
    /// Design:
    /// - Integrates custom GraphElement controls for visual data representation
    /// - Maintains separate data collection intervals for metrics vs graphs
    /// - Uses color-coded indicators for quick performance assessment
    /// - Efficient data sampling to prevent performance impact
    /// </summary>
    public class DebugPopup : UIPopup
    {
        private VisualElement _root;
        private Label _debugLabel;
        private Label _fpsLabel;
        private Label _memoryLabel;
        private Label _versionLabel;
        private Label _buildLabel;
        
        // Graph elements
        private GraphElement _fpsGraph;
        private GraphElement _memoryGraph;
        private VisualElement _fpsGraphContainer;
        private VisualElement _memoryGraphContainer;
        
        // FPS calculation variables
        private float _deltaTimeAccumulator = 0f;
        private int _frameCount = 0;
        private float _updateInterval = 0.5f;
        private float _currentFps = 0f;
        
        // Memory tracking
        private float _memoryUpdateTimer = 0f;
        private float _memoryUpdateInterval = 1f;
        private long _currentMemoryUsage = 0;
        
        // Graph update timing
        private float _graphUpdateTimer = 0f;
        private float _graphUpdateInterval = 2f; // Update graphs every 2 seconds
        
        /// <summary>
        /// Debug popup doesn't block game operations
        /// </summary>
        public override bool CountsAsGameBlockingPopup => false;

        public DebugPopup(VisualElement rootElement) : base(rootElement)
        {
            _root = rootElement;
            
            CacheUIElements();
            InitializeGraphs();
            InitializeStaticInfo();
            EnableFrameUpdates();
        }
        
        private void CacheUIElements()
        {
            _fpsLabel = _root?.Q<Label>("lbl_FPS");
            _memoryLabel = _root?.Q<Label>("lbl_Memory");
            _debugLabel = _root?.Q<Label>("lbl_Debug");
            _versionLabel = _root?.Q<Label>("lbl_Version");
            _buildLabel = _root?.Q<Label>("lbl_Build");
            
            // Cache graph containers
            _fpsGraphContainer = _root?.Q<VisualElement>("container_FPSGraph");
            _memoryGraphContainer = _root?.Q<VisualElement>("container_MemoryGraph");
            
            //UIElementValidator.ValidateElementsWithNames(this, UIElementValidator.ValidationMode.ThrowExceptions);
        }
        
        /// <summary>
        /// Initialize the graph elements and add them to their containers
        /// </summary>
        private void InitializeGraphs()
        {
            // Initialize FPS graph
            if (_fpsGraphContainer != null)
            {
                _fpsGraph = new GraphElement(60); // Store 60 data points (2 minutes at 2-second intervals)
                _fpsGraph.SetLineColor(Color.green);
                _fpsGraph.EnableAutoScale(); // Fixed scale for FPS (0-120)
                _fpsGraphContainer.Add(_fpsGraph);
            }
            
            // Initialize Memory graph
            if (_memoryGraphContainer != null)
            {
                _memoryGraph = new GraphElement(60);
                _memoryGraph.SetLineColor(Color.cyan);
                _memoryGraph.EnableAutoScale(); // Auto-scale for memory usage
                _memoryGraphContainer.Add(_memoryGraph);
            }
        }
        
        private void InitializeStaticInfo()
        {
            if (_versionLabel != null)
            {
                _versionLabel.text = $"Version: {Application.version}";
            }
            
            if (_buildLabel != null)
            {
                string buildType = Debug.isDebugBuild ? "Debug" : "Release";
                _buildLabel.text = $"Build: {buildType}";
            }
        }

        protected override void OnShow()
        {
            // Reset all counters when popup becomes visible
            ResetCounters();
        }
        
        protected override void OnHide()
        {
        }
        
        private void ResetCounters()
        {
            _deltaTimeAccumulator = 0f;
            _frameCount = 0;
            _memoryUpdateTimer = 0f;
            _graphUpdateTimer = 0f;
        }
                
        protected override void OnUpdate(float deltaTime)
        {
            UpdateFPS(deltaTime);
            UpdateMemoryUsage(deltaTime);
            UpdateGraphs(deltaTime);
        }

        private void UpdateFPS(float deltaTime)
        {
            _deltaTimeAccumulator += deltaTime;
            _frameCount++;
    
            if (_deltaTimeAccumulator >= _updateInterval)
            {
                _currentFps = _frameCount / _deltaTimeAccumulator;
        
                if (_fpsLabel != null)
                {
                    // More efficient than string interpolation, less complex than caching
                    _fpsLabel.text = "FPS: " + Mathf.RoundToInt(_currentFps).ToString();
            
                    // Color coding based on FPS ranges
                    if (_currentFps >= 50f)
                        _fpsLabel.style.color = Color.green;
                    else if (_currentFps >= 30f)
                        _fpsLabel.style.color = Color.yellow;
                    else
                        _fpsLabel.style.color = Color.red;
                }
        
                _deltaTimeAccumulator = 0f;
                _frameCount = 0;
            }
        }

        private void UpdateMemoryUsage(float deltaTime)
        {
            _memoryUpdateTimer += deltaTime;
    
            if (_memoryUpdateTimer >= _memoryUpdateInterval)
            {
                _currentMemoryUsage = Profiler.GetTotalAllocatedMemoryLong();
        
                if (_memoryLabel != null)
                {
                    float memoryMB = _currentMemoryUsage / (1024f * 1024f);
                    _memoryLabel.text = "Memory: " + memoryMB.ToString("F1") + "MB";
            
                    // Color coding for memory usage
                    if (memoryMB > 500f)
                        _memoryLabel.style.color = Color.red;
                    else if (memoryMB > 250f)
                        _memoryLabel.style.color = Color.yellow;
                    else
                        _memoryLabel.style.color = Color.green;
                }
        
                _memoryUpdateTimer = 0f;
            }
        }


        private void UpdateGraphs(float deltaTime)
        {
            _graphUpdateTimer += deltaTime;
            
            if (_graphUpdateTimer >= _graphUpdateInterval)
            {
                // Add current FPS to graph
                if (_fpsGraph != null && _currentFps > 0)
                {
                    try
                    {
                        _fpsGraph.AddDataPoint(_currentFps);
                        
                        // Update graph color based on current FPS
                        if (_currentFps >= 50f)
                            _fpsGraph.SetLineColor(Color.green);
                        else if (_currentFps >= 30f)
                            _fpsGraph.SetLineColor(Color.yellow);
                        else
                            _fpsGraph.SetLineColor(Color.red);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[DebugPopup] Error updating FPS graph: {e.Message}");
                    }
                }
                
                // Add current memory usage to graph
                if (_memoryGraph != null && _currentMemoryUsage > 0)
                {
                    try
                    {
                        float memoryMB = _currentMemoryUsage / (1024f * 1024f);
                        _memoryGraph.AddDataPoint(memoryMB);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[DebugPopup] Error updating Memory graph: {e.Message}");
                    }
                }
                
                _graphUpdateTimer = 0f;
            }
        }

        
        /// <summary>
        /// Clear all graph data (useful for testing or resetting)
        /// </summary>
        public void ClearGraphs()
        {
            _fpsGraph?.Clear();
            _memoryGraph?.Clear();
        }

        public void SetText(string text)
        {
            if (_debugLabel != null)
            {
                _debugLabel.text = text;
            }
        }

        public override void Cleanup()
        {
            // Clear graph data
            ClearGraphs();
            
            DisableFrameUpdates();
            base.Cleanup();
        }
        

    }
}
