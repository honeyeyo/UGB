// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

namespace PongHub.App
{
    /// <summary>
    /// 产品类别静态类
    /// 包含易于使用的产品和产品类别字符串常量
    /// 用于应用内购买和商店系统的产品分类
    /// </summary>
    public static class ProductCategories
    {
        // 产品类别常量

        /// <summary>
        /// 图标类别
        /// 用于用户头像图标相关的产品
        /// </summary>
        public const string ICONS = "Icons";

        /// <summary>
        /// 消耗品类别
        /// 用于一次性使用的产品，如道具、货币等
        /// </summary>
        public const string CONSUMABLES = "Consumables";

        // 具体产品SKU常量

        /// <summary>
        /// 猫咪产品的SKU标识符
        /// 用于购买和管理猫咪相关的消耗品
        /// </summary>
        public const string CAT = "cat";
    }
}