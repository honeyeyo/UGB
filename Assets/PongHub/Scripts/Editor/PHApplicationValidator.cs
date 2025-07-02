// Copyright (c) MagnusLab Inc. and affiliates.

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using PongHub.App;
using Meta.Multiplayer.Core;

namespace PongHub.Editor
{
    /// <summary>
    /// PHApplication验证工具
    /// 帮助检查和修复PHApplication组件的字段分配问题
    /// </summary>
    public class PHApplicationValidator : EditorWindow
    {
        [MenuItem("PongHub/Tools/PHApplication Validator")]
        public static void ShowWindow()
        {
            GetWindow<PHApplicationValidator>("PHApplication Validator");
        }

        void OnGUI()
        {
            GUILayout.Label("PHApplication 字段验证工具", EditorStyles.boldLabel);
            GUILayout.Space(10);

            var phApplication = FindObjectOfType<PHApplication>();
            
            if (phApplication == null)
            {
                EditorGUILayout.HelpBox("❌ 场景中没有找到PHApplication组件！", MessageType.Error);
                
                if (GUILayout.Button("查找PHApplication预制体"))
                {
                    var guids = AssetDatabase.FindAssets("t:Prefab PHApplication");
                    if (guids.Length > 0)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        Selection.activeObject = prefab;
                        EditorGUIUtility.PingObject(prefab);
                    }
                    else
                    {
                        Debug.LogWarning("未找到PHApplication预制体");
                    }
                }
                return;
            }

            EditorGUILayout.HelpBox("✓ 找到PHApplication组件", MessageType.Info);
            
            if (GUILayout.Button("选中PHApplication GameObject"))
            {
                Selection.activeGameObject = phApplication.gameObject;
                EditorGUIUtility.PingObject(phApplication.gameObject);
            }

            GUILayout.Space(10);
            GUILayout.Label("字段验证结果:", EditorStyles.boldLabel);

            // 检查NetworkLayer字段
            if (phApplication.NetworkLayer == null)
            {
                EditorGUILayout.HelpBox("❌ NetworkLayer字段未分配", MessageType.Error);
                
                if (GUILayout.Button("尝试自动分配NetworkLayer"))
                {
                    var networkLayer = FindObjectOfType<NetworkLayer>();
                    if (networkLayer != null)
                    {
                        phApplication.NetworkLayer = networkLayer;
                        EditorUtility.SetDirty(phApplication);
                        Debug.Log("✓ 自动分配NetworkLayer成功");
                    }
                    else
                    {
                        Debug.LogWarning("场景中没有找到NetworkLayer组件");
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("✓ NetworkLayer字段已正确分配", MessageType.Info);
            }

            // 检查Voip字段
            if (phApplication.Voip == null)
            {
                EditorGUILayout.HelpBox("❌ Voip字段未分配", MessageType.Error);
                
                if (GUILayout.Button("尝试自动分配Voip"))
                {
                    var voip = FindObjectOfType<VoipController>();
                    if (voip != null)
                    {
                        phApplication.Voip = voip;
                        EditorUtility.SetDirty(phApplication);
                        Debug.Log("✓ 自动分配Voip成功");
                    }
                    else
                    {
                        Debug.LogWarning("场景中没有找到VoipController组件");
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("✓ Voip字段已正确分配", MessageType.Info);
            }

            GUILayout.Space(10);

            // 一键修复按钮
            if (GUILayout.Button("一键检查并修复所有问题", GUILayout.Height(30)))
            {
                bool hasChanges = false;

                if (phApplication.NetworkLayer == null)
                {
                    var networkLayer = FindObjectOfType<NetworkLayer>();
                    if (networkLayer != null)
                    {
                        phApplication.NetworkLayer = networkLayer;
                        hasChanges = true;
                        Debug.Log("✓ 自动分配NetworkLayer");
                    }
                }

                if (phApplication.Voip == null)
                {
                    var voip = FindObjectOfType<VoipController>();
                    if (voip != null)
                    {
                        phApplication.Voip = voip;
                        hasChanges = true;
                        Debug.Log("✓ 自动分配Voip");
                    }
                }

                if (hasChanges)
                {
                    EditorUtility.SetDirty(phApplication);
                    Debug.Log("✅ 一键修复完成，请保存场景");
                }
                else
                {
                    Debug.Log("所有字段都已正确分配");
                }
            }

            if (GUI.changed)
            {
                Repaint();
            }
        }
    }

    /// <summary>
    /// PHApplication自定义Inspector
    /// 在Inspector中显示验证信息
    /// </summary>
    [CustomEditor(typeof(PHApplication))]
    public class PHApplicationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var phApp = (PHApplication)target;

            // 显示验证状态
            GUILayout.Space(5);
            EditorGUILayout.LabelField("验证状态", EditorStyles.boldLabel);

            var networkLayerStatus = phApp.NetworkLayer != null ? "✓ 已分配" : "❌ 未分配";
            var voipStatus = phApp.Voip != null ? "✓ 已分配" : "❌ 未分配";

            EditorGUILayout.LabelField($"NetworkLayer: {networkLayerStatus}");
            EditorGUILayout.LabelField($"Voip: {voipStatus}");

            if (phApp.NetworkLayer == null || phApp.Voip == null)
            {
                EditorGUILayout.HelpBox("有字段未分配！使用菜单 PongHub/Tools/PHApplication Validator 进行修复", MessageType.Warning);
            }

            GUILayout.Space(5);
            DrawDefaultInspector();
        }
    }
}
#endif