// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Meta.Utilities
{
    /// <summary>
    /// 相机朝向组件
    /// 使游戏对象始终面向主相机，常用于UI元素或需要始终面向玩家的对象
    /// 支持可选的Y轴固定，避免对象在垂直方向上的旋转
    /// </summary>
    public class CameraFacing : MonoBehaviour
    {
        /// <summary>
        /// 是否固定Y轴
        /// 如果为true，则只在水平方向上旋转面向相机，忽略垂直角度差
        /// </summary>
        [SerializeField] private bool m_fixY = false;

        /// <summary>
        /// LateUpdate方法，在每帧最后更新旋转
        /// 计算从对象到相机的方向向量，并设置对象的旋转使其面向相机
        /// </summary>
        private void LateUpdate()
        {
            // 获取主相机的变换组件
            var cameraPosition = Camera.main.transform;

            // 计算从对象位置到相机位置的方向向量
            var dir = transform.position - cameraPosition.position;

            // 如果启用Y轴固定，则将Y分量设为0，只在水平面上旋转
            if (m_fixY)
            {
                dir.y = 0;
            }

            // 如果方向向量不为零，则设置对象旋转使其朝向该方向
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
