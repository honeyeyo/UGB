// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Netcode.Components;
using UnityEngine;

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// 客户端网络变换组件
    /// 用于同步具有客户端侧更改的变换，包括主机
    /// 不支持纯服务器作为所有者的情况
    /// 对于始终由服务器拥有的变换，请使用NetworkTransform
    /// </summary>
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        /// <summary>
        /// 是否忽略更新
        /// 用于临时禁用变换同步更新
        /// </summary>
        public bool IgnoreUpdates { get; set; }

        /// <summary>
        /// 更新是否可以提交变换
        /// 只有网络对象的所有者才能提交变换更改
        /// </summary>
        protected void UpdateCanCommit() => CanCommitToTransform = NetworkObject.IsOwner;

        /// <summary>
        /// 网络对象生成时调用
        /// 初始化组件并处理所有者的初始位置设置
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            UpdateCanCommit();

            if (CanCommitToTransform)
            {
                // 解决已知问题的工作方案
                // https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/issues/1560#issuecomment-1013217835

                // 临时禁用缩放阈值
                var cache = ScaleThreshold;
                ScaleThreshold = -1;

                // 获取当前变换数据
                var position = InLocalSpace ? transform.localPosition : transform.position;
                var rotation = InLocalSpace ? transform.localRotation : transform.rotation;
                var scale = transform.localScale;

                // 传送到当前位置以确保同步
                Teleport(position, rotation, scale);

                // 恢复缩放阈值
                ScaleThreshold = cache;
            }
        }

        /// <summary>
        /// 每帧更新方法
        /// 更新提交权限并处理变换同步，同时处理忽略更新的情况
        /// </summary>
        protected override void Update()
        {
            UpdateCanCommit();

            var thisTransform = transform;

            // 缓存当前本地变换数据
            var cacheLocalPosition = thisTransform.localPosition;
            var cacheLocalRotation = thisTransform.localRotation;

            // 调用基类更新方法
            base.Update();

            // 如果设置了忽略更新，恢复缓存的变换数据
            if (IgnoreUpdates)
            {
                // 恢复本地位置和旋转，防止网络更新覆盖本地更改
                thisTransform.localPosition = cacheLocalPosition;
                thisTransform.localRotation = cacheLocalRotation;
            }

            if (NetworkManager != null && (NetworkManager.IsConnectedClient || NetworkManager.IsListening))
            {
                if (CanCommitToTransform)
                {
                    TryCommitTransformToServer(transform, NetworkManager.LocalTime.Time);
                }
            }
        }

        /// <summary>
        /// 在网络对象销毁时调用
        /// 清理资源和重置状态
        /// </summary>
        public override void OnNetworkDespawn()
        {
            // 重置更新权限
            CanCommitToTransform = false;

            // 重置忽略更新标志
            IgnoreUpdates = false;

            base.OnNetworkDespawn();
        }

        /// <summary>
        /// 强制同步当前变换状态
        /// 用于确保变换状态在特定时刻的同步
        /// </summary>
        public void ForceSyncTransform()
        {
            if (CanCommitToTransform)
            {
                var position = InLocalSpace ? transform.localPosition : transform.position;
                var rotation = InLocalSpace ? transform.localRotation : transform.rotation;
                var scale = transform.localScale;

                Teleport(position, rotation, scale);
            }
        }

        public override void OnGainedOwnership()
        {
            UpdateCanCommit();
            base.OnGainedOwnership();
        }

        public override void OnLostOwnership()
        {
            UpdateCanCommit();
            base.OnLostOwnership();
        }

        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
