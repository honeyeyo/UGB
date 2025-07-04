using UnityEngine;
using PongHub.Gameplay.Table;
using PongHub.Gameplay.Ball;
using PongHub.Gameplay.Paddle;
using PongHub.AI;
using System;

namespace PongHub.Core
{
    /// <summary>
    /// 单人模式游戏管理器
    /// 统一管理各种单机游戏模式的切换和状态
    /// </summary>
    public class ModeSingleManager : MonoBehaviour
    {
        public static ModeSingleManager Instance { get; private set; }

        [Header("组件引用")]
        [SerializeField]
        [Tooltip("Table Single / 单人球桌 - Table component for single player mode")]
        private TableSingle m_tableSingle;

        [SerializeField]
        [Tooltip("Ball Manager / 球管理器 - Ball manager for single player mode")]
        private BallSingleManager m_ballManager;

        [SerializeField]
        [Tooltip("AI System / AI系统 - AI system for single player mode")]
        private AISingle m_aiSystem;

        [Header("模式配置")]
        [SerializeField]
        [Tooltip("Default Mode / 默认模式 - Default game mode to start with")]
        private SingleGameMode m_defaultMode = SingleGameMode.Practice;

        [SerializeField]
        [Tooltip("Auto Start Default Mode / 自动启动默认模式 - Whether to automatically start default mode")]
        private bool m_autoStartDefaultMode = true;

        [Header("练习模式设置")]
        [SerializeField]
        [Tooltip("Enable Practice Statistics / 启用练习统计 - Whether to enable practice mode statistics")]
        private bool m_enablePracticeStatistics = true;
        // [SerializeField] private bool m_showPracticeUI = true;

        [Header("AI模式设置")]
        [SerializeField]
        [Tooltip("Default AI Difficulty / 默认AI难度 - Default difficulty level for AI mode")]
        private float m_defaultAIDifficulty = 0.5f;

        [SerializeField]
        [Tooltip("Enable AI Visualization / 启用AI可视化 - Whether to enable AI visualization")]
        private bool m_enableAIVisualization = true;

        [Header("调试设置")]
        [SerializeField]
        [Tooltip("Enable Debug Log / 启用调试日志 - Whether to enable debug logging")]
        private bool m_enableDebugLog = false;
        // [SerializeField] private bool m_showModeUI = true;

        // 当前状态
        private SingleGameMode m_currentMode = SingleGameMode.None;
        private bool m_isInitialized = false;
        private bool m_isModeActive = false;

        // 模式统计
        private float m_modeStartTime = 0f;
        private int m_modeChanges = 0;

        // 事件
        public event Action<SingleGameMode> OnModeChanged;
        public event Action<SingleGameMode> OnModeStarted;
        public event Action<SingleGameMode> OnModeStopped;

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            StartManager();
        }

        private void Update()
        {
            if (m_isInitialized && m_isModeActive)
            {
                UpdateCurrentMode();
            }
        }
        #endregion

        #region Initialization
        private void InitializeManager()
        {
            // 自动查找组件
            FindComponents();

            // 验证组件
            ValidateComponents();

            m_isInitialized = true;
            LogDebug("ModeSingleManager initialized");
        }

        private void FindComponents()
        {
            if (m_tableSingle == null)
                m_tableSingle = FindObjectOfType<TableSingle>();

            if (m_ballManager == null)
                m_ballManager = FindObjectOfType<BallSingleManager>();

            if (m_aiSystem == null)
                m_aiSystem = FindObjectOfType<AISingle>();
        }

        private void ValidateComponents()
        {
            if (m_tableSingle == null)
                LogWarning("TableSingle component not found!");

            if (m_ballManager == null)
                LogWarning("BallSingleManager component not found!");

            if (m_aiSystem == null)
                LogWarning("AISingle component not found!");
        }

        private void StartManager()
        {
            if (!m_isInitialized) return;

            // 自动启动默认模式
            if (m_autoStartDefaultMode && m_defaultMode != SingleGameMode.None)
            {
                SwitchToMode(m_defaultMode);
            }

            LogDebug("ModeSingleManager started");
        }
        #endregion

