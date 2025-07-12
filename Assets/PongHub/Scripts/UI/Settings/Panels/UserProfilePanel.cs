using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PongHub.UI.Settings.Core;
using PongHub.UI.Settings.Components;

namespace PongHub.UI.Settings.Panels
{
    /// <summary>
    /// 用户资料设置面板
    /// </summary>
    public class UserProfilePanel : MonoBehaviour
    {
        [Header("基本资料")]
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private Slider heightSlider;
        [SerializeField] private TextMeshProUGUI heightValueText;
        [SerializeField] private SettingDropdown handPreferenceDropdown;
        [SerializeField] private SettingDropdown experienceDropdown;

        [Header("偏好设置")]
        [SerializeField] private SettingDropdown languageDropdown;
        [SerializeField] private SettingToggle showOnlineStatusToggle;
        [SerializeField] private SettingToggle allowFriendRequestsToggle;
        [SerializeField] private SettingToggle shareStatisticsToggle;
        [SerializeField] private SettingDropdown privacyLevelDropdown;

        [Header("统计信息")]
        [SerializeField] private TextMeshProUGUI totalGamesText;
        [SerializeField] private TextMeshProUGUI winsText;
        [SerializeField] private TextMeshProUGUI lossesText;
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private TextMeshProUGUI winRateText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI totalPlayTimeText;
        [SerializeField] private TextMeshProUGUI lastPlayedText;

        [Header("成就")]
        [SerializeField] private Transform achievementsContainer;
        [SerializeField] private Button achievementItemPrefab;

        [Header("头像")]
        [SerializeField] private RawImage avatarImage;
        [SerializeField] private Button changeAvatarButton;

        [Header("按钮")]
        [SerializeField] private Button resetStatsButton;
        [SerializeField] private Button exportDataButton;

        private UserProfile currentProfile;
        private GameplaySettings gameplaySettings;
        private bool isUpdating = false;

        private void Start()
        {
            InitializePanel();
            SetupEventHandlers();
            LoadCurrentProfile();
        }

        private void InitializePanel()
        {
            // 初始化手偏好下拉框
            handPreferenceDropdown.ClearOptions();
            handPreferenceDropdown.AddOptions(new[] { "左手", "右手", "双手" });

            // 初始化经验等级下拉框
            experienceDropdown.ClearOptions();
            experienceDropdown.AddOptions(new[] { "初学者", "中级", "高级", "专家" });

            // 初始化语言下拉框
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new[] { "英语", "中文", "日语", "韩语", "法语", "德语", "西班牙语" });

            // 初始化隐私等级下拉框
            privacyLevelDropdown.ClearOptions();
            privacyLevelDropdown.AddOptions(new[] { "公开", "仅朋友", "私有" });

            // 设置身高滑块
            heightSlider.minValue = 120f;
            heightSlider.maxValue = 220f;
            heightSlider.wholeNumbers = true;
        }

        private void SetupEventHandlers()
        {
            // 基本资料事件
            playerNameInput.onValueChanged.AddListener(OnPlayerNameChanged);
            heightSlider.onValueChanged.AddListener(OnHeightChanged);
            handPreferenceDropdown.onValueChanged.AddListener(OnHandPreferenceChanged);
            experienceDropdown.onValueChanged.AddListener(OnExperienceChanged);

            // 偏好设置事件
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
            showOnlineStatusToggle.onValueChanged.AddListener(OnShowOnlineStatusChanged);
            allowFriendRequestsToggle.onValueChanged.AddListener(OnAllowFriendRequestsChanged);
            shareStatisticsToggle.onValueChanged.AddListener(OnShareStatisticsChanged);
            privacyLevelDropdown.onValueChanged.AddListener(OnPrivacyLevelChanged);

            // 按钮事件
            resetStatsButton.onClick.AddListener(OnResetStatsClick);
            exportDataButton.onClick.AddListener(OnExportDataClick);
            changeAvatarButton.onClick.AddListener(OnChangeAvatarClick);
        }

        private void LoadCurrentProfile()
        {
            currentProfile = SettingsManager.Instance.GetUserProfile();
            gameplaySettings = SettingsManager.Instance.GetGameplaySettings();
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (currentProfile == null) return;

            isUpdating = true;

            // 基本资料
            playerNameInput.text = currentProfile.playerName;
            heightSlider.value = currentProfile.heightCm;
            UpdateHeightText();
            handPreferenceDropdown.value = (int)currentProfile.handPreference;
            experienceDropdown.value = (int)currentProfile.experience;

            // 偏好设置 - 映射到gameplay settings
            languageDropdown.value = (int)gameplaySettings.language;

            // 隐私设置 - 使用默认值（因为UserProfile中没有这些字段）
            showOnlineStatusToggle.isOn = true;
            allowFriendRequestsToggle.isOn = true;
            shareStatisticsToggle.isOn = gameplaySettings.showStatistics;
            privacyLevelDropdown.value = 0; // 默认公开

            // 统计信息
            totalGamesText.text = currentProfile.totalMatches.ToString();
            winsText.text = currentProfile.wins.ToString();
            lossesText.text = Mathf.Max(0, currentProfile.totalMatches - currentProfile.wins).ToString();
            bestScoreText.text = "N/A"; // UserProfile中没有bestScore字段
            winRateText.text = $"{currentProfile.GetWinRate() * 100:F1}%";
            levelText.text = GetLevelText(currentProfile.experience);
            totalPlayTimeText.text = FormatPlayTime(currentProfile.totalPlayTime);
            lastPlayedText.text = currentProfile.lastPlayed.ToString("yyyy-MM-dd");

            // 成就
            UpdateAchievements();

            isUpdating = false;
        }

