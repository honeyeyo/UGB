using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PongHub.UI
{
    /// <summary>
    /// 菜单交互距离测试器
    /// 用于测试不同距离下的VR菜单交互体验
    /// </summary>
    public class MenuInteractionDistanceTester : MonoBehaviour
    {
        [Header("测试设置")]
        [SerializeField] private VRMenuInteraction m_menuInteraction;
        [SerializeField] private MenuInputHandler m_menuInputHandler;
        [SerializeField] private Transform m_menuTransform;

        [Header("测试距离")]
        [SerializeField] private float[] m_testDistances = new float[] { 1.0f, 2.5f, 5.0f, 7.5f };
        [SerializeField] private string[] m_distanceLabels = new string[] { "近距离", "中距离", "远距离", "超远距离" };

        [Header("UI显示")]
        [SerializeField] private TextMeshProUGUI m_distanceText;
        [SerializeField] private TextMeshProUGUI m_statusText;
        [SerializeField] private Button m_nextDistanceButton;
        [SerializeField] private Button m_prevDistanceButton;
        [SerializeField] private Button m_resetStatsButton;

        // 当前测试距离索引
        private int m_currentDistanceIndex = 0;
        // 测试数据收集
        private Dictionary<int, int> m_interactionSuccessCount = new Dictionary<int, int>();
        private Dictionary<int, int> m_interactionTotalCount = new Dictionary<int, int>();

        #region Unity生命周期

        private void Awake()
        {
            // 初始化测试数据
            InitializeTestData();
        }

        private void Start()
        {
            // 设置初始距离
            SetTestDistance(m_currentDistanceIndex);

            // 注册按钮事件
            if (m_nextDistanceButton != null)
            {
                m_nextDistanceButton.onClick.AddListener(NextDistance);
            }

                        if (m_prevDistanceButton != null)
            {
                m_prevDistanceButton.onClick.AddListener(PreviousDistance);
            }

            if (m_resetStatsButton != null)
            {
                m_resetStatsButton.onClick.AddListener(ResetStats);
            }

            // 注册菜单交互事件
            if (m_menuInteraction != null)
            {
                m_menuInteraction.OnUISelected += OnUIInteraction;
            }
        }

        private void OnDestroy()
        {
            // 取消注册事件
            if (m_menuInteraction != null)
            {
                m_menuInteraction.OnUISelected -= OnUIInteraction;
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 切换到下一个测试距离
        /// </summary>
        public void NextDistance()
        {
            m_currentDistanceIndex = (m_currentDistanceIndex + 1) % m_testDistances.Length;
            SetTestDistance(m_currentDistanceIndex);
        }

        /// <summary>
        /// 切换到上一个测试距离
        /// </summary>
        public void PreviousDistance()
        {
            m_currentDistanceIndex = (m_currentDistanceIndex - 1 + m_testDistances.Length) % m_testDistances.Length;
            SetTestDistance(m_currentDistanceIndex);
        }

        /// <summary>
        /// 记录交互成功
        /// </summary>
        public void RecordInteractionSuccess()
        {
            m_interactionSuccessCount[m_currentDistanceIndex]++;
            m_interactionTotalCount[m_currentDistanceIndex]++;
            UpdateStatusText();
        }

                /// <summary>
        /// 记录交互失败
        /// </summary>
        public void RecordInteractionFailure()
        {
            m_interactionTotalCount[m_currentDistanceIndex]++;
            UpdateStatusText();
        }

        /// <summary>
        /// 重置统计数据
        /// </summary>
        public void ResetStats()
        {
            // 重置当前距离的统计数据
            m_interactionSuccessCount[m_currentDistanceIndex] = 0;
            m_interactionTotalCount[m_currentDistanceIndex] = 0;
            UpdateStatusText();
        }

        /// <summary>
        /// 重置所有统计数据
        /// </summary>
        public void ResetAllStats()
        {
            // 重置所有距离的统计数据
            InitializeTestData();
            UpdateStatusText();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化测试数据
        /// </summary>
        private void InitializeTestData()
        {
            for (int i = 0; i < m_testDistances.Length; i++)
            {
                m_interactionSuccessCount[i] = 0;
                m_interactionTotalCount[i] = 0;
            }
        }

                /// <summary>
        /// 设置测试距离
        /// </summary>
        private void SetTestDistance(int distanceIndex)
        {
            if (distanceIndex >= 0 && distanceIndex < m_testDistances.Length)
            {
                // 设置菜单位置
                if (m_menuTransform != null)
                {
                    Vector3 position = m_menuTransform.position;
                    position.z = m_testDistances[distanceIndex];
                    m_menuTransform.position = position;
                }

                // 设置交互距离阈值
                if (m_menuInteraction != null)
                {
                    // 设置交互距离为当前测试距离的1.2倍，以便测试边界情况
                    float interactionDistance = m_testDistances[distanceIndex] * 1.2f;
                    m_menuInteraction.SetInteractionDistance(interactionDistance);
                }

                // 更新UI显示
                UpdateDistanceText();
                UpdateStatusText();
            }
        }

        /// <summary>
        /// 更新距离文本显示
        /// </summary>
        private void UpdateDistanceText()
        {
            if (m_distanceText != null)
            {
                string distanceLabel = m_distanceLabels[m_currentDistanceIndex];
                float distance = m_testDistances[m_currentDistanceIndex];
                m_distanceText.text = $"当前测试距离: {distanceLabel} ({distance}m)";
            }
        }

        /// <summary>
        /// 更新状态文本显示
        /// </summary>
        private void UpdateStatusText()
        {
            if (m_statusText != null)
            {
                int successCount = m_interactionSuccessCount[m_currentDistanceIndex];
                int totalCount = m_interactionTotalCount[m_currentDistanceIndex];
                float successRate = totalCount > 0 ? (float)successCount / totalCount * 100f : 0f;

                m_statusText.text = $"交互成功率: {successCount}/{totalCount} ({successRate:F1}%)";
            }
        }

        /// <summary>
        /// UI交互事件处理
        /// </summary>
        private void OnUIInteraction(RaycastResult result, bool isLeft)
        {
            // 检查交互是否在有效距离内
            bool isDistanceValid = m_menuInteraction.IsInteractionDistanceValid();

            if (isDistanceValid)
            {
                RecordInteractionSuccess();
            }
            else
            {
                RecordInteractionFailure();
            }
        }

        #endregion
    }
}