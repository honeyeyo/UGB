// Copyright (c) MagnusLab Inc. and affiliates.

using Meta.Utilities;
using PongHub.Arena.Player.Respawning;
using PongHub.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace PongHub.Arena.Player
{
    /// <summary>
    /// 玩家HUD控制器,负责将HUD界面保持在玩家相机前方,并进行平滑跟随。
    /// 继承自Singleton单例模式基类,确保全局只有一个实例。
    /// </summary>
    public class PlayerHud : Singleton<PlayerHud>
    {
        #region Properties
        /// <summary>
        /// 获取重生HUD界面的引用
        /// </summary>
        public RespawnHud RespawnHud => m_respawnHud;
        #endregion

        /// <summary>
        /// VR相机中心眼睛锚点的Transform引用
        /// </summary>
        [SerializeField] private Transform m_centerEyeAnchor;

        /// <summary>
        /// HUD旋转插值系数,控制旋转跟随的平滑程度
        /// </summary>
        [SerializeField] private float m_slerpValueRotation = 1f;

        /// <summary>
        /// HUD高度插值系数,控制高度跟随的平滑程度
        /// </summary>
        [SerializeField] private float m_lerpValueHeight = 0.8f;

        /// <summary>
        /// 重生HUD界面组件引用
        /// </summary>
        [SerializeField] private RespawnHud m_respawnHud;

        /// <summary>
        /// 初始化时检查必要组件并重置HUD位置
        /// </summary>
        private void Start()
        {
            // 确保中心眼睛锚点已正确赋值
            Assert.IsNotNull(m_centerEyeAnchor, $"Forgot to serialize {nameof(m_centerEyeAnchor)}");

            ResetHudPosition();
        }

        /// <summary>
        /// 每帧更新HUD的位置和旋转,实现平滑跟随
        /// </summary>
        private void Update()
        {
            // 获取相机前方向量(忽略Y轴)作为HUD朝向
            var lookDirection = m_centerEyeAnchor.forward.SetY(0).normalized;
            // 如果向量为零,默认朝向右方
            if (lookDirection == Vector3.zero)
                lookDirection = Vector3.right;

            // 计算目标旋转角度
            var targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);

            // 使用球形插值平滑过渡到目标旋转
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, m_slerpValueRotation * Time.deltaTime);

            // 计算目标高度位置
            var targetPosition = transform.position.SetY(m_centerEyeAnchor.position.y);

            // 使用线性插值平滑过渡到目标高度
            transform.position = Vector3.Lerp(transform.position, targetPosition, m_lerpValueHeight * Time.deltaTime);
        }

        /// <summary>
        /// 重置HUD到初始位置和旋转状态
        /// </summary>
        private void ResetHudPosition()
        {
            // 设置初始旋转,使HUD面向相机前方
            transform.rotation = Quaternion.LookRotation(m_centerEyeAnchor.forward.SetY(0).normalized, Vector3.up);

            // 设置初始高度与相机眼睛高度一致
            transform.position = transform.position.SetY(m_centerEyeAnchor.position.y);
        }
    }
}
