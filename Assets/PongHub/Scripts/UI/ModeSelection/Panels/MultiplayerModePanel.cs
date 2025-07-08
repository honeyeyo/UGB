using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using PongHub.UI.Core;
using PongHub.UI.Localization;
using System.Linq;
using PongHub.Core.Audio;

namespace PongHub.UI.ModeSelection
{
    /// <summary>
    /// 多人模式面板
    /// 提供房间创建、房间浏览和好友列表功能
    /// </summary>
    public class MultiplayerModePanel : MonoBehaviour
    {
        [Header("面板配置")]
        [SerializeField] private GameObject m_panelRoot;
        [SerializeField] private TextMeshProUGUI m_titleText;
        [SerializeField] private Transform m_mainContainer;
        [SerializeField] private Button m_backButton;

        [Header("房间创建界面")]
        [SerializeField] private GameObject m_createRoomPanel;
        [SerializeField] private TMP_InputField m_roomNameInput;
        [SerializeField] private TMP_InputField m_playerNameInput;
        [SerializeField] private Toggle m_privateRoomToggle;
        [SerializeField] private TMP_Dropdown m_maxPlayersDropdown;
        [SerializeField] private TMP_Dropdown m_difficultyDropdown;
        [SerializeField] private Button m_createRoomButton;
        [SerializeField] private Button m_cancelCreateButton;

        [Header("房间浏览界面")]
        [SerializeField] private GameObject m_roomBrowserPanel;
        [SerializeField] private Transform m_roomListContainer;
        [SerializeField] private GameObject m_roomItemPrefab;
        [SerializeField] private Button m_refreshRoomsButton;
        [SerializeField] private TMP_InputField m_roomSearchInput;
        [SerializeField] private TextMeshProUGUI m_roomCountText;
        [SerializeField] private ScrollRect m_roomScrollView;

        [Header("好友列表界面")]
        [SerializeField] private GameObject m_friendsPanel;
        [SerializeField] private Transform m_friendsListContainer;
        [SerializeField] private GameObject m_friendItemPrefab;
        [SerializeField] private Button m_refreshFriendsButton;
        [SerializeField] private TMP_InputField m_friendSearchInput;
        [SerializeField] private TextMeshProUGUI m_friendCountText;
        [SerializeField] private ScrollRect m_friendScrollView;

        [Header("主菜单按钮")]
        [SerializeField] private Button m_createRoomMenuButton;
        [SerializeField] private Button m_joinRoomMenuButton;
        [SerializeField] private Button m_friendsMenuButton;
        [SerializeField] private Button m_quickMatchButton;

        [Header("状态显示")]
        [SerializeField] private GameObject m_connectionStatusPanel;
        [SerializeField] private TextMeshProUGUI m_connectionStatusText;
        [SerializeField] private Image m_connectionStatusIcon;
        [SerializeField] private Color m_connectedColor = Color.green;
        [SerializeField] private Color m_disconnectedColor = Color.red;

        [Header("本地化键")]
        [SerializeField] private string m_titleKey = "multiplayer.title";
        [SerializeField] private string m_createRoomKey = "multiplayer.create_room";
        [SerializeField] private string m_joinRoomKey = "multiplayer.join_room";
        [SerializeField] private string m_friendsKey = "multiplayer.friends";
        [SerializeField] private string m_quickMatchKey = "multiplayer.quick_match";

        // 多人模式类型
        public enum MultiplayerModeType
        {
            CreateRoom,     // 创建房间
            JoinRoom,       // 加入房间
            Friends,        // 好友列表
            QuickMatch      // 快速匹配
        }

        // 房间信息
        [System.Serializable]
        public class RoomInfo
        {
            public string roomId;
            public string roomName;
            public string hostName;
            public int currentPlayers;
            public int maxPlayers;
            public bool isPrivate;
            public string difficulty;
            public int ping;
            public bool isPasswordProtected;
        }

        // 好友信息
        [System.Serializable]
        public class FriendInfo
        {
            public string friendId;
            public string friendName;
            public string status;  // Online, Offline, InGame
            public string currentRoom;
            public System.DateTime lastSeen;
        }

        // 事件定义
        public System.Action<MultiplayerModeType> OnModeSelected;
        public System.Action<RoomInfo> OnRoomSelected;
        public System.Action<FriendInfo> OnFriendSelected;
        public System.Action<string> OnRoomCreated;
        public System.Action OnBackPressed;

