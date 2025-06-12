using UnityEngine;

namespace PongHub.Gameplay.Paddle
{
    [CreateAssetMenu(fileName = "PaddleRubber", menuName = "PongHub/Paddle Rubber")]
    public class PaddleRubber : ScriptableObject
    {
        [Header("胶皮类型")]
        public string RubberName = "Default";
        public enum RubberType
        {
            Inverted,    // 反胶
            PipsOut,     // 正胶
            LongPips,    // 长胶
            AntiSpin     // 防弧
        }
        public RubberType Type = RubberType.Inverted;

        [Header("物理特性")]
        [Range(0f, 1f)]
        public float Hardness = 0.5f;        // 硬度
        [Range(0f, 1f)]
        public float SpongeThickness = 0.5f; // 海绵厚度
        [Range(0f, 1f)]
        public float Grip = 0.5f;            // 摩擦力

        [Header("性能参数")]
        [Range(0f, 2f)]
        public float Speed = 1f;             // 速度
        [Range(0f, 2f)]
        public float Spin = 1f;              // 旋转
        [Range(0f, 2f)]
        public float Control = 1f;           // 控制

        // 获取法向力修正系数
        public float GetNormalForceModifier()
        {
            float baseModifier = Speed * (1f + SpongeThickness);

            switch (Type)
            {
                case RubberType.Inverted:
                    return baseModifier * 1.2f;
                case RubberType.PipsOut:
                    return baseModifier * 0.9f;
                case RubberType.LongPips:
                    return baseModifier * 0.7f;
                case RubberType.AntiSpin:
                    return baseModifier * 0.5f;
                default:
                    return baseModifier;
            }
        }

        // 获取切向力修正系数
        public float GetTangentialForceModifier()
        {
            float baseModifier = Spin * Grip;

            switch (Type)
            {
                case RubberType.Inverted:
                    return baseModifier * 1.3f;
                case RubberType.PipsOut:
                    return baseModifier * 0.8f;
                case RubberType.LongPips:
                    return baseModifier * 0.6f;
                case RubberType.AntiSpin:
                    return baseModifier * 0.4f;
                default:
                    return baseModifier;
            }
        }

        // 获取控制修正系数
        public float GetControlModifier()
        {
            return Control * (1f - Hardness) * (1f + Grip);
        }
    }
}