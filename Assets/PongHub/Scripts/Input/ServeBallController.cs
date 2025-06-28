using UnityEngine;

namespace PongHub.Input
{
    /// <summary>
    /// 发球控制器
    /// 处理非持拍手的发球逻辑和球体生成
    /// </summary>
    public class ServeBallController : MonoBehaviour
    {
        [Header("发球设置")]
        [SerializeField] private GameObject m_ballPrefab;           // 球体预制件
        [SerializeField] private float m_ballGenerationHeight = 0.2f; // 球生成高度偏移
        [SerializeField] private float m_ballLifetime = 30f;        // 球体生存时间
        [SerializeField] private int m_maxActiveBalls = 3;          // 最大活跃球数

        [Header("发球规则设置")]
        [SerializeField] private bool m_enforceServingRules = true; // 是否强制发球规则
        [SerializeField] private float m_minThrowHeight = 0.16f;    // 最小抛球高度（16cm）
        [SerializeField] private float m_maxThrowAngle = 15f;       // 最大抛球角度偏差

        [Header("物理设置")]
        [SerializeField] private float m_initialVelocityMultiplier = 1f; // 初始速度倍数
        [SerializeField] private PhysicMaterial m_ballPhysicMaterial;    // 球体物理材质

        [Header("音效")]
        [SerializeField] private AudioClip m_ballGenerateSound;     // 球生成音效
        [SerializeField] private AudioClip m_invalidServeSound;     // 无效发球音效

        [Header("组件引用")]
        [SerializeField] private Transform m_leftHandAnchor;        // 左手锚点
        [SerializeField] private Transform m_rightHandAnchor;       // 右手锚点

        // 私有变量
        private GameObject[] m_activeBalls;
        private int m_currentBallIndex = 0;
        private AudioSource m_audioSource;

        // 发球状态跟踪
        private Vector3 m_lastHandPosition;
        private Vector3 m_handVelocity;
        private bool m_isTrackingThrow = false;

        private void Awake()
        {
            InitializeServeBallController();
        }

        private void Start()
        {
            SetupBallPool();
        }

        /// <summary>
        /// 初始化发球控制器
        /// </summary>
        private void InitializeServeBallController()
        {
            // 初始化活跃球数组
            m_activeBalls = new GameObject[m_maxActiveBalls];

            // 添加音频源
            m_audioSource = GetComponent<AudioSource>();
            if (m_audioSource == null)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }
            m_audioSource.playOnAwake = false;
            m_audioSource.spatialBlend = 0f; // 2D音效

            Debug.Log("ServeBallController 初始化完成");
        }

        /// <summary>
        /// 设置球体对象池
        /// </summary>
        private void SetupBallPool()
        {
            if (m_ballPrefab == null)
            {
                Debug.LogError("ServeBallController: 球体预制件未指定!");
                return;
            }

            // 预创建球体（对象池）
            for (int i = 0; i < m_maxActiveBalls; i++)
            {
                GameObject ball = Instantiate(m_ballPrefab, transform);
                ball.name = $"ServeBall_{i}";
                ball.SetActive(false);
                m_activeBalls[i] = ball;

                // 设置物理材质
                if (m_ballPhysicMaterial != null)
                {
                    Collider ballCollider = ball.GetComponent<Collider>();
                    if (ballCollider != null)
                    {
                        ballCollider.material = m_ballPhysicMaterial;
                    }
                }
            }
        }

        /// <summary>
        /// 生成发球
        /// </summary>
        /// <param name="isLeftHand">是否为左手发球</param>
        public void GenerateServeBall(bool isLeftHand)
        {
            Transform handAnchor = isLeftHand ? m_leftHandAnchor : m_rightHandAnchor;

            if (handAnchor == null)
            {
                Debug.LogError($"ServeBallController: {(isLeftHand ? "左" : "右")}手锚点未指定!");
                return;
            }

            // 检查发球条件
            if (!CanServe(isLeftHand))
            {
                PlayInvalidServeSound();
                return;
            }

            // 生成球体
            GameObject ball = GetNextAvailableBall();
            if (ball == null)
            {
                Debug.LogWarning("ServeBallController: 已达到最大活跃球数限制");
                return;
            }

            // 设置球体位置和状态
            SetupBall(ball, handAnchor, isLeftHand);

            // 播放音效
            PlayBallGenerateSound();

            Debug.Log($"{(isLeftHand ? "左" : "右")}手发球成功");
        }

