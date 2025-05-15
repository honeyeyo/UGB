using UnityEngine;

namespace PongHub.Paddle
{
    public class PaddleRubber : MonoBehaviour
    {
        [Header("胶皮物理属性")]
        [SerializeField] private float m_spinFactor = 1f;      // 旋转系数
        [SerializeField] private float m_speedFactor = 1f;     // 速度系数
        [SerializeField] private float m_controlFactor = 1f;   // 控制系数

        [Header("胶皮类型")]
        [SerializeField] private bool m_isForehand = true;     // 是否正手面

        public float GetNormalForceModifier()
        {
            return m_speedFactor * m_controlFactor;
        }

        public float GetTangentialForceModifier()
        {
            return m_spinFactor * m_controlFactor;
        }

        public bool IsForehand => m_isForehand;
    }
}
