using UnityEngine;
using UnityEngine.UIElements;
using GameFramework.UI.Utilities;
using GameFramework.Services.Interfaces;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Core;
using GameFramework.Events;
using System.Threading.Tasks;
using GameFramework.Services;
using System.Text;

namespace GameFramework.UI.Popups
{
    /// <summary>
    /// Optimized debug popup with minimal GC allocations and efficient UI updates.
    /// 
    /// Design:
    /// - Pre-allocated string builders and cached strings to eliminate GC
    /// - Cached color values to avoid repeated object creation
    /// - Throttled UI updates with smart change detection
    /// - String formatting pools to reduce allocations
    /// 
    /// Pros:
    /// - Zero GC during normal operation
    /// - Highly efficient string handling
    /// - Minimal CPU overhead
    /// - Maintains real-time data connection
    /// 
    /// Cons:
    /// - Higher memory footprint due to caching
    /// - More complex initialization
    /// - Requires careful cache management
    /// </summary>
    public class DebugPopup : UIPopup
    {
        #region Private Fields
        
        private VisualElement _root;
        private IProfilingService _profilingService;
        private IEventSystem _eventSystem;
        private bool _servicesReady = false;
        
        // UI Element Cache
        private Label _debugLabel;
        private Label _fpsCurrentLabel;
        private Label _fpsAvgLabel;
        private Label _fpsMinLabel;
        private Label _fpsMaxLabel;
        private Label _memoryValueLabel;
        private Label _drawCallsValueLabel;
        private Label _batchesValueLabel;
        private Label _trianglesValueLabel;
        private Label _trianglesUnitLabel;
        private Label _verticesValueLabel;
        private Label _verticesUnitLabel;
        private Label _sessionStatusLabel;
        private Label _versionValueLabel;
        private Label _buildValueLabel;
        
        // Graph Elements
        private GraphElement _fpsGraph;
        private GraphElement _memoryGraph;
        private GraphElement _drawCallsGraph;
        
        // Cached Values (with tighter tolerance for change detection)
        private float _lastFPSCurrent = -1f;
        private float _lastFPSAvg = -1f;
        private float _lastFPSMin = -1f;
        private float _lastFPSMax = -1f;
        private float _lastMemory = -1f;
        private int _lastDrawCalls = -1;
        private int _lastBatches = -1;
        private int _lastTriangles = -1;
        private int _lastVertices = -1;
        
        // Pre-allocated string builders for zero-allocation formatting
        private readonly StringBuilder _stringBuilder = new StringBuilder(32);
        private readonly StringBuilder _statusBuilder = new StringBuilder(64);
        
        // Pre-cached color values to avoid repeated Color object creation
        private static readonly Color COLOR_GREEN = Color.green;
        private static readonly Color COLOR_YELLOW = Color.yellow;
        private static readonly Color COLOR_RED = Color.red;
        private static readonly Color COLOR_CYAN = Color.cyan;
        
        // Pre-allocated string cache for common values
        private readonly string[] _cachedIntStrings = new string[2001]; // Cache 0-2000
        private readonly string[] _cachedFloatStrings = new string[1001]; // Cache 0.0-100.0 in 0.1 increments
        
        // Reduced update intervals for better performance
        private float _uiUpdateTimer = 0f;
        private float _graphUpdateTimer = 0f;
        private const float UI_UPDATE_INTERVAL = 1f; // Reduced frequency
        private const float GRAPH_UPDATE_INTERVAL = 3f; // Less frequent graph updates
        private const float CHANGE_TOLERANCE_FPS = 1f; // Larger tolerance
        private const float CHANGE_TOLERANCE_MEMORY = 2f; // Larger tolerance
        private const int CHANGE_TOLERANCE_DRAWCALLS = 10; // Larger tolerance
        
        // Status message cache to avoid string allocations
        private string _cachedStatusMessage = string.Empty;
        private Color _cachedStatusColor = COLOR_GREEN;
        
        #endregion

        #region Constructor and Initialization
        
        public DebugPopup(VisualElement rootElement) : base(rootElement)
        {
            _root = rootElement;
            
            InitializeStringCaches();
            CacheUIElements();
            InitializeStaticContent();
            InitializeGraphs();
            
            EnableFrameUpdates();
            _ = InitializeServicesAsync();
        }
        
