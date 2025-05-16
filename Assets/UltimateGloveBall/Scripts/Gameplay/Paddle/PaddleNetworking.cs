using UnityEngine;
using Unity.Netcode;
using PongHub.Core;

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
            base.OnNetworkSpawn();
            if (IsServer)
            {
                // 初始化网络状态
                m_networkPosition.Value = transform.position;
                m_networkRotation.Value = transform.rotation;
                m_networkVelocity.Value = Vector3.zero;
                m_isForehand.Value = true;
                m_networkState.Value = PaddleState.Anchored;
            }
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
            m_networkState.Value = m_paddle.State;
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
        }

        // 网络命令
        [ServerRpc(RequireOwnership = false)]
        public void SwitchSideServerRpc()
        {
            m_isForehand.Value = !m_isForehand.Value;
            m_paddle.SetForehand(m_isForehand.Value);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetStateServerRpc(PaddleState state)
        {
            m_networkState.Value = state;
            m_paddle.SetState(state);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetTransformServerRpc(Vector3 position, Quaternion rotation)
        {
            m_networkPosition.Value = position;
            m_networkRotation.Value = rotation;
            transform.position = position;
            transform.rotation = rotation;
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetVelocityServerRpc(Vector3 velocity)
        {
            m_networkVelocity.Value = velocity;
            m_paddle.SetVelocity(velocity);
        }

        // 客户端RPC
        [ClientRpc]
        private void PlayHitSoundClientRpc()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPaddleHit();
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