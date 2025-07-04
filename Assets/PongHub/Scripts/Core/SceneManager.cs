using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace PongHub.Core
{
    public class SceneManager : MonoBehaviour
    {
        public static SceneManager Instance { get; private set; }

        [Header("场景设置")]
        [SerializeField]
        [Tooltip("Main Menu Scene / 主菜单场景 - Scene name for main menu")]
        private string mainMenuScene = "MainMenu";

        [SerializeField]
        [Tooltip("Game Scene / 游戏场景 - Scene name for game")]
        private string gameScene = "Game";

        [SerializeField]
        [Tooltip("Min Loading Time / 最小加载时间 - Minimum loading time for loading screen")]
        private float minLoadingTime = 1f;  // 最小加载时间，用于显示加载画面

        private bool isLoading = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void LoadMainMenu()
        {
            if (!isLoading)
            {
                StartCoroutine(LoadSceneAsync(mainMenuScene));
            }
        }

        public void LoadGame()
        {
            if (!isLoading)
            {
                StartCoroutine(LoadSceneAsync(gameScene));
            }
        }

        public void ReloadCurrentScene()
        {
            if (!isLoading)
            {
                StartCoroutine(LoadSceneAsync(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name));
            }
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            isLoading = true;

            // 显示加载画面
            // TODO: 显示加载UI

            // 开始加载场景
            AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            float startTime = Time.time;
            float progress = 0f;

            // 等待场景加载完成
            while (!asyncLoad.isDone)
            {
                progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

                // 更新加载进度UI
                // TODO: 更新加载进度UI

                // 确保加载时间至少为minLoadingTime
                if (asyncLoad.progress >= 0.9f && Time.time - startTime >= minLoadingTime)
                {
                    asyncLoad.allowSceneActivation = true;
                }

                yield return null;
            }

            // 隐藏加载画面
            // TODO: 隐藏加载UI

            isLoading = false;
        }

        public void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        public bool IsLoading()
        {
            return isLoading;
        }

        public float GetLoadingProgress()
        {
            if (isLoading)
            {
                return UnityEngine.SceneManagement.SceneManager.GetActiveScene().isLoaded ? 1f : 0f;
            }
            return 1f;
        }
    }
}