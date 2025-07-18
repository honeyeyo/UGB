using UnityEngine;
using UnityEngine.UI;
using Meta.Utilities.Input;

public class PaddleConfigurationManager : MonoBehaviour
{
    [Header("配置UI")]
    [SerializeField]
    [Tooltip("Config Canvas / 配置画布 - Canvas for paddle configuration UI")]
    private Canvas configCanvas;

    [SerializeField]
    [Tooltip("Position X Slider / X位置滑块 - Slider for X position adjustment")]
    private Slider positionXSlider;

    [SerializeField]
    [Tooltip("Position Y Slider / Y位置滑块 - Slider for Y position adjustment")]
    private Slider positionYSlider;

    [SerializeField]
    [Tooltip("Position Z Slider / Z位置滑块 - Slider for Z position adjustment")]
    private Slider positionZSlider;

    [SerializeField]
    [Tooltip("Rotation X Slider / X旋转滑块 - Slider for X rotation adjustment")]
    private Slider rotationXSlider;

    [SerializeField]
    [Tooltip("Rotation Y Slider / Y旋转滑块 - Slider for Y rotation adjustment")]
    private Slider rotationYSlider;

    [SerializeField]
    [Tooltip("Rotation Z Slider / Z旋转滑块 - Slider for Z rotation adjustment")]
    private Slider rotationZSlider;

    [SerializeField]
    [Tooltip("Save Button / 保存按钮 - Button to save configuration")]
    private Button saveButton;

    [SerializeField]
    [Tooltip("Reset Button / 重置按钮 - Button to reset configuration")]
    private Button resetButton;

    [SerializeField]
    [Tooltip("Preview Button / 预览按钮 - Button to toggle preview")]
    private Button previewButton;

    [Header("预览设置")]
    [SerializeField]
    [Tooltip("Preview Paddle Prefab / 预览球拍预制件 - Prefab for paddle preview")]
    private GameObject previewPaddlePrefab;

    [SerializeField]
    [Tooltip("Preview Material / 预览材质 - Material for preview paddle")]
    private Material previewMaterial;

    [Header("默认设置")]
    [SerializeField]
    [Tooltip("Default Position / 默认位置 - Default position offset")]
    private Vector3 defaultPosition = Vector3.zero;

    [SerializeField]
    [Tooltip("Default Rotation / 默认旋转 - Default rotation offset")]
    private Vector3 defaultRotation = Vector3.zero;

    // 私有变量
    private GameObject previewPaddle;
    private PaddleConfiguration currentConfig;
    private XRInputManager xrInputManager;
    private bool isConfiguring = false;
    private bool isLeftHandConfig = false;

    // 配置数据结构
    [System.Serializable]
    public class PaddleConfiguration
    {
        public Vector3 leftHandPosition;
        public Vector3 leftHandRotation;
        public Vector3 rightHandPosition;
        public Vector3 rightHandRotation;

        public PaddleConfiguration()
        {
            leftHandPosition = Vector3.zero;
            leftHandRotation = Vector3.zero;
            rightHandPosition = Vector3.zero;
            rightHandRotation = Vector3.zero;
        }
    }

    private void Awake()
    {
        xrInputManager = FindObjectOfType<XRInputManager>();
        if (xrInputManager == null)
        {
            Debug.LogError("PaddleConfigurationManager: 未找到XRInputManager组件！");
        }

        LoadConfiguration();
        SetupUI();
    }

