using UnityEngine;
using Unity.Netcode;
using PongHub.Core;
using PongHub.Networking;
using Unity.Netcode.Components;

namespace PongHub.Gameplay.Ball
{
    [RequireComponent(typeof(BallPhysics))]
    [RequireComponent(typeof(BallSpinVisual))]
    public class BallNetworking : NetworkBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private BallPhysics m_ballPhysics;
        [SerializeField] private BallSpinVisual m_ballVisual;
        [SerializeField] private NetworkTransform m_networkTransform;

        [Header("网络同步参数")]
        [SerializeField] private float m_positionLerpSpeed = 15f;
        [SerializeField] private float m_rotationLerpSpeed = 15f;
        [SerializeField] private float m_velocityLerpSpeed = 15f;
        [SerializeField] private float m_angularVelocityLerpSpeed = 15f;

        // 网络同步变量
        private NetworkVariable<Vector3> m_networkPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> m_networkRotation = new NetworkVariable<Quaternion>();
        private NetworkVariable<Vector3> m_networkVelocity = new NetworkVariable<Vector3>();
        private NetworkVariable<Vector3> m_networkAngularVelocity = new NetworkVariable<Vector3>();
        private NetworkVariable<HitType> m_networkHitType = new NetworkVariable<HitType>();
        private NetworkVariable<float> m_networkHitTime = new NetworkVariable<float>();
        private NetworkVariable<BallState> m_networkState = new NetworkVariable<BallState>();

        // 插值变量
        private Vector3 m_targetPosition;
        private Quaternion m_targetRotation;
        private Vector3 m_targetVelocity;
        private Vector3 m_targetAngularVelocity;

