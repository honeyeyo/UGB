using System.Collections.Generic;
using UnityEngine;

namespace PongHub.UI.ModeSelection
{
    /// <summary>
    /// 模式选择配置
    /// ScriptableObject，用于配置模式选择界面
    /// </summary>
    [CreateAssetMenu(fileName = "ModeSelectionConfig", menuName = "PongHub/UI/Mode Selection Config")]
    public class ModeSelectionConfig : ScriptableObject
    {
        [Header("可用模式")]
        [SerializeField] private List<GameModeInfo> m_availableModes = new List<GameModeInfo>();
        [SerializeField] private string m_defaultModeId = "practice";
        [SerializeField] private bool m_sortByDisplayOrder = true;

        [Header("界面布局")]
        [SerializeField] private float m_cardSpacing = 20f;
        [SerializeField] private int m_cardsPerRow = 2;
        [SerializeField] private Vector2 m_cardSize = new Vector2(300f, 200f);
        [SerializeField] private float m_maxPanelWidth = 800f;

        [Header("动画配置")]
        [SerializeField] private float m_animationDuration = 0.3f;
        [SerializeField] private AnimationCurve m_transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float m_cardHoverScale = 1.05f;
        [SerializeField] private float m_cardSelectScale = 0.95f;

        [Header("快捷功能")]
        [SerializeField] private bool m_enableQuickStart = true;
        [SerializeField] private bool m_rememberLastMode = true;
        [SerializeField] private bool m_enableRandomMode = true;
        [SerializeField] private bool m_enableModePreview = true;

        [Header("视觉效果")]
        [SerializeField] private Color m_selectedModeColor = new Color(0.2f, 0.6f, 1f, 1f);
        [SerializeField] private Color m_unavailableModeColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        [SerializeField] private Color m_recommendedModeColor = new Color(1f, 0.8f, 0.2f, 1f);
        [SerializeField] private float m_glowIntensity = 1.5f;

        [Header("音效配置")]
        [SerializeField] private AudioClip m_modeSelectSound;
        [SerializeField] private AudioClip m_modeHoverSound;
        [SerializeField] private AudioClip m_modeConfirmSound;
        [SerializeField] private AudioClip m_modeUnavailableSound;

        [Header("本地化键")]
        [SerializeField] private string m_titleKey = "mode_selection.title";
        [SerializeField] private string m_quickStartKey = "mode_selection.quick_start";
        [SerializeField] private string m_randomModeKey = "mode_selection.random_mode";
        [SerializeField] private string m_lastPlayedKey = "mode_selection.last_played";
        [SerializeField] private string m_recommendedKey = "mode_selection.recommended";

        [Header("性能优化")]
        [SerializeField] private bool m_enableCardPooling = true;
        [SerializeField] private int m_maxPoolSize = 10;
        [SerializeField] private bool m_enableAsyncLoading = true;

        #region 属性访问器

        public List<GameModeInfo> AvailableModes => m_availableModes;
        public string DefaultModeId => m_defaultModeId;
        public bool SortByDisplayOrder => m_sortByDisplayOrder;

        public float CardSpacing => m_cardSpacing;
        public int CardsPerRow => m_cardsPerRow;
        public Vector2 CardSize => m_cardSize;
        public float MaxPanelWidth => m_maxPanelWidth;

        public float AnimationDuration => m_animationDuration;
        public AnimationCurve TransitionCurve => m_transitionCurve;
        public float CardHoverScale => m_cardHoverScale;
        public float CardSelectScale => m_cardSelectScale;

        public bool EnableQuickStart => m_enableQuickStart;
        public bool RememberLastMode => m_rememberLastMode;
        public bool EnableRandomMode => m_enableRandomMode;
        public bool EnableModePreview => m_enableModePreview;

