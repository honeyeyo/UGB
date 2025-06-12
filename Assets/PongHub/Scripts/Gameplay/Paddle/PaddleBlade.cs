using UnityEngine;

namespace PongHub.Gameplay.Paddle
{
    [CreateAssetMenu(fileName = "PaddleBlade", menuName = "PongHub/Paddle Blade")]
    public class PaddleBlade : ScriptableObject
    {
        [Header("底板类型")]
        public string BladeName = "Default";
        public enum BladeType
        {
            AllRound,    // 全面型
            Offensive,   // 进攻型
            Defensive,   // 防守型
            Fast        // 快攻型
        }
        public BladeType Type = BladeType.AllRound;

        [Header("物理特性")]
        [Range(0f, 1f)]
        public float Stiffness = 0.5f;      // 硬度
        [Range(0f, 1f)]
        public float Thickness = 0.5f;      // 厚度
        [Range(0f, 1f)]
        public float Weight = 0.5f;         // 重量

        [Header("性能参数")]
        [Range(0f, 2f)]
        public float Speed = 1f;            // 速度
        [Range(0f, 2f)]
        public float Control = 1f;          // 控制
        [Range(0f, 2f)]
        public float Vibration = 1f;        // 震动

        // 获取法向力修正系数
        public float GetNormalForceModifier()
        {
            float baseModifier = Speed * (1f + Thickness);

            switch (Type)
            {
                case BladeType.AllRound:
                    return baseModifier * 1.0f;
                case BladeType.Offensive:
                    return baseModifier * 1.3f;
                case BladeType.Defensive:
                    return baseModifier * 0.7f;
                case BladeType.Fast:
                    return baseModifier * 1.2f;
                default:
                    return baseModifier;
            }
        }

        // 获取切向力修正系数
        public float GetTangentialForceModifier()
        {
            float baseModifier = Control * (1f - Stiffness);

            switch (Type)
            {
                case BladeType.AllRound:
                    return baseModifier * 1.0f;
                case BladeType.Offensive:
                    return baseModifier * 0.8f;
                case BladeType.Defensive:
                    return baseModifier * 1.3f;
                case BladeType.Fast:
                    return baseModifier * 0.9f;
                default:
                    return baseModifier;
            }
        }

        // 获取震动反馈系数
        public float GetVibrationModifier()
        {
            return Vibration * (1f + Stiffness) * (1f - Weight);
        }

        // 获取重量修正系数
        public float GetWeightModifier()
        {
            return Weight * (1f + Thickness);
        }
    }
}