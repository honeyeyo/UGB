using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using PongHub.UI.Panels;
using PongHub.Core;

namespace PongHub.UI
{
    /// <summary>
    /// Table menu system core component
    /// 桌面菜单系统核心组件，负责管理平铺在球桌表面的VR菜单系统
    /// </summary>
    public class TableMenuSystem : MonoBehaviour
    {
        [Header("Menu Configuration - 菜单配置")]
        [SerializeField]
        [Tooltip("Menu Canvas / 菜单画布 - Root canvas for the table menu system (World Space recommended)")]
        private Canvas menuCanvas;

        [SerializeField]
        [Tooltip("Canvas Group / 画布组 - Canvas group for fade animations and alpha control")]
        private CanvasGroup menuCanvasGroup;

        [SerializeField]
        [Tooltip("Table Transform / 桌子Transform - Table transform for menu positioning reference")]
        private Transform tableTransform;

        [SerializeField]
        [Tooltip("Menu Offset / 菜单偏移 - Menu offset from table center (x=forward/back, y=up/down, z=left/right)")]
        private Vector3 menuOffset = new Vector3(0, 0.1f, 0);

        [SerializeField]
        [Tooltip("Menu Size / 菜单尺寸 - Menu canvas size in world units (width x height)")]
        private Vector2 menuSize = new Vector2(1.6f, 1.2f);

        [Header("Animation Settings")]
        [SerializeField]
        [Tooltip("Show animation duration")]
        private float showDuration = 0.5f;

#pragma warning disable 0414
        [SerializeField]
        [Tooltip("Ease Strength / 缓动强度 - Strength of the easing function for animations")]
        private float easeStrength = 2f; // 保留用于将来实现缓动效果
#pragma warning restore 0414

        [Header("Menu Panels")]
        [SerializeField]
        [Tooltip("Panel Array / 面板数组 - Array of menu panel GameObjects in order: Main, GameMode, Settings, Audio, Controls, Exit")]
        private GameObject[] menuPanels;

        [Header("Panel References - 面板引用")]
        [SerializeField]
        [Tooltip("Main menu panel component - 主菜单面板组件")]
        private MainMenuPanel mainMenuPanel;

        [SerializeField]
        [Tooltip("Settings panel component - 设置面板组件")]
        private SettingsPanel settingsPanel;

        [SerializeField]
        [Tooltip("Exit confirmation panel component - 退出确认面板组件")]
        private ExitConfirmPanel exitConfirmPanel;

        // Private fields
        private bool isMenuVisible = false;
        private MenuPanel currentPanel = MenuPanel.Main;
        private Coroutine currentAnimation;
        private RectTransform menuRectTransform;
        private Vector3 menuRotation = new Vector3(90f, 0f, 0f); // Default table menu rotation

        // Public properties
        public bool IsMenuVisible => isMenuVisible;
        public MenuPanel CurrentPanel => currentPanel;

        // Events
        public System.Action<bool> OnMenuVisibilityChanged;
        public System.Action<MenuPanel> OnPanelChanged;

        private void Awake()
        {
            InitializeComponents();
            SetupMenuPosition();
            HideMenuImmediate();
        }

        private void Start()
        {
            // Find table transform if not specified
            if (tableTransform == null)
            {
                FindTableTransform();
            }

            // Ensure menu is correctly positioned
            UpdateMenuPosition();
        }

        /// <summary>
        /// 异步初始化方法
        /// </summary>
        public async System.Threading.Tasks.Task InitializeAsync()
        {
            await System.Threading.Tasks.Task.Yield();

            // 初始化组件
            InitializeComponents();
            SetupMenuPosition();
            HideMenuImmediate();

            // 查找桌子Transform
            if (tableTransform == null)
            {
                FindTableTransform();
            }

            // 更新菜单位置
            UpdateMenuPosition();
        }

        /// <summary>
        /// 配置球拍
        /// </summary>
        public void ConfigurePaddle(bool leftHand)
        {
            // TODO: 实现球拍配置逻辑
            Debug.Log($"配置{(leftHand ? "左手" : "右手")}球拍");

            // 可以调用输入管理器的配置方法
            var inputManager = FindObjectOfType<PongHub.Input.PongHubInputManager>();
            if (inputManager != null)
            {
                // inputManager.ConfigurePaddle(leftHand);
            }
        }

