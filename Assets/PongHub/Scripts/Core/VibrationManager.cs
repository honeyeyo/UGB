using UnityEngine;
using System.Threading.Tasks;

namespace PongHub.Core
{
    public class VibrationManager : MonoBehaviour
    {
        private static VibrationManager s_instance;
        public static VibrationManager Instance => s_instance;

        private float m_vibrationIntensity = 1f;

        private void Awake()
        {
            if (s_instance == null)
            {
                s_instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async Task InitializeAsync()
        {
            await Task.Yield();
        }

        public void Cleanup()
        {
            // 清理资源
        }

        public void PlayVibration(float intensity)
        {
            // 实现振动反馈
            float finalIntensity = intensity * m_vibrationIntensity;
            // TODO: 实现具体的振动反馈逻辑
        }

        public void SetVibrationIntensity(float intensity)
        {
            m_vibrationIntensity = Mathf.Clamp01(intensity);
        }
    }
}