        private List<RoomInfo> m_availableRooms = new List<RoomInfo>();
        private List<FriendInfo> m_friendsList = new List<FriendInfo>();
        private List<GameObject> m_roomItems = new List<GameObject>();
        private List<GameObject> m_friendItems = new List<GameObject>();

        private LocalizationManager m_localizationManager;

        private bool m_isInitialized = false;
        private bool m_isConnected = false;
        private MultiplayerModeType m_selectedMode = MultiplayerModeType.CreateRoom;
        private GameObject m_joinRoomPanel;
        public System.Action OnBack;

        /// <summary>
        /// 初始化多人模式面板
        /// </summary>
        private void Start()
        {
            InitializeComponents();
            SetupUI();
            CheckConnectionStatus();
        }

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitializeComponents()
        {
            if (m_isInitialized) return;

            // 获取管理器引用
            m_localizationManager = FindObjectOfType<LocalizationManager>();

            m_isInitialized = true;
        }

        /// <summary>
        /// 设置UI界面
        /// </summary>
        private void SetupUI()
        {
            // 设置标题
            UpdateTitle();

            // 设置下拉框选项
            SetupDropdowns();

            // 设置事件监听
            SetupEventListeners();

            // 初始化面板状态
            ShowMainMenu();
        }

        /// <summary>
        /// 更新标题文本
        /// </summary>
        private void UpdateTitle()
        {
            if (m_titleText != null && m_localizationManager != null)
            {
                m_titleText.text = m_localizationManager.GetLocalizedText(m_titleKey);
            }
        }

        /// <summary>
        /// 设置下拉框选项
        /// </summary>
        private void SetupDropdowns()
        {
            // 设置最大玩家数下拉框
            if (m_maxPlayersDropdown != null)
            {
                m_maxPlayersDropdown.options.Clear();
                m_maxPlayersDropdown.options.Add(new TMP_Dropdown.OptionData("2"));
                m_maxPlayersDropdown.options.Add(new TMP_Dropdown.OptionData("4"));
                m_maxPlayersDropdown.options.Add(new TMP_Dropdown.OptionData("6"));
                m_maxPlayersDropdown.options.Add(new TMP_Dropdown.OptionData("8"));
                m_maxPlayersDropdown.value = 0; // 默认2人
            }

            // 设置难度下拉框
            if (m_difficultyDropdown != null)
            {
                m_difficultyDropdown.options.Clear();
                m_difficultyDropdown.options.Add(new TMP_Dropdown.OptionData(GetLocalizedText("difficulty.easy")));
                m_difficultyDropdown.options.Add(new TMP_Dropdown.OptionData(GetLocalizedText("difficulty.medium")));
                m_difficultyDropdown.options.Add(new TMP_Dropdown.OptionData(GetLocalizedText("difficulty.hard")));
                m_difficultyDropdown.value = 1; // 默认中等难度
            }
        }

        /// <summary>
        /// 设置事件监听器
        /// </summary>
        private void SetupEventListeners()
        {
            // 主菜单按钮
            if (m_createRoomMenuButton != null)
            {
                m_createRoomMenuButton.onClick.AddListener(() => ShowCreateRoomPanel());
            }

            if (m_joinRoomMenuButton != null)
            {
                m_joinRoomMenuButton.onClick.AddListener(() => ShowRoomBrowserPanel());
            }

            if (m_friendsMenuButton != null)
            {
                m_friendsMenuButton.onClick.AddListener(() => ShowFriendsPanel());
            }

            if (m_quickMatchButton != null)
            {
                m_quickMatchButton.onClick.AddListener(() => StartQuickMatch());
            }

            // 创建房间按钮
            if (m_createRoomButton != null)
            {
                m_createRoomButton.onClick.AddListener(() => CreateRoom());
            }

            if (m_cancelCreateButton != null)
            {
                m_cancelCreateButton.onClick.AddListener(() => ShowMainMenu());
            }

            // 刷新按钮
            if (m_refreshRoomsButton != null)
            {
                m_refreshRoomsButton.onClick.AddListener(() => RefreshRoomList());
            }

            if (m_refreshFriendsButton != null)
            {
                m_refreshFriendsButton.onClick.AddListener(() => RefreshFriendsList());
            }

            // 搜索输入框
            if (m_roomSearchInput != null)
            {
                m_roomSearchInput.onValueChanged.AddListener(OnRoomSearchChanged);
            }

            if (m_friendSearchInput != null)
            {
                m_friendSearchInput.onValueChanged.AddListener(OnFriendSearchChanged);
            }

            // 返回按钮
            if (m_backButton != null)
            {
                m_backButton.onClick.AddListener(() => OnBackButtonClicked());
            }
        }

