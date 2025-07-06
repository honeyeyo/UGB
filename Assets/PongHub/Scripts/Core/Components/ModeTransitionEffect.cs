using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using PongHub.Core;
using System.Linq; // Added for FirstOrDefault

namespace PongHub.Core.Components
{
    /// <summary>
    /// 模式切换效果控制器
    /// 负责在游戏模式切换时提供视觉反馈和过渡动画
    /// </summary>
    public class ModeTransitionEffect : MonoBehaviour, IGameModeComponent
    {
        [Header("过渡效果设置")]
        [SerializeField]
        [Tooltip("Transition Canvas / 过渡画布 - Canvas for transition effects")]
        private Canvas m_transitionCanvas;

        [SerializeField]
        [Tooltip("Fade Image / 淡入淡出图像 - Image used for fade transitions")]
        private Image m_fadeImage;

        [SerializeField]
        [Tooltip("Transition Duration / 过渡时长 - Duration of transition animations in seconds")]
        private float m_transitionDuration = 0.5f;

        [SerializeField]
        [Tooltip("Transition Curve / 过渡曲线 - Animation curve for transition effects")]
        private AnimationCurve m_transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("视觉效果设置")]
        [SerializeField]
        [Tooltip("Mode Icons / 模式图标 - Icons for different game modes (Local, Network, Menu)")]
        private Sprite[] m_modeIcons;

        [SerializeField]
        [Tooltip("Mode Icon Image / 模式图标图像 - Image component to display mode icons")]
        private Image m_modeIconImage;

        [SerializeField]
        [Tooltip("Show Mode Icon / 显示模式图标 - Show mode icon during transition")]
        private bool m_showModeIcon = true;

        [SerializeField]
        [Tooltip("Icon Scale Animation / 图标缩放动画 - Animate icon scale during transition")]
        private bool m_animateIconScale = true;

        [Header("音频设置")]
        [SerializeField]
        [Tooltip("Audio Source / 音频源 - Audio source for transition sounds")]
        private AudioSource m_audioSource;

        [SerializeField]
        [Tooltip("Transition Sound / 过渡音效 - Sound played during mode transitions")]
        private AudioClip m_transitionSound;

        [SerializeField]
        [Tooltip("Volume / 音量 - Volume of transition sounds")]
        [Range(0f, 1f)]
        private float m_volume = 0.5f;

        [Header("调试设置")]
        [SerializeField]
        [Tooltip("Debug Mode / 调试模式 - Enable debug logging for transition effects")]
        private bool m_debugMode = false;

        // 私有字段
        private Coroutine m_transitionCoroutine = null;
        private bool m_isTransitioning = false;
        private GameMode m_targetMode = GameMode.Local;

        // 事件
        public System.Action OnTransitionStarted;
        public System.Action OnTransitionCompleted;
        public System.Action<GameMode, GameMode> OnTransitionProgress;

        #region Unity 生命周期

        private void Awake()
        {
            // 初始化组件
            InitializeComponents();
        }

        private void Start()
        {
            // 注册到GameModeManager
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.RegisterComponent(this);
            }
            else
            {
                Debug.LogWarning("[ModeTransitionEffect] GameModeManager实例不存在，延迟注册");
                Invoke(nameof(RegisterWithDelay), 0.5f);
            }

