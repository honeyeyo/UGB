// Copyright (c) MagnusLab Inc. and affiliates.

using Meta.Utilities;
using PongHub.Arena.Player.Respawning;
using UnityEngine;

namespace PongHub.Arena.Player
{
    /// <summary>
    /// You own a cat? You can spawn or despawn the cat as an owner. This also is an interface for the Cat to get
    /// instructions on how to behave.
    /// </summary>
    public class CatOwner : MonoBehaviour
    {
        [SerializeField] private CatAI m_catPrefab;
        [SerializeField] private float m_spawnCatDistance = 2;
        [SerializeField, AutoSet] private RespawnController m_respawnController;
        [SerializeField, AutoSet] private PlayerControllerNetwork m_playerControllerNetwork;
        private CatAI m_cat;

        private void Start()
        {
            if (m_respawnController)
            {
                m_respawnController.OnKnockedOutEvent += OnOwnerKnockedOut;
                m_respawnController.OnRespawnCompleteEvent += OnOwnerRespawn;
            }

            // 移除了无敌状态相关代码
        }

        private void OnDestroy()
        {
            if (m_respawnController)
            {
                m_respawnController.OnKnockedOutEvent -= OnOwnerKnockedOut;
                m_respawnController.OnRespawnCompleteEvent -= OnOwnerRespawn;
            }

            // 移除了无敌状态相关代码
        }

        public void SpawnCat()
        {
            if (m_cat != null)
            {
                // spawn maximum 1 cat
                return;
            }

            var thisTransform = transform;
            var currentPos = thisTransform.position;
            // spawn in front of player
            var spawnPos = currentPos + thisTransform.forward * m_spawnCatDistance;
            m_cat = Instantiate(m_catPrefab, spawnPos, Quaternion.FromToRotation(Vector3.forward, currentPos - spawnPos));
            m_cat.SetOwner(this);

            if (m_respawnController.IsKnockedOut)
            {
                OnOwnerKnockedOut();
            }

            // 移除了无敌状态相关代码
        }

        public void DeSpawnCat()
        {
            if (m_cat != null)
            {
                Destroy(m_cat.gameObject);
            }
        }

        private void OnOwnerKnockedOut()
        {
            m_cat?.UnfollowOwner();
        }

        private void OnOwnerRespawn()
        {
            m_cat?.FollowOwner();
        }

        // 移除了无敌状态相关方法
    }
}