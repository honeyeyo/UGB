// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Meta.Utilities
{
    /// <summary>
    /// 可序列化的可空浮点数结构体
    /// 在Unity中提供对可空float类型的支持，使用NaN来表示null值
    /// 可以在Inspector中显示并编辑
    /// </summary>
    [System.Serializable]
    public struct NullableFloat
    {
        /// <summary>
        /// 内部存储的浮点数值
        /// 使用NaN表示null状态
        /// </summary>
        [SerializeField] internal float m_value;

        /// <summary>
        /// 可空浮点数的值属性
        /// 如果内部值为NaN则返回null，否则返回实际值
        /// </summary>
        public float? Value
        {
            get => float.IsNaN(m_value) ? null : m_value;
            set => m_value = value ?? float.NaN;
        }

        /// <summary>
        /// 隐式转换操作符，将可空float转换为NullableFloat
        /// </summary>
        /// <param name="value">可空浮点数值</param>
        /// <returns>对应的NullableFloat实例</returns>
        public static implicit operator NullableFloat(float? value) => new()
        {
            Value = value
        };
    }

#if UNITY_EDITOR

    /// <summary>
    /// NullableFloat的自定义属性绘制器
    /// 在Inspector中提供复选框和浮点数输入框的组合界面
    /// </summary>
    [CustomPropertyDrawer(typeof(NullableFloat))]
    internal class NullableFloatDrawer : PropertyDrawer
    {
        /// <summary>
        /// 复选框的宽度
        /// 通过计算toggle样式的尺寸获得
        /// </summary>
        private static readonly float s_toggleWidth = EditorStyles.toggle.CalcSize(GUIContent.none).x;

        /// <summary>
        /// 绘制属性的GUI
        /// 显示一个复选框来控制值是否存在，以及一个浮点数输入框
        /// </summary>
        /// <param name="position">绘制区域</param>
        /// <param name="property">序列化属性</param>
        /// <param name="label">属性标签</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            // 获取内部浮点数属性
            var floatProp = property.FindPropertyRelative(nameof(NullableFloat.m_value));

            // 检查当前是否有值（不是NaN）
            var hadValue = new NullableFloat { m_value = floatProp.floatValue }.Value.HasValue;

            // 绘制复选框区域
            var toggleRect = position;
            toggleRect.xMax = toggleRect.xMin + EditorGUIUtility.labelWidth + s_toggleWidth;

            // 如果复选框被选中（有值）
            if (EditorGUI.Toggle(toggleRect, label, hadValue))
            {
                // 绘制浮点数输入框
                var floatRect = position;
                floatRect.xMin = toggleRect.xMax;
                floatProp.floatValue = hadValue ? EditorGUI.FloatField(floatRect, floatProp.floatValue) : 0.0f;
            }
            else
            {
                // 如果复选框未选中，设置为null（NaN）
                floatProp.floatValue = ((NullableFloat)null).m_value;
            }

            EditorGUI.EndProperty();
        }
    }

#endif
}