using UnityEngine;
using UnityEngine.UI;
using PongHub.Core;

namespace PongHub.UI.Panels
{
    /// <summary>
    /// Main menu panel for table-based VR interface
    /// 主菜单面板，用于桌面VR界面
    /// </summary>
    public class MainMenuPanel : MonoBehaviour
    {
        [Header("UI Components - UI组件")]
        [SerializeField]
        [Tooltip("Title Text / 标题文本 - Main menu title display component")]
        private Text titleText;

        [SerializeField]
        [Tooltip("Single Player Button / 单机模式按钮 - Opens single player/practice mode")]
        private Button singlePlayerButton;

        [SerializeField]
        [Tooltip("Multi-Player Button / 多人模式按钮 - Opens multiplayer network mode")]
        private Button multiPlayerButton;

        [SerializeField]
        [Tooltip("Settings Button / 设置按钮 - Opens game settings panel")]
        private Button settingsButton;

        [SerializeField]
        [Tooltip("Exit Button / 退出按钮 - Closes the game application")]
        private Button exitButton;

        [Header("Text Components - 文本组件")]
        [SerializeField]
        [Tooltip("Local Mode Label / 单机模式标签 - Text label for single player button")]
        private Text localModeText;

        [SerializeField]
        [Tooltip("Network Mode Label / 网络模式标签 - Text label for multiplayer button")]
        private Text networkModeText;

        [SerializeField]
        [Tooltip("Settings Label / 设置标签 - Text label for settings button")]
        private Text settingsText;

        [SerializeField]
        [Tooltip("Exit Label / 退出标签 - Text label for exit button")]
        private Text exitText;

        // References
        private TableMenuSystem tableMenuSystem;
        private GameModeManager gameModeManager;

        // Events
        public System.Action OnLocalModeSelected;
        public System.Action OnNetworkModeSelected;
        public System.Action OnSettingsSelected;
        public System.Action OnExitSelected;

        private void Awake()
        {
            InitializeComponents();
            SetupButtons();
        }

        private void Start()
        {
            FindReferences();
            SetupTexts();
            ApplyVRUISettings();
        }

        private void InitializeComponents()
        {
            // Try to find buttons automatically if not specified
            if (singlePlayerButton == null)
                singlePlayerButton = transform.Find("SinglePlayerButton")?.GetComponent<Button>();

            if (multiPlayerButton == null)
                multiPlayerButton = transform.Find("MultiPlayerButton")?.GetComponent<Button>();

            if (settingsButton == null)
                settingsButton = transform.Find("SettingsButton")?.GetComponent<Button>();

            if (exitButton == null)
                exitButton = transform.Find("ExitButton")?.GetComponent<Button>();
        }

        private void SetupButtons()
        {
            // Setup button click events
            if (singlePlayerButton != null)
            {
                singlePlayerButton.onClick.AddListener(OnLocalModeButtonClicked);
            }

            if (multiPlayerButton != null)
            {
                multiPlayerButton.onClick.AddListener(OnNetworkModeButtonClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            }

            if (exitButton != null)
            {
                exitButton.onClick.AddListener(OnExitButtonClicked);
            }
        }

        private void FindReferences()
        {
            // Find system references
            if (tableMenuSystem == null)
                tableMenuSystem = FindObjectOfType<TableMenuSystem>();

            if (gameModeManager == null)
                gameModeManager = FindObjectOfType<GameModeManager>();
        }

        private void SetupTexts()
        {
            // Set VR-optimized text with emojis
            if (titleText != null)
                titleText.text = "🏓 PongHub VR";

            if (localModeText != null)
                localModeText.text = "🎮 Single Player";

            if (networkModeText != null)
                networkModeText.text = "🌐 Multi Player";

            if (settingsText != null)
                settingsText.text = "⚙️ Settings";

            if (exitText != null)
                exitText.text = "🚪 Exit";
        }

        private void ApplyVRUISettings()
        {
            // Apply VR-optimized font settings
            ApplyVRFontSettings(titleText, 36, FontStyle.Bold);
            ApplyVRFontSettings(localModeText, 24, FontStyle.Bold);
            ApplyVRFontSettings(networkModeText, 24, FontStyle.Bold);
            ApplyVRFontSettings(settingsText, 24, FontStyle.Bold);
            ApplyVRFontSettings(exitText, 24, FontStyle.Bold);

            // Apply VR-friendly button sizing
            ApplyVRButtonSettings(singlePlayerButton);
            ApplyVRButtonSettings(multiPlayerButton);
            ApplyVRButtonSettings(settingsButton);
            ApplyVRButtonSettings(exitButton);
        }

        private void ApplyVRFontSettings(Text textComponent, int fontSize, FontStyle fontStyle)
        {
            if (textComponent != null)
            {
                textComponent.fontSize = fontSize;
                textComponent.fontStyle = fontStyle;
                textComponent.lineSpacing = 1.5f;

                // Apply adaptive contrast (simplified version)
                // In a full implementation, this would check table surface color
                textComponent.color = Color.white;
            }
        }

        private void ApplyVRButtonSettings(Button button)
        {
            if (button != null)
            {
                // Set minimum VR-friendly button size
                var rectTransform = button.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    var currentSize = rectTransform.sizeDelta;
                    rectTransform.sizeDelta = new Vector2(
                        Mathf.Max(currentSize.x, 120f),
                        Mathf.Max(currentSize.y, 80f)
                    );
                }

                // Setup VR-friendly visual feedback
                var colors = button.colors;
                colors.normalColor = new Color(1f, 1f, 1f, 0.9f);
                colors.highlightedColor = new Color(0f, 1f, 1f, 1f); // Cyan highlight
                colors.pressedColor = new Color(0f, 0.5f, 1f, 1f); // Blue pressed
                colors.selectedColor = new Color(1f, 1f, 0f, 1f); // Yellow selected
                button.colors = colors;
            }
        }

