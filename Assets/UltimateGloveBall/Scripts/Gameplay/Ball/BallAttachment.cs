// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using Unity.Netcode;
using UnityEngine;

namespace PongHub.Ball
{
    /// <summary>
    /// 乒乓球附着系统 - 处理球与非持拍手的附着逻辑
    /// 与原版手套系统的主要区别：
    /// 1. 附着到玩家的非持拍手而不是手套
    /// 2. 根据持拍状态自动选择附着的手
    /// 3. 附着位置在手心前方5cm处
    /// </summary>
    public class BallAttachment : NetworkBehaviour
    {
        #region Serialized Fields
        [Header("附着设置")]
        [SerializeField] private Vector3 attachOffset = Vector3.forward * 0.001f; // 1mm offset
        [SerializeField] private float attachmentSmoothing = 10f; // 附着位置平滑度
        [SerializeField] private bool showAttachmentGizmos = true; // 调试显示
        #endregion

        #region Private Fields
        private Transform attachedHand;
        private bool isAttached = false;
        private Rigidbody ballRigidbody;
        private Collider ballCollider;
        private Vector3 lastValidPosition;
        private bool wasKinematic;
        private bool wasTrigger;
        #endregion

        #region Properties
        public bool IsAttached => isAttached;
        public Transform AttachedHand => attachedHand;
        public Vector3 AttachOffset => attachOffset;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 获取组件引用
            ballRigidbody = GetComponent<Rigidbody>();
            ballCollider = GetComponent<Collider>();

            // 记录初始状态
            if (ballRigidbody != null)
            {
                wasKinematic = ballRigidbody.isKinematic;
            }
            if (ballCollider != null)
            {
                wasTrigger = ballCollider.isTrigger;
            }
        }

        private void Update()
        {
            if (isAttached && attachedHand != null)
            {
                UpdateAttachedPosition();
            }
        }

        private void OnDisable()
        {
            // 确保在禁用时正确清理附着状态
            DetachBall();
        }
        #endregion

        #region Attachment Logic
        /// <summary>
        /// 附着到非持拍手
        /// </summary>
        /// <param name="handTransform">目标手部变换</param>
        public void AttachToNonPaddleHand(Transform handTransform)
        {
            if (handTransform == null)
            {
                Debug.LogError("PongBallAttachment: 尝试附着到空的手部变换");
                return;
            }

            // 设置附着状态
            attachedHand = handTransform;
            isAttached = true;

            // 记录当前位置作为备用
            lastValidPosition = transform.position;

            // 禁用物理，启用跟随
            SetPhysicsState(false);

            // 设置初始位置
            UpdateAttachedPosition();

            Debug.Log($"球已附着到手部: {handTransform.name}");
        }

        /// <summary>
        /// 分离球
        /// </summary>
        public void DetachBall()
        {
            if (!isAttached) return;

            isAttached = false;
            attachedHand = null;

            // 恢复物理状态
            SetPhysicsState(true);

            Debug.Log("球已从手部分离");
        }

        /// <summary>
        /// 释放球并应用速度
        /// </summary>
        /// <param name="releaseVelocity">释放时的速度</param>
        public void ReleaseBall(Vector3 releaseVelocity)
        {
            if (!isAttached) return;

            // 分离球
            DetachBall();

            // 应用释放速度
            if (ballRigidbody != null)
            {
                ballRigidbody.velocity = releaseVelocity;

                // 添加一些角速度使球看起来更自然
                Vector3 angularVelocity = Vector3.Cross(releaseVelocity.normalized, Vector3.up) *
                                         releaseVelocity.magnitude * 0.1f;
                ballRigidbody.angularVelocity = angularVelocity;
            }

            Debug.Log($"球已释放，速度: {releaseVelocity.magnitude:F2} m/s");
        }
        #endregion