        /// <summary>
        /// 瞬移到指定位置
        /// </summary>
        public void TeleportToPoint(int pointIndex)
        {
            // TODO: 实现瞬移功能
            Debug.Log($"瞬移到位置 {pointIndex}");

            // 可以调用VR传送系统
            var cameraRig = FindObjectOfType<OVRCameraRig>();
            if (cameraRig != null)
            {
                // 实现瞬移逻辑
                Vector3 teleportPosition = GetTeleportPosition(pointIndex);
                cameraRig.transform.position = teleportPosition;
            }
        }

        /// <summary>
        /// 获取瞬移位置
        /// </summary>
        private Vector3 GetTeleportPosition(int pointIndex)
        {
            // 根据索引返回预定义的瞬移位置
            Vector3[] teleportPositions = {
                new Vector3(-2f, 0f, 0f), // 左侧位置
                new Vector3(2f, 0f, 0f),  // 右侧位置
                new Vector3(0f, 0f, -2f), // 后方位置
                new Vector3(0f, 0f, 2f)   // 前方位置
            };

            if (pointIndex >= 0 && pointIndex < teleportPositions.Length)
            {
                return teleportPositions[pointIndex];
            }

            return Vector3.zero;
        }

        private void InitializeComponents()
        {
            if (menuCanvas == null)
                menuCanvas = GetComponentInChildren<Canvas>();

            if (menuCanvasGroup == null)
                menuCanvasGroup = menuCanvas?.GetComponent<CanvasGroup>();

            if (menuCanvas != null)
            {
                menuRectTransform = menuCanvas.GetComponent<RectTransform>();

                // Set Canvas to World Space
                menuCanvas.renderMode = RenderMode.WorldSpace;
                menuCanvas.worldCamera = Camera.main;

                // Set Canvas size
                if (menuRectTransform != null)
                {
                    menuRectTransform.sizeDelta = menuSize;
                }
            }

            // Ensure CanvasGroup exists for animations
            if (menuCanvasGroup == null && menuCanvas != null)
            {
                menuCanvasGroup = menuCanvas.gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void SetupMenuPosition()
        {
            if (menuCanvas == null) return;

            // Set menu initial position and rotation
            var menuTransform = menuCanvas.transform;
            menuTransform.localRotation = Quaternion.Euler(menuRotation);
            menuTransform.localScale = Vector3.one;
        }

        private void FindTableTransform()
        {
            // Look for table in GameArea
            var gameArea = FindObjectOfType<GameModeManager>();
            if (gameArea != null)
            {
                // Find GameObject named "Table"
                var tableObject = GameObject.Find("Table");
                if (tableObject != null)
                {
                    tableTransform = tableObject.transform;
                }
            }
        }

        private void UpdateMenuPosition()
        {
            if (tableTransform == null || menuCanvas == null) return;

            // Calculate menu position on table surface
            Vector3 tablePosition = tableTransform.position;
            Vector3 menuPosition = tablePosition + menuOffset;

            // Apply table rotation
            menuPosition = tableTransform.TransformPoint(menuOffset);

            menuCanvas.transform.position = menuPosition;
            menuCanvas.transform.rotation = tableTransform.rotation * Quaternion.Euler(menuRotation);
        }

        public void ToggleMenu()
        {
            if (isMenuVisible)
            {
                HideMenu();
            }
            else
            {
                ShowMenu();
            }
        }

        public void ShowMenu()
        {
            if (isMenuVisible) return;

            isMenuVisible = true;
            UpdateMenuPosition();

            // Stop current animation
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
                currentAnimation = null;
            }

            // Enable Canvas
            if (menuCanvas != null)
            {
                menuCanvas.gameObject.SetActive(true);
            }

            // Show animation
            if (menuCanvasGroup != null)
            {
                currentAnimation = StartCoroutine(ShowMenuAnimated());
            }

            // Show current panel
            ShowPanel(currentPanel);

            // Trigger event
            OnMenuVisibilityChanged?.Invoke(true);

            Debug.Log("TableMenuSystem: Menu shown - 菜单已显示");
        }

        public void HideMenu()
        {
            if (!isMenuVisible) return;

            isMenuVisible = false;

            // Stop current animation
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
                currentAnimation = null;
            }

            // Hide animation
            if (menuCanvasGroup != null)
            {
                currentAnimation = StartCoroutine(HideMenuAnimated());
            }
            else
            {
                // If no CanvasGroup, hide directly
                if (menuCanvas != null)
                {
                    menuCanvas.gameObject.SetActive(false);
                }
            }

            // Trigger event
            OnMenuVisibilityChanged?.Invoke(false);

            Debug.Log("TableMenuSystem: Menu hidden - 菜单已隐藏");
        }

