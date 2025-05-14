// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System.Collections;
using UltimateGloveBall.Arena.Balls;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEngine;

namespace UltimateGloveBall.Arena.Player
{
    /// <summary>
    /// 玩家护盾控制器
    /// 负责处理护盾被击中时的视觉状态变化,并根据能量等级更新护盾颜色
    /// </summary>
    public class Shield : MonoBehaviour
    {
        /// <summary>
        /// 护盾被击中纹理显示时间
        /// </summary>
        private const float HIT_TEXTURE_TIME = 1f;

        /// <summary>
        /// 护盾颜色着色器参数ID
        /// </summary>
        private static readonly int s_shieldColorParam = Shader.PropertyToID("_Color");
        /// <summary>
        /// 击中时间着色器参数ID
        /// </summary>
        private static readonly int s_hitTimeParam = Shader.PropertyToID("_HitTime");

        /// <summary>
        /// 手套骨骼网络组件引用
        /// </summary>
        [SerializeField] private GloveArmatureNetworking m_armatureNet;

        /// <summary>
        /// 满能量时的护盾颜色
        /// </summary>
        [SerializeField] private Color m_fullEnergyColor;
        /// <summary>
        /// 低能量时的护盾颜色
        /// </summary>
        [SerializeField] private Color m_lowEnergyColor;

        /// <summary>
        /// 护盾渲染器数组
        /// </summary>
        [SerializeField] private MeshRenderer[] m_shieldRenderers;

        /// <summary>
        /// 击中纹理计时器
        /// </summary>
        private float m_hitTextureTimer;
        /// <summary>
        /// 是否处于被击中状态
        /// </summary>
        private bool m_inHitState;

        /// <summary>
        /// 材质属性块,用于批量修改材质属性
        /// </summary>
        private MaterialPropertyBlock m_materialBlock;

        /// <summary>
        /// 组件禁用时的处理
        /// </summary>
        private void OnDisable()
        {
            if (m_inHitState)
            {
                StopCoroutine(SwapBackToUnhitWhenReady());
                m_inHitState = false;
                RemoveKeyWord("_HITENABLED");
            }
        }

        /// <summary>
        /// 碰撞开始时的处理
        /// 检测是否被球击中
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            var ball = collision.gameObject.GetComponent<BallNetworking>();
            if (ball != null && !ball.HasOwner)
            {
                OnBallHit();
            }
        }

        /// <summary>
        /// 触发器进入时的处理
        /// 检测是否被电球击中
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }

            var fireball = other.gameObject.GetComponent<ElectricBall>();
            if (fireball != null && !fireball.Ball.HasOwner && fireball.Ball.IsAlive)
            {
                var controller = m_armatureNet.OwnerClientId == NetworkManager.Singleton.LocalClientId
                    ? LocalPlayerEntities.Instance.LocalPlayerController
                    : LocalPlayerEntities.Instance.GetPlayerObjects(m_armatureNet.OwnerClientId).PlayerController;
                controller.OnShieldHit(m_armatureNet.Side);
            }
        }

        /// <summary>
        /// 更新护盾充能等级
        /// </summary>
        /// <param name="chargeLevel">充能等级(0-100)</param>
        public void UpdateChargeLevel(float chargeLevel)
        {
            m_materialBlock ??= new MaterialPropertyBlock();

            foreach (var shieldRenderer in m_shieldRenderers)
            {
                shieldRenderer.GetPropertyBlock(m_materialBlock);
                var color = Color.Lerp(m_lowEnergyColor, m_fullEnergyColor, chargeLevel / 100f);
                m_materialBlock.SetColor(s_shieldColorParam, color);
                shieldRenderer.SetPropertyBlock(m_materialBlock);
            }
        }

        /// <summary>
        /// 处理球击中护盾的效果
        /// </summary>
        private void OnBallHit()
        {
            m_hitTextureTimer = HIT_TEXTURE_TIME;
            if (!m_inHitState)
            {
                m_inHitState = true;
                SetKeyWord("_HITENABLED");
                _ = StartCoroutine(SwapBackToUnhitWhenReady());
            }
        }

        /// <summary>
        /// 协程:等待击中效果结束后恢复正常状态
        /// </summary>
        private IEnumerator SwapBackToUnhitWhenReady()
        {
            while (m_hitTextureTimer >= 0)
            {
                m_hitTextureTimer -= Time.deltaTime;
                SetValue(s_hitTimeParam, Mathf.Lerp(0, 1, 1f - m_hitTextureTimer / HIT_TEXTURE_TIME));
                yield return null;
            }

            m_inHitState = false;
            RemoveKeyWord("_HITENABLED");
        }

        /// <summary>
        /// 设置材质属性值
        /// </summary>
        /// <param name="valueId">属性ID</param>
        /// <param name="value">属性值</param>
        private void SetValue(int valueId, float value)
        {
            m_materialBlock ??= new MaterialPropertyBlock();

            foreach (var shieldRenderer in m_shieldRenderers)
            {
                shieldRenderer.GetPropertyBlock(m_materialBlock);
                m_materialBlock.SetFloat(valueId, value);
                shieldRenderer.SetPropertyBlock(m_materialBlock);
            }
        }

        /// <summary>
        /// 启用材质关键字
        /// </summary>
        /// <param name="keyword">关键字名称</param>
        private void SetKeyWord(string keyword)
        {
            foreach (var shieldRenderer in m_shieldRenderers)
            {
                shieldRenderer.material.EnableKeyword(keyword);
            }
        }

        /// <summary>
        /// 禁用材质关键字
        /// </summary>
        /// <param name="keyword">关键字名称</param>
        private void RemoveKeyWord(string keyword)
        {
            foreach (var shieldRenderer in m_shieldRenderers)
            {
                shieldRenderer.material.DisableKeyword(keyword);
            }
        }
    }
}