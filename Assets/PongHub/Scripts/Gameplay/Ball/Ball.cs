using UnityEngine;
using PongHub.Core;
using PongHub.Core.Audio;
using System.Threading.Tasks;

namespace PongHub.Gameplay.Ball
{
    /// <summary>
    /// 球的统一基础类 - 提供统一接口和组件管理
    /// 解决Ball组件分散的问题，提供与Table、Paddle一致的设计模式
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Ball : MonoBehaviour
    {
        [Header("核心组件")]
        [SerializeField] private BallPhysics m_physics;
        [SerializeField] private BallNetworking m_networking;
        [SerializeField] private BallSpin m_spin;
        [SerializeField] private BallAttachment m_attachment;
        [SerializeField] private BallStateSync m_stateSync;
        [SerializeField] private BallAudio m_audio;
        [SerializeField] private BallParticles m_particles;
        [SerializeField] private BallSpinVisual m_spinVisual;

        [Header("Unity组件")]
        [SerializeField] private Rigidbody m_rigidbody;
        [SerializeField] private Collider m_collider;
        [SerializeField] private MeshRenderer m_renderer;

        [Header("配置")]
        [SerializeField] private BallData m_ballData;

        [Header("调试")]
        [SerializeField] private bool m_enableDebugLog = false;

        // 球状态
        private BallState m_currentState = BallState.None;
        private Vector3 m_lastPosition;
        private Vector3 m_lastVelocity;
        private float m_lastHitTime;

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeComponents();
            ValidateComponents();
        }

        private void Start()
        {
            Initialize();
        }

