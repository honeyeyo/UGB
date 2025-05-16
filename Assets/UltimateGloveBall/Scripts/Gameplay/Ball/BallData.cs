using UnityEngine;

namespace PongHub.Gameplay.Ball
{
    [CreateAssetMenu(fileName = "BallData", menuName = "PongHub/Ball Data")]
    public class BallData : ScriptableObject
    {
        [Header("物理参数")]
        public float Mass = 0.0027f;        // 标准乒乓球约2.7g
        public float Radius = 0.02f;        // 标准乒乓球直径40mm
        public float Bounce = 0.8f;         // 球的弹性
        public float Friction = 0.1f;       // 球的摩擦系数
        public float AirResistance = 0.1f;  // 空气阻力系数

        [Header("运动参数")]
        public float MaxSpeed = 20f;        // 最大速度
        public float MinSpeed = 2f;         // 最小速度
        public float MaxSpin = 100f;        // 最大旋转速度
        public Vector3 SpinDecay = new(0.95f, 0.95f, 0.95f); // 旋转衰减

        [Header("视觉效果")]
        public float TrailWidth = 0.01f;    // 拖尾宽度
        public float TrailTime = 0.5f;      // 拖尾时间
        public Color TrailColor = Color.white; // 拖尾颜色
        public float SpinVisualMultiplier = 1f; // 旋转视觉效果系数

        [Header("乒乓球特定参数")]
        public float PaddleHitMultiplier = 1.2f;  // 球拍击球系数
        public float TableHitMultiplier = 0.8f;   // 球桌击球系数
        public float NetHitMultiplier = 0.5f;     // 球网击球系数
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
            float speed = velocity.magnitude;
            float dragForce = 0.5f * AirResistance * GetSurfaceArea() * speed * speed;
            return -velocity.normalized * dragForce;
        }

        // 获取旋转衰减
        public Vector3 GetSpinDecay(Vector3 angularVelocity)
        {
            return Vector3.Scale(angularVelocity, SpinDecay);
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