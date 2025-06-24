using UnityEngine;
using PongHub.Gameplay.Ball;
using PongHub.Gameplay.Paddle;
using PongHub.Core;

namespace PongHub.AI
{
    /// <summary>
    /// 单人模式AI系统
    /// 控制AI对手的行为和决策
    /// </summary>
    public class AISingle : MonoBehaviour
    {
        [Header("AI配置")]
        [SerializeField] private float m_difficulty = 0.5f;
        [SerializeField] private Paddle m_aiPaddle;
        [SerializeField] private Transform m_aiPaddleTransform;

        [Header("AI行为参数")]
        [SerializeField] private float m_reactionTime = 0.2f;
        [SerializeField] private float m_maxSpeed = 5f;
        [SerializeField] private float m_predictionAccuracy = 0.8f;
        [SerializeField] private float m_errorRange = 0.1f;

        [Header("目标区域")]
        [SerializeField] private Vector3 m_aiSideCenter = new Vector3(0, 0, -1.5f);
        [SerializeField] private float m_aiSideWidth = 1.5f;
        [SerializeField] private float m_aiSideLength = 1.37f;

        [Header("调试设置")]
        [SerializeField] private bool m_enableDebugLog = false;
        [SerializeField] private bool m_showAIGizmos = false;
        [SerializeField] private bool m_enableAIVisualization = false;

        // AI状态
        private AIState m_currentState = AIState.Idle;
        private Ball m_targetBall;
        private Vector3 m_targetPosition;
        private Vector3 m_predictedBallPosition;
        private float m_lastReactionTime;

        // AI决策
        private bool m_isActive = false;
        private bool m_isInitialized = false;
        private float m_nextReactionTime = 0f;

        // 性能统计
        private int m_totalHits = 0;
        private int m_missedHits = 0;
        // private float m_averageReactionTime = 0f;

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeAI();
        }

        private void Start()
        {
            StartAI();
        }

        private void Update()
        {
            if (m_isActive && m_isInitialized)
            {
                UpdateAI();
            }
        }

        private void OnDrawGizmos()
        {
            if (m_showAIGizmos)
            {
                DrawAIGizmos();
            }
        }
        #endregion

        #region Initialization
        private void InitializeAI()
        {
            // 查找AI球拍
            if (m_aiPaddle == null)
            {
                m_aiPaddle = FindObjectOfType<Paddle>();
            }

            if (m_aiPaddle != null)
            {
                m_aiPaddleTransform = m_aiPaddle.transform;
            }

            // 查找目标球
            FindTargetBall();

            m_isInitialized = true;
            LogDebug("AI initialized successfully");
        }

        private void StartAI()
        {
            if (!m_isInitialized) return;

            SetAIState(AIState.Waiting);
            LogDebug("AI started");
        }

        private void FindTargetBall()
        {
            var ballManager = FindObjectOfType<BallSingleManager>();
            if (ballManager != null)
            {
                m_targetBall = ballManager.CurrentBall;
            }

            if (m_targetBall == null)
            {
                m_targetBall = FindObjectOfType<Ball>();
            }
        }
        #endregion

        #region AI State Management
        private void UpdateAI()
        {
            // 确保有目标球
            if (m_targetBall == null)
            {
                FindTargetBall();
                return;
            }

            // 根据AI状态执行不同逻辑
            switch (m_currentState)
            {
                case AIState.Idle:
                    UpdateIdleState();
                    break;
                case AIState.Waiting:
                    UpdateWaitingState();
                    break;
                case AIState.Tracking:
                    UpdateTrackingState();
                    break;
                case AIState.Moving:
                    UpdateMovingState();
                    break;
                case AIState.Attacking:
                    UpdateAttackingState();
                    break;
            }

            // 更新AI可视化
            if (m_enableAIVisualization)
            {
                UpdateVisualization();
            }
        }

        private void UpdateIdleState()
        {
            // 空闲状态：等待球进入AI区域
            if (IsBallInAIZone())
            {
                SetAIState(AIState.Tracking);
            }
        }

        private void UpdateWaitingState()
        {
            // 等待状态：准备接球
            if (IsBallMovingTowardsAI())
            {
                SetAIState(AIState.Tracking);
            }
        }

