using UnityEngine;
using System.Collections;

namespace PongHub.Input
{
    /// <summary>
    /// 传送控制器
    /// 处理右手摇杆的传送和快速转向功能
    /// </summary>
    public class TeleportController : MonoBehaviour
    {
        [Header("传送设置")]
        [SerializeField]
        [Tooltip("Teleport Activation Threshold / 传送激活阈值 - Threshold for activating teleport")]
        private float m_teleportActivationThreshold = 0.7f; // 前推激活阈值

        [SerializeField]
        [Tooltip("Teleport Cancel Threshold / 传送取消阈值 - Threshold for canceling teleport")]
        private float m_teleportCancelThreshold = 0.3f;     // 松开取消阈值

        [SerializeField]
        [Tooltip("Max Teleport Distance / 最大传送距离 - Maximum distance for teleportation")]
        private float m_maxTeleportDistance = 10f;          // 最大传送距离

        [SerializeField]
        [Tooltip("Teleport Layer Mask / 传送检测层 - Layer mask for teleport detection")]
        private LayerMask m_teleportLayerMask = -1;         // 传送检测层

        [Header("快速转向设置")]
        [SerializeField]
        [Tooltip("Snap Turn Activation Threshold / 快速转向激活阈值 - Threshold for snap turn activation")]
        private float m_snapTurnActivationThreshold = 0.8f; // 转向激活阈值

        [SerializeField]
        [Tooltip("Snap Turn Angle / 快速转向角度 - Angle for each snap turn")]
        private float m_snapTurnAngle = 45f;                // 每次转向角度

        [SerializeField]
        [Tooltip("Snap Turn Cooldown / 快速转向冷却 - Cooldown time between snap turns")]
        private float m_snapTurnCooldown = 0.3f;            // 转向冷却时间

        [Header("组件引用")]
        [SerializeField]
        [Tooltip("Player Rig / 玩家装备 - Player rig transform")]
        private Transform m_playerRig;        // 玩家Rig

        [SerializeField]
        [Tooltip("Right Hand Anchor / 右手锚点 - Transform anchor for right hand")]
        private Transform m_rightHandAnchor;  // 右手锚点

        [SerializeField]
        [Tooltip("Teleport Line / 传送射线 - Line renderer for teleport ray")]
        private LineRenderer m_teleportLine; // 传送射线

        [SerializeField]
        [Tooltip("Teleport Target / 传送目标点 - GameObject for teleport target indicator")]
        private GameObject m_teleportTarget; // 传送目标点

        [SerializeField]
        [Tooltip("Player Camera / 玩家摄像机 - Main camera for player")]
        private Camera m_playerCamera;       // 玩家摄像机

        [Header("视觉效果")]
        [SerializeField]
        [Tooltip("Valid Teleport Material / 有效传送材质 - Material for valid teleport indicator")]
        private Material m_validTeleportMaterial;   // 有效传送材质

        [SerializeField]
        [Tooltip("Invalid Teleport Material / 无效传送材质 - Material for invalid teleport indicator")]
        private Material m_invalidTeleportMaterial; // 无效传送材质

        [SerializeField]
        [Tooltip("Teleport Sound / 传送音效 - Audio clip for teleport action")]
        private AudioClip m_teleportSound;          // 传送音效

        [SerializeField]
        [Tooltip("Snap Turn Sound / 转向音效 - Audio clip for snap turn action")]
        private AudioClip m_snapTurnSound;          // 转向音效

        // 私有变量
        private bool m_isTeleportActive = false;
        private bool m_isValidTeleportTarget = false;
        private Vector3 m_teleportDestination = Vector3.zero;
        private float m_lastSnapTurnTime = 0f;
        private bool m_hasTriggeredSnapTurn = false;
        private AudioSource m_audioSource;

        // 传送状态
        private enum TeleportState
        {
            Inactive,
            Aiming,
            ReadyToTeleport
        }
        private TeleportState m_currentTeleportState = TeleportState.Inactive;

        private void Awake()
        {
            InitializeTeleportController();
        }

        private void Start()
        {
            SetupTeleportVisuals();
        }

