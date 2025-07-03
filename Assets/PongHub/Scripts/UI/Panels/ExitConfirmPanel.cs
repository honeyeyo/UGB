using UnityEngine;
using UnityEngine.UI;

namespace PongHub.UI.Panels
{
    /// <summary>
    /// Exit confirmation panel
    /// Provides exit game confirmation interface
    /// </summary>
    public class ExitConfirmPanel : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField]
        [Tooltip("Confirm Button / 确认按钮 - Confirms exit action and closes application")]
        private Button confirmButton;

        [SerializeField]
        [Tooltip("Cancel Button / 取消按钮 - Cancels exit action and returns to main menu")]
        private Button cancelButton;

        [Header("Text Components")]
        [SerializeField]
        [Tooltip("Title Text / 标题文本 - Exit confirmation dialog title display")]
        private Text titleText;

        [SerializeField]
        [Tooltip("Message Text / 消息文本 - Exit confirmation message display")]
        private Text messageText;

        [SerializeField]
        [Tooltip("Confirm Label / 确认标签 - Text label for confirm button")]
        private Text confirmText;

        [SerializeField]
        [Tooltip("Cancel Label / 取消标签 - Text label for cancel button")]
        private Text cancelText;

        // References
        private TableMenuSystem tableMenuSystem;

        // Events
        public System.Action OnExitConfirmed;
        public System.Action OnExitCancelled;

        private void Awake()
        {
            InitializeComponents();
            SetupButtons();
        }

        private void Start()
        {
            FindReferences();
            SetupTexts();
        }

        private void InitializeComponents()
        {
            // Try to find buttons automatically if not specified
            if (confirmButton == null)
                confirmButton = transform.Find("ConfirmButton")?.GetComponent<Button>();

            if (cancelButton == null)
                cancelButton = transform.Find("CancelButton")?.GetComponent<Button>();
        }

        private void SetupButtons()
        {
            // Setup button click events
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }
        }

        private void FindReferences()
        {
            // Find system references
            if (tableMenuSystem == null)
                tableMenuSystem = FindObjectOfType<TableMenuSystem>();
        }

        private void SetupTexts()
        {
            // Set VR-optimized text with emojis
            if (titleText != null)
                titleText.text = "🚪 Exit Game";

            if (messageText != null)
                messageText.text = "❓ Are you sure you want to exit PongHub VR?";

            if (confirmText != null)
                confirmText.text = "✅ Confirm";

            if (cancelText != null)
                cancelText.text = "❌ Cancel";

            // Apply VR font settings
            ApplyVRUISettings();
        }

        private void ApplyVRUISettings()
        {
            // Apply VR-optimized font settings
            ApplyVRFontSettings(titleText, 32, FontStyle.Bold);
            ApplyVRFontSettings(messageText, 24, FontStyle.Normal);
            ApplyVRFontSettings(confirmText, 24, FontStyle.Bold);
            ApplyVRFontSettings(cancelText, 24, FontStyle.Bold);

            // Apply VR-friendly button sizing
            ApplyVRButtonSettings(confirmButton);
            ApplyVRButtonSettings(cancelButton);
        }

        private void ApplyVRFontSettings(Text textComponent, int fontSize, FontStyle fontStyle)
        {
            if (textComponent != null)
            {
                textComponent.fontSize = fontSize;
                textComponent.fontStyle = fontStyle;
                textComponent.lineSpacing = 1.5f;
                textComponent.color = Color.white; // High contrast for VR
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

                // Setup VR-friendly visual feedback with distinct colors
                var colors = button.colors;

                // Different colors for confirm vs cancel
                if (button == confirmButton)
                {
                    colors.normalColor = new Color(1f, 0.3f, 0.3f, 0.9f); // Red tint for danger
                    colors.highlightedColor = new Color(1f, 0.5f, 0.5f, 1f); // Lighter red
                    colors.pressedColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Darker red
                }
                else if (button == cancelButton)
                {
                    colors.normalColor = new Color(0.3f, 1f, 0.3f, 0.9f); // Green tint for safe
                    colors.highlightedColor = new Color(0.5f, 1f, 0.5f, 1f); // Lighter green
                    colors.pressedColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Darker green
                }

                colors.selectedColor = new Color(1f, 1f, 0f, 1f); // Yellow selected
                button.colors = colors;
            }
        }

        private void OnConfirmButtonClicked()
        {
            Debug.Log("ExitConfirmPanel: Confirm exit game");

            OnExitConfirmed?.Invoke();

            // Exit application
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        private void OnCancelButtonClicked()
        {
            Debug.Log("ExitConfirmPanel: Cancel exit");

            // Return to main menu
            if (tableMenuSystem != null)
            {
                tableMenuSystem.ShowPanel(MenuPanel.Main);
            }

            OnExitCancelled?.Invoke();
        }

        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        public void SetButtonTexts(string confirmText, string cancelText)
        {
            if (this.confirmText != null)
                this.confirmText.text = confirmText;

            if (this.cancelText != null)
                this.cancelText.text = cancelText;
        }

        private void OnDestroy()
        {
            // Clean up button events
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);

            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(OnCancelButtonClicked);
        }
    }
}