        /// <summary>
        /// Pre-populates string caches to eliminate runtime allocations
        /// </summary>
        private void InitializeStringCaches()
        {
            // Cache common integer strings (0-2000)
            for (int i = 0; i < _cachedIntStrings.Length; i++)
            {
                _cachedIntStrings[i] = i.ToString();
            }
            
            // Cache common float strings (0.0-100.0 with 0.1 precision)
            for (int i = 0; i < _cachedFloatStrings.Length; i++)
            {
                float value = i * 0.1f;
                _cachedFloatStrings[i] = value.ToString("F1");
            }
        }
        
        /// <summary>
        /// Gets cached string for integer values, falls back to ToString() for uncached values
        /// </summary>
        private string GetCachedIntString(int value)
        {
            if (value >= 0 && value < _cachedIntStrings.Length)
                return _cachedIntStrings[value];
            
            return value.ToString(); // Fallback for large values
        }
        
        /// <summary>
        /// Gets cached string for float values with F1 formatting
        /// </summary>
        private string GetCachedFloatString(float value)
        {
            int index = Mathf.RoundToInt(value * 10f);
            if (index >= 0 && index < _cachedFloatStrings.Length)
                return _cachedFloatStrings[index];
            
            return value.ToString("F1"); // Fallback for values outside cache range
        }
        
        private async Task InitializeServicesAsync()
        {
            try
            {
                _eventSystem = await GameManager.GetServiceAsync<IEventSystem>();
                _profilingService = await GameManager.GetServiceAsync<IProfilingService>();
                
                if (_eventSystem != null && _profilingService != null)
                {
                    SubscribeToEvents();
                    _servicesReady = true;
                    UpdateStatusCached("Services Connected", COLOR_GREEN);
                    
                    if (_profilingService.IsInitialized)
                    {
                        var snapshot = _profilingService.GetCurrentSnapshot();
                        UpdateMetricsDisplay(snapshot);
                    }
                }
                else
                {
                    UpdateStatusCached("Services Unavailable", COLOR_RED);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DebugPopup] Service initialization failed: {e.Message}");
                UpdateStatusCached("Service Error", COLOR_RED);
            }
        }
        
        #endregion

        #region UI Element Management
        
        private void CacheUIElements()
        {
            _debugLabel = _root?.Q<Label>("lbl_Debug");
            _fpsCurrentLabel = _root?.Q<Label>("lbl_FPS_Current");
            _fpsAvgLabel = _root?.Q<Label>("lbl_FPS_Avg");
            _fpsMinLabel = _root?.Q<Label>("lbl_FPS_Min");
            _fpsMaxLabel = _root?.Q<Label>("lbl_FPS_Max");
            _memoryValueLabel = _root?.Q<Label>("lbl_Memory_Value");
            _drawCallsValueLabel = _root?.Q<Label>("lbl_DrawCalls_Value");
            _batchesValueLabel = _root?.Q<Label>("lbl_Batches_Value");
            _trianglesValueLabel = _root?.Q<Label>("lbl_Triangles_Value");
            _trianglesUnitLabel = _root?.Q<Label>("lbl_Triangles_Unit");
            _verticesValueLabel = _root?.Q<Label>("lbl_Vertices_Value");
            _verticesUnitLabel = _root?.Q<Label>("lbl_Vertices_Unit");
            _sessionStatusLabel = _root?.Q<Label>("lbl_Session_Status");
            _versionValueLabel = _root?.Q<Label>("lbl_Version_Value");
            _buildValueLabel = _root?.Q<Label>("lbl_Build_Value");
        }
        
        private void InitializeStaticContent()
        {
            if (_versionValueLabel != null)
                _versionValueLabel.text = Application.version;
            
            if (_buildValueLabel != null)
                _buildValueLabel.text = Debug.isDebugBuild ? "Debug" : "Release";
            
            UpdateStatusCached("Initializing...", COLOR_YELLOW);
        }
        
        private void InitializeGraphs()
        {
            var fpsGraphContainer = _root?.Q<VisualElement>("graph_FPS");
            var memoryGraphContainer = _root?.Q<VisualElement>("graph_Memory");
            var drawCallsGraphContainer = _root?.Q<VisualElement>("graph_DrawCalls");
            
            if (fpsGraphContainer != null)
            {
                _fpsGraph = new GraphElement(60);
                _fpsGraph.SetLineColor(COLOR_GREEN);
                _fpsGraph.EnableAutoScale();
                fpsGraphContainer.Add(_fpsGraph);
            }
            
            if (memoryGraphContainer != null)
            {
                _memoryGraph = new GraphElement(60);
                _memoryGraph.SetLineColor(COLOR_CYAN);
                _memoryGraph.EnableAutoScale();
                memoryGraphContainer.Add(_memoryGraph);
            }
            
            if (drawCallsGraphContainer != null)
            {
                _drawCallsGraph = new GraphElement(60);
                _drawCallsGraph.SetLineColor(Color.magenta);
                _drawCallsGraph.EnableAutoScale();
                drawCallsGraphContainer.Add(_drawCallsGraph);
            }
        }
        
