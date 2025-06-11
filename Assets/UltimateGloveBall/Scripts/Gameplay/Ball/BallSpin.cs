// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using UnityEngine;

namespace PongHub.Ball
{
    /// <summary>
    /// 乒乓球旋转系统 - 实现真实的乒乓球旋转物理效果
    /// 包含马格努斯力、旋转衰减、旋转可视化等功能
    /// </summary>
    public class BallSpin : MonoBehaviour
    {
        #region Serialized Fields
        [Header("旋转物理 - ITTF标准参数")]
        [SerializeField] private float spinDecayRate = 0.85f;         // 旋转衰减率（15-20%/秒）
        [SerializeField] private float magnusForceMultiplier = 2.1f;  // 马格努斯力系数（基于真实CAP球）
        [SerializeField] private float maxSpinRate = 157f;            // 最大旋转速率（约1500 RPM）
        [SerializeField] private float minSpinThreshold = 0.5f;       // 最小旋转阈值（提高精度）

        [Header("物理常量 - 基于ITTF标准")]
        [SerializeField] private float ballMass = 0.0027f;            // 球质量（2.7g）
        [SerializeField] private float ballRadius = 0.02f;            // 球半径（20mm）
        [SerializeField] private float airDensity = 1.225f;           // 空气密度
        [SerializeField] private float magnusCoefficient = 0.42f;     // 马格努斯系数

        [Header("旋转可视化")]
        [SerializeField] private bool showSpinVisualization = true;   // 显示旋转可视化
        [SerializeField] private ParticleSystem spinTrailEffect;     // 旋转轨迹特效
        [SerializeField] private LineRenderer spinAxisIndicator;     // 旋转轴指示器
        [SerializeField] private Material spinVisualizationMaterial; // 旋转可视化材质

        [Header("音效")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip spinSoundClip;            // 旋转音效
        [SerializeField] private float spinSoundVolumeMultiplier = 0.5f;
        #endregion

        #region Private Fields
        private Rigidbody ballRigidbody;
        private Vector3 spinAxis = Vector3.zero;
        private float spinRate = 0f;
        private Vector3 lastVelocity = Vector3.zero;
        private bool isSpinning = false;
        private float spinSoundTimer = 0f;
        private const float SPIN_SOUND_INTERVAL = 0.1f;
        #endregion

        #region Properties
        public Vector3 SpinAxis => spinAxis;
        public float SpinRate => spinRate;
        public bool IsSpinning => isSpinning && spinRate > minSpinThreshold;
        public float SpinIntensity => spinRate / maxSpinRate; // 0-1之间的旋转强度
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            ballRigidbody = GetComponent<Rigidbody>();
            if (ballRigidbody == null)
            {
                Debug.LogError("BallSpin: 未找到Rigidbody组件");
            }

            // 初始化音效组件
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            // 初始化可视化组件
            InitializeVisualization();
        }

        private void Start()
        {
            lastVelocity = ballRigidbody != null ? ballRigidbody.velocity : Vector3.zero;
        }

        private void FixedUpdate()
        {
            if (ballRigidbody == null) return;

            // 应用马格努斯力
            ApplyMagnusForce();

            // 旋转衰减
            DecaySpin();

            // 更新可视化
            UpdateVisualization();

            // 更新音效
            UpdateSpinAudio();

            lastVelocity = ballRigidbody.velocity;
        }

        private void Update()
        {
            // 更新旋转轴指示器（如果启用）
            if (showSpinVisualization && spinAxisIndicator != null)
            {
                UpdateSpinAxisIndicator();
            }
        }
        #endregion

