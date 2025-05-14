// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using TMPro;
using UnityEngine;

namespace UltimateGloveBall.MainMenu
{
    /// <summary>
    /// 错误面板控制器类
    /// 负责管理错误提示面板的显示和内容设置
    /// 包含标题和消息文本的引用，以及显示和关闭面板的方法
    /// </summary>
    public class MenuErrorPanel : MonoBehaviour
    {
        /// <summary>
        /// 默认错误标题文本
        /// </summary>
        private const string DEFAULT_TITLE = "ERROR";

        /// <summary>
        /// 错误标题文本组件引用
        /// </summary>
        [SerializeField] private TMP_Text m_titleText;

        /// <summary>
        /// 错误消息文本组件引用
        /// </summary>
        [SerializeField] private TMP_Text m_messageText;

        /// <summary>
        /// 显示错误消息
        /// </summary>
        /// <param name="message">要显示的错误消息内容</param>
        /// <param name="title">错误标题，默认为"ERROR"</param>
        public void ShowMessage(string message, string title = DEFAULT_TITLE)
        {
            // 设置标题和消息文本
            m_titleText.text = title;
            m_messageText.text = message;
            // 激活错误面板
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 关闭错误面板
        /// 通过禁用游戏对象来隐藏面板
        /// </summary>
        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}