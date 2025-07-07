using System;
using UnityEngine;
using UnityEngine.UI;
using PongHub.Core;

namespace PongHub.UI.Panels
{
    /// <summary>
    /// 主菜单面板，显示游戏的主要功能入口
    /// </summary>
    public class MainMenuPanel : MenuPanelBase
    {
        [Header("按钮引用")]
        [SerializeField] private Button m_singlePlayerButton;    // 单机模式按钮
        [SerializeField] private Button m_multiplayerButton;     // 多人模式按钮
        [SerializeField] private Button m_settingsButton;        // 设置按钮
        [SerializeField] private Button m_helpButton;            // 帮助按钮
        [SerializeField] private Button m_exitButton;            // 退出按钮

        [Header("玩家信息")]
        [SerializeField] private Text m_playerNameText;          // 玩家名称文本
        [SerializeField] private Text m_playerStatusText;        // 玩家状态文本
        [SerializeField] private Image m_playerAvatar;           // 玩家头像

        [Header("音效")]
        [SerializeField] private AudioClip m_buttonClickSound;   // 按钮点击音效
        [SerializeField] private AudioClip m_panelShowSound;     // 面板显示音效
        [SerializeField] private AudioClip m_panelHideSound;     // 面板隐藏音效

        // 事件
        public event Action OnSinglePlayerSelected;
        public event Action OnMultiplayerSelected;
        public event Action OnSettingsSelected;
        public event Action OnHelpSelected;
        public event Action OnExitSelected;

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
            RegisterButtonEvents();

            // 更新玩家信息
            UpdatePlayerInfo();

            // 设置自定义动画
            SetupCustomAnimation();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // 取消注册按钮事件
            UnregisterButtonEvents();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 更新玩家信息
        /// </summary>
        public void UpdatePlayerInfo()
        {
            // 获取玩家信息
            string playerName = "Player";
            string playerStatus = "Online";

            // TODO: 从玩家数据管理器获取实际信息
            var playerManager = FindObjectOfType<PlayerManager>();
            if (playerManager != null)
            {
                // playerName = playerManager.GetPlayerName();
                // playerStatus = playerManager.GetPlayerStatus();
            }

            // 更新UI
            if (m_playerNameText != null)
            {
                m_playerNameText.text = playerName;
            }

            if (m_playerStatusText != null)
            {
                m_playerStatusText.text = playerStatus;
            }
        }

        #endregion

        #region 保护方法重写

        /// <summary>
        /// 初始化时调用
        /// </summary>
        protected override void OnInitialize()
        {
            m_panelName = "MainMenu";
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
        }

