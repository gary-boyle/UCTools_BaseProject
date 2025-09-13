using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using GameFramework.Services.Data;
using GameFramework.Services;

namespace GameFramework.Editor.ProfilerSessionManager
{
    /// <summary>
    /// Manages loading, saving, and organizing profiling session data
    /// 
    /// Design:
    /// - Centralized file management for profiling sessions
    /// - JSON serialization with Unity's JsonUtility
    /// - Automatic discovery of existing session files
    /// - Thread-safe file operations
    /// </summary>
    public static class ProfilingSessionManager
    {
        private static readonly string SessionsDirectory = Path.Combine(Application.persistentDataPath, "ProfilingSessions");
        
        static ProfilingSessionManager()
        {
            // Ensure sessions directory exists
            if (!Directory.Exists(SessionsDirectory))
            {
                Directory.CreateDirectory(SessionsDirectory);
            }
        }
        
        /// <summary>
        /// Load all available profiling sessions from the sessions directory
        /// </summary>
        public static List<ProfilingSessionInfo> GetAllSessions()
        {
            var sessions = new List<ProfilingSessionInfo>();
            
            try
            {
                var jsonFiles = Directory.GetFiles(SessionsDirectory, "*.json");
                
                foreach (var filePath in jsonFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        var sessionInfo = new ProfilingSessionInfo
                        {
                            FilePath = filePath,
                            FileName = Path.GetFileNameWithoutExtension(filePath),
                            FileSize = fileInfo.Length,
                            CreationTime = fileInfo.CreationTime,
                            LastModified = fileInfo.LastWriteTime
                        };
                        
                        // Try to read session name from file
                        try
                        {
                            var jsonContent = File.ReadAllText(filePath);
                            var sessionData = JsonUtility.FromJson<ProfilingSessionData>(jsonContent);
                            sessionInfo.SessionName = sessionData.sessionName;
                            sessionInfo.TotalFrames = sessionData.totalFrames;
                            sessionInfo.DurationSeconds = sessionData.durationSeconds;
                            sessionInfo.DeviceInfo = sessionData.deviceInfo;
                            sessionInfo.UnityVersion = sessionData.unityVersion;
                            sessionInfo.BuildConfiguration = sessionData.buildConfiguration;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Could not read session metadata from {filePath}: {ex.Message}");
                            sessionInfo.SessionName = sessionInfo.FileName;
                        }
                        
                        sessions.Add(sessionInfo);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error processing session file {filePath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error scanning sessions directory: {ex.Message}");
            }
            
            // Sort by creation time, newest first
            sessions.Sort((a, b) => b.CreationTime.CompareTo(a.CreationTime));
            return sessions;
        }
        
        /// <summary>
        /// Load a complete profiling session from file
        /// </summary>
        public static ProfilingSessionData LoadSession(string filePath)
        {
            try
            {
                var jsonContent = File.ReadAllText(filePath);
                return JsonUtility.FromJson<ProfilingSessionData>(jsonContent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load session from {filePath}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Save a profiling session to file
        /// </summary>
        public static bool SaveSession(ProfilingSessionData session, string fileName = null)
        {
            try
            {
                fileName ??= $"{session.sessionName}.json";
                var filePath = Path.Combine(SessionsDirectory, fileName);
                
                var jsonContent = JsonUtility.ToJson(session, true);
                File.WriteAllText(filePath, jsonContent);
                
                Debug.Log($"Session saved to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save session: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Delete a session file
        /// </summary>
        public static bool DeleteSession(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to delete session {filePath}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Open the sessions directory in the system file explorer
        /// </summary>
        public static void OpenSessionsFolder()
        {
            try
            {
                System.Diagnostics.Process.Start(SessionsDirectory);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to open sessions folder: {ex.Message}");
            }
        }
        
 public static string GetSessionsDirectory()
        {
            return SessionsDirectory;
        }
        
        /// <summary>
        /// Get formatted file size string
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;
            
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            
            return $"{number:n1} {suffixes[counter]}";
        }
    }
    
    /// <summary>
    /// Lightweight session information for list display
    /// </summary>
    [Serializable]
    public class ProfilingSessionInfo
    {
        public string FilePath;
        public string FileName;
        public string SessionName;
        public long FileSize;
        public DateTime CreationTime;
        public DateTime LastModified;
        public int TotalFrames;
        public float DurationSeconds;
        public string DeviceInfo;
        public string UnityVersion;
        public string BuildConfiguration;
        
        /// <summary>Get formatted file size</summary>
        public string FormattedFileSize => ProfilingSessionManager.FormatFileSize(FileSize);
        
        /// <summary>Get formatted duration</summary>
        public string FormattedDuration => $"{DurationSeconds:F2}s";
        
        /// <summary>Get average FPS if available</summary>
        public string FormattedFrameRate => TotalFrames > 0 && DurationSeconds > 0 
            ? $"{TotalFrames / DurationSeconds:F1} FPS" 
            : "N/A";
    }
    
    /// <summary>
    /// Complete profiling session data structure for JSON serialization
    /// </summary>
    [Serializable]
    public class ProfilingSessionData
    {
        public string sessionName;
        public float durationSeconds;
        public int totalFrames;
        public PerformanceStats fpsStats;
        public PerformanceStats memoryStats;
        public PerformanceStats drawCallStats;
        public PerformanceStats batchStats;
        public PerformanceStats triangleStats;
        public PerformanceStats vertexStats;
        public PerformanceSnapshot[] snapshots;
        public string deviceInfo;
        public string unityVersion;
        public string gameVersion;
        public string buildConfiguration;
    }
}