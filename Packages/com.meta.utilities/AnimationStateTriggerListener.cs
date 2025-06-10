// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Events;

namespace Meta.Utilities
{
    /// <summary>
    /// 动画状态触发器监听器
    /// 监听动画状态的进入和退出事件，并触发相应的UnityEvent
    /// 与AnimationStateTriggers配合使用，提供可在Inspector中配置的事件回调
    /// </summary>
    public class AnimationStateTriggerListener : MonoBehaviour
    {
        /// <summary>
        /// 状态进入时触发的事件
        /// 在Inspector中可以配置要执行的方法
        /// </summary>
        [SerializeField] private UnityEvent m_onStateEnter;

        /// <summary>
        /// 状态退出时触发的事件
        /// 在Inspector中可以配置要执行的方法
        /// </summary>
        [SerializeField] private UnityEvent m_onStateExit;

        /// <summary>
        /// 状态进入时的虚拟方法
        /// 由AnimationStateTriggers调用，可以被子类重写以实现自定义逻辑
        /// </summary>
        /// <param name="stateInfo">动画状态信息</param>
        /// <param name="layerIndex">动画层索引</param>
        public virtual void OnStateEnter(AnimatorStateInfo stateInfo, int layerIndex)
        {
            m_onStateEnter?.Invoke();
        }

        /// <summary>
        /// 状态退出时的虚拟方法
        /// 由AnimationStateTriggers调用，可以被子类重写以实现自定义逻辑
        /// </summary>
        /// <param name="stateInfo">动画状态信息</param>
        /// <param name="layerIndex">动画层索引</param>
        public virtual void OnStateExit(AnimatorStateInfo stateInfo, int layerIndex)
        {
            m_onStateExit?.Invoke();
        }
    }
}
