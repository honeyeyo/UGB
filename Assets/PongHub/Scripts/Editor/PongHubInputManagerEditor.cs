using UnityEngine;
using UnityEditor;
using PongHub.Input;

namespace PongHub.Editor
{
    /// <summary>
    /// PongHubInputManager 自定义编辑器
    /// 提供性能监控和优化设置的可视化界面
    /// </summary>
    [CustomEditor(typeof(PongHubInputManager))]
    public class PongHubInputManagerEditor : UnityEditor.Editor
    {
        private bool m_showPerformanceSettings = true;
        private bool m_showPerformanceStats = true;
        private bool m_showInputActions = false;
        private bool m_showComponentReferences = false;

        private float m_lastUpdateTime;
        private const float UPDATE_INTERVAL = 0.5f;

        public override void OnInspectorGUI()
        {
            PongHubInputManager manager = (PongHubInputManager)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("PongHub输入系统管理器", EditorStyles.boldLabel);

            // 绘制状态信息
            DrawStatusInfo(manager);

            EditorGUILayout.Space();

            // 性能设置折叠面板
            m_showPerformanceSettings = EditorGUILayout.Foldout(m_showPerformanceSettings, "🚀 性能优化设置", true);
            if (m_showPerformanceSettings)
            {
                DrawPerformanceSettings(manager);
            }

            EditorGUILayout.Space();

            // 性能统计折叠面板
            if (Application.isPlaying)
            {
                m_showPerformanceStats = EditorGUILayout.Foldout(m_showPerformanceStats, "📊 实时性能统计", true);
                if (m_showPerformanceStats)
                {
                    DrawPerformanceStats(manager);
                }

                EditorGUILayout.Space();
            }

            // 输入动作折叠面板
            m_showInputActions = EditorGUILayout.Foldout(m_showInputActions, "🎮 输入动作配置", true);
            if (m_showInputActions)
            {
                DrawInputActionsSettings();
            }

            EditorGUILayout.Space();

            // 组件引用折叠面板
            m_showComponentReferences = EditorGUILayout.Foldout(m_showComponentReferences, "🔗 组件引用", true);
            if (m_showComponentReferences)
            {
                DrawComponentReferences();
            }

            // 应用修改
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }

        private void DrawStatusInfo(PongHubInputManager manager)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            string status = Application.isPlaying ? "运行中" : "未运行";
            Color statusColor = Application.isPlaying ? Color.green : Color.gray;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("状态:", GUILayout.Width(50));

            var originalColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(status, EditorStyles.boldLabel);
            GUI.color = originalColor;

            EditorGUILayout.EndHorizontal();

            if (Application.isPlaying && manager != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"左手球拍: {(manager.IsLeftPaddleGripped ? "已抓取" : "未抓取")}");
                EditorGUILayout.LabelField($"右手球拍: {(manager.IsRightPaddleGripped ? "已抓取" : "未抓取")}");
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPerformanceSettings(PongHubInputManager manager)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 优化模式切换
            EditorGUILayout.BeginHorizontal();
            manager.m_useOptimizedPolling = EditorGUILayout.Toggle("启用优化轮询", manager.m_useOptimizedPolling);
            if (GUILayout.Button("?", GUILayout.Width(20)))
            {
                EditorUtility.DisplayDialog("优化轮询说明",
                    "优化轮询模式将连续输入的更新频率限制为90Hz，而不是每帧更新。\n" +
                    "这可以显著减少CPU开销，特别是在VR环境中。\n\n" +
                    "关闭此选项将恢复到每帧更新模式。", "确定");
            }
            EditorGUILayout.EndHorizontal();

            // 性能日志
            manager.m_enablePerformanceLogging = EditorGUILayout.Toggle("启用性能日志", manager.m_enablePerformanceLogging);

            if (manager.m_useOptimizedPolling)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("优化设置", EditorStyles.miniBoldLabel);

                // 更新频率设置
                SerializedProperty updateRateProp = serializedObject.FindProperty("m_continuousInputUpdateRate");
                EditorGUILayout.PropertyField(updateRateProp, new GUIContent("连续输入更新频率"));

                // 显示计算出的间隔
                float interval = 1f / updateRateProp.floatValue;
                EditorGUILayout.LabelField($"更新间隔: {interval * 1000f:F1}ms", EditorStyles.miniLabel);
            }

