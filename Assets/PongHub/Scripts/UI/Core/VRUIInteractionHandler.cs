using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace PongHub.UI.Core
{
    /// <summary>
    /// VR UI交互处理器
    /// 负责处理VR控制器与UI组件的交互
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class VRUIInteractionHandler : MonoBehaviour
    {
        [Header("射线设置")]
        [SerializeField]
        [Tooltip("Controller Type / 控制器类型 - Which controller this handler is attached to")]
        private ControllerType m_controllerType = ControllerType.Right;

        [SerializeField]
        [Tooltip("Ray Max Distance / 射线最大距离 - Maximum distance of the interaction ray")]
        private float m_rayMaxDistance = 5f;

        [SerializeField]
        [Tooltip("Ray Width / 射线宽度 - Width of the ray visualization")]
        private float m_rayWidth = 0.005f;

        [SerializeField]
        [Tooltip("Ray Color / 射线颜色 - Color of the ray visualization")]
        private Color m_rayColor = new Color(0.227f, 0.525f, 1f, 0.75f);

        [SerializeField]
        [Tooltip("Hit Color / 命中颜色 - Color of the ray when hitting a UI element")]
        private Color m_hitColor = new Color(1f, 0f, 0.431f, 0.75f);

        [Header("交互设置")]
        [SerializeField]
        [Tooltip("Interaction Button / 交互按钮 - Button used for interaction")]
        private UnityEngine.XR.Interaction.Toolkit.InputHelpers.Button m_interactionButton = UnityEngine.XR.Interaction.Toolkit.InputHelpers.Button.Trigger;

        [SerializeField]
        [Tooltip("Hover Cursor / 悬停光标 - Cursor shown when hovering over UI elements")]
        private GameObject m_hoverCursor;

        [SerializeField]
        [Tooltip("Cursor Scale / 光标缩放 - Scale of the hover cursor")]
        private float m_cursorScale = 0.02f;

        [Header("调试设置")]
        [SerializeField]
        [Tooltip("Debug Mode / 调试模式 - Enable debug visualization and logging")]
        private bool m_debugMode = false;

        // 控制器类型枚举
        public enum ControllerType
        {
            Left,
            Right
        }

        // 组件引用
        private LineRenderer m_lineRenderer;
        private Transform m_controllerTransform;
        private InputDevice m_targetDevice;
        private GameObject m_cursorInstance;

        // 交互状态
        private VRUIComponent m_currentHoveredComponent;
        private VRUIComponent m_currentPressedComponent;
        private bool m_wasInteractionButtonPressed = false;

        // 射线检测结果
        private RaycastHit m_hitInfo;

        #region Unity生命周期

        private void Awake()
        {
            // 初始化线渲染器
            InitializeLineRenderer();

            // 创建光标实例
            CreateCursorInstance();
        }

        private void Start()
        {
            // 查找控制器
            FindControllerDevice();
        }

        private void Update()
        {
            // 如果控制器未找到，重新查找
            if (!m_targetDevice.isValid)
            {
                FindControllerDevice();
                return;
            }

            // 更新控制器变换
            UpdateControllerTransform();

            // 处理交互
            HandleInteraction();

            // 更新射线可视化
            UpdateRayVisualization();
        }

        private void OnDisable()
        {
            // 清理当前交互状态
            if (m_currentHoveredComponent != null)
            {
                m_currentHoveredComponent.OnPointerExit();
                m_currentHoveredComponent = null;
            }

            if (m_currentPressedComponent != null)
            {
                m_currentPressedComponent.OnPointerUp();
                m_currentPressedComponent = null;
            }

            // 隐藏射线和光标
            if (m_lineRenderer != null)
            {
                m_lineRenderer.enabled = false;
            }

            if (m_cursorInstance != null)
            {
                m_cursorInstance.SetActive(false);
            }
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化线渲染器
        /// </summary>
        private void InitializeLineRenderer()
        {
            m_lineRenderer = GetComponent<LineRenderer>();
            m_lineRenderer.useWorldSpace = true;
            m_lineRenderer.startWidth = m_rayWidth;
            m_lineRenderer.endWidth = m_rayWidth;
            m_lineRenderer.startColor = m_rayColor;
            m_lineRenderer.endColor = new Color(m_rayColor.r, m_rayColor.g, m_rayColor.b, 0f);
            m_lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            m_lineRenderer.enabled = true;
            m_lineRenderer.positionCount = 2;
        }

        /// <summary>
        /// 创建光标实例
        /// </summary>
        private void CreateCursorInstance()
        {
            if (m_hoverCursor != null)
            {
                m_cursorInstance = Instantiate(m_hoverCursor);
                m_cursorInstance.transform.localScale = Vector3.one * m_cursorScale;
                m_cursorInstance.SetActive(false);
            }
            else
            {
                // 创建默认光标
                m_cursorInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                m_cursorInstance.transform.localScale = Vector3.one * m_cursorScale;

                // 移除碰撞器
                Destroy(m_cursorInstance.GetComponent<Collider>());

                // 设置材质
                var renderer = m_cursorInstance.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                renderer.material.color = m_hitColor;

                m_cursorInstance.SetActive(false);
            }
        }

        /// <summary>
        /// 查找控制器设备
        /// </summary>
        private void FindControllerDevice()
        {
            var characteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller;

            if (m_controllerType == ControllerType.Left)
            {
                characteristics |= InputDeviceCharacteristics.Left;
            }
            else
            {
                characteristics |= InputDeviceCharacteristics.Right;
            }

            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(characteristics, devices);

            if (devices.Count > 0)
            {
                m_targetDevice = devices[0];

                if (m_debugMode)
                {
                    Debug.Log($"[VRUIInteractionHandler] 找到控制器: {m_targetDevice.name}");
                }
            }
            else if (m_debugMode)
            {
                Debug.LogWarning($"[VRUIInteractionHandler] 未找到 {m_controllerType} 控制器");
            }
        }

        #endregion

        #region 交互处理

        /// <summary>
        /// 更新控制器变换
        /// </summary>
        private void UpdateControllerTransform()
        {
            if (m_targetDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
                m_targetDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                transform.position = position;
                transform.rotation = rotation;
            }
        }

        /// <summary>
        /// 处理交互
        /// </summary>
        private void HandleInteraction()
        {
            // 射线起点和方向
            Vector3 rayOrigin = transform.position;
            Vector3 rayDirection = transform.forward;

            // 检测是否按下交互按钮
            bool isInteractionButtonPressed = IsButtonPressed(m_interactionButton);

            // 射线检测
            bool hitUI = Physics.Raycast(rayOrigin, rayDirection, out m_hitInfo, m_rayMaxDistance);
            VRUIComponent hitComponent = null;

            if (hitUI)
            {
                // 查找命中对象上的VRUIComponent
                hitComponent = m_hitInfo.collider.GetComponent<VRUIComponent>();
                if (hitComponent == null)
                {
                    hitComponent = m_hitInfo.collider.GetComponentInParent<VRUIComponent>();
                }
            }

            // 处理悬停状态变化
            if (hitComponent != m_currentHoveredComponent)
            {
                // 退出之前的悬停组件
                if (m_currentHoveredComponent != null)
                {
                    m_currentHoveredComponent.OnPointerExit();
                }

                // 进入新的悬停组件
                m_currentHoveredComponent = hitComponent;
                if (m_currentHoveredComponent != null)
                {
                    m_currentHoveredComponent.OnPointerEnter();
                }
            }

            // 处理按下状态变化
            if (isInteractionButtonPressed != m_wasInteractionButtonPressed)
            {
                m_wasInteractionButtonPressed = isInteractionButtonPressed;

                if (isInteractionButtonPressed)
                {
                    // 按下
                    if (m_currentHoveredComponent != null)
                    {
                        m_currentHoveredComponent.OnPointerDown();
                        m_currentPressedComponent = m_currentHoveredComponent;
                    }
                }
                else
                {
                    // 释放
                    if (m_currentPressedComponent != null)
                    {
                        m_currentPressedComponent.OnPointerUp();

                        // 如果释放时仍然悬停在同一组件上，触发点击
                        if (m_currentPressedComponent == m_currentHoveredComponent)
                        {
                            m_currentPressedComponent.OnPointerClick();
                        }

                        m_currentPressedComponent = null;
                    }
                }
            }

            // 更新光标位置
            UpdateCursorPosition(hitUI);
        }

        /// <summary>
        /// 更新射线可视化
        /// </summary>
        private void UpdateRayVisualization()
        {
            if (m_lineRenderer != null)
            {
                Vector3 startPoint = transform.position;
                Vector3 endPoint;

                if (m_hitInfo.collider != null && m_currentHoveredComponent != null)
                {
                    // 命中UI组件时，射线到命中点
                    endPoint = m_hitInfo.point;
                    m_lineRenderer.startColor = m_hitColor;
                    m_lineRenderer.endColor = new Color(m_hitColor.r, m_hitColor.g, m_hitColor.b, 0f);
                }
                else
                {
                    // 未命中时，射线到最大距离
                    endPoint = transform.position + transform.forward * m_rayMaxDistance;
                    m_lineRenderer.startColor = m_rayColor;
                    m_lineRenderer.endColor = new Color(m_rayColor.r, m_rayColor.g, m_rayColor.b, 0f);
                }

                m_lineRenderer.SetPosition(0, startPoint);
                m_lineRenderer.SetPosition(1, endPoint);
            }
        }

        /// <summary>
        /// 更新光标位置
        /// </summary>
        private void UpdateCursorPosition(bool hitUI)
        {
            if (m_cursorInstance != null)
            {
                if (hitUI && m_currentHoveredComponent != null)
                {
                    m_cursorInstance.SetActive(true);
                    m_cursorInstance.transform.position = m_hitInfo.point;
                    m_cursorInstance.transform.forward = m_hitInfo.normal;
                }
                else
                {
                    m_cursorInstance.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 检查按钮是否按下
        /// </summary>
        private bool IsButtonPressed(UnityEngine.XR.Interaction.Toolkit.InputHelpers.Button button)
        {
            if (!m_targetDevice.isValid)
                return false;

            bool isPressed = false;
            UnityEngine.XR.Interaction.Toolkit.InputHelpers.IsPressed(m_targetDevice, button, out isPressed);
            return isPressed;
        }

        #endregion
    }
}