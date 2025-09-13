using UnityEngine;
using UnityEngine.UIElements;
using GameFramework.UI.Utilities;
using GameFramework.Services;
using GameFramework.Services.Data;

namespace GameFramework.Editor.ProfilerSessionManager.UI
{
    /// <summary>
    /// UI component that displays performance graphs using UXML layout
    /// </summary>
    public class PerformanceGraphsPanel : VisualElement
    {
        private const string UXMLPath = "Assets/Scripts/Editor/ProfilerSessionManager/UI/UXML/PerformanceGraphsPanel.uxml";
        
        private GraphElement _fpsGraph;
        private GraphElement _memoryGraph;
        private GraphElement _drawCallGraph;
        private GraphElement _batchGraph;
        
        private Label _fpsStats;
        private Label _memoryStats;
        private Label _drawCallStats;
        private Label _batchStats;
        
        private SliderInt _maxPointsSlider;
        private Label _maxPointsValue;
        
        private ProfilingSessionData _currentSession;
        private int _maxDisplayPoints = 100;
        
        public PerformanceGraphsPanel()
        {
            LoadUI();
        }
        
        private void LoadUI()
        {
            var uxml = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXMLPath);
            if (uxml == null)
            {
                Debug.LogError($"Could not load UXML file at {UXMLPath}");
                return;
            }
            
            uxml.CloneTree(this);
            
            // Get references to stat labels
            _fpsStats = this.Q<Label>("fps-stats");
            _memoryStats = this.Q<Label>("memory-stats");
            _drawCallStats = this.Q<Label>("drawcalls-stats");
            _batchStats = this.Q<Label>("batches-stats");
            
            // Get options controls
            _maxPointsSlider = this.Q<SliderInt>("max-points-slider");
            _maxPointsValue = this.Q<Label>("max-points-value");
            
            // Setup slider
            _maxPointsSlider.RegisterValueChangedCallback(OnMaxPointsChanged);
            _maxPointsValue.text = _maxPointsSlider.value.ToString();
            _maxDisplayPoints = _maxPointsSlider.value;
            
            // Create graph elements and add them to containers
            CreateGraphs();
        }
        
        private void CreateGraphs()
        {
            // Create FPS graph
            _fpsGraph = new GraphElement(_maxDisplayPoints);
            _fpsGraph.Width = 900;
            _fpsGraph.Height = 200;
            _fpsGraph.SetLineColor(Color.green);
            this.Q("fps-graph-container").Add(_fpsGraph);
            
            // Create Memory graph
            _memoryGraph = new GraphElement(_maxDisplayPoints);
            _memoryGraph.Width = 900;
            _memoryGraph.Height = 200;
            _memoryGraph.SetLineColor(Color.cyan);
            this.Q("memory-graph-container").Add(_memoryGraph);
            
            // Create Draw Calls graph
            _drawCallGraph = new GraphElement(_maxDisplayPoints);
            _drawCallGraph.Width = 900;
            _drawCallGraph.Height = 200;
            _drawCallGraph.SetLineColor(Color.yellow);
            this.Q("drawcalls-graph-container").Add(_drawCallGraph);
            
            // Create Batches graph
            _batchGraph = new GraphElement(_maxDisplayPoints);
            _batchGraph.Width = 900;
            _batchGraph.Height = 200;
            _batchGraph.SetLineColor(Color.orange);
            this.Q("batches-graph-container").Add(_batchGraph);
        }
        
        private void OnMaxPointsChanged(ChangeEvent<int> evt)
        {
            _maxDisplayPoints = evt.newValue;
            _maxPointsValue.text = evt.newValue.ToString();
            
            // Update graphs with new max points
            if (_fpsGraph != null) _fpsGraph.MaxDataPoints = _maxDisplayPoints;
            if (_memoryGraph != null) _memoryGraph.MaxDataPoints = _maxDisplayPoints;
            if (_drawCallGraph != null) _drawCallGraph.MaxDataPoints = _maxDisplayPoints;
            if (_batchGraph != null) _batchGraph.MaxDataPoints = _maxDisplayPoints;
            
            // Reload current session with new sampling
            if (_currentSession != null)
            {
                LoadSession(_currentSession);
            }
        }
        
        public void LoadSession(ProfilingSessionData session)
        {
            _currentSession = session;
            
            if (session?.snapshots == null || session.snapshots.Length == 0)
            {
                ClearGraphs();
                return;
            }
            
            ClearGraphs();
            
            // Sample data if needed
            var snapshots = session.snapshots;
            if (snapshots.Length > _maxDisplayPoints)
            {
                snapshots = SampleData(snapshots, _maxDisplayPoints);
            }
            
            // Populate graphs
            foreach (var snapshot in snapshots)
            {
                _fpsGraph.AddDataPoint(snapshot.fps);
                _memoryGraph.AddDataPoint(snapshot.MemoryMB);
                _drawCallGraph.AddDataPoint(snapshot.drawCalls);
                _batchGraph.AddDataPoint(snapshot.batches);
            }
            
            UpdateStatsLabels(session);
        }
        
        private void UpdateStatsLabels(ProfilingSessionData session)
        {
            _fpsStats.text = $"Avg: {session.fpsStats.average:F1} | Min: {session.fpsStats.min:F1} | Max: {session.fpsStats.max:F1}";
            _memoryStats.text = $"Avg: {session.memoryStats.average:F1} | Min: {session.memoryStats.min:F1} | Max: {session.memoryStats.max:F1}";
            _drawCallStats.text = $"Avg: {session.drawCallStats.average:F0} | Min: {session.drawCallStats.min:F0} | Max: {session.drawCallStats.max:F0}";
            _batchStats.text = $"Avg: {session.batchStats.average:F0} | Min: {session.batchStats.min:F0} | Max: {session.batchStats.max:F0}";
        }
        
        private void ClearGraphs()
        {
            _fpsGraph?.Clear();
            _memoryGraph?.Clear();
            _drawCallGraph?.Clear();
            _batchGraph?.Clear();
            
            _fpsStats.text = "No data";
            _memoryStats.text = "No data";
            _drawCallStats.text = "No data";
            _batchStats.text = "No data";
        }
        
        private PerformanceSnapshot[] SampleData(PerformanceSnapshot[] data, int targetCount)
        {
            if (data.Length <= targetCount) return data;
            
            var sampled = new PerformanceSnapshot[targetCount];
            float step = (float)data.Length / targetCount;
            
            for (int i = 0; i < targetCount; i++)
            {
                int index = Mathf.RoundToInt(i * step);
                index = Mathf.Clamp(index, 0, data.Length - 1);
                sampled[i] = data[index];
            }
            
            return sampled;
        }
    }
}
