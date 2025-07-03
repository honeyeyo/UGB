using UnityEngine;
using UnityEngine.UI;

namespace PongHub.UI
{
    /// <summary>
    /// VR UI Helper - Utility class for VR-optimized UI settings
    /// VR UI辅助工具 - 用于VR优化UI设置的工具类
    /// </summary>
    public static class VRUIHelper
    {
        // VR UI Constants
        public const int HEADER_FONT_SIZE = 36;
        public const int TITLE_FONT_SIZE = 32;
        public const int BODY_FONT_SIZE = 24;
        public const int SMALL_FONT_SIZE = 20;

        public const float MIN_BUTTON_WIDTH = 120f;
        public const float MIN_BUTTON_HEIGHT = 80f;
        public const float LINE_SPACING = 1.5f;

        // Color scheme for VR
        public static readonly Color VR_WHITE = Color.white;
        public static readonly Color VR_CYAN_HIGHLIGHT = new Color(0f, 1f, 1f, 1f);
        public static readonly Color VR_BLUE_PRESSED = new Color(0f, 0.5f, 1f, 1f);
        public static readonly Color VR_YELLOW_SELECTED = new Color(1f, 1f, 0f, 1f);
        public static readonly Color VR_SEMI_TRANSPARENT = new Color(1f, 1f, 1f, 0.9f);

        // Danger/Safe colors
        public static readonly Color VR_RED_DANGER = new Color(1f, 0.3f, 0.3f, 0.9f);
        public static readonly Color VR_GREEN_SAFE = new Color(0.3f, 1f, 0.3f, 0.9f);

        /// <summary>
        /// Apply VR-optimized font settings to text component
        /// 为文本组件应用VR优化的字体设置
        /// </summary>
        public static void ApplyVRFontSettings(Text textComponent, int fontSize, FontStyle fontStyle = FontStyle.Normal)
        {
            if (textComponent == null) return;

            textComponent.fontSize = fontSize;
            textComponent.fontStyle = fontStyle;
            textComponent.lineSpacing = LINE_SPACING;

            // Apply adaptive contrast based on current table color
            textComponent.color = GetAdaptiveTextColor();
        }

        /// <summary>
        /// Apply VR-friendly button settings
        /// 应用VR友好的按钮设置
        /// </summary>
        public static void ApplyVRButtonSettings(Button button, VRButtonType buttonType = VRButtonType.Normal)
        {
            if (button == null) return;

            // Set minimum VR-friendly button size
            var rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                var currentSize = rectTransform.sizeDelta;
                rectTransform.sizeDelta = new Vector2(
                    Mathf.Max(currentSize.x, MIN_BUTTON_WIDTH),
                    Mathf.Max(currentSize.y, MIN_BUTTON_HEIGHT)
                );
            }

            // Setup VR-friendly visual feedback
            var colors = button.colors;

            switch (buttonType)
            {
                case VRButtonType.Normal:
                    colors.normalColor = VR_SEMI_TRANSPARENT;
                    colors.highlightedColor = VR_CYAN_HIGHLIGHT;
                    colors.pressedColor = VR_BLUE_PRESSED;
                    break;

                case VRButtonType.Danger:
                    colors.normalColor = VR_RED_DANGER;
                    colors.highlightedColor = new Color(1f, 0.5f, 0.5f, 1f);
                    colors.pressedColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                    break;

                case VRButtonType.Safe:
                    colors.normalColor = VR_GREEN_SAFE;
                    colors.highlightedColor = new Color(0.5f, 1f, 0.5f, 1f);
                    colors.pressedColor = new Color(0.2f, 0.8f, 0.2f, 1f);
                    break;
            }

            colors.selectedColor = VR_YELLOW_SELECTED;
            button.colors = colors;
        }

        /// <summary>
        /// Get adaptive text color based on table surface brightness
        /// 根据桌面亮度获取自适应文本颜色
        /// </summary>
        public static Color GetAdaptiveTextColor()
        {
            // Get table surface color (simplified - in full implementation would check actual table material)
            Color tableColor = GetTableSurfaceColor();

            // Calculate brightness using luminance formula
            float brightness = tableColor.r * 0.299f + tableColor.g * 0.587f + tableColor.b * 0.114f;

            // Return high contrast color
            return brightness > 0.5f ? Color.black : Color.white;
        }

        /// <summary>
        /// Get adaptive background color based on table surface
        /// 根据桌面表面获取自适应背景颜色
        /// </summary>
        public static Color GetAdaptiveBackgroundColor()
        {
            Color tableColor = GetTableSurfaceColor();
            float brightness = tableColor.r * 0.299f + tableColor.g * 0.587f + tableColor.b * 0.114f;

            if (brightness > 0.5f) // Light table
            {
                return new Color(1f, 1f, 1f, 0.9f); // Semi-transparent white
            }
            else // Dark table
            {
                return new Color(0f, 0f, 0f, 0.9f); // Semi-transparent black
            }
        }

        /// <summary>
        /// Get current table surface color
        /// 获取当前桌面表面颜色
        /// </summary>
        private static Color GetTableSurfaceColor()
        {
            // Simplified implementation - returns default green table color
            // In full implementation, this would check the actual table material/texture

            // Try to find table renderer
            var tableRenderer = GameObject.FindObjectOfType<Renderer>();
            if (tableRenderer != null && tableRenderer.material != null)
            {
                return tableRenderer.material.color;
            }

            // Default ping pong table green
            return new Color(0.2f, 0.6f, 0.2f, 1f);
        }

        /// <summary>
        /// Apply VR UI settings to a panel
        /// 为面板应用VR UI设置
        /// </summary>
        public static void ApplyVRPanelSettings(GameObject panel)
        {
            if (panel == null) return;

            // Apply to all text components
            var textComponents = panel.GetComponentsInChildren<Text>();
            foreach (var text in textComponents)
            {
                // Determine font size based on text content or component name
                int fontSize = BODY_FONT_SIZE;
                FontStyle fontStyle = FontStyle.Normal;

                if (text.name.ToLower().Contains("title"))
                {
                    fontSize = TITLE_FONT_SIZE;
                    fontStyle = FontStyle.Bold;
                }
                else if (text.name.ToLower().Contains("header"))
                {
                    fontSize = HEADER_FONT_SIZE;
                    fontStyle = FontStyle.Bold;
                }
                else if (text.name.ToLower().Contains("button"))
                {
                    fontStyle = FontStyle.Bold;
                }

                ApplyVRFontSettings(text, fontSize, fontStyle);
            }

            // Apply to all button components
            var buttonComponents = panel.GetComponentsInChildren<Button>();
            foreach (var button in buttonComponents)
            {
                VRButtonType buttonType = VRButtonType.Normal;

                // Determine button type based on name
                if (button.name.ToLower().Contains("confirm") || button.name.ToLower().Contains("exit"))
                {
                    buttonType = VRButtonType.Danger;
                }
                else if (button.name.ToLower().Contains("cancel") || button.name.ToLower().Contains("back"))
                {
                    buttonType = VRButtonType.Safe;
                }

                ApplyVRButtonSettings(button, buttonType);
            }
        }
    }

    /// <summary>
    /// VR Button Type enumeration
    /// VR按钮类型枚举
    /// </summary>
    public enum VRButtonType
    {
        Normal,     // 普通按钮
        Danger,     // 危险操作按钮 (红色)
        Safe        // 安全操作按钮 (绿色)
    }
}