        #region Magnus Force Physics
        /// <summary>
        /// 应用马格努斯力（旋转球的偏转力）
        /// 使用真实物理公式：F_magnus = 0.5 * ρ * A * Cl * r * |v| * (ω × v)
        /// </summary>
        private void ApplyMagnusForce()
        {
            if (spinRate <= minSpinThreshold || ballRigidbody == null) return;

            Vector3 velocity = ballRigidbody.velocity;
            if (velocity.magnitude < 0.1f) return;

            // 计算球的截面积：A = π * r²
            float crossSectionalArea = Mathf.PI * ballRadius * ballRadius;

            // 计算马格努斯力：F = 0.5 * ρ * A * Cl * r * |v| * (ω × v)
            Vector3 angularVelocityVector = spinAxis * spinRate;
            Vector3 magnusDirection = Vector3.Cross(angularVelocityVector, velocity);

            float magnusForceMagnitude = 0.5f * airDensity * crossSectionalArea *
                                       magnusCoefficient * ballRadius * velocity.magnitude;

            Vector3 magnusForce = magnusDirection.normalized * magnusForceMagnitude * magnusForceMultiplier;

            // 限制马格努斯力的最大值（防止非现实的效果）
            float maxMagnusForce = ballMass * 30f; // 最大3g的加速度
            if (magnusForce.magnitude > maxMagnusForce)
            {
                magnusForce = magnusForce.normalized * maxMagnusForce;
            }

            // 应用力
            ballRigidbody.AddForce(magnusForce, ForceMode.Force);

            // 调试信息
            if (showSpinVisualization)
            {
                Debug.DrawRay(transform.position, magnusForce.normalized * 0.5f, Color.red, 0.02f);
                Debug.DrawRay(transform.position, spinAxis * 0.3f, Color.blue, 0.02f);
                Debug.DrawRay(transform.position, velocity.normalized * 0.4f, Color.green, 0.02f);
            }
        }

        /// <summary>
        /// 旋转衰减 - 使用指数衰减公式
        /// 基于ITTF标准：空气中约15-20%/秒的衰减率
        /// </summary>
        private void DecaySpin()
        {
            if (spinRate > 0)
            {
                // 使用指数衰减公式：ω_next = ω * exp(-k * dt)
                // 其中k为衰减常数，对应15-20%/秒的衰减率
                float decayConstant = -Mathf.Log(spinDecayRate); // 将衰减率转换为衰减常数
                spinRate *= Mathf.Exp(-decayConstant * Time.fixedDeltaTime);

                // 当旋转速率低于阈值时停止旋转
                if (spinRate < minSpinThreshold)
                {
                    spinRate = 0f;
                    isSpinning = false;
                    spinAxis = Vector3.zero;
                }
            }
        }
        #endregion

        #region Spin Control
        /// <summary>
        /// 添加旋转（击球时调用）
        /// </summary>
        /// <param name="axis">旋转轴</param>
        /// <param name="rate">旋转速率</param>
        public void AddSpin(Vector3 axis, float rate)
        {
            if (axis == Vector3.zero || rate <= 0) return;

            // 限制旋转速率
            rate = Mathf.Clamp(rate, 0, maxSpinRate);

            // 如果当前有旋转，则合并旋转
            if (isSpinning && spinRate > minSpinThreshold)
            {
                // 计算合成旋转
                Vector3 currentSpin = spinAxis * spinRate;
                Vector3 newSpin = axis.normalized * rate;
                Vector3 combinedSpin = currentSpin + newSpin;

                spinAxis = combinedSpin.normalized;
                spinRate = Mathf.Min(combinedSpin.magnitude, maxSpinRate);
            }
            else
            {
                // 设置新旋转
                spinAxis = axis.normalized;
                spinRate = rate;
            }

            isSpinning = spinRate > minSpinThreshold;

            Debug.Log($"添加旋转 - 轴: {spinAxis:F3}, 速率: {spinRate:F2}");
        }

        /// <summary>
        /// 设置旋转（直接设置，用于网络同步）
        /// </summary>
        /// <param name="axis">旋转轴</param>
        /// <param name="rate">旋转速率</param>
        public void SetSpin(Vector3 axis, float rate)
        {
            spinAxis = axis.normalized;
            spinRate = Mathf.Clamp(rate, 0, maxSpinRate);
            isSpinning = spinRate > minSpinThreshold;
        }

