using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PongHub.UI.Testing;

namespace PongHub.UI
{
    /// <summary>
    /// 菜单交互距离测试器
    /// 用于测试不同距离下的菜单交互体验
    /// </summary>
    public class MenuInteractionDistanceTester : MonoBehaviour
    {
        [Header("测试设置")]
        [SerializeField] private Transform m_playerTransform;
        [SerializeField] private Transform m_tableTransform;
        [SerializeField] private float[] m_testDistances = new float[] { 1.0f, 2.5f, 5.0f };
        [SerializeField] private float m_testDuration = 60f; // 每个距离测试持续时间（秒）
        [SerializeField] private int m_tasksPerTest = 10; // 每次测试的任务数量

        [Header("UI元素")]
        [SerializeField] private GameObject m_testControlPanel;
        [SerializeField] private Button m_startTestButton;
        [SerializeField] private Button m_endTestButton;
        [SerializeField] private Button m_saveResultsButton;
        [SerializeField] private TextMeshProUGUI m_statusText;
        [SerializeField] private TextMeshProUGUI m_timerText;
        [SerializeField] private TextMeshProUGUI m_distanceText;
        [SerializeField] private TextMeshProUGUI m_taskText;
        [SerializeField] private Slider m_userRatingSlider;
        [SerializeField] private TextMeshProUGUI m_userRatingText;

        [Header("测试目标")]
        [SerializeField] private List<GameObject> m_testTargets = new List<GameObject>();
        [SerializeField] private Material m_highlightMaterial;
        [SerializeField] private Color m_normalHighlightColor = Color.yellow;
        [SerializeField] private Color m_successHighlightColor = Color.green;
        [SerializeField] private Color m_failureHighlightColor = Color.red;

        [Header("组件引用")]
        [SerializeField] private VRMenuInteraction m_menuInteraction;
        [SerializeField] private MenuDistanceWarningController m_distanceWarningController;
        [SerializeField] private InteractionTestVisualizer m_visualizer;

        // 私有变量
        private InteractionTestData m_testData;
        private int m_currentDistanceIndex = 0;
        private bool m_isTestRunning = false;
        private float m_testStartTime = 0f;
        private float m_testTimeRemaining = 0f;
        private int m_currentTaskIndex = 0;
        private GameObject m_currentTarget = null;
        private Material[] m_originalMaterials = null;
        private List<Renderer> m_currentRenderers = null;

        #region Unity生命周期

        private void Awake()
        {
            // 初始化测试数据
            m_testData = new InteractionTestData();

            // 查找必要组件
            if (m_playerTransform == null)
            {
                var playerRig = FindObjectOfType<OVRCameraRig>();
                if (playerRig != null)
                {
                    m_playerTransform = playerRig.transform;
                }
            }

            if (m_menuInteraction == null)
            {
                m_menuInteraction = FindObjectOfType<VRMenuInteraction>();
            }

            if (m_distanceWarningController == null)
            {
                m_distanceWarningController = FindObjectOfType<MenuDistanceWarningController>();
            }

            if (m_visualizer == null)
            {
                m_visualizer = FindObjectOfType<InteractionTestVisualizer>();
            }
        }

        private void Start()
        {
            // 初始化UI
            UpdateUI();

            // 注册按钮事件
            if (m_startTestButton != null)
            {
                m_startTestButton.onClick.AddListener(StartTest);
            }

            if (m_endTestButton != null)
            {
                m_endTestButton.onClick.AddListener(EndTest);
                m_endTestButton.interactable = false;
            }

            if (m_saveResultsButton != null)
            {
                m_saveResultsButton.onClick.AddListener(SaveResults);
                m_saveResultsButton.interactable = false;
            }

            if (m_userRatingSlider != null)
            {
                m_userRatingSlider.onValueChanged.AddListener(OnUserRatingChanged);
                m_userRatingSlider.interactable = false;
            }
        }

        private void Update()
        {
            if (m_isTestRunning)
            {
                // 更新计时器
                m_testTimeRemaining -= Time.deltaTime;
                if (m_testTimeRemaining <= 0f)
                {
                    CompleteCurrentTest();
                }

                // 更新UI
                UpdateTimerText();
            }
        }