            // 初始隐藏过渡效果
            HideTransitionEffects();
        }

        private void OnDestroy()
        {
            // 停止过渡协程
            if (m_transitionCoroutine != null)
            {
                StopCoroutine(m_transitionCoroutine);
                m_transitionCoroutine = null;
            }

            // 从GameModeManager注销
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.UnregisterComponent(this);
            }
        }

        #endregion

        #region IGameModeComponent 实现

        public void OnGameModeChanged(GameMode newMode, GameMode previousMode)
        {
            if (previousMode == newMode)
            {
                return;
            }

            if (m_debugMode)
            {
                Debug.Log($"[ModeTransitionEffect] 模式切换: {previousMode} -> {newMode}");
            }

            // 开始过渡动画
            m_targetMode = newMode;
            StartTransitionEffect(previousMode, newMode);
        }

        public bool IsActiveInMode(GameMode mode)
        {
            // 过渡效果在所有模式下都处于活动状态
            return true;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponents()
        {
            // 如果没有指定过渡画布，尝试查找或创建
            if (m_transitionCanvas == null)
            {
                m_transitionCanvas = GetComponentInChildren<Canvas>();

                if (m_transitionCanvas == null)
                {
                    // 创建过渡画布
                    GameObject canvasObj = new GameObject("TransitionCanvas");
                    canvasObj.transform.SetParent(transform);
                    m_transitionCanvas = canvasObj.AddComponent<Canvas>();
                    m_transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    m_transitionCanvas.sortingOrder = 1000; // 确保在最上层

                    // 添加CanvasScaler
                    CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);

                    // 添加GraphicRaycaster
                    canvasObj.AddComponent<GraphicRaycaster>();
                }
            }

            // 如果没有指定淡入淡出图像，尝试查找或创建
            if (m_fadeImage == null && m_transitionCanvas != null)
            {
                m_fadeImage = m_transitionCanvas.GetComponentInChildren<Image>();

                if (m_fadeImage == null)
                {
                    // 创建淡入淡出图像
                    GameObject imageObj = new GameObject("FadeImage");
                    imageObj.transform.SetParent(m_transitionCanvas.transform, false);

                    // 设置为全屏
                    RectTransform rectTransform = imageObj.AddComponent<RectTransform>();
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;

                    // 添加Image组件
                    m_fadeImage = imageObj.AddComponent<Image>();
                    m_fadeImage.color = new Color(0, 0, 0, 0); // 透明黑色
                }
            }

            // 如果没有指定模式图标图像，尝试查找或创建
            if (m_modeIconImage == null && m_transitionCanvas != null)
            {
                m_modeIconImage = m_transitionCanvas.GetComponentsInChildren<Image>()
                    .FirstOrDefault(img => img != m_fadeImage);

                if (m_modeIconImage == null && m_showModeIcon)
                {
                    // 创建模式图标图像
                    GameObject iconObj = new GameObject("ModeIcon");
                    iconObj.transform.SetParent(m_transitionCanvas.transform, false);

                    // 设置为居中
                    RectTransform rectTransform = iconObj.AddComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.sizeDelta = new Vector2(128, 128);
                    rectTransform.anchoredPosition = Vector2.zero;

                    // 添加Image组件
                    m_modeIconImage = iconObj.AddComponent<Image>();
                    m_modeIconImage.preserveAspect = true;
                    m_modeIconImage.color = new Color(1, 1, 1, 0); // 透明白色
                }
            }

            // 如果没有指定音频源，尝试查找或创建
            if (m_audioSource == null)
            {
                m_audioSource = GetComponent<AudioSource>();

                if (m_audioSource == null && m_transitionSound != null)
                {
                    m_audioSource = gameObject.AddComponent<AudioSource>();
                    m_audioSource.playOnAwake = false;
                    m_audioSource.volume = m_volume;
                }
            }
        }

        /// <summary>
        /// 延迟注册到GameModeManager
        /// </summary>
        private void RegisterWithDelay()
        {
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.RegisterComponent(this);
            }
            else
            {
                Debug.LogError("[ModeTransitionEffect] 无法找到GameModeManager实例");
            }
        }

        /// <summary>
        /// 隐藏过渡效果
        /// </summary>
        private void HideTransitionEffects()
        {
            if (m_fadeImage != null)
            {
                m_fadeImage.color = new Color(0, 0, 0, 0);
            }

            if (m_modeIconImage != null)
            {
                m_modeIconImage.color = new Color(1, 1, 1, 0);
            }

            if (m_transitionCanvas != null)
            {
                m_transitionCanvas.enabled = false;
            }
        }

        /// <summary>
        /// 开始过渡效果
        /// </summary>
        private void StartTransitionEffect(GameMode previousMode, GameMode newMode)
        {
            // 如果已经在过渡中，停止当前过渡
            if (m_transitionCoroutine != null)
            {
                StopCoroutine(m_transitionCoroutine);
            }

            // 开始新的过渡
            m_transitionCoroutine = StartCoroutine(TransitionEffectCoroutine(previousMode, newMode));
        }

        /// <summary>
        /// 过渡效果协程
        /// </summary>
        private IEnumerator TransitionEffectCoroutine(GameMode previousMode, GameMode newMode)
        {
            m_isTransitioning = true;

            // 通知过渡开始
            OnTransitionStarted?.Invoke();

            // 启用过渡画布
            if (m_transitionCanvas != null)
            {
                m_transitionCanvas.enabled = true;
            }

            // 设置模式图标
            if (m_showModeIcon && m_modeIconImage != null)
            {
                int iconIndex = (int)newMode;
                if (iconIndex >= 0 && iconIndex < m_modeIcons.Length)
                {
                    m_modeIconImage.sprite = m_modeIcons[iconIndex];
                }
            }

            // 播放过渡音效
            if (m_audioSource != null && m_transitionSound != null)
            {
                m_audioSource.PlayOneShot(m_transitionSound, m_volume);
            }

            // 淡入阶段
            float elapsedTime = 0;
            float halfDuration = m_transitionDuration * 0.5f;

            while (elapsedTime < halfDuration)
            {
                float normalizedTime = elapsedTime / halfDuration;
                float curveValue = m_transitionCurve.Evaluate(normalizedTime);

                // 更新淡入淡出图像
                if (m_fadeImage != null)
                {
                    m_fadeImage.color = new Color(0, 0, 0, curveValue);
                }

                // 更新模式图标
                if (m_showModeIcon && m_modeIconImage != null)
                {
                    m_modeIconImage.color = new Color(1, 1, 1, curveValue);

                    if (m_animateIconScale)
                    {
                        m_modeIconImage.transform.localScale = Vector3.one * (0.5f + curveValue * 0.5f);
                    }
                }

                // 通知过渡进度
                OnTransitionProgress?.Invoke(previousMode, newMode);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 淡出阶段
            elapsedTime = 0;

            while (elapsedTime < halfDuration)
            {
                float normalizedTime = elapsedTime / halfDuration;
                float curveValue = m_transitionCurve.Evaluate(1 - normalizedTime);

                // 更新淡入淡出图像
                if (m_fadeImage != null)
                {
                    m_fadeImage.color = new Color(0, 0, 0, curveValue);
                }

                // 更新模式图标
                if (m_showModeIcon && m_modeIconImage != null)
                {
                    m_modeIconImage.color = new Color(1, 1, 1, curveValue);

                    if (m_animateIconScale)
                    {
                        m_modeIconImage.transform.localScale = Vector3.one * (0.5f + curveValue * 0.5f);
                    }
                }

                // 通知过渡进度
                OnTransitionProgress?.Invoke(previousMode, newMode);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 隐藏过渡效果
            HideTransitionEffects();

            m_isTransitioning = false;
            m_transitionCoroutine = null;

            // 通知过渡完成
            OnTransitionCompleted?.Invoke();
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 是否正在过渡中
        /// </summary>
        public bool IsTransitioning => m_isTransitioning;

        /// <summary>
        /// 获取目标模式
        /// </summary>
        public GameMode TargetMode => m_targetMode;

        /// <summary>
        /// 设置过渡时长
        /// </summary>
        public void SetTransitionDuration(float duration)
        {
            if (duration < 0.1f)
            {
                duration = 0.1f;
            }

            m_transitionDuration = duration;
        }

        /// <summary>
        /// 设置过渡音量
        /// </summary>
        public void SetTransitionVolume(float volume)
        {
            m_volume = Mathf.Clamp01(volume);

            if (m_audioSource != null)
            {
                m_audioSource.volume = m_volume;
            }
        }

        /// <summary>
        /// 手动触发过渡效果
        /// </summary>
        public void TriggerTransition(GameMode fromMode, GameMode toMode)
        {
            if (m_isTransitioning)
            {
                return;
            }

            StartTransitionEffect(fromMode, toMode);
        }

        #endregion
    }
}