        /// <summary>
        /// 重置旋转
        /// </summary>
        public void ResetSpin()
        {
            spinAxis = Vector3.zero;
            spinRate = 0f;
            isSpinning = false;

            // 停止特效
            if (spinTrailEffect != null && spinTrailEffect.isPlaying)
            {
                spinTrailEffect.Stop();
            }
        }

        /// <summary>
        /// 从碰撞计算旋转
        /// </summary>
        /// <param name="collision">碰撞信息</param>
        /// <param name="paddleVelocity">球拍速度</param>
        public void CalculateSpinFromCollision(Collision collision, Vector3 paddleVelocity)
        {
            if (collision.contacts.Length == 0) return;

            ContactPoint contact = collision.contacts[0];
            Vector3 contactNormal = contact.normal;
            Vector3 relativeVelocity = ballRigidbody.velocity - paddleVelocity;

            // 获取碰撞物体的材质信息
            float frictionMultiplier = GetMaterialFrictionMultiplier(collision.collider);
            float spinEfficiency = GetMaterialSpinEfficiency(collision.collider);

            // 计算切向速度（产生旋转的部分）
            Vector3 tangentialVelocity = relativeVelocity - Vector3.Dot(relativeVelocity, contactNormal) * contactNormal;

            if (tangentialVelocity.magnitude > 0.1f)
            {
                // 计算旋转轴（垂直于切向速度和法向量）
                Vector3 newSpinAxis = Vector3.Cross(tangentialVelocity.normalized, contactNormal).normalized;

                // 基于材质调整旋转强度
                float baseSpinRate = tangentialVelocity.magnitude * 2f;
                float adjustedSpinRate = baseSpinRate * frictionMultiplier * spinEfficiency;

                AddSpin(newSpinAxis, adjustedSpinRate);

                Debug.Log($"材质碰撞旋转 - 基础:{baseSpinRate:F2}, 摩擦倍数:{frictionMultiplier:F2}, 效率:{spinEfficiency:F2}, 最终:{adjustedSpinRate:F2}");
            }
        }

        /// <summary>
        /// 获取材质的摩擦倍数
        /// </summary>
        private float GetMaterialFrictionMultiplier(Collider collider)
        {
            string materialName = GetPhysicsMaterialName(collider);

            return materialName switch
            {
                "PhyRubber" => 2.0f,      // 橡胶拍面：最强摩擦
                "PhyBlade" => 1.0f,       // 木质刀片：标准摩擦
                "PhyWood" => 0.6f,        // 球桌台面：中等摩擦
                "PhyNet" => 1.5f,         // 球网：高摩擦但低转换
                "PhyMetalSolid" => 0.3f,  // 金属支架：低摩擦
                "PhyCAP" => 0.1f,         // 球与球碰撞：极低摩擦
                _ => 1.0f                 // 默认标准摩擦
            };
        }

        /// <summary>
        /// 获取材质的旋转效率
        /// </summary>
        private float GetMaterialSpinEfficiency(Collider collider)
        {
            string materialName = GetPhysicsMaterialName(collider);

            return materialName switch
            {
                "PhyRubber" => 1.0f,      // 橡胶拍面：100%转换效率
                "PhyBlade" => 0.7f,       // 木质刀片：70%效率
                "PhyWood" => 0.8f,        // 球桌台面：80%效率
                "PhyNet" => 0.3f,         // 球网：30%效率（吸收大部分旋转）
                "PhyMetalSolid" => 0.1f,  // 金属支架：10%效率
                "PhyCAP" => 0.9f,         // 球与球：90%效率
                _ => 0.8f                 // 默认80%效率
            };
        }

        /// <summary>
        /// 获取物理材质名称
        /// </summary>
        private string GetPhysicsMaterialName(Collider collider)
        {
            if (collider.material != null)
            {
                return collider.material.name.Replace(" (Instance)", "");
            }

            // 基于标签或组件名称的后备判断
            if (collider.CompareTag("Paddle"))
            {
                // 进一步判断是拍面还是刀片（可以通过碰撞点或子对象判断）
                return "PhyRubber"; // 默认假设是拍面
            }
            else if (collider.CompareTag("Table"))
            {
                return "PhyWood";
            }
            else if (collider.CompareTag("Ball"))
            {
                return "PhyCAP";
            }

            return "Unknown";
        }
        #endregion

