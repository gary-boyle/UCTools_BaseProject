using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using GameFramework.Events;
using GameFramework.Services.Interfaces;
using GameFramework.EventSystem.Interfaces;
using GameFramework.Services.Data;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace GameFramework.Services
{
    /// <summary>
    /// Simplified profiling service that monitors performance metrics and manages frame-based profiling sessions.
    /// 
    /// Design:
    /// - Frame-based profiling with simple progress tracking
    /// - Circular buffers for efficient memory management
    /// - Event-driven architecture for UI updates
    /// - Direct Unity Profiler integration for accurate metrics
    /// 
    /// Pros:
    /// - Clean separation of concerns
    /// - Efficient memory usage
    /// - Simple session management
    /// - Comprehensive performance monitoring
    /// 
    /// Cons:
    /// - Memory overhead for data storage
    /// - Dependency on EventSystem
    /// - Performance impact during sessions
    /// </summary>
    public class ProfilingService : IProfilingService
    {
        #region Private Fields

        private bool _isInitialized;
        private IEventSystem _eventSystem;
        
        // Unity Profiler recorders
        private ProfilerRecorder _drawCallsRecorder;
        private ProfilerRecorder _batchesRecorder;
        private ProfilerRecorder _trianglesRecorder;
        private ProfilerRecorder _verticesRecorder;

        // Current performance metrics
        private float _currentFps;
        private long _currentMemoryUsage;
        private int _currentDrawCalls;
        private int _currentBatches;
        private int _currentTriangles;
        private int _currentVertices;
        
        // Update timing
        private float _lastUpdateTime;
        private float _updateInterval = 1f;
        
        // Historical data storage
        private readonly Queue<PerformanceData> _historicalData = new();
        private readonly int _maxHistoricalSamples = 300;
        
        // Session management
        private ProfilingSession _currentSession;
        private List<PerformanceSnapshot> _sessionData;
        private bool _isSessionActive;
        private int _sessionTargetFrames;
        private int _sessionCurrentFrame;
        
        // Thread safety
        private readonly object _dataLock = new object();

        // FPS calculation
        private readonly Queue<float> _fpsHistory = new();
        private readonly int _maxFpsHistorySamples = 30;
        private int _fpsFrameCount;
        private float _fpsTimeAccumulator;

        #endregion

        #region Constructor

        public ProfilingService(IEventSystem eventSystem)
        {
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
        }

        #endregion

        #region Properties

        public bool IsInitialized => _isInitialized;
        public float CurrentFPS => _currentFps;
        public long CurrentMemoryUsage => _currentMemoryUsage;
        public int CurrentDrawCalls => _currentDrawCalls;
        public int CurrentBatches => _currentBatches;
        public int CurrentTriangles => _currentTriangles;
        public int CurrentVertices => _currentVertices;
        
        public bool IsSessionActive => _isSessionActive;
        public float SessionProgress => _sessionTargetFrames > 0 ? (float)_sessionCurrentFrame / _sessionTargetFrames : 0f;

        #endregion

        #region Service Lifecycle

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            InitializeProfilerRecorders();
            UpdatePerformanceMetrics();
            _lastUpdateTime = Time.realtimeSinceStartup;
            
            _isInitialized = true;
            await Task.CompletedTask;
        }

        public void Shutdown()
        {
            if (_isSessionActive)
                StopSession();
        
            DisposeProfilerRecorders();
            ClearAllData();
            
            _isInitialized = false;
        }

        #endregion

        #region Update Loop

        public void Update()
        {
            if (!_isInitialized) return;

            float currentTime = Time.realtimeSinceStartup;
            float deltaTime = Time.unscaledDeltaTime;

            UpdateFPSCalculation(deltaTime);

            if (_isSessionActive)
            {
                // Collect data every frame during sessions
                UpdatePerformanceMetrics();
                var snapshot = CreateSnapshot(currentTime, deltaTime);
                
                AddToSession(snapshot);
                PublishPerformanceUpdate(snapshot);

                if (_sessionCurrentFrame >= _sessionTargetFrames)
                {
                    StopSession();
                }
            }
            else
            {
                // Update at specified interval when not in session
                if (currentTime - _lastUpdateTime >= _updateInterval)
                {
                    UpdatePerformanceMetrics();
                    UpdateHistoricalData();
                    
                    var snapshot = CreateSnapshot(currentTime, deltaTime);
                    PublishPerformanceUpdate(snapshot);
                    
                    _lastUpdateTime = currentTime;
                }
            }
        }

        #endregion

        #region Session Management

        public void StartFrameSession(int frameCount, string sessionName = null)
        {
            if (_isSessionActive)
                StopSession();

            _currentSession = new ProfilingSession(sessionName);
            _sessionData = new List<PerformanceSnapshot>(frameCount);
            
            _isSessionActive = true;
            _sessionTargetFrames = frameCount;
            _sessionCurrentFrame = 0;

            _eventSystem.Publish(new ProfilingSessionStartedEvent(
                _currentSession.sessionName, 
                true, 
                frameCount
            ));
        }

        public void StopSession()
        {
            if (!_isSessionActive) return;

            _isSessionActive = false;
            _currentSession.endTime = DateTime.Now;
            _currentSession.totalFrames = _sessionData?.Count ?? 0;

            ProcessSessionData();
            string filePath = SaveSessionToFile(_currentSession);

            _eventSystem.Publish(new ProfilingSessionCompletedEvent(_currentSession, filePath));

            _currentSession = null;
            _sessionData = null;
            _sessionCurrentFrame = 0;
        }

        #endregion

        #region Data Access

        public PerformanceSnapshot GetCurrentSnapshot()
        {
            return CreateSnapshot(Time.time, Time.unscaledDeltaTime);
        }

        public PerformanceData[] GetHistoricalData(int sampleCount = 60)
        {
            lock (_dataLock)
            {
                return _historicalData.TakeLast(Mathf.Min(sampleCount, _historicalData.Count)).ToArray();
            }
        }

        public FPSStats GetFPSStats()
        {
            if (_fpsHistory.Count == 0)
            {
                return new FPSStats
                {
                    Current = _currentFps,
                    Average = _currentFps,
                    Min = _currentFps,
                    Max = _currentFps,
                    SampleCount = 0
                };
            }

            var samples = _fpsHistory.ToArray();
            return new FPSStats
            {
                Current = _currentFps,
                Average = samples.Average(),
                Min = samples.Min(),
                Max = samples.Max(),
                SampleCount = samples.Length
            };
        }

        #endregion

        #region Configuration

        public void SetUpdateFrequency(float intervalSeconds)
        {
            _updateInterval = Mathf.Max(0.1f, intervalSeconds);
        }

        public void ClearHistory()
        {
            lock (_dataLock)
            {
                _historicalData.Clear();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes Unity profiler recorders for rendering statistics
        /// </summary>
        private void InitializeProfilerRecorders()
        {
            _drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _trianglesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            _verticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
        }

        /// <summary>
        /// Disposes Unity profiler recorders
        /// </summary>
        private void DisposeProfilerRecorders()
        {
            _drawCallsRecorder.Dispose();
            _batchesRecorder.Dispose();
            _trianglesRecorder.Dispose();
            _verticesRecorder.Dispose();
        }

        /// <summary>
        /// Updates FPS calculation with smoothing
        /// </summary>
        private void UpdateFPSCalculation(float deltaTime)
        {
            _fpsTimeAccumulator += deltaTime;
            _fpsFrameCount++;

            // Calculate FPS every 10 frames for stability
            if (_fpsFrameCount >= 10)
            {
                if (_fpsTimeAccumulator > 0f)
                {
                    float batchFps = _fpsFrameCount / _fpsTimeAccumulator;
                    batchFps = Mathf.Clamp(batchFps, 1f, 1000f);
                    
                    _currentFps = _currentFps > 0f ? Mathf.Lerp(_currentFps, batchFps, 0.2f) : batchFps;
                    
                    // Update FPS history
                    _fpsHistory.Enqueue(_currentFps);
                    while (_fpsHistory.Count > _maxFpsHistorySamples)
                        _fpsHistory.Dequeue();
                    
                    _fpsTimeAccumulator = 0f;
                    _fpsFrameCount = 0;
                }
            }
        }

        /// <summary>
        /// Updates current performance metrics from Unity profilers
        /// </summary>
        private void UpdatePerformanceMetrics()
        {
            _currentMemoryUsage = Profiler.GetTotalAllocatedMemoryLong();
            
            _currentDrawCalls = _drawCallsRecorder.Valid ? (int)_drawCallsRecorder.LastValue : 0;
            _currentBatches = _batchesRecorder.Valid ? (int)_batchesRecorder.LastValue : 0;
            _currentTriangles = _trianglesRecorder.Valid ? (int)_trianglesRecorder.LastValue : 0;
            _currentVertices = _verticesRecorder.Valid ? (int)_verticesRecorder.LastValue : 0;
        }

        /// <summary>
        /// Creates a performance snapshot from current metrics
        /// </summary>
        private PerformanceSnapshot CreateSnapshot(float timestamp, float deltaTime)
        {
            float fps = _currentFps;
            if (fps <= 0f && deltaTime > 0f)
            {
                fps = Mathf.Clamp(1f / deltaTime, 0f, 1000f);
            }

            return new PerformanceSnapshot(
                timestamp,
                fps,
                _currentMemoryUsage,
                _currentDrawCalls,
                _currentBatches,
                _currentTriangles,
                _currentVertices,
                deltaTime
            );
        }

        /// <summary>
        /// Updates historical data with current performance metrics
        /// </summary>
        private void UpdateHistoricalData()
        {
            lock (_dataLock)
            {
                var data = new PerformanceData(GetCurrentSnapshot());
                _historicalData.Enqueue(data);
                
                while (_historicalData.Count > _maxHistoricalSamples)
                {
                    _historicalData.Dequeue();
                }
            }
        }

        /// <summary>
        /// Adds performance snapshot to current session
        /// </summary>
        private void AddToSession(PerformanceSnapshot snapshot)
        {
            _sessionData?.Add(snapshot);
            _sessionCurrentFrame++;
        }

        /// <summary>
        /// Publishes performance update event with optional session progress
        /// </summary>
        private void PublishPerformanceUpdate(PerformanceSnapshot snapshot)
        {
            if (_isSessionActive)
            {
                int progressPercent = Mathf.RoundToInt(SessionProgress * 100f);
                bool isComplete = _sessionCurrentFrame >= _sessionTargetFrames;
                
                _eventSystem.Publish(new PerformanceDataUpdatedEvent(snapshot, progressPercent, isComplete));
            }
            else
            {
                _eventSystem.Publish(new PerformanceDataUpdatedEvent(snapshot));
            }
        }

        /// <summary>
        /// Processes session data to calculate statistics
        /// </summary>
        private void ProcessSessionData()
        {
            if (_sessionData == null || _sessionData.Count == 0) 
            {
                var emptyStats = new PerformanceStats();
                _currentSession.fpsStats = emptyStats;
                _currentSession.memoryStats = emptyStats;
                _currentSession.drawCallStats = emptyStats;
                _currentSession.batchStats = emptyStats;
                _currentSession.triangleStats = emptyStats;
                _currentSession.vertexStats = emptyStats;
                _currentSession.snapshots = new PerformanceSnapshot[0];
                return;
            }

            var snapshots = _sessionData.ToArray();
            _currentSession.snapshots = snapshots;

            _currentSession.fpsStats = CalculateStats(snapshots.Select(s => s.fps).ToArray());
            _currentSession.memoryStats = CalculateStats(snapshots.Select(s => s.MemoryMB).ToArray());
            _currentSession.drawCallStats = CalculateStats(snapshots.Select(s => (float)s.drawCalls).ToArray());
            _currentSession.batchStats = CalculateStats(snapshots.Select(s => (float)s.batches).ToArray());
            _currentSession.triangleStats = CalculateStats(snapshots.Select(s => (float)s.triangles).ToArray());
            _currentSession.vertexStats = CalculateStats(snapshots.Select(s => (float)s.vertices).ToArray());
        }

        /// <summary>
        /// Calculates statistical data from array of values
        /// </summary>
        private PerformanceStats CalculateStats(float[] values)
        {
            if (values.Length == 0) return new PerformanceStats();
            
            Array.Sort(values);
            
            float min = values[0];
            float max = values[values.Length - 1];
            float average = values.Average();
            float median = values.Length % 2 == 0 
                ? (values[values.Length / 2 - 1] + values[values.Length / 2]) / 2f
                : values[values.Length / 2];
            
            return new PerformanceStats(min, max, average, median, values.Length);
        }

        /// <summary>
        /// Saves profiling session data to JSON file
        /// </summary>
        private string SaveSessionToFile(ProfilingSession session)
        {
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, "ProfilingSessions");

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string filename = $"{session.sessionName}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string filepath = Path.Combine(directory, filename);

                string json = JsonUtility.ToJson(session, true);
                File.WriteAllText(filepath, json);

                return File.Exists(filepath) ? filepath : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Clears all stored data
        /// </summary>
        private void ClearAllData()
        {
            lock (_dataLock)
            {
                _historicalData.Clear();
                _sessionData?.Clear();
                _fpsHistory.Clear();
            }
        }

        #endregion
    }
}
