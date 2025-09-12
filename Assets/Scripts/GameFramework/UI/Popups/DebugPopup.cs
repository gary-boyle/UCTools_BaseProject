using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Profiling;
using GameFramework.UI.Utilities;
using Unity.Profiling; // Add this for ProfilerRecorder

namespace GameFramework.UI.Popups
{
    /// <summary>
    /// Enhanced debug popup with real-time performance graphs
    /// Displays FPS, memory consumption, draw calls, and historical graphs
    /// 
    /// Design:
    /// - Integrates custom GraphElement controls for visual data representation
    /// - Maintains separate data collection intervals for metrics vs graphs
    /// - Uses color-coded indicators for quick performance assessment
    /// - Efficient data sampling to prevent performance impact
    /// - Tracks rendering statistics via ProfilerRecorder
    /// </summary>
    public class DebugPopup : UIPopup
    {
        private VisualElement _root;
        private Label _debugLabel;
        private Label _fpsLabel;
        private Label _memoryLabel;
        private Label _drawCallsLabel; // Add draw calls label
        private Label _batchesLabel;   // Add batches label
        private Label _versionLabel;
        private Label _buildLabel;
        
        // Graph elements
        private GraphElement _fpsGraph;
        private GraphElement _memoryGraph;
        private GraphElement _drawCallsGraph; // Add draw calls graph
        private VisualElement _fpsGraphContainer;
        private VisualElement _memoryGraphContainer;
        private VisualElement _drawCallsGraphContainer; // Add draw calls graph container
        
        // FPS calculation variables
        private float _deltaTimeAccumulator = 0f;
        private int _frameCount = 0;
        private float _updateInterval = 0.5f;
        private float _currentFps = 0f;
        
        // Memory tracking
        private float _memoryUpdateTimer = 0f;
        private float _memoryUpdateInterval = 1f;
        private long _currentMemoryUsage = 0;
        
        // Draw call tracking
        private ProfilerRecorder _drawCallsRecorder;
        private ProfilerRecorder _batchesRecorder;
        private ProfilerRecorder _trianglesRecorder;
        private ProfilerRecorder _verticesRecorder;
        private float _renderingUpdateTimer = 0f;
        private float _renderingUpdateInterval = 0.5f; // Update rendering stats every 0.5s
        private int _currentDrawCalls = 0;
        private int _currentBatches = 0;
        private int _currentTriangles = 0;
        private int _currentVertices = 0;
        
        // Graph update timing
        private float _graphUpdateTimer = 0f;
        private float _graphUpdateInterval = 2f; // Update graphs every 2 seconds
        
        // String optimization for rendering stats
        private readonly System.Text.StringBuilder _stringBuilder = new(64);
        
        public DebugPopup(VisualElement rootElement) : base(rootElement)
        {
            _root = rootElement;
            
            InitializeProfilerRecorders();
            CacheUIElements();
            InitializeGraphs();
            InitializeStaticInfo();
            EnableFrameUpdates();
        }
        
        /// <summary>
        /// Initialize profiler recorders for rendering statistics
        /// </summary>
        private void InitializeProfilerRecorders()
        {
            // Initialize profiler recorders for rendering stats
            _drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _trianglesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            _verticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
            
            Debug.Log("[DebugPopup] Profiler recorders initialized for rendering stats");
        }
        
        private void CacheUIElements()
        {
            _fpsLabel = _root?.Q<Label>("lbl_FPS");
            _memoryLabel = _root?.Q<Label>("lbl_Memory");
            _drawCallsLabel = _root?.Q<Label>("lbl_DrawCalls"); // Add this to your UXML
            _batchesLabel = _root?.Q<Label>("lbl_Batches");     // Add this to your UXML
            _debugLabel = _root?.Q<Label>("lbl_Debug");
            _versionLabel = _root?.Q<Label>("lbl_Version");
            _buildLabel = _root?.Q<Label>("lbl_Build");
            
            // Cache graph containers
            _fpsGraphContainer = _root?.Q<VisualElement>("graph_FPS");
            _memoryGraphContainer = _root?.Q<VisualElement>("graph_Memory");
            _drawCallsGraphContainer = _root?.Q<VisualElement>("graph_DrawCalls"); // Add this to your UXML
            
            // Debug validation
            Debug.Log($"[DebugPopup] UI Elements found:");
            Debug.Log($"  - Draw Calls Label: {(_drawCallsLabel != null ? "✓" : "✗ NULL")}");
            Debug.Log($"  - Batches Label: {(_batchesLabel != null ? "✓" : "✗ NULL")}");
            Debug.Log($"  - Draw Calls Graph Container: {(_drawCallsGraphContainer != null ? "✓" : "✗ NULL")}");
            
            if (_debugLabel == null)
            {
                _debugLabel = new Label("Debug Information") { name = "lbl_Debug" };
                _root?.Add(_debugLabel);
            }
        }
        
