using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using PongHub.Input;

namespace PongHub.UI
{
    public class GameplayHUD : MonoBehaviour
    {
        [Header("状态显示")]
        [SerializeField] private TextMeshProUGUI m_gameStatusText;
        [SerializeField] private TextMeshProUGUI m_paddleStatusText;
        [SerializeField] private TextMeshProUGUI m_instructionText;

        [Header("临时消息")]
        [SerializeField] private GameObject m_messagePanel;
        [SerializeField] private TextMeshProUGUI m_messageText;

        [Header("游戏内按钮")]
        [SerializeField] private Button m_menuButton;
        [SerializeField] private Button m_teleportLeftButton;
        [SerializeField] private Button m_teleportRightButton;

        // 私有变量
        private PongHubInputManager m_inputManager;
        private Coroutine m_messageCoroutine;

        private void Start()
        {
            m_inputManager = PongHubInputManager.Instance;
            InitializeButtons();
            SetupInstructions();
        }

        private void Update()
        {
            UpdateGameStatus();
        }

        private void InitializeButtons()
        {
            if (m_menuButton != null)
                m_menuButton.onClick.AddListener(OnMenuClicked);
            if (m_teleportLeftButton != null)
                m_teleportLeftButton.onClick.AddListener(() => TeleportToSide(0));
            if (m_teleportRightButton != null)
                m_teleportRightButton.onClick.AddListener(() => TeleportToSide(1));
        }

        private void SetupInstructions()
        {
            if (m_instructionText != null)
            {
                m_instructionText.text = "操作提示:\n" +
                                       "长按Grip - 握持/释放球拍\n" +
                                       "Trigger - 生成球(非持拍手)\n" +
                                       "A+B - 配置球拍位置\n" +
                                       "长按Meta - 回到出生点";
            }
        }

        private void UpdateGameStatus()
        {
            if (m_inputManager == null) return;

            // 更新球拍状态
            if (m_paddleStatusText != null)
            {
                bool leftGripped = m_inputManager.IsLeftPaddleGripped;
                bool rightGripped = m_inputManager.IsRightPaddleGripped;

                if (leftGripped || rightGripped)
                {
                    string paddleInfo = "";
                    if (leftGripped && rightGripped)
                        paddleInfo = "双手";
                    else if (leftGripped)
                        paddleInfo = "左手";
                    else
                        paddleInfo = "右手";

                    m_paddleStatusText.text = $"球拍: {paddleInfo}";
                    m_paddleStatusText.color = Color.green;
                }
                else
                {
                    m_paddleStatusText.text = "球拍: 未握持";
                    m_paddleStatusText.color = Color.white;
                }
            }

            // 更新游戏状态
            if (m_gameStatusText != null)
            {
                // 这里可以显示分数、时间等信息
                m_gameStatusText.text = "游戏进行中";
            }
        }

        public void ShowMessage(string message, float duration = 3f)
        {
            if (m_messageCoroutine != null)
            {
                StopCoroutine(m_messageCoroutine);
            }

            m_messageCoroutine = StartCoroutine(ShowMessageCoroutine(message, duration));
        }

        private IEnumerator ShowMessageCoroutine(string message, float duration)
        {
            if (m_messagePanel != null && m_messageText != null)
            {
                m_messageText.text = message;
                m_messagePanel.SetActive(true);

                yield return new WaitForSeconds(duration);

                m_messagePanel.SetActive(false);
            }
        }

        private void OnMenuClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.PauseGame();
            }
        }

        private void TeleportToSide(int sideIndex)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.TeleportToPoint(sideIndex);
                ShowMessage($"已瞬移到{(sideIndex == 0 ? "左" : "右")}侧", 1f);
            }
        }
    }
}