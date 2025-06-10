// Copyright (c) Meta Platforms, Inc. and affiliates.

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Meta.Utilities
{
    /// <summary>
    /// 网络设置管理器
    /// 保存和检索编辑器网络设置，用于更快的编辑器测试，
    /// 无需更改预制件和场景即可调整网络参数
    /// </summary>
    public static class NetworkSettings
    {
        /// <summary>
        /// 自动启动设置的EditorPrefs键名
        /// </summary>
        private static readonly string s_autostartKey = $"{Application.productName}.NetworkSettingsToolbar.Autostart";

        /// <summary>
        /// 使用设备房间设置的EditorPrefs键名
        /// </summary>
        private static readonly string s_useDeviceRoomKey = $"{Application.productName}.NetworkSettingsToolbar.UseDeviceRoom";

        /// <summary>
        /// 房间名称设置的EditorPrefs键名
        /// </summary>
        private static readonly string s_roomNameKey = $"{Application.productName}.NetworkSettingsToolbar.RoomName";

        /// <summary>
        /// 是否自动启动网络连接
        /// 从EditorPrefs中读取和保存设置
        /// </summary>
        public static bool Autostart
        {
            get => EditorPrefs.GetBool(s_autostartKey);
            set => EditorPrefs.SetBool(s_autostartKey, value);
        }

        /// <summary>
        /// 是否使用设备房间
        /// 从EditorPrefs中读取和保存设置
        /// </summary>
        public static bool UseDeviceRoom
        {
            get => EditorPrefs.GetBool(s_useDeviceRoomKey);
            set => EditorPrefs.SetBool(s_useDeviceRoomKey, value);
        }

        /// <summary>
        /// 房间名称
        /// 从EditorPrefs中读取和保存设置
        /// </summary>
        public static string RoomName
        {
            get => EditorPrefs.GetString(s_roomNameKey);
            set => EditorPrefs.SetString(s_roomNameKey, value);
        }
    }
}

#else

namespace Meta.Utilities
{
    /// <summary>
    /// 网络设置管理器（运行时版本）
    /// 在非编辑器环境下，所有属性都抛出NotImplementedException
    /// </summary>
    public static class NetworkSettings
    {
        /// <summary>
        /// 是否自动启动网络连接（运行时不支持）
        /// </summary>
        public static bool Autostart => throw new System.NotImplementedException();

        /// <summary>
        /// 是否使用设备房间（运行时不支持）
        /// </summary>
        public static bool UseDeviceRoom => throw new System.NotImplementedException();

        /// <summary>
        /// 房间名称（运行时不支持）
        /// </summary>
        public static string RoomName => throw new System.NotImplementedException();
    }
}

#endif
