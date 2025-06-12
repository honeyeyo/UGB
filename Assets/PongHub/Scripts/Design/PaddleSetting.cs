using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PongHub.Design
{
    public class PaddleSettings : MonoBehaviour
    {
        [SerializeField] private PaddleData m_paddleData;

        [Header("UI References")]
        [SerializeField] private Slider m_bounceForceSlider;
        [SerializeField] private Slider m_spinMultiplierSlider;
        [SerializeField] private Slider m_surfaceHardnessSlider;
        [SerializeField] private Slider m_surfaceFrictionSlider;
        [SerializeField] private Slider m_surfaceBounceSlider;

        [SerializeField] private TextMeshProUGUI m_bounceForceText;
        [SerializeField] private TextMeshProUGUI m_spinMultiplierText;
        [SerializeField] private TextMeshProUGUI m_surfaceHardnessText;
        [SerializeField] private TextMeshProUGUI m_surfaceFrictionText;
        [SerializeField] private TextMeshProUGUI m_surfaceBounceText;

        private void Start()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            // 初始化滑块
            m_bounceForceSlider.value = m_paddleData.BounceForce;
            m_spinMultiplierSlider.value = m_paddleData.SpinMultiplier;
            m_surfaceHardnessSlider.value = m_paddleData.SurfaceHardness;
            m_surfaceFrictionSlider.value = m_paddleData.SurfaceFriction;
            m_surfaceBounceSlider.value = m_paddleData.SurfaceBounce;

            // 更新文本
            UpdateUIValues();

            // 添加监听器
            m_bounceForceSlider.onValueChanged.AddListener(OnBounceForceChanged);
            m_spinMultiplierSlider.onValueChanged.AddListener(OnSpinMultiplierChanged);
            m_surfaceHardnessSlider.onValueChanged.AddListener(OnSurfaceHardnessChanged);
            m_surfaceFrictionSlider.onValueChanged.AddListener(OnSurfaceFrictionChanged);
            m_surfaceBounceSlider.onValueChanged.AddListener(OnSurfaceBounceChanged);
        }

        private void UpdateUIValues()
        {
            m_bounceForceText.text = $"击球力度: {m_paddleData.BounceForce:F2}";
            m_spinMultiplierText.text = $"旋转系数: {m_paddleData.SpinMultiplier:F2}";
            m_surfaceHardnessText.text = $"拍面硬度: {m_paddleData.SurfaceHardness:F2}";
            m_surfaceFrictionText.text = $"摩擦系数: {m_paddleData.SurfaceFriction:F2}";
            m_surfaceBounceText.text = $"拍面弹性: {m_paddleData.SurfaceBounce:F2}";
        }

        private void OnBounceForceChanged(float value)
        {
            m_paddleData.BounceForce = value;
            UpdateUIValues();
        }

        private void OnSpinMultiplierChanged(float value)
        {
            m_paddleData.SpinMultiplier = value;
            UpdateUIValues();
        }

        private void OnSurfaceHardnessChanged(float value)
        {
            m_paddleData.SurfaceHardness = value;
            UpdateUIValues();
        }

        private void OnSurfaceFrictionChanged(float value)
        {
            m_paddleData.SurfaceFriction = value;
            UpdateUIValues();
        }

        private void OnSurfaceBounceChanged(float value)
        {
            m_paddleData.SurfaceBounce = value;
            UpdateUIValues();
        }

        // 保存设置
        public void SaveSettings()
        {
            // 这里可以添加保存到PlayerPrefs或配置文件的逻辑
            Debug.Log("Paddle settings saved");
        }

        // 重置设置
        public void ResetSettings()
        {
            m_paddleData.BounceForce = 1.0f;
            m_paddleData.SpinMultiplier = 1.0f;
            m_paddleData.SurfaceHardness = 0.5f;
            m_paddleData.SurfaceFriction = 0.5f;
            m_paddleData.SurfaceBounce = 0.5f;

            InitializeUI();
        }
    }
}
