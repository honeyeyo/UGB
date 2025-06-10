// Copyright (c) Meta Platforms, Inc. and affiliates.

#if HAS_META_INTERACTION

using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Assertions;

namespace Meta.Utilities.Input
{
    /// <summary>
    /// 从XR头显数据源获取头显数据的组件
    /// 继承自DataSource<HmdDataAsset>，用于处理头显的位置和旋转跟踪数据
    /// 执行顺序设置为-80，确保在其他组件之前执行
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class FromXRHmdDataSource : DataSource<HmdDataAsset>
    {
        /// <summary>
        /// 根部位置偏移量，用于调整头显相对于基准点的位置
        /// </summary>
        [SerializeField]
        private Vector3 m_rootOffset;

        /// <summary>
        /// 根部角度偏移量，用于调整头显相对于基准点的旋转
        /// </summary>
        [SerializeField]
        private Vector3 m_rootAngleOffset;

        [Header("XR Data Source")]
        /// <summary>
        /// OVR相机装置引用，用于获取头显的跟踪数据
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
        /// 跟踪坐标系到世界坐标系的转换器
        /// </summary>
        [SerializeField, Interface(typeof(ITrackingToWorldTransformer))]
        private MonoBehaviour m_trackingToWorldTransformer;
        private ITrackingToWorldTransformer TrackingToWorldTransformer { get; set; }

#if HAS_NAUGHTY_ATTRIBUTES
        [NaughtyAttributes.ShowNativeProperty]
#endif
        /// <summary>
        /// 当前根部位置，用于调试显示
        /// </summary>
        private Vector3 CurrentRootPosition => DataAsset?.Root.position ?? Vector3.zero;

        /// <summary>
        /// 是否处理延迟更新的属性访问器
        /// </summary>
        public bool ProcessLateUpdates
        {
            get => m_processLateUpdates;
            set => m_processLateUpdates = value;
        }

        /// <summary>
        /// 头显数据资产实例，存储头显相关的所有数据
        /// </summary>
        private readonly HmdDataAsset m_hmdDataAsset = new();

        /// <summary>
        /// 头显数据源配置
        /// </summary>
        private HmdDataSourceConfig m_config;

        /// <summary>
        /// 姿态偏移量
        /// </summary>
        private Pose m_poseOffset;

        /// <summary>
        /// 数据资产属性，返回当前的头显数据资产
        /// </summary>
        protected override HmdDataAsset DataAsset => m_hmdDataAsset;

        /// <summary>
        /// 组件唤醒时的初始化
        /// 设置坐标转换器和相机装置引用，更新配置
        /// </summary>
        protected void Awake()
        {
            TrackingToWorldTransformer = m_trackingToWorldTransformer as ITrackingToWorldTransformer;
            CameraRigRef = m_cameraRigRef as IOVRCameraRigRef;

            UpdateConfig();
        }

        /// <summary>
        /// 组件开始时的初始化
        /// 验证必要的引用，计算姿态偏移，更新配置
        /// </summary>
        protected override void Start()
        {
            this.BeginStart(ref _started, base.Start);

            // 验证必要的组件引用
            Assert.IsNotNull(CameraRigRef);
            Assert.IsNotNull(TrackingToWorldTransformer);

            // 计算姿态偏移量
            var offset = new Pose(m_rootOffset, Quaternion.Euler(m_rootAngleOffset));
            m_poseOffset = offset;

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
        /// 配置属性访问器
        /// 延迟创建配置实例
        /// </summary>
        private HmdDataSourceConfig Config
        {
            get
            {
                if (m_config != null)
                {
                    return m_config;
                }

                m_config = new();

                return m_config;
            }
        }

        /// <summary>
        /// 更新配置信息
        /// 设置坐标转换器
        /// </summary>
        private void UpdateConfig()
        {
            Config.TrackingToWorldTransformer = TrackingToWorldTransformer;
        }

        /// <summary>
        /// 更新头显数据
        /// 从相机装置获取头显的位置和旋转数据并更新头显数据资产
        /// </summary>
        protected override void UpdateData()
        {
            // 设置配置
            m_hmdDataAsset.Config = Config;

            // 设置跟踪状态和帧ID
            m_hmdDataAsset.IsTracked = true;
            m_hmdDataAsset.FrameId = Time.frameCount;

            // 将头显姿态从世界坐标系转换到跟踪坐标系
            var centerEyeAnchor = CameraRigRef.CameraRig.centerEyeAnchor;
            var pose = new Pose(centerEyeAnchor.position, centerEyeAnchor.rotation);

            pose = Config.TrackingToWorldTransformer.ToTrackingPose(pose);

            // 应用姿态偏移并设置根部位置
            PoseUtils.Multiply(pose, m_poseOffset, ref m_hmdDataAsset.Root);
        }

        #region Inject

        /// <summary>
        /// 注入所有必要的依赖项
        /// 用于外部配置和初始化该组件
        /// </summary>
        public void InjectAllFromOVRControllerHandDataSource(UpdateModeFlags updateMode, IDataSource updateAfter,
            ITrackingToWorldTransformer trackingToWorldTransformer,
            IDataSource<HmdDataAsset> hmdData,
            Vector3 rootOffset, Vector3 rootAngleOffset)
        {
            InjectAllDataSource(updateMode, updateAfter);
            InjectTrackingToWorldTransformer(trackingToWorldTransformer);
            InjectRootOffset(rootOffset);
            InjectRootAngleOffset(rootAngleOffset);
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
