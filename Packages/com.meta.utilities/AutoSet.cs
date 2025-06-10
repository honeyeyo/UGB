// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;

namespace Meta.Utilities
{
    /// <summary>
    /// 自动设置字段值的特性基类
    /// 用于标记需要自动填充的字段，支持编辑器自动化操作
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AutoSetAttribute : PropertyAttribute
    {
        /// <summary>
        /// AutoSetAttribute构造函数
        /// </summary>
        /// <param name="type">可选的类型参数，用于指定查找的组件类型</param>
        public AutoSetAttribute(Type type = default) { }
    }

    /// <summary>
    /// 从父对象自动设置字段值的特性
    /// 在父对象及其祖先中查找指定类型的组件并自动赋值
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AutoSetFromParentAttribute : AutoSetAttribute
    {
        /// <summary>
        /// 是否包含非活动的游戏对象
        /// 如果为true，则在搜索时包含被禁用的对象
        /// </summary>
        public bool IncludeInactive { get; set; } = false;
    }

    /// <summary>
    /// 从子对象自动设置字段值的特性
    /// 在子对象中查找指定类型的组件并自动赋值
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AutoSetFromChildrenAttribute : AutoSetAttribute
    {
        /// <summary>
        /// 是否包含非活动的游戏对象
        /// 如果为true，则在搜索时包含被禁用的对象
        /// </summary>
        public bool IncludeInactive { get; set; } = false;
    }
}