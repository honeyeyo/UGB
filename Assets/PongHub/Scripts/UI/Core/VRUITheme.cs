using UnityEngine;
using TMPro;

namespace PongHub.UI.Core
{
    /// <summary>
    /// VR UI主题系统
    /// 用于统一管理UI组件的样式，提供一致的视觉体验
    /// </summary>
    [CreateAssetMenu(fileName = "VRUITheme", menuName = "PongHub/UI/Theme")]
    public class VRUITheme : ScriptableObject
    {
        [Header("颜色设置")]
        [Tooltip("Primary Color / 主色 - Main color for UI elements")]
        public Color primaryColor = new Color(0.227f, 0.525f, 1f); // #3A86FF

        [Tooltip("Secondary Color / 辅助色 - Secondary color for UI elements")]
        public Color secondaryColor = new Color(0.514f, 0.22f, 0.925f); // #8338EC

        [Tooltip("Accent Color / 强调色 - Accent color for highlights and important elements")]
        public Color accentColor = new Color(1f, 0f, 0.431f); // #FF006E

        [Tooltip("Background Color / 背景色 - Background color for panels and containers")]
        public Color backgroundColor = new Color(0f, 0.094f, 0.271f); // #001845

        [Tooltip("Text Color / 文本色 - Color for text elements")]
        public Color textColor = Color.white;

        [Tooltip("Disabled Color / 禁用色 - Color for disabled elements")]
        public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        [Header("状态颜色")]
        [Tooltip("Normal Color / 正常状态色 - Color for normal state")]
        public Color normalColor;

        [Tooltip("Highlighted Color / 高亮状态色 - Color for highlighted state")]
        public Color highlightedColor;

        [Tooltip("Pressed Color / 按下状态色 - Color for pressed state")]
        public Color pressedColor;

        [Tooltip("Selected Color / 选中状态色 - Color for selected state")]
        public Color selectedColor;

        [Header("字体设置")]
        [Tooltip("Primary Font / 主要字体 - Main font for UI elements")]
        public TMP_FontAsset primaryFont;

        [Tooltip("Secondary Font / 辅助字体 - Secondary font for UI elements")]
        public TMP_FontAsset secondaryFont;

        [Header("尺寸设置")]
        [Tooltip("Base Font Size / 基础字号 - Base font size in points")]
        [Range(12f, 48f)]
        public float baseFontSize = 24f;

        [Tooltip("Header Font Size / 标题字号 - Header font size in points")]
        [Range(18f, 72f)]
        public float headerFontSize = 36f;

        [Tooltip("Base Element Size / 基础元素尺寸 - Base size for UI elements in mm")]
        [Range(20f, 100f)]
        public float baseElementSize = 60f;

        [Tooltip("Element Spacing / 元素间距 - Spacing between UI elements in mm")]
        [Range(5f, 30f)]
        public float elementSpacing = 10f;

        [Tooltip("Element Padding / 元素内边距 - Padding inside UI elements in mm")]
        [Range(5f, 30f)]
        public float elementPadding = 15f;

        [Header("动画设置")]
        [Tooltip("Transition Duration / 过渡时长 - Duration of state transitions in seconds")]
        [Range(0.05f, 0.5f)]
        public float transitionDuration = 0.1f;

        [Tooltip("Transition Curve / 过渡曲线 - Animation curve for state transitions")]
        public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("其他设置")]
        [Tooltip("Corner Radius / 圆角半径 - Radius of element corners in mm")]
        [Range(0f, 20f)]
        public float cornerRadius = 5f;

        [Tooltip("Shadow Strength / 阴影强度 - Strength of shadows (0-1)")]
        [Range(0f, 1f)]
        public float shadowStrength = 0.3f;

        [Tooltip("Shadow Distance / 阴影距离 - Distance of shadows in mm")]
        [Range(0f, 10f)]
        public float shadowDistance = 3f;

        private void OnEnable()
        {
            // 初始化状态颜色（如果未设置）
            if (normalColor == Color.clear)
                normalColor = primaryColor;

            if (highlightedColor == Color.clear)
                highlightedColor = Color.Lerp(primaryColor, Color.white, 0.3f);

            if (pressedColor == Color.clear)
                pressedColor = Color.Lerp(primaryColor, Color.black, 0.2f);

            if (selectedColor == Color.clear)
                selectedColor = accentColor;
        }

        /// <summary>
        /// 获取指定状态的颜色
        /// </summary>
        public Color GetStateColor(VRUIComponent.InteractionState state)
        {
            switch (state)
            {
                case VRUIComponent.InteractionState.Normal:
                    return normalColor;
                case VRUIComponent.InteractionState.Highlighted:
                    return highlightedColor;
                case VRUIComponent.InteractionState.Pressed:
                    return pressedColor;
                case VRUIComponent.InteractionState.Selected:
                    return selectedColor;
                case VRUIComponent.InteractionState.Disabled:
                    return disabledColor;
                default:
                    return normalColor;
            }
        }

        /// <summary>
        /// 获取文本颜色（考虑状态）
        /// </summary>
        public Color GetTextColor(VRUIComponent.InteractionState state)
        {
            if (state == VRUIComponent.InteractionState.Disabled)
                return new Color(textColor.r, textColor.g, textColor.b, 0.5f);

            return textColor;
        }

        /// <summary>
        /// 获取字体资源
        /// </summary>
        public TMP_FontAsset GetFont(bool isSecondary = false)
        {
            return isSecondary ? secondaryFont : primaryFont;
        }

        /// <summary>
        /// 获取字体大小
        /// </summary>
        public float GetFontSize(bool isHeader = false)
        {
            return isHeader ? headerFontSize : baseFontSize;
        }

        /// <summary>
        /// 创建默认主题
        /// </summary>
        public static VRUITheme CreateDefaultTheme()
        {
            var theme = CreateInstance<VRUITheme>();
            theme.name = "DefaultVRUITheme";
            return theme;
        }
    }
}