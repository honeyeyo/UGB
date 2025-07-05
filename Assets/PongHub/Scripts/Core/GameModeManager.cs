// Copyright (c) MagnusLab Inc. and affiliates.

using UnityEngine;
using System.Collections;
using PongHub.Gameplay.Table;
using PongHub.Gameplay.Ball;
using PongHub.Gameplay.Paddle;
using Unity.Netcode;
using System;
using System.Collections.Generic;
using PongHub.Utils;
using PongHub.Core.Components;

namespace PongHub.Core
{
    /// <summary>
    /// 游戏模式管理器
    /// 负责管理游戏的不同模式（单机、多人、菜单）以及相关组件的状态切换
    /// </summary>
    public class GameModeManager : MonoBehaviour
    {
        [Header("Game Mode Settings / 游戏模式设置")]
        [SerializeField]
        [Tooltip("Default Mode / 默认模式 - Default game mode to start with (Local/Network/Menu)")]
        private GameMode m_defaultMode = GameMode.Local;

        [SerializeField]
        [Tooltip("Debug Mode / 调试模式 - Enable debug logging for game mode operations")]
        private bool m_debugMode = false;

        [Header("Mode Switch Settings / 模式切换设置")]
        [SerializeField]
        [Tooltip("Switch Delay / 切换延迟 - Delay in seconds between mode switch operations")]
        private float m_switchDelay = 0.1f; // 模式切换延迟

        [Header("Game Area References / 游戏区域引用")]
        [SerializeField]
        [Tooltip("Game Area Root / 游戏区域根节点 - Root transform containing all game area objects")]
        private Transform m_gameAreaRoot;

        [SerializeField]
        [Tooltip("Table Reference / 桌子引用 - Reference to the ping pong table component")]
        private Table m_table;

        [SerializeField]
        [Tooltip("Ball Physics / 球物理 - Reference to the ball physics component")]
        private BallPhysics m_ballPhysics;

        [SerializeField]
        [Tooltip("Paddles Array / 球拍数组 - Array of paddle components for local/network modes")]
        private Paddle[] m_paddles;

        [Header("Local Mode Components / 单机模式组件")]
        [SerializeField]
        [Tooltip("Local Components / 本地组件 - Components active only in local/single-player mode")]
        private MonoBehaviour[] m_localModeComponents;

        [Header("Network Mode Components / 网络模式组件")]
        [SerializeField]
        [Tooltip("Network Components / 网络组件 - NetworkBehaviour components active only in multiplayer mode")]
        private NetworkBehaviour[] m_networkModeComponents;

        [Header("Environment References / 环境引用（保持不变）")]
        [SerializeField]
        [Tooltip("Environment Root / 环境根节点 - Root transform for environment objects (always active)")]
        private Transform m_environmentRoot;

        [SerializeField]
        [Tooltip("Environment Lights / 环境灯光 - Array of lights that remain active across all modes")]
        private Light[] m_environmentLights;

        [SerializeField]
        [Tooltip("Environment Audio / 环境音频 - Array of audio sources for ambient sounds")]
        private AudioSource[] m_environmentAudio;

        // 单例实例
        public static GameModeManager Instance { get; private set; }

        // 当前游戏模式
        public GameMode CurrentMode { get; private set; }

        // 模式改变事件
        public event Action<GameMode, GameMode> OnModeChanged;

        // 注册的组件列表
        private readonly List<IGameModeComponent> m_registeredComponents = new();

        // 模式切换过程中的标志
        private bool m_isSwitching = false;

        // 自动发现的组件缓存
        private readonly Dictionary<System.Type, List<IGameModeComponent>> m_componentCache = new();

        // 事务性切换状态
        private GameMode m_pendingMode = GameMode.Local;
        private bool m_hasTransaction = false;

        // 性能监控
        private float m_lastSwitchTime = 0f;
        private int m_switchCount = 0;

        #region Unity 生命周期

