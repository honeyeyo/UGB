// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Meta.Utilities
{
    /// <summary>
    /// Android平台辅助工具类
    /// 提供Android平台特有的功能，如读取Intent参数等
    /// 仅在Android平台运行时有效，编辑器模式下返回默认值
    /// </summary>
    public static class AndroidHelpers
    {
        /// <summary>
        /// Android Intent对象的引用
        /// 用于获取启动应用时的Intent参数
        /// </summary>
        private static AndroidJavaObject s_intent;

        /// <summary>
        /// 静态构造函数
        /// 在Android平台上初始化Intent对象，获取当前Activity的Intent
        /// </summary>
        static AndroidHelpers()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        // 获取Unity Player类
        var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        // 获取当前Activity
        var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
        // 获取启动Intent
        s_intent = activity.Call<AndroidJavaObject>("getIntent");
#endif
        }

        /// <summary>
        /// 检查Intent是否包含指定的额外参数
        /// </summary>
        /// <param name="argumentName">参数名称</param>
        /// <returns>如果包含指定参数则返回true，否则返回false</returns>
        public static bool HasIntentExtra(string argumentName) => s_intent?.Call<bool>("hasExtra", argumentName) ?? false;

        /// <summary>
        /// 获取Intent中的字符串类型额外参数
        /// </summary>
        /// <param name="extraName">参数名称</param>
        /// <returns>参数值，如果不存在则返回null</returns>
        public static string GetStringIntentExtra(string extraName) =>
            HasIntentExtra(extraName) ?
                s_intent.Call<string>("getStringExtra", extraName) :
                null;

        /// <summary>
        /// 获取Intent中的浮点数类型额外参数
        /// </summary>
        /// <param name="extraName">参数名称</param>
        /// <returns>参数值，如果不存在则返回null</returns>
        public static float? GetFloatIntentExtra(string extraName) =>
            HasIntentExtra(extraName) ?
                s_intent.Call<float>("getFloatExtra", extraName, 0.0f) :
                null;
    }
}
