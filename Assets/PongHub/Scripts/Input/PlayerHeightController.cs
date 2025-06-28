using UnityEngine;
using System.Collections;

namespace PongHub.Input
{
    /// <summary>
    /// 玩家高度调整控制器
    /// 通过左手X/Y键精确调整玩家相对地面的高度
    /// </summary>
    public class PlayerHeightController : MonoBehaviour
    {
        [Header("高度调整设置")]
        [SerializeField] private float m_heightSpeed = 0.01f; // 1cm/s = 0.01m/s
        [SerializeField] private float m_minHeightOffset = -0.5f; // 最低高度偏移
        [SerializeField] private float m_maxHeightOffset = 2.0f;  // 最高高度偏移

        [Header("组件引用")]
        [SerializeField] private Transform m_playerRig; // OVRCameraRig或XR Rig

        [Header("调试信息")]
        [SerializeField] private bool m_showDebugInfo = true;
        [SerializeField] private float m_currentHeightOffset = 0f;

        // 私有变量
        private Vector3 m_originalPosition;
        private bool m_isAdjustingHeight = false;
        private bool m_isAdjustingUp = false;
        private Coroutine m_heightAdjustmentCoroutine;

        // UI反馈（可选）
        [Header("UI反馈")]
        [SerializeField] private GameObject m_heightIndicatorUI;
        [SerializeField] private TMPro.TextMeshProUGUI m_heightText;

        private void Start()
        {
            InitializeHeightController();
        }

        private void OnValidate()
        {
            // 在编辑器中验证设置
            if (m_heightSpeed <= 0)
                m_heightSpeed = 0.01f;

            if (m_minHeightOffset > m_maxHeightOffset)
                m_minHeightOffset = m_maxHeightOffset - 0.1f;
        }

        /// <summary>
        /// 初始化高度控制器
        /// </summary>
        private void InitializeHeightController()
        {
            // 自动查找PlayerRig如果未指定
            if (m_playerRig == null)
            {
                // 尝试查找常见的VR Rig名称
                GameObject rig = GameObject.Find("OVRCameraRig") ??
                                GameObject.Find("XR Rig") ??
                                GameObject.Find("CameraRig");

                if (rig != null)
                {
                    m_playerRig = rig.transform;
                    Debug.Log($"PlayerHeightController: 自动找到Player Rig: {rig.name}");
                }
                else
                {
                    Debug.LogError("PlayerHeightController: 无法找到Player Rig，请手动指定!");
                    return;
                }
            }

            // 记录初始位置
            m_originalPosition = m_playerRig.position;
            m_currentHeightOffset = 0f;

            // 初始化UI
            UpdateHeightUI();

            Debug.Log($"PlayerHeightController 初始化完成，初始位置: {m_originalPosition}");
        }

        /// <summary>
        /// 开始高度调整
        /// </summary>
        /// <param name="adjustUp">true=升高，false=降低</param>
        public void StartHeightAdjustment(bool adjustUp)
        {
            if (m_playerRig == null) return;

            m_isAdjustingHeight = true;
            m_isAdjustingUp = adjustUp;

            // 停止之前的协程
            if (m_heightAdjustmentCoroutine != null)
            {
                StopCoroutine(m_heightAdjustmentCoroutine);
            }

            // 开始新的高度调整协程
            m_heightAdjustmentCoroutine = StartCoroutine(AdjustHeightCoroutine());

            // 显示UI反馈
            ShowHeightUI(true);

            if (m_showDebugInfo)
            {
                Debug.Log($"开始{(adjustUp ? "升高" : "降低")}角色");
            }
        }

        /// <summary>
        /// 停止高度调整
        /// </summary>
        public void StopHeightAdjustment()
        {
            m_isAdjustingHeight = false;

            if (m_heightAdjustmentCoroutine != null)
            {
                StopCoroutine(m_heightAdjustmentCoroutine);
                m_heightAdjustmentCoroutine = null;
            }

            // 隐藏UI反馈
            ShowHeightUI(false);

            if (m_showDebugInfo)
            {
                Debug.Log($"停止高度调整，当前高度偏移: {m_currentHeightOffset * 100:F1}cm");
            }
        }

        /// <summary>
        /// 高度调整协程
        /// </summary>
        private IEnumerator AdjustHeightCoroutine()
        {
            while (m_isAdjustingHeight)
            {
                float deltaHeight = m_heightSpeed * Time.deltaTime;
                if (!m_isAdjustingUp)
                    deltaHeight = -deltaHeight;

                AdjustHeight(deltaHeight);

                yield return null; // 等待下一帧
            }
        }