        /// <summary>
        /// 初始化传送控制器
        /// </summary>
        private void InitializeTeleportController()
        {
            // 自动查找组件
            if (m_playerRig == null)
            {
                GameObject rig = GameObject.Find("OVRCameraRig") ??
                                GameObject.Find("XR Rig") ??
                                GameObject.Find("CameraRig");
                if (rig != null) m_playerRig = rig.transform;
            }

            if (m_playerCamera == null)
            {
                m_playerCamera = Camera.main ?? FindObjectOfType<Camera>();
            }

            // 添加音频源
            m_audioSource = GetComponent<AudioSource>();
            if (m_audioSource == null)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }
            m_audioSource.playOnAwake = false;
            m_audioSource.spatialBlend = 0f; // 2D音效
        }

        /// <summary>
        /// 设置传送视觉效果
        /// </summary>
        private void SetupTeleportVisuals()
        {
            // 创建传送射线
            if (m_teleportLine == null)
            {
                GameObject lineObj = new GameObject("TeleportLine");
                lineObj.transform.SetParent(m_rightHandAnchor);
                m_teleportLine = lineObj.AddComponent<LineRenderer>();
            }

            // 配置射线
            m_teleportLine.startWidth = 0.02f;
            m_teleportLine.endWidth = 0.01f;
            m_teleportLine.positionCount = 2;
            m_teleportLine.useWorldSpace = true;
            m_teleportLine.enabled = false;

            // 创建传送目标点
            if (m_teleportTarget == null)
            {
                m_teleportTarget = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                m_teleportTarget.name = "TeleportTarget";
                m_teleportTarget.transform.localScale = new Vector3(1f, 0.1f, 1f);

                // 移除碰撞器
                Destroy(m_teleportTarget.GetComponent<Collider>());

                m_teleportTarget.SetActive(false);
            }
        }

        /// <summary>
        /// 处理传送输入
        /// </summary>
        /// <param name="stickInput">摇杆输入值</param>
        public void HandleTeleportInput(Vector2 stickInput)
        {
            // 处理传送（Y轴）
            HandleTeleportLogic(stickInput.y);

            // 处理快速转向（X轴）
            HandleSnapTurnLogic(stickInput.x);
        }

        /// <summary>
        /// 处理传送逻辑
        /// </summary>
        private void HandleTeleportLogic(float yInput)
        {
            switch (m_currentTeleportState)
            {
                case TeleportState.Inactive:
                    if (yInput > m_teleportActivationThreshold)
                    {
                        StartTeleportAiming();
                    }
                    break;

                case TeleportState.Aiming:
                    if (yInput < m_teleportCancelThreshold)
                    {
                        if (m_isValidTeleportTarget)
                        {
                            ExecuteTeleport();
                        }
                        else
                        {
                            CancelTeleport();
                        }
                    }
                    else
                    {
                        UpdateTeleportAiming();
                    }
                    break;
            }
        }

        /// <summary>
        /// 处理快速转向逻辑
        /// </summary>
        private void HandleSnapTurnLogic(float xInput)
        {
            float absInput = Mathf.Abs(xInput);

            if (absInput > m_snapTurnActivationThreshold)
            {
                if (!m_hasTriggeredSnapTurn && Time.time - m_lastSnapTurnTime > m_snapTurnCooldown)
                {
                    bool turnRight = xInput > 0;
                    ExecuteSnapTurn(turnRight);
                    m_hasTriggeredSnapTurn = true;
                    m_lastSnapTurnTime = Time.time;
                }
            }
            else
            {
                m_hasTriggeredSnapTurn = false;
            }
        }

        /// <summary>
        /// 开始传送瞄准
        /// </summary>
        private void StartTeleportAiming()
        {
            m_currentTeleportState = TeleportState.Aiming;
            m_isTeleportActive = true;

            // 显示传送射线和目标点
            m_teleportLine.enabled = true;
            m_teleportTarget.SetActive(true);

            Debug.Log("开始传送瞄准");
        }

