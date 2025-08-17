using UnityEngine;
using PongHub.Gameplay.Table;
using PongHub.Core.Audio;

namespace PongHub.Gameplay.Ball
{
    /// <summary>
    /// 球与桌子的交互处理扩展
    /// 处理基于TablePart的精确碰撞检测和音效播放
    /// </summary>
    public class BallTableInteraction : MonoBehaviour
    {
        [Header("交互配置")]
        [SerializeField]
        [Tooltip("Enable Table Part Detection / 启用桌子部件检测 - Use TablePart system for collision detection")]
        private bool m_enableTablePartDetection = true;

        [SerializeField]
        [Tooltip("Fallback to Tag Detection / 回退到Tag检测 - Use tag-based detection when TablePart is not available")]
        private bool m_fallbackToTagDetection = true;

        [Header("音效配置")]
        [SerializeField]
        [Tooltip("Volume Multiplier / 音量倍数 - Multiplier for all table hit sounds")]
        [Range(0f, 2f)]
        private float m_volumeMultiplier = 1.0f;

        [SerializeField]
        [Tooltip("Min Force for Sound / 最小音效触发力度 - Minimum force to trigger hit sounds")]
        private float m_minForceForSound = 0.5f;

        [Header("调试设置")]
        [SerializeField]
        [Tooltip("Show Debug Info / 显示调试信息 - Whether to show debug information")]
        private bool m_showDebugInfo = false;

        // 缓存的组件引用
        private Ball m_ball;
        private Rigidbody m_rigidbody;

        // 统计数据
        private int m_totalTableHits = 0;
        private float m_lastHitTime = 0f;

        #region Properties
        /// <summary>
        /// 总桌子击中次数
        /// </summary>
        public int TotalTableHits => m_totalTableHits;

        /// <summary>
        /// 最后击中时间
        /// </summary>
        public float LastHitTime => m_lastHitTime;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            ValidateConfiguration();
        }
        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            m_ball = GetComponent<Ball>();
            m_rigidbody = GetComponent<Rigidbody>();

            if (m_ball == null)
            {
                Debug.LogError("BallTableInteraction requires Ball component!", this);
            }

