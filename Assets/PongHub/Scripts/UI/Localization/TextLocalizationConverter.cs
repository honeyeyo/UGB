using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;
using System.Text.RegularExpressions;

namespace PongHub.UI.Localization
{
#if UNITY_EDITOR
    /// <summary>
    /// 文本本地化转换器
    /// 用于将现有的TextMeshProUGUI组件转换为LocalizedText组件
    /// </summary>
    public class TextLocalizationConverter : EditorWindow
    {
        // 目标根对象
        private GameObject m_targetRoot;

        // 是否递归查找子对象
        private bool m_recursive = true;

        // 是否自动生成本地化键
        private bool m_autoGenerateKeys = true;

        // 是否保留原始文本
        private bool m_keepOriginalText = true;

        // 是否自动调整字体大小
        private bool m_autoResizeFont = true;

        // 是否覆盖现有的LocalizedText组件
        private bool m_overrideExisting = false;

        // 本地化键前缀
        private string m_keyPrefix = "";

        // 转换结果
        private List<TextMeshProUGUI> m_foundTexts = new List<TextMeshProUGUI>();
        private List<LocalizedText> m_convertedTexts = new List<LocalizedText>();

        // 滚动位置
        private Vector2 m_scrollPosition;

        [MenuItem("PongHub/Localization/Text Localization Converter")]
        public static void ShowWindow()
        {
            GetWindow<TextLocalizationConverter>("Text Localization Converter");
        }