        /// <summary>
        /// Initialize the graph elements and add them to their containers
        /// </summary>
        private void InitializeGraphs()
        {
            // Initialize FPS graph
            if (_fpsGraphContainer != null)
            {
                _fpsGraph = new GraphElement(60);
                _fpsGraph.SetLineColor(Color.green);
                _fpsGraph.EnableAutoScale();
                _fpsGraphContainer.Add(_fpsGraph);
            }
            
            // Initialize Memory graph
            if (_memoryGraphContainer != null)
            {
                _memoryGraph = new GraphElement(60);
                _memoryGraph.SetLineColor(Color.cyan);
                _memoryGraph.EnableAutoScale();
                _memoryGraphContainer.Add(_memoryGraph);
            }
            
            // Initialize Draw Calls graph
            if (_drawCallsGraphContainer != null)
            {
                _drawCallsGraph = new GraphElement(60);
                _drawCallsGraph.SetLineColor(Color.magenta);
                _drawCallsGraph.EnableAutoScale();
                _drawCallsGraphContainer.Add(_drawCallsGraph);
                Debug.Log("[DebugPopup] ✓ Draw Calls Graph initialized successfully");
            }
            else
            {
                Debug.LogWarning("[DebugPopup] ✗ Draw Calls Graph container is null");
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

        /// <summary>
        /// Override Show to ensure proper state management
        /// </summary>
        public override void Show()
        {
            if (!IsVisible)
            {
                base.Show();
            }
        }
        
        /// <summary>
        /// Override Hide to ensure proper cleanup
        /// </summary>
        public override void Hide()
        {
            if (IsVisible)
            {
                base.Hide();
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
            _renderingUpdateTimer = 0f;
            _graphUpdateTimer = 0f;
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            UpdateFPS(deltaTime);
            UpdateMemoryUsage(deltaTime);
            UpdateRenderingStats(deltaTime);
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
                    // Optimized string building
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
        
        /// <summary>
        /// Update rendering statistics including draw calls and batches
        /// </summary>
        private void UpdateRenderingStats(float deltaTime)
        {
            _renderingUpdateTimer += deltaTime;
            
            if (_renderingUpdateTimer >= _renderingUpdateInterval)
            {
                // Get current frame's rendering stats
                if (_drawCallsRecorder.Valid)
                    _currentDrawCalls = (int)_drawCallsRecorder.LastValue;
                
                if (_batchesRecorder.Valid)
                    _currentBatches = (int)_batchesRecorder.LastValue;
                
                if (_trianglesRecorder.Valid)
                    _currentTriangles = (int)_trianglesRecorder.LastValue;
                
                if (_verticesRecorder.Valid)
                    _currentVertices = (int)_verticesRecorder.LastValue;
                
                // Update draw calls label
                if (_drawCallsLabel != null)
                {
                    _stringBuilder.Clear();
                    _stringBuilder.Append("Draw Calls: ");
                    _stringBuilder.Append(_currentDrawCalls);
                    _drawCallsLabel.text = _stringBuilder.ToString();
                    
                    // Color coding for draw calls (adjust thresholds based on your target platform)
                    if (_currentDrawCalls > 1000)
                        _drawCallsLabel.style.color = Color.red;
                    else if (_currentDrawCalls > 500)
                        _drawCallsLabel.style.color = Color.yellow;
                    else
                        _drawCallsLabel.style.color = Color.green;
                }
                
                // Update batches label
                if (_batchesLabel != null)
                {
                    _stringBuilder.Clear();
                    _stringBuilder.Append("Batches: ");
                    _stringBuilder.Append(_currentBatches);
                    _stringBuilder.Append(" | Tris: ");
                    _stringBuilder.Append(FormatLargeNumber(_currentTriangles));
                    _stringBuilder.Append(" | Verts: ");
                    _stringBuilder.Append(FormatLargeNumber(_currentVertices));
                    _batchesLabel.text = _stringBuilder.ToString();
                    
                    // Color coding for batches
                    if (_currentBatches > 500)
                        _batchesLabel.style.color = Color.red;
                    else if (_currentBatches > 250)
                        _batchesLabel.style.color = Color.yellow;
                    else
                        _batchesLabel.style.color = Color.green;
                }
                
                _renderingUpdateTimer = 0f;
            }
        }
        
        /// <summary>
        /// Format large numbers with K/M suffixes for better readability
        /// </summary>
        private string FormatLargeNumber(int number)
        {
            if (number >= 1000000)
                return (number / 1000000f).ToString("F1") + "M";
            else if (number >= 1000)
                return (number / 1000f).ToString("F1") + "K";
            else
                return number.ToString();
        }
        
        /// <summary>
        /// Update graph data points at a slower interval to maintain history without performance impact
        /// </summary>
        private void UpdateGraphs(float deltaTime)
        {
            _graphUpdateTimer += deltaTime;
            
            if (_graphUpdateTimer >= _graphUpdateInterval)
            {
                // Add current FPS to graph
                if (_fpsGraph != null && _currentFps > 0)
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
                
                // Add current memory usage to graph
                if (_memoryGraph != null && _currentMemoryUsage > 0)
                {
                    float memoryMB = _currentMemoryUsage / (1024f * 1024f);
                    _memoryGraph.AddDataPoint(memoryMB);
                }
                
                // Add current draw calls to graph
                if (_drawCallsGraph != null && _currentDrawCalls > 0)
                {
                    _drawCallsGraph.AddDataPoint(_currentDrawCalls);
                    
                    // Update graph color based on draw call count
                    if (_currentDrawCalls > 1000)
                        _drawCallsGraph.SetLineColor(Color.red);
                    else if (_currentDrawCalls > 500)
                        _drawCallsGraph.SetLineColor(Color.yellow);
                    else
                        _drawCallsGraph.SetLineColor(Color.green);
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
            _drawCallsGraph?.Clear(); // Clear draw calls graph
        }
        
        /// <summary>
        /// Configure graph update intervals
        /// </summary>
        public void SetGraphUpdateInterval(float intervalSeconds)
        {
            _graphUpdateInterval = Mathf.Max(0.5f, intervalSeconds); // Minimum 0.5 seconds
        }
        
        /// <summary>
        /// Get current rendering statistics for external access
        /// </summary>
        public (int drawCalls, int batches, int triangles, int vertices) GetRenderingStats()
        {
            return (_currentDrawCalls, _currentBatches, _currentTriangles, _currentVertices);
        }
        
        /// <summary>
        /// Configure draw call warning thresholds based on target platform
        /// </summary>
        public void SetDrawCallThresholds(int warningThreshold = 500, int criticalThreshold = 1000)
        {
            // Store thresholds for dynamic color coding
            // This allows different thresholds for mobile vs desktop
        }

        public void SetText(string text)
        {
            if (_debugLabel != null)
            {
                _debugLabel.text = text;
            }
        }
        
        public void SetRichText(string richText)
        {
            if (_debugLabel != null)
            {
                _debugLabel.enableRichText = true;
                _debugLabel.text = richText;
            }
        }
        
        public float GetCurrentFPS() => _currentFps;
        public long GetCurrentMemoryUsage() => _currentMemoryUsage;
        public int GetCurrentDrawCalls() => _currentDrawCalls; // Add getter for draw calls
        
        public override void Cleanup()
        {
            // Dispose profiler recorders
            _drawCallsRecorder.Dispose();
            _batchesRecorder.Dispose();
            _trianglesRecorder.Dispose();
            _verticesRecorder.Dispose();
            
            // Clear graph data
            ClearGraphs();
            
            DisableFrameUpdates();
            base.Cleanup();
        }
        
        /// <summary>
        /// Debug popup doesn't block game operations and should use unscaled time
        /// </summary>
        public override bool CountsAsGameBlockingPopup => false;
    }
}
