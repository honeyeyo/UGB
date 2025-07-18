// Copyright (c) MagnusLab Inc. and affiliates.

#if !(UNITY_EDITOR || UNITY_STANDALONE_WIN)
using Oculus.Platform;
#endif
using System.Collections.Generic;
using PongHub.App;
using UnityEngine;
using UnityEngine.Serialization;

namespace PongHub.Arena.Player.Menu
{
    /// <summary>
    /// In game menu that shows information of the current players in the game.
    /// List of all player using the PlayerInfoItem and a button to open the roster panel.
    /// https://developer.oculus.com/documentation/unity/ps-roster/
    /// </summary>
    public class PlayersMenu : BasePlayerMenuView
    {
        [FormerlySerializedAs("playerInfoItemRef")]
        [SerializeField]
        [Tooltip("Player Info Item Reference / 玩家信息项引用 - Template for player info items in the list")]
        private PlayerInfoItem m_playerInfoItemRef;

        private List<PlayerInfoItem> m_playerInfoItems = new();
        private void Awake()
        {
            m_playerInfoItemRef.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            var allCurrentPlayerStates = FindObjectsOfType<PlayerStateNetwork>();
            var itemIndex = 0;
            for (var i = 0; i < allCurrentPlayerStates.Length; ++i)
            {
                var playerState = allCurrentPlayerStates[i];
                // Don't show our own player
                if (playerState.UserId == LocalPlayerState.Instance.UserId)
                {
                    continue;
                }

                PlayerInfoItem playerInfoItem;
                if (m_playerInfoItems.Count > itemIndex)
                {
                    playerInfoItem = m_playerInfoItems[itemIndex];
                }
                else
                {
                    playerInfoItem = Instantiate(m_playerInfoItemRef, m_playerInfoItemRef.transform.parent);
                    m_playerInfoItems.Add(playerInfoItem);
                }
                itemIndex++;
                playerInfoItem.gameObject.SetActive(true);
                playerInfoItem.SetupUser(playerState);
            }

            // hide all other player info
            for (var i = itemIndex; i < m_playerInfoItems.Count; ++i)
            {
                m_playerInfoItems[i].gameObject.SetActive(false);
            }
        }

        public void OnRosterButtonClicked()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            Debug.Log("Roster Button Clicked");
#else
            GroupPresence.LaunchRosterPanel(new RosterOptions());
#endif
        }
    }
}