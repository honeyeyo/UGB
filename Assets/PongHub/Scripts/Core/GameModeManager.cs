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

            // 通知所有组件
            NotifyComponents(newMode, previousMode);

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
    }
}