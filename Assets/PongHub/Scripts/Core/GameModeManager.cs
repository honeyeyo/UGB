// Copyright (c) MagnusLab Inc. and affiliates.

using UnityEngine;
using System.Collections;
using PongHub.Gameplay.Table;
using PongHub.Gameplay.Ball;
using PongHub.Gameplay.Paddle;
using Unity.Netcode;

namespace PongHub.Core
{
    /// <summary>
    /// 游戏模式管理器
    /// 处理单机模式和网络模式之间的无缝切换
    /// 避免重复销毁和初始化环境对象，只切换游戏逻辑组件的状态
    /// </summary>
    public class GameModeManager : MonoBehaviour
    {
        /// <summary>
        /// 游戏模式枚举
        /// </summary>
        public enum GameMode
        {
            Local,      // 单机模式（练习、AI对战）
            Network     // 网络多人模式
        }

        [Header("模式设置")]
        [SerializeField] private GameMode m_currentMode = GameMode.Local;
        [SerializeField] private bool m_enableModeTransition = true;

        [Header("游戏区域引用")]
        [SerializeField] private Transform m_gameAreaRoot;
        [SerializeField] private Table m_table;
        [SerializeField] private BallPhysics m_ballPhysics;
        [SerializeField] private Paddle[] m_paddles;

        [Header("单机模式组件")]
        [SerializeField] private MonoBehaviour[] m_localModeComponents;

        [Header("网络模式组件")]
        [SerializeField] private NetworkBehaviour[] m_networkModeComponents;

        [Header("环境引用（保持不变）")]
        [SerializeField] private Transform m_environmentRoot;
        [SerializeField] private Light[] m_environmentLights;
        [SerializeField] private AudioSource[] m_environmentAudio;

        // 事件
        public static event System.Action<GameMode> OnModeChanged;

        private static GameModeManager s_instance;
        public static GameModeManager Instance => s_instance;

        public GameMode CurrentMode => m_currentMode;
        public bool IsLocalMode => m_currentMode == GameMode.Local;
        public bool IsNetworkMode => m_currentMode == GameMode.Network;

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
                return;
            }

            // 初始化为单机模式
            SetMode(GameMode.Local, false);
        }

        /// <summary>
        /// 设置游戏模式
        /// </summary>
        /// <param name="newMode">新的游戏模式</param>
        /// <param name="playTransition">是否播放过渡动画</param>
        public void SetMode(GameMode newMode, bool playTransition = true)
        {
            if (m_currentMode == newMode) return;

            Debug.Log($"GameModeManager: 切换模式 {m_currentMode} → {newMode}");

            if (playTransition && m_enableModeTransition)
            {
                StartCoroutine(TransitionToMode(newMode));
            }
            else
            {
                ApplyModeChange(newMode);
            }
        }

        /// <summary>
        /// 带过渡效果的模式切换
        /// </summary>
        private IEnumerator TransitionToMode(GameMode newMode)
        {
            // 淡出当前模式
            yield return StartCoroutine(FadeOutCurrentMode());

            // 应用新模式
            ApplyModeChange(newMode);

            // 淡入新模式
            yield return StartCoroutine(FadeInNewMode());
        }

        /// <summary>
        /// 应用模式更改
        /// </summary>
        private void ApplyModeChange(GameMode newMode)
        {
            var oldMode = m_currentMode;
            m_currentMode = newMode;

            switch (newMode)
            {
                case GameMode.Local:
                    EnableLocalMode();
                    break;
                case GameMode.Network:
                    EnableNetworkMode();
                    break;
            }

            // 通知模式改变
            OnModeChanged?.Invoke(newMode);
            Debug.Log($"✓ 模式切换完成: {oldMode} → {newMode}");
        }

        /// <summary>
        /// 启用单机模式
        /// </summary>
        private void EnableLocalMode()
        {
            Debug.Log("启用单机模式组件...");

            // 启用单机模式组件
            foreach (var component in m_localModeComponents)
            {
                if (component != null)
                {
                    component.enabled = true;
                }
            }

            // 禁用网络模式组件
            foreach (var component in m_networkModeComponents)
            {
                if (component != null)
                {
                    component.enabled = false;
                }
            }

            // 配置游戏对象为单机模式
            ConfigureGameObjectsForLocalMode();
        }

        /// <summary>
        /// 启用网络模式
        /// </summary>
        private void EnableNetworkMode()
        {
            Debug.Log("启用网络模式组件...");

            // 禁用单机模式组件
            foreach (var component in m_localModeComponents)
            {
                if (component != null)
                {
                    component.enabled = false;
                }
            }

            // 启用网络模式组件
            foreach (var component in m_networkModeComponents)
            {
                if (component != null)
                {
                    component.enabled = true;
                }
            }

            // 配置游戏对象为网络模式
            ConfigureGameObjectsForNetworkMode();
        }

        /// <summary>
        /// 配置游戏对象为单机模式
        /// </summary>
        private void ConfigureGameObjectsForLocalMode()
        {
            // 配置球桌
            if (m_table != null)
            {
                // 移除或禁用网络组件
                var networkObject = m_table.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.enabled = false;
                }
            }

            // 配置球
            if (m_ballPhysics != null)
            {
                // 设置为本地物理模拟
                var rigidbody = m_ballPhysics.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.isKinematic = false;
                }
            }

            // 配置球拍
            foreach (var paddle in m_paddles)
            {
                if (paddle != null)
                {
                    // 启用本地控制
                    paddle.enabled = true;
                }
            }
        }

        /// <summary>
        /// 配置游戏对象为网络模式
        /// </summary>
        private void ConfigureGameObjectsForNetworkMode()
        {
            // 配置球桌
            if (m_table != null)
            {
                // 启用网络组件
                var networkObject = m_table.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.enabled = true;
                }
            }

            // 配置球
            if (m_ballPhysics != null)
            {
                // 等待网络授权
                var rigidbody = m_ballPhysics.GetComponent<Rigidbody>();
                if (rigidbody != null && !NetworkManager.Singleton.IsServer)
                {
                    rigidbody.isKinematic = true; // 客户端不控制物理
                }
            }

            // 配置球拍
            foreach (var paddle in m_paddles)
            {
                if (paddle != null)
                {
                    // 根据网络权限配置
                    paddle.enabled = true; // 具体逻辑由paddle内部处理
                }
            }
        }

        /// <summary>
        /// 淡出当前模式
        /// </summary>
        private IEnumerator FadeOutCurrentMode()
        {
            // 可以添加视觉效果，比如屏幕淡出
            if (OVRScreenFade.instance != null)
            {
                OVRScreenFade.instance.FadeOut();
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
        }

        /// <summary>
        /// 淡入新模式
        /// </summary>
        private IEnumerator FadeInNewMode()
        {
            yield return new WaitForSeconds(0.1f);

            // 可以添加视觉效果，比如屏幕淡入
            if (OVRScreenFade.instance != null)
            {
                OVRScreenFade.instance.FadeIn();
            }
        }

        /// <summary>
        /// 快速切换到单机模式
        /// </summary>
        public void SwitchToLocalMode()
        {
            SetMode(GameMode.Local);
        }

        /// <summary>
        /// 快速切换到网络模式
        /// </summary>
        public void SwitchToNetworkMode()
        {
            SetMode(GameMode.Network);
        }

        /// <summary>
        /// 检查是否可以切换模式
        /// </summary>
        public bool CanSwitchMode()
        {
            // 检查网络状态等
            return m_enableModeTransition &&
                   (IsLocalMode || !NetworkManager.Singleton.IsConnectedClient);
        }

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