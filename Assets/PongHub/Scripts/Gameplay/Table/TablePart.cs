using UnityEngine;
using PongHub.Core.Audio;

namespace PongHub.Gameplay.Table
{
    /// <summary>
    /// 球桌部件标识符
    /// 用于标识球桌的不同部位，支持差异化的物理和音效处理
    /// </summary>
    public enum TablePartType
    {
        Surface,    // 桌面
        Net,        // 球网
        Edge,       // 桌边
        Leg,        // 桌腿
        Support     // 横杠支撑
    }

    /// <summary>
    /// 球桌部件组件
    /// 挂载到球桌的各个碰撞体部件上，用于标识和配置部件特性
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TablePart : MonoBehaviour
    {
        [Header("部件标识")]
        [SerializeField]
        [Tooltip("Part Type / 部件类型 - The type of this table part")]
        private TablePartType m_partType = TablePartType.Surface;

        [Header("音效配置")]
        [SerializeField]
        [Tooltip("Hit Sound / 击中音效 - Audio clip to play when ball hits this part")]
        private AudioClip m_hitSound;

        [SerializeField]
        [Tooltip("Volume / 音量 - Volume for the hit sound (0-1)")]
        [Range(0f, 1f)]
        private float m_volume = 1.0f;

        [Header("物理参数")]
        [SerializeField]
        [Tooltip("Bounciness / 弹性 - Bounce multiplier for this part")]
        [Range(0f, 2f)]
        private float m_bounciness = 1.0f;

        [SerializeField]
        [Tooltip("Friction / 摩擦力 - Friction multiplier for this part")]
        [Range(0f, 2f)]
        private float m_friction = 1.0f;

        [Header("游戏逻辑")]
        [SerializeField]
        [Tooltip("Is Scoring Surface / 是否得分表面 - Whether hitting this part counts for scoring")]
        private bool m_isScorerSurface = false;

        [SerializeField]
        [Tooltip("Causes Ball Death / 导致球死亡 - Whether hitting this part kills the ball")]
        private bool m_causesBallDeath = false;

        [Header("调试")]
        [SerializeField]
        [Tooltip("Debug Color / 调试颜色 - Color to display in debug mode")]
        private Color m_debugColor = Color.white;

        [SerializeField]
        [Tooltip("Show Debug Info / 显示调试信息 - Whether to show debug information")]
        private bool m_showDebugInfo = false;

        // 缓存的组件引用
        private Collider m_collider;
        private Renderer m_renderer;

        #region Properties
        /// <summary>
        /// 部件类型
        /// </summary>
        public TablePartType PartType => m_partType;

        /// <summary>
        /// 击中音效
        /// </summary>
        public AudioClip HitSound => m_hitSound;

        /// <summary>
        /// 音量
        /// </summary>
        public float Volume => m_volume;

        /// <summary>
        /// 弹性系数
        /// </summary>
        public float Bounciness => m_bounciness;

        /// <summary>
        /// 摩擦系数
        /// </summary>
        public float Friction => m_friction;

        /// <summary>
        /// 是否为得分表面
        /// </summary>
        public bool IsScorerSurface => m_isScorerSurface;

        /// <summary>
        /// 是否导致球死亡
        /// </summary>
        public bool CausesBallDeath => m_causesBallDeath;

        /// <summary>
        /// 碰撞体组件
        /// </summary>
        public Collider Collider => m_collider;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeComponents();
            ValidateConfiguration();
        }

