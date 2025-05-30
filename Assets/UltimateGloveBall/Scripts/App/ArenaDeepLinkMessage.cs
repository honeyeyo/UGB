// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

namespace PongHub.App
{
    /// <summary>
    /// 竞技场深度链接消息类
    /// 用于反序列化从竞技场目标收到的深度链接消息
    /// 当用户通过邀请或深度链接加入游戏时，会接收到此类型的消息
    /// </summary>
    public class ArenaDeepLinkMessage
    {
        /// <summary>
        /// 区域信息
        /// 指定用户要加入的服务器区域
        /// </summary>
        public string Region;
    }
}