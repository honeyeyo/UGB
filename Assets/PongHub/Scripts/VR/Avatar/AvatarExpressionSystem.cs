using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;
using Oculus.Avatar2;
using PongHub.Core;
using PongHub.VR;

namespace PongHub.VR.Avatar
{
    /// <summary>
    /// Avatar表情系统
    /// 负责管理Avatar的面部表情、口型同步、眼部追踪和情绪表达
    /// </summary>
    public class AvatarExpressionSystem : MonoBehaviour
    {
        /// <summary>
        /// 表情模式枚举
        /// </summary>
        public enum ExpressionMode
        {
            None,           // 无表情
            Basic,          // 基础表情
            Realistic,      // 写实表情
            Exaggerated     // 夸张表情
        }

        /// <summary>
        /// 基础表情类型
        /// </summary>
        public enum BasicExpression
        {
            Neutral,        // 中性
            Happy,          // 快乐
            Sad,            // 悲伤
            Angry,          // 愤怒
            Surprised,      // 惊讶
            Focused,        // 专注
            Excited,        // 兴奋
            Confused        // 困惑
        }

        /// <summary>
        /// 口型同步模式
        /// </summary>
        public enum LipSyncMode
        {
            Disabled,       // 禁用
            Basic,          // 基础口型
            Phoneme,        // 音素匹配
            Viseme          // 视素匹配
        }

        [Header("Expression Settings")]
        [SerializeField]
        [Tooltip("启用表情系统")]
        private bool m_enableExpressions = true;

        [SerializeField]
        [Tooltip("表情模式")]
        private ExpressionMode m_expressionMode = ExpressionMode.Realistic;

        [SerializeField]
        [Tooltip("默认表情")]
        private BasicExpression m_defaultExpression = BasicExpression.Neutral;

        [SerializeField]
        [Tooltip("表情变化速度")]
        [Range(0.1f, 5f)]
        private float m_expressionSpeed = 1.5f;

        [SerializeField]
        [Tooltip("表情强度")]
        [Range(0.1f, 2f)]
        private float m_expressionIntensity = 1f;

        [Header("Lip Sync")]
        [SerializeField]
        [Tooltip("启用口型同步")]
        private bool m_enableLipSync = true;

        [SerializeField]
        [Tooltip("口型同步模式")]
        private LipSyncMode m_lipSyncMode = LipSyncMode.Viseme;

        [SerializeField]
        [Tooltip("口型同步敏感度")]
        [Range(0.1f, 2f)]
        private float m_lipSyncSensitivity = 1.2f;

        [SerializeField]
        [Tooltip("口型平滑因子")]
        [Range(0.1f, 1f)]
        private float m_lipSmoothingFactor = 0.7f;

        [Header("Eye Tracking")]
        [SerializeField]
        [Tooltip("启用眼部追踪")]
        private bool m_enableEyeTracking = true;

        [SerializeField]
        [Tooltip("眼球移动速度")]
        [Range(0.1f, 5f)]
        private float m_eyeMovementSpeed = 2f;

        [SerializeField]
        [Tooltip("眨眼频率")]
        [Range(0.1f, 5f)]
        private float m_blinkFrequency = 1.5f;

        [SerializeField]
        [Tooltip("注视目标")]
        private Transform m_gazeTarget;

        [Header("Emotional Response")]
        [SerializeField]
        [Tooltip("启用情绪响应")]
        private bool m_enableEmotionalResponse = true;

        [SerializeField]
        [Tooltip("情绪变化阈值")]
        [Range(0.1f, 1f)]
        private float m_emotionThreshold = 0.6f;

        [SerializeField]
        [Tooltip("情绪持续时间")]
        [Range(1f, 10f)]
        private float m_emotionDuration = 3f;

        [Header("Performance")]
        [SerializeField]
        [Tooltip("更新频率")]
        [Range(15f, 60f)]
        private float m_updateFrequency = 30f;

        [SerializeField]
        [Tooltip("启用距离优化")]
        private bool m_enableDistanceOptimization = true;

