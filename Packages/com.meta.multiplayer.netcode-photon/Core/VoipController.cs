// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Photon.Voice.Unity;
using Photon.Voice.Unity.UtilityScripts;
using Unity.Netcode;
using UnityEngine;
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
using UnityEngine.Android;
#endif

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// 控制Photon Voice的语音IP设置
    /// 包括麦克风权限要求和语音组件的创建管理
    /// </summary>
    public class VoipController : MonoBehaviour
    {
        /// <summary>
        /// 语音扬声器预制体
        /// 用于创建远程玩家语音播放器的预制体
        /// </summary>
        [SerializeField] private Speaker m_voipSpeakerPrefab;

        /// <summary>
        /// 语音连接预制体
        /// 用于创建本地玩家语音录制器的预制体
        /// </summary>
        [SerializeField] private VoiceConnection m_voipRecorderPrefab;

        /// <summary>
        /// 头部高度
        /// 语音扬声器相对于玩家位置的高度偏移
        /// </summary>
        [SerializeField] private float m_headHeight = 1.0f;

        /// <summary>
        /// 本地语音录制器实例
        /// 当前本地玩家的语音录制器
        /// </summary>
        private VoiceConnection m_localVoipRecorder;

        /// <summary>
        /// 启用时的初始化
        /// 确保语音控制器在场景切换时不被销毁
        /// </summary>
        private void OnEnable()
        {
            DontDestroyOnLoad(this);
        }

        /// <summary>
        /// 启动时请求麦克风权限
        /// 在非编辑器和非Windows平台上请求麦克风使用权限
        /// </summary>
        private void Start()
        {
            // 在启动时请求麦克风权限
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
#endif
        }

        /// <summary>
        /// 启动VOIP功能
        /// 为指定的父对象创建和配置语音系统
        /// </summary>
        /// <param name="parent">语音组件的父变换，通常是玩家对象</param>
        public void StartVoip(Transform parent)
        {
            // 创建本地语音录制器实例
            m_localVoipRecorder = Instantiate(m_voipRecorderPrefab, parent);

            // 如果父对象有VoipHandler组件，设置录制器
            if (parent.gameObject.TryGetComponent<VoipHandler>(out var voipHandler))
            {
                voipHandler.SetRecorder(m_localVoipRecorder);
            }

            // 将录制器附加到有网络对象的玩家实体上，以便引用网络ID
            var networkObject = parent.gameObject.GetComponentInParent<NetworkObject>();

            // 设置Photon玩家的自定义属性，包含网络对象ID
            _ = m_localVoipRecorder.Client.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
            {
                [nameof(NetworkObject.NetworkObjectId)] = (int)networkObject.NetworkObjectId,
            });

            // 设置扬声器工厂函数，用于创建远程玩家的扬声器
            m_localVoipRecorder.SpeakerFactory = (playerId, voiceId, userData) => CreateSpeaker(playerId, m_localVoipRecorder);

            // 启动加入Photon语音房间的协程
            _ = StartCoroutine(JoinPhotonVoiceRoom());
        }

        /// <summary>
        /// 创建扬声器
        /// 为远程玩家创建语音扬声器组件
        /// </summary>
        /// <param name="playerId">Photon玩家ID</param>
        /// <param name="voiceConnection">语音连接组件</param>
        /// <returns>创建的扬声器组件</returns>
        private Speaker CreateSpeaker(int playerId, VoiceConnection voiceConnection)
        {
            // 获取Photon玩家对象
            var actor = voiceConnection.Client.LocalPlayer.Get(playerId);
            Debug.Assert(actor != null, $"Could not find voice client for Player #{playerId}");

            // 从玩家自定义属性中获取网络对象ID
            _ = actor.CustomProperties.TryGetValue(nameof(NetworkObject.NetworkObjectId), out var networkId);
            Debug.Assert(networkId != null, $"Could not find network object id for Player #{playerId}");

            // 根据网络对象ID查找玩家实例
            _ = NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue((ulong)(int)networkId, out var player);
            Debug.Assert(player != null, $"Could not find player instance for Player #{playerId} network id #{networkId}");

            // 在玩家对象上创建扬声器
            var speaker = Instantiate(m_voipSpeakerPrefab, player.transform);

            // 如果玩家有VoipHandler组件，设置扬声器
            if (player.TryGetComponent<VoipHandler>(out var voipHandler))
            {
                voipHandler.SetSpeaker(speaker);
            }

            // 设置扬声器在头部高度的位置
            speaker.transform.localPosition = new Vector3(0.0f, m_headHeight, 0.0f);

            return speaker;
        }

        /// <summary>
        /// 加入Photon语音房间协程
        /// 等待语音房间名称设置完成后加入语音房间
        /// </summary>
        /// <returns>协程枚举器</returns>
        private IEnumerator JoinPhotonVoiceRoom()
        {
            // 等待语音房间名称设置完成且录制器准备好
            yield return new WaitUntil(() => NetworkSession.PhotonVoiceRoom != "" && m_localVoipRecorder != null);

            // 只有在可以录制语音时才加入房间
            if (CanRecordVoice())
            {
                // 获取连接和加入组件
                var connectAndJoin = m_localVoipRecorder.GetComponent<ConnectAndJoin>();

                // 设置房间名称并连接
                connectAndJoin.RoomName = NetworkSession.PhotonVoiceRoom;
                connectAndJoin.ConnectNow();
            }
        }

        /// <summary>
        /// 检查是否可以录制语音
        /// 验证麦克风权限是否已获得
        /// </summary>
        /// <returns>如果可以录制语音返回true，否则返回false</returns>
        private bool CanRecordVoice()
        {
            // 只有在获得麦克风权限时才录制语音
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
            return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
            // 在编辑器和Windows平台默认返回true
            return true;
#endif
        }
    }
}