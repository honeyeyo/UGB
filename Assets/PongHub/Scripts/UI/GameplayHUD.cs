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
        [SerializeField]
        [Tooltip("Game Status Text / 游戏状态文本 - Text component for displaying game status")]
        private TextMeshProUGUI m_gameStatusText;

        [SerializeField]
        [Tooltip("Paddle Status Text / 球拍状态文本 - Text component for displaying paddle status")]
        private TextMeshProUGUI m_paddleStatusText;

        [SerializeField]
        [Tooltip("Instruction Text / 指令文本 - Text component for displaying instructions")]
        private TextMeshProUGUI m_instructionText;

        [Header("临时消息")]
        [SerializeField]
        [Tooltip("Message Panel / 消息面板 - Panel for displaying temporary messages")]
        private GameObject m_messagePanel;

        [SerializeField]
        [Tooltip("Message Text / 消息文本 - Text component for temporary messages")]
        private TextMeshProUGUI m_messageText;

        [Header("游戏内按钮")]
        [SerializeField]
        [Tooltip("Menu Button / 菜单按钮 - Button for opening game menu")]
        private Button m_menuButton;

        [SerializeField]
        [Tooltip("Teleport Left Button / 瞬移左方按钮 - Button for teleporting to left side")]
        private Button m_teleportLeftButton;

        [SerializeField]
        [Tooltip("Teleport Right Button / 瞬移右方按钮 - Button for teleporting to right side")]
        private Button m_teleportRightButton;

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
            MenuCanvasController menuCanvasController = FindObjectOfType<MenuCanvasController>();
            if (menuCanvasController != null)
            {
                menuCanvasController.PauseGame();
            }
        }

        private void TeleportToSide(int sideIndex)
        {
            TableMenuSystem tableMenuSystem = FindObjectOfType<TableMenuSystem>();
            if (tableMenuSystem != null)
            {
                tableMenuSystem.TeleportToPoint(sideIndex);
                ShowMessage($"已瞬移到{(sideIndex == 0 ? "左" : "右")}侧", 1f);
            }
        }
    }
}