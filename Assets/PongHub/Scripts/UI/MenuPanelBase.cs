using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace PongHub.UI
{
    /// <summary>
    /// 菜单面板基类，提供所有菜单面板的基本功能
    /// </summary>
    public abstract class MenuPanelBase : MonoBehaviour
    {
        [Header("面板设置")]
        [SerializeField] protected string m_panelName = "Panel";
        [SerializeField] protected bool m_showOnStart = false;
        [SerializeField] protected float m_showAnimationDuration = 0.3f;
        [SerializeField] protected float m_hideAnimationDuration = 0.2f;

        [Header("面板组件")]
        [SerializeField] protected CanvasGroup m_canvasGroup;
        [SerializeField] protected RectTransform m_panelTransform;
        [SerializeField] protected Button m_backButton;

        [Header("动画设置")]
        [SerializeField] protected AnimationCurve m_showAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] protected AnimationCurve m_hideAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] protected Vector3 m_hiddenPositionOffset = new Vector3(0, -50, 0);
        [SerializeField] protected Vector3 m_hiddenRotationOffset = new Vector3(0, 0, 0);
        [SerializeField] protected Vector3 m_hiddenScaleOffset = new Vector3(0.9f, 0.9f, 0.9f);

        [Header("增强动画设置")]
        [SerializeField] protected PanelAnimationType m_animationType = PanelAnimationType.Fade;
        [SerializeField] protected PanelAnimationDirection m_animationDirection = PanelAnimationDirection.Bottom;
        [SerializeField] protected float m_animationDistance = 100f;
        [SerializeField] protected float m_rotationAngle = 15f;
        [SerializeField] protected bool m_useCustomHiddenPosition = false;
        [SerializeField] protected bool m_useElasticEffect = false;
        [SerializeField] protected float m_elasticOvershoot = 1.1f;

        // 动画协程
        protected Coroutine m_showAnimationCoroutine;
        protected Coroutine m_hideAnimationCoroutine;

        // 面板状态
        protected bool m_isInitialized = false;
        protected bool m_isVisible = false;

        // 事件
        public event Action<MenuPanelBase> OnPanelShown;
        public event Action<MenuPanelBase> OnPanelHidden;

        // 原始变换值
        protected Vector3 m_originalPosition;
        protected Vector3 m_originalRotation;
        protected Vector3 m_originalScale;

        /// <summary>
        /// 面板动画类型
        /// </summary>
        public enum PanelAnimationType
        {
            Fade,               // 淡入淡出
            Slide,              // 滑动
            Scale,              // 缩放
            Rotate,             // 旋转
            FadeAndSlide,       // 淡入淡出+滑动
            FadeAndScale,       // 淡入淡出+缩放
            FadeAndRotate,      // 淡入淡出+旋转
            SlideAndRotate,     // 滑动+旋转
            ScaleAndRotate,     // 缩放+旋转
            Complete            // 综合动画（淡入淡出+滑动+缩放+旋转）
        }

        /// <summary>
        /// 面板动画方向
        /// </summary>
        public enum PanelAnimationDirection
        {
            Left,               // 左侧
            Right,              // 右侧
            Top,                // 顶部
            Bottom,             // 底部
            TopLeft,            // 左上
            TopRight,           // 右上
            BottomLeft,         // 左下
            BottomRight,        // 右下
            Center              // 中心
        }

        #region Unity生命周期

        protected virtual void Awake()
        {
            // 确保有CanvasGroup组件
            if (m_canvasGroup == null)
            {
                m_canvasGroup = GetComponent<CanvasGroup>();
                if (m_canvasGroup == null)
                {
                    m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            // 确保有RectTransform组件
            if (m_panelTransform == null)
            {
                m_panelTransform = GetComponent<RectTransform>();
            }

            // 保存原始变换值
            if (m_panelTransform != null)
            {
                m_originalPosition = m_panelTransform.localPosition;
                m_originalRotation = m_panelTransform.localEulerAngles;
                m_originalScale = m_panelTransform.localScale;
            }

            // 设置返回按钮事件
            if (m_backButton != null)
            {
                m_backButton.onClick.AddListener(OnBackButtonClicked);
            }
        }

        protected virtual void Start()
        {
            Initialize();

            // 根据配置决定是否显示面板
            if (m_showOnStart)
            {
                Show();
            }
            else
            {
                Hide(true);
            }
        }

        protected virtual void OnDestroy()
        {
            // 清理事件
            if (m_backButton != null)
            {
                m_backButton.onClick.RemoveListener(OnBackButtonClicked);
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化面板
        /// </summary>
        public virtual void Initialize()
        {
            if (m_isInitialized) return;

            // 子类可以在这里进行初始化操作
            OnInitialize();

            m_isInitialized = true;
        }

        /// <summary>
        /// 显示面板
        /// </summary>
        /// <param name="instant">是否立即显示（无动画）</param>
        public virtual void Show(bool instant = false)
        {
            if (m_isVisible) return;

            // 停止任何正在进行的动画
            StopAllAnimations();

            // 确保游戏对象是激活的
            gameObject.SetActive(true);

            if (instant)
            {
                // 立即显示
                SetPanelVisibility(true);
                OnPanelShown?.Invoke(this);
            }
            else
            {
                // 播放显示动画
                m_showAnimationCoroutine = StartCoroutine(PlayShowAnimation());
            }

            m_isVisible = true;
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        /// <param name="instant">是否立即隐藏（无动画）</param>
        public virtual void Hide(bool instant = false)
        {
            if (!m_isVisible) return;

            // 停止任何正在进行的动画
            StopAllAnimations();

            if (instant)
            {
                // 立即隐藏
                SetPanelVisibility(false);
                OnPanelHidden?.Invoke(this);
            }
            else
            {
                // 播放隐藏动画
                m_hideAnimationCoroutine = StartCoroutine(PlayHideAnimation());
            }

            m_isVisible = false;
        }

        /// <summary>
        /// 重置面板状态
        /// </summary>
        public virtual void Reset()
        {
            // 子类可以在这里进行重置操作
            OnReset();
        }

        /// <summary>
        /// 切换面板可见性
        /// </summary>
        public virtual void Toggle()
        {
            if (m_isVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// 设置动画类型
        /// </summary>
        public virtual void SetAnimationType(PanelAnimationType animationType)
        {
            m_animationType = animationType;
        }

        /// <summary>
        /// 设置动画方向
        /// </summary>
        public virtual void SetAnimationDirection(PanelAnimationDirection direction)
        {
            m_animationDirection = direction;
            UpdateHiddenPositionOffset();
        }

        #endregion

        #region 保护方法

        /// <summary>
        /// 初始化时调用
        /// </summary>
        protected virtual void OnInitialize()
        {
            // 子类实现具体初始化逻辑
        }

        /// <summary>
        /// 重置时调用
        /// </summary>
        protected virtual void OnReset()
        {
            // 子类实现具体重置逻辑
        }

        /// <summary>
        /// 显示动画开始时调用
        /// </summary>
        protected virtual void OnShowAnimationStart()
        {
            // 子类实现具体显示动画开始逻辑
        }

        /// <summary>
        /// 显示动画结束时调用
        /// </summary>
        protected virtual void OnShowAnimationComplete()
        {
            // 子类实现具体显示动画结束逻辑
        }

        /// <summary>
        /// 隐藏动画开始时调用
        /// </summary>
        protected virtual void OnHideAnimationStart()
        {
            // 子类实现具体隐藏动画开始逻辑
        }

        /// <summary>
        /// 隐藏动画结束时调用
        /// </summary>
        protected virtual void OnHideAnimationComplete()
        {
            // 子类实现具体隐藏动画结束逻辑
        }

        /// <summary>
        /// 返回按钮点击时调用
        /// </summary>
        protected virtual void OnBackButtonClicked()
        {
            // 默认行为是隐藏当前面板
            // 子类可以覆盖此方法提供自定义行为
            Hide();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 设置面板可见性
        /// </summary>
        private void SetPanelVisibility(bool visible)
        {
            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = visible ? 1f : 0f;
                m_canvasGroup.interactable = visible;
                m_canvasGroup.blocksRaycasts = visible;
            }

            if (m_panelTransform != null)
            {
                if (visible)
                {
                    m_panelTransform.localPosition = m_originalPosition;
                    m_panelTransform.localEulerAngles = m_originalRotation;
                    m_panelTransform.localScale = m_originalScale;
                }
                else
                {
                    Vector3 hiddenPosition = m_useCustomHiddenPosition ?
                        m_originalPosition + m_hiddenPositionOffset :
                        GetDirectionalOffset(m_originalPosition);

                    m_panelTransform.localPosition = hiddenPosition;
                    m_panelTransform.localEulerAngles = m_originalRotation + m_hiddenRotationOffset;
                    m_panelTransform.localScale = Vector3.Scale(m_originalScale, m_hiddenScaleOffset);
                }
            }

            // 如果完全隐藏，可以禁用游戏对象以节省资源
            if (!visible)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 停止所有动画协程
        /// </summary>
        private void StopAllAnimations()
        {
            if (m_showAnimationCoroutine != null)
            {
                StopCoroutine(m_showAnimationCoroutine);
                m_showAnimationCoroutine = null;
            }

            if (m_hideAnimationCoroutine != null)
            {
                StopCoroutine(m_hideAnimationCoroutine);
                m_hideAnimationCoroutine = null;
            }
        }

        /// <summary>
        /// 根据动画方向获取位置偏移
        /// </summary>
        private Vector3 GetDirectionalOffset(Vector3 basePosition)
        {
            Vector3 offset = Vector3.zero;

            switch (m_animationDirection)
            {
                case PanelAnimationDirection.Left:
                    offset = new Vector3(-m_animationDistance, 0, 0);
                    break;
                case PanelAnimationDirection.Right:
                    offset = new Vector3(m_animationDistance, 0, 0);
                    break;
                case PanelAnimationDirection.Top:
                    offset = new Vector3(0, m_animationDistance, 0);
                    break;
                case PanelAnimationDirection.Bottom:
                    offset = new Vector3(0, -m_animationDistance, 0);
                    break;
                case PanelAnimationDirection.TopLeft:
                    offset = new Vector3(-m_animationDistance, m_animationDistance, 0);
                    break;
                case PanelAnimationDirection.TopRight:
                    offset = new Vector3(m_animationDistance, m_animationDistance, 0);
                    break;
                case PanelAnimationDirection.BottomLeft:
                    offset = new Vector3(-m_animationDistance, -m_animationDistance, 0);
                    break;
                case PanelAnimationDirection.BottomRight:
                    offset = new Vector3(m_animationDistance, -m_animationDistance, 0);
                    break;
                case PanelAnimationDirection.Center:
                    // 从中心缩放，不需要位置偏移
                    break;
            }

            return basePosition + offset;
        }

        /// <summary>
        /// 更新隐藏位置偏移
        /// </summary>
        private void UpdateHiddenPositionOffset()
        {
            if (!m_useCustomHiddenPosition)
            {
                switch (m_animationDirection)
                {
                    case PanelAnimationDirection.Left:
                        m_hiddenPositionOffset = new Vector3(-m_animationDistance, 0, 0);
                        break;
                    case PanelAnimationDirection.Right:
                        m_hiddenPositionOffset = new Vector3(m_animationDistance, 0, 0);
                        break;
                    case PanelAnimationDirection.Top:
                        m_hiddenPositionOffset = new Vector3(0, m_animationDistance, 0);
                        break;
                    case PanelAnimationDirection.Bottom:
                        m_hiddenPositionOffset = new Vector3(0, -m_animationDistance, 0);
                        break;
                    case PanelAnimationDirection.TopLeft:
                        m_hiddenPositionOffset = new Vector3(-m_animationDistance, m_animationDistance, 0);
                        break;
                    case PanelAnimationDirection.TopRight:
                        m_hiddenPositionOffset = new Vector3(m_animationDistance, m_animationDistance, 0);
                        break;
                    case PanelAnimationDirection.BottomLeft:
                        m_hiddenPositionOffset = new Vector3(-m_animationDistance, -m_animationDistance, 0);
                        break;
                    case PanelAnimationDirection.BottomRight:
                        m_hiddenPositionOffset = new Vector3(m_animationDistance, -m_animationDistance, 0);
                        break;
                    case PanelAnimationDirection.Center:
                        m_hiddenPositionOffset = Vector3.zero;
                        break;
                }
            }
        }

        /// <summary>
        /// 获取旋转偏移
        /// </summary>
        private Vector3 GetRotationOffset()
        {
            Vector3 rotationOffset = Vector3.zero;

            switch (m_animationDirection)
            {
                case PanelAnimationDirection.Left:
                    rotationOffset = new Vector3(0, 0, m_rotationAngle);
                    break;
                case PanelAnimationDirection.Right:
                    rotationOffset = new Vector3(0, 0, -m_rotationAngle);
                    break;
                case PanelAnimationDirection.Top:
                    rotationOffset = new Vector3(-m_rotationAngle, 0, 0);
                    break;
                case PanelAnimationDirection.Bottom:
                    rotationOffset = new Vector3(m_rotationAngle, 0, 0);
                    break;
                case PanelAnimationDirection.TopLeft:
                    rotationOffset = new Vector3(-m_rotationAngle, 0, m_rotationAngle);
                    break;
                case PanelAnimationDirection.TopRight:
                    rotationOffset = new Vector3(-m_rotationAngle, 0, -m_rotationAngle);
                    break;
                case PanelAnimationDirection.BottomLeft:
                    rotationOffset = new Vector3(m_rotationAngle, 0, m_rotationAngle);
                    break;
                case PanelAnimationDirection.BottomRight:
                    rotationOffset = new Vector3(m_rotationAngle, 0, -m_rotationAngle);
                    break;
                case PanelAnimationDirection.Center:
                    rotationOffset = new Vector3(0, m_rotationAngle, 0);
                    break;
            }

            return rotationOffset;
        }

        /// <summary>
        /// 播放显示动画
        /// </summary>
        private IEnumerator PlayShowAnimation()
        {
            OnShowAnimationStart();

            // 确保游戏对象是激活的
            gameObject.SetActive(true);

            // 设置初始状态
            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = 0f;
                m_canvasGroup.interactable = false;
                m_canvasGroup.blocksRaycasts = false;
            }

            // 获取动画起始位置
            Vector3 startPosition = m_useCustomHiddenPosition ?
                m_originalPosition + m_hiddenPositionOffset :
                GetDirectionalOffset(m_originalPosition);

            Vector3 startRotation = m_originalRotation;
            Vector3 startScale = Vector3.Scale(m_originalScale, m_hiddenScaleOffset);

            // 根据动画类型设置起始旋转
            if (m_animationType == PanelAnimationType.Rotate ||
                m_animationType == PanelAnimationType.FadeAndRotate ||
                m_animationType == PanelAnimationType.SlideAndRotate ||
                m_animationType == PanelAnimationType.ScaleAndRotate ||
                m_animationType == PanelAnimationType.Complete)
            {
                startRotation = m_originalRotation + GetRotationOffset();
            }

            if (m_panelTransform != null)
            {
                m_panelTransform.localPosition = startPosition;
                m_panelTransform.localEulerAngles = startRotation;
                m_panelTransform.localScale = startScale;
            }

            float elapsedTime = 0f;
            while (elapsedTime < m_showAnimationDuration)
            {
                float t = elapsedTime / m_showAnimationDuration;
                float curveValue = m_showAnimationCurve.Evaluate(t);

                // 如果使用弹性效果，调整曲线值
                if (m_useElasticEffect && t > 0.7f)
                {
                    float elasticT = (t - 0.7f) / 0.3f;
                    float elasticValue = Mathf.Sin(elasticT * Mathf.PI * 2) * (1 - elasticT) * 0.1f * m_elasticOvershoot;
                    curveValue += elasticValue;
                    curveValue = Mathf.Clamp01(curveValue);
                }

                // 根据动画类型更新面板状态
                UpdatePanelAnimation(curveValue, startPosition, startRotation, startScale, true);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 确保最终状态正确
            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = 1f;
                m_canvasGroup.interactable = true;
                m_canvasGroup.blocksRaycasts = true;
            }

            if (m_panelTransform != null)
            {
                m_panelTransform.localPosition = m_originalPosition;
                m_panelTransform.localEulerAngles = m_originalRotation;
                m_panelTransform.localScale = m_originalScale;
            }

            OnShowAnimationComplete();
            OnPanelShown?.Invoke(this);
            m_showAnimationCoroutine = null;
        }

        /// <summary>
        /// 播放隐藏动画
        /// </summary>
        private IEnumerator PlayHideAnimation()
        {
            OnHideAnimationStart();

            // 获取动画目标位置
            Vector3 targetPosition = m_useCustomHiddenPosition ?
                m_originalPosition + m_hiddenPositionOffset :
                GetDirectionalOffset(m_originalPosition);

            Vector3 targetRotation = m_originalRotation;
            Vector3 targetScale = Vector3.Scale(m_originalScale, m_hiddenScaleOffset);

            // 根据动画类型设置目标旋转
            if (m_animationType == PanelAnimationType.Rotate ||
                m_animationType == PanelAnimationType.FadeAndRotate ||
                m_animationType == PanelAnimationType.SlideAndRotate ||
                m_animationType == PanelAnimationType.ScaleAndRotate ||
                m_animationType == PanelAnimationType.Complete)
            {
                targetRotation = m_originalRotation + GetRotationOffset();
            }

            float elapsedTime = 0f;
            while (elapsedTime < m_hideAnimationDuration)
            {
                float t = elapsedTime / m_hideAnimationDuration;
                float curveValue = m_hideAnimationCurve.Evaluate(t);

                // 根据动画类型更新面板状态
                UpdatePanelAnimation(1f - curveValue, targetPosition, targetRotation, targetScale, false);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 确保最终状态正确
            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = 0f;
                m_canvasGroup.interactable = false;
                m_canvasGroup.blocksRaycasts = false;
            }

            OnHideAnimationComplete();
            OnPanelHidden?.Invoke(this);
            m_hideAnimationCoroutine = null;

            // 隐藏游戏对象
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 根据动画类型更新面板状态
        /// </summary>
        private void UpdatePanelAnimation(float t, Vector3 startPosition, Vector3 startRotation, Vector3 startScale, bool isShowing)
        {
            if (m_panelTransform == null) return;

            // 目标位置、旋转和缩放
            Vector3 targetPosition = isShowing ? m_originalPosition : startPosition;
            Vector3 targetRotation = isShowing ? m_originalRotation : startRotation;
            Vector3 targetScale = isShowing ? m_originalScale : startScale;

            // 当前位置、旋转和缩放
            Vector3 currentPosition = m_panelTransform.localPosition;
            Vector3 currentRotation = m_panelTransform.localEulerAngles;
            Vector3 currentScale = m_panelTransform.localScale;

            // 根据动画类型应用不同的动画效果
            switch (m_animationType)
            {
                case PanelAnimationType.Fade:
                    // 只更新透明度
                    if (m_canvasGroup != null)
                    {
                        m_canvasGroup.alpha = t;
                    }
                    break;

                case PanelAnimationType.Slide:
                    // 只更新位置
                    m_panelTransform.localPosition = Vector3.Lerp(
                        isShowing ? startPosition : m_originalPosition,
                        isShowing ? m_originalPosition : startPosition,
                        t
                    );
                    break;

                case PanelAnimationType.Scale:
                    // 只更新缩放
                    m_panelTransform.localScale = Vector3.Lerp(
                        isShowing ? startScale : m_originalScale,
                        isShowing ? m_originalScale : startScale,
                        t
                    );
                    break;

                case PanelAnimationType.Rotate:
                    // 只更新旋转
                    m_panelTransform.localEulerAngles = Vector3.Lerp(
                        isShowing ? startRotation : m_originalRotation,
                        isShowing ? m_originalRotation : startRotation,
                        t
                    );
                    break;

                case PanelAnimationType.FadeAndSlide:
                    // 更新透明度和位置
                    if (m_canvasGroup != null)
                    {
                        m_canvasGroup.alpha = t;
                    }
                    m_panelTransform.localPosition = Vector3.Lerp(
                        isShowing ? startPosition : m_originalPosition,
                        isShowing ? m_originalPosition : startPosition,
                        t
                    );
                    break;

                case PanelAnimationType.FadeAndScale:
                    // 更新透明度和缩放
                    if (m_canvasGroup != null)
                    {
                        m_canvasGroup.alpha = t;
                    }
                    m_panelTransform.localScale = Vector3.Lerp(
                        isShowing ? startScale : m_originalScale,
                        isShowing ? m_originalScale : startScale,
                        t
                    );
                    break;

                case PanelAnimationType.FadeAndRotate:
                    // 更新透明度和旋转
                    if (m_canvasGroup != null)
                    {
                        m_canvasGroup.alpha = t;
                    }
                    m_panelTransform.localEulerAngles = Vector3.Lerp(
                        isShowing ? startRotation : m_originalRotation,
                        isShowing ? m_originalRotation : startRotation,
                        t
                    );
                    break;

                case PanelAnimationType.SlideAndRotate:
                    // 更新位置和旋转
                    m_panelTransform.localPosition = Vector3.Lerp(
                        isShowing ? startPosition : m_originalPosition,
                        isShowing ? m_originalPosition : startPosition,
                        t
                    );
                    m_panelTransform.localEulerAngles = Vector3.Lerp(
                        isShowing ? startRotation : m_originalRotation,
                        isShowing ? m_originalRotation : startRotation,
                        t
                    );
                    break;

                case PanelAnimationType.ScaleAndRotate:
                    // 更新缩放和旋转
                    m_panelTransform.localScale = Vector3.Lerp(
                        isShowing ? startScale : m_originalScale,
                        isShowing ? m_originalScale : startScale,
                        t
                    );
                    m_panelTransform.localEulerAngles = Vector3.Lerp(
                        isShowing ? startRotation : m_originalRotation,
                        isShowing ? m_originalRotation : startRotation,
                        t
                    );
                    break;

                case PanelAnimationType.Complete:
                    // 更新所有变换
                    if (m_canvasGroup != null)
                    {
                        m_canvasGroup.alpha = t;
                    }
                    m_panelTransform.localPosition = Vector3.Lerp(
                        isShowing ? startPosition : m_originalPosition,
                        isShowing ? m_originalPosition : startPosition,
                        t
                    );
                    m_panelTransform.localScale = Vector3.Lerp(
                        isShowing ? startScale : m_originalScale,
                        isShowing ? m_originalScale : startScale,
                        t
                    );
                    m_panelTransform.localEulerAngles = Vector3.Lerp(
                        isShowing ? startRotation : m_originalRotation,
                        isShowing ? m_originalRotation : startRotation,
                        t
                    );
                    break;
            }

            // 设置交互状态
            if (m_canvasGroup != null)
            {
                m_canvasGroup.interactable = t > 0.5f;
                m_canvasGroup.blocksRaycasts = t > 0.5f;
            }
        }

        #endregion
    }
}