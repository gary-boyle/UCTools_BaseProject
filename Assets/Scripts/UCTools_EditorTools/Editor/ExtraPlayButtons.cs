using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityToolbarExtender; // https://github.com/marijnz/unity-toolbar-extender.git

[InitializeOnLoad]
public class SceneSwitchLeftButton
{
    static SceneSwitchLeftButton()
    {
        ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
    }

    static void OnToolbarGUI()
    {
        GUILayout.FlexibleSpace();

        if(GUILayout.Button(new GUIContent("Save and Play", "Save Current Scene and Enter Playmode"), EditorStyles.toolbarButton))
        {
            // Save the current scene if it has unsaved changes
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                EditorSceneManager.SaveOpenScenes();
                Debug.Log("Scene saved before entering Play mode");
            }

            EditorApplication.EnterPlaymode();
        }
        
        if(GUILayout.Button(new GUIContent("Bootloader", "Save Current Scene and Enter Playmode"), EditorStyles.toolbarButton))
        {
            // Save the current scene if it has unsaved changes
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                EditorSceneManager.SaveOpenScenes();
                Debug.Log("Scene saved before entering Play mode");
            }

            //Scene path to bootloader scene
            string scenePath = "Assets/Scenes/Bootloader.unity";
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            EditorApplication.EnterPlaymode();
        }
        
        if(GUILayout.Button(new GUIContent("Reset Domain" ,"Reset the domain and reload all assemblies"), EditorStyles.toolbarButton))
        {
            // Reset the domain and reload all assemblies
            UnityEditor.EditorUtility.RequestScriptReload();
            Debug.Log("Domain reset and assemblies reloaded");
        }
    }
}