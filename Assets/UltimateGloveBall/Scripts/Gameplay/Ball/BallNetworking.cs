using UnityEngine;
using Unity.Netcode;
using PongHub.Core;

namespace PongHub.Gameplay.Ball
{
    [RequireComponent(typeof(BallPhysics))]
    [RequireComponent(typeof(BallSpinVisual))]
    public class BallNetworking : NetworkBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private BallPhysics m_ballPhysics;
        [SerializeField] private BallSpinVisual m_ballVisual;

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
                m_networkAngularVelocity.Value = Vector3.zero;
                m_networkHitType.Value = HitType.Table;
                m_networkHitTime.Value = 0f;
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
            m_networkVelocity.Value = m_ballPhysics.Velocity;
            m_networkAngularVelocity.Value = m_ballPhysics.AngularVelocity;
            m_networkHitType.Value = m_ballPhysics.GetLastHitType();
            m_networkHitTime.Value = m_ballPhysics.GetLastHitTime();
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
                        AudioManager.Instance.PlayPaddleHit();
                        break;
                    case HitType.Table:
                        AudioManager.Instance.PlayTableHit(m_ballPhysics.BallData.TableHitMultiplier);
                        break;
                    case HitType.Net:
                        AudioManager.Instance.PlayNetHit(m_ballPhysics.BallData.NetHitMultiplier);
                        break;
                }
            }
        }

        // 属性
        public Vector3 NetworkPosition => m_networkPosition.Value;
        public Quaternion NetworkRotation => m_networkRotation.Value;
        public Vector3 NetworkVelocity => m_networkVelocity.Value;
        public Vector3 NetworkAngularVelocity => m_networkAngularVelocity.Value;
        public HitType NetworkHitType => m_networkHitType.Value;
        public float NetworkHitTime => m_networkHitTime.Value;
    }
}