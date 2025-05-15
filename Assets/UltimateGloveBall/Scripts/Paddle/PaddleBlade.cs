using UnityEngine;

namespace PongHub.Paddle
{
    public class PaddleBlade : MonoBehaviour
    {
        [Header("底板物理属性")]
        [SerializeField] private float m_stiffness = 1f;       // 硬度
        [SerializeField] private float m_weight = 1f;          // 重量
        [SerializeField] private float m_balance = 1f;         // 平衡性

        [Header("底板材质")]
        [SerializeField] private string m_materialType = "Wood"; // 材质类型(木质/碳纤维等)

        public float GetNormalForceModifier()
        {
            return m_stiffness * m_balance;
        }

        public float GetTangentialForceModifier()
        {
            return m_stiffness * m_weight;
        }

        public string MaterialType => m_materialType;
    }
}
