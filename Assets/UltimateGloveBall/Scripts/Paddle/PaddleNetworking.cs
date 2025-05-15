using Unity.Netcode;
using UnityEngine;

namespace PongHub.Paddle
{
    // 球拍网络同步类
    [RequireComponent(typeof(Paddle))]
    public class PaddleNetworking : NetworkBehaviour
    {
        // 网络变量
        private NetworkVariable<bool> m_isForehand = new();
        private NetworkVariable<Vector3> m_velocity = new();
        private NetworkVariable<Vector3> m_acceleration = new();

        private Paddle m_paddle;

        private void Awake()
        {
            m_paddle = GetComponent<Paddle>();
        }

        // 碰撞信息同步
        [ServerRpc]
        public void SyncCollisionInfoServerRpc(ulong ballId, Vector3 contactPoint, Vector3 contactNormal, Vector3 impactForce)
        {
            // 广播碰撞信息给所有客户端
            SyncCollisionInfoClientRpc(ballId, contactPoint, contactNormal, impactForce);
        }

        [ClientRpc]
        private void SyncCollisionInfoClientRpc(ulong ballId, Vector3 contactPoint, Vector3 contactNormal, Vector3 impactForce)
        {
            // 所有客户端根据相同的碰撞信息模拟物理
            if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(ballId, out var networkObject))
            {
                if (networkObject.TryGetComponent<PongHub.Ball.PongBall>(out var ball))
                {
                    ball.ApplyCollisionForce(contactPoint, contactNormal, impactForce);
                }
            }
        }

        // 运动状态同步
        [ServerRpc]
        public void SyncMotionStateServerRpc(Vector3 velocity, Vector3 acceleration)
        {
            m_velocity.Value = velocity;
            m_acceleration.Value = acceleration;
        }
    }
}