        #region Mode Management
        private void UpdateCurrentMode()
        {
            // 根据当前模式执行特定的更新逻辑
            switch (m_currentMode)
            {
                case SingleGameMode.Practice:
                    UpdatePracticeMode();
                    break;
                case SingleGameMode.AIMatch:
                    UpdateAIMode();
                    break;
                case SingleGameMode.MirrorMatch:
                    UpdateMirrorMode();
                    break;
            }
        }

        private void UpdatePracticeMode()
        {
            // 练习模式更新逻辑
            if (m_tableSingle != null && m_enablePracticeStatistics)
            {
                // 更新练习统计
                // 可以在这里处理练习进度、成就等
            }
        }

        private void UpdateAIMode()
        {
            // AI模式更新逻辑
            if (m_aiSystem != null && !m_aiSystem.IsActive)
            {
                // AI系统异常停止，重新启动
                LogWarning("AI system stopped unexpectedly, restarting...");
                m_aiSystem.StartAIMatch();
            }
        }

        private void UpdateMirrorMode()
        {
            // 镜像模式更新逻辑
            // TODO: 实现镜像模式的具体逻辑
        }

        public void SwitchToMode(SingleGameMode newMode)
        {
            if (m_currentMode == newMode) return;

            LogDebug($"Switching from {m_currentMode} to {newMode}");

            // 停止当前模式
            StopCurrentModeInternal();

            // 切换到新模式
            StartNewMode(newMode);

            // 更新状态
            var previousMode = m_currentMode;
            m_currentMode = newMode;
            m_modeStartTime = Time.time;
            m_modeChanges++;

            // 触发事件
            OnModeChanged?.Invoke(newMode);
            OnModeStarted?.Invoke(newMode);

            LogDebug($"Successfully switched to {newMode}");
        }

        private void StopCurrentModeInternal()
        {
            if (m_currentMode == SingleGameMode.None) return;

            LogDebug($"Stopping mode: {m_currentMode}");

            switch (m_currentMode)
            {
                case SingleGameMode.Practice:
                    StopPracticeMode();
                    break;
                case SingleGameMode.AIMatch:
                    StopAIMode();
                    break;
                case SingleGameMode.MirrorMatch:
                    StopMirrorMode();
                    break;
            }

            OnModeStopped?.Invoke(m_currentMode);
            m_isModeActive = false;
        }

        private void StartNewMode(SingleGameMode mode)
        {
            LogDebug($"Starting mode: {mode}");

            switch (mode)
            {
                case SingleGameMode.Practice:
                    StartPracticeMode();
                    break;
                case SingleGameMode.AIMatch:
                    StartAIMode();
                    break;
                case SingleGameMode.MirrorMatch:
                    StartMirrorMode();
                    break;
                case SingleGameMode.None:
                    // 不做任何操作
                    break;
            }

            m_isModeActive = (mode != SingleGameMode.None);
        }
        #endregion

        #region Specific Mode Control
        private void StartPracticeMode()
        {
            if (m_tableSingle != null)
            {
                m_tableSingle.StartPracticeMode();
            }

            if (m_ballManager != null)
            {
                m_ballManager.StartPracticeMode();
            }

            // 停止AI系统
            if (m_aiSystem != null && m_aiSystem.IsActive)
            {
                m_aiSystem.StopAIMatch();
            }

            LogDebug("Practice mode started");
        }

        private void StopPracticeMode()
        {
            if (m_tableSingle != null)
            {
                m_tableSingle.StopCurrentMode();
            }

            LogDebug("Practice mode stopped");
        }

        private void StartAIMode()
        {
            if (m_tableSingle != null)
            {
                m_tableSingle.StartAIMatch();
            }

            if (m_aiSystem != null)
            {
                m_aiSystem.SetDifficulty(m_defaultAIDifficulty);
                m_aiSystem.StartAIMatch();
            }

            if (m_ballManager != null)
            {
                // 确保有球可供AI对战
                if (m_ballManager.CurrentBall == null)
                {
                    m_ballManager.SpawnBall();
                }
            }

            LogDebug("AI mode started");
        }

