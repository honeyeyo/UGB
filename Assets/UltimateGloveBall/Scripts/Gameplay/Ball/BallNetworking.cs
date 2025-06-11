// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using Meta.Utilities;
using PongHub.Arena.Gameplay;
using PongHub.Arena.Player;
using PongHub.Arena.Services;
using Unity.Netcode;
using UnityEngine;

namespace PongHub.Ball
{
    /// <summary>
    /// 乒乓球网络管理脚本
    /// 处理乒乓球的网络同步、发球权限验证、球的生成和附着逻辑
    /// 功能包括:
    /// 1. 球附着到非持拍手
    /// 2. 基于发球权限控制球的生成
    /// 3. 专门针对乒乓球物理特性优化
    /// </summary>
    public class BallNetworking : NetworkBehaviour
    {
        #region Events
        public event Action BallWasServedLocally;
        public event Action<BallNetworking, bool> BallDied;
        // public event Action BallServedFromServer; // 暂未使用，已注释
        public event Action<float> OnBallServed;
        public event Action<ulong, ulong> OnOwnerChanged;
        #endregion

        #region Serialized Fields
        [SerializeField, AutoSet] private Rigidbody m_rigidbody;
        [SerializeField, AutoSet] private BallStateSync m_stateSync;
        [SerializeField, AutoSet] private BallAttachment m_ballAttachment;
        [SerializeField, AutoSet] private BallSpin m_ballSpin;
        [SerializeField] private BallPhysicsData m_ballPhysics;

        [Header("Audio")]
        [SerializeField] private AudioSource m_audioSource;
        [SerializeField] private AudioClip m_onServedAudioClip;
        [SerializeField] private AudioClip m_ballBounceClip;
        [SerializeField] private AudioClip m_ballHitClip;

        [Header("Visual")]
        [SerializeField] private MeshRenderer m_ballRenderer;
        [SerializeField] private Material m_defaultMaterial;
        [SerializeField] private Material m_deadMaterial;
        #endregion

        #region Network Variables
        private NetworkVariable<ulong> m_attachedPlayerId = new(ulong.MaxValue);
        private NetworkVariable<bool> m_isAttached = new(false);
        private NetworkVariable<bool> m_isAlive = new(true);
        #endregion

        #region Private Fields
        private ulong m_serverPlayerClientId = ulong.MaxValue;
        private bool m_ballIsDead;
        private Transform m_attachedHand;
        #endregion

        #region Properties
        public bool IsAttached => m_isAttached.Value;
        public bool IsAlive => m_isAlive.Value && !m_ballIsDead;
        public ulong AttachedPlayerId => m_attachedPlayerId.Value;
        public bool HasAttachedPlayer => m_attachedPlayerId.Value != ulong.MaxValue;
        public BallSpin BallSpin => m_ballSpin;
        public BallPhysicsData BallPhysics => m_ballPhysics;
        #endregion

        #region NetworkBehaviour Overrides
        public override void OnNetworkSpawn()
        {
            m_attachedPlayerId.OnValueChanged += OnAttachedPlayerChanged;
            m_isAttached.OnValueChanged += OnAttachedStateChanged;
            m_isAlive.OnValueChanged += OnAliveStateChanged;

            // 初始化物理属性
            InitializeBallPhysics();

            // 设置初始状态
            OnAttachedPlayerChanged(m_attachedPlayerId.Value, m_attachedPlayerId.Value);
            OnAttachedStateChanged(m_isAttached.Value, m_isAttached.Value);
        }

        public override void OnNetworkDespawn()
        {
            m_attachedPlayerId.OnValueChanged -= OnAttachedPlayerChanged;
            m_isAttached.OnValueChanged -= OnAttachedStateChanged;
            m_isAlive.OnValueChanged -= OnAliveStateChanged;
        }
        #endregion

        #region Initialization
        private void Awake()
        {
            // 初始化组件
            if (m_ballPhysics == null)
                m_ballPhysics = new BallPhysicsData();

            if (m_ballRenderer != null && m_defaultMaterial == null)
                m_defaultMaterial = m_ballRenderer.material;
        }

