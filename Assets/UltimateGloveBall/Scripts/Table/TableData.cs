using UnityEngine;

namespace PongHub.Table
{
    [CreateAssetMenu(fileName = "TableData", menuName = "PongHub/Table Data")]
    public class TableData : ScriptableObject
    {
        [Header("物理参数")]
        public float Bounce = 0.8f;          // 台面弹性系数
        public float Friction = 0.2f;        // 台面摩擦系数
        public float NetBounce = 0.5f;       // 球网弹性系数
        public float NetFriction = 0.3f;     // 球网摩擦系数

        [Header("尺寸参数")]
        public float Length = 2.74f;         // 标准乒乓球台长度(米)
        public float Width = 1.525f;         // 标准乒乓球台宽度(米)
        public float Height = 0.76f;         // 标准乒乓球台高度(米)
        public float NetHeight = 0.1525f;    // 标准球网高度(米)

        [Header("视觉效果")]
        public Color TableColor = new(0.2f, 0.4f, 0.2f);  // 台面颜色
        public Color LineColor = Color.white;             // 台面线条颜色
        public Color NetColor = Color.white;              // 球网颜色
    }
}
