using UnityEngine;

namespace PongHub.Design
{
    [CreateAssetMenu(fileName = "PaddleData", menuName = "PongHub/Paddle Data")]
    public class PaddleData : ScriptableObject
    {
        [Header("基础参数")]
        [Tooltip("球拍名称")]
        public string PaddleName = "Standard Paddle";
        [Tooltip("球拍描述")]
        public string Description = "Standard table tennis paddle";

        [Header("物理参数")]
        [Tooltip("击球力度系数")]
        [Range(0.5f, 2.0f)]
        public float BounceForce = 1.0f;

        [Tooltip("最大击球力度")]
        [Range(10f, 40f)]
        public float MaxBounceForce = 20f;

        [Tooltip("旋转系数")]
        [Range(0.5f, 2.0f)]
        public float SpinMultiplier = 1.0f;

        [Tooltip("阻尼系数")]
        [Range(0.1f, 1.0f)]
        public float DampingFactor = 0.8f;

        [Header("拍面参数")]
        [Tooltip("拍面硬度")]
        [Range(0.1f, 1.0f)]
        public float SurfaceHardness = 0.5f;

        [Tooltip("拍面摩擦系数")]
        [Range(0.1f, 1.0f)]
        public float SurfaceFriction = 0.5f;

        [Tooltip("拍面弹性")]
        [Range(0.1f, 1.0f)]
        public float SurfaceBounce = 0.5f;

        [Header("控制参数")]
        [Tooltip("挥拍速度")]
        [Range(5f, 20f)]
        public float SwingSpeed = 10f;

        [Tooltip("最大挥拍速度")]
        [Range(10f, 30f)]
        public float MaxSwingSpeed = 20f;

        [Tooltip("旋转速度")]
        [Range(90f, 360f)]
        public float RotationSpeed = 180f;

        [Header("特效参数")]
        [Tooltip("击球特效强度")]
        [Range(0.1f, 2.0f)]
        public float HitEffectIntensity = 1.0f;

        [Tooltip("拖尾持续时间")]
        [Range(0.1f, 1.0f)]
        public float TrailDuration = 0.5f;

        [Tooltip("拖尾颜色")]
        public Color TrailColor = Color.white;

        [Header("声音参数")]
        [Tooltip("击球音量")]
        [Range(0.1f, 1.0f)]
        public float HitVolume = 0.8f;

        [Tooltip("击球音调")]
        [Range(0.5f, 1.5f)]
        public float HitPitch = 1.0f;
    }
}