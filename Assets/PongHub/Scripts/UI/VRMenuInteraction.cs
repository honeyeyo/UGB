using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PongHub.UI
{
    /// <summary>
    /// VR菜单交互控制器，处理VR控制器与UI的交互
    /// 使用OVR SDK进行VR交互
    /// </summary>
    public class VRMenuInteraction : MonoBehaviour
    {
        [Header("交互设置")]
        [SerializeField] private Transform m_leftControllerTransform;    // 左手控制器Transform
        [SerializeField] private Transform m_rightControllerTransform;   // 右手控制器Transform
        [SerializeField] private float m_maxRayDistance = 10f;          // 最大射线距离
        [SerializeField] private LayerMask m_uiLayerMask;               // UI层掩码

        [Header("视觉反馈")]
        [SerializeField] private LineRenderer m_leftLineRenderer;       // 左手线渲染器
        [SerializeField] private LineRenderer m_rightLineRenderer;      // 右手线渲染器
        [SerializeField] private GameObject m_leftReticle;              // 左手瞄准点
        [SerializeField] private GameObject m_rightReticle;             // 右手瞄准点
        [SerializeField] private float m_lineWidth = 0.01f;             // 线宽度
        [SerializeField] private Color m_normalColor = Color.white;     // 正常颜色
        [SerializeField] private Color m_hoverColor = Color.cyan;       // 悬停颜色
        [SerializeField] private Color m_selectColor = Color.yellow;    // 选择颜色
        [SerializeField] private Color m_invalidColor = Color.red;      // 无效颜色
        [SerializeField] private float m_reticleHoverScale = 1.5f;      // 悬停时瞄准点缩放
        [SerializeField] private float m_reticleSelectScale = 2.0f;     // 选择时瞄准点缩放

        [Header("输入设置")]
        [SerializeField] private InputActionProperty m_leftActivateAction;  // 左手激活动作
        [SerializeField] private InputActionProperty m_rightActivateAction; // 右手激活动作

        [Header("反馈设置")]
        [SerializeField] private bool m_enableVisualFeedback = true;    // 启用视觉反馈
        [SerializeField] private bool m_enableHapticFeedback = true;    // 启用触觉反馈
        // [SerializeField] private bool m_enableAudioFeedback = true;     // 启用音频反馈（暂未使用）
        [SerializeField] private float m_interactionDistance = 5.0f;    // 交互距离阈值

        // 事件
        public event Action<RaycastResult, bool> OnUIHovered;           // UI悬停事件
        public event Action<RaycastResult, bool> OnUISelected;          // UI选择事件
        public event Action OnSelectionTriggered;                       // 选择触发事件

        // 私有变量
        private bool m_isLeftRayActive = false;                         // 左手射线是否激活
        private bool m_isRightRayActive = false;                        // 右手射线是否激活
        private RaycastResult m_leftCurrentResult;                      // 左手当前射线结果
        private RaycastResult m_rightCurrentResult;                     // 右手当前射线结果
        private GameObject m_leftCurrentTarget;                         // 左手当前目标
        private GameObject m_rightCurrentTarget;                        // 右手当前目标
        private MenuInputHandler m_menuInputHandler;                    // 菜单输入处理器引用

        // 视觉反馈变量
        private Vector3 m_leftReticleOriginalScale = Vector3.one;       // 左手瞄准点原始缩放
        private Vector3 m_rightReticleOriginalScale = Vector3.one;      // 右手瞄准点原始缩放
        private float m_leftLastHoverTime = 0f;                         // 左手上次悬停时间
        private float m_rightLastHoverTime = 0f;                        // 右手上次悬停时间
        private float m_hoverFeedbackCooldown = 0.2f;                   // 悬停反馈冷却时间

        #region Unity生命周期

        private void Awake()
        {
            // 初始化组件
            InitializeComponents();

            // 查找菜单输入处理器
            m_menuInputHandler = FindObjectOfType<MenuInputHandler>();
        }

        private void OnEnable()
        {
            // 启用输入动作
            EnableInputActions();
        }

        private void OnDisable()
        {
            // 禁用输入动作
            DisableInputActions();
        }

        private void Update()
        {
            // 更新射线
            UpdateRays();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 触发选择事件
        /// </summary>
        public void TriggerSelection()
        {
            // 触发选择事件
            OnSelectionTriggered?.Invoke();

            // 处理当前悬停的UI元素
            if (m_isLeftRayActive && m_leftCurrentTarget != null)
            {
                HandleUIInteraction(m_leftCurrentResult, true);
            }
            else if (m_isRightRayActive && m_rightCurrentTarget != null)
            {
                HandleUIInteraction(m_rightCurrentResult, false);
            }
        }

        /// <summary>
        /// 设置射线可见性
        /// </summary>
        public void SetRayVisibility(bool isVisible)
        {
            if (m_leftLineRenderer != null)
            {
                m_leftLineRenderer.enabled = isVisible && m_isLeftRayActive;
            }

            if (m_rightLineRenderer != null)
            {
                m_rightLineRenderer.enabled = isVisible && m_isRightRayActive;
            }

            if (m_leftReticle != null)
            {
                m_leftReticle.SetActive(isVisible && m_isLeftRayActive);
            }

            if (m_rightReticle != null)
            {
                m_rightReticle.SetActive(isVisible && m_isRightRayActive);
            }
        }

        /// <summary>
        /// 检查是否有有效的UI目标
        /// </summary>
        public bool HasValidTarget()
        {
            return (m_isLeftRayActive && m_leftCurrentTarget != null) ||
                   (m_isRightRayActive && m_rightCurrentTarget != null);
        }

        /// <summary>
        /// 获取当前交互距离
        /// </summary>
        public float GetCurrentInteractionDistance()
        {
            if (m_isLeftRayActive && m_leftCurrentTarget != null)
            {
                return Vector3.Distance(m_leftControllerTransform.position, m_leftCurrentResult.worldPosition);
            }
            else if (m_isRightRayActive && m_rightCurrentTarget != null)
            {
                return Vector3.Distance(m_rightControllerTransform.position, m_rightCurrentResult.worldPosition);
            }

            return float.MaxValue;
        }

        /// <summary>
        /// 检查交互距离是否有效
        /// </summary>
        public bool IsInteractionDistanceValid()
        {
            float distance = GetCurrentInteractionDistance();
            return distance <= m_interactionDistance;
        }

        /// <summary>
        /// 设置交互距离阈值
        /// </summary>
        /// <param name="distance">新的交互距离阈值</param>
        public void SetInteractionDistance(float distance)
        {
            if (distance > 0)
            {
                m_interactionDistance = distance;
            }
        }

        /// <summary>
        /// 获取当前交互距离阈值
        /// </summary>
        /// <returns>当前交互距离阈值</returns>
        public float GetInteractionDistance()
        {
            return m_interactionDistance;
        }

        /// <summary>
        /// 菜单按钮按下事件（用于测试）
        /// </summary>
        public void OnMenuButtonPressed()
        {
            // 触发选择事件
            TriggerSelection();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponents()
        {
            // 自动查找控制器Transform（如果未指定）
            if (m_leftControllerTransform == null || m_rightControllerTransform == null)
            {
                var cameraRig = FindObjectOfType<OVRCameraRig>();
                if (cameraRig != null)
                {
                    if (m_leftControllerTransform == null)
                        m_leftControllerTransform = cameraRig.leftControllerAnchor;
                    if (m_rightControllerTransform == null)
                        m_rightControllerTransform = cameraRig.rightControllerAnchor;
                }
            }

            // 设置线渲染器
            if (m_leftLineRenderer != null)
            {
                SetupLineRenderer(m_leftLineRenderer);
            }

            if (m_rightLineRenderer != null)
            {
                SetupLineRenderer(m_rightLineRenderer);
            }

            // 创建瞄准点（如果需要）
            if (m_leftReticle == null)
            {
                m_leftReticle = CreateReticle("LeftReticle");
            }

            if (m_rightReticle == null)
            {
                m_rightReticle = CreateReticle("RightReticle");
            }

            // 保存瞄准点原始缩放
            if (m_leftReticle != null)
            {
                m_leftReticleOriginalScale = m_leftReticle.transform.localScale;
            }

            if (m_rightReticle != null)
            {
                m_rightReticleOriginalScale = m_rightReticle.transform.localScale;
            }
        }

        /// <summary>
        /// 设置线渲染器
        /// </summary>
        private void SetupLineRenderer(LineRenderer lineRenderer)
        {
            lineRenderer.startWidth = m_lineWidth;
            lineRenderer.endWidth = m_lineWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = m_normalColor;
            lineRenderer.endColor = m_normalColor;
            lineRenderer.positionCount = 2;
        }

        /// <summary>
        /// 创建瞄准点
        /// </summary>
        private GameObject CreateReticle(string name)
        {
            GameObject reticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reticle.name = name;
            reticle.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            Destroy(reticle.GetComponent<Collider>());
            reticle.SetActive(false);

            // 添加材质
            Renderer renderer = reticle.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                renderer.material.color = m_normalColor;
            }

            return reticle;
        }

        /// <summary>
        /// 启用输入动作
        /// </summary>
        private void EnableInputActions()
        {
            if (m_leftActivateAction.action != null)
            {
                m_leftActivateAction.action.performed += OnLeftActivatePerformed;
                m_leftActivateAction.action.canceled += OnLeftActivateCanceled;
                m_leftActivateAction.action.Enable();
            }

            if (m_rightActivateAction.action != null)
            {
                m_rightActivateAction.action.performed += OnRightActivatePerformed;
                m_rightActivateAction.action.canceled += OnRightActivateCanceled;
                m_rightActivateAction.action.Enable();
            }
        }

        /// <summary>
        /// 禁用输入动作
        /// </summary>
        private void DisableInputActions()
        {
            if (m_leftActivateAction.action != null)
            {
                m_leftActivateAction.action.performed -= OnLeftActivatePerformed;
                m_leftActivateAction.action.canceled -= OnLeftActivateCanceled;
                m_leftActivateAction.action.Disable();
            }

            if (m_rightActivateAction.action != null)
            {
                m_rightActivateAction.action.performed -= OnRightActivatePerformed;
                m_rightActivateAction.action.canceled -= OnRightActivateCanceled;
                m_rightActivateAction.action.Disable();
            }
        }

        /// <summary>
        /// 更新射线
        /// </summary>
        private void UpdateRays()
        {
            // 更新左手射线
            if (m_leftControllerTransform != null)
            {
                UpdateRay(m_leftControllerTransform, m_leftLineRenderer, m_leftReticle, true);
            }

            // 更新右手射线
            if (m_rightControllerTransform != null)
            {
                UpdateRay(m_rightControllerTransform, m_rightLineRenderer, m_rightReticle, false);
            }
        }

        /// <summary>
        /// 更新单个射线
        /// </summary>
        private void UpdateRay(Transform controllerTransform, LineRenderer lineRenderer, GameObject reticle, bool isLeft)
        {
            if (controllerTransform == null) return;

            bool rayActive = isLeft ? m_isLeftRayActive : m_isRightRayActive;

            // 进行射线检测
            RaycastHit hit;
            Vector3 rayOrigin = controllerTransform.position;
            Vector3 rayDirection = controllerTransform.forward;

            bool hasHit = Physics.Raycast(rayOrigin, rayDirection, out hit, m_maxRayDistance, m_uiLayerMask);

            // 更新线渲染器
            if (rayActive && hasHit)
            {
                lineRenderer.enabled = true;
                Vector3 endPoint = hit.point;
                lineRenderer.SetPosition(0, controllerTransform.position);
                lineRenderer.SetPosition(1, endPoint);

                // 根据是否命中设置颜色
                Color rayColor = hasHit ? m_hoverColor : m_normalColor;
                lineRenderer.startColor = rayColor;
                lineRenderer.endColor = rayColor;
            }
            else if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }

            // 更新瞄准点
            if (reticle != null && rayActive)
            {
                if (hasHit)
                {
                    reticle.SetActive(true);
                    reticle.transform.position = hit.point;
                    reticle.transform.LookAt(controllerTransform.position);
                }
                else
                {
                    reticle.SetActive(false);
                }
            }
            else if (reticle != null)
            {
                reticle.SetActive(false);
            }

            // 处理UI交互
            if (hasHit && rayActive)
            {
                // 创建RaycastResult用于UI系统
                RaycastResult result = new RaycastResult
                {
                    gameObject = hit.collider.gameObject,
                    worldPosition = hit.point,
                    distance = hit.distance
                };

                // 检查是否是新目标
                GameObject currentTarget = isLeft ? m_leftCurrentTarget : m_rightCurrentTarget;

                if (currentTarget != hit.collider.gameObject)
                {
                    // 退出旧目标
                    if (currentTarget != null)
                    {
                        HandleUIExit(isLeft ? m_leftCurrentResult : m_rightCurrentResult, isLeft);
                    }

                    // 进入新目标
                    HandleUIEnter(result, isLeft);

                    // 更新当前目标
                    if (isLeft)
                    {
                        m_leftCurrentTarget = hit.collider.gameObject;
                        m_leftCurrentResult = result;
                    }
                    else
                    {
                        m_rightCurrentTarget = hit.collider.gameObject;
                        m_rightCurrentResult = result;
                    }
                }
                else
                {
                    // 更新结果
                    if (isLeft)
                    {
                        m_leftCurrentResult = result;
                    }
                    else
                    {
                        m_rightCurrentResult = result;
                    }
                }
            }
            else
            {
                // 没有命中，清除当前目标
                GameObject currentTarget = isLeft ? m_leftCurrentTarget : m_rightCurrentTarget;
                if (currentTarget != null)
                {
                    HandleUIExit(isLeft ? m_leftCurrentResult : m_rightCurrentResult, isLeft);

                    if (isLeft)
                    {
                        m_leftCurrentTarget = null;
                    }
                    else
                    {
                        m_rightCurrentTarget = null;
                    }
                }
            }
        }

        /// <summary>
        /// 处理UI进入事件
        /// </summary>
        private void HandleUIEnter(RaycastResult result, bool isLeft)
        {
            // 触发悬停事件
            OnUIHovered?.Invoke(result, isLeft);

            // 提供触觉反馈
            if (m_menuInputHandler != null && m_enableHapticFeedback)
            {
                // 检查冷却时间
                float currentTime = Time.time;
                float lastHoverTime = isLeft ? m_leftLastHoverTime : m_rightLastHoverTime;

                if (currentTime - lastHoverTime >= m_hoverFeedbackCooldown)
                {
                    // 提供悬停反馈
                    m_menuInputHandler.ProvideHoverFeedback(isLeft);

                    // 更新上次悬停时间
                    if (isLeft)
                    {
                        m_leftLastHoverTime = currentTime;
                    }
                    else
                    {
                        m_rightLastHoverTime = currentTime;
                    }
                }
            }

            // 执行UI事件
            ExecuteUIEvent(result.gameObject, EventTriggerType.PointerEnter);
        }

        /// <summary>
        /// 处理UI退出事件
        /// </summary>
        private void HandleUIExit(RaycastResult result, bool isLeft)
        {
            // 执行UI事件
            ExecuteUIEvent(result.gameObject, EventTriggerType.PointerExit);
        }

        /// <summary>
        /// 处理UI交互事件
        /// </summary>
        private void HandleUIInteraction(RaycastResult result, bool isLeft)
        {
            // 检查交互距离是否有效
            bool isDistanceValid = Vector3.Distance(
                isLeft ? m_leftControllerTransform.position : m_rightControllerTransform.position,
                result.worldPosition
            ) <= m_interactionDistance;

            // 只有在有效距离内才处理交互
            if (isDistanceValid)
            {
                // 触发选择事件
                OnUISelected?.Invoke(result, isLeft);

                // 提供触觉反馈
                if (m_menuInputHandler != null && m_enableHapticFeedback)
                {
                    m_menuInputHandler.ProvideButtonClickFeedback(isLeft);
                }

                // 执行UI事件
                ExecuteUIEvent(result.gameObject, EventTriggerType.PointerClick);

                // 视觉反馈 - 闪烁瞄准点
                if (m_enableVisualFeedback)
                {
                    GameObject reticle = isLeft ? m_leftReticle : m_rightReticle;
                    if (reticle != null)
                    {
                        StartCoroutine(FlashReticle(reticle, isLeft));
                    }
                }
            }
            else
            {
                // 提供错误反馈
                if (m_menuInputHandler != null && m_enableHapticFeedback)
                {
                    m_menuInputHandler.ProvideErrorFeedback(isLeft);
                }
            }
        }

        /// <summary>
        /// 闪烁瞄准点
        /// </summary>
        private System.Collections.IEnumerator FlashReticle(GameObject reticle, bool isLeft)
        {
            Renderer renderer = reticle.GetComponent<Renderer>();
            if (renderer == null) yield break;

            // 保存原始颜色
            Color originalColor = renderer.material.color;
            Vector3 originalScale = reticle.transform.localScale;

            // 设置选择颜色和缩放
            renderer.material.color = m_selectColor;
            Vector3 baseScale = isLeft ? m_leftReticleOriginalScale : m_rightReticleOriginalScale;
            reticle.transform.localScale = baseScale * m_reticleSelectScale;

            // 等待短暂时间
            yield return new WaitForSeconds(0.1f);

            // 恢复原始颜色和缩放
            renderer.material.color = originalColor;
            reticle.transform.localScale = originalScale;
        }

        /// <summary>
        /// 执行UI事件
        /// </summary>
        private void ExecuteUIEvent(GameObject target, EventTriggerType eventType)
        {
            if (target == null) return;

            // 创建指针事件数据
            PointerEventData eventData = new PointerEventData(EventSystem.current);

            // 根据事件类型执行相应的事件
            switch (eventType)
            {
                case EventTriggerType.PointerEnter:
                    ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerEnterHandler);
                    break;
                case EventTriggerType.PointerExit:
                    ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerExitHandler);
                    break;
                case EventTriggerType.PointerDown:
                    ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerDownHandler);
                    break;
                case EventTriggerType.PointerUp:
                    ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerUpHandler);
                    break;
                case EventTriggerType.PointerClick:
                    ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerClickHandler);
                    break;
            }
        }

        #endregion

        #region 输入事件处理

        /// <summary>
        /// 左手激活按钮按下
        /// </summary>
        private void OnLeftActivatePerformed(InputAction.CallbackContext context)
        {
            m_isLeftRayActive = true;
            SetRayVisibility(true);
        }

        /// <summary>
        /// 左手激活按钮释放
        /// </summary>
        private void OnLeftActivateCanceled(InputAction.CallbackContext context)
        {
            m_isLeftRayActive = false;
            SetRayVisibility(m_isRightRayActive);
        }

        /// <summary>
        /// 右手激活按钮按下
        /// </summary>
        private void OnRightActivatePerformed(InputAction.CallbackContext context)
        {
            m_isRightRayActive = true;
            SetRayVisibility(true);
        }

        /// <summary>
        /// 右手激活按钮释放
        /// </summary>
        private void OnRightActivateCanceled(InputAction.CallbackContext context)
        {
            m_isRightRayActive = false;
            SetRayVisibility(m_isLeftRayActive);
        }

        #endregion
    }
}