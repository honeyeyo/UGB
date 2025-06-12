// Copyright (c) MagnusLab Inc. and affiliates.

using System;
using System.Collections.Generic;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;

namespace PongHub.App
{
    /// <summary>
    /// 应用内购买管理器
    /// 封装了Oculus.Platform.IAP的功能,简化了商品和购买记录的获取以及购买流程
    /// 参考文档: https://developer.oculus.com/documentation/unity/ps-iap/
    /// </summary>
    public class IAPManager
    {
        #region Singleton
        /// <summary>
        /// 单例实例
        /// </summary>
        private static IAPManager s_instance;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static IAPManager Instance
        {
            get
            {
                s_instance ??= new IAPManager();
                return s_instance;
            }
        }

        /// <summary>
        /// 在子系统注册时销毁实例
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void DestroyInstance()
        {
            s_instance = null;
        }
        #endregion // Singleton

        /// <summary>
        /// 购买错误消息的数据结构
        /// 从购买时的错误消息JSON字符串中获取
        /// </summary>
        private class PurchaseErrorMessage
        {
            // 由于从JSON转换,命名必须与JSON格式保持一致(全小写)
#pragma warning disable IDE1006
            // ReSharper disable once InconsistentNaming
            public string category; // 错误类别
            // ReSharper disable once InconsistentNaming
            public int code; // 错误代码
            // ReSharper disable once InconsistentNaming
            public string message; // 错误消息
#pragma warning restore IDE1006
        }

        /// <summary>
        /// 商品字典,key为商品SKU
        /// </summary>
        private Dictionary<string, Product> m_products = new();

        /// <summary>
        /// 购买记录字典,key为商品SKU
        /// </summary>
        private Dictionary<string, Purchase> m_purchases = new();

        /// <summary>
        /// 按类别分组的商品SKU字典
        /// </summary>
        private Dictionary<string, List<string>> m_productsByCategory = new();

        /// <summary>
        /// 可用商品SKU列表
        /// </summary>
        private List<string> m_availableSkus = new();

        /// <summary>
        /// 获取可用商品SKU列表
        /// </summary>
        public IList<string> AvailableSkus => m_availableSkus;

        /// <summary>
        /// 异步获取指定SKU的商品信息
        /// </summary>
        /// <param name="skus">商品SKU数组</param>
        /// <param name="category">商品类别(可选)</param>
        public void FetchProducts(string[] skus, string category = null)
        {
            _ = IAP.GetProductsBySKU(skus).OnComplete(message =>
            {
                GetProductsBySKUCallback(message, category);
            });
        }

        /// <summary>
        /// 异步获取用户的所有购买记录
        /// </summary>
        public void FetchPurchases()
        {
            _ = IAP.GetViewerPurchases().OnComplete(GetViewerPurchasesCallback);
        }

        /// <summary>
        /// 获取指定类别的所有商品SKU
        /// </summary>
        /// <param name="category">商品类别</param>
        /// <returns>商品SKU列表,如果类别不存在则返回null</returns>
        public List<string> GetProductSkusForCategory(string category)
        {
            return m_productsByCategory.TryGetValue(category, out var categorySkus) ? categorySkus : null;
        }

        /// <summary>
        /// 获取指定SKU的商品信息
        /// </summary>
        /// <param name="sku">商品SKU</param>
        /// <returns>商品信息,如果商品不存在则返回null</returns>
        public Product GetProduct(string sku)
        {
            if (m_products.TryGetValue(sku, out var product))
            {
                return product;
            }

            Debug.LogError($"[IAPManager] Product {sku} doesn't exist!");
            return null;
        }

        /// <summary>
        /// 检查指定SKU的商品是否已购买
        /// </summary>
        /// <param name="sku">商品SKU</param>
        /// <returns>是否已购买</returns>
        public bool IsPurchased(string sku)
        {
            return m_purchases.TryGetValue(sku, out _);
        }

