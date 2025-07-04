using UnityEngine;

namespace PongHub.Gameplay.Table
{
    [CreateAssetMenu(fileName = "TableData", menuName = "PongHub/Table/TableData")]
    public class TableData : ScriptableObject
    {
        [Header("尺寸设置")]
        [SerializeField]
        [Tooltip("Width / 宽度 - Width of the table in meters")]
        private float m_width = 1.525f;

        [SerializeField]
        [Tooltip("Length / 长度 - Length of the table in meters")]
        private float m_length = 2.74f;

        [SerializeField]
        [Tooltip("Height / 高度 - Height of the table surface from ground")]
        private float m_height = 0.76f;

        [SerializeField]
        [Tooltip("Net Height / 网高 - Height of the net above table surface")]
        private float m_netHeight = 0.1525f;

        [SerializeField]
        [Tooltip("Edge Width / 边缘宽度 - Width of the table edge")]
        private float m_edgeWidth = 0.1f;

        [Header("物理属性")]
        [SerializeField]
        [Tooltip("Bounce / 弹性 - Bounce coefficient when ball hits table")]
        private float m_bounce = 0.8f;

        [SerializeField]
        [Tooltip("Friction / 摩擦力 - Friction coefficient for table surface")]
        private float m_friction = 0.1f;

        [SerializeField]
        [Tooltip("Net Bounce / 网弹性 - Bounce coefficient when ball hits net")]
        private float m_netBounce = 0.5f;

        [SerializeField]
        [Tooltip("Net Friction / 网摩擦力 - Friction coefficient for net")]
        private float m_netFriction = 0.2f;

        [SerializeField]
        [Tooltip("Hit Multiplier / 击球倍数 - Multiplier for ball speed when hitting table")]
        private float m_hitMultiplier = 1.0f;

        [SerializeField]
        [Tooltip("Hit Volume / 击球音量 - Volume level for table hit sound")]
        private float m_hitVolume = 1.0f;

        [Header("颜色设置")]
        [SerializeField]
        [Tooltip("Table Color / 球桌颜色 - Color of the table surface")]
        private Color m_tableColor = Color.blue;

        [SerializeField]
        [Tooltip("Net Color / 网颜色 - Color of the net")]
        private Color m_netColor = Color.white;

        [SerializeField]
        [Tooltip("Line Color / 线颜色 - Color of the table lines")]
        private Color m_lineColor = Color.white;

        // 尺寸属性
        public float Width => m_width;
        public float Length => m_length;
        public float Height => m_height;
        public float NetHeight => m_netHeight;
        public float EdgeWidth => m_edgeWidth;

        // 物理属性
        public float Bounce => m_bounce;
        public float Friction => m_friction;
        public float NetBounce => m_netBounce;
        public float NetFriction => m_netFriction;
        public float HitMultiplier => m_hitMultiplier;
        public float HitVolume => m_hitVolume;

        // 颜色属性
        public Color TableColor => m_tableColor;
        public Color NetColor => m_netColor;
        public Color LineColor => m_lineColor;

        // 获取球桌中心点
        public Vector3 GetTableCenter()
        {
            return new Vector3(0f, m_height / 2f, 0f);
        }

        // 获取球网位置
        public Vector3 GetNetPosition()
        {
            return new Vector3(0f, m_netHeight / 2f, 0f);
        }

        // 检查点是否在球桌范围内
        public bool IsPointInTable(Vector3 point)
        {
            float halfWidth = m_width / 2f;
            float halfLength = m_length / 2f;
            return Mathf.Abs(point.x) <= halfWidth && Mathf.Abs(point.z) <= halfLength;
        }

        // 检查点是否在发球区内
        public bool IsPointInServiceArea(Vector3 point, bool isRightSide)
        {
            float halfWidth = m_width / 2f;
            float halfLength = m_length / 2f;
            float serviceLength = m_length / 4f;

            if (isRightSide)
            {
                return Mathf.Abs(point.x) <= halfWidth &&
                       point.z >= -halfLength &&
                       point.z <= -halfLength + serviceLength;
            }
            else
            {
                return Mathf.Abs(point.x) <= halfWidth &&
                       point.z <= halfLength &&
                       point.z >= halfLength - serviceLength;
            }
        }
    }
}