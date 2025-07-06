using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using PongHub.Core;

namespace PongHub.UI
{
    /// <summary>
    /// 菜单Canvas控制器
    /// 负责管理菜单Canvas的显示、隐藏和布局
    /// </summary>
    public class MenuCanvasController : MonoBehaviour
    {
        [Header("Canvas Configuration / Canvas配置")]
        [SerializeField]
        [Tooltip("Canvas Component / Canvas组件 - Main canvas component for VR menu display")]
        private Canvas canvas;

        [SerializeField]
        [Tooltip("Canvas Scaler / Canvas缩放器 - Controls UI scaling for different screen sizes")]
        private CanvasScaler canvasScaler;

        [SerializeField]
        [Tooltip("Graphic Raycaster / 图形射线投射器 - Handles UI input detection and interaction")]
        private GraphicRaycaster graphicRaycaster;

        [SerializeField]
        [Tooltip("Canvas Group / Canvas组 - Controls canvas transparency and interaction")]
        private CanvasGroup canvasGroup;

        [Header("Layout Configuration / 布局配置")]
        [SerializeField]
        [Tooltip("Content Root / 内容根节点 - Root transform for all UI content")]
        private RectTransform contentRoot;

        [SerializeField]
        [Tooltip("Reference Resolution / 参考分辨率 - Base resolution for UI scaling (1920x1080)")]
        private Vector2 referenceResolution = new Vector2(1920, 1080);

        [SerializeField]
        [Tooltip("Scale Factor / 缩放因子 - VR world space scaling factor (0.01 for proper VR scale)")]
        private float scaleFactor = 0.01f; // VR中的缩放因子

        [Header("Render Configuration / 渲染配置")]
        [SerializeField]
        [Tooltip("Sorting Order / 渲染顺序 - Canvas rendering order (higher values render on top)")]
        private int sortingOrder = 10;

        [SerializeField]
        [Tooltip("Sorting Layer / 渲染层 - Canvas sorting layer name")]
        private string sortingLayerName = "UI";

        [SerializeField]
        [Tooltip("Block Raycast / 阻挡射线 - Whether canvas blocks raycasts for interaction")]
        private bool blockRaycast = true;

#pragma warning disable 0414
        [SerializeField]
        [Tooltip("Animation Duration / 动画持续时间 - Duration of menu animations")]
        private float animationDuration = 0.3f; // 保留用于将来实现菜单动画
#pragma warning restore 0414

        // 私有字段
        private bool isInitialized = false;
        private Vector3 originalScale;
        private float originalAlpha;

        // 事件
        public System.Action OnCanvasShown;
        public System.Action OnCanvasHidden;

        /// <summary>
        /// Canvas组件引用
        /// </summary>
        public Canvas Canvas => canvas;

        private void Awake()
        {
            InitializeComponents();
            SetupCanvas();
        }

        private void Start()
        {
            if (!isInitialized)
            {
                Initialize();
            }
        }

        private void InitializeComponents()
        {
            // 获取或创建必要组件
            if (canvas == null)
                canvas = GetComponent<Canvas>();

            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();

            if (canvasScaler == null)
                canvasScaler = GetComponent<CanvasScaler>();

            if (canvasScaler == null)
                canvasScaler = gameObject.AddComponent<CanvasScaler>();

            if (graphicRaycaster == null)
                graphicRaycaster = GetComponent<GraphicRaycaster>();

            if (graphicRaycaster == null)
                graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // 查找内容根节点
            if (contentRoot == null)
            {
                var contentObj = transform.Find("Content");
                if (contentObj != null)
                {
                    contentRoot = contentObj.GetComponent<RectTransform>();
                }
                else
                {
                    // 创建内容根节点
                    var contentGO = new GameObject("Content");
                    contentGO.transform.SetParent(transform);
                    contentRoot = contentGO.AddComponent<RectTransform>();

                    // 设置为全屏
                    contentRoot.anchorMin = Vector2.zero;
                    contentRoot.anchorMax = Vector2.one;
                    contentRoot.offsetMin = Vector2.zero;
                    contentRoot.offsetMax = Vector2.zero;
                    contentRoot.localScale = Vector3.one;
                }
            }
        }

        private void SetupCanvas()
        {
            if (canvas == null) return;

            // 设置Canvas为World Space模式
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = sortingOrder;
            canvas.sortingLayerName = sortingLayerName;

            // 设置Canvas尺寸和位置
            var rectTransform = canvas.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = referenceResolution;
                rectTransform.localScale = Vector3.one * scaleFactor;
            }

            // 配置CanvasScaler
            if (canvasScaler != null)
            {
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = referenceResolution;
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                canvasScaler.matchWidthOrHeight = 0.5f;
            }

            // 配置GraphicRaycaster
            if (graphicRaycaster != null)
            {
                graphicRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.ThreeD;
                graphicRaycaster.blockingMask = -1; // 所有层
            }

            // 配置CanvasGroup
            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = blockRaycast;
                originalAlpha = canvasGroup.alpha;
            }

