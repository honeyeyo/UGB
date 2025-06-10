/// <summary>
/// Photon应用程序设置ScriptableObject - 连接相关设置的集合，由PhotonNetwork.ConnectUsingSettings内部使用
/// Collection of connection-relevant settings, used internally by PhotonNetwork.ConnectUsingSettings.
/// </summary>
/// <remarks>
/// 包括来自Realtime API的AppSettings类以及其他一些PUN相关的设置。
/// Includes the AppSettings class from the Realtime APIs plus some other, PUN-relevant, settings.
/// </remarks>
// -----------------------------------------------------------------------
// <copyright file="PhotonAppSettings.cs" company="Exit Games GmbH">
// </copyright>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

#if UNITY_2017_4_OR_NEWER
#define SUPPORTED_UNITY
#endif


#if !PHOTON_UNITY_NETWORKING

namespace Photon.Realtime
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    /// <summary>
    /// Photon应用程序设置ScriptableObject - 连接相关设置的集合，由PhotonNetwork.ConnectUsingSettings内部使用
    /// Collection of connection-relevant settings, used internally by PhotonNetwork.ConnectUsingSettings.
    /// </summary>
    /// <remarks>
    /// 包括来自Realtime API的AppSettings类以及其他一些PUN相关的设置。
    /// Includes the AppSettings class from the Realtime APIs plus some other, PUN-relevant, settings.
    /// </remarks>
    [Serializable]
    [HelpURL("https://doc.photonengine.com/en-us/pun/v2/getting-started/initial-setup")]
    public class PhotonAppSettings : ScriptableObject
    {
        [Tooltip("Core Photon Server/Cloud settings.")]
        /// <summary>核心Photon服务器/云设置 - Core Photon Server/Cloud settings</summary>
        public AppSettings AppSettings;

        #if UNITY_EDITOR
        [HideInInspector]
        /// <summary>禁用自动打开向导 - Disable auto open wizard</summary>
        public bool DisableAutoOpenWizard;
        //public bool ShowSettings;
        //public bool DevRegionSetOnce;
        #endif

        /// <summary>PhotonAppSettings单例实例 - PhotonAppSettings singleton instance</summary>
        private static PhotonAppSettings instance;

        /// <summary>
        /// 序列化的服务器设置，由设置向导编写，用于ConnectUsingSettings
        /// Serialized server settings, written by the Setup Wizard for use in ConnectUsingSettings.
        /// </summary>
        public static PhotonAppSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    LoadOrCreateSettings();
                }

                return instance;
            }

            private set { instance = value; }
        }


        /// <summary>
        /// 加载或创建设置实例
        /// Load or create settings instance
        /// </summary>
        public static void LoadOrCreateSettings()
        {
            if (instance != null)
            {
                Debug.LogWarning("Instance is not null. Will not LoadOrCreateSettings().");
                return;
            }


            #if UNITY_EDITOR
            // 让我们检查AssetDatabase是否找到文件；旨在避免创建多个文件，可能是徒劳的步骤
            // let's check if the AssetDatabase finds the file; aimed to avoid multiple files being created, potentially a futile step
            AssetDatabase.Refresh();
            #endif

            // 尝试加载资源/资产(ServerSettings也称为PhotonServerSettings)
            // try to load the resource / asset (ServerSettings a.k.a. PhotonServerSettings)
            instance = (PhotonAppSettings)Resources.Load(typeof(PhotonAppSettings).Name, typeof(PhotonAppSettings));
            if (instance != null)
            {
                //Debug.LogWarning("Settings from Resources."); // DEBUG
                return;
            }


            // 如果未加载则创建它
            // create it if not loaded
            if (instance == null)
            {
                instance = (PhotonAppSettings)CreateInstance(typeof(PhotonAppSettings));
                if (instance == null)
                {
                    Debug.LogError("Failed to create ServerSettings. PUN is unable to run this way. If you deleted it from the project, reload the Editor.");
                    return;
                }

                //Debug.LogWarning("Settings created!"); // DEBUG
            }

            // 在编辑器中，存储设置文件，因为它没有被加载
            // in the editor, store the settings file as it's not loaded
            #if UNITY_EDITOR
            string punResourcesDirectory = "Assets/Photon/Resources/";
            string serverSettingsAssetPath = punResourcesDirectory + typeof(PhotonAppSettings).Name + ".asset";
            string serverSettingsDirectory = Path.GetDirectoryName(serverSettingsAssetPath);

            if (!Directory.Exists(serverSettingsDirectory))
            {
                // 创建Photon资源目录 - Create Photon resources directory
                Directory.CreateDirectory(serverSettingsDirectory);
                AssetDatabase.ImportAsset(serverSettingsDirectory);
            }

            // 在AssetDatabase中创建和保存设置资产
            // Create and save settings asset in AssetDatabase
            AssetDatabase.CreateAsset(instance, serverSettingsAssetPath);
            AssetDatabase.SaveAssets();


            //Debug.Log("Settings stored to DB."); // DEBUG
            #endif
        }
    }
}
#endif