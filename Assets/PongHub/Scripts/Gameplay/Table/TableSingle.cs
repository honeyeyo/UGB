using UnityEngine;
using PongHub.Gameplay.Ball;
using PongHub.Core;
using PongHub.Core.Audio;
using PongHub.AI;

namespace PongHub.Gameplay.Table
{
    /// <summary>
    /// 单人模式球桌组件
    /// 管理单机模式下的球桌功能，不涉及网络同步
    /// </summary>
    [RequireComponent(typeof(Table))]
    public class TableSingle : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private Table m_table;

        [Header("单机模式设置")]
        [SerializeField] private bool m_enableAI = true;
        [SerializeField] private bool m_enableMirrorMode = true;
        [SerializeField] private bool m_enablePracticeMode = true;

        [Header("练习模式配置")]
        [SerializeField] private bool m_showTrajectoryLine = true;
        [SerializeField] private bool m_enableSlowMotion = false;
        [SerializeField] private float m_slowMotionScale = 0.5f;

        [Header("调试显示")]
        [SerializeField] private bool m_showDebugInfo = false;
        [SerializeField] private bool m_showCollisionGizmos = false;

        // 单机模式状态
        private SinglePlayerMode m_currentMode = SinglePlayerMode.Practice;
        private bool m_isInitialized = false;

