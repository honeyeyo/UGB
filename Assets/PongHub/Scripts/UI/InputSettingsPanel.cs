using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using PongHub.Input;

namespace PongHub.UI
{
    public class InputSettingsPanel : MonoBehaviour
    {
        [Header("球拍配置UI")]
        [SerializeField]
        [Tooltip("Config Left Paddle Button / 配置左手球拍按钮 - Button for configuring left hand paddle")]
        private Button m_configLeftPaddleButton;

        [SerializeField]
        [Tooltip("Config Right Paddle Button / 配置右手球拍按钮 - Button for configuring right hand paddle")]
        private Button m_configRightPaddleButton;

        [Header("瞬移设置")]
        [SerializeField]
        [Tooltip("Teleport Left Button / 瞬移左方按钮 - Button for teleporting to left side")]
        private Button m_teleportLeftButton;

        [SerializeField]
        [Tooltip("Teleport Right Button / 瞬移右方按钮 - Button for teleporting to right side")]
        private Button m_teleportRightButton;

        [Header("导航按钮")]
        [SerializeField]
        [Tooltip("Back Button / 返回按钮 - Button for returning to previous menu")]
        private Button m_backButton;

        [Header("状态显示")]
        [SerializeField]
        [Tooltip("Status Text / 状态文本 - Text component for displaying status information")]
        private TextMeshProUGUI m_statusText;

        private PongHubInputManager m_inputManager;

        private void Awake()
        {
            InitializeButtons();
        }

        private void Start()
        {
            m_inputManager = PongHubInputManager.Instance;
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