        private void Awake()
        {
            // 单例模式实现
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[GameModeManager] 检测到重复实例，销毁 {gameObject.name}");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 初始化当前模式
            CurrentMode = m_defaultMode;

            if (m_debugMode)
            {
                Debug.Log($"[GameModeManager] 初始化完成，默认模式: {CurrentMode}");
            }
        }

        private void Start()
        {
            // 延迟一帧确保所有组件都已初始化
            Invoke(nameof(InitializeDefaultMode), 0.1f);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 切换到指定的游戏模式
        /// </summary>
        /// <param name="newMode">目标游戏模式</param>
        /// <param name="force">是否强制切换（忽略当前切换状态）</param>
        public void SwitchToMode(GameMode newMode, bool force = false)
        {
            if (!force && m_isSwitching)
            {
                Debug.LogWarning($"[GameModeManager] 正在切换模式中，忽略新的切换请求: {newMode}");
                return;
            }

            if (CurrentMode == newMode && !force)
            {
                if (m_debugMode)
                {
                    Debug.Log($"[GameModeManager] 已经是目标模式: {newMode}");
                }
                return;
            }

            StartCoroutine(SwitchModeCoroutine(newMode));
        }

        /// <summary>
        /// 注册游戏模式组件
        /// </summary>
        /// <param name="component">要注册的组件</param>
        public void RegisterComponent(IGameModeComponent component)
        {
            if (component == null)
            {
                Debug.LogError("[GameModeManager] 尝试注册空的游戏模式组件");
                return;
            }

            if (m_registeredComponents.Contains(component))
            {
                Debug.LogWarning($"[GameModeManager] 组件已经注册: {component.GetType().Name}");
                return;
            }

            m_registeredComponents.Add(component);

            // 如果当前已经有激活模式，立即通知新组件
            if (CurrentMode != GameMode.Menu) // Menu是临时状态，不通知
            {
                try
                {
                    component.OnGameModeChanged(CurrentMode, CurrentMode);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameModeManager] 组件 {component.GetType().Name} 初始化失败: {e.Message}");
                }
            }

            if (m_debugMode)
            {
                Debug.Log($"[GameModeManager] 注册组件: {component.GetType().Name}");
            }
        }

        /// <summary>
        /// 注销游戏模式组件
        /// </summary>
        /// <param name="component">要注销的组件</param>
        public void UnregisterComponent(IGameModeComponent component)
        {
            if (component == null)
            {
                return;
            }

            if (m_registeredComponents.Remove(component))
            {
                if (m_debugMode)
                {
                    Debug.Log($"[GameModeManager] 注销组件: {component.GetType().Name}");
                }
            }
        }

        /// <summary>
        /// 获取当前注册的组件数量
        /// </summary>
        public int GetRegisteredComponentCount()
        {
            return m_registeredComponents.Count;
        }

        /// <summary>
        /// 检查是否正在切换模式
        /// </summary>
        public bool IsSwitching => m_isSwitching;

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化默认模式
        /// </summary>
        private void InitializeDefaultMode()
        {
            if (m_debugMode)
            {
                Debug.Log($"[GameModeManager] 初始化默认模式: {m_defaultMode}");
            }

            // 通知所有已注册的组件
            NotifyComponents(m_defaultMode, GameMode.Menu);
        }

        /// <summary>
        /// 模式切换协程
        /// </summary>
        private System.Collections.IEnumerator SwitchModeCoroutine(GameMode newMode)
        {
            m_isSwitching = true;
            GameMode previousMode = CurrentMode;

            if (m_debugMode)
            {
                Debug.Log($"[GameModeManager] 开始切换模式: {previousMode} -> {newMode}");
            }

            // 切换延迟
            if (m_switchDelay > 0)
            {
                yield return new WaitForSeconds(m_switchDelay);
            }

            // 更新当前模式
            CurrentMode = newMode;

            // 使用优化的组件切换
            OptimizeComponentSwitching(newMode, previousMode);

            // 触发模式改变事件
            OnModeChanged?.Invoke(newMode, previousMode);

            m_isSwitching = false;

            if (m_debugMode)
            {
                Debug.Log($"[GameModeManager] 模式切换完成: {previousMode} -> {newMode}");
            }
        }

