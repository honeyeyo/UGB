using UnityEngine;
using System.Threading.Tasks;
using Unity.Netcode;

namespace PongHub.Networking
{
    public class PongHubNetworkManager : MonoBehaviour
    {
        private static PongHubNetworkManager s_instance;
        public static PongHubNetworkManager Instance => s_instance;

        [SerializeField]
        [Tooltip("Network Manager / 网络管理器 - Unity Netcode NetworkManager component")]
        private NetworkManager m_networkManager;

        [SerializeField]
        [Tooltip("Network Prefabs / 网络预制体 - Array of NetworkObject prefabs for spawning")]
        private NetworkObject[] m_networkPrefabs;

        // 创建一个预制体处理器类
        private class CustomPrefabHandler : INetworkPrefabInstanceHandler
        {
            private readonly NetworkObject m_prefab;

            public CustomPrefabHandler(NetworkObject prefab)
            {
                m_prefab = prefab;
            }

            public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
            {
                var instance = UnityEngine.Object.Instantiate(m_prefab, position, rotation);
                return instance;
            }

            public void Destroy(NetworkObject networkObject)
            {
                UnityEngine.Object.Destroy(networkObject.gameObject);
            }
        }

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
                m_networkManager = NetworkManager.Singleton;
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
                    // 使用自定义的预制体处理器
                    var handler = new CustomPrefabHandler(prefab);
                    m_networkManager.PrefabHandler.AddHandler(prefab.gameObject, handler);
                }
            }
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