using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UI;
using TMPro;
using PongHub.Core;
using PongHub.Core.Audio;

namespace PongHub.UI
{
    public class SettingsPanel : MonoBehaviour
    {
        [Header("音频设置")]
        [SerializeField]
        [Tooltip("Master Volume Slider / 主音量滑块 - Slider for adjusting master volume")]
        private Slider m_masterVolumeSlider;
        [SerializeField]
        [Tooltip("Music Volume Slider / 音乐音量滑块 - Slider for adjusting music volume")]
        private Slider m_musicVolumeSlider;
        [SerializeField]
        [Tooltip("SFX Volume Slider / 音效音量滑块 - Slider for adjusting sound effects volume")]
        private Slider m_sfxVolumeSlider;

        [Header("图形设置")]
        [SerializeField]
        [Tooltip("Quality Dropdown / 质量下拉菜单 - Dropdown for selecting graphics quality")]
        private TMP_Dropdown m_qualityDropdown;
        [SerializeField]
        [Tooltip("VSync Toggle / 垂直同步开关 - Toggle for vertical synchronization")]
        private Toggle m_vsyncToggle;
        [SerializeField]
        [Tooltip("Fullscreen Toggle / 全屏开关 - Toggle for fullscreen mode")]
        private Toggle m_fullscreenToggle;

        [Header("控制设置")]
        [SerializeField]
        [Tooltip("Vibration Intensity Slider / 振动强度滑块 - Slider for adjusting vibration intensity")]
        private Slider m_vibrationIntensitySlider;
        [SerializeField]
        [Tooltip("Invert Y Toggle / 反转Y轴开关 - Toggle for inverting Y axis")]
        private Toggle m_invertYToggle;
        [SerializeField]
        [Tooltip("Input Settings Button / 输入设置按钮 - Button to open input settings")]
        private Button m_inputSettingsButton;

        private void Start()
        {
            InitializeUI();
            LoadSettings();
        }

        private void InitializeUI()
        {
            if (m_masterVolumeSlider != null)
            {
                m_masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (m_musicVolumeSlider != null)
            {
                m_musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (m_sfxVolumeSlider != null)
            {
                m_sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            if (m_vibrationIntensitySlider != null)
            {
                m_vibrationIntensitySlider.onValueChanged.AddListener(OnVibrationIntensityChanged);
            }

            if (m_inputSettingsButton != null)
            {
                m_inputSettingsButton.onClick.AddListener(OnInputSettingsClicked);
            }
        }

        private void LoadSettings()
        {
            // 加载音频设置
            if (m_masterVolumeSlider != null)
            {
                m_masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            }

            if (m_musicVolumeSlider != null)
            {
                m_musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
            }

            if (m_sfxVolumeSlider != null)
            {
                m_sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            }

            // 加载振动设置
            if (m_vibrationIntensitySlider != null)
            {
                m_vibrationIntensitySlider.value = PlayerPrefs.GetFloat("VibrationIntensity", 1f);
            }
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(value);
            }
            PlayerPrefs.SetFloat("MasterVolume", value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(value);
            }
            PlayerPrefs.SetFloat("MusicVolume", value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(value);
            }
            PlayerPrefs.SetFloat("SFXVolume", value);
        }

        private void OnVibrationIntensityChanged(float value)
        {
            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.SetVibrationIntensity(value);
            }
            PlayerPrefs.SetFloat("VibrationIntensity", value);
        }

        private void OnInputSettingsClicked()
        {
            MenuCanvasController menuCanvasController = FindObjectOfType<MenuCanvasController>();
            if (menuCanvasController != null)
            {
                menuCanvasController.ShowInputSettings();
            }
        }

        public async Task InitializeAsync()
        {
            await Task.Yield();
            InitializeUI();
            LoadSettings();
        }
    }
}