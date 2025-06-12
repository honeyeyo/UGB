// Copyright (c) MagnusLab Inc. and affiliates.

using System.Collections;
using PongHub.App;
using UnityEngine;

namespace PongHub.Arena.Player.Menu
{
    /// <summary>
    /// 游戏内玩家菜单类，根据选中的标签页显示不同的菜单内容。
    /// 当应用程序失去焦点时（如用户打开系统菜单），菜单会自动隐藏，
    /// 并在重新获得焦点时重新显示。
    /// </summary>
    public class PlayerInGameMenu : MonoBehaviour
    {
        /// <summary>
        /// 菜单视图类型枚举
        /// </summary>
        private enum ViewType
        {
            Settings,    // 设置视图
            Info,       // 调试信息视图
            Players,    // 玩家列表视图
        }

        // 更新频率常量（秒）
        private const float UPDATE_FREQUENCY = 0.25f;

        [Header("基础组件")]
        [SerializeField] private GameObject m_menuRoot;           // 菜单根节点
        [SerializeField] private Color m_tabSelectedColor;       // 标签选中颜色
        [SerializeField] private Color m_tabUnselectedColor;     // 标签未选中颜色
        [SerializeField] private Collider m_canvasCollider;      // 画布碰撞体
        [SerializeField] private float m_closingSqrMagnitude = 9;// 关闭菜单的距离阈值（平方）

        [Header("视图组件")]
        [SerializeField] private BasePlayerMenuView m_settingsView;   // 设置视图
        [SerializeField] private BasePlayerMenuView m_debugInfoView;  // 调试信息视图
        [SerializeField] private BasePlayerMenuView m_playersView;    // 玩家列表视图

        [Header("标签按钮")]
        [SerializeField] private TabButton m_tabSettingButton;    // 设置标签按钮
        [SerializeField] private TabButton m_tabDebugInfoButton;  // 调试信息标签按钮
        [SerializeField] private TabButton m_tabPlayersButton;    // 玩家列表标签按钮

        private Transform m_cameraTransform;      // 相机变换组件引用
        private float m_updateTimer;             // 更新计时器
        private BasePlayerMenuView m_currentView;// 当前显示的视图

        /// <summary>
        /// 初始化组件和设置
        /// </summary>
        private void Awake()
        {
            // 获取主相机引用
            if (Camera.main != null)
            {
                m_cameraTransform = Camera.main.transform;
            }

            // 设置标签按钮颜色
            m_tabSettingButton.Setup(m_tabSelectedColor, m_tabUnselectedColor);
            m_tabDebugInfoButton.Setup(m_tabSelectedColor, m_tabUnselectedColor);
            m_tabPlayersButton.Setup(m_tabSelectedColor, m_tabUnselectedColor);

            // 初始化显示设置视图
            HideAllViews();
            ShowView(ViewType.Settings);
        }

        /// <summary>
        /// 当应用程序焦点状态改变时调用
        /// </summary>
        /// <param name="focusStatus">是否获得焦点</param>
        private void OnApplicationFocus(bool focusStatus)
        {
            m_menuRoot.SetActive(focusStatus);
        }

        /// <summary>
        /// 切换菜单显示状态
        /// </summary>
        public void Toggle()
        {
            if (gameObject.activeSelf)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// 显示菜单
        /// </summary>
        public void Show()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                m_menuRoot.SetActive(true);

                // 将菜单放置在玩家前方2单位处
                var thisTrans = transform;
                var forward = m_cameraTransform.forward;
                forward.y = 0;
                thisTrans.position = m_cameraTransform.position + forward * 2f;
            }
        }

        /// <summary>
        /// 隐藏菜单
        /// </summary>
        public void Hide()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        private void Update()
        {
            // 定时更新当前视图
            m_updateTimer += Time.deltaTime;
            if (m_updateTimer >= UPDATE_FREQUENCY)
            {
                m_updateTimer = UPDATE_FREQUENCY - m_updateTimer;
                m_currentView.OnUpdate();
            }

            // 检查与相机的距离，超过阈值则关闭菜单
            if ((m_cameraTransform.position - transform.position).sqrMagnitude > m_closingSqrMagnitude)
            {
                Hide();
            }
        }

        /// <summary>
        /// 设置标签点击回调
        /// </summary>
        public void OnSettingsClicked()
        {
            ShowView(ViewType.Settings);
        }

        /// <summary>
        /// 调试信息标签点击回调
        /// </summary>
        public void OnDebugInfoClicked()
        {
            ShowView(ViewType.Info);
        }

        /// <summary>
        /// 玩家列表标签点击回调
        /// </summary>
        public void OnPlayersTabClicked()
        {
            ShowView(ViewType.Players);
        }

        /// <summary>
        /// 退出按钮点击回调
        /// </summary>
        public void OnQuitButtonClicked()
        {
            UGBApplication.Instance.NavigationController.GoToMainMenu();
            _ = StartCoroutine(Disable());
        }

        /// <summary>
        /// 显示指定类型的视图
        /// </summary>
        /// <param name="viewType">要显示的视图类型</param>
        private void ShowView(ViewType viewType)
        {
            // 隐藏当前视图
            if (m_currentView)
            {
                m_currentView.gameObject.SetActive(false);
            }

            // 根据类型切换视图
            switch (viewType)
            {
                case ViewType.Info:
                    m_currentView = m_debugInfoView;
                    break;
                case ViewType.Settings:
                    m_currentView = m_settingsView;
                    break;
                case ViewType.Players:
                    m_currentView = m_playersView;
                    break;
            }

            // 显示新视图并更新标签状态
            m_currentView.gameObject.SetActive(true);
            UpdateTabs(viewType);
        }

        /// <summary>
        /// 隐藏所有视图
        /// </summary>
        private void HideAllViews()
        {
            m_debugInfoView.gameObject.SetActive(false);
            m_settingsView.gameObject.SetActive(false);
            m_playersView.gameObject.SetActive(false);
        }

        /// <summary>
        /// 禁用画布碰撞体的协程
        /// </summary>
        private IEnumerator Disable()
        {
            yield return new WaitForEndOfFrame();
            m_canvasCollider.enabled = false;
        }

        /// <summary>
        /// 更新标签按钮状态
        /// </summary>
        /// <param name="selectedView">当前选中的视图类型</param>
        private void UpdateTabs(ViewType selectedView)
        {
            // 更新设置标签状态
            if (selectedView == ViewType.Settings)
            {
                m_tabSettingButton.OnSelected();
            }
            else
            {
                m_tabSettingButton.OnDeselected();
            }

            // 更新调试信息标签状态
            if (selectedView == ViewType.Info)
            {
                m_tabDebugInfoButton.OnSelected();
            }
            else
            {
                m_tabDebugInfoButton.OnDeselected();
            }

            // 更新玩家列表标签状态
            if (selectedView == ViewType.Players)
            {
                m_tabPlayersButton.OnSelected();
            }
            else
            {
                m_tabPlayersButton.OnDeselected();
            }
        }

        /// <summary>
        /// GUI绘制回调，用于显示退出按钮
        /// </summary>
        private void OnGUI()
        {
            if (GUILayout.Button("Quit"))
            {
                OnQuitButtonClicked();
            }
        }
    }
}