        [SerializeField]
        [Tooltip("最大表情距离")]
        [Range(5f, 30f)]
        private float m_maxExpressionDistance = 15f;

        // 组件引用
        private VRAvatarManager m_avatarManager;
        private OvrAvatarEntity m_avatarEntity;
        private OvrAvatarLipSyncContext m_lipSyncContext;
        private OvrAvatarFacePoseBehavior m_facePoseBehavior;
        private OvrAvatarEyePoseBehavior m_eyePoseBehavior;
        private Camera m_mainCamera;

        // 表情数据
        private struct ExpressionData
        {
            public BasicExpression expression;
            public float intensity;
            public float duration;
            public float startTime;
            public bool isActive;
        }

        private ExpressionData m_currentExpression;
        private ExpressionData m_targetExpression;
        private BasicExpression m_lastExpression;

        // 口型数据
        private float[] m_visemeWeights = new float[15]; // OVRLipSync有15个视素
        private float[] m_targetVisemeWeights = new float[15];
        private float m_speechVolume = 0f;

        // 眼部数据
        private Vector3 m_currentGazeDirection = Vector3.forward;
        private Vector3 m_targetGazeDirection = Vector3.forward;
        private float m_eyeOpenness = 1f;
        private float m_lastBlinkTime = 0f;
        private bool m_isBlinking = false;

        // 状态管理
        private bool m_isInitialized = false;
        private float m_lastUpdateTime = 0f;
        private float m_updateInterval = 0f;

        // 情绪响应
        private Dictionary<string, float> m_emotionTriggers = new Dictionary<string, float>();

        // 事件
        public UnityEvent<BasicExpression> OnExpressionChanged = new UnityEvent<BasicExpression>();
        public UnityEvent<float> OnSpeechDetected = new UnityEvent<float>();
        public UnityEvent<Vector3> OnGazeDirectionChanged = new UnityEvent<Vector3>();
        public UnityEvent OnExpressionSystemInitialized = new UnityEvent();

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => m_isInitialized;

        /// <summary>
        /// 当前表情
        /// </summary>
        public BasicExpression CurrentExpression => m_currentExpression.expression;

        /// <summary>
        /// 当前语音音量
        /// </summary>
        public float SpeechVolume => m_speechVolume;

        /// <summary>
        /// 当前注视方向
        /// </summary>
        public Vector3 GazeDirection => m_currentGazeDirection;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            StartCoroutine(InitializeAsync());
        }

        private void Update()
        {
            if (m_isInitialized && m_enableExpressions)
            {
                if (Time.time - m_lastUpdateTime >= m_updateInterval)
                {
                    UpdateExpressionSystem();
                    m_lastUpdateTime = Time.time;
                }
            }
        }

        private void OnDestroy()
        {
            CleanupExpressionSystem();
        }

        private void InitializeComponents()
        {
            // 获取Avatar管理器
            m_avatarManager = GetComponent<VRAvatarManager>();
            if (m_avatarManager == null)
            {
                m_avatarManager = FindObjectOfType<VRAvatarManager>();
            }

            // 获取相机
            m_mainCamera = Camera.main;
            if (m_mainCamera == null)
            {
                m_mainCamera = FindObjectOfType<Camera>();
            }

            // 计算更新间隔
            m_updateInterval = 1f / m_updateFrequency;

            // 初始化表情数据
            m_currentExpression = new ExpressionData
            {
                expression = m_defaultExpression,
                intensity = 1f,
                duration = 0f,
                startTime = Time.time,
                isActive = true
            };

            m_targetExpression = m_currentExpression;

            Debug.Log("[AvatarExpressionSystem] Components initialized");
        }

