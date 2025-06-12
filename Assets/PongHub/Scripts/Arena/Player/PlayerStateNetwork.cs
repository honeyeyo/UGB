// Copyright (c) MagnusLab Inc. and affiliates.

using Meta.Multiplayer.Core;
using Meta.Utilities;
using PongHub.App;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace PongHub.Arena.Player
{
    /// <summary>
    /// 玩家状态网络同步组件
    /// 负责跟踪玩家状态并在网络中同步到其他客户端
    /// 处理玩家名称显示和语音通话静音/取消静音状态
    /// </summary>
    public class PlayerStateNetwork : NetworkBehaviour
    {
        /// <summary>
        /// 玩家名称显示组件
        /// </summary>
        [SerializeField] private PlayerNameVisual m_playerNameVisual;

        /// <summary>
        /// 是否启用本地玩家名称显示
        /// </summary>
        [SerializeField] private bool m_enableLocalPlayerName;

        /// <summary>
        /// 语音通话处理组件
        /// </summary>
        [SerializeField] private VoipHandler m_voipHandler;

        /// <summary>
        /// 宠物猫拥有者组件,自动设置
        /// </summary>
        [SerializeField, AutoSet] private CatOwner m_catOwner;

        /// <summary>
        /// 玩家用户名网络变量,只有拥有者可写
        /// </summary>
        private NetworkVariable<FixedString128Bytes> m_username = new(
            writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// 玩家用户ID网络变量,只有拥有者可写
        /// </summary>
        private NetworkVariable<ulong> m_userId = new(
            writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// 是否为主机客户端网络变量,只有拥有者可写
        /// </summary>
        private NetworkVariable<bool> m_isMasterClient = new(
            true,
            writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// 用户图标SKU网络变量,只有拥有者可写
        /// </summary>
        private NetworkVariable<FixedString128Bytes> m_userIconSku = new(
            writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// 是否拥有猫的网络变量,只有拥有者可写
        /// </summary>
        private NetworkVariable<bool> m_hasACat = new(
            writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// 获取玩家用户名
        /// </summary>
        public string Username => m_username.Value.ToString();

        /// <summary>
        /// 获取玩家用户ID
        /// </summary>
        public ulong UserId => m_userId.Value;

        /// <summary>
        /// 获取语音通话处理组件
        /// </summary>
        public VoipHandler VoipHandler => m_voipHandler;

        /// <summary>
        /// 获取本地玩家状态,如果不是本地玩家则返回null
        /// </summary>
        private LocalPlayerState LocalPlayerState => IsOwner ? LocalPlayerState.Instance : null;

        /// <summary>
        /// 组件启动时初始化
        /// </summary>
        private void Start()
        {
            // 初始化所有状态变化回调
            OnUsernameChanged(m_username.Value, m_username.Value);
            OnUserIdChanged(m_userId.Value, m_userId.Value);
            OnMasterClientChanged(m_isMasterClient.Value, m_isMasterClient.Value);
            OnUserIconChanged(m_userIconSku.Value, m_userIconSku.Value);
            OnUserCatOwnershipChanged(m_hasACat.Value, m_hasACat.Value);

            // 注册用户静音状态变化回调
            UserMutingManager.Instance.RegisterCallback(OnUserMuteStateChanged);

            if (!LocalPlayerState) return;

            // 将本地玩家位置对齐到此玩家位置
            PlayerMovement.Instance.SnapPositionToTransform(transform);
            LocalPlayerState.OnChange += UpdateData;
            LocalPlayerState.OnSpawnCatChange += OnSpawnCatChanged;

            UpdateData();
        }

        /// <summary>
        /// 组件销毁时清理
        /// </summary>
        public override void OnDestroy()
        {
            base.OnDestroy();

            // 注销用户静音状态回调
            UserMutingManager.Instance.UnregisterCallback(OnUserMuteStateChanged);

            // 销毁宠物猫
            if (m_catOwner)
            {
                m_catOwner.DeSpawnCat();
            }

            if (!LocalPlayerState) return;

            // 保存最后位置和旋转
            var thisTransform = transform;
            var playerTransform = LocalPlayerState.transform;
            playerTransform.position = thisTransform.position;
            playerTransform.rotation = thisTransform.rotation;

            // 注销状态变化回调
            LocalPlayerState.OnChange -= UpdateData;
            LocalPlayerState.OnSpawnCatChange -= OnSpawnCatChanged;
        }

        /// <summary>
        /// 组件启用时注册网络变量回调
        /// </summary>
        private void OnEnable()
        {
            m_username.OnValueChanged += OnUsernameChanged;
            m_userId.OnValueChanged += OnUserIdChanged;
            m_isMasterClient.OnValueChanged += OnMasterClientChanged;
            m_userIconSku.OnValueChanged += OnUserIconChanged;
            m_hasACat.OnValueChanged += OnUserCatOwnershipChanged;

            m_playerNameVisual?.SetEnableState(m_enableLocalPlayerName);
        }

        /// <summary>
        /// 组件禁用时注销网络变量回调
        /// </summary>
        private void OnDisable()
        {
            m_username.OnValueChanged -= OnUsernameChanged;
            m_userId.OnValueChanged -= OnUserIdChanged;
            m_isMasterClient.OnValueChanged -= OnMasterClientChanged;
            m_userIconSku.OnValueChanged -= OnUserIconChanged;
            m_hasACat.OnValueChanged += OnUserCatOwnershipChanged;
        }

        /// <summary>
        /// 网络对象生成时初始化
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // 设置名称显示状态
            m_playerNameVisual?.SetEnableState(m_enableLocalPlayerName || LocalPlayerState == null);

            if (LocalPlayerState)
            {
                // 对齐本地玩家位置
                PlayerMovement.Instance.SnapPositionToTransform(transform);

                // 设置初始状态
                SetState(LocalPlayerState.Username, LocalPlayerState.UserId, LocalPlayerState.UserIconSku,
                    LocalPlayerState.SpawnCatInNextGame);
                SetIsMaster(IsHost);
            }
        }

        /// <summary>
        /// 更新玩家数据
        /// </summary>
        private void UpdateData()
        {
            SetState(LocalPlayerState.Username, LocalPlayerState.UserId, LocalPlayerState.UserIconSku,
                LocalPlayerState.SpawnCatInNextGame);
        }

        /// <summary>
        /// 处理生成猫状态变化
        /// </summary>
        private void OnSpawnCatChanged()
        {
            m_hasACat.Value = LocalPlayerState.SpawnCatInNextGame;
        }

        /// <summary>
        /// 设置玩家状态
        /// </summary>
        private void SetState(string username, ulong userId, string userIcon, bool ownsCat)
        {
            m_username.Value = username;
            m_userId.Value = userId;
            m_userIconSku.Value = userIcon;
            m_hasACat.Value = ownsCat;
        }

        /// <summary>
        /// 设置是否为主机
        /// </summary>
        private void SetIsMaster(bool isMasterClient)
        {
            m_isMasterClient.Value = isMasterClient;
        }

        /// <summary>
        /// 处理用户ID变化
        /// </summary>
        private void OnUserIdChanged(ulong prevUserId, ulong newUserId)
        {
            if (newUserId != 0)
            {
                // 更新语音静音状态
                m_voipHandler.IsMuted = BlockUserManager.Instance.IsUserBlocked(newUserId) ||
                                        UserMutingManager.Instance.IsUserMuted(newUserId);
            }
        }

        /// <summary>
        /// 处理用户名变化
        /// </summary>
        private void OnUsernameChanged(FixedString128Bytes oldName, FixedString128Bytes newName)
        {
            m_playerNameVisual?.SetUsername(newName.ConvertToString());
        }

        /// <summary>
        /// 处理用户图标变化
        /// </summary>
        private void OnUserIconChanged(FixedString128Bytes oldIcon, FixedString128Bytes newIcon)
        {
            if (m_playerNameVisual != null)
            {
                var iconSku = newIcon.ConvertToString();
                var icon = UserIconManager.Instance.GetIconForSku(iconSku);
                m_playerNameVisual.SetUserIcon(icon);
            }
        }

        /// <summary>
        /// 处理猫的所有权变化
        /// </summary>
        private void OnUserCatOwnershipChanged(bool oldValue, bool newValue)
        {
            if (m_catOwner != null)
            {
                if (newValue)
                {
                    m_catOwner.SpawnCat();
                }
                else
                {
                    m_catOwner.DeSpawnCat();
                }
            }
        }

        /// <summary>
        /// 处理主机状态变化
        /// </summary>
        private void OnMasterClientChanged(bool oldVal, bool newVal)
        {
            m_playerNameVisual?.ShowMasterIcon(newVal);
        }

        /// <summary>
        /// 处理用户静音状态变化
        /// </summary>
        private void OnUserMuteStateChanged(ulong userId, bool isMuted)
        {
            if (userId == m_userId.Value)
            {
                m_voipHandler.IsMuted = isMuted || BlockUserManager.Instance.IsUserBlocked(userId);
            }
        }
    }
}