        private void InitializeBallPhysics()
        {
            if (m_rigidbody != null && m_ballPhysics != null)
            {
                m_rigidbody.mass = m_ballPhysics.mass;
                m_rigidbody.drag = m_ballPhysics.airDrag;
                m_rigidbody.angularDrag = m_ballPhysics.angularDrag;

                // 设置碰撞器大小
                var sphereCollider = GetComponent<SphereCollider>();
                if (sphereCollider != null)
                {
                    sphereCollider.radius = m_ballPhysics.diameter / 2f;
                }
            }
        }
        #endregion

        #region Ball Generation System
        /// <summary>
        /// 请求生成球到非持拍手
        /// 基于Input系统实现.md中的设计
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestGenerateBallServerRpc(ulong requesterId = 0, ServerRpcParams rpcParams = default)
        {
            if (requesterId == 0)
                requesterId = rpcParams.Receive.SenderClientId;

            // 检查发球权限
            if (ServePermissionManager.Instance != null &&
                ServePermissionManager.Instance.CanPlayerServe(requesterId))
            {
                // 生成球并附着到非持拍手
                AttachBallToNonPaddleHandServerRpc(requesterId);
                SpawnBallForPlayerClientRpc(requesterId);
            }
            else
            {
                // 发送权限拒绝消息
                SendServePermissionDeniedClientRpc(requesterId);
            }
        }

        [ClientRpc]
        private void SpawnBallForPlayerClientRpc(ulong playerId)
        {
            Debug.Log($"球已生成给玩家: {playerId}");
            OnBallServed?.Invoke(1.0f);

            if (m_audioSource != null && m_onServedAudioClip != null)
            {
                m_audioSource.PlayOneShot(m_onServedAudioClip);
            }
        }

        [ClientRpc]
        private void SendServePermissionDeniedClientRpc(ulong playerId)
        {
            if (NetworkManager.LocalClientId == playerId)
            {
                Debug.LogWarning("发球权限被拒绝 - 当前不是您的发球轮次");
            }
        }
        #endregion

        #region Ball Attachment System
        /// <summary>
        /// 附着球到指定玩家的非持拍手
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AttachBallToNonPaddleHandServerRpc(ulong playerId)
        {
            if (!IsServer) return;

            m_attachedPlayerId.Value = playerId;
            m_isAttached.Value = true;
            m_isAlive.Value = true;
            m_ballIsDead = false;

            // 通知客户端附着状态
            AttachBallToPlayerClientRpc(playerId);
        }

        [ClientRpc]
        private void AttachBallToPlayerClientRpc(ulong playerId)
        {
            // 找到目标玩家并获取其非持拍手
            var targetHand = GetNonPaddleHandTransform(playerId);
            if (targetHand != null)
            {
                m_ballAttachment.AttachToNonPaddleHand(targetHand);
                m_attachedHand = targetHand;

                Debug.Log($"球已附着到玩家 {playerId} 的非持拍手");
            }
            else
            {
                Debug.LogError($"无法找到玩家 {playerId} 的非持拍手变换");
            }
        }