            if (m_rigidbody == null)
            {
                Debug.LogError("BallTableInteraction requires Rigidbody component!", this);
            }
        }

        private void ValidateConfiguration()
        {
            if (!m_enableTablePartDetection && !m_fallbackToTagDetection)
            {
                Debug.LogWarning("Both TablePart and Tag detection are disabled. No table collision will be detected.", this);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 处理与桌子的碰撞
        /// </summary>
        /// <param name="collision">碰撞信息</param>
        /// <returns>是否成功处理了桌子碰撞</returns>
        public bool HandleTableCollision(Collision collision)
        {
            var hitObject = collision.gameObject;
            var contactPoint = collision.contacts[0].point;
            var hitForce = collision.relativeVelocity.magnitude;

            LogDebug($"Processing collision with {hitObject.name}, force: {hitForce:F2}");

            // 检查力度是否足够触发音效
            if (hitForce < m_minForceForSound)
            {
                LogDebug("Hit force too low, skipping sound");
                return false;
            }

            // 尝试使用TablePart系统
            if (m_enableTablePartDetection)
            {
                var tablePart = collision.collider.GetComponent<TablePart>();
                if (tablePart != null)
                {
                    HandleTablePartCollision(tablePart, collision);
                    return true;
                }
                else
                {
                    LogDebug("No TablePart component found");
                }
            }

            // 回退到Tag检测
            if (m_fallbackToTagDetection)
            {
                return HandleTagBasedCollision(collision);
            }

            return false;
        }

        /// <summary>
        /// 重置统计数据
        /// </summary>
        public void ResetStatistics()
        {
            m_totalTableHits = 0;
            m_lastHitTime = 0f;
            LogDebug("Statistics reset");
        }

        /// <summary>
        /// 获取交互统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetStatistics()
        {
            return $"Total table hits: {m_totalTableHits}, Last hit: {m_lastHitTime:F2}s ago";
        }
        #endregion

        #region Private Methods - TablePart System
        private void HandleTablePartCollision(TablePart tablePart, Collision collision)
        {
            var contactPoint = collision.contacts[0].point;
            var hitForce = collision.relativeVelocity.magnitude;

            LogDebug($"TablePart collision: {tablePart.PartType}, force: {hitForce:F2}");

            // 更新统计
            m_totalTableHits++;
            m_lastHitTime = Time.time;

            // 让TablePart处理碰撞（包括音效和物理效果）
            tablePart.HandleBallHit(collision, gameObject);

            // 额外的球特定处理
            HandleBallSpecificEffects(tablePart.PartType, collision);

            // 检查特殊规则
            if (tablePart.CausesBallDeath)
            {
                HandleBallDeath(tablePart.PartType);
            }
        }

        private void HandleBallSpecificEffects(TablePartType partType, Collision collision)
        {
            var contactPoint = collision.contacts[0].point;
            var velocity = collision.relativeVelocity;

            switch (partType)
            {
                case TablePartType.Surface:
                    // 桌面击中 - 可能产生旋转效果
                    if (m_ball?.Spin != null)
                    {
                        // 根据击中角度添加旋转
                        Vector3 spinAxis = Vector3.Cross(velocity.normalized, collision.contacts[0].normal);
                        m_ball.Spin.AddSpin(spinAxis, velocity.magnitude * 0.1f);
                    }
                    break;

                case TablePartType.Net:
                    // 球网击中 - 减少速度
                    if (m_rigidbody != null)
                    {
                        m_rigidbody.velocity *= 0.3f; // 大幅减速
                    }
                    break;

                case TablePartType.Edge:
                    // 边缘击中 - 特殊反弹
                    break;

                case TablePartType.Leg:
                case TablePartType.Support:
                    // 结构件击中 - 可能产生特殊音效
                    break;
            }

            // 播放粒子效果
            if (m_ball?.Particles != null)
            {
                m_ball.Particles.PlayHitParticles(contactPoint, velocity);
            }
        }
        #endregion

        #region Private Methods - Fallback Tag System
        private bool HandleTagBasedCollision(Collision collision)
        {
            var hitObject = collision.gameObject;
            var contactPoint = collision.contacts[0].point;
            var hitForce = collision.relativeVelocity.magnitude;

            LogDebug($"Tag-based collision with {hitObject.tag}");

            // 更新统计
            m_totalTableHits++;
            m_lastHitTime = Time.time;

            // 基于Tag播放音效
            PlayTagBasedAudio(hitObject, contactPoint, hitForce);

            return true;
        }

        private void PlayTagBasedAudio(GameObject hitObject, Vector3 position, float force)
        {
            if (AudioManager.Instance == null) return;

            float adjustedVolume = m_volumeMultiplier;

            if (hitObject.CompareTag("Table"))
            {
                AudioManager.Instance.PlayTableHit(position, force * adjustedVolume);
            }
            else if (hitObject.CompareTag("Net"))
            {
                AudioManager.Instance.PlayNetHit(position, force * adjustedVolume);
            }
            else if (hitObject.CompareTag("Edge"))
            {
                AudioManager.Instance.PlayEdgeHit(position, force * adjustedVolume);
            }
            else
            {
                // 通用桌子音效
                AudioManager.Instance.PlayTableHit(position, force * adjustedVolume);
            }
        }
        #endregion

        #region Private Methods - Ball Death Handling
        private void HandleBallDeath(TablePartType partType)
        {
            LogDebug($"Ball death caused by hitting {partType}");

            // 通知Ball主组件
            if (m_ball != null)
            {
                // 可以调用Ball的死亡处理方法
                // m_ball.HandleDeath();
            }

            // 通知网络系统
            if (m_ball?.Networking != null)
            {
                // m_ball.Networking.NotifyBallDeath();
            }

            // 播放死亡音效
            if (AudioManager.Instance != null)
            {
                // 使用出界音效作为球死亡音效
                AudioManager.Instance.PlayBallOutOfBounds(transform.position);
            }
        }
        #endregion

        #region Utility Methods
        private void LogDebug(string message)
        {
            if (m_showDebugInfo)
            {
                Debug.Log($"[BallTableInteraction] {message}", this);
            }
        }

        /// <summary>
        /// 判断碰撞对象是否为桌子相关
        /// </summary>
        /// <param name="hitObject">碰撞对象</param>
        /// <returns>是否为桌子相关对象</returns>
        public static bool IsTableRelated(GameObject hitObject)
        {
            // 检查TablePart组件
            if (hitObject.GetComponent<TablePart>() != null)
                return true;

            // 检查父对象中的Table组件
            if (hitObject.GetComponentInParent<Table.Table>() != null)
                return true;

            // 检查常用Tag
            return hitObject.CompareTag("Table") || 
                   hitObject.CompareTag("Net") || 
                   hitObject.CompareTag("Edge");
        }
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        [ContextMenu("Test Table Collision")]
        private void TestTableCollisionEditor()
        {
            Debug.Log($"Ball Table Interaction Configuration:\n" +
                     $"TablePart Detection: {m_enableTablePartDetection}\n" +
                     $"Tag Fallback: {m_fallbackToTagDetection}\n" +
                     $"Volume Multiplier: {m_volumeMultiplier}\n" +
                     $"Min Force: {m_minForceForSound}");
        }

        [ContextMenu("Reset Statistics")]
        private void ResetStatisticsEditor()
        {
            ResetStatistics();
            Debug.Log("Ball table interaction statistics reset");
        }
#endif
        #endregion
    }
}