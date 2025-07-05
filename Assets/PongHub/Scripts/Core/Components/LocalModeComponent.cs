using UnityEngine;

namespace PongHub.Core
{
    /// <summary>
    /// 单机模式组件
    /// 管理单机模式下的游戏逻辑，包括AI对手、本地物理模拟等
    /// </summary>
    public class LocalModeComponent : MonoBehaviour, IGameModeComponent
    {
        [Header("单机模式设置")]
        [SerializeField]
        [Tooltip("Enable AI / 启用AI - Enable AI opponent in local mode")]
        private bool m_enableAI = true;

        [SerializeField]
        [Tooltip("Local Only Objects / 本地模式专用对象 - GameObjects that are only active in local mode")]
        private GameObject[] m_localOnlyObjects;

        [SerializeField]
        [Tooltip("Local Only Components / 本地模式专用组件 - MonoBehaviours that are only enabled in local mode")]
        private MonoBehaviour[] m_localOnlyComponents;

        [Header("调试设置")]
        [SerializeField]
        [Tooltip("Debug Mode / 调试模式 - Enable debug logging for local mode operations")]
        private bool m_debugMode = false;

        #region IGameModeComponent 实现

        public void OnGameModeChanged(GameMode newMode, GameMode previousMode)
        {
            bool shouldBeActive = IsActiveInMode(newMode);

            if (m_debugMode)
            {
                Debug.Log($"[LocalModeComponent] 模式切换: {previousMode} -> {newMode}, 激活状态: {shouldBeActive}");
            }

            // 启用/禁用本地模式专用对象
            foreach (var obj in m_localOnlyObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(shouldBeActive);
                }
            }

            // 启用/禁用本地模式专用组件
            foreach (var component in m_localOnlyComponents)
            {
                if (component != null)
                {
                    component.enabled = shouldBeActive;
                }
            }

            // 根据模式配置AI
            if (shouldBeActive)
            {
                EnableLocalMode();
            }
            else
            {
                DisableLocalMode();
            }
        }

        public bool IsActiveInMode(GameMode mode)
        {
            return mode == GameMode.Local;
        }

        #endregion

        #region Unity 生命周期

        private void Start()
        {
            // 注册到GameModeManager
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.RegisterComponent(this);
            }
            else
            {
                Debug.LogWarning("[LocalModeComponent] GameModeManager实例不存在，延迟注册");
                Invoke(nameof(RegisterWithDelay), 0.5f);
            }
        }

        private void OnDestroy()
        {
            // 从GameModeManager注销
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.UnregisterComponent(this);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 延迟注册到GameModeManager
        /// </summary>
        private void RegisterWithDelay()
        {
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.RegisterComponent(this);
            }
            else
            {
                Debug.LogError("[LocalModeComponent] 无法找到GameModeManager实例");
            }
        }

        /// <summary>
        /// 启用单机模式
        /// </summary>
        private void EnableLocalMode()
        {
            if (m_debugMode)
            {
                Debug.Log("[LocalModeComponent] 启用单机模式");
            }

            // 启用AI对手（如果配置了）
            if (m_enableAI)
            {
                EnableAI();
            }

            // 配置本地物理模拟
            ConfigureLocalPhysics();

            // 其他单机模式特定的初始化
            InitializeLocalGameplay();
        }

        /// <summary>
        /// 禁用单机模式
        /// </summary>
        private void DisableLocalMode()
        {
            if (m_debugMode)
            {
                Debug.Log("[LocalModeComponent] 禁用单机模式");
            }

            // 禁用AI对手
            DisableAI();

            // 清理单机模式状态
            CleanupLocalGameplay();
        }

        /// <summary>
        /// 启用AI对手
        /// </summary>
        private void EnableAI()
        {
            // TODO: 实现AI启用逻辑
            // 例如：启用AI控制器，设置AI难度等
            if (m_debugMode)
            {
                Debug.Log("[LocalModeComponent] AI对手已启用");
            }
        }

        /// <summary>
        /// 禁用AI对手
        /// </summary>
        private void DisableAI()
        {
            // TODO: 实现AI禁用逻辑
            if (m_debugMode)
            {
                Debug.Log("[LocalModeComponent] AI对手已禁用");
            }
        }

        /// <summary>
        /// 配置本地物理模拟
        /// </summary>
        private void ConfigureLocalPhysics()
        {
            // TODO: 配置本地物理参数
            // 例如：设置重力、碰撞检测、球的物理属性等
            if (m_debugMode)
            {
                Debug.Log("[LocalModeComponent] 本地物理模拟已配置");
            }
        }

        /// <summary>
        /// 初始化单机游戏玩法
        /// </summary>
        private void InitializeLocalGameplay()
        {
            // TODO: 初始化单机游戏特定的功能
            // 例如：重置分数、设置发球顺序、配置规则等
            if (m_debugMode)
            {
                Debug.Log("[LocalModeComponent] 单机游戏玩法已初始化");
            }
        }

        /// <summary>
        /// 清理单机游戏状态
        /// </summary>
        private void CleanupLocalGameplay()
        {
            // TODO: 清理单机模式的游戏状态
            if (m_debugMode)
            {
                Debug.Log("[LocalModeComponent] 单机游戏状态已清理");
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置AI启用状态
        /// </summary>
        /// <param name="enabled">是否启用AI</param>
        public void SetAIEnabled(bool enabled)
        {
            m_enableAI = enabled;

            // 如果当前处于单机模式，立即应用设置
            if (GameModeManager.Instance != null &&
                GameModeManager.Instance.CurrentMode == GameMode.Local)
            {
                if (enabled)
                {
                    EnableAI();
                }
                else
                {
                    DisableAI();
                }
            }
        }

        #endregion
    }
}