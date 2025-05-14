// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UltimateGloveBall.MainMenu
{
    /// <summary>
    /// 主菜单按钮实现类
    /// 用于处理按钮的悬停状态，包括指针跟踪和按钮内部状态（图片和文本）的变化
    /// 这是一个自定义实现，可以同时处理多个指针的交互
    /// </summary>
    public class MainMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // 高亮状态下的文本颜色（紫色）
        private static readonly Color s_highlightTextColor = new(174f / 255f, 0f, 1f);
        // 普通状态下的文本颜色（黑色）
        private static readonly Color s_normalTextColor = Color.black;

        [Header("UI组件引用")]
        [SerializeField] private Transform m_root;           // 按钮的根Transform，用于旋转效果
        [SerializeField] private Image m_bgImage;           // 按钮背景图片
        [SerializeField] private TMPro.TMP_Text m_text;     // 按钮文本组件
        [SerializeField] private Color m_normalTextColor = s_normalTextColor;  // 可自定义的普通文本颜色
        [SerializeField] private UnityEvent<string> m_onHover;  // 悬停事件回调

        // 当前指向按钮的指针数量
        private int m_currentPointerCount = 0;

        /// <summary>
        /// 当组件启用时调用
        /// 重置按钮状态
        /// </summary>
        private void OnEnable()
        {
            Reset();
        }

        /// <summary>
        /// 当组件禁用时调用
        /// 重置按钮状态
        /// </summary>
        private void OnDisable()
        {
            Reset();
        }

        /// <summary>
        /// 处理指针进入按钮区域的事件
        /// </summary>
        /// <param name="eventData">指针事件数据</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            // 设置文本颜色为高亮色
            m_text.color = s_highlightTextColor;
            // 旋转按钮5度
            m_root.localEulerAngles = new Vector3(0, 0, 5);
            // 显示背景图片
            if (m_bgImage)
            {
                m_bgImage.enabled = true;
            }

            // 如果是第一个进入的指针，触发悬停事件
            if (m_currentPointerCount == 0)
            {
                m_onHover?.Invoke(null);
            }
            // 增加指针计数
            m_currentPointerCount++;
        }

        /// <summary>
        /// 处理指针离开按钮区域的事件
        /// </summary>
        /// <param name="eventData">指针事件数据</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            // 减少指针计数
            m_currentPointerCount--;
            // 如果没有指针在按钮上，重置按钮状态
            if (m_currentPointerCount <= 0)
            {
                Reset();
            }
        }

        /// <summary>
        /// 重置按钮状态到初始状态
        /// 包括文本颜色、旋转角度、背景图片和指针计数
        /// </summary>
        private void Reset()
        {
            // 重置文本颜色
            if (m_text)
            {
                m_text.color = m_normalTextColor;
            }

            // 重置旋转角度
            if (m_root)
            {
                m_root.localEulerAngles = Vector3.zero;
            }

            // 隐藏背景图片
            if (m_bgImage)
            {
                m_bgImage.enabled = false;
            }

            // 重置指针计数
            m_currentPointerCount = 0;
        }
    }
}