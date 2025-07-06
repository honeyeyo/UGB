using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Events;
using PongHub.UI.Core;
using UnityEngine.EventSystems; // Added for IPointerDownHandler, IPointerUpHandler, IDragHandler

namespace PongHub.UI.Components
{
    /// <summary>
    /// VR弹出窗口组件
    /// 实现VR环境中的弹窗显示和交互功能
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class VRPopupWindow : VRUIComponent
    {
        [Header("窗口设置")]
        [SerializeField]
        [Tooltip("Title / 标题 - Window title text")]
        private string m_Title = "窗口";

        [SerializeField]
        [Tooltip("Content / 内容 - Window content")]
        private GameObject m_Content;

        [SerializeField]
        [Tooltip("Modal / 模态 - Whether the window blocks interaction with elements behind it")]
        private bool m_Modal = true;

        [SerializeField]
        [Tooltip("Close On Click Outside / 点击外部关闭 - Whether to close the window when clicking outside")]
        private bool m_CloseOnClickOutside = true;

        [SerializeField]
        [Tooltip("Draggable / 可拖动 - Whether the window can be dragged")]
        private bool m_Draggable = true;

        [SerializeField]
        [Tooltip("Resizable / 可调整大小 - Whether the window can be resized")]
        private bool m_Resizable = false;

        [SerializeField]
        [Tooltip("Min Size / 最小尺寸 - Minimum size of the window")]
        private Vector2 m_MinSize = new Vector2(200, 150);

        [SerializeField]
        [Tooltip("Max Size / 最大尺寸 - Maximum size of the window")]
        private Vector2 m_MaxSize = new Vector2(800, 600);

        [SerializeField]
        [Tooltip("Default Size / 默认尺寸 - Default size of the window")]
        private Vector2 m_DefaultSize = new Vector2(400, 300);

        [SerializeField]
        [Tooltip("Show Close Button / 显示关闭按钮 - Whether to show a close button")]
        private bool m_ShowCloseButton = true;

        [Header("视觉设置")]
        [SerializeField]
        [Tooltip("Window / 窗口 - Window container")]
        private RectTransform m_Window;

        [SerializeField]
        [Tooltip("Title Bar / 标题栏 - Title bar of the window")]
        private RectTransform m_TitleBar;

        [SerializeField]
        [Tooltip("Title Text / 标题文本 - Text component for the window title")]
        private TextMeshProUGUI m_TitleText;

        [SerializeField]
        [Tooltip("Close Button / 关闭按钮 - Button to close the window")]
        private VRButton m_CloseButton;

        [SerializeField]
        [Tooltip("Content Area / 内容区域 - Container for window content")]
        private RectTransform m_ContentArea;

        [SerializeField]
        [Tooltip("Background / 背景 - Background for the window")]
        private Image m_Background;

        [SerializeField]
        [Tooltip("Overlay / 遮罩层 - Overlay that blocks interaction with elements behind the window")]
        private Image m_Overlay;

        [SerializeField]
        [Tooltip("Title Bar Height / 标题栏高度 - Height of the title bar")]
        private float m_TitleBarHeight = 40f;

        [SerializeField]
        [Tooltip("Border Width / 边框宽度 - Width of the window border")]
        private float m_BorderWidth = 2f;

        [SerializeField]
        [Tooltip("Corner Radius / 圆角半径 - Radius of the window corners")]
        private float m_CornerRadius = 10f;

        [SerializeField]
        [Tooltip("Animation Duration / 动画时长 - Duration of the window animation")]
        private float m_AnimationDuration = 0.3f;

        [SerializeField]
        [Tooltip("Use Animation / 使用动画 - Whether to animate the window")]
        private bool m_UseAnimation = true;

        // 事件
        [Header("事件")]
        public UnityEvent OnOpen = new UnityEvent();
        public UnityEvent OnClose = new UnityEvent();
        public UnityEvent OnDragBegin = new UnityEvent();
        public UnityEvent OnDragEnd = new UnityEvent();
        public UnityEvent OnResize = new UnityEvent();

        // 内部变量
        private RectTransform m_rectTransform;
        private bool m_IsOpen = false;
        private bool m_IsDragging = false;
        private Vector2 m_DragOffset;
        private bool m_IsResizing = false;
        private Vector2 m_ResizeStartSize;
        private Vector2 m_ResizeStartPosition;
        private Coroutine m_AnimationCoroutine;
        private Canvas m_Canvas;
        private CanvasGroup m_CanvasGroup;

        #region Unity生命周期

        protected override void Awake()
        {
            base.Awake();

            // 获取RectTransform
            m_rectTransform = GetComponent<RectTransform>();

            // 初始化组件
            InitializeComponents();

            // 更新视觉状态
            UpdateVisualState(m_interactable ? InteractionState.Normal : InteractionState.Disabled);

            // 注册到UI管理器
            if (VRUIManager.Instance != null)
            {
                VRUIManager.Instance.RegisterComponent(this);
            }

            // 获取Canvas
            m_Canvas = GetComponentInParent<Canvas>();
            if (m_Canvas == null)
            {
                m_Canvas = gameObject.AddComponent<Canvas>();
                m_Canvas.overrideSorting = true;
                m_Canvas.sortingOrder = 100;
            }

            // 添加CanvasGroup
            m_CanvasGroup = GetComponent<CanvasGroup>();
            if (m_CanvasGroup == null)
            {
                m_CanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // 初始状态为关闭
            if (!m_IsOpen)
            {
                m_CanvasGroup.alpha = 0;
                m_CanvasGroup.blocksRaycasts = false;
                m_CanvasGroup.interactable = false;
                gameObject.SetActive(false);
            }
        }

        protected virtual void Start()
        {
            // 设置窗口大小
            if (m_Window != null)
            {
                m_Window.sizeDelta = m_DefaultSize;
            }

            // 设置标题
            if (m_TitleText != null)
            {
                m_TitleText.text = m_Title;
            }

            // 设置关闭按钮事件
            if (m_CloseButton != null)
            {
                m_CloseButton.OnClick.AddListener(Close);
            }

            // 初始状态为关闭
            if (!m_IsOpen)
            {
                m_CanvasGroup.alpha = 0;
                m_CanvasGroup.blocksRaycasts = false;
                m_CanvasGroup.interactable = false;
                gameObject.SetActive(false);
            }
        }

        protected virtual void OnDestroy()
        {
            // 从UI管理器注销
            if (VRUIManager.Instance != null)
            {
                VRUIManager.Instance.UnregisterComponent(this);
            }
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 打开窗口
        /// </summary>
        public void Open()
        {
            if (m_IsOpen)
                return;

            // 激活游戏对象
            gameObject.SetActive(true);

            // 设置窗口状态
            m_IsOpen = true;

            // 动画显示
            if (m_UseAnimation)
            {
                if (m_AnimationCoroutine != null)
                {
                    StopCoroutine(m_AnimationCoroutine);
                }
                m_AnimationCoroutine = StartCoroutine(AnimateOpen());
            }
            else
            {
                m_CanvasGroup.alpha = 1;
                m_CanvasGroup.blocksRaycasts = true;
                m_CanvasGroup.interactable = true;
            }

            // 触发事件
            OnOpen.Invoke();
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        public void Close()
        {
            if (!m_IsOpen)
                return;

            // 设置窗口状态
            m_IsOpen = false;

            // 动画隐藏
            if (m_UseAnimation)
            {
                if (m_AnimationCoroutine != null)
                {
                    StopCoroutine(m_AnimationCoroutine);
                }
                m_AnimationCoroutine = StartCoroutine(AnimateClose());
            }
            else
            {
                m_CanvasGroup.alpha = 0;
                m_CanvasGroup.blocksRaycasts = false;
                m_CanvasGroup.interactable = false;
                gameObject.SetActive(false);
            }

            // 触发事件
            OnClose.Invoke();
        }

        /// <summary>
        /// 设置标题
        /// </summary>
        public void SetTitle(string title)
        {
            m_Title = title;
            if (m_TitleText != null)
            {
                m_TitleText.text = title;
            }
        }

        /// <summary>
        /// 设置内容
        /// </summary>
        public void SetContent(GameObject content)
        {
            // 移除旧内容
            if (m_Content != null && m_Content.transform.parent == m_ContentArea)
            {
                Destroy(m_Content);
            }

            // 设置新内容
            m_Content = content;
            if (m_Content != null && m_ContentArea != null)
            {
                m_Content.transform.SetParent(m_ContentArea);
                RectTransform contentRect = m_Content.GetComponent<RectTransform>();
                if (contentRect != null)
                {
                    contentRect.anchorMin = Vector2.zero;
                    contentRect.anchorMax = Vector2.one;
                    contentRect.offsetMin = Vector2.zero;
                    contentRect.offsetMax = Vector2.zero;
                }
            }
        }

        /// <summary>
        /// 设置窗口大小
        /// </summary>
        public void SetSize(Vector2 size)
        {
            if (m_Window != null)
            {
                // 限制大小范围
                size.x = Mathf.Clamp(size.x, m_MinSize.x, m_MaxSize.x);
                size.y = Mathf.Clamp(size.y, m_MinSize.y, m_MaxSize.y);

                m_Window.sizeDelta = size;

                // 触发事件
                OnResize.Invoke();
            }
        }

        /// <summary>
        /// 设置窗口位置
        /// </summary>
        public void SetPosition(Vector2 position)
        {
            if (m_Window != null)
            {
                m_Window.anchoredPosition = position;
            }
        }

        /// <summary>
        /// 设置模态
        /// </summary>
        public void SetModal(bool modal)
        {
            m_Modal = modal;
            if (m_Overlay != null)
            {
                m_Overlay.raycastTarget = m_Modal;
                Color color = m_Overlay.color;
                color.a = m_Modal ? 0.5f : 0f;
                m_Overlay.color = color;
            }
        }

        /// <summary>
        /// 设置可拖动
        /// </summary>
        public void SetDraggable(bool draggable)
        {
            m_Draggable = draggable;
        }

        /// <summary>
        /// 设置可调整大小
        /// </summary>
        public void SetResizable(bool resizable)
        {
            m_Resizable = resizable;
        }

        /// <summary>
        /// 开始拖动
        /// </summary>
        public void BeginDrag(Vector2 position)
        {
            if (!m_Draggable || !m_interactable)
                return;

            m_IsDragging = true;
            m_DragOffset = m_Window.anchoredPosition - position;

            // 触发事件
            OnDragBegin.Invoke();
        }

        /// <summary>
        /// 拖动
        /// </summary>
        public void Drag(Vector2 position)
        {
            if (!m_IsDragging)
                return;

            m_Window.anchoredPosition = position + m_DragOffset;
        }

        /// <summary>
        /// 结束拖动
        /// </summary>
        public void EndDrag()
        {
            if (!m_IsDragging)
                return;

            m_IsDragging = false;

            // 触发事件
            OnDragEnd.Invoke();
        }

        /// <summary>
        /// 开始调整大小
        /// </summary>
        public void BeginResize(Vector2 position)
        {
            if (!m_Resizable || !m_interactable)
                return;

            m_IsResizing = true;
            m_ResizeStartSize = m_Window.sizeDelta;
            m_ResizeStartPosition = position;
        }

        /// <summary>
        /// 调整大小
        /// </summary>
        public void Resize(Vector2 position)
        {
            if (!m_IsResizing)
                return;

            Vector2 delta = position - m_ResizeStartPosition;
            Vector2 newSize = m_ResizeStartSize + new Vector2(delta.x, -delta.y);

            // 限制大小范围
            newSize.x = Mathf.Clamp(newSize.x, m_MinSize.x, m_MaxSize.x);
            newSize.y = Mathf.Clamp(newSize.y, m_MinSize.y, m_MaxSize.y);

            m_Window.sizeDelta = newSize;

            // 触发事件
            OnResize.Invoke();
        }

        /// <summary>
        /// 结束调整大小
        /// </summary>
        public void EndResize()
        {
            if (!m_IsResizing)
                return;

            m_IsResizing = false;
        }

        #endregion

        #region VRUIComponent实现

        /// <summary>
        /// 设置组件是否可交互
        /// </summary>
        public override void SetInteractable(bool interactable)
        {
            if (m_interactable == interactable)
                return;

            m_interactable = interactable;
            UpdateVisualState(m_interactable ? InteractionState.Normal : InteractionState.Disabled);
            OnInteractableChanged.Invoke(m_interactable);

            // 更新关闭按钮可交互性
            if (m_CloseButton != null)
            {
                m_CloseButton.SetInteractable(m_interactable);
            }

            // 如果禁用，停止拖动和调整大小
            if (!m_interactable)
            {
                m_IsDragging = false;
                m_IsResizing = false;
            }
        }

        /// <summary>
        /// 更新视觉状态
        /// </summary>
        public override void UpdateVisualState(InteractionState state)
        {
            // 保存当前状态
            m_currentState = state;

            // 根据状态设置视觉效果
            Color backgroundColor;
            Color titleBarColor;
            Color titleTextColor;

            // 根据主题设置颜色
            if (m_theme != null)
            {
                backgroundColor = m_theme.backgroundColor;
                titleBarColor = m_theme.accentColor;
                titleTextColor = m_theme.GetTextColor(InteractionState.Normal);
            }
            else
            {
                // 默认颜色
                backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
                titleBarColor = new Color(0.3f, 0.3f, 0.3f, 1f);
                titleTextColor = Color.white;

                // 根据状态调整
                if (state == InteractionState.Disabled)
                {
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
                    titleBarColor = new Color(0.3f, 0.3f, 0.3f, 0.7f);
                    titleTextColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);
                }
            }

            // 应用颜色
            if (m_Background != null)
            {
                m_Background.color = backgroundColor;
            }

            if (m_TitleBar != null && m_TitleBar.GetComponent<Image>() != null)
            {
                m_TitleBar.GetComponent<Image>().color = titleBarColor;
            }

            if (m_TitleText != null)
            {
                m_TitleText.color = titleTextColor;
            }
        }

        /// <summary>
        /// 点击遮罩层
        /// </summary>
        public void OnOverlayClicked()
        {
            if (m_CloseOnClickOutside && m_interactable)
            {
                Close();
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponents()
        {
            // 如果没有RectTransform，添加一个
            if (m_rectTransform == null)
            {
                m_rectTransform = gameObject.AddComponent<RectTransform>();
                m_rectTransform.anchorMin = Vector2.zero;
                m_rectTransform.anchorMax = Vector2.one;
                m_rectTransform.offsetMin = Vector2.zero;
                m_rectTransform.offsetMax = Vector2.zero;
            }

            // 如果没有遮罩层，创建一个
            if (m_Overlay == null)
            {
                GameObject overlayObj = new GameObject("Overlay");
                overlayObj.transform.SetParent(transform);

                RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;

                m_Overlay = overlayObj.AddComponent<Image>();
                m_Overlay.color = new Color(0, 0, 0, m_Modal ? 0.5f : 0);
                m_Overlay.raycastTarget = m_Modal;

                // 添加点击事件
                VRButton overlayButton = overlayObj.AddComponent<VRButton>();
                overlayButton.OnClick.AddListener(OnOverlayClicked);
            }

            // 如果没有窗口，创建一个
            if (m_Window == null)
            {
                GameObject windowObj = new GameObject("Window");
                windowObj.transform.SetParent(transform);

                m_Window = windowObj.AddComponent<RectTransform>();
                m_Window.anchorMin = new Vector2(0.5f, 0.5f);
                m_Window.anchorMax = new Vector2(0.5f, 0.5f);
                m_Window.pivot = new Vector2(0.5f, 0.5f);
                m_Window.sizeDelta = m_DefaultSize;
                m_Window.anchoredPosition = Vector2.zero;

                m_Background = windowObj.AddComponent<Image>();
                m_Background.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            }

            // 如果没有标题栏，创建一个
            if (m_TitleBar == null)
            {
                GameObject titleBarObj = new GameObject("TitleBar");
                titleBarObj.transform.SetParent(m_Window);

                m_TitleBar = titleBarObj.AddComponent<RectTransform>();
                m_TitleBar.anchorMin = new Vector2(0, 1);
                m_TitleBar.anchorMax = new Vector2(1, 1);
                m_TitleBar.pivot = new Vector2(0.5f, 1);
                m_TitleBar.sizeDelta = new Vector2(0, m_TitleBarHeight);
                m_TitleBar.anchoredPosition = Vector2.zero;

                Image titleBarImage = titleBarObj.AddComponent<Image>();
                titleBarImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

                // 创建拖动处理器
                GameObject dragHandlerObj = new GameObject("DragHandler");
                dragHandlerObj.transform.SetParent(titleBarObj.transform);

                RectTransform dragHandlerRect = dragHandlerObj.AddComponent<RectTransform>();
                dragHandlerRect.anchorMin = Vector2.zero;
                dragHandlerRect.anchorMax = Vector2.one;
                dragHandlerRect.offsetMin = Vector2.zero;
                dragHandlerRect.offsetMax = Vector2.zero;

                // 添加拖动处理组件
                DragHandler dragHandler = dragHandlerObj.AddComponent<DragHandler>();
                dragHandler.popupWindow = this;
            }

            // 如果没有标题文本，创建一个
            if (m_TitleText == null)
            {
                GameObject titleTextObj = new GameObject("TitleText");
                titleTextObj.transform.SetParent(m_TitleBar);

                RectTransform titleTextRect = titleTextObj.AddComponent<RectTransform>();
                titleTextRect.anchorMin = new Vector2(0, 0);
                titleTextRect.anchorMax = new Vector2(1, 1);
                titleTextRect.offsetMin = new Vector2(10, 0);
                titleTextRect.offsetMax = new Vector2(-40, 0);

                m_TitleText = titleTextObj.AddComponent<TextMeshProUGUI>();
                m_TitleText.text = m_Title;
                m_TitleText.alignment = TextAlignmentOptions.Left;
                m_TitleText.enableWordWrapping = false;
                m_TitleText.overflowMode = TextOverflowModes.Ellipsis;
                m_TitleText.color = Color.white;
            }

            // 如果没有关闭按钮，创建一个
            if (m_CloseButton == null && m_ShowCloseButton)
            {
                GameObject closeButtonObj = new GameObject("CloseButton");
                closeButtonObj.transform.SetParent(m_TitleBar);

                RectTransform closeButtonRect = closeButtonObj.AddComponent<RectTransform>();
                closeButtonRect.anchorMin = new Vector2(1, 0.5f);
                closeButtonRect.anchorMax = new Vector2(1, 0.5f);
                closeButtonRect.pivot = new Vector2(1, 0.5f);
                closeButtonRect.sizeDelta = new Vector2(30, 30);
                closeButtonRect.anchoredPosition = new Vector2(-5, 0);

                m_CloseButton = closeButtonObj.AddComponent<VRButton>();
                m_CloseButton.SetText("X");
                m_CloseButton.OnClick.AddListener(Close);

                // 设置主题
                if (m_theme != null)
                {
                    m_CloseButton.SetTheme(m_theme);
                }
            }

            // 如果没有内容区域，创建一个
            if (m_ContentArea == null)
            {
                GameObject contentAreaObj = new GameObject("ContentArea");
                contentAreaObj.transform.SetParent(m_Window);

                m_ContentArea = contentAreaObj.AddComponent<RectTransform>();
                m_ContentArea.anchorMin = new Vector2(0, 0);
                m_ContentArea.anchorMax = new Vector2(1, 1);
                m_ContentArea.offsetMin = new Vector2(m_BorderWidth, m_BorderWidth);
                m_ContentArea.offsetMax = new Vector2(-m_BorderWidth, -m_TitleBarHeight);
            }

            // 如果启用调整大小，添加调整大小控件
            if (m_Resizable)
            {
                GameObject resizeHandleObj = new GameObject("ResizeHandle");
                resizeHandleObj.transform.SetParent(m_Window);

                RectTransform resizeHandleRect = resizeHandleObj.AddComponent<RectTransform>();
                resizeHandleRect.anchorMin = new Vector2(1, 0);
                resizeHandleRect.anchorMax = new Vector2(1, 0);
                resizeHandleRect.pivot = new Vector2(1, 0);
                resizeHandleRect.sizeDelta = new Vector2(20, 20);
                resizeHandleRect.anchoredPosition = Vector2.zero;

                Image resizeHandleImage = resizeHandleObj.AddComponent<Image>();
                resizeHandleImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

                // 添加调整大小事件
                VRButton resizeButton = resizeHandleObj.AddComponent<VRButton>();
                resizeButton.OnPointerDown.AddListener((eventData) => BeginResize(eventData.position));
                resizeButton.OnPointerDrag.AddListener((eventData) => Resize(eventData.position));
                resizeButton.OnPointerUp.AddListener((eventData) => EndResize());
            }
        }

        /// <summary>
        /// 打开动画
        /// </summary>
        private IEnumerator AnimateOpen()
        {
            // 初始状态
            m_CanvasGroup.alpha = 0;
            m_CanvasGroup.blocksRaycasts = true;
            m_CanvasGroup.interactable = true;

            if (m_Window != null)
            {
                Vector2 targetSize = m_Window.sizeDelta;
                m_Window.sizeDelta = targetSize * 0.8f;

                // 动画过渡
                float elapsedTime = 0;
                while (elapsedTime < m_AnimationDuration)
                {
                    float t = elapsedTime / m_AnimationDuration;
                    m_CanvasGroup.alpha = t;
                    m_Window.sizeDelta = Vector2.Lerp(targetSize * 0.8f, targetSize, t);

                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                // 最终状态
                m_CanvasGroup.alpha = 1;
                m_Window.sizeDelta = targetSize;
            }
            else
            {
                // 简单淡入
                float elapsedTime = 0;
                while (elapsedTime < m_AnimationDuration)
                {
                    m_CanvasGroup.alpha = elapsedTime / m_AnimationDuration;
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                m_CanvasGroup.alpha = 1;
            }
        }

        /// <summary>
        /// 关闭动画
        /// </summary>
        private IEnumerator AnimateClose()
        {
            // 初始状态
            m_CanvasGroup.alpha = 1;
            m_CanvasGroup.blocksRaycasts = false;
            m_CanvasGroup.interactable = false;

            if (m_Window != null)
            {
                Vector2 startSize = m_Window.sizeDelta;
                Vector2 targetSize = startSize * 0.8f;

                // 动画过渡
                float elapsedTime = 0;
                while (elapsedTime < m_AnimationDuration)
                {
                    float t = elapsedTime / m_AnimationDuration;
                    m_CanvasGroup.alpha = 1 - t;
                    m_Window.sizeDelta = Vector2.Lerp(startSize, targetSize, t);

                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                // 简单淡出
                float elapsedTime = 0;
                while (elapsedTime < m_AnimationDuration)
                {
                    m_CanvasGroup.alpha = 1 - (elapsedTime / m_AnimationDuration);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }

            // 最终状态
            m_CanvasGroup.alpha = 0;
            gameObject.SetActive(false);
        }

        #endregion
    }

    // 创建DragHandler类
    public class DragHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public VRPopupWindow popupWindow;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (popupWindow != null)
            {
                popupWindow.BeginDrag(eventData.position);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (popupWindow != null)
            {
                popupWindow.Drag(eventData.position);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (popupWindow != null)
            {
                popupWindow.EndDrag();
            }
        }
    }
}