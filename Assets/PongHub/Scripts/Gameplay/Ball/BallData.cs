using UnityEngine;

namespace PongHub.Ball
{
    [CreateAssetMenu(fileName = "BallData", menuName = "PongHub/Ball/BallData")]
    public class BallData : ScriptableObject
    {
        [Header("物理属性")]
        [SerializeField] private float m_mass = 0.0027f;
        [SerializeField] private float m_radius = 0.02f;
        [SerializeField] private float m_bounce = 0.8f;
        [SerializeField] private float m_friction = 0.1f;
        [SerializeField] private float m_drag = 0.1f;
        [SerializeField] private float m_angularDrag = 0.1f;
        [SerializeField] private float m_maxSpeed = 10f;
        [SerializeField] private float m_minSpeed = 0.5f;
        [SerializeField] private float m_maxSpin = 20f;
        [SerializeField] private float m_spinDecay = 0.95f;

        [Header("音效设置")]
        [SerializeField] private float m_paddleHitMultiplier = 1f;
        [SerializeField] private float m_tableHitMultiplier = 1f;
        [SerializeField] private float m_netHitMultiplier = 1f;

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