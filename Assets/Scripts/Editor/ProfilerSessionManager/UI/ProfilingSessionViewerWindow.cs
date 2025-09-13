using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor.UIElements;

namespace GameFramework.Editor.ProfilerSessionManager.UI
{
    /// <summary>
    /// Main editor window for viewing and managing profiling sessions
    /// 
    /// Design:
    /// - Three-panel layout: Session list, details, and graphs
    /// - Resizable panels with splitter controls
    /// - Toolbar with common actions
    /// - Responsive layout that adapts to window size
    /// 
    /// Pros:
    /// - Comprehensive session management in one window
    /// - Intuitive three-pane interface
    /// - Rich visualization with graphs
    /// - Full file management capabilities
    /// 
    /// Cons:
    /// - Complex UI layout may be overwhelming
    /// - Requires significant screen real estate
    /// - Memory usage grows with large sessions
    /// </summary>
    public class ProfilingSessionViewerWindow : EditorWindow
    {
        private const string UXMLPath = "Assets/Scripts/Editor/ProfilerSessionManager/UI/UXML/ProfilingSessionViewerWindow.uxml";
        private const string SessionItemUXMLPath = "Assets/Scripts/Editor/ProfilerSessionManager/UI/UXML/SessionListItem.uxml";
        
        // UI References
        private ListView _sessionsList;
        private SessionDetailsPanel _detailsPanel;
        private PerformanceGraphsPanel _graphsPanel;
        private ToolbarSearchField _searchField;
        private Button _refreshButton;
        private Button _openFolderButton;
        private Button _importButton;
        
        // Data
        private List<ProfilingSessionInfo> _allSessions;
        private List<ProfilingSessionInfo> _filteredSessions;
        private ProfilingSessionInfo _selectedSession;
        private ProfilingSessionData _selectedSessionData;
        
        // Templates
        private VisualTreeAsset _sessionItemTemplate;
        
        [MenuItem("UCTools/Game Framework/Profiling Session Viewer")]
        public static void ShowWindow()
        {
            var window = GetWindow<ProfilingSessionViewerWindow>();
            window.titleContent = new GUIContent("Profiling Session Viewer");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        
        public void CreateGUI()
        {
            LoadUIAssets();
            LoadSessions();
        }
        
        private void LoadUIAssets()
        {
            // Load main UXML
            var mainUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXMLPath);
            if (mainUxml == null)
            {
                Debug.LogError($"Could not load UXML file at {UXMLPath}");
                return;
            }
            
            // Load session item template
            _sessionItemTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SessionItemUXMLPath);
            if (_sessionItemTemplate == null)
            {
                Debug.LogError($"Could not load session item template at {SessionItemUXMLPath}");
                return;
            }
            
            // Clone the main template
            mainUxml.CloneTree(rootVisualElement);
            
            // Get references to UI elements
            _searchField = rootVisualElement.Q<ToolbarSearchField>("search-field");
            _importButton = rootVisualElement.Q<Button>("import-button");
            _refreshButton = rootVisualElement.Q<Button>("refresh-button");
            _openFolderButton = rootVisualElement.Q<Button>("open-folder-button");
            _sessionsList = rootVisualElement.Q<ListView>("sessions-list");
            
            // Setup event handlers
            _searchField.RegisterValueChangedCallback(OnSearchChanged);
            _importButton.clicked += OnImportSession;
            _refreshButton.clicked += LoadSessions;
            _openFolderButton.clicked += OnOpenFolder;
            
            // Setup sessions list
            SetupSessionsList();
            
            // Create and add custom panels
            CreateCustomPanels();
        }
        private void SetupSessionsList()
        {
            _filteredSessions = new List<ProfilingSessionInfo>();
            _sessionsList.itemsSource = _filteredSessions;
            _sessionsList.makeItem = MakeSessionListItem;
            _sessionsList.bindItem = BindSessionListItem;
            _sessionsList.itemHeight = 60;
            _sessionsList.selectionType = SelectionType.Single;
            _sessionsList.onSelectionChange += OnSessionSelected;
        }
        private void CreateCustomPanels()
        {
            // Create and add details panel
            var detailsContainer = rootVisualElement.Q("details-container");
            _detailsPanel = new SessionDetailsPanel();
            _detailsPanel.OnDeleteRequested += OnDeleteSession;
            _detailsPanel.OnOpenFileRequested += OnOpenSessionFile;
            _detailsPanel.OnExportRequested += OnExportSession;
            detailsContainer.Add(_detailsPanel);
            
            // Create and add graphs panel
            var graphsContainer = rootVisualElement.Q("graphs-container");
            _graphsPanel = new PerformanceGraphsPanel();
            graphsContainer.Add(_graphsPanel);
        }

        private VisualElement MakeSessionListItem()
        {
            return _sessionItemTemplate.CloneTree();
        }

