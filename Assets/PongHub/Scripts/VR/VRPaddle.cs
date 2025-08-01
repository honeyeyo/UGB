using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using System.Linq;
using PongHub.Core;
using PongHub.Core.Audio;
using PongHub.Gameplay.Paddle;

namespace PongHub.VR
{
    [RequireComponent(typeof(VRInteractable))]
    [RequireComponent(typeof(Paddle))]
    public class VRPaddle : MonoBehaviour
    {
        [Header("球拍设置")]
        [SerializeField]
        [Tooltip("Paddle Head / 球拍头部 - Transform of the paddle head for movement tracking")]
        private Transform m_paddleHead;

        [SerializeField]
        [Tooltip("Paddle Handle / 球拍手柄 - Transform of the paddle handle for grip reference")]
        private Transform m_paddleHandle;

        [SerializeField]
        [Tooltip("Swing Force / 挥拍力度 - Minimum force threshold for swing detection")]
        private float m_swingForce = 10f;

        [SerializeField]
        [Tooltip("Swing Angle / 挥拍角度 - Maximum angle for swing detection")]
        private float m_swingAngle = 45f;

        [Header("振动设置")]
        [SerializeField]
        [Tooltip("Hit Vibration Intensity / 击球振动强度 - Vibration intensity when hitting ball")]
        private float m_hitVibrationIntensity = 0.5f;

        [SerializeField]
        [Tooltip("Hit Vibration Duration / 击球振动时长 - Duration of hit vibration in seconds")]
        private float m_hitVibrationDuration = 0.1f;

        [SerializeField]
        [Tooltip("Swing Vibration Intensity / 挥拍振动强度 - Vibration intensity during swing")]
        private float m_swingVibrationIntensity = 0.3f;

        [SerializeField]
        [Tooltip("Swing Vibration Duration / 挥拍振动时长 - Duration of swing vibration in seconds")]
        private float m_swingVibrationDuration = 0.05f;

        [Header("输入动作")]
        [SerializeField]
        [Tooltip("Swing Action / 挥拍动作 - Input action reference for swing detection")]
        private InputActionReference m_swingAction;

        [SerializeField]
        [Tooltip("Hit Action / 击球动作 - Input action reference for hit detection")]
        private InputActionReference m_hitAction;

        private VRInteractable m_interactable;
        private Paddle m_paddle;
        private XRController m_controller;
        private Vector3 m_lastPosition;
        private Quaternion m_lastRotation;
        private float m_swingSpeed;
        private bool m_isSwinging;

        [SerializeField]
        [Tooltip("Grab Interactable / 抓取交互 - XR grab interactable component for VR interaction")]
        private XRGrabInteractable m_grabInteractable;

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
                    
                    // 根据手柄旋转判断正反手
                    bool isForehand = DetermineForehandFromRotation(currentRotation, velocity);
                    m_paddle.SetForehand(isForehand);
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
            
            // 播放挥拍音效
            if (AudioManager.Instance != null && m_paddleHead != null)
            {
                float swingIntensity = Mathf.Clamp01(m_swingSpeed / m_swingForce);
                AudioManager.Instance.PlayPaddleSwing(m_paddleHead.position, swingIntensity);
            }
        }

        private void OnSwingEnd()
        {
            m_isSwinging = false;
            
            // 播放挥拍结束音效
            if (AudioManager.Instance != null && m_paddleHead != null)
            {
                float swingIntensity = Mathf.Clamp01(m_swingSpeed / m_swingForce);
                AudioManager.Instance.PlayPaddleSwingEnd(m_paddleHead.position, swingIntensity * 0.7f);
            }
        }

        private void PlayHitVibration()
        {
            // 使用新的VibrationManager系统
            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayVibration(VibrationManager.VibrationType.PaddleHit, GetControllerHand());
            }
            else if (m_controller != null)
            {
                // 备用方案：直接使用XR控制器
                m_controller.SendHapticImpulse(m_hitVibrationIntensity, m_hitVibrationDuration);
            }
        }

        private void PlaySwingVibration()
        {
            // 使用新的VibrationManager系统
            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayCustomVibration(m_swingVibrationIntensity, m_swingVibrationDuration, GetControllerHand());
            }
            else if (m_controller != null)
            {
                // 备用方案：直接使用XR控制器
                m_controller.SendHapticImpulse(m_swingVibrationIntensity, m_swingVibrationDuration);
            }
        }

        /// <summary>
        /// 获取控制器手部索引
        /// </summary>
        /// <returns>0=左手, 1=右手, -1=未知</returns>
        private int GetControllerHand()
        {
            if (m_controller == null) return -1;
            
            // 通过控制器的节点类型判断左右手
            var interactor = m_grabInteractable?.interactorsSelecting?.FirstOrDefault() as XRDirectInteractor;
            if (interactor != null)
            {
                var controllerNode = interactor.GetComponent<XRController>()?.controllerNode;
                switch (controllerNode)
                {
                    case UnityEngine.XR.XRNode.LeftHand:
                        return 0;
                    case UnityEngine.XR.XRNode.RightHand:
                        return 1;
                    default:
                        return -1;
                }
            }
            
            return -1;
        }

        /// <summary>
        /// 根据手柄旋转和挥拍方向判断正反手
        /// </summary>
        /// <param name="paddleRotation">球拍旋转</param>
        /// <param name="velocity">挥拍速度向量</param>
        /// <returns>true为正手，false为反手</returns>
        private bool DetermineForehandFromRotation(Quaternion paddleRotation, Vector3 velocity)
        {
            if (velocity.magnitude < 0.1f) 
                return true; // 静止时默认正手

            // 获取控制器手部类型
            int handType = GetControllerHand();
            
            // 计算球拍朝向（球拍面法线）
            Vector3 paddleForward = paddleRotation * Vector3.forward;
            
            // 计算挥拍方向的水平分量
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z).normalized;
            
            if (handType == 0) // 左手
            {
                // 左手：如果球拍面朝向与挥拍方向大致相同，则为正手
                float dot = Vector3.Dot(paddleForward, horizontalVelocity);
                return dot > 0.3f; // 允许一定角度偏差
            }
            else if (handType == 1) // 右手
            {
                // 右手：如果球拍面朝向与挥拍方向大致相同，则为正手
                float dot = Vector3.Dot(paddleForward, horizontalVelocity);
                return dot > 0.3f; // 允许一定角度偏差
            }
            
            // 未知手部或无法判断时，使用球拍旋转的Y轴旋转角度
            float yRotation = paddleRotation.eulerAngles.y;
            
            // 基于旋转角度的简单判断
            // 0-90度和270-360度认为是正手，90-270度认为是反手
            return (yRotation <= 90f) || (yRotation >= 270f);
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
        
        /// <summary>
        /// 检查是否为左手球拍
        /// </summary>
        public bool IsLeftHand()
        {
            int handType = GetControllerHand();
            return handType == 0; // 0 = 左手
        }
    }
}