            // 保存原始缩放
            originalScale = transform.localScale;
        }

        public void Initialize()
        {
            if (isInitialized) return;

            SetupCanvas();

            // 设置初始状态为隐藏
            SetVisibility(false, false);

            isInitialized = true;
        }

        /// <summary>
        /// 异步初始化方法
        /// </summary>
        public async System.Threading.Tasks.Task InitializeAsync()
        {
            await System.Threading.Tasks.Task.Yield();
            Initialize();
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            // 隐藏菜单，开始游戏
            HideCanvas(true);

            // 通知游戏核心开始游戏
            var gameCore = FindObjectOfType<GameCore>();
            if (gameCore != null)
            {
                gameCore.StartGame();
            }
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            // 显示暂停菜单
            ShowCanvas(true);

            // 切换到暂停面板
            var tableMenuSystem = FindObjectOfType<TableMenuSystem>();
            if (tableMenuSystem != null)
            {
                tableMenuSystem.ShowPanel(MenuPanel.Main);
            }
        }

        /// <summary>
        /// 显示设置
        /// </summary>
        public void ShowSettings()
        {
            // 显示菜单并切换到设置面板
            ShowCanvas(true);

            var tableMenuSystem = FindObjectOfType<TableMenuSystem>();
            if (tableMenuSystem != null)
            {
                tableMenuSystem.ShowPanel(MenuPanel.Settings);
            }
        }

        /// <summary>
        /// 显示输入设置
        /// </summary>
        public void ShowInputSettings()
        {
            // 显示菜单并切换到控制设置面板
            ShowCanvas(true);

            var tableMenuSystem = FindObjectOfType<TableMenuSystem>();
            if (tableMenuSystem != null)
            {
                tableMenuSystem.ShowPanel(MenuPanel.Controls);
            }
        }

        public void ShowCanvas(bool animated = true)
        {
            if (!isInitialized) Initialize();

            SetVisibility(true, animated);
        }

        public void HideCanvas(bool animated = true)
        {
            SetVisibility(false, animated);
        }

        private void SetVisibility(bool visible, bool animated)
        {
            if (canvas == null) return;

            // 停止所有协程
            StopAllCoroutines();

            if (visible)
            {
                // 显示Canvas
                canvas.gameObject.SetActive(true);

                if (animated)
                {
                    // 动画显示
                    StartCoroutine(ShowCanvasAnimated());
                }
                else
                {
                    // 立即显示
                    if (canvasGroup != null)
                        canvasGroup.alpha = originalAlpha;

                    transform.localScale = originalScale;
                    OnCanvasShown?.Invoke();
                }
            }
            else
            {
                // 隐藏Canvas
                if (animated)
                {
                    // 动画隐藏
                    StartCoroutine(HideCanvasAnimated());
                }
                else
                {
                    // 立即隐藏
                    if (canvasGroup != null)
                        canvasGroup.alpha = 0f;

                    transform.localScale = Vector3.zero;
                    canvas.gameObject.SetActive(false);
                    OnCanvasHidden?.Invoke();
                }
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (canvasGroup != null)
            {
                canvasGroup.interactable = interactable;
                canvasGroup.blocksRaycasts = interactable && blockRaycast;
            }
        }

        public void SetAlpha(float alpha)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }
        }

        public void SetScale(Vector3 scale)
        {
            transform.localScale = scale;
        }

        public void SetSortingOrder(int order)
        {
            sortingOrder = order;
            if (canvas != null)
            {
                canvas.sortingOrder = sortingOrder;
            }
        }

        public void SetCamera(Camera camera)
        {
            if (canvas != null)
            {
                canvas.worldCamera = camera;
            }
        }

        public void AddUIElement(GameObject element, RectTransform parent = null)
        {
            if (element == null) return;

            var targetParent = parent != null ? parent : contentRoot;
            if (targetParent != null)
            {
                element.transform.SetParent(targetParent, false);
            }
        }

        public void RemoveUIElement(GameObject element)
        {
            if (element != null)
            {
                element.transform.SetParent(null);
            }
        }

        public RectTransform GetContentRoot()
        {
            return contentRoot;
        }

        public bool IsVisible()
        {
            return canvas != null && canvas.gameObject.activeInHierarchy;
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        public void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
        }

        private void OnDestroy()
        {
            // 清理协程
            StopAllCoroutines();
        }

        private IEnumerator ShowCanvasAnimated()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            transform.localScale = Vector3.zero;

            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 使用ease out back效果
                float scaleT = EaseOutBack(t);
                transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, scaleT);

                // 淡入效果
                if (canvasGroup != null)
                {
                    float alphaT = Mathf.Clamp01(t / 0.5f); // 前半段时间淡入
                    canvasGroup.alpha = Mathf.Lerp(0f, originalAlpha, alphaT);
                }

                yield return null;
            }

            transform.localScale = originalScale;
            if (canvasGroup != null)
                canvasGroup.alpha = originalAlpha;

            OnCanvasShown?.Invoke();
        }

        private IEnumerator HideCanvasAnimated()
        {
            float duration = 0.2f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 使用ease in back效果
                float scaleT = EaseInBack(t);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, scaleT);

                // 淡出效果
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                }

                yield return null;
            }

            transform.localScale = Vector3.zero;
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            canvas.gameObject.SetActive(false);
            OnCanvasHidden?.Invoke();
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private float EaseInBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && isInitialized)
            {
                SetupCanvas();
            }
        }

        private void OnDrawGizmos()
        {
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            {
                var rectTransform = canvas.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // 绘制Canvas边界
                    Gizmos.color = Color.green;
                    var size = rectTransform.sizeDelta * scaleFactor;
                    Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0.01f));
                }
            }
        }
#endif
    }
}