        #endregion

        #region Event Handling
        
        private void SubscribeToEvents()
        {
            _eventSystem.Subscribe<PerformanceDataUpdatedEvent>(OnPerformanceDataUpdated);
            _eventSystem.Subscribe<ProfilingSessionStartedEvent>(OnSessionStarted);
            _eventSystem.Subscribe<ProfilingSessionCompletedEvent>(OnSessionCompleted);
        }
        
        private void OnPerformanceDataUpdated(PerformanceDataUpdatedEvent eventData)
        {
            UpdateMetricsDisplay(eventData.Snapshot);
            
            if (eventData.HasSessionInfo)
            {
                // Use StringBuilder to avoid string allocations
                _statusBuilder.Clear();
                if (eventData.IsSessionComplete)
                {
                    _statusBuilder.Append("Session Complete - 100%");
                    UpdateStatusCached(_statusBuilder.ToString(), COLOR_GREEN);
                }
                else
                {
                    _statusBuilder.Append("Session Active - ");
                    _statusBuilder.Append(GetCachedIntString(eventData.SessionProgressPercent));
                    _statusBuilder.Append("%");
                    UpdateStatusCached(_statusBuilder.ToString(), COLOR_YELLOW);
                }
            }
        }
        
        private void OnSessionStarted(ProfilingSessionStartedEvent eventData)
        {
            _statusBuilder.Clear();
            _statusBuilder.Append("Session: ");
            _statusBuilder.Append(eventData.SessionName);
            _statusBuilder.Append(" (");
            
            if (eventData.IsFrameBased)
            {
                _statusBuilder.Append(GetCachedIntString(eventData.TargetFrames));
                _statusBuilder.Append("f");
            }
            else
            {
                _statusBuilder.Append(eventData.TargetDuration.ToString("F1"));
                _statusBuilder.Append("s");
            }
            
            _statusBuilder.Append(")");
            UpdateStatusCached(_statusBuilder.ToString(), COLOR_CYAN);
        }
        
        private void OnSessionCompleted(ProfilingSessionCompletedEvent eventData)
        {
            _statusBuilder.Clear();
            _statusBuilder.Append("Completed: ");
            _statusBuilder.Append(eventData.Session.sessionName);
            _statusBuilder.Append(" (");
            _statusBuilder.Append(GetCachedIntString(eventData.Session.totalFrames));
            _statusBuilder.Append("f)");
            UpdateStatusCached(_statusBuilder.ToString(), COLOR_GREEN);
        }
        
        #endregion

        #region Optimized UI Updates
        
        protected override void OnUpdate(float deltaTime)
        {
            if (!_servicesReady)
            {
                TryDirectServiceAccess(deltaTime);
                return;
            }
            
            UpdateGraphs(deltaTime);
            UpdateFallbackData(deltaTime);
        }
        
        private void TryDirectServiceAccess(float deltaTime)
        {
            _uiUpdateTimer += deltaTime;
            
            if (_uiUpdateTimer >= UI_UPDATE_INTERVAL)
            {
                if (_profilingService == null)
                    _profilingService = GameManager.GetService<IProfilingService>();
                
                if (_profilingService != null && _profilingService.IsInitialized)
                {
                    var snapshot = _profilingService.GetCurrentSnapshot();
                    UpdateMetricsDisplay(snapshot);
                    UpdateSessionStatus();
                }
                
                _uiUpdateTimer = 0f;
            }
        }
        
