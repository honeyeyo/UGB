// Copyright (c) Meta Platforms, Inc. and affiliates.

#if HAS_META_AVATARS

using Oculus.Avatar2;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Meta.Utilities.Input
{
    /// <summary>
    /// XR输入管理器
    /// 继承自OvrAvatarInputManager，负责管理XR设备的输入系统
    /// 包括头显、控制器的跟踪数据和输入控制数据的处理
    /// </summary>
    public class XRInputManager : OvrAvatarInputManager
    {
        /// <summary>
        /// 日志作用域标识符
        /// </summary>
        private const string LOG_SCOPE = "xrInput";

        /// <summary>
        /// OVR相机装置引用（可选）
        /// 如果添加，将直接使用OVRCameraRig的输入而不是进行自己的计算
        /// </summary>
        [SerializeField]
        [Tooltip("Optional. If added, it will use input directly from OVRCameraRig instead of doing its own calculations.")]
        private OVRCameraRig m_ovrCameraRig;

        /// <summary>
        /// XR输入控制动作配置
        /// 定义了所有控制器按钮、轴向和触摸输入的映射
        /// </summary>
        [SerializeField] private XRInputControlActions m_controlActions;

        // 仅在编辑器中使用，在打包时会产生警告
#pragma warning disable CS0414 // is assigned but its value is never used
        /// <summary>
        /// 是否在场景视图中绘制跟踪位置的调试信息
        /// 仅在Unity编辑器中有效
        /// </summary>
        [SerializeField]
        private bool m_debugDrawTrackingLocations;
#pragma warning restore CS0414 // is assigned but its value is never used

        /// <summary>
        /// 组件唤醒时的初始化
        /// 设置场景视图的调试绘制回调，启用控制动作
        /// </summary>
        protected void Awake()
        {
            // 调试绘制设置
#if UNITY_EDITOR
#if UNITY_2019_3_OR_NEWER
            SceneView.duringSceneGui += OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
#endif

            // 启用输入控制动作
            m_controlActions.EnableActions();
        }

        /// <summary>
        /// 跟踪系统初始化完成后的回调
        /// 创建输入跟踪委托和输入控制委托，设置相应的提供者
        /// </summary>
        protected override void OnTrackingInitialized()
        {
            // 创建XR输入跟踪委托，处理头显和控制器的位置跟踪
            var inputTrackingDelegate = new XRInputTrackingDelegate(m_ovrCameraRig, true);
            // 创建XR输入控制委托，处理按钮、触摸和轴向输入
            var inputControlDelegate = new XRInputControlDelegate(m_controlActions);

            // 设置委托提供者
            _inputTrackingProvider = new OvrAvatarInputTrackingDelegatedProvider(inputTrackingDelegate);
            _inputControlProvider = new OvrAvatarInputControlDelegatedProvider(inputControlDelegate);
        }

        /// <summary>
        /// 组件销毁时的清理
        /// 取消注册场景视图的调试绘制回调
        /// </summary>
        protected override void OnDestroyCalled()
        {
#if UNITY_EDITOR
#if UNITY_2019_3_OR_NEWER
            SceneView.duringSceneGui -= OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
#endif
#endif

            base.OnDestroyCalled();
        }

        /// <summary>
        /// 获取指定手部的控制器动作集合
        /// </summary>
        /// <param name="forLeftHand">是否为左手，true为左手，false为右手</param>
        /// <returns>对应手部的控制器动作配置</returns>
        public XRInputControlActions.Controller GetActions(bool forLeftHand)
        {
            return forLeftHand ? m_controlActions.LeftController : m_controlActions.RightController;
        }

        /// <summary>
        /// 获取指定手部的控制器锚点变换
        /// </summary>
        /// <param name="forLeftHand">是否为左手，true为左手，false为右手</param>
        /// <returns>对应手部的控制器锚点Transform</returns>
        public Transform GetAnchor(bool forLeftHand)
        {
            return forLeftHand ? m_ovrCameraRig.leftHandAnchor : m_ovrCameraRig.rightHandAnchor;
        }

#if UNITY_EDITOR

        #region Debug Drawing

        /// <summary>
        /// 场景视图GUI绘制回调
        /// 在Unity编辑器的场景视图中绘制跟踪位置的调试信息
        /// </summary>
        /// <param name="sceneView">场景视图实例</param>
        private void OnSceneGUI(SceneView sceneView)
        {
            if (m_debugDrawTrackingLocations) DrawTrackingLocations();
        }

        /// <summary>
        /// 绘制跟踪位置的调试信息
        /// 在场景视图中显示头显和控制器的位置、朝向信息
        /// </summary>
        private void DrawTrackingLocations()
        {
            if (InputTrackingProvider == null)
            {
                return;
            }

            var inputTrackingState = InputTrackingProvider.State;

            var radius = 0.2f;
            Quaternion orientation;

            // 绘制头显位置（蓝色）
            Handles.color = Color.blue;
            _ = Handles.RadiusHandle(Quaternion.identity, inputTrackingState.headset.position, radius);

            orientation = inputTrackingState.headset.orientation;
            Handles.DrawLine((Vector3)inputTrackingState.headset.position + Forward() * radius,
                (Vector3)inputTrackingState.headset.position + Forward() * OuterRadius());

            radius = 0.1f;

            // 绘制左控制器位置（黄色）
            Handles.color = Color.yellow;
            _ = Handles.RadiusHandle(Quaternion.identity, inputTrackingState.leftController.position, radius);

            orientation = inputTrackingState.leftController.orientation;
            Handles.DrawLine((Vector3)inputTrackingState.leftController.position + Forward() * radius,
                (Vector3)inputTrackingState.leftController.position + Forward() * OuterRadius());

            // 绘制右控制器位置（黄色）
            Handles.color = Color.yellow;
            _ = Handles.RadiusHandle(Quaternion.identity, inputTrackingState.rightController.position, radius);

            orientation = inputTrackingState.rightController.orientation;
            Handles.DrawLine((Vector3)inputTrackingState.rightController.position + Forward() * radius,
                (Vector3)inputTrackingState.rightController.position + Forward() * OuterRadius());
            return;

            // 局部辅助函数
            Vector3 Forward() => orientation * Vector3.forward;

            float OuterRadius() => radius + 0.25f;
        }

        #endregion

#endif // UNITY_EDITOR
    }
}

#endif
