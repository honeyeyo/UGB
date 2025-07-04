// Copyright (c) MagnusLab Inc. and affiliates.

using System.Collections.Generic;
using TMPro;
using PongHub.App;
using UnityEngine;
using UnityEngine.UI;

namespace PongHub.MainMenu
{
    /// <summary>
    /// 商店菜单控制器
    /// 负责展示可购买的商品图标,处理商品的选择和购买流程
    /// </summary>
    public class StoreMenuController : BaseMenuController
    {
        [SerializeField]
        [Tooltip("Store Icon Button Prefab / 商店图标按钮预制件 - Prefab for store icon buttons")]
        private StoreIconButton m_storeIconButtonPrefab;

        [SerializeField]
        [Tooltip("Icon Selection View / 图标选择视图 - GameObject for icon selection interface")]
        private GameObject m_iconSelectionView;

        [SerializeField]
        [Tooltip("Grid Transform / 网格变换 - Transform for arranging icon buttons in grid")]
        private Transform m_grid;

        [SerializeField]
        [Tooltip("Back Button / 返回按钮 - Button for navigating back to menu")]
        private GameObject m_backButton;

        [SerializeField]
        [Tooltip("Menu Controller / 菜单控制器 - Reference to main menu controller")]
        private MainMenuController m_menuController;

        [Header("Purchase Flow")]
        [SerializeField]
        [Tooltip("Purchase Flow Root / 购买流程根对象 - Root GameObject for purchase flow UI")]
        private GameObject m_purchaseFlowRoot;

        [SerializeField]
        [Tooltip("Purchase Name Text / 购买名称文本 - Text component for purchase item name")]
        private TMP_Text m_purchaseName;

        [SerializeField]
        [Tooltip("Purchase Image / 购买图像 - Image component for purchase item preview")]
        private Image m_purchaseImage;

        [SerializeField]
        [Tooltip("Purchase Price Text / 购买价格文本 - Text component for purchase item price")]
        private TMP_Text m_purchasePrice;

        [SerializeField]
        [Tooltip("Processing Message / 处理中消息 - GameObject for processing purchase message")]
        private GameObject m_processingMessage;

        [Header("Cats")]
        [SerializeField]
        [Tooltip("Cat Count Text / 猫咪数量文本 - Text component displaying owned cat count")]
        private TMP_Text m_catCountText;

        [SerializeField]
        [Tooltip("Cat Buy Button / 猫咪购买按钮 - Button for purchasing cats")]
        private Button m_catBuyButton;

        private StoreIconButton m_selectedButton;

        private Dictionary<string, StoreIconButton> m_skuToButton = new();

        private string m_skuToPurchase;
        private void Start()
        {
            m_iconSelectionView.gameObject.SetActive(true);
            m_purchaseFlowRoot.SetActive(false);
            m_processingMessage.SetActive(false);
            SetupIcons();
            UpdateCatPurchaseState();
        }

        /// <summary>
        /// 当对象启用时更新猫咪购买状态
        /// </summary>
        private void OnEnable()
        {
            UpdateCatPurchaseState();
        }

        /// <summary>
        /// 更新猫咪购买状态UI
        /// </summary>
        private void UpdateCatPurchaseState()
        {
            var catCount = GameSettings.Instance.OwnedCatsCount;
            m_catCountText.text = catCount.ToString();
            m_catBuyButton.gameObject.SetActive(catCount < 3);
        }

        /// <summary>
        /// 设置商店图标
        /// 创建"无图标"选项和所有可购买的图标按钮
        /// </summary>
        private void SetupIcons()
        {
            // 创建"无图标"选项
            var noneIconButton = Instantiate(m_storeIconButtonPrefab, m_grid);
            noneIconButton.Setup(null, "None", null, null, true, OnIconClicked);

            // 创建所有可购买的图标按钮
            var iap = IAPManager.Instance;
            foreach (var sku in iap.GetProductSkusForCategory(ProductCategories.ICONS))
            {
                var product = iap.GetProduct(sku);
                if (product != null)
                {
                    var iconButton = Instantiate(m_storeIconButtonPrefab, m_grid);
                    iconButton.Setup(sku, product.Name, product.FormattedPrice, UserIconManager.Instance.GetIconForSku(sku),
                        iap.IsPurchased(sku), OnIconClicked);
                    m_skuToButton[sku] = iconButton;

                    // 如果当前SKU是已选择的图标,则选中该按钮
                    if (sku == GameSettings.Instance.SelectedUserIconSku)
                    {
                        SelectButton(iconButton);
                    }
                }
            }

            // 如果没有选中的按钮,默认选中"无图标"选项
            if (m_selectedButton == null)
            {
                SelectButton(noneIconButton);
            }
        }

