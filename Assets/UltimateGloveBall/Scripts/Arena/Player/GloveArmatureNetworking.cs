// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEngine;

namespace UltimateGloveBall.Arena.Player
{
    /// <summary>
    /// 手套骨架网络同步组件。
    /// 负责同步幽灵特效、护盾指示器、弹簧状态、护盾状态等。
    /// </summary>
    public class GloveArmatureNetworking : NetworkBehaviour
    {
        // Shader属性ID，用于控制幽灵特效
        private static readonly int s_ghostProperty = Shader.PropertyToID("ENABLE_GHOST_EFFECT");

        [SerializeField] private Transform m_root; // 手套骨架根节点
        [SerializeField] private Transform m_shieldAnchor; // 护盾锚点
        [SerializeField] private GloveSpringController m_springController; // 弹簧控制器
        [SerializeField] private Shield m_shield; // 护盾对象
        [SerializeField] private ShieldIndicator m_shieldIndicator; // 护盾指示器
        [SerializeField] private AudioSource m_shieldAudioSource; // 护盾音效播放器
        [SerializeField] private AudioClip m_shieldActivatedAudioClip; // 护盾激活音效
        [SerializeField] private AudioClip m_shieldDeactivatedAudioClip; // 护盾关闭音效
        [SerializeField] private AudioClip m_shieldUnAvailableAudioClip; // 护盾不可用音效

        [SerializeField] public Transform ElectricTetherForHandPoint; // 手部电缆锚点
        [SerializeField] public LODGroup[] LODGroups; // LOD组数组

        // 网络变量：手套左右
        private NetworkVariable<Glove.GloveSide> m_side = new();
        // 网络变量：弹簧激活状态，仅拥有者可写
        private NetworkVariable<bool> m_activated = new(writePerm: NetworkVariableWritePermission.Owner);
        // 网络变量：护盾激活状态
        private NetworkVariable<bool> m_shieldActivated = new();
        // 网络变量：护盾充能百分比，初始为100
        private NetworkVariable<float> m_shieldChargedLevel = new(100);
        // 网络变量：护盾禁用状态
        private NetworkVariable<bool> m_shieldDisabled = new();

        /// <summary>
        /// 手套左右属性
        /// </summary>
        public Glove.GloveSide Side
        {
            get => m_side.Value;
            set
            {
                m_side.Value = value;
                ApplyRotationAndScale(m_side.Value, true);
            }
        }

        /// <summary>
        /// 护盾充能百分比
        /// </summary>
        public float ShieldChargeLevel
        {
            get => m_shieldChargedLevel.Value;
            set => m_shieldChargedLevel.Value = value;
        }

        /// <summary>
        /// 弹簧压缩程度，[0-1]，1为完全压缩，0为未压缩
        /// </summary>
        public float SpringCompression => m_springController.Compression;

        /// <summary>
        /// 弹簧激活状态
        /// </summary>
        public bool Activated
        {
            get => m_activated.Value;
            set => m_activated.Value = value;
        }

        /// <summary>
        /// 激活护盾
        /// </summary>
        public void ActivateShield()
        {
            m_shieldActivated.Value = true;
        }

        /// <summary>
        /// 关闭护盾
        /// </summary>
        public void DeactivateShield()
        {
            m_shieldActivated.Value = false;
        }

        /// <summary>
        /// 启用护盾（解除禁用）
        /// </summary>
        public void EnableShield()
        {
            m_shieldDisabled.Value = false;
        }

        /// <summary>
        /// 禁用护盾
        /// </summary>
        public void DisableShield()
        {
            m_shieldDisabled.Value = true;
        }

        /// <summary>
        /// 护盾不可用时播放音效
        /// </summary>
        public void OnShieldNotAvailable()
        {
            m_shieldAudioSource.PlayOneShot(m_shieldUnAvailableAudioClip);
        }

        /// <summary>
        /// Unity生命周期：Awake，注册网络变量变更回调
        /// </summary>
        private void Awake()
        {
            m_side.OnValueChanged += OnSideChanged;
            m_activated.OnValueChanged += OnActivated;
            m_shieldActivated.OnValueChanged += OnShieldActivated;
            m_shieldChargedLevel.OnValueChanged += OnShieldChargedLevelChanged;
            m_shieldDisabled.OnValueChanged += OnShieldDisabledChanged;
        }

        /// <summary>
        /// 护盾充能百分比变更回调
        /// </summary>
        private void OnShieldChargedLevelChanged(float previousvalue, float newvalue)
        {
            UpdateShieldChargeLevel(newvalue);
        }

