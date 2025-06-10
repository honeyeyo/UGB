// Copyright (c) Meta Platforms, Inc. and affiliates.

#if HAS_META_INTERACTION && HAS_META_AVATARS

using Meta.Utilities;
using Meta.Utilities.Input;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Assertions;

namespace Meta.Utilities.Input
{
    /// <summary>
    /// 从XR手部数据源获取手部数据的组件
    /// 该类继承自 DataSource<HandDataAsset>，用于处理从XR输入管理器获取的手部跟踪数据
    /// 执行顺序设置为-80，确保在其他组件之前执行
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class FromXRHandDataSource : DataSource<HandDataAsset>
    {
        /// <summary>
        /// 手部骨骼变换数组，用于定义手部的骨骼结构
        /// </summary>
        [SerializeField]
        private Transform[] m_bones;

        /// <summary>
        /// 根部位置偏移量，用于调整手部相对于控制器的位置
        /// </summary>
        [SerializeField]
        private Vector3 m_rootOffset;

        /// <summary>
        /// 根部角度偏移量，用于调整手部相对于控制器的旋转
        /// </summary>
        [SerializeField]
        private Vector3 m_rootAngleOffset;

        [Header("OVR Data Source")]
        /// <summary>
        /// OVR相机装置引用，用于获取头显和控制器的位置信息
        /// </summary>
        [SerializeField, Interface(typeof(IOVRCameraRigRef))]
        private MonoBehaviour m_cameraRigRef;
        private IOVRCameraRigRef CameraRigRef { get; set; }

        /// <summary>
        /// 是否处理延迟更新，决定是否在LateUpdate中处理输入数据
        /// </summary>
        [SerializeField]
        private bool m_processLateUpdates;

        [Header("Shared Configuration")]
        /// <summary>
        /// 手部类型（左手或右手）
        /// </summary>
        [SerializeField]
        private Handedness m_handedness;

        /// <summary>
        /// 跟踪坐标系到世界坐标系的转换器
        /// </summary>
        [SerializeField, Interface(typeof(ITrackingToWorldTransformer))]
        private MonoBehaviour m_trackingToWorldTransformer;
        private ITrackingToWorldTransformer TrackingToWorldTransformer { get; set; }

        /// <summary>
        /// 是否处理延迟更新的属性访问器
        /// </summary>
        public bool ProcessLateUpdates
        {
            get => m_processLateUpdates;
            set => m_processLateUpdates = value;
        }

        /// <summary>
        /// 手部数据资产实例，存储所有手部相关的数据
        /// </summary>
        private readonly HandDataAsset m_handDataAsset = new();

        /// <summary>
        /// XR输入管理器引用，从父对象自动设置
        /// </summary>
        [AutoSetFromParent]
        [SerializeField] private XRInputManager m_xrInputManager;

        /// <summary>
        /// OVR控制器锚点变换
        /// </summary>
        private Transform m_ovrControllerAnchor;

        /// <summary>
        /// 手部数据源配置
        /// </summary>
        private HandDataSourceConfig m_config;

        /// <summary>
        /// 姿态偏移量
        /// </summary>
        private Pose m_poseOffset;

        /// <summary>
        /// 手腕修正旋转，用于修正手腕的默认旋转
        /// </summary>
        public static Quaternion WristFixupRotation { get; } = new(0.0f, 1.0f, 0.0f, 0.0f);

        /// <summary>
        /// 数据资产属性，返回当前的手部数据资产
        /// </summary>
        protected override HandDataAsset DataAsset => m_handDataAsset;

#if HAS_NAUGHTY_ATTRIBUTES
        [NaughtyAttributes.ShowNativeProperty]
#endif
        /// <summary>
        /// 当前根部位置，用于调试显示
        /// </summary>
        private Vector3 CurrentRootPosition => DataAsset?.Root.position ?? Vector3.zero;

        /// <summary>
        /// 手部骨骼数据
        /// </summary>
        private HandSkeleton m_skeleton;

        /// <summary>
        /// 组件唤醒时的初始化
        /// 创建手部骨骼数据，设置转换器和相机装置引用，更新配置
        /// </summary>
        protected void Awake()
        {
            // 根据手部类型创建骨骼数据
            m_skeleton = HandSkeletonOVR.CreateSkeletonData(m_handedness);

            // 设置坐标转换器和相机装置引用
            TrackingToWorldTransformer = m_trackingToWorldTransformer as ITrackingToWorldTransformer;
            CameraRigRef = m_cameraRigRef as IOVRCameraRigRef;

            UpdateConfig();
        }

        /// <summary>
        /// 组件开始时的初始化
        /// 验证必要的引用，设置控制器锚点，计算姿态偏移，更新骨骼和配置
        /// </summary>
        protected override void Start()
        {
            this.BeginStart(ref _started, base.Start);

            // 验证必要的组件引用
            Assert.IsNotNull(CameraRigRef);
            Assert.IsNotNull(TrackingToWorldTransformer);

            // 根据手部类型设置相应的控制器锚点
            if (m_handedness == Handedness.Left)
            {
                Assert.IsNotNull(CameraRigRef.LeftHand);
                m_ovrControllerAnchor = CameraRigRef.LeftController;
            }
            else
            {
                Assert.IsNotNull(CameraRigRef.RightHand);
                m_ovrControllerAnchor = CameraRigRef.RightController;
            }

            // 计算姿态偏移量
            var offset = new Pose(m_rootOffset, Quaternion.Euler(m_rootAngleOffset));
            if (m_handedness == Handedness.Left)
            {
                // 左手需要镜像X轴位置和添加180度Y轴旋转
                offset.position.x = -offset.position.x;
                offset.rotation = Quaternion.Euler(180f, 0f, 0f) * offset.rotation;
            }
            m_poseOffset = offset;

            UpdateSkeleton();
            UpdateConfig();
            this.EndStart(ref _started);
        }

        /// <summary>
        /// 组件启用时的处理
        /// 如果已开始，则订阅输入数据更新事件
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
            {
                CameraRigRef.WhenInputDataDirtied += HandleInputDataDirtied;
            }
        }

