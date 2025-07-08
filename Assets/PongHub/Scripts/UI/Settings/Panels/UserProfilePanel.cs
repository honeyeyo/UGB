using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using PongHub.UI.Settings.Core;
using PongHub.UI.Settings.Components;

namespace PongHub.UI.Settings.Panels
{
    /// <summary>
    /// 用户资料面板
    /// User profile panel for player information and preferences
    /// </summary>
    public class UserProfilePanel : MonoBehaviour
    {
        [Header("用户信息")]
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private SettingDropdown languageDropdown;
        [SerializeField] private Image avatarImage;
        [SerializeField] private Button changeAvatarButton;
        [SerializeField] private TextMeshProUGUI playerLevelText;
        [SerializeField] private Slider experienceSlider;
        [SerializeField] private TextMeshProUGUI experienceText;

        [Header("用户统计")]
        [SerializeField] private TextMeshProUGUI totalGamesText;
        [SerializeField] private TextMeshProUGUI winsText;
        [SerializeField] private TextMeshProUGUI lossesText;
        [SerializeField] private TextMeshProUGUI winRateText;
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private TextMeshProUGUI totalPlayTimeText;
        [SerializeField] private TextMeshProUGUI averageGameTimeText;

        [Header("成就系统")]
        [SerializeField] private Transform achievementsParent;
        [SerializeField] private GameObject achievementPrefab;
        [SerializeField] private TextMeshProUGUI achievementCountText;
        [SerializeField] private Button viewAllAchievementsButton;

        [Header("排行榜")]
        [SerializeField] private TextMeshProUGUI globalRankText;
        [SerializeField] private TextMeshProUGUI weeklyRankText;
        [SerializeField] private Button viewLeaderboardButton;

        [Header("社交功能")]
        [SerializeField] private TextMeshProUGUI friendCountText;
        [SerializeField] private Button manageFriendsButton;
        [SerializeField] private Button shareStatsButton;

        [Header("数据管理")]
        [SerializeField] private Button exportDataButton;
        [SerializeField] private Button resetStatsButton;
        [SerializeField] private Button deleteProfileButton;

        [Header("偏好设置")]
        [SerializeField] private SettingToggle showOnlineStatusToggle;
        [SerializeField] private SettingToggle allowFriendRequestsToggle;
        [SerializeField] private SettingToggle shareStatisticsToggle;
        [SerializeField] private SettingDropdown privacyLevelDropdown;

        // 成就数据
        [System.Serializable]
        public class Achievement
        {
            public string id;
            public string title;
            public string description;
            public Sprite icon;
            public bool unlocked;
            public System.DateTime unlockedDate;
        }

        [SerializeField] private Achievement[] availableAchievements;

        private SettingsManager settingsManager;
        private VRHapticFeedback hapticFeedback;
        private List<GameObject> achievementDisplays = new List<GameObject>();

        private void Awake()
        {
            settingsManager = SettingsManager.Instance;
            hapticFeedback = FindObjectOfType<VRHapticFeedback>();
        }

        private void Start()
        {
            SetupComponents();
            RefreshPanel();
            UpdateAchievements();
        }

        private void SetupComponents()
        {
            // 用户信息事件
            playerNameInput?.onValueChanged.AddListener(OnPlayerNameChanged);
            languageDropdown?.OnValueChanged.AddListener(OnLanguageChanged);
            changeAvatarButton?.onClick.AddListener(OnChangeAvatar);

            // 按钮事件
            viewAllAchievementsButton?.onClick.AddListener(OnViewAllAchievements);
            viewLeaderboardButton?.onClick.AddListener(OnViewLeaderboard);
            manageFriendsButton?.onClick.AddListener(OnManageFriends);
            shareStatsButton?.onClick.AddListener(OnShareStats);

            // 数据管理事件
            exportDataButton?.onClick.AddListener(OnExportData);
            resetStatsButton?.onClick.AddListener(OnResetStats);
            deleteProfileButton?.onClick.AddListener(OnDeleteProfile);

            // 偏好设置事件
            showOnlineStatusToggle?.OnValueChanged.AddListener(OnShowOnlineStatusChanged);
            allowFriendRequestsToggle?.OnValueChanged.AddListener(OnAllowFriendRequestsChanged);
            shareStatisticsToggle?.OnValueChanged.AddListener(OnShareStatisticsChanged);
            privacyLevelDropdown?.OnValueChanged.AddListener(OnPrivacyLevelChanged);
        }