        /// <summary>
        /// 释放球 - 当玩家按下Trigger键时调用
        /// </summary>
        public void ReleaseBall(Vector3 releaseVelocity, Vector3 spinAxis, float spinRate)
        {
            if (!IsAttached || !IsOwner) return;

            ReleaseBallServerRpc(releaseVelocity, spinAxis, spinRate);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ReleaseBallServerRpc(Vector3 releaseVelocity, Vector3 spinAxis, float spinRate, ServerRpcParams rpcParams = default)
        {
            m_isAttached.Value = false;
            m_attachedPlayerId.Value = ulong.MaxValue;

            // 记录发球者用于后续逻辑
            m_serverPlayerClientId = rpcParams.Receive.SenderClientId;

            ReleaseBallClientRpc(releaseVelocity, spinAxis, spinRate);
        }

        [ClientRpc]
        private void ReleaseBallClientRpc(Vector3 releaseVelocity, Vector3 spinAxis, float spinRate)
        {
            m_ballAttachment.ReleaseBall(releaseVelocity);

            // 添加旋转
            if (m_ballSpin != null)
            {
                m_ballSpin.AddSpin(spinAxis, spinRate);
            }

            m_attachedHand = null;

            Debug.Log($"球已释放，速度: {releaseVelocity.magnitude:F2} m/s");
            BallWasServedLocally?.Invoke();
        }
        #endregion

        #region Hand Recognition
        /// <summary>
        /// 获取指定玩家的非持拍手变换
        /// 根据持拍状态自动判断，使用LocalPlayerEntities获取手部变换
        /// </summary>
        private Transform GetNonPaddleHandTransform(ulong playerId)
        {
            // 如果是本地玩家
            if (playerId == NetworkManager.Singleton.LocalClientId)
            {
                // 获取持拍状态
                var inputManager = FindObjectOfType<PongInputManager>();
                if (inputManager == null || LocalPlayerEntities.Instance == null) return null;

                // 根据持拍手返回非持拍手
                if (inputManager.IsLeftHandHoldingPaddle)
                {
                    // 左手持拍，返回右手
                    return LocalPlayerEntities.Instance.RightGloveHand?.transform;
                }
                else
                {
                    // 右手持拍或无持拍，返回左手
                    return LocalPlayerEntities.Instance.LeftGloveHand?.transform;
                }
            }
            else
            {
                // 其他玩家：使用LocalPlayerEntities获取
                var playerObjects = LocalPlayerEntities.Instance?.GetPlayerObjects(playerId);
                if (playerObjects == null) return null;

                // 简化处理：默认返回左手（实际实现中需要获取该玩家的持拍状态）
                return playerObjects.LeftGloveHand?.transform;
            }
        }

        /// <summary>
        /// 查找指定玩家的Avatar组件
        /// 使用LocalPlayerEntities系统来获取玩家Avatar
        /// </summary>
        private PlayerAvatarEntity FindPlayerAvatar(ulong playerId)
        {
            // 如果是本地玩家
            if (playerId == NetworkManager.Singleton.LocalClientId)
            {
                return LocalPlayerEntities.Instance?.Avatar;
            }

            // 查找其他玩家的Avatar
            var playerObjects = LocalPlayerEntities.Instance?.GetPlayerObjects(playerId);
            return playerObjects?.Avatar;
        }
        #endregion

        #region Network Variable Callbacks
        private void OnAttachedPlayerChanged(ulong previousValue, ulong newValue)
        {
            Debug.Log($"球附着玩家变更: {previousValue} -> {newValue}");
            OnOwnerChanged?.Invoke(previousValue, newValue);
        }

        private void OnAttachedStateChanged(bool previousValue, bool newValue)
        {
            Debug.Log($"球附着状态变更: {previousValue} -> {newValue}");

            if (newValue)
            {
                // 球被附着
                EnablePhysics(false);
            }
            else
            {
                // 球被释放
                EnablePhysics(true);
            }
        }

        private void OnAliveStateChanged(bool previousValue, bool newValue)
        {
            UpdateVisuals(!newValue);

            if (!newValue)
            {
                // 球死亡
                m_ballIsDead = true;
                BallDied?.Invoke(this, true);
            }
        }
        #endregion

        #region Physics and Collision
        private void FixedUpdate()
        {
            // 附着状态下不处理物理
            if (IsAttached) return;

            // 低速时锁定物理体
            if (IsOwner && !m_rigidbody.isKinematic && m_rigidbody.velocity.magnitude < 0.1f)
            {
                m_rigidbody.velocity = Vector3.zero;
                m_rigidbody.angularVelocity = Vector3.zero;
            }

            // 检查球是否掉落
            if (transform.position.y < -5f)
            {
                KillBall(true);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (m_ballIsDead || IsAttached) return;

            // 播放碰撞音效
            PlayCollisionAudio(collision);

            // 处理碰撞逻辑
            HandleCollision(collision);
        }

        private void PlayCollisionAudio(Collision collision)
        {
            if (m_audioSource == null) return;

            var hitObject = collision.gameObject;

            // 根据碰撞对象选择音效
            if (hitObject.CompareTag("Player"))
            {
                m_audioSource.PlayOneShot(m_ballHitClip);
            }
            else
            {
                m_audioSource.PlayOneShot(m_ballBounceClip);
            }
        }

        private void HandleCollision(Collision collision)
        {
            // 处理与球拍的碰撞 - 添加旋转
            // 使用标签检测球拍，而不是特定的组件
            if (collision.gameObject.CompareTag("Paddle") && m_ballSpin != null)
            {
                // 根据碰撞计算旋转
                var contact = collision.contacts[0];
                var relativeVelocity = collision.relativeVelocity;

                // 假设球拍有一个速度（可以从Rigidbody获取）
                var paddleRigidbody = collision.gameObject.GetComponent<Rigidbody>();
                Vector3 paddleVelocity = paddleRigidbody != null ? paddleRigidbody.velocity : Vector3.zero;

                // 使用PongBallSpin的碰撞计算方法
                m_ballSpin.CalculateSpinFromCollision(collision, paddleVelocity);
            }

            // 处理其他碰撞逻辑...
        }

        public void EnablePhysics(bool enable)
        {
            if (m_rigidbody != null)
            {
                m_rigidbody.isKinematic = !enable;
                m_rigidbody.detectCollisions = enable;
            }

            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = !enable;
            }
        }
        #endregion

        #region Ball Lifecycle
        public void KillBall(bool announceDeath, bool dieInstantly = false)
        {
            if (m_ballIsDead) return;

            m_ballIsDead = true;

            if (IsServer)
            {
                m_isAlive.Value = false;
            }

            if (announceDeath)
            {
                BallDied?.Invoke(this, dieInstantly);
            }

            Debug.Log("乒乓球已死亡");
        }

        public void ResetBall()
        {
            m_ballIsDead = false;
            m_attachedHand = null;
            m_serverPlayerClientId = ulong.MaxValue;

            if (IsServer)
            {
                m_attachedPlayerId.Value = ulong.MaxValue;
                m_isAttached.Value = false;
                m_isAlive.Value = true;
            }

            // 重置物理状态
            if (m_rigidbody != null)
            {
                m_rigidbody.velocity = Vector3.zero;
                m_rigidbody.angularVelocity = Vector3.zero;
            }

            // 重置旋转
            if (m_ballSpin != null)
            {
                m_ballSpin.ResetSpin();
            }

            // 重置附着
            if (m_ballAttachment != null)
            {
                m_ballAttachment.DetachBall();
            }

            UpdateVisuals(false);
        }

        private void UpdateVisuals(bool isDead)
        {
            if (m_ballRenderer != null)
            {
                m_ballRenderer.material = isDead ? m_deadMaterial : m_defaultMaterial;
            }
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// 获取球的生成位置（非持拍手位置）
        /// </summary>
        public Vector3 GetBallSpawnPosition()
        {
            if (m_attachedHand != null)
            {
                return m_attachedHand.position + m_attachedHand.forward * 0.05f;
            }
            return transform.position;
        }

        /// <summary>
        /// 获取球的生成旋转
        /// </summary>
        public Quaternion GetBallSpawnRotation()
        {
            if (m_attachedHand != null)
            {
                return m_attachedHand.rotation;
            }
            return transform.rotation;
        }

        /// <summary>
        /// 检查是否可以生成球（集成发球权限检查）
        /// </summary>
        public bool CanGenerateBall(ulong playerId)
        {
            if (ServePermissionManager.Instance == null) return true;
            return ServePermissionManager.Instance.CanPlayerServe(playerId);
        }
        #endregion
    }

    /// <summary>
    /// 乒乓球物理参数配置
    /// 基于真实乒乓球的物理特性
    /// </summary>
    [System.Serializable]
    public class BallPhysicsData
    {
        [Header("基础物理")]
        public float mass = 0.0027f;           // 乒乓球质量(2.7g)
        public float diameter = 0.04f;         // 直径(40mm)
        public float bounceCoefficient = 0.9f; // 反弹系数

        [Header("空气阻力")]
        public float airDrag = 0.1f;           // 空气阻力
        public float angularDrag = 0.05f;      // 角阻力

        [Header("旋转效果")]
        public float spinDecayRate = 0.98f;    // 旋转衰减率
        public float magnusForceMultiplier = 1.5f; // 马格努斯力系数

        [Header("碰撞")]
        public PhysicMaterial ballPhysicMaterial; // 物理材质
    }
}