using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PongHub.UI.Localization
{
    /// <summary>
    /// 语言选择器
    /// 用于在设置菜单中选择语言
    /// </summary>
    public class LanguageSelector : MonoBehaviour
    {
        [Header("UI组件")]
        [Tooltip("语言选择下拉菜单")]
        [SerializeField] private TMP_Dropdown m_languageDropdown;

        [Tooltip("预览文本组件")]
        [SerializeField] private TextMeshProUGUI m_previewText;

        [Header("预览设置")]
        [Tooltip("预览文本的本地化键")]
        [SerializeField] private string m_previewKey = "settings.language.preview";

        [Tooltip("是否在选择语言时立即应用")]
        [SerializeField] private bool m_applyImmediately = true;

        // 语言代码到下拉菜单索引的映射
        private Dictionary<string, int> m_languageCodeToIndex = new Dictionary<string, int>();

        // 下拉菜单索引到语言代码的映射
        private Dictionary<int, string> m_indexToLanguageCode = new Dictionary<int, string>();

        // 当前预览的语言代码
        private string m_previewLanguage;

        #region Unity生命周期

        private void Start()
        {
            // 初始化语言选择器
            Initialize();
        }

        private void OnEnable()
        {
            // 注册事件
            if (m_languageDropdown != null)
            {
                m_languageDropdown.onValueChanged.AddListener(OnLanguageSelected);
            }
        }

        private void OnDisable()
        {
            // 取消注册事件
            if (m_languageDropdown != null)
            {
                m_languageDropdown.onValueChanged.RemoveListener(OnLanguageSelected);
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 应用当前选择的语言
        /// </summary>
        public void ApplySelectedLanguage()
        {
            if (m_languageDropdown == null || LocalizationManager.Instance == null)
            {
                return;
            }

            int selectedIndex = m_languageDropdown.value;
            if (m_indexToLanguageCode.TryGetValue(selectedIndex, out string languageCode))
            {
                LocalizationManager.Instance.SwitchLanguage(languageCode);
            }
        }

        /// <summary>
        /// 刷新语言列表
        /// </summary>
        public void RefreshLanguageList()
        {
            UpdateLanguageList();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化
        /// </summary>
        private void Initialize()
        {
            if (LocalizationManager.Instance == null)
            {
                Debug.LogWarning("[LanguageSelector] 本地化管理器未初始化");
                return;
            }

            // 如果本地化管理器已初始化，则更新语言列表
            if (LocalizationManager.Instance.IsInitialized)
            {
                UpdateLanguageList();
            }
            else
            {
                // 否则，等待本地化管理器初始化
                LocalizationManager.Instance.OnInitialized += OnLocalizationManagerInitialized;
            }
        }

        /// <summary>
        /// 本地化管理器初始化完成回调
        /// </summary>
        private void OnLocalizationManagerInitialized()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnInitialized -= OnLocalizationManagerInitialized;
            }

            UpdateLanguageList();
        }

        /// <summary>
        /// 更新语言列表
        /// </summary>
        private void UpdateLanguageList()
        {
            if (m_languageDropdown == null || LocalizationManager.Instance == null)
            {
                return;
            }

            // 清空下拉菜单
            m_languageDropdown.ClearOptions();
            m_languageCodeToIndex.Clear();
            m_indexToLanguageCode.Clear();

            // 获取可用语言
            List<LanguageInfo> languages = LocalizationManager.Instance.AvailableLanguages;
            if (languages == null || languages.Count == 0)
            {
                Debug.LogWarning("[LanguageSelector] 没有可用的语言");
                return;
            }

            // 创建下拉菜单选项
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            for (int i = 0; i < languages.Count; i++)
            {
                LanguageInfo language = languages[i];
                options.Add(new TMP_Dropdown.OptionData(language.name));

                // 更新映射
                m_languageCodeToIndex[language.code] = i;
                m_indexToLanguageCode[i] = language.code;
            }

            // 设置下拉菜单选项
            m_languageDropdown.AddOptions(options);

            // 设置当前选中的语言
            string currentLanguage = LocalizationManager.Instance.CurrentLanguage;
            if (m_languageCodeToIndex.TryGetValue(currentLanguage, out int index))
            {
                m_languageDropdown.SetValueWithoutNotify(index);
            }
        }

        /// <summary>
        /// 语言选择回调
        /// </summary>
        private void OnLanguageSelected(int index)
        {
            if (!m_indexToLanguageCode.TryGetValue(index, out string languageCode))
            {
                return;
            }

            // 更新预览
            UpdatePreviewText(languageCode);

            // 如果配置为立即应用，则切换语言
            if (m_applyImmediately)
            {
                LocalizationManager.Instance.SwitchLanguage(languageCode);
            }
        }

        /// <summary>
        /// 更新预览文本
        /// </summary>
        private void UpdatePreviewText(string languageCode)
        {
            if (m_previewText == null || LocalizationManager.Instance == null)
            {
                return;
            }

            // 保存当前预览的语言
            m_previewLanguage = languageCode;

            // 如果预览键为空，则使用语言名称作为预览
            if (string.IsNullOrEmpty(m_previewKey))
            {
                LanguageInfo langInfo = LocalizationManager.Instance.GetLanguageInfo(languageCode);
                if (langInfo != null)
                {
                    m_previewText.text = langInfo.name;
                }
                else
                {
                    m_previewText.text = languageCode;
                }
                return;
            }

            // 获取特定语言的文本
            string previewText = LocalizationManager.Instance.GetLocalizedTextForLanguage(m_previewKey, languageCode);
            if (!string.IsNullOrEmpty(previewText))
            {
                m_previewText.text = previewText;
            }
            else
            {
                // 回退到当前语言
                previewText = LocalizationManager.Instance.GetLocalizedText(m_previewKey);
                m_previewText.text = $"{previewText} ({languageCode})";
            }
        }

        #endregion
    }
}