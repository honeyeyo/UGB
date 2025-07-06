using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PongHub.UI.Core
{
    /// <summary>
    /// VR UI组件基类
    /// 为所有VR UI组件提供基础的交互功能和状态管理
    /// </summary>
    public abstract class VRUIComponent : MonoBehaviour
    {
        /// <summary>
        /// 组件交互状态枚举
        /// </summary>
        public enum InteractionState
        {
            Normal,     // 正常状态
            Highlighted, // 高亮状态（悬停）
            Pressed,    // 按下状态
            Selected,   // 选中状态
            Disabled    // 禁用状态
        }

        [Header("交互设置")]
        [SerializeField]
        [Tooltip("Interactable / 可交互 - Whether the component can be interacted with")]
        protected bool m_interactable = true;

        [Header("反馈设置")]
        [SerializeField]
        [Tooltip("Hover Sound / 悬停音效 - Sound played when pointer enters the component")]
        protected AudioClip m_hoverSound;

        [SerializeField]
        [Tooltip("Click Sound / 点击音效 - Sound played when component is clicked")]
        protected AudioClip m_clickSound;

        [SerializeField]
        [Tooltip("Haptic Feedback Intensity / 触觉反馈强度 - Intensity of haptic feedback (0-1)")]
        [Range(0f, 1f)]
        protected float m_hapticFeedbackIntensity = 0.2f;

        [SerializeField]
        [Tooltip("Haptic Feedback Duration / 触觉反馈时长 - Duration of haptic feedback in seconds")]
        [Range(0.01f, 0.5f)]
        protected float m_hapticFeedbackDuration = 0.05f;

        [Header("动画设置")]
        [SerializeField]
        [Tooltip("Use Animation / 使用动画 - Enable state transition animations")]
        protected bool m_useAnimation = true;

        [SerializeField]
        [Tooltip("Animation Duration / 动画时长 - Duration of state transition animations")]
        [Range(0.01f, 0.5f)]
        protected float m_animationDuration = 0.1f;

        // 事件
        [Header("事件")]
        public UnityEvent OnClick = new UnityEvent();
        public UnityEvent OnHover = new UnityEvent();
        public UnityEvent OnHoverExit = new UnityEvent();
        public UnityEvent<bool> OnInteractableChanged = new UnityEvent<bool>();

        // 当前状态
        protected InteractionState m_currentState = InteractionState.Normal;
        protected bool m_isHovered = false;
        protected bool m_isPressed = false;

        // 音频源
        protected AudioSource m_audioSource;

        // 主题引用
        protected VRUITheme m_theme;

        #region Unity生命周期

        protected virtual void Awake()
        {
            // 初始化音频源
            SetupAudioSource();

            // 获取主题
            GetTheme();

            // 初始化状态
            UpdateVisualState(m_interactable ? InteractionState.Normal : InteractionState.Disabled);
        }

        protected virtual void OnEnable()
        {
            // 重置状态
            m_isHovered = false;
            m_isPressed = false;
            UpdateVisualState(m_interactable ? InteractionState.Normal : InteractionState.Disabled);
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 设置组件是否可交互
        /// </summary>
        public virtual void SetInteractable(bool interactable)
        {
            if (m_interactable == interactable)
                return;

            m_interactable = interactable;
            UpdateVisualState(m_interactable ? InteractionState.Normal : InteractionState.Disabled);
            OnInteractableChanged.Invoke(m_interactable);
        }

        /// <summary>
        /// 获取组件是否可交互
        /// </summary>
        public bool IsInteractable()
        {
            return m_interactable;
        }

        /// <summary>
        /// 获取当前交互状态
        /// </summary>
        public InteractionState GetCurrentState()
        {
            return m_currentState;
        }

        /// <summary>
        /// 设置主题
        /// </summary>
        public virtual void SetTheme(VRUITheme theme)
        {
            m_theme = theme;
            UpdateVisualState(m_currentState);
        }

        #endregion

        #region 交互方法

        /// <summary>
        /// 指针进入
        /// </summary>
        public virtual void OnPointerEnter()
        {
            if (!m_interactable)
                return;

            m_isHovered = true;
            UpdateVisualState(m_isPressed ? InteractionState.Pressed : InteractionState.Highlighted);
            PlayHoverSound();
            OnHover.Invoke();
        }

        /// <summary>
        /// 指针退出
        /// </summary>
        public virtual void OnPointerExit()
        {
            if (!m_interactable)
                return;

            m_isHovered = false;
            UpdateVisualState(InteractionState.Normal);
            OnHoverExit.Invoke();
        }

        /// <summary>
        /// 指针按下
        /// </summary>
        public virtual void OnPointerDown()
        {
            if (!m_interactable)
                return;

            m_isPressed = true;
            UpdateVisualState(InteractionState.Pressed);
            TriggerHapticFeedback();
        }

        /// <summary>
        /// 指针抬起
        /// </summary>
        public virtual void OnPointerUp()
        {
            if (!m_interactable)
                return;

            m_isPressed = false;
            UpdateVisualState(m_isHovered ? InteractionState.Highlighted : InteractionState.Normal);
        }

        /// <summary>
        /// 指针点击
        /// </summary>
        public virtual void OnPointerClick()
        {
            if (!m_interactable)
                return;

            PlayClickSound();
            TriggerHapticFeedback();
            OnClick.Invoke();
        }

        #endregion

        #region 抽象方法

        /// <summary>
        /// 更新视觉状态
        /// </summary>
        public abstract void UpdateVisualState(InteractionState state);

        #endregion

        #region 辅助方法

        /// <summary>
        /// 设置音频源
        /// </summary>
        protected virtual void SetupAudioSource()
        {
            // 查找或创建音频源
            m_audioSource = GetComponent<AudioSource>();
            if (m_audioSource == null)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
                m_audioSource.playOnAwake = false;
                m_audioSource.spatialBlend = 1.0f; // 3D音效
                m_audioSource.minDistance = 0.1f;
                m_audioSource.maxDistance = 5.0f;
                m_audioSource.rolloffMode = AudioRolloffMode.Linear;
                m_audioSource.volume = 0.5f;
            }
        }

        /// <summary>
        /// 获取主题
        /// </summary>
        protected virtual void GetTheme()
        {
            // 尝试从VRUIManager获取主题
            if (VRUIManager.Instance != null)
            {
                m_theme = VRUIManager.Instance.GetCurrentTheme();
            }
        }

        /// <summary>
        /// 播放悬停音效
        /// </summary>
        protected virtual void PlayHoverSound()
        {
            if (m_audioSource != null && m_hoverSound != null)
            {
                m_audioSource.PlayOneShot(m_hoverSound, 0.5f);
            }
        }

        /// <summary>
        /// 播放点击音效
        /// </summary>
        protected virtual void PlayClickSound()
        {
            if (m_audioSource != null && m_clickSound != null)
            {
                m_audioSource.PlayOneShot(m_clickSound, 0.7f);
            }
        }

        /// <summary>
        /// 触发触觉反馈
        /// </summary>
        protected virtual void TriggerHapticFeedback()
        {
            // 触发触觉反馈
            // 注：实际实现需要与VR控制器交互系统集成
            if (VRUIManager.Instance != null)
            {
                VRUIManager.Instance.TriggerHapticFeedback(m_hapticFeedbackIntensity, m_hapticFeedbackDuration);
            }
        }

        #endregion
    }
}