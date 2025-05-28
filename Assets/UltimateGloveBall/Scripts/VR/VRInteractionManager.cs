using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using XRController = UnityEngine.XR.Interaction.Toolkit.XRController;
using PongHub.Core;

namespace PongHub.VR
{
    public class VRInteractionManager : MonoBehaviour
    {
        [Header("控制器引用")]
        [SerializeField] private XRController m_leftController;
        [SerializeField] private XRController m_rightController;

        [Header("交互器引用")]
        [SerializeField] private XRBaseInteractor m_leftInteractor;
        [SerializeField] private XRBaseInteractor m_rightInteractor;
        [SerializeField] private XRRayInteractor m_leftRayInteractor;
        [SerializeField] private XRRayInteractor m_rightRayInteractor;

        [Header("输入动作")]
        [SerializeField] private InputActionReference m_leftGripAction;
        [SerializeField] private InputActionReference m_rightGripAction;
        [SerializeField] private InputActionReference m_leftTriggerAction;
        [SerializeField] private InputActionReference m_rightTriggerAction;
        [SerializeField] private InputActionReference m_leftActivateAction;
        [SerializeField] private InputActionReference m_rightActivateAction;

        [Header("交互设置")]
        [SerializeField] private float m_grabThreshold = 0.1f;
        [SerializeField] private float m_throwForce = 10f;
        [SerializeField] private float m_throwAngle = 45f;

        private bool m_isLeftGrabbing;
        private bool m_isRightGrabbing;
        private GameObject m_leftGrabbedObject;
        private GameObject m_rightGrabbedObject;

        private void Awake()
        {
            SetupControllers();
            SetupInteractors();
        }

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        private void EnableInputActions()
        {
            m_leftGripAction?.action.Enable();
            m_rightGripAction?.action.Enable();
            m_leftTriggerAction?.action.Enable();
            m_rightTriggerAction?.action.Enable();
        }

        private void DisableInputActions()
        {
            m_leftGripAction?.action.Disable();
            m_rightGripAction?.action.Disable();
            m_leftTriggerAction?.action.Disable();
            m_rightTriggerAction?.action.Disable();
        }

        private void SetupControllers()
        {
            if (m_leftController != null)
            {
                m_leftController.enableInputActions = true;
            }

            if (m_rightController != null)
            {
                m_rightController.enableInputActions = true;
            }
        }

        private void SetupInteractors()
        {
            if (m_leftInteractor != null)
            {
                m_leftInteractor.enabled = true;
            }

            if (m_rightInteractor != null)
            {
                m_rightInteractor.enabled = true;
            }

            if (m_leftRayInteractor != null)
            {
                m_leftRayInteractor.enabled = true;
            }

            if (m_rightRayInteractor != null)
            {
                m_rightRayInteractor.enabled = true;
            }
        }

        #region 直接交互事件处理
        private void OnLeftHoverEntered(HoverEnterEventArgs args)
        {
            // 处理左手悬停进入
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                // TODO: 实现悬停效果
            }
        }