        /// <summary>
        /// 显示主菜单
        /// </summary>
        public void ShowMainMenu()
        {
            SetPanelActive(m_panelRoot, true);
            SetPanelActive(m_createRoomPanel, false);
            SetPanelActive(m_roomBrowserPanel, false);
            SetPanelActive(m_friendsPanel, false);
        }

        /// <summary>
        /// 显示创建房间面板
        /// </summary>
        public void ShowCreateRoomPanel()
        {
            if (!m_isConnected)
            {
                ShowConnectionError();
                return;
            }

            SetPanelActive(m_panelRoot, false);
            SetPanelActive(m_createRoomPanel, true);
            SetPanelActive(m_roomBrowserPanel, false);
            SetPanelActive(m_friendsPanel, false);

            // 设置默认房间名
            if (m_roomNameInput != null)
            {
                m_roomNameInput.text = $"Room_{System.DateTime.Now.ToString("HHmm")}";
            }

            OnModeSelected?.Invoke(MultiplayerModeType.CreateRoom);
        }

        /// <summary>
        /// 显示房间浏览面板
        /// </summary>
        public void ShowRoomBrowserPanel()
        {
            if (!m_isConnected)
            {
                ShowConnectionError();
                return;
            }

            SetPanelActive(m_panelRoot, false);
            SetPanelActive(m_createRoomPanel, false);
            SetPanelActive(m_roomBrowserPanel, true);
            SetPanelActive(m_friendsPanel, false);

            // 刷新房间列表
            RefreshRoomList();

            OnModeSelected?.Invoke(MultiplayerModeType.JoinRoom);
        }

        /// <summary>
        /// 显示好友列表面板
        /// </summary>
        public void ShowFriendsPanel()
        {
            if (!m_isConnected)
            {
                ShowConnectionError();
                return;
            }

            SetPanelActive(m_panelRoot, false);
            SetPanelActive(m_createRoomPanel, false);
            SetPanelActive(m_roomBrowserPanel, false);
            SetPanelActive(m_friendsPanel, true);

            // 刷新好友列表
            RefreshFriendsList();

            OnModeSelected?.Invoke(MultiplayerModeType.Friends);
        }

        /// <summary>
        /// 创建房间
        /// </summary>
        private void CreateRoom()
        {
            if (!ValidateRoomSettings())
            {
                return;
            }

            PlayButtonClickSound();

            string roomName = m_roomNameInput.text.Trim();
            if (string.IsNullOrEmpty(roomName))
            {
                roomName = $"Room_{System.DateTime.Now.ToString("HHmm")}";
            }

            // 创建房间参数
            var roomSettings = new RoomCreationSettings
            {
                roomName = roomName,
                maxPlayers = int.Parse(m_maxPlayersDropdown.options[m_maxPlayersDropdown.value].text),
                isPrivate = m_privateRoomToggle.isOn,
                difficulty = m_difficultyDropdown.options[m_difficultyDropdown.value].text
            };

            // 通知创建房间
            OnRoomCreated?.Invoke(roomName);

            // 模拟创建房间（实际应该调用网络管理器）
            StartCoroutine(CreateRoomCoroutine(roomSettings));
        }

        /// <summary>
        /// 创建房间协程
        /// </summary>
        private IEnumerator CreateRoomCoroutine(RoomCreationSettings settings)
        {
            // 显示创建中状态
            if (m_createRoomButton != null)
            {
                m_createRoomButton.interactable = false;
                var buttonText = m_createRoomButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = GetLocalizedText("multiplayer.creating_room");
                }
            }

            // 模拟网络延迟
            yield return new WaitForSeconds(2f);

            // 恢复按钮状态
            if (m_createRoomButton != null)
            {
                m_createRoomButton.interactable = true;
                var buttonText = m_createRoomButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = GetLocalizedText("multiplayer.create_room");
                }
            }

