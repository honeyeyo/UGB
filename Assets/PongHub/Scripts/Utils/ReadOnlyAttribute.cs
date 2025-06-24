using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PongHub.Utils
{
    /// <summary>
    /// 使字段在Inspector中显示为只读
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute
    {
    }
}

#if UNITY_EDITOR

namespace PongHub.Utils
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}
#endif