        /// <summary>
        /// 组件禁用时的处理
        /// 取消订阅输入数据更新事件
        /// </summary>
        protected override void OnDisable()
        {
            if (_started)
            {
                CameraRigRef.WhenInputDataDirtied -= HandleInputDataDirtied;
            }

            base.OnDisable();
        }

        /// <summary>
        /// 处理输入数据更新事件
        /// 根据是否处理延迟更新来决定是否标记数据需要更新
        /// </summary>
        /// <param name="isLateUpdate">是否为延迟更新</param>
        private void HandleInputDataDirtied(bool isLateUpdate)
        {
            if (isLateUpdate && !m_processLateUpdates)
            {
                return;
            }
            MarkInputDataRequiresUpdate();
        }

        /// <summary>
        /// 更新骨骼数据
        /// 将Transform数组中的局部位置和旋转信息复制到骨骼关节中
        /// </summary>
        private void UpdateSkeleton()
        {
            if (_started)
            {
                for (var i = 0; i < m_skeleton.joints.Length; i++)
                {
                    m_skeleton.joints[i].pose.position = m_bones[i].localPosition;
                    m_skeleton.joints[i].pose.rotation = m_bones[i].localRotation;
                }
            }
        }

        /// <summary>
        /// 配置属性访问器
        /// 延迟创建配置实例，设置手部类型
        /// </summary>
        private HandDataSourceConfig Config
        {
            get
            {
                if (m_config != null)
                {
                    return m_config;
                }

                m_config = new HandDataSourceConfig()
                {
                    Handedness = m_handedness
                };

                return m_config;
            }
        }

        /// <summary>
        /// 更新配置信息
        /// 设置手部类型、坐标转换器和手部骨骼
        /// </summary>
        private void UpdateConfig()
        {
            Config.Handedness = m_handedness;
            Config.TrackingToWorldTransformer = TrackingToWorldTransformer;
            Config.HandSkeleton = m_skeleton;
        }

