// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.Multiplayer.Core;
using Oculus.Avatar2;
using Oculus.Avatar2.Experimental;
using UnityEngine;

namespace PongHub.Arena.Player
{
    /// <summary>
    /// 此脚本是MecanimLegsAnimationController.cs的补充,因为我们不想完全重写该脚本,以避免在更新Avatar SDK和示例时出现复杂的更改。
    /// 我们将执行顺序设置为之后,以便在装备移动时可以覆盖移动动画。
    /// </summary>
    [DefaultExecutionOrder(1)]
    public class PlayerAvatarAnimationBehavior : OvrAvatarBaseBehavior
    {
        // 从MecanimLegsAnimationController.cs中获取的常量
        private const float WALK_SPEED_THRESHOLD = 0.1f; // 行走速度阈值
        private const float RUN_SPEED_THRESHOLD = 10.0f; // 奔跑速度阈值
        private const float MOVE_SPEED_RANGE = RUN_SPEED_THRESHOLD - WALK_SPEED_THRESHOLD; // 移动速度范围

        // 移动方向角度阈值
        private const float FORWARD_LATERAL_ANGLE_THRESHOLD = 45.0f; // 前向侧移角度阈值
        private const float LATERAL_ANGLE_THRESHOLD = 67.5f; // 侧移角度阈值
        private const float BACKWARD_LATERAL_ANGLE_THRESHOLD = 112.5f; // 后向侧移角度阈值
        private const float BACKWARD_ANGLE_THRESHOLD = 135.0f; // 后向角度阈值

        private const float MOVE_THRESHOLD_SQR = 0.01f * 0.01f; // 移动判定阈值的平方

        // 动画参数哈希值
        private static readonly int s_isMoving = Animator.StringToHash("IsMoving"); // 是否移动
        private static readonly int s_moveForward = Animator.StringToHash("MoveForward"); // 前进
        private static readonly int s_moveBackward = Animator.StringToHash("MoveBackward"); // 后退
        private static readonly int s_moveLeft = Animator.StringToHash("MoveLeft"); // 左移
        private static readonly int s_moveRight = Animator.StringToHash("MoveRight"); // 右移
        private static readonly int s_moveSpeed = Animator.StringToHash("MoveSpeed"); // 移动速度

        [SerializeField] private OvrAvatarAnimationBehavior m_avatarAnimationBehavior; // Avatar动画行为组件
        protected override string MainBehavior { get; set; } // 主要行为
        protected override string FirstPersonOutputPose { get; set; } // 第一人称输出姿势
        protected override string ThirdPersonOutputPose { get; set; } // 第三人称输出姿势

        private MecanimLegsAnimationController m_legAnimController; // 腿部动画控制器
        private Animator m_animator; // 动画器组件
        private Transform m_cameraRigTransform; // 相机装备变换组件
        private Vector3 m_previousRigPosition; // 上一帧装备位置

        /// <summary>
        /// 移动数据结构,用于存储角色移动状态信息
        /// </summary>
        private struct MovementData
        {
            public bool IsMoving; // 是否移动
            public float Speed; // 移动速度
            public bool MoveForward; // 是否前进
            public bool MoveBackward; // 是否后退
            public bool MoveRight; // 是否右移
            public bool MoveLeft; // 是否左移
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void Awake()
        {
            m_cameraRigTransform = CameraRigRef.Instance.CameraRig.transform;
            if (m_avatarAnimationBehavior == null)
            {
                m_avatarAnimationBehavior = GetComponentInChildren<OvrAvatarAnimationBehavior>();
            }
        }