        /// <summary>
        /// 通知所有注册的组件模式变化
        /// </summary>
        private void NotifyComponents(GameMode newMode, GameMode previousMode)
        {
            var componentsToRemove = new List<IGameModeComponent>();

            foreach (var component in m_registeredComponents)
            {
                if (component == null)
                {
                    componentsToRemove.Add(component);
                    continue;
                }

                try
                {
                    component.OnGameModeChanged(newMode, previousMode);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameModeManager] 组件 {component.GetType().Name} 模式切换失败: {e.Message}");
                }
            }

            // 清理空引用
            foreach (var nullComponent in componentsToRemove)
            {
                m_registeredComponents.Remove(nullComponent);
            }

            if (m_debugMode && componentsToRemove.Count > 0)
            {
                Debug.Log($"[GameModeManager] 清理了 {componentsToRemove.Count} 个空引用组件");
            }
        }

        #endregion

        #region 调试方法

        /// <summary>
        /// 获取调试信息
        /// </summary>
        public string GetDebugInfo()
        {
            return $"CurrentMode: {CurrentMode}, " +
                   $"RegisteredComponents: {m_registeredComponents.Count}, " +
                   $"IsSwitching: {m_isSwitching}";
        }

        #endregion

        /// <summary>
        /// 获取环境对象（这些对象在模式切换时保持不变）
        /// </summary>
        public Transform GetEnvironmentRoot()
        {
            return m_environmentRoot;
        }

        /// <summary>
        /// 自动分配组件引用
        /// </summary>
        [ContextMenu("Auto Assign References")]
        private void AutoAssignReferences()
        {
            if (m_gameAreaRoot == null)
                m_gameAreaRoot = GameObject.Find("Game Area")?.transform;

            if (m_environmentRoot == null)
                m_environmentRoot = GameObject.Find("Environment")?.transform;

            if (m_table == null)
                m_table = FindObjectOfType<Table>();

            if (m_ballPhysics == null)
                m_ballPhysics = FindObjectOfType<BallPhysics>();

            if (m_paddles == null || m_paddles.Length == 0)
                m_paddles = FindObjectsOfType<Paddle>();

            Debug.Log("✓ GameModeManager 引用自动分配完成");
        }

        #region 组件自动发现功能

        /// <summary>
        /// 自动发现并注册场景中的游戏模式组件
        /// </summary>
        [ContextMenu("Auto Discover Components")]
        public void AutoDiscoverComponents()
        {
            if (m_debugMode)
            {
                Debug.Log("[GameModeManager] 开始自动发现组件");
            }

            // 清理缓存
            m_componentCache.Clear();

            // 发现LocalModeComponent
            var localComponents = FindObjectsOfType<LocalModeComponent>();
            CacheComponents(typeof(LocalModeComponent), localComponents);

            // 发现NetworkModeComponent
            var networkComponents = FindObjectsOfType<NetworkModeComponent>();
            CacheComponents(typeof(NetworkModeComponent), networkComponents);

            // 注册所有发现的组件
            int registeredCount = 0;
            foreach (var componentList in m_componentCache.Values)
            {
                foreach (var component in componentList)
                {
                    if (!m_registeredComponents.Contains(component))
                    {
                        RegisterComponent(component);
                        registeredCount++;
                    }
                }
            }

            if (m_debugMode)
            {
                Debug.Log($"[GameModeManager] 自动发现完成，注册了 {registeredCount} 个新组件，" +
                         $"缓存了 {m_componentCache.Count} 种类型");
            }
        }

