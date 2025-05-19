using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PongHub.Core;

namespace PongHub.Gameplay.Ball
{
    public class BallUI : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] private Canvas m_canvas;
        [SerializeField] private TextMeshProUGUI m_speedText;
        [SerializeField] private TextMeshProUGUI m_spinText;
        [SerializeField] private Image m_speedBar;
        [SerializeField] private Image m_spinBar;
        [SerializeField] private GameObject m_scorePopup;

        [Header("UI配置")]
        [SerializeField] private float m_uiUpdateInterval = 0.1f;
        [SerializeField] private float m_popupDuration = 2f;
        [SerializeField] private float m_popupFadeSpeed = 1f;
        [SerializeField] private Vector3 m_popupOffset = new Vector3(0f, 1f, 0f);

        private BallPhysics m_ballPhysics;
        private float m_lastUpdateTime;
        private float m_popupTimer;
        private bool m_isPopupVisible;

        private void Awake()
        {
            m_ballPhysics = GetComponent<BallPhysics>();
            SetupUI();
        }

        private void SetupUI()
        {
            // 设置Canvas
            if (m_canvas != null)
            {
                m_canvas.renderMode = RenderMode.WorldSpace;
                m_canvas.transform.localScale = Vector3.one * 0.01f; // 缩放以适应世界空间
            }

            // 初始化UI元素
            if (m_speedText != null)
                m_speedText.text = "0";
            if (m_spinText != null)
                m_spinText.text = "0";
            if (m_speedBar != null)
                m_speedBar.fillAmount = 0f;
            if (m_spinBar != null)
                m_spinBar.fillAmount = 0f;
            if (m_scorePopup != null)
                m_scorePopup.SetActive(false);
        }

        private void Update()
        {
            if (Time.time - m_lastUpdateTime >= m_uiUpdateInterval)
            {
                UpdateUI();
                m_lastUpdateTime = Time.time;
            }

            UpdatePopup();
        }

        private void UpdateUI()
        {
            if (m_ballPhysics == null)
                return;

            // 更新速度显示
            float speed = m_ballPhysics.Velocity.magnitude;
            if (m_speedText != null)
                m_speedText.text = speed.ToString("F1");
            if (m_speedBar != null)
                m_speedBar.fillAmount = speed / m_ballPhysics.BallData.MaxSpeed;

            // 更新旋转显示
            float spin = m_ballPhysics.AngularVelocity.magnitude;
            if (m_spinText != null)
                m_spinText.text = spin.ToString("F1");
            if (m_spinBar != null)
                m_spinBar.fillAmount = spin / m_ballPhysics.BallData.MaxSpin;
        }

        private void UpdatePopup()
        {
            if (!m_isPopupVisible)
                return;

            m_popupTimer -= Time.deltaTime;
            if (m_popupTimer <= 0f)
            {
                HidePopup();
            }
            else
            {
                // 更新弹出框位置
                if (m_scorePopup != null)
                {
                    m_scorePopup.transform.position = transform.position + m_popupOffset;
                    // 淡出效果
                    float alpha = m_popupTimer / m_popupDuration;
                    var canvasGroup = m_scorePopup.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = alpha;
                    }
                }
            }
        }

        // 显示得分弹出框
        public void ShowScorePopup(int score)
        {
            if (m_scorePopup != null)
            {
                m_scorePopup.SetActive(true);
                var text = m_scorePopup.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = score.ToString();
                }
                m_popupTimer = m_popupDuration;
                m_isPopupVisible = true;
            }
        }

        // 隐藏弹出框
        private void HidePopup()
        {
            if (m_scorePopup != null)
            {
                m_scorePopup.SetActive(false);
                m_isPopupVisible = false;
            }
        }

        // 设置UI可见性
        public void SetUIVisibility(bool visible)
        {
            if (m_canvas != null)
            {
                m_canvas.gameObject.SetActive(visible);
            }
        }

        // 设置UI位置
        public void SetUIPosition(Vector3 position)
        {
            if (m_canvas != null)
            {
                m_canvas.transform.position = position;
            }
        }

        // 设置UI旋转
        public void SetUIRotation(Quaternion rotation)
        {
            if (m_canvas != null)
            {
                m_canvas.transform.rotation = rotation;
            }
        }
    }
}