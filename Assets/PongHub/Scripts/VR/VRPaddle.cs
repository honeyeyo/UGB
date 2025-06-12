using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using PongHub.Core;
using PongHub.Gameplay.Paddle;

namespace PongHub.VR
{
    [RequireComponent(typeof(VRInteractable))]
    [RequireComponent(typeof(Paddle))]
    public class VRPaddle : MonoBehaviour
    {
        [Header("球拍设置")]
        [SerializeField] private Transform m_paddleHead;
        [SerializeField] private Transform m_paddleHandle;
        [SerializeField] private float m_swingForce = 10f;
        [SerializeField] private float m_swingAngle = 45f;

        [Header("振动设置")]
        [SerializeField] private float m_hitVibrationIntensity = 0.5f;
        [SerializeField] private float m_hitVibrationDuration = 0.1f;
        [SerializeField] private float m_swingVibrationIntensity = 0.3f;
        [SerializeField] private float m_swingVibrationDuration = 0.05f;

        [Header("输入动作")]
        [SerializeField] private InputActionReference m_swingAction;
        [SerializeField] private InputActionReference m_hitAction;

        private VRInteractable m_interactable;
        private Paddle m_paddle;
        private XRController m_controller;
        private Vector3 m_lastPosition;
        private Quaternion m_lastRotation;
        private float m_swingSpeed;
        private bool m_isSwinging;
        [SerializeField] private XRGrabInteractable m_grabInteractable;

        private void Awake()
        {
            m_interactable = GetComponent<VRInteractable>();
            m_paddle = GetComponent<Paddle>();
            if (m_grabInteractable == null)
                m_grabInteractable = GetComponent<XRGrabInteractable>();

            SetupInteractable();
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
            m_swingAction?.action.Enable();
            m_hitAction?.action.Enable();
        }

        private void DisableInputActions()
        {
            m_swingAction?.action.Disable();
            m_hitAction?.action.Disable();
        }

        private void Start()
        {
            SetupPaddle();
        }

        private void Update()
        {
            if (m_interactable.IsGrabbed())
            {
                UpdatePaddleMovement();
                UpdateSwingDetection();
            }
        }

        private void SetupPaddle()
        {
            // 设置球拍物理属性
            if (m_paddle != null)
            {
                m_paddle.SetVelocity(Vector3.zero);
                m_paddle.SetForehand(true);
                m_paddle.SetState(PaddleState.Free);
            }
        }

        private void UpdatePaddleMovement()
        {
            if (m_paddleHead != null && m_paddleHandle != null)
            {
                // 更新球拍位置和旋转
                Vector3 currentPosition = m_paddleHead.position;
                Quaternion currentRotation = m_paddleHead.rotation;

                // 计算速度和角速度
                Vector3 velocity = (currentPosition - m_lastPosition) / Time.deltaTime;
                Quaternion deltaRotation = currentRotation * Quaternion.Inverse(m_lastRotation);
                Vector3 angularVelocity = deltaRotation.eulerAngles / Time.deltaTime;

                // 更新球拍状态
                if (m_paddle != null)
                {
                    m_paddle.SetVelocity(velocity);
                    // TODO: 根据手柄旋转判断正反手
                    m_paddle.SetForehand(true);
                }

                // 保存当前位置和旋转
                m_lastPosition = currentPosition;
                m_lastRotation = currentRotation;
            }
        }

        private void UpdateSwingDetection()
        {
            if (m_paddleHead != null)
            {
                // 计算挥拍速度
                Vector3 velocity = (m_paddleHead.position - m_lastPosition) / Time.deltaTime;
                m_swingSpeed = velocity.magnitude;

                // 检测挥拍动作
                if (m_swingSpeed > m_swingForce && !m_isSwinging)
                {
                    OnSwingStart();
                }
                else if (m_swingSpeed < m_swingForce && m_isSwinging)
                {
                    OnSwingEnd();
                }
            }
        }

        private void OnSwingStart()
        {
            m_isSwinging = true;
            PlaySwingVibration();
            // TODO: 播放挥拍音效
        }

        private void OnSwingEnd()
        {
            m_isSwinging = false;
            // TODO: 播放挥拍结束音效
        }

        private void PlayHitVibration()
        {
            if (m_controller != null)
            {
                m_controller.SendHapticImpulse(m_hitVibrationIntensity, m_hitVibrationDuration);
            }
        }

        private void PlaySwingVibration()
        {
            if (m_controller != null)
            {
                m_controller.SendHapticImpulse(m_swingVibrationIntensity, m_swingVibrationDuration);
            }
        }

        // 设置控制器引用
        public void SetController(XRController controller)
        {
            m_controller = controller;
        }

        // 获取挥拍速度
        public float GetSwingSpeed()
        {
            return m_swingSpeed;
        }

        // 获取是否正在挥拍
        public bool IsSwinging()
        {
            return m_isSwinging;
        }

        // 设置挥拍力度
        public void SetSwingForce(float force)
        {
            m_swingForce = force;
        }

        // 设置挥拍角度
        public void SetSwingAngle(float angle)
        {
            m_swingAngle = angle;
        }

        // 设置振动强度
        public void SetHitVibrationIntensity(float intensity)
        {
            m_hitVibrationIntensity = intensity;
        }

        public void SetSwingVibrationIntensity(float intensity)
        {
            m_swingVibrationIntensity = intensity;
        }

        // 设置振动持续时间
        public void SetHitVibrationDuration(float duration)
        {
            m_hitVibrationDuration = duration;
        }

        public void SetSwingVibrationDuration(float duration)
        {
            m_swingVibrationDuration = duration;
        }

        private void SetupInteractable()
        {
            if (m_grabInteractable != null)
            {
                m_grabInteractable.selectEntered.AddListener((args) => OnGrab(args.interactorObject));
                m_grabInteractable.selectExited.AddListener((args) => {
                    if (!args.isCanceled)
                        OnRelease(args.interactorObject);
                });
            }
        }

        private void OnGrab(IXRInteractor interactor)
        {
            if (m_paddle != null)
            {
                m_paddle.SetState(PaddleState.Grabbed);
            }
        }

        private void OnRelease(IXRInteractor interactor)
        {
            if (m_paddle != null)
            {
                m_paddle.SetState(PaddleState.Free);
            }
        }
    }
}