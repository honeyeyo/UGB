// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PongHub.Arena.Balls;
using PongHub.Arena.Gameplay;
using PongHub.Arena.Player;
using PongHub.Arena.Services;
using PongHub.Input;

namespace PongHub.UI
{
    /// <summary>
    /// 乒乓球物理调试UI - VR专用版本
    /// 提供实时物理参数调整和状态显示
    /// 专为VR环境设计，支持手势控制和语音交互
    /// </summary>
    public class PongPhysicsDebugUI : MonoBehaviour
    {
        [Header("UI面板")]
        [SerializeField] private Canvas debugCanvas;
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private Transform panelTransform;
        [SerializeField] private float uiDistance = 1.5f;           // UI距离玩家的距离
        [SerializeField] private float uiHeight = 0.2f;             // UI高度偏移

        [Header("VR交互设置")]
        [SerializeField] private bool enableHandTracking = true;    // 启用手部追踪
        [SerializeField] private float handProximityThreshold = 0.3f; // 手部接近阈值
        [SerializeField] private LayerMask uiLayerMask = -1;        // UI交互层级

        [Header("球物理调试")]
        [SerializeField] private Slider ballMassSlider;
        [SerializeField] private TextMeshProUGUI ballMassText;
        [SerializeField] private Slider ballDragSlider;
        [SerializeField] private TextMeshProUGUI ballDragText;
        [SerializeField] private Slider ballBounciness;
        [SerializeField] private TextMeshProUGUI ballBouncinessText;

        [Header("旋转调试")]
        [SerializeField] private Slider magnusForceSlider;
        [SerializeField] private TextMeshProUGUI magnusForceText;
        [SerializeField] private Slider spinDecaySlider;
        [SerializeField] private TextMeshProUGUI spinDecayText;
        [SerializeField] private Toggle spinVisualizationToggle;

        [Header("网络调试")]
        [SerializeField] private Slider syncRateSlider;
        [SerializeField] private TextMeshProUGUI syncRateText;
        [SerializeField] private TextMeshProUGUI networkLatencyText;
        [SerializeField] private TextMeshProUGUI packetLossText;
        [SerializeField] private Toggle predictiveMotionToggle;

        [Header("游戏状态")]
        [SerializeField] private TextMeshProUGUI gamePhaseText;
        [SerializeField] private TextMeshProUGUI currentServerText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Button resetGameButton;
        [SerializeField] private Button spawnBallButton;

        [Header("实时状态")]
        [SerializeField] private TextMeshProUGUI ballPositionText;
        [SerializeField] private TextMeshProUGUI ballVelocityText;
        [SerializeField] private TextMeshProUGUI ballSpinText;
        [SerializeField] private TextMeshProUGUI ballAttachmentText;

        [Header("快速设置")]
        [SerializeField] private Button presetRealisticButton;
        [SerializeField] private Button presetArcadeButton;
        [SerializeField] private Button presetSlowMotionButton;

        [Header("控制")]
        [SerializeField] private Toggle uiVisibilityToggle;
        [SerializeField] private Button saveSettingsButton;
        [SerializeField] private Button loadSettingsButton;

        [Header("VR特定UI")]
        [SerializeField] private Button repositionUIButton;         // 重新定位UI按钮
        [SerializeField] private Button followPlayerToggle;         // 跟随玩家开关
        [SerializeField] private TextMeshProUGUI instructionText;   // 操作说明文本

        // 引用组件
        private PongBallNetworking currentBall;
        private PongBallSpin ballSpin;
        private PongBallStateSync ballStateSync;
        private ServePermissionManager serveManager;
        private PongInputManager inputManager;
        private PhysicsMaterialConfig materialConfig;

        // VR相关
        private Transform playerTransform;
        private Transform leftHand;
        private Transform rightHand;
        private bool followPlayer = true;
        private Vector3 originalLocalPosition;

        // 状态
        private bool isUIVisible = true;
        private float lastToggleTime = 0f;
        private const float TOGGLE_COOLDOWN = 0.5f;

        // 默认值
        private struct PhysicsPreset
        {
            public float mass;
            public float drag;
            public float bounciness;
            public float magnusForce;
            public float spinDecay;
            public float syncRate;
        }

        private readonly PhysicsPreset realisticPreset = new PhysicsPreset
        {
            mass = 0.0027f,     // 2.7g - 真实乒乓球重量
            drag = 0.47f,       // 真实空气阻力
            bounciness = 0.9f,  // 真实弹性
            magnusForce = 2.1f, // 真实马格努斯力
            spinDecay = 0.85f,  // 真实旋转衰减（15%/秒）
            syncRate = 60f      // 60Hz同步
        };

