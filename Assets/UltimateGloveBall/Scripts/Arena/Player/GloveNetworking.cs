// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using Meta.Multiplayer.Core;
using UltimateGloveBall.App;
using UltimateGloveBall.Arena.Balls;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace UltimateGloveBall.Arena.Player
{
    /// <summary>
    /// 手套网络状态同步组件。
    /// 负责同步幽灵特效、拉索音效、动画状态、左右手、飞行状态和是否抓球等。
    /// </summary>
    [RequireComponent(typeof(Glove))]
    public class GloveNetworking : NetworkBehaviour
    {
        // Shader属性ID：控制幽灵特效
        private static readonly int s_ghostProperty = Shader.PropertyToID("ENABLE_GHOST_EFFECT");
        // Animator参数ID：控制抓球动画
        private static readonly int s_grabbed = Animator.StringToHash("Grabbed");

        [SerializeField] private Transform m_root; // 手套骨架根节点
        [SerializeField] private Animator m_animator; // 手套动画器
        [SerializeField] private AudioClip m_zipSound; // 拉索音效

        public Transform BallAnchor; // 球的锚点

        // 网络变量：是否抓住了球，仅拥有者可写
        private NetworkVariable<bool> m_ballGrabbed = new(writePerm: NetworkVariableWritePermission.Owner);
        // 网络变量：是否处于飞行状态，仅拥有者可写
        private NetworkVariable<bool> m_flying = new(writePerm: NetworkVariableWritePermission.Owner);

        private Glove m_glove; // 手套本地逻辑组件
        // 网络变量：手套左右，仅拥有者可写
        private NetworkVariable<Glove.GloveSide> m_side = new(writePerm: NetworkVariableWritePermission.Owner);
        private AudioSource m_zipAudioSource; // 拉索音效播放器

        public Action OnTryGrabBall; // 试图抓球时的回调

        /// <summary>
        /// 是否抓住了球
        /// </summary>
        public bool Grabbed
        {
            get => m_ballGrabbed.Value;
            set
            {
                // 只有拥有者可以设置
                if (IsOwner)
                {
                    m_ballGrabbed.Value = value;
                }
            }
        }

        /// <summary>
        /// 是否处于飞行状态
        /// </summary>
        public bool Flying
        {
            get => m_flying.Value;
            set => m_flying.Value = value;
        }

        /// <summary>
        /// 手套左右属性
        /// </summary>
        public Glove.GloveSide Side
        {
            get => m_side.Value;
            set
            {
                m_side.Value = value;
                // 设置手套根节点的旋转
                Glove.SetRootRotation(m_root, m_side.Value, true);
            }
        }

        /// <summary>
        /// 初始化，获取组件并注册回调
        /// </summary>
        private void Awake()
        {
            m_glove = GetComponent<Glove>();
            Assert.IsNotNull(m_glove, $"Did not find component for {nameof(m_glove)}.");

            // 注册左右手和飞行状态变化的回调
            m_side.OnValueChanged += OnSideChanged;
            m_flying.OnValueChanged += OnFlyingStateChanged;

            // 创建拉索音效播放器
            m_zipAudioSource = new GameObject("ZipSound").AddComponent<AudioSource>();
            m_zipAudioSource.outputAudioMixerGroup = AudioController.Instance.SfxGroup;
            m_zipAudioSource.clip = m_zipSound;
            m_zipAudioSource.spatialize = true;

            // 默认忽略网络位置同步，由飞行状态控制
            GetComponent<ClientNetworkTransform>().IgnoreUpdates = true;
        }

        /// <summary>
        /// 进入触发器时尝试抓球
        /// </summary>
        /// <param name="other">碰撞到的物体</param>
        private void OnTriggerEnter(Collider other)
        {
            // 只有本地拥有者才处理
            if (!IsOwner) return;

            // 已经尝试抓球则不再处理
            if (m_glove.TriedGrabbingBall)
                return; // 已检测到抓球则不再尝试

            // 获取球的网络组件
            var ballNet = other.gameObject.GetComponent<BallNetworking>();

            if (!ballNet) return;
            // 尝试抓球
            ballNet.TryGrabBall(m_glove);
            // 触发抓球事件
            OnTryGrabBall?.Invoke();
        }

        /// <summary>
        /// 飞行状态变化时回调
        /// </summary>
        /// <param name="previousvalue">之前的状态</param>
        /// <param name="newvalue">新的状态</param>
        private void OnFlyingStateChanged(bool previousvalue, bool newvalue)
        {
            // 飞行时启用网络同步，非飞行时忽略
            GetComponent<ClientNetworkTransform>().IgnoreUpdates = !newvalue;
        }

        /// <summary>
        /// 左右手变化时回调
        /// </summary>
        /// <param name="previousvalue">之前的手</param>
        /// <param name="newvalue">新的手</param>
        private void OnSideChanged(Glove.GloveSide previousvalue, Glove.GloveSide newvalue)
        {
            // 设置手套根节点的旋转
            Glove.SetRootRotation(m_root, newvalue, true);
        }

        /// <summary>
        /// 网络对象生成时回调
        /// </summary>
        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                // 本地玩家，注册左右手
                var playerEnts = LocalPlayerEntities.Instance;
                if (Side == Glove.GloveSide.Left)
                {
                    playerEnts.LeftGloveHand = GetComponent<Glove>();
                }
                else if (Side == Glove.GloveSide.Right)
                {
                    playerEnts.RightGloveHand = GetComponent<Glove>();
                }

                // 尝试附加手套
                playerEnts.TryAttachGloves();
                // 设置LOD为本地
                m_glove.SetLODLocal();
            }
            else
            {
                // 远程玩家，注册左右手
                var playerObjects = LocalPlayerEntities.Instance.GetPlayerObjects(OwnerClientId);
                if (Side == Glove.GloveSide.Left)
                {
                    playerObjects.LeftGloveHand = GetComponent<Glove>();
                }
                else if (Side == Glove.GloveSide.Right)
                {
                    playerObjects.RightGloveHand = GetComponent<Glove>();
                }

                // 尝试附加远程对象
                playerObjects.TryAttachObjects();
            }

            // 初始化左右手和抓球状态
            OnSideChanged(m_side.Value, m_side.Value);
            m_ballGrabbed.OnValueChanged += OnGrabbedStateChanged;
            OnGrabbedStateChanged(m_ballGrabbed.Value, m_ballGrabbed.Value);
        }

        /// <summary>
        /// 网络对象销毁时回调
        /// </summary>
        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                // 本地玩家，注销左右手
                var playerEnts = LocalPlayerEntities.Instance;
                if (Side == Glove.GloveSide.Left)
                {
                    playerEnts.LeftGloveHand = null;
                }
                else if (Side == Glove.GloveSide.Right)
                {
                    playerEnts.RightGloveHand = null;
                }
            }
        }

        /// <summary>
        /// 触发拉索音效（本地和网络同步）
        /// </summary>
        /// <param name="position">音效播放位置</param>
        public void OnZip(Vector3 position)
        {
            // 本地播放
            PlayZipSound(position);
            // 通知服务器同步
            OnZipServerRPC(position);
        }

        /// <summary>
        /// 服务器RPC：同步拉索音效到所有客户端
        /// </summary>
        /// <param name="position">音效播放位置</param>
        [ServerRpc]
        private void OnZipServerRPC(Vector3 position)
        {
            // 通知所有客户端
            OnZipClientRPC(position);
        }

        /// <summary>
        /// 客户端RPC：在非拥有者客户端播放拉索音效
        /// </summary>
        /// <param name="position">音效播放位置</param>
        [ClientRpc]
        private void OnZipClientRPC(Vector3 position)
        {
            // 拥有者本地已播放，无需重复
            if (NetworkManager.Singleton.LocalClientId != OwnerClientId)
            {
                PlayZipSound(position);
            }
        }

        /// <summary>
        /// 播放拉索音效
        /// </summary>
        /// <param name="position">音效播放位置</param>
        private void PlayZipSound(Vector3 position)
        {
            m_zipAudioSource.transform.position = position;
            m_zipAudioSource.PlayOneShot(m_zipSound);
        }

        /// <summary>
        /// 抓球状态变化时回调，更新动画
        /// </summary>
        /// <param name="previousvalue">之前的状态</param>
        /// <param name="newvalue">新的状态</param>
        private void OnGrabbedStateChanged(bool previousvalue, bool newvalue)
        {
            m_animator.SetBool(s_grabbed, newvalue);
        }

        /// <summary>
        /// 设置手套的幽灵特效
        /// </summary>
        /// <param name="enable">是否启用</param>
        public void SetGhostEffect(bool enable)
        {
            // 获取所有子Renderer
            var rends = m_glove.transform.GetComponentsInChildren<Renderer>();

            foreach (var rend in rends)
            {
                var material = rend.material;
                // 设置Shader属性
                material.SetFloat(s_ghostProperty, enable ? 1 : 0);
            }
        }
    }
}
