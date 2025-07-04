using UnityEngine;

namespace PongHub.Gameplay.Ball
{
    [CreateAssetMenu(fileName = "BallData", menuName = "PongHub/Ball/BallData")]
    public class BallData : ScriptableObject
    {
        [Header("物理属性")]
        [SerializeField]
        [Tooltip("Mass / 质量 - Mass of the ball for physics calculations")]
        private float m_mass = 0.0027f;

        [SerializeField]
        [Tooltip("Radius / 半径 - Radius of the ball")]
        private float m_radius = 0.02f;

        [SerializeField]
        [Tooltip("Bounce / 弹性 - Bounce coefficient when ball hits surfaces")]
        private float m_bounce = 0.8f;

        [SerializeField]
        [Tooltip("Friction / 摩擦力 - Friction coefficient for ball movement")]
        private float m_friction = 0.1f;

        [SerializeField]
        [Tooltip("Drag / 阻力 - Air resistance coefficient for the ball")]
        private float m_drag = 0.1f;

        [SerializeField]
        [Tooltip("Angular Drag / 角阻力 - Angular drag coefficient for ball rotation")]
        private float m_angularDrag = 0.1f;

        [SerializeField]
        [Tooltip("Max Speed / 最大速度 - Maximum speed limit for ball movement")]
        private float m_maxSpeed = 10f;

        [SerializeField]
        [Tooltip("Min Speed / 最小速度 - Minimum speed threshold for ball movement")]
        private float m_minSpeed = 0.5f;

        [SerializeField]
        [Tooltip("Max Spin / 最大旋转 - Maximum spin limit for ball rotation")]
        private float m_maxSpin = 20f;

        [SerializeField]
        [Tooltip("Spin Decay / 旋转衰减 - Spin decay coefficient over time")]
        private float m_spinDecay = 0.95f;

        [Header("音效设置")]
        [SerializeField]
        [Tooltip("Paddle Hit Multiplier / 球拍击球倍数 - Sound multiplier when ball hits paddle")]
        private float m_paddleHitMultiplier = 1f;

        [SerializeField]
        [Tooltip("Table Hit Multiplier / 球桌击球倍数 - Sound multiplier when ball hits table")]
        private float m_tableHitMultiplier = 1f;

        [SerializeField]
        [Tooltip("Net Hit Multiplier / 球网击球倍数 - Sound multiplier when ball hits net")]
        private float m_netHitMultiplier = 1f;

        // 物理属性
        public float Mass => m_mass;
        public float Radius => m_radius;
        public float Bounce => m_bounce;
        public float Friction => m_friction;
        public float Drag => m_drag;
        public float AngularDrag => m_angularDrag;
        public float MaxSpeed => m_maxSpeed;
        public float MinSpeed => m_minSpeed;
        public float MaxSpin => m_maxSpin;
        public float SpinDecay => m_spinDecay;

        // 音效设置
        public float PaddleHitMultiplier => m_paddleHitMultiplier;
        public float TableHitMultiplier => m_tableHitMultiplier;
        public float NetHitMultiplier => m_netHitMultiplier;

        [Header("视觉效果")]
        public float TrailWidth = 0.01f;    // 拖尾宽度
        public float TrailTime = 0.5f;      // 拖尾时间
        public Color TrailColor = Color.white; // 拖尾颜色
        public float SpinVisualMultiplier = 1f; // 旋转视觉效果系数

        [Header("乒乓球特定参数")]
        public float SpinInfluence = 1.5f;        // 旋转影响系数

        // 获取球的体积
        public float GetVolume()
        {
            return (4f / 3f) * Mathf.PI * Mathf.Pow(Radius, 3);
        }

        // 获取球的表面积
        public float GetSurfaceArea()
        {
            return 4f * Mathf.PI * Mathf.Pow(Radius, 2);
        }

        // 获取空气阻力
        public Vector3 GetAirResistance(Vector3 velocity)
        {
            return -velocity.normalized * Drag * velocity.sqrMagnitude;
        }

        // 获取旋转衰减
        public Vector3 GetSpinDecay(Vector3 angularVelocity)
        {
            return angularVelocity * SpinDecay;
        }

        // 获取击球系数
        public float GetHitMultiplier(HitType hitType)
        {
            switch (hitType)
            {
                case HitType.Paddle:
                    return PaddleHitMultiplier;
                case HitType.Table:
                    return TableHitMultiplier;
                case HitType.Net:
                    return NetHitMultiplier;
                default:
                    return 1f;
            }
        }
    }

    // 击球类型
    public enum HitType
    {
        Paddle, // 球拍击球
        Table,  // 球桌击球
        Net     // 球网击球
    }
}