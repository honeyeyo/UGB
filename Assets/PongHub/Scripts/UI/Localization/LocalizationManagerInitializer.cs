using UnityEngine;

namespace PongHub.UI.Localization
{
    /// <summary>
    /// 本地化管理器初始化器
    /// 用于在场景加载时初始化本地化系统
    /// </summary>
    public class LocalizationManagerInitializer : MonoBehaviour
    {
        [Header("初始化设置")]
        [Tooltip("是否在Awake时初始化")]
        [SerializeField] private bool m_initOnAwake = true;

        [Tooltip("本地化管理器预制件")]
        [SerializeField] private GameObject m_localizationManagerPrefab;

        private void Awake()
        {
            if (m_initOnAwake)
            {
                InitializeLocalizationManager();
            }
        }

        /// <summary>
        /// 初始化本地化管理器
        /// </summary>
        public void InitializeLocalizationManager()
        {
            // 检查本地化管理器是否已存在
            if (LocalizationManager.Instance != null)
            {
                return;
            }

            // 如果有预制件，则实例化
            if (m_localizationManagerPrefab != null)
            {
                Instantiate(m_localizationManagerPrefab);
            }
            else
            {
                // 否则，创建一个空对象并添加本地化管理器组件
                GameObject localizationManagerObject = new GameObject("LocalizationManager");
                localizationManagerObject.AddComponent<LocalizationManager>();
                DontDestroyOnLoad(localizationManagerObject);
            }
        }
    }
}