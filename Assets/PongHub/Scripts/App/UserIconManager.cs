// Copyright (c) MagnusLab Inc. and affiliates.

using System;
using System.Collections.Generic;
using Meta.Utilities;
using UnityEngine;

namespace PongHub.App
{
    /// <summary>
    /// 用户图标管理器
    /// 管理游戏中可使用的图标。这个单例会存在于启动场景的游戏对象上
    /// 包含图标SKU映射到图标精灵的列表，使得在游戏中可以轻松地将SKU映射到精灵
    /// </summary>
    public class UserIconManager : Singleton<UserIconManager>
    {
        /// <summary>
        /// 图标数据结构
        /// 包含SKU标识符和对应的精灵图标
        /// </summary>
        [Serializable]
        public struct IconData
        {
            /// <summary>
            /// 图标的SKU标识符（库存管理单位）
            /// </summary>
            public string SKU;

            /// <summary>
            /// 对应的图标精灵
            /// </summary>
            public Sprite Icon;
        }

        /// <summary>
        /// 图标数据数组
        /// 在Inspector中配置的SKU到图标的映射
        /// </summary>
        [SerializeField] private IconData[] m_iconDataArray;

        /// <summary>
        /// SKU到图标的字典映射
        /// 用于快速查找图标
        /// </summary>
        private Dictionary<string, Sprite> m_skuToIcon = new();

        /// <summary>
        /// 所有可用SKU的列表
        /// </summary>
        private List<string> m_allSkus = new();

        /// <summary>
        /// 获取所有可用的SKU数组
        /// </summary>
        public string[] AllSkus => m_allSkus.ToArray();

        /// <summary>
        /// 根据SKU获取对应的图标精灵
        /// </summary>
        /// <param name="sku">图标的SKU标识符</param>
        /// <returns>对应的图标精灵，如果找不到则返回null</returns>
        public Sprite GetIconForSku(string sku)
        {
            // 尝试从字典中获取图标，如果存在则返回，否则返回null
            return m_skuToIcon.TryGetValue(sku, out var icon) ? icon : null;
        }

        /// <summary>
        /// 内部初始化方法
        /// 在单例创建时调用，构建SKU到图标的映射关系
        /// </summary>
        protected override void InternalAwake()
        {
            base.InternalAwake();

            // 遍历所有图标数据，构建映射关系
            foreach (var iconData in m_iconDataArray)
            {
                // 建立SKU到图标的映射
                m_skuToIcon[iconData.SKU] = iconData.Icon;
                // 添加SKU到列表中
                m_allSkus.Add(iconData.SKU);
            }
        }
    }
}