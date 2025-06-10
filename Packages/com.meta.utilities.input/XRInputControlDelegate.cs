// Copyright (c) Meta Platforms, Inc. and affiliates.

#if HAS_META_AVATARS

#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

using Oculus.Avatar2;

#if USING_XR_SDK
using Button = OVRInput.Button;
using Touch = OVRInput.Touch;
#endif

namespace Meta.Utilities.Input
{
    /// <summary>
    /// XR输入控制委托
    /// 继承自OvrAvatarInputControlDelegate，负责处理XR控制器的输入状态
    /// 包括按钮按下、触摸检测、触发器和握持值的读取
    /// </summary>
    public class XRInputControlDelegate : OvrAvatarInputControlDelegate
    {
        /// <summary>
        /// XR输入控制动作配置引用
        /// 包含左右控制器的所有输入动作映射
        /// </summary>
        private XRInputControlActions m_controlActions;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="controlActions">输入控制动作配置</param>
        public XRInputControlDelegate(XRInputControlActions controlActions) => m_controlActions = controlActions;

        /// <summary>
        /// 获取输入控制状态
        /// 读取当前帧的所有控制器输入数据并返回状态
        /// </summary>
        /// <param name="inputControlState">输出的输入控制状态</param>
        /// <returns>是否成功获取状态</returns>
        public override bool GetInputControlState(out OvrAvatarInputControlState inputControlState)
        {
            // 检查是否有控制器连接（使用OVR原生检测）
            if (OVRInput.GetConnectedControllers() != OVRInput.Controller.None)
            {
                // 基于Avatar示例的SampleInputControlDelegate实现
                inputControlState = new OvrAvatarInputControlState
                {
                    type = GetControllerType()
                };

#if USING_XR_SDK
                // 使用OVR SDK更新控制器输入
                UpdateControllerInput(ref inputControlState.leftControllerState, OVRInput.Controller.LTouch);
                UpdateControllerInput(ref inputControlState.rightControllerState, OVRInput.Controller.RTouch);
#endif
            }

            // 初始化输入控制状态
            inputControlState = default;
            // 使用自定义输入动作更新控制器状态
            UpdateControllerInput(ref inputControlState.leftControllerState, ref m_controlActions.LeftController);
            UpdateControllerInput(ref inputControlState.rightControllerState, ref m_controlActions.RightController);
            return true;
        }

        /// <summary>
        /// 使用自定义输入动作更新控制器输入状态
        /// 从InputSystem读取按钮、触摸和轴向值
        /// </summary>
        /// <param name="controllerState">要更新的控制器状态</param>
        /// <param name="controller">控制器动作配置</param>
        private void UpdateControllerInput(ref OvrAvatarControllerState controllerState, ref XRInputControlActions.Controller controller)
        {
            // 重置按钮和触摸掩码
            controllerState.buttonMask = 0;
            controllerState.touchMask = 0;

            // 按钮按下检测
            if (controller.ButtonOne.action.ReadValue<float>() > 0.5f)
            {
                controllerState.buttonMask |= CAPI.ovrAvatar2Button.One;
            }
            if (controller.ButtonTwo.action.ReadValue<float>() > 0.5f)
            {
                controllerState.buttonMask |= CAPI.ovrAvatar2Button.Two;
            }
            if (controller.ButtonThree.action.ReadValue<float>() > 0.5f)
            {
                controllerState.buttonMask |= CAPI.ovrAvatar2Button.Three;
            }

            // 按钮触摸检测
            if (controller.TouchOne.action.ReadValue<float>() > 0.5f)
            {
                controllerState.touchMask |= CAPI.ovrAvatar2Touch.One;
            }
            if (controller.TouchTwo.action.ReadValue<float>() > 0.5f)
            {
                controllerState.touchMask |= CAPI.ovrAvatar2Touch.Two;
            }
            if (controller.TouchPrimaryThumbstick.action.ReadValue<float>() > 0.5f)
            {
                controllerState.touchMask |= CAPI.ovrAvatar2Touch.Joystick;
            }

            // 触发器（扳机）值读取
            controllerState.indexTrigger = controller.AxisIndexTrigger.action.ReadValue<float>();

            // 握持值读取
            controllerState.handTrigger = controller.AxisHandTrigger.action.ReadValue<float>();
        }

        // 基于Avatar示例的SampleInputControlDelegate实现
#if USING_XR_SDK
        /// <summary>
        /// 使用OVR SDK更新控制器输入状态
        /// 直接从OVRInput读取原生输入数据
        /// </summary>
        /// <param name="controllerState">要更新的控制器状态</param>
        /// <param name="controller">OVR控制器类型</param>
        private void UpdateControllerInput(ref OvrAvatarControllerState controllerState, OVRInput.Controller controller)
        {
            // 重置按钮和触摸掩码
            controllerState.buttonMask = 0;
            controllerState.touchMask = 0;

            // 按钮按下检测
            if (OVRInput.Get(Button.One, controller))
            {
                controllerState.buttonMask |= CAPI.ovrAvatar2Button.One;
            }
            if (OVRInput.Get(Button.Two, controller))
            {
                controllerState.buttonMask |= CAPI.ovrAvatar2Button.Two;
            }
            if (OVRInput.Get(Button.Three, controller))
            {
                controllerState.buttonMask |= CAPI.ovrAvatar2Button.Three;
            }
            if (OVRInput.Get(Button.PrimaryThumbstick, controller))
            {
                controllerState.buttonMask |= CAPI.ovrAvatar2Button.Joystick;
            }

            // 按钮触摸检测
            if (OVRInput.Get(Touch.One, controller))
            {
                controllerState.touchMask |= CAPI.ovrAvatar2Touch.One;
            }
            if (OVRInput.Get(Touch.Two, controller))
            {
                controllerState.touchMask |= CAPI.ovrAvatar2Touch.Two;
            }
            if (OVRInput.Get(Touch.PrimaryThumbstick, controller))
            {
                controllerState.touchMask |= CAPI.ovrAvatar2Touch.Joystick;
            }
            if (OVRInput.Get(Touch.PrimaryThumbRest, controller))
            {
                controllerState.touchMask |= CAPI.ovrAvatar2Touch.ThumbRest;
            }

            // 触发器（扳机）处理
            controllerState.indexTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller);
            if (OVRInput.Get(Touch.PrimaryIndexTrigger, controller))
            {
                controllerState.touchMask |= CAPI.ovrAvatar2Touch.Index;
            }
            else if (controllerState.indexTrigger <= 0f)
            {
                // TODO: Not sure if this is the correct way to do this
                controllerState.touchMask |= CAPI.ovrAvatar2Touch.Pointing;
            }

            // 握持值读取
            controllerState.handTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller);

            // Set ThumbUp if no other thumb-touch is set.
            // TODO: Not sure if this is the correct way to do this
            if ((controllerState.touchMask & (CAPI.ovrAvatar2Touch.One | CAPI.ovrAvatar2Touch.Two |
                                              CAPI.ovrAvatar2Touch.Joystick | CAPI.ovrAvatar2Touch.ThumbRest)) == 0)
            {
                controllerState.touchMask |= CAPI.ovrAvatar2Touch.ThumbUp;
            }
        }
#endif
    }
}

#endif
