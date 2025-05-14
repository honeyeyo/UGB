// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using Meta.Multiplayer.Core;
using Meta.Utilities;
using UltimateGloveBall.App;
using UltimateGloveBall.Arena.Services;
using UltimateGloveBall.Utils;
using UnityEngine;

namespace UltimateGloveBall.Arena.Player
{
    /// <summary>
    /// 玩家移动控制器,负责处理玩家的所有移动相关功能。
    /// 支持多种移动方式:传送、快速移动/缩放、步行。
    /// 可以设置移动边界限制玩家活动范围。
    /// 移动计算基于玩家头部位置而不是相机中心点。
    /// 继承自Singleton单例模式基类,确保全局只有一个实例。
    /// </summary>
    public class PlayerMovement : Singleton<PlayerMovement>
    {
        /// <summary>
        /// OVR相机组件引用
        /// </summary>
        [SerializeField] private OVRCameraRig m_cameraRig;

        /// <summary>
        /// 快速移动速度
        /// </summary>
        [SerializeField] private float m_movementSpeed = 3;

        /// <summary>
        /// 步行速度
        /// </summary>
        [SerializeField] private float m_walkSpeed = 2;

        /// <summary>
        /// 玩家头部Transform引用
        /// </summary>
        [SerializeField] private Transform m_head;

        /// <summary>
        /// 编辑器模式下的默认头部高度
        /// </summary>
        [SerializeField] private float m_inEditorHeadHeight = 1.7f;

        /// <summary>
        /// 是否正在移动中
        /// </summary>
        private bool m_isMoving = false;

        /// <summary>
        /// 目标移动位置
        /// </summary>
        private Vector3 m_destination;

        /// <summary>
        /// 是否允许使用任意摇杆进行旋转
        /// </summary>
        public bool RotationEitherThumbstick = true;

        /// <summary>
        /// 是否启用旋转功能
        /// </summary>
        public bool IsRotationEnabled = true;

        /// <summary>
        /// 每次旋转的角度
        /// </summary>
        public float RotationAngle = 45.0f;

        /// <summary>
        /// 是否准备好进行快速转向
        /// </summary>
        private bool m_readyToSnapTurn;

        /// <summary>
        /// 是否启用移动边界限制
        /// </summary>
        private bool m_useLimits;

        /// <summary>
        /// 移动边界数组[minX, maxX, minZ, maxZ]
        /// </summary>
        private float[] m_limits;

        /// <summary>
        /// 初始化时设置编辑器模式下的头部高度
        /// </summary>
        private void Start()
        {
#if UNITY_EDITOR
            // 在编辑器模式下设置默认头部高度
            var localPos = m_head.localPosition;
            m_head.localPosition = localPos.SetY(m_inEditorHeadHeight);
#endif
        }

        /// <summary>
        /// 设置移动边界限制
        /// </summary>
        /// <param name="minX">X轴最小值</param>
        /// <param name="maxX">X轴最大值</param>
        /// <param name="minZ">Z轴最小值</param>
        /// <param name="maxZ">Z轴最大值</param>
        public void SetLimits(float minX, float maxX, float minZ, float maxZ)
        {
            m_useLimits = true;
            m_limits = new float[4] { minX, maxX, minZ, maxZ };
        }

        /// <summary>
        /// 重置移动边界限制
        /// </summary>
        public void ResetLimits()
        {
            m_useLimits = false;
        }

        /// <summary>
        /// 将玩家位置和旋转瞬移到指定Transform
        /// </summary>
        /// <param name="trans">目标Transform</param>
        public void SnapPositionToTransform(Transform trans)
        {
            SnapPosition(trans.position, trans.rotation);
        }

        /// <summary>
        /// 将玩家瞬移到指定位置和旋转
        /// </summary>
        /// <param name="destination">目标位置</param>
        /// <param name="rotation">目标旋转</param>
        public void SnapPosition(Vector3 destination, Quaternion rotation)
        {
            var thisTransform = transform;
            var curPosition = thisTransform.position;
            var headOffset = m_head.position - curPosition;
            headOffset.y = 0;
            destination -= headOffset;
            thisTransform.position = destination;
            thisTransform.rotation = rotation;
        }

