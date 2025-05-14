using UnityEngine;

namespace PongHub.Design
{
    [CreateAssetMenu(fileName = "PaddleData", menuName = "PongHub/Paddle Data")]
    public class PaddleData : ScriptableObject
    {
        [Header("物理参数")]
        public float BounceForce = 15f;        // 击球力度
        public float MaxBounceForce = 30f;     // 最大击球力度
        public float SpinMultiplier = 1.5f;    // 旋转系数
        public float DampingFactor = 0.8f;     // 阻尼系数

        [Header("控制参数")]
        public float SwingSpeed = 10f;         // 挥拍速度
        public float MaxSwingSpeed = 20f;      // 最大挥拍速度
        public float RotationSpeed = 180f;     // 旋转速度

        [Header("特效参数")]
        public float HitEffectIntensity = 1f;  // 击球特效强度
        public float TrailDuration = 0.5f;     // 拖尾持续时间
        public Color TrailColor = Color.white; // 拖尾颜色
    }
}