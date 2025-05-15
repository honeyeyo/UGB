using UnityEngine;
using PongHub.Ball;

namespace PongHub.Paddle
{
    // 球拍状态
    public enum State
    {
        Anchored,   // 固定在手上
        Free        // 自由状态(比如掉在地上)
    }

    // 球拍组件
    [SerializeField] private Rigidbody m_rigidbody;
    [SerializeField] private Collider m_collider;
    [SerializeField] private PaddleNetworking m_paddleNetworking;

    // 球拍结构
    [SerializeField] private PaddleRubber m_forehandRubber;  // 正手胶皮
    [SerializeField] private PaddleRubber m_backhandRubber;  // 反手胶皮
    [SerializeField] private PaddleBlade m_blade;           // 底板

    // 球拍状态
    private State m_currentState = State.Anchored;
    private bool m_isForehand = true;  // 当前使用正手面
    private Vector3 m_lastVelocity;    // 上一帧速度
    private Vector3 m_currentVelocity; // 当前速度
    private Vector3 m_acceleration;    // 加速度

    private void FixedUpdate()
    {
        if (m_currentState == State.Anchored)
        {
            // 计算球拍的运动状态
            m_lastVelocity = m_currentVelocity;
            m_currentVelocity = m_rigidbody.velocity;
            m_acceleration = (m_currentVelocity - m_lastVelocity) / Time.fixedDeltaTime;

            // 同步运动状态到网络
            m_paddleNetworking.SyncMotionStateServerRpc(m_currentVelocity, m_acceleration);
        }
    }

    // 碰撞处理
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<PongBall>(out var ball))
        {
            // 计算碰撞信息
            var contactPoint = collision.GetContact(0).point;
            var contactNormal = collision.GetContact(0).normal;
            var relativeVelocity = m_currentVelocity - ball.Velocity;
            var impactForce = CalculateImpactForce(relativeVelocity, contactNormal);

            // 同步碰撞信息到网络
            m_paddleNetworking.SyncCollisionInfoServerRpc(ball.NetworkObjectId, contactPoint, contactNormal, impactForce);
        }
    }

    private Vector3 CalculateImpactForce(Vector3 relativeVelocity, Vector3 contactNormal)
    {
        // 根据球拍材质和碰撞角度计算击球力
        var rubber = m_isForehand ? m_forehandRubber : m_backhandRubber;
        var normalForce = Vector3.Dot(relativeVelocity, contactNormal) * contactNormal;
        var tangentialForce = relativeVelocity - normalForce;

        // 结合胶皮和底板的物理属性计算最终力
        var normalModifier = rubber.GetNormalForceModifier() * m_blade.GetNormalForceModifier();
        var tangentialModifier = rubber.GetTangentialForceModifier() * m_blade.GetTangentialForceModifier();

        return normalForce * normalModifier + tangentialForce * tangentialModifier;
    }

    // 切换正反手
    public void SwitchSide()
    {
        m_isForehand = !m_isForehand;
    }

    // 属性
    public State CurrentState => m_currentState;
    public bool IsForehand => m_isForehand;
    public Vector3 CurrentVelocity => m_currentVelocity;
    public Vector3 Acceleration => m_acceleration;
}