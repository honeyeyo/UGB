using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using PongHub.Input;
using PongHub.Core;
using UnityEngine.InputSystem;
using UnityEngine.Audio;

namespace PongHub.UI
{
    /// <summary>
    /// VR菜单交互控制器
    /// 处理VR手柄与桌面菜单的交互逻辑
    /// </summary>
    public class VRMenuInteraction : MonoBehaviour
    {
        [Header("Input Configuration")]
        [SerializeField]
        [Tooltip("Input Manager / 输入管理器 - Input manager for handling VR controller inputs")]
        private PongHubInputManager inputManager;

        [SerializeField]
        [Tooltip("Left Ray Interactor / 左手射线交互器 - Left hand XR ray interactor for UI interaction")]
        private XRRayInteractor leftRayInteractor;

        [SerializeField]
        [Tooltip("Right Ray Interactor / 右手射线交互器 - Right hand XR ray interactor for UI interaction")]
        private XRRayInteractor rightRayInteractor;

        [Header("Menu System")]
        [SerializeField]
        [Tooltip("Table Menu System / 桌面菜单系统 - Reference to the table menu system component")]
        private TableMenuSystem tableMenuSystem;

        [Header("Interaction Feedback")]
        [SerializeField]
        [Tooltip("Audio Source / 音频源 - Audio source for interaction feedback sounds")]
        private AudioSource audioSource;

        [SerializeField]
        [Tooltip("Click Sound / 点击音效 - Audio clip played when UI elements are clicked")]
        private AudioClip clickSound;

        [SerializeField]
        [Tooltip("Hover Sound / 悬停音效 - Audio clip played when hovering over UI elements")]
        private AudioClip hoverSound;

#pragma warning disable 0414
        [SerializeField]
        [Tooltip("Enable Haptic Feedback / 启用触觉反馈 - Whether to enable haptic feedback for menu interactions")]
        private bool enableHapticFeedback = true; // 保留用于将来实现触觉反馈
#pragma warning restore 0414

        [Header("Interaction Feedback")]
        [SerializeField]
        [Tooltip("Scale multiplier when hovering over UI elements (1.1 = 10% larger)")]
        private float hoverScale = 1.1f;

        [SerializeField]
        [Tooltip("Scale multiplier when clicking UI elements (0.95 = 5% smaller)")]
        private float clickScale = 0.95f;

        [SerializeField]
        [Tooltip("Duration of visual feedback animations in seconds")]
        private float feedbackDuration = 0.1f;

        // 私有字段
        private GameObject currentHoveredObject;
        private bool isMenuButtonPressed = false;
        private Camera vrCamera;

        // 事件
        public System.Action<GameObject> OnUIElementHovered;
        public System.Action<GameObject> OnUIElementClickedEvent;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            SetupInputHandlers();
            SetupVRCamera();
        }