        /// <summary>
        /// 缓存组件到类型字典中
        /// </summary>
        private void CacheComponents<T>(System.Type type, T[] components) where T : MonoBehaviour, IGameModeComponent
        {
            var componentList = new List<IGameModeComponent>();
            foreach (var component in components)
            {
                componentList.Add(component);
            }

            m_componentCache[type] = componentList;

            if (m_debugMode)
            {
                Debug.Log($"[GameModeManager] 缓存了 {components.Length} 个 {type.Name} 组件");
            }
        }

        /// <summary>
        /// 根据类型获取缓存的组件
        /// </summary>
        public List<IGameModeComponent> GetComponentsByType(System.Type type)
        {
            return m_componentCache.TryGetValue(type, out var components) ? components : new List<IGameModeComponent>();
        }

        /// <summary>
        /// 获取指定模式下激活的组件数量
        /// </summary>
        public int GetActiveComponentCount(GameMode mode)
        {
            int count = 0;
            foreach (var component in m_registeredComponents)
            {
                if (component != null && component.IsActiveInMode(mode))
                {
                    count++;
                }
            }
            return count;
        }

        #endregion

        #region 事务性模式切换

        /// <summary>
        /// 开始事务性模式切换
        /// </summary>
        public void BeginModeTransaction(GameMode targetMode)
        {
            if (m_hasTransaction)
            {
                Debug.LogWarning($"[GameModeManager] 已有活跃事务，目标模式: {m_pendingMode}");
                return;
            }

            m_pendingMode = targetMode;
            m_hasTransaction = true;

            if (m_debugMode)
            {
                Debug.Log($"[GameModeManager] 开始模式切换事务: {CurrentMode} -> {targetMode}");
            }
        }

        /// <summary>
        /// 提交事务性模式切换
        /// </summary>
        public void CommitModeTransaction()
        {
            if (!m_hasTransaction)
            {
                Debug.LogWarning("[GameModeManager] 没有活跃的模式切换事务");
                return;
            }

            if (m_debugMode)
            {
                Debug.Log($"[GameModeManager] 提交模式切换事务: {m_pendingMode}");
            }

            SwitchToMode(m_pendingMode);
            m_hasTransaction = false;
        }

        /// <summary>
        /// 回滚事务性模式切换
        /// </summary>
        public void RollbackModeTransaction()
        {
            if (!m_hasTransaction)
            {
                Debug.LogWarning("[GameModeManager] 没有活跃的模式切换事务");
                return;
            }

            if (m_debugMode)
            {
                Debug.Log($"[GameModeManager] 回滚模式切换事务: {m_pendingMode}");
            }

            m_hasTransaction = false;
            m_pendingMode = CurrentMode;
        }

        /// <summary>
        /// 检查是否有活跃的事务
        /// </summary>
        public bool HasActiveTransaction => m_hasTransaction;

        /// <summary>
        /// 获取待处理的模式
        /// </summary>
        public GameMode PendingMode => m_pendingMode;

        #endregion

        #region 性能优化和监控

        /// <summary>
        /// 优化组件状态切换性能
        /// </summary>
        private void OptimizeComponentSwitching(GameMode newMode, GameMode previousMode)
        {
            // 记录性能数据
            m_lastSwitchTime = Time.time;
            m_switchCount++;

            // 按优先级排序组件，重要组件先切换
            var prioritizedComponents = new List<IGameModeComponent>(m_registeredComponents);
            prioritizedComponents.Sort((a, b) => GetComponentPriority(a).CompareTo(GetComponentPriority(b)));

            // 分批处理组件以避免帧率下降
            StartCoroutine(BatchProcessComponents(prioritizedComponents, newMode, previousMode));
        }

        /// <summary>
        /// 获取组件优先级（数值越小优先级越高）
        /// </summary>
        private int GetComponentPriority(IGameModeComponent component)
        {
            if (component is LocalModeComponent) return 1;
            if (component is NetworkModeComponent) return 2;
            return 10; // 默认优先级
        }

