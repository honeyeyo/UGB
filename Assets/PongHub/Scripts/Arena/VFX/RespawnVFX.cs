// Copyright (c) MagnusLab Inc. and affiliates.

using Meta.Multiplayer.Avatar;
using Oculus.Avatar2;
using UnityEngine;

namespace PongHub.Arena.VFX
{
    /// <summary>
    /// Component to be placed on the Respawn VFX game object. It animates the disolve amount on the material overtime.
    /// As soon as the gameobject is enable the animation starts and when the animation is done it will deactivate the
    /// gameobject.
    /// </summary>
    public class RespawnVFX : MonoBehaviour
    {
        private static readonly int s_dissolveAmountParam = Shader.PropertyToID("_DisAmount");
        [SerializeField]
        [Tooltip("Start Dissolve Amount / 开始溶解量 - Initial dissolve amount for the material")]
        private float m_startDissolveAmount;

        [SerializeField]
        [Tooltip("End Dissolve Amount / 结束溶解量 - Final dissolve amount for the material")]
        private float m_endDissolveAmount;

        [SerializeField]
        [Tooltip("Duration / 持续时间 - Animation duration in seconds")]
        private float m_duration;

        [SerializeField]
        [Tooltip("Mesh Renderer / 网格渲染器 - MeshRenderer component for the VFX")]
        private MeshRenderer m_meshRenderer;

        [SerializeField]
        [Tooltip("Avatar Entity / 头像实体 - Avatar entity for position reference")]
        private AvatarEntity m_avatar;
        private MaterialPropertyBlock m_materialBlock;
        private float m_timer;
        private bool m_active;
        private void Awake()
        {
            m_materialBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (m_avatar != null)
            {
                var chest = m_avatar.GetJointTransform(CAPI.ovrAvatar2JointType.Chest);
                var chestPos = chest.position;
                var newPos = chestPos;
                newPos.y = transform.position.y;
                transform.position = newPos;
            }
            m_meshRenderer.GetPropertyBlock(m_materialBlock);
            m_materialBlock.SetFloat(s_dissolveAmountParam, m_startDissolveAmount);
            m_meshRenderer.SetPropertyBlock(m_materialBlock);
            m_timer = 0;
            m_active = true;
        }

        public void Update()
        {
            if (!m_active)
            {
                gameObject.SetActive(false);
                return;
            }

            m_timer += Time.deltaTime;
            m_meshRenderer.GetPropertyBlock(m_materialBlock);
            m_materialBlock.SetFloat(s_dissolveAmountParam,
                Mathf.Lerp(m_startDissolveAmount, m_endDissolveAmount, m_timer / m_duration));
            m_meshRenderer.SetPropertyBlock(m_materialBlock);
            if (m_timer >= m_duration)
            {
                m_active = false;
            }
        }
    }
}