        private void Start()
        {
            ApplyPhysicsSettings();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                ApplyPhysicsSettings();
            }
        }
        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            m_collider = GetComponent<Collider>();
            m_renderer = GetComponent<Renderer>();

            if (m_collider == null)
            {
                Debug.LogError($"TablePart on {gameObject.name}: Missing Collider component!", this);
            }
        }

        private void ValidateConfiguration()
        {
            // 根据部件类型设置默认配置
            switch (m_partType)
            {
                case TablePartType.Surface:
                    if (m_hitSound == null)
                        LogDebug("Surface part missing hit sound");
                    m_isScorerSurface = true;
                    break;

                case TablePartType.Net:
                    m_causesBallDeath = true; // 击中网通常导致球死亡
                    m_bounciness = 0.3f; // 网的弹性较低
                    break;

                case TablePartType.Edge:
                    m_causesBallDeath = true; // 击中边缘通常导致球死亡
                    break;

                case TablePartType.Leg:
                case TablePartType.Support:
                    m_causesBallDeath = true; // 击中结构件导致球死亡
                    m_bounciness = 0.8f; // 金属结构弹性较高
                    break;
            }
        }

        private void ApplyPhysicsSettings()
        {
            if (m_collider != null && m_collider.material != null)
            {
                // 注意：这里不直接修改共享的PhysicMaterial
                // 实际的物理参数调整应该在碰撞处理时进行
                LogDebug($"Physics settings applied: Bounciness={m_bounciness}, Friction={m_friction}");
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 处理球碰撞
        /// </summary>
        /// <param name="collision">碰撞信息</param>
        /// <param name="ball">球对象</param>
        public void HandleBallHit(Collision collision, GameObject ball)
        {
            LogDebug($"Ball hit {m_partType} part");

            // 播放音效
            PlayHitSound(collision.contacts[0].point);

            // 应用物理效果
            ApplyPhysicsEffect(collision, ball);

            // 处理游戏逻辑
            HandleGameLogic(collision, ball);
        }

        /// <summary>
        /// 获取部件信息字符串
        /// </summary>
        public string GetPartInfo()
        {
            return $"Type: {m_partType}, Bouncy: {m_bounciness:F2}, Friction: {m_friction:F2}, Scoring: {m_isScorerSurface}";
        }
        #endregion

        #region Private Methods
        private void PlayHitSound(Vector3 position)
        {
            if (AudioManager.Instance != null)
            {
                if (m_hitSound != null)
                {
                    // 使用AudioManager播放自定义音效
                    AudioManager.Instance.PlaySound(m_hitSound, m_volume);
                }
                else
                {
                    // 根据部件类型播放默认音效
                    switch (m_partType)
                    {
                        case TablePartType.Surface:
                            AudioManager.Instance.PlayTableHit(position, m_volume);
                            break;
                        case TablePartType.Net:
                            AudioManager.Instance.PlayNetHit(position, m_volume);
                            break;
                        case TablePartType.Edge:
                            AudioManager.Instance.PlayEdgeHit(position, m_volume);
                            break;
                        case TablePartType.Leg:
                        case TablePartType.Support:
                            // 使用通用音效，因为没有专门的金属音效
                            AudioManager.Instance.PlayBallBounce(position, m_volume);
                            break;
                    }
                }
            }
        }

        private void ApplyPhysicsEffect(Collision collision, GameObject ball)
        {
            var ballRigidbody = ball.GetComponent<Rigidbody>();
            if (ballRigidbody == null) return;

            // 根据部件类型调整反弹效果
            Vector3 normal = collision.contacts[0].normal;
            Vector3 incomingVelocity = ballRigidbody.velocity;

            // 计算反弹速度
            Vector3 reflectedVelocity = Vector3.Reflect(incomingVelocity, normal);
            
            // 应用弹性系数
            reflectedVelocity *= m_bounciness;

            // 应用摩擦力（减少切向速度）
            Vector3 tangent = Vector3.Cross(Vector3.Cross(normal, incomingVelocity), normal).normalized;
            float tangentSpeed = Vector3.Dot(reflectedVelocity, tangent);
            reflectedVelocity -= tangent * (tangentSpeed * (1f - m_friction));

            // 特殊处理
            switch (m_partType)
            {
                case TablePartType.Net:
                    // 网会显著减速球
                    reflectedVelocity *= 0.5f;
                    break;
                    
                case TablePartType.Edge:
                    // 边缘可能有特殊的反弹角度
                    break;
            }

            LogDebug($"Applied physics effect: {incomingVelocity} -> {reflectedVelocity}");
        }

        private void HandleGameLogic(Collision collision, GameObject ball)
        {
            // 处理得分逻辑
            if (m_isScorerSurface)
            {
                // 通知得分系统
                LogDebug("Valid scoring surface hit");
            }

            // 处理球死亡逻辑
            if (m_causesBallDeath)
            {
                LogDebug("Ball death caused by hitting this part");
                // 可以发送事件通知球管理器
            }
        }

        private void LogDebug(string message)
        {
            if (m_showDebugInfo)
            {
                Debug.Log($"[TablePart-{m_partType}] {message}", this);
            }
        }
        #endregion

        #region Gizmos
        private void OnDrawGizmos()
        {
            if (!m_showDebugInfo) return;

            Gizmos.color = m_debugColor;
            
            if (m_collider != null)
            {
                if (m_collider is BoxCollider boxCollider)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (m_collider != null && m_collider is BoxCollider boxCollider)
            {
                Gizmos.color = Color.yellow;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
        }
        #endregion
    }
}