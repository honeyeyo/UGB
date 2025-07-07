using System;
using UnityEngine;
using UnityEngine.UI;
using PongHub.Core;

namespace PongHub.UI.Panels
{
    /// <summary>
    /// 游戏模式选择面板，提供单机和多人游戏模式选择
    /// </summary>
    public class GameModePanel : MenuPanelBase
    {
        [Header("单机模式")]
        [SerializeField] private Button m_practiceButton;        // 练习模式按钮
        [SerializeField] private Button m_aiGameButton;          // AI对战按钮
        [SerializeField] private Slider m_aiDifficultySlider;    // AI难度滑块

        [Header("多人模式")]
        [SerializeField] private Button m_createRoomButton;      // 创建房间按钮
        [SerializeField] private Button m_joinRoomButton;        // 加入房间按钮
        [SerializeField] private Button m_friendsListButton;     // 好友列表按钮

        [Header("控制按钮")]
        [SerializeField] private Button m_backButton;            // 返回按钮

        [Header("音效")]
        [SerializeField] private AudioClip m_buttonClickSound;   // 按钮点击音效
        [SerializeField] private AudioClip m_sliderChangeSound;  // 滑块变化音效
        [SerializeField] private AudioClip m_panelShowSound;     // 面板显示音效
        [SerializeField] private AudioClip m_panelHideSound;     // 面板隐藏音效

        [Header("分组容器")]
        [SerializeField] private RectTransform m_singlePlayerGroup; // 单机模式组
        [SerializeField] private RectTransform m_multiplayerGroup;  // 多人模式组
        [SerializeField] private RectTransform m_controlsGroup;     // 控制按钮组

        // 事件
        public event Action OnPracticeSelected;
        public event Action<float> OnAIGameSelected;
        public event Action OnCreateRoomSelected;
        public event Action OnJoinRoomSelected;
        public event Action OnFriendsListSelected;

        // 音频源
        private AudioSource m_audioSource;

        // 主菜单控制器引用
        private MainMenuController m_menuController;

        #region Unity生命周期

        protected override void Awake()
        {
            base.Awake();

            // 获取音频源
            m_audioSource = GetComponent<AudioSource>();
            if (m_audioSource == null)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }

            // 查找主菜单控制器
            m_menuController = FindObjectOfType<MainMenuController>();
        }

        protected override void Start()
        {
            base.Start();

            // 注册按钮事件
            RegisterEvents();

            // 设置自定义动画
            SetupCustomAnimation();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // 取消注册按钮事件
            UnregisterEvents();
        }

        #endregion

        #region 保护方法重写

        /// <summary>
        /// 初始化时调用
        /// </summary>
        protected override void OnInitialize()
        {
            m_panelName = "GameMode";
        }

        /// <summary>
        /// 显示动画开始时调用
        /// </summary>
        protected override void OnShowAnimationStart()
        {
            // 播放显示音效
            PlaySound(m_panelShowSound);

            // 设置按钮初始状态
            SetButtonsInteractable(false);

            // 隐藏分组
            if (m_singlePlayerGroup != null) m_singlePlayerGroup.gameObject.SetActive(false);
            if (m_multiplayerGroup != null) m_multiplayerGroup.gameObject.SetActive(false);
            if (m_controlsGroup != null) m_controlsGroup.gameObject.SetActive(false);
        }

        /// <summary>
        /// 显示动画结束时调用
        /// </summary>
        protected override void OnShowAnimationComplete()
        {
            // 设置按钮可交互
            SetButtonsInteractable(true);

            // 显示分组并添加动画
            StartCoroutine(AnimateGroupsSequentially());
        }

        /// <summary>
        /// 隐藏动画开始时调用
        /// </summary>
        protected override void OnHideAnimationStart()
        {
            // 播放隐藏音效
            PlaySound(m_panelHideSound);

            // 设置按钮不可交互
            SetButtonsInteractable(false);
        }

