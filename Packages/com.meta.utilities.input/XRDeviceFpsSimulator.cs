// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using CommonUsages = UnityEngine.InputSystem.CommonUsages;

namespace Meta.Utilities.Input
{
    /// <summary>
    /// XR设备FPS模拟器
    /// 在没有真实XR头显的情况下，使用鼠标键盘模拟XR设备的行为
    /// 支持头部旋转、移动以及控制器的输入模拟
    /// </summary>
    public class XRDeviceFpsSimulator : MonoBehaviour
    {
        /// <summary>
        /// 模拟的头显状态数据
        /// </summary>
        private XRSimulatedHMDState m_hmdState;

        /// <summary>
        /// 模拟的左控制器状态数据
        /// </summary>
        private XRSimulatedControllerState m_leftControllerState;

        /// <summary>
        /// 模拟的右控制器状态数据
        /// </summary>
        private XRSimulatedControllerState m_rightControllerState;

        /// <summary>
        /// 模拟的头显设备实例
        /// </summary>
        private XRSimulatedHMD m_hmdDevice;

        /// <summary>
        /// 模拟的左控制器设备实例
        /// </summary>
        private XRSimulatedController m_leftControllerDevice;

        /// <summary>
        /// 模拟的右控制器设备实例
        /// </summary>
        private XRSimulatedController m_rightControllerDevice;

        /// <summary>
        /// 是否在检测到真实头显连接时禁用模拟器
        /// </summary>
        [SerializeField] private bool m_disableIfRealHmdConnected = true;

        [Header("Mouse Capture")]
        /// <summary>
        /// 是否捕获鼠标光标（用于视角控制）
        /// </summary>
        [SerializeField] private bool m_captureMouse = true;

        /// <summary>
        /// 是否需要窗口焦点才能接收输入
        /// </summary>
        [SerializeField] private bool m_requireFocus = true;

        /// <summary>
        /// 释放鼠标捕获的输入动作
        /// </summary>
        [SerializeField] private InputActionProperty m_releaseMouseCaptureAction;

        [Header("Input Configuration")]
        /// <summary>
        /// 头部旋转输入动作（通常绑定到鼠标移动）
        /// </summary>
        [SerializeField] private InputActionProperty m_rotationAction;

        /// <summary>
        /// 头部移动输入动作（通常绑定到WASD键）
        /// </summary>
        [SerializeField] private InputActionProperty m_headMovementAction;

        /// <summary>
        /// 左手摇杆输入动作
        /// </summary>
        [SerializeField] private InputActionProperty m_leftThumbstickAction;

        /// <summary>
        /// 右手摇杆输入动作
        /// </summary>
        [SerializeField] private InputActionProperty m_rightThumbstickAction;

        /// <summary>
        /// 左手握持输入动作
        /// </summary>
        [SerializeField] private InputActionProperty m_leftGrabAction;

        /// <summary>
        /// 右手握持输入动作
        /// </summary>
        [SerializeField] private InputActionProperty m_rightGrabAction;

        /// <summary>
        /// 左手触发器输入动作
        /// </summary>
        [SerializeField] private InputActionProperty m_leftTriggerAction;

        /// <summary>
        /// 右手触发器输入动作
        /// </summary>
        [SerializeField] private InputActionProperty m_rightTriggerAction;

        /// <summary>
        /// 右手移动输入动作（XY轴）
        /// </summary>
        [SerializeField] private InputActionProperty m_rightHandMovementAction;

        /// <summary>
        /// 右手移动输入动作（Z轴）
        /// </summary>
        [SerializeField] private InputActionProperty m_rightHandMovementZAction;

        /// <summary>
        /// 左手菜单按钮输入动作
        /// </summary>
        [SerializeField] private InputActionProperty m_leftHandMenuAction;

        /// <summary>
        /// 所有输入动作的集合，用于批量操作
        /// </summary>
        private InputActionProperty[] AllActions => new[] {
            m_releaseMouseCaptureAction,
            m_rotationAction,
            m_headMovementAction,
            m_leftThumbstickAction,
            m_rightThumbstickAction,
            m_leftGrabAction,
            m_rightGrabAction,
            m_leftTriggerAction,
            m_rightTriggerAction,
            m_rightHandMovementAction,
            m_rightHandMovementZAction,
            m_leftHandMenuAction,
        };