        private IEnumerator InitializeAsync()
        {
            // 等待Avatar管理器初始化
            while (m_avatarManager == null || !m_avatarManager.IsInitialized)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // 获取Avatar实体
            m_avatarEntity = m_avatarManager.AvatarEntity;
            if (m_avatarEntity == null)
            {
                Debug.LogError("[AvatarExpressionSystem] Avatar entity not found");
                yield break;
            }

            // 等待Avatar加载完成
            yield return StartCoroutine(WaitForAvatarLoad());

            // 设置表情组件
            SetupExpressionComponents();

            // 设置口型同步
            if (m_enableLipSync)
            {
                SetupLipSync();
            }

            // 设置眼部追踪
            if (m_enableEyeTracking)
            {
                SetupEyeTracking();
            }

            // 初始化情绪系统
            InitializeEmotionalResponse();

            // 设置默认表情
            SetExpression(m_defaultExpression, 1f);

            m_isInitialized = true;
            OnExpressionSystemInitialized?.Invoke();

            Debug.Log("[AvatarExpressionSystem] Expression system initialized successfully");
        }

        private IEnumerator WaitForAvatarLoad()
        {
            float timeout = 30f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                if (m_avatarEntity != null && m_avatarEntity.IsCreated)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Debug.LogWarning("[AvatarExpressionSystem] Avatar load timeout");
        }

        private void SetupExpressionComponents()
        {
            // 获取或创建面部姿态组件
            m_facePoseBehavior = m_avatarEntity.GetComponent<OvrAvatarFacePoseBehavior>();
            if (m_facePoseBehavior == null)
            {
                m_facePoseBehavior = m_avatarEntity.gameObject.AddComponent<OvrAvatarFacePoseBehavior>();
            }

            Debug.Log("[AvatarExpressionSystem] Expression components setup complete");
        }

        private void SetupLipSync()
        {
            // 获取或创建口型同步组件
            m_lipSyncContext = m_avatarEntity.GetComponent<OvrAvatarLipSyncContext>();
            if (m_lipSyncContext == null)
            {
                m_lipSyncContext = m_avatarEntity.gameObject.AddComponent<OvrAvatarLipSyncContext>();
            }

            Debug.Log("[AvatarExpressionSystem] Lip sync setup complete");
        }

        private void SetupEyeTracking()
        {
            // 获取或创建眼部追踪组件
            m_eyePoseBehavior = m_avatarEntity.GetComponent<OvrAvatarEyePoseBehavior>();
            if (m_eyePoseBehavior == null)
            {
                m_eyePoseBehavior = m_avatarEntity.gameObject.AddComponent<OvrAvatarEyePoseBehavior>();
            }

            Debug.Log("[AvatarExpressionSystem] Eye tracking setup complete");
        }

        private void InitializeEmotionalResponse()
        {
            m_emotionTriggers.Clear();
            
            // 初始化情绪触发器
            m_emotionTriggers["victory"] = 0f;
            m_emotionTriggers["defeat"] = 0f;
            m_emotionTriggers["surprise"] = 0f;
            m_emotionTriggers["focus"] = 0f;

            Debug.Log("[AvatarExpressionSystem] Emotional response initialized");
        }

        private void UpdateExpressionSystem()
        {
            // 检查距离优化
            if (m_enableDistanceOptimization && !IsWithinExpressionDistance())
            {
                return;
            }

            // 更新表情
            UpdateExpressions();

            // 更新口型同步
            if (m_enableLipSync)
            {
                UpdateLipSync();
            }

            // 更新眼部追踪
            if (m_enableEyeTracking)
            {
                UpdateEyeTracking();
            }

            // 更新情绪响应
            if (m_enableEmotionalResponse)
            {
                UpdateEmotionalResponse();
            }
        }

        private bool IsWithinExpressionDistance()
        {
            if (m_mainCamera == null || m_avatarEntity == null)
                return true;

            float distance = Vector3.Distance(m_mainCamera.transform.position, m_avatarEntity.transform.position);
            return distance <= m_maxExpressionDistance;
        }

        private void UpdateExpressions()
        {
            // 检查表情变化
            if (m_currentExpression.expression != m_targetExpression.expression)
            {
                TransitionToExpression(m_targetExpression);
            }

            // 更新表情强度
            if (m_currentExpression.isActive && m_currentExpression.duration > 0f)
            {
                float elapsed = Time.time - m_currentExpression.startTime;
                if (elapsed >= m_currentExpression.duration)
                {
                    // 表情持续时间结束，回到默认表情
                    SetExpression(m_defaultExpression, 1f);
                }
            }

            // 应用当前表情到Avatar
            ApplyExpressionToAvatar();
        }

