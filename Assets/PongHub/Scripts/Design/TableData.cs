using UnityEngine;

namespace PongHub.Design
{
    [CreateAssetMenu(fileName = "TableData", menuName = "PongHub/Table Data")]
    public class TableData : ScriptableObject
    {
        [Header("尺寸参数")]
        public float Length = 2.74f;           // 球桌长度(标准2.74m)
        public float Width = 1.525f;           // 球桌宽度(标准1.525m)
        public float Height = 0.76f;           // 球桌高度(标准0.76m)
        public float NetHeight = 0.1525f;      // 网高(标准15.25cm)

        [Header("物理参数")]
        public float TableBounce = 0.8f;       // 桌面弹性
        public float TableFriction = 0.2f;     // 桌面摩擦
        public float NetBounce = 0.5f;         // 网弹性

        [Header("视觉效果")]
        public Color TableColor = Color.blue;   // 桌面颜色
        public Color LineColor = Color.white;   // 线条颜色
        public float LineWidth = 0.02f;        // 线条宽度
    }
}
