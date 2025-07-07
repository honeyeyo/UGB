using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

namespace PongHub.UI
{
    /// <summary>
    /// 菜单距离警告控制器
    /// 当玩家距离球桌过远时显示警告和传送选项
    /// </summary>
    public class MenuDistanceWarningController : MonoBehaviour
    {
        [Header("距离设置")]
        [SerializeField] private float m_maxInteractionDistance = 5.0f;
        [SerializeField] private float m_warningStartDistance = 4.5f;
        [SerializeField] private Transform m_tableTransform;
        [SerializeField] private Transform m_playerTransform;

        [Header("UI元素")]
        [SerializeField] private GameObject m_warningPanel;
        [SerializeField] private TextMeshProUGUI m_warningText;
        [SerializeField] private Image m_distanceIcon;
        [SerializeField] private Image m_directionIndicator;
        [SerializeField] private Button m_quickTeleportButton;
        [SerializeField] private CanvasGroup m_menuCanvasGroup;

        [Header("视觉效果")]
        [SerializeField] private float m_fadeStartAlpha = 1.0f;
        [SerializeField] private float m_fadeEndAlpha = 0.3f;
        [SerializeField] private Color m_normalColor = Color.white;
        [SerializeField] private Color m_warningColor = Color.yellow;
        [SerializeField] private Color m_errorColor = Color.red;
        [SerializeField] private float m_pulseSpeed = 1.0f;
        [SerializeField] private float m_pulseAmount = 0.2f;

        [Header("音频设置")]
        [SerializeField] private AudioSource m_audioSource;
        [SerializeField] private AudioClip m_warningSound;
        [SerializeField] private AudioClip m_teleportSound;
        [SerializeField] private float m_warningCooldown = 5.0f;

        [Header("传送设置")]
        [SerializeField] private OVRCameraRig m_cameraRig;              // OVR相机装置引用
        [SerializeField] private Transform[] m_teleportPoints;          // 传送点数组

        // 私有变量
        private float m_currentDistance;
        private float m_lastWarningTime;
        private bool m_isWarningActive;
        private bool m_isOutOfRange;
        private Coroutine m_pulseCoroutine;
        private VRMenuInteraction m_menuInteraction;

        #region Unity生命周期

        private void Awake()
        {
            // 查找必要组件
            if (m_playerTransform == null)
            {
                var playerRig = FindObjectOfType<OVRCameraRig>();
                if (playerRig != null)
                {
                    m_playerTransform = playerRig.transform;
                }
            }

            m_menuInteraction = FindObjectOfType<VRMenuInteraction>();

            // 初始化状态
            m_isWarningActive = false;
            m_isOutOfRange = false;
            m_lastWarningTime = -m_warningCooldown; // 允许立即显示第一次警告
        }

        private void Start()
        {
            // 初始化UI
            if (m_warningPanel != null)
            {
                m_warningPanel.SetActive(false);
            }

            // 注册按钮事件
            if (m_quickTeleportButton != null)
            {
                m_quickTeleportButton.onClick.AddListener(TeleportToNearestPoint);
            }
        }

        private void Update()
        {
            // 检查距离
            CheckDistance();

            // 更新UI
            UpdateUI();
        }