        private void TransitionToExpression(ExpressionData targetExpression)
        {
            float t = Mathf.Clamp01((Time.time - m_currentExpression.startTime) * m_expressionSpeed);
            
            if (t >= 1f)
            {
                m_currentExpression = targetExpression;
                m_currentExpression.startTime = Time.time;
                OnExpressionChanged?.Invoke(m_currentExpression.expression);
            }
            else
            {
                // 平滑过渡
                m_currentExpression.intensity = Mathf.Lerp(m_currentExpression.intensity, targetExpression.intensity, t);
            }
        }

        private void ApplyExpressionToAvatar()
        {
            if (m_facePoseBehavior == null) return;

            // 根据表情类型设置面部参数
            switch (m_currentExpression.expression)
            {
                case BasicExpression.Happy:
                    ApplyHappyExpression();
                    break;
                case BasicExpression.Sad:
                    ApplySadExpression();
                    break;
                case BasicExpression.Angry:
                    ApplyAngryExpression();
                    break;
                case BasicExpression.Surprised:
                    ApplySurprisedExpression();
                    break;
                case BasicExpression.Focused:
                    ApplyFocusedExpression();
                    break;
                case BasicExpression.Excited:
                    ApplyExcitedExpression();
                    break;
                case BasicExpression.Confused:
                    ApplyConfusedExpression();
                    break;
                default:
                    ApplyNeutralExpression();
                    break;
            }
        }

        private void ApplyHappyExpression()
        {
            // 设置快乐表情的面部参数
            // 这里需要根据具体的Avatar SDK API来设置
        }

        private void ApplySadExpression()
        {
            // 设置悲伤表情的面部参数
        }

        private void ApplyAngryExpression()
        {
            // 设置愤怒表情的面部参数
        }

        private void ApplySurprisedExpression()
        {
            // 设置惊讶表情的面部参数
        }

        private void ApplyFocusedExpression()
        {
            // 设置专注表情的面部参数
        }

        private void ApplyExcitedExpression()
        {
            // 设置兴奋表情的面部参数
        }

        private void ApplyConfusedExpression()
        {
            // 设置困惑表情的面部参数
        }

        private void ApplyNeutralExpression()
        {
            // 设置中性表情的面部参数
        }

        private void UpdateLipSync()
        {
            if (m_lipSyncContext == null) return;

            // 获取语音输入
            m_speechVolume = GetSpeechVolume();
            
            if (m_speechVolume > 0.01f)
            {
                OnSpeechDetected?.Invoke(m_speechVolume);
                
                // 根据语音音量和频率生成视素权重
                GenerateVisemeWeights();
                
                // 应用视素权重到Avatar
                ApplyVisemeWeights();
            }
            else
            {
                // 没有语音时，逐渐关闭嘴部
                for (int i = 0; i < m_visemeWeights.Length; i++)
                {
                    m_visemeWeights[i] = Mathf.Lerp(m_visemeWeights[i], 0f, Time.deltaTime * 5f);
                }
            }
        }

        private float GetSpeechVolume()
        {
            // 这里应该从麦克风或音频源获取语音音量
            // 暂时返回模拟值
            return 0f;
        }

        private void GenerateVisemeWeights()
        {
            // 基于语音音量生成基础的视素权重
            // 这是一个简化的实现，实际应该使用更复杂的语音分析
            
            switch (m_lipSyncMode)
            {
                case LipSyncMode.Basic:
                    GenerateBasicVisemeWeights();
                    break;
                case LipSyncMode.Phoneme:
                    GeneratePhonemeBasedWeights();
                    break;
                case LipSyncMode.Viseme:
                    GenerateAdvancedVisemeWeights();
                    break;
            }
        }

