// Copyright (c) MagnusLab Inc. and affiliates.

using UnityEngine;

namespace PongHub.Arena.Spectator
{
    /// <summary>
    /// Base class for all spectator items so we can set the team color on the item.
    /// </summary>
    public class SpectatorItem : MonoBehaviour
    {
        private static readonly int s_attachmentColorID = Shader.PropertyToID("_Attachment_Color");

        [SerializeField] private Renderer m_renderer;
        private MaterialPropertyBlock m_materialBlock;
        public void SetColor(Color color)
        {
            m_materialBlock ??= new MaterialPropertyBlock();
            m_renderer.GetPropertyBlock(m_materialBlock);
            m_materialBlock.SetColor(s_attachmentColorID, color);
            m_renderer.SetPropertyBlock(m_materialBlock);
        }
    }
}