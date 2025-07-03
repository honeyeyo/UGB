using UnityEngine;
using System.Threading.Tasks;
using PongHub.Core;
using PongHub.Input;

namespace PongHub.UI
{
    /// <summary>
    /// [DEPRECATED] 旧版UI管理器
    /// 此脚本已被新的模块化UI架构替代，计划在后续版本中移除
    /// 新架构使用: MenuCanvasController + TableMenuSystem + VRMenuInteraction
    /// 迁移指南: 请参考 Documentation/Scripts_Usage_Guide.md
    /// </summary>
    [System.Obsolete("UIManager is deprecated. Use MenuCanvasController + TableMenuSystem instead. See Scripts_Usage_Guide.md for migration details.", false)]
    public class UIManager : MonoBehaviour
    {
        private static UIManager s_instance;
        public static UIManager Instance => s_instance;

        [Header("UI面板")]
        [SerializeField] private MainMenuPanel m_mainMenuPanel;
        [SerializeField] private SettingsPanel m_settingsPanel;
        [SerializeField] private PauseMenuPanel m_pauseMenuPanel;
        [SerializeField] private ScoreboardPanel m_scoreboardPanel;
        [SerializeField] private InputSettingsPanel m_inputSettingsPanel;
        [SerializeField] private GameplayHUD m_gameplayHUD;

        [Header("游戏状态")]
        [SerializeField] private PongHubInputManager m_inputManager;

        // 私有变量
        private bool isMenuOpen = false;
        private GameState currentGameState = GameState.MainMenu;

        public enum GameState
        {
            MainMenu,
            Settings,
            InputSettings,
            Playing,
            Paused
        }

        private void Awake()
        {
            if (s_instance == null)
            {
                s_instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async Task InitializeAsync()
        {
            await Task.Yield();
            InitializePanels();
        }

        private void InitializePanels()
        {
            SetGameState(GameState.MainMenu);
        }

        public void ShowMainMenu()
        {
            HideAllPanels();
            if (m_mainMenuPanel != null)
            {
                m_mainMenuPanel.gameObject.SetActive(true);
            }
        }

        public void ShowSettings()
        {
            HideAllPanels();
            if (m_settingsPanel != null)
            {
                m_settingsPanel.gameObject.SetActive(true);
            }
        }

        public void ShowPauseMenu()
        {
            if (m_pauseMenuPanel != null)
            {
                m_pauseMenuPanel.gameObject.SetActive(true);
            }
        }

        public void HidePauseMenu()
        {
            if (m_pauseMenuPanel != null)
            {
                m_pauseMenuPanel.gameObject.SetActive(false);
            }
        }

        public void ShowScoreboard()
        {
            if (m_scoreboardPanel != null)
            {
                m_scoreboardPanel.gameObject.SetActive(true);
            }
        }

        public void HideScoreboard()
        {
            if (m_scoreboardPanel != null)
            {
                m_scoreboardPanel.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 设置游戏状态
        /// </summary>
        public void SetGameState(GameState newState)
        {
            currentGameState = newState;

            // 隐藏所有面板
            HideAllPanels();

            // 根据状态显示相应面板
            switch (newState)
            {
                case GameState.MainMenu:
                    if (m_mainMenuPanel != null)
                        m_mainMenuPanel.gameObject.SetActive(true);
                    break;

                case GameState.Settings:
                    if (m_settingsPanel != null)
                        m_settingsPanel.gameObject.SetActive(true);
                    break;

                case GameState.InputSettings:
                    if (m_inputSettingsPanel != null)
                        m_inputSettingsPanel.gameObject.SetActive(true);
                    break;

                case GameState.Playing:
                    if (m_gameplayHUD != null)
                        m_gameplayHUD.gameObject.SetActive(true);
                    break;

                case GameState.Paused:
                    if (m_pauseMenuPanel != null)
                        m_pauseMenuPanel.gameObject.SetActive(true);
                    if (m_gameplayHUD != null)
                        m_gameplayHUD.gameObject.SetActive(true);
                    break;
            }

            Debug.Log($"游戏状态切换为: {newState}");
        }

        /// <summary>
        /// 显示输入设置
        /// </summary>
        public void ShowInputSettings()
        {
            SetGameState(GameState.InputSettings);
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            SetGameState(GameState.Playing);
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            SetGameState(GameState.Paused);
        }

        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            SetGameState(GameState.Playing);
        }

        /// <summary>
        /// 显示提示信息
        /// </summary>
        public void ShowMessage(string message, float duration = 3f)
        {
            if (m_gameplayHUD != null)
            {
                m_gameplayHUD.ShowMessage(message, duration);
            }
        }

        /// <summary>
        /// 配置球拍
        /// </summary>
        public void ConfigurePaddle(bool leftHand)
        {
            // TODO: 重新实现球拍配置逻辑
            // 新的输入系统中，球拍配置逻辑已移至PaddleController
            Debug.Log($"请重新实现{(leftHand ? "左手" : "右手")}球拍配置");

            // 可以考虑调用PaddleController的相关方法
            // var paddleController = FindObjectOfType<PaddleController>();
            // if (paddleController != null) { ... }
        }

        /// <summary>
        /// 瞬移到指定位置
        /// </summary>
        public void TeleportToPoint(int pointIndex)
        {
            // TODO: 重新实现传送逻辑
            // 新的输入系统中，传送逻辑已移至TeleportController
            Debug.Log($"请重新实现传送到位置 {pointIndex}");

            // 可以考虑调用TeleportController的相关方法
            // var teleportController = FindObjectOfType<TeleportController>();
            // if (teleportController != null) { ... }
        }

        private void HideAllPanels()
        {
            if (m_mainMenuPanel != null)
                m_mainMenuPanel.gameObject.SetActive(false);
            if (m_settingsPanel != null)
                m_settingsPanel.gameObject.SetActive(false);
            if (m_inputSettingsPanel != null)
                m_inputSettingsPanel.gameObject.SetActive(false);
            if (m_pauseMenuPanel != null)
                m_pauseMenuPanel.gameObject.SetActive(false);
            if (m_scoreboardPanel != null)
                m_scoreboardPanel.gameObject.SetActive(false);
            if (m_gameplayHUD != null)
                m_gameplayHUD.gameObject.SetActive(false);
        }

        // 公共属性
        public GameState CurrentGameState => currentGameState;
        public bool IsPlaying => currentGameState == GameState.Playing;
        public bool IsMenuOpen => isMenuOpen;
        public PongHubInputManager InputManager => m_inputManager;
    }
}