using UnityEngine;
using UnityEngine.UIElements;
using GameFramework.UI.Utilities;
using GameFramework.Services.Interfaces;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Core;
using GameFramework.Events;
using System.Threading.Tasks;
using GameFramework.Services;

namespace GameFramework.UI.Popups
{
    /// <summary>
    /// Simplified debug popup that displays real-time performance metrics with minimal allocations.
    /// 
    /// Design:
    /// - Clean separation of UI element caching and updates
    /// - Event-driven updates to minimize polling
    /// - Efficient change detection to prevent redundant UI updates
    /// - Simple graph integration for historical data visualization
    /// 
    /// Pros:
    /// - Zero-allocation UI updates during normal operation
    /// - Event-driven architecture reduces CPU overhead
    /// - Clear separation of concerns
    /// - Robust service connection handling
    /// 
    /// Cons:
    /// - Requires UXML structure to match expected element names
    /// - Dependency on EventSystem for real-time updates
    /// </summary>
    public class DebugPopup : UIPopup
    {
        #region Private Fields
        
        private VisualElement _root;
        private IProfilingService _profilingService;
        private IEventSystem _eventSystem;
        private bool _servicesReady = false;
        
        // UI Element Cache - matching your UXML structure
        private Label _debugLabel;
        
        // FPS Labels
        private Label _fpsCurrentLabel;
        private Label _fpsAvgLabel;
        private Label _fpsMinLabel;
        private Label _fpsMaxLabel;
        
        // Other Metric Labels
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
        
        // Cached Values (for change detection)
        private float _lastFPSCurrent = -1f;
        private float _lastFPSAvg = -1f;
        private float _lastFPSMin = -1f;
        private float _lastFPSMax = -1f;
        private float _lastMemory = -1f;
        private int _lastDrawCalls = -1;
        private int _lastBatches = -1;
        private int _lastTriangles = -1;
        private int _lastVertices = -1;
        
        // Update Timers
        private float _uiUpdateTimer = 0f;
        private float _graphUpdateTimer = 0f;
        private const float UI_UPDATE_INTERVAL = 0.5f;
        private const float GRAPH_UPDATE_INTERVAL = 2f;
        
        #endregion

        #region Constructor and Initialization
        
        public DebugPopup(VisualElement rootElement) : base(rootElement)
        {
            _root = rootElement;
            
            CacheUIElements();
            InitializeStaticContent();
            InitializeGraphs();
            
            EnableFrameUpdates();
            _ = InitializeServicesAsync();
        }
        
        /// <summary>
        /// Asynchronously initializes service connections
        /// </summary>
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
                    UpdateStatus("Services Connected", Color.green);
                    
