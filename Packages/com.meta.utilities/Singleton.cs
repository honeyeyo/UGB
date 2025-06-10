// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Meta.Utilities
{
    /// <summary>
    /// 泛型单例模式基类
    /// 继承自MonoBehaviour，确保在整个应用程序生命周期内只有一个实例存在
    /// 提供全局访问点和实例化时的回调机制
    /// </summary>
    /// <typeparam name="T">继承自Singleton的具体类型</typeparam>
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        /// <summary>
        /// 单例实例
        /// 提供对唯一实例的静态访问
        /// </summary>
        public static T Instance { get; private set; }

        /// <summary>
        /// 实例化时的回调动作
        /// 用于在单例实例创建后执行特定操作
        /// </summary>
        private static System.Action<T> s_onAwake;

        /// <summary>
        /// 当单例实例化时执行指定动作
        /// 如果实例已存在则立即执行，否则延迟到实例创建时执行
        /// </summary>
        /// <param name="action">要执行的动作，接收单例实例作为参数</param>
        public static void WhenInstantiated(System.Action<T> action)
        {
            if (Instance != null)
                action(Instance);
            else
                s_onAwake += action;
        }

        /// <summary>
        /// MonoBehaviour的Awake生命周期方法
        /// 初始化单例实例，确保只有一个实例存在
        /// </summary>
        protected void Awake()
        {
            // 如果组件被禁用则不进行初始化
            if (!enabled)
                return;

            // 断言确保不会有多个实例同时存在
            Debug.Assert(Instance == null, $"Singleton {typeof(T).Name} has been instantiated more than once.", this);
            Instance = (T)this;

            // 调用内部初始化方法
            InternalAwake();

            // 执行所有待执行的回调动作
            s_onAwake?.Invoke(Instance);
            s_onAwake = null;
        }

        /// <summary>
        /// MonoBehaviour的OnEnable生命周期方法
        /// 确保在组件启用时正确设置单例实例
        /// </summary>
        protected void OnEnable()
        {
            if (Instance != this)
                Awake();
        }

        /// <summary>
        /// 内部初始化方法
        /// 子类可以重写此方法来执行特定的初始化逻辑
        /// </summary>
        protected virtual void InternalAwake() { }
    }
}