        private readonly PhysicsPreset arcadePreset = new PhysicsPreset
        {
            mass = 0.001f,      // 更轻，更容易控制
            drag = 0.1f,        // 减少空气阻力
            bounciness = 1.2f,  // 增强弹性
            magnusForce = 3.5f, // 增强旋转效果
            spinDecay = 0.75f,  // 更快衰减
            syncRate = 30f      // 30Hz同步
        };

        private readonly PhysicsPreset slowMotionPreset = new PhysicsPreset
        {
            mass = 0.01f,       // 更重
            drag = 2.0f,        // 高空气阻力
            bounciness = 0.5f,  // 低弹性
            magnusForce = 0.5f, // 低旋转效果
            spinDecay = 0.95f,  // 慢衰减
            syncRate = 20f      // 20Hz同步
        };

        #region Unity Lifecycle
        private void Awake()
        {
            if (debugCanvas == null)
                debugCanvas = GetComponentInParent<Canvas>();

            InitializeUI();
        }

        private void Start()
        {
            FindGameComponents();
            SetupEventListeners();
            SetupVRComponents();
            LoadSettings();
        }

        private void Update()
        {
            UpdateRealTimeStatus();
            HandleVRInteraction();
            UpdateUIPosition();
        }

        private void OnDestroy()
        {
            SaveSettings();
            CleanupEventListeners();
        }
        #endregion

        #region Initialization
        private void InitializeUI()
        {
            // 设置Canvas为World Space，适配VR环境
            if (debugCanvas != null)
            {
                debugCanvas.renderMode = RenderMode.WorldSpace;
                debugCanvas.worldCamera = Camera.main;
                debugCanvas.sortingOrder = 100;

                // 设置合适的缩放
                debugCanvas.transform.localScale = Vector3.one * 0.001f; // 1mm per unit
            }

            // 初始化滑块范围
            if (ballMassSlider != null)
            {
                ballMassSlider.minValue = 0.001f;
                ballMassSlider.maxValue = 0.01f;
                ballMassSlider.value = realisticPreset.mass;
            }

            if (ballDragSlider != null)
            {
                ballDragSlider.minValue = 0f;
                ballDragSlider.maxValue = 2f;
                ballDragSlider.value = realisticPreset.drag;
            }

            if (ballBounciness != null)
            {
                ballBounciness.minValue = 0f;
                ballBounciness.maxValue = 2f;
                ballBounciness.value = realisticPreset.bounciness;
            }

            if (magnusForceSlider != null)
            {
                magnusForceSlider.minValue = 0f;
                magnusForceSlider.maxValue = 5f;
                magnusForceSlider.value = realisticPreset.magnusForce;
            }

            if (spinDecaySlider != null)
            {
                spinDecaySlider.minValue = 0.7f;
                spinDecaySlider.maxValue = 0.999f;
                spinDecaySlider.value = realisticPreset.spinDecay;
            }

            if (syncRateSlider != null)
            {
                syncRateSlider.minValue = 10f;
                syncRateSlider.maxValue = 120f;
                syncRateSlider.value = realisticPreset.syncRate;
            }

            // 设置操作说明
            if (instructionText != null)
            {
                instructionText.text = "VR操作说明:\n" +
                                     "双手握拳 - 切换UI显示\n" +
                                     "指向UI - 射线交互\n" +
                                     "长按Meta键 - 重置位置\n" +
                                     "A+B组合键 - 快速设置";
            }
        }

        private void FindGameComponents()
        {
            // 查找游戏组件
            serveManager = ServePermissionManager.Instance;
            inputManager = PongInputManager.Instance;

            // 查找物理材质配置
            materialConfig = Resources.Load<PhysicsMaterialConfig>("PhysicsMaterialConfig");

            // 查找当前球
            RefreshBallReference();
        }

        private void SetupVRComponents()
        {
            // 查找玩家和手部变换
            var playerEntity = LocalPlayerEntities.Instance;
            if (playerEntity != null)
            {
                playerTransform = playerEntity.transform;
                leftHand = playerEntity.LeftGloveHand?.transform;
                rightHand = playerEntity.RightGloveHand?.transform;
            }

            // 记录原始位置
            originalLocalPosition = panelTransform.localPosition;

            // 初始UI位置
            RepositionUI();
        }

