using UnityEngine;
using PongHub.Gameplay.Ball;
using PongHub.Core;

namespace PongHub.Gameplay.Paddle
{
    [RequireComponent(typeof(Rigidbody))]
    public class Paddle : MonoBehaviour
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
            Inactive,   // 非活动状态
            Active,     // 活动状态
            Disabled    // 禁用状态
        }

        [Header("组件引用")]
        [SerializeField] private Rigidbody m_rigidbody;
        [SerializeField] private MeshRenderer m_renderer;
        [SerializeField] private Collider m_collider;
        [SerializeField] private PaddleRubber m_forehandRubber;  // 正手胶皮
        [SerializeField] private PaddleRubber m_backhandRubber;  // 反手胶皮
        [SerializeField] private PaddleBlade m_blade;           // 底板

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
        private PaddleState m_state;
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
            m_rigidbody.drag = m_paddleData.AirResistance;
            m_rigidbody.angularDrag = m_paddleData.AirResistance * 2f;
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
            m_renderer.material.color = m_paddleData.Color;
        }

        private void FixedUpdate()
        {
            if (m_gripState == PaddleGripState.Anchored)
            {
                UpdateMotionState();
            }
            if (m_state == PaddleState.Active)
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
                HandleBallCollision(collision, ball);
            }
        }

        private void HandleBallCollision(Collision collision, BallPhysics ball)
        {
            // 计算碰撞信息
            var contact = collision.GetContact(0);
            var contactPoint = contact.point;
            var contactNormal = contact.normal;
            var relativeVelocity = m_currentVelocity - ball.Velocity;

            // 计算击球力
            var impactForce = CalculateImpactForce(relativeVelocity, contactNormal);

            // 应用击球力
            ball.ApplyCollisionForce(contactPoint, contactNormal, impactForce, GetHitMultiplier());

            // 播放击球音效
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPaddleHit();
            }

            // 更新击球时间
            m_lastHitTime = Time.time;
        }

        private Vector3 CalculateImpactForce(Vector3 relativeVelocity, Vector3 contactNormal)
        {
            // 获取当前使用的胶皮
            var rubber = m_isForehand ? m_forehandRubber : m_backhandRubber;

            // 计算法向力和切向力
            var normalForce = Vector3.Dot(relativeVelocity, contactNormal) * contactNormal;
            var tangentialForce = relativeVelocity - normalForce;

            // 计算旋转影响
            var spinInfluence = CalculateSpinInfluence(relativeVelocity, contactNormal);

            // 结合胶皮和底板的物理属性计算最终力
            var normalModifier = rubber.GetNormalForceModifier() * m_blade.GetNormalForceModifier();
            var tangentialModifier = rubber.GetTangentialForceModifier() * m_blade.GetTangentialForceModifier();

            return (normalForce * normalModifier + tangentialForce * tangentialModifier) * spinInfluence;
        }

        private float CalculateSpinInfluence(Vector3 relativeVelocity, Vector3 contactNormal)
        {
            // 计算击球方向与垂直方向的夹角
            float verticalAngle = Vector3.Angle(relativeVelocity, Vector3.up);

            // 根据击球角度确定旋转类型
            if (verticalAngle < 45f)
            {
                // 上旋
                return m_paddleData.TopspinMultiplier;
            }
            else if (verticalAngle > 135f)
            {
                // 下旋
                return m_paddleData.BackspinMultiplier;
            }
            else
            {
                // 侧旋
                return m_paddleData.SidespinMultiplier;
            }
        }

        private float GetHitMultiplier()
        {
            // 基础力量系数
            float basePower = m_isForehand ? m_paddleData.ForehandPower : m_paddleData.BackhandPower;

            // 检查是否是扣杀
            if (m_currentVelocity.magnitude > m_paddleData.MaxSpeed * 0.8f)
            {
                return basePower * m_paddleData.SmashMultiplier;
            }

            return basePower;
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
        public void SetState(PaddleState state)
        {
            m_state = state;
            if (state == PaddleState.Inactive)
            {
                m_rigidbody.velocity = Vector3.zero;
                m_velocity = Vector3.zero;
            }
        }

        // 播放震动反馈
        public void PlayVibration(float intensity = 1f)
        {
            if (Time.time - m_lastVibrationTime < 0.1f)
                return;

            m_lastVibrationTime = Time.time;
            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayVibration(intensity);
            }
        }

        // 属性
        public PaddleGripState GripState => m_gripState;
        public bool IsForehand => m_isForehand;
        public Vector3 CurrentVelocity => m_currentVelocity;
        public Vector3 Acceleration => m_acceleration;
        public float LastHitTime => m_lastHitTime;
        public Vector3 Velocity => m_rigidbody.velocity;
        public PaddleState State => m_state;
        public PaddleData PaddleData => m_paddleData;
    }

    
}