using UnityEngine;
using UnityEngine.UI;

namespace PongHub.UI.Panels
{
    /// <summary>
    /// Settings panel
    /// Provides game settings options
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField]
        [Tooltip("Audio Settings Button / 音频设置按钮 - Opens the audio settings panel")]
        private Button audioButton;

        [SerializeField]
        [Tooltip("Controls Settings Button / 控制设置按钮 - Opens the control settings panel")]
        private Button controlsButton;

        [SerializeField]
        [Tooltip("Graphics Settings Button / 图形设置按钮 - Opens the graphics settings panel")]
        private Button graphicsButton;

        [SerializeField]
        [Tooltip("Back Button / 返回按钮 - Returns to main menu")]
        private Button backButton;

        [Header("Text Components")]
        [SerializeField]
        [Tooltip("Title Text / 标题文本 - Settings panel title display")]
        private Text titleText;

        [SerializeField]
        [Tooltip("Audio Label / 音频标签 - Text label for audio settings button")]
        private Text audioText;

        [SerializeField]
        [Tooltip("Controls Label / 控制标签 - Text label for controls settings button")]
        private Text controlsText;

        [SerializeField]
        [Tooltip("Graphics Label / 图形标签 - Text label for graphics settings button")]
        private Text graphicsText;

        [SerializeField]
        [Tooltip("Back Label / 返回标签 - Text label for back button")]
        private Text backText;

        [Header("Settings Items")]
        [SerializeField]
        [Tooltip("Master Volume Slider / 主音量滑块 - Controls overall game volume (0.0 = mute, 1.0 = full volume)")]
        private Slider masterVolumeSlider;

        [SerializeField]
        [Tooltip("SFX Volume Slider / 音效音量滑块 - Controls sound effects volume (0.0 = mute, 1.0 = full volume)")]
        private Slider sfxVolumeSlider;

        [SerializeField]
        [Tooltip("Music Volume Slider / 音乐音量滑块 - Controls background music volume (0.0 = mute, 1.0 = full volume)")]
        private Slider musicVolumeSlider;

        [SerializeField]
        [Tooltip("Vibration Toggle / 震动开关 - Enable/disable haptic feedback for VR controllers")]
        private Toggle vibrationToggle;

        [SerializeField]
        [Tooltip("Auto-Aim Toggle / 自动瞄准开关 - Enable/disable auto-aim assistance feature")]
        private Toggle autoAimToggle;

        [Header("Settings Display")]
        [SerializeField]
        [Tooltip("Master Volume Value / 主音量数值 - Text displaying current master volume percentage")]
        private Text masterVolumeValueText;

        [SerializeField]
        [Tooltip("SFX Volume Value / 音效音量数值 - Text displaying current SFX volume percentage")]
        private Text sfxVolumeValueText;

        [SerializeField]
        [Tooltip("Music Volume Value / 音乐音量数值 - Text displaying current music volume percentage")]
        private Text musicVolumeValueText;

        [SerializeField]
        [Tooltip("Vibration Status / 震动状态 - Text displaying vibration toggle status (ON/OFF)")]
        private Text vibrationStatusText;

        [SerializeField]
        [Tooltip("Auto-Aim Status / 自动瞄准状态 - Text displaying auto-aim toggle status (ON/OFF)")]
        private Text autoAimStatusText;

        // References
        private TableMenuSystem tableMenuSystem;

        // Events
        public System.Action OnAudioSettingsSelected;
        public System.Action OnControlsSettingsSelected;
        public System.Action OnGraphicsSettingsSelected;
        public System.Action OnBackSelected;

        private void Awake()
        {
            InitializeComponents();
            SetupButtons();
            SetupSliders();
            SetupToggles();
        }

        private void Start()
        {
            FindReferences();
            SetupTexts();
            LoadSettings();
        }

        private void InitializeComponents()
        {
            // Try to find buttons automatically if not specified
            if (audioButton == null)
                audioButton = transform.Find("AudioButton")?.GetComponent<Button>();

            if (controlsButton == null)
                controlsButton = transform.Find("ControlsButton")?.GetComponent<Button>();

            if (graphicsButton == null)
                graphicsButton = transform.Find("GraphicsButton")?.GetComponent<Button>();

            if (backButton == null)
                backButton = transform.Find("BackButton")?.GetComponent<Button>();
        }

        private void SetupButtons()
        {
            // Setup button click events
            if (audioButton != null)
            {
                audioButton.onClick.AddListener(OnAudioButtonClicked);
            }

            if (controlsButton != null)
            {
                controlsButton.onClick.AddListener(OnControlsButtonClicked);
            }

            if (graphicsButton != null)
            {
                graphicsButton.onClick.AddListener(OnGraphicsButtonClicked);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackButtonClicked);
            }
        }

        private void SetupSliders()
        {
            // Setup slider events
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
                masterVolumeSlider.minValue = 0f;
                masterVolumeSlider.maxValue = 1f;
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
                sfxVolumeSlider.minValue = 0f;
                sfxVolumeSlider.maxValue = 1f;
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
                musicVolumeSlider.minValue = 0f;
                musicVolumeSlider.maxValue = 1f;
            }
        }

        private void SetupToggles()
        {
            // Setup toggle events
            if (vibrationToggle != null)
            {
                vibrationToggle.onValueChanged.AddListener(OnVibrationToggleChanged);
            }

            if (autoAimToggle != null)
            {
                autoAimToggle.onValueChanged.AddListener(OnAutoAimToggleChanged);
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
                titleText.text = "⚙️ Settings";

            if (audioText != null)
                audioText.text = "🔊 Audio Settings";

            if (controlsText != null)
                controlsText.text = "🎮 Control Settings";

            if (graphicsText != null)
                graphicsText.text = "🎨 Graphics Settings";

            if (backText != null)
                backText.text = "🔙 Back";

            // Apply VR font settings
            ApplyVRUISettings();
        }

        private void ApplyVRUISettings()
        {
            // Apply VR-optimized font settings
            ApplyVRFontSettings(titleText, 32, FontStyle.Bold);
            ApplyVRFontSettings(audioText, 24, FontStyle.Bold);
            ApplyVRFontSettings(controlsText, 24, FontStyle.Bold);
            ApplyVRFontSettings(graphicsText, 24, FontStyle.Bold);
            ApplyVRFontSettings(backText, 24, FontStyle.Bold);

            // Apply VR-friendly button sizing
            ApplyVRButtonSettings(audioButton);
            ApplyVRButtonSettings(controlsButton);
            ApplyVRButtonSettings(graphicsButton);
            ApplyVRButtonSettings(backButton);
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

                // Setup VR-friendly visual feedback
                var colors = button.colors;
                colors.normalColor = new Color(1f, 1f, 1f, 0.9f);
                colors.highlightedColor = new Color(0f, 1f, 1f, 1f); // Cyan highlight
                colors.pressedColor = new Color(0f, 0.5f, 1f, 1f); // Blue pressed
                colors.selectedColor = new Color(1f, 1f, 0f, 1f); // Yellow selected
                button.colors = colors;
            }
        }

        private void LoadSettings()
        {
            // Load settings from PlayerPrefs
            float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.6f);
            bool vibrationEnabled = PlayerPrefs.GetInt("VibrationEnabled", 1) == 1;
            bool autoAimEnabled = PlayerPrefs.GetInt("AutoAimEnabled", 0) == 1;

            // Apply settings to UI
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = masterVolume;

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = sfxVolume;

            if (musicVolumeSlider != null)
                musicVolumeSlider.value = musicVolume;

            if (vibrationToggle != null)
                vibrationToggle.isOn = vibrationEnabled;

            if (autoAimToggle != null)
                autoAimToggle.isOn = autoAimEnabled;

            // Update display text
            UpdateVolumeDisplays();
            UpdateToggleDisplays();
        }

        private void SaveSettings()
        {
            // Save settings to PlayerPrefs
            if (masterVolumeSlider != null)
                PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);

            if (sfxVolumeSlider != null)
                PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);

            if (musicVolumeSlider != null)
                PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);

            if (vibrationToggle != null)
                PlayerPrefs.SetInt("VibrationEnabled", vibrationToggle.isOn ? 1 : 0);

            if (autoAimToggle != null)
                PlayerPrefs.SetInt("AutoAimEnabled", autoAimToggle.isOn ? 1 : 0);

            PlayerPrefs.Save();
        }

        private void OnAudioButtonClicked()
        {
            Debug.Log("SettingsPanel: Open audio settings");

            // Switch to audio settings panel
            if (tableMenuSystem != null)
            {
                tableMenuSystem.ShowPanel(MenuPanel.Audio);
            }

            OnAudioSettingsSelected?.Invoke();
        }

        private void OnControlsButtonClicked()
        {
            Debug.Log("SettingsPanel: Open control settings");

            // Switch to control settings panel
            if (tableMenuSystem != null)
            {
                tableMenuSystem.ShowPanel(MenuPanel.Controls);
            }

            OnControlsSettingsSelected?.Invoke();
        }

        private void OnGraphicsButtonClicked()
        {
            Debug.Log("SettingsPanel: Open graphics settings");

            // Graphics settings panel logic can be added here
            // Show a message for now
            Debug.Log("Graphics settings feature is under development...");

            OnGraphicsSettingsSelected?.Invoke();
        }

        private void OnBackButtonClicked()
        {
            Debug.Log("SettingsPanel: Return to main menu");

            // Save settings
            SaveSettings();

            // Return to main menu
            if (tableMenuSystem != null)
            {
                tableMenuSystem.ShowPanel(MenuPanel.Main);
            }

            OnBackSelected?.Invoke();
        }

        private void OnMasterVolumeChanged(float value)
        {
            // Apply master volume setting
            AudioListener.volume = value;

            // Update display
            UpdateVolumeDisplays();

            // Auto save
            SaveSettings();
        }

        private void OnSFXVolumeChanged(float value)
        {
            // SFX volume can be set here
            // Needs to work with audio manager

            // Update display
            UpdateVolumeDisplays();

            // Auto save
            SaveSettings();
        }

        private void OnMusicVolumeChanged(float value)
        {
            // Music volume can be set here
            // Needs to work with audio manager

            // Update display
            UpdateVolumeDisplays();

            // Auto save
            SaveSettings();
        }

        private void OnVibrationToggleChanged(bool enabled)
        {
            // Set vibration toggle
            Debug.Log($"Vibration setting: {(enabled ? "Enabled" : "Disabled")}");

            // Update display
            UpdateToggleDisplays();

            // Auto save
            SaveSettings();
        }

        private void OnAutoAimToggleChanged(bool enabled)
        {
            // Set auto aim toggle
            Debug.Log($"Auto aim setting: {(enabled ? "Enabled" : "Disabled")}");

            // Update display
            UpdateToggleDisplays();

            // Auto save
            SaveSettings();
        }

        private void UpdateVolumeDisplays()
        {
            if (masterVolumeValueText != null && masterVolumeSlider != null)
            {
                masterVolumeValueText.text = $"{Mathf.RoundToInt(masterVolumeSlider.value * 100)}%";
            }

            if (sfxVolumeValueText != null && sfxVolumeSlider != null)
            {
                sfxVolumeValueText.text = $"{Mathf.RoundToInt(sfxVolumeSlider.value * 100)}%";
            }

            if (musicVolumeValueText != null && musicVolumeSlider != null)
            {
                musicVolumeValueText.text = $"{Mathf.RoundToInt(musicVolumeSlider.value * 100)}%";
            }
        }

        private void UpdateToggleDisplays()
        {
            if (vibrationStatusText != null && vibrationToggle != null)
            {
                vibrationStatusText.text = vibrationToggle.isOn ? "Enabled" : "Disabled";
            }

            if (autoAimStatusText != null && autoAimToggle != null)
            {
                autoAimStatusText.text = autoAimToggle.isOn ? "Enabled" : "Disabled";
            }
        }

        public void ResetToDefaults()
        {
            // Reset to default settings
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = 0.8f;

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = 0.8f;

            if (musicVolumeSlider != null)
                musicVolumeSlider.value = 0.6f;

            if (vibrationToggle != null)
                vibrationToggle.isOn = true;

            if (autoAimToggle != null)
                autoAimToggle.isOn = false;

            // Save settings
            SaveSettings();

            Debug.Log("Settings have been reset to default values");
        }

        private void OnDestroy()
        {
            // Clean up events
            if (audioButton != null)
                audioButton.onClick.RemoveListener(OnAudioButtonClicked);

            if (controlsButton != null)
                controlsButton.onClick.RemoveListener(OnControlsButtonClicked);

            if (graphicsButton != null)
                graphicsButton.onClick.RemoveListener(OnGraphicsButtonClicked);

            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackButtonClicked);

            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

            if (vibrationToggle != null)
                vibrationToggle.onValueChanged.RemoveListener(OnVibrationToggleChanged);

            if (autoAimToggle != null)
                autoAimToggle.onValueChanged.RemoveListener(OnAutoAimToggleChanged);
        }
    }
}