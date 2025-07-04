// Copyright (c) MagnusLab Inc. and affiliates.

using TMPro;
using PongHub.App;
using Unity.Netcode;
using UnityEngine;

namespace PongHub.Arena.Player.Menu
{
    /// <summary>
    /// Menu that shows some debug info to the user.
    /// </summary>
    public class DebugInfoMenu : BasePlayerMenuView
    {
        [SerializeField]
        [Tooltip("Server Time Text / 服务器时间文本 - Text component to display server time")]
        private TMP_Text m_serverTimeText;

        [SerializeField]
        [Tooltip("Ping Time Text / 延迟时间文本 - Text component to display network ping time")]
        private TMP_Text m_pingTimeText;

        [SerializeField]
        [Tooltip("Region Text / 区域文本 - Text component to display network region")]
        private TMP_Text m_regionText;

        [SerializeField]
        [Tooltip("FPS Text / 帧率文本 - Text component to display frames per second")]
        private TMP_Text m_fpsText;

        private void OnEnable()
        {
            m_regionText.text = NetworkRegionMapping.GetRegionShortName(PHApplication.Instance.NetworkLayer.GetRegion());
        }

        public override void OnUpdate()
        {
            if (NetworkManager.Singleton.IsListening)
            {
                m_serverTimeText.text = NetworkManager.Singleton.ServerTime.Time.ToString("0.###");

                m_pingTimeText.text =
                    $"{NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(0)} ms";
            }

            m_fpsText.text = (1f / Time.smoothDeltaTime).ToString("N0");
        }
    }
}