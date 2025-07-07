using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using PongHub.UI.Core;
using PongHub.UI.Localization;

namespace PongHub.UI.ModeSelection
{
    /// <summary>
    /// 模式卡片组件
    /// 显示游戏模式信息的UI卡片
    /// </summary>
    public class ModeCard : VRUIComponent
    {
        [Header("UI引用")]
        [SerializeField] private Image m_backgroundImage;
        [SerializeField] private Image m_iconImage;
        [SerializeField] private TextMeshProUGUI m_titleText;
        [SerializeField] private TextMeshProUGUI m_descriptionText;
        [SerializeField] private TextMeshProUGUI m_playersText;
        [SerializeField] private TextMeshProUGUI m_statusText;

        [Header("统计信息UI")]
        [SerializeField] private GameObject m_statsPanel;
        [SerializeField] private TextMeshProUGUI m_timesPlayedText;
        [SerializeField] private TextMeshProUGUI m_averageScoreText;
        [SerializeField] private TextMeshProUGUI m_lastPlayedText;

        [Header("状态指示器")]
        [SerializeField] private GameObject m_recommendedBadge;
        [SerializeField] private GameObject m_unavailableMask;
        [SerializeField] private GameObject m_networkRequiredIcon;
        [SerializeField] private GameObject m_aiRequiredIcon;

        [Header("视觉效果")]
        [SerializeField] private Image m_glowEffect;
        [SerializeField] private ParticleSystem m_selectEffect;
        [SerializeField] private CanvasGroup m_canvasGroup;

        [Header("动画配置")]
        [SerializeField] private float m_hoverAnimDuration = 0.2f;
        [SerializeField] private float m_selectAnimDuration = 0.1f;
        [SerializeField] private AnimationCurve m_animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("事件")]
        public UnityEvent<GameModeInfo> OnModeSelected;
        public UnityEvent<GameModeInfo> OnModeHovered;
        public UnityEvent<GameModeInfo> OnModeUnhovered;

        // 私有变量
        private GameModeInfo m_modeInfo;
        private ModeSelectionConfig m_config;
        private RectTransform m_rectTransform;
        private Vector3 m_originalScale;
        private Color m_originalBackgroundColor;
        private bool m_isSelected = false;
        private new bool m_isHovered = false;
        private Coroutine m_animationCoroutine;

        // 本地化管理器
        private LocalizationManager m_localizationManager;

        #region 属性

        public GameModeInfo ModeInfo => m_modeInfo;
        public bool IsSelected
        {
            get => m_isSelected;
            set => SetSelected(value);
        }
        public bool IsHovered => m_isHovered;

        #endregion

        #region Unity生命周期

        protected override void Awake()
        {
            base.Awake();

            m_rectTransform = GetComponent<RectTransform>();
            m_originalScale = transform.localScale;

            if (m_backgroundImage != null)
                m_originalBackgroundColor = m_backgroundImage.color;

            // 查找本地化管理器
            m_localizationManager = FindObjectOfType<LocalizationManager>();
        }

        private void Start()
        {
            // 设置初始状态
            if (m_canvasGroup != null)
                m_canvasGroup.alpha = 1f;

            UpdateVisualState(InteractionState.Normal);
        }