        #region Visualization
        /// <summary>
        /// 初始化可视化组件
        /// </summary>
        private void InitializeVisualization()
        {
            // 初始化轨迹特效
            if (spinTrailEffect == null)
            {
                var trailObject = new GameObject("SpinTrailEffect");
                trailObject.transform.SetParent(transform);
                trailObject.transform.localPosition = Vector3.zero;
                spinTrailEffect = trailObject.AddComponent<ParticleSystem>();
                ConfigureTrailEffect();
            }

            // 初始化旋转轴指示器
            if (spinAxisIndicator == null)
            {
                var indicatorObject = new GameObject("SpinAxisIndicator");
                indicatorObject.transform.SetParent(transform);
                indicatorObject.transform.localPosition = Vector3.zero;
                spinAxisIndicator = indicatorObject.AddComponent<LineRenderer>();
                ConfigureAxisIndicator();
            }
        }

        /// <summary>
        /// 配置轨迹特效
        /// </summary>
        private void ConfigureTrailEffect()
        {
            if (spinTrailEffect == null) return;

            var main = spinTrailEffect.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 0.1f;
            main.startSize = 0.005f;
            main.startColor = Color.cyan;
            main.maxParticles = 50;

            var emission = spinTrailEffect.emission;
            emission.enabled = false; // 默认关闭，旋转时开启
        }

        /// <summary>
        /// 配置旋转轴指示器
        /// </summary>
        private void ConfigureAxisIndicator()
        {
            if (spinAxisIndicator == null) return;

            spinAxisIndicator.material = spinVisualizationMaterial;
            spinAxisIndicator.startWidth = 0.002f;
            spinAxisIndicator.endWidth = 0.002f;
            spinAxisIndicator.positionCount = 2;
            spinAxisIndicator.enabled = false; // 默认隐藏
        }

        /// <summary>
        /// 更新可视化效果
        /// </summary>
        private void UpdateVisualization()
        {
            if (!showSpinVisualization) return;

            bool shouldShowVisualization = IsSpinning;

            // 更新轨迹特效
            if (spinTrailEffect != null)
            {
                var emission = spinTrailEffect.emission;
                emission.enabled = shouldShowVisualization;

                if (shouldShowVisualization)
                {
                    emission.rateOverTime = SpinIntensity * 20f;
                }
            }

            // 更新旋转轴指示器
            if (spinAxisIndicator != null)
            {
                spinAxisIndicator.enabled = shouldShowVisualization;
            }
        }

        /// <summary>
        /// 更新旋转轴指示器
        /// </summary>
        private void UpdateSpinAxisIndicator()
        {
            if (!IsSpinning || spinAxisIndicator == null) return;

            Vector3 center = transform.position;
            Vector3 axisEnd = center + spinAxis * 0.2f * SpinIntensity;

            spinAxisIndicator.SetPosition(0, center);
            spinAxisIndicator.SetPosition(1, axisEnd);

            // 根据旋转强度调整颜色
            Color spinColor = Color.Lerp(Color.blue, Color.red, SpinIntensity);
            spinAxisIndicator.startColor = spinColor;
            spinAxisIndicator.endColor = spinColor;
        }
        #endregion