        public Color SelectedModeColor => m_selectedModeColor;
        public Color UnavailableModeColor => m_unavailableModeColor;
        public Color RecommendedModeColor => m_recommendedModeColor;
        public float GlowIntensity => m_glowIntensity;

        public AudioClip ModeSelectSound => m_modeSelectSound;
        public AudioClip ModeHoverSound => m_modeHoverSound;
        public AudioClip ModeConfirmSound => m_modeConfirmSound;
        public AudioClip ModeUnavailableSound => m_modeUnavailableSound;

        public string TitleKey => m_titleKey;
        public string QuickStartKey => m_quickStartKey;
        public string RandomModeKey => m_randomModeKey;
        public string LastPlayedKey => m_lastPlayedKey;
        public string RecommendedKey => m_recommendedKey;

        public bool EnableCardPooling => m_enableCardPooling;
        public int MaxPoolSize => m_maxPoolSize;
        public bool EnableAsyncLoading => m_enableAsyncLoading;

        #endregion

        #region Unity生命周期

        private void OnValidate()
        {
            // 验证配置参数
            ValidateConfiguration();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取指定ID的模式信息
        /// </summary>
        /// <param name="modeId">模式ID</param>
        /// <returns>模式信息，如果不存在返回null</returns>
        public GameModeInfo GetModeInfo(string modeId)
        {
            return m_availableModes.Find(mode => mode.ModeId == modeId);
        }

        /// <summary>
        /// 获取可用的模式列表
        /// </summary>
        /// <returns>可用的模式列表</returns>
        public List<GameModeInfo> GetAvailableModes()
        {
            List<GameModeInfo> availableModes = new List<GameModeInfo>();

            foreach (GameModeInfo mode in m_availableModes)
            {
                if (mode.CheckAvailability())
                {
                    availableModes.Add(mode);
                }
            }

            // 根据配置排序
            if (m_sortByDisplayOrder)
            {
                availableModes.Sort((a, b) => a.DisplayOrder.CompareTo(b.DisplayOrder));
            }

            return availableModes;
        }

        /// <summary>
        /// 获取推荐的模式列表
        /// </summary>
        /// <returns>推荐的模式列表</returns>
        public List<GameModeInfo> GetRecommendedModes()
        {
            List<GameModeInfo> recommendedModes = new List<GameModeInfo>();

            foreach (GameModeInfo mode in GetAvailableModes())
            {
                if (mode.IsRecommended)
                {
                    recommendedModes.Add(mode);
                }
            }

            return recommendedModes;
        }

        /// <summary>
        /// 获取快速开始模式列表
        /// </summary>
        /// <returns>快速开始模式列表</returns>
        public List<GameModeInfo> GetQuickStartModes()
        {
            if (!m_enableQuickStart)
                return new List<GameModeInfo>();

            List<GameModeInfo> quickStartModes = new List<GameModeInfo>();

            foreach (GameModeInfo mode in GetAvailableModes())
            {
                if (mode.ShowInQuickStart)
                {
                    quickStartModes.Add(mode);
                }
            }

            return quickStartModes;
        }

        /// <summary>
        /// 获取默认模式
        /// </summary>
        /// <returns>默认模式，如果不存在返回第一个可用模式</returns>
        public GameModeInfo GetDefaultMode()
        {
            // 首先尝试获取指定的默认模式
            GameModeInfo defaultMode = GetModeInfo(m_defaultModeId);
            if (defaultMode != null && defaultMode.CheckAvailability())
            {
                return defaultMode;
            }

            // 如果默认模式不可用，返回第一个可用模式
            List<GameModeInfo> availableModes = GetAvailableModes();
            return availableModes.Count > 0 ? availableModes[0] : null;
        }

        /// <summary>
        /// 获取随机模式
        /// </summary>
        /// <returns>随机选择的可用模式</returns>
        public GameModeInfo GetRandomMode()
        {
            if (!m_enableRandomMode)
                return GetDefaultMode();

            List<GameModeInfo> availableModes = GetAvailableModes();
            if (availableModes.Count == 0)
                return null;

            int randomIndex = Random.Range(0, availableModes.Count);
            return availableModes[randomIndex];
        }

        /// <summary>
        /// 添加模式
        /// </summary>
        /// <param name="modeInfo">要添加的模式信息</param>
        public void AddMode(GameModeInfo modeInfo)
        {
            if (modeInfo == null || string.IsNullOrEmpty(modeInfo.ModeId))
                return;

            // 检查是否已存在相同ID的模式
            if (GetModeInfo(modeInfo.ModeId) != null)
            {
                Debug.LogWarning($"Mode with ID '{modeInfo.ModeId}' already exists!");
                return;
            }

            m_availableModes.Add(modeInfo);
        }

        /// <summary>
        /// 移除模式
        /// </summary>
        /// <param name="modeId">要移除的模式ID</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveMode(string modeId)
        {
            GameModeInfo modeToRemove = GetModeInfo(modeId);
            if (modeToRemove != null)
            {
                return m_availableModes.Remove(modeToRemove);
            }

            return false;
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        /// <returns>默认配置的ModeSelectionConfig</returns>
        public static ModeSelectionConfig CreateDefault()
        {
            ModeSelectionConfig config = CreateInstance<ModeSelectionConfig>();
            config.InitializeDefaults();
            return config;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 验证配置参数
        /// </summary>
        private void ValidateConfiguration()
        {
            // 确保基本参数有效
            m_cardSpacing = Mathf.Max(0f, m_cardSpacing);
            m_cardsPerRow = Mathf.Max(1, m_cardsPerRow);
            m_animationDuration = Mathf.Max(0.1f, m_animationDuration);
            m_cardHoverScale = Mathf.Max(1f, m_cardHoverScale);
            m_cardSelectScale = Mathf.Max(0.1f, m_cardSelectScale);

            // 确保颜色alpha值有效
            m_selectedModeColor.a = Mathf.Clamp01(m_selectedModeColor.a);
            m_unavailableModeColor.a = Mathf.Clamp01(m_unavailableModeColor.a);
            m_recommendedModeColor.a = Mathf.Clamp01(m_recommendedModeColor.a);

            // 验证性能参数
            m_maxPoolSize = Mathf.Max(1, m_maxPoolSize);

            // 检查模式ID唯一性
            HashSet<string> modeIds = new HashSet<string>();
            for (int i = m_availableModes.Count - 1; i >= 0; i--)
            {
                GameModeInfo mode = m_availableModes[i];
                if (mode == null || string.IsNullOrEmpty(mode.ModeId))
                {
                    Debug.LogWarning($"Found invalid mode at index {i}, removing...");
                    m_availableModes.RemoveAt(i);
                    continue;
                }

                if (modeIds.Contains(mode.ModeId))
                {
                    Debug.LogWarning($"Duplicate mode ID '{mode.ModeId}' found, removing duplicate...");
                    m_availableModes.RemoveAt(i);
                    continue;
                }

                modeIds.Add(mode.ModeId);
            }
        }

        /// <summary>
        /// 初始化默认值
        /// </summary>
        private void InitializeDefaults()
        {
            // 创建默认模式列表
            m_availableModes = new List<GameModeInfo>
            {
                new GameModeInfo("practice", "mode.practice.title", GameModeType.Practice),
                new GameModeInfo("ai_battle", "mode.ai_battle.title", GameModeType.AIBattle),
                new GameModeInfo("multiplayer", "mode.multiplayer.title", GameModeType.OnlineMultiplayer)
            };

            // 设置默认参数
            m_defaultModeId = "practice";
            m_cardSpacing = 20f;
            m_cardsPerRow = 2;
            m_animationDuration = 0.3f;
            m_enableQuickStart = true;
            m_rememberLastMode = true;
            m_enableRandomMode = true;
        }

        #endregion
    }
}