        private void OnDestroy()
        {
            if (m_animationCoroutine != null)
            {
                StopCoroutine(m_animationCoroutine);
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化模式卡片
        /// </summary>
        /// <param name="modeInfo">模式信息</param>
        /// <param name="config">配置</param>
        public void Initialize(GameModeInfo modeInfo, ModeSelectionConfig config)
        {
            m_modeInfo = modeInfo;
            m_config = config;

            RefreshContent();
        }

        /// <summary>
        /// 刷新卡片内容
        /// </summary>
        public void RefreshContent()
        {
            if (m_modeInfo == null)
                return;

            UpdateTexts();
            UpdateVisuals();
            UpdateStats();
            UpdateAvailability();
        }

        /// <summary>
        /// 设置选中状态
        /// </summary>
        /// <param name="selected">是否选中</param>
        public void SetSelected(bool selected)
        {
            if (m_isSelected == selected)
                return;

            m_isSelected = selected;
            UpdateVisualState(selected ? InteractionState.Selected : InteractionState.Normal);

            if (selected && m_selectEffect != null)
            {
                m_selectEffect.Play();
            }
        }

        /// <summary>
        /// 播放悬停动画
        /// </summary>
        public void PlayHoverAnimation()
        {
            if (m_animationCoroutine != null)
                StopCoroutine(m_animationCoroutine);

            m_animationCoroutine = StartCoroutine(AnimateScale(m_config.CardHoverScale, m_hoverAnimDuration));
        }

        /// <summary>
        /// 播放取消悬停动画
        /// </summary>
        public void PlayUnhoverAnimation()
        {
            if (m_animationCoroutine != null)
                StopCoroutine(m_animationCoroutine);

            m_animationCoroutine = StartCoroutine(AnimateScale(1f, m_hoverAnimDuration));
        }

        /// <summary>
        /// 播放点击动画
        /// </summary>
        public void PlayClickAnimation()
        {
            if (m_animationCoroutine != null)
                StopCoroutine(m_animationCoroutine);

            m_animationCoroutine = StartCoroutine(AnimateClickEffect());
        }

        /// <summary>
        /// 设置卡片大小
        /// </summary>
        /// <param name="size">卡片大小</param>
        public void SetSize(Vector2 size)
        {
            if (m_rectTransform != null)
            {
                m_rectTransform.sizeDelta = size;
            }
        }

        /// <summary>
        /// 设置卡片透明度
        /// </summary>
        /// <param name="alpha">透明度</param>
        public void SetAlpha(float alpha)
        {
            if (m_canvasGroup != null)
            {
                m_canvasGroup.alpha = alpha;
            }
        }

        #endregion

        #region VRUIComponent重写方法

        public override void SetInteractable(bool interactable)
        {
            m_interactable = interactable;

            if (m_canvasGroup != null)
            {
                m_canvasGroup.interactable = interactable;
                m_canvasGroup.blocksRaycasts = interactable;
            }

            UpdateVisualState(interactable ? InteractionState.Normal : InteractionState.Disabled);
        }

        public override void UpdateVisualState(InteractionState state)
        {
            m_currentState = state;

            if (m_modeInfo == null || m_config == null)
                return;

            Color targetColor = m_originalBackgroundColor;
            float glowIntensity = 0f;

            switch (state)
            {
                case InteractionState.Normal:
                    targetColor = m_isSelected ? m_config.SelectedModeColor : m_originalBackgroundColor;
                    glowIntensity = m_isSelected ? m_config.GlowIntensity : 0f;
                    break;

                case InteractionState.Highlighted:
                    targetColor = Color.Lerp(m_originalBackgroundColor, m_config.SelectedModeColor, 0.5f);
                    glowIntensity = m_config.GlowIntensity * 0.7f;
                    break;

                case InteractionState.Selected:
                    targetColor = m_config.SelectedModeColor;
                    glowIntensity = m_config.GlowIntensity;
                    break;

                case InteractionState.Disabled:
                    targetColor = m_config.UnavailableModeColor;
                    glowIntensity = 0f;
                    break;
            }

            // 如果是推荐模式，使用推荐颜色
            if (m_modeInfo.IsRecommended && state != InteractionState.Disabled)
            {
                targetColor = Color.Lerp(targetColor, m_config.RecommendedModeColor, 0.3f);
            }

            // 应用颜色
            if (m_backgroundImage != null)
            {
                m_backgroundImage.color = targetColor;
            }

            // 应用发光效果
            if (m_glowEffect != null)
            {
                Color glowColor = targetColor;
                glowColor.a = glowIntensity;
                m_glowEffect.color = glowColor;
            }
        }

        public override void OnPointerEnter()
        {
            base.OnPointerEnter();

            if (!m_interactable || m_modeInfo == null || !m_modeInfo.CheckAvailability())
                return;

            m_isHovered = true;
            UpdateVisualState(InteractionState.Highlighted);
            PlayHoverAnimation();

            // 触发悬停事件
            OnModeHovered?.Invoke(m_modeInfo);
        }

        public override void OnPointerExit()
        {
            base.OnPointerExit();

            m_isHovered = false;
            UpdateVisualState(m_isSelected ? InteractionState.Selected : InteractionState.Normal);
            PlayUnhoverAnimation();

            // 触发取消悬停事件
            OnModeUnhovered?.Invoke(m_modeInfo);
        }

        public override void OnPointerClick()
        {
            base.OnPointerClick();

            if (!m_interactable || m_modeInfo == null)
                return;

            // 检查模式可用性
            if (!m_modeInfo.CheckAvailability())
            {
                // 播放不可用音效
                PlayUnavailableSound();
                return;
            }

            PlayClickAnimation();

            // 触发选择事件
            OnModeSelected?.Invoke(m_modeInfo);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新文本内容
        /// </summary>
        private void UpdateTexts()
        {
            // 更新标题
            if (m_titleText != null)
            {
                string title = GetLocalizedText(m_modeInfo.TitleKey);
                m_titleText.text = title;
            }

            // 更新描述
            if (m_descriptionText != null)
            {
                string description = GetLocalizedText(m_modeInfo.DescriptionKey);
                m_descriptionText.text = description;
            }

            // 更新玩家数量信息
            if (m_playersText != null)
            {
                if (m_modeInfo.MinPlayers == m_modeInfo.MaxPlayers)
                {
                    m_playersText.text = $"{m_modeInfo.MinPlayers} {GetLocalizedText("ui.players")}";
                }
                else
                {
                    m_playersText.text = $"{m_modeInfo.MinPlayers}-{m_modeInfo.MaxPlayers} {GetLocalizedText("ui.players")}";
                }
            }

            // 更新状态文本
            UpdateStatusText();
        }

        /// <summary>
        /// 更新状态文本
        /// </summary>
        private void UpdateStatusText()
        {
            if (m_statusText == null)
                return;

            if (!m_modeInfo.CheckAvailability())
            {
                if (!string.IsNullOrEmpty(m_modeInfo.AvailabilityConditionKey))
                {
                    m_statusText.text = GetLocalizedText(m_modeInfo.AvailabilityConditionKey);
                }
                else
                {
                    m_statusText.text = GetLocalizedText("ui.unavailable");
                }
                m_statusText.color = Color.red;
            }
            else if (m_modeInfo.IsRecommended)
            {
                m_statusText.text = GetLocalizedText("ui.recommended");
                m_statusText.color = m_config.RecommendedModeColor;
            }
            else
            {
                m_statusText.text = "";
            }
        }

        /// <summary>
        /// 更新视觉元素
        /// </summary>
        private void UpdateVisuals()
        {
            // 更新图标
            if (m_iconImage != null && m_modeInfo.Icon != null)
            {
                m_iconImage.sprite = m_modeInfo.Icon;
                m_iconImage.color = m_modeInfo.ThemeColor;
            }

            // 更新徽章显示
            if (m_recommendedBadge != null)
            {
                m_recommendedBadge.SetActive(m_modeInfo.IsRecommended);
            }

            // 更新要求图标
            if (m_networkRequiredIcon != null)
            {
                m_networkRequiredIcon.SetActive(m_modeInfo.RequiresNetwork);
            }

            if (m_aiRequiredIcon != null)
            {
                m_aiRequiredIcon.SetActive(m_modeInfo.RequiresAI);
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStats()
        {
            if (m_statsPanel == null)
                return;

            bool hasStats = m_modeInfo.TimesPlayed > 0;
            m_statsPanel.SetActive(hasStats);

            if (!hasStats)
                return;

            // 更新游戏次数
            if (m_timesPlayedText != null)
            {
                m_timesPlayedText.text = $"{GetLocalizedText("ui.times_played")}: {m_modeInfo.TimesPlayed}";
            }

            // 更新平均分数
            if (m_averageScoreText != null)
            {
                m_averageScoreText.text = $"{GetLocalizedText("ui.average_score")}: {m_modeInfo.AverageScore:F1}";
            }

            // 更新最后游戏时间
            if (m_lastPlayedText != null)
            {
                if (m_modeInfo.LastPlayedTime != DateTime.MinValue)
                {
                    string timeAgo = GetTimeAgoString(m_modeInfo.LastPlayedTime);
                    m_lastPlayedText.text = $"{GetLocalizedText("ui.last_played")}: {timeAgo}";
                }
                else
                {
                    m_lastPlayedText.text = "";
                }
            }
        }

        /// <summary>
        /// 更新可用性状态
        /// </summary>
        private void UpdateAvailability()
        {
            bool isAvailable = m_modeInfo.CheckAvailability();

            // 更新不可用遮罩
            if (m_unavailableMask != null)
            {
                m_unavailableMask.SetActive(!isAvailable);
            }

            // 更新交互性
            SetInteractable(isAvailable);
        }

        /// <summary>
        /// 缩放动画协程
        /// </summary>
        /// <param name="targetScale">目标缩放</param>
        /// <param name="duration">动画时长</param>
        private System.Collections.IEnumerator AnimateScale(float targetScale, float duration)
        {
            Vector3 startScale = transform.localScale;
            Vector3 endScale = m_originalScale * targetScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curve = m_animationCurve.Evaluate(t);

                transform.localScale = Vector3.Lerp(startScale, endScale, curve);
                yield return null;
            }

            transform.localScale = endScale;
            m_animationCoroutine = null;
        }

        /// <summary>
        /// 点击效果动画协程
        /// </summary>
        private System.Collections.IEnumerator AnimateClickEffect()
        {
            Vector3 startScale = transform.localScale;
            Vector3 compressScale = m_originalScale * m_config.CardSelectScale;
            Vector3 endScale = m_originalScale * (m_isHovered ? m_config.CardHoverScale : 1f);

            // 压缩阶段
            float elapsed = 0f;
            float compressDuration = m_selectAnimDuration * 0.3f;

            while (elapsed < compressDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / compressDuration;
                transform.localScale = Vector3.Lerp(startScale, compressScale, t);
                yield return null;
            }

            // 恢复阶段
            elapsed = 0f;
            float expandDuration = m_selectAnimDuration * 0.7f;

            while (elapsed < expandDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / expandDuration;
                transform.localScale = Vector3.Lerp(compressScale, endScale, m_animationCurve.Evaluate(t));
                yield return null;
            }

            transform.localScale = endScale;
            m_animationCoroutine = null;
        }

        /// <summary>
        /// 获取本地化文本
        /// </summary>
        /// <param name="key">本地化键</param>
        /// <returns>本地化文本</returns>
        private string GetLocalizedText(string key)
        {
            if (m_localizationManager != null)
            {
                return m_localizationManager.GetLocalizedText(key);
            }
            return key; // 回退到键本身
        }

        /// <summary>
        /// 获取时间间隔描述
        /// </summary>
        /// <param name="dateTime">时间</param>
        /// <returns>时间间隔描述</returns>
        private string GetTimeAgoString(DateTime dateTime)
        {
            TimeSpan timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalDays >= 365)
            {
                int years = (int)(timeSpan.TotalDays / 365);
                return $"{years} {GetLocalizedText("ui.years_ago")}";
            }
            else if (timeSpan.TotalDays >= 30)
            {
                int months = (int)(timeSpan.TotalDays / 30);
                return $"{months} {GetLocalizedText("ui.months_ago")}";
            }
            else if (timeSpan.TotalDays >= 1)
            {
                int days = (int)timeSpan.TotalDays;
                return $"{days} {GetLocalizedText("ui.days_ago")}";
            }
            else if (timeSpan.TotalHours >= 1)
            {
                int hours = (int)timeSpan.TotalHours;
                return $"{hours} {GetLocalizedText("ui.hours_ago")}";
            }
            else
            {
                return GetLocalizedText("ui.recently");
            }
        }

        /// <summary>
        /// 播放不可用音效
        /// </summary>
        private void PlayUnavailableSound()
        {
            if (m_config != null && m_config.ModeUnavailableSound != null)
            {
                AudioSource.PlayClipAtPoint(m_config.ModeUnavailableSound, transform.position);
            }
        }

        #endregion
    }
}