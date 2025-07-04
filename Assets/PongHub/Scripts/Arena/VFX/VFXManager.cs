// Copyright (c) MagnusLab Inc. and affiliates.

using System.Collections.Generic;
using Meta.Utilities;
using UnityEngine;

namespace PongHub.Arena.VFX
{
    /// <summary>
    /// Manages the game vfx.
    /// Keeps a circular list of the hit vfx and play them sequentially.
    /// </summary>
    public class VFXManager : Singleton<VFXManager>
    {
        [SerializeField]
        [Tooltip("Hit VFXs / 击中特效 - List of particle systems for hit effects")]
        private List<ParticleSystem> m_hitVfxs;

        private int m_hitVFXIndex;

        public void PlayHitVFX(Vector3 position, Vector3 forward)
        {
            var fx = m_hitVfxs[m_hitVFXIndex++];
            var trans = fx.transform;
            trans.position = position;
            trans.forward = forward;
            fx.Play(true);
            if (m_hitVFXIndex >= m_hitVfxs.Count)
            {
                m_hitVFXIndex = 0;
            }
        }
    }
}