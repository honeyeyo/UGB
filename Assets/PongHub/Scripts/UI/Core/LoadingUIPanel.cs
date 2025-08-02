using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using PongHub.UI.Core;
using PongHub.UI.Localization;

namespace PongHub.UI.Core
{
    /// <summary>
    /// 场景加载UI面板 - 为VR环境优化的加载界面
    /// Loading UI Panel - VR-optimized loading interface for scene transitions
    /// </summary>
    public class LoadingUIPanel : VRUIComponent
    {
        [Header("加载界面设置")]
        [SerializeField]
        [Tooltip("背景图片 - Loading screen background")]
        private Image m_backgroundImage;

        [SerializeField]
        [Tooltip("加载标题文本")]
        private TextMeshProUGUI m_titleText;

        [SerializeField]
        [Tooltip("加载提示文本")]
        private TextMeshProUGUI m_loadingText;

        [SerializeField]
        [Tooltip("进度条")]
        private Slider m_progressBar;

        [SerializeField]
        [Tooltip("进度条填充图像")]
        private Image m_progressFill;

        [SerializeField]
        [Tooltip("旋转加载指示器")]
        private RectTransform m_loadingSpinner;

        [SerializeField]
        [Tooltip("进度数值文本")]
        private TextMeshProUGUI m_progressText;

        [Header("动画设置")]
        [SerializeField]
        [Tooltip("淡入时间")]
        [Range(0.1f, 2f)]
        private float m_fadeInDuration = 0.5f;

        [SerializeField]
        [Tooltip("淡出时间")]
        [Range(0.1f, 2f)]
        private float m_fadeOutDuration = 0.3f;

        [SerializeField]
        [Tooltip("旋转动画速度")]
        [Range(10f, 360f)]
        private float m_spinnerSpeed = 180f;

        [SerializeField]
        [Tooltip("进度条动画时间")]
        [Range(0.1f, 1f)]
        private float m_progressAnimDuration = 0.3f;

        [Header("颜色设置")]
        [SerializeField]
        [Tooltip("背景颜色")]
        private Color m_backgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.9f);

        [SerializeField]
        [Tooltip("进度条颜色")]
        private Color m_progressColor = new Color(0.2f, 0.6f, 1f, 1f);

        [SerializeField]
        [Tooltip("文本颜色")]
        private Color m_textColor = Color.white;

        // 私有变量
        private CanvasGroup m_canvasGroup;
        private Tween m_spinnerTween;
        private Tween m_progressTween;
        private bool m_isVisible = false;
        private float m_currentProgress = 0f;

        // 本地化键值
        private const string LOADING_TITLE_KEY = "ui.loading.title";
        private const string LOADING_TEXT_KEY = "ui.loading.loading";

        #region Unity生命周期

        protected override void Awake()
        {
            base.Awake();

            // 获取或创建CanvasGroup
            m_canvasGroup = GetComponent<CanvasGroup>();
            if (m_canvasGroup == null)
            {
                m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // 初始化组件
            InitializeComponents();

            // 初始状态为隐藏
            SetVisibilityImmediate(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // 订阅本地化事件
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
                UpdateLocalizedText();
            }
        }

        public override void OnDisable()
        {
            base.OnDisable();

            // 取消订阅本地化事件
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            }

            // 停止所有动画
            StopAllAnimations();
        }

