using UnityEngine;

namespace PongHub.Gameplay.Table
{
    [CreateAssetMenu(fileName = "TableData", menuName = "PongHub/Table Data")]
    public class TableData : ScriptableObject
    {
        [Header("球桌尺寸")]
        public float Length = 2.74f;        // 标准乒乓球桌长度
        public float Width = 1.525f;        // 标准乒乓球桌宽度
        public float Height = 0.76f;        // 标准乒乓球桌高度
        public float NetHeight = 0.1525f;   // 球网高度

        [Header("物理参数")]
        public float Bounce = 0.8f;         // 球桌弹性
        public float Friction = 0.2f;       // 球桌摩擦系数
        public float NetBounce = 0.5f;      // 球网弹性
        public float NetFriction = 0.1f;    // 球网摩擦系数

        [Header("区域参数")]
        public float ServiceAreaLength = 0.5f;  // 发球区长度
        public float ServiceAreaWidth = 0.5f;   // 发球区宽度
        public float EdgeWidth = 0.02f;         // 球桌边缘宽度

        [Header("视觉效果")]
        public Color TableColor = new Color(0.2f, 0.4f, 0.2f);  // 球桌颜色
        public Color LineColor = Color.white;                   // 线条颜色
        public Color NetColor = Color.white;                    // 球网颜色
        public float LineWidth = 0.02f;                         // 线条宽度

        [Header("音效参数")]
        public float TableHitVolume = 1f;       // 球桌击球音量
        public float NetHitVolume = 0.8f;       // 球网击球音量
        public float EdgeHitVolume = 1.2f;      // 边缘击球音量

        // 获取球桌中心点
        public Vector3 GetTableCenter()
        {
            return new Vector3(0f, Height, 0f);
        }

        // 获取球网位置
        public Vector3 GetNetPosition()
        {
            return new Vector3(0f, Height + NetHeight * 0.5f, 0f);
        }

        // 获取发球区中心点
        public Vector3 GetServiceAreaCenter(bool isRightSide)
        {
            float z = isRightSide ? Length * 0.25f : -Length * 0.25f;
            return new Vector3(0f, Height, z);
        }

        // 检查点是否在球桌范围内
        public bool IsPointInTable(Vector3 point)
        {
            return Mathf.Abs(point.x) <= Width * 0.5f &&
                   Mathf.Abs(point.z) <= Length * 0.5f &&
                   point.y >= Height &&
                   point.y <= Height + NetHeight;
        }

        // 检查点是否在发球区内
        public bool IsPointInServiceArea(Vector3 point, bool isRightSide)
        {
            float zMin = isRightSide ? 0f : -Length * 0.5f;
            float zMax = isRightSide ? Length * 0.5f : 0f;
            return Mathf.Abs(point.x) <= ServiceAreaWidth * 0.5f &&
                   point.z >= zMin &&
                   point.z <= zMax &&
                   point.y >= Height &&
                   point.y <= Height + NetHeight;
        }
    }
}