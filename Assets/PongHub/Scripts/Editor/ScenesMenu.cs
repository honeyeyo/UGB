// Copyright (c) MagnusLab Inc. and affiliates.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityToolbarExtender;

namespace PongHub.Editor
{
    /// <summary>
    /// Adds a quick way to load the different scenes by adding a button for each scene on the toolbar.
    /// </summary>
    public static class ScenesMenu
    {

        [InitializeOnLoadMethod]
        private static void Initialize() => ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);

        private static void OnToolbarGUI()
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("Startup", "Load startup scene.")))
            {
                LoadStartup();
            }
            if (GUILayout.Button(new GUIContent("Menu", "Load startup scene.")))
            {
                LoadMenu();
            }
            if (GUILayout.Button(new GUIContent("SchoolGym", "Load school gym scene.")))
            {
                LoadSchoolGym();
            }
            GUILayout.Space(100);
        }


        [MenuItem("Scenes/Startup")]
        public static void LoadStartup()
        {
            OpenScene("Startup");
        }

        [MenuItem("Scenes/Menu")]
        public static void LoadMenu()
        {
            OpenScene("MainMenu");
        }

        [MenuItem("Scenes/SchoolGym")]
        public static void LoadSchoolGym()
        {
            OpenScene("Gym", "Assets/TirgamesAssets/SchoolGym");
        }

        [MenuItem("Scenes/Startup &1", true)]
        [MenuItem("Scenes/Menu &2", true)]
        [MenuItem("Scenes/SchoolGym &3", true)]
        public static bool LoadSceneValidation()
        {
            return !Application.isPlaying;
        }

        private static void OpenScene(string name, string path = "Assets/PongHub/Scenes")
        {
            var saved = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            if (saved)
            {
                _ = EditorSceneManager.OpenScene($"{path}/{name}.unity");
            }
        }
    }
}