        private void RefreshBallReference()
        {
            var spawner = PongBallSpawner.Instance;
            if (spawner != null)
            {
                var activeBalls = spawner.GetActiveBalls();
                if (activeBalls.Count > 0)
                {
                    currentBall = activeBalls[0];
                    ballSpin = currentBall.BallSpin;
                    ballStateSync = currentBall.GetComponent<PongBallStateSync>();
                }
            }
        }

        private void SetupEventListeners()
        {
            // 滑块事件
            ballMassSlider?.onValueChanged.AddListener(OnBallMassChanged);
            ballDragSlider?.onValueChanged.AddListener(OnBallDragChanged);
            ballBounciness?.onValueChanged.AddListener(OnBallBouncinessChanged);
            magnusForceSlider?.onValueChanged.AddListener(OnMagnusForceChanged);
            spinDecaySlider?.onValueChanged.AddListener(OnSpinDecayChanged);
            syncRateSlider?.onValueChanged.AddListener(OnSyncRateChanged);

            // 开关事件
            spinVisualizationToggle?.onValueChanged.AddListener(OnSpinVisualizationChanged);
            predictiveMotionToggle?.onValueChanged.AddListener(OnPredictiveMotionChanged);
            uiVisibilityToggle?.onValueChanged.AddListener(OnUIVisibilityChanged);

            // 按钮事件
            resetGameButton?.onClick.AddListener(OnResetGame);
            spawnBallButton?.onClick.AddListener(OnSpawnBall);
            presetRealisticButton?.onClick.AddListener(() => ApplyPreset(realisticPreset));
            presetArcadeButton?.onClick.AddListener(() => ApplyPreset(arcadePreset));
            presetSlowMotionButton?.onClick.AddListener(() => ApplyPreset(slowMotionPreset));
            saveSettingsButton?.onClick.AddListener(SaveSettings);
            loadSettingsButton?.onClick.AddListener(LoadSettings);

            // VR特定按钮
            repositionUIButton?.onClick.AddListener(RepositionUI);
            followPlayerToggle?.onClick.AddListener(ToggleFollowPlayer);

            // 订阅输入事件
            if (inputManager != null)
            {
                PongInputManager.OnTeleportPerformed += OnTeleportPerformed;
            }
        }

        private void CleanupEventListeners()
        {
            // 清理输入事件订阅
            if (inputManager != null)
            {
                PongInputManager.OnTeleportPerformed -= OnTeleportPerformed;
            }
        }
        #endregion

        #region Parameter Adjustment
        private void OnBallMassChanged(float value)
        {
            if (currentBall != null)
            {
                var rigidbody = currentBall.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.mass = value;
                }
            }

            if (ballMassText != null)
                ballMassText.text = $"质量: {value:F4}kg";
        }

        private void OnBallDragChanged(float value)
        {
            if (currentBall != null)
            {
                var rigidbody = currentBall.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.drag = value;
                }
            }

