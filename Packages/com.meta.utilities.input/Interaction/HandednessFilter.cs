// Copyright (c) Meta Platforms, Inc. and affiliates.

#if HAS_META_INTERACTION

using Oculus.Interaction;
using UnityEngine;

namespace Meta.Utilities.Input
{
    /// <summary>
    /// 手部类型过滤器
    /// 实现IGameObjectFilter接口，用于根据手部类型（左手或右手）过滤游戏对象
    /// 只有当游戏对象包含匹配手部类型的HandRef组件时才通过过滤
    /// </summary>
    public class HandednessFilter : MonoBehaviour, IGameObjectFilter
    {
        /// <summary>
        /// 指定的手部类型（左手或右手）
        /// 用于过滤条件的手部类型参考
        /// </summary>
        [SerializeField]
        private Oculus.Interaction.Input.Handedness m_hand;

        /// <summary>
        /// 过滤方法实现
        /// 检查游戏对象是否包含HandRef组件且手部类型匹配
        /// </summary>
        /// <param name="gameObject">要过滤的游戏对象</param>
        /// <returns>如果游戏对象的手部类型匹配则返回true，否则返回false</returns>
        public bool Filter(GameObject gameObject)
        {
            return gameObject.TryGetComponent<Oculus.Interaction.Input.HandRef>(out var hand) && hand.Handedness == m_hand;
        }
    }
}

#endif