            // 这里应该调用实际的网络管理器创建房间
            // 成功后切换到游戏场景
            Debug.Log($"Room created: {settings.roomName}");
        }

        /// <summary>
        /// 开始快速匹配
        /// </summary>
        private void StartQuickMatch()
        {
            if (!m_isConnected)
            {
                ShowConnectionError();
                return;
            }

            PlayButtonClickSound();
            OnModeSelected?.Invoke(MultiplayerModeType.QuickMatch);

            // 这里应该调用实际的快速匹配逻辑
            Debug.Log("Starting quick match...");
        }

        /// <summary>
        /// 刷新房间列表
        /// </summary>
        private void RefreshRoomList()
        {
            if (!m_isConnected) return;

            PlayButtonClickSound();

            // 清除现有房间项
            ClearRoomItems();

            // 模拟获取房间列表
            StartCoroutine(RefreshRoomListCoroutine());
        }

        /// <summary>
        /// 刷新房间列表协程
        /// </summary>
        private IEnumerator RefreshRoomListCoroutine()
        {
            // 显示加载状态
            if (m_refreshRoomsButton != null)
            {
                m_refreshRoomsButton.interactable = false;
            }

            // 模拟网络延迟
            yield return new WaitForSeconds(1f);

            // 模拟房间数据
            m_availableRooms.Clear();
            m_availableRooms.AddRange(GenerateTestRooms());

            // 创建房间UI项
            foreach (var room in m_availableRooms)
            {
                CreateRoomItem(room);
            }

            // 更新房间数量显示
            if (m_roomCountText != null)
            {
                m_roomCountText.text = $"找到 {m_availableRooms.Count} 个房间";
            }

            // 恢复按钮状态
            if (m_refreshRoomsButton != null)
            {
                m_refreshRoomsButton.interactable = true;
            }
        }

        /// <summary>
        /// 刷新好友列表
        /// </summary>
        private void RefreshFriendsList()
        {
            if (!m_isConnected) return;

            PlayButtonClickSound();

            // 清除现有好友项
            ClearFriendItems();

            // 模拟获取好友列表
            StartCoroutine(RefreshFriendsListCoroutine());
        }

        /// <summary>
        /// 刷新好友列表协程
        /// </summary>
        private IEnumerator RefreshFriendsListCoroutine()
        {
            // 显示加载状态
            if (m_refreshFriendsButton != null)
            {
                m_refreshFriendsButton.interactable = false;
            }

            // 模拟网络延迟
            yield return new WaitForSeconds(1f);

            // 模拟好友数据
            m_friendsList.Clear();
            m_friendsList.AddRange(GenerateTestFriends());

            // 创建好友UI项
            foreach (var friend in m_friendsList)
            {
                CreateFriendItem(friend);
            }

            // 更新好友数量显示
            if (m_friendCountText != null)
            {
                m_friendCountText.text = $"在线好友: {m_friendsList.Count(f => f.status == "Online")} / {m_friendsList.Count}";
            }

            // 恢复按钮状态
            if (m_refreshFriendsButton != null)
            {
                m_refreshFriendsButton.interactable = true;
            }
        }

        /// <summary>
        /// 创建房间项UI
        /// </summary>
        private void CreateRoomItem(RoomInfo room)
        {
            if (m_roomItemPrefab == null || m_roomListContainer == null) return;

            GameObject roomItem = Instantiate(m_roomItemPrefab, m_roomListContainer);

            // 设置房间信息
            var roomNameText = roomItem.transform.Find("RoomName")?.GetComponent<TextMeshProUGUI>();
            if (roomNameText != null)
            {
                roomNameText.text = room.roomName;
            }

            var hostNameText = roomItem.transform.Find("HostName")?.GetComponent<TextMeshProUGUI>();
            if (hostNameText != null)
            {
                hostNameText.text = $"主机: {room.hostName}";
            }

            var playersText = roomItem.transform.Find("Players")?.GetComponent<TextMeshProUGUI>();
            if (playersText != null)
            {
                playersText.text = $"{room.currentPlayers}/{room.maxPlayers}";
            }

            var difficultyText = roomItem.transform.Find("Difficulty")?.GetComponent<TextMeshProUGUI>();
            if (difficultyText != null)
            {
                difficultyText.text = room.difficulty;
            }

            var pingText = roomItem.transform.Find("Ping")?.GetComponent<TextMeshProUGUI>();
            if (pingText != null)
            {
                pingText.text = $"{room.ping}ms";
            }

            // 设置加入按钮
            var joinButton = roomItem.transform.Find("JoinButton")?.GetComponent<Button>();
            if (joinButton != null)
            {
                joinButton.onClick.AddListener(() => JoinRoom(room));
                joinButton.interactable = room.currentPlayers < room.maxPlayers;
            }

            // 设置私有房间图标
            var privateIcon = roomItem.transform.Find("PrivateIcon")?.gameObject;
            if (privateIcon != null)
            {
                privateIcon.SetActive(room.isPrivate);
            }

            m_roomItems.Add(roomItem);
        }

        /// <summary>
        /// 创建好友项UI
        /// </summary>
        private void CreateFriendItem(FriendInfo friend)
        {
            if (m_friendItemPrefab == null || m_friendsListContainer == null) return;

            GameObject friendItem = Instantiate(m_friendItemPrefab, m_friendsListContainer);

            // 设置好友信息
            var friendNameText = friendItem.transform.Find("FriendName")?.GetComponent<TextMeshProUGUI>();
            if (friendNameText != null)
            {
                friendNameText.text = friend.friendName;
            }

            var statusText = friendItem.transform.Find("Status")?.GetComponent<TextMeshProUGUI>();
            if (statusText != null)
            {
                statusText.text = GetLocalizedText($"friend_status.{friend.status.ToLower()}");
                statusText.color = friend.status == "Online" ? Color.green : Color.gray;
            }

            var roomText = friendItem.transform.Find("CurrentRoom")?.GetComponent<TextMeshProUGUI>();
            if (roomText != null)
            {
                roomText.text = string.IsNullOrEmpty(friend.currentRoom) ? "大厅" : friend.currentRoom;
            }

            // 设置邀请按钮
            var inviteButton = friendItem.transform.Find("InviteButton")?.GetComponent<Button>();
            if (inviteButton != null)
            {
                inviteButton.onClick.AddListener(() => InviteFriend(friend));
                inviteButton.interactable = friend.status == "Online";
            }

            // 设置加入按钮
            var joinFriendButton = friendItem.transform.Find("JoinFriendButton")?.GetComponent<Button>();
            if (joinFriendButton != null)
            {
                joinFriendButton.onClick.AddListener(() => JoinFriend(friend));
                joinFriendButton.interactable = friend.status == "InGame" && !string.IsNullOrEmpty(friend.currentRoom);
            }

            m_friendItems.Add(friendItem);
        }

        /// <summary>
        /// 加入房间
        /// </summary>
        private void JoinRoom(RoomInfo room)
        {
            PlayButtonClickSound();
            OnRoomSelected?.Invoke(room);

            // 这里应该调用实际的加入房间逻辑
            Debug.Log($"Joining room: {room.roomName}");
        }

        /// <summary>
        /// 邀请好友
        /// </summary>
        private void InviteFriend(FriendInfo friend)
        {
            PlayButtonClickSound();
            OnFriendSelected?.Invoke(friend);

            // 这里应该调用实际的邀请逻辑
            Debug.Log($"Inviting friend: {friend.friendName}");
        }

        /// <summary>
        /// 加入好友房间
        /// </summary>
        private void JoinFriend(FriendInfo friend)
        {
            PlayButtonClickSound();
            OnFriendSelected?.Invoke(friend);

            // 这里应该调用实际的加入好友房间逻辑
            Debug.Log($"Joining friend's room: {friend.friendName} in {friend.currentRoom}");
        }

        /// <summary>
        /// 房间搜索变化处理
        /// </summary>
        private void OnRoomSearchChanged(string searchText)
        {
            FilterRoomList(searchText);
        }

        /// <summary>
        /// 好友搜索变化处理
        /// </summary>
        private void OnFriendSearchChanged(string searchText)
        {
            FilterFriendsList(searchText);
        }

        /// <summary>
        /// 过滤房间列表
        /// </summary>
        private void FilterRoomList(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                // 显示所有房间
                foreach (var item in m_roomItems)
                {
                    item.SetActive(true);
                }
                return;
            }

            // 根据搜索文本过滤
            foreach (var item in m_roomItems)
            {
                var roomNameText = item.transform.Find("RoomName")?.GetComponent<TextMeshProUGUI>();
                var hostNameText = item.transform.Find("HostName")?.GetComponent<TextMeshProUGUI>();

                bool matchesSearch = false;
                if (roomNameText != null && roomNameText.text.ToLower().Contains(searchText.ToLower()))
                {
                    matchesSearch = true;
                }
                else if (hostNameText != null && hostNameText.text.ToLower().Contains(searchText.ToLower()))
                {
                    matchesSearch = true;
                }

                item.SetActive(matchesSearch);
            }
        }

        /// <summary>
        /// 过滤好友列表
        /// </summary>
        private void FilterFriendsList(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                // 显示所有好友
                foreach (var item in m_friendItems)
                {
                    item.SetActive(true);
                }
                return;
            }

            // 根据搜索文本过滤
            foreach (var item in m_friendItems)
            {
                var friendNameText = item.transform.Find("FriendName")?.GetComponent<TextMeshProUGUI>();

                bool matchesSearch = false;
                if (friendNameText != null && friendNameText.text.ToLower().Contains(searchText.ToLower()))
                {
                    matchesSearch = true;
                }

                item.SetActive(matchesSearch);
            }
        }

        /// <summary>
        /// 验证房间设置
        /// </summary>
        private bool ValidateRoomSettings()
        {
            if (m_roomNameInput != null && m_roomNameInput.text.Trim().Length > 20)
            {
                ShowError(GetLocalizedText("multiplayer.room_name_too_long"));
                return false;
            }

            if (m_playerNameInput != null && string.IsNullOrEmpty(m_playerNameInput.text.Trim()))
            {
                ShowError(GetLocalizedText("multiplayer.player_name_required"));
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查连接状态
        /// </summary>
        private void CheckConnectionStatus()
        {
            // 这里应该检查实际的网络连接状态
            // 目前模拟为已连接
            m_isConnected = true;
            UpdateConnectionStatus();
        }

        /// <summary>
        /// 更新连接状态显示
        /// </summary>
        private void UpdateConnectionStatus()
        {
            if (m_connectionStatusText != null)
            {
                m_connectionStatusText.text = m_isConnected ?
                    GetLocalizedText("multiplayer.connected") :
                    GetLocalizedText("multiplayer.disconnected");
            }

            if (m_connectionStatusIcon != null)
            {
                m_connectionStatusIcon.color = m_isConnected ? m_connectedColor : m_disconnectedColor;
            }
        }

        /// <summary>
        /// 显示连接错误
        /// </summary>
        private void ShowConnectionError()
        {
            ShowError(GetLocalizedText("multiplayer.connection_error"));
        }

        /// <summary>
        /// 显示错误信息
        /// </summary>
        private void ShowError(string message)
        {
            // 这里应该显示错误对话框或提示
            Debug.LogError(message);
        }

        /// <summary>
        /// 返回按钮点击处理
        /// </summary>
        private void OnBackButtonClicked()
        {
            PlayButtonClickSound();
            OnBackPressed?.Invoke();
        }

        /// <summary>
        /// 播放按钮点击音效
        /// </summary>
        private void PlayButtonClickSound()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUISound("button_click");
            }
        }

        /// <summary>
        /// 设置面板激活状态
        /// </summary>
        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }

        /// <summary>
        /// 清除房间项
        /// </summary>
        private void ClearRoomItems()
        {
            foreach (var item in m_roomItems)
            {
                if (item != null)
                {
                    DestroyImmediate(item);
                }
            }
            m_roomItems.Clear();
        }

        /// <summary>
        /// 清除好友项
        /// </summary>
        private void ClearFriendItems()
        {
            foreach (var item in m_friendItems)
            {
                if (item != null)
                {
                    DestroyImmediate(item);
                }
            }
            m_friendItems.Clear();
        }

        /// <summary>
        /// 获取本地化字符串
        /// </summary>
        private string GetLocalizedText(string key)
        {
            return m_localizationManager?.GetLocalizedText(key) ?? key;
        }

        /// <summary>
        /// 创建房间回调
        /// </summary>
        public void OnCreateRoom()
        {
            PlayButtonClickSound();

            // 验证输入
            if (!ValidateInput())
            {
                return;
            }

            // 创建房间
            CreateRoom();
        }

        /// <summary>
        /// 返回按钮点击回调
        /// </summary>
        public void OnBackClicked()
        {
            PlayButtonClickSound();

            // 触发返回事件
            OnBack?.Invoke();
        }

        /// <summary>
        /// 显示模式详情
        /// </summary>
        public void ShowModeDetails(MultiplayerModeType modeType)
        {
            m_selectedMode = modeType;

            // 更新UI显示
            UpdateModeDisplay();

            // 根据模式类型更新相应的面板
            switch (modeType)
            {
                case MultiplayerModeType.CreateRoom:
                    ShowCreateRoomPanel();
                    break;
                case MultiplayerModeType.JoinRoom:
                    ShowJoinRoomPanel();
                    break;
                case MultiplayerModeType.Friends:
                    ShowFriendsPanel();
                    break;
                case MultiplayerModeType.QuickMatch:
                    ShowQuickMatchPanel();
                    break;
            }
        }

        /// <summary>
        /// 显示加入房间面板
        /// </summary>
        private void ShowJoinRoomPanel()
        {
            // 显示加入房间相关UI
            if (m_createRoomPanel != null)
            {
                m_createRoomPanel.SetActive(false);
            }
            if (m_joinRoomPanel != null)
            {
                m_joinRoomPanel.SetActive(true);
            }
            if (m_friendsPanel != null)
            {
                m_friendsPanel.SetActive(false);
            }

            // 刷新房间列表
            RefreshRoomList();
        }

        /// <summary>
        /// 显示快速匹配面板
        /// </summary>
        private void ShowQuickMatchPanel()
        {
            // 启动快速匹配
            StartQuickMatch();
        }

        /// <summary>
        /// 更新模式显示
        /// </summary>
        private void UpdateModeDisplay()
        {
            // 更新标题和描述
            if (m_titleText != null)
            {
                string titleKey = GetModeTitle(m_selectedMode);
                m_titleText.text = GetLocalizedText(titleKey);
            }
        }

        /// <summary>
        /// 获取模式标题键
        /// </summary>
        private string GetModeTitle(MultiplayerModeType modeType)
        {
            switch (modeType)
            {
                case MultiplayerModeType.CreateRoom:
                    return "multiplayer.create_room";
                case MultiplayerModeType.JoinRoom:
                    return "multiplayer.join_room";
                case MultiplayerModeType.Friends:
                    return "multiplayer.friends";
                case MultiplayerModeType.QuickMatch:
                    return "multiplayer.quick_match";
                default:
                    return "multiplayer.title";
            }
        }

        /// <summary>
        /// 生成测试房间数据
        /// </summary>
        private List<RoomInfo> GenerateTestRooms()
        {
            var rooms = new List<RoomInfo>();

            for (int i = 0; i < 5; i++)
            {
                rooms.Add(new RoomInfo
                {
                    roomId = $"room_{i}",
                    roomName = $"房间 {i + 1}",
                    hostName = $"玩家{i + 1}",
                    currentPlayers = UnityEngine.Random.Range(1, 4),
                    maxPlayers = 4,
                    isPrivate = UnityEngine.Random.Range(0, 2) == 0,
                    difficulty = i % 2 == 0 ? "简单" : "困难",
                    ping = UnityEngine.Random.Range(20, 100),
                    isPasswordProtected = UnityEngine.Random.Range(0, 3) == 0
                });
            }

            return rooms;
        }

        /// <summary>
        /// 生成测试好友数据
        /// </summary>
        private List<FriendInfo> GenerateTestFriends()
        {
            var friends = new List<FriendInfo>();
            var statuses = new string[] { "Online", "Offline", "InGame" };

            for (int i = 0; i < 8; i++)
            {
                var status = statuses[UnityEngine.Random.Range(0, statuses.Length)];
                friends.Add(new FriendInfo
                {
                    friendId = $"friend_{i}",
                    friendName = $"好友{i + 1}",
                    status = status,
                    currentRoom = status == "InGame" ? $"房间{UnityEngine.Random.Range(1, 5)}" : "",
                    lastSeen = System.DateTime.Now.AddMinutes(-UnityEngine.Random.Range(0, 1440))
                });
            }

            return friends;
        }

        /// <summary>
        /// 验证输入
        /// </summary>
        private bool ValidateInput()
        {
            if (m_roomNameInput != null && string.IsNullOrEmpty(m_roomNameInput.text.Trim()))
            {
                ShowError("请输入房间名称");
                return false;
            }

            if (m_playerNameInput != null && string.IsNullOrEmpty(m_playerNameInput.text.Trim()))
            {
                ShowError("请输入玩家名称");
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 房间创建设置
    /// </summary>
    [System.Serializable]
    public class RoomCreationSettings
    {
        public string roomName;
        public int maxPlayers;
        public bool isPrivate;
        public string difficulty;
    }
}