        // 练习统计
        private int m_totalHits = 0;
        private int m_consecutiveHits = 0;
        private float m_practiceStartTime = 0f;

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            InitializeSingleMode();
        }

        private void Update()
        {
            if (m_isInitialized)
            {
                UpdateSingleMode();
            }
        }

        private void OnDrawGizmos()
        {
            if (m_showCollisionGizmos && m_table != null)
            {
                DrawTableGizmos();
            }
        }
        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            if (m_table == null)
            {
                m_table = GetComponent<Table>();
            }

            if (m_table == null)
            {
                Debug.LogError($"[TableSingle] Table component not found on {gameObject.name}");
                return;
            }
        }

        private void InitializeSingleMode()
        {
            if (m_table == null) return;

            // 设置单机模式特有的配置
            ConfigureSingleMode();

            // 初始化练习统计
            ResetPracticeStats();

            m_isInitialized = true;

            LogDebug("TableSingle initialized successfully");
        }

        private void ConfigureSingleMode()
        {
            // 单机模式下的特殊配置
            // 例如：调整物理参数、启用特殊效果等

            if (m_showTrajectoryLine)
            {
                // 启用轨迹线显示
                // TODO: 实现轨迹线显示功能
            }
        }
        #endregion

        #region Single Mode Management
        private void UpdateSingleMode()
        {
            UpdatePracticeStats();

            // 根据当前模式更新不同的逻辑
            switch (m_currentMode)
            {
                case SinglePlayerMode.Practice:
                    UpdatePracticeMode();
                    break;
                case SinglePlayerMode.AIMatch:
                    UpdateAIMode();
                    break;
                case SinglePlayerMode.MirrorMatch:
                    UpdateMirrorMode();
                    break;
            }
        }

        private void UpdatePracticeMode()
        {
            // 练习模式特有的更新逻辑
            // VR环境下的慢动作可以通过其他方式触发，比如UI按钮或手势
            // 暂时移除键盘输入，后续可以添加VR手柄输入
            if (m_enableSlowMotion && ShouldActivateSlowMotion())
            {
                Time.timeScale = m_slowMotionScale;
            }
            else
            {
                Time.timeScale = 1f;
            }
        }

        private bool ShouldActivateSlowMotion()
        {
            // TODO: 实现VR手柄的慢动作触发逻辑
            // 例如：同时按下两个手柄的特定按钮
            // 或者通过UI界面切换
            return false; // 暂时禁用，避免键盘输入
        }

        private void UpdateAIMode()
        {
            // AI模式的更新逻辑
            // 这里会与AIPlayer组件协作
        }

        private void UpdateMirrorMode()
        {
            // 镜像模式的更新逻辑
            // 记录和回放玩家动作
        }
        #endregion

        #region Mode Switching
        public void StartPracticeMode()
        {
            if (!m_enablePracticeMode) return;

            m_currentMode = SinglePlayerMode.Practice;
            ResetPracticeStats();

            LogDebug("Practice mode started");
        }

        public void StartAIMatch()
        {
            if (!m_enableAI) return;

            m_currentMode = SinglePlayerMode.AIMatch;

            // 通知AI系统开始
            var modeManager = ModeSingleManager.Instance;
            if (modeManager?.AISystem != null)
            {
                modeManager.AISystem.StartAIMatch();
            }

            LogDebug("AI match started");
        }

        public void StartMirrorMatch()
        {
            if (!m_enableMirrorMode) return;

            m_currentMode = SinglePlayerMode.MirrorMatch;

            LogDebug("Mirror match started");
        }

        public void StopCurrentMode()
        {
            switch (m_currentMode)
            {
                case SinglePlayerMode.AIMatch:
                    var modeManager = ModeSingleManager.Instance;
                    if (modeManager?.AISystem != null)
                    {
                        modeManager.AISystem.StopAIMatch();
                    }
                    break;
            }

            m_currentMode = SinglePlayerMode.Practice;
            LogDebug("Stopped current mode, returned to practice");
        }
        #endregion

        #region Practice Statistics
        private void UpdatePracticeStats()
        {
            // 更新练习统计数据
            // 这些数据可以用于显示练习进度
        }

        private void ResetPracticeStats()
        {
            m_totalHits = 0;
            m_consecutiveHits = 0;
            m_practiceStartTime = Time.time;
        }

        public void OnBallHitTable(PongHub.Gameplay.Ball.Ball ball, Vector3 hitPoint)
        {
            m_totalHits++;
            m_consecutiveHits++;

            LogDebug($"Ball hit table. Total: {m_totalHits}, Consecutive: {m_consecutiveHits}");
        }

        public void OnBallMissedTable(PongHub.Gameplay.Ball.Ball ball)
        {
            m_consecutiveHits = 0;
            LogDebug("Ball missed table. Consecutive hits reset.");
        }
        #endregion

        #region Properties and Getters
        public SinglePlayerMode CurrentMode => m_currentMode;
        public bool IsInitialized => m_isInitialized;
        public Table Table => m_table;

        // 练习统计属性
        public int TotalHits => m_totalHits;
        public int ConsecutiveHits => m_consecutiveHits;
        public float PracticeTime => Time.time - m_practiceStartTime;

        // 模式可用性
        public bool IsAIEnabled => m_enableAI;
        public bool IsMirrorModeEnabled => m_enableMirrorMode;
        public bool IsPracticeModeEnabled => m_enablePracticeMode;
        #endregion

        #region Debug and Visualization
        private void DrawTableGizmos()
        {
            if (m_table?.TableData == null) return;

            var tableData = m_table.TableData;

            // 绘制球桌边界
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                transform.position + tableData.GetTableCenter(),
                new Vector3(tableData.Width, 0.1f, tableData.Length)
            );

            // 绘制网的位置
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(
                transform.position + tableData.GetNetPosition(),
                new Vector3(tableData.Width, tableData.NetHeight, 0.1f)
            );
        }

        private void LogDebug(string message)
        {
            if (m_showDebugInfo)
            {
                Debug.Log($"[TableSingle] {message}");
            }
        }
        #endregion

        #region Public Configuration
        public void SetAIEnabled(bool enabled)
        {
            m_enableAI = enabled;
        }

        public void SetMirrorModeEnabled(bool enabled)
        {
            m_enableMirrorMode = enabled;
        }

        public void SetPracticeModeEnabled(bool enabled)
        {
            m_enablePracticeMode = enabled;
        }

        public void SetSlowMotionEnabled(bool enabled)
        {
            m_enableSlowMotion = enabled;
        }

        public void SetSlowMotionScale(float scale)
        {
            m_slowMotionScale = Mathf.Clamp01(scale);
        }

        public void SetTrajectoryLineEnabled(bool enabled)
        {
            m_showTrajectoryLine = enabled;
        }
        #endregion
    }

    /// <summary>
    /// 单人游戏模式枚举
    /// </summary>
    public enum SinglePlayerMode
    {
        Practice,    // 练习模式
        AIMatch,     // AI对战
        MirrorMatch  // 镜像对战
    }
}