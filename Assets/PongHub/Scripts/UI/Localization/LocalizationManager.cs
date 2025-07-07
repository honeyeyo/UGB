using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace PongHub.UI.Localization
{
    /// <summary>
    /// 本地化系统管理器
    /// 负责管理语言资源的加载和切换，提供本地化文本查询接口
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        #region 单例实现

        /// <summary>
        /// 单例实例
        /// </summary>
        public static LocalizationManager Instance { get; private set; }

        #endregion

        #region 序列化字段

        [Header("配置")]
        [SerializeField] private string m_configPath = "Localization/Config/languages";
        [SerializeField] private string m_textResourcePath = "Localization/Text/";
        [SerializeField] private string m_fontResourcePath = "Localization/Fonts/";
        [SerializeField] private string m_defaultLanguage = "en";
        [SerializeField] private bool m_useSystemLanguage = true;

        [Header("调试")]
        [SerializeField] private bool m_debugMode = false;
        [SerializeField] private string m_debugLanguage = "";

        #endregion

        #region 公共属性

        /// <summary>
        /// 当前语言代码
        /// </summary>
        public string CurrentLanguage { get; private set; }

        /// <summary>
        /// 可用语言列表
        /// </summary>
        public List<LanguageInfo> AvailableLanguages { get; private set; } = new List<LanguageInfo>();

        /// <summary>
        /// 语言变更事件
        /// </summary>
        public event Action<string> OnLanguageChanged;

        /// <summary>
        /// 初始化完成事件
        /// </summary>
        public event Action OnInitialized;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized { get; private set; } = false;

        #endregion

        #region 私有字段

        // 本地化文本资源
        private Dictionary<string, Dictionary<string, string>> m_textResources = new Dictionary<string, Dictionary<string, string>>();

        // 字体资源
        private Dictionary<string, Font> m_fontResources = new Dictionary<string, Font>();

        // 语言配置
        private LanguageConfig m_languageConfig;

        // 当前加载的语言
        private HashSet<string> m_loadedLanguages = new HashSet<string>();

        // 初始化协程
        private Coroutine m_initCoroutine;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            // 确保单例
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 初始化本地化系统
            m_initCoroutine = StartCoroutine(Initialize());
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            // 停止所有协程
            if (m_initCoroutine != null)
            {
                StopCoroutine(m_initCoroutine);
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 切换语言
        /// </summary>
        /// <param name="languageCode">语言代码</param>
        public void SwitchLanguage(string languageCode)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[LocalizationManager] 本地化系统尚未初始化，无法切换语言");
                return;
            }

            if (languageCode == CurrentLanguage)
            {
                return;
            }

            // 检查语言是否有效
            if (!IsLanguageAvailable(languageCode))
            {
                Debug.LogWarning($"[LocalizationManager] 不支持的语言: {languageCode}");
                return;
            }

            // 加载语言资源
            StartCoroutine(LoadLanguageResources(languageCode));
        }

        /// <summary>
        /// 获取本地化文本
        /// </summary>
        /// <param name="key">本地化键</param>
        /// <param name="args">格式化参数</param>
        /// <returns>本地化文本</returns>
        public string GetLocalizedText(string key, params object[] args)
        {
            if (!IsInitialized)
            {
                return key;
            }

            // 尝试从当前语言获取文本
            if (TryGetTextFromLanguage(CurrentLanguage, key, out string text))
            {
                // 如果有格式化参数，则应用格式化
                if (args != null && args.Length > 0)
                {
                    try
                    {
                        return string.Format(text, args);
                    }
                    catch (FormatException ex)
                    {
                        Debug.LogError($"[LocalizationManager] 格式化文本失败: {key}, {ex.Message}");
                        return text;
                    }
                }
                return text;
            }

            // 如果当前语言没有找到，尝试从回退语言获取
            string fallbackLanguage = GetFallbackLanguage(CurrentLanguage);
            if (!string.IsNullOrEmpty(fallbackLanguage) && TryGetTextFromLanguage(fallbackLanguage, key, out text))
            {
                if (args != null && args.Length > 0)
                {
                    try
                    {
                        return string.Format(text, args);
                    }
                    catch (FormatException ex)
                    {
                        Debug.LogError($"[LocalizationManager] 格式化文本失败: {key}, {ex.Message}");
                        return text;
                    }
                }
                return text;
            }

            // 如果都没找到，返回键值
            return key;
        }

        /// <summary>
        /// 获取语言字体
        /// </summary>
        /// <param name="languageCode">语言代码</param>
        /// <returns>字体资源</returns>
        public Font GetLanguageFont(string languageCode)
        {
            if (m_fontResources.TryGetValue(languageCode, out Font font))
            {
                return font;
            }

            // 如果没有找到字体，返回默认字体
            if (m_fontResources.TryGetValue(m_defaultLanguage, out font))
            {
                return font;
            }

            return null;
        }

        /// <summary>
        /// 保存语言设置
        /// </summary>
        /// <param name="languageCode">语言代码</param>
        public void SaveLanguagePreference(string languageCode)
        {
            PlayerPrefs.SetString("Localization_Language", languageCode);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 加载语言设置
        /// </summary>
        /// <returns>保存的语言代码</returns>
        public string LoadLanguagePreference()
        {
            return PlayerPrefs.GetString("Localization_Language", "");
        }

        /// <summary>
        /// 检查语言是否可用
        /// </summary>
        /// <param name="languageCode">语言代码</param>
        /// <returns>是否可用</returns>
        public bool IsLanguageAvailable(string languageCode)
        {
            return AvailableLanguages.Exists(lang => lang.code == languageCode);
        }

        /// <summary>
        /// 获取语言信息
        /// </summary>
        /// <param name="languageCode">语言代码</param>
        /// <returns>语言信息</returns>
        public LanguageInfo GetLanguageInfo(string languageCode)
        {
            return AvailableLanguages.Find(lang => lang.code == languageCode);
        }

        /// <summary>
        /// 获取特定语言的本地化文本
        /// </summary>
        /// <param name="key">本地化键</param>
        /// <param name="languageCode">语言代码</param>
        /// <param name="args">格式化参数</param>
        /// <returns>特定语言的本地化文本</returns>
        public string GetLocalizedTextForLanguage(string key, string languageCode, params object[] args)
        {
            if (!IsInitialized)
            {
                return key;
            }

            // 检查语言是否有效
            if (!IsLanguageAvailable(languageCode))
            {
                Debug.LogWarning($"[LocalizationManager] 不支持的语言: {languageCode}");
                return key;
            }

            // 尝试从指定语言获取文本
            if (TryGetTextFromLanguage(languageCode, key, out string text))
            {
                // 如果有格式化参数，则应用格式化
                if (args != null && args.Length > 0)
                {
                    try
                    {
                        return string.Format(text, args);
                    }
                    catch (FormatException ex)
                    {
                        Debug.LogError($"[LocalizationManager] 格式化文本失败: {key}, {ex.Message}");
                        return text;
                    }
                }
                return text;
            }

            // 如果指定语言没有找到，尝试从回退语言获取
            string fallbackLanguage = GetFallbackLanguage(languageCode);
            if (!string.IsNullOrEmpty(fallbackLanguage) && TryGetTextFromLanguage(fallbackLanguage, key, out text))
            {
                if (args != null && args.Length > 0)
                {
                    try
                    {
                        return string.Format(text, args);
                    }
                    catch (FormatException ex)
                    {
                        Debug.LogError($"[LocalizationManager] 格式化文本失败: {key}, {ex.Message}");
                        return text;
                    }
                }
                return text;
            }

            // 如果都没找到，返回键值
            return key;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化本地化系统
        /// </summary>
        private IEnumerator Initialize()
        {
            // 加载语言配置
            yield return LoadLanguageConfig();

            if (m_languageConfig == null || AvailableLanguages.Count == 0)
            {
                Debug.LogError("[LocalizationManager] 加载语言配置失败");
                yield break;
            }

            // 确定初始语言
            string initialLanguage = DetermineInitialLanguage();

            // 加载初始语言资源
            yield return LoadLanguageResources(initialLanguage);

            // 标记初始化完成
            IsInitialized = true;
            OnInitialized?.Invoke();

            if (m_debugMode)
            {
                Debug.Log($"[LocalizationManager] 初始化完成，当前语言: {CurrentLanguage}, 可用语言: {AvailableLanguages.Count}");
            }
        }

        /// <summary>
        /// 加载语言配置
        /// </summary>
        private IEnumerator LoadLanguageConfig()
        {
            // 加载语言配置文件
            ResourceRequest request = Resources.LoadAsync<TextAsset>(m_configPath);
            yield return request;

            TextAsset configAsset = request.asset as TextAsset;
            if (configAsset == null)
            {
                Debug.LogError($"[LocalizationManager] 加载语言配置文件失败: {m_configPath}");
                yield break;
            }

            try
            {
                // 解析语言配置
                m_languageConfig = JsonConvert.DeserializeObject<LanguageConfig>(configAsset.text);
                AvailableLanguages = m_languageConfig.languages;

                if (m_debugMode)
                {
                    Debug.Log($"[LocalizationManager] 加载语言配置成功，默认语言: {m_languageConfig.defaultLanguage}, 可用语言: {AvailableLanguages.Count}");
                }

                // 更新默认语言
                if (!string.IsNullOrEmpty(m_languageConfig.defaultLanguage))
                {
                    m_defaultLanguage = m_languageConfig.defaultLanguage;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalizationManager] 解析语言配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 确定初始语言
        /// </summary>
        private string DetermineInitialLanguage()
        {
            // 如果是调试模式且指定了调试语言，则使用调试语言
            if (m_debugMode && !string.IsNullOrEmpty(m_debugLanguage) && IsLanguageAvailable(m_debugLanguage))
            {
                return m_debugLanguage;
            }

            // 尝试加载用户偏好的语言
            string savedLanguage = LoadLanguagePreference();
            if (!string.IsNullOrEmpty(savedLanguage) && IsLanguageAvailable(savedLanguage))
            {
                return savedLanguage;
            }

            // 如果配置为使用系统语言，则尝试使用系统语言
            if (m_useSystemLanguage)
            {
                // 将Unity系统语言转换为语言代码
                string systemLanguage = ConvertSystemLanguageToCode(Application.systemLanguage);
                if (!string.IsNullOrEmpty(systemLanguage) && IsLanguageAvailable(systemLanguage))
                {
                    return systemLanguage;
                }
            }

            // 最后使用默认语言
            return m_defaultLanguage;
        }

        /// <summary>
        /// 将Unity系统语言转换为语言代码
        /// </summary>
        private string ConvertSystemLanguageToCode(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.English:
                    return "en";
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                    return "zh";
                case SystemLanguage.ChineseTraditional:
                    return "zh-TW";
                // 可以根据需要添加更多语言
                default:
                    return "en";
            }
        }

        /// <summary>
        /// 加载语言资源
        /// </summary>
        private IEnumerator LoadLanguageResources(string languageCode)
        {
            // 如果语言已加载，直接切换
            if (m_loadedLanguages.Contains(languageCode))
            {
                OnLanguageResourcesLoaded(languageCode);
                yield break;
            }

            // 加载文本资源
            ResourceRequest textRequest = Resources.LoadAsync<TextAsset>($"{m_textResourcePath}{languageCode}");
            yield return textRequest;

            // 处理文本资源
            TextAsset textAsset = textRequest.asset as TextAsset;
            if (textAsset != null)
            {
                ProcessTextResource(languageCode, textAsset);
            }
            else
            {
                Debug.LogError($"[LocalizationManager] 加载文本资源失败: {languageCode}");
                yield break;
            }

            // 加载字体资源
            string fontName = GetFontNameForLanguage(languageCode);
            if (!string.IsNullOrEmpty(fontName))
            {
                ResourceRequest fontRequest = Resources.LoadAsync<Font>($"{m_fontResourcePath}{fontName}");
                yield return fontRequest;

                Font font = fontRequest.asset as Font;
                if (font != null)
                {
                    m_fontResources[languageCode] = font;
                }
                else
                {
                    Debug.LogWarning($"[LocalizationManager] 加载字体资源失败: {fontName}");
                }
            }

            // 标记语言已加载
            m_loadedLanguages.Add(languageCode);

            // 通知加载完成
            OnLanguageResourcesLoaded(languageCode);
        }

        /// <summary>
        /// 处理文本资源
        /// </summary>
        private void ProcessTextResource(string languageCode, TextAsset textAsset)
        {
            try
            {
                // 解析JSON文本资源
                Dictionary<string, string> textDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(textAsset.text);
                m_textResources[languageCode] = textDict;

                if (m_debugMode)
                {
                    Debug.Log($"[LocalizationManager] 加载文本资源成功: {languageCode}, 条目数: {textDict.Count}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalizationManager] 解析文本资源失败: {languageCode}, {ex.Message}");
            }
        }

        /// <summary>
        /// 获取语言的字体名称
        /// </summary>
        private string GetFontNameForLanguage(string languageCode)
        {
            LanguageInfo langInfo = GetLanguageInfo(languageCode);
            return langInfo?.font;
        }

        /// <summary>
        /// 获取语言的回退语言
        /// </summary>
        private string GetFallbackLanguage(string languageCode)
        {
            LanguageInfo langInfo = GetLanguageInfo(languageCode);
            return langInfo?.fallback;
        }

        /// <summary>
        /// 尝试从指定语言获取文本
        /// </summary>
        private bool TryGetTextFromLanguage(string languageCode, string key, out string text)
        {
            text = null;

            if (m_textResources.TryGetValue(languageCode, out Dictionary<string, string> textDict))
            {
                return textDict.TryGetValue(key, out text);
            }

            return false;
        }

        /// <summary>
        /// 语言资源加载完成处理
        /// </summary>
        private void OnLanguageResourcesLoaded(string languageCode)
        {
            string previousLanguage = CurrentLanguage;
            CurrentLanguage = languageCode;

            // 保存用户偏好
            SaveLanguagePreference(languageCode);

            // 通知所有监听者
            OnLanguageChanged?.Invoke(languageCode);

            if (m_debugMode)
            {
                Debug.Log($"[LocalizationManager] 语言切换: {previousLanguage} -> {CurrentLanguage}");
            }
        }

        #endregion
    }

    #region 数据类

    /// <summary>
    /// 语言配置
    /// </summary>
    [Serializable]
    public class LanguageConfig
    {
        public List<LanguageInfo> languages = new List<LanguageInfo>();
        public string defaultLanguage = "en";
    }

    /// <summary>
    /// 语言信息
    /// </summary>
    [Serializable]
    public class LanguageInfo
    {
        public string code;      // 语言代码
        public string name;      // 语言名称
        public string font;      // 字体资源名称
        public string fallback;  // 回退语言
    }

    #endregion
}