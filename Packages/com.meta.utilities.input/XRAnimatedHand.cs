// Copyright (c) Meta Platforms, Inc. and affiliates.

#if HAS_META_AVATARS

using Meta.Utilities;
using Meta.Utilities.Input;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace Meta.Utilities.Input
{
    /// <summary>
    /// XR动画手部控制器
    /// 根据控制器输入状态自动播放手部动画
    /// 包括指向、竖拇指、握拳等手势动画
    /// </summary>
    public class XRAnimatedHand : MonoBehaviour
    {
        /// <summary>
        /// 指向动画层名称常量
        /// </summary>
        public const string LAYER_NAME_POINT = "Point Layer";

        /// <summary>
        /// 拇指动画层名称常量
        /// </summary>
        public const string LAYER_NAME_THUMB = "Thumb Layer";

        /// <summary>
        /// 握拳参数名称常量
        /// </summary>
        public const string PARAM_NAME_FLEX = "Flex";

        /// <summary>
        /// 输入值变化速率常量，控制动画过渡的平滑度
        /// </summary>
        public const float INPUT_RATE_CHANGE = 20.0f;

        /// <summary>
        /// 手部动画控制器
        /// </summary>
        [SerializeField] private Animator m_animator;

        /// <summary>
        /// XR输入管理器引用，从父对象自动设置
        /// </summary>
        [AutoSetFromParent]
        [SerializeField] private XRInputManager m_xrInputManager;

        /// <summary>
        /// 是否为左手标志
        /// </summary>
        [SerializeField] private bool m_isLeftHand;

        /// <summary>
        /// 指向动画层索引
        /// </summary>
        private int m_animLayerIndexPoint = -1;

        /// <summary>
        /// 拇指动画层索引
        /// </summary>
        private int m_animLayerIndexThumb = -1;

        /// <summary>
        /// 握拳动画参数索引
        /// </summary>
        private int m_animParamIndexFlex = -1;

        /// <summary>
        /// 是否正在竖拇指状态
        /// </summary>
        private bool m_isGivingThumbsUp;

        /// <summary>
        /// 是否正在指向状态
        /// </summary>
        private bool m_isPointing;

        /// <summary>
        /// 指向动画混合值（0-1）
        /// </summary>
        private float m_pointBlend;

        /// <summary>
        /// 竖拇指动画混合值（0-1）
        /// </summary>
        private float m_thumbsUpBlend;

        /// <summary>
        /// 当前手部的控制器动作集合
        /// </summary>
        private XRInputControlActions.Controller Actions => m_xrInputManager.GetActions(m_isLeftHand);

        /// <summary>
        /// 组件开始时的初始化
        /// 获取动画层和参数索引，初始化XR设备输入
        /// </summary>
        protected virtual void Start()
        {
            // 获取动画层索引
            m_animLayerIndexPoint = m_animator.GetLayerIndex(LAYER_NAME_POINT);
            m_animLayerIndexThumb = m_animator.GetLayerIndex(LAYER_NAME_THUMB);
            m_animParamIndexFlex = Animator.StringToHash(PARAM_NAME_FLEX);

            // 查询XR设备以"唤醒"电容式触摸输入
            var device = InputDevices.GetDeviceAtXRNode(m_isLeftHand ? XRNode.LeftHand : XRNode.RightHand);
            _ = InputHelpers.IsPressed(device, InputHelpers.Button.PrimaryTouch, out _);
        }

        /// <summary>
        /// 每帧更新手部动画状态
        /// 检测输入状态并更新动画参数
        /// </summary>
        protected virtual void Update()
        {
            // 更新电容式触摸状态
            UpdateCapTouchStates();
            // 更新指向状态
            UpdatePointingState();
            // 平滑过渡动画混合值
            m_pointBlend = InputValueRateChange(m_isPointing, m_pointBlend);
            m_thumbsUpBlend = InputValueRateChange(m_isGivingThumbsUp, m_thumbsUpBlend);

            // 更新动画状态
            UpdateAnimStates();
        }

        /// <summary>
        /// 更新指向状态
        /// 当食指触发器值小于0.5时认为是在指向
        /// </summary>
        private void UpdatePointingState()
        {
            m_isPointing = Actions.AxisIndexTrigger.action.ReadValue<float>() < 0.5f;
        }

        /// <summary>
        /// 更新电容式触摸状态
        /// 当拇指没有触摸任何主要按钮时认为是竖拇指状态
        /// </summary>
        private void UpdateCapTouchStates()
        {
            m_isGivingThumbsUp = Actions.AnyPrimaryThumbButtonTouching < 0.5f;
        }

        /// <summary>
        /// 基于OVR示例的InputValueRateChange方法
        /// 确保动画混合以可控的时间进行，而不是瞬间变化
        /// </summary>
        /// <param name="isDown">动画方向</param>
        /// <param name="value">要改变的值</param>
        /// <returns>以固定速率增加或减少的输入值</returns>
        private float InputValueRateChange(bool isDown, float value)
        {
            var rateDelta = Time.deltaTime * INPUT_RATE_CHANGE;
            var sign = isDown ? 1.0f : -1.0f;
            return Mathf.Clamp01(value + rateDelta * sign);
        }

        /// <summary>
        /// 更新动画状态参数
        /// 设置握拳、指向、竖拇指和捏取的动画参数
        /// </summary>
        private void UpdateAnimStates()
        {
            // 握拳动画
            // 在张开手掌和完全握拳之间混合
            var flex = Actions.AxisHandTrigger.action.ReadValue<float>();
            m_animator.SetFloat(m_animParamIndexFlex, flex);

            // 指向动画
            m_animator.SetLayerWeight(m_animLayerIndexPoint, m_pointBlend);

            // 竖拇指动画
            m_animator.SetLayerWeight(m_animLayerIndexThumb, m_thumbsUpBlend);

            // 捏取动画
            var pinch = Actions.AxisIndexTrigger.action.ReadValue<float>();
            m_animator.SetFloat("Pinch", pinch);
        }
    }
}

#endif