        /// <summary>
        /// 更新手部数据
        /// 这是核心方法，负责从XR输入管理器获取数据并更新手部数据资产
        /// </summary>
        protected override void UpdateData()
        {
            // 设置基本配置和连接状态
            m_handDataAsset.Config = Config;
            m_handDataAsset.IsDataValid = true;
            m_handDataAsset.IsConnected = true;

            // 如果未连接，重置所有状态字段为默认值
            if (!m_handDataAsset.IsConnected)
            {
                m_handDataAsset.IsTracked = default;
                m_handDataAsset.RootPoseOrigin = default;
                m_handDataAsset.PointerPoseOrigin = default;
                m_handDataAsset.IsHighConfidence = default;
                for (var fingerIdx = 0; fingerIdx < Constants.NUM_FINGERS; fingerIdx++)
                {
                    m_handDataAsset.IsFingerPinching[fingerIdx] = default;
                    m_handDataAsset.IsFingerHighConfidence[fingerIdx] = default;
                }
                return;
            }

            // 设置基本跟踪状态
            m_handDataAsset.IsTracked = true;
            m_handDataAsset.IsHighConfidence = true;
            m_handDataAsset.HandScale = 1f;

            // 设置主导手（右手为主导手）
            m_handDataAsset.IsDominantHand = m_handedness == Handedness.Right;

            // 获取当前手部的输入动作和锚点
            var actions = m_xrInputManager.GetActions(m_handedness == Handedness.Left);
            var anchor = m_xrInputManager.GetAnchor(m_handedness == Handedness.Left);

            // 读取触发器和握持强度
            var indexStrength = actions.AxisIndexTrigger.action.ReadValue<float>();
            var gripStrength = actions.AxisHandTrigger.action.ReadValue<float>();

            // 设置拇指的捏取状态和强度
            m_handDataAsset.IsFingerHighConfidence[(int)HandFinger.Thumb] = true;
            m_handDataAsset.IsFingerPinching[(int)HandFinger.Thumb] = indexStrength >= 0.95f || gripStrength >= 0.95f;
            m_handDataAsset.FingerPinchStrength[(int)HandFinger.Thumb] = Mathf.Max(indexStrength, gripStrength);

            // 设置食指的捏取状态和强度（主要由扳机控制）
            m_handDataAsset.IsFingerHighConfidence[(int)HandFinger.Index] = true;
            m_handDataAsset.IsFingerPinching[(int)HandFinger.Index] = indexStrength >= 0.95f;
            m_handDataAsset.FingerPinchStrength[(int)HandFinger.Index] = indexStrength;

            // 设置中指的捏取状态和强度（主要由握持控制）
            m_handDataAsset.IsFingerHighConfidence[(int)HandFinger.Middle] = true;
            m_handDataAsset.IsFingerPinching[(int)HandFinger.Middle] = gripStrength >= 0.95f;
            m_handDataAsset.FingerPinchStrength[(int)HandFinger.Middle] = gripStrength;

            // 设置无名指的捏取状态和强度（主要由握持控制）
            m_handDataAsset.IsFingerHighConfidence[(int)HandFinger.Ring] = true;
            m_handDataAsset.IsFingerPinching[(int)HandFinger.Ring] = gripStrength >= 0.95f;
            m_handDataAsset.FingerPinchStrength[(int)HandFinger.Ring] = gripStrength;

            // 设置小指的捏取状态和强度（主要由握持控制）
            m_handDataAsset.IsFingerHighConfidence[(int)HandFinger.Pinky] = true;
            m_handDataAsset.IsFingerPinching[(int)HandFinger.Pinky] = gripStrength >= 0.95f;
            m_handDataAsset.FingerPinchStrength[(int)HandFinger.Pinky] = gripStrength;

            // 设置指针姿态（用于射线交互）
            m_handDataAsset.PointerPoseOrigin = PoseOrigin.RawTrackedPose;
            m_handDataAsset.PointerPose = new Pose(anchor.localPosition, anchor.localRotation);

            // 更新手部关节旋转数据
            for (var i = 0; i < m_bones.Length; i++)
            {
                m_handDataAsset.Joints[i] = m_bones[i].localRotation;
            }

            // 应用手腕修正旋转
            m_handDataAsset.Joints[0] = WristFixupRotation;

            // 将控制器姿态从世界坐标系转换到跟踪坐标系
            var pose = new Pose(m_ovrControllerAnchor.position, m_ovrControllerAnchor.rotation);
            pose = Config.TrackingToWorldTransformer.ToTrackingPose(pose);

            // 应用姿态偏移并设置根部位置
            PoseUtils.Multiply(pose, m_poseOffset, ref m_handDataAsset.Root);
            m_handDataAsset.RootPoseOrigin = PoseOrigin.RawTrackedPose;
        }

        #region Inject

        /// <summary>
        /// 注入所有必要的依赖项
        /// 用于外部配置和初始化该组件
        /// </summary>
        public void InjectAllFromOVRControllerHandDataSource(UpdateModeFlags updateMode, IDataSource updateAfter,
            Handedness handedness, ITrackingToWorldTransformer trackingToWorldTransformer,
            IDataSource<HmdDataAsset> hmdData, Transform[] bones,
            Vector3 rootOffset, Vector3 rootAngleOffset)
        {
            InjectAllDataSource(updateMode, updateAfter);
            InjectHandedness(handedness);
            InjectTrackingToWorldTransformer(trackingToWorldTransformer);
            InjectBones(bones);
            InjectRootOffset(rootOffset);
            InjectRootAngleOffset(rootAngleOffset);
        }

        /// <summary>
        /// 注入手部类型
        /// </summary>
        public void InjectHandedness(Handedness handedness)
        {
            m_handedness = handedness;
        }

        /// <summary>
        /// 注入坐标转换器
        /// </summary>
        public void InjectTrackingToWorldTransformer(ITrackingToWorldTransformer trackingToWorldTransformer)
        {
            m_trackingToWorldTransformer = trackingToWorldTransformer as MonoBehaviour;
            TrackingToWorldTransformer = trackingToWorldTransformer;
        }

        /// <summary>
        /// 注入骨骼变换数组
        /// </summary>
        public void InjectBones(Transform[] bones)
        {
            m_bones = bones;
        }

        /// <summary>
        /// 注入根部位置偏移
        /// </summary>
        public void InjectRootOffset(Vector3 rootOffset)
        {
            m_rootOffset = rootOffset;
        }

        /// <summary>
        /// 注入根部角度偏移
        /// </summary>
        public void InjectRootAngleOffset(Vector3 rootAngleOffset)
        {
            m_rootAngleOffset = rootAngleOffset;
        }

        #endregion
    }
}

#endif