        private void BindSessionListItem(VisualElement element, int index)
        {
            if (index >= _filteredSessions.Count) return;
            
            var session = _filteredSessions[index];
            
            element.Q<Label>("session-name").text = session.SessionName ?? session.FileName;
            element.Q<Label>("duration").text = $"Duration: {session.FormattedDuration}";
            element.Q<Label>("frames").text = $"Frames: {session.TotalFrames}";
            element.Q<Label>("file-size").text = session.FormattedFileSize;
            element.Q<Label>("creation-date").text = session.CreationTime.ToString("MMM dd, HH:mm");
        }
        
        private void OnSessionSelected(IEnumerable<object> selectedItems)
        {
            var selectedSession = selectedItems.FirstOrDefault() as ProfilingSessionInfo;
            if (selectedSession == null)
            {
                _selectedSession = null;
                _selectedSessionData = null;
                _detailsPanel.LoadSession(null, null);
                _graphsPanel.LoadSession(null);
                return;
            }
            
            _selectedSession = selectedSession;
            _selectedSessionData = ProfilingSessionManager.LoadSession(selectedSession.FilePath);
            
            _detailsPanel.LoadSession(selectedSession, _selectedSessionData);
            _graphsPanel.LoadSession(_selectedSessionData);
        }
        
        private void LoadSessions()
        {
            _allSessions = ProfilingSessionManager.GetAllSessions();
            ApplyFilter();
        }
        
        private void ApplyFilter()
        {
            var searchText = _searchField?.value?.ToLowerInvariant() ?? "";
            
            if (string.IsNullOrEmpty(searchText))
            {
                _filteredSessions = new List<ProfilingSessionInfo>(_allSessions);
            }
            else
            {
                _filteredSessions = _allSessions.Where(s => 
                    s.SessionName?.ToLowerInvariant().Contains(searchText) == true ||
                    s.FileName?.ToLowerInvariant().Contains(searchText) == true ||
                    s.DeviceInfo?.ToLowerInvariant().Contains(searchText) == true
                ).ToList();
            }
            
            _sessionsList.itemsSource = _filteredSessions;
            _sessionsList.Rebuild();
        }
        
        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            ApplyFilter();
        }
        
        private void OnDeleteSession(ProfilingSessionInfo sessionInfo)
        {
            if (EditorUtility.DisplayDialog("Delete Session", 
                $"Are you sure you want to delete '{sessionInfo.SessionName}'?", 
                "Delete", "Cancel"))
            {
                if (ProfilingSessionManager.DeleteSession(sessionInfo.FilePath))
                {
                    LoadSessions();
                    
                    // Clear selection if deleted session was selected
                    if (_selectedSession == sessionInfo)
                    {
                        _selectedSession = null;
                        _selectedSessionData = null;
                        _detailsPanel.LoadSession(null, null);
                        _graphsPanel.LoadSession(null);
                    }
                }
            }
        }
        
        private void OnOpenSessionFile(ProfilingSessionInfo sessionInfo)
        {
            if (File.Exists(sessionInfo.FilePath))
            {
                System.Diagnostics.Process.Start(sessionInfo.FilePath);
            }
        }
        
        private void OnExportSession(ProfilingSessionData sessionData)
        {
            var path = EditorUtility.SaveFilePanel("Export Session Data", 
                "", $"{sessionData.sessionName}.csv", "csv");
                
            if (!string.IsNullOrEmpty(path))
            {
                ExportToCSV(sessionData, path);
            }
        }
        
        private void OnImportSession()
        {
            var path = EditorUtility.OpenFilePanelWithFilters("Import Profiling Session", 
                "", new string[] { "JSON Files", "json" });
                
            if (!string.IsNullOrEmpty(path))
            {
                var sourcePath = path;
                var fileName = Path.GetFileName(sourcePath);
                var targetPath = Path.Combine(ProfilingSessionManager.GetSessionsDirectory(), fileName);
                
                try
                {
                    File.Copy(sourcePath, targetPath, true);
                    LoadSessions();
                    Debug.Log($"Session imported: {fileName}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to import session: {ex.Message}");
                }
            }
        }
        
        private void OnOpenFolder()
        {
            ProfilingSessionManager.OpenSessionsFolder();
        }
        
        private void ExportToCSV(ProfilingSessionData sessionData, string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    // Write header
                    writer.WriteLine("Timestamp,FPS,Memory(MB),DrawCalls,Batches,Triangles,Vertices,DeltaTime");
                    
                    // Write data
                    foreach (var snapshot in sessionData.snapshots)
                    {
                        writer.WriteLine($"{snapshot.timestamp:F4},{snapshot.fps:F2},{snapshot.MemoryMB:F2}," +
                                       $"{snapshot.drawCalls},{snapshot.batches},{snapshot.triangles}," +
                                       $"{snapshot.vertices},{snapshot.deltaTime:F6}");
                    }
                }
                
                Debug.Log($"Session exported to: {filePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to export session: {ex.Message}");
            }
        }
    }
}