        /// <summary>
        /// 显示动画结束时调用
        /// </summary>
        protected override void OnShowAnimationComplete()
        {
            // 设置按钮可交互
            SetButtonsInteractable(true);

            // 可以在这里添加按钮出现的序列动画
            StartCoroutine(AnimateButtonsSequentially());
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

        #endregion

        #region 私有方法

        /// <summary>
        /// 设置自定义动画
        /// </summary>
        private void SetupCustomAnimation()
        {
            // 设置动画类型为淡入淡出+缩放
            SetAnimationType(PanelAnimationType.FadeAndScale);

            // 设置动画方向为底部
            SetAnimationDirection(PanelAnimationDirection.Bottom);

            // 启用弹性效果
            m_useElasticEffect = true;
            m_elasticOvershoot = 1.2f;

            // 自定义动画时长
            m_showAnimationDuration = 0.4f;
            m_hideAnimationDuration = 0.3f;
        }

        /// <summary>
        /// 注册按钮事件
        /// </summary>
        private void RegisterButtonEvents()
        {
            if (m_singlePlayerButton != null)
            {
                m_singlePlayerButton.onClick.AddListener(OnSinglePlayerButtonClicked);
            }

            if (m_multiplayerButton != null)
            {
                m_multiplayerButton.onClick.AddListener(OnMultiplayerButtonClicked);
            }

            if (m_settingsButton != null)
            {
                m_settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            }

            if (m_helpButton != null)
            {
                m_helpButton.onClick.AddListener(OnHelpButtonClicked);
            }

            if (m_exitButton != null)
            {
                m_exitButton.onClick.AddListener(OnExitButtonClicked);
            }
        }

        /// <summary>
        /// 取消注册按钮事件
        /// </summary>
        private void UnregisterButtonEvents()
        {
            if (m_singlePlayerButton != null)
            {
                m_singlePlayerButton.onClick.RemoveListener(OnSinglePlayerButtonClicked);
            }

            if (m_multiplayerButton != null)
            {
                m_multiplayerButton.onClick.RemoveListener(OnMultiplayerButtonClicked);
            }

            if (m_settingsButton != null)
            {
                m_settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
            }

            if (m_helpButton != null)
            {
                m_helpButton.onClick.RemoveListener(OnHelpButtonClicked);
            }

            if (m_exitButton != null)
            {
                m_exitButton.onClick.RemoveListener(OnExitButtonClicked);
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
            if (m_singlePlayerButton != null) m_singlePlayerButton.interactable = interactable;
            if (m_multiplayerButton != null) m_multiplayerButton.interactable = interactable;
            if (m_settingsButton != null) m_settingsButton.interactable = interactable;
            if (m_helpButton != null) m_helpButton.interactable = interactable;
            if (m_exitButton != null) m_exitButton.interactable = interactable;
        }

        /// <summary>
        /// 按顺序动画显示按钮
        /// </summary>
        private System.Collections.IEnumerator AnimateButtonsSequentially()
        {
            // 创建按钮数组
            Button[] buttons = new Button[]
            {
                m_singlePlayerButton,
                m_multiplayerButton,
                m_settingsButton,
                m_helpButton,
                m_exitButton
            };

            // 按顺序为每个按钮添加动画
            foreach (Button button in buttons)
            {
                if (button != null)
                {
                    // 确保按钮有RectTransform组件
                    RectTransform buttonRect = button.GetComponent<RectTransform>();
                    if (buttonRect != null)
                    {
                        // 保存原始缩放
                        Vector3 originalScale = buttonRect.localScale;

                        // 设置初始缩放
                        buttonRect.localScale = originalScale * 0.5f;

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
                                buttonRect.localScale = Vector3.Lerp(originalScale * 0.5f, originalScale, smoothT) * (1 + bounce);
                            }
                            else
                            {
                                buttonRect.localScale = Vector3.Lerp(originalScale * 0.5f, originalScale, smoothT);
                            }

                            elapsed += Time.deltaTime;
                            yield return null;
                        }

                        // 确保最终缩放正确
                        buttonRect.localScale = originalScale;
                    }

                    // 等待短暂时间后继续下一个按钮
                    yield return new WaitForSeconds(0.05f);
                }
            }
        }

        #endregion

        #region 按钮事件处理

        /// <summary>
        /// 单机模式按钮点击
        /// </summary>
        private void OnSinglePlayerButtonClicked()
        {
            PlaySound(m_buttonClickSound);

            // 调用事件
            OnSinglePlayerSelected?.Invoke();

            // 如果有主菜单控制器，则调用相应方法
            if (m_menuController != null)
            {
                m_menuController.ShowGameModePanel();
            }
        }

        /// <summary>
        /// 多人模式按钮点击
        /// </summary>
        private void OnMultiplayerButtonClicked()
        {
            PlaySound(m_buttonClickSound);

            // 调用事件
            OnMultiplayerSelected?.Invoke();

            // 如果有主菜单控制器，则调用相应方法
            if (m_menuController != null)
            {
                m_menuController.ShowGameModePanel();
            }
        }

        /// <summary>
        /// 设置按钮点击
        /// </summary>
        private void OnSettingsButtonClicked()
        {
            PlaySound(m_buttonClickSound);

            // 调用事件
            OnSettingsSelected?.Invoke();

            // 如果有主菜单控制器，则调用相应方法
            if (m_menuController != null)
            {
                m_menuController.ShowSettingsPanel();
            }
        }

        /// <summary>
        /// 帮助按钮点击
        /// </summary>
        private void OnHelpButtonClicked()
        {
            PlaySound(m_buttonClickSound);

            // 调用事件
            OnHelpSelected?.Invoke();

            // TODO: 显示帮助面板
        }

        /// <summary>
        /// 退出按钮点击
        /// </summary>
        private void OnExitButtonClicked()
        {
            PlaySound(m_buttonClickSound);

            // 调用事件
            OnExitSelected?.Invoke();

            // 如果有主菜单控制器，则调用相应方法
            if (m_menuController != null)
            {
                m_menuController.ShowExitConfirmPanel();
            }
        }

        #endregion
    }
}