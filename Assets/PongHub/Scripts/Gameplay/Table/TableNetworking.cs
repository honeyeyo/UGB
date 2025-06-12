using UnityEngine;
using Unity.Netcode;
using PongHub.Core;

namespace PongHub.Gameplay.Table
{
    [RequireComponent(typeof(Table))]
    public class TableNetworking : NetworkBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private Table m_table;

        [Header("网络同步参数")]
        [SerializeField] private float m_positionLerpSpeed = 15f;
        [SerializeField] private float m_rotationLerpSpeed = 15f;
        [SerializeField] private float m_colorLerpSpeed = 15f;

        // 网络同步变量
        private NetworkVariable<Vector3> m_networkPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> m_networkRotation = new NetworkVariable<Quaternion>();
        private NetworkVariable<Color> m_networkTableColor = new NetworkVariable<Color>();
        private NetworkVariable<Color> m_networkNetColor = new NetworkVariable<Color>();
        private NetworkVariable<Color> m_networkLineColor = new NetworkVariable<Color>();

        // 插值变量
        private Vector3 m_targetPosition;
        private Quaternion m_targetRotation;
        private Color m_targetTableColor;
        private Color m_targetNetColor;
        private Color m_targetLineColor;

        private void Awake()
        {
            if (m_table == null)
                m_table = GetComponent<Table>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                // 初始化网络状态
                m_networkPosition.Value = transform.position;
                m_networkRotation.Value = transform.rotation;
                m_networkTableColor.Value = m_table.TableColor;
                m_networkNetColor.Value = m_table.NetColor;
                m_networkLineColor.Value = m_table.LineColor;
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
            m_networkTableColor.Value = m_table.TableColor;
            m_networkNetColor.Value = m_table.NetColor;
            m_networkLineColor.Value = m_table.LineColor;
        }

        private void SmoothInterpolate()
        {
            // 平滑插值位置和旋转
            transform.position = Vector3.Lerp(transform.position, m_networkPosition.Value, m_positionLerpSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, m_networkRotation.Value, m_rotationLerpSpeed * Time.deltaTime);

            // 平滑插值颜色
            m_targetTableColor = Color.Lerp(m_targetTableColor, m_networkTableColor.Value, m_colorLerpSpeed * Time.deltaTime);
            m_targetNetColor = Color.Lerp(m_targetNetColor, m_networkNetColor.Value, m_colorLerpSpeed * Time.deltaTime);
            m_targetLineColor = Color.Lerp(m_targetLineColor, m_networkLineColor.Value, m_colorLerpSpeed * Time.deltaTime);

            // 应用插值后的颜色
            m_table.SetTableColor(m_targetTableColor);
            m_table.SetNetColor(m_targetNetColor);
            m_table.SetLineColor(m_targetLineColor);
        }

        // 网络命令
        [ServerRpc(RequireOwnership = false)]
        public void ResetTableServerRpc()
        {
            // 重置球桌状态
            m_table.ResetTable();

            // 同步重置状态
            m_networkPosition.Value = transform.position;
            m_networkRotation.Value = transform.rotation;
            m_networkTableColor.Value = m_table.TableColor;
            m_networkNetColor.Value = m_table.NetColor;
            m_networkLineColor.Value = m_table.LineColor;

            // 广播重置事件
            ResetTableClientRpc();
        }

        [ClientRpc]
        private void ResetTableClientRpc()
        {
            // 重置视觉效果
            m_table.ResetTable();
        }

        // 客户端RPC
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
        private void PlayEdgeHitSoundClientRpc(Vector3 position, float volume)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayEdgeHit(position, volume);
            }
        }

        [ClientRpc]
        private void PlayScoreSoundClientRpc()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayScore();
            }
        }

        // 属性
        public Vector3 NetworkPosition => m_networkPosition.Value;
        public Quaternion NetworkRotation => m_networkRotation.Value;
        public Color NetworkTableColor => m_networkTableColor.Value;
        public Color NetworkNetColor => m_networkNetColor.Value;
        public Color NetworkLineColor => m_networkLineColor.Value;
    }
}