        private void Awake()
        {
            if (m_ballPhysics == null)
                m_ballPhysics = GetComponent<BallPhysics>();
            if (m_ballVisual == null)
                m_ballVisual = GetComponent<BallSpinVisual>();
            if (m_networkTransform == null)
                m_networkTransform = GetComponent<NetworkTransform>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                // 初始化本地球
                m_ballPhysics.SetState(BallState.Idle);
            }
            else
            {
                // 初始化远程球
                m_ballPhysics.SetState(BallState.Idle);
            }
        }

        public override void OnNetworkDespawn()
        {
            // 清理网络资源
        }

        [ServerRpc]
        public void SetStateServerRpc(BallState state)
        {
            if (m_ballPhysics != null)
            {
                m_ballPhysics.SetState(state);
            }
        }

        [ServerRpc]
        public void SetVelocityServerRpc(Vector3 velocity)
        {
            if (m_ballPhysics != null)
            {
                m_ballPhysics.SetVelocity(velocity);
            }
        }

        [ServerRpc]
        public void SetAngularVelocityServerRpc(Vector3 angularVelocity)
        {
            if (m_ballPhysics != null)
            {
                m_ballPhysics.SetAngularVelocity(angularVelocity);
            }
        }

        private void Update()
        {
            if (IsOwner)
            {
                // 更新网络状态
                m_networkState.Value = m_ballPhysics.State;
                m_networkVelocity.Value = m_ballPhysics.Velocity;
                m_networkAngularVelocity.Value = m_ballPhysics.AngularVelocity;
            }
            else
            {
                // 同步网络状态
                m_ballPhysics.SetState(m_networkState.Value);
                m_ballPhysics.SetVelocity(m_networkVelocity.Value);
                m_ballPhysics.SetAngularVelocity(m_networkAngularVelocity.Value);
            }
        }

        private void SmoothInterpolate()
        {
            // 平滑插值位置和旋转
            transform.position = Vector3.Lerp(transform.position, m_networkPosition.Value, m_positionLerpSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, m_networkRotation.Value, m_rotationLerpSpeed * Time.deltaTime);

            // 平滑插值速度和角速度
            m_targetVelocity = Vector3.Lerp(m_targetVelocity, m_networkVelocity.Value, m_velocityLerpSpeed * Time.deltaTime);
            m_targetAngularVelocity = Vector3.Lerp(m_targetAngularVelocity, m_networkAngularVelocity.Value, m_angularVelocityLerpSpeed * Time.deltaTime);

            // 应用插值后的速度和角速度
            m_ballPhysics.SetVelocity(m_targetVelocity);
            m_ballPhysics.SetAngularVelocity(m_targetAngularVelocity);
        }

        // 网络命令
        [ServerRpc(RequireOwnership = false)]
        public void ResetBallServerRpc()
        {
            // 重置球的状态
            m_ballPhysics.ResetBall();
            m_ballVisual.ResetVisuals();

            // 同步重置状态
            m_networkPosition.Value = transform.position;
            m_networkRotation.Value = transform.rotation;
            m_networkVelocity.Value = Vector3.zero;
            m_networkAngularVelocity.Value = Vector3.zero;
            m_networkHitType.Value = HitType.Table;
            m_networkHitTime.Value = 0f;

            // 广播重置事件
            ResetBallClientRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ApplyCollisionForceServerRpc(Vector3 contactPoint, Vector3 contactNormal, float force, HitType hitType)
        {
            // 应用碰撞力
            m_ballPhysics.ApplyCollisionForce(contactPoint, contactNormal, force, hitType);

            // 同步碰撞状态
            m_networkPosition.Value = transform.position;
            m_networkRotation.Value = transform.rotation;
            m_networkVelocity.Value = m_ballPhysics.Velocity;
            m_networkAngularVelocity.Value = m_ballPhysics.AngularVelocity;
            m_networkHitType.Value = hitType;
            m_networkHitTime.Value = Time.time;

            // 广播碰撞事件
            OnBallCollisionClientRpc(hitType);
        }

        // 客户端RPC
        [ClientRpc]
        private void ResetBallClientRpc()
        {
            // 重置视觉效果
            m_ballVisual.ResetVisuals();
        }

        [ClientRpc]
        private void OnBallCollisionClientRpc(HitType hitType)
        {
            // 播放碰撞音效
            if (AudioManager.Instance != null)
            {
                switch (hitType)
                {
                    case HitType.Paddle:
                        PlayPaddleHitSoundClientRpc(transform.position, 1f);
                        break;
                    case HitType.Table:
                        PlayTableHitSoundClientRpc(transform.position, m_ballPhysics.BallData.TableHitMultiplier);
                        break;
                    case HitType.Net:
                        PlayNetHitSoundClientRpc(transform.position, m_ballPhysics.BallData.NetHitMultiplier);
                        break;
                }
            }
        }

        [ClientRpc]
        private void PlayPaddleHitSoundClientRpc(Vector3 position, float volume)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPaddleHit(position, volume);
            }
        }

        [ClientRpc]
        private void PlayTableHitSoundClientRpc(Vector3 position, float volume)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayTableHit(position, volume);
            }
        }

        [ClientRpc]
        private void PlayNetHitSoundClientRpc(Vector3 position, float volume)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayNetHit(position, volume);
            }
        }

        [ClientRpc]
        public void PlayHitSoundClientRpc(Vector3 position, float volume)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBallHit(position, volume);
            }
        }

        [ClientRpc]
        public void PlayVibrationClientRpc(float intensity)
        {
            m_ballPhysics.PlayVibration(intensity);
        }

        // 属性
        public Vector3 NetworkPosition => m_networkPosition.Value;
        public Quaternion NetworkRotation => m_networkRotation.Value;
        public Vector3 NetworkVelocity => m_networkVelocity.Value;
        public Vector3 NetworkAngularVelocity => m_networkAngularVelocity.Value;
        public HitType NetworkHitType => m_networkHitType.Value;
        public float NetworkHitTime => m_networkHitTime.Value;
        public BallState NetworkState => m_networkState.Value;
    }
}