using UnityEngine;

namespace PongHub.Gameplay.Table
{
    [CreateAssetMenu(fileName = "TableData", menuName = "PongHub/Table/TableData")]
    public class TableData : ScriptableObject
    {
        [Header("尺寸设置")]
        [SerializeField] private float m_width = 1.525f;
        [SerializeField] private float m_length = 2.74f;
        [SerializeField] private float m_height = 0.76f;
        [SerializeField] private float m_netHeight = 0.1525f;
        [SerializeField] private float m_edgeWidth = 0.1f;

        [Header("物理属性")]
        [SerializeField] private float m_bounce = 0.8f;
        [SerializeField] private float m_friction = 0.1f;
        [SerializeField] private float m_netBounce = 0.5f;
        [SerializeField] private float m_netFriction = 0.2f;
        [SerializeField] private float m_hitMultiplier = 1.0f;
        [SerializeField] private float m_hitVolume = 1.0f;

        [Header("颜色设置")]
        [SerializeField] private Color m_tableColor = Color.blue;
        [SerializeField] private Color m_netColor = Color.white;
        [SerializeField] private Color m_lineColor = Color.white;

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