// Copyright (c) MagnusLab Inc. and affiliates.

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PongHub.Arena.Gameplay;
using PongHub.Arena.Services;
using PongHub.App;
using Unity.Netcode;

namespace PongHub.Arena.PostGame
{
    /// <summary>
    /// PostGame控制器
    /// 负责管理PostGame界面的显示、数据更新和用户交互
    /// </summary>
    public class PostGameController : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private GameObject m_postGamePanel;
        [SerializeField] private TMP_Text m_resultTitle;
        [SerializeField] private TMP_Text m_scoreDisplay;
        [SerializeField] private TMP_Text m_setProgressText;

        [Header("技术统计面板")]
        [SerializeField] private GameObject m_statsPanel;
        [SerializeField] private TMP_Text m_winnersStatsText;
        [SerializeField] private TMP_Text m_errorsStatsText;
        [SerializeField] private TMP_Text m_rallyStatsText;
        [SerializeField] private TMP_Text m_timeStatsText;

        [Header("按钮")]
        [SerializeField] private Button m_nextSetButton;
        [SerializeField] private Button m_spectatorButton;
        [SerializeField] private Button m_exitButton;

        [Header("效果")]
        [SerializeField] private GameObject m_fireworksContainer;
        [SerializeField] private AudioSource m_victoryAudio;

        [Header("设置")]
        [SerializeField] private bool m_enableAutoProgress = false;
        [SerializeField] private float m_autoProgressDelay = 10f;

        // 私有字段
        private GameStatistics m_currentStats;
        private bool m_isVisible;
        private float m_showTime;

        // 事件
        public static event Action OnNextSetRequested;
        public static event Action OnSpectatorModeRequested;
        public static event Action OnExitRequested;

        #region Unity生命周期

        private void Start()
        {
            InitializeUI();
            SetupButtonEvents();
        }

        private void Update()
        {
            if (m_isVisible && m_enableAutoProgress)
            {
                CheckAutoProgress();
            }
        }

        #endregion

        #region 初始化

        private void InitializeUI()
        {
            if (m_postGamePanel != null)
                m_postGamePanel.SetActive(false);

            m_isVisible = false;
        }

