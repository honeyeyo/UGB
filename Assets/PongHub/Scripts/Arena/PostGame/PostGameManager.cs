// Copyright (c) MagnusLab Inc. and affiliates.

using UnityEngine;
using PongHub.Arena.Gameplay;

namespace PongHub.Arena.PostGame
{
    /// <summary>
    /// PostGame管理器
    /// 作为所有PostGame相关组件的协调器，简化配置和管理
    /// </summary>
    public class PostGameManager : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField]
        [Tooltip("Post Game Controller / 赛后控制器 - Controller component managing post-game UI")]
        private PostGameController m_postGameController;

        [SerializeField]
        [Tooltip("Statistics Tracker / 统计跟踪器 - Component tracking game statistics")]
        private GameStatisticsTracker m_statisticsTracker;

        [SerializeField]
        [Tooltip("Technical Stats Panel / 技术统计面板 - Panel displaying detailed technical statistics")]
        private TechnicalStatsPanel m_technicalStatsPanel;

        [Header("UI容器")]
        [SerializeField]
        [Tooltip("Post Game Container / 赛后容器 - Root container for all post-game UI elements")]
        private GameObject m_postGameContainer;

        // 私有字段
        private bool m_isInitialized;

        #region Unity生命周期

        private void Awake()
        {
            ValidateComponents();
        }

        private void Start()
        {
            Initialize();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 验证组件
        /// </summary>
        private void ValidateComponents()
        {
            // 自动查找组件（如果没有手动设置）
            if (m_postGameController == null)
                m_postGameController = GetComponentInChildren<PostGameController>();

            if (m_statisticsTracker == null)
                m_statisticsTracker = FindObjectOfType<GameStatisticsTracker>();

            if (m_technicalStatsPanel == null)
                m_technicalStatsPanel = GetComponentInChildren<TechnicalStatsPanel>();

            // 警告缺失组件
            if (m_postGameController == null)
                Debug.LogWarning("[PostGameManager] 缺少PostGameController组件");

            if (m_statisticsTracker == null)
                Debug.LogWarning("[PostGameManager] 缺少GameStatisticsTracker组件");

            if (m_technicalStatsPanel == null)
                Debug.LogWarning("[PostGameManager] 缺少TechnicalStatsPanel组件");
        }

        /// <summary>
        /// 初始化管理器
        /// </summary>
        private void Initialize()
        {
            // 设置事件监听
            if (m_statisticsTracker != null)
            {
                GameStatisticsTracker.OnStatisticsUpdated += OnStatisticsUpdated;
            }

            // 初始化组件
            if (m_technicalStatsPanel != null)
            {
                m_technicalStatsPanel.ResetPanel();
            }

            m_isInitialized = true;
            Debug.Log("[PostGameManager] PostGame管理器初始化完成");
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 统计数据更新事件处理
        /// </summary>
        private void OnStatisticsUpdated(GameStatistics stats)
        {
            // 更新技术统计面板
            if (m_technicalStatsPanel != null)
            {
                m_technicalStatsPanel.UpdateStats(stats);
            }
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 显示PostGame界面
        /// </summary>
        public void ShowPostGame(GameStatistics stats, bool isMatchComplete)
        {
            if (!m_isInitialized)
            {
                Debug.LogWarning("[PostGameManager] 管理器未初始化");
                return;
            }

            // 激活容器
            if (m_postGameContainer != null)
                m_postGameContainer.SetActive(true);

            // 更新技术统计面板
            if (m_technicalStatsPanel != null)
            {
                m_technicalStatsPanel.UpdateStats(stats);
                m_technicalStatsPanel.Show();
            }

            // 显示主控制器
            if (m_postGameController != null)
            {
                m_postGameController.ShowPostGameUI(stats, isMatchComplete);
            }

            Debug.Log($"[PostGameManager] 显示PostGame界面 - {stats.GetWinnerText()}");
        }

        /// <summary>
        /// 隐藏PostGame界面
        /// </summary>
        public void HidePostGame()
        {
            // 隐藏技术统计面板
            if (m_technicalStatsPanel != null)
            {
                m_technicalStatsPanel.Hide();
            }

            // 隐藏主控制器
            if (m_postGameController != null)
            {
                m_postGameController.HidePostGameUI();
            }

            // 停用容器
            if (m_postGameContainer != null)
                m_postGameContainer.SetActive(false);

            Debug.Log("[PostGameManager] 隐藏PostGame界面");
        }

        /// <summary>
        /// 重置PostGame状态
        /// </summary>
        public void ResetPostGame()
        {
            if (m_technicalStatsPanel != null)
            {
                m_technicalStatsPanel.ResetPanel();
            }

            if (m_statisticsTracker != null)
            {
                m_statisticsTracker.ResetMatchStatistics();
            }

            Debug.Log("[PostGameManager] 重置PostGame状态");
        }

        #endregion

        #region 获取器

        /// <summary>
        /// 获取PostGame控制器
        /// </summary>
        public PostGameController GetPostGameController()
        {
            return m_postGameController;
        }

        /// <summary>
        /// 获取统计数据跟踪器
        /// </summary>
        public GameStatisticsTracker GetStatisticsTracker()
        {
            return m_statisticsTracker;
        }

        /// <summary>
        /// 获取技术统计面板
        /// </summary>
        public TechnicalStatsPanel GetTechnicalStatsPanel()
        {
            return m_technicalStatsPanel;
        }

        #endregion

        #region 销毁处理

        private void OnDestroy()
        {
            // 取消事件监听
            if (m_statisticsTracker != null)
            {
                GameStatisticsTracker.OnStatisticsUpdated -= OnStatisticsUpdated;
            }
        }

        #endregion

        #region 编辑器辅助方法

#if UNITY_EDITOR
        /// <summary>
        /// 在编辑器中自动设置组件引用
        /// </summary>
        [ContextMenu("自动设置组件引用")]
        private void AutoSetupComponents()
        {
            ValidateComponents();
            Debug.Log("[PostGameManager] 已自动设置组件引用");
        }

        /// <summary>
        /// 创建默认的PostGame结构
        /// </summary>
        [ContextMenu("创建默认PostGame结构")]
        private void CreateDefaultPostGameStructure()
        {
            // 这里可以添加代码来自动创建PostGame的UI结构
            Debug.Log("[PostGameManager] 创建默认PostGame结构功能需要进一步实现");
        }
#endif

        #endregion
    }
}