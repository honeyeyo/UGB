// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meta.Utilities;
using Meta.Multiplayer.Core;
using Oculus.Avatar2;
using Oculus.Platform;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using static Oculus.Avatar2.CAPI;

namespace Meta.Multiplayer.Avatar
{
    /// <summary>
    /// Avatar实体管理器
    /// 处理Avatar的设置，为基类OvrAvatarEntity添加功能
    /// 处理关节加载回调，设置唇同步和身体追踪
    /// 与AvatarNetworking配合在多人项目中支持网络功能
    /// 在非网络设置中，它会跟踪相机装备以保持位置同步
    /// </summary>
    [DefaultExecutionOrder(50)] // 在GpuSkinningConfiguration初始化之后执行
    public class AvatarEntity : OvrAvatarEntity
    {
        /// <summary>
        /// 关节加载事件对结构体
        /// 定义当特定关节加载完成时要执行的操作
        /// </summary>
        [Serializable]
        public struct OnJointLoadedPair
        {
            /// <summary>要监听的关节类型</summary>
            public ovrAvatar2JointType Joint;
            /// <summary>要设置为子对象的目标变换</summary>
            public Transform TargetToSetAsChild;
            /// <summary>关节加载完成时触发的事件</summary>
            public UnityEvent<Transform> OnLoaded;
        }

        /// <summary>网络对象组件，自动设置</summary>
        [SerializeField, AutoSet] private NetworkObject m_networkObject;
        /// <summary>Avatar网络同步组件，自动设置</summary>
        [SerializeField, AutoSet] private AvatarNetworking m_networking;

        /// <summary>
        /// 唇同步行为组件
        /// 从子对象中自动设置，包括非活跃的对象
        /// </summary>
        [SerializeField, AutoSetFromChildren(IncludeInactive = true)]
        private OvrAvatarLipSyncBehavior m_lipSync;

        /// <summary>
        /// 如果没有网络连接时是否为本地Avatar
        /// 用于非网络环境下的Avatar设置
        /// </summary>
        [SerializeField] private bool m_isLocalIfNotNetworked;

        /// <summary>
        /// 关节加载事件列表
        /// 定义各种关节加载完成时要执行的操作
        /// </summary>
        public List<OnJointLoadedPair> OnJointLoadedEvents = new();

        /// <summary>
        /// 获取指定类型关节的变换组件
        /// </summary>
        /// <param name="jointType">关节类型</param>
        /// <returns>关节的变换组件</returns>
        public Transform GetJointTransform(ovrAvatar2JointType jointType) => GetSkeletonTransformByType(jointType);

        [Header("Face Pose Input")]
        /// <summary>面部姿态提供器，自动设置</summary>
        [SerializeField, AutoSet]
        private OvrAvatarFacePoseBehavior m_facePoseProvider;
        /// <summary>眼部姿态提供器，自动设置</summary>
        [SerializeField, AutoSet]
        private OvrAvatarEyePoseBehavior m_eyePoseProvider;

        /// <summary>初始化任务</summary>
        private Task m_initializationTask;
        /// <summary>设置访问令牌任务</summary>
        public Task m_setUpAccessTokenTask;

        /// <summary>
        /// 组件唤醒时初始化
        /// 设置访问令牌并启动面部和眼部追踪
        /// </summary>
        protected override void Awake()
        {
            // 设置访问令牌的异步任务
            m_setUpAccessTokenTask = SetUpAccessTokenAsync();
            base.Awake();

            // 启动面部和眼部追踪
            OVRPlugin.StartFaceTracking();
            OVRPlugin.StartEyeTracking();
        }

        /// <summary>
        /// 启动时处理非网络环境的初始化
        /// 如果没有网络管理器或不在监听状态，且设置为本地Avatar，则初始化
        /// </summary>
        private void Start()
        {
            if ((m_networkObject == null || m_networkObject.NetworkManager == null || !m_networkObject.NetworkManager.IsListening) && m_isLocalIfNotNetworked)
            {
                Initialize();
            }
        }

        /// <summary>
        /// 异步设置访问令牌
        /// 从Oculus平台获取访问令牌并设置给Avatar系统
        /// </summary>
        /// <returns>异步任务</returns>
        private async Task SetUpAccessTokenAsync()
        {
            var accessToken = await Users.GetAccessToken().Gen();
            OvrAvatarEntitlement.SetAccessToken(accessToken.Data);
        }

        /// <summary>
        /// 初始化Avatar
        /// 启动Avatar初始化过程，确保不会重复初始化
        /// </summary>
        public void Initialize()
        {
            var prevInit = m_initializationTask;
            m_initializationTask = Impl();

            /// <summary>
            /// 内部异步实现
            /// 等待之前的初始化完成，然后执行新的初始化
            /// </summary>
            async Task Impl()
            {
                if (prevInit != null)
                    await prevInit;
                await InitializeImpl();
            }
        }

