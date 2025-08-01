using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using PongHub.UI.Core;

namespace PongHub.Core
{
    /// <summary>
    /// 场景管理器 - 处理场景切换和加载界面
    /// Scene Manager - Handles scene transitions and loading UI
    /// </summary>
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
        private float minLoadingTime = 1f;

        [Header("加载界面设置")]
        [SerializeField]
        [Tooltip("Loading UI Panel / 加载界面面板 - Reference to the loading UI panel")]
        private LoadingUIPanel m_loadingUIPanel;

        [SerializeField]
        [Tooltip("Loading Canvas / 加载画布 - Canvas for loading UI (will be created if not assigned)")]
        private Canvas m_loadingCanvas;

        // 私有变量
        private bool isLoading = false;
        private AsyncOperation m_currentLoadOperation;
        private float m_loadingStartTime;

        #region Unity生命周期

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeLoadingUI();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化加载界面
        /// </summary>
        private void InitializeLoadingUI()
        {
            // 如果没有指定加载UI面板，尝试查找或创建
            if (m_loadingUIPanel == null)
            {
                m_loadingUIPanel = FindObjectOfType<LoadingUIPanel>();
                
                if (m_loadingUIPanel == null)
                {
                    CreateLoadingUI();
                }
            }

            // 确保加载UI面板不会被场景切换销毁
            if (m_loadingUIPanel != null)
            {
                DontDestroyOnLoad(m_loadingUIPanel.gameObject);
                
                // 初始状态为隐藏
                m_loadingUIPanel.Hide(true);
            }
        }

        /// <summary>
        /// 创建加载界面
        /// </summary>
        private void CreateLoadingUI()
        {
            // 创建Canvas
            GameObject canvasGO = new GameObject("LoadingCanvas");
            m_loadingCanvas = canvasGO.AddComponent<Canvas>();
            m_loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_loadingCanvas.sortingOrder = 1000; // 确保在最上层
            
            // 添加CanvasScaler
            var canvasScaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            
            // 添加GraphicRaycaster
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // 创建加载面板
            GameObject panelGO = new GameObject("LoadingPanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            
            m_loadingUIPanel = panelGO.AddComponent<LoadingUIPanel>();
            
            // 设置面板填满整个屏幕
            var rectTransform = panelGO.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            DontDestroyOnLoad(canvasGO);
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 加载主菜单场景
        /// </summary>
        public void LoadMainMenu()
        {
            if (!isLoading)
            {
                StartCoroutine(LoadSceneAsync(mainMenuScene));
            }
        }

        /// <summary>
        /// 加载游戏场景
        /// </summary>
        public void LoadGame()
        {
            if (!isLoading)
            {
                StartCoroutine(LoadSceneAsync(gameScene));
            }
        }

        /// <summary>
        /// 重新加载当前场景
        /// </summary>
        public void ReloadCurrentScene()
        {
            if (!isLoading)
            {
                StartCoroutine(LoadSceneAsync(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name));
            }
        }

        /// <summary>
        /// 加载指定场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        public void LoadScene(string sceneName)
        {
            if (!isLoading)
            {
                StartCoroutine(LoadSceneAsync(sceneName));
            }
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        public void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        /// <summary>
        /// 获取是否正在加载
        /// </summary>
        /// <returns>是否正在加载</returns>
        public bool IsLoading()
        {
            return isLoading;
        }

        /// <summary>
        /// 获取当前加载进度
        /// </summary>
        /// <returns>加载进度 (0-1)</returns>
        public float GetLoadingProgress()
        {
            if (m_currentLoadOperation != null)
            {
                return Mathf.Clamp01(m_currentLoadOperation.progress / 0.9f);
            }
            return isLoading ? 0f : 1f;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        private IEnumerator LoadSceneAsync(string sceneName)
        {
            isLoading = true;
            m_loadingStartTime = Time.time;

            // 显示加载界面
            if (m_loadingUIPanel != null)
            {
                m_loadingUIPanel.Show();
                m_loadingUIPanel.SetProgress(0f, false);
                m_loadingUIPanel.SetLoadingText($"Loading {sceneName}...");
            }

            // 等待一帧确保UI显示
            yield return null;

            // 开始加载场景
            m_currentLoadOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            m_currentLoadOperation.allowSceneActivation = false;

            float progress = 0f;

            // 等待场景加载完成
            while (!m_currentLoadOperation.isDone)
            {
                // 计算加载进度
                progress = Mathf.Clamp01(m_currentLoadOperation.progress / 0.9f);

                // 更新加载进度UI
                if (m_loadingUIPanel != null)
                {
                    m_loadingUIPanel.SetProgress(progress);
                }

                // 检查是否满足最小加载时间和加载完成条件
                float elapsedTime = Time.time - m_loadingStartTime;
                if (m_currentLoadOperation.progress >= 0.9f && elapsedTime >= minLoadingTime)
                {
                    // 显示100%进度
                    if (m_loadingUIPanel != null)
                    {
                        m_loadingUIPanel.SetProgress(1f);
                    }

                    // 等待进度动画完成
                    yield return new WaitForSeconds(0.3f);

                    // 激活场景
                    m_currentLoadOperation.allowSceneActivation = true;
                }

                yield return null;
            }

            // 场景加载完成，等待一小段时间再隐藏UI
            yield return new WaitForSeconds(0.2f);

            // 隐藏加载界面
            if (m_loadingUIPanel != null)
            {
                m_loadingUIPanel.Hide();
            }

            // 清理状态
            m_currentLoadOperation = null;
            isLoading = false;

            Debug.Log($"Scene '{sceneName}' loaded successfully");
        }

        #endregion

        #region 调试功能

        /// <summary>
        /// 测试加载界面
        /// </summary>
        [ContextMenu("Test Loading UI")]
        private void TestLoadingUI()
        {
            if (Application.isPlaying && m_loadingUIPanel != null)
            {
                StartCoroutine(TestLoadingSequence());
            }
        }

        /// <summary>
        /// 测试加载序列
        /// </summary>
        private IEnumerator TestLoadingSequence()
        {
            m_loadingUIPanel.Show();
            m_loadingUIPanel.SetLoadingText("Testing loading sequence...");

            for (float i = 0; i <= 1f; i += 0.1f)
            {
                m_loadingUIPanel.SetProgress(i);
                yield return new WaitForSeconds(0.3f);
            }

            yield return new WaitForSeconds(1f);
            m_loadingUIPanel.Hide();
        }

        #endregion
    }
}