        private void StopAIMode()
        {
            if (m_aiSystem != null)
            {
                m_aiSystem.StopAIMatch();
            }

            if (m_tableSingle != null)
            {
                m_tableSingle.StopCurrentMode();
            }

            LogDebug("AI mode stopped");
        }

        private void StartMirrorMode()
        {
            if (m_tableSingle != null)
            {
                m_tableSingle.StartMirrorMatch();
            }

            // TODO: 启动镜像录制系统

            LogDebug("Mirror mode started");
        }

        private void StopMirrorMode()
        {
            if (m_tableSingle != null)
            {
                m_tableSingle.StopCurrentMode();
            }

            // TODO: 停止镜像录制系统

            LogDebug("Mirror mode stopped");
        }
        #endregion

        #region Public Interface
        public void StartPractice()
        {
            SwitchToMode(SingleGameMode.Practice);
        }

        public void StartAIMatch(float difficulty = -1f)
        {
            if (difficulty >= 0f)
            {
                SetAIDifficulty(difficulty);
            }
            SwitchToMode(SingleGameMode.AIMatch);
        }

        public void StartMirrorMatch()
        {
            SwitchToMode(SingleGameMode.MirrorMatch);
        }

        public void StopCurrentMode()
        {
            SwitchToMode(SingleGameMode.None);
        }

        public void SetAIDifficulty(float difficulty)
        {
            m_defaultAIDifficulty = Mathf.Clamp01(difficulty);

            if (m_aiSystem != null)
            {
                m_aiSystem.SetDifficulty(m_defaultAIDifficulty);
            }

            LogDebug($"AI difficulty set to {m_defaultAIDifficulty:F2}");
        }

        public void RestartCurrentMode()
        {
            var currentMode = m_currentMode;
            SwitchToMode(SingleGameMode.None);
            SwitchToMode(currentMode);
        }

        public bool IsInMode(SingleGameMode mode)
        {
            return m_currentMode == mode && m_isModeActive;
        }

        public bool IsAnyModeActive()
        {
            return m_isModeActive && m_currentMode != SingleGameMode.None;
        }
        #endregion

        #region Properties and Statistics
        public SingleGameMode CurrentMode => m_currentMode;
        public bool IsInitialized => m_isInitialized;
        public bool IsModeActive => m_isModeActive;
        public float CurrentModeTime => m_isModeActive ? Time.time - m_modeStartTime : 0f;
        public int ModeChanges => m_modeChanges;

        // 组件引用
        public TableSingle TableSingle => m_tableSingle;
        public BallSingleManager BallManager => m_ballManager;
        public AISingle AISystem => m_aiSystem;

        // 模式可用性
        public bool IsPracticeModeAvailable => m_tableSingle?.IsPracticeModeEnabled ?? false;
        public bool IsAIModeAvailable => m_tableSingle?.IsAIEnabled ?? false && m_aiSystem != null;
        public bool IsMirrorModeAvailable => m_tableSingle?.IsMirrorModeEnabled ?? false;
        #endregion

        #region Configuration
        public void SetDefaultMode(SingleGameMode mode)
        {
            m_defaultMode = mode;
        }

        public void SetAutoStartDefaultMode(bool autoStart)
        {
            m_autoStartDefaultMode = autoStart;
        }

        public void SetPracticeStatisticsEnabled(bool enabled)
        {
            m_enablePracticeStatistics = enabled;
        }

        public void SetAIVisualizationEnabled(bool enabled)
        {
            m_enableAIVisualization = enabled;

            if (m_aiSystem != null)
            {
                // TODO: 设置AI可视化
            }
        }
        #endregion

        #region Debug and Logging
        private void LogDebug(string message)
        {
            if (m_enableDebugLog)
            {
                Debug.Log($"[ModeSingleManager] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[ModeSingleManager] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[ModeSingleManager] {message}");
        }
        #endregion
    }

    /// <summary>
    /// 单人游戏模式枚举
    /// </summary>
    public enum SingleGameMode
    {
        None,        // 无模式
        Practice,    // 练习模式
        AIMatch,     // AI对战
        MirrorMatch  // 镜像对战
    }
}