    private void Start()
    {
        configCanvas.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isConfiguring)
        {
            UpdatePreview();
            HandleConfigurationInput();
        }
    }

    /// <summary>
    /// 设置UI事件
    /// </summary>
    private void SetupUI()
    {
        if (positionXSlider) positionXSlider.onValueChanged.AddListener(OnPositionChanged);
        if (positionYSlider) positionYSlider.onValueChanged.AddListener(OnPositionChanged);
        if (positionZSlider) positionZSlider.onValueChanged.AddListener(OnPositionChanged);
        if (rotationXSlider) rotationXSlider.onValueChanged.AddListener(OnRotationChanged);
        if (rotationYSlider) rotationYSlider.onValueChanged.AddListener(OnRotationChanged);
        if (rotationZSlider) rotationZSlider.onValueChanged.AddListener(OnRotationChanged);

        if (saveButton) saveButton.onClick.AddListener(SaveConfiguration);
        if (resetButton) resetButton.onClick.AddListener(ResetToDefault);
        if (previewButton) previewButton.onClick.AddListener(TogglePreview);
    }

    /// <summary>
    /// 开始配置模式
    /// </summary>
    public void StartConfiguration(bool forLeftHand)
    {
        isConfiguring = true;
        isLeftHandConfig = forLeftHand;
        configCanvas.gameObject.SetActive(true);

        // 设置UI值
        Vector3 currentPos = forLeftHand ? currentConfig.leftHandPosition : currentConfig.rightHandPosition;
        Vector3 currentRot = forLeftHand ? currentConfig.leftHandRotation : currentConfig.rightHandRotation;

        UpdateUIValues(currentPos, currentRot);
        CreatePreview();

        Debug.Log($"开始配置{(forLeftHand ? "左手" : "右手")}球拍位置");
    }

    /// <summary>
    /// 结束配置模式
    /// </summary>
    public void EndConfiguration()
    {
        isConfiguring = false;
        configCanvas.gameObject.SetActive(false);
        DestroyPreview();

        Debug.Log("配置模式结束");
    }

    /// <summary>
    /// 更新UI数值
    /// </summary>
    private void UpdateUIValues(Vector3 position, Vector3 rotation)
    {
        if (positionXSlider) positionXSlider.value = position.x;
        if (positionYSlider) positionYSlider.value = position.y;
        if (positionZSlider) positionZSlider.value = position.z;
        if (rotationXSlider) rotationXSlider.value = rotation.x;
        if (rotationYSlider) rotationYSlider.value = rotation.y;
        if (rotationZSlider) rotationZSlider.value = rotation.z;
    }

    /// <summary>
    /// 创建预览球拍
    /// </summary>
    private void CreatePreview()
    {
        if (previewPaddle == null && previewPaddlePrefab != null)
        {
            previewPaddle = Instantiate(previewPaddlePrefab);

            // 设置预览材质
            if (previewMaterial != null)
            {
                Renderer renderer = previewPaddle.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = previewMaterial;
                }
            }

            // 使预览球拍半透明
            SetPreviewTransparency(0.7f);
        }
    }

    /// <summary>
    /// 销毁预览球拍
    /// </summary>
    private void DestroyPreview()
    {
        if (previewPaddle != null)
        {
            Destroy(previewPaddle);
            previewPaddle = null;
        }
    }

    /// <summary>
    /// 更新预览球拍位置
    /// </summary>
    private void UpdatePreview()
    {
        if (previewPaddle == null || xrInputManager == null) return;

        Transform handAnchor = xrInputManager.GetAnchor(isLeftHandConfig);
        if (handAnchor == null) return;

        Vector3 position = new Vector3(
            positionXSlider ? positionXSlider.value : 0,
            positionYSlider ? positionYSlider.value : 0,
            positionZSlider ? positionZSlider.value : 0
        );

        Vector3 rotation = new Vector3(
            rotationXSlider ? rotationXSlider.value : 0,
            rotationYSlider ? rotationYSlider.value : 0,
            rotationZSlider ? rotationZSlider.value : 0
        );

        previewPaddle.transform.position = handAnchor.position + handAnchor.TransformDirection(position);
        previewPaddle.transform.rotation = handAnchor.rotation * Quaternion.Euler(rotation);
    }

    /// <summary>
    /// 处理配置模式下的输入
    /// </summary>
    private void HandleConfigurationInput()
    {
        if (xrInputManager == null) return;

        var leftActions = xrInputManager.GetActions(true);
        var rightActions = xrInputManager.GetActions(false);

        // B键退出配置模式（使用按下检测避免重复触发）
        bool leftB = leftActions.ButtonTwo.action.WasPressedThisFrame();
        bool rightB = rightActions.ButtonTwo.action.WasPressedThisFrame();

        if (leftB || rightB)
        {
            EndConfiguration();
        }
    }

    /// <summary>
    /// 位置改变事件
    /// </summary>
    private void OnPositionChanged(float value)
    {
        if (!isConfiguring) return;

        Vector3 newPosition = new Vector3(
            positionXSlider ? positionXSlider.value : 0,
            positionYSlider ? positionYSlider.value : 0,
            positionZSlider ? positionZSlider.value : 0
        );

        if (isLeftHandConfig)
            currentConfig.leftHandPosition = newPosition;
        else
            currentConfig.rightHandPosition = newPosition;
    }

    /// <summary>
    /// 旋转改变事件
    /// </summary>
    private void OnRotationChanged(float value)
    {
        if (!isConfiguring) return;

        Vector3 newRotation = new Vector3(
            rotationXSlider ? rotationXSlider.value : 0,
            rotationYSlider ? rotationYSlider.value : 0,
            rotationZSlider ? rotationZSlider.value : 0
        );

        if (isLeftHandConfig)
            currentConfig.leftHandRotation = newRotation;
        else
            currentConfig.rightHandRotation = newRotation;
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    private void SaveConfiguration()
    {
        string configJson = JsonUtility.ToJson(currentConfig, true);
        PlayerPrefs.SetString("PaddleConfiguration", configJson);
        PlayerPrefs.Save();

        Debug.Log("球拍配置已保存");
        EndConfiguration();
    }

    /// <summary>
    /// 重置为默认值
    /// </summary>
    private void ResetToDefault()
    {
        if (isLeftHandConfig)
        {
            currentConfig.leftHandPosition = defaultPosition;
            currentConfig.leftHandRotation = defaultRotation;
            UpdateUIValues(defaultPosition, defaultRotation);
        }
        else
        {
            currentConfig.rightHandPosition = defaultPosition;
            currentConfig.rightHandRotation = defaultRotation;
            UpdateUIValues(defaultPosition, defaultRotation);
        }

        Debug.Log($"{(isLeftHandConfig ? "左手" : "右手")}球拍配置已重置为默认值");
    }

    /// <summary>
    /// 切换预览显示
    /// </summary>
    private void TogglePreview()
    {
        if (previewPaddle != null)
        {
            previewPaddle.SetActive(!previewPaddle.activeSelf);
        }
    }

    /// <summary>
    /// 设置预览透明度
    /// </summary>
    private void SetPreviewTransparency(float alpha)
    {
        if (previewPaddle == null) return;

        Renderer renderer = previewPaddle.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            Color color = renderer.material.color;
            color.a = alpha;
            renderer.material.color = color;
        }
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    private void LoadConfiguration()
    {
        string configJson = PlayerPrefs.GetString("PaddleConfiguration", "");

        if (!string.IsNullOrEmpty(configJson))
        {
            currentConfig = JsonUtility.FromJson<PaddleConfiguration>(configJson);
        }
        else
        {
            currentConfig = new PaddleConfiguration();
        }
    }

    /// <summary>
    /// 获取当前配置
    /// </summary>
    public PaddleConfiguration GetCurrentConfiguration()
    {
        return currentConfig;
    }

    /// <summary>
    /// 获取指定手的球拍配置
    /// </summary>
    public void GetPaddleTransform(bool isLeftHand, out Vector3 position, out Vector3 rotation)
    {
        if (isLeftHand)
        {
            position = currentConfig.leftHandPosition;
            rotation = currentConfig.leftHandRotation;
        }
        else
        {
            position = currentConfig.rightHandPosition;
            rotation = currentConfig.rightHandRotation;
        }
    }
}