        #region Position Update
        /// <summary>
        /// 更新附着位置
        /// </summary>
        private void UpdateAttachedPosition()
        {
            if (!isAttached || attachedHand == null) return;

            try
            {
                // 计算目标位置：手心前方5cm
                Vector3 targetPosition = attachedHand.position +
                                       attachedHand.TransformDirection(attachOffset);

                // 计算目标旋转：跟随手部旋转
                Quaternion targetRotation = attachedHand.rotation;

                // 平滑插值到目标位置和旋转
                if (attachmentSmoothing > 0)
                {
                    transform.position = Vector3.Lerp(transform.position, targetPosition,
                                                    attachmentSmoothing * Time.deltaTime);
                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation,
                                                       attachmentSmoothing * Time.deltaTime);
                }
                else
                {
                    // 直接设置位置
                    transform.position = targetPosition;
                    transform.rotation = targetRotation;
                }

                // 更新最后有效位置
                lastValidPosition = transform.position;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"PongBallAttachment: 更新附着位置时出错: {e.Message}");
                // 恢复到最后有效位置
                transform.position = lastValidPosition;
            }
        }
        #endregion

        #region Physics Control
        /// <summary>
        /// 设置物理状态
        /// </summary>
        /// <param name="enablePhysics">是否启用物理</param>
        private void SetPhysicsState(bool enablePhysics)
        {
            if (ballRigidbody != null)
            {
                ballRigidbody.isKinematic = !enablePhysics;
                ballRigidbody.detectCollisions = enablePhysics;

                if (!enablePhysics)
                {
                    // 停止所有运动
                    ballRigidbody.velocity = Vector3.zero;
                    ballRigidbody.angularVelocity = Vector3.zero;
                }
            }

            if (ballCollider != null)
            {
                ballCollider.isTrigger = !enablePhysics;
            }
        }

        /// <summary>
        /// 重置物理状态到初始值
        /// </summary>
        public void ResetPhysicsState()
        {
            if (ballRigidbody != null)
            {
                ballRigidbody.isKinematic = wasKinematic;
                ballRigidbody.velocity = Vector3.zero;
                ballRigidbody.angularVelocity = Vector3.zero;
            }

            if (ballCollider != null)
            {
                ballCollider.isTrigger = wasTrigger;
            }
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// 获取球相对于手部的位置
        /// </summary>
        /// <returns>相对位置</returns>
        public Vector3 GetRelativePosition()
        {
            if (!isAttached || attachedHand == null) return Vector3.zero;

            return attachedHand.InverseTransformPoint(transform.position);
        }

        /// <summary>
        /// 检查是否可以附着到指定的手部
        /// </summary>
        /// <param name="handTransform">目标手部变换</param>
        /// <returns>是否可以附着</returns>
        public bool CanAttachToHand(Transform handTransform)
        {
            if (handTransform == null) return false;
            if (isAttached) return false; // 已经附着了

            // 检查距离是否合理
            float distance = Vector3.Distance(transform.position, handTransform.position);
            return distance < 0.5f; // 50cm内才能附着
        }

        /// <summary>
        /// 强制设置球到指定位置（用于网络同步）
        /// </summary>
        /// <param name="position">目标位置</param>
        /// <param name="rotation">目标旋转</param>
        public void ForceSetPosition(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;
            lastValidPosition = position;
        }
        #endregion

        #region Debug and Gizmos
        private void OnDrawGizmos()
        {
            if (!showAttachmentGizmos) return;

            if (isAttached && attachedHand != null)
            {
                // 绘制附着连接线
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, attachedHand.position);

                // 绘制附着偏移
                Gizmos.color = Color.yellow;
                Vector3 offsetPosition = attachedHand.position +
                                       attachedHand.TransformDirection(attachOffset);
                Gizmos.DrawWireSphere(offsetPosition, 0.02f);

                // 绘制信息
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(transform.position, 0.025f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (attachedHand != null)
            {
                // 绘制附着范围
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(attachedHand.position, 0.5f);
            }
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// 设置附着偏移
        /// </summary>
        /// <param name="offset">新的偏移值</param>
        public void SetAttachOffset(Vector3 offset)
        {
            attachOffset = offset;

            // 如果当前已附着，立即更新位置
            if (isAttached)
            {
                UpdateAttachedPosition();
            }
        }

        /// <summary>
        /// 设置附着平滑度
        /// </summary>
        /// <param name="smoothing">平滑度（0为无平滑）</param>
        public void SetAttachmentSmoothing(float smoothing)
        {
            attachmentSmoothing = Mathf.Max(0, smoothing);
        }

        /// <summary>
        /// 获取当前附着状态信息
        /// </summary>
        /// <returns>附着状态信息字符串</returns>
        public string GetAttachmentInfo()
        {
            if (!isAttached) return "未附着";

            string handName = attachedHand != null ? attachedHand.name : "Unknown";
            Vector3 relativePos = GetRelativePosition();

            return $"附着到: {handName}\n" +
                   $"相对位置: {relativePos:F3}\n" +
                   $"偏移: {attachOffset:F3}";
        }
        #endregion
    }
}