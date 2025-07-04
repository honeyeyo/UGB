// Copyright (c) MagnusLab Inc. and affiliates.

using Meta.Utilities;
using PongHub.App;
using UnityEngine;

namespace PongHub.Arena.VFX
{
    /// <summary>
    /// Manages the screen fx, the vignette for death and locomotion. This singleton can be access to enable and disable
    /// the vignettes.
    /// It also keeps the vignettes in sync with the main camera position.
    /// </summary>
    public class ScreenFXManager : Singleton<ScreenFXManager>
    {
        [SerializeField]
        [Tooltip("Death Vignette / 死亡晕影 - GameObject for death screen effect")]
        private GameObject m_deathVignette;

        [SerializeField]
        [Tooltip("Locomotion Vignette / 运动晕影 - GameObject for locomotion screen effect")]
        private GameObject m_locomotionVignette;

        private Transform m_mainCamera;
        private void LateUpdate()
        {
            if (m_mainCamera == null)
            {
                m_mainCamera = Camera.main.transform;
            }

            var thisTransform = transform;
            thisTransform.position = m_mainCamera.position;
            thisTransform.rotation = m_mainCamera.rotation;
        }

        public void ShowLocomotionFX(bool show)
        {
            m_locomotionVignette.SetActive(show && GameSettings.Instance.UseLocomotionVignette);
        }

        public void ShowDeathFX(bool show)
        {
            m_deathVignette.SetActive(show);
        }
    }
}