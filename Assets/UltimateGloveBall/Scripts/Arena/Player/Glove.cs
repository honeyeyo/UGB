// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using Meta.Utilities;
using Oculus.Avatar2;
using Oculus.Interaction;
using PongHub.Arena.Balls;
using PongHub.Arena.Environment;
using PongHub.Arena.Gameplay;
using PongHub.Arena.Services;
using UnityEngine;

namespace PongHub.Arena.Player
{
    /// <summary>
    /// 处理手套的本地逻辑，包括手套移动、动作触发、追踪目标（用于追踪球）、地面和球的目标指示器、持球等。
    /// </summary>
    public class Glove : MonoBehaviour
    {
        /// <summary>
        /// 手套的左右枚举
        /// </summary>
        public enum GloveSide
        {
            Left,   // 左手
            Right,  // 右手
        }

        /// <summary>
        /// 手套的状态
        /// </summary>
        public enum State
        {
            Anchored,   // 固定在手上
            Flying,     // 飞行中
        }

        [SerializeField] private float m_travelDistance = 3; // 手套最大飞行距离
        [SerializeField] private float m_travelSpeed = 0.5f; // 手套飞行速度

        [SerializeField] private Collider m_collider; // 手套的碰撞体

        [SerializeField, AutoSet] private Rigidbody m_rigidbody; // 手套的刚体

        [SerializeField] private Transform m_ballAnchor; // 球的锚点（球附着的位置）

        [SerializeField, AutoSet] private GloveNetworking m_gloveNetworking; // 手套的网络同步组件

        [SerializeField] private ParticleSystem m_ballLaunchVFX; // 投掷球时的特效

        [SerializeField] private GameObject m_targetIndicatorPrefab; // 目标指示器预制体
        [SerializeField] private float m_maxAngleHomingTarget = 45f; // 追踪球最大锁定角度

        [SerializeField] private GameObject m_cheveronPrefab; // Chevron指示器预制体
        [SerializeField] private LODGroup m_lodGroup; // LOD组

        public State CurrentState { get; private set; } = State.Anchored; // 当前手套状态

        public GloveNetworking GloveNetworkComponent => m_gloveNetworking; // 网络组件访问器

        public bool HasBall => CurrentBall != null; // 是否持有球

        public Transform HandAnchor = null; // 手部锚点

        private RayInteractor m_uiRayInteractor; // UI射线交互器

        // 飞行相关数据
        private Vector3 m_destination; // 飞行目标点
        private bool m_flyingBack = false; // 是否正在返回

        public BallNetworking CurrentBall { get; private set; } = null; // 当前持有的球

        private bool m_actionPressed = false; // 动作按钮是否按下

        private ulong? m_selectedTargetId = null; // 选中的目标ID
        private bool m_findTarget = false; // 是否需要寻找目标
        private GameObject m_targetIndicator = null; // 目标指示器实例

        private GameObject m_chevronVisual; // Chevron指示器实例
        private readonly RaycastHit[] m_chevronRaycastResults = new RaycastHit[20]; // Chevron射线检测结果缓存
        private bool m_chevronOnABall = false; // Chevron是否指向球

        public bool TriedGrabbingBall { get; private set; } = false; // 是否尝试抓球

        public Func<bool> IsMovementEnabled; // 判断是否允许移动的委托

        /// <summary>
        /// 启用时注册事件
        /// </summary>
        private void OnEnable()
        {
            m_gloveNetworking.OnTryGrabBall += OnTryGrabBall;
        }

        /// <summary>
        /// 禁用时注销事件并重置状态
        /// </summary>
        private void OnDisable()
        {
            m_gloveNetworking.OnTryGrabBall -= OnTryGrabBall;
            m_targetIndicator?.SetActive(false);

            m_chevronVisual?.SetActive(false);

            // 禁用时重置为锚定状态
            CurrentState = State.Anchored;
        }

        /// <summary>
        /// 尝试抓球事件回调
        /// </summary>
        private void OnTryGrabBall()
        {
            TriedGrabbingBall = true;
        }

