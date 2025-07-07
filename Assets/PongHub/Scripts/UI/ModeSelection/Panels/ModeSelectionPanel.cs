using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using PongHub.UI.Core;

namespace PongHub.UI.ModeSelection
{
    /// <summary>
    /// 模式选择主面板
    /// 显示所有可用的游戏模式卡片
    /// </summary>
    public class ModeSelectionPanel : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] private TextMeshProUGUI m_titleText;
        [SerializeField] private Transform m_modeCardsContainer;
        [SerializeField] private GridLayoutGroup m_modeCardsGrid;
        [SerializeField] private ScrollRect m_scrollRect;

        [Header("快捷操作区")]
        [SerializeField] private Button m_quickStartButton;
        [SerializeField] private Button m_randomModeButton;
        [SerializeField] private Button m_lastPlayedButton;
        [SerializeField] private TextMeshProUGUI m_quickStartText;
        [SerializeField] private TextMeshProUGUI m_randomModeText;
        [SerializeField] private TextMeshProUGUI m_lastPlayedText;

        [Header("推荐模式区")]
        [SerializeField] private GameObject m_recommendedSection;
        [SerializeField] private Transform m_recommendedContainer;
        [SerializeField] private TextMeshProUGUI m_recommendedTitle;

        [Header("分类和筛选")]
        [SerializeField] private TMP_Dropdown m_categoryDropdown;
        [SerializeField] private Toggle m_showUnavailableToggle;
        [SerializeField] private Button m_refreshButton;

        [Header("底部导航")]
        [SerializeField] private Button m_backButton;
        [SerializeField] private Button m_settingsButton;

        [Header("预制件")]
        [SerializeField] private ModeCard m_modeCardPrefab;

        [Header("动画")]
        [SerializeField] private CanvasGroup m_canvasGroup;
        [SerializeField] private float m_showAnimDuration = 0.3f;
        [SerializeField] private AnimationCurve m_showAnimCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("事件")]
        public UnityEvent<GameModeInfo> OnModeCardClicked;
        public UnityEvent OnQuickStartClicked;
        public UnityEvent OnRandomModeClicked;
        public UnityEvent OnBackClicked;
        public UnityEvent OnSettingsClicked;

        // 私有变量
        private ModeSwitchController m_controller;
        private ModeSelectionConfig m_config;
        private PongHub.UI.Localization.LocalizationManager m_localizationManager;

        // 模式卡片管理
        private List<ModeCard> m_activeModeCards = new List<ModeCard>();
        private List<ModeCard> m_cardPool = new List<ModeCard>();
        private GameModeInfo m_selectedMode;

        // 筛选和分类
        private GameModeType m_currentCategory = (GameModeType)(-1); // -1表示显示所有
        private bool m_showUnavailable = false;

        // 动画
        private Coroutine m_showAnimCoroutine;

        #region 属性

        public GameModeInfo SelectedMode => m_selectedMode;
        public bool IsShowing => gameObject.activeInHierarchy;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            // 查找系统引用
            m_localizationManager = FindObjectOfType<PongHub.UI.Localization.LocalizationManager>();

            // 设置初始状态
            if (m_canvasGroup != null)
                m_canvasGroup.alpha = 0f;
        }

        private void Start()
        {
            // 绑定UI事件
            BindUIEvents();

            // 初始化筛选选项
            InitializeCategoryDropdown();
        }

        private void OnDestroy()
        {
            // 解绑事件
            UnbindUIEvents();

            // 清理协程
            if (m_showAnimCoroutine != null)
            {
                StopCoroutine(m_showAnimCoroutine);
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化面板
        /// </summary>
        /// <param name="controller">控制器引用</param>
        /// <param name="config">配置</param>
        public void Initialize(ModeSwitchController controller, ModeSelectionConfig config)
        {
            m_controller = controller;
            m_config = config;

            // 配置网格布局
            ConfigureGridLayout();

            // 更新本地化文本
            UpdateLocalizedTexts();

            Debug.Log("ModeSelectionPanel initialized");
        }

        /// <summary>
        /// 刷新模式列表
        /// </summary>
        public void RefreshModeList()
        {
            if (m_config == null)
                return;

            StartCoroutine(RefreshModeListCoroutine());
        }

        /// <summary>
        /// 显示面板
        /// </summary>
        public void Show()
        {
            if (m_showAnimCoroutine != null)
                StopCoroutine(m_showAnimCoroutine);

            gameObject.SetActive(true);
            m_showAnimCoroutine = StartCoroutine(ShowAnimation());
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        public void Hide()
        {
            if (m_showAnimCoroutine != null)
                StopCoroutine(m_showAnimCoroutine);

            m_showAnimCoroutine = StartCoroutine(HideAnimation());
        }

        /// <summary>
        /// 选择模式
        /// </summary>
        /// <param name="modeInfo">要选择的模式</param>
        public void SelectMode(GameModeInfo modeInfo)
        {
            // 取消之前的选择
            if (m_selectedMode != null)
            {
                ModeCard previousCard = FindModeCard(m_selectedMode);
                if (previousCard != null)
                    previousCard.IsSelected = false;
            }

            // 设置新选择
            m_selectedMode = modeInfo;

            if (modeInfo != null)
            {
                ModeCard currentCard = FindModeCard(modeInfo);
                if (currentCard != null)
                    currentCard.IsSelected = true;
            }

            // 更新快捷按钮状态
            UpdateQuickActionButtons();
        }

        /// <summary>
        /// 设置分类筛选
        /// </summary>
        /// <param name="category">分类类型</param>
        public void SetCategory(GameModeType category)
        {
            m_currentCategory = category;
            RefreshModeList();
        }

        /// <summary>
        /// 设置是否显示不可用模式
        /// </summary>
        /// <param name="show">是否显示</param>
        public void SetShowUnavailable(bool show)
        {
            m_showUnavailable = show;
            RefreshModeList();
        }

        /// <summary>
        /// 滚动到指定模式
        /// </summary>
        /// <param name="modeInfo">要滚动到的模式</param>
        public void ScrollToMode(GameModeInfo modeInfo)
        {
            ModeCard card = FindModeCard(modeInfo);
            if (card != null && m_scrollRect != null)
            {
                StartCoroutine(ScrollToCardCoroutine(card));
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 绑定UI事件
        /// </summary>
        private void BindUIEvents()
        {
            if (m_quickStartButton != null)
                m_quickStartButton.onClick.AddListener(HandleQuickStartClicked);

            if (m_randomModeButton != null)
                m_randomModeButton.onClick.AddListener(HandleRandomModeClicked);

            if (m_lastPlayedButton != null)
                m_lastPlayedButton.onClick.AddListener(HandleLastPlayedClicked);

            if (m_categoryDropdown != null)
                m_categoryDropdown.onValueChanged.AddListener(HandleCategoryChanged);

            if (m_showUnavailableToggle != null)
                m_showUnavailableToggle.onValueChanged.AddListener(HandleShowUnavailableChanged);

            if (m_refreshButton != null)
                m_refreshButton.onClick.AddListener(RefreshModeList);

            if (m_backButton != null)
                m_backButton.onClick.AddListener(HandleBackClicked);

            if (m_settingsButton != null)
                m_settingsButton.onClick.AddListener(HandleSettingsClicked);
        }

        /// <summary>
        /// 解绑UI事件
        /// </summary>
        private void UnbindUIEvents()
        {
            if (m_quickStartButton != null)
                m_quickStartButton.onClick.RemoveListener(HandleQuickStartClicked);

            if (m_randomModeButton != null)
                m_randomModeButton.onClick.RemoveListener(HandleRandomModeClicked);

            if (m_lastPlayedButton != null)
                m_lastPlayedButton.onClick.RemoveListener(HandleLastPlayedClicked);

            if (m_categoryDropdown != null)
                m_categoryDropdown.onValueChanged.RemoveListener(HandleCategoryChanged);

            if (m_showUnavailableToggle != null)
                m_showUnavailableToggle.onValueChanged.RemoveListener(HandleShowUnavailableChanged);

            if (m_refreshButton != null)
                m_refreshButton.onClick.RemoveListener(RefreshModeList);

            if (m_backButton != null)
                m_backButton.onClick.RemoveListener(HandleBackClicked);

            if (m_settingsButton != null)
                m_settingsButton.onClick.RemoveListener(HandleSettingsClicked);
        }

        /// <summary>
        /// 配置网格布局
        /// </summary>
        private void ConfigureGridLayout()
        {
            if (m_modeCardsGrid == null || m_config == null)
                return;

            // 设置网格参数
            m_modeCardsGrid.cellSize = m_config.CardSize;
            m_modeCardsGrid.spacing = new Vector2(m_config.CardSpacing, m_config.CardSpacing);

            // 计算约束数量
            if (m_config.CardsPerRow > 0)
            {
                m_modeCardsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                m_modeCardsGrid.constraintCount = m_config.CardsPerRow;
            }
        }

        /// <summary>
        /// 初始化分类下拉菜单
        /// </summary>
        private void InitializeCategoryDropdown()
        {
            if (m_categoryDropdown == null)
                return;

            m_categoryDropdown.ClearOptions();

            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

            // 添加"所有"选项
            options.Add(new TMP_Dropdown.OptionData(GetLocalizedText("ui.all_modes")));

            // 添加各种模式类型
            foreach (GameModeType modeType in Enum.GetValues(typeof(GameModeType)))
            {
                string optionText = GetLocalizedText($"mode_type.{modeType.ToString().ToLower()}");
                options.Add(new TMP_Dropdown.OptionData(optionText));
            }

            m_categoryDropdown.AddOptions(options);
            m_categoryDropdown.value = 0; // 默认选择"所有"
        }

        /// <summary>
        /// 更新本地化文本
        /// </summary>
        private void UpdateLocalizedTexts()
        {
            if (m_config == null)
                return;

            if (m_titleText != null)
                m_titleText.text = GetLocalizedText(m_config.TitleKey);

            if (m_quickStartText != null)
                m_quickStartText.text = GetLocalizedText(m_config.QuickStartKey);

            if (m_randomModeText != null)
                m_randomModeText.text = GetLocalizedText(m_config.RandomModeKey);

            if (m_lastPlayedText != null)
                m_lastPlayedText.text = GetLocalizedText(m_config.LastPlayedKey);

            if (m_recommendedTitle != null)
                m_recommendedTitle.text = GetLocalizedText(m_config.RecommendedKey);
        }

        /// <summary>
        /// 刷新模式列表协程
        /// </summary>
        private IEnumerator RefreshModeListCoroutine()
        {
            // 清理现有卡片
            ClearModeCards();

            yield return null; // 等待一帧确保清理完成

            // 获取要显示的模式列表
            List<GameModeInfo> modesToShow = GetModesToShow();

            // 创建推荐模式
            CreateRecommendedModes(modesToShow);

            yield return null;

            // 创建普通模式卡片
            CreateModeCards(modesToShow);

            // 更新快捷按钮
            UpdateQuickActionButtons();

            // 更新滚动区域
            UpdateScrollRect();
        }

        /// <summary>
        /// 获取要显示的模式列表
        /// </summary>
        /// <returns>筛选后的模式列表</returns>
        private List<GameModeInfo> GetModesToShow()
        {
            List<GameModeInfo> allModes = m_config.GetAvailableModes();
            List<GameModeInfo> filteredModes = new List<GameModeInfo>();

            foreach (GameModeInfo mode in allModes)
            {
                // 分类筛选
                if (m_currentCategory != (GameModeType)(-1) && mode.ModeType != m_currentCategory)
                    continue;

                // 可用性筛选
                if (!m_showUnavailable && !mode.CheckAvailability())
                    continue;

                filteredModes.Add(mode);
            }

            return filteredModes;
        }

        /// <summary>
        /// 创建推荐模式
        /// </summary>
        /// <param name="modes">模式列表</param>
        private void CreateRecommendedModes(List<GameModeInfo> modes)
        {
            if (m_recommendedSection == null || m_recommendedContainer == null)
                return;

            List<GameModeInfo> recommendedModes = new List<GameModeInfo>();
            foreach (GameModeInfo mode in modes)
            {
                if (mode.IsRecommended)
                    recommendedModes.Add(mode);
            }

            bool hasRecommended = recommendedModes.Count > 0;
            m_recommendedSection.SetActive(hasRecommended);

            if (hasRecommended)
            {
                foreach (GameModeInfo mode in recommendedModes)
                {
                    ModeCard card = GetOrCreateModeCard();
                    card.transform.SetParent(m_recommendedContainer);
                    card.Initialize(mode, m_config);
                    card.OnModeSelected.AddListener(HandleModeCardClicked);
                    m_activeModeCards.Add(card);
                }
            }
        }

        /// <summary>
        /// 创建模式卡片
        /// </summary>
        /// <param name="modes">模式列表</param>
        private void CreateModeCards(List<GameModeInfo> modes)
        {
            foreach (GameModeInfo mode in modes)
            {
                ModeCard card = GetOrCreateModeCard();
                if (card != null)
                {
                    card.Initialize(mode, m_config);
                    card.OnModeSelected.AddListener((selectedMode) => HandleModeCardClicked(selectedMode));
                    m_activeModeCards.Add(card);
                }
            }

            // 更新滚动视图
            UpdateScrollRect();
        }

        /// <summary>
        /// 获取或创建模式卡片
        /// </summary>
        /// <returns>模式卡片实例</returns>
        private ModeCard GetOrCreateModeCard()
        {
            ModeCard card = null;

            // 从对象池获取
            if (m_config.EnableCardPooling && m_cardPool.Count > 0)
            {
                card = m_cardPool[0];
                m_cardPool.RemoveAt(0);
                card.gameObject.SetActive(true);
            }
            else
            {
                // 创建新卡片
                if (m_modeCardPrefab != null)
                {
                    card = Instantiate(m_modeCardPrefab);
                }
            }

            return card;
        }

        /// <summary>
        /// 清理模式卡片
        /// </summary>
        private void ClearModeCards()
        {
            foreach (ModeCard card in m_activeModeCards)
            {
                card.OnModeSelected.RemoveListener(HandleModeCardClicked);

                if (m_config.EnableCardPooling && m_cardPool.Count < m_config.MaxPoolSize)
                {
                    // 放回对象池
                    card.gameObject.SetActive(false);
                    card.transform.SetParent(transform);
                    m_cardPool.Add(card);
                }
                else
                {
                    // 销毁对象
                    if (card != null)
                        Destroy(card.gameObject);
                }
            }

            m_activeModeCards.Clear();
        }

        /// <summary>
        /// 查找模式卡片
        /// </summary>
        /// <param name="modeInfo">模式信息</param>
        /// <returns>对应的卡片，如果没找到返回null</returns>
        private ModeCard FindModeCard(GameModeInfo modeInfo)
        {
            foreach (ModeCard card in m_activeModeCards)
            {
                if (card.ModeInfo == modeInfo)
                    return card;
            }
            return null;
        }

        /// <summary>
        /// 更新快捷操作按钮
        /// </summary>
        private void UpdateQuickActionButtons()
        {
            // 更新快速开始按钮
            if (m_quickStartButton != null)
            {
                bool hasQuickStartModes = m_config.GetQuickStartModes().Count > 0;
                m_quickStartButton.interactable = hasQuickStartModes;
            }

            // 更新随机模式按钮
            if (m_randomModeButton != null)
            {
                bool hasAvailableModes = m_config.GetAvailableModes().Count > 0;
                m_randomModeButton.interactable = hasAvailableModes && m_config.EnableRandomMode;
            }

            // 更新上次游戏按钮
            if (m_lastPlayedButton != null)
            {
                bool hasLastPlayed = m_controller != null && m_controller.LastPlayedMode != null;
                m_lastPlayedButton.interactable = hasLastPlayed;

                if (hasLastPlayed && m_lastPlayedText != null)
                {
                    string lastModeTitle = GetLocalizedText(m_controller.LastPlayedMode.TitleKey);
                    m_lastPlayedText.text = $"{GetLocalizedText(m_config.LastPlayedKey)}: {lastModeTitle}";
                }
            }
        }

        /// <summary>
        /// 更新滚动区域
        /// </summary>
        private void UpdateScrollRect()
        {
            if (m_scrollRect != null)
            {
                // 重置滚动位置到顶部
                m_scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        /// <summary>
        /// 滚动到卡片协程
        /// </summary>
        /// <param name="card">目标卡片</param>
        private IEnumerator ScrollToCardCoroutine(ModeCard card)
        {
            if (m_scrollRect == null)
                yield break;

            yield return null; // 等待布局更新

            RectTransform cardRect = card.GetComponent<RectTransform>();
            RectTransform contentRect = m_scrollRect.content;
            RectTransform viewportRect = m_scrollRect.viewport;

            if (cardRect == null || contentRect == null || viewportRect == null)
                yield break;

            // 计算目标位置
            float cardY = cardRect.anchoredPosition.y;
            float contentHeight = contentRect.rect.height;
            float viewportHeight = viewportRect.rect.height;

            float targetY = Mathf.Abs(cardY) / (contentHeight - viewportHeight);
            targetY = Mathf.Clamp01(targetY);

            // 平滑滚动
            float currentY = m_scrollRect.verticalNormalizedPosition;
            float elapsed = 0f;
            float duration = 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                m_scrollRect.verticalNormalizedPosition = Mathf.Lerp(currentY, 1f - targetY, t);
                yield return null;
            }

            m_scrollRect.verticalNormalizedPosition = 1f - targetY;
        }

        /// <summary>
        /// 显示动画协程
        /// </summary>
        private IEnumerator ShowAnimation()
        {
            if (m_canvasGroup == null)
                yield break;

            float elapsed = 0f;
            float startAlpha = m_canvasGroup.alpha;

            while (elapsed < m_showAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / m_showAnimDuration;
                float curve = m_showAnimCurve.Evaluate(t);
                m_canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, curve);
                yield return null;
            }

            m_canvasGroup.alpha = 1f;
            m_showAnimCoroutine = null;
        }

        /// <summary>
        /// 隐藏动画协程
        /// </summary>
        private IEnumerator HideAnimation()
        {
            if (m_canvasGroup == null)
            {
                gameObject.SetActive(false);
                yield break;
            }

            float elapsed = 0f;
            float startAlpha = m_canvasGroup.alpha;

            while (elapsed < m_showAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / m_showAnimDuration;
                float curve = m_showAnimCurve.Evaluate(1f - t);
                m_canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, 1f - curve);
                yield return null;
            }

            m_canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            m_showAnimCoroutine = null;
        }

        /// <summary>
        /// 获取本地化文本
        /// </summary>
        /// <param name="key">本地化键</param>
        /// <returns>本地化文本</returns>
        private string GetLocalizedText(string key)
        {
            if (m_localizationManager != null)
            {
                return m_localizationManager.GetLocalizedText(key);
            }
            return key; // 回退到键本身
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 处理模式卡片点击
        /// </summary>
        /// <param name="modeInfo">点击的模式</param>
        private void HandleModeCardClicked(GameModeInfo modeInfo)
        {
            if (modeInfo != null)
            {
                SelectMode(modeInfo);
                OnModeCardClicked?.Invoke(modeInfo);
            }
        }

        /// <summary>
        /// 处理快速开始点击
        /// </summary>
        private void HandleQuickStartClicked()
        {
            OnQuickStartClicked?.Invoke();
        }

        /// <summary>
        /// 处理随机模式点击
        /// </summary>
        private void HandleRandomModeClicked()
        {
            OnRandomModeClicked?.Invoke();
        }

        /// <summary>
        /// 处理上次游戏点击
        /// </summary>
        private void HandleLastPlayedClicked()
        {
            if (m_controller != null && m_controller.LastPlayedMode != null)
            {
                HandleModeCardClicked(m_controller.LastPlayedMode);
            }
        }

        /// <summary>
        /// 处理分类改变
        /// </summary>
        /// <param name="index">选择的索引</param>
        private void HandleCategoryChanged(int index)
        {
            if (index == 0)
            {
                // "所有"选项
                SetCategory((GameModeType)(-1));
            }
            else
            {
                // 具体分类
                GameModeType[] modeTypes = (GameModeType[])Enum.GetValues(typeof(GameModeType));
                if (index - 1 < modeTypes.Length)
                {
                    SetCategory(modeTypes[index - 1]);
                }
            }
        }

        /// <summary>
        /// 处理显示不可用模式切换
        /// </summary>
        /// <param name="show">是否显示</param>
        private void HandleShowUnavailableChanged(bool show)
        {
            SetShowUnavailable(show);
        }

        /// <summary>
        /// 处理返回按钮点击
        /// </summary>
        private void HandleBackClicked()
        {
            OnBackClicked?.Invoke();
        }

        /// <summary>
        /// 处理设置按钮点击
        /// </summary>
        private void HandleSettingsClicked()
        {
            OnSettingsClicked?.Invoke();
        }

        #endregion
    }
}