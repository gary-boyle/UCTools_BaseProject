using UnityEngine;
using UnityEngine.UIElements;
using GameFramework.Services.Data;

namespace GameFramework.Editor.ProfilerSessionManager.UI
{
    /// <summary>
    /// Panel displaying detailed information about a selected profiling session
    /// Now uses UXML for UI definition
    /// </summary>
    public class SessionDetailsPanel : VisualElement
    {
        private const string UXMLPath = "Assets/Scripts/Editor/ProfilerSessionManager/UI/UXML/SessionDetailsPanel.uxml";
        
        private ProfilingSessionInfo _currentSessionInfo;
        private ProfilingSessionData _currentSessionData;
        
        // UI References
        private ScrollView _statsContainer;
        private Button _openFileButton;
        private Button _deleteButton;
        private Button _exportButton;
        
        public event System.Action<ProfilingSessionInfo> OnDeleteRequested;
        public event System.Action<ProfilingSessionInfo> OnOpenFileRequested;
        public event System.Action<ProfilingSessionData> OnExportRequested;
        
        public SessionDetailsPanel()
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
            
            // Get references
            _statsContainer = this.Q<ScrollView>("stats-container");
            _openFileButton = this.Q<Button>("open-file-button");
            _deleteButton = this.Q<Button>("delete-button");
            _exportButton = this.Q<Button>("export-button");
            
            // Setup event handlers
            _openFileButton.clicked += () => OnOpenFileRequested?.Invoke(_currentSessionInfo);
            _deleteButton.clicked += () => OnDeleteRequested?.Invoke(_currentSessionInfo);
            _exportButton.clicked += () => OnExportRequested?.Invoke(_currentSessionData);
            
            // Initially hide
            SetVisible(false);
        }
        
        public void LoadSession(ProfilingSessionInfo sessionInfo, ProfilingSessionData sessionData)
        {
            _currentSessionInfo = sessionInfo;
            _currentSessionData = sessionData;
            
            if (sessionInfo == null)
            {
                SetVisible(false);
                return;
            }
            
            SetVisible(true);
            UpdateUI(sessionInfo, sessionData);
        }
        
        private void UpdateUI(ProfilingSessionInfo sessionInfo, ProfilingSessionData sessionData)
        {
            // Update basic info
            this.Q<Label>("session-name-value").text = sessionInfo.SessionName ?? "Unknown";
            this.Q<Label>("duration-value").text = sessionInfo.FormattedDuration;
            this.Q<Label>("frames-value").text = sessionInfo.TotalFrames.ToString();
            this.Q<Label>("fps-value").text = sessionInfo.FormattedFrameRate;
            this.Q<Label>("filesize-value").text = sessionInfo.FormattedFileSize;
            this.Q<Label>("creation-value").text = sessionInfo.CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
            
            // Update device info
            this.Q<Label>("device-value").text = sessionInfo.DeviceInfo ?? "Unknown";
            this.Q<Label>("unity-version-value").text = sessionInfo.UnityVersion ?? "Unknown";
            this.Q<Label>("build-config-value").text = sessionInfo.BuildConfiguration ?? "Unknown";
            
            // Update statistics
            UpdateStatistics(sessionData);
        }
        
        private void UpdateStatistics(ProfilingSessionData sessionData)
        {
            _statsContainer.Clear();
            
            if (sessionData == null) return;
            
            CreateStatItem("FPS", sessionData.fpsStats);
            CreateStatItem("Memory (MB)", sessionData.memoryStats);
            CreateStatItem("Draw Calls", sessionData.drawCallStats);
            CreateStatItem("Batches", sessionData.batchStats);
            CreateStatItem("Triangles", sessionData.triangleStats);
            CreateStatItem("Vertices", sessionData.vertexStats);
        }
        
        private void CreateStatItem(string metric, PerformanceStats stats)
        {
            var statItem = new VisualElement();
            statItem.AddToClassList("stat-item");
            
            var title = new Label(metric);
            title.AddToClassList("stat-title");
            statItem.Add(title);
            
            var details = new Label($"Min: {stats.min:F2} | Max: {stats.max:F2} | Avg: {stats.average:F2} | Median: {stats.median:F2}");
            details.AddToClassList("stat-details");
            statItem.Add(details);
            
            _statsContainer.Add(statItem);
        }
        
        private void SetVisible(bool visible)
        {
            style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
