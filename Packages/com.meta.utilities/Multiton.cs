// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Meta.Utilities
{
    /// <summary>
    /// 泛型多例模式基类
    /// 继承自MonoBehaviour，允许存在多个实例并提供统一的实例管理
    /// 自动维护所有活动实例的集合，支持对所有实例的枚举访问
    /// </summary>
    /// <typeparam name="T">继承自Multiton的具体类型</typeparam>
    public class Multiton<T> : MonoBehaviour where T : Multiton<T>
    {
        /// <summary>
        /// 内部实例集合
        /// 存储所有活动的实例引用，使用HashSet确保唯一性和快速查找
        /// </summary>
        protected static readonly HashSet<T> InternalInstances = new();

        /// <summary>
        /// 只读实例列表结构体
        /// 提供对内部实例集合的只读访问，实现IEnumerable接口支持foreach遍历
        /// </summary>
        public struct ReadOnlyList : IEnumerable<T>
        {
            /// <summary>
            /// 获取实例集合的枚举器
            /// </summary>
            /// <returns>HashSet的枚举器</returns>
            public HashSet<T>.Enumerator GetEnumerator() => InternalInstances.GetEnumerator();
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// 所有实例的只读访问属性
        /// 返回包含所有活动实例的只读列表
        /// </summary>
        public static ReadOnlyList Instances => new();

        /// <summary>
        /// MonoBehaviour的Awake生命周期方法
        /// 当组件首次创建时将实例添加到集合中
        /// </summary>
        protected void Awake()
        {
            // 如果组件被禁用则不添加到实例集合
            if (!enabled)
                return;
            _ = InternalInstances.Add((T)this);
        }

        /// <summary>
        /// MonoBehaviour的OnEnable生命周期方法
        /// 当组件被启用时确保实例在集合中
        /// </summary>
        protected void OnEnable()
        {
            _ = InternalInstances.Add((T)this);
        }

        /// <summary>
        /// MonoBehaviour的OnDisable生命周期方法
        /// 当组件被禁用时从集合中移除实例
        /// </summary>
        protected void OnDisable()
        {
            _ = InternalInstances.Remove((T)this);
        }

        /// <summary>
        /// MonoBehaviour的OnDestroy生命周期方法
        /// 当组件被销毁时从集合中移除实例
        /// </summary>
        protected void OnDestroy()
        {
            _ = InternalInstances.Remove((T)this);
        }
    }
}
