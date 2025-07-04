using UnityEngine;

namespace PongHub.Gameplay.Paddle
{
    [CreateAssetMenu(fileName = "PaddleData", menuName = "PongHub/Paddle/PaddleData")]
    public class PaddleData : ScriptableObject
    {
        [Header("物理属性")]
        [SerializeField]
        [Tooltip("Mass / 质量 - Mass of the paddle for physics calculations")]
        private float m_mass = 0.17f;
        [SerializeField]
        [Tooltip("Drag / 阻力 - Air resistance coefficient for the paddle")]
        private float m_drag = 0.1f;
        [SerializeField]
        [Tooltip("Bounce / 弹性 - Bounce coefficient when ball hits paddle")]
        private float m_bounce = 0.8f;
        [SerializeField]
        [Tooltip("Friction / 摩擦力 - Friction coefficient for paddle movement")]
        private float m_friction = 0.1f;
        [SerializeField]
        [Tooltip("Max Speed / 最大速度 - Maximum speed limit for paddle movement")]
        private float m_maxSpeed = 10f;
        [SerializeField]
        [Tooltip("Hit Multiplier / 击球倍数 - Multiplier for ball speed when hitting paddle")]
        private float m_hitMultiplier = 1.0f;
        [SerializeField]
        [Tooltip("Hit Volume / 击球音量 - Volume level for paddle hit sound")]
        private float m_hitVolume = 1.0f;

        [Header("视觉效果")]
        [SerializeField]
        [Tooltip("Paddle Color / 球拍颜色 - Color of the paddle for visual representation")]
        private Color m_paddleColor = Color.red;
        [SerializeField]
        [Tooltip("Trail Width / 轨迹宽度 - Width of the paddle movement trail")]
        private float m_trailWidth = 0.01f;
        [SerializeField]
        [Tooltip("Trail Time / 轨迹时间 - Duration of the paddle movement trail")]
        private float m_trailTime = 0.5f;

        // 物理属性
        public float Mass => m_mass;
        public float Drag => m_drag;
        public float Bounce => m_bounce;
        public float Friction => m_friction;
        public float MaxSpeed => m_maxSpeed;
        public float HitMultiplier => m_hitMultiplier;
        public float HitVolume => m_hitVolume;

        // 视觉效果
        public Color PaddleColor => m_paddleColor;
        public float TrailWidth => m_trailWidth;
        public float TrailTime => m_trailTime;

        [Header("运动参数")]
        public float MinSpeed = 2f;          // 最小速度
        public Vector3 SpinDecay = new(0.95f, 0.95f, 0.95f); // 旋转衰减

        [Header("乒乓球拍特定参数")]
        public float ForehandPower = 1.2f;   // 正手力量系数
        public float BackhandPower = 1.0f;   // 反手力量系数
        public float TopspinMultiplier = 1.3f;  // 上旋系数
        public float BackspinMultiplier = 0.8f; // 下旋系数
        public float SidespinMultiplier = 1.1f; // 侧旋系数
        public float SmashMultiplier = 1.5f;    // 扣杀系数
    }
}