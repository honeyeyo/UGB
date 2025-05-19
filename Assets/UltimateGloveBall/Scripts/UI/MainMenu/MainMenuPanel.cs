using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PongHub.Core;

namespace PongHub.UI
{
    public class MainMenuPanel : MonoBehaviour
    {
        [Header("游戏模式")]
        [SerializeField] private TMP_Dropdown m_gameModeDropdown;
        [SerializeField] private TMP_Dropdown m_difficultyDropdown;

        [Header("按钮")]
        [SerializeField] private Button m_startGameButton;
        [SerializeField] private Button m_settingsButton;
        [SerializeField] private Button m_quitButton;

        [Header("设置面板")]
        [SerializeField] private GameObject m_settingsPanel;
        [SerializeField] private Slider m_masterVolumeSlider;
        [SerializeField] private Slider m_musicVolumeSlider;
        [SerializeField] private Slider m_sfxVolumeSlider;
        [SerializeField] private Toggle m_vibrationToggle;
        [SerializeField] private Button m_settingsBackButton;

        private void Start()
        {
            SetupDropdowns();
            SetupButtons();
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

        private void SetupButtons()
        {
            // 开始游戏按钮
            if (m_startGameButton != null)
            {
                m_startGameButton.onClick.AddListener(() =>
                {
                    int gameMode = m_gameModeDropdown.value;
                    int difficulty = m_difficultyDropdown.value;
                    GameManager.Instance.StartGame(gameMode, difficulty);
                });
            }

            // 设置按钮
            if (m_settingsButton != null)
            {
                m_settingsButton.onClick.AddListener(() =>
                {
                    ShowSettingsPanel(true);
                });
            }

            // 退出按钮
            if (m_quitButton != null)
            {
                m_quitButton.onClick.AddListener(() =>
                {
                    #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
                    #else
                        Application.Quit();
                    #endif
                });
            }

            // 设置返回按钮
            if (m_settingsBackButton != null)
            {
                m_settingsBackButton.onClick.AddListener(() =>
                {
                    ShowSettingsPanel(false);
                    SaveSettings();
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
            // TODO: 实现振动设置
        }

        private void ShowSettingsPanel(bool show)
        {
            if (m_settingsPanel != null)
            {
                m_settingsPanel.SetActive(show);
            }
        }
    }
}