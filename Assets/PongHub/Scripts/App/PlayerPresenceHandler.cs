// Copyright (c) MagnusLab Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using Meta.Multiplayer.Core;
using Oculus.Platform;
using UnityEngine;

namespace PongHub.App
{
    /// <summary>
    /// Handles the current user presence.
    /// Loads and keeps track of the Destinations through the RichPresence API and extract the deeplink message.
    /// Generate and exposes the current group presence state.
    /// </summary>
    public class PlayerPresenceHandler
    {
        private bool m_destinationReceived;
        private readonly Dictionary<string, string> m_destinationsAPIToDisplayName = new();
        private readonly Dictionary<string, string> m_destinationsAPIToRegion = new();
        private readonly Dictionary<string, string> m_regionToDestinationAPI = new();

        public GroupPresenceState GroupPresenceState { get; private set; }

        public IEnumerator Init()
        {
            // 检查是否在开发环境
            if (DevelopmentConfig.EnableOculusPlatformDevelopmentMode)
            {
                DevelopmentConfig.LogDevelopmentMode("PlayerPresenceHandler: 开发模式 - 模拟destinations数据");
                InitializeDevelopmentDestinations();
                m_destinationReceived = true;
                yield break;
            }

            // 正常模式：从Oculus Platform获取destinations
            _ = RichPresence.GetDestinations().OnComplete(OnGetDestinations);
            yield return new WaitUntil(() => m_destinationReceived);
        }

        /// <summary>
        /// 开发模式下初始化模拟的destinations数据
        /// </summary>
        private void InitializeDevelopmentDestinations()
        {
            // 模拟一些基本的destinations
            m_destinationsAPIToDisplayName["MainMenu"] = "主菜单";
            m_destinationsAPIToDisplayName["Arena"] = "竞技场";
            m_destinationsAPIToDisplayName["Arena_USW"] = "竞技场 (美西)";
            m_destinationsAPIToDisplayName["Arena_USE"] = "竞技场 (美东)";
            m_destinationsAPIToDisplayName["Arena_EU"] = "竞技场 (欧洲)";
            m_destinationsAPIToDisplayName["Arena_ASIA"] = "竞技场 (亚洲)";

            // 模拟区域映射
            m_destinationsAPIToRegion["Arena"] = "usw";
            m_destinationsAPIToRegion["Arena_USW"] = "usw";
            m_destinationsAPIToRegion["Arena_USE"] = "use";
            m_destinationsAPIToRegion["Arena_EU"] = "eu";
            m_destinationsAPIToRegion["Arena_ASIA"] = "asia";

            // 模拟区域到destination的映射
            m_regionToDestinationAPI["usw"] = "Arena_USW";
            m_regionToDestinationAPI["use"] = "Arena_USE";
            m_regionToDestinationAPI["eu"] = "Arena_EU";
            m_regionToDestinationAPI["asia"] = "Arena_ASIA";

            DevelopmentConfig.LogDevelopmentMode("PlayerPresenceHandler: 开发模式destinations数据已初始化");
        }

        public IEnumerator GenerateNewGroupPresence(string dest, string roomName = null)
        {
            GroupPresenceState ??= new GroupPresenceState();
            var lobbyId = string.Empty;
            var joinable = false;
            if (dest != "MainMenu")
            {
                lobbyId = roomName ?? $"Arena-{LocalPlayerState.Instance.Username}-{(uint)(Random.value * uint.MaxValue)}";
                joinable = true;
            }
            return GroupPresenceState.Set(
                dest,
                lobbyId,
                string.Empty,
                joinable
            );
        }

        // Based on the region we are connected we use the right Arena Destination API
        public string GetArenaDestinationAPI(string region)
        {
            return !m_regionToDestinationAPI.TryGetValue(region, out var destAPI) ? "Arena" : destAPI;
        }

        public string GetDestinationDisplayName(string destinationAPI)
        {
            if (!m_destinationsAPIToDisplayName.TryGetValue(destinationAPI, out var displayName))
            {
                displayName = destinationAPI;
            }

            return displayName;
        }

        public string GetRegionFromDestination(string destinationAPI)
        {
            if (!m_destinationsAPIToRegion.TryGetValue(destinationAPI, out var region))
            {
                region = "usw";
            }
            return region;
        }

        private void OnGetDestinations(Message<Oculus.Platform.Models.DestinationList> message)
        {
            if (message.IsError)
            {
                LogError("Could not get the list of destinations!", message.GetError());
            }
            else
            {
                foreach (var destination in message.Data)
                {
                    var apiName = destination.ApiName;
                    m_destinationsAPIToDisplayName[apiName] = destination.DisplayName;
                    // For Arenas we detect what region they are in by betting the region in the deeplink message
                    if (apiName.StartsWith("Arena"))
                    {
                        var msg = JsonUtility.FromJson<ArenaDeepLinkMessage>(destination.DeeplinkMessage);
                        m_destinationsAPIToRegion[apiName] = msg.Region;
                        if (!string.IsNullOrEmpty(msg.Region))
                        {
                            m_regionToDestinationAPI[msg.Region] = apiName;
                        }
                    }
                }
            }
            m_destinationReceived = true;
        }

        private void LogError(string message, Oculus.Platform.Models.Error error)
        {
            Debug.LogError($"{message}" +
                           $"ERROR MESSAGE:   {error.Message}" +
                           $"ERROR CODE:      {error.Code}" +
                           $"ERROR HTTP CODE: {error.HttpCode}");
        }
    }
}