// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PongHub.MainMenu
{
    /// <summary>
    /// 菜单控制器的基类，用于控制主菜单中的不同视图
    /// 提供了基本的显示/隐藏功能和按钮状态管理
    /// </summary>
    public class BaseMenuController : MonoBehaviour
    {
        /// <summary>
        /// 菜单中所有需要控制的按钮列表
        /// 在Unity编辑器中通过Inspector面板设置
        /// </summary>
        [SerializeField] private List<Button> m_menuButtons;

        /// <summary>
        /// 显示当前菜单
        /// 通过激活GameObject来实现
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏当前菜单
        /// 通过停用GameObject来实现
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 启用所有菜单按钮
        /// 调用SetButtonState(true)来设置按钮状态
        /// </summary>
        public void EnableButtons()
        {
            SetButtonState(true);
        }

        /// <summary>
        /// 禁用所有菜单按钮
        /// 调用SetButtonState(false)来设置按钮状态
        /// </summary>
        public void DisableButtons()
        {
            SetButtonState(false);
        }

        /// <summary>
        /// 设置所有按钮的交互状态
        /// </summary>
        /// <param name="enable">true表示启用按钮，false表示禁用按钮</param>
        private void SetButtonState(bool enable)
        {
            // 检查按钮列表是否为空
            if (m_menuButtons != null)
            {
                // 遍历所有按钮并设置其交互状态
                foreach (var button in m_menuButtons)
                {
                    button.interactable = enable;
                }
            }
        }
    }
}