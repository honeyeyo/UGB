// Copyright (c) MagnusLab Inc. and affiliates.

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