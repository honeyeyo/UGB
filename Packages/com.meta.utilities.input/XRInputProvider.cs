// Copyright (c) Meta Platforms, Inc. and affiliates.

#if HAS_META_AVATARS

using Meta.Utilities;
using Meta.Utilities.Input;
using UnityEngine;

namespace Meta.Decommissioned.Input
{
    /// <summary>
    /// XR输入提供者（已弃用）
    /// 单例模式的XR输入管理器包装器
    /// 提供对XRInputManager的全局访问
    /// </summary>
    public class XRInputProvider : Singleton<XRInputProvider>
    {
        /// <summary>
        /// XR输入管理器实例
        /// 自动设置的序列化字段，提供只读的公共访问
        /// </summary>
        [field: SerializeField, AutoSet]
        public XRInputManager InputManager { get; private set; }
    }
}

#endif
