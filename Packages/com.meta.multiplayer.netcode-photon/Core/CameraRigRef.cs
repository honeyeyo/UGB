// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.Utilities;
using Oculus.Avatar2;
using UnityEngine;

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// 相机装备引用单例
    /// 用于轻松访问相机装备和装备相关组件的单例类
    /// 提供全局访问OVR相机装备和Avatar输入管理器的便捷方式
    /// </summary>
    [RequireComponent(typeof(OVRCameraRig))]
    public class CameraRigRef : Singleton<CameraRigRef>
    {
        /// <summary>
        /// OVR相机装备组件
        /// Oculus VR相机装备，处理VR头显的位置和旋转跟踪
        /// 使用AutoSet特性自动设置组件引用
        /// </summary>
        [AutoSet] public OVRCameraRig CameraRig;

        /// <summary>
        /// Avatar输入管理器
        /// 管理Avatar系统的输入，处理手部追踪和控制器输入
        /// 用于Avatar动画和交互系统
        /// </summary>
        public OvrAvatarInputManager AvatarInputManager;
    }
}