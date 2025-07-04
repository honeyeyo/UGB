// Copyright (c) MagnusLab Inc. and affiliates.

using System.Collections.Generic;
using TMPro;
using PongHub.App;
using PongHub.Core.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace PongHub.MainMenu
{
    /// <summary>
    /// 设置菜单控制器类
    /// 负责管理设置菜单的UI元素和交互
    /// 通过Unity事件处理设置菜单的操作
    /// 通过滑块和开关按钮设置游戏设置
    /// 显示应用程序的当前版本
    /// </summary>
    public class SettingsMenu : BaseMenuController
    {
        [Header("音频设置")]
        [SerializeField]
        [Tooltip("Music Volume Slider / 音乐音量滑块 - Slider for adjusting music volume")]
        private Slider m_musicVolumeSlider;        // 音乐音量滑块

        [SerializeField]
        [Tooltip("Music Volume Value Text / 音乐音量数值文本 - Text displaying current music volume percentage")]
        private TMP_Text m_musicVolumeValueText;   // 音乐音量数值文本

        [Header("移动设置")]
        [SerializeField]
        [Tooltip("Snap Blackout Toggle / 瞬移黑屏开关 - Toggle for enabling blackout during snap turns")]
        private Toggle m_snapBlackoutToggle;       // 瞬移黑屏开关

        [SerializeField]
        [Tooltip("Free Locomotion Toggle / 自由移动开关 - Toggle for enabling free locomotion movement")]
        private Toggle m_freeLocomotionToggle;     // 自由移动开关

        [SerializeField]
        [Tooltip("Locomotion Vignette Toggle / 移动晕影开关 - Toggle for enabling vignette during locomotion")]
        private Toggle m_locomotionVignetteToggle; // 移动晕影开关

        [Header("网络设置")]
        [SerializeField]
        [Tooltip("Region Dropdown / 区域选择下拉框 - Dropdown for selecting network region")]
        private TMP_Dropdown m_regionDropDown;     // 区域选择下拉框

        [Header("版本信息")]
        [SerializeField]
        [Tooltip("Version Text / 版本号文本 - Text component displaying application version")]
        private TMP_Text m_versionText;            // 版本号文本

        // 是否处理区域变更的标志
        private bool m_handleRegionChange = false;

        /// <summary>
        /// 初始化设置菜单
        /// 设置各个UI控件的初始值和事件监听
        /// </summary>
        private void Awake()
        {
            // 初始化音频设置
            var audioInterface = UIAudioInterface.Instance;
            m_musicVolumeSlider.value = audioInterface.MusicVolume;
            m_musicVolumeValueText.text = audioInterface.MusicVolumePct.ToString("N0") + "%";

            // 初始化游戏设置
            var settings = GameSettings.Instance;
            m_snapBlackoutToggle.isOn = settings.UseBlackoutOnSnap;
            m_freeLocomotionToggle.isOn = !settings.IsFreeLocomotionDisabled;
            m_locomotionVignetteToggle.isOn = settings.UseLocomotionVignette;

            // 添加事件监听器
            m_musicVolumeSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            m_snapBlackoutToggle.onValueChanged.AddListener(OnSnapBlackoutChanged);
            m_freeLocomotionToggle.onValueChanged.AddListener(OnFreeLocomotionChanged);
            m_locomotionVignetteToggle.onValueChanged.AddListener(OnLocomotionVignetteChanged);
            m_regionDropDown.onValueChanged.AddListener(OnRegionValueChanged);

            // 设置版本号
            m_versionText.text = $"version: {Application.version}";
        }

        /// <summary>
        /// 当设置菜单启用时调用
        /// 初始化区域选择下拉框的选项
        /// </summary>
        private void OnEnable()
        {
            // 清空现有选项
            m_regionDropDown.ClearOptions();
            List<TMP_Dropdown.OptionData> options = new();

            // 获取网络层和当前区域信息
            var networkLayer = PHApplication.Instance.NetworkLayer;
            var currentRegion = networkLayer.GetRegion();
            var selected = -1;
            var index = 0;

            // 添加所有可用区域选项
            foreach (var region in networkLayer.EnabledRegions)
            {
                var code = region.Code;
                options.Add(new TMP_Dropdown.OptionData(NetworkRegionMapping.GetRegionName(code)));
                if (code == currentRegion)
                {
                    selected = index;
                }
                index++;
            }

            // 更新下拉框选项和选中值
            m_regionDropDown.AddOptions(options);
            m_handleRegionChange = false;
            m_regionDropDown.value = selected;
            m_handleRegionChange = true;
        }

        /// <summary>
        /// 音乐音量滑块值改变时的回调
        /// </summary>
        /// <param name="val">新的音量值</param>
        private void OnMusicSliderChanged(float val)
        {
            var audioInterface = UIAudioInterface.Instance;
            audioInterface.SetMusicVolume(val);
            m_musicVolumeValueText.text = audioInterface.MusicVolumePct.ToString("N0") + "%";

            // 播放滑块变化音效
            audioInterface.PlaySliderChanged();
        }

        /// <summary>
        /// 瞬移黑屏开关状态改变时的回调
        /// </summary>
        /// <param name="val">新的开关状态</param>
        private void OnSnapBlackoutChanged(bool val)
        {
            GameSettings.Instance.UseBlackoutOnSnap = val;
        }

        /// <summary>
        /// 自由移动开关状态改变时的回调
        /// </summary>
        /// <param name="val">新的开关状态</param>
        private void OnFreeLocomotionChanged(bool val)
        {
            GameSettings.Instance.IsFreeLocomotionDisabled = !val;
        }

        /// <summary>
        /// 移动晕影开关状态改变时的回调
        /// </summary>
        /// <param name="val">新的开关状态</param>
        private void OnLocomotionVignetteChanged(bool val)
        {
            GameSettings.Instance.UseLocomotionVignette = val;
        }

        /// <summary>
        /// 区域选择下拉框值改变时的回调
        /// </summary>
        /// <param name="region">选中的区域索引</param>
        private void OnRegionValueChanged(int region)
        {
            if (!m_handleRegionChange)
            {
                return;
            }
            var networkLayer = PHApplication.Instance.NetworkLayer;
            networkLayer.SetRegion(networkLayer.EnabledRegions[region].Code);
        }
    }
}