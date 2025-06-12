using UnityEngine;

namespace PongHub.Gameplay.Paddle
{
    [CreateAssetMenu(fileName = "PaddleData", menuName = "PongHub/Paddle/PaddleData")]
    public class PaddleData : ScriptableObject
    {
        [Header("物理属性")]
        [SerializeField] private float m_mass = 0.17f;
        [SerializeField] private float m_drag = 0.1f;
        [SerializeField] private float m_bounce = 0.8f;
        [SerializeField] private float m_friction = 0.1f;
        [SerializeField] private float m_maxSpeed = 10f;
        [SerializeField] private float m_hitMultiplier = 1.0f;
        [SerializeField] private float m_hitVolume = 1.0f;

        [Header("视觉效果")]
        [SerializeField] private Color m_paddleColor = Color.red;
        [SerializeField] private float m_trailWidth = 0.01f;
        [SerializeField] private float m_trailTime = 0.5f;

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