        private void UpdateTrackingState()
        {
            // 跟踪状态：预测球的位置
            PredictBallPosition();

            if (ShouldMoveToIntercept())
            {
                SetAIState(AIState.Moving);
            }
        }

        private void UpdateMovingState()
        {
            // 移动状态：移动到拦截位置
            MoveToTarget();

            if (IsCloseToTarget() && CanHitBall())
            {
                SetAIState(AIState.Attacking);
            }
        }

        private void UpdateAttackingState()
        {
            // 攻击状态：击球
            AttemptHit();
            SetAIState(AIState.Waiting);
        }

        private void SetAIState(AIState newState)
        {
            if (m_currentState != newState)
            {
                LogDebug($"AI state changed: {m_currentState} -> {newState}");
                m_currentState = newState;
            }
        }
        #endregion

        #region AI Decision Making
        private bool IsBallInAIZone()
        {
            if (m_targetBall == null) return false;

            Vector3 ballPos = m_targetBall.transform.position;
            Vector3 aiCenter = m_aiSideCenter;

            return Mathf.Abs(ballPos.x - aiCenter.x) <= m_aiSideWidth / 2f &&
                   ballPos.z <= aiCenter.z + m_aiSideLength / 2f &&
                   ballPos.z >= aiCenter.z - m_aiSideLength / 2f;
        }

        private bool IsBallMovingTowardsAI()
        {
            if (m_targetBall == null) return false;

            var ballPhysics = m_targetBall.GetComponent<BallPhysics>();
            if (ballPhysics == null) return false;

            // 检查球是否朝AI方向移动
            Vector3 ballVelocity = ballPhysics.Velocity;
            Vector3 ballPosition = m_targetBall.transform.position;
            Vector3 toAI = m_aiSideCenter - ballPosition;

            return Vector3.Dot(ballVelocity.normalized, toAI.normalized) > 0.5f;
        }

        private void PredictBallPosition()
        {
            if (m_targetBall == null) return;

            var ballPhysics = m_targetBall.GetComponent<BallPhysics>();
            if (ballPhysics == null) return;

            // 简单的线性预测
            Vector3 ballPos = m_targetBall.transform.position;
            Vector3 ballVel = ballPhysics.Velocity;

            // 预测球到达AI区域的时间
            float timeToReachAI = CalculateTimeToReachAI(ballPos, ballVel);

            // 预测球的位置
            m_predictedBallPosition = ballPos + ballVel * timeToReachAI;

            // 添加一些误差（基于难度）
            AddPredictionError();
        }

        private float CalculateTimeToReachAI(Vector3 ballPos, Vector3 ballVel)
        {
            if (Mathf.Abs(ballVel.z) < 0.1f) return 0f;

            float distanceToAI = Mathf.Abs(m_aiSideCenter.z - ballPos.z);
            return distanceToAI / Mathf.Abs(ballVel.z);
        }

        private void AddPredictionError()
        {
            // 基于难度添加预测误差
            float errorMultiplier = 1f - m_predictionAccuracy;
            float errorX = Random.Range(-m_errorRange, m_errorRange) * errorMultiplier;
            float errorZ = Random.Range(-m_errorRange, m_errorRange) * errorMultiplier;

            m_predictedBallPosition += new Vector3(errorX, 0, errorZ);
        }

        private bool ShouldMoveToIntercept()
        {
            if (Time.time < m_nextReactionTime) return false;

            // 检查是否需要移动
            float distanceToTarget = Vector3.Distance(m_aiPaddleTransform.position, m_predictedBallPosition);
            return distanceToTarget > 0.2f;
        }

        private void MoveToTarget()
        {
            if (m_aiPaddleTransform == null) return;

            // 计算目标位置（只在X轴移动）
            Vector3 currentPos = m_aiPaddleTransform.position;
            Vector3 targetPos = new Vector3(m_predictedBallPosition.x, currentPos.y, currentPos.z);

            // 限制在AI区域内
            targetPos.x = Mathf.Clamp(targetPos.x,
                m_aiSideCenter.x - m_aiSideWidth / 2f,
                m_aiSideCenter.x + m_aiSideWidth / 2f);

            // 平滑移动
            float moveSpeed = m_maxSpeed * m_difficulty;
            Vector3 newPos = Vector3.MoveTowards(currentPos, targetPos, moveSpeed * Time.deltaTime);
            m_aiPaddleTransform.position = newPos;

            m_targetPosition = targetPos;
        }