        private void OnDestroy()
        {
            // 取消注册按钮事件
            if (m_quickTeleportButton != null)
            {
                m_quickTeleportButton.onClick.RemoveListener(TeleportToNearestPoint);
            }

            // 停止所有协程
            if (m_pulseCoroutine != null)
            {
                StopCoroutine(m_pulseCoroutine);
                m_pulseCoroutine = null;
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置最大交互距离
        /// </summary>
        public void SetMaxInteractionDistance(float distance)
        {
            if (distance > 0)
            {
                m_maxInteractionDistance = distance;
                m_warningStartDistance = distance * 0.9f; // 默认在最大距离的90%处开始警告
            }
        }

        /// <summary>
        /// 强制显示警告
        /// </summary>
        public void ForceShowWarning(bool show)
        {
            if (show)
            {
                ShowWarning();
            }
            else
            {
                HideWarning();
            }
        }

        /// <summary>
        /// 传送到最近的传送点
        /// </summary>
        public void TeleportToNearestPoint()
        {
            if (m_cameraRig == null || m_teleportPoints == null || m_teleportPoints.Length == 0 || m_playerTransform == null)
                return;

            // 查找最近的传送点
            Transform nearestPoint = null;
            float nearestDistance = float.MaxValue;

            foreach (var point in m_teleportPoints)
            {
                if (point == null)
                    continue;

                float distance = Vector3.Distance(point.position, m_tableTransform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPoint = point;
                }
            }

            // 执行传送
            if (nearestPoint != null)
            {
                TeleportToPosition(nearestPoint.position);
            }
        }

        /// <summary>
        /// 传送到指定位置
        /// </summary>
        public void TeleportToPosition(Vector3 position)
        {
            if (m_cameraRig == null || m_playerTransform == null)
                return;

            // 计算传送偏移，保持相机高度
            Vector3 currentPosition = m_cameraRig.transform.position;
            Vector3 targetPosition = new Vector3(position.x, currentPosition.y, position.z);

            // 执行传送
            m_cameraRig.transform.position = targetPosition; // 直接移动相机

            // 播放音效
            if (m_audioSource != null && m_teleportSound != null)
            {
                m_audioSource.PlayOneShot(m_teleportSound);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 检查距离
        /// </summary>
        private void CheckDistance()
        {
            if (m_playerTransform == null || m_tableTransform == null)
                return;

            // 计算距离
            m_currentDistance = Vector3.Distance(m_playerTransform.position, m_tableTransform.position);

            // 检查是否超出范围
            bool wasOutOfRange = m_isOutOfRange;
            m_isOutOfRange = m_currentDistance > m_maxInteractionDistance;

            // 检查是否需要显示警告
            bool shouldShowWarning = m_currentDistance > m_warningStartDistance;

            // 状态变化时处理
            if (shouldShowWarning != m_isWarningActive)
            {
                if (shouldShowWarning)
                {
                    ShowWarning();
                }
                else
                {
                    HideWarning();
                }
            }

            // 状态变化时播放音效
            if (wasOutOfRange != m_isOutOfRange && m_isOutOfRange)
            {
                PlayWarningSound();
            }

            // 更新菜单交互组件的交互距离
            if (m_menuInteraction != null)
            {
                m_menuInteraction.SetInteractionDistance(m_maxInteractionDistance);
            }

            // 更新菜单透明度
            UpdateMenuAlpha();
        }

        /// <summary>
        /// 显示警告
        /// </summary>
        private void ShowWarning()
        {
            if (m_warningPanel != null)
            {
                m_warningPanel.SetActive(true);
            }

            m_isWarningActive = true;

            // 开始脉冲效果
            if (m_pulseCoroutine != null)
            {
                StopCoroutine(m_pulseCoroutine);
            }
            m_pulseCoroutine = StartCoroutine(PulseEffect());

            // 更新警告文本
            UpdateWarningText();
        }

        /// <summary>
        /// 隐藏警告
        /// </summary>
        private void HideWarning()
        {
            if (m_warningPanel != null)
            {
                m_warningPanel.SetActive(false);
            }

            m_isWarningActive = false;

            // 停止脉冲效果
            if (m_pulseCoroutine != null)
            {
                StopCoroutine(m_pulseCoroutine);
                m_pulseCoroutine = null;
            }

            // 恢复菜单透明度
            if (m_menuCanvasGroup != null)
            {
                m_menuCanvasGroup.alpha = 1.0f;
            }
        }

        /// <summary>
        /// 更新警告文本
        /// </summary>
        private void UpdateWarningText()
        {
            if (m_warningText == null)
                return;

            if (m_isOutOfRange)
            {
                m_warningText.text = "距离过远！\n请靠近球桌或使用传送功能";
                m_warningText.color = m_errorColor;
            }
            else
            {
                m_warningText.text = "即将超出交互范围\n请靠近球桌";
                m_warningText.color = m_warningColor;
            }
        }

        /// <summary>
        /// 更新UI
        /// </summary>
        private void UpdateUI()
        {
            if (!m_isWarningActive)
                return;

            // 更新距离图标
            if (m_distanceIcon != null)
            {
                // 根据距离设置颜色
                if (m_isOutOfRange)
                {
                    m_distanceIcon.color = m_errorColor;
                }
                else
                {
                    float t = Mathf.InverseLerp(m_warningStartDistance, m_maxInteractionDistance, m_currentDistance);
                    m_distanceIcon.color = Color.Lerp(m_normalColor, m_warningColor, t);
                }
            }

            // 更新方向指示器
            if (m_directionIndicator != null && m_tableTransform != null && m_playerTransform != null)
            {
                // 计算方向
                Vector3 direction = m_tableTransform.position - m_playerTransform.position;
                direction.y = 0; // 忽略垂直差异

                // 计算角度
                float angle = Vector3.SignedAngle(m_playerTransform.forward, direction, Vector3.up);

                // 设置旋转
                m_directionIndicator.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);

                // 根据距离设置透明度
                float alpha = Mathf.Lerp(0.5f, 1.0f, Mathf.InverseLerp(m_warningStartDistance, m_maxInteractionDistance * 1.5f, m_currentDistance));
                Color color = m_directionIndicator.color;
                color.a = alpha;
                m_directionIndicator.color = color;
            }
        }

        /// <summary>
        /// 更新菜单透明度
        /// </summary>
        private void UpdateMenuAlpha()
        {
            if (m_menuCanvasGroup == null)
                return;

            if (m_isOutOfRange)
            {
                // 超出范围时，菜单半透明
                m_menuCanvasGroup.alpha = m_fadeEndAlpha;
            }
            else if (m_currentDistance > m_warningStartDistance)
            {
                // 在警告区域内，菜单逐渐变透明
                float t = Mathf.InverseLerp(m_warningStartDistance, m_maxInteractionDistance, m_currentDistance);
                m_menuCanvasGroup.alpha = Mathf.Lerp(m_fadeStartAlpha, m_fadeEndAlpha, t);
            }
            else
            {
                // 在安全区域内，菜单完全不透明
                m_menuCanvasGroup.alpha = m_fadeStartAlpha;
            }
        }

        /// <summary>
        /// 播放警告音效
        /// </summary>
        private void PlayWarningSound()
        {
            if (m_audioSource == null || m_warningSound == null)
                return;

            // 检查冷却时间
            if (Time.time - m_lastWarningTime < m_warningCooldown)
                return;

            m_audioSource.PlayOneShot(m_warningSound);
            m_lastWarningTime = Time.time;
        }

        /// <summary>
        /// 脉冲效果协程
        /// </summary>
        private IEnumerator PulseEffect()
        {
            while (m_isWarningActive)
            {
                // 计算脉冲值
                float pulse = Mathf.Sin(Time.time * m_pulseSpeed) * m_pulseAmount + 1.0f;

                // 应用到UI元素
                if (m_distanceIcon != null)
                {
                    m_distanceIcon.transform.localScale = Vector3.one * pulse;
                }

                if (m_directionIndicator != null)
                {
                    m_directionIndicator.transform.localScale = Vector3.one * pulse;
                }

                yield return null;
            }
        }

        #endregion
    }
}