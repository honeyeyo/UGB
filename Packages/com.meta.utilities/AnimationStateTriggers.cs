// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Meta.Utilities
{
    /// <summary>
    /// 动画状态触发器
    /// 继承自StateMachineBehaviour，监听动画状态的进入和退出事件
    /// 与AnimationStateTriggerListener配合使用，实现动画状态的事件回调
    /// </summary>
    public class AnimationStateTriggers : StateMachineBehaviour
    {
        /// <summary>
        /// 动画状态触发器监听器的缓存引用
        /// 避免每次状态变化时重复查找组件
        /// </summary>
        private AnimationStateTriggerListener m_listener;

        /// <summary>
        /// 动画状态进入时的回调
        /// 当动画状态机进入某个状态时自动调用
        /// </summary>
        /// <param name="animator">动画控制器</param>
        /// <param name="stateInfo">状态信息</param>
        /// <param name="layerIndex">动画层索引</param>
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // 如果监听器尚未缓存，则从animator上获取
            if (m_listener == null)
                m_listener = animator.GetComponent<AnimationStateTriggerListener>();

            // 通知监听器状态进入事件
            m_listener.OnStateEnter(stateInfo, layerIndex);
        }

        /// <summary>
        /// 动画状态退出时的回调
        /// 当动画状态机退出某个状态时自动调用
        /// </summary>
        /// <param name="animator">动画控制器</param>
        /// <param name="stateInfo">状态信息</param>
        /// <param name="layerIndex">动画层索引</param>
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // 如果监听器尚未缓存，则从animator上获取
            if (m_listener == null)
                m_listener = animator.GetComponent<AnimationStateTriggerListener>();

            // 通知监听器状态退出事件
            m_listener.OnStateExit(stateInfo, layerIndex);
        }
    }
}