        private bool IsCloseToTarget()
        {
            if (m_aiPaddleTransform == null) return false;

            float distance = Vector3.Distance(m_aiPaddleTransform.position, m_targetPosition);
            return distance < 0.1f;
        }

        private bool CanHitBall()
        {
            if (m_targetBall == null || m_aiPaddleTransform == null) return false;

            float distanceToBall = Vector3.Distance(m_aiPaddleTransform.position, m_targetBall.transform.position);
            return distanceToBall < 0.3f; // 击球范围
        }

        private void AttemptHit()
        {
            if (m_targetBall == null || m_aiPaddle == null) return;

            // 模拟击球
            var ballPhysics = m_targetBall.GetComponent<BallPhysics>();
            if (ballPhysics != null)
            {
                // 计算击球方向（朝玩家方向）
                Vector3 hitDirection = (Vector3.zero - m_targetBall.transform.position).normalized;
                hitDirection.y = 0.2f; // 添加一些向上的力

                // 根据难度调整击球力度
                float hitForce = 3f + m_difficulty * 2f;

                // 应用击球力
                ballPhysics.SetVelocity(hitDirection * hitForce);

                m_totalHits++;
                LogDebug($"AI hit ball with force {hitForce}");
            }

            // 设置下次反应时间
            m_nextReactionTime = Time.time + m_reactionTime * (2f - m_difficulty);
        }
        #endregion

        #region Public Interface
        public void StartAIMatch()
        {
            m_isActive = true;
            SetAIState(AIState.Waiting);
            LogDebug("AI match started");
        }

        public void StopAIMatch()
        {
            m_isActive = false;
            SetAIState(AIState.Idle);
            LogDebug("AI match stopped");
        }

        public void SetDifficulty(float difficulty)
        {
            m_difficulty = Mathf.Clamp01(difficulty);

            // 根据难度调整参数
            m_reactionTime = Mathf.Lerp(0.5f, 0.1f, m_difficulty);
            m_predictionAccuracy = Mathf.Lerp(0.5f, 0.95f, m_difficulty);
            m_maxSpeed = Mathf.Lerp(3f, 8f, m_difficulty);

            LogDebug($"AI difficulty set to {m_difficulty:F2}");
        }

        public void SetAIPaddle(Paddle paddle)
        {
            m_aiPaddle = paddle;
            if (paddle != null)
            {
                m_aiPaddleTransform = paddle.transform;
            }
        }

        public void SetTargetBall(Ball ball)
        {
            m_targetBall = ball;
        }
        #endregion

        #region Properties
        public float Difficulty => m_difficulty;
        public AIState CurrentState => m_currentState;
        public bool IsActive => m_isActive;
        public int TotalHits => m_totalHits;
        public int MissedHits => m_missedHits;
        public float HitRate => m_totalHits > 0 ? (float)m_totalHits / (m_totalHits + m_missedHits) : 0f;
        #endregion

        #region Debug and Visualization
        private void UpdateVisualization()
        {
            // 更新AI可视化信息
            // 可以在这里更新UI显示AI状态等
        }

        private void DrawAIGizmos()
        {
            // 绘制AI区域
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(m_aiSideCenter, new Vector3(m_aiSideWidth, 0.1f, m_aiSideLength));

            // 绘制目标位置
            if (m_currentState == AIState.Moving || m_currentState == AIState.Attacking)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(m_targetPosition, 0.1f);
            }

            // 绘制预测位置
            if (m_currentState == AIState.Tracking)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(m_predictedBallPosition, 0.05f);
            }
        }

        private void LogDebug(string message)
        {
            if (m_enableDebugLog)
            {
                Debug.Log($"[AISingle] {message}");
            }
        }
        #endregion
    }

    /// <summary>
    /// AI状态枚举
    /// </summary>
    public enum AIState
    {
        Idle,       // 空闲
        Waiting,    // 等待
        Tracking,   // 跟踪球
        Moving,     // 移动到位
        Attacking   // 击球
    }
}