        private void InitializeComponents()
        {
            // 查找组件如果没有指定
            if (inputManager == null)
                inputManager = FindObjectOfType<PongHubInputManager>();

            if (tableMenuSystem == null)
                tableMenuSystem = FindObjectOfType<TableMenuSystem>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            // 如果没有AudioSource，创建一个
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.volume = 0.5f;
            }
        }

        private void SetupInputHandlers()
        {
            if (inputManager != null)
            {
                // 监听Menu按键事件
                // 注意：这里需要根据实际的PongHubInputManager接口来调整
                // inputManager.OnMenuButtonPressed += OnMenuButtonPressed;
            }
        }

        private void SetupVRCamera()
        {
            // 查找VR摄像机 - 使用OVR框架
            vrCamera = Camera.main;
            if (vrCamera == null)
            {
                // 查找OVRCameraRig
                var ovrCameraRig = FindObjectOfType<OVRCameraRig>();
                if (ovrCameraRig != null)
                {
                    vrCamera = ovrCameraRig.GetComponentInChildren<Camera>();
                }
            }
        }

        private void Update()
        {
            // 检查Menu按键输入
            CheckMenuButtonInput();

            // 更新射线交互
            UpdateRaycastInteraction();
        }

        private void CheckMenuButtonInput()
        {
            // 检查Menu按键（通常是左手柄的Menu按键）
            bool menuButtonDown = false;

            // 使用Unity的Input System检查Menu按键
            if (UnityEngine.Input.GetKeyDown(KeyCode.M) || // 键盘调试
                UnityEngine.Input.GetButtonDown("Menu")) // VR控制器
            {
                menuButtonDown = true;
            }

            // 也可以通过XR Input检查
            if (leftRayInteractor != null)
            {
                var leftController = leftRayInteractor.GetComponent<XRController>();
                if (leftController != null)
                {
                    leftController.inputDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton, out bool leftMenuButton);
                    if (leftMenuButton && !isMenuButtonPressed)
                    {
                        menuButtonDown = true;
                    }
                    isMenuButtonPressed = leftMenuButton;
                }
            }

            if (menuButtonDown)
            {
                OnMenuButtonPressed();
            }
        }

        public void OnMenuButtonPressed()
        {
            if (tableMenuSystem != null)
            {
                tableMenuSystem.ToggleMenu();

                // 播放音效
                if (audioSource != null && clickSound != null)
                {
                    audioSource.PlayOneShot(clickSound);
                }
            }
        }

        public void UpdateRaycastInteraction()
        {
            GameObject hitObject = null;

            // 检查左手射线交互
            if (leftRayInteractor != null && leftRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit leftHit))
            {
                hitObject = leftHit.collider.gameObject;
            }

            // 检查右手射线交互（如果左手没有命中）
            if (hitObject == null && rightRayInteractor != null && rightRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit rightHit))
            {
                hitObject = rightHit.collider.gameObject;
            }

            // 处理Hover状态变化
            if (hitObject != currentHoveredObject)
            {
                // 取消之前的Hover效果
                if (currentHoveredObject != null)
                {
                    OnUIElementHoverExit(currentHoveredObject);
                }

                // 应用新的Hover效果
                if (hitObject != null && IsUIElement(hitObject))
                {
                    OnUIElementHoverEnter(hitObject);
                    currentHoveredObject = hitObject;
                }
                else
                {
                    currentHoveredObject = null;
                }
            }

            // 检查点击
            if (currentHoveredObject != null && IsClickTriggered())
            {
                OnUIElementClicked(currentHoveredObject);
            }
        }

        private bool IsUIElement(GameObject obj)
        {
            // 检查是否是UI元素
            return obj.GetComponent<Button>() != null ||
                   obj.GetComponent<Toggle>() != null ||
                   obj.GetComponent<Slider>() != null ||
                   obj.GetComponentInParent<Canvas>() != null;
        }

        private bool IsClickTriggered()
        {
            // 检查是否触发了点击（trigger按键或select按键）
            bool clickTriggered = false;

            // 检查左手控制器
            if (leftRayInteractor != null)
            {
                var leftController = leftRayInteractor.GetComponent<XRController>();
                if (leftController != null)
                {
                    leftController.inputDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool leftTrigger);
                    leftController.inputDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool leftPrimary);
                    clickTriggered = leftTrigger || leftPrimary;
                }
            }

            // 检查右手控制器
            if (!clickTriggered && rightRayInteractor != null)
            {
                var rightController = rightRayInteractor.GetComponent<XRController>();
                if (rightController != null)
                {
                    rightController.inputDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool rightTrigger);
                    rightController.inputDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool rightPrimary);
                    clickTriggered = rightTrigger || rightPrimary;
                }
            }

            return clickTriggered;
        }

        private void OnUIElementHoverEnter(GameObject element)
        {
            // 应用Hover效果
            var rectTransform = element.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one * hoverScale;
            }

            // 播放Hover音效
            if (audioSource != null && hoverSound != null)
            {
                audioSource.PlayOneShot(hoverSound);
            }

            OnUIElementHovered?.Invoke(element);
        }

        private void OnUIElementHoverExit(GameObject element)
        {
            // 取消Hover效果
            var rectTransform = element.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
            }
        }

        public void OnUIElementClicked(GameObject element)
        {
            // 应用点击效果
            var rectTransform = element.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                var originalScale = rectTransform.localScale;
                rectTransform.localScale = originalScale * clickScale;

                // 恢复原始尺寸
                StartCoroutine(RestoreScaleAfterDelay(rectTransform, originalScale));
            }

            // 播放点击音效
            if (audioSource != null && clickSound != null)
            {
                audioSource.PlayOneShot(clickSound);
            }

            // 处理按钮点击
            var button = element.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.Invoke();
            }

            OnUIElementClickedEvent?.Invoke(element);
        }

        private System.Collections.IEnumerator RestoreScaleAfterDelay(RectTransform rectTransform, Vector3 originalScale)
        {
            yield return new WaitForSeconds(feedbackDuration);
            if (rectTransform != null)
            {
                rectTransform.localScale = originalScale;
            }
        }

        private void OnDestroy()
        {
            // 清理事件监听
            if (inputManager != null)
            {
                // inputManager.OnMenuButtonPressed -= OnMenuButtonPressed;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // 绘制射线交互的可视化调试信息
            if (leftRayInteractor != null)
            {
                var leftTransform = leftRayInteractor.transform;
                Gizmos.color = Color.red;
                Gizmos.DrawRay(leftTransform.position, leftTransform.forward * 10f);
            }

            if (rightRayInteractor != null)
            {
                var rightTransform = rightRayInteractor.transform;
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(rightTransform.position, rightTransform.forward * 10f);
            }
        }
#endif
    }
}