        /// <summary>
        /// 设置UI射线交互器
        /// </summary>
        public void SetRayInteractor(RayInteractor interactor)
        {
            m_uiRayInteractor = interactor;
        }

        /// <summary>
        /// 强制设置LOD为本地最高
        /// </summary>
        public void SetLODLocal()
        {
            m_lodGroup.ForceLOD(0);
        }

        /// <summary>
        /// 重置手套状态为锚定
        /// </summary>
        public void ResetGlove()
        {
            CurrentState = State.Anchored;
        }

        /// <summary>
        /// 移动手套到指定位置和旋转
        /// </summary>
        public void Move(Vector3 position, Quaternion rotation)
        {
            var isOwner = m_gloveNetworking.IsOwner;
            if ((isOwner && CurrentState == State.Anchored) || (!isOwner && !m_gloveNetworking.Flying))
            {
                var trans = transform;
                trans.position = position;
                trans.rotation = rotation;
                UpdateBall();
            }
        }

        /// <summary>
        /// 触发手套动作（投掷/发射/抓取）
        /// </summary>
        /// <param name="released">是否松开按钮</param>
        /// <param name="chargeUpPct">蓄力百分比</param>
        public void TriggerAction(bool released, float chargeUpPct = 0)
        {
            // 如果悬停在UI上，手套不可用，不能触发动作
            if (m_uiRayInteractor.State == InteractorState.Hover)
            {
                m_actionPressed = false;
                return;
            }

            switch (CurrentState)
            {
                case State.Anchored:
                    {
                        if (released)
                        {
                            if (m_actionPressed)
                            {
                                if (CurrentBall != null)
                                {
                                    // 持球时投掷球
                                    ThrowBall(chargeUpPct);
                                }
                                else
                                {
                                    // 未持球时发射手套
                                    SendGlove();
                                }
                            }

                            m_actionPressed = false;
                        }
                        else
                        {
                            m_actionPressed = true;
                        }
                    }

                    break;
                case State.Flying:
                    // 飞行中不响应动作
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 分配球给手套（抓到球时调用）
        /// </summary>
        public void AssignBall(BallNetworking ball)
        {
            m_gloveNetworking.Grabbed = true;
            SetCurrentBall(ball);
            m_collider.enabled = false;

            // 抓到球后手套返回玩家
            m_flyingBack = true;

            // 如果是追踪球，开始寻找目标
            if (CurrentBall.BallBehaviour is HomingBall)
            {
                m_findTarget = true;
                m_selectedTargetId = null;
            }
        }

        /// <summary>
        /// 设置当前手套持有的球，并同步网络状态
        /// </summary>
        /// <param name="ball">要设置的球</param>
        public void SetCurrentBall(BallNetworking ball)
        {
            CurrentBall = ball;
            if (ball == null)
            {
                m_gloveNetworking.Grabbed = false;
            }
        }

        /// <summary>
        /// Unity每帧更新
        /// </summary>
        private void Update()
        {
            UpdateBall();
            if (m_gloveNetworking.IsOwner)
            {
                UpdateTarget();
                UpdateCheveron();
            }
        }

        /// <summary>
        /// 更新球的位置和旋转，使其跟随手套
        /// </summary>
        private void UpdateBall()
        {
            if (CurrentBall != null)
            {
                var ballTrans = CurrentBall.transform;
                ballTrans.position = m_ballAnchor.position;
                ballTrans.rotation = m_ballAnchor.rotation;
            }
        }

        /// <summary>
        /// 更新追踪球的目标指示器
        /// </summary>
        private void UpdateTarget()
        {
            if (!m_findTarget || !m_gloveNetworking.IsOwner) return;

            if (CurrentBall == null)
            {
                m_findTarget = false;
                m_selectedTargetId = null;
                m_targetIndicator?.SetActive(false);

                return;
            }

            // 寻找追踪球目标
            var savedAngle = float.MaxValue;
            m_selectedTargetId = null;
            PlayerAvatarEntity targetAvatar = null;
            var myTeam = LocalPlayerEntities.Instance.LocalPlayerController.NetworkedTeamComp.MyTeam;
            foreach (var clientId in LocalPlayerEntities.Instance.PlayerIds)
            {
                if (clientId == m_gloveNetworking.OwnerClientId)
                {
                    continue;
                }

                var playerObjects = LocalPlayerEntities.Instance.GetPlayerObjects(clientId);
                var player = playerObjects.PlayerController;
                // 玩家已离开
                if (player == null)
                {
                    continue;
                }

                var team = player.NetworkedTeamComp.MyTeam;
                if (team != myTeam)
                {
                    if (player.RespawnController.KnockedOut.Value)
                    {
                        continue;
                    }

                    var targetDir = player.transform.position - transform.position;
                    targetDir.y = 0;
                    var forward = m_ballAnchor.forward;
                    forward.y = 0;

                    var angle = Mathf.Abs(Vector3.Angle(forward, targetDir));
                    if (angle <= m_maxAngleHomingTarget && angle < savedAngle)
                    {
                        savedAngle = angle;
                        m_selectedTargetId = clientId;
                        targetAvatar = playerObjects.Avatar;
                    }
                }
            }

            if (targetAvatar != null)
            {
                // 获取目标胸部位置
                var targetPos = targetAvatar.GetJointTransform(CAPI.ovrAvatar2JointType.Chest).position;

                if (m_targetIndicator == null)
                {
                    m_targetIndicator = Instantiate(m_targetIndicatorPrefab);
                }

                m_targetIndicator.SetActive(true);
                m_targetIndicator.transform.position = targetPos;
            }
            else
            {
                m_targetIndicator?.SetActive(false);
            }
        }

        /// <summary>
        /// 更新Chevron指示器（地面/球目标指示）
        /// </summary>
        private void UpdateCheveron()
        {
            var show = false;
            m_chevronOnABall = false;
            var movementIsEnabled = IsMovementEnabled != null && IsMovementEnabled();
            if (movementIsEnabled && m_actionPressed && CurrentState == State.Anchored && CurrentBall == null)
            {
                var foundFloor = false;
                var floorPosition = Vector3.zero;
                var foundBall = false;
                var ballPosition = Vector3.zero;
                // +1 额外缓冲，考虑碰撞体大小
                var hitCount = Physics.BoxCastNonAlloc(m_ballAnchor.position, new Vector3(0.1f, 0.07f, 0.1f),
                    m_ballAnchor.forward,
                    m_chevronRaycastResults, Quaternion.identity, m_travelDistance + 1,
                    ObjectLayers.DEFAULT_AND_BALL_SPAWN_MASK,
                    QueryTriggerInteraction.Collide);
                for (var i = 0; i < hitCount; ++i)
                {
                    var hit = m_chevronRaycastResults[i];
                    if (hit.transform.gameObject.GetComponent<Floor>() != null)
                    {
                        foundFloor = true;
                        floorPosition = hit.point;
                        show = true;
                    }

                    if (hit.transform.gameObject.GetComponent<BallNetworking>() != null)
                    {
                        ballPosition = hit.transform.position + Vector3.up * 0.25f; // 球半径为0.25
                        foundBall = true;
                        show = true;
                        break; // 找到第一个球就停止
                    }
                }

                if (foundFloor || foundBall)
                {
                    if (m_chevronVisual == null)
                    {
                        m_chevronVisual = Instantiate(m_cheveronPrefab);
                    }

                    var position = foundBall ? ballPosition : floorPosition;
                    m_chevronVisual.transform.position = position;
                }

                m_chevronOnABall = foundBall;
            }

            if (m_chevronVisual != null && m_chevronVisual.activeSelf != show)
            {
                m_chevronVisual.SetActive(show);
            }
        }

        /// <summary>
        /// 丢弃当前持有的球
        /// </summary>
        public void DropBall()
        {
            if (!CurrentBall)
            {
                return;
            }

            m_gloveNetworking.Grabbed = false;
            CurrentBall.Drop();
            CurrentBall = null;
        }

        /// <summary>
        /// 投掷当前持有的球
        /// </summary>
        /// <param name="chargeUpPct">蓄力百分比</param>
        private void ThrowBall(float chargeUpPct)
        {
            if (!CurrentBall)
            {
                return;
            }

            m_ballLaunchVFX.Play(true);
            m_gloveNetworking.Grabbed = false;
            if (CurrentBall.BallBehaviour is HomingBall ball)
            {
                // 追踪球投掷，带目标
                ball.Throw(m_ballAnchor.forward, m_selectedTargetId, chargeUpPct);
            }
            else
            {
                // 普通球投掷
                CurrentBall.Throw(m_ballAnchor.forward, chargeUpPct);
            }

            CurrentBall = null;
        }

        /// <summary>
        /// 发射手套（飞行出去）
        /// </summary>
        private void SendGlove()
        {
            TriedGrabbingBall = false;
            m_collider.enabled = true;
            CurrentState = State.Flying;
            m_gloveNetworking.Flying = true;
            var trans = transform;
            m_destination = trans.position + m_ballAnchor.forward * m_travelDistance;
            m_flyingBack = false;
        }

        /// <summary>
        /// 物理更新（处理手套飞行逻辑）
        /// </summary>
        private void FixedUpdate()
        {
            if (CurrentState == State.Flying)
            {
                var distance = Time.fixedDeltaTime * m_travelSpeed;
                var dest = m_flyingBack ? HandAnchor.position : m_destination;
                var dir = dest - transform.position;
                var normDir = dir.normalized;
                if (dir.sqrMagnitude <= distance * distance)
                {
                    // 到达目标点
                    // 先移动到目标点，然后开始返回
                    m_rigidbody.MovePosition(dest);

                    if (m_flyingBack)
                    {
                        CurrentState = State.Anchored;
                        m_gloveNetworking.Flying = false;
                        m_collider.enabled = false;
                        return;
                    }

                    // 更新剩余距离
                    distance = Mathf.Max(0, distance - dir.magnitude);
                    normDir = (HandAnchor.position - transform.position).normalized;
                    m_flyingBack = true;
                }

                if (distance > 0)
                {
                    m_rigidbody.MovePosition(transform.position + normDir * distance);
                }
            }
        }

        /// <summary>
        /// 手套击中地面时的回调
        /// </summary>
        /// <param name="dest">击中点</param>
        public void OnHitFloor(Vector3 dest)
        {
            if (IsMovementEnabled() && !m_chevronOnABall && !TriedGrabbingBall && m_gloveNetworking.IsOwner)
            {
                // 限制只能移动到各自队伍的区域
                var team = LocalPlayerEntities.Instance.LocalPlayerController.NetworkedTeamComp.MyTeam;
                if (team == NetworkedTeam.Team.NoTeam ||
                    (team == NetworkedTeam.Team.TeamA && dest.z < -1f) ||
                    (team == NetworkedTeam.Team.TeamB && dest.z > 1f))
                {
                    PlayerMovement.Instance.MoveTo(dest);
                    m_gloveNetworking.OnZip(dest);
                }
            }

            m_flyingBack = true;
        }

        /// <summary>
        /// 手套击中障碍物时的回调
        /// </summary>
        public void OnHitObstacle()
        {
            m_flyingBack = true;
        }

        /// <summary>
        /// 设置手套根节点的旋转和缩放
        /// </summary>
        /// <param name="root">根节点</param>
        /// <param name="side">左右手</param>
        /// <param name="withScale">是否设置缩放</param>
        public static void SetRootRotation(Transform root, GloveSide side, bool withScale = false)
        {
            root.localRotation = side == GloveSide.Left
                ? Quaternion.Euler(0, 90, 180)
                : Quaternion.Euler(0, -90, 0);

            if (withScale)
            {
                root.localScale = side == GloveSide.Left
                    ? new Vector3(-1, 1, 1)
                    : Vector3.one;
            }
        }
    }
}