        private void GenerateBasicVisemeWeights()
        {
            // 基础口型：主要是开合嘴部
            float openMouth = m_speechVolume * m_lipSyncSensitivity;
            m_targetVisemeWeights[0] = openMouth; // sil (静音)
            m_targetVisemeWeights[1] = openMouth * 0.8f; // aa
            m_targetVisemeWeights[2] = openMouth * 0.6f; // E
        }

        private void GeneratePhonemeBasedWeights()
        {
            // 基于音素的更复杂的口型生成
            // 这里需要音频分析来识别音素
        }

        private void GenerateAdvancedVisemeWeights()
        {
            // 高级视素权重生成
            // 需要更复杂的语音识别和分析
        }

        private void ApplyVisemeWeights()
        {
            // 平滑过渡到目标视素权重
            for (int i = 0; i < m_visemeWeights.Length; i++)
            {
                m_visemeWeights[i] = Mathf.Lerp(m_visemeWeights[i], m_targetVisemeWeights[i], 
                    Time.deltaTime * (1f / m_lipSmoothingFactor));
            }

            // 将权重应用到Avatar的口型系统
            // 这里需要调用Avatar SDK的具体API
        }

        private void UpdateEyeTracking()
        {
            if (m_eyePoseBehavior == null) return;

            // 更新注视方向
            UpdateGazeDirection();

            // 更新眨眼
            UpdateBlinking();

            // 应用眼部数据到Avatar
            ApplyEyeDataToAvatar();
        }

        private void UpdateGazeDirection()
        {
            Vector3 newGazeDirection = m_currentGazeDirection;

            if (m_gazeTarget != null)
            {
                // 计算到目标的方向
                Vector3 targetDirection = (m_gazeTarget.position - m_avatarEntity.transform.position).normalized;
                newGazeDirection = Vector3.Lerp(m_currentGazeDirection, targetDirection, 
                    Time.deltaTime * m_eyeMovementSpeed);
            }
            else
            {
                // 没有目标时，随机轻微移动眼球
                float randomX = Mathf.PerlinNoise(Time.time * 0.5f, 0f) * 0.2f - 0.1f;
                float randomY = Mathf.PerlinNoise(0f, Time.time * 0.3f) * 0.2f - 0.1f;
                newGazeDirection = Vector3.forward + new Vector3(randomX, randomY, 0f);
                newGazeDirection = newGazeDirection.normalized;
            }

            if (Vector3.Distance(newGazeDirection, m_currentGazeDirection) > 0.01f)
            {
                m_currentGazeDirection = newGazeDirection;
                OnGazeDirectionChanged?.Invoke(m_currentGazeDirection);
            }
        }

        private void UpdateBlinking()
        {
            // 自然眨眼逻辑
            if (!m_isBlinking && Time.time - m_lastBlinkTime > (1f / m_blinkFrequency))
            {
                StartCoroutine(BlinkCoroutine());
            }
        }

        private IEnumerator BlinkCoroutine()
        {
            m_isBlinking = true;
            
            // 闭眼
            float blinkDuration = 0.15f;
            float elapsed = 0f;
            
            while (elapsed < blinkDuration / 2f)
            {
                elapsed += Time.deltaTime;
                m_eyeOpenness = Mathf.Lerp(1f, 0f, elapsed / (blinkDuration / 2f));
                yield return null;
            }
            
            // 睁眼
            elapsed = 0f;
            while (elapsed < blinkDuration / 2f)
            {
                elapsed += Time.deltaTime;
                m_eyeOpenness = Mathf.Lerp(0f, 1f, elapsed / (blinkDuration / 2f));
                yield return null;
            }
            
            m_eyeOpenness = 1f;
            m_isBlinking = false;
            m_lastBlinkTime = Time.time;
        }

        private void ApplyEyeDataToAvatar()
        {
            // 将眼部数据应用到Avatar
            // 这里需要调用Avatar SDK的具体API来设置眼球方向和开合度
        }