        private void OnDestroy()
        {
            // 取消注册按钮事件
            if (m_startTestButton != null)
            {
                m_startTestButton.onClick.RemoveListener(StartTest);
            }

            if (m_endTestButton != null)
            {
                m_endTestButton.onClick.RemoveListener(EndTest);
            }

            if (m_saveResultsButton != null)
            {
                m_saveResultsButton.onClick.RemoveListener(SaveResults);
            }

            if (m_userRatingSlider != null)
            {
                m_userRatingSlider.onValueChanged.RemoveListener(OnUserRatingChanged);
            }

            // 恢复所有目标的材质
            RestoreTargetMaterials();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 开始测试
        /// </summary>
        public void StartTest()
        {
            if (m_isTestRunning)
                return;

            // 重置测试数据
            m_testData.Reset();

            // 开始第一个距离的测试
            m_currentDistanceIndex = 0;
            StartTestAtCurrentDistance();

            // 更新UI状态
            m_startTestButton.interactable = false;
            m_endTestButton.interactable = true;
            m_saveResultsButton.interactable = false;
            m_userRatingSlider.interactable = false;

            // 如果有距离警告控制器，暂时禁用它
            if (m_distanceWarningController != null)
            {
                m_distanceWarningController.ForceShowWarning(false);
            }
        }

        /// <summary>
        /// 结束测试
        /// </summary>
        public void EndTest()
        {
            if (!m_isTestRunning)
                return;

            // 完成当前测试
            CompleteCurrentTest();

            // 重置状态
            m_isTestRunning = false;
            m_currentDistanceIndex = 0;

            // 更新UI状态
            m_startTestButton.interactable = true;
            m_endTestButton.interactable = false;
            m_saveResultsButton.interactable = true;
            m_userRatingSlider.interactable = true;

            // 恢复目标材质
            RestoreTargetMaterials();

            // 更新状态文本
            m_statusText.text = "测试已结束";
            m_taskText.text = "请评分并保存结果";

            // 如果有可视化工具，更新显示
            if (m_visualizer != null)
            {
                m_visualizer.VisualizeData(m_testData);
            }

            // 如果有距离警告控制器，重新启用它
            if (m_distanceWarningController != null)
            {
                m_distanceWarningController.SetMaxInteractionDistance(m_testDistances[m_testDistances.Length - 1]);
            }
        }

        /// <summary>
        /// 保存测试结果
        /// </summary>
        public void SaveResults()
        {
            if (m_isTestRunning)
                return;

            // 添加用户评分
            m_testData.AddUserRating((int)m_userRatingSlider.value);

            // 生成报告
            string reportPath = m_testData.GenerateReport();

            // 更新状态文本
            m_statusText.text = $"结果已保存至: {reportPath}";

            // 重置UI
            m_userRatingSlider.value = 3;
            m_userRatingSlider.interactable = false;
            m_saveResultsButton.interactable = false;
        }

        /// <summary>
        /// 记录交互事件
        /// </summary>
        public void RecordInteraction(GameObject target, bool success, float responseTime)
        {
            if (!m_isTestRunning || m_currentTarget == null)
                return;

            // 检查是否是当前目标
            if (target == m_currentTarget)
            {
                // 记录数据
                float distance = m_testDistances[m_currentDistanceIndex];
                m_testData.AddInteraction(distance, success, responseTime);

                // 高亮显示结果
                HighlightTarget(success ? m_successHighlightColor : m_failureHighlightColor);

                // 短暂延迟后进入下一个任务
                StartCoroutine(NextTaskAfterDelay(0.5f));
            }
        }

        /// <summary>
        /// 设置测试距离
        /// </summary>
        public void SetTestDistance(int distanceIndex)
        {
            if (distanceIndex < 0 || distanceIndex >= m_testDistances.Length)
                return;

            m_currentDistanceIndex = distanceIndex;
            UpdateDistanceText();

            // 如果测试正在运行，更新玩家位置
            if (m_isTestRunning)
            {
                PositionPlayerAtCurrentDistance();
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 在当前距离开始测试
        /// </summary>
        private void StartTestAtCurrentDistance()
        {
            // 设置测试状态
            m_isTestRunning = true;
            m_testStartTime = Time.time;
            m_testTimeRemaining = m_testDuration;

            // 放置玩家在当前测试距离
            PositionPlayerAtCurrentDistance();

            // 更新UI
            UpdateDistanceText();
            UpdateTimerText();
            m_statusText.text = $"测试进行中 - 距离: {m_testDistances[m_currentDistanceIndex]}米";

            // 开始第一个任务
            m_currentTaskIndex = 0;
            StartNextTask();

            // 如果有菜单交互组件，更新其交互距离
            if (m_menuInteraction != null)
            {
                m_menuInteraction.SetInteractionDistance(m_testDistances[m_currentDistanceIndex] + 0.5f);
            }

            // 如果有距离警告控制器，更新其距离设置
            if (m_distanceWarningController != null)
            {
                m_distanceWarningController.SetMaxInteractionDistance(m_testDistances[m_currentDistanceIndex] + 0.5f);
            }
        }

        /// <summary>
        /// 完成当前距离的测试
        /// </summary>
        private void CompleteCurrentTest()
        {
            // 恢复目标材质
            RestoreTargetMaterials();

            // 收集当前距离的测试数据
            float distance = m_testDistances[m_currentDistanceIndex];
            m_testData.CompleteDistanceTest(distance);

            // 移动到下一个距离
            m_currentDistanceIndex++;
            if (m_currentDistanceIndex < m_testDistances.Length)
            {
                // 继续下一个距离的测试
                StartTestAtCurrentDistance();
            }
            else
            {
                // 所有距离测试完成
                EndTest();
            }
        }

        /// <summary>
        /// 开始下一个任务
        /// </summary>
        private void StartNextTask()
        {
            // 恢复上一个目标的材质
            RestoreTargetMaterials();

            // 检查是否已完成所有任务
            if (m_currentTaskIndex >= m_tasksPerTest)
            {
                CompleteCurrentTest();
                return;
            }

            // 随机选择一个目标
            if (m_testTargets.Count > 0)
            {
                int targetIndex = Random.Range(0, m_testTargets.Count);
                m_currentTarget = m_testTargets[targetIndex];

                // 高亮显示目标
                HighlightTarget(m_normalHighlightColor);

                // 更新任务文本
                m_taskText.text = $"任务 {m_currentTaskIndex + 1}/{m_tasksPerTest}: 点击高亮目标";

                // 增加任务索引
                m_currentTaskIndex++;
            }
        }

        /// <summary>
        /// 短暂延迟后进入下一个任务
        /// </summary>
        private IEnumerator NextTaskAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartNextTask();
        }

        /// <summary>
        /// 高亮显示当前目标
        /// </summary>
        private void HighlightTarget(Color highlightColor)
        {
            if (m_currentTarget == null || m_highlightMaterial == null)
                return;

            // 获取所有渲染器
            Renderer[] renderers = m_currentTarget.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            // 保存原始材质
            m_currentRenderers = new List<Renderer>(renderers);
            m_originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                m_originalMaterials[i] = renderers[i].material;

                // 创建高亮材质的实例
                Material highlightMat = new Material(m_highlightMaterial);
                highlightMat.color = highlightColor;
                renderers[i].material = highlightMat;
            }
        }

        /// <summary>
        /// 恢复目标的原始材质
        /// </summary>
        private void RestoreTargetMaterials()
        {
            if (m_currentRenderers == null || m_originalMaterials == null)
                return;

            for (int i = 0; i < m_currentRenderers.Count; i++)
            {
                if (m_currentRenderers[i] != null && i < m_originalMaterials.Length)
                {
                    m_currentRenderers[i].material = m_originalMaterials[i];
                }
            }

            m_currentRenderers = null;
            m_originalMaterials = null;
        }

        /// <summary>
        /// 放置玩家在当前测试距离
        /// </summary>
        private void PositionPlayerAtCurrentDistance()
        {
            if (m_playerTransform == null || m_tableTransform == null)
                return;

            float distance = m_testDistances[m_currentDistanceIndex];
            Vector3 direction = -m_tableTransform.forward; // 假设桌子的前方是玩家应该站立的位置
            Vector3 position = m_tableTransform.position + direction.normalized * distance;

            // 保持y坐标不变
            position.y = m_playerTransform.position.y;

            // 设置玩家位置
            m_playerTransform.position = position;

            // 让玩家面向桌子
            m_playerTransform.LookAt(new Vector3(m_tableTransform.position.x, m_playerTransform.position.y, m_tableTransform.position.z));
        }

        /// <summary>
        /// 更新距离文本
        /// </summary>
        private void UpdateDistanceText()
        {
            if (m_distanceText != null && m_currentDistanceIndex < m_testDistances.Length)
            {
                m_distanceText.text = $"当前距离: {m_testDistances[m_currentDistanceIndex]:F1}米";
            }
        }

        /// <summary>
        /// 更新计时器文本
        /// </summary>
        private void UpdateTimerText()
        {
            if (m_timerText != null)
            {
                int minutes = Mathf.FloorToInt(m_testTimeRemaining / 60f);
                int seconds = Mathf.FloorToInt(m_testTimeRemaining % 60f);
                m_timerText.text = $"剩余时间: {minutes:00}:{seconds:00}";
            }
        }

        /// <summary>
        /// 更新UI
        /// </summary>
        private void UpdateUI()
        {
            UpdateDistanceText();
            UpdateTimerText();

            if (m_statusText != null)
            {
                m_statusText.text = "准备测试";
            }

            if (m_taskText != null)
            {
                m_taskText.text = "点击开始测试按钮";
            }

            if (m_userRatingText != null && m_userRatingSlider != null)
            {
                OnUserRatingChanged(m_userRatingSlider.value);
            }
        }

        /// <summary>
        /// 用户评分变化事件
        /// </summary>
        private void OnUserRatingChanged(float value)
        {
            if (m_userRatingText != null)
            {
                int rating = Mathf.RoundToInt(value);
                string[] ratingTexts = { "非常差", "较差", "一般", "良好", "优秀" };
                m_userRatingText.text = $"评分: {rating} - {ratingTexts[rating - 1]}";
            }
        }

        #endregion
    }
}