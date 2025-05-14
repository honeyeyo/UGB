// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateGloveBall.Arena.Player
{
    /// <summary>
    /// 处理玩家名称标签和主机状态的显示。
    /// 可以分别控制不同部分的可见性,也可以控制整个可视化组件的显示/隐藏。
    /// </summary>
    public class PlayerNameVisual : MonoBehaviour
    {
        /// <summary>
        /// UI画布组件引用
        /// </summary>
        [SerializeField] private Transform m_canvas;

        /// <summary>
        /// 用户名文本组件引用
        /// </summary>
        [SerializeField] private TMP_Text m_usernameText;

        /// <summary>
        /// 主机图标组件引用
        /// </summary>
        [SerializeField] private Image m_masterIcon;

        /// <summary>
        /// 用户头像图标组件引用
        /// </summary>
        [SerializeField] private Image m_userIcon;

        /// <summary>
        /// 组件是否启用
        /// </summary>
        private bool m_isEnabled = true;

        /// <summary>
        /// 组件是否可见
        /// </summary>
        private bool m_visible = true;

        /// <summary>
        /// 设置组件的启用状态
        /// </summary>
        /// <param name="enable">是否启用</param>
        public void SetEnableState(bool enable)
        {
            m_isEnabled = enable;
            SetVisibility(m_visible);
        }

        /// <summary>
        /// 设置组件的可见性
        /// </summary>
        /// <param name="show">是否显示</param>
        public void SetVisibility(bool show)
        {
            m_visible = show;
            m_canvas?.gameObject.SetActive(show && m_isEnabled);
        }

        /// <summary>
        /// 设置玩家用户名
        /// </summary>
        /// <param name="username">用户名文本</param>
        public void SetUsername(string username)
        {
            if (m_usernameText != null)
            {
                m_usernameText.text = username;
            }
        }

        /// <summary>
        /// 设置用户头像图标
        /// </summary>
        /// <param name="userIcon">头像Sprite图片</param>
        public void SetUserIcon(Sprite userIcon)
        {
            if (m_userIcon != null)
            {
                m_userIcon.sprite = userIcon;
            }

            m_userIcon.enabled = userIcon != null;
        }

        /// <summary>
        /// 显示/隐藏用户名
        /// </summary>
        /// <param name="show">是否显示</param>
        public void ShowUsername(bool show)
        {
            m_usernameText?.gameObject.SetActive(show);
        }

        /// <summary>
        /// 显示/隐藏主机图标
        /// </summary>
        /// <param name="show">是否显示</param>
        public void ShowMasterIcon(bool show)
        {
            m_masterIcon?.gameObject.SetActive(show);
        }

        /// <summary>
        /// 显示/隐藏用户头像
        /// </summary>
        /// <param name="show">是否显示</param>
        public void ShowUserIcon(bool show)
        {
            m_userIcon?.gameObject.SetActive(show);
        }
    }
}