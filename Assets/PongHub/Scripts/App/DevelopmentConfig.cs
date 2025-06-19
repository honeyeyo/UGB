using UnityEngine;
using System.Linq;

namespace PongHub.App
{
    /// <summary>
    /// 开发模式配置管理
    /// 集中管理开发环境相关的设置和检查
    /// </summary>
    public static class DevelopmentConfig
    {
        /// <summary>
        /// 检查是否在开发环境中运行
        /// </summary>
        public static bool IsDevelopmentBuild
        {
            get
            {
                return Debug.isDebugBuild || Application.isEditor;
            }
        }

        /// <summary>
        /// 是否启用Oculus Platform开发模式
        /// </summary>
        public static bool EnableOculusPlatformDevelopmentMode
        {
            get
            {
                // 在编辑器或开发构建中启用
                return IsDevelopmentBuild;
            }
        }

        /// <summary>
        /// 是否跳过Oculus权限验证
        /// </summary>
        public static bool SkipOculusEntitlementCheck
        {
            get
            {
                return IsDevelopmentBuild;
            }
        }

        /// <summary>
        /// 开发模式默认用户名
        /// </summary>
        public static string DevelopmentUserName
        {
            get
            {
                return $"开发用户_{System.Environment.UserName}";
            }
        }

        /// <summary>
        /// 开发模式默认用户ID
        /// </summary>
        public static ulong DevelopmentUserId
        {
            get
            {
                // 基于用户名生成一个一致的ID
                string userIdString = $"dev_user_{System.Environment.UserName}";
                return (ulong)System.Math.Abs(userIdString.GetHashCode());
            }
        }

        /// <summary>
        /// 可以在开发环境中忽略的Oculus错误代码
        /// </summary>
        public static readonly int[] IgnorableOculusErrorCodes = new int[]
        {
            1971051, // Must call get_signature first
            1971031, // Missing entitlement
            100,     // Unknown error
            1,       // General error
            -1,      // HTTP error
        };

        /// <summary>
        /// 检查指定的错误代码是否可以在开发环境中忽略
        /// </summary>
        public static bool ShouldIgnoreOculusError(int errorCode)
        {
            if (!IsDevelopmentBuild) return false;

            return IgnorableOculusErrorCodes.Contains(errorCode);
        }

        /// <summary>
        /// 记录开发模式信息
        /// </summary>
        public static void LogDevelopmentMode(string message)
        {
            if (IsDevelopmentBuild)
            {
                Debug.Log($"[开发模式] {message}");
            }
        }

        /// <summary>
        /// 记录开发模式警告
        /// </summary>
        public static void LogDevelopmentWarning(string message)
        {
            if (IsDevelopmentBuild)
            {
                Debug.LogWarning($"[开发模式] {message}");
            }
        }

        /// <summary>
        /// 记录开发模式错误
        /// </summary>
        public static void LogDevelopmentError(string message)
        {
            if (IsDevelopmentBuild)
            {
                Debug.LogError($"[开发模式] {message}");
            }
        }
    }
}