        private void OnGUI()
        {
            GUILayout.Label("Text Localization Converter", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 目标根对象
            m_targetRoot = EditorGUILayout.ObjectField("Target Root", m_targetRoot, typeof(GameObject), true) as GameObject;

            // 选项
            m_recursive = EditorGUILayout.Toggle("Recursive", m_recursive);
            m_autoGenerateKeys = EditorGUILayout.Toggle("Auto Generate Keys", m_autoGenerateKeys);
            m_keepOriginalText = EditorGUILayout.Toggle("Keep Original Text", m_keepOriginalText);
            m_autoResizeFont = EditorGUILayout.Toggle("Auto Resize Font", m_autoResizeFont);
            m_overrideExisting = EditorGUILayout.Toggle("Override Existing", m_overrideExisting);

            // 本地化键前缀
            if (m_autoGenerateKeys)
            {
                m_keyPrefix = EditorGUILayout.TextField("Key Prefix", m_keyPrefix);
            }

            EditorGUILayout.Space();

            // 查找按钮
            if (GUILayout.Button("Find Text Components"))
            {
                FindTextComponents();
            }

            // 显示查找结果
            if (m_foundTexts.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Found {m_foundTexts.Count} TextMeshProUGUI components", EditorStyles.boldLabel);

                m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

                foreach (var text in m_foundTexts)
                {
                    EditorGUILayout.ObjectField(text, typeof(TextMeshProUGUI), true);
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();

                // 转换按钮
                if (GUILayout.Button("Convert to LocalizedText"))
                {
                    ConvertToLocalizedText();
                }
            }

            // 显示转换结果
            if (m_convertedTexts.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Converted {m_convertedTexts.Count} components", EditorStyles.boldLabel);

                if (GUILayout.Button("Clear Results"))
                {
                    m_foundTexts.Clear();
                    m_convertedTexts.Clear();
                }
            }
        }

        /// <summary>
        /// 查找文本组件
        /// </summary>
        private void FindTextComponents()
        {
            if (m_targetRoot == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a target root object", "OK");
                return;
            }

            m_foundTexts.Clear();
            m_convertedTexts.Clear();

            // 查找TextMeshProUGUI组件
            TextMeshProUGUI[] texts;
            if (m_recursive)
            {
                texts = m_targetRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            }
            else
            {
                texts = m_targetRoot.GetComponents<TextMeshProUGUI>();
            }

            // 过滤已有LocalizedText组件的对象
            foreach (var text in texts)
            {
                if (!m_overrideExisting && text.GetComponent<LocalizedText>() != null)
                {
                    continue;
                }

                m_foundTexts.Add(text);
            }
        }

        /// <summary>
        /// 转换为本地化文本组件
        /// </summary>
        private void ConvertToLocalizedText()
        {
            if (m_foundTexts.Count == 0)
            {
                return;
            }

            // 创建英文文本资源字典
            Dictionary<string, string> enTexts = new Dictionary<string, string>();

            // 转换每个文本组件
            foreach (var text in m_foundTexts)
            {
                // 跳过空文本
                if (string.IsNullOrEmpty(text.text))
                {
                    continue;
                }

                // 生成本地化键
                string key = GenerateLocalizationKey(text);

                // 添加到英文文本资源
                enTexts[key] = text.text;

                // 添加LocalizedText组件
                LocalizedText localizedText = text.gameObject.GetComponent<LocalizedText>();
                if (localizedText == null)
                {
                    localizedText = text.gameObject.AddComponent<LocalizedText>();
                }

                // 设置本地化键
                SetPrivateField(localizedText, "m_localizationKey", key);

                // 设置自动调整字体大小
                SetPrivateField(localizedText, "m_autoResizeFont", m_autoResizeFont);

                // 保存原始文本
                if (!m_keepOriginalText)
                {
                    text.text = key;
                }

                m_convertedTexts.Add(localizedText);
            }

            // 更新英文文本资源文件
            if (enTexts.Count > 0)
            {
                UpdateTextResource("en", enTexts);
            }

            // 提示转换完成
            EditorUtility.DisplayDialog("Conversion Complete", $"Converted {m_convertedTexts.Count} TextMeshProUGUI components to LocalizedText", "OK");
        }

        /// <summary>
        /// 生成本地化键
        /// </summary>
        /// <param name="text">文本组件</param>
        /// <returns>本地化键</returns>
        private string GenerateLocalizationKey(TextMeshProUGUI text)
        {
            if (!m_autoGenerateKeys)
            {
                return text.text;
            }

            // 获取对象路径
            string path = GetObjectPath(text.gameObject);

            // 生成键
            string key = path;
            if (!string.IsNullOrEmpty(m_keyPrefix))
            {
                key = $"{m_keyPrefix}.{key}";
            }

            // 清理键
            key = CleanKey(key);

            return key;
        }

        /// <summary>
        /// 获取对象路径
        /// </summary>
        /// <param name="obj">游戏对象</param>
        /// <returns>对象路径</returns>
        private string GetObjectPath(GameObject obj)
        {
            string path = obj.name;

            // 如果有父对象且不是目标根对象，则递归获取路径
            if (obj.transform.parent != null && obj.transform.parent.gameObject != m_targetRoot)
            {
                path = $"{GetObjectPath(obj.transform.parent.gameObject)}.{path}";
            }

            return path;
        }

        /// <summary>
        /// 清理键
        /// </summary>
        /// <param name="key">原始键</param>
        /// <returns>清理后的键</returns>
        private string CleanKey(string key)
        {
            // 移除特殊字符
            key = Regex.Replace(key, @"[^a-zA-Z0-9._]", "");

            // 转换为小写
            key = key.ToLower();

            return key;
        }

        /// <summary>
        /// 更新文本资源文件
        /// </summary>
        /// <param name="languageCode">语言代码</param>
        /// <param name="texts">文本字典</param>
        private void UpdateTextResource(string languageCode, Dictionary<string, string> texts)
        {
            // 获取文本资源路径
            string path = $"Assets/Resources/Localization/Text/{languageCode}.json";

            // 读取现有文本资源
            Dictionary<string, string> existingTexts = new Dictionary<string, string>();
            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                existingTexts = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }

            // 合并文本
            foreach (var pair in texts)
            {
                existingTexts[pair.Key] = pair.Value;
            }

            // 保存文本资源
            string newJson = Newtonsoft.Json.JsonConvert.SerializeObject(existingTexts, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(path, newJson);

            // 刷新资源
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 设置私有字段
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="fieldName">字段名</param>
        /// <param name="value">字段值</param>
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            System.Type type = obj.GetType();
            System.Reflection.FieldInfo field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }
    }
#endif
}