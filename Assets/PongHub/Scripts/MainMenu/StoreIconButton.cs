// Copyright (c) MagnusLab Inc. and affiliates.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PongHub.MainMenu
{
    /// <summary>
    /// 商店图标按钮控制器类
    /// 用于显示可购买或选择的图标
    /// 处理按钮的内部状态并向StoreMenuController暴露关键数据
    /// </summary>
    public class StoreIconButton : MonoBehaviour
    {
        [Header("UI组件引用")]
        [SerializeField]
        [Tooltip("Price Text / 价格文本 - Text component for displaying product price")]
        private TMP_Text m_priceText;    // 价格文本组件

        [SerializeField]
        [Tooltip("Name Text / 名称文本 - Text component for displaying product name")]
        private TMP_Text m_nameText;     // 名称文本组件

        [SerializeField]
        [Tooltip("Icon Image / 图标图片 - Image component for displaying product icon")]
        private Image m_iconImg;         // 图标图片组件

        [SerializeField]
        [Tooltip("Background Image / 背景图片 - Image component for button background")]
        private Image m_bgImg;           // 背景图片组件

        private Color m_baseColor;                        // 背景基础颜色
        private Action<StoreIconButton> m_onClickedCallback;  // 点击回调函数
        public string SKU { get; private set; }           // 商品唯一标识符
        public bool Owned { get; private set; }           // 是否已拥有

        /// <summary>
        /// 初始化函数
        /// 保存背景图片的原始颜色
        /// </summary>
        private void Awake()
        {
            m_baseColor = m_bgImg.color;
        }

        /// <summary>
        /// 设置按钮的初始状态和显示内容
        /// </summary>
        /// <param name="sku">商品唯一标识符</param>
        /// <param name="name">商品名称</param>
        /// <param name="price">商品价格</param>
        /// <param name="icon">商品图标</param>
        /// <param name="purchased">是否已购买</param>
        /// <param name="onClicked">点击回调函数</param>
        public void Setup(string sku, string name, string price, Sprite icon, bool purchased, Action<StoreIconButton> onClicked)
        {
            SKU = sku;
            m_nameText.text = name;
            Owned = purchased;
            // 根据购买状态和价格显示不同文本
            m_priceText.text = purchased ? "Owned" : price.Contains("0.00") ? "Free" : price;
            m_iconImg.sprite = icon;
            m_iconImg.gameObject.SetActive(icon != null);

            m_onClickedCallback = onClicked;
        }

        /// <summary>
        /// 处理购买完成后的状态更新
        /// </summary>
        public void OnPurchased()
        {
            Owned = true;
            m_priceText.text = "Owned";
        }

        /// <summary>
        /// 选中按钮时改变背景颜色
        /// </summary>
        public void Select()
        {
            m_bgImg.color = Color.cyan;
        }

        /// <summary>
        /// 取消选中时恢复背景颜色
        /// </summary>
        public void Deselect()
        {
            m_bgImg.color = m_baseColor;
        }

        /// <summary>
        /// 处理按钮点击事件
        /// 调用注册的回调函数
        /// </summary>
        public void OnClick()
        {
            m_onClickedCallback?.Invoke(this);
        }
    }
}