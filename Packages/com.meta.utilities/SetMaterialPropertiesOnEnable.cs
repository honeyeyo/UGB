// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;

namespace Meta.Utilities
{
    /// <summary>
    /// 启用时设置材质属性组件
    /// 在组件启用时自动设置渲染器的材质属性，支持多种属性类型
    /// 使用MaterialPropertyBlock来设置属性，避免创建新的材质实例
    /// </summary>
    public class SetMaterialPropertiesOnEnable : MonoBehaviour
    {
        /// <summary>
        /// 属性值结构体
        /// 存储属性名和对应的值
        /// </summary>
        /// <typeparam name="T">属性值的类型</typeparam>
        [Serializable]
        public struct PropertyValue<T>
        {
            /// <summary>
            /// 属性名称
            /// </summary>
            public string Name;

            /// <summary>
            /// 属性值
            /// </summary>
            public T Value;
        }

        /// <summary>
        /// 目标渲染器
        /// </summary>
        [SerializeField] private Renderer m_renderer;

        /// <summary>
        /// 材质索引
        /// 指定要修改的材质在渲染器材质数组中的索引
        /// </summary>
        [SerializeField] private int m_materialIndex = 0;

        [Header("Properties")]
        /// <summary>
        /// 颜色属性数组
        /// 存储要设置的所有颜色类型的材质属性
        /// </summary>
        [SerializeField] private PropertyValue<Color>[] m_colors = { };

        /// <summary>
        /// 整数属性数组
        /// 存储要设置的所有整数类型的材质属性
        /// </summary>
        [SerializeField] private PropertyValue<int>[] m_integers = { };

        /// <summary>
        /// 浮点数属性数组
        /// 存储要设置的所有浮点数类型的材质属性
        /// </summary>
        [SerializeField] private PropertyValue<float>[] m_floats = { };

        /// <summary>
        /// 向量属性数组
        /// 存储要设置的所有Vector4类型的材质属性
        /// </summary>
        [SerializeField] private PropertyValue<Vector4>[] m_vectors = { };

        /// <summary>
        /// 纹理属性数组
        /// 存储要设置的所有纹理类型的材质属性
        /// </summary>
        [SerializeField] private PropertyValue<Texture>[] m_textures = { };

        /// <summary>
        /// 组件启用时的处理
        /// 创建MaterialPropertyBlock并设置所有配置的属性
        /// </summary>
        protected void OnEnable()
        {
            var block = new MaterialPropertyBlock();

            // 设置所有颜色属性
            foreach (var value in m_colors)
                block.SetColor(value.Name, value.Value);

            // 设置所有整数属性
            foreach (var value in m_integers)
                block.SetInteger(value.Name, value.Value);

            // 设置所有浮点数属性
            foreach (var value in m_floats)
                block.SetFloat(value.Name, value.Value);

            // 设置所有向量属性
            foreach (var value in m_vectors)
                block.SetVector(value.Name, value.Value);

            // 设置所有纹理属性
            foreach (var value in m_textures)
                block.SetTexture(value.Name, value.Value);

            // 将属性块应用到指定的材质
            m_renderer.SetPropertyBlock(block, m_materialIndex);
        }

        /// <summary>
        /// 编辑器验证时的处理
        /// 在编辑器中修改属性时立即应用更改
        /// </summary>
        protected void OnValidate()
        {
            OnEnable();
        }
    }
}