        private void OnLeftHoverExited(HoverExitEventArgs args)
        {
            // 处理左手悬停退出
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                // TODO: 取消悬停效果
            }
        }

        private void OnLeftSelectEntered(SelectEnterEventArgs args)
        {
            // 处理左手抓取
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                m_isLeftGrabbing = true;
                m_leftGrabbedObject = interactable.transform.gameObject;
                // TODO: 实现抓取效果
            }
        }

        private void OnLeftSelectExited(SelectExitEventArgs args)
        {
            // 处理左手释放
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                m_isLeftGrabbing = false;
                m_leftGrabbedObject = null;
                // TODO: 实现释放效果
            }
        }

        private void OnRightHoverEntered(HoverEnterEventArgs args)
        {
            // 处理右手悬停进入
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                // TODO: 实现悬停效果
            }
        }

        private void OnRightHoverExited(HoverExitEventArgs args)
        {
            // 处理右手悬停退出
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                // TODO: 取消悬停效果
            }
        }

        private void OnRightSelectEntered(SelectEnterEventArgs args)
        {
            // 处理右手抓取
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                m_isRightGrabbing = true;
                m_rightGrabbedObject = interactable.transform.gameObject;
                // TODO: 实现抓取效果
            }
        }

        private void OnRightSelectExited(SelectExitEventArgs args)
        {
            // 处理右手释放
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                m_isRightGrabbing = false;
                m_rightGrabbedObject = null;
                // TODO: 实现释放效果
            }
        }
        #endregion

        #region 射线交互事件处理
        private void OnLeftRayHoverEntered(HoverEnterEventArgs args)
        {
            // 处理左手射线悬停进入
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                // TODO: 实现射线悬停效果
            }
        }

        private void OnLeftRayHoverExited(HoverExitEventArgs args)
        {
            // 处理左手射线悬停退出
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                // TODO: 取消射线悬停效果
            }
        }

        private void OnLeftRaySelectEntered(SelectEnterEventArgs args)
        {
            // 处理左手射线选择
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                // TODO: 实现射线选择效果
            }
        }

        private void OnLeftRaySelectExited(SelectExitEventArgs args)
        {
            // 处理左手射线取消选择
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                // TODO: 实现射线取消选择效果
            }
        }

        private void OnRightRayHoverEntered(HoverEnterEventArgs args)
        {
            // 处理右手射线悬停进入
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                // TODO: 实现射线悬停效果
            }
        }

        private void OnRightRayHoverExited(HoverExitEventArgs args)
        {
            // 处理右手射线悬停退出
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                // TODO: 取消射线悬停效果
            }
        }

        private void OnRightRaySelectEntered(SelectEnterEventArgs args)
        {
            // 处理右手射线选择
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                // TODO: 实现射线选择效果
            }
        }

        private void OnRightRaySelectExited(SelectExitEventArgs args)
        {
            // 处理右手射线取消选择
            var interactable = args.interactableObject;
            if (interactable != null)
            {
                // TODO: 实现射线取消选择效果
            }
        }
        #endregion

        // 获取控制器位置和旋转
        public Vector3 GetLeftControllerPosition()
        {
            return m_leftController != null ? m_leftController.transform.position : Vector3.zero;
        }

        public Quaternion GetLeftControllerRotation()
        {
            return m_leftController != null ? m_leftController.transform.rotation : Quaternion.identity;
        }

        public Vector3 GetRightControllerPosition()
        {
            return m_rightController != null ? m_rightController.transform.position : Vector3.zero;
        }

        public Quaternion GetRightControllerRotation()
        {
            return m_rightController != null ? m_rightController.transform.rotation : Quaternion.identity;
        }

        // 获取抓取状态
        public bool IsLeftGrabbing()
        {
            return m_isLeftGrabbing;
        }

        public bool IsRightGrabbing()
        {
            return m_isRightGrabbing;
        }

        // 获取抓取的对象
        public GameObject GetLeftGrabbedObject()
        {
            return m_leftGrabbedObject;
        }

        public GameObject GetRightGrabbedObject()
        {
            return m_rightGrabbedObject;
        }

        // 发送触觉反馈
        public void SendHapticImpulse(bool isLeft, float amplitude, float duration)
        {
            var controller = isLeft ? m_leftController : m_rightController;
            if (controller != null)
            {
                controller.SendHapticImpulse(amplitude, duration);
            }
        }

        public bool IsControllerGrabbing(bool isLeft)
        {
            var controller = isLeft ? m_leftController : m_rightController;
            var activateAction = isLeft ? m_leftActivateAction : m_rightActivateAction;
            
            if (controller != null && activateAction != null)
            {
                return activateAction.action.ReadValue<float>() > 0.5f;
            }
            return false;
        }

        public Vector3 CalculateThrowVelocity(Vector3 direction)
        {
            // 根据投掷角度和力度计算投掷速度
            float angleRad = m_throwAngle * Mathf.Deg2Rad;
            Vector3 throwDirection = Quaternion.Euler(-m_throwAngle, 0f, 0f) * direction;
            return throwDirection.normalized * m_throwForce;
        }

        // 属性
        public XRController LeftController => m_leftController;
        public XRController RightController => m_rightController;
        public XRBaseInteractor LeftInteractor => m_leftInteractor;
        public XRBaseInteractor RightInteractor => m_rightInteractor;
        public XRRayInteractor LeftRayInteractor => m_leftRayInteractor;
        public XRRayInteractor RightRayInteractor => m_rightRayInteractor;
    }
}