        /// <summary>
        /// 护盾禁用状态变更回调
        /// </summary>
        private void OnShieldDisabledChanged(bool previousvalue, bool newvalue)
        {
            if (newvalue)
            {
                m_shieldIndicator.SetDisabledState();
            }
            else
            {
                m_shieldIndicator.SetEnabledState();
            }
        }

        /// <summary>
        /// 护盾激活状态变更回调
        /// </summary>
        private void OnShieldActivated(bool previousvalue, bool newvalue)
        {
            if (previousvalue != newvalue)
            {
                // 切换护盾音效
                m_shieldAudioSource.Stop();
                m_shieldAudioSource.clip = newvalue ? m_shieldActivatedAudioClip : m_shieldDeactivatedAudioClip;
                m_shieldAudioSource.Play();
            }

            // 显示或隐藏护盾对象
            m_shield.gameObject.SetActive(newvalue);
            // 更新护盾充能显示
            m_shield.UpdateChargeLevel(m_shieldChargedLevel.Value);
        }

        /// <summary>
        /// 弹簧激活状态变更回调
        /// </summary>
        private void OnActivated(bool previousvalue, bool newvalue)
        {
            if (newvalue)
            {
                m_springController.Activate();
            }
            else
            {
                m_springController.Deactivate();
            }
        }

        /// <summary>
        /// 手套左右变更回调
        /// </summary>
        private void OnSideChanged(Glove.GloveSide previousvalue, Glove.GloveSide newvalue)
        {
            // 根据手腕方向调整骨架朝向
            ApplyRotationAndScale(newvalue, true);
        }

        /// <summary>
        /// 根据手套左右调整骨架旋转和缩放
        /// </summary>
        /// <param name="gloveSide">手套左右</param>
        /// <param name="withScale">是否应用缩放</param>
        private void ApplyRotationAndScale(Glove.GloveSide gloveSide, bool withScale)
        {
            Glove.SetRootRotation(m_root, gloveSide, withScale);
            // 为避免物理碰撞体因负缩放出错，护盾锚点缩放与骨架一致
            m_shieldAnchor.localScale = m_root.localScale;
        }

        /// <summary>
        /// 网络对象生成时回调
        /// </summary>
        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                // 本地玩家
                var playerEnts = LocalPlayerEntities.Instance;
                if (Side == Glove.GloveSide.Left)
                {
                    playerEnts.LeftGloveArmature = this;
                }
                else if (Side == Glove.GloveSide.Right)
                {
                    playerEnts.RightGloveArmature = this;
                }

                playerEnts.TryAttachGloves();
                SetLocalLoDs();
            }
            else
            {
                // 远程玩家
                var playerObjects = LocalPlayerEntities.Instance.GetPlayerObjects(OwnerClientId);
                if (Side == Glove.GloveSide.Left)
                {
                    playerObjects.LeftGloveArmature = this;
                }
                else if (Side == Glove.GloveSide.Right)
                {
                    playerObjects.RightGloveArmature = this;
                }

                playerObjects.TryAttachObjects();
            }

            // 初始化朝向
            OnSideChanged(m_side.Value, m_side.Value);
        }

        /// <summary>
        /// 网络对象销毁时回调
        /// </summary>
        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                var playerEnts = LocalPlayerEntities.Instance;
                if (Side == Glove.GloveSide.Left)
                {
                    playerEnts.LeftGloveArmature = null;
                }
                else if (Side == Glove.GloveSide.Right)
                {
                    playerEnts.RightGloveArmature = null;
                }
            }
        }

        /// <summary>
        /// 更新护盾充能显示
        /// </summary>
        /// <param name="level">充能百分比</param>
        private void UpdateShieldChargeLevel(float level)
        {
            m_shieldIndicator.UpdateChargeLevel(level);
            if (m_shield.gameObject.activeSelf)
            {
                m_shield.UpdateChargeLevel(level);
            }
        }

        /// <summary>
        /// 设置本地LOD为最高
        /// </summary>
        private void SetLocalLoDs()
        {
            foreach (var lodGroup in LODGroups)
            {
                if (lodGroup)
                {
                    lodGroup.ForceLOD(0);
                }
            }
        }

        /// <summary>
        /// 设置幽灵特效开关
        /// </summary>
        /// <param name="enable">是否启用</param>
        public void SetGhostEffect(bool enable)
        {
            foreach (var group in LODGroups)
            {
                var rends = group.transform.GetComponentsInChildren<Renderer>();

                foreach (var rend in rends)
                {
                    var material = rend.material;
                    material.SetFloat(s_ghostProperty, enable ? 1 : 0);
                }
            }
        }
    }
}