            if (ballDragText != null)
                ballDragText.text = $"阻力: {value:F2}";
        }

        private void OnBallBouncinessChanged(float value)
        {
            if (currentBall != null)
            {
                var collider = currentBall.GetComponent<SphereCollider>();
                if (collider != null && collider.material != null)
                {
                    collider.material.bounciness = value;
                }
            }

            if (ballBouncinessText != null)
                ballBouncinessText.text = $"弹性: {value:F2}";
        }

        private void OnMagnusForceChanged(float value)
        {
            if (ballSpin != null)
            {
                ballSpin.SetMagnusForceMultiplier(value);
            }

            if (magnusForceText != null)
                magnusForceText.text = $"马格努斯力: {value:F1}";
        }

        private void OnSpinDecayChanged(float value)
        {
            if (ballSpin != null)
            {
                ballSpin.SetSpinDecayRate(value);
            }

            if (spinDecayText != null)
                spinDecayText.text = $"旋转衰减: {value:F3}";
        }

        private void OnSyncRateChanged(float value)
        {
            if (ballStateSync != null)
            {
                ballStateSync.SetSyncRate(value);
            }

            if (syncRateText != null)
                syncRateText.text = $"同步频率: {value:F0}Hz";
        }

        private void OnSpinVisualizationChanged(bool value)
        {
            if (ballSpin != null)
            {
                ballSpin.SetVisualizationEnabled(value);
            }
        }

        private void OnPredictiveMotionChanged(bool value)
        {
            if (ballStateSync != null)
            {
                ballStateSync.SetPredictiveMotionEnabled(value);
            }
        }

        private void OnUIVisibilityChanged(bool value)
        {
            isUIVisible = value;
            if (debugPanel != null)
                debugPanel.SetActive(value);
        }
        #endregion

        #region Preset Application
        private void ApplyPreset(PhysicsPreset preset)
        {
            // 应用滑块值
            ballMassSlider.value = preset.mass;
            ballDragSlider.value = preset.drag;
            ballBounciness.value = preset.bounciness;
            magnusForceSlider.value = preset.magnusForce;
            spinDecaySlider.value = preset.spinDecay;
            syncRateSlider.value = preset.syncRate;

            Debug.Log($"物理预设已应用: {preset.mass}kg, 阻力{preset.drag}, 弹性{preset.bounciness}");
        }
        #endregion

        #region Real-time Status Update
        private void UpdateRealTimeStatus()
        {
            if (currentBall == null)
            {
                RefreshBallReference();
                return;
            }

            // 更新球状态
            if (ballPositionText != null)
                ballPositionText.text = $"位置: {currentBall.transform.position:F2}";

            var rigidbody = currentBall.GetComponent<Rigidbody>();
            if (rigidbody != null && ballVelocityText != null)
                ballVelocityText.text = $"速度: {rigidbody.velocity.magnitude:F2} m/s";

            if (ballSpin != null && ballSpinText != null)
                ballSpinText.text = ballSpin.GetSpinInfo();

            if (ballAttachmentText != null)
            {
                var attachment = currentBall.GetComponent<PongBallAttachment>();
                if (attachment != null)
                {
                    ballAttachmentText.text = attachment.IsAttached ?
                        $"附着: {attachment.AttachedHand?.name}" : "自由状态";
                }
            }

            // 更新网络状态
            if (ballStateSync != null && networkLatencyText != null)
            {
                var stats = ballStateSync.GetNetworkStats();
                networkLatencyText.text = stats;
            }

            // 更新游戏状态
            if (serveManager != null)
            {
                if (gamePhaseText != null)
                    gamePhaseText.text = $"游戏阶段: {serveManager.CurrentGamePhase.Value}";

                if (currentServerText != null)
                    currentServerText.text = $"发球方: {serveManager.CurrentServerPlayerId.Value}";

                if (scoreText != null)
                    scoreText.text = $"比分: {serveManager.Player1Score.Value} - {serveManager.Player2Score.Value}";
            }
        }
        #endregion

        #region VR Interaction
        private void HandleVRInteraction()
        {
            if (inputManager == null) return;

            // 检查双手握拳手势来切换UI显示
            if (CheckDoubleGripGesture())
            {
                if (Time.time - lastToggleTime > TOGGLE_COOLDOWN)
                {
                    OnUIVisibilityChanged(!isUIVisible);
                    lastToggleTime = Time.time;
                }
            }

            // 检查A+B组合键快速设置
            var inputState = inputManager.GetCurrentInputState();
            if (inputState.leftAB || inputState.rightAB)
            {
                ShowQuickSettingsMenu();
            }
        }

        private bool CheckDoubleGripGesture()
        {
            if (inputManager == null) return false;

            var inputState = inputManager.GetCurrentInputState();
            return inputState.leftGrip > 0.8f && inputState.rightGrip > 0.8f;
        }

        private void ShowQuickSettingsMenu()
        {
            // 显示快速设置菜单的逻辑
            Debug.Log("显示快速设置菜单");
        }

        private void UpdateUIPosition()
        {
            if (!followPlayer || playerTransform == null || panelTransform == null) return;

            // 让UI始终面向玩家，保持合适距离
            Vector3 targetPosition = playerTransform.position +
                                   playerTransform.forward * uiDistance +
                                   Vector3.up * uiHeight;

            panelTransform.position = Vector3.Lerp(panelTransform.position, targetPosition, Time.deltaTime * 2f);
            panelTransform.LookAt(playerTransform.position);
            panelTransform.Rotate(0, 180, 0); // UI面向玩家
        }

        private void RepositionUI()
        {
            if (playerTransform == null || panelTransform == null) return;

            Vector3 newPosition = playerTransform.position +
                                playerTransform.forward * uiDistance +
                                Vector3.up * uiHeight;

            panelTransform.position = newPosition;
            panelTransform.LookAt(playerTransform.position);
            panelTransform.Rotate(0, 180, 0);

            Debug.Log("UI已重新定位");
        }

        private void ToggleFollowPlayer()
        {
            followPlayer = !followPlayer;
            Debug.Log($"跟随玩家: {(followPlayer ? "开启" : "关闭")}");
        }

        private void OnTeleportPerformed()
        {
            // 瞬移后重新定位UI
            if (followPlayer)
            {
                RepositionUI();
            }
        }
        #endregion

        #region Game Control
        private void OnResetGame()
        {
            if (serveManager != null)
            {
                serveManager.EndMatchServerRpc();
                serveManager.StartPracticeModeServerRpc();
            }
        }

        private void OnSpawnBall()
        {
            var spawner = PongBallSpawner.Instance;
            if (spawner != null)
            {
                spawner.SpawnBallForPlayerServerRpc();
            }
        }
        #endregion

        #region Settings Persistence
        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("PongDebug_BallMass", ballMassSlider.value);
            PlayerPrefs.SetFloat("PongDebug_BallDrag", ballDragSlider.value);
            PlayerPrefs.SetFloat("PongDebug_BallBounciness", ballBounciness.value);
            PlayerPrefs.SetFloat("PongDebug_MagnusForce", magnusForceSlider.value);
            PlayerPrefs.SetFloat("PongDebug_SpinDecay", spinDecaySlider.value);
            PlayerPrefs.SetFloat("PongDebug_SyncRate", syncRateSlider.value);
            PlayerPrefs.SetInt("PongDebug_SpinVisualization", spinVisualizationToggle.isOn ? 1 : 0);
            PlayerPrefs.SetInt("PongDebug_PredictiveMotion", predictiveMotionToggle.isOn ? 1 : 0);
            PlayerPrefs.SetInt("PongDebug_UIVisible", isUIVisible ? 1 : 0);
            PlayerPrefs.SetInt("PongDebug_FollowPlayer", followPlayer ? 1 : 0);

            PlayerPrefs.Save();
            Debug.Log("VR调试设置已保存");
        }

        private void LoadSettings()
        {
            if (PlayerPrefs.HasKey("PongDebug_BallMass"))
            {
                ballMassSlider.value = PlayerPrefs.GetFloat("PongDebug_BallMass", realisticPreset.mass);
                ballDragSlider.value = PlayerPrefs.GetFloat("PongDebug_BallDrag", realisticPreset.drag);
                ballBounciness.value = PlayerPrefs.GetFloat("PongDebug_BallBounciness", realisticPreset.bounciness);
                magnusForceSlider.value = PlayerPrefs.GetFloat("PongDebug_MagnusForce", realisticPreset.magnusForce);
                spinDecaySlider.value = PlayerPrefs.GetFloat("PongDebug_SpinDecay", realisticPreset.spinDecay);
                syncRateSlider.value = PlayerPrefs.GetFloat("PongDebug_SyncRate", realisticPreset.syncRate);
                spinVisualizationToggle.isOn = PlayerPrefs.GetInt("PongDebug_SpinVisualization", 1) == 1;
                predictiveMotionToggle.isOn = PlayerPrefs.GetInt("PongDebug_PredictiveMotion", 0) == 1;
                isUIVisible = PlayerPrefs.GetInt("PongDebug_UIVisible", 1) == 1;
                followPlayer = PlayerPrefs.GetInt("PongDebug_FollowPlayer", 1) == 1;

                OnUIVisibilityChanged(isUIVisible);
                Debug.Log("VR调试设置已加载");
            }
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// 切换UI显示状态
        /// </summary>
        public void ToggleUI()
        {
            OnUIVisibilityChanged(!isUIVisible);
        }

        /// <summary>
        /// 设置UI面板位置（用于VR环境）
        /// </summary>
        /// <param name="position">世界坐标位置</param>
        /// <param name="rotation">旋转</param>
        public void SetUIPosition(Vector3 position, Quaternion rotation)
        {
            if (panelTransform != null)
            {
                panelTransform.position = position;
                panelTransform.rotation = rotation;
            }
        }

        /// <summary>
        /// 获取当前物理设置信息
        /// </summary>
        /// <returns>设置信息字符串</returns>
        public string GetCurrentSettings()
        {
            return $"质量: {ballMassSlider.value:F4}kg\n" +
                   $"阻力: {ballDragSlider.value:F2}\n" +
                   $"弹性: {ballBounciness.value:F2}\n" +
                   $"马格努斯力: {magnusForceSlider.value:F1}\n" +
                   $"旋转衰减: {spinDecaySlider.value:F3}\n" +
                   $"同步频率: {syncRateSlider.value:F0}Hz";
        }

        /// <summary>
        /// 应用物理材质配置
        /// </summary>
        public void ApplyMaterialConfig(string materialName)
        {
            if (materialConfig != null)
            {
                var properties = materialConfig.GetMaterialProperties(materialName);
                ballBounciness.value = properties.bounciness;

                Debug.Log($"已应用材质配置: {materialName}");
            }
        }
        #endregion
    }
}