        /// <summary>
        /// 当用户Avatar加载完成时调用
        /// </summary>
        /// <param name="entity">Avatar实体</param>
        protected override void OnUserAvatarLoaded(OvrAvatarEntity entity)
        {
            m_legAnimController = GetComponentInChildren<MecanimLegsAnimationController>();
            m_legAnimController.enableCrouchTimeout = false; // 禁用蹲伏超时
            m_animator = m_legAnimController.GetComponent<Animator>();
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        private void Update()
        {
            if (!Entity.IsLocal)
            {
                enabled = false;
                return;
            }
            if (m_legAnimController && m_animator)
            {
                var moveData = CalculateMovement();
                if (moveData.IsMoving)
                {
                    m_animator.SetBool(s_isMoving, true);
                    m_animator.SetBool(s_moveForward, moveData.MoveForward);
                    m_animator.SetBool(s_moveBackward, moveData.MoveBackward);
                    m_animator.SetBool(s_moveLeft, moveData.MoveLeft);
                    m_animator.SetBool(s_moveRight, moveData.MoveRight);
                    m_animator.SetFloat(s_moveSpeed, moveData.Speed);
                }
            }
        }

        /// <summary>
        /// 计算移动数据
        /// </summary>
        /// <returns>移动数据结构</returns>
        private MovementData CalculateMovement()
        {
            // 计算当前位置与上一帧位置的差值
            var rigPosition = m_cameraRigTransform.position;
            var deltaRigPosition = rigPosition - m_previousRigPosition;
            deltaRigPosition = Vector3.ProjectOnPlane(deltaRigPosition, Vector3.up);

            var velocity = deltaRigPosition / Time.deltaTime;

            var lowerBodyRotation = m_avatarAnimationBehavior.RootRotation;
            var relativeVelocity = Quaternion.Inverse(lowerBodyRotation) * velocity;
            var movementAngle = Vector3.Angle(relativeVelocity, Vector3.forward);

            // 计算移动速度
            var moveSpeed = Mathf.Clamp(
                (velocity.magnitude - WALK_SPEED_THRESHOLD) / MOVE_SPEED_RANGE,
                0.0f,
                1.0f);
            m_previousRigPosition = rigPosition;
            // 检查移动是否超过阈值
            var isMoving = velocity.sqrMagnitude > MOVE_THRESHOLD_SQR;
            return BuildMovementData(isMoving, moveSpeed, relativeVelocity, movementAngle);
        }

        /// <summary>
        /// 构建移动数据
        /// </summary>
        /// <param name="isMoving">是否移动</param>
        /// <param name="moveSpeed">移动速度</param>
        /// <param name="relativeVelocity">相对速度</param>
        /// <param name="movementAngle">移动角度</param>
        /// <returns>移动数据结构</returns>
        private MovementData BuildMovementData(bool isMoving, float moveSpeed, Vector3 relativeVelocity, float movementAngle)
        {
            var moveData = new MovementData() { IsMoving = isMoving, Speed = moveSpeed };
            if (!isMoving)
            {
                return moveData;
            }

            var lateralMovement = false;

            // 根据移动角度判断移动方向
            if (movementAngle <= FORWARD_LATERAL_ANGLE_THRESHOLD)
            {
                // 前进
                moveData.MoveForward = true;
            }
            else if (movementAngle <= LATERAL_ANGLE_THRESHOLD)
            {
                // 前向侧移
                moveData.MoveForward = true;
                lateralMovement = true;
            }
            else if (movementAngle <= BACKWARD_LATERAL_ANGLE_THRESHOLD)
            {
                // 侧移
                lateralMovement = true;
            }
            else if (movementAngle <= BACKWARD_ANGLE_THRESHOLD)
            {
                // 后向侧移
                moveData.MoveBackward = true;
                lateralMovement = true;
            }
            else
            {
                // 后退
                moveData.MoveBackward = true;
            }

            // 如果有侧向移动,判断是向左还是向右
            if (!lateralMovement)
            {
                return moveData;
            }

            if (relativeVelocity.x < 0.0f)
            {
                moveData.MoveLeft = true;
            }
            else
            {
                moveData.MoveRight = true;
            }

            return moveData;
        }
    }
}