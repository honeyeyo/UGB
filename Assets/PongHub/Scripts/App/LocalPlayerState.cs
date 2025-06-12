// Copyright (c) MagnusLab Inc. and affiliates.

using System;
#if UNITY_EDITOR
using System.Security.Cryptography;
#endif
using System.Text;
using Meta.Utilities;
using UnityEngine;

namespace PongHub.App
{
    /// <summary>
    /// 本地玩家状态管理类，继承自Singleton。
    /// 用于跟踪和管理可以从任何地方访问的本地玩家状态。
    /// 这些是游戏特定的玩家状态。
    /// </summary>
    public class LocalPlayerState : Singleton<LocalPlayerState>
    {
        /// <summary>
        /// 应用程序ID
        /// </summary>
        [SerializeField] private string m_applicationID;

        /// <summary>
        /// 当玩家状态发生变化时触发的事件
        /// </summary>
        public event Action OnChange;

        /// <summary>
        /// 当生成猫咪状态发生变化时触发的事件
        /// </summary>
        public event Action OnSpawnCatChange;

        /// <summary>
        /// 玩家用户名
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// 玩家用户ID
        /// </summary>
        public ulong UserId { get; private set; }

        /// <summary>
        /// 获取应用程序ID
        /// </summary>
        public string ApplicationID => m_applicationID;

        /// <summary>
        /// 玩家唯一标识符
        /// </summary>
        public string PlayerUid { get; private set; }

        /// <summary>
        /// 是否使用自定义应用程序ID
        /// </summary>
        public bool HasCustomAppId { get; private set; }

        /// <summary>
        /// 是否为观战者
        /// </summary>
        public bool IsSpectator { get; set; }

        /// <summary>
        /// 获取用户图标SKU
        /// </summary>
        public string UserIconSku => GameSettings.Instance.SelectedUserIconSku;

        /// <summary>
        /// 是否在游戏中生成猫咪
        /// </summary>
        private bool m_spawnCatInGame;

        /// <summary>
        /// 是否在下一场游戏中生成猫咪
        /// </summary>
        public bool SpawnCatInNextGame
        {
            get => m_spawnCatInGame;
            set
            {
                m_spawnCatInGame = value;
                OnSpawnCatChange?.Invoke();
            }
        }

        /// <summary>
        /// 当对象启用时调用
        /// </summary>
        private new void OnEnable()
        {
            base.OnEnable();
            DontDestroyOnLoad(this);
        }

        /// <summary>
        /// 内部初始化方法
        /// </summary>
        protected override void InternalAwake()
        {
            base.InternalAwake();

            HasCustomAppId = true;
            if (string.IsNullOrEmpty(m_applicationID))
            {
                HasCustomAppId = false;
                // 暂时强制生成唯一的会话ID
                m_applicationID = GenerateApplicationID();
            }

            PlayerUid = PlayerPrefs.GetString("PlayerUid", GeneratePlayerID());
            PlayerPrefs.SetString("PlayerUid", PlayerUid);
#if UNITY_EDITOR
            // 当使用多个编辑器打开同一项目时，需要基于项目位置附加唯一ID
            // 因为每个编辑器实例的项目位置都是唯一的
            var hashedBytes = new MD5CryptoServiceProvider()
                .ComputeHash(Encoding.UTF8.GetBytes(Application.dataPath));
            Array.Resize(ref hashedBytes, 16);
            PlayerUid += new Guid(hashedBytes).ToString("N");
#endif
        }

        /// <summary>
        /// 设置应用程序ID
        /// </summary>
        /// <param name="applicationId">应用程序ID</param>
        public void SetApplicationID(string applicationId)
        {
            m_applicationID = applicationId;
            HasCustomAppId = !string.IsNullOrWhiteSpace(applicationId);
        }

        /// <summary>
        /// 初始化玩家信息
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="userId">用户ID</param>
        public void Init(string username, ulong userId)
        {
            Username = username;
            UserId = userId;
            OnChange?.Invoke();
        }

        /// <summary>
        /// 生成应用程序ID
        /// </summary>
        /// <returns>生成的应用程序ID</returns>
        private string GenerateApplicationID()
        {
            var id = (uint)(UnityEngine.Random.value * uint.MaxValue);
            return id.ToString("X").ToLower();
        }

        /// <summary>
        /// 生成唯一的玩家ID
        /// </summary>
        /// <returns>生成的玩家ID</returns>
        private string GeneratePlayerID()
        {
            return Guid.NewGuid().ToString();
        }
    }
}