            // 性能建议
            if (!manager.m_useOptimizedPolling)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("建议在VR项目中启用优化轮询以提高性能。", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

                private void DrawPerformanceStats(PongHubInputManager manager)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (PongHubInputManager.Instance != null)
            {
                // 定期刷新统计信息
                if (Time.realtimeSinceStartup - m_lastUpdateTime > UPDATE_INTERVAL)
                {
                    m_lastUpdateTime = Time.realtimeSinceStartup;
                    Repaint();
                }

                                // CPU时间
                float cpuTime = PongHubInputManager.Instance.LastFrameCPUTime;
                Color cpuColor = cpuTime < 20f ? Color.green : cpuTime < 50f ? Color.yellow : Color.red;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("CPU时间:", GUILayout.Width(80));
                var originalColor = GUI.color;
                GUI.color = cpuColor;
                EditorGUILayout.LabelField($"{cpuTime:F1}μs", EditorStyles.boldLabel);
                GUI.color = originalColor;
                EditorGUILayout.EndHorizontal();

                // 更新频率
                float actualRate = PongHubInputManager.Instance.ActualUpdateRate;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("实际频率:", GUILayout.Width(80));
                EditorGUILayout.LabelField($"{actualRate:F1}Hz");
                EditorGUILayout.EndHorizontal();

                // 性能评级
                string performanceGrade = GetPerformanceGrade(cpuTime);
                Color gradeColor = cpuTime < 20f ? Color.green : cpuTime < 50f ? Color.yellow : Color.red;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("性能评级:", GUILayout.Width(80));
                GUI.color = gradeColor;
                EditorGUILayout.LabelField(performanceGrade, EditorStyles.boldLabel);
                GUI.color = originalColor;
                EditorGUILayout.EndHorizontal();

                // 实时图表（简单的条形图）
                EditorGUILayout.Space();
                DrawPerformanceBar("CPU使用率", cpuTime, 100f);
            }
            else
            {
                EditorGUILayout.LabelField("等待运行时数据...", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPerformanceBar(string label, float value, float maxValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(80));

            Rect rect = GUILayoutUtility.GetRect(100, 16);
            float fillPercent = Mathf.Clamp01(value / maxValue);

            // 背景
            EditorGUI.DrawRect(rect, Color.gray);

            // 填充条
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * fillPercent, rect.height);
            Color fillColor = fillPercent < 0.2f ? Color.green : fillPercent < 0.5f ? Color.yellow : Color.red;
            EditorGUI.DrawRect(fillRect, fillColor);

            // 文本
            GUI.Label(rect, $"{value:F1}", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.EndHorizontal();
        }

        private string GetPerformanceGrade(float cpuTime)
        {
            if (cpuTime < 10f) return "优秀 (A+)";
            if (cpuTime < 20f) return "良好 (A)";
            if (cpuTime < 50f) return "中等 (B)";
            if (cpuTime < 100f) return "较差 (C)";
            return "很差 (D)";
        }

        private void DrawInputActionsSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            SerializedProperty inputActionsProp = serializedObject.FindProperty("m_inputActionsAsset");
            EditorGUILayout.PropertyField(inputActionsProp, new GUIContent("输入动作资源"));

            EditorGUILayout.EndVertical();
        }

        private void DrawComponentReferences()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 手部锚点
            SerializedProperty leftHandProp = serializedObject.FindProperty("m_leftHandAnchor");
            SerializedProperty rightHandProp = serializedObject.FindProperty("m_rightHandAnchor");
            EditorGUILayout.PropertyField(leftHandProp, new GUIContent("左手锚点"));
            EditorGUILayout.PropertyField(rightHandProp, new GUIContent("右手锚点"));

            EditorGUILayout.Space();

            // 控制器引用
            SerializedProperty heightControllerProp = serializedObject.FindProperty("m_heightController");
            SerializedProperty teleportControllerProp = serializedObject.FindProperty("m_teleportController");
            SerializedProperty serveBallControllerProp = serializedObject.FindProperty("m_serveBallController");
            SerializedProperty paddleControllerProp = serializedObject.FindProperty("m_paddleController");

            EditorGUILayout.PropertyField(heightControllerProp, new GUIContent("高度控制器"));
            EditorGUILayout.PropertyField(teleportControllerProp, new GUIContent("传送控制器"));
            EditorGUILayout.PropertyField(serveBallControllerProp, new GUIContent("发球控制器"));
            EditorGUILayout.PropertyField(paddleControllerProp, new GUIContent("球拍控制器"));

            EditorGUILayout.Space();

            // 移动设置
            SerializedProperty playerRigProp = serializedObject.FindProperty("m_playerRig");
            SerializedProperty moveSpeedProp = serializedObject.FindProperty("m_moveSpeed");
            SerializedProperty deadZoneProp = serializedObject.FindProperty("m_deadZone");

            EditorGUILayout.PropertyField(playerRigProp, new GUIContent("玩家Rig"));
            EditorGUILayout.PropertyField(moveSpeedProp, new GUIContent("移动速度"));
            EditorGUILayout.PropertyField(deadZoneProp, new GUIContent("死区大小"));

            EditorGUILayout.EndVertical();

            // 应用序列化属性的修改
            serializedObject.ApplyModifiedProperties();
        }
    }
}