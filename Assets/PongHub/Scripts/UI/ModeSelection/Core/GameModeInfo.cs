using System;
using System.Collections.Generic;
using UnityEngine;

namespace PongHub.UI.ModeSelection
{
    /// <summary>
    /// 游戏模式类型枚举
    /// </summary>
    public enum GameModeType
    {
        Practice,           // 练习模式
        AIBattle,          // AI对战
        LocalMultiplayer,  // 本地多人
        OnlineMultiplayer, // 在线多人
        Tournament,        // 锦标赛
        Training,          // 训练
        Custom             // 自定义
    }

    /// <summary>
    /// 游戏模式难度等级
    /// </summary>
    public enum DifficultyLevel
    {
        Beginner,    // 初学者
        Easy,        // 简单
        Normal,      // 普通
        Hard,        // 困难
        Expert,      // 专家
        Master       // 大师
    }

    /// <summary>
    /// 游戏模式信息
    /// 包含模式的所有配置和元数据
    /// </summary>
    [Serializable]
    public class GameModeInfo
    {
        [Header("基本信息")]
        [SerializeField] private string m_modeId;                    // 模式唯一标识
        [SerializeField] private string m_titleKey;                  // 本地化标题键
        [SerializeField] private string m_descriptionKey;            // 本地化描述键
        [SerializeField] private Sprite m_icon;                      // 模式图标
        [SerializeField] private Color m_themeColor = Color.white;   // 主题颜色

        [Header("游戏配置")]
        [SerializeField] private GameModeType m_modeType;            // 模式类型
        [SerializeField] private int m_minPlayers = 1;               // 最小玩家数
        [SerializeField] private int m_maxPlayers = 2;               // 最大玩家数
        [SerializeField] private bool m_requiresNetwork = false;     // 是否需要网络
        [SerializeField] private bool m_requiresAI = false;          // 是否需要AI
        [SerializeField] private List<DifficultyLevel> m_availableDifficulties; // 可用难度

        [Header("可用性")]
        [SerializeField] private bool m_isAvailable = true;          // 是否可用
        [SerializeField] private string m_availabilityConditionKey; // 可用性条件本地化键
        [SerializeField] private List<string> m_requiredFeatures;   // 需要的功能特性

        [Header("显示配置")]
        [SerializeField] private bool m_showInQuickStart = true;     // 是否在快速开始中显示
        [SerializeField] private int m_displayOrder = 0;            // 显示顺序
        [SerializeField] private bool m_isRecommended = false;       // 是否推荐模式

        // 运行时统计数据（不序列化，从存档加载）
        [NonSerialized] private int m_timesPlayed = 0;              // 游戏次数
        [NonSerialized] private float m_averageScore = 0f;          // 平均分数
        [NonSerialized] private DateTime m_lastPlayedTime;          // 最后游戏时间
        [NonSerialized] private float m_totalPlayTime = 0f;         // 总游戏时间

        #region 属性访问器

        public string ModeId => m_modeId;
        public string TitleKey => m_titleKey;
        public string DescriptionKey => m_descriptionKey;
        public Sprite Icon => m_icon;
        public Color ThemeColor => m_themeColor;

        public GameModeType ModeType => m_modeType;
        public int MinPlayers => m_minPlayers;
        public int MaxPlayers => m_maxPlayers;
        public bool RequiresNetwork => m_requiresNetwork;
        public bool RequiresAI => m_requiresAI;
        public List<DifficultyLevel> AvailableDifficulties => m_availableDifficulties;

        public bool IsAvailable => m_isAvailable;
        public string AvailabilityConditionKey => m_availabilityConditionKey;
        public List<string> RequiredFeatures => m_requiredFeatures;

        public bool ShowInQuickStart => m_showInQuickStart;
        public int DisplayOrder => m_displayOrder;
        public bool IsRecommended => m_isRecommended;

        public int TimesPlayed => m_timesPlayed;
        public float AverageScore => m_averageScore;
        public DateTime LastPlayedTime => m_lastPlayedTime;
        public float TotalPlayTime => m_totalPlayTime;

        #endregion

        #region 构造函数

        public GameModeInfo()
        {
            m_availableDifficulties = new List<DifficultyLevel>();
            m_requiredFeatures = new List<string>();
            m_lastPlayedTime = DateTime.MinValue;
        }

