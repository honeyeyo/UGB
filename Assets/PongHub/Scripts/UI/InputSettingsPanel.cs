using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

namespace PongHub.UI
{
    public class InputSettingsPanel : MonoBehaviour
    {
        [Header("球拍配置UI")]
        [SerializeField] private Button m_configLeftPaddleButton;
        [SerializeField] private Button m_configRightPaddleButton;

        [Header("瞬移设置")]
        [SerializeField] private Button m_teleportLeftButton;
        [SerializeField] private Button m_teleportRightButton;

        [Header("导航按钮")]
        [SerializeField] private Button m_backButton;

        [Header("状态显示")]
        [SerializeField] private TextMeshProUGUI m_statusText;

        private PongInputManager m_inputManager;

        private void Awake()
        {
            InitializeButtons();
        }

        private void Start()
        {
            m_inputManager = FindObjectOfType<PongInputManager>();
        }

        private void InitializeButtons()
        {
            if (m_configLeftPaddleButton != null)
                m_configLeftPaddleButton.onClick.AddListener(() => ConfigurePaddle(true));
            if (m_configRightPaddleButton != null)
                m_configRightPaddleButton.onClick.AddListener(() => ConfigurePaddle(false));
            if (m_teleportLeftButton != null)
                m_teleportLeftButton.onClick.AddListener(() => TeleportToSide(0));
            if (m_teleportRightButton != null)
                m_teleportRightButton.onClick.AddListener(() => TeleportToSide(1));
            if (m_backButton != null)
                m_backButton.onClick.AddListener(OnBackClicked);
        }

        private void ConfigurePaddle(bool leftHand)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ConfigurePaddle(leftHand);
            }
        }

        private void TeleportToSide(int sideIndex)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.TeleportToPoint(sideIndex);
            }
        }

        private void OnBackClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowSettings();
            }
        }
    }
}