        private void OnLocalModeButtonClicked()
        {
            Debug.Log("MainMenuPanel: Local mode selected - 选择单机模式");

            // Switch to Local game mode
            if (gameModeManager != null)
            {
                gameModeManager.SwitchToMode(GameMode.Local);
            }

            // Hide menu after selection
            if (tableMenuSystem != null)
            {
                tableMenuSystem.HideMenu();
            }

            OnLocalModeSelected?.Invoke();
        }

        private void OnNetworkModeButtonClicked()
        {
            Debug.Log("MainMenuPanel: Network mode selected - 选择多人模式");

            // Switch to Network game mode
            if (gameModeManager != null)
            {
                gameModeManager.SwitchToMode(GameMode.Network);
            }

            // Hide menu after selection
            if (tableMenuSystem != null)
            {
                tableMenuSystem.HideMenu();
            }

            OnNetworkModeSelected?.Invoke();
        }

        private void OnSettingsButtonClicked()
        {
            Debug.Log("MainMenuPanel: Settings selected - 选择设置");

            // Switch to settings panel
            if (tableMenuSystem != null)
            {
                tableMenuSystem.ShowPanel(MenuPanel.Settings);
            }

            OnSettingsSelected?.Invoke();
        }

        private void OnExitButtonClicked()
        {
            Debug.Log("MainMenuPanel: Exit selected - 退出游戏");

            // Switch to exit confirmation panel
            if (tableMenuSystem != null)
            {
                tableMenuSystem.ShowPanel(MenuPanel.Exit);
            }

            OnExitSelected?.Invoke();
        }

        public void SetButtonInteractable(bool localMode, bool networkMode, bool settings, bool exit)
        {
            if (singlePlayerButton != null)
                singlePlayerButton.interactable = localMode;

            if (multiPlayerButton != null)
                multiPlayerButton.interactable = networkMode;

            if (settingsButton != null)
                settingsButton.interactable = settings;

            if (exitButton != null)
                exitButton.interactable = exit;
        }

        public void SetButtonColors(Color normalColor, Color highlightColor, Color pressedColor, Color disabledColor)
        {
            var colorBlock = new ColorBlock
            {
                normalColor = normalColor,
                highlightedColor = highlightColor,
                pressedColor = pressedColor,
                disabledColor = disabledColor,
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };

            if (singlePlayerButton != null)
                singlePlayerButton.colors = colorBlock;

            if (multiPlayerButton != null)
                multiPlayerButton.colors = colorBlock;

            if (settingsButton != null)
                settingsButton.colors = colorBlock;

            if (exitButton != null)
                exitButton.colors = colorBlock;
        }

        public void UpdateGameModeStatus(GameMode currentMode)
        {
            // 根据当前游戏模式更新按钮状态
            bool isLocalMode = currentMode == GameMode.Local;
            bool isNetworkMode = currentMode == GameMode.Network;

            // 可以在这里添加视觉反馈，比如高亮当前模式的按钮
            if (singlePlayerButton != null)
            {
                var image = singlePlayerButton.GetComponent<Image>();
                if (image != null)
                {
                    image.color = isLocalMode ? Color.green : Color.white;
                }
            }

            if (multiPlayerButton != null)
            {
                var image = multiPlayerButton.GetComponent<Image>();
                if (image != null)
                {
                    image.color = isNetworkMode ? Color.green : Color.white;
                }
            }
        }

        private void OnDestroy()
        {
            // Clean up button events
            if (singlePlayerButton != null)
                singlePlayerButton.onClick.RemoveListener(OnLocalModeButtonClicked);

            if (multiPlayerButton != null)
                multiPlayerButton.onClick.RemoveListener(OnNetworkModeButtonClicked);

            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);

            if (exitButton != null)
                exitButton.onClick.RemoveListener(OnExitButtonClicked);
        }
    }
}