        public GameModeInfo(string modeId, string titleKey, GameModeType modeType) : this()
        {
            m_modeId = modeId;
            m_titleKey = titleKey;
            m_modeType = modeType;
            m_descriptionKey = titleKey + ".description";
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 检查模式是否可用
        /// </summary>
        /// <returns>是否可用</returns>
        public bool CheckAvailability()
        {
            if (!m_isAvailable)
                return false;

            // 检查网络需求
            if (m_requiresNetwork && !IsNetworkAvailable())
                return false;

            // 检查必需功能
            foreach (string feature in m_requiredFeatures)
            {
                if (!IsFeatureAvailable(feature))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 检查指定难度是否可用
        /// </summary>
        /// <param name="difficulty">难度等级</param>
        /// <returns>是否可用</returns>
        public bool IsDifficultyAvailable(DifficultyLevel difficulty)
        {
            return m_availableDifficulties.Contains(difficulty);
        }

        /// <summary>
        /// 获取默认难度
        /// </summary>
        /// <returns>默认难度</returns>
        public DifficultyLevel GetDefaultDifficulty()
        {
            if (m_availableDifficulties.Count == 0)
                return DifficultyLevel.Normal;

            // 返回中等难度，如果没有则返回第一个
            if (m_availableDifficulties.Contains(DifficultyLevel.Normal))
                return DifficultyLevel.Normal;

            return m_availableDifficulties[0];
        }

        /// <summary>
        /// 更新游戏统计数据
        /// </summary>
        /// <param name="score">本次得分</param>
        /// <param name="playTime">本次游戏时间</param>
        public void UpdateStats(float score, float playTime)
        {
            m_timesPlayed++;
            m_totalPlayTime += playTime;

            // 计算平均分数
            m_averageScore = (m_averageScore * (m_timesPlayed - 1) + score) / m_timesPlayed;

            m_lastPlayedTime = DateTime.Now;
        }

        /// <summary>
        /// 重置统计数据
        /// </summary>
        public void ResetStats()
        {
            m_timesPlayed = 0;
            m_averageScore = 0f;
            m_totalPlayTime = 0f;
            m_lastPlayedTime = DateTime.MinValue;
        }

        /// <summary>
        /// 获取平均游戏时长（分钟）
        /// </summary>
        /// <returns>平均游戏时长</returns>
        public float GetAveragePlayTime()
        {
            if (m_timesPlayed == 0)
                return 0f;

            return m_totalPlayTime / m_timesPlayed / 60f; // 转换为分钟
        }

        /// <summary>
        /// 从统计数据加载
        /// </summary>
        /// <param name="stats">统计数据</param>
        public void LoadStats(ModeStatsData stats)
        {
            m_timesPlayed = stats.timesPlayed;
            m_averageScore = stats.averageScore;
            m_totalPlayTime = stats.totalPlayTime;
            m_lastPlayedTime = stats.lastPlayedTime;
        }

        /// <summary>
        /// 保存统计数据
        /// </summary>
        /// <returns>统计数据</returns>
        public ModeStatsData SaveStats()
        {
            return new ModeStatsData
            {
                modeId = m_modeId,
                timesPlayed = m_timesPlayed,
                averageScore = m_averageScore,
                totalPlayTime = m_totalPlayTime,
                lastPlayedTime = m_lastPlayedTime
            };
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 检查网络是否可用
        /// </summary>
        /// <returns>网络是否可用</returns>
        private bool IsNetworkAvailable()
        {
            // 实现网络状态检查
            // 检查Unity网络连通性
            bool hasNetworkConnection = Application.internetReachability != NetworkReachability.NotReachable;
            
            // 检查Photon网络管理器状态
            var networkManager = FindObjectOfType<PongHub.Networking.PongHubNetworkManager>();
            bool photonConnected = false;
            
            if (networkManager != null)
            {
                // 检查Photon连接状态
                photonConnected = networkManager.IsConnectedAndReady;
            }
            
            // 检查Unity Netcode连接状态
            bool netcodeReady = false;
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                netcodeReady = Unity.Netcode.NetworkManager.Singleton.IsListening || 
                              Unity.Netcode.NetworkManager.Singleton.IsConnectedClient;
            }
            
            // 至少需要有网络连接
            bool isAvailable = hasNetworkConnection;
            
            Debug.Log($"[GameModeInfo] 网络状态检查 - 连通性: {hasNetworkConnection}, Photon: {photonConnected}, Netcode: {netcodeReady}");
            
            return isAvailable;
        }

        /// <summary>
        /// 检查功能是否可用
        /// </summary>
        /// <param name="feature">功能名称</param>
        /// <returns>功能是否可用</returns>
        private bool IsFeatureAvailable(string feature)
        {
            // 实现功能可用性检查
            switch (feature.ToLower())
            {
                case "multiplayer":
                case "网络模式":
                case "network":
                    return IsNetworkAvailable() && CheckMultiplayerRequirements();
                    
                case "ai":
                    return CheckAIRequirements();
                    
                case "voice":
                    return CheckVoiceRequirements();
                    
                case "vr":
                    return CheckVRRequirements();
                    
                case "avatar":
                    return CheckAvatarRequirements();
                    
                default:
                    return true; // 默认功能可用
            }
        }

        /// <summary>
        /// 检查多人游戏需求
        /// </summary>
        private bool CheckMultiplayerRequirements()
        {
            var networkManager = FindObjectOfType<PongHub.Networking.PongHubNetworkManager>();
            bool hasNetcode = Unity.Netcode.NetworkManager.Singleton != null;
            return networkManager != null && hasNetcode;
        }

        /// <summary>
        /// 检查AI功能需求
        /// </summary>
        private bool CheckAIRequirements()
        {
            var aiSingle = FindObjectOfType<PongHub.AI.AISingle>();
            return aiSingle != null; // AI功能始终可用
        }

        /// <summary>
        /// 检查语音功能需求
        /// </summary>
        private bool CheckVoiceRequirements()
        {
            return Microphone.devices.Length > 0;
        }

        /// <summary>
        /// 检查VR功能需求
        /// </summary>
        private bool CheckVRRequirements()
        {
            var vrInputManager = FindObjectOfType<PongHub.Input.PongHubInputManager>();
            bool isVREnabled = UnityEngine.XR.XRSettings.enabled;
            return vrInputManager != null && isVREnabled;
        }

        /// <summary>
        /// 检查Avatar功能需求
        /// </summary>
        private bool CheckAvatarRequirements()
        {
            var avatarEntity = FindObjectOfType<PongHub.Arena.Player.PlayerAvatarEntity>();
            return avatarEntity != null;
        }

        #endregion
    }

    /// <summary>
    /// 模式统计数据
    /// 用于序列化和存储
    /// </summary>
    [Serializable]
    public class ModeStatsData
    {
        public string modeId;
        public int timesPlayed;
        public float averageScore;
        public float totalPlayTime;
        public DateTime lastPlayedTime;
    }
}