        /// <summary>
        /// 调整高度
        /// </summary>
        /// <param name="deltaHeight">高度变化量（米）</param>
        private void AdjustHeight(float deltaHeight)
        {
            // 计算新的高度偏移
            float newHeightOffset = m_currentHeightOffset + deltaHeight;

            // 限制在允许范围内
            newHeightOffset = Mathf.Clamp(newHeightOffset, m_minHeightOffset, m_maxHeightOffset);

            // 如果高度没有变化，说明达到了极限
            if (Mathf.Approximately(newHeightOffset, m_currentHeightOffset))
            {
                if (m_showDebugInfo)
                {
                    string limitType = newHeightOffset >= m_maxHeightOffset ? "最高" : "最低";
                    Debug.Log($"已达到{limitType}高度限制: {newHeightOffset * 100:F1}cm");
                }
                return;
            }

            // 更新高度
            m_currentHeightOffset = newHeightOffset;
            Vector3 newPosition = m_originalPosition;
            newPosition.y += m_currentHeightOffset;

            m_playerRig.position = newPosition;

            // 更新UI显示
            UpdateHeightUI();
        }

        /// <summary>
        /// 重置高度到初始位置
        /// </summary>
        public void ResetHeight()
        {
            StopHeightAdjustment();

            m_currentHeightOffset = 0f;
            m_playerRig.position = m_originalPosition;

            UpdateHeightUI();

            Debug.Log("玩家高度已重置到初始位置");
        }

        /// <summary>
        /// 显示/隐藏高度UI
        /// </summary>
        private void ShowHeightUI(bool show)
        {
            if (m_heightIndicatorUI != null)
            {
                m_heightIndicatorUI.SetActive(show);
            }
        }

        /// <summary>
        /// 更新高度UI显示
        /// </summary>
        private void UpdateHeightUI()
        {
            if (m_heightText != null)
            {
                float heightInCm = m_currentHeightOffset * 100f;
                string sign = heightInCm >= 0 ? "+" : "";
                m_heightText.text = $"高度: {sign}{heightInCm:F1}cm";
            }
        }

        /// <summary>
        /// 设置新的基准位置
        /// </summary>
        public void SetNewBasePosition()
        {
            m_originalPosition = m_playerRig.position;
            m_currentHeightOffset = 0f;
            UpdateHeightUI();

            Debug.Log($"设置新的基准位置: {m_originalPosition}");
        }

        #region 公共属性和方法

        /// <summary>
        /// 当前高度偏移（米）
        /// </summary>
        public float CurrentHeightOffset => m_currentHeightOffset;

        /// <summary>
        /// 当前高度偏移（厘米）
        /// </summary>
        public float CurrentHeightOffsetCm => m_currentHeightOffset * 100f;

        /// <summary>
        /// 是否正在调整高度
        /// </summary>
        public bool IsAdjustingHeight => m_isAdjustingHeight;

        /// <summary>
        /// 设置高度调整速度
        /// </summary>
        public void SetHeightSpeed(float speedCmPerSecond)
        {
            m_heightSpeed = speedCmPerSecond / 100f; // 转换为米/秒
        }

        /// <summary>
        /// 直接设置高度偏移
        /// </summary>
        public void SetHeightOffset(float offsetInMeters)
        {
            float clampedOffset = Mathf.Clamp(offsetInMeters, m_minHeightOffset, m_maxHeightOffset);

            m_currentHeightOffset = clampedOffset;
            Vector3 newPosition = m_originalPosition;
            newPosition.y += m_currentHeightOffset;

            m_playerRig.position = newPosition;
            UpdateHeightUI();
        }

        #endregion

        #region Unity编辑器调试

        private void OnDrawGizmosSelected()
        {
            if (m_playerRig == null) return;

            // 绘制高度调整范围
            Vector3 basePos = Application.isPlaying ? m_originalPosition : m_playerRig.position;

            // 最低位置
            Gizmos.color = Color.red;
            Vector3 minPos = basePos;
            minPos.y += m_minHeightOffset;
            Gizmos.DrawWireCube(minPos, Vector3.one * 0.3f);

            // 最高位置
            Gizmos.color = Color.green;
            Vector3 maxPos = basePos;
            maxPos.y += m_maxHeightOffset;
            Gizmos.DrawWireCube(maxPos, Vector3.one * 0.3f);

            // 当前位置
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(m_playerRig.position, Vector3.one * 0.2f);
            }

            // 绘制调整范围线
            Gizmos.color = Color.white;
            Gizmos.DrawLine(minPos, maxPos);
        }

        #endregion
    }
}