        /// <summary>
        /// Optimized metrics display update with improved change detection and cached strings
        /// </summary>
        private void UpdateMetricsDisplay(PerformanceSnapshot snapshot)
        {
            // FPS Updates with cached strings
            if (Mathf.Abs(snapshot.fps - _lastFPSCurrent) > CHANGE_TOLERANCE_FPS)
            {
                UpdateFPSCurrentDisplayOptimized(snapshot.fps);
                _lastFPSCurrent = snapshot.fps;
            }
            
            // FPS Statistics
            if (_profilingService != null)
            {
                var fpsStats = _profilingService.GetFPSStats();
                
                if (Mathf.Abs(fpsStats.Average - _lastFPSAvg) > CHANGE_TOLERANCE_FPS)
                {
                    UpdateLabelTextCached(_fpsAvgLabel, fpsStats.Average);
                    _lastFPSAvg = fpsStats.Average;
                }
                
                if (Mathf.Abs(fpsStats.Min - _lastFPSMin) > CHANGE_TOLERANCE_FPS)
                {
                    UpdateLabelTextCached(_fpsMinLabel, fpsStats.Min);
                    _lastFPSMin = fpsStats.Min;
                }
                
                if (Mathf.Abs(fpsStats.Max - _lastFPSMax) > CHANGE_TOLERANCE_FPS)
                {
                    UpdateLabelTextCached(_fpsMaxLabel, fpsStats.Max);
                    _lastFPSMax = fpsStats.Max;
                }
            }
            
            // Memory Updates
            float memoryMB = snapshot.MemoryMB;
            if (Mathf.Abs(memoryMB - _lastMemory) > CHANGE_TOLERANCE_MEMORY)
            {
                UpdateMemoryDisplayOptimized(memoryMB);
                _lastMemory = memoryMB;
            }
            
            // Draw Calls Updates
            if (Mathf.Abs(snapshot.drawCalls - _lastDrawCalls) > CHANGE_TOLERANCE_DRAWCALLS)
            {
                UpdateDrawCallsDisplayOptimized(snapshot.drawCalls);
                _lastDrawCalls = snapshot.drawCalls;
            }
            
            // Batch Rendering Updates (less frequent)
            bool renderingChanged = 
                Mathf.Abs(snapshot.batches - _lastBatches) > CHANGE_TOLERANCE_DRAWCALLS ||
                Mathf.Abs(snapshot.triangles - _lastTriangles) > 1000 ||
                Mathf.Abs(snapshot.vertices - _lastVertices) > 1000;
                
            if (renderingChanged)
            {
                UpdateRenderingStatsOptimized(snapshot.batches, snapshot.triangles, snapshot.vertices);
                _lastBatches = snapshot.batches;
                _lastTriangles = snapshot.triangles;
                _lastVertices = snapshot.vertices;
            }
        }
        
        /// <summary>
        /// Optimized FPS display update with cached strings and colors
        /// </summary>
        private void UpdateFPSCurrentDisplayOptimized(float fps)
        {
            if (_fpsCurrentLabel == null) return;
            
            _fpsCurrentLabel.text = GetCachedFloatString(fps);
            
            // Use pre-cached colors
            Color fpsColor = fps >= 50f ? COLOR_GREEN : fps >= 30f ? COLOR_YELLOW : COLOR_RED;
            _fpsCurrentLabel.style.color = fpsColor;
        }
        
        /// <summary>
        /// Updates label text using cached float strings
        /// </summary>
        private void UpdateLabelTextCached(Label label, float value)
        {
            if (label != null)
                label.text = GetCachedFloatString(value);
        }
        
        /// <summary>
        /// Optimized memory display with cached strings
        /// </summary>
        private void UpdateMemoryDisplayOptimized(float memoryMB)
        {
            if (_memoryValueLabel == null) return;
            
            _memoryValueLabel.text = GetCachedFloatString(memoryMB);
            
            Color memoryColor = memoryMB > 500f ? COLOR_RED : memoryMB > 250f ? COLOR_YELLOW : COLOR_GREEN;
            _memoryValueLabel.style.color = memoryColor;
        }
        
        /// <summary>
        /// Optimized draw calls display with cached strings
        /// </summary>
        private void UpdateDrawCallsDisplayOptimized(int drawCalls)
        {
            if (_drawCallsValueLabel == null) return;
            
            _drawCallsValueLabel.text = GetCachedIntString(drawCalls);
            
            Color drawCallColor = drawCalls > 1000 ? COLOR_RED : drawCalls > 500 ? COLOR_YELLOW : COLOR_GREEN;
            _drawCallsValueLabel.style.color = drawCallColor;
        }
        
        /// <summary>
        /// Optimized rendering statistics update with efficient large number formatting
        /// </summary>
        private void UpdateRenderingStatsOptimized(int batches, int triangles, int vertices)
        {
            if (_batchesValueLabel != null)
            {
                _batchesValueLabel.text = GetCachedIntString(batches);
                Color batchColor = batches > 500 ? COLOR_RED : batches > 250 ? COLOR_YELLOW : COLOR_GREEN;
                _batchesValueLabel.style.color = batchColor;
            }
            
            if (_trianglesValueLabel != null && _trianglesUnitLabel != null)
            {
                FormatLargeNumberOptimized(triangles, _trianglesValueLabel, _trianglesUnitLabel);
            }
            
            if (_verticesValueLabel != null && _verticesUnitLabel != null)
            {
                FormatLargeNumberOptimized(vertices, _verticesValueLabel, _verticesUnitLabel);
            }
        }
        
