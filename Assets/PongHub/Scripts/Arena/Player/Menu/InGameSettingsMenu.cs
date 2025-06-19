// Copyright (c) MagnusLab Inc. and affiliates.

using TMPro;
using PongHub.App;
using PongHub.Arena.Spectator;
using PongHub.Core.Audio;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace PongHub.Arena.Player.Menu
{
    /// <summary>
    /// Game settings menu view for the in game menu.
    /// </summary>
    public class InGameSettingsMenu : BasePlayerMenuView
    {
        [SerializeField] private Slider m_musicVolumeSlider;
        [SerializeField] private TMP_Text m_musicVolumeValueText;

        [SerializeField] private Slider m_sfxVolumeSlider;
        [SerializeField] private TMP_Text m_sfxVolumeValueText;

        [SerializeField] private Slider m_crowdVolumeSlider;
        [SerializeField] private TMP_Text m_crowdVolumeValueText;

        [SerializeField] private Toggle m_snapBlackoutToggle;
        [SerializeField] private Toggle m_freeLocomotionToggle;
        [SerializeField] private Toggle m_locomotionVignetteToggle;

        [Header("Spawn Cat")]
        [SerializeField] private Button m_spawnCatButton;

        [Header("Spectator")]
        [SerializeField] private Button m_switchSideButton;

        private void Start()
        {
            m_musicVolumeSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            m_sfxVolumeSlider.onValueChanged.AddListener(OnSfxSliderChanged);
            m_crowdVolumeSlider.onValueChanged.AddListener(OnCrowdSliderChanged);
            m_snapBlackoutToggle.onValueChanged.AddListener(OnSnapBlackoutChanged);
            m_freeLocomotionToggle.onValueChanged.AddListener(OnFreeLocomotionChanged);
            m_locomotionVignetteToggle.onValueChanged.AddListener(OnLocomotionVignetteChanged);
        }

        private void OnEnable()
        {
            m_switchSideButton.gameObject.SetActive(LocalPlayerState.Instance.IsSpectator);

            m_spawnCatButton.gameObject.SetActive(!LocalPlayerState.Instance.IsSpectator &&
                !LocalPlayerState.Instance.SpawnCatInNextGame && GameSettings.Instance.OwnedCatsCount > 0);

            var audioInterface = UIAudioInterface.Instance;
            m_musicVolumeSlider.value = audioInterface.MusicVolume;
            m_musicVolumeValueText.text = audioInterface.MusicVolumePct.ToString("N0") + "%";
            m_sfxVolumeSlider.value = audioInterface.SfxVolume;
            m_sfxVolumeValueText.text = audioInterface.SfxVolumePct.ToString("N0") + "%";
            m_crowdVolumeSlider.value = audioInterface.CrowdVolume;
            m_crowdVolumeValueText.text = audioInterface.CrowdVolumePct.ToString("N0") + "%";
            var settings = GameSettings.Instance;
            m_snapBlackoutToggle.isOn = settings.UseBlackoutOnSnap;
            m_freeLocomotionToggle.isOn = !settings.IsFreeLocomotionDisabled;
            m_locomotionVignetteToggle.isOn = settings.UseLocomotionVignette;

        }

        public void OnSwitchSidesButtonClicked()
        {
            var spectatorNet =
                NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<SpectatorNetwork>();

            spectatorNet.RequestSwitchSide();
        }

        public void OnSpawnCatButtonClicked()
        {
            if (GameSettings.Instance.OwnedCatsCount > 0)
            {
                GameSettings.Instance.OwnedCatsCount--;
                LocalPlayerState.Instance.SpawnCatInNextGame = true;
            }
            m_spawnCatButton.gameObject.SetActive(false);
        }

        private void OnMusicSliderChanged(float val)
        {
            var audioInterface = UIAudioInterface.Instance;
            audioInterface.SetMusicVolume(val);
            m_musicVolumeValueText.text = audioInterface.MusicVolumePct.ToString("N0") + "%";
            audioInterface.PlaySliderChanged();
        }

        private void OnSfxSliderChanged(float val)
        {
            var audioInterface = UIAudioInterface.Instance;
            audioInterface.SetSfxVolume(val);
            m_sfxVolumeValueText.text = audioInterface.SfxVolumePct.ToString("N0") + "%";
            audioInterface.PlaySliderChanged();
        }

        private void OnCrowdSliderChanged(float val)
        {
            var audioInterface = UIAudioInterface.Instance;
            audioInterface.SetCrowdVolume(val);
            m_crowdVolumeValueText.text = audioInterface.CrowdVolumePct.ToString("N0") + "%";
            audioInterface.PlaySliderChanged();
        }

        private void OnSnapBlackoutChanged(bool val)
        {
            GameSettings.Instance.UseBlackoutOnSnap = val;
        }

        private void OnFreeLocomotionChanged(bool val)
        {
            GameSettings.Instance.IsFreeLocomotionDisabled = !val;
            PlayerInputController.Instance.OnSettingsUpdated();
        }

        private void OnLocomotionVignetteChanged(bool val)
        {
            GameSettings.Instance.UseLocomotionVignette = val;
        }
    }
}