        [Header("XR Simulation Configuration")]
        /// <summary>
        /// 头显中心视点的起始位置
        /// </summary>
        [SerializeField] private Vector3 m_centerEyeStartPosition = Vector3.up;

        /// <summary>
        /// 左控制器相对于头显的位置偏移
        /// </summary>
        [SerializeField] private Vector3 m_leftControllerOffset = new(-0.25f, -0.15f, 0.65f);

        /// <summary>
        /// 左控制器的旋转偏移（欧拉角）
        /// </summary>
        [SerializeField] private Vector3 m_leftControllerRotation = new(-30, 0, -60);

        /// <summary>
        /// 右控制器相对于头显的位置偏移
        /// </summary>
        [SerializeField] private Vector3 m_rightControllerOffset = new(0.25f, -0.15f, 0.65f);

        /// <summary>
        /// 右控制器的旋转偏移（欧拉角）
        /// </summary>
        [SerializeField] private Vector3 m_rightControllerRotation = new(-30, 0, 60);

        /// <summary>
        /// 头部旋转累积值（用于鼠标控制视角）
        /// </summary>
        private Vector2 m_headRotator = Vector2.zero;

        /// <summary>
        /// 组件启用时的初始化
        /// 检查是否有真实头显连接，初始化设备状态，添加模拟设备
        /// </summary>
        private void OnEnable()
        {
            // 如果检测到真实头显连接且设置了禁用标志，则禁用模拟器
            if (m_disableIfRealHmdConnected && XRSettings.loadedDeviceName?.Trim() is not ("MockHMD Display" or "" or null))
            {
                Debug.Log($"[XRDeviceFpsSimulator] Disabling in favor of {XRSettings.loadedDeviceName}", this);
                enabled = false;
                return;
            }

            // 重置所有设备状态
            m_hmdState.Reset();
            m_hmdState.centerEyePosition = m_centerEyeStartPosition;
            m_leftControllerState.Reset();
            m_rightControllerState.Reset();

            // 添加模拟设备到输入系统
            AddDevices();
            // 启用所有输入动作
            EnableActions();
        }

        /// <summary>
        /// 组件禁用时的清理
        /// 禁用输入动作，移除模拟设备
        /// </summary>
        private void OnDisable()
        {
            DisableActions();
            RemoveDevices();
        }

        /// <summary>
        /// 启用所有输入动作
        /// </summary>
        private void EnableActions()
        {
            foreach (var action in AllActions)
                action.action.Enable();
        }

        /// <summary>
        /// 禁用所有输入动作
        /// </summary>
        private void DisableActions()
        {
            foreach (var action in AllActions)
                action.action.Disable();
        }

        /// <summary>
        /// 向输入系统添加模拟设备
        /// 创建模拟的头显和左右控制器设备
        /// </summary>
        private void AddDevices()
        {
            // 添加模拟头显设备
            m_hmdDevice = InputSystem.AddDevice<XRSimulatedHMD>();
            if (m_hmdDevice == null)
            {
                Debug.LogError($"Failed to create {nameof(XRSimulatedHMD)}.");
            }

            // 添加模拟左控制器设备
            m_leftControllerDevice = InputSystem.AddDevice<XRSimulatedController>($"{nameof(XRSimulatedController)} - {CommonUsages.LeftHand}");
            if (m_leftControllerDevice != null)
            {
                InputSystem.SetDeviceUsage(m_leftControllerDevice, CommonUsages.LeftHand);
            }
            else
            {
                Debug.LogError($"Failed to create {nameof(XRSimulatedController)} for {CommonUsages.LeftHand}.", this);
            }

            // 添加模拟右控制器设备
            m_rightControllerDevice = InputSystem.AddDevice<XRSimulatedController>($"{nameof(XRSimulatedController)} - {CommonUsages.RightHand}");
            if (m_rightControllerDevice != null)
            {
                InputSystem.SetDeviceUsage(m_rightControllerDevice, CommonUsages.RightHand);
            }
            else
            {
                Debug.LogError($"Failed to create {nameof(XRSimulatedController)} for {CommonUsages.RightHand}.", this);
            }
        }

