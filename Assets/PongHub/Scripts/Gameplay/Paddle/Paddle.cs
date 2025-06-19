using UnityEngine;
using PongHub.Gameplay.Ball;
using PongHub.Core;
using PongHub.Core.Audio;
using UnityEngine.XR.Interaction.Toolkit;
using System.Threading.Tasks;

namespace PongHub.Gameplay.Paddle
{
    // 球拍状态
    public enum PaddleGripState
    {
        Anchored,   // 固定在手上
        Free        // 自由状态(比如掉在地上)
    }

    // 球拍状态枚举
    public enum PaddleState
    {
        None,
        Free,
        Grabbed,
        Throwing
    }
    [RequireComponent(typeof(Rigidbody))]
    public class Paddle : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private Rigidbody m_rigidbody;
        [SerializeField] private MeshRenderer m_renderer;
        [SerializeField] private Collider m_collider;
        [SerializeField] private PaddleRubber m_forehandRubber;  // 正手胶皮
        [SerializeField] private PaddleRubber m_backhandRubber;  // 反手胶皮
        [SerializeField] private PaddleBlade m_blade;           // 底板
        [SerializeField] private XRController m_controller;     // XR控制器引用

        [Header("配置")]
        [SerializeField] private PaddleData m_paddleData;

        // 球拍状态
        private PaddleGripState m_gripState = PaddleGripState.Anchored;
        private bool m_isForehand = true;  // 当前使用正手面
        private Vector3 m_lastVelocity;    // 上一帧速度
        private Vector3 m_currentVelocity; // 当前速度
        private Vector3 m_acceleration;    // 加速度
        private float m_lastHitTime;       // 上次击球时间
        private Vector3 m_velocity;
        private PaddleState m_currentState;
        private float m_lastVibrationTime;

        private void Awake()
        {
            if (m_rigidbody == null)
                m_rigidbody = GetComponent<Rigidbody>();
            if (m_renderer == null)
                m_renderer = GetComponent<MeshRenderer>();
            if (m_collider == null)
                m_collider = GetComponent<Collider>();

            SetupRigidbody();
            SetupCollider();
            SetupVisuals();
        }

        private void SetupRigidbody()
        {
            m_rigidbody.mass = m_paddleData.Mass;
            m_rigidbody.drag = m_paddleData.Drag;
            m_rigidbody.angularDrag = m_paddleData.Drag * 2f;
            m_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            m_rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            m_rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        }

        private void SetupCollider()
        {
            m_collider.material = new PhysicMaterial("Paddle")
            {
                bounciness = m_paddleData.Bounce,
                dynamicFriction = m_paddleData.Friction,
                staticFriction = m_paddleData.Friction
            };
        }

        private void SetupVisuals()
        {
            m_renderer.material.color = m_paddleData.PaddleColor;
        }

        private void FixedUpdate()
        {
            if (m_gripState == PaddleGripState.Anchored)
            {
                UpdateMotionState();
            }
            if (m_currentState == PaddleState.Free || m_currentState == PaddleState.Grabbed || m_currentState == PaddleState.Throwing)
            {
                UpdateMovement();
            }
        }

        private void UpdateMotionState()
        {
            // 计算球拍的运动状态
            m_lastVelocity = m_currentVelocity;
            m_currentVelocity = m_rigidbody.velocity;
            m_acceleration = (m_currentVelocity - m_lastVelocity) / Time.fixedDeltaTime;

            // 限制最大速度
            if (m_currentVelocity.magnitude > m_paddleData.MaxSpeed)
            {
                m_rigidbody.velocity = m_currentVelocity.normalized * m_paddleData.MaxSpeed;
            }
        }

        private void UpdateMovement()
        {
            // 应用速度
            m_rigidbody.velocity = m_velocity;

            // 限制最大速度
            if (m_rigidbody.velocity.magnitude > m_paddleData.MaxSpeed)
            {
                m_rigidbody.velocity = m_rigidbody.velocity.normalized * m_paddleData.MaxSpeed;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.TryGetComponent<BallPhysics>(out var ball))
            {
                var contact = collision.GetContact(0);
                var contactPoint = contact.point;
                var contactNormal = contact.normal;

                // 计算击球力度
                float hitForce = CalculateHitForce(ball.Velocity);

                // 应用碰撞力
                ball.ApplyCollisionForce(contactPoint, contactNormal, hitForce, HitType.Paddle);

                // 播放击球音效
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayPaddleHit(contactPoint, m_paddleData.HitVolume);
                }

                // 触发振动反馈
                TriggerHapticFeedback();
            }
        }

        private void TriggerHapticFeedback()
        {
            // 使用XR控制器进行振动反馈
            if (m_controller != null)
            {
                m_controller.SendHapticImpulse(0.5f, 0.1f);
            }
        }

        private float CalculateHitForce(Vector3 ballVelocity)
        {
            // 计算击球力度
            float hitForce = ballVelocity.magnitude * m_paddleData.HitMultiplier;
            return hitForce;
        }

        // 切换正反手
        public void SwitchSide()
        {
            m_isForehand = !m_isForehand;
        }

        // 设置球拍握持状态
        public void SetGripState(PaddleGripState newState)
        {
            m_gripState = newState;
            m_rigidbody.isKinematic = (newState == PaddleGripState.Anchored);
        }

        // 设置速度
        public void SetVelocity(Vector3 velocity)
        {
            m_velocity = velocity;
        }

        // 设置正手/反手
        public void SetForehand(bool isForehand)
        {
            m_isForehand = isForehand;
        }

        // 设置状态
        public void SetState(PaddleState newState)
        {
            m_currentState = newState;
            switch (newState)
            {
                case PaddleState.Free:
                    // 处理自由状态
                    break;
                case PaddleState.Grabbed:
                    // 处理抓取状态
                    break;
                case PaddleState.Throwing:
                    // 处理投掷状态
                    break;
            }
        }

        // 播放震动反馈
        public void PlayVibration(float intensity = 1f)
        {
            if (Time.time - m_lastVibrationTime < 0.1f)
                return;

            m_lastVibrationTime = Time.time;
            if (m_controller != null)
            {
                m_controller.SendHapticImpulse(intensity, 0.1f);
            }
        }

        // 属性
        public PaddleGripState GripState => m_gripState;
        public bool IsForehand => m_isForehand;
        public Vector3 CurrentVelocity => m_currentVelocity;
        public Vector3 Acceleration => m_acceleration;
        public float LastHitTime => m_lastHitTime;
        public Vector3 Velocity => m_rigidbody.velocity;
        public PaddleState CurrentState => m_currentState;
        public PaddleData PaddleData => m_paddleData;

        public async Task InitializeAsync()
        {
            await Task.Yield();
            SetState(PaddleState.Free);
        }
    }
}