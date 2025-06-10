// Copyright (c) Meta Platforms, Inc. and affiliates.

#if HAS_META_INTERACTION

using System.Linq;
using Oculus.Interaction;
using UnityEngine;
using static Oculus.Interaction.RayInteractor;

namespace Meta.Utilities.Input
{
    /// <summary>
    /// 交互对象过滤器活动状态
    /// 实现IActiveState接口，根据交互器的候选属性和排除标签来确定活动状态
    /// 当射线交互器指向的最近交互对象不在排除标签列表中时，状态为活动
    /// </summary>
    public class InteractableFilterActiveState : MonoBehaviour, IActiveState
    {
        /// <summary>
        /// 交互器视图接口的MonoBehaviour引用
        /// 用于获取交互器的候选属性信息
        /// </summary>
        [SerializeField, Interface(typeof(IInteractorView))]
        private MonoBehaviour m_interactor;

        /// <summary>
        /// 交互器视图属性访问器
        /// 将MonoBehaviour转换为IInteractorView接口
        /// </summary>
        private IInteractorView Interactor => (IInteractorView)m_interactor;

        /// <summary>
        /// 排除的标签数组
        /// 包含这些标签的交互对象将被排除，不会激活此状态
        /// </summary>
        [SerializeField]
        private string[] m_excludedTags;

        /// <summary>
        /// 活动状态属性
        /// 当交互器的候选属性是RayCandidateProperties类型，
        /// 且最近的交互对象存在并且不包含任何排除标签时返回true
        /// </summary>
        public bool Active => Interactor.CandidateProperties is RayCandidateProperties candidate &&
            candidate.ClosestInteractable != null &&
            m_excludedTags.All(t => candidate.ClosestInteractable.CompareTag(t) is false);
    }
}

#endif