        /// <summary>
        /// 更新传送瞄准
        /// </summary>
        private void UpdateTeleportAiming()
        {
            if (m_rightHandAnchor == null) return;

            // 从右手发射射线
            Vector3 origin = m_rightHandAnchor.position;
            Vector3 direction = m_rightHandAnchor.forward;

            // 执行射线检测
            RaycastHit hit;
            bool hasHit = Physics.Raycast(origin, direction, out hit, m_maxTeleportDistance, m_teleportLayerMask);

            Vector3 endPoint;
            if (hasHit)
            {
                endPoint = hit.point;
                m_teleportDestination = hit.point;
                m_isValidTeleportTarget = IsValidTeleportSurface(hit);
            }
            else
            {
                endPoint = origin + direction * m_maxTeleportDistance;
                m_isValidTeleportTarget = false;
            }

            // 更新射线显示
            m_teleportLine.SetPosition(0, origin);
            m_teleportLine.SetPosition(1, endPoint);
            m_teleportLine.material = m_isValidTeleportTarget ? m_validTeleportMaterial : m_invalidTeleportMaterial;

            // 更新目标点显示
            if (m_isValidTeleportTarget)
            {
                m_teleportTarget.transform.position = m_teleportDestination;
                m_teleportTarget.GetComponent<Renderer>().material = m_validTeleportMaterial;
            }
            else
            {
                m_teleportTarget.GetComponent<Renderer>().material = m_invalidTeleportMaterial;
            }
        }

        /// <summary>
        /// 检查是否为有效传送表面
        /// </summary>
        private bool IsValidTeleportSurface(RaycastHit hit)
        {
            // 检查表面角度（不能太陡）
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            if (angle > 45f) return false;

            // 检查是否有足够空间站立
            Vector3 checkPosition = hit.point + Vector3.up * 0.1f;
            bool hasSpace = !Physics.CheckSphere(checkPosition, 0.3f, m_teleportLayerMask);

            return hasSpace;
        }

        /// <summary>
        /// 执行传送
        /// </summary>
        private void ExecuteTeleport()
        {
            if (m_playerRig == null) return;

            // 计算传送偏移（保持相对摄像机的位置）
            Vector3 cameraOffset = m_playerCamera.transform.position - m_playerRig.position;
            cameraOffset.y = 0; // 不考虑Y轴偏移

            Vector3 newPosition = m_teleportDestination - cameraOffset;
            m_playerRig.position = newPosition;

            // 播放音效
            PlayTeleportSound();

            // 结束传送
            CancelTeleport();

            Debug.Log($"传送到: {m_teleportDestination}");
        }

        /// <summary>
        /// 取消传送
        /// </summary>
        private void CancelTeleport()
        {
            m_currentTeleportState = TeleportState.Inactive;
            m_isTeleportActive = false;
            m_isValidTeleportTarget = false;

            // 隐藏传送射线和目标点
            m_teleportLine.enabled = false;
            m_teleportTarget.SetActive(false);

            Debug.Log("传送已取消");
        }

        /// <summary>
        /// 执行快速转向
        /// </summary>
        private void ExecuteSnapTurn(bool turnRight)
        {
            if (m_playerRig == null) return;

            float turnAngle = turnRight ? m_snapTurnAngle : -m_snapTurnAngle;
            m_playerRig.Rotate(0, turnAngle, 0);

            // 播放音效
            PlaySnapTurnSound();

            Debug.Log($"快速转向: {(turnRight ? "右" : "左")} {m_snapTurnAngle}度");
        }

        /// <summary>
        /// 播放传送音效
        /// </summary>
        private void PlayTeleportSound()
        {
            if (m_teleportSound != null && m_audioSource != null)
            {
                m_audioSource.PlayOneShot(m_teleportSound);
            }
        }

        /// <summary>
        /// 播放转向音效
        /// </summary>
        private void PlaySnapTurnSound()
        {
            if (m_snapTurnSound != null && m_audioSource != null)
            {
                m_audioSource.PlayOneShot(m_snapTurnSound);
            }
        }

        #region 公共属性和方法

        /// <summary>
        /// 是否正在传送瞄准
        /// </summary>
        public bool IsTeleportActive => m_isTeleportActive;

        /// <summary>
        /// 当前传送目标是否有效
        /// </summary>
        public bool IsValidTeleportTarget => m_isValidTeleportTarget;

        /// <summary>
        /// 设置传送距离
        /// </summary>
        public void SetTeleportDistance(float distance)
        {
            m_maxTeleportDistance = Mathf.Max(1f, distance);
        }

        /// <summary>
        /// 设置快速转向角度
        /// </summary>
        public void SetSnapTurnAngle(float angle)
        {
            m_snapTurnAngle = Mathf.Clamp(angle, 15f, 90f);
        }

        /// <summary>
        /// 强制取消当前传送
        /// </summary>
        public void ForceCancelTeleport()
        {
            if (m_isTeleportActive)
            {
                CancelTeleport();
            }
        }

        #endregion
    }
}