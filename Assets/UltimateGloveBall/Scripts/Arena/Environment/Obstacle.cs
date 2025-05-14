// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using Meta.Utilities;
using UltimateGloveBall.Arena.Balls;
using UltimateGloveBall.Arena.Gameplay;
using UltimateGloveBall.Arena.Player;
using UltimateGloveBall.Arena.Services;
using UltimateGloveBall.Arena.VFX;
using Unity.Netcode;
using UnityEngine;

namespace UltimateGloveBall.Arena.Environment
{
    /// <summary>
    /// 游戏中的网络同步障碍物。主要功能包括:
    /// 1. 同步障碍物的充气/放气状态
    /// 2. 处理碰撞音效和颜色变化
    /// 3. 管理障碍物的形变和碰撞体积
    /// </summary>
    public class Obstacle : NetworkBehaviour
    {
        // 充气和放气的速率常量
        private const float INFLATION_RATE = 70f;        // 充气速率
        private const float DEFLATION_RATE = 60f;       // 放气速率
        private const float TIME_BEFORE_REFLATION = 6f; // 重新充气前的等待时间

        [SerializeField, AutoSet] private TeamColoringNetComponent m_teamColoring;  // 队伍颜色组件
        [SerializeField] private Collider m_collisionCollider;                      // 碰撞体
        [SerializeField] private SkinnedMeshRenderer m_mesh;                        // 网格渲染器

        [Header("Collider Data")]
        [SerializeField] private Vector3 m_colliderCenterInflated;   // 充气状态下碰撞体中心点
        [SerializeField] private Vector3 m_colliderCenterDeflated;   // 放气状态下碰撞体中心点
        [SerializeField] private float m_colliderHeightInflated;     // 充气状态下碰撞体高度
        [SerializeField] private float m_colliderHeightDeflated;     // 放气状态下碰撞体高度

        [Header("Sounds")]
        [SerializeField] private AudioSource m_audioSource;          // 音频源
        [SerializeField] private AudioClip m_inflateSound;          // 充气音效
        [SerializeField] private AudioClip m_deflateSound;          // 放气音效
        [SerializeField] private AudioClip m_punctureSound;         // 刺破音效

        private NetworkVariable<bool> m_inflated = new(true);       // 网络同步的充气状态

        private CapsuleCollider m_capsuleCollider = null;           // 胶囊碰撞体引用
        private SphereCollider m_sphereCollider = null;             // 球形碰撞体引用

        private float m_deflatedPct = 100;                          // 放气百分比
        private float m_reflationTimer = 0;                         // 重新充气计时器

        /// <summary>
        /// 初始化时获取碰撞体类型
        /// </summary>
        private void Awake()
        {
            if (m_collisionCollider is CapsuleCollider)
            {
                m_capsuleCollider = m_collisionCollider as CapsuleCollider;
            }
            else if (m_collisionCollider is SphereCollider)
            {
                m_sphereCollider = m_collisionCollider as SphereCollider;
            }
        }

        /// <summary>
        /// 网络对象生成时注册状态变化回调
        /// </summary>
        public override void OnNetworkSpawn()
        {
            m_inflated.OnValueChanged += OnInflatedStateChanged;
        }

        /// <summary>
        /// 处理充气状态变化,播放相应音效
        /// </summary>
        private void OnInflatedStateChanged(bool previousvalue, bool newvalue)
        {
            if (previousvalue != newvalue)
            {
                m_audioSource.Stop();
                bool playSound;
                if (newvalue)
                {
                    m_audioSource.clip = m_inflateSound;
                    playSound = m_deflatedPct > 0;
                }
                else
                {
                    m_audioSource.clip = m_deflateSound;
                    playSound = m_deflatedPct < 100;
                }

                if (playSound)
                {
                    m_audioSource.Play();
                }
            }
        }

        /// <summary>
        /// 更新障碍物的队伍颜色
        /// </summary>
        public void UpdateColor(TeamColor color)
        {
            m_teamColoring.TeamColor = color;
        }

        /// <summary>
        /// 处理与玩家的碰撞,导致放气
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            if (IsServer)
            {
                if (collision.gameObject.GetComponent<PlayerAvatarEntity>() != null)
                {
                    m_inflated.Value = false;
                }
            }
        }

        /// <summary>
        /// 处理与手套和电球的触发碰撞
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            var glove = other.gameObject.GetComponentInParent<Glove>();
            if (glove)
            {
                glove.OnHitObstacle();
                return;
            }

            if (m_inflated.Value)
            {
                var fireBall = other.gameObject.GetComponent<ElectricBall>();
                if (fireBall != null && fireBall.Ball.IsAlive)
                {
                    if (IsServer)
                    {
                        m_inflated.Value = false;
                        TriggerPunctureClientRPC();
                    }

                    var ballPosition = other.transform.position;
                    var contact = m_collisionCollider.ClosestPointOnBounds(ballPosition);
                    VFXManager.Instance.PlayHitVFX(contact, ballPosition - contact);
                }
            }
        }

        /// <summary>
        /// 每帧更新充气/放气状态和形变
        /// </summary>
        private void Update()
        {
            if (m_inflated.Value && m_deflatedPct > 0)
            {
                m_deflatedPct -= INFLATION_RATE * Time.deltaTime;
                if (m_deflatedPct <= 0)
                {
                    m_deflatedPct = 0;
                    m_audioSource.Stop();
                }

                UpdateDeflation();
            }
            else if (!m_inflated.Value && m_deflatedPct < 100)
            {
                m_deflatedPct += DEFLATION_RATE * Time.deltaTime;
                if (m_deflatedPct >= 100)
                {
                    m_deflatedPct = 100;
                    m_reflationTimer = 0;
                    m_audioSource.Stop();
                }

                UpdateDeflation();
            }

            if (IsServer && !m_inflated.Value && m_deflatedPct >= 100)
            {
                m_reflationTimer += Time.deltaTime;
                if (m_reflationTimer >= TIME_BEFORE_REFLATION)
                {
                    m_reflationTimer = 0;
                    m_inflated.Value = true;
                }
            }
        }

        /// <summary>
        /// 更新障碍物的形变和碰撞体积
        /// </summary>
        private void UpdateDeflation()
        {
            m_mesh.SetBlendShapeWeight(0, m_deflatedPct);
            var deflated01 = m_deflatedPct / 100f;
            if (m_capsuleCollider != null)
            {
                m_capsuleCollider.center = Vector3.Lerp(m_colliderCenterInflated, m_colliderCenterDeflated, deflated01);
                m_capsuleCollider.height = Mathf.Lerp(m_colliderHeightInflated, m_colliderHeightDeflated, deflated01);
            }
            else if (m_sphereCollider != null)
            {
                m_sphereCollider.center = Vector3.Lerp(m_colliderCenterInflated, m_colliderCenterDeflated, deflated01);
            }
        }

        /// <summary>
        /// 在所有客户端上触发刺破音效的RPC
        /// </summary>
        [ClientRpc]
        private void TriggerPunctureClientRPC()
        {
            m_audioSource.PlayOneShot(m_punctureSound);
        }
    }
}