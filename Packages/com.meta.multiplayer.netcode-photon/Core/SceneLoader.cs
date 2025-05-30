// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// 场景加载器
    /// 处理场景加载并跟踪当前已加载的场景
    /// 通过NetCode NetworkManager加载场景以支持网络同步
    /// </summary>
    public class SceneLoader
    {
        /// <summary>
        /// 当前场景名称
        /// 静态变量，保存当前已加载的场景名称
        /// </summary>
        private static string s_currentScene = null;

        /// <summary>
        /// 场景是否已加载完成
        /// 指示当前场景是否已完成加载过程
        /// </summary>
        public bool SceneLoaded { get; private set; } = false;

        /// <summary>
        /// 场景加载器构造函数
        /// 注册场景加载完成事件监听器
        /// </summary>
        public SceneLoader() => SceneManager.sceneLoaded += OnSceneLoaded;

        /// <summary>
        /// 析构函数
        /// 取消注册场景加载事件监听器，防止内存泄漏
        /// </summary>
        ~SceneLoader()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// 场景加载完成回调
        /// 当场景加载完成时由Unity的SceneManager调用
        /// </summary>
        /// <param name="scene">已加载的场景</param>
        /// <param name="mode">场景加载模式</param>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 标记场景已加载完成
            SceneLoaded = true;

            // 更新当前场景名称
            s_currentScene = scene.name;

            // 设置新加载的场景为活跃场景
            _ = SceneManager.SetActiveScene(scene);
        }

        /// <summary>
        /// 加载指定场景
        /// 根据网络状态决定使用NetworkManager还是直接使用SceneManager加载场景
        /// </summary>
        /// <param name="scene">要加载的场景名称</param>
        /// <param name="useNetManager">是否使用网络管理器加载场景，默认为true</param>
        public void LoadScene(string scene, bool useNetManager = true)
        {
            // 记录场景加载信息到控制台
            Debug.Log($"LoadScene({scene}) (currentScene = {s_currentScene}, IsClient = {NetworkManager.Singleton.IsClient})");

            // 如果要加载的场景已经是当前场景，直接返回
            if (scene == s_currentScene) return;

            // 标记场景开始加载，尚未完成
            SceneLoaded = false;

            // 如果使用网络管理器且当前是客户端状态
            if (useNetManager && NetworkManager.Singleton.IsClient)
            {
                // 使用网络管理器的场景管理器加载场景
                // 这确保了多人游戏中的场景同步
                _ = NetworkManager.Singleton.SceneManager.LoadScene(scene, LoadSceneMode.Single);
                return;
            }

            // 否则直接使用Unity的场景管理器异步加载场景
            // 通常用于单人模式或者特殊情况
            _ = SceneManager.LoadSceneAsync(scene);
        }
    }
}