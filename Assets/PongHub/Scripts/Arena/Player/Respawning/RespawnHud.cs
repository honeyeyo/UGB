// Copyright (c) MagnusLab Inc. and affiliates.

using System;
using TMPro;
using UnityEngine;

namespace PongHub.Arena.Player.Respawning
{
    /// <summary>
    /// Hud that tracks the players camera to show the respawn count down and message.
    /// </summary>
    public class RespawnHud : MonoBehaviour
    {
        public event Action RespawnInitiated;

        [SerializeField] private TMP_Text m_text;
        [SerializeField] private GameObject m_button;

        public void DisplayText(bool enable)
        {
            m_text.gameObject.SetActive(enable);
        }

        public void DisplayRespawnButton(bool enable)
        {
            m_button.SetActive(enable);
        }

        public void UpdateText(float time)
        {
            SetText($"ELIMINATED\n" +
                          $"{time:F0}");
        }

        public void SetText(string text)
        {
            m_text.text = text;
        }

        public void OnRespawnButtonSelected()
        {
            RespawnInitiated?.Invoke();
        }

        private void OnGUI()
        {
            if (!m_button.activeSelf) return;

            if (GUILayout.Button("Respawn"))    // Adds a respawn button to the editor for debugging
            {
                OnRespawnButtonSelected();
            }
        }
    }
}