        /// <summary>
        /// 从输入系统移除模拟设备
        /// </summary>
        private void RemoveDevices()
        {
            if (m_hmdDevice != null && m_hmdDevice.added)
                InputSystem.RemoveDevice(m_hmdDevice);

            if (m_leftControllerDevice != null && m_leftControllerDevice.added)
                InputSystem.RemoveDevice(m_leftControllerDevice);

            if (m_rightControllerDevice != null && m_rightControllerDevice.added)
                InputSystem.RemoveDevice(m_rightControllerDevice);
        }

        /// <summary>
        /// 读取输入动作的值（泛型版本）
        /// </summary>
        /// <typeparam name="T">值的类型</typeparam>
        /// <param name="property">输入动作属性</param>
        /// <returns>读取到的值，如果输入被禁用则返回默认值</returns>
        private T ReadAction<T>(InputActionProperty property) where T : struct =>
            IsInputEnabled ? property.action.ReadValue<T>() : default;

        /// <summary>
        /// 检查输入动作是否被按下
        /// </summary>
        /// <param name="property">输入动作属性</param>
        /// <returns>是否被按下</returns>
        private bool IsPressed(InputActionProperty property) =>
            IsInputEnabled && property.action.IsPressed();

        /// <summary>
        /// 每帧更新模拟设备的状态
        /// 处理鼠标捕获、头部移动旋转、控制器输入等
        /// </summary>
        private void Update()
        {
            // 处理鼠标捕获
            if (m_captureMouse)
            {
                var shouldCapture = !m_releaseMouseCaptureAction.action.IsPressed();
                Cursor.lockState = shouldCapture ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !shouldCapture;
            }

            // 设置所有设备为已跟踪状态
            m_leftControllerState.isTracked = true;
            m_rightControllerState.isTracked = true;
            m_hmdState.isTracked = true;
            m_leftControllerState.trackingState = (int)(InputTrackingState.Position | InputTrackingState.Rotation);
            m_rightControllerState.trackingState = (int)(InputTrackingState.Position | InputTrackingState.Rotation);
            m_hmdState.trackingState = (int)(InputTrackingState.Position | InputTrackingState.Rotation);

            // 处理头部旋转（鼠标移动）
            var mouseDelta = ReadAction<Vector2>(m_rotationAction);
            m_headRotator += mouseDelta;
            m_hmdState.centerEyeRotation = Quaternion.Euler(m_headRotator.y, m_headRotator.x, 0);
            m_hmdState.deviceRotation = m_hmdState.centerEyeRotation;

            // 添加微小抖动以保持更新（防止系统优化导致的停止更新）
            m_hmdState.centerEyePosition += Mathf.Sin(Time.time) * 0.001f * Time.deltaTime * Vector3.up;
            var moveDelta = ReadAction<Vector3>(m_headMovementAction);
            m_hmdState.centerEyePosition += m_hmdState.deviceRotation * moveDelta;
            m_hmdState.devicePosition = m_hmdState.centerEyePosition;

            // 处理右手移动（用于调试和测试）
            var handMovement = ReadAction<Vector2>(m_rightHandMovementAction);
            var handMovementZ = ReadAction<float>(m_rightHandMovementZAction);
            m_rightControllerOffset += handMovement.WithZ(handMovementZ);

            // 更新控制器位置和旋转
            UpdateControllerState(ref m_leftControllerState, m_leftControllerOffset, Quaternion.Euler(m_leftControllerRotation));
            UpdateControllerState(ref m_rightControllerState, m_rightControllerOffset, Quaternion.Euler(m_rightControllerRotation));

            // 更新左控制器的菜单按钮状态
            m_leftControllerState = m_leftControllerState.WithButton(ControllerButton.MenuButton, IsPressed(m_leftHandMenuAction));

            // 更新左控制器的输入值
            m_leftControllerState.trigger = ReadAction<float>(m_leftTriggerAction);
            m_leftControllerState.grip = ReadAction<float>(m_leftGrabAction);
            m_leftControllerState.primary2DAxis = ReadAction<Vector2>(m_leftThumbstickAction);
            UpdateButtons(ref m_leftControllerState);

            // 更新右控制器的输入值
            m_rightControllerState.trigger = ReadAction<float>(m_rightTriggerAction);
            m_rightControllerState.grip = ReadAction<float>(m_rightGrabAction);
            m_rightControllerState.primary2DAxis = ReadAction<Vector2>(m_rightThumbstickAction);
            UpdateButtons(ref m_rightControllerState);

            // 如果输入启用，则更新设备状态
            if (IsInputEnabled)
                UpdateDevices();
        }

