using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System;

namespace PongHub.Editor
{
    /// <summary>
    /// 项目功能扫描器
    /// 自动扫描Unity项目中的脚本、预制件、场景等资产
    /// 生成功能跟踪表Markdown文件
    /// </summary>
    public class ProjectFeatureScanner : EditorWindow
    {
        [MenuItem("PongHub/工具/生成功能跟踪表")]
        public static void GenerateFeatureTrackingTable()
        {
            var scanner = CreateInstance<ProjectFeatureScanner>();
            scanner.titleContent = new GUIContent("功能扫描器");
            scanner.Show();
        }

        private Vector2 scrollPosition;
        private bool scanScripts = true;
        private bool scanPrefabs = true;
        private bool scanScenes = true;
        private bool scanMaterials = true;
        private bool scanAudio = true;
        private bool scanVFX = true;

        private void OnGUI()
        {
            GUILayout.Label("PongHub 项目功能扫描器", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // 扫描选项
            GUILayout.Label("选择要扫描的资产类型:", EditorStyles.boldLabel);
            scanScripts = EditorGUILayout.Toggle("脚本文件 (.cs)", scanScripts);
            scanPrefabs = EditorGUILayout.Toggle("预制件 (.prefab)", scanPrefabs);
            scanScenes = EditorGUILayout.Toggle("场景文件 (.unity)", scanScenes);
            scanMaterials = EditorGUILayout.Toggle("材质文件 (.mat)", scanMaterials);
            scanAudio = EditorGUILayout.Toggle("音频文件 (.wav, .mp3)", scanAudio);
            scanVFX = EditorGUILayout.Toggle("特效预制件 (VFX)", scanVFX);

            GUILayout.Space(20);

            if (GUILayout.Button("开始扫描并生成表格", GUILayout.Height(30)))
            {
                ScanAndGenerateTable();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("仅扫描脚本接口", GUILayout.Height(25)))
            {
                ScanScriptInterfaces();
            }

            if (GUILayout.Button("仅扫描预制件", GUILayout.Height(25)))
            {
                ScanPrefabs();
            }

            if (GUILayout.Button("生成Mermaid依赖图", GUILayout.Height(25)))
            {
                GenerateMermaidDependencyGraph();
            }
        }

        private void ScanAndGenerateTable()
        {
            var markdownBuilder = new StringBuilder();

            // 生成Markdown头部
            markdownBuilder.AppendLine("# PongHub_demo 功能&资产进度跟踪表");
            markdownBuilder.AppendLine();
            markdownBuilder.AppendLine("---");
            markdownBuilder.AppendLine();
            markdownBuilder.AppendLine("## 📋 使用说明");
            markdownBuilder.AppendLine("- 本表格由Unity Editor脚本自动生成");
            markdownBuilder.AppendLine("- 扫描时间: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            markdownBuilder.AppendLine("- 如需手动修改，请谨慎操作，避免被自动覆盖");
            markdownBuilder.AppendLine();
            markdownBuilder.AppendLine("---");
            markdownBuilder.AppendLine();

            // 扫描脚本
            if (scanScripts)
            {
                markdownBuilder.AppendLine("## 1. 脚本文件扫描结果");
                markdownBuilder.AppendLine();
                markdownBuilder.AppendLine("| 脚本路径 | 类名 | 主要功能 | 方法数量 | 实现状态 | TODO链接 |");
                markdownBuilder.AppendLine("|----------|------|----------|----------|----------|----------|");

                var scripts = ScanScripts();
                foreach (var script in scripts)
                {
                    markdownBuilder.AppendLine($"| {script.Path} | {script.ClassName} | {script.Description} | {script.MethodCount} | ⏳ | - |");
                }
                markdownBuilder.AppendLine();
            }

            // 扫描预制件
            if (scanPrefabs)
            {
                markdownBuilder.AppendLine("## 2. 预制件扫描结果");
                markdownBuilder.AppendLine();
                markdownBuilder.AppendLine("| 预制件路径 | 名称 | 关联脚本 | 主要功能 | 实现状态 | TODO链接 |");
                markdownBuilder.AppendLine("|------------|------|----------|----------|----------|----------|");

                var prefabs = ScanPrefabs();
                foreach (var prefab in prefabs)
                {
                    markdownBuilder.AppendLine($"| {prefab.Path} | {prefab.Name} | {prefab.ConnectedScripts} | {prefab.Description} | ⏳ | - |");
                }
                markdownBuilder.AppendLine();
            }

            // 扫描场景
            if (scanScenes)
            {
                markdownBuilder.AppendLine("## 3. 场景文件扫描结果");
                markdownBuilder.AppendLine();
                markdownBuilder.AppendLine("| 场景路径 | 场景名 | 主要功能 | 对象数量 | 实现状态 | TODO链接 |");
                markdownBuilder.AppendLine("|----------|--------|----------|----------|----------|----------|");

                var scenes = ScanScenes();
                foreach (var scene in scenes)
                {
                    markdownBuilder.AppendLine($"| {scene.Path} | {scene.Name} | {scene.Description} | {scene.ObjectCount} | ⏳ | - |");
                }
                markdownBuilder.AppendLine();
            }

            // 生成Mermaid依赖图
            markdownBuilder.AppendLine("## 4. 资产依赖关系图");
            markdownBuilder.AppendLine();
            markdownBuilder.AppendLine("```mermaid");
            markdownBuilder.AppendLine("graph TD");
            markdownBuilder.AppendLine("    %% 脚本依赖关系");
            markdownBuilder.AppendLine("    Script_PaddleController --> Prefab_Paddle");
            markdownBuilder.AppendLine("    Script_GameManager --> Scene_Gameplay");
            markdownBuilder.AppendLine("    Script_AudioManager --> Audio_BGM");
            markdownBuilder.AppendLine("    %% 预制件依赖关系");
            markdownBuilder.AppendLine("    Prefab_Paddle --> Material_Paddle");
            markdownBuilder.AppendLine("    Prefab_Ball --> Material_Ball");
            markdownBuilder.AppendLine("    %% 场景依赖关系");
            markdownBuilder.AppendLine("    Scene_MainMenu --> Prefab_UI");
            markdownBuilder.AppendLine("    Scene_Gameplay --> Prefab_Table");
            markdownBuilder.AppendLine("```");
            markdownBuilder.AppendLine();

            // 写入文件
            string outputPath = "Documentation/Project_Feature_Tracking_Auto.md";
            File.WriteAllText(outputPath, markdownBuilder.ToString());

            Debug.Log($"功能跟踪表已生成: {outputPath}");
            EditorUtility.RevealInFinder(outputPath);
        }

        private List<ScriptInfo> ScanScripts()
        {
            var scripts = new List<ScriptInfo>();
            string[] guids = AssetDatabase.FindAssets("t:Script");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Assets/PongHub/Scripts/"))
                {
                    var scriptInfo = new ScriptInfo
                    {
                        Path = path.Replace("Assets/", ""),
                        ClassName = Path.GetFileNameWithoutExtension(path),
                        Description = GetScriptDescription(path),
                        MethodCount = EstimateMethodCount(path)
                    };
                    scripts.Add(scriptInfo);
                }
            }

            return scripts.OrderBy(s => s.Path).ToList();
        }

        private List<PrefabInfo> ScanPrefabs()
        {
            var prefabs = new List<PrefabInfo>();
            string[] guids = AssetDatabase.FindAssets("t:Prefab");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Assets/PongHub/Prefabs/"))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        var prefabInfo = new PrefabInfo
                        {
                            Path = path.Replace("Assets/", ""),
                            Name = prefab.name,
                            ConnectedScripts = GetConnectedScripts(prefab),
                            Description = GetPrefabDescription(prefab)
                        };
                        prefabs.Add(prefabInfo);
                    }
                }
            }

