#if UNITY_EDITOR
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace GameFramework.Editor
{
    public class DocumentationBuilder : UnityEditor.Editor
    {
        [MenuItem("UCTools/Game Framework/Documentation/Build Documentation")]
        public static void BuildDocs()
        {
            string projectPath = System.IO.Path.GetDirectoryName(Application.dataPath);
            if (projectPath != null)
            {
                string docPath = System.IO.Path.Combine(projectPath, "Documentation");
        
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "docfx",
                    Arguments = "docfx.json",
                    WorkingDirectory = docPath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
        
                Process.Start(startInfo);
            }

            UnityEngine.Debug.Log("Documentation build started!");
        }
    
        [MenuItem("UCTools/Game Framework/Documentation/Serve Documentation")]
        public static void ServeDocs()
        {
            string projectPath = System.IO.Path.GetDirectoryName(Application.dataPath);
            if (projectPath != null)
            {
                string docPath = System.IO.Path.Combine(projectPath, "Documentation");
        
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = "docfx",
                    Arguments = "--serve --port 8080",
                    WorkingDirectory = docPath,
                    UseShellExecute = true
                };
        
                Process.Start(startInfo);
            }

            Application.OpenURL("http://localhost:8080");
        }
    }
}
#endif