using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using PongHub.Core.Audio;

namespace PongHub.UI.ModeSelection
{
    /// <summary>
    /// 模式切换过渡效果控制器
    /// 提供流畅的面板切换动画和视觉反馈
    /// </summary>
    public class ModeTransitionEffect : MonoBehaviour
    {
        [Header("过渡配置")]
        [SerializeField] private float m_transitionDuration = 0.5f;
        [SerializeField] private float m_fadeInDuration = 0.3f;
        [SerializeField] private float m_fadeOutDuration = 0.3f;
        [SerializeField] private AnimationCurve m_transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve m_scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("遮罩效果")]
        [SerializeField] private GameObject m_transitionOverlay;
        [SerializeField] private Image m_overlayImage;
        [SerializeField] private Color m_overlayColor = Color.black;
        [SerializeField] private float m_overlayMaxAlpha = 0.7f;

        [Header("模式卡片动画")]
        [SerializeField] private float m_cardAnimationDelay = 0.1f;
        [SerializeField] private float m_cardScaleAmount = 1.1f;
        [SerializeField] private float m_cardRotationAmount = 5f;
        [SerializeField] private Vector3 m_cardSlideOffset = new Vector3(0, -50, 0);

        [Header("文本动画")]
        [SerializeField] private float m_textTypewriterSpeed = 30f;
        [SerializeField] private float m_textGlowDuration = 0.5f;
        [SerializeField] private Color m_textHighlightColor = Color.yellow;

        [Header("音效配置")]
        [SerializeField] private AudioClip m_transitionInSound;
        [SerializeField] private AudioClip m_transitionOutSound;
        [SerializeField] private AudioClip m_cardSelectSound;
        [SerializeField] private AudioClip m_cardHoverSound;

        // 过渡状态
        public enum TransitionState
        {
            Idle,
            FadingOut,
            Switching,
            FadingIn
        }

        // 过渡类型
        public enum TransitionType
        {
            Slide,          // 滑动过渡
            Fade,           // 淡入淡出
            Scale,          // 缩放过渡
            Rotate,         // 旋转过渡
            Morph,          // 形变过渡
            Particle        // 粒子过渡
        }

        // 事件定义
        public System.Action OnTransitionStarted;
        public System.Action OnTransitionCompleted;
        public System.Action<float> OnTransitionProgress;

        private TransitionState m_currentState = TransitionState.Idle;
        private TransitionType m_currentTransitionType = TransitionType.Fade;
        private Coroutine m_currentTransition;

        private List<RectTransform> m_activeCards = new List<RectTransform>();
        private List<TextMeshProUGUI> m_activeTexts = new List<TextMeshProUGUI>();
        private Camera m_vrCamera;

        /// <summary>
        /// 初始化过渡效果控制器
        /// </summary>
        private void Start()
        {
            InitializeComponents();
            SetupOverlay();
        }

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            m_vrCamera = Camera.main;

