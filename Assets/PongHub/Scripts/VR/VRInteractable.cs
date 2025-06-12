using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using PongHub.Core;

namespace PongHub.VR
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public class VRInteractable : MonoBehaviour
    {
        [Header("交互设置")]
        [SerializeField] protected bool m_isGrabbable = true;
        [SerializeField] protected bool m_isThrowable = true;
        [SerializeField] protected float m_throwForce = 10f;
        [SerializeField] protected float m_throwAngle = 45f;

        [Header("交互效果")]
        [SerializeField] protected GameObject m_hoverEffect;
        [SerializeField] protected GameObject m_grabEffect;
        [SerializeField] protected AudioClip m_hoverSound;
        [SerializeField] protected AudioClip m_grabSound;
        [SerializeField] protected AudioClip m_releaseSound;

        [Header("输入动作")]
        [SerializeField] protected InputActionReference m_gripAction;
        [SerializeField] protected InputActionReference m_triggerAction;

        protected XRGrabInteractable m_grabInteractable;
        protected Rigidbody m_rigidbody;
        protected AudioSource m_audioSource;
        protected bool m_isGrabbed;
        protected bool m_isHovered;
        protected XRController m_currentController;

        protected virtual void Awake()
        {
            m_grabInteractable = GetComponent<XRGrabInteractable>();
            m_rigidbody = GetComponent<Rigidbody>();
            m_audioSource = GetComponent<AudioSource>();

            SetupInteractable();
        }

        protected virtual void OnEnable()
        {
            EnableInputActions();
        }

        protected virtual void OnDisable()
        {
            DisableInputActions();
        }

        protected virtual void EnableInputActions()
        {
            m_gripAction?.action.Enable();
            m_triggerAction?.action.Enable();
        }

        protected virtual void DisableInputActions()
        {
            m_gripAction?.action.Disable();
            m_triggerAction?.action.Disable();
        }

        protected virtual void SetupInteractable()
        {
            if (m_grabInteractable != null)
            {
                m_grabInteractable.selectEntered.AddListener(OnGrab);
                m_grabInteractable.selectExited.AddListener(OnRelease);
                m_grabInteractable.hoverEntered.AddListener(OnHoverEnter);
                m_grabInteractable.hoverExited.AddListener(OnHoverExit);

                m_grabInteractable.throwOnDetach = m_isThrowable;
                m_grabInteractable.throwSmoothingDuration = 0.1f;
                m_grabInteractable.throwVelocityScale = m_throwForce;
                m_grabInteractable.throwAngularVelocityScale = m_throwAngle;
            }
        }

        protected virtual void OnGrab(SelectEnterEventArgs args)
        {
            m_isGrabbed = true;
            m_currentController = args.interactorObject.transform.GetComponent<XRController>();
            PlayGrabEffect();
            PlayGrabSound();
        }

        protected virtual void OnRelease(SelectExitEventArgs args)
        {
            m_isGrabbed = false;
            m_currentController = null;
            PlayReleaseSound();
        }

        protected virtual void OnHoverEnter(HoverEnterEventArgs args)
        {
            m_isHovered = true;
            PlayHoverEffect();
            PlayHoverSound();
        }

        protected virtual void OnHoverExit(HoverExitEventArgs args)
        {
            m_isHovered = false;
            StopHoverEffect();
        }

        protected virtual void PlayHoverEffect()
        {
            if (m_hoverEffect != null)
            {
                m_hoverEffect.SetActive(true);
            }
        }

        protected virtual void StopHoverEffect()
        {
            if (m_hoverEffect != null)
            {
                m_hoverEffect.SetActive(false);
            }
        }

        protected virtual void PlayGrabEffect()
        {
            if (m_grabEffect != null)
            {
                m_grabEffect.SetActive(true);
            }
        }

        protected virtual void StopGrabEffect()
        {
            if (m_grabEffect != null)
            {
                m_grabEffect.SetActive(false);
            }
        }

        protected virtual void PlayHoverSound()
        {
            if (m_audioSource != null && m_hoverSound != null)
            {
                m_audioSource.PlayOneShot(m_hoverSound);
            }
        }

        protected virtual void PlayGrabSound()
        {
            if (m_audioSource != null && m_grabSound != null)
            {
                m_audioSource.PlayOneShot(m_grabSound);
            }
        }

        protected virtual void PlayReleaseSound()
        {
            if (m_audioSource != null && m_releaseSound != null)
            {
                m_audioSource.PlayOneShot(m_releaseSound);
            }
        }

        // 获取交互状态
        public bool IsGrabbed()
        {
            return m_isGrabbed;
        }

        public bool IsHovered()
        {
            return m_isHovered;
        }

        // 获取当前控制器
        public XRController GetCurrentController()
        {
            return m_currentController;
        }

        // 设置可抓取状态
        public void SetGrabbable(bool grabbable)
        {
            m_isGrabbable = grabbable;
            if (m_grabInteractable != null)
            {
                m_grabInteractable.enabled = grabbable;
            }
        }

        // 设置可投掷状态
        public void SetThrowable(bool throwable)
        {
            m_isThrowable = throwable;
            if (m_grabInteractable != null)
            {
                m_grabInteractable.throwOnDetach = throwable;
            }
        }

        // 设置投掷力度
        public void SetThrowForce(float force)
        {
            m_throwForce = force;
            if (m_grabInteractable != null)
            {
                m_grabInteractable.throwVelocityScale = force;
            }
        }

        // 设置投掷角度
        public void SetThrowAngle(float angle)
        {
            m_throwAngle = angle;
            if (m_grabInteractable != null)
            {
                m_grabInteractable.throwAngularVelocityScale = angle;
            }
        }
    }
}