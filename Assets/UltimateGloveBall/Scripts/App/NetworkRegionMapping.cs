// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System.Collections.Generic;

namespace PongHub.App
{
    /// <summary>
    /// 网络区域映射静态类
    /// 将Photon区域标识符映射到用户可读的名称
    /// 提供区域的显示名称和简短名称的转换功能
    /// </summary>
    public static class NetworkRegionMapping
    {
        /// <summary>
        /// 区域映射字典
        /// 键：Photon区域代码，值：用户友好的区域名称
        /// </summary>
        private static readonly Dictionary<string, string> s_regionMap = new()
        {
            {"usw", "North America"},     // 美国西部 -> 北美
            {"eu", "Europe"},             // 欧洲
            {"jp", "Japan"},              // 日本
            {"sa", "South America"},      // 南美洲
            {"asia", "Asia"},             // 亚洲
            {"au", "Australia"},          // 澳大利亚
        };

        /// <summary>
        /// 获取区域的显示名称
        /// 根据区域键返回用户友好的区域名称
        /// </summary>
        /// <param name="regionKey">Photon区域代码</param>
        /// <returns>用户友好的区域名称，如果找不到映射则返回原始键</returns>
        public static string GetRegionName(string regionKey)
        {
            // 尝试从映射字典中获取区域名称
            _ = s_regionMap.TryGetValue(regionKey, out var name);

            // 如果没有找到映射或名称为空，使用原始键作为名称
            if (string.IsNullOrEmpty(name))
            {
                name = regionKey;
            }

            return name;
        }

        /// <summary>
        /// 获取区域的简短名称
        /// 返回区域的简短标识符用于UI显示
        /// </summary>
        /// <param name="regionKey">Photon区域代码</param>
        /// <returns>区域的简短名称（大写）</returns>
        public static string GetRegionShortName(string regionKey)
        {
            // 特殊处理美国西部，显示为NA（北美）
            // 其他区域转换为大写显示
            return regionKey == "usw" ? "NA" : regionKey.ToUpper();
        }
    }
}