using UnityEngine;
using PongHub.Core;

namespace PongHub.Gameplay.Ball
{
    [RequireComponent(typeof(Rigidbody))]
    public class BallPhysics : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private Rigidbody m_rigidbody;
        [SerializeField] private SphereCollider m_collider;

        [Header("配置")]
        [SerializeField] private BallData m_ballData;

        // 物理状态
        private Vector3 m_lastVelocity;
        private Vector3 m_lastAngularVelocity;
        private HitType m_lastHitType;
        private float m_lastHitTime;

        private void Awake()
        {
            if (m_rigidbody == null)
                m_rigidbody = GetComponent<Rigidbody>();
            if (m_collider == null)
                m_collider = GetComponent<SphereCollider>();

            SetupRigidbody();
            SetupCollider();
        }

        private void SetupRigidbody()
        {
            m_rigidbody.mass = m_ballData.Mass;
            m_rigidbody.drag = 0f;  // 使用自定义空气阻力
            m_rigidbody.angularDrag = 0.05f;
            m_rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            m_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            m_rigidbody.useGravity = true;
        }

        private void SetupCollider()
        {
            m_collider.radius = m_ballData.Radius;
            m_collider.material = new PhysicMaterial("Ball")
            {
                bounciness = m_ballData.Bounce,
                dynamicFriction = m_ballData.Friction,
                staticFriction = m_ballData.Friction
            };
        }

        private void FixedUpdate()
        {
            // 记录上一帧的状态
            m_lastVelocity = m_rigidbody.velocity;
            m_lastAngularVelocity = m_rigidbody.angularVelocity;

            // 应用空气阻力
            ApplyAirResistance();

            // 限制最大速度
            LimitMaxSpeed();

            // 限制最小速度
            LimitMinSpeed();

            // 限制最大旋转
            LimitMaxSpin();

            // 应用旋转衰减
            ApplySpinDecay();
        }

        private void ApplyAirResistance()
        {
            var airResistance = m_ballData.GetAirResistance(m_rigidbody.velocity);
            m_rigidbody.AddForce(airResistance, ForceMode.Force);
        }

        private void LimitMaxSpeed()
        {
            if (m_rigidbody.velocity.magnitude > m_ballData.MaxSpeed)
            {
                m_rigidbody.velocity = m_rigidbody.velocity.normalized * m_ballData.MaxSpeed;
            }
        }

        private void LimitMinSpeed()
        {
            if (m_rigidbody.velocity.magnitude < m_ballData.MinSpeed && m_rigidbody.velocity.magnitude > 0.1f)
            {
                m_rigidbody.velocity = m_rigidbody.velocity.normalized * m_ballData.MinSpeed;
            }
        }

        private void LimitMaxSpin()
        {
            if (m_rigidbody.angularVelocity.magnitude > m_ballData.MaxSpin)
            {
                m_rigidbody.angularVelocity = m_rigidbody.angularVelocity.normalized * m_ballData.MaxSpin;
            }
        }

        private void ApplySpinDecay()
        {
            m_rigidbody.angularVelocity = m_ballData.GetSpinDecay(m_rigidbody.angularVelocity);
        }

        // 应用碰撞力
        public void ApplyCollisionForce(Vector3 contactPoint, Vector3 contactNormal, float force, HitType hitType = HitType.Table)
        {
            // 计算碰撞力
            var impactForce = contactNormal * force * m_ballData.GetHitMultiplier(hitType);

            // 应用力
            m_rigidbody.AddForceAtPosition(impactForce, contactPoint, ForceMode.Impulse);

            // 记录碰撞信息
            m_lastHitType = hitType;
            m_lastHitTime = Time.time;

            // 播放碰撞音效
            PlayCollisionSound(hitType);
        }

        private void PlayCollisionSound(HitType hitType)
        {
            if (AudioManager.Instance != null)
            {
                switch (hitType)
                {
                    case HitType.Paddle:
                        AudioManager.Instance.PlayPaddleHit();
                        break;
                    case HitType.Table:
                        AudioManager.Instance.PlayTableHit(m_ballData.TableHitMultiplier);
                        break;
                    case HitType.Net:
                        AudioManager.Instance.PlayNetHit(m_ballData.NetHitMultiplier);
                        break;
                }
            }
        }

        // 获取上一帧速度
        public Vector3 GetLastVelocity()
        {
            return m_lastVelocity;
        }

        // 获取上一帧角速度
        public Vector3 GetLastAngularVelocity()
        {
            return m_lastAngularVelocity;
        }

        // 获取最后碰撞类型
        public HitType GetLastHitType()
        {
            return m_lastHitType;
        }

        // 获取最后碰撞时间
        public float GetLastHitTime()
        {
            return m_lastHitTime;
        }

        // 重置球的状态
        public void ResetBall()
        {
            m_rigidbody.velocity = Vector3.zero;
            m_rigidbody.angularVelocity = Vector3.zero;
            m_lastVelocity = Vector3.zero;
            m_lastAngularVelocity = Vector3.zero;
            m_lastHitType = HitType.Table;
            m_lastHitTime = 0f;
        }

        // 属性
        public Vector3 Velocity => m_rigidbody.velocity;
        public Vector3 AngularVelocity => m_rigidbody.angularVelocity;
        public BallData BallData => m_ballData;
    }
}