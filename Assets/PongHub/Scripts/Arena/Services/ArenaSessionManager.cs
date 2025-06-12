// Copyright (c) MagnusLab Inc. and affiliates.

using System.Collections.Generic;
using Meta.Utilities;
using UnityEngine;

namespace PongHub.Arena.Services
{
    /// <summary>
    /// 竞技场会话管理器
    /// 管理当前竞技场会话,跟踪每个连接客户端的玩家数据。
    /// 通过保存每个玩家的信息,ArenaSpawningManager可以跟踪重新连接的玩家。
    /// </summary>
    public class ArenaSessionManager : Singleton<ArenaSessionManager>
    {
        /// <summary>
        /// 存储玩家ID和玩家数据的映射字典
        /// </summary>
        private readonly Dictionary<string, ArenaPlayerData> m_playerDataDict;

        /// <summary>
        /// 存储客户端ID和玩家ID的映射字典
        /// </summary>
        private readonly Dictionary<ulong, string> m_clientIdToPlayerId;

        /// <summary>
        /// 构造函数,初始化字典
        /// </summary>
        public ArenaSessionManager()
        {
            m_playerDataDict = new Dictionary<string, ArenaPlayerData>();
            m_clientIdToPlayerId = new Dictionary<ulong, string>();
        }

        /// <summary>
        /// 设置玩家数据
        /// </summary>
        /// <param name="clientId">客户端ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="playerData">玩家数据</param>
        public void SetupPlayerData(ulong clientId, string playerId, ArenaPlayerData playerData)
        {
            var isReconnecting = false;
            if (IsDuplicateConnection(playerId))
            {
                Debug.LogError($"Player Already in game: {playerId}");
                // 玩家已连接
                return;
            }

            // 检查是否为重新连接的玩家
            if (m_playerDataDict.ContainsKey(playerId))
            {
                if (!m_playerDataDict[playerId].IsConnected)
                {
                    // 如果连接的客户端与断开连接的客户端有相同的玩家ID,则为重新连接
                    isReconnecting = true;
                }
            }

            if (isReconnecting)
            {
                playerData = m_playerDataDict[playerId];
                playerData.ClientId = clientId;
                playerData.IsConnected = true;
            }

            // 更新字典
            m_clientIdToPlayerId[clientId] = playerId;
            m_playerDataDict[playerId] = playerData;
        }

        /// <summary>
        /// 检查是否为重复连接
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>如果玩家已连接则返回true</returns>
        public bool IsDuplicateConnection(string playerId)
        {
            return m_playerDataDict.ContainsKey(playerId) && m_playerDataDict[playerId].IsConnected;
        }

        /// <summary>
        /// 根据客户端ID获取玩家ID
        /// </summary>
        /// <param name="clientId">客户端ID</param>
        /// <returns>玩家ID,如果未找到则返回null</returns>
        public string GetPlayerId(ulong clientId)
        {
            if (m_clientIdToPlayerId.TryGetValue(clientId, out var playerId))
            {
                return playerId;
            }

            Debug.Log($"No player Id mapped to client id: {clientId}");
            return null;
        }

        /// <summary>
        /// 根据玩家ID获取玩家数据
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>玩家数据,如果未找到则返回null</returns>
        public ArenaPlayerData? GetPlayerData(string playerId)
        {
            if (m_playerDataDict.TryGetValue(playerId, out var data))
            {
                return data;
            }

            Debug.Log($"No PlayerData found for player ID: {playerId}");
            return null;
        }

        /// <summary>
        /// 根据客户端ID获取玩家数据
        /// </summary>
        /// <param name="clientId">客户端ID</param>
        /// <returns>玩家数据,如果未找到则返回null</returns>
        public ArenaPlayerData? GetPlayerData(ulong clientId)
        {
            var playerId = GetPlayerId(clientId);
            if (playerId != null)
            {
                return GetPlayerData(playerId);
            }

            Debug.LogError($"No player Id found for client ID: {clientId}");
            return null;
        }

        /// <summary>
        /// 设置玩家数据
        /// </summary>
        /// <param name="clientId">客户端ID</param>
        /// <param name="playerData">玩家数据</param>
        public void SetPlayerData(ulong clientId, ArenaPlayerData playerData)
        {
            if (m_clientIdToPlayerId.TryGetValue(clientId, out var playerId))
            {
                m_playerDataDict[playerId] = playerData;
            }
            else
            {
                Debug.LogError($"No player Id found for client ID: {clientId}");
            }
        }

        /// <summary>
        /// 断开客户端连接
        /// </summary>
        /// <param name="clientId">客户端ID</param>
        public void DisconnectClient(ulong clientId)
        {
            if (m_clientIdToPlayerId.TryGetValue(clientId, out var playerId))
            {
                if (GetPlayerData(playerId)?.ClientId == clientId)
                {
                    var clientData = m_playerDataDict[playerId];
                    clientData.IsConnected = false;
                    m_playerDataDict[playerId] = clientData;
                }
            }
        }
    }
}