        /// <summary>
        /// 将玩家传送到指定位置和旋转,并同步网络状态
        /// </summary>
        /// <param name="destination">目标位置</param>
        /// <param name="rotation">目标旋转</param>
        public void TeleportTo(Vector3 destination, Quaternion rotation)
        {
            var netTransformComp = LocalPlayerEntities.Instance.Avatar.GetComponent<ClientNetworkTransform>();
            var thisTransform = transform;
            var curPosition = thisTransform.position;
            var headOffset = m_head.position - curPosition;
            headOffset.y = 0;
            destination -= headOffset;
            thisTransform.position = destination;
            thisTransform.rotation = rotation;
            var netTransform = netTransformComp.transform;
            netTransform.position = destination;
            netTransform.rotation = rotation;
            netTransformComp.Teleport(destination, rotation, Vector3.one);
            m_isMoving = false;
            FadeOutScreen();
        }

        /// <summary>
        /// 平滑移动到指定位置
        /// </summary>
        /// <param name="destination">目标位置</param>
        public void MoveTo(Vector3 destination)
        {
            FadeScreen();
            var playerTransform = transform;
            var position = playerTransform.position;
            var headOffset = m_head.position - position;
            headOffset.y = 0;
            var newPos = destination - headOffset;
            StayWithinLimits(ref newPos, Vector3.zero);
            m_destination = newPos;
            m_isMoving = true;
        }

        /// <summary>
        /// 相对于玩家前方向量进行移动
        /// </summary>
        /// <param name="direction">移动方向</param>
        public void WalkInDirectionRelToForward(Vector3 direction)
        {
            var headDir = m_head.forward;
            headDir.y = 0; // 移除高度方向
            var dir = Quaternion.FromToRotation(Vector3.forward, headDir) * direction;
            var moveDist = Time.deltaTime * m_walkSpeed;
            var playerTransform = transform;
            var position = playerTransform.position;
            var headOffset = m_head.position - position;
            var newPos = position + dir * moveDist;
            StayWithinLimits(ref newPos, headOffset);

            transform.position = newPos;
        }

        /// <summary>
        /// 确保移动位置在边界限制内
        /// </summary>
        /// <param name="newPos">新位置</param>
        /// <param name="headOffset">头部偏移</param>
        private void StayWithinLimits(ref Vector3 newPos, Vector3 headOffset)
        {
            if (m_useLimits)
            {
                var headnewPos = newPos + headOffset;
                if (headnewPos.x < m_limits[0])
                {
                    newPos.x = m_limits[0] - headOffset.x;
                }

                if (headnewPos.x > m_limits[1])
                {
                    newPos.x = m_limits[1] - headOffset.x;
                }

                if (headnewPos.z < m_limits[2])
                {
                    newPos.z = m_limits[2] - headOffset.z;
                }

                if (headnewPos.z > m_limits[3])
                {
                    newPos.z = m_limits[3] - headOffset.z;
                }
            }
        }

        /// <summary>
        /// 淡入黑屏效果
        /// </summary>
        private void FadeScreen()
        {
            if (GameSettings.Instance.UseBlackoutOnSnap)
            {
                OVRScreenFade.instance.SetExplicitFade(1);
            }
        }

        /// <summary>
        /// 淡出黑屏效果
        /// </summary>
        private void FadeOutScreen()
        {
            if (GameSettings.Instance.UseBlackoutOnSnap)
            {
                OVRScreenFade.instance.SetExplicitFade(0);
            }
        }

        /// <summary>
        /// 每帧更新移动状态
        /// </summary>
        private void Update()
        {
            if (m_isMoving)
            {
                var moveDist = Time.deltaTime * m_movementSpeed;
                transform.position = Vector3.MoveTowards(transform.position, m_destination, moveDist);
                if (Vector3.SqrMagnitude(transform.position - m_destination) <= Mathf.Epsilon * Mathf.Epsilon)
                {
                    transform.position = m_destination;
                    m_isMoving = false;
                    FadeOutScreen();
                }
            }
        }

        /// <summary>
        /// 执行快速转向
        /// </summary>
        /// <param name="toRight">是否向右转向</param>
        public void DoSnapTurn(bool toRight)
        {
            if (IsRotationEnabled)
            {
                transform.RotateAround(m_cameraRig.centerEyeAnchor.position, Vector3.up, toRight ? RotationAngle : -RotationAngle);
            }
        }
    }
}