        /// <summary>
        /// 处理图标按钮点击事件
        /// </summary>
        /// <param name="button">被点击的按钮</param>
        private void OnIconClicked(StoreIconButton button)
        {
            if (button.Owned)
            {
                SelectButton(button);
            }
            else
            {
                ShowPurchaseFlow(button.SKU);
            }
        }

        /// <summary>
        /// 显示购买流程界面
        /// </summary>
        /// <param name="sku">要购买的商品SKU</param>
        private void ShowPurchaseFlow(string sku)
        {
            m_iconSelectionView.gameObject.SetActive(false);
            m_purchaseFlowRoot.SetActive(true);
            m_backButton.SetActive(false);

            var iap = IAPManager.Instance;
            m_skuToPurchase = sku;
            var product = iap.GetProduct(sku);
            m_purchaseName.text = product.Name;
            m_purchaseImage.sprite = UserIconManager.Instance.GetIconForSku(sku);
            var price = product.FormattedPrice;
            m_purchasePrice.text = price.Contains("0.00") ? "Free" : price;
        }

        /// <summary>
        /// 取消购买流程
        /// </summary>
        public void OnCancelPurchaseFlowClicked()
        {
            m_iconSelectionView.gameObject.SetActive(true);
            m_purchaseFlowRoot.SetActive(false);
            m_backButton.SetActive(true);
            m_skuToPurchase = null;
        }

        /// <summary>
        /// 处理购买按钮点击
        /// </summary>
        public void OnBuyClicked()
        {
            m_purchaseFlowRoot.SetActive(false);
            m_processingMessage.SetActive(true);
            IAPManager.Instance.Purchase(m_skuToPurchase, OnPurchaseFlowCompleted);
        }

        /// <summary>
        /// 处理购买猫咪按钮点击
        /// </summary>
        public void OnBuyCatClicked()
        {
            m_iconSelectionView.SetActive(false);
            m_processingMessage.SetActive(true);
            m_backButton.SetActive(false);

            // 检查是否已购买但未使用
            if (IAPManager.Instance.IsPurchased(ProductCategories.CAT))
            {
                // if something happened and we already had purchased it, but not used it
                // we consume the purchase
                IAPManager.Instance.ConsumePurchase(ProductCategories.CAT, OnCatPurchaseConsumed);
            }
            else
            {
                IAPManager.Instance.Purchase(ProductCategories.CAT, OnCatPurchaseCompleted);
            }
        }

        /// <summary>
        /// 处理猫咪购买完成回调
        /// </summary>
        private void OnCatPurchaseCompleted(string sku, bool success, string errorMsg)
        {
            if (success)
            {
                // 购买成功后消费该商品并保存到库存
                IAPManager.Instance.ConsumePurchase(ProductCategories.CAT, OnCatPurchaseConsumed);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(errorMsg) && m_menuController)
                {
                    m_menuController.OnShowErrorMsgEvent(errorMsg);
                }
                m_purchaseFlowRoot.SetActive(false);
                m_processingMessage.SetActive(false);
                m_iconSelectionView.SetActive(true);
                m_backButton.SetActive(true);
            }
        }

        /// <summary>
        /// 处理猫咪购买消费完成回调
        /// </summary>
        private void OnCatPurchaseConsumed(string sku, bool success)
        {
            if (success)
            {
                GameSettings.Instance.OwnedCatsCount++;
                UpdateCatPurchaseState();
            }

            OnPurchaseComplete();
        }

        /// <summary>
        /// 处理商品购买完成回调
        /// </summary>
        private void OnPurchaseFlowCompleted(string sku, bool success, string errorMsg)
        {
            if (success)
            {
                var button = m_skuToButton[sku];
                button.OnPurchased();
                SelectButton(button);
            }
            else if (!string.IsNullOrWhiteSpace(errorMsg) && m_menuController)
            {
                m_menuController.OnShowErrorMsgEvent(errorMsg);
            }

            OnPurchaseComplete();
        }

        /// <summary>
        /// 处理购买完成后的界面更新
        /// </summary>
        private void OnPurchaseComplete()
        {
            m_purchaseFlowRoot.SetActive(false);
            m_processingMessage.SetActive(false);
            m_iconSelectionView.SetActive(true);
            m_backButton.SetActive(true);
        }

        /// <summary>
        /// 选中指定按钮
        /// </summary>
        /// <param name="button">要选中的按钮</param>
        private void SelectButton(StoreIconButton button)
        {
            if (m_selectedButton)
            {
                m_selectedButton.Deselect();
            }

            m_selectedButton = button;
            m_selectedButton.Select();
            GameSettings.Instance.SelectedUserIconSku = button.SKU;
        }
    }
}