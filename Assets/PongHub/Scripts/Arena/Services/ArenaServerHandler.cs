// Copyright (c) MagnusLab Inc. and affiliates.

using Unity.Netcode;

namespace PongHub.Arena.Services
{
    /// <summary>
    /// Handles client disconnect from the arena.
    /// </summary>
    public class ArenaServerHandler : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (clientId == OwnerClientId)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
            else
            {
                ArenaSessionManager.Instance.DisconnectClient(clientId);
            }
        }
    }
}