                    // Initial data load
                    if (_profilingService.IsInitialized)
                    {
                        var snapshot = _profilingService.GetCurrentSnapshot();
                        UpdateMetricsDisplay(snapshot);
                    }
                }
                else
                {
                    UpdateStatus("Services Unavailable", Color.red);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DebugPopup] Service initialization failed: {e.Message}");
                UpdateStatus("Service Error", Color.red);
            }
        }
        
        #endregion

        #region UI Element Management
        
        /// <summary>
        /// Caches UI elements for efficient access - matches your UXML structure
        /// </summary>
        private void CacheUIElements()
        {
            _debugLabel = _root?.Q<Label>("lbl_Debug");
            
            // FPS labels
            _fpsCurrentLabel = _root?.Q<Label>("lbl_FPS_Current");
            _fpsAvgLabel = _root?.Q<Label>("lbl_FPS_Avg");
            _fpsMinLabel = _root?.Q<Label>("lbl_FPS_Min");
            _fpsMaxLabel = _root?.Q<Label>("lbl_FPS_Max");
            
            // Memory labels
            _memoryValueLabel = _root?.Q<Label>("lbl_Memory_Value");
            
            // Draw calls labels
            _drawCallsValueLabel = _root?.Q<Label>("lbl_DrawCalls_Value");
            
            // Rendering details labels
            _batchesValueLabel = _root?.Q<Label>("lbl_Batches_Value");
            _trianglesValueLabel = _root?.Q<Label>("lbl_Triangles_Value");
            _trianglesUnitLabel = _root?.Q<Label>("lbl_Triangles_Unit");
            _verticesValueLabel = _root?.Q<Label>("lbl_Vertices_Value");
            _verticesUnitLabel = _root?.Q<Label>("lbl_Vertices_Unit");
            
            // Status and version labels
            _sessionStatusLabel = _root?.Q<Label>("lbl_Session_Status");
            _versionValueLabel = _root?.Q<Label>("lbl_Version_Value");
            _buildValueLabel = _root?.Q<Label>("lbl_Build_Value");
            
            // Debug: Log which elements were found
            Debug.Log($"[DebugPopup] Cached elements - FPS Current: {_fpsCurrentLabel != null}, Memory: {_memoryValueLabel != null}, Draw Calls: {_drawCallsValueLabel != null}, Status: {_sessionStatusLabel != null}");
        }
        
        /// <summary>
        /// Initializes static content that doesn't change
        /// </summary>
        private void InitializeStaticContent()
        {
            if (_versionValueLabel != null)
                _versionValueLabel.text = Application.version;
            
            if (_buildValueLabel != null)
                _buildValueLabel.text = Debug.isDebugBuild ? "Debug" : "Release";
            
            UpdateStatus("Initializing...", Color.yellow);
        }
        
        /// <summary>
        /// Initializes performance graphs
        /// </summary>
        private void InitializeGraphs()
        {
            var fpsGraphContainer = _root?.Q<VisualElement>("graph_FPS");
            var memoryGraphContainer = _root?.Q<VisualElement>("graph_Memory");
            var drawCallsGraphContainer = _root?.Q<VisualElement>("graph_DrawCalls");
            
            if (fpsGraphContainer != null)
            {
                _fpsGraph = new GraphElement(60);
                _fpsGraph.SetLineColor(Color.green);
                _fpsGraph.EnableAutoScale();
                fpsGraphContainer.Add(_fpsGraph);
            }
            
            if (memoryGraphContainer != null)
            {
                _memoryGraph = new GraphElement(60);
                _memoryGraph.SetLineColor(Color.cyan);
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
        
        /// <summary>
        /// Subscribes to profiling service events
        /// </summary>
        private void SubscribeToEvents()
        {
            _eventSystem.Subscribe<PerformanceDataUpdatedEvent>(OnPerformanceDataUpdated);
            _eventSystem.Subscribe<ProfilingSessionStartedEvent>(OnSessionStarted);
            _eventSystem.Subscribe<ProfilingSessionCompletedEvent>(OnSessionCompleted);
        }
        
        /// <summary>
        /// Handles performance data updates
        /// </summary>
        private void OnPerformanceDataUpdated(PerformanceDataUpdatedEvent eventData)
        {
            UpdateMetricsDisplay(eventData.Snapshot);
            
            if (eventData.HasSessionInfo)
            {
                if (eventData.IsSessionComplete)
                {
                    UpdateStatus("Session Complete - 100%", Color.green);
                }
                else
                {
                    UpdateStatus($"Session Active - {eventData.SessionProgressPercent}%", Color.yellow);
                }
            }
        }
        
        /// <summary>
        /// Handles session start events
        /// </summary>
        private void OnSessionStarted(ProfilingSessionStartedEvent eventData)
        {
            string sessionInfo = eventData.IsFrameBased ? $"{eventData.TargetFrames}f" : $"{eventData.TargetDuration:F1}s";
            UpdateStatus($"Session: {eventData.SessionName} ({sessionInfo})", Color.cyan);
        }
        
        /// <summary>
        /// Handles session completion events
        /// </summary>
        private void OnSessionCompleted(ProfilingSessionCompletedEvent eventData)
        {
            UpdateStatus($"Completed: {eventData.Session.sessionName} ({eventData.Session.totalFrames}f)", Color.green);
        }
        
        #endregion

        #region UI Updates
        
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
        
        /// <summary>
        /// Attempts direct service access when event system isn't ready
        /// </summary>
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
        /// Updates performance metrics display with change detection
        /// </summary>
        private void UpdateMetricsDisplay(PerformanceSnapshot snapshot)
        {
            // Update FPS values
            if (Mathf.Abs(snapshot.fps - _lastFPSCurrent) > 0.5f)
            {
                UpdateFPSCurrentDisplay(snapshot.fps);
                _lastFPSCurrent = snapshot.fps;
            }
            
            // Update FPS statistics if available
            if (_profilingService != null)
            {
                var fpsStats = _profilingService.GetFPSStats();
                
                if (Mathf.Abs(fpsStats.Average - _lastFPSAvg) > 0.5f)
                {
                    UpdateFPSAvgDisplay(fpsStats.Average);
                    _lastFPSAvg = fpsStats.Average;
                }
                
                if (Mathf.Abs(fpsStats.Min - _lastFPSMin) > 0.5f)
                {
                    UpdateFPSMinDisplay(fpsStats.Min);
                    _lastFPSMin = fpsStats.Min;
                }
                
                if (Mathf.Abs(fpsStats.Max - _lastFPSMax) > 0.5f)
                {
                    UpdateFPSMaxDisplay(fpsStats.Max);
                    _lastFPSMax = fpsStats.Max;
                }
            }
            
            // Update Memory
            float memoryMB = snapshot.MemoryMB;
            if (Mathf.Abs(memoryMB - _lastMemory) > 1f)
            {
                UpdateMemoryDisplay(memoryMB);
                _lastMemory = memoryMB;
            }
            
            // Update Draw Calls
            if (Mathf.Abs(snapshot.drawCalls - _lastDrawCalls) > 5)
            {
                UpdateDrawCallsDisplay(snapshot.drawCalls);
                _lastDrawCalls = snapshot.drawCalls;
            }
            
            // Update Rendering Stats
            bool renderingChanged = 
                Mathf.Abs(snapshot.batches - _lastBatches) > 5 ||
                Mathf.Abs(snapshot.triangles - _lastTriangles) > 100 ||
                Mathf.Abs(snapshot.vertices - _lastVertices) > 100;
                
            if (renderingChanged)
            {
                UpdateRenderingStats(snapshot.batches, snapshot.triangles, snapshot.vertices);
                _lastBatches = snapshot.batches;
                _lastTriangles = snapshot.triangles;
                _lastVertices = snapshot.vertices;
            }
        }
        
        /// <summary>
        /// Updates FPS current display with color coding
        /// </summary>
        private void UpdateFPSCurrentDisplay(float fps)
        {
            if (_fpsCurrentLabel == null) return;
            
            _fpsCurrentLabel.text = fps.ToString("F1");
            
            Color fpsColor = fps >= 50f ? Color.green : fps >= 30f ? Color.yellow : Color.red;
            _fpsCurrentLabel.style.color = fpsColor;
        }
        
        /// <summary>
        /// Updates FPS average display
        /// </summary>
        private void UpdateFPSAvgDisplay(float fpsAvg)
        {
            if (_fpsAvgLabel != null)
                _fpsAvgLabel.text = fpsAvg.ToString("F1");
        }
        
        /// <summary>
        /// Updates FPS minimum display
        /// </summary>
        private void UpdateFPSMinDisplay(float fpsMin)
        {
            if (_fpsMinLabel != null)
                _fpsMinLabel.text = fpsMin.ToString("F1");
        }
        
        /// <summary>
        /// Updates FPS maximum display
        /// </summary>
        private void UpdateFPSMaxDisplay(float fpsMax)
        {
            if (_fpsMaxLabel != null)
                _fpsMaxLabel.text = fpsMax.ToString("F1");
        }
        
        /// <summary>
        /// Updates memory display with color coding
        /// </summary>
        private void UpdateMemoryDisplay(float memoryMB)
        {
            if (_memoryValueLabel == null) return;
            
            _memoryValueLabel.text = memoryMB.ToString("F1");
            
            Color memoryColor = memoryMB > 500f ? Color.red : memoryMB > 250f ? Color.yellow : Color.green;
            _memoryValueLabel.style.color = memoryColor;
        }
        
        /// <summary>
        /// Updates draw calls display with color coding
        /// </summary>
        private void UpdateDrawCallsDisplay(int drawCalls)
        {
            if (_drawCallsValueLabel == null) return;
            
            _drawCallsValueLabel.text = drawCalls.ToString();
            
            Color drawCallColor = drawCalls > 1000 ? Color.red : drawCalls > 500 ? Color.yellow : Color.green;
            _drawCallsValueLabel.style.color = drawCallColor;
        }
        
        /// <summary>
        /// Updates rendering statistics display
        /// </summary>
        private void UpdateRenderingStats(int batches, int triangles, int vertices)
        {
            if (_batchesValueLabel != null)
            {
                _batchesValueLabel.text = batches.ToString();
                
                Color batchColor = batches > 500 ? Color.red : batches > 250 ? Color.yellow : Color.green;
                _batchesValueLabel.style.color = batchColor;
            }
            
            if (_trianglesValueLabel != null && _trianglesUnitLabel != null)
            {
                var (value, unit) = FormatLargeNumber(triangles);
                _trianglesValueLabel.text = value;
                _trianglesUnitLabel.text = unit;
            }
            
            if (_verticesValueLabel != null && _verticesUnitLabel != null)
            {
                var (value, unit) = FormatLargeNumber(vertices);
                _verticesValueLabel.text = value;
                _verticesUnitLabel.text = unit;
            }
        }
        
        /// <summary>
        /// Updates session status display
        /// </summary>
        private void UpdateSessionStatus()
        {
            if (_sessionStatusLabel == null || _profilingService == null) return;
            
            if (_profilingService.IsSessionActive)
            {
                float progress = _profilingService.SessionProgress;
                int progressPercent = Mathf.RoundToInt(progress * 100f);
                UpdateStatus($"Session Active - {progressPercent}%", Color.yellow);
            }
            else
            {
                UpdateStatus("Real-time Monitoring", Color.green);
            }
        }
        
        /// <summary>
        /// Updates status label with color
        /// </summary>
        private void UpdateStatus(string message, Color color)
        {
            if (_sessionStatusLabel != null)
            {
                _sessionStatusLabel.text = message;
                _sessionStatusLabel.style.color = color;
            }
        }
        
        /// <summary>
        /// Updates graphs with historical data
        /// </summary>
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
        
        /// <summary>
        /// Fallback data update when events aren't working
        /// </summary>
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

        #region Helper Methods
        
        /// <summary>
        /// Formats large numbers into value and unit pairs without string concatenation
        /// </summary>
        private (string value, string unit) FormatLargeNumber(int number)
        {
            if (number >= 1000000)
                return ((number / 1000000f).ToString("F1"), "M");
            else if (number >= 1000)
                return ((number / 1000f).ToString("F1"), "K");
            else
                return (number.ToString(), "");
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
            base.Cleanup();
        }
        
        public override bool CountsAsGameBlockingPopup => false;
        
        #endregion
    }
}
