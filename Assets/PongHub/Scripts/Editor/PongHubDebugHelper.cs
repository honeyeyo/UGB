using UnityEngine;
using UnityEditor;
using PongHub.App;
using PongHub.Core;
using PongHub.Core.Audio;
using PongHub.Networking;
using PongHub.UI;

namespace PongHub.Editor
{
    /// <summary>
    /// PongHub 调试辅助工具
    /// 帮助开发者快速检查和修复常见的启动问题
    /// </summary>
    public class PongHubDebugHelper : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool autoFix = true;

        [MenuItem("PongHub/Debug Helper")]
        public static void ShowWindow()
        {
            GetWindow<PongHubDebugHelper>("PongHub Debug Helper");
        }

        private void OnGUI()
        {
            GUILayout.Label("PongHub 调试辅助工具", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space();

            autoFix = EditorGUILayout.Toggle("自动修复问题", autoFix);

            EditorGUILayout.Space();

            if (GUILayout.Button("检查所有问题", GUILayout.Height(30)))
            {
                CheckAllIssues();
            }

            EditorGUILayout.Space();

            GUILayout.Label("具体检查项:", EditorStyles.boldLabel);

            if (GUILayout.Button("检查场景管理器"))
            {
                CheckSceneManagers();
            }

            if (GUILayout.Button("检查网络预制体配置"))
            {
                CheckNetworkPrefabs();
            }

            if (GUILayout.Button("检查UserIconManager配置"))
            {
                CheckUserIconManager();
            }

            if (GUILayout.Button("验证开发模式配置"))
            {
                ValidateDevelopmentConfig();
            }

            EditorGUILayout.EndScrollView();
        }

        private void CheckAllIssues()
        {
            Debug.Log("=== PongHub Debug Helper: 开始全面检查 ===");

            CheckSceneManagers();
            CheckNetworkPrefabs();
            CheckUserIconManager();
            ValidateDevelopmentConfig();

            Debug.Log("=== PongHub Debug Helper: 检查完成 ===");
        }

        private void CheckSceneManagers()
        {
            Debug.Log("--- 检查场景管理器 ---");

            // 检查各种管理器是否存在
            CheckManager<PHApplication>("PHApplication");
            CheckManager<LocalPlayerState>("LocalPlayerState");
            CheckManager<UserIconManager>("UserIconManager");
            CheckManager<AudioManager>("AudioManager");
            CheckManager<VibrationManager>("VibrationManager");
            CheckManager<PongHub.Networking.NetworkManager>("NetworkManager");
            CheckManager<GameCore>("GameCore");
            CheckManager<MenuCanvasController>("MenuCanvasController");
        }

        private void CheckManager<T>(string name) where T : MonoBehaviour
        {
            var manager = FindObjectOfType<T>();
            if (manager != null)
            {
                Debug.Log($"✓ {name} 存在于场景中");
            }
            else
            {
                Debug.LogWarning($"⚠ {name} 不存在于场景中");

                if (autoFix)
                {
                    // 尝试查找对应的预制体
                    string[] guids = AssetDatabase.FindAssets($"{name} t:Prefab");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab != null)
                        {
                            Instantiate(prefab);
                            Debug.Log($"✓ 已自动创建 {name}");
                        }
                    }
                }
            }
        }

        private void CheckNetworkPrefabs()
        {
            Debug.Log("--- 检查网络预制体配置 ---");

            // 查找 DefaultNetworkPrefabs 资产
            string[] guids = AssetDatabase.FindAssets("DefaultNetworkPrefabs t:ScriptableObject");
            if (guids.Length == 0)
            {
                Debug.LogError("未找到 DefaultNetworkPrefabs 配置文件");
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            Debug.Log($"找到网络预制体配置: {path}");

            // 验证预制体引用
            var networkPrefabs = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (networkPrefabs != null)
            {
                Debug.Log("✓ 网络预制体配置文件加载成功");
                Debug.Log("请手动检查是否有无效的预制体引用");
            }
        }

        private void CheckUserIconManager()
        {
            Debug.Log("--- 检查UserIconManager配置 ---");

            var userIconManager = FindObjectOfType<UserIconManager>();
            if (userIconManager == null)
            {
                Debug.LogWarning("⚠ UserIconManager 不存在于场景中");

                if (autoFix)
                {
                    // 尝试查找 IconManager 预制体
                    string[] guids = AssetDatabase.FindAssets("IconManager t:Prefab");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab != null)
                        {
                            Instantiate(prefab);
                            Debug.Log("✓ 已自动创建 UserIconManager");
                        }
                    }
                }
                return;
            }

            Debug.Log("✓ UserIconManager 存在于场景中");

            // 检查 AllSkus 属性
            if (userIconManager.AllSkus != null && userIconManager.AllSkus.Length > 0)
            {
                Debug.Log($"✓ UserIconManager 包含 {userIconManager.AllSkus.Length} 个图标SKU");
                foreach (var sku in userIconManager.AllSkus)
                {
                    Debug.Log($"  - {sku}");
                }
            }
            else
            {
                Debug.LogWarning("⚠ UserIconManager.AllSkus 为空或null");
            }
        }

        private void ValidateDevelopmentConfig()
        {
            Debug.Log("--- 验证开发模式配置 ---");

            Debug.Log($"开发构建: {DevelopmentConfig.IsDevelopmentBuild}");
            Debug.Log($"Oculus平台开发模式: {DevelopmentConfig.EnableOculusPlatformDevelopmentMode}");
            Debug.Log($"跳过权限检查: {DevelopmentConfig.SkipOculusEntitlementCheck}");
            Debug.Log($"开发用户名: {DevelopmentConfig.DevelopmentUserName}");
            Debug.Log($"开发用户ID: {DevelopmentConfig.DevelopmentUserId}");

            Debug.Log("✓ 开发模式配置验证完成");
        }
    }
}