        /// <summary>
        /// Avatar初始化的具体实现
        /// 根据是否为所有者设置不同的Avatar配置
        /// </summary>
        /// <returns>异步任务</returns>
        private async Task InitializeImpl()
        {
            // 清理现有状态
            Teardown();

            // 确定是否为所有者（本地Avatar）
            var isOwner = m_networkObject == null || (m_networkObject != null && !m_networkObject.NetworkManager.IsClient) ? m_isLocalIfNotNetworked : m_networkObject.IsOwner;

            // 设置为本地或远程Avatar
            SetIsLocal(isOwner);

            if (isOwner)
            {
                // 本地Avatar设置

                // 启用动画功能
                _creationInfo.features |= Oculus.Avatar2.CAPI.ovrAvatar2EntityFeatures.Animation;

                // 设置输入管理器
                var inputManager = CameraRigRef.Instance.AvatarInputManager;
                SetInputManager(inputManager);

                // 启用唇同步
                m_lipSync.gameObject.SetActive(true);
                SetLipSync(m_lipSync);

                // 设置面部和眼部姿态提供器
                SetFacePoseProvider(m_facePoseProvider);
                SetEyePoseProvider(m_eyePoseProvider);

                // 设置第一人称Avatar LOD
                AvatarLODManager.Instance.firstPersonAvatarLod = AvatarLOD;
            }
            else
            {
                // 远程Avatar设置

                // 禁用动画功能
                _creationInfo.features &= ~ovrAvatar2EntityFeatures.Animation;

                // 清除各种提供器
                SetInputManager(null);
                SetFacePoseProvider(null);
                SetEyePoseProvider(null);
                SetLipSync(null);
            }

            // 设置渲染视图标志
            _creationInfo.renderFilters.viewFlags = isOwner ?
                Oculus.Avatar2.CAPI.ovrAvatar2EntityViewFlags.FirstPerson :
                Oculus.Avatar2.CAPI.ovrAvatar2EntityViewFlags.ThirdPerson;

            // 创建Avatar实体
            CreateEntity();

            // 设置活动视图
            SetActiveView(_creationInfo.renderFilters.viewFlags);

            // 等待访问令牌设置完成
            await m_setUpAccessTokenTask;

            // 初始化网络组件
            if (m_networking != null)
            {
                m_networking.Init();
            }

            if (IsLocal)
            {
                // 本地Avatar的额外设置

                // 获取登录用户信息
                var user = await Users.GetLoggedInUser().Gen();
                _userId = user.Data.ID;

                if (m_networking)
                {
                    m_networking.UserId = _userId;
                }

                // 加载用户Avatar
                LoadUser();

                if (!m_networking)
                {
                    // 如果没有网络组件，跟踪相机位置
                    UpdatePositionToCamera();
                    StartCoroutine(TrackCamera());
                }
            }
            else if (_userId != 0)
            {
                // 远程Avatar且有用户ID时加载用户
                LoadUser();
            }
        }

        /// <summary>
        /// 加载指定用户ID的Avatar
        /// 如果用户ID不同则更新并重新加载
        /// </summary>
        /// <param name="userId">要加载的用户ID</param>
        public void LoadUser(ulong userId)
        {
            if (_userId != userId)
            {
                _userId = userId;
                LoadUser();
            }
        }

        /// <summary>
        /// 显示Avatar
        /// 根据是否为所有者设置适当的视图标志
        /// </summary>
        public void Show()
        {
            SetActiveView(!m_networkObject.IsOwner
                ? ovrAvatar2EntityViewFlags.ThirdPerson
                : ovrAvatar2EntityViewFlags.FirstPerson);
        }

        /// <summary>
        /// 隐藏Avatar
        /// 设置视图标志为无，使Avatar不可见
        /// </summary>
        public void Hide()
        {
            SetActiveView(ovrAvatar2EntityViewFlags.None);
        }

        protected override void OnSkeletonLoaded()
        {
            base.OnSkeletonLoaded();

            foreach (var evt in OnJointLoadedEvents)
            {
                var jointTransform = GetJointTransform(evt.Joint);
                if (evt.TargetToSetAsChild != null)
                {
                    evt.TargetToSetAsChild.SetParent(jointTransform, false);
                }

                evt.OnLoaded?.Invoke(jointTransform);
            }
        }

        private IEnumerator TrackCamera()
        {
            while (true)
            {
                UpdatePositionToCamera();
                yield return null;
            }
        }

        private void UpdatePositionToCamera()
        {
            var cameraTransform = CameraRigRef.Instance.transform;
            transform.SetPositionAndRotation(
                cameraTransform.position,
                cameraTransform.rotation);
        }
    }
}
