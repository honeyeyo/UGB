// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.XR;

namespace Meta.Utilities.Input
{
    /// <summary>
    /// XR跟踪姿态驱动器
    /// 继承自TrackedPoseDriver，增加了输入数据更新事件的功能
    /// 用于跟踪XR设备的位置和旋转，并在数据更新时触发事件
    /// </summary>
    public class XRTrackedPoseDriver : TrackedPoseDriver
    {
        /// <summary>
        /// 当执行更新时触发的Unity事件
        /// 可在Inspector中配置响应方法
        /// </summary>
        [SerializeField] private UnityEvent m_onPerformUpdate = new();

        /// <summary>
        /// 输入数据可用时触发的C#事件
        /// 用于代码中的事件订阅
        /// </summary>
        public event Action InputDataAvailable;

        /// <summary>
        /// 当前数据版本号
        /// 每次更新时递增，用于跟踪数据的变化
        /// </summary>
        public int CurrentDataVersion { get; private set; }

        /// <summary>
        /// 执行姿态更新
        /// 重写基类方法，在执行基础更新后触发相关事件
        /// </summary>
        protected override void PerformUpdate()
        {
            // 执行基类的姿态更新逻辑
            base.PerformUpdate();

            // 递增数据版本号
            CurrentDataVersion += 1;

            // 触发输入数据可用事件
            InputDataAvailable?.Invoke();

            // 触发Unity事件
            m_onPerformUpdate.Invoke();
        }

        /// <summary>
        /// 重写基类的OnUpdate方法
        /// 直接调用PerformUpdate来确保事件正常触发
        /// </summary>
        protected override void OnUpdate()
        {
            PerformUpdate();
        }
    }
}
