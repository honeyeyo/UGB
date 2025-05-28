using UnityEngine;
using System.Threading.Tasks;
using Unity.Netcode;

namespace PongHub.Networking
{
    public class NetworkManager : MonoBehaviour
    {
        private static NetworkManager s_instance;
        public static NetworkManager Instance => s_instance;

        [SerializeField] private Unity.Netcode.NetworkManager m_networkManager;
        [SerializeField] private NetworkObject[] m_networkPrefabs;

        private void Awake()
        {
            if (s_instance == null)
            {
                s_instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            if (m_networkManager == null)
            {
                m_networkManager = Unity.Netcode.NetworkManager.Singleton;
            }
        }

        public async Task InitializeAsync()
        {
            await Task.Yield();
            RegisterPrefabs();
        }

        private void RegisterPrefabs()
        {
            if (m_networkPrefabs != null && m_networkManager != null)
            {
                foreach (var prefab in m_networkPrefabs)
                {
                    m_networkManager.PrefabHandler.AddHandler(prefab.gameObject, new NetworkPrefabHandler.HandleNetworkPrefabSpawn(SpawnPrefabHandler));
                }
            }
        }

        private NetworkObject SpawnPrefabHandler(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            var prefabInstance = Instantiate(m_networkPrefabs[0], position, rotation);
            return prefabInstance;
        }

        public void StartHost()
        {
            if (m_networkManager != null)
            {
                m_networkManager.StartHost();
            }
        }

        public void StartClient()
        {
            if (m_networkManager != null)
            {
                m_networkManager.StartClient();
            }
        }

        public void StartServer()
        {
            if (m_networkManager != null)
            {
                m_networkManager.StartServer();
            }
        }

        public void Shutdown()
        {
            if (m_networkManager != null && m_networkManager.IsListening)
            {
                m_networkManager.Shutdown();
            }
        }

        public void Disconnect()
        {
            if (m_networkManager != null)
            {
                if (m_networkManager.IsHost)
                {
                    m_networkManager.Shutdown();
                }
                else if (m_networkManager.IsClient)
                {
                    m_networkManager.DisconnectClient(m_networkManager.LocalClientId);
                }
            }
        }

        public NetworkPrefabHandler PrefabHandler => m_networkManager?.PrefabHandler;
    }
} 