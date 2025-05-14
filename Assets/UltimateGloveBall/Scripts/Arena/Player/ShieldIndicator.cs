// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System.Collections.Generic;
using UnityEngine;

namespace UltimateGloveBall.Arena.Player
{
    /// <summary>
    /// 护盾指示器控制器
    /// 负责控制手套骨骼上护盾指示器的状态,更新视觉效果以显示护盾剩余能量和充能状态
    /// </summary>
    public class ShieldIndicator : MonoBehaviour
    {
        /// <summary>
        /// 发光颜色着色器参数ID
        /// </summary>
        private static readonly int s_emissionParam = Shader.PropertyToID("_EmissionColor");

        /// <summary>
        /// 基础颜色着色器参数ID
        /// </summary>
        private static readonly int s_baseColorParam = Shader.PropertyToID("_BaseColor");

        /// <summary>
        /// 护盾能量指示器分段列表(4个部分)
        /// </summary>
        [SerializeField] private List<Renderer> m_sections;

        /// <summary>
        /// 指示器主网格渲染器
        /// </summary>
        [SerializeField] private Renderer m_indicatorMesh;

        /// <summary>
        /// 禁用状态下的主颜色
        /// </summary>
        [ColorUsage(true, true)]
        [SerializeField] private Color m_disabledMainColor;

        /// <summary>
        /// 禁用状态下的发光颜色
        /// </summary>
        [ColorUsage(true, true)]
        [SerializeField] private Color m_disabledEmissionColor;

        /// <summary>
        /// 各分段的基础颜色列表
        /// </summary>
        private readonly List<Color> m_baseSectionColors = new();

        /// <summary>
        /// 各分段的发光颜色列表
        /// </summary>
        [ColorUsage(true, true)] private readonly List<Color> m_emissionSectionColors = new();

        /// <summary>
        /// 指示器主网格的基础颜色
        /// </summary>
        private Color m_indicatorMeshBaseColor;

        /// <summary>
        /// 指示器主网格的发光颜色
        /// </summary>
        [ColorUsage(true, true)] private Color m_indicatorMeshEmissionColor;

        /// <summary>
        /// 材质属性块,用于批量修改材质属性
        /// </summary>
        private MaterialPropertyBlock m_materialPropertyBlock;

        /// <summary>
        /// 每个分段代表的能量百分比
        /// </summary>
        private float m_pctPerSection = 25;

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void Awake()
        {
            // 计算每个分段的能量百分比
            if (m_sections.Count > 0)
            {
                m_pctPerSection = 100f / m_sections.Count;
            }

            m_materialPropertyBlock = new MaterialPropertyBlock();

            // 初始化颜色缓存
            if (m_emissionSectionColors.Count == 0)
            {
                // 缓存各分段的颜色
                for (var i = 0; i < m_sections.Count; ++i)
                {
                    var section = m_sections[i];
                    var material = section.sharedMaterial;
                    m_baseSectionColors.Add(material.color);
                    m_emissionSectionColors.Add(material.GetVector(s_emissionParam));
                }

                // 缓存主网格的颜色
                {
                    var material = m_indicatorMesh.sharedMaterial;
                    m_indicatorMeshBaseColor = material.color;
                    m_indicatorMeshEmissionColor = material.GetVector(s_emissionParam);
                }
            }
        }

        /// <summary>
        /// 更新充能等级显示
        /// </summary>
        /// <param name="charge">当前充能百分比(0-100)</param>
        public void UpdateChargeLevel(float charge)
        {
            for (var i = 0; i < m_sections.Count; ++i)
            {
                var curSectionPct = i * m_pctPerSection;
                var show = curSectionPct < charge;
                m_sections[i].enabled = show;
            }
        }

        /// <summary>
        /// 设置为禁用状态
        /// 将所有分段和主网格的颜色设置为禁用状态颜色
        /// </summary>
        public void SetDisabledState()
        {
            for (var i = 0; i < m_sections.Count; ++i)
            {
                var section = m_sections[i];
                section.GetPropertyBlock(m_materialPropertyBlock);
                m_materialPropertyBlock.SetVector(s_emissionParam, m_disabledEmissionColor);
                m_materialPropertyBlock.SetColor(s_baseColorParam, m_disabledMainColor);
                section.SetPropertyBlock(m_materialPropertyBlock);
            }

            m_indicatorMesh.GetPropertyBlock(m_materialPropertyBlock);
            m_materialPropertyBlock.SetVector(s_emissionParam, m_disabledEmissionColor);
            m_materialPropertyBlock.SetColor(s_baseColorParam, m_disabledMainColor);
            m_indicatorMesh.SetPropertyBlock(m_materialPropertyBlock);
        }

        /// <summary>
        /// 设置为启用状态
        /// 恢复所有分段和主网格的原始颜色
        /// </summary>
        public void SetEnabledState()
        {
            for (var i = 0; i < m_sections.Count; ++i)
            {
                var section = m_sections[i];
                section.GetPropertyBlock(m_materialPropertyBlock);
                m_materialPropertyBlock.SetVector(s_emissionParam, m_emissionSectionColors[i]);
                m_materialPropertyBlock.SetColor(s_baseColorParam, m_baseSectionColors[i]);
                section.SetPropertyBlock(m_materialPropertyBlock);
            }

            m_indicatorMesh.GetPropertyBlock(m_materialPropertyBlock);
            m_materialPropertyBlock.SetVector(s_emissionParam, m_indicatorMeshEmissionColor);
            m_materialPropertyBlock.SetColor(s_baseColorParam, m_indicatorMeshBaseColor);
            m_indicatorMesh.SetPropertyBlock(m_materialPropertyBlock);
        }
    }
}