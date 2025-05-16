using UnityEngine;

namespace PongHub.Gameplay.Paddle
{
    [CreateAssetMenu(fileName = "PaddleData", menuName = "PongHub/Paddle Data")]
    public class PaddleData : ScriptableObject
    {
        [Header("物理参数")]
        public float Mass = 0.17f;           // 标准乒乓球拍约170g
        public float Bounce = 0.8f;          // 球拍弹性
        public float Friction = 0.2f;        // 球拍摩擦系数

        [Header("运动参数")]
        public float MaxSpeed = 20f;         // 最大速度
        public float MinSpeed = 2f;          // 最小速度
        public Vector3 SpinDecay = new(0.95f, 0.95f, 0.95f); // 旋转衰减

        [Header("视觉效果")]
        public float TrailWidth = 0.1f;      // 拖尾宽度
        public float TrailTime = 0.5f;       // 拖尾时间
        public Color TrailColor = Color.white; // 拖尾颜色

        [Header("乒乓球拍特定参数")]
        public float ForehandPower = 1.2f;   // 正手力量系数
        public float BackhandPower = 1.0f;   // 反手力量系数
        public float TopspinMultiplier = 1.3f;  // 上旋系数
        public float BackspinMultiplier = 0.8f; // 下旋系数
        public float SidespinMultiplier = 1.1f; // 侧旋系数
        public float SmashMultiplier = 1.5f;    // 扣杀系数
    }
}