        private void FixedUpdate()
        {
            UpdateBallState();
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision);
        }
        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            // 自动获取组件引用
            if (m_physics == null) m_physics = GetComponent<BallPhysics>();
            if (m_networking == null) m_networking = GetComponent<BallNetworking>();
            if (m_spin == null) m_spin = GetComponent<BallSpin>();
            if (m_attachment == null) m_attachment = GetComponent<BallAttachment>();
            if (m_stateSync == null) m_stateSync = GetComponent<BallStateSync>();
            if (m_audio == null) m_audio = GetComponent<BallAudio>();
            if (m_particles == null) m_particles = GetComponent<BallParticles>();
            if (m_spinVisual == null) m_spinVisual = GetComponent<BallSpinVisual>();

            // Unity组件
            if (m_rigidbody == null) m_rigidbody = GetComponent<Rigidbody>();
            if (m_collider == null) m_collider = GetComponent<Collider>();
            if (m_renderer == null) m_renderer = GetComponent<MeshRenderer>();
        }

        private void ValidateComponents()
        {
            // 验证必需组件
            if (m_rigidbody == null)
            {
                Debug.LogError($"Ball {name}: 缺少 Rigidbody 组件", this);
            }

            if (m_collider == null)
            {
                Debug.LogError($"Ball {name}: 缺少 Collider 组件", this);
            }

            if (m_ballData == null)
            {
                Debug.LogWarning($"Ball {name}: 缺少 BallData 配置", this);
            }
        }

        public void Initialize()
        {
            LogDebug("Ball 初始化开始");

            // 初始化物理组件
            if (m_physics != null && m_ballData != null)
            {
                m_physics.SetBallData(m_ballData);
                LogDebug("BallPhysics 初始化完成");
            }

            // 初始化其他组件
            InitializeSubComponents();

            // 设置初始状态
            m_currentState = BallState.Idle;
            m_lastPosition = transform.position;
            m_lastVelocity = Vector3.zero;

            LogDebug("Ball 初始化完成");
        }

        private void InitializeSubComponents()
        {
            // 各子组件的初始化由其自身的Awake/Start方法处理
            // 这里只做必要的配置传递

            if (m_audio != null && m_ballData != null)
            {
                // 传递音频配置
                var audioData = Resources.Load<BallAudioData>("BallAudioData");
                if (audioData != null)
                {
                    // m_audio.SetAudioData(audioData);
                }
            }
        }
        #endregion

        #region State Management
        private void UpdateBallState()
        {
            // 更新球状态
            m_lastPosition = transform.position;
            m_lastVelocity = m_rigidbody != null ? m_rigidbody.velocity : Vector3.zero;

            // 根据速度和附着状态更新状态
            if (IsAttached)
            {
                m_currentState = BallState.Hit; // 附着在手上
            }
            else if (m_lastVelocity.magnitude > 0.1f)
            {
                m_currentState = BallState.Moving;
            }
            else
            {
                m_currentState = BallState.Idle;
            }
        }

        public void ResetBall()
        {
            LogDebug("重置球状态");

            // 重置物理状态
            if (m_rigidbody != null)
            {
                m_rigidbody.velocity = Vector3.zero;
                m_rigidbody.angularVelocity = Vector3.zero;
                m_rigidbody.isKinematic = false;
            }

            // 重置各组件
            m_physics?.ResetBall();
            m_networking?.ResetBall();
            m_spin?.ResetSpin();
            m_attachment?.DetachBall();

            // 重置状态
            m_currentState = BallState.Idle;
            m_lastHitTime = 0f;

            LogDebug("球状态重置完成");
        }

        public void DestroyBall()
        {
            LogDebug("销毁球");

            // 清理各组件
            // 具体的清理逻辑由各组件的OnDestroy处理

            m_currentState = BallState.None;
        }
        #endregion

        #region Collision Handling
        private void HandleCollision(Collision collision)
        {
            if (m_currentState == BallState.None) return;

            var hitObject = collision.gameObject;
            var contact = collision.GetContact(0);
            var contactPoint = contact.point;
            var hitForce = collision.relativeVelocity.magnitude;

            LogDebug($"球碰撞: {hitObject.name}, 力度: {hitForce:F2}");

            // 更新击球时间
            m_lastHitTime = Time.time;

            // 根据碰撞对象播放音效 - Ball负责音效判断
            PlayCollisionAudio(hitObject, contactPoint, hitForce);

            // 播放粒子效果
            PlayCollisionParticles(contactPoint, collision.relativeVelocity);

            // 委托给物理组件处理具体的物理响应
            if (m_physics != null)
            {
                var hitType = DetermineHitType(hitObject);
                m_physics.ApplyCollisionForce(contactPoint, contact.normal, hitForce, hitType);
            }
        }

        private void PlayCollisionAudio(GameObject hitObject, Vector3 position, float force)
        {
            // Ball统一负责判断和播放碰撞音效
            if (AudioManager.Instance == null) return;

            if (hitObject.CompareTag("Table"))
            {
                AudioManager.Instance.PlayTableHit(position, force);
            }
            else if (hitObject.CompareTag("Net"))
            {
                AudioManager.Instance.PlayNetHit(position, force);
            }
            else if (hitObject.CompareTag("Paddle"))
            {
                AudioManager.Instance.PlayPaddleHit(position, force);
            }
            else if (hitObject.CompareTag("Edge"))
            {
                AudioManager.Instance.PlayEdgeHit(position, force);
            }
            else
            {
                // 通用碰撞音效
                AudioManager.Instance.PlayBallBounce(position, force);

            }
        }

        private void PlayCollisionParticles(Vector3 position, Vector3 velocity)
        {
            if (m_particles != null)
            {
                m_particles.PlayHitParticles(position, velocity);
            }
        }

        private HitType DetermineHitType(GameObject hitObject)
        {
            if (hitObject.CompareTag("Table")) return HitType.Table;
            if (hitObject.CompareTag("Paddle")) return HitType.Paddle;
            if (hitObject.CompareTag("Net")) return HitType.Net;
            return HitType.Table; // 默认
        }
        #endregion

        #region Public Interface
        // 统一的公共接口
        public BallPhysics Physics => m_physics;
        public BallNetworking Networking => m_networking;
        public BallSpin Spin => m_spin;
        public BallAttachment Attachment => m_attachment;
        public BallStateSync StateSync => m_stateSync;
        public BallAudio Audio => m_audio;
        public BallParticles Particles => m_particles;
        public BallSpinVisual SpinVisual => m_spinVisual;
        public BallData Data => m_ballData;

        // 球状态属性
        public BallState CurrentState => m_currentState;
        public Vector3 Velocity => m_rigidbody != null ? m_rigidbody.velocity : Vector3.zero;
        public Vector3 AngularVelocity => m_rigidbody != null ? m_rigidbody.angularVelocity : Vector3.zero;
        public bool IsAttached => m_networking != null && m_networking.IsAttached;
        public bool IsAlive => m_networking != null && m_networking.IsAlive;
        public float LastHitTime => m_lastHitTime;

        // 便捷方法
        public void SetVelocity(Vector3 velocity)
        {
            if (m_rigidbody != null)
            {
                m_rigidbody.velocity = velocity;
            }
        }

        public void SetAngularVelocity(Vector3 angularVelocity)
        {
            if (m_rigidbody != null)
            {
                m_rigidbody.angularVelocity = angularVelocity;
            }
        }

        public void AddSpin(Vector3 spinAxis, float spinRate)
        {
            if (m_spin != null)
            {
                m_spin.AddSpin(spinAxis, spinRate);
            }
        }

        public void AttachToHand(Transform hand)
        {
            if (m_attachment != null)
            {
                m_attachment.AttachToNonPaddleHand(hand);
            }
        }

        public void ReleaseBall(Vector3 releaseVelocity)
        {
            if (m_networking != null)
            {
                m_networking.ReleaseBall(releaseVelocity, Vector3.up, 0f);
            }
        }
        #endregion

        #region Async Interface
        public async Task InitializeAsync()
        {
            await Task.Yield();
            Initialize();
        }
        #endregion

        #region Debug
        private void LogDebug(string message)
        {
            if (m_enableDebugLog)
            {
                Debug.Log($"[Ball {name}] {message}", this);
            }
        }

        public string GetDebugInfo()
        {
            return $"状态: {m_currentState}\n" +
                   $"速度: {Velocity.magnitude:F2} m/s\n" +
                   $"附着: {IsAttached}\n" +
                   $"存活: {IsAlive}\n" +
                   $"最后击球: {m_lastHitTime:F2}s";
        }
        #endregion
    }
}