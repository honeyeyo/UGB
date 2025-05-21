// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using Meta.Utilities;
using PongHub.App;
using PongHub.Arena.Player.Menu;
using PongHub.Arena.Services;
using PongHub.Arena.Spectator;
using PongHub.Arena.VFX;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

namespace PongHub.Arena.Player
{
    /// <summary>
    /// 玩家输入控制器,负责处理玩家的所有输入操作。
    /// 根据玩家当前状态处理相应的输入并调用对应方法。
    /// 继承自Singleton单例模式基类,确保全局只有一个实例。
    /// </summary>
    public class PlayerInputController : Singleton<PlayerInputController>
    {
        /// <summary>
        /// 玩家输入组件,自动获取引用
        /// </summary>
        [SerializeField, AutoSet] private PlayerInput m_input;

        /// <summary>
        /// 玩家游戏内菜单组件引用
        /// </summary>
        [SerializeField] private PlayerInGameMenu m_playerMenu;

        /// <summary>
        /// 观战者网络组件引用
        /// </summary>
        private SpectatorNetwork m_spectatorNet = null;

        /// <summary>
        /// 是否启用自由移动
        /// </summary>
        private bool m_freeLocomotionEnabled = true;

        /// <summary>
        /// 是否启用输入
        /// </summary>
        public bool InputEnabled { get; set; } = true;

        /// <summary>
        /// 是否启用移动
        /// </summary>
        public bool MovementEnabled { get; set; } = true;

        /// <summary>
        /// 上一帧是否在移动
        /// </summary>
        private bool m_wasMoving = false;

        /// <summary>
        /// 移动输入动作引用
        /// </summary>
        private InputAction m_moveAction;

        /// <summary>
        /// 设置观战者模式
        /// </summary>
        /// <param name="spectator">观战者网络组件</param>
        public void SetSpectatorMode(SpectatorNetwork spectator)
        {
            m_spectatorNet = spectator;
            // 根据是否为观战模式切换输入动作映射
            m_input.SwitchCurrentActionMap(m_spectatorNet != null ? "Spectator" : "Player");
        }

        /// <summary>
        /// 更新游戏设置
        /// </summary>
        public void OnSettingsUpdated()
        {
            m_freeLocomotionEnabled = !GameSettings.Instance.IsFreeLocomotionDisabled;
            PlayerMovement.Instance.RotationEitherThumbstick = !m_freeLocomotionEnabled;
        }

        /// <summary>
        /// 初始化时设置移动模式
        /// </summary>
        private void Start()
        {
            m_freeLocomotionEnabled = !GameSettings.Instance.IsFreeLocomotionDisabled;
            PlayerMovement.Instance.RotationEitherThumbstick = !m_freeLocomotionEnabled;
        }

        /// <summary>
        /// 销毁时重置旋转设置
        /// </summary>
        private void OnDestroy()
        {
            PlayerMovement.Instance.RotationEitherThumbstick = true;
        }

        /// <summary>
        /// 每帧更新处理玩家输入
        /// </summary>
        private void Update()
        {
            if (m_spectatorNet == null)
            {
                ProcessPlayerInput();
            }
        }

        /// <summary>
        /// 处理向左快速转向
        /// </summary>
        public void OnSnapTurnLeft(CallbackContext context) => OnSnapTurn(context, false);

        /// <summary>
        /// 处理向右快速转向
        /// </summary>
        public void OnSnapTurnRight(CallbackContext context) => OnSnapTurn(context, true);

        /// <summary>
        /// 非自由移动模式下的向左快速转向
        /// </summary>
        public void OnSnapTurnLeftNoFree(CallbackContext context)
        {
            if (PlayerMovement.Instance.RotationEitherThumbstick)
                OnSnapTurnLeft(context);
        }

        /// <summary>
        /// 非自由移动模式下的向右快速转向
        /// </summary>
        public void OnSnapTurnRightNoFree(CallbackContext context)
        {
            if (PlayerMovement.Instance.RotationEitherThumbstick)
                OnSnapTurnRight(context);
        }

        /// <summary>
        /// 执行快速转向
        /// </summary>
        /// <param name="context">输入上下文</param>
        /// <param name="toRight">是否向右转向</param>
        private void OnSnapTurn(CallbackContext context, bool toRight)
        {
            if (context.performed)
                PlayerMovement.Instance.DoSnapTurn(toRight);
        }

        /// <summary>
        /// 处理菜单按钮
        /// </summary>
        public void OnMenuButton(CallbackContext context)
        {
            if (context.performed)
                m_playerMenu.Toggle();
        }

