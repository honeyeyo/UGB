using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PongHub.Core;
using PongHub.Gameplay;
using PongHub.Gameplay.Ball;

namespace PongHub.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("UI面板")]
        [SerializeField] private GameObject m_hudPanel;
        [SerializeField] private GameObject m_mainMenuPanel;
        [SerializeField] private GameObject m_scoreboardPanel;

        [Header("HUD元素")]
        [SerializeField] private TextMeshProUGUI m_playerScoreText;
        [SerializeField] private TextMeshProUGUI m_opponentScoreText;
        [SerializeField] private TextMeshProUGUI m_timerText;
        [SerializeField] private TextMeshProUGUI m_ballSpeedText;
        [SerializeField] private TextMeshProUGUI m_ballSpinText;
        [SerializeField] private Image m_ballSpeedBar;
        [SerializeField] private Image m_ballSpinBar;

        [Header("主菜单元素")]
        [SerializeField] private Button m_startGameButton;
        [SerializeField] private Button m_settingsButton;
        [SerializeField] private Button m_quitButton;
        [SerializeField] private TMP_Dropdown m_gameModeDropdown;
        [SerializeField] private TMP_Dropdown m_difficultyDropdown;

        [Header("记分牌元素")]
        [SerializeField] private TextMeshProUGUI m_playerNameText;
        [SerializeField] private TextMeshProUGUI m_opponentNameText;
        [SerializeField] private TextMeshProUGUI m_playerTotalScoreText;
        [SerializeField] private TextMeshProUGUI m_opponentTotalScoreText;
        [SerializeField] private TextMeshProUGUI m_gameStatusText;
        [SerializeField] private Button m_rematchButton;
        [SerializeField] private Button m_mainMenuButton;

        private GameManager m_gameManager;
        private BallPhysics m_ballPhysics;

        private void Awake()
        {
            m_gameManager = FindObjectOfType<GameManager>();
            m_ballPhysics = FindObjectOfType<BallPhysics>();

            SetupUI();
            SetupEventListeners();
        }

        private void SetupUI()
        {
            // 初始化UI面板
            ShowMainMenu();
            HideHUD();
            HideScoreboard();

            // 初始化下拉菜单
            if (m_gameModeDropdown != null)
            {
                m_gameModeDropdown.ClearOptions();
                m_gameModeDropdown.AddOptions(new System.Collections.Generic.List<string> { "单打", "双打", "练习模式" });
            }

            if (m_difficultyDropdown != null)
            {
                m_difficultyDropdown.ClearOptions();
                m_difficultyDropdown.AddOptions(new System.Collections.Generic.List<string> { "简单", "中等", "困难" });
            }
        }

        private void SetupEventListeners()
        {
            // 主菜单按钮事件
            if (m_startGameButton != null)
                m_startGameButton.onClick.AddListener(OnStartGameClicked);
            if (m_settingsButton != null)
                m_settingsButton.onClick.AddListener(OnSettingsClicked);
            if (m_quitButton != null)
                m_quitButton.onClick.AddListener(OnQuitClicked);

            // 记分牌按钮事件
            if (m_rematchButton != null)
                m_rematchButton.onClick.AddListener(OnRematchClicked);
            if (m_mainMenuButton != null)
                m_mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        private void Update()
        {
            if (m_gameManager != null && m_gameManager.IsGameActive)
            {
                UpdateHUD();
            }
        }

        private void UpdateHUD()
        {
            // 更新分数
            if (m_playerScoreText != null)
                m_playerScoreText.text = m_gameManager.PlayerScore.ToString();
            if (m_opponentScoreText != null)
                m_opponentScoreText.text = m_gameManager.OpponentScore.ToString();

            // 更新计时器
            if (m_timerText != null)
                m_timerText.text = FormatTime(m_gameManager.GameTime);

            // 更新球的速度和旋转
            if (m_ballPhysics != null)
            {
                float speed = m_ballPhysics.Velocity.magnitude;
                float spin = m_ballPhysics.AngularVelocity.magnitude;

                if (m_ballSpeedText != null)
                    m_ballSpeedText.text = speed.ToString("F1");
                if (m_ballSpinText != null)
                    m_ballSpinText.text = spin.ToString("F1");
                if (m_ballSpeedBar != null)
                    m_ballSpeedBar.fillAmount = speed / m_ballPhysics.BallData.MaxSpeed;
                if (m_ballSpinBar != null)
                    m_ballSpinBar.fillAmount = spin / m_ballPhysics.BallData.MaxSpin;
            }
        }

        private string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        // UI显示控制
        public void ShowHUD()
        {
            if (m_hudPanel != null)
                m_hudPanel.SetActive(true);
        }

        public void HideHUD()
        {
            if (m_hudPanel != null)
                m_hudPanel.SetActive(false);
        }

        public void ShowMainMenu()
        {
            if (m_mainMenuPanel != null)
                m_mainMenuPanel.SetActive(true);
        }

        public void HideMainMenu()
        {
            if (m_mainMenuPanel != null)
                m_mainMenuPanel.SetActive(false);
        }

        public void ShowScoreboard()
        {
            if (m_scoreboardPanel != null)
                m_scoreboardPanel.SetActive(true);
        }

        public void HideScoreboard()
        {
            if (m_scoreboardPanel != null)
                m_scoreboardPanel.SetActive(false);
        }

        // 按钮事件处理
        private void OnStartGameClicked()
        {
            if (m_gameManager != null)
            {
                m_gameManager.StartGame(m_gameModeDropdown.value == 0); // 0表示单打模式
                HideMainMenu();
                ShowHUD();
            }
        }

        private void OnSettingsClicked()
        {
            // TODO: 实现设置菜单
        }

        private void OnQuitClicked()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        private void OnRematchClicked()
        {
            if (m_gameManager != null)
            {
                m_gameManager.RestartGame();
                HideScoreboard();
                ShowHUD();
            }
        }

        private void OnMainMenuClicked()
        {
            if (m_gameManager != null)
            {
                m_gameManager.SetState(PongHub.Gameplay.GameManager.GameState.MainMenu);
                HideScoreboard();
                ShowMainMenu();
            }
        }

        // 更新记分牌
        public void UpdateScoreboard(string playerName, string opponentName, int playerScore, int opponentScore, string gameStatus)
        {
            if (m_playerNameText != null)
                m_playerNameText.text = playerName;
            if (m_opponentNameText != null)
                m_opponentNameText.text = opponentName;
            if (m_playerTotalScoreText != null)
                m_playerTotalScoreText.text = playerScore.ToString();
            if (m_opponentTotalScoreText != null)
                m_opponentTotalScoreText.text = opponentScore.ToString();
            if (m_gameStatusText != null)
                m_gameStatusText.text = gameStatus;
        }
    }
}