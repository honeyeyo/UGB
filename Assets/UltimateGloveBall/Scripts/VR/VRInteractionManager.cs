using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using PongHub.Core;

namespace PongHub.VR
{
    public class VRInteractionManager : MonoBehaviour
    {
        [Header("XR引用")]
        [SerializeField] private XRController m_leftController;
        [SerializeField] private XRController m_rightController;
        [SerializeField] private XRDirectInteractor m_leftInteractor;
        [SerializeField] private XRDirectInteractor m_rightInteractor;
        [SerializeField] private XRRayInteractor m_leftRayInteractor;
        [SerializeField] private XRRayInteractor m_rightRayInteractor;

        [Header("输入动作")]
        [SerializeField] private InputActionReference m_leftGripAction;
        [SerializeField] private InputActionReference m_rightGripAction;
        [SerializeField] private InputActionReference m_leftTriggerAction;
        [SerializeField] private InputActionReference m_rightTriggerAction;

        [Header("交互设置")]
        [SerializeField] private float m_grabThreshold = 0.8f;
        [SerializeField] private float m_throwForce = 10f;
        [SerializeField] private float m_throwAngle = 45f;

        private bool m_isLeftGrabbing;
        private bool m_isRightGrabbing;
        private GameObject m_leftGrabbedObject;
        private GameObject m_rightGrabbedObject;

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        private void Start()
        {
            SetupControllers();
            SetupInteractors();
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
            // 设置控制器
            if (m_leftController != null)
            {
                m_leftController.selectActionTrigger = XRController.ButtonTriggerType.StateChange;
                m_leftController.activateActionTrigger = XRController.ButtonTriggerType.StateChange;
            }

            if (m_rightController != null)
            {
                m_rightController.selectActionTrigger = XRController.ButtonTriggerType.StateChange;
                m_rightController.activateActionTrigger = XRController.ButtonTriggerType.StateChange;
            }
        }

        private void SetupInteractors()
        {
            // 设置直接交互器
            if (m_leftInteractor != null)
            {
                m_leftInteractor.selectActionTrigger = XRBaseInteractor.InputTriggerType.StateChange;
                m_leftInteractor.hoverEntered.AddListener(OnLeftHoverEntered);
                m_leftInteractor.hoverExited.AddListener(OnLeftHoverExited);
                m_leftInteractor.selectEntered.AddListener(OnLeftSelectEntered);
                m_leftInteractor.selectExited.AddListener(OnLeftSelectExited);
            }

            if (m_rightInteractor != null)
            {
                m_rightInteractor.selectActionTrigger = XRBaseInteractor.InputTriggerType.StateChange;
                m_rightInteractor.hoverEntered.AddListener(OnRightHoverEntered);
                m_rightInteractor.hoverExited.AddListener(OnRightHoverExited);
                m_rightInteractor.selectEntered.AddListener(OnRightSelectEntered);
                m_rightInteractor.selectExited.AddListener(OnRightSelectExited);
            }

            // 设置射线交互器
            if (m_leftRayInteractor != null)
            {
                m_leftRayInteractor.selectActionTrigger = XRBaseInteractor.InputTriggerType.StateChange;
                m_leftRayInteractor.hoverEntered.AddListener(OnLeftRayHoverEntered);
                m_leftRayInteractor.hoverExited.AddListener(OnLeftRayHoverExited);
                m_leftRayInteractor.selectEntered.AddListener(OnLeftRaySelectEntered);
                m_leftRayInteractor.selectExited.AddListener(OnLeftRaySelectExited);
            }

            if (m_rightRayInteractor != null)
            {
                m_rightRayInteractor.selectActionTrigger = XRBaseInteractor.InputTriggerType.StateChange;
                m_rightRayInteractor.hoverEntered.AddListener(OnRightRayHoverEntered);
                m_rightRayInteractor.hoverExited.AddListener(OnRightRayHoverExited);
                m_rightRayInteractor.selectEntered.AddListener(OnRightRaySelectEntered);
                m_rightRayInteractor.selectExited.AddListener(OnRightRaySelectExited);
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
        public void SendHapticImpulse(XRController controller, float intensity, float duration)
        {
            if (controller != null)
            {
                controller.SendHapticImpulse(intensity, duration);
            }
        }
    }
}