        /// <summary>
        /// 更新控制器按钮状态
        /// 根据触发器和握持值自动设置按钮按下状态
        /// </summary>
        /// <param name="state">要更新的控制器状态</param>
        private static void UpdateButtons(ref XRSimulatedControllerState state)
        {
            state = state.WithButton(ControllerButton.GripButton, state.grip > 0.5f);
            state = state.WithButton(ControllerButton.TriggerButton, state.trigger > 0.5f);
        }

        /// <summary>
        /// 检查应用程序是否获得焦点且光标不可见
        /// </summary>
        private static bool IsFocused => Application.isFocused && !IsCursorVisible() && !Cursor.visible;

        /// <summary>
        /// 输入是否启用（基于焦点要求）
        /// </summary>
        private bool IsInputEnabled => !m_requireFocus || IsFocused;

#if UNITY_EDITOR_WIN
#pragma warning disable IDE0049
#pragma warning disable IDE1006

        /// <summary>
        /// Windows API结构：屏幕坐标点
        /// </summary>
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct POINT { public Int32 x; public Int32 y; }

        /// <summary>
        /// Windows API结构：光标信息
        /// </summary>
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct CURSORINFO
        {
            public Int32 cbSize; public Int32 flags; public IntPtr hCursor; public POINT ptScreenPos;
        }

        /// <summary>
        /// Windows API：获取光标信息
        /// </summary>
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetCursorInfo")]
        public static extern bool GetCursorInfo(ref CURSORINFO pci);

#pragma warning restore IDE0049
#pragma warning restore IDE1006
#endif

        /// <summary>
        /// 检查光标是否可见（特别是在Windows编辑器中）
        /// </summary>
        /// <returns>光标是否可见</returns>
        private static bool IsCursorVisible()
        {
#if UNITY_EDITOR_WIN
            var cursor = new CURSORINFO();
            cursor.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(cursor);

            if (GetCursorInfo(ref cursor))
            {
                return cursor.flags == 1;
            }
#endif

            return Cursor.visible;
        }

        /// <summary>
        /// 更新控制器的位置和旋转状态
        /// </summary>
        /// <param name="controller">要更新的控制器状态</param>
        /// <param name="offset">相对于头显的位置偏移</param>
        /// <param name="rot">旋转偏移</param>
        private void UpdateControllerState(ref XRSimulatedControllerState controller, Vector3 offset, Quaternion rot)
        {
            controller.devicePosition = m_hmdState.devicePosition + m_hmdState.deviceRotation * offset;
            controller.deviceRotation = m_hmdState.deviceRotation * rot;
        }

        /// <summary>
        /// 将更新后的状态数据发送到输入系统的设备中
        /// </summary>
        private void UpdateDevices()
        {
            if (m_hmdDevice != null)
            {
                InputState.Change(m_hmdDevice, m_hmdState);
            }

            if (m_leftControllerDevice != null)
            {
                InputState.Change(m_leftControllerDevice, m_leftControllerState);
            }

            if (m_rightControllerDevice != null)
            {
                InputState.Change(m_rightControllerDevice, m_rightControllerState);
            }
        }
    }
}