        /// <summary>
        /// 处理观战者左扳机
        /// </summary>
        public void OnSpectatorTriggerLeft(CallbackContext context)
        {
            if (context.phase is InputActionPhase.Performed)
                m_spectatorNet?.TriggerLeftAction();
        }

        /// <summary>
        /// 处理观战者右扳机
        /// </summary>
        public void OnSpectatorTriggerRight(CallbackContext context)
        {
            if (context.phase is InputActionPhase.Performed)
                m_spectatorNet?.TriggerRightAction();
        }

        /// <summary>
        /// 处理移动输入
        /// </summary>
        public void OnMove(CallbackContext context)
        {
            m_moveAction = context.phase is InputActionPhase.Disabled ? null : context.action;
        }

        /// <summary>
        /// 处理左手投掷
        /// </summary>
        public void OnThrowLeft(CallbackContext context)
        {
            if (!InputEnabled) return;

            var glove = LocalPlayerEntities.Instance.LeftGloveHand;
            var gloveArmature = LocalPlayerEntities.Instance.LeftGloveArmature;
            if (context.phase is InputActionPhase.Performed)
                OnThrow(glove, gloveArmature);
            else if (context.phase is InputActionPhase.Canceled)
                OnRelease(glove, gloveArmature);
        }

        /// <summary>
        /// 处理右手投掷
        /// </summary>
        public void OnThrowRight(CallbackContext context)
        {
            if (!InputEnabled) return;

            var glove = LocalPlayerEntities.Instance.RightGloveHand;
            var gloveArmature = LocalPlayerEntities.Instance.RightGloveArmature;
            if (context.phase is InputActionPhase.Performed)
                OnThrow(glove, gloveArmature);
            else if (context.phase is InputActionPhase.Canceled)
                OnRelease(glove, gloveArmature);
        }

        /// <summary>
        /// 处理左手护盾
        /// </summary>
        public void OnShieldLeft(CallbackContext context)
        {
            if (!InputEnabled) return;

            OnShield(Glove.GloveSide.Left, context.phase is InputActionPhase.Performed);
        }

        /// <summary>
        /// 处理右手护盾
        /// </summary>
        public void OnShieldRight(CallbackContext context)
        {
            if (!InputEnabled) return;

            OnShield(Glove.GloveSide.Right, context.phase is InputActionPhase.Performed);
        }

        /// <summary>
        /// 处理玩家输入,包括移动和相关特效
        /// </summary>
        private void ProcessPlayerInput()
        {
            if (!InputEnabled)
            {
                if (m_wasMoving)
                {
                    ScreenFXManager.Instance.ShowLocomotionFX(false);
                    m_wasMoving = false;
                }
                return;
            }

            if (MovementEnabled && m_freeLocomotionEnabled)
            {
                var direction = m_moveAction?.ReadValue<Vector2>() ?? default;
                if (direction != Vector2.zero)
                {
                    var dir = new Vector3(direction.x, 0, direction.y);
                    PlayerMovement.Instance.WalkInDirectionRelToForward(dir);
                    if (!m_wasMoving)
                    {
                        ScreenFXManager.Instance.ShowLocomotionFX(true);
                    }

                    m_wasMoving = true;
                }
                else if (m_wasMoving)
                {
                    ScreenFXManager.Instance.ShowLocomotionFX(false);
                    m_wasMoving = false;
                }
            }
        }

        /// <summary>
        /// 处理护盾状态
        /// </summary>
        /// <param name="side">手套侧边</param>
        /// <param name="state">护盾状态</param>
        private static void OnShield(Glove.GloveSide side, bool state)
        {
            var playerController = LocalPlayerEntities.Instance.LocalPlayerController;
            if (state)
                playerController.TriggerShield(side);
            else
                playerController.StopShieldServerRPC(side);
        }

        /// <summary>
        /// 处理释放动作
        /// </summary>
        /// <param name="glove">手套组件</param>
        /// <param name="gloveArmature">手套骨骼组件</param>
        private static void OnRelease(Glove glove, GloveArmatureNetworking gloveArmature)
        {
            if (glove && gloveArmature)
            {
                glove.TriggerAction(true, gloveArmature.SpringCompression);
                gloveArmature.Activated = false;
            }
        }

        /// <summary>
        /// 处理投掷动作
        /// </summary>
        /// <param name="glove">手套组件</param>
        /// <param name="gloveArmature">手套骨骼组件</param>
        private static void OnThrow(Glove glove, GloveArmatureNetworking gloveArmature)
        {
            if (glove)
            {
                glove.TriggerAction(false);
            }

            if (gloveArmature)
            {
                gloveArmature.Activated = true;
            }
        }
    }
}