        /// <summary>
        /// 返回按钮点击时调用
        /// </summary>
        protected override void OnBackButtonClicked()
        {
            PlaySound(m_buttonClickSound);

            // 返回主菜单
            if (m_menuController != null)
            {
                m_menuController.ShowMainMenuPanel();
            }
            else
            {
                Hide();
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 设置自定义动画
        /// </summary>
        private void SetupCustomAnimation()
        {
            // 设置动画类型为淡入淡出+滑动
            SetAnimationType(PanelAnimationType.FadeAndSlide);

            // 设置动画方向为右侧
            SetAnimationDirection(PanelAnimationDirection.Right);

            // 启用弹性效果
            m_useElasticEffect = true;
            m_elasticOvershoot = 1.1f;

            // 自定义动画时长
            m_showAnimationDuration = 0.4f;
            m_hideAnimationDuration = 0.3f;
        }

        /// <summary>
        /// 注册事件
        /// </summary>
        private void RegisterEvents()
        {
            // 单机模式按钮
            if (m_practiceButton != null)
            {
                m_practiceButton.onClick.AddListener(OnPracticeButtonClicked);
            }

            if (m_aiGameButton != null)
            {
                m_aiGameButton.onClick.AddListener(OnAIGameButtonClicked);
            }

            // 多人模式按钮
            if (m_createRoomButton != null)
            {
                m_createRoomButton.onClick.AddListener(OnCreateRoomButtonClicked);
            }

            if (m_joinRoomButton != null)
            {
                m_joinRoomButton.onClick.AddListener(OnJoinRoomButtonClicked);
            }

            if (m_friendsListButton != null)
            {
                m_friendsListButton.onClick.AddListener(OnFriendsListButtonClicked);
            }

            // AI难度滑块
            if (m_aiDifficultySlider != null)
            {
                m_aiDifficultySlider.onValueChanged.AddListener(OnAIDifficultyChanged);
            }

            // 返回按钮
            if (m_backButton != null)
            {
                m_backButton.onClick.AddListener(OnBackButtonClicked);
            }
        }

        /// <summary>
        /// 取消注册事件
        /// </summary>
        private void UnregisterEvents()
        {
            // 单机模式按钮
            if (m_practiceButton != null)
            {
                m_practiceButton.onClick.RemoveListener(OnPracticeButtonClicked);
            }

            if (m_aiGameButton != null)
            {
                m_aiGameButton.onClick.RemoveListener(OnAIGameButtonClicked);
            }

            // 多人模式按钮
            if (m_createRoomButton != null)
            {
                m_createRoomButton.onClick.RemoveListener(OnCreateRoomButtonClicked);
            }

            if (m_joinRoomButton != null)
            {
                m_joinRoomButton.onClick.RemoveListener(OnJoinRoomButtonClicked);
            }

            if (m_friendsListButton != null)
            {
                m_friendsListButton.onClick.RemoveListener(OnFriendsListButtonClicked);
            }

            // AI难度滑块
            if (m_aiDifficultySlider != null)
            {
                m_aiDifficultySlider.onValueChanged.RemoveListener(OnAIDifficultyChanged);
            }

            // 返回按钮
            if (m_backButton != null)
            {
                m_backButton.onClick.RemoveListener(OnBackButtonClicked);
            }
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (clip != null && m_audioSource != null)
            {
                m_audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// 设置所有按钮的可交互状态
        /// </summary>
        private void SetButtonsInteractable(bool interactable)
        {
            // 单机模式按钮
            if (m_practiceButton != null) m_practiceButton.interactable = interactable;
            if (m_aiGameButton != null) m_aiGameButton.interactable = interactable;
            if (m_aiDifficultySlider != null) m_aiDifficultySlider.interactable = interactable;

            // 多人模式按钮
            if (m_createRoomButton != null) m_createRoomButton.interactable = interactable;
            if (m_joinRoomButton != null) m_joinRoomButton.interactable = interactable;
            if (m_friendsListButton != null) m_friendsListButton.interactable = interactable;

            // 控制按钮
            if (m_backButton != null) m_backButton.interactable = interactable;
        }

        /// <summary>
        /// 按顺序动画显示分组
        /// </summary>
        private System.Collections.IEnumerator AnimateGroupsSequentially()
        {
            // 创建分组数组
            RectTransform[] groups = new RectTransform[]
            {
                m_controlsGroup,
                m_singlePlayerGroup,
                m_multiplayerGroup
            };

            // 按顺序为每个分组添加动画
            foreach (RectTransform group in groups)
            {
                if (group != null)
                {
                    // 激活组
                    group.gameObject.SetActive(true);

                    // 保存原始位置和缩放
                    Vector3 originalPosition = group.localPosition;
                    Vector3 originalScale = group.localScale;

                    // 设置初始状态
                    group.localPosition = originalPosition + new Vector3(50, 0, 0);
                    group.localScale = originalScale * 0.9f;

                    // 设置初始透明度
                    CanvasGroup canvasGroup = group.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = group.gameObject.AddComponent<CanvasGroup>();
                    }
                    canvasGroup.alpha = 0;

                    // 执行动画
                    float duration = 0.3f;
                    float elapsed = 0f;

                    while (elapsed < duration)
                    {
                        float t = elapsed / duration;
                        float smoothT = Mathf.SmoothStep(0, 1, t);

                        // 更新位置、缩放和透明度
                        group.localPosition = Vector3.Lerp(
                            originalPosition + new Vector3(50, 0, 0),
                            originalPosition,
                            smoothT
                        );

                        group.localScale = Vector3.Lerp(
                            originalScale * 0.9f,
                            originalScale,
                            smoothT
                        );

                        canvasGroup.alpha = smoothT;

                        elapsed += Time.deltaTime;
                        yield return null;
                    }

                    // 确保最终状态正确
                    group.localPosition = originalPosition;
                    group.localScale = originalScale;
                    canvasGroup.alpha = 1;

                    // 等待短暂时间后继续下一个分组
                    yield return new WaitForSeconds(0.1f);
                }
            }

            // 动画完成后，为按钮添加动画
            yield return StartCoroutine(AnimateButtonsInGroups());
        }

        /// <summary>
        /// 为分组中的按钮添加动画
        /// </summary>
        private System.Collections.IEnumerator AnimateButtonsInGroups()
        {
            // 按钮分组
            Button[][] buttonGroups = new Button[][]
            {
                new Button[] { m_backButton },
                new Button[] { m_practiceButton, m_aiGameButton },
                new Button[] { m_createRoomButton, m_joinRoomButton, m_friendsListButton }
            };

            // 为每个分组中的按钮添加动画
            foreach (Button[] buttons in buttonGroups)
            {
                foreach (Button button in buttons)
                {
                    if (button != null)
                    {
                        // 执行按钮动画
                        RectTransform buttonRect = button.GetComponent<RectTransform>();
                        if (buttonRect != null)
                        {
                            // 保存原始缩放
                            Vector3 originalScale = buttonRect.localScale;

                            // 设置初始缩放
                            buttonRect.localScale = originalScale * 0.8f;

                            // 执行缩放动画
                            float duration = 0.2f;
                            float elapsed = 0f;

                            while (elapsed < duration)
                            {
                                float t = elapsed / duration;
                                float smoothT = Mathf.SmoothStep(0, 1, t);

                                // 添加弹性效果
                                if (t > 0.7f)
                                {
                                    float bounce = Mathf.Sin((t - 0.7f) * 3 * Mathf.PI) * 0.1f * (1 - t);
                                    buttonRect.localScale = Vector3.Lerp(originalScale * 0.8f, originalScale, smoothT) * (1 + bounce);
                                }
                                else
                                {
                                    buttonRect.localScale = Vector3.Lerp(originalScale * 0.8f, originalScale, smoothT);
                                }

                                elapsed += Time.deltaTime;
                                yield return null;
                            }

                            // 确保最终缩放正确
                            buttonRect.localScale = originalScale;
                        }

                        // 播放轻微的点击音效
                        PlaySound(m_buttonClickSound, 0.1f);

                        // 等待短暂时间后继续下一个按钮
                        yield return new WaitForSeconds(0.05f);
                    }
                }

                // 等待短暂时间后继续下一组按钮
                yield return new WaitForSeconds(0.1f);
            }
        }

        /// <summary>
        /// 播放音效（带音量控制）
        /// </summary>
        private void PlaySound(AudioClip clip, float volumeScale = 1.0f)
        {
            if (clip != null && m_audioSource != null)
            {
                m_audioSource.PlayOneShot(clip, volumeScale);
            }
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 练习模式按钮点击
        /// </summary>
        private void OnPracticeButtonClicked()
        {
            PlaySound(m_buttonClickSound);

            // 调用事件
            OnPracticeSelected?.Invoke();

            // 切换到练习模式
            if (m_menuController != null)
            {
                m_menuController.SwitchToLocalMode();
            }
        }

        /// <summary>
        /// AI对战按钮点击
        /// </summary>
        private void OnAIGameButtonClicked()
        {
            PlaySound(m_buttonClickSound);

            // 获取AI难度
            float aiDifficulty = 0.5f;
            if (m_aiDifficultySlider != null)
            {
                aiDifficulty = m_aiDifficultySlider.value;
            }

            // 调用事件
            OnAIGameSelected?.Invoke(aiDifficulty);

            // 切换到AI对战模式
            if (m_menuController != null)
            {
                // TODO: 设置AI难度
                m_menuController.SwitchToLocalMode();
            }
        }

        /// <summary>
        /// 创建房间按钮点击
        /// </summary>
        private void OnCreateRoomButtonClicked()
        {
            PlaySound(m_buttonClickSound);

            // 调用事件
            OnCreateRoomSelected?.Invoke();

            // 切换到网络模式并创建房间
            if (m_menuController != null)
            {
                m_menuController.SwitchToNetworkMode();
            }
        }

        /// <summary>
        /// 加入房间按钮点击
        /// </summary>
        private void OnJoinRoomButtonClicked()
        {
            PlaySound(m_buttonClickSound);

            // 调用事件
            OnJoinRoomSelected?.Invoke();

            // TODO: 显示房间列表或加入房间界面
        }

        /// <summary>
        /// 好友列表按钮点击
        /// </summary>
        private void OnFriendsListButtonClicked()
        {
            PlaySound(m_buttonClickSound);

            // 调用事件
            OnFriendsListSelected?.Invoke();

            // TODO: 显示好友列表
        }

        /// <summary>
        /// AI难度滑块变化
        /// </summary>
        private void OnAIDifficultyChanged(float value)
        {
            PlaySound(m_sliderChangeSound);

            // TODO: 更新AI难度显示
        }

        #endregion
    }
}