        private void UpdateHeightText()
        {
            heightValueText.text = $"{heightSlider.value:F0} cm";
        }

        private string GetLevelText(ExperienceLevel level)
        {
            switch (level)
            {
                case ExperienceLevel.Beginner: return "初学者";
                case ExperienceLevel.Intermediate: return "中级";
                case ExperienceLevel.Advanced: return "高级";
                case ExperienceLevel.Expert: return "专家";
                default: return "未知";
            }
        }

        private string FormatPlayTime(int totalMinutes)
        {
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            return $"{hours}小时 {minutes}分钟";
        }

        private void UpdateAchievements()
        {
            // 清空现有成就
            foreach (Transform child in achievementsContainer)
            {
                if (child != achievementItemPrefab.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            // 添加成就
            if (currentProfile.achievements != null)
            {
                foreach (string achievement in currentProfile.achievements)
                {
                    GameObject achievementItem = Instantiate(achievementItemPrefab.gameObject, achievementsContainer);
                    TextMeshProUGUI achievementText = achievementItem.GetComponentInChildren<TextMeshProUGUI>();
                    if (achievementText != null)
                    {
                        achievementText.text = achievement;
                    }
                    achievementItem.SetActive(true);
                }
            }
        }

        #region 事件处理器

        private void OnPlayerNameChanged(string newName)
        {
            if (isUpdating) return;
            currentProfile.playerName = newName;
            SettingsManager.Instance.SaveUserProfile(currentProfile);
        }

        private void OnHeightChanged(float newHeight)
        {
            if (isUpdating) return;
            currentProfile.heightCm = newHeight;
            UpdateHeightText();
            SettingsManager.Instance.SaveUserProfile(currentProfile);
        }

        private void OnHandPreferenceChanged(int value)
        {
            if (isUpdating) return;
            currentProfile.handPreference = (HandPreference)value;
            SettingsManager.Instance.SaveUserProfile(currentProfile);
        }

        private void OnExperienceChanged(int value)
        {
            if (isUpdating) return;
            currentProfile.experience = (ExperienceLevel)value;
            SettingsManager.Instance.SaveUserProfile(currentProfile);
        }

        private void OnLanguageChanged(int value)
        {
            if (isUpdating) return;
            gameplaySettings.language = (LanguageCode)value;
            SettingsManager.Instance.SaveGameplaySettings(gameplaySettings);
        }

        private void OnShowOnlineStatusChanged(bool value)
        {
            if (isUpdating) return;
            // 这个功能暂时没有对应的存储字段，只是UI展示
            Debug.Log($"显示在线状态: {value}");
        }

        private void OnAllowFriendRequestsChanged(bool value)
        {
            if (isUpdating) return;
            // 这个功能暂时没有对应的存储字段，只是UI展示
            Debug.Log($"允许好友请求: {value}");
        }

        private void OnShareStatisticsChanged(bool value)
        {
            if (isUpdating) return;
            gameplaySettings.showStatistics = value;
            SettingsManager.Instance.SaveGameplaySettings(gameplaySettings);
        }

        private void OnPrivacyLevelChanged(int value)
        {
            if (isUpdating) return;
            // 这个功能暂时没有对应的存储字段，只是UI展示
            Debug.Log($"隐私级别: {value}");
        }

        private void OnResetStatsClick()
        {
            if (currentProfile == null) return;

            // 显示确认对话框
            if (ShowResetConfirmation())
            {
                // 重置统计数据
                currentProfile.totalMatches = 0;
                currentProfile.wins = 0;
                currentProfile.totalPlayTime = 0;
                currentProfile.achievements.Clear();

                SettingsManager.Instance.SaveUserProfile(currentProfile);
                UpdateUI();
            }
        }

        private void OnExportDataClick()
        {
            // 导出用户数据
            string json = JsonUtility.ToJson(currentProfile, true);
            Debug.Log("导出用户数据：\n" + json);

            // 这里可以添加保存到文件的逻辑
            // System.IO.File.WriteAllText("user_profile.json", json);
        }

        private void OnChangeAvatarClick()
        {
            // 更换头像功能
            Debug.Log("更换头像功能待实现");
        }

        #endregion

        #region 辅助方法

        private bool ShowResetConfirmation()
        {
            // 简化的确认对话框，实际项目中应该使用UI对话框
            return true;
        }

        private void RefreshProfile()
        {
            LoadCurrentProfile();
        }

        #endregion

        #region 设置面板接口

        public void ResetToDefaults()
        {
            if (currentProfile == null) return;

            currentProfile.playerName = "Player";
            currentProfile.heightCm = 170f;
            currentProfile.handPreference = HandPreference.Right;
            currentProfile.experience = ExperienceLevel.Intermediate;
            currentProfile.preferredPaddleColor = "Blue";

            SettingsManager.Instance.SaveUserProfile(currentProfile);
            UpdateUI();
        }

        public void OnProfileUpdated(UserProfile newProfile)
        {
            currentProfile = newProfile;
            UpdateUI();
        }

        /// <summary>
        /// 刷新面板
        /// </summary>
        public void RefreshPanel()
        {
            LoadCurrentProfile();
        }

        #endregion
    }
}