        /// <summary>
        /// 获取指定SKU的购买记录
        /// </summary>
        /// <param name="sku">商品SKU</param>
        /// <returns>购买记录,如果未购买则返回null</returns>
        public Purchase GetPurchase(string sku)
        {
            return m_purchases.TryGetValue(sku, out var purchase) ? purchase : null;
        }

        /// <summary>
        /// 购买指定SKU的商品
        /// </summary>
        /// <param name="sku">商品SKU</param>
        /// <param name="onPurchaseFlowCompleted">购买流程完成回调</param>
        public void Purchase(string sku, Action<string, bool, string> onPurchaseFlowCompleted)
        {
#if UNITY_EDITOR
            // 在编辑器中无法创建购买记录,但需要跟踪购买状态
            m_purchases[sku] = null;
            onPurchaseFlowCompleted?.Invoke(sku, true, null);
#else
            IAP.LaunchCheckoutFlow(sku).OnComplete((Message<Purchase> msg) =>
            {
                if (msg.IsError)
                {
                    var errorMsgString = msg.GetError().Message;
                    Debug.LogError($"[IAPManager] Error while purchasing: {errorMsgString}");
                    var errorData = JsonUtility.FromJson<PurchaseErrorMessage>(errorMsgString);
                    onPurchaseFlowCompleted?.Invoke(sku, false, errorData.message);
                    return;
                }

                var p = msg.GetPurchase();
                Debug.Log("[IAPManager] Purchased " + p.Sku);
                m_purchases[sku] = p;
                onPurchaseFlowCompleted?.Invoke(sku, true, null);
            });
#endif
        }

        /// <summary>
        /// 消费指定SKU的购买记录
        /// </summary>
        /// <param name="sku">商品SKU</param>
        /// <param name="onConsumptionCompleted">消费完成回调</param>
        public void ConsumePurchase(string sku, Action<string, bool> onConsumptionCompleted)
        {
#if UNITY_EDITOR
            m_purchases.Remove(sku);
            onConsumptionCompleted?.Invoke(sku, true);
#else
            _ = IAP.ConsumePurchase(sku).OnComplete(msg =>
            {
                if (msg.IsError)
                {
                    Debug.LogError($"[IAPManager] Error while consuming: {msg.GetError().Message}");
                    onConsumptionCompleted?.Invoke(sku, false);
                    return;
                }

                Debug.Log("[IAPManager] Consumed " + sku);
                m_purchases.Remove(sku);
                onConsumptionCompleted?.Invoke(sku, true);
            });
#endif
        }

        /// <summary>
        /// 获取商品信息的回调处理
        /// </summary>
        /// <param name="msg">商品列表消息</param>
        /// <param name="category">商品类别</param>
        private void GetProductsBySKUCallback(Message<ProductList> msg, string category)
        {
            if (msg.IsError)
            {
                Debug.LogError($"[IAPManager] Failed to fetch products, {msg.GetError().Message}");
                return;
            }

            foreach (var p in msg.GetProductList())
            {
                Debug.LogFormat("[IAPManager] Product: sku:{0} name:{1} price:{2}", p.Sku, p.Name, p.FormattedPrice);
                m_products[p.Sku] = p;
                m_availableSkus.Add(p.Sku);
                if (!string.IsNullOrWhiteSpace(category))
                {
                    if (!m_productsByCategory.TryGetValue(category, out var categorySkus))
                    {
                        categorySkus = new List<string>();
                        m_productsByCategory[category] = categorySkus;
                    }

                    categorySkus.Add(p.Sku);
                }
            }
        }

        /// <summary>
        /// 获取购买记录的回调处理
        /// </summary>
        /// <param name="msg">购买记录列表消息</param>
        private void GetViewerPurchasesCallback(Message<PurchaseList> msg)
        {
            if (msg.IsError)
            {
                Debug.LogError($"[IAPManager] Failed to fetch purchased products, {msg.GetError().Message}");
                return;
            }

            foreach (var p in msg.GetPurchaseList())
            {
                Debug.Log($"[IAPManager] Purchased: sku:{p.Sku} granttime:{p.GrantTime} id:{p.ID}");
                m_purchases[p.Sku] = p;
            }
        }
    }
}