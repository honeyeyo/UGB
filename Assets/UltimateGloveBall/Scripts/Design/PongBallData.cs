using UnityEngine;

namespace PongHub.Design
{
    [CreateAssetMenu(fileName = "BallData", menuName = "PongHub/Ball Data")]
    public class BallData : ScriptableObject
    {
        [Header("物理参数")]
        public float Mass = 0.0027f;           // 球的质量(标准乒乓球2.7g)
        public float Bounce = 0.9f;            // 弹性系数
        public float Friction = 0.1f;          // 摩擦系数
        public float AirResistance = 0.1f;     // 空气阻力

        [Header("运动参数")]
        public float MaxSpeed = 20f;           // 最大速度
        public float MinSpeed = 2f;            // 最小速度
        public Vector3 SpinDecay = new(0.95f, 0.95f, 0.95f); // 旋转衰减

        [Header("视觉效果")]
        public float TrailWidth = 0.1f;        // 拖尾宽度
        public float TrailTime = 0.5f;         // 拖尾时间
        public Color TrailColor = Color.white; // 拖尾颜色
    }
}