        #region 用户信息事件

        private void OnPlayerNameChanged(string newName)
        {
            if (settingsManager != null && !string.IsNullOrEmpty(newName))
            {
                var userProfile = settingsManager.GetUserProfile();
                userProfile.playerName = newName;
                settingsManager.UpdateUserProfile(userProfile);

                // 触觉反馈
                if (hapticFeedback != null)
                {
                    hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.Selection);
                }
            }
        }

        private void OnLanguageChanged(int languageIndex)
        {
            if (settingsManager != null)
            {
                var userProfile = settingsManager.GetUserProfile();
                userProfile.language = (Language)languageIndex;
                settingsManager.UpdateUserProfile(userProfile);

                // 应用语言变更
                ApplyLanguageChange((Language)languageIndex);
            }
        }

        private void OnChangeAvatar()
        {
            // 打开头像选择界面
            Debug.Log("Opening avatar selection interface");

            if (hapticFeedback != null)
            {
                hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.Selection);
            }
        }

        #endregion

        #region 按钮事件

        private void OnViewAllAchievements()
        {
            Debug.Log("Opening achievements panel");

            if (hapticFeedback != null)
            {
                hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.PageChange);
            }
        }

        private void OnViewLeaderboard()
        {
            Debug.Log("Opening leaderboard");

            if (hapticFeedback != null)
            {
                hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.PageChange);
            }
        }

        private void OnManageFriends()
        {
            Debug.Log("Opening friends management");

            if (hapticFeedback != null)
            {
                hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.Selection);
            }
        }

        private void OnShareStats()
        {
            Debug.Log("Sharing statistics");

            if (hapticFeedback != null)
            {
                hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.ModeConfirm);
            }
        }

        #endregion

        #region 数据管理事件

        private void OnExportData()
        {
            if (settingsManager != null)
            {
                string exportPath = System.IO.Path.Combine(Application.persistentDataPath, "user_profile_export.json");
                _ = settingsManager.ExportSettingsAsync(exportPath);

                Debug.Log($"User data exported to: {exportPath}");

                if (hapticFeedback != null)
                {
                    hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.ModeConfirm);
                }
            }
        }

        private void OnResetStats()
        {
            // 显示确认对话框
            if (Application.isEditor)
            {
                if (UnityEditor.EditorUtility.DisplayDialog("重置统计", "确定要重置所有游戏统计数据吗？此操作不可撤销。", "确定", "取消"))
                {
                    ResetUserStatistics();
                }
            }
            else
            {
                // 在VR中使用自定义确认对话框
                ResetUserStatistics();
            }
        }

        private void OnDeleteProfile()
        {
            // 显示确认对话框
            if (Application.isEditor)
            {
                if (UnityEditor.EditorUtility.DisplayDialog("删除档案", "确定要删除整个用户档案吗？此操作不可撤销。", "确定", "取消"))
                {
                    DeleteUserProfile();
                }
            }
            else
            {
                // 在VR中使用自定义确认对话框
                DeleteUserProfile();
            }
        }

        #endregion

        #region 偏好设置事件

        private void OnShowOnlineStatusChanged(bool show)
        {
            if (settingsManager != null)
            {
                var userProfile = settingsManager.GetUserProfile();
                userProfile.showOnlineStatus = show;
                settingsManager.UpdateUserProfile(userProfile);
            }
        }

        private void OnAllowFriendRequestsChanged(bool allow)
        {
            if (settingsManager != null)
            {
                var userProfile = settingsManager.GetUserProfile();
                userProfile.allowFriendRequests = allow;
                settingsManager.UpdateUserProfile(userProfile);
            }
        }

        private void OnShareStatisticsChanged(bool share)
        {
            if (settingsManager != null)
            {
                var userProfile = settingsManager.GetUserProfile();
                userProfile.shareStatistics = share;
                settingsManager.UpdateUserProfile(userProfile);
            }
        }

        private void OnPrivacyLevelChanged(int level)
        {
            if (settingsManager != null)
            {
                var userProfile = settingsManager.GetUserProfile();
                userProfile.privacyLevel = (PrivacyLevel)level;
                settingsManager.UpdateUserProfile(userProfile);
            }
        }

        #endregion

        #region 数据处理

        /// <summary>
        /// 重置用户统计数据
        /// </summary>
        private void ResetUserStatistics()
        {
            if (settingsManager != null)
            {
                var userProfile = settingsManager.GetUserProfile();
                userProfile.totalGames = 0;
                userProfile.wins = 0;
                userProfile.losses = 0;
                userProfile.bestScore = 0;
                userProfile.totalPlayTime = 0f;
                userProfile.experience = 0;
                userProfile.level = 1;

                settingsManager.UpdateUserProfile(userProfile);
                RefreshPanel();

                Debug.Log("User statistics reset");

                if (hapticFeedback != null)
                {
                    hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.Warning);
                }
            }
        }

        /// <summary>
        /// 删除用户档案
        /// </summary>
        private void DeleteUserProfile()
        {
            if (settingsManager != null)
            {
                // 重置为默认用户档案
                settingsManager.ResetToDefaults();
                RefreshPanel();
                UpdateAchievements();

                Debug.Log("User profile deleted");

                if (hapticFeedback != null)
                {
                    hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.Warning);
                }
            }
        }

        /// <summary>
        /// 应用语言变更
        /// </summary>
        private void ApplyLanguageChange(Language language)
        {
            // 这里可以集成本地化系统
            Debug.Log($"Language changed to: {language}");

            // 刷新UI文本
            RefreshPanel();
        }

        #endregion

        #region 成就系统

        /// <summary>
        /// 更新成就显示
        /// </summary>
        private void UpdateAchievements()
        {
            if (achievementsParent == null || achievementPrefab == null) return;

            // 清理现有显示
            foreach (var display in achievementDisplays)
            {
                if (display != null)
                {
                    DestroyImmediate(display);
                }
            }
            achievementDisplays.Clear();

            int unlockedCount = 0;

            // 创建成就显示（只显示前几个）
            int displayCount = Mathf.Min(availableAchievements.Length, 5);
            for (int i = 0; i < displayCount; i++)
            {
                var achievement = availableAchievements[i];
                var display = Instantiate(achievementPrefab, achievementsParent);
                achievementDisplays.Add(display);

                // 配置成就显示
                ConfigureAchievementDisplay(display, achievement);

                if (achievement.unlocked)
                {
                    unlockedCount++;
                }
            }

            // 统计所有解锁的成就
            foreach (var achievement in availableAchievements)
            {
                if (achievement.unlocked)
                {
                    unlockedCount++;
                }
            }

            // 更新成就计数
            if (achievementCountText != null)
            {
                achievementCountText.text = $"{unlockedCount}/{availableAchievements.Length}";
            }
        }

        /// <summary>
        /// 配置成就显示
        /// </summary>
        private void ConfigureAchievementDisplay(GameObject display, Achievement achievement)
        {
            // 查找子组件
            var iconImage = display.GetComponentInChildren<Image>();
            var titleText = display.GetComponentsInChildren<TextMeshProUGUI>()[0];
            var descText = display.GetComponentsInChildren<TextMeshProUGUI>()[1];

            // 设置内容
            if (iconImage != null && achievement.icon != null)
            {
                iconImage.sprite = achievement.icon;
                iconImage.color = achievement.unlocked ? Color.white : Color.gray;
            }

            if (titleText != null)
            {
                titleText.text = achievement.title;
                titleText.color = achievement.unlocked ? Color.white : Color.gray;
            }

            if (descText != null)
            {
                descText.text = achievement.description;
                descText.color = achievement.unlocked ? Color.white : Color.gray;
            }
        }

        #endregion

        #region 统计计算

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics(UserProfile userProfile)
        {
            // 基础统计
            if (totalGamesText != null)
                totalGamesText.text = userProfile.totalGames.ToString();

            if (winsText != null)
                winsText.text = userProfile.wins.ToString();

            if (lossesText != null)
                lossesText.text = userProfile.losses.ToString();

            if (winRateText != null)
            {
                float winRate = userProfile.totalGames > 0 ?
                    (float)userProfile.wins / userProfile.totalGames * 100f : 0f;
                winRateText.text = $"{winRate:F1}%";
            }

            if (bestScoreText != null)
                bestScoreText.text = userProfile.bestScore.ToString();

            // 时间统计
            if (totalPlayTimeText != null)
            {
                var timeSpan = System.TimeSpan.FromSeconds(userProfile.totalPlayTime);
                totalPlayTimeText.text = $"{timeSpan.Hours}h {timeSpan.Minutes}m";
            }

            if (averageGameTimeText != null)
            {
                float avgTime = userProfile.totalGames > 0 ?
                    userProfile.totalPlayTime / userProfile.totalGames : 0f;
                var avgTimeSpan = System.TimeSpan.FromSeconds(avgTime);
                averageGameTimeText.text = $"{avgTimeSpan.Minutes}m {avgTimeSpan.Seconds}s";
            }

            // 等级和经验
            if (playerLevelText != null)
                playerLevelText.text = $"等级 {userProfile.level}";

            if (experienceSlider != null)
            {
                int expForCurrentLevel = GetExperienceForLevel(userProfile.level);
                int expForNextLevel = GetExperienceForLevel(userProfile.level + 1);
                int currentLevelExp = userProfile.experience - expForCurrentLevel;
                int expNeeded = expForNextLevel - expForCurrentLevel;

                experienceSlider.minValue = 0;
                experienceSlider.maxValue = expNeeded;
                experienceSlider.value = currentLevelExp;
            }

            if (experienceText != null)
            {
                int expForNextLevel = GetExperienceForLevel(userProfile.level + 1);
                int expNeeded = expForNextLevel - userProfile.experience;
                experienceText.text = $"{expNeeded} EXP 到下一级";
            }

            // 排行榜（模拟数据）
            if (globalRankText != null)
                globalRankText.text = $"全球排名: #{UnityEngine.Random.Range(1, 10000)}";

            if (weeklyRankText != null)
                weeklyRankText.text = $"本周排名: #{UnityEngine.Random.Range(1, 1000)}";

            // 社交（模拟数据）
            if (friendCountText != null)
                friendCountText.text = $"好友: {UnityEngine.Random.Range(0, 50)}";
        }

        /// <summary>
        /// 获取指定等级所需经验值
        /// </summary>
        private int GetExperienceForLevel(int level)
        {
            // 简单的经验值计算公式
            return (level - 1) * 1000 + (level - 1) * (level - 1) * 100;
        }

        #endregion

        public void RefreshPanel()
        {
            if (settingsManager == null) return;

            var userProfile = settingsManager.GetUserProfile();

            // 更新用户信息
            if (playerNameInput != null)
                playerNameInput.text = userProfile.playerName;

            languageDropdown?.SetValue((int)userProfile.language);

            // 更新统计信息
            UpdateStatistics(userProfile);

            // 更新偏好设置
            showOnlineStatusToggle?.SetValue(userProfile.showOnlineStatus);
            allowFriendRequestsToggle?.SetValue(userProfile.allowFriendRequests);
            shareStatisticsToggle?.SetValue(userProfile.shareStatistics);
            privacyLevelDropdown?.SetValue((int)userProfile.privacyLevel);
        }

        private void OnDestroy()
        {
            // 清理事件监听
            playerNameInput?.onValueChanged.RemoveListener(OnPlayerNameChanged);
            changeAvatarButton?.onClick.RemoveListener(OnChangeAvatar);
            viewAllAchievementsButton?.onClick.RemoveListener(OnViewAllAchievements);
            viewLeaderboardButton?.onClick.RemoveListener(OnViewLeaderboard);
            manageFriendsButton?.onClick.RemoveListener(OnManageFriends);
            shareStatsButton?.onClick.RemoveListener(OnShareStats);
            exportDataButton?.onClick.RemoveListener(OnExportData);
            resetStatsButton?.onClick.RemoveListener(OnResetStats);
            deleteProfileButton?.onClick.RemoveListener(OnDeleteProfile);
        }
    }
}