        private void HideMenuImmediate()
        {
            isMenuVisible = false;

            if (menuCanvas != null)
            {
                menuCanvas.gameObject.SetActive(false);
            }

            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = 0f;
                menuCanvasGroup.transform.localScale = Vector3.zero;
            }
        }

        public void ShowPanel(MenuPanel panel)
        {
            if (currentPanel == panel) return;

            currentPanel = panel;

            // Hide all panels
            if (menuPanels != null)
            {
                foreach (var panelObj in menuPanels)
                {
                    if (panelObj != null)
                    {
                        panelObj.SetActive(false);
                    }
                }
            }

            // Show target panel
            int panelIndex = (int)panel;
            if (menuPanels != null && panelIndex >= 0 && panelIndex < menuPanels.Length)
            {
                if (menuPanels[panelIndex] != null)
                {
                    menuPanels[panelIndex].SetActive(true);
                }
            }

            // Trigger event
            OnPanelChanged?.Invoke(panel);

            Debug.Log($"TableMenuSystem: Panel changed to {panel} - 面板切换到 {panel}");
        }

        public Vector3 GetTableMenuPosition()
        {
            if (tableTransform == null) return Vector3.zero;

            return tableTransform.TransformPoint(menuOffset);
        }

        public void SetTableTransform(Transform table)
        {
            tableTransform = table;
            UpdateMenuPosition();
        }

        private void OnDestroy()
        {
            // Clean up animations
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
        }

        private IEnumerator ShowMenuAnimated()
        {
            if (menuCanvasGroup == null) yield break;

            menuCanvasGroup.alpha = 0f;
            menuCanvasGroup.transform.localScale = Vector3.zero;

            float elapsed = 0f;

            while (elapsed < showDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / showDuration;

                // Ease out back for scale
                float scaleT = EaseOutBack(t);
                menuCanvasGroup.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, scaleT);

                // Linear fade in
                menuCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

                yield return null;
            }

            menuCanvasGroup.transform.localScale = Vector3.one;
            menuCanvasGroup.alpha = 1f;
            currentAnimation = null;
        }

        private IEnumerator HideMenuAnimated()
        {
            if (menuCanvasGroup == null) yield break;

            Vector3 startScale = menuCanvasGroup.transform.localScale;
            float startAlpha = menuCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < showDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / showDuration;

                // Ease in back for scale
                float scaleT = EaseInBack(t);
                menuCanvasGroup.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, scaleT);

                // Linear fade out
                menuCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

                yield return null;
            }

            menuCanvasGroup.transform.localScale = Vector3.zero;
            menuCanvasGroup.alpha = 0f;

            if (menuCanvas != null)
            {
                menuCanvas.gameObject.SetActive(false);
            }

            currentAnimation = null;
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

        private void OnDrawGizmos()
        {
            if (tableTransform != null)
            {
                // Draw menu position preview
                Vector3 menuPosition = GetTableMenuPosition();
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(menuPosition, new Vector3(menuSize.x, 0.01f, menuSize.y));

                // Draw connection line
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(tableTransform.position, menuPosition);
            }
        }
    }

    public enum MenuPanel
    {
        Main = 0,           // Main menu - 主菜单
        GameMode = 1,       // Game mode selection - 游戏模式选择
        Settings = 2,       // Settings - 设置
        Audio = 3,          // Audio settings - 音频设置
        Controls = 4,       // Control settings - 控制设置
        Exit = 5            // Exit confirmation - 退出确认
    }
}