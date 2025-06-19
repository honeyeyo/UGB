using UnityEngine;
using Unity.Netcode;
using PongHub.Core;
using PongHub.Core.Audio;
using PongHub.Gameplay.Paddle;

namespace PongHub.Gameplay.Paddle
{
    [RequireComponent(typeof(Paddle))]
    public class PaddleNetworking : NetworkBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private Paddle m_paddle;

        [Header("网络同步参数")]
        [SerializeField] private float m_positionLerpSpeed = 15f;
        [SerializeField] private float m_rotationLerpSpeed = 15f;
        [SerializeField] private float m_velocityLerpSpeed = 15f;

        // 网络同步变量
        private NetworkVariable<Vector3> m_networkPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> m_networkRotation = new NetworkVariable<Quaternion>();
        private NetworkVariable<Vector3> m_networkVelocity = new NetworkVariable<Vector3>();
        private NetworkVariable<bool> m_isForehand = new NetworkVariable<bool>();
        private NetworkVariable<PaddleState> m_networkState = new NetworkVariable<PaddleState>();

        // 插值变量
        private Vector3 m_targetPosition;
        private Quaternion m_targetRotation;
        private Vector3 m_targetVelocity;

        private void Awake()
        {
            if (m_paddle == null)
                m_paddle = GetComponent<Paddle>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                // 初始化本地玩家
                m_paddle.SetState(PaddleState.Free);
            }
            else
            {
                // 初始化远程玩家
                m_paddle.SetState(PaddleState.Free);
            }
        }

        public override void OnNetworkDespawn()
        {
            // 清理网络资源
        }

        private void Update()
        {
            if (IsServer)
            {
                UpdateServerState();
            }
            else if (IsClient)
            {
                SmoothInterpolate();
            }
        }

        private void UpdateServerState()
        {
            // 更新服务器状态
            m_networkPosition.Value = transform.position;
            m_networkRotation.Value = transform.rotation;
            m_networkVelocity.Value = m_paddle.Velocity;
            m_isForehand.Value = m_paddle.IsForehand;
            m_networkState.Value = m_paddle.CurrentState;
        }

        private void SmoothInterpolate()
        {
            // 平滑插值位置和旋转
            transform.position = Vector3.Lerp(transform.position, m_networkPosition.Value, m_positionLerpSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, m_networkRotation.Value, m_rotationLerpSpeed * Time.deltaTime);

            // 平滑插值速度
            m_targetVelocity = Vector3.Lerp(m_targetVelocity, m_networkVelocity.Value, m_velocityLerpSpeed * Time.deltaTime);

            // 应用插值后的速度
            m_paddle.SetVelocity(m_targetVelocity);

            // 更新状态
            m_paddle.SetState(m_networkState.Value);
        }

        // 网络命令
        [ServerRpc]
        public void SetGripStateServerRpc(PaddleGripState gripState)
        {
            if (m_paddle != null)
            {
                m_paddle.SetState(gripState == PaddleGripState.Anchored ? PaddleState.Grabbed : PaddleState.Free);
            }
        }

        [ServerRpc]
        public void SetVelocityServerRpc(Vector3 velocity)
        {
            if (m_paddle != null)
            {
                m_paddle.SetVelocity(velocity);
            }
        }

        // 客户端RPC
        [ClientRpc]
        private void PlayHitSoundClientRpc(Vector3 position, float volume)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPaddleHit(position, volume);
            }
        }

        [ClientRpc]
        private void PlayVibrationClientRpc(float intensity)
        {
            // 实现手柄震动反馈
            if (m_paddle != null)
            {
                m_paddle.PlayVibration(intensity);
            }
        }

        // 属性
        public Vector3 NetworkPosition => m_networkPosition.Value;
        public Quaternion NetworkRotation => m_networkRotation.Value;
        public Vector3 NetworkVelocity => m_networkVelocity.Value;
        public bool IsForehand => m_isForehand.Value;
        public PaddleState NetworkState => m_networkState.Value;
    }
}