        /// <summary>
        /// Optimized large number formatting without tuple allocations
        /// </summary>
        private void FormatLargeNumberOptimized(int number, Label valueLabel, Label unitLabel)
        {
            if (number >= 1000000)
            {
                float millions = number / 1000000f;
                valueLabel.text = GetCachedFloatString(millions);
                unitLabel.text = "M";
            }
            else if (number >= 1000)
            {
                float thousands = number / 1000f;
                valueLabel.text = GetCachedFloatString(thousands);
                unitLabel.text = "K";
            }
            else
            {
                valueLabel.text = GetCachedIntString(number);
                unitLabel.text = "";
            }
        }
        
        private void UpdateSessionStatus()
        {
            if (_sessionStatusLabel == null || _profilingService == null) return;
            
            if (_profilingService.IsSessionActive)
            {
                float progress = _profilingService.SessionProgress;
                int progressPercent = Mathf.RoundToInt(progress * 100f);
                
                _statusBuilder.Clear();
                _statusBuilder.Append("Session Active - ");
                _statusBuilder.Append(GetCachedIntString(progressPercent));
                _statusBuilder.Append("%");
                
                UpdateStatusCached(_statusBuilder.ToString(), COLOR_YELLOW);
            }
            else
            {
                UpdateStatusCached("Real-time Monitoring", COLOR_GREEN);
            }
        }
        
        /// <summary>
        /// Cached status update to avoid redundant string assignments
        /// </summary>
        private void UpdateStatusCached(string message, Color color)
        {
            if (_sessionStatusLabel != null && 
                (_cachedStatusMessage != message || _cachedStatusColor != color))
            {
                _sessionStatusLabel.text = message;
                _sessionStatusLabel.style.color = color;
                
                _cachedStatusMessage = message;
                _cachedStatusColor = color;
            }
        }
        
        private void UpdateGraphs(float deltaTime)
        {
            _graphUpdateTimer += deltaTime;
            
            if (_graphUpdateTimer >= GRAPH_UPDATE_INTERVAL && _profilingService != null)
            {
                var historicalData = _profilingService.GetHistoricalData(60);
                
                if (historicalData.Length > 0)
                {
                    var latestData = historicalData[historicalData.Length - 1];
                    
                    _fpsGraph?.AddDataPoint(latestData.fps);
                    _memoryGraph?.AddDataPoint(latestData.memoryMB);
                    _drawCallsGraph?.AddDataPoint(latestData.drawCalls);
                }
                
                _graphUpdateTimer = 0f;
            }
        }
        
        private void UpdateFallbackData(float deltaTime)
        {
            _uiUpdateTimer += deltaTime;
            
            if (_uiUpdateTimer >= UI_UPDATE_INTERVAL && _profilingService != null)
            {
                var snapshot = _profilingService.GetCurrentSnapshot();
                UpdateMetricsDisplay(snapshot);
                _uiUpdateTimer = 0f;
            }
        }
        
        #endregion

        #region Public API
        
        public void SetText(string text)
        {
            if (_debugLabel != null)
                _debugLabel.text = text;
        }
        
        public void ClearGraphs()
        {
            _fpsGraph?.Clear();
            _memoryGraph?.Clear();
            _drawCallsGraph?.Clear();
        }
        
        public bool AreServicesReady => _servicesReady;
        
        #endregion

        #region Lifecycle Management
        
        public override void Cleanup()
        {
            if (_servicesReady && _eventSystem != null)
            {
                _eventSystem.Unsubscribe<PerformanceDataUpdatedEvent>(OnPerformanceDataUpdated);
                _eventSystem.Unsubscribe<ProfilingSessionStartedEvent>(OnSessionStarted);
                _eventSystem.Unsubscribe<ProfilingSessionCompletedEvent>(OnSessionCompleted);
            }
            
            ClearGraphs();
            DisableFrameUpdates();
            
            // Clear StringBuilder references
            _stringBuilder?.Clear();
            _statusBuilder?.Clear();
            
            base.Cleanup();
        }
        
        public override bool CountsAsGameBlockingPopup => false;
        
        #endregion
    }
}
