// Copyright (c) MagnusLab Inc. and affiliates.

using Meta.Multiplayer.Core;
using TMPro;
using PongHub.App;
using UnityEngine;
using UnityEngine.UI;

namespace PongHub.Arena.Player.Menu
{
    /// <summary>
    /// 玩家信息项组件，显示在游戏内菜单的玩家列表中。
    /// 展示玩家信息，以及静音/取消静音按钮和屏蔽/取消屏蔽按钮
    /// </summary>
    public class PlayerInfoItem : MonoBehaviour
    {
        /// <summary>
        /// 屏蔽按钮文本常量
        /// </summary>
        private const string BLOCK = "block";

        /// <summary>
        /// 取消屏蔽按钮文本常量
        /// </summary>
        private const string UNBLOCK = "unblock";

        /// <summary>
        /// 用户名文本组件
        /// </summary>
        [SerializeField] private TMP_Text m_usernameText;

        /// <summary>
        /// 静音按钮组件
        /// </summary>
        [SerializeField] private Button m_muteButton;

        /// <summary>
        /// 静音状态图标
        /// </summary>
        [SerializeField] private Image m_mutedIcon;

        /// <summary>
        /// 非静音状态图标
        /// </summary>
        [SerializeField] private Image m_unmutedIcon;

        /// <summary>
        /// 屏蔽按钮文本组件
        /// </summary>
        [SerializeField] private TMP_Text m_blockButtonText;

        /// <summary>
        /// 用户当前是否处于静音状态
        /// </summary>
        private bool m_isUserMutedState;

        /// <summary>
        /// 用户当前是否处于屏蔽状态
        /// </summary>
        private bool m_isUserBlockedState;

        /// <summary>
        /// 玩家网络状态组件引用
        /// </summary>
        private PlayerStateNetwork m_playerState;

        /// <summary>
        /// 初始化用户信息
        /// </summary>
        /// <param name="playerState">玩家网络状态组件</param>
        public void SetupUser(PlayerStateNetwork playerState)
        {
            m_playerState = playerState;
            m_usernameText.text = playerState.Username;
            m_isUserMutedState = playerState.VoipHandler.IsMuted;
            m_isUserBlockedState = BlockUserManager.Instance.IsUserBlocked(playerState.UserId);
            UpdateState();
        }

        /// <summary>
        /// 静音按钮点击回调
        /// </summary>
        public void OnMuteButtonClicked()
        {
            SetMuteState(!m_isUserMutedState);
        }

        /// <summary>
        /// 屏蔽按钮点击回调
        /// </summary>
        public void OnBlockUserClicked()
        {
            if (m_isUserBlockedState)
            {
                BlockUserManager.Instance.UnblockUser(m_playerState.UserId, OnUnblockSuccess);
            }
            else
            {
                BlockUserManager.Instance.BlockUser(m_playerState.UserId, OnBlockSuccess);
            }
        }

        /// <summary>
        /// 屏蔽用户成功回调
        /// </summary>
        /// <param name="userId">被屏蔽的用户ID</param>
        private void OnBlockSuccess(ulong userId)
        {
            m_isUserBlockedState = true;
            m_playerState.VoipHandler.IsMuted = true;
            UpdateState();
        }

        /// <summary>
        /// 取消屏蔽用户成功回调
        /// </summary>
        /// <param name="userId">被取消屏蔽的用户ID</param>
        private void OnUnblockSuccess(ulong userId)
        {
            m_isUserBlockedState = false;
            m_playerState.VoipHandler.IsMuted = UserMutingManager.Instance.IsUserMuted(userId);
            UpdateState();
        }

        /// <summary>
        /// 设置用户静音状态
        /// </summary>
        /// <param name="muted">是否静音</param>
        private void SetMuteState(bool muted)
        {
            if (muted)
            {
                UserMutingManager.Instance.MuteUser(m_playerState.UserId);
            }
            else
            {
                UserMutingManager.Instance.UnmuteUser(m_playerState.UserId);
            }
            m_isUserMutedState = muted;
            UpdateMuteButton();
        }

        /// <summary>
        /// 更新静音按钮状态
        /// </summary>
        private void UpdateMuteButton()
        {
            var showMute = m_isUserMutedState || m_isUserBlockedState;
            m_mutedIcon.gameObject.SetActive(showMute);
            m_unmutedIcon.gameObject.SetActive(!showMute);
            m_muteButton.interactable = !m_isUserBlockedState;
        }

        /// <summary>
        /// 更新屏蔽按钮状态
        /// </summary>
        private void UpdateBlockButtonState()
        {
            m_blockButtonText.text = m_isUserBlockedState ? UNBLOCK : BLOCK;
        }

        /// <summary>
        /// 更新所有UI状态
        /// </summary>
        private void UpdateState()
        {
            UpdateMuteButton();
            UpdateBlockButtonState();
        }
    }
}