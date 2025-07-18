using UnityEngine;
using PongHub.Core;
using PongHub.Core.Audio;
using PongHub.Networking;
using PongHub.Input;
using PongHub.UI;

namespace PongHub.App
{
    /// <summary>
    /// 场景管理器检查器
    /// 确保所有必要的单例管理器在场景中存在并正确初始化
    /// </summary>
    public class SceneManagerChecker : MonoBehaviour
    {
        [Header("检查设置")]
        [SerializeField] private bool m_autoCreateMissingManagers = true;
        [SerializeField] private bool m_logMissingManagers = true;

        [Header("必需的管理器预制体")]
        [SerializeField] private GameObject m_audioManagerPrefab;
        [SerializeField] private GameObject m_vibrationManagerPrefab;
        [SerializeField] private GameObject m_networkManagerPrefab;
        [SerializeField] private GameObject m_gameCoreManagerPrefab;
        [SerializeField] private GameObject m_uiManagerPrefab;
        [SerializeField] private GameObject m_userIconManagerPrefab;
        [SerializeField] private GameObject m_inputManagerPrefab;

        private void Awake()
        {
            CheckAndCreateManagers();
        }

        /// <summary>
        /// 检查并创建缺失的管理器
        /// </summary>
        private void CheckAndCreateManagers()
        {
            Debug.Log("=== SceneManagerChecker: 开始检查管理器 ===");

            CheckManager<AudioManager>("AudioManager", m_audioManagerPrefab);
            CheckManager<VibrationManager>("VibrationManager", m_vibrationManagerPrefab);
            CheckManager<PongHubNetworkManager>("PongHubNetworkManager", m_networkManagerPrefab);
            CheckManager<GameCore>("GameCore", m_gameCoreManagerPrefab);
            CheckManager<UIManager>("UIManager", m_uiManagerPrefab);
            CheckManager<UserIconManager>("UserIconManager", m_userIconManagerPrefab);
            CheckManager<PongHubInputManager>("InputManager", m_inputManagerPrefab);

            Debug.Log("=== SceneManagerChecker: 管理器检查完成 ===");
        }

        /// <summary>
        /// 检查特定类型的管理器
        /// </summary>
        private void CheckManager<T>(string managerName, GameObject prefab) where T : MonoBehaviour
        {
            var existingManager = FindObjectOfType<T>();

            if (existingManager != null)
            {
                if (m_logMissingManagers)
                {
                    Debug.Log($"✓ {managerName} 已存在于场景中");
                }
                return;
            }

            if (m_logMissingManagers)
            {
                Debug.LogWarning($"⚠ {managerName} 在场景中不存在");
            }

            if (m_autoCreateMissingManagers && prefab != null)
            {
                var newManager = Instantiate(prefab);
                newManager.name = managerName;
                Debug.Log($"✓ 已创建 {managerName}");
            }
            else if (m_autoCreateMissingManagers)
            {
                Debug.LogError($"✗ 无法创建 {managerName}，未指定预制体");
            }
        }

        /// <summary>
        /// 手动触发管理器检查（用于编辑器脚本或调试）
        /// </summary>
        [ContextMenu("检查管理器")]
        public void ManualCheckManagers()
        {
            CheckAndCreateManagers();
        }
    }
}