            // 如果没有遮罩，创建一个
            if (m_transitionOverlay == null)
            {
                CreateTransitionOverlay();
            }
        }

        /// <summary>
        /// 设置过渡遮罩
        /// </summary>
        private void SetupOverlay()
        {
            if (m_overlayImage != null)
            {
                m_overlayImage.color = new Color(m_overlayColor.r, m_overlayColor.g, m_overlayColor.b, 0);
                m_overlayImage.raycastTarget = false;
            }

            if (m_transitionOverlay != null)
            {
                m_transitionOverlay.SetActive(false);
            }
        }

        /// <summary>
        /// 创建过渡遮罩
        /// </summary>
        private void CreateTransitionOverlay()
        {
            // 创建遮罩GameObject
            m_transitionOverlay = new GameObject("TransitionOverlay");
            m_transitionOverlay.transform.SetParent(transform, false);

            // 添加RectTransform组件
            RectTransform rectTransform = m_transitionOverlay.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // 添加Image组件
            m_overlayImage = m_transitionOverlay.AddComponent<Image>();
            m_overlayImage.color = new Color(m_overlayColor.r, m_overlayColor.g, m_overlayColor.b, 0);
            m_overlayImage.raycastTarget = false;

            // 确保遮罩在最前面
            m_transitionOverlay.transform.SetAsLastSibling();
            m_transitionOverlay.SetActive(false);
        }

        /// <summary>
        /// 开始模式切换过渡
        /// </summary>
        public void StartTransition(GameObject fromPanel, GameObject toPanel, TransitionType transitionType = TransitionType.Fade)
        {
            if (m_currentState != TransitionState.Idle)
            {
                Debug.LogWarning("Transition already in progress");
                return;
            }

            m_currentTransitionType = transitionType;

            if (m_currentTransition != null)
            {
                StopCoroutine(m_currentTransition);
            }

            m_currentTransition = StartCoroutine(TransitionCoroutine(fromPanel, toPanel));
        }

        /// <summary>
        /// 过渡协程
        /// </summary>
        private IEnumerator TransitionCoroutine(GameObject fromPanel, GameObject toPanel)
        {
            m_currentState = TransitionState.FadingOut;
            OnTransitionStarted?.Invoke();

            // 播放过渡开始音效
            PlayTransitionSound(m_transitionInSound);

            // 1. 淡出当前面板
            yield return StartCoroutine(FadeOutPanel(fromPanel));

            m_currentState = TransitionState.Switching;

            // 2. 切换面板
            if (fromPanel != null)
            {
                fromPanel.SetActive(false);
            }

            if (toPanel != null)
            {
                toPanel.SetActive(true);
                PrepareNewPanel(toPanel);
            }

            m_currentState = TransitionState.FadingIn;

            // 3. 淡入新面板
            yield return StartCoroutine(FadeInPanel(toPanel));

            // 播放过渡结束音效
            PlayTransitionSound(m_transitionOutSound);

            m_currentState = TransitionState.Idle;
            OnTransitionCompleted?.Invoke();
        }

        /// <summary>
        /// 淡出面板
        /// </summary>
        private IEnumerator FadeOutPanel(GameObject panel)
        {
            if (panel == null) yield break;

            // 启用遮罩
            if (m_transitionOverlay != null)
            {
                m_transitionOverlay.SetActive(true);
            }

            float elapsedTime = 0f;
            Vector3 originalScale = panel.transform.localScale;

            while (elapsedTime < m_fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / m_fadeOutDuration;
                float curveValue = m_transitionCurve.Evaluate(progress);

                // 更新遮罩透明度
                if (m_overlayImage != null)
                {
                    float alpha = curveValue * m_overlayMaxAlpha;
                    m_overlayImage.color = new Color(m_overlayColor.r, m_overlayColor.g, m_overlayColor.b, alpha);
                }

                // 根据过渡类型执行不同的动画
                switch (m_currentTransitionType)
                {
                    case TransitionType.Scale:
                        panel.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, curveValue);
                        break;
                    case TransitionType.Slide:
                        panel.transform.localPosition = Vector3.Lerp(Vector3.zero, m_cardSlideOffset, curveValue);
                        break;
                    case TransitionType.Rotate:
                        panel.transform.localRotation = Quaternion.Lerp(Quaternion.identity,
                            Quaternion.Euler(0, 0, m_cardRotationAmount), curveValue);
                        break;
                }

                OnTransitionProgress?.Invoke(progress * 0.5f); // 前半段进度
                yield return null;
            }

            // 确保最终状态
            if (m_overlayImage != null)
            {
                m_overlayImage.color = new Color(m_overlayColor.r, m_overlayColor.g, m_overlayColor.b, m_overlayMaxAlpha);
            }
        }

        /// <summary>
        /// 淡入面板
        /// </summary>
        private IEnumerator FadeInPanel(GameObject panel)
        {
            if (panel == null) yield break;

            float elapsedTime = 0f;
            Vector3 targetScale = panel.transform.localScale;

            // 设置初始状态
            switch (m_currentTransitionType)
            {
                case TransitionType.Scale:
                    panel.transform.localScale = Vector3.zero;
                    break;
                case TransitionType.Slide:
                    panel.transform.localPosition = -m_cardSlideOffset;
                    break;
                case TransitionType.Rotate:
                    panel.transform.localRotation = Quaternion.Euler(0, 0, -m_cardRotationAmount);
                    break;
            }

            while (elapsedTime < m_fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / m_fadeInDuration;
                float curveValue = m_transitionCurve.Evaluate(progress);

                // 更新遮罩透明度
                if (m_overlayImage != null)
                {
                    float alpha = m_overlayMaxAlpha * (1f - curveValue);
                    m_overlayImage.color = new Color(m_overlayColor.r, m_overlayColor.g, m_overlayColor.b, alpha);
                }

                // 根据过渡类型执行不同的动画
                switch (m_currentTransitionType)
                {
                    case TransitionType.Scale:
                        panel.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, curveValue);
                        break;
                    case TransitionType.Slide:
                        panel.transform.localPosition = Vector3.Lerp(-m_cardSlideOffset, Vector3.zero, curveValue);
                        break;
                    case TransitionType.Rotate:
                        panel.transform.localRotation = Quaternion.Lerp(
                            Quaternion.Euler(0, 0, -m_cardRotationAmount), Quaternion.identity, curveValue);
                        break;
                }

                OnTransitionProgress?.Invoke(0.5f + progress * 0.5f); // 后半段进度
                yield return null;
            }

            // 确保最终状态
            panel.transform.localScale = targetScale;
            panel.transform.localPosition = Vector3.zero;
            panel.transform.localRotation = Quaternion.identity;

            if (m_overlayImage != null)
            {
                m_overlayImage.color = new Color(m_overlayColor.r, m_overlayColor.g, m_overlayColor.b, 0);
            }

            // 禁用遮罩
            if (m_transitionOverlay != null)
            {
                m_transitionOverlay.SetActive(false);
            }
        }

        /// <summary>
        /// 准备新面板
        /// </summary>
        private void PrepareNewPanel(GameObject panel)
        {
            // 收集面板中的卡片和文本
            CollectPanelElements(panel);

            // 为卡片添加入场动画
            StartCoroutine(AnimateCards());

            // 为文本添加打字机效果
            StartCoroutine(AnimateTexts());
        }

        /// <summary>
        /// 收集面板元素
        /// </summary>
        private void CollectPanelElements(GameObject panel)
        {
            m_activeCards.Clear();
            m_activeTexts.Clear();

            // 收集所有卡片（带有ModeCard组件的对象）
            ModeCard[] cards = panel.GetComponentsInChildren<ModeCard>();
            foreach (var card in cards)
            {
                if (card.GetComponent<RectTransform>())
                {
                    m_activeCards.Add(card.GetComponent<RectTransform>());
                }
            }

            // 收集所有文本组件
            TextMeshProUGUI[] texts = panel.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                if (text.gameObject.activeInHierarchy)
                {
                    m_activeTexts.Add(text);
                }
            }
        }

        /// <summary>
        /// 卡片动画协程
        /// </summary>
        private IEnumerator AnimateCards()
        {
            for (int i = 0; i < m_activeCards.Count; i++)
            {
                var card = m_activeCards[i];
                if (card != null)
                {
                    StartCoroutine(AnimateCard(card, i * m_cardAnimationDelay));
                }
            }
            yield return null;
        }

        /// <summary>
        /// 单个卡片动画
        /// </summary>
        private IEnumerator AnimateCard(RectTransform card, float delay)
        {
            yield return new WaitForSeconds(delay);

            Vector3 originalScale = card.localScale;
            Vector3 originalPosition = card.localPosition;

            // 设置初始状态
            card.localScale = Vector3.zero;
            card.localPosition = originalPosition + m_cardSlideOffset;

            float elapsedTime = 0f;
            while (elapsedTime < m_transitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / m_transitionDuration;
                float curveValue = m_scaleCurve.Evaluate(progress);

                // 弹性缩放动画
                float scaleMultiplier = progress < 0.5f ?
                    Mathf.Lerp(0, m_cardScaleAmount, curveValue) :
                    Mathf.Lerp(m_cardScaleAmount, 1f, curveValue);

                card.localScale = originalScale * scaleMultiplier;
                card.localPosition = Vector3.Lerp(originalPosition + m_cardSlideOffset, originalPosition, curveValue);

                yield return null;
            }

            // 确保最终状态
            card.localScale = originalScale;
            card.localPosition = originalPosition;
        }

        /// <summary>
        /// 文本动画协程
        /// </summary>
        private IEnumerator AnimateTexts()
        {
            foreach (var text in m_activeTexts)
            {
                if (text != null)
                {
                    StartCoroutine(TypewriterEffect(text));
                }
            }
            yield return null;
        }

        /// <summary>
        /// 打字机效果
        /// </summary>
        private IEnumerator TypewriterEffect(TextMeshProUGUI text)
        {
            if (text == null) yield break;

            string fullText = text.text;
            text.text = "";

            for (int i = 0; i <= fullText.Length; i++)
            {
                text.text = fullText.Substring(0, i);
                yield return new WaitForSeconds(1f / m_textTypewriterSpeed);
            }

            // 添加发光效果
            StartCoroutine(TextGlowEffect(text));
        }

        /// <summary>
        /// 文本发光效果
        /// </summary>
        private IEnumerator TextGlowEffect(TextMeshProUGUI text)
        {
            if (text == null) yield break;

            Color originalColor = text.color;
            float elapsedTime = 0f;

            while (elapsedTime < m_textGlowDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / m_textGlowDuration;
                float glowIntensity = Mathf.Sin(progress * Mathf.PI);

                text.color = Color.Lerp(originalColor, m_textHighlightColor, glowIntensity * 0.5f);
                yield return null;
            }

            text.color = originalColor;
        }

        /// <summary>
        /// 卡片悬停效果
        /// </summary>
        public void OnCardHover(RectTransform card)
        {
            if (card == null) return;

            PlayTransitionSound(m_cardHoverSound);

            // 使用DOTween实现悬停效果
            card.DOScale(1.1f, 0.2f).SetEase(Ease.OutBack);
            card.DORotate(new Vector3(0, 0, 2f), 0.2f).SetEase(Ease.OutBack);
        }

        /// <summary>
        /// 卡片离开效果
        /// </summary>
        public void OnCardExit(RectTransform card)
        {
            if (card == null) return;

            // 使用DOTween实现离开效果
            card.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
            card.DORotate(Vector3.zero, 0.2f).SetEase(Ease.OutBack);
        }

        /// <summary>
        /// 卡片选择效果
        /// </summary>
        public void OnCardSelect(RectTransform card)
        {
            if (card == null) return;

            PlayTransitionSound(m_cardSelectSound);

            // 使用DOTween实现选择效果
            var sequence = DOTween.Sequence();
            sequence.Append(card.DOScale(1.2f, 0.1f));
            sequence.Append(card.DOScale(1f, 0.1f));
            sequence.Join(card.DORotate(new Vector3(0, 0, 360f), 0.3f, RotateMode.FastBeyond360));
        }

        /// <summary>
        /// 播放过渡音效
        /// </summary>
        private void PlayTransitionSound(AudioClip clip)
        {
            if (AudioManager.Instance != null && clip != null)
            {
                AudioManager.Instance.PlaySound(clip);
            }
        }

        /// <summary>
        /// 强制停止当前过渡
        /// </summary>
        public void StopTransition()
        {
            if (m_currentTransition != null)
            {
                StopCoroutine(m_currentTransition);
                m_currentTransition = null;
            }

            m_currentState = TransitionState.Idle;

            if (m_transitionOverlay != null)
            {
                m_transitionOverlay.SetActive(false);
            }
        }

        /// <summary>
        /// 检查是否正在过渡
        /// </summary>
        public bool IsTransitioning()
        {
            return m_currentState != TransitionState.Idle;
        }

        /// <summary>
        /// 获取当前过渡状态
        /// </summary>
        public TransitionState GetCurrentState()
        {
            return m_currentState;
        }

        /// <summary>
        /// 设置过渡参数
        /// </summary>
        public void SetTransitionParameters(float duration, float fadeInDuration, float fadeOutDuration)
        {
            m_transitionDuration = duration;
            m_fadeInDuration = fadeInDuration;
            m_fadeOutDuration = fadeOutDuration;
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void OnDestroy()
        {
            StopTransition();
            DOTween.KillAll();
        }

        /// <summary>
        /// 触发过渡效果（兼容接口）
        /// </summary>
        public void TriggerTransition(GameObject fromPanel, GameObject toPanel, TransitionType transitionType = TransitionType.Fade)
        {
            // 调用主要的过渡方法
            StartTransition(fromPanel, toPanel, transitionType);
        }

        /// <summary>
        /// 触发过渡效果（重载方法）
        /// </summary>
        public void TriggerTransition(GameObject fromPanel, GameObject toPanel)
        {
            // 使用默认的过渡类型
            StartTransition(fromPanel, toPanel, TransitionType.Fade);
        }

        /// <summary>
        /// 触发过渡效果（仅指定目标面板）
        /// </summary>
        public void TriggerTransition(GameObject toPanel)
        {
            // 查找当前活动的面板
            GameObject activePanel = FindActivePanel();
            StartTransition(activePanel, toPanel, TransitionType.Fade);
        }

        /// <summary>
        /// 查找当前活动的面板
        /// </summary>
        private GameObject FindActivePanel()
        {
            // 在父物体中查找当前活动的面板
            Transform parent = transform.parent;
            if (parent != null)
            {
                foreach (Transform child in parent)
                {
                    if (child.gameObject.activeInHierarchy && child.gameObject != gameObject)
                    {
                        return child.gameObject;
                    }
                }
            }
            return null;
        }
    }
}