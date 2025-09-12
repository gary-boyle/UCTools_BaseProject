using System.IO;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;

namespace GameFramework.Editor
{
    /// <summary>
    /// Editor tool for quick access to the game's config file
    /// Provides menu items to open, reveal, and manage the config.json file
    /// </summary>
    public static class ConfigFileEditorTool
    {
        private const string CONFIG_FILE_NAME = "config.json";
        
        /// <summary>
        /// Gets the full path to the config file (same as SettingsRegistry uses)
        /// </summary>
        private static string ConfigFilePath => Path.Combine(Application.persistentDataPath, CONFIG_FILE_NAME);

        [MenuItem("UCTools/Game Framework/Config File/Open Config File", priority = 100)]
        public static void OpenConfigFile()
        {
            string filePath = ConfigFilePath;
            
            if (!File.Exists(filePath))
            {
                // Offer to create an empty config file
                bool createFile = EditorUtility.DisplayDialog(
                    "Config File Not Found",
                    $"Config file doesn't exist at:\n{filePath}\n\nWould you like to create an empty one?",
                    "Create File",
                    "Cancel"
                );
                
                if (createFile)
                {
                    CreateEmptyConfigFile(filePath);
                }
                else
                {
                    return;
                }
            }

            // Open the file with the default system editor
            try
            {
                Process.Start(filePath);
                UnityEngine.Debug.Log($"[ConfigFileEditorTool] Opened config file: {filePath}");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog(
                    "Failed to Open File",
                    $"Could not open config file:\n{e.Message}",
                    "OK"
                );
            }
        }

        [MenuItem("UCTools/Game Framework/Config File/Reveal Config File in Explorer", priority = 101)]
        public static void RevealConfigFileInExplorer()
        {
            string filePath = ConfigFilePath;
            string folderPath = Path.GetDirectoryName(filePath);
            
            if (!Directory.Exists(folderPath))
            {
                EditorUtility.DisplayDialog(
                    "Folder Not Found",
                    $"Config folder doesn't exist at:\n{folderPath}",
                    "OK"
                );
                return;
            }

            // Reveal in explorer/finder
            EditorUtility.RevealInFinder(File.Exists(filePath) ? filePath : folderPath);
            UnityEngine.Debug.Log($"[ConfigFileEditorTool] Revealed config location: {folderPath}");
        }

        [MenuItem("UCTools/Game Framework/Config File/Copy Config File Path", priority = 102)]
        public static void CopyConfigFilePath()
        {
            string filePath = ConfigFilePath;
            EditorGUIUtility.systemCopyBuffer = filePath;
            UnityEngine.Debug.Log($"[ConfigFileEditorTool] Copied to clipboard: {filePath}");
            
            // Show a brief notification
            ShowNotification($"Config path copied to clipboard:\n{Path.GetFileName(filePath)}");
        }

        [MenuItem("UCTools/Game Framework/Config File/Delete Config File", priority = 120)]
        public static void DeleteConfigFile()
        {
            string filePath = ConfigFilePath;
            
            if (!File.Exists(filePath))
            {
                EditorUtility.DisplayDialog(
                    "File Not Found",
                    $"Config file doesn't exist at:\n{filePath}",
                    "OK"
                );
                return;
            }

            bool confirmDelete = EditorUtility.DisplayDialog(
                "Delete Config File",
                $"Are you sure you want to permanently delete the config file?\n\n{filePath}\n\nThis will reset all settings to defaults on next game launch.",
                "Delete",
                "Cancel"
            );

            if (confirmDelete)
            {
                try
                {
                    File.Delete(filePath);
                    UnityEngine.Debug.Log($"[ConfigFileEditorTool] Deleted config file: {filePath}");
                    ShowNotification("Config file deleted successfully");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog(
                        "Failed to Delete File",
                        $"Could not delete config file:\n{e.Message}",
                        "OK"
                    );
                }
            }
        }

        [MenuItem("UCTools/Game Framework/Config File/Show Config Info", priority = 140)]
        public static void ShowConfigInfo()
        {
            string filePath = ConfigFilePath;
            bool fileExists = File.Exists(filePath);
            
            string info = $"Config File Information:\n\n";
            info += $"Path: {filePath}\n";
            info += $"Exists: {(fileExists ? "Yes" : "No")}\n";
            
            if (fileExists)
            {
                var fileInfo = new FileInfo(filePath);
                info += $"Size: {FormatFileSize(fileInfo.Length)}\n";
                info += $"Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}\n";
                info += $"Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}\n";
                
                // Try to count config entries
                try
                {
                    string content = File.ReadAllText(filePath);
                    int entryCount = CountConfigEntries(content);
                    info += $"Config Entries: {entryCount}\n";
                }
                catch
                {
                    info += "Config Entries: Unable to read\n";
                }
            }
            
            info += $"\nPersistent Data Path:\n{Application.persistentDataPath}";
            
            EditorUtility.DisplayDialog("Config File Info", info, "OK");
        }

        /// <summary>
        /// Validate menu items - disable if not in play mode for some operations
        /// </summary>
        [MenuItem("UCTools/Game Framework/Config File/Open Config File", true)]
        public static bool ValidateOpenConfigFile()
        {
            return true; // Always available
        }

        /// <summary>
        /// Creates an empty config file with basic structure
        /// </summary>
        private static void CreateEmptyConfigFile(string filePath)
        {
            try
            {
                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                
                // Create empty config structure
                string emptyConfig = "{\n  \"entries\": []\n}";
                File.WriteAllText(filePath, emptyConfig);
                
                UnityEngine.Debug.Log($"[ConfigFileEditorTool] Created empty config file: {filePath}");
                ShowNotification("Empty config file created");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog(
                    "Failed to Create File",
                    $"Could not create config file:\n{e.Message}",
                    "OK"
                );
            }
        }

        /// <summary>
        /// Format file size in human-readable format
        /// </summary>
        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Count config entries in JSON content (rough estimate)
        /// </summary>
        private static int CountConfigEntries(string jsonContent)
        {
            // Simple count of "key": occurrences - not perfect but gives an idea
            int count = 0;
            int index = 0;
            while ((index = jsonContent.IndexOf("\"key\":", index)) != -1)
            {
                count++;
                index += 6;
            }
            return count;
        }

        /// <summary>
        /// Show a notification in the Scene view
        /// </summary>
        private static void ShowNotification(string message)
        {
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.ShowNotification(new GUIContent(message), 3f);
            }
        }
    }
}
