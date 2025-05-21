// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace PongHub.Networking.Pooling
{
    /// <summary>
    /// 网络对象池类
    /// 用于管理和复用网络对象,提供从池中获取和返回网络对象的公共方法
    /// 基于Unity的网络对象池教程实现
    /// https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/object-pooling/index.html
    /// </summary>
    public class NetworkObjectPool : NetworkBehaviour
    {
        /// <summary>
        /// 预制体配置结构体
        /// 用于配置需要池化的预制体及其预热数量
        /// </summary>
        [Serializable]
        private struct PoolConfigObject
        {
            public GameObject Prefab;      // 要池化的预制体
            public int PrewarmCount;       // 预热时创建的实例数量
        }

        #region Fields

        /// <summary>
        /// 单例实例
        /// 用于全局访问对象池
        /// </summary>
        public static NetworkObjectPool Singleton { get; private set; }

        /// <summary>
        /// 预制体配置列表
        /// 在Inspector中配置需要池化的预制体
        /// </summary>
        [SerializeField] private List<PoolConfigObject> m_pooledPrefabsList = new();

        /// <summary>
        /// 初始化标志
        /// 防止重复初始化
        /// </summary>
        private bool m_hasInitialized;

        /// <summary>
        /// 已注册的预制体集合
        /// 用于快速查找预制体是否已注册
        /// </summary>
        private readonly HashSet<GameObject> m_prefabs = new();

        /// <summary>
        /// 对象池字典
        /// 键为预制体,值为该预制体的对象队列
        /// </summary>
        private readonly Dictionary<GameObject, Queue<NetworkObject>> m_pooledObjects = new();

        #endregion

        #region Lifecycle

        /// <summary>
        /// 初始化单例
        /// 如果已存在实例则销毁当前对象
        /// </summary>
        private void Awake()
        {
            if (Singleton != null && Singleton != this)
                Destroy(gameObject);
            else
                Singleton = this;
        }

        /// <summary>
        /// 网络对象生成时初始化对象池
        /// </summary>
        public override void OnNetworkSpawn()
        {
            InitializePool();
        }

        /// <summary>
        /// 网络对象销毁时清理对象池
        /// </summary>
        public override void OnNetworkDespawn()
        {
            ClearPool();
        }

        /// <summary>
        /// 验证预制体配置
        /// 确保所有预制体都有NetworkObject组件
        /// </summary>
        private void OnValidate()
        {
            for (var i = 0; i < m_pooledPrefabsList.Count; i++)
            {
                var prefab = m_pooledPrefabsList[i].Prefab;
                if (prefab != null)
                {
                    Assert.IsNotNull(prefab.GetComponent<NetworkObject>(),
                        $"{nameof(NetworkObjectPool)}: Pooled prefab \"{prefab.name}\" at index {i} has no {nameof(NetworkObject)} component.");
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 从对象池获取网络对象
        /// 返回放置在原点位置的网络对象
        /// </summary>
        /// <param name="prefab">要获取的预制体</param>
        /// <returns>获取的网络对象</returns>
        public NetworkObject GetNetworkObject(GameObject prefab)
        {
            return GetNetworkObjectInternal(prefab, Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        /// 从对象池获取网络对象
        /// 返回放置在指定位置和旋转的网络对象
        /// </summary>
        /// <param name="prefab">要获取的预制体</param>
        /// <param name="position">生成位置</param>
        /// <param name="rotation">生成旋转</param>
        /// <returns>获取的网络对象</returns>
        public NetworkObject GetNetworkObject(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return GetNetworkObjectInternal(prefab, position, rotation);
        }

        /// <summary>
        /// 将网络对象返回到对象池
        /// </summary>
        /// <param name="networkObject">要返回的网络对象</param>
        /// <param name="prefab">生成该网络对象的预制体</param>
        public void ReturnNetworkObject(NetworkObject networkObject, GameObject prefab)
        {
            var go = networkObject.gameObject;
            go.SetActive(false);
            m_pooledObjects[prefab].Enqueue(networkObject);
        }

        /// <summary>
        /// 添加新的预制体到对象池
        /// </summary>
        /// <param name="prefab">要池化的预制体</param>
        /// <param name="prewarmCount">预热时创建的实例数量</param>
        public void AddPrefab(GameObject prefab, int prewarmCount = 0)
        {
            var networkObject = prefab.GetComponent<NetworkObject>();

            Assert.IsNotNull(networkObject, $"{nameof(prefab)} must have {nameof(networkObject)} component.");
            Assert.IsFalse(m_prefabs.Contains(prefab), $"Prefab {prefab.name} is already registered in the pool.");

            RegisterPrefabInternal(prefab, prewarmCount);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 内部获取网络对象方法
        /// 从对象池获取或创建新的网络对象
        /// </summary>
        private NetworkObject GetNetworkObjectInternal(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var queue = m_pooledObjects[prefab];

            var networkObject = queue.Count > 0 ? queue.Dequeue() : CreateInstance(prefab).GetComponent<NetworkObject>();

            var go = networkObject.gameObject;

            go.transform.SetPositionAndRotation(position, rotation);
            go.SetActive(true);

            return networkObject;
        }

        /// <summary>
        /// 初始化对象池
        /// 注册所有配置的预制体
        /// </summary>
        private void InitializePool()
        {
            if (m_hasInitialized) return;
            foreach (var configObject in m_pooledPrefabsList)
            {
                RegisterPrefabInternal(configObject.Prefab, configObject.PrewarmCount);
            }
        }

        /// <summary>
        /// 注册预制体到对象池
        /// 创建预热数量的实例并添加到对象池
        /// </summary>
        /// <param name="prefab">要注册的预制体</param>
        /// <param name="prewarmCount">预热实例数量</param>
        private void RegisterPrefabInternal(GameObject prefab, int prewarmCount)
        {
            _ = m_prefabs.Add(prefab);

            var prefabQueue = new Queue<NetworkObject>();
            m_pooledObjects[prefab] = prefabQueue;
            for (var i = 0; i < prewarmCount; i++)
            {
                var go = CreateInstance(prefab);
                ReturnNetworkObject(go.GetComponent<NetworkObject>(), prefab);
            }

            _ = NetworkManager.Singleton.PrefabHandler.AddHandler(prefab, new PooledPrefabInstanceHandler(prefab, this));
        }

        /// <summary>
        /// 创建预制体实例
        /// </summary>
        /// <param name="prefab">要实例化的预制体</param>
        /// <returns>创建的实例</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private GameObject CreateInstance(GameObject prefab)
        {
            return Instantiate(prefab);
        }

        /// <summary>
        /// 清理对象池
        /// 移除所有预制体的处理器并清空对象池
        /// </summary>
        private void ClearPool()
        {
            foreach (var prefab in m_prefabs)
            {
                _ = NetworkManager.Singleton.PrefabHandler.RemoveHandler(prefab);
            }

            m_pooledObjects.Clear();
        }

        #endregion
    }
}