        private void UpdateEmotionalResponse()
        {
            // 检查情绪触发器
            foreach (var trigger in m_emotionTriggers.Keys)
            {
                float value = m_emotionTriggers[trigger];
                if (value > m_emotionThreshold)
                {
                    TriggerEmotionalResponse(trigger, value);
                    m_emotionTriggers[trigger] = 0f; // 重置触发器
                }
            }
        }

        private void TriggerEmotionalResponse(string trigger, float intensity)
        {
            BasicExpression emotion = BasicExpression.Neutral;
            
            switch (trigger)
            {
                case "victory":
                    emotion = BasicExpression.Happy;
                    break;
                case "defeat":
                    emotion = BasicExpression.Sad;
                    break;
                case "surprise":
                    emotion = BasicExpression.Surprised;
                    break;
                case "focus":
                    emotion = BasicExpression.Focused;
                    break;
            }

            SetExpression(emotion, intensity, m_emotionDuration);
        }

        private void CleanupExpressionSystem()
        {
            StopAllCoroutines();
            m_emotionTriggers.Clear();
            Debug.Log("[AvatarExpressionSystem] Expression system cleanup completed");
        }

        /// <summary>
        /// 设置表情
        /// </summary>
        public void SetExpression(BasicExpression expression, float intensity = 1f, float duration = 0f)
        {
            m_targetExpression = new ExpressionData
            {
                expression = expression,
                intensity = Mathf.Clamp01(intensity * m_expressionIntensity),
                duration = duration,
                startTime = Time.time,
                isActive = true
            };

            Debug.Log($"[AvatarExpressionSystem] Setting expression: {expression} with intensity {intensity}");
        }

        /// <summary>
        /// 触发情绪响应
        /// </summary>
        public void TriggerEmotion(string emotionType, float intensity)
        {
            if (m_emotionTriggers.ContainsKey(emotionType))
            {
                m_emotionTriggers[emotionType] = Mathf.Max(m_emotionTriggers[emotionType], intensity);
            }
        }

        /// <summary>
        /// 设置注视目标
        /// </summary>
        public void SetGazeTarget(Transform target)
        {
            m_gazeTarget = target;
        }

        /// <summary>
        /// 设置表情模式
        /// </summary>
        public void SetExpressionMode(ExpressionMode mode)
        {
            m_expressionMode = mode;
            Debug.Log($"[AvatarExpressionSystem] Expression mode set to: {mode}");
        }

        /// <summary>
        /// 获取表情系统诊断信息
        /// </summary>
        public string GetDiagnostics()
        {
            var diagnostics = new System.Text.StringBuilder();
            diagnostics.AppendLine("=== Avatar Expression System Diagnostics ===");
            diagnostics.AppendLine($"Initialized: {m_isInitialized}");
            diagnostics.AppendLine($"Expressions Enabled: {m_enableExpressions}");
            diagnostics.AppendLine($"Expression Mode: {m_expressionMode}");
            diagnostics.AppendLine($"Current Expression: {m_currentExpression.expression}");
            diagnostics.AppendLine($"Expression Intensity: {m_currentExpression.intensity:F2}");
            diagnostics.AppendLine($"Lip Sync Enabled: {m_enableLipSync}");
            diagnostics.AppendLine($"Lip Sync Mode: {m_lipSyncMode}");
            diagnostics.AppendLine($"Speech Volume: {m_speechVolume:F3}");
            diagnostics.AppendLine($"Eye Tracking Enabled: {m_enableEyeTracking}");
            diagnostics.AppendLine($"Gaze Direction: {m_currentGazeDirection}");
            diagnostics.AppendLine($"Eye Openness: {m_eyeOpenness:F2}");
            diagnostics.AppendLine($"Is Blinking: {m_isBlinking}");
            diagnostics.AppendLine($"Emotional Response Enabled: {m_enableEmotionalResponse}");
            diagnostics.AppendLine($"Update Frequency: {m_updateFrequency:F1}Hz");
            diagnostics.AppendLine($"Max Expression Distance: {m_maxExpressionDistance:F1}m");
            
            return diagnostics.ToString();
        }
    }
}