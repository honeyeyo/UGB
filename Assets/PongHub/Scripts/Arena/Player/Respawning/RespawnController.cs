// Copyright (c) MagnusLab Inc. and affiliates.

using System;
using System.Collections;
using Meta.Utilities;
using PongHub.Arena.Gameplay;
using PongHub.Arena.Services;
using PongHub.Arena.VFX;
using Unity.Netcode;
using UnityEngine;

namespace PongHub.Arena.Player.Respawning
{
    /// <summary>
    /// Handles the state of a player when they get knocked out. Set the state of the player on knock out and handles
    /// respawn timing and player visual state.
    /// </summary>
    [RequireComponent(typeof(NetworkedTeam))]
    public class RespawnController : NetworkBehaviour
    {
        #region Networked Variables

        public NetworkVariable<bool> KnockedOut = new();

        public NetworkVariable<float> RespawnTimer = new();

        #endregion

        #region Fields

        private RespawnHud m_hud;
        private const float RESPAWN_TIMER_VALUE = 6f;

        [SerializeField] private Collider m_collider;

        [SerializeField] private GameObject m_respawnEffect;

        [SerializeField, AutoSet] private NetworkedTeam m_networkedTeamComponent;

        [SerializeField, AutoSet] private PlayerControllerNetwork m_playerController;

        [SerializeField] private PlayerNameVisual m_playerNameVisual;

        #endregion

        #region LifeCycle

        public bool IsKnockedOut => KnockedOut.Value;
        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                PlayerHud.WhenInstantiated(_ => OnHudInstantiated());
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                m_hud?.DisplayText(false);
            }
        }

        private void OnEnable()
        {
            KnockedOut.OnValueChanged += OnKnockedOut;
        }

        private void OnDisable()
        {
            KnockedOut.OnValueChanged -= OnKnockedOut;
            if (m_hud != null)
                m_hud.RespawnInitiated -= RequestRespawnServerRpc;
        }

        #endregion

        public Action OnKnockedOutEvent;
        public Action OnRespawnCompleteEvent;

        [ContextMenu("Knockout Player")]
        public void KnockOutPlayer()
        {
            if (!IsServer) return;

            if (KnockedOut.Value) return;

            KnockedOut.Value = true; // Notify everyone about new knockout state
            _ = StartCoroutine(StartRespawnCountdown()); // Start countdown on server
        }

        private IEnumerator StartRespawnCountdown()
        {
            RespawnTimer.Value = RESPAWN_TIMER_VALUE;
            while (RespawnTimer.Value > 0)
            {
                yield return null;
                RespawnTimer.Value -= Time.deltaTime;
            }

            KnockedOut.Value = false;
        }

        private void OnKnockedOut(bool wasKnockedOut, bool isKnockedOut)
        {
            if (IsOwner)
            {
                if (m_hud == null)
                {
                    Debug.LogWarning("Failed starting respawn countdown. Could not find player hud.");
                    return;
                }

                // 移除了无敌状态相关代码
                if (!wasKnockedOut && isKnockedOut) // Just got knocked out
                {
                    m_hud.DisplayText(true);
                    m_hud.DisplayRespawnButton(false);
                    m_hud.UpdateText(RESPAWN_TIMER_VALUE);
                    ScreenFXManager.Instance.ShowDeathFX(true);
                    m_collider.enabled = false;

                    PlayerInputController.Instance.InputEnabled = false;

                    _ = StartCoroutine(DespawnPlayer());
                }

                if (wasKnockedOut && !isKnockedOut) // No longer knocked out
                {
                    m_hud.SetText("Respawning");
                    RequestRespawnServerRpc();
                    // m_hud.DisplayText(false);
                    //m_hud.DisplayRespawnButton(true);
                }
            }
            else
            {
                if (!wasKnockedOut && isKnockedOut) // Just got knocked out
                {
                    m_collider.enabled = false;
                    _ = StartCoroutine(DespawnPlayer());
                }
            }

            if (!wasKnockedOut && isKnockedOut)
            {
                OnKnockedOutEvent?.Invoke();
            }
        }

        private void Update()
        {
            if (KnockedOut.Value && m_hud)
            {
                m_hud.UpdateText(RespawnTimer.Value);
            }
        }

        private void OnHudInstantiated()
        {
            m_hud = PlayerHud.Instance.RespawnHud;

            if (IsOwner)
            {
                m_hud.RespawnInitiated += RequestRespawnServerRpc;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestRespawnServerRpc()
        {
            if (RespawnTimer.Value <= 0)
            {
                SpawningManagerBase.Instance.GetRespawnPoint(
                    OwnerClientId,
                    m_networkedTeamComponent.MyTeam,
                    out var position,
                    out var rotation);
                OnRespawnClientRpc(position, rotation);
            }
        }

        [ClientRpc]
        private void OnRespawnClientRpc(Vector3 position, Quaternion rotation)
        {
            if (IsOwner)
            {
                m_hud.DisplayRespawnButton(false);

                PlayerMovement.Instance.TeleportTo(position, rotation); // Move player to spawn
            }

            _ = StartCoroutine(ShowPlayerDelayed());
        }

        private IEnumerator DespawnPlayer()
        {
            var timer = 2f;
            var avatar = GetComponentInChildren<PlayerAvatarEntity>();
            var material = avatar.Material;
            material.SetKeyword("ENABLE_CUSTOM_EFFECT", true);

            while (timer > 0)
            {
                timer -= Time.deltaTime;
                _ = material.SetFloat("_DisAmount", Mathf.Lerp(-1.2f, 0.8f, timer / 2f));
                avatar.ApplyMaterial();
                yield return null;
            }

            m_playerNameVisual.SetVisibility(false);
            avatar.Hide();
            yield return null;
            material.SetKeyword("ENABLE_CUSTOM_EFFECT", false);
            avatar.ApplyMaterial();
        }

        private IEnumerator ShowPlayerDelayed()
        {
            yield return new WaitForSeconds(0.25f);
            m_respawnEffect.SetActive(true);
            var timer = 2f;

            var avatar = GetComponentInChildren<PlayerAvatarEntity>();
            var material = avatar.Material;
            material.SetKeyword("ENABLE_CUSTOM_EFFECT", true);
            material.SetKeyword("ENABLE_GHOST_EFFECT", true);
            avatar.Show();

            while (timer > 0)
            {
                timer -= Time.deltaTime;
                _ = material.SetFloat("_DisAmount", Mathf.Lerp(0.8f, -1.2f, timer / 2f));
                avatar.ApplyMaterial();
                yield return null;
            }

            material.SetKeyword("ENABLE_CUSTOM_EFFECT", false);
            avatar.ApplyMaterial();

            m_playerNameVisual.SetVisibility(true);

            if (IsOwner)
            {
                ScreenFXManager.Instance.ShowDeathFX(false);
                m_hud.DisplayText(false);
                PlayerInputController.Instance.InputEnabled = true;
            }

            // 等待1秒后完全恢复
            yield return new WaitForSeconds(1f);
            m_collider.enabled = true;
            m_respawnEffect.SetActive(false);
            material.SetKeyword("ENABLE_GHOST_EFFECT", false);
            avatar.ApplyMaterial();
            OnRespawnCompleteEvent?.Invoke();
        }
    }
}