        /// <summary>
        /// 检查是否可以发球
        /// </summary>
        private bool CanServe(bool isLeftHand)
        {
            // 检查持拍状态（非持拍手才能发球）
            PongHubInputManager inputManager = PongHubInputManager.Instance;
            if (inputManager == null) return true; // 如果没有输入管理器，允许发球

            bool isHoldingPaddle = isLeftHand ? inputManager.IsLeftPaddleGripped : inputManager.IsRightPaddleGripped;
            if (isHoldingPaddle)
            {
                Debug.Log($"发球失败: {(isLeftHand ? "左" : "右")}手正在持拍");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取下一个可用球体
        /// </summary>
        private GameObject GetNextAvailableBall()
        {
            // 从当前索引开始查找未激活的球
            for (int i = 0; i < m_maxActiveBalls; i++)
            {
                int index = (m_currentBallIndex + i) % m_maxActiveBalls;
                if (m_activeBalls[index] != null && !m_activeBalls[index].activeInHierarchy)
                {
                    m_currentBallIndex = (index + 1) % m_maxActiveBalls;
                    return m_activeBalls[index];
                }
            }

            // 如果所有球都在使用，回收最老的球
            GameObject oldestBall = m_activeBalls[m_currentBallIndex];
            DeactivateBall(oldestBall);
            m_currentBallIndex = (m_currentBallIndex + 1) % m_maxActiveBalls;
            return oldestBall;
        }

        /// <summary>
        /// 设置球体
        /// </summary>
        private void SetupBall(GameObject ball, Transform handAnchor, bool isLeftHand)
        {
            // 设置球体位置（手部锚点上方）
            Vector3 spawnPosition = handAnchor.position + Vector3.up * m_ballGenerationHeight;
            ball.transform.position = spawnPosition;
            ball.transform.rotation = Quaternion.identity;

            // 激活球体
            ball.SetActive(true);

            // 设置初始物理状态
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.velocity = Vector3.zero;
                ballRb.angularVelocity = Vector3.zero;

                // 如果启用发球规则，给球一个小的向上初速度
                if (m_enforceServingRules)
                {
                    Vector3 initialVelocity = Vector3.up * 0.5f; // 轻微向上的速度
                    ballRb.velocity = initialVelocity * m_initialVelocityMultiplier;
                }
            }

            // 添加自动销毁组件
            AutoDestroyBall autoDestroy = ball.GetComponent<AutoDestroyBall>();
            if (autoDestroy == null)
            {
                autoDestroy = ball.AddComponent<AutoDestroyBall>();
            }
            autoDestroy.Initialize(m_ballLifetime, this);

            Debug.Log($"球体已生成在 {(isLeftHand ? "左" : "右")}手位置: {spawnPosition}");
        }

        /// <summary>
        /// 停用球体
        /// </summary>
        public void DeactivateBall(GameObject ball)
        {
            if (ball != null)
            {
                ball.SetActive(false);

                // 重置物理状态
                Rigidbody ballRb = ball.GetComponent<Rigidbody>();
                if (ballRb != null)
                {
                    ballRb.velocity = Vector3.zero;
                    ballRb.angularVelocity = Vector3.zero;
                }
            }
        }

        /// <summary>
        /// 清理所有活跃球体
        /// </summary>
        public void ClearAllBalls()
        {
            for (int i = 0; i < m_maxActiveBalls; i++)
            {
                if (m_activeBalls[i] != null)
                {
                    DeactivateBall(m_activeBalls[i]);
                }
            }

            Debug.Log("所有发球已清理");
        }

        /// <summary>
        /// 播放球生成音效
        /// </summary>
        private void PlayBallGenerateSound()
        {
            if (m_ballGenerateSound != null && m_audioSource != null)
            {
                m_audioSource.PlayOneShot(m_ballGenerateSound);
            }
        }

        /// <summary>
        /// 播放无效发球音效
        /// </summary>
        private void PlayInvalidServeSound()
        {
            if (m_invalidServeSound != null && m_audioSource != null)
            {
                m_audioSource.PlayOneShot(m_invalidServeSound);
            }
        }

        #region 公共属性和方法

        /// <summary>
        /// 获取当前活跃球数量
        /// </summary>
        public int GetActiveBallCount()
        {
            int count = 0;
            for (int i = 0; i < m_maxActiveBalls; i++)
            {
                if (m_activeBalls[i] != null && m_activeBalls[i].activeInHierarchy)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 设置球体预制件
        /// </summary>
        public void SetBallPrefab(GameObject ballPrefab)
        {
            m_ballPrefab = ballPrefab;
            SetupBallPool(); // 重新设置球池
        }

        /// <summary>
        /// 设置最大活跃球数
        /// </summary>
        public void SetMaxActiveBalls(int maxBalls)
        {
            m_maxActiveBalls = Mathf.Max(1, maxBalls);
            SetupBallPool(); // 重新设置球池
        }

        #endregion
    }

    /// <summary>
    /// 球体自动销毁组件
    /// </summary>
    public class AutoDestroyBall : MonoBehaviour
    {
        private float m_lifetime;
        private float m_spawnTime;
        private ServeBallController m_controller;

        public void Initialize(float lifetime, ServeBallController controller)
        {
            m_lifetime = lifetime;
            m_spawnTime = Time.time;
            m_controller = controller;
        }

        private void Update()
        {
            if (Time.time - m_spawnTime >= m_lifetime)
            {
                m_controller?.DeactivateBall(gameObject);
            }
        }
    }
}