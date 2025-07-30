using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UI;
using TMPro;
using PongHub.Core;
using PongHub.Core.Audio;
using PongHub.Gameplay;

namespace PongHub.UI
{
    public class MainMenuPanel : MonoBehaviour
    {
        [Header("游戏模式")]
        [SerializeField]
        [Tooltip("Game Mode Dropdown / 游戏模式下拉菜单 - Dropdown for selecting game mode")]
        private TMP_Dropdown m_gameModeDropdown;

        [SerializeField]
        [Tooltip("Difficulty Dropdown / 难度下拉菜单 - Dropdown for selecting difficulty level")]
        private TMP_Dropdown m_difficultyDropdown;

        [Header("UI按钮")]
        [SerializeField]
        [Tooltip("Start Game Button / 开始游戏按钮 - Button for starting the game")]
        private Button m_startGameButton;

        [SerializeField]
        [Tooltip("Settings Button / 设置按钮 - Button for opening settings")]
        private Button m_settingsButton;

        [SerializeField]
        [Tooltip("Quit Button / 退出按钮 - Button for quitting the game")]
        private Button m_quitButton;

        [Header("设置面板")]
        [SerializeField]
        [Tooltip("Settings Panel / 设置面板 - Panel containing settings UI")]
        private GameObject m_settingsPanel;

        [SerializeField]
        [Tooltip("Master Volume Slider / 主音量滑块 - Slider for master volume control")]
        private Slider m_masterVolumeSlider;

        [SerializeField]
        [Tooltip("Music Volume Slider / 音乐音量滑块 - Slider for music volume control")]
        private Slider m_musicVolumeSlider;

        [SerializeField]
        [Tooltip("SFX Volume Slider / 音效音量滑块 - Slider for sound effects volume control")]
        private Slider m_sfxVolumeSlider;

        [SerializeField]
        [Tooltip("Vibration Toggle / 振动开关 - Toggle for vibration settings")]
        private Toggle m_vibrationToggle;

        [SerializeField]
        [Tooltip("Settings Back Button / 设置返回按钮 - Button for returning from settings")]
        private Button m_settingsBackButton;

        private void Awake()
        {
            InitializeButtons();
        }

        public async Task InitializeAsync()
        {
            await Task.Yield();
            InitializeButtons();
        }

        private void InitializeButtons()
        {
            if (m_startGameButton != null)
            {
                m_startGameButton.onClick.AddListener(OnStartGameClicked);
            }

            if (m_settingsButton != null)
            {
                m_settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (m_quitButton != null)
            {
                m_quitButton.onClick.AddListener(OnQuitClicked);
            }

            if (m_settingsBackButton != null)
            {
                m_settingsBackButton.onClick.AddListener(() =>
                {
                    ShowSettingsPanel(false);
                    SaveSettings();
                });
            }
        }

        private void Start()
        {
            SetupDropdowns();
            LoadSettings();
        }

        private void SetupDropdowns()
        {
            // 游戏模式下拉菜单
            if (m_gameModeDropdown != null)
            {
                m_gameModeDropdown.ClearOptions();
                m_gameModeDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "单打",
                    "双打",
                    "练习模式"
                });
            }

            // 难度下拉菜单
            if (m_difficultyDropdown != null)
            {
                m_difficultyDropdown.ClearOptions();
                m_difficultyDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "简单",
                    "中等",
                    "困难"
                });
            }
        }

        private void LoadSettings()
        {
            // 加载音量设置
            if (m_masterVolumeSlider != null)
                m_masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            if (m_musicVolumeSlider != null)
                m_musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
            if (m_sfxVolumeSlider != null)
                m_sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);

            // 加载振动设置
            if (m_vibrationToggle != null)
                m_vibrationToggle.isOn = PlayerPrefs.GetInt("Vibration", 1) == 1;

            // 应用设置
            ApplySettings();
        }

        private void SaveSettings()
        {
            // 保存音量设置
            if (m_masterVolumeSlider != null)
                PlayerPrefs.SetFloat("MasterVolume", m_masterVolumeSlider.value);
            if (m_musicVolumeSlider != null)
                PlayerPrefs.SetFloat("MusicVolume", m_musicVolumeSlider.value);
            if (m_sfxVolumeSlider != null)
                PlayerPrefs.SetFloat("SFXVolume", m_sfxVolumeSlider.value);

            // 保存振动设置
            if (m_vibrationToggle != null)
                PlayerPrefs.SetInt("Vibration", m_vibrationToggle.isOn ? 1 : 0);

            PlayerPrefs.Save();
            ApplySettings();
        }

        private void ApplySettings()
        {
            // 应用音量设置
            float masterVolume = m_masterVolumeSlider != null ? m_masterVolumeSlider.value : 1f;
            float musicVolume = m_musicVolumeSlider != null ? m_musicVolumeSlider.value : 1f;
            float sfxVolume = m_sfxVolumeSlider != null ? m_sfxVolumeSlider.value : 1f;

            AudioManager.Instance.SetMasterVolume(masterVolume);
            AudioManager.Instance.SetMusicVolume(musicVolume);
            AudioManager.Instance.SetSFXVolume(sfxVolume);

            // 应用振动设置
            bool vibration = m_vibrationToggle != null ? m_vibrationToggle.isOn : true;
            // 实现振动设置功能
            ApplyVibrationSettings(vibration);
        }

        /// <summary>
        /// 应用振动设置
        /// </summary>
        /// <param name="enableVibration">是否启用振动</param>
        private void ApplyVibrationSettings(bool enableVibration)
        {
            // 查找VR输入管理器或触觉反馈组件
            var inputManager = FindObjectOfType<PongHub.Input.PongHubInputManager>();
            if (inputManager != null)
            {
                // 设置振动开关状态
                inputManager.SetHapticEnabled(enableVibration);
                Debug.Log($"[MainMenuPanel] 振动设置已应用: {(enableVibration ? "启用" : "禁用")}");
            }

            // 查找VR触觉反馈组件
            var hapticFeedback = FindObjectOfType<PongHub.UI.ModeSelection.VRHapticFeedback>();
            if (hapticFeedback != null)
            {
                hapticFeedback.SetEnabled(enableVibration);
                
                // 如果启用了振动，播放测试反馈
                if (enableVibration)
                {
                    hapticFeedback.PlayHaptic(PongHub.UI.ModeSelection.VRHapticFeedback.HapticType.Medium);
                }
            }

            // 保存到PlayerPrefs
            PlayerPrefs.SetInt("VibrationEnabled", enableVibration ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ShowSettingsPanel(bool show)
        {
            if (m_settingsPanel != null)
            {
                m_settingsPanel.SetActive(show);
            }
        }

        private void OnStartGameClicked()
        {
            MenuCanvasController menuCanvasController = FindObjectOfType<MenuCanvasController>();
            if (menuCanvasController != null)
            {
                menuCanvasController.StartGame();
            }
            else if (GameCore.Instance != null)
            {
                GameCore.Instance.StartGame();
            }
        }

        private void OnSettingsClicked()
        {
            MenuCanvasController menuCanvasController = FindObjectOfType<MenuCanvasController>();
            if (menuCanvasController != null)
            {
                menuCanvasController.ShowSettings();
            }
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}