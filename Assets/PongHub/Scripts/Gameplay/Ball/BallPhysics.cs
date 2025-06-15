using UnityEngine;
using System.Threading.Tasks;
using PongHub.Core;

namespace PongHub.Gameplay.Ball
{
    public enum BallState
    {
        None,
        Idle,
        Moving,
        Hit,
        OutOfBounds
    }

    [RequireComponent(typeof(Rigidbody))]
    public class BallPhysics : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private Rigidbody m_rigidbody;
        [SerializeField] private Collider m_collider;

        [Header("配置")]
        [SerializeField] private BallData m_ballData;

        // 物理状态
        private Vector3 m_velocity;
        private Vector3 m_angularVelocity;
        private HitType m_lastHitType;
        private float m_lastHitTime;
        private float m_lastHitForce;

        private BallState m_state;
        public BallState State => m_state;

        private void Awake()
        {
            if (m_rigidbody == null)
                m_rigidbody = GetComponent<Rigidbody>();
            if (m_collider == null)
                m_collider = GetComponent<Collider>();

            SetupRigidbody();
            SetupCollider();
        }

        private void SetupRigidbody()
        {
            m_rigidbody.mass = m_ballData.Mass;
            m_rigidbody.drag = m_ballData.Drag;
            m_rigidbody.angularDrag = m_ballData.AngularDrag;
            m_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            m_rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        private void SetupCollider()
        {
            m_collider.material = new PhysicMaterial("Ball")
            {
                bounciness = m_ballData.Bounce,
                dynamicFriction = m_ballData.Friction,
                staticFriction = m_ballData.Friction
            };
        }

        private void FixedUpdate()
        {
            // 更新物理状态
            m_velocity = m_rigidbody.velocity;
            m_angularVelocity = m_rigidbody.angularVelocity;

            // 应用空气阻力
            m_rigidbody.AddForce(m_ballData.GetAirResistance(m_velocity));

            // 应用旋转衰减
            m_rigidbody.AddTorque(m_ballData.GetSpinDecay(m_angularVelocity));

            // 限制最大速度
            if (m_rigidbody.velocity.magnitude > m_ballData.MaxSpeed)
            {
                m_rigidbody.velocity = m_rigidbody.velocity.normalized * m_ballData.MaxSpeed;
            }

            // 限制最小速度
            if (m_rigidbody.velocity.magnitude < m_ballData.MinSpeed)
            {
                m_rigidbody.velocity = Vector3.zero;
            }

            // 限制最大旋转
            if (m_rigidbody.angularVelocity.magnitude > m_ballData.MaxSpin)
            {
                m_rigidbody.angularVelocity = m_rigidbody.angularVelocity.normalized * m_ballData.MaxSpin;
            }
        }

        public void ApplyCollisionForce(Vector3 contactPoint, Vector3 contactNormal, float force, HitType hitType)
        {
            // 记录碰撞信息
            m_lastHitType = hitType;
            m_lastHitTime = Time.time;
            m_lastHitForce = force;

            // 计算反弹方向
            Vector3 reflectDir = Vector3.Reflect(m_rigidbody.velocity.normalized, contactNormal);

            // 应用反弹力
            m_rigidbody.velocity = reflectDir * force;

            // 应用旋转
            Vector3 spinAxis = Vector3.Cross(contactNormal, m_rigidbody.velocity);
            m_rigidbody.angularVelocity = spinAxis * force * m_ballData.SpinInfluence;

            // 播放碰撞音效
            if (AudioManager.Instance != null)
            {
                switch (hitType)
                {
                    case HitType.Paddle:
                        AudioManager.Instance.PlayPaddleHit(contactPoint, m_ballData.PaddleHitMultiplier);
                        break;
                    case HitType.Table:
                        AudioManager.Instance.PlayTableHit(contactPoint, m_ballData.TableHitMultiplier);
                        break;
                    case HitType.Net:
                        AudioManager.Instance.PlayNetHit(contactPoint, m_ballData.NetHitMultiplier);
                        break;
                }
            }
        }

        public void ResetBall()
        {
            m_rigidbody.velocity = Vector3.zero;
            m_rigidbody.angularVelocity = Vector3.zero;
            m_lastHitType = HitType.Table;
            m_lastHitTime = 0f;
            m_lastHitForce = 0f;
        }

        public void SetBallData(BallData data)
        {
            m_ballData = data;
            SetupRigidbody();
            SetupCollider();
        }

        public void SetVelocity(Vector3 velocity)
        {
            m_rigidbody.velocity = velocity;
        }

        public void SetAngularVelocity(Vector3 angularVelocity)
        {
            m_rigidbody.angularVelocity = angularVelocity;
        }

        // 属性
        public Vector3 Velocity => m_rigidbody.velocity;
        public Vector3 AngularVelocity => m_rigidbody.angularVelocity;
        public HitType LastHitType => m_lastHitType;
        public float LastHitTime => m_lastHitTime;
        public float LastHitForce => m_lastHitForce;
        public BallData BallData => m_ballData;

        public void SetState(BallState newState)
        {
            m_state = newState;
            switch (newState)
            {
                case BallState.Idle:
                    // 处理空闲状态
                    break;
                case BallState.Moving:
                    // 处理移动状态
                    break;
                case BallState.Hit:
                    // 处理击中状态
                    break;
                case BallState.OutOfBounds:
                    // 处理出界状态
                    break;
            }
        }

        public void PlayVibration(float intensity)
        {
            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayVibration(intensity);
            }
        }

        public async Task InitializeAsync()
        {
            await Task.Yield();
            SetState(BallState.Idle);
        }
    }
}