        private void SetupButtonEvents()
        {
            if (m_nextSetButton != null)
            {
                m_nextSetButton.onClick.AddListener(OnNextSetButtonClicked);
            }

            if (m_spectatorButton != null)
            {
                m_spectatorButton.onClick.AddListener(OnSpectatorButtonClicked);
            }

            if (m_exitButton != null)
            {
                m_exitButton.onClick.AddListener(OnExitButtonClicked);
            }
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 显示PostGame界面
        /// </summary>
        /// <param name="stats">游戏统计数据</param>
        /// <param name="isMatchComplete">是否完成整场比赛</param>
        public void ShowPostGameUI(GameStatistics stats, bool isMatchComplete)
        {
            m_currentStats = stats;
            m_showTime = Time.time;

            // 显示面板
            if (m_postGamePanel != null)
                m_postGamePanel.SetActive(true);

            // 更新显示内容
            UpdateResultDisplay(stats, isMatchComplete);
            UpdateStatsDisplay(stats);
            UpdateButtonStates(isMatchComplete);

            // 播放效果
            PlayVictoryEffects(stats);

            m_isVisible = true;

            Debug.Log($"[PostGameController] 显示PostGame界面 - {stats.GetWinnerText()}");
        }

        /// <summary>
        /// 隐藏PostGame界面
        /// </summary>
        public void HidePostGameUI()
        {
            if (m_postGamePanel != null)
                m_postGamePanel.SetActive(false);

            StopVictoryEffects();
            m_isVisible = false;

            Debug.Log("[PostGameController] 隐藏PostGame界面");
        }

        #endregion

        #region UI更新

        /// <summary>
        /// 更新比赛结果显示
        /// </summary>
        private void UpdateResultDisplay(GameStatistics stats, bool isMatchComplete)
        {
            // 更新标题
            if (m_resultTitle != null)
            {
                string titleText = isMatchComplete ?
                    $"比赛结束 - {stats.GetWinnerText()}" :
                    $"第{stats.CurrentSet}局 - {stats.GetWinnerText()}";
                m_resultTitle.text = titleText;

                // 设置获胜方颜色
                if (stats.CurrentSetWinner == NetworkedTeam.Team.TeamA)
                    m_resultTitle.color = Color.blue;
                else if (stats.CurrentSetWinner == NetworkedTeam.Team.TeamB)
                    m_resultTitle.color = Color.red;
                else
                    m_resultTitle.color = Color.white;
            }

            // 更新比分
            if (m_scoreDisplay != null)
            {
                m_scoreDisplay.text = stats.GetScoreText();
            }

            // 更新局数进度
            if (m_setProgressText != null)
            {
                m_setProgressText.text = stats.GetSetProgressText();
            }
        }

        /// <summary>
        /// 更新技术统计显示
        /// </summary>
        private void UpdateStatsDisplay(GameStatistics stats)
        {
            if (m_statsPanel != null)
                m_statsPanel.SetActive(true);

            if (m_winnersStatsText != null)
                m_winnersStatsText.text = $"制胜球: {stats.PlayerAWinners} - {stats.PlayerBWinners}";

            if (m_errorsStatsText != null)
                m_errorsStatsText.text = $"失误: {stats.PlayerAErrors} - {stats.PlayerBErrors}";

            if (m_rallyStatsText != null)
                m_rallyStatsText.text = $"最长回合: {stats.LongestRally}次";

            if (m_timeStatsText != null)
                m_timeStatsText.text = $"本局用时: {FormatTime(stats.SetDuration)}";
        }

        /// <summary>
        /// 更新按钮状态
        /// </summary>
        private void UpdateButtonStates(bool isMatchComplete)
        {
            // 设置"下一局"按钮
            if (m_nextSetButton != null)
            {
                m_nextSetButton.gameObject.SetActive(true);
                var buttonText = m_nextSetButton.GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                {
                    buttonText.text = isMatchComplete ? "新的比赛" : "下一局";
                }
            }

            // 设置"观众"按钮
            if (m_spectatorButton != null)
            {
                m_spectatorButton.gameObject.SetActive(true);
            }

            // 设置"退出"按钮
            if (m_exitButton != null)
            {
                m_exitButton.gameObject.SetActive(true);
            }
        }

        #endregion

        #region 效果播放

        /// <summary>
        /// 播放胜利效果
        /// </summary>
        private void PlayVictoryEffects(GameStatistics stats)
        {
            // 播放烟花效果
            if (m_fireworksContainer != null && stats.CurrentSetWinner != NetworkedTeam.Team.NoTeam)
            {
                m_fireworksContainer.SetActive(true);

                // 如果有FireworkController组件，触发烟花
                var fireworkController = m_fireworksContainer.GetComponent<PongHub.Arena.VFX.FireworkController>();
                if (fireworkController != null)
                {
                    fireworkController.enabled = true;
                }
            }

            // 播放音效
            if (m_victoryAudio != null && stats.CurrentSetWinner != NetworkedTeam.Team.NoTeam)
            {
                m_victoryAudio.Play();
            }
        }

        /// <summary>
        /// 停止胜利效果
        /// </summary>
        private void StopVictoryEffects()
        {
            if (m_fireworksContainer != null)
            {
                m_fireworksContainer.SetActive(false);
            }

            if (m_victoryAudio != null && m_victoryAudio.isPlaying)
            {
                m_victoryAudio.Stop();
            }
        }

        #endregion

        #region 按钮事件处理

        private void OnNextSetButtonClicked()
        {
            Debug.Log("[PostGameController] 请求开始下一局");
            OnNextSetRequested?.Invoke();
            HidePostGameUI();
        }

        private void OnSpectatorButtonClicked()
        {
            Debug.Log("[PostGameController] 请求切换到观众模式");
            SwitchToSpectatorMode();
        }

        private void OnExitButtonClicked()
        {
            Debug.Log("[PostGameController] 请求退出房间");
            OnExitRequested?.Invoke();
            ExitToMainMenu();
        }

        #endregion

        #region 功能实现

        /// <summary>
        /// 切换到观众模式
        /// </summary>
        private void SwitchToSpectatorMode()
        {
            try
            {
                var sessionManager = PongSessionManager.Instance;
                if (sessionManager == null)
                {
                    Debug.LogError("[PostGameController] 找不到PongSessionManager");
                    return;
                }

                var clientId = NetworkManager.Singleton.LocalClientId;
                var playerData = sessionManager.GetPlayerData(clientId);

                if (!playerData.HasValue)
                {
                    Debug.LogError("[PostGameController] 找不到玩家数据");
                    return;
                }

                // 销毁当前玩家对象
                var localPlayer = FindObjectOfType<LocalPlayerEntities>();
                if (localPlayer?.Avatar != null)
                {
                    var networkObject = localPlayer.Avatar.GetComponent<NetworkObject>();
                    if (networkObject != null)
                    {
                        networkObject.Despawn();
                    }
                }

                // 使用新的乒乓球生成系统切换到观众模式
                // 选择一个随机的观众席位（不指定特定队伍）
                var preferredTeam = UnityEngine.Random.Range(0, 2) == 0
                    ? NetworkedTeam.Team.TeamA
                    : NetworkedTeam.Team.TeamB;

                var spawningManager = FindObjectOfType<PongPlayerSpawningManager>();
                if (spawningManager != null)
                {
                    spawningManager.SwitchToSpectatorServerRpc(preferredTeam);
                }
                else
                {
                    Debug.LogError("[PostGameController] 找不到PongPlayerSpawningManager");
                }

                OnSpectatorModeRequested?.Invoke();
                HidePostGameUI();
            }
            catch (Exception e)
            {
                Debug.LogError($"[PostGameController] 切换观众模式时出错: {e.Message}");
            }
        }

        /// <summary>
        /// 退出到主菜单
        /// </summary>
        private void ExitToMainMenu()
        {
            try
            {
                // 断开网络连接
                if (NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.Shutdown();
                }

                // 返回主菜单
                var phApplication = PongHub.App.PHApplication.Instance;
                if (phApplication?.NavigationController != null)
                {
                    phApplication.NavigationController.GoToMainMenu();
                }
                else
                {
                    Debug.LogWarning("[PostGameController] 找不到NavigationController，尝试加载主菜单场景");
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PostGameController] 退出到主菜单时出错: {e.Message}");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查自动进度
        /// </summary>
        private void CheckAutoProgress()
        {
            if (Time.time - m_showTime >= m_autoProgressDelay)
            {
                Debug.Log("[PostGameController] 自动进入下一局");
                OnNextSetButtonClicked();
            }
        }

        /// <summary>
        /// 格式化时间显示
        /// </summary>
        private string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
            return $"{minutes:00}:{seconds:00}";
        }

        #endregion

        #region 销毁处理

        private void OnDestroy()
        {
            // 清理按钮事件
            if (m_nextSetButton != null)
                m_nextSetButton.onClick.RemoveListener(OnNextSetButtonClicked);

            if (m_spectatorButton != null)
                m_spectatorButton.onClick.RemoveListener(OnSpectatorButtonClicked);

            if (m_exitButton != null)
                m_exitButton.onClick.RemoveListener(OnExitButtonClicked);
        }

        #endregion
    }
}