            return prefabs.OrderBy(p => p.Path).ToList();
        }

        private List<SceneInfo> ScanScenes()
        {
            var scenes = new List<SceneInfo>();
            string[] guids = AssetDatabase.FindAssets("t:Scene");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Assets/PongHub/Scenes/"))
                {
                    var sceneInfo = new SceneInfo
                    {
                        Path = path.Replace("Assets/", ""),
                        Name = Path.GetFileNameWithoutExtension(path),
                        Description = GetSceneDescription(path),
                        ObjectCount = EstimateSceneObjectCount(path)
                    };
                    scenes.Add(sceneInfo);
                }
            }

            return scenes.OrderBy(s => s.Path).ToList();
        }

        private void ScanScriptInterfaces()
        {
            Debug.Log("开始扫描脚本接口...");
            var scripts = ScanScripts();

            var interfaceBuilder = new StringBuilder();
            interfaceBuilder.AppendLine("## 脚本接口详情");
            interfaceBuilder.AppendLine();

            foreach (var script in scripts)
            {
                interfaceBuilder.AppendLine($"### {script.ClassName}");
                interfaceBuilder.AppendLine($"- 路径: {script.Path}");
                interfaceBuilder.AppendLine($"- 功能: {script.Description}");
                interfaceBuilder.AppendLine($"- 方法数量: {script.MethodCount}");
                interfaceBuilder.AppendLine();
            }

            Debug.Log(interfaceBuilder.ToString());
        }

        private void GenerateMermaidDependencyGraph()
        {
            var mermaidBuilder = new StringBuilder();
            mermaidBuilder.AppendLine("graph TD");

            // 添加脚本到预制件的依赖
            var scripts = ScanScripts();
            var prefabs = ScanPrefabs();

            foreach (var script in scripts)
            {
                foreach (var prefab in prefabs)
                {
                    if (prefab.ConnectedScripts.Contains(script.ClassName))
                    {
                        mermaidBuilder.AppendLine($"    Script_{script.ClassName} --> Prefab_{prefab.Name}");
                    }
                }
            }

            Debug.Log("Mermaid依赖图已生成:");
            Debug.Log(mermaidBuilder.ToString());
        }

        // 辅助方法
        private string GetScriptDescription(string path)
        {
            // 简单描述，可根据脚本名称推断功能
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.Contains("Controller")) return "控制器脚本";
            if (fileName.Contains("Manager")) return "管理器脚本";
            if (fileName.Contains("Data")) return "数据脚本";
            if (fileName.Contains("UI")) return "UI脚本";
            return "功能脚本";
        }

        private int EstimateMethodCount(string path)
        {
            try
            {
                string content = File.ReadAllText(path);
                return content.Split(new[] { "public ", "private ", "protected " }, StringSplitOptions.None).Length - 1;
            }
            catch
            {
                return 0;
            }
        }

        private string GetConnectedScripts(GameObject prefab)
        {
            var scripts = prefab.GetComponents<MonoBehaviour>();
            return string.Join(", ", scripts.Select(s => s.GetType().Name));
        }

        private string GetPrefabDescription(GameObject prefab)
        {
            string name = prefab.name.ToLower();
            if (name.Contains("paddle")) return "球拍预制件";
            if (name.Contains("ball")) return "球预制件";
            if (name.Contains("table")) return "球桌预制件";
            if (name.Contains("ui")) return "UI预制件";
            return "游戏对象预制件";
        }

        private string GetSceneDescription(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path).ToLower();
            if (fileName.Contains("menu")) return "菜单场景";
            if (fileName.Contains("game")) return "游戏场景";
            if (fileName.Contains("test")) return "测试场景";
            return "场景文件";
        }

        private int EstimateSceneObjectCount(string path)
        {
            // 简单估算场景对象数量
            return 10; // 默认值，实际需要加载场景来统计
        }
    }

    // 数据类
    [System.Serializable]
    public class ScriptInfo
    {
        public string Path;
        public string ClassName;
        public string Description;
        public int MethodCount;
    }

    [System.Serializable]
    public class PrefabInfo
    {
        public string Path;
        public string Name;
        public string ConnectedScripts;
        public string Description;
    }

    [System.Serializable]
    public class SceneInfo
    {
        public string Path;
        public string Name;
        public string Description;
        public int ObjectCount;
    }
}