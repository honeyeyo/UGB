using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PongHub.UI.Localization;

namespace PongHub.UI.Settings
{
    /// <summary>
    /// 语言设置面板
    /// 用于在设置菜单中显示和切换语言
    /// </summary>
    public class LanguageSettingsPanel : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private TMP_Dropdown m_languageDropdown;
        [SerializeField] private TextMeshProUGUI m_previewText;
        [SerializeField] private Button m_applyButton;
        [SerializeField] private Button m_cancelButton;

        [Header("预览设置")]
        [SerializeField] private string m_previewKey = "settings.language.preview";
        [SerializeField] private bool m_applyImmediately = false;

        [Header("动画")]
        [SerializeField] private float m_previewFadeTime = 0.3f;

        // 语言选择器组件
        private LanguageSelector m_languageSelector;

        // 当前选择的语言代码
        private string m_selectedLanguageCode;

        // 原始语言代码（用于取消操作）
        private string m_originalLanguageCode;

        #region Unity生命周期

        private void Awake()
        {
            // 获取或添加语言选择器组件
            m_languageSelector = GetComponent<LanguageSelector>();
            if (m_languageSelector == null)
            {
                m_languageSelector = gameObject.AddComponent<LanguageSelector>();
            }

            // 设置语言选择器的引用
            if (m_languageSelector != null)
            {
                // 使用反射设置私有字段（实际项目中应该提供公共属性）
                System.Type type = m_languageSelector.GetType();
                System.Reflection.FieldInfo dropdownField = type.GetField("m_languageDropdown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                System.Reflection.FieldInfo previewTextField = type.GetField("m_previewText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                System.Reflection.FieldInfo previewKeyField = type.GetField("m_previewKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                System.Reflection.FieldInfo applyImmediatelyField = type.GetField("m_applyImmediately", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (dropdownField != null) dropdownField.SetValue(m_languageSelector, m_languageDropdown);
                if (previewTextField != null) previewTextField.SetValue(m_languageSelector, m_previewText);
                if (previewKeyField != null) previewKeyField.SetValue(m_languageSelector, m_previewKey);
                if (applyImmediatelyField != null) applyImmediatelyField.SetValue(m_languageSelector, m_applyImmediately);
            }
        }

        private void OnEnable()
        {
            // 注册按钮事件
            if (m_applyButton != null)
            {
                m_applyButton.onClick.AddListener(OnApplyButtonClicked);
            }

            if (m_cancelButton != null)
            {
                m_cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }

            // 保存原始语言代码
            if (LocalizationManager.Instance != null)
            {
                m_originalLanguageCode = LocalizationManager.Instance.CurrentLanguage;
                m_selectedLanguageCode = m_originalLanguageCode;
            }
        }

        private void OnDisable()
        {
            // 取消注册按钮事件
            if (m_applyButton != null)
            {
                m_applyButton.onClick.RemoveListener(OnApplyButtonClicked);
            }

            if (m_cancelButton != null)
            {
                m_cancelButton.onClick.RemoveListener(OnCancelButtonClicked);
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 刷新语言列表
        /// </summary>
        public void RefreshLanguageList()
        {
            if (m_languageSelector != null)
            {
                m_languageSelector.RefreshLanguageList();
            }
        }

        /// <summary>
        /// 设置预览文本
        /// </summary>
        /// <param name="text">预览文本</param>
        public void SetPreviewText(string text)
        {
            if (m_previewText != null)
            {
                StartCoroutine(FadePreviewText(text));
            }
        }

        /// <summary>
        /// 应用语言设置
        /// </summary>
        public void ApplyLanguageSetting()
        {
            if (m_languageSelector != null)
            {
                m_languageSelector.ApplySelectedLanguage();
            }
        }

        /// <summary>
        /// 取消语言设置
        /// </summary>
        public void CancelLanguageSetting()
        {
            if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(m_originalLanguageCode))
            {
                LocalizationManager.Instance.SwitchLanguage(m_originalLanguageCode);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 应用按钮点击事件
        /// </summary>
        private void OnApplyButtonClicked()
        {
            ApplyLanguageSetting();

            // 更新原始语言代码
            if (LocalizationManager.Instance != null)
            {
                m_originalLanguageCode = LocalizationManager.Instance.CurrentLanguage;
            }
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void OnCancelButtonClicked()
        {
            CancelLanguageSetting();
        }

        /// <summary>
        /// 淡入淡出预览文本
        /// </summary>
        /// <param name="newText">新文本</param>
        private IEnumerator FadePreviewText(string newText)
        {
            // 淡出
            float time = 0f;
            Color originalColor = m_previewText.color;
            Color targetColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);

            while (time < m_previewFadeTime)
            {
                m_previewText.color = Color.Lerp(originalColor, targetColor, time / m_previewFadeTime);
                time += Time.deltaTime;
                yield return null;
            }

            // 设置新文本
            m_previewText.text = newText;

            // 淡入
            time = 0f;
            while (time < m_previewFadeTime)
            {
                m_previewText.color = Color.Lerp(targetColor, originalColor, time / m_previewFadeTime);
                time += Time.deltaTime;
                yield return null;
            }

            // 确保最终颜色正确
            m_previewText.color = originalColor;
        }

        #endregion
    }
}