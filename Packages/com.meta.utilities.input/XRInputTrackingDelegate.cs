// Copyright (c) Meta Platforms, Inc. and affiliates.

#if HAS_META_AVATARS

using Oculus.Avatar2;
using UnityEngine;

namespace Meta.Utilities.Input
{
    /// <summary>
    /// XR输入跟踪委托
    /// 继承自OvrAvatarInputTrackingDelegate，负责获取XR设备的位置跟踪数据
    /// 包括头显和控制器的位置、旋转信息
    /// </summary>
    public class XRInputTrackingDelegate : OvrAvatarInputTrackingDelegate
    {
        /// <summary>
        /// OVR相机装置引用，提供跟踪空间和设备锚点信息
        /// </summary>
        protected OVRCameraRig m_ovrCameraRig = null;

        /// <summary>
        /// 控制器是否启用标志
        /// </summary>
        protected bool m_controllersEnabled = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ovrCameraRig">OVR相机装置实例</param>
        /// <param name="controllersEnabled">是否启用控制器跟踪</param>
        public XRInputTrackingDelegate(OVRCameraRig ovrCameraRig, bool controllersEnabled)
        {
            m_ovrCameraRig = ovrCameraRig;
            m_controllersEnabled = controllersEnabled;
        }

        /// <summary>
        /// 获取原始输入跟踪状态
        /// 从OVRCameraRig读取头显和控制器的位置、旋转数据
        /// </summary>
        /// <param name="inputTrackingState">输出的跟踪状态数据</param>
        /// <returns>是否成功获取跟踪状态</returns>
        public override bool GetRawInputTrackingState(out OvrAvatarInputTrackingState inputTrackingState)
        {
            // 初始化跟踪状态
            inputTrackingState = default;

            // 设置设备激活状态
            inputTrackingState.headsetActive = true;
            inputTrackingState.leftControllerActive = m_controllersEnabled;
            inputTrackingState.rightControllerActive = m_controllersEnabled;
            inputTrackingState.leftControllerVisible = false;
            inputTrackingState.rightControllerVisible = false;

            // 转换并设置各设备的变换数据
            inputTrackingState.headset = ConvertTransform(m_ovrCameraRig.trackingSpace, m_ovrCameraRig.centerEyeAnchor);
            inputTrackingState.leftController = ConvertTransform(m_ovrCameraRig.trackingSpace, m_ovrCameraRig.leftControllerAnchor);
            inputTrackingState.rightController = ConvertTransform(m_ovrCameraRig.trackingSpace, m_ovrCameraRig.rightControllerAnchor);
            return true;
        }

        /// <summary>
        /// 转换Unity Transform为Avatar2变换格式
        /// 将设备锚点的世界变换转换为相对于跟踪空间的局部变换
        /// </summary>
        /// <param name="trackingSpace">跟踪空间变换</param>
        /// <param name="centerEyeAnchor">设备锚点变换</param>
        /// <returns>Avatar2格式的变换数据</returns>
        private static CAPI.ovrAvatar2Transform ConvertTransform(Transform trackingSpace, Transform centerEyeAnchor)
        {
            // 计算从设备锚点到跟踪空间的变换矩阵
            var matrix = trackingSpace.worldToLocalMatrix * centerEyeAnchor.localToWorldMatrix;
            return new CAPI.ovrAvatar2Transform(
                matrix.GetPosition(),
                matrix.rotation,
                matrix.lossyScale
            );
        }
    }
}

#endif