        /// <summary>
        /// 分批处理组件协程
        /// </summary>
        private System.Collections.IEnumerator BatchProcessComponents(List<IGameModeComponent> components, GameMode newMode, GameMode previousMode)
        {
            const int batchSize = 5; // 每批处理5个组件
            var componentsToRemove = new List<IGameModeComponent>();

            for (int i = 0; i < components.Count; i += batchSize)
            {
                // 处理当前批次
                for (int j = i; j < Mathf.Min(i + batchSize, components.Count); j++)
                {
                    var component = components[j];
                    if (component == null)
                    {
                        componentsToRemove.Add(component);
                        continue;
                    }

                    try
                    {
                        component.OnGameModeChanged(newMode, previousMode);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[GameModeManager] 组件 {component.GetType().Name} 模式切换失败: {e.Message}");
                    }
                }

                // 每批处理后暂停一帧
                yield return null;
            }

            // 清理空引用
            foreach (var nullComponent in componentsToRemove)
            {
                m_registeredComponents.Remove(nullComponent);
            }
        }

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        public string GetPerformanceStats()
        {
            return $"模式切换次数: {m_switchCount}\n" +
                   $"上次切换时间: {m_lastSwitchTime:F2}s\n" +
                   $"注册组件数量: {m_registeredComponents.Count}\n" +
                   $"缓存组件类型: {m_componentCache.Count}\n" +
                   $"当前事务状态: {(m_hasTransaction ? $"活跃({m_pendingMode})" : "无")}";
        }

        /// <summary>
        /// 重置性能统计
        /// </summary>
        [ContextMenu("Reset Performance Stats")]
        public void ResetPerformanceStats()
        {
            m_switchCount = 0;
            m_lastSwitchTime = 0f;

            if (m_debugMode)
            {
                Debug.Log("[GameModeManager] 性能统计已重置");
            }
        }

        #endregion

        #region 增强调试功能

        /// <summary>
        /// 获取详细调试信息
        /// </summary>
        public string GetDetailedDebugInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine($"=== GameModeManager 调试信息 ===");
            info.AppendLine($"当前模式: {CurrentMode}");
            info.AppendLine($"正在切换: {m_isSwitching}");
            info.AppendLine($"注册组件数量: {m_registeredComponents.Count}");

            // 按模式分组显示组件
            foreach (GameMode mode in System.Enum.GetValues(typeof(GameMode)))
            {
                int activeCount = GetActiveComponentCount(mode);
                info.AppendLine($"{mode}模式激活组件: {activeCount}");
            }

            info.AppendLine(GetPerformanceStats());

            return info.ToString();
        }

        /// <summary>
        /// 验证系统完整性
        /// </summary>
        [ContextMenu("Validate System Integrity")]
        public bool ValidateSystemIntegrity()
        {
            bool isValid = true;
            var issues = new List<string>();

            // 检查必要的引用
            if (m_gameAreaRoot == null)
            {
                issues.Add("Game Area Root 引用缺失");
                isValid = false;
            }

            if (m_environmentRoot == null)
            {
                issues.Add("Environment Root 引用缺失");
                isValid = false;
            }

            // 检查组件完整性
            foreach (var component in m_registeredComponents)
            {
                if (component == null)
                {
                    issues.Add("发现空引用组件");
                    isValid = false;
                }
            }

            // 检查是否至少有一个本地模式组件
            bool hasLocalComponent = GetActiveComponentCount(GameMode.Local) > 0;
            if (!hasLocalComponent)
            {
                issues.Add("缺少本地模式组件");
                isValid = false;
            }

            // 检查是否至少有一个网络模式组件
            bool hasNetworkComponent = GetActiveComponentCount(GameMode.Network) > 0;
            if (!hasNetworkComponent)
            {
                issues.Add("缺少网络模式组件");
                isValid = false;
            }

            if (m_debugMode)
            {
                if (isValid)
                {
                    Debug.Log("[GameModeManager] ✓ 系统完整性验证通过");
                }
                else
                {
                    Debug.LogWarning($"[GameModeManager] ✗ 系统完整性验证失败:\n{string.Join("\n", issues)}");
                }
            }

            return isValid;
        }

        #endregion
    }
}