        private void OnDestroy()
        {
            StopAllAnimations();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponents()
        {
            // 设置背景
            if (m_backgroundImage != null)
            {
                m_backgroundImage.color = m_backgroundColor;
            }

            // 设置进度条
            if (m_progressBar != null)
            {
                m_progressBar.value = 0f;
                m_progressBar.minValue = 0f;
                m_progressBar.maxValue = 1f;
            }

            if (m_progressFill != null)
            {
                m_progressFill.color = m_progressColor;
            }

            // 设置文本颜色
            if (m_titleText != null)
            {
                m_titleText.color = m_textColor;
            }

            if (m_loadingText != null)
            {
                m_loadingText.color = m_textColor;
            }

            if (m_progressText != null)
            {
                m_progressText.color = m_textColor;
                m_progressText.text = "0%";
            }

            // 更新本地化文本
            UpdateLocalizedText();
        }

        /// <summary>
        /// 更新本地化文本
        /// </summary>
        private void UpdateLocalizedText()
        {
            if (m_titleText != null)
            {
                string titleText = LocalizationManager.Instance?.GetLocalizedText(LOADING_TITLE_KEY) ?? "Loading";
                m_titleText.text = titleText;
            }

            if (m_loadingText != null)
            {
                string loadingText = LocalizationManager.Instance?.GetLocalizedText(LOADING_TEXT_KEY) ?? "Loading...";
                m_loadingText.text = loadingText;
            }
        }

        /// <summary>
        /// 语言变更回调
        /// </summary>
        private void OnLanguageChanged(string languageCode)
        {
            UpdateLocalizedText();
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 显示加载界面
        /// </summary>
        /// <param name="immediately">是否立即显示</param>
        public void Show(bool immediately = false)
        {
            if (m_isVisible) return;

            m_isVisible = true;
            gameObject.SetActive(true);

            if (immediately)
            {
                SetVisibilityImmediate(true);
            }
            else
            {
                StartFadeIn();
            }

            StartSpinnerAnimation();
        }

        /// <summary>
        /// 隐藏加载界面
        /// </summary>
        /// <param name="immediately">是否立即隐藏</param>
        public void Hide(bool immediately = false)
        {
            if (!m_isVisible) return;

            m_isVisible = false;

            if (immediately)
            {
                SetVisibilityImmediate(false);
                gameObject.SetActive(false);
            }
            else
            {
                StartFadeOut();
            }

            StopSpinnerAnimation();
        }

        /// <summary>
        /// 更新加载进度
        /// </summary>
        /// <param name="progress">进度值 (0-1)</param>
        /// <param name="animated">是否使用动画</param>
        public void SetProgress(float progress, bool animated = true)
        {
            progress = Mathf.Clamp01(progress);

            if (animated)
            {
                AnimateProgress(progress);
            }
            else
            {
                SetProgressImmediate(progress);
            }
        }

        /// <summary>
        /// 设置加载提示文本
        /// </summary>
        /// <param name="text">提示文本</param>
        public void SetLoadingText(string text)
        {
            if (m_loadingText != null)
            {
                m_loadingText.text = text;
            }
        }

        /// <summary>
        /// 获取当前是否可见
        /// </summary>
        /// <returns>是否可见</returns>
        public bool IsVisible()
        {
            return m_isVisible;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 立即设置可见性
        /// </summary>
        /// <param name="visible">是否可见</param>
        private void SetVisibilityImmediate(bool visible)
        {
            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = visible ? 1f : 0f;
                m_canvasGroup.interactable = visible;
                m_canvasGroup.blocksRaycasts = visible;
            }
        }

        /// <summary>
        /// 开始淡入动画
        /// </summary>
        private void StartFadeIn()
        {
            if (m_canvasGroup == null) return;

            m_canvasGroup.interactable = true;
            m_canvasGroup.blocksRaycasts = true;

            m_canvasGroup.alpha = 0f;
            DOTween.To(() => m_canvasGroup.alpha, x => m_canvasGroup.alpha = x, 1f, m_fadeInDuration)
                .SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// 开始淡出动画
        /// </summary>
        private void StartFadeOut()
        {
            if (m_canvasGroup == null) return;

            m_canvasGroup.interactable = false;
            m_canvasGroup.blocksRaycasts = false;

            DOTween.To(() => m_canvasGroup.alpha, x => m_canvasGroup.alpha = x, 0f, m_fadeOutDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(() => gameObject.SetActive(false));
        }

        /// <summary>
        /// 开始旋转动画
        /// </summary>
        private void StartSpinnerAnimation()
        {
            if (m_loadingSpinner == null) return;

            StopSpinnerAnimation();
            m_spinnerTween = m_loadingSpinner.DORotate(Vector3.forward * -360f, 360f / m_spinnerSpeed, RotateMode.WorldAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        }

        /// <summary>
        /// 停止旋转动画
        /// </summary>
        private void StopSpinnerAnimation()
        {
            if (m_spinnerTween != null && m_spinnerTween.IsActive())
            {
                m_spinnerTween.Kill();
                m_spinnerTween = null;
            }
        }

        /// <summary>
        /// 动画更新进度
        /// </summary>
        /// <param name="targetProgress">目标进度</param>
        private void AnimateProgress(float targetProgress)
        {
            if (m_progressTween != null && m_progressTween.IsActive())
            {
                m_progressTween.Kill();
            }

            m_progressTween = DOTween.To(() => m_currentProgress, x => SetProgressImmediate(x), targetProgress, m_progressAnimDuration)
                .SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// 立即设置进度
        /// </summary>
        /// <param name="progress">进度值</param>
        private void SetProgressImmediate(float progress)
        {
            m_currentProgress = progress;

            if (m_progressBar != null)
            {
                m_progressBar.value = progress;
            }

            if (m_progressText != null)
            {
                m_progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
            }
        }

        /// <summary>
        /// 停止所有动画
        /// </summary>
        private void StopAllAnimations()
        {
            StopSpinnerAnimation();

            if (m_progressTween != null && m_progressTween.IsActive())
            {
                m_progressTween.Kill();
                m_progressTween = null;
            }
        }

        #endregion

        #region VRUIComponent重写

        public override void UpdateVisualState(InteractionState state)
        {
            // 加载界面通常不需要交互状态变化
            // 这里可以根据需要添加特殊的视觉效果
        }

        #endregion

        #region 调试功能

        /// <summary>
        /// 测试显示加载界面
        /// </summary>
        [ContextMenu("Test Show Loading")]
        private void TestShowLoading()
        {
            if (Application.isPlaying)
            {
                Show();
                StartCoroutine(TestProgressAnimation());
            }
        }

        /// <summary>
        /// 测试隐藏加载界面
        /// </summary>
        [ContextMenu("Test Hide Loading")]
        private void TestHideLoading()
        {
            if (Application.isPlaying)
            {
                Hide();
            }
        }

        /// <summary>
        /// 测试进度动画
        /// </summary>
        private System.Collections.IEnumerator TestProgressAnimation()
        {
            float progress = 0f;
            while (progress < 1f)
            {
                progress += 0.1f;
                SetProgress(progress);
                yield return new WaitForSeconds(0.3f);
            }

            yield return new WaitForSeconds(1f);
            Hide();
        }

        #endregion
    }
}