        #region Audio
        /// <summary>
        /// 更新旋转音效
        /// </summary>
        private void UpdateSpinAudio()
        {
            if (audioSource == null || spinSoundClip == null) return;

            spinSoundTimer += Time.fixedDeltaTime;

            if (IsSpinning && spinSoundTimer >= SPIN_SOUND_INTERVAL)
            {
                float volume = SpinIntensity * spinSoundVolumeMultiplier;
                float pitch = 0.8f + SpinIntensity * 0.4f; // 0.8 - 1.2

                audioSource.pitch = pitch;
                audioSource.PlayOneShot(spinSoundClip, volume);

                spinSoundTimer = 0f;
            }
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// 获取旋转信息字符串
        /// </summary>
        /// <returns>旋转信息</returns>
        public string GetSpinInfo()
        {
            if (!IsSpinning) return "无旋转";

            return $"旋转轴: {spinAxis:F3}\n" +
                   $"旋转速率: {spinRate:F2}\n" +
                   $"旋转强度: {SpinIntensity:P1}";
        }

        /// <summary>
        /// 设置可视化显示状态
        /// </summary>
        /// <param name="show">是否显示</param>
        public void SetVisualizationEnabled(bool show)
        {
            showSpinVisualization = show;

            if (!show)
            {
                if (spinTrailEffect != null)
                {
                    var emission = spinTrailEffect.emission;
                    emission.enabled = false;
                }

                if (spinAxisIndicator != null)
                {
                    spinAxisIndicator.enabled = false;
                }
            }
        }

        /// <summary>
        /// 获取当前旋转数据（用于网络同步）
        /// </summary>
        /// <returns>旋转数据</returns>
        public SpinData GetSpinData()
        {
            return new SpinData
            {
                axis = spinAxis,
                rate = spinRate,
                isSpinning = isSpinning
            };
        }

        /// <summary>
        /// 应用旋转数据（用于网络同步）
        /// </summary>
        /// <param name="data">旋转数据</param>
        public void ApplySpinData(SpinData data)
        {
            SetSpin(data.axis, data.rate);
            isSpinning = data.isSpinning;
        }

        /// <summary>
        /// 设置马格努斯力倍数（调试用）
        /// </summary>
        public void SetMagnusForceMultiplier(float multiplier)
        {
            magnusForceMultiplier = Mathf.Clamp(multiplier, 0f, 10f);
            Debug.Log($"马格努斯力倍数设置为: {magnusForceMultiplier}");
        }

        /// <summary>
        /// 设置旋转衰减率（调试用）
        /// </summary>
        public void SetSpinDecayRate(float decayRate)
        {
            spinDecayRate = Mathf.Clamp(decayRate, 0.1f, 1f);
            Debug.Log($"旋转衰减率设置为: {spinDecayRate}");
        }

        /// <summary>
        /// 获取当前马格努斯力倍数
        /// </summary>
        public float GetMagnusForceMultiplier()
        {
            return magnusForceMultiplier;
        }

        /// <summary>
        /// 获取当前旋转衰减率
        /// </summary>
        public float GetSpinDecayRate()
        {
            return spinDecayRate;
        }
        #endregion

        #region Gizmos
        private void OnDrawGizmos()
        {
            if (!showSpinVisualization || !IsSpinning) return;

            // 绘制旋转轴
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, spinAxis * 0.3f);

            // 绘制旋转圆圈
            Gizmos.color = Color.cyan;
            float radius = 0.025f * SpinIntensity;

            // 简化的圆圈绘制
            Vector3 center = transform.position;
            Vector3 right = Vector3.Cross(spinAxis, Vector3.up).normalized * radius;
            Vector3 forward = Vector3.Cross(spinAxis, right).normalized * radius;

            for (int i = 0; i < 16; i++)
            {
                float angle1 = (i / 16f) * 2f * Mathf.PI;
                float angle2 = ((i + 1) / 16f) * 2f * Mathf.PI;

                Vector3 point1 = center + right * Mathf.Cos(angle1) + forward * Mathf.Sin(angle1);
                Vector3 point2 = center + right * Mathf.Cos(angle2) + forward * Mathf.Sin(angle2);

                Gizmos.DrawLine(point1, point2);
            }
        }
        #endregion
    }

    /// <summary>
    /// 旋转数据结构（用于网络同步）
    /// </summary>
    [System.Serializable]
    public struct SpinData
    {
        public Vector3 axis;
        public float rate;
        public bool isSpinning;
    }
}