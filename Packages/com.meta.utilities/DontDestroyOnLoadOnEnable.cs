// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Meta.Utilities
{
    /// <summary>
    /// 场景加载时不销毁组件
    /// 在组件启用时自动调用DontDestroyOnLoad，确保该游戏对象
    /// 在场景切换时不被销毁，适用于需要跨场景保持的对象
    /// </summary>
    public class DontDestroyOnLoadOnEnable : MonoBehaviour
    {
        /// <summary>
        /// 组件启用时的处理
        /// 将当前游戏对象标记为场景加载时不销毁
        /// </summary>
        protected void OnEnable()
        {
            DontDestroyOnLoad(this);
        }
    }
}