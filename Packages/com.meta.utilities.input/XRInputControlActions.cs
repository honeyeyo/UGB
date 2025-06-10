// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Meta.Utilities.Input
{
    /// <summary>
    /// XR输入控制动作配置
    /// ScriptableObject资产，定义了左右控制器的所有输入动作映射
    /// 包括按钮、触摸、触发器和轴向输入
    /// </summary>
    public class XRInputControlActions : ScriptableObject
    {
        /// <summary>
        /// 控制器输入动作结构
        /// 定义单个控制器的所有输入动作属性
        /// </summary>
        [System.Serializable]
        public struct Controller
        {
            /// <summary>
            /// 第一个按钮输入动作（通常为A/X按钮）
            /// </summary>
            public InputActionProperty ButtonOne;

            /// <summary>
            /// 第二个按钮输入动作（通常为B/Y按钮）
            /// </summary>
            public InputActionProperty ButtonTwo;

            /// <summary>
            /// 第三个按钮输入动作（通常为菜单按钮）
            /// </summary>
            public InputActionProperty ButtonThree;

            /// <summary>
            /// 主摇杆按钮输入动作
            /// </summary>
            public InputActionProperty ButtonPrimaryThumbstick;

            /// <summary>
            /// 第一个按钮触摸检测动作
            /// </summary>
            public InputActionProperty TouchOne;

            /// <summary>
            /// 第二个按钮触摸检测动作
            /// </summary>
            public InputActionProperty TouchTwo;

            /// <summary>
            /// 主摇杆触摸检测动作
            /// </summary>
            public InputActionProperty TouchPrimaryThumbstick;

            /// <summary>
            /// 拇指休息区触摸检测动作
            /// </summary>
            public InputActionProperty TouchPrimaryThumbRest;

            /// <summary>
            /// 食指触发器轴向输入动作（0-1范围）
            /// </summary>
            public InputActionProperty AxisIndexTrigger;

            /// <summary>
            /// 手部握持触发器轴向输入动作（0-1范围）
            /// </summary>
            public InputActionProperty AxisHandTrigger;

            /// <summary>
            /// 所有输入动作的集合，用于批量操作
            /// </summary>
            public InputActionProperty[] AllActions => new[] {
                ButtonOne,
                ButtonTwo,
                ButtonThree,
                ButtonPrimaryThumbstick,
                TouchOne,
                TouchTwo,
                TouchPrimaryThumbstick,
                TouchPrimaryThumbRest,
                AxisIndexTrigger,
                AxisHandTrigger,
            };

            /// <summary>
            /// 主要拇指按钮和触摸区域的集合
            /// 用于检测拇指是否在任何主要交互区域
            /// </summary>
            public InputActionProperty[] PrimaryThumbButtonTouches => new[] {
                TouchOne,
                TouchTwo,
                TouchPrimaryThumbRest,
                TouchPrimaryThumbstick
            };

            /// <summary>
            /// 检测拇指是否在任何主要按钮或触摸区域
            /// 返回所有主要拇指触摸区域中的最大值
            /// </summary>
            public float AnyPrimaryThumbButtonTouching =>
                PrimaryThumbButtonTouches.Max(a => a.action.ReadValue<float>());
        }

        /// <summary>
        /// 左控制器的输入动作配置
        /// </summary>
        public Controller LeftController;

        /// <summary>
        /// 右控制器的输入动作配置
        /// </summary>
        public Controller RightController;

        /// <summary>
        /// 所有控制器的所有输入动作集合
        /// 用于批量启用、禁用或管理所有输入动作
        /// </summary>
        public IEnumerable<InputActionProperty> AllActions =>
            new[] { LeftController, RightController }.SelectMany(c => c.AllActions);

        /// <summary>
        /// 启用所有输入动作
        /// 调用此方法来激活左右控制器的所有输入监听
        /// </summary>
        public void EnableActions()
        {
            foreach (var action in AllActions)
                action.action.Enable();
        }
    }
}
