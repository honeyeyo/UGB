// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using UnityEngine;

namespace PongHub.App
{
    /// <summary>
    /// 游戏设置的包装类，基于PlayerPrefs实现。可以通过settings实例来获取或设置游戏设置。
    /// </summary>
    public class GameSettings
    {
        #region singleton
        /// <summary>
        /// 单例实例
        /// </summary>
        private static GameSettings s_instance;

        /// <summary>
        /// 获取GameSettings的单例实例
        /// </summary>
        public static GameSettings Instance
        {
            get
            {
                s_instance ??= new GameSettings();
                return s_instance;
            }
        }

        /// <summary>
        /// 在子系统注册时销毁实例
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void DestroyInstance()
        {
            s_instance = null;
        }
        #endregion

        #region 常量定义
        /// <summary>
        /// PlayerPrefs中存储的键名
        /// </summary>
        private const string KEY_MUSIC_VOLUME = "MusicVolume";
        private const string KEY_SFX_VOLUME = "SfxVolume";
        private const string KEY_CROWD_VOLUME = "CrowdVolume";
        private const string KEY_SNAP_BLACKOUT = "BalckoutOnSnap";
        private const string KEY_DISABLE_FREE_LOCOMOTION = "DisableFreeLocomotion";
        private const string KEY_LOCOMOTION_VIGNETTE = "LocomotionVignette";
        private const string KEY_SELECTED_USER_ICON_SKU = "SelectedUserIcon";
        private const string KEY_OWNED_CAT_COUNT = "OwnedCatCount";

        /// <summary>
        /// 默认值设置
        /// </summary>
        private const float DEFAULT_MUSIC_VOLUME = 0.5f;
        private const float DEFAULT_SFX_VOLUME = 1.0f;
        private const float DEFAULT_CROWD_VOLUME = 1.0f;
        private const bool DEFAULT_BLACKOUT_ON_SNAP_MOVE = false;
        private const bool DEFAULT_DISABLE_FREE_LOCOMOTION = false;
        private const bool DEFAULT_LOCOMOTION_VIGNETTE = true;
        private const string DEFAULT_USER_ICON_SKU = null;
        private const int DEFAULT_OWNED_CAT_COUNT = 0;
        #endregion

        #region 音频设置
        /// <summary>
        /// 音乐音量
        /// </summary>
        private float m_musicVolume;
        public float MusicVolume
        {
            get => m_musicVolume;
            set
            {
                m_musicVolume = value;
                SetFloat(KEY_MUSIC_VOLUME, m_musicVolume);
            }
        }

        /// <summary>
        /// 音效音量
        /// </summary>
        private float m_sfxVolume;
        public float SfxVolume
        {
            get => m_sfxVolume;
            set
            {
                m_sfxVolume = value;
                SetFloat(KEY_SFX_VOLUME, m_sfxVolume);
            }
        }

        /// <summary>
        /// 人群音量
        /// </summary>
        private float m_crowdVolume;
        public float CrowdVolume
        {
            get => m_crowdVolume;
            set
            {
                m_crowdVolume = value;
                SetFloat(KEY_CROWD_VOLUME, m_crowdVolume);
            }
        }
        #endregion

        #region 移动设置
        /// <summary>
        /// 是否在瞬移时使用黑屏效果
        /// </summary>
        private bool m_useBlackoutOnSnap;
        public bool UseBlackoutOnSnap
        {
            get => m_useBlackoutOnSnap;
            set
            {
                m_useBlackoutOnSnap = value;
                SetBool(KEY_SNAP_BLACKOUT, m_useBlackoutOnSnap);
            }
        }

        /// <summary>
        /// 是否禁用自由移动
        /// </summary>
        private bool m_isFreeLocomotionDisabled;
        public bool IsFreeLocomotionDisabled
        {
            get => m_isFreeLocomotionDisabled;
            set
            {
                m_isFreeLocomotionDisabled = value;
                SetBool(KEY_DISABLE_FREE_LOCOMOTION, m_isFreeLocomotionDisabled);
            }
        }

        /// <summary>
        /// 是否使用移动时的晕影效果
        /// </summary>
        private bool m_useLocomotionVignette;
        public bool UseLocomotionVignette
        {
            get => m_useLocomotionVignette;
            set
            {
                m_useLocomotionVignette = value;
                SetBool(KEY_LOCOMOTION_VIGNETTE, m_useLocomotionVignette);
            }
        }
        #endregion

        #region 用户设置
        /// <summary>
        /// 用户选择的图标SKU
        /// </summary>
        private string m_selectedUserIconSku;
        public string SelectedUserIconSku
        {
            get => m_selectedUserIconSku;
            set
            {
                m_selectedUserIconSku = value;
                SetString(KEY_SELECTED_USER_ICON_SKU, m_selectedUserIconSku);
            }
        }

        /// <summary>
        /// 拥有的猫咪数量
        /// </summary>
        private int m_ownedCatsCount;
        public int OwnedCatsCount
        {
            get => m_ownedCatsCount;
            set
            {
                m_ownedCatsCount = Mathf.Max(0, value);
                SetInt(KEY_OWNED_CAT_COUNT, m_ownedCatsCount);
            }
        }
        #endregion

        /// <summary>
        /// 构造函数，初始化所有设置值
        /// </summary>
        private GameSettings()
        {
            m_musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME, DEFAULT_MUSIC_VOLUME);
            m_sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOLUME, DEFAULT_SFX_VOLUME);
            m_crowdVolume = PlayerPrefs.GetFloat(KEY_CROWD_VOLUME, DEFAULT_CROWD_VOLUME);
            m_useBlackoutOnSnap = GetBool(KEY_SNAP_BLACKOUT, DEFAULT_BLACKOUT_ON_SNAP_MOVE);
            m_isFreeLocomotionDisabled = GetBool(KEY_DISABLE_FREE_LOCOMOTION, DEFAULT_DISABLE_FREE_LOCOMOTION);
            m_useLocomotionVignette = GetBool(KEY_LOCOMOTION_VIGNETTE, DEFAULT_LOCOMOTION_VIGNETTE);
            m_selectedUserIconSku = PlayerPrefs.GetString(KEY_SELECTED_USER_ICON_SKU, DEFAULT_USER_ICON_SKU);
            m_ownedCatsCount = PlayerPrefs.GetInt(KEY_OWNED_CAT_COUNT, DEFAULT_OWNED_CAT_COUNT);
        }

        #region PlayerPrefs辅助方法
        /// <summary>
        /// 设置浮点数值
        /// </summary>
        private void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
        }

        /// <summary>
        /// 获取布尔值
        /// </summary>
        private bool GetBool(string key, bool defaultValue)
        {
            return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;
        }

        /// <summary>
        /// 设置布尔值
        /// </summary>
        private void SetBool(string key, bool value)
        {
            SetInt(key, value ? 1 : 0);
        }

        /// <summary>
        /// 设置字符串值
        /// </summary>
        private void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        /// <summary>
        /// 设置整数值
        /// </summary>
        private void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }
        #endregion
    }
}