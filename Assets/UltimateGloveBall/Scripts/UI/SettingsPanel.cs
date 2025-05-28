using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UI;
using TMPro;
using PongHub.Core;

namespace PongHub.UI
{
    public class SettingsPanel : MonoBehaviour
    {
        [Header("音频设置")]
        [SerializeField] private Slider m_masterVolumeSlider;
        [SerializeField] private Slider m_musicVolumeSlider;
        [SerializeField] private Slider m_sfxVolumeSlider;

        [Header("图形设置")]
        [SerializeField] private TMP_Dropdown m_qualityDropdown;
        [SerializeField] private Toggle m_vsyncToggle;
        [SerializeField] private Toggle m_fullscreenToggle;

        [Header("控制设置")]
        [SerializeField] private Slider m_vibrationIntensitySlider;
        [SerializeField] private Toggle m_invertYToggle;

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

        public async Task InitializeAsync()
        {
            await Task.Yield();
            InitializeUI();
            LoadSettings();
        }
    }
} 