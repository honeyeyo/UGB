using UnityEngine;
using PongHub.Core;
using PongHub.Gameplay.Serve;

namespace PongHub.Core
{
    /// <summary>
    /// 游戏演示控制器
    /// 整合现有管理器组件，提供演示和测试功能
    /// </summary>
    public class GameDemo : MonoBehaviour
    {
        #region 组件引用
        [Header("管理器引用")]
        [SerializeField]
        [Tooltip("Game Mode Controller / 游戏模式控制器 - Controller for managing game modes")]
        private GameModeController gameModeController;

        [SerializeField]
        [Tooltip("Serve Validator / 发球验证器 - Validator for serve rules and mechanics")]
        private ServeValidator serveValidator;

        [SerializeField]
        [Tooltip("Editor Input Simulator / 编辑器输入模拟器 - Input simulator for editor testing")]
        private PongHub.Input.EditorInputSimulator editorInputSimulator;

        [Header("演示设置")]
        [SerializeField]
        [Tooltip("Auto Start Demo / 自动开始演示 - Whether to automatically start the demo")]
        private bool autoStartDemo = true;

        [SerializeField]
        [Tooltip("Enable Keyboard Controls / 启用键盘控制 - Whether to enable keyboard input controls")]
        private bool enableKeyboardControls = true;

        [SerializeField]
        [Tooltip("Auto Score Interval / 自动得分间隔 - Time interval for automatic scoring")]
        private float autoScoreInterval = 5f;

        [SerializeField]
        [Tooltip("Show Debug Info / 显示调试信息 - Whether to show debug information on screen")]
        private bool showDebugInfo = true;

        [Header("调试显示")]
        [SerializeField]
        [Tooltip("Show Gizmos / 显示Gizmos - Whether to show debug gizmos in scene view")]
        private bool showGizmos = true;

        [SerializeField]
        [Tooltip("Demo Area / 演示区域 - Transform defining the demo area boundaries")]
        private Transform demoArea;
        #endregion

        #region 私有变量
        private bool isInitialized = false;
        private float lastAutoScoreTime = 0f;
        private int autoScorePlayer = 1;

        // 引用现有管理器
        private MatchManager matchManager;
        private ScoreManager scoreManager;
        #endregion

        #region Unity生命周期
        private void Start()
        {
            InitializeDemo();
        }

        private void Update()
        {
            if (!isInitialized) return;

            HandleKeyboardInput();
            HandleAutoDemo();
        }

        private void OnGUI()
        {
            if (showDebugInfo)
            {
                DrawDebugInfo();
            }
        }

        private void OnDrawGizmos()
        {
            if (showGizmos)
            {
                DrawDemoGizmos();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEditorInputEvents();
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化演示
        /// </summary>
        private void InitializeDemo()
        {
            Debug.Log("[GameDemo] 开始初始化演示场景");

            // 获取现有管理器
            FindExistingManagers();

            // 确保所有组件都存在
            EnsureComponents();

            // 设置初始配置
            SetupInitialConfiguration();

            // 如果启用自动演示，开始演示
            if (autoStartDemo)
            {
                StartAutoDemo();
            }

            isInitialized = true;
            Debug.Log("[GameDemo] 演示场景初始化完成");
        }

        /// <summary>
        /// 查找现有管理器
        /// </summary>
        private void FindExistingManagers()
        {
            // 获取现有管理器的引用
            matchManager = MatchManager.Instance;
            scoreManager = ScoreManager.Instance;

            if (matchManager == null)
            {
                Debug.LogWarning("[GameDemo] 未找到MatchManager实例");
            }

            if (scoreManager == null)
            {
                Debug.LogWarning("[GameDemo] 未找到ScoreManager实例");
            }
        }

        /// <summary>
        /// 确保所有必要组件存在
        /// </summary>
        private void EnsureComponents()
        {
            // 游戏模式控制器
            if (gameModeController == null)
            {
                gameModeController = FindObjectOfType<GameModeController>();
                if (gameModeController == null)
                {
                    gameModeController = gameObject.AddComponent<GameModeController>();
                    Debug.Log("[GameDemo] 创建了GameModeController组件");
                }
            }

            // 发球验证器
            if (serveValidator == null)
            {
                serveValidator = FindObjectOfType<ServeValidator>();
                if (serveValidator == null)
                {
                    serveValidator = gameObject.AddComponent<ServeValidator>();
                    Debug.Log("[GameDemo] 创建了ServeValidator组件");
                }
            }

            // Editor输入模拟器
            if (editorInputSimulator == null)
            {
                editorInputSimulator = FindObjectOfType<PongHub.Input.EditorInputSimulator>();
                if (editorInputSimulator == null)
                {
                    editorInputSimulator = gameObject.AddComponent<PongHub.Input.EditorInputSimulator>();
                    Debug.Log("[GameDemo] 创建了EditorInputSimulator组件");
                }
            }
        }

        /// <summary>
        /// 设置初始配置
        /// </summary>
        private void SetupInitialConfiguration()
        {
            // 设置发球规则
            serveValidator?.SetServeRules(0.16f, 30f, 30f); // 16cm最小高度，30度最大角度，30秒时间限制

            // 设置模式控制器为离线模式
            gameModeController?.ForceOfflineMode();

            // 设置比赛规则（如果MatchManager存在）
            matchManager?.SetRoundsToWin(3); // 三局两胜

            // 订阅Editor输入事件
            SubscribeToEditorInputEvents();

            Debug.Log("[GameDemo] 初始配置设置完成");
        }
        #endregion

        #region 演示控制
        /// <summary>
        /// 开始自动演示
        /// </summary>
        private void StartAutoDemo()
        {
            Debug.Log("[GameDemo] 开始自动演示");

            // 等待一秒后开始游戏
            Invoke(nameof(StartDemoGame), 1f);
        }

        /// <summary>
        /// 开始演示游戏
        /// </summary>
        private void StartDemoGame()
        {
            // 启动比赛（如果MatchManager存在）
            if (matchManager != null)
            {
                matchManager.StartRound();
                lastAutoScoreTime = Time.time;
            }
            else
            {
                Debug.LogWarning("[GameDemo] MatchManager不存在，无法启动比赛");
            }
        }

        /// <summary>
        /// 处理自动演示
        /// </summary>
        private void HandleAutoDemo()
        {
            if (!autoStartDemo) return;

            // 在游戏进行中自动得分
            if (matchManager != null &&
                matchManager.CurrentState == MatchManager.MatchState.Playing &&
                Time.time - lastAutoScoreTime >= autoScoreInterval)
            {
                // 交替让玩家得分
                if (scoreManager != null)
                {
                    scoreManager.AddScore(autoScorePlayer);
                    autoScorePlayer = autoScorePlayer == 1 ? 2 : 1;
                    lastAutoScoreTime = Time.time;
                }
            }
        }

        /// <summary>
        /// 处理键盘输入
        /// </summary>
        private void HandleKeyboardInput()
        {
            if (!enableKeyboardControls) return;

            // 游戏控制
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                // 暂停/继续
                if (gameModeController != null)
                {
                    gameModeController.TogglePause();
                }
            }

            // 手动得分
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1))
            {
                scoreManager?.AddScore(1);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2))
            {
                scoreManager?.AddScore(2);
            }

            // 重新开始
            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                matchManager?.ResetMatch();
                scoreManager?.ResetScores();
            }

            // 模式切换
            if (UnityEngine.Input.GetKeyDown(KeyCode.O))
            {
                gameModeController?.ForceOfflineMode();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.N))
            {
                gameModeController?.ForceOnlineMode();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.H))
            {
                gameModeController?.EnableHybridMode();
            }

            // 发球测试
            if (UnityEngine.Input.GetKeyDown(KeyCode.S))
            {
                TestServeValidation();
            }

            // 切换调试信息
            if (UnityEngine.Input.GetKeyDown(KeyCode.F1))
            {
                showDebugInfo = !showDebugInfo;
            }

            // 切换自动演示
            if (UnityEngine.Input.GetKeyDown(KeyCode.F2))
            {
                autoStartDemo = !autoStartDemo;
            }
        }
        #endregion

        #region 测试功能
        /// <summary>
        /// 测试发球验证
        /// </summary>
        private void TestServeValidation()
        {
            if (serveValidator == null) return;

            // 模拟发球数据
            Vector3 testPosition = transform.position + Vector3.up * 1.5f;
            Vector3 testVelocity = Vector3.up * 3f + Vector3.forward * 0.5f;

            var result = serveValidator.ValidateServe(testPosition, testVelocity);

            Debug.Log($"[GameDemo] 发球测试结果: {(result.IsValid ? "有效" : "无效")}");
            if (!result.IsValid)
            {
                Debug.Log($"[GameDemo] 失误原因: {result.FaultDescription}");
            }
        }

        /// <summary>
        /// 重置演示
        /// </summary>
        public void ResetDemo()
        {
            matchManager?.ResetMatch();
            scoreManager?.ResetScores();
            gameModeController?.ForceOfflineMode();
            autoScorePlayer = 1;
            lastAutoScoreTime = 0f;

            Debug.Log("[GameDemo] 演示已重置");
        }

        /// <summary>
        /// 切换演示模式
        /// </summary>
        public void ToggleDemoMode()
        {
            autoStartDemo = !autoStartDemo;
            Debug.Log($"[GameDemo] 自动演示: {(autoStartDemo ? "开启" : "关闭")}");
        }
        #endregion

        #region 调试显示
        /// <summary>
        /// 绘制调试信息
        /// </summary>
        private void DrawDebugInfo()
        {
            float yOffset = 10f;
            float lineHeight = 20f;

            // 标题
            GUI.Label(new Rect(10, yOffset, 400, lineHeight), "乒乓球游戏演示 - 调试信息", GetLabelStyle(16, Color.yellow));
            yOffset += lineHeight * 1.5f;

            // MatchManager信息
            if (matchManager != null)
            {
                GUI.Label(new Rect(10, yOffset, 400, lineHeight), $"比赛状态: {matchManager.CurrentState}", GetLabelStyle());
                yOffset += lineHeight;

                GUI.Label(new Rect(10, yOffset, 400, lineHeight), $"当前轮次: {matchManager.CurrentRound}", GetLabelStyle());
                yOffset += lineHeight;

                var roundScores = matchManager.GetRoundScores();
                GUI.Label(new Rect(10, yOffset, 400, lineHeight), $"轮次分数: {roundScores.Item1} - {roundScores.Item2}", GetLabelStyle());
                yOffset += lineHeight;
            }

            // ScoreManager信息
            if (scoreManager != null)
            {
                GUI.Label(new Rect(10, yOffset, 400, lineHeight), $"当前比分: {scoreManager.Player1Score} - {scoreManager.Player2Score}", GetLabelStyle());
                yOffset += lineHeight;

                GUI.Label(new Rect(10, yOffset, 400, lineHeight), $"领先玩家: {GetLeadingPlayerText()}", GetLabelStyle());
                yOffset += lineHeight;
            }

            yOffset += 10f;

            // GameModeController信息
            if (gameModeController != null)
            {
                GUI.Label(new Rect(10, yOffset, 400, lineHeight), $"游戏模式: {gameModeController.CurrentNetworkState}", GetLabelStyle());
                yOffset += lineHeight;

                GUI.Label(new Rect(10, yOffset, 400, lineHeight), $"连接状态: {gameModeController.CurrentConnectionState}", GetLabelStyle());
                yOffset += lineHeight;

                GUI.Label(new Rect(10, yOffset, 400, lineHeight), $"当前模式: {(gameModeController.IsOfflineMode ? "离线" : "在线")}", GetLabelStyle());
                yOffset += lineHeight;

                GUI.Label(new Rect(10, yOffset, 400, lineHeight), $"可暂停: {gameModeController.CanPause}", GetLabelStyle());
                yOffset += lineHeight;
            }

            yOffset += 10f;

            // ServeValidator信息
            if (serveValidator != null)
            {
                GUI.Label(new Rect(10, yOffset, 400, lineHeight), $"发球状态: {serveValidator.CurrentServeState}", GetLabelStyle());
                yOffset += lineHeight;

                GUI.Label(new Rect(10, yOffset, 400, lineHeight), $"最小抛球高度: {serveValidator.MinThrowHeight:F3}m", GetLabelStyle());
                yOffset += lineHeight;

                GUI.Label(new Rect(10, yOffset, 400, lineHeight), $"最大抛球角度: {serveValidator.MaxThrowAngle:F1}°", GetLabelStyle());
                yOffset += lineHeight;
            }

            yOffset += 20f;

            // 控制说明
            GUI.Label(new Rect(10, yOffset, 400, lineHeight), "控制说明:", GetLabelStyle(14, Color.cyan));
            yOffset += lineHeight;

            string[] controls = {
                "空格键: 暂停/继续",
                "1/2键: 玩家1/2得分",
                "R键: 重置比赛",
                "O键: 离线模式",
                "N键: 在线模式",
                "H键: 混合模式",
                "S键: 测试发球验证",
                "F1键: 切换调试信息",
                "F2键: 切换自动演示"
            };

            foreach (string control in controls)
            {
                GUI.Label(new Rect(20, yOffset, 400, lineHeight), control, GetLabelStyle(12));
                yOffset += lineHeight;
            }

            yOffset += 10f;

            yOffset += 10f;

            // Editor输入模拟器信息
            if (editorInputSimulator != null)
            {
                GUI.Label(new Rect(10, yOffset, 400, lineHeight), "Editor输入模拟器:", GetLabelStyle(14, Color.cyan));
                yOffset += lineHeight;

                var inputState = editorInputSimulator.GetCurrentInputState();
                GUI.Label(new Rect(10, yOffset, 400, lineHeight), $"头部位置: {inputState.HeadPosition:F2}", GetLabelStyle(12));
                yOffset += lineHeight;

                GUI.Label(new Rect(10, yOffset, 400, lineHeight), $"移动输入: {inputState.MoveInput:F2}", GetLabelStyle(12));
                yOffset += lineHeight;

                GUI.Label(new Rect(10, yOffset, 400, lineHeight), $"左拍: {(inputState.LeftTriggerPressed ? "按下" : "释放")}", GetLabelStyle(12));
                yOffset += lineHeight;

                GUI.Label(new Rect(10, yOffset, 400, lineHeight), $"右拍: {(inputState.RightTriggerPressed ? "按下" : "释放")}", GetLabelStyle(12));
                yOffset += lineHeight;
            }

            yOffset += 10f;

            // 演示状态
            GUI.Label(new Rect(10, yOffset, 400, lineHeight), $"自动演示: {(autoStartDemo ? "开启" : "关闭")}", GetLabelStyle(12, autoStartDemo ? Color.green : Color.red));
        }

        /// <summary>
        /// 获取领先玩家文本
        /// </summary>
        private string GetLeadingPlayerText()
        {
            if (scoreManager == null) return "未知";

            int leadingPlayer = scoreManager.GetLeadingPlayer();
            return leadingPlayer switch
            {
                1 => "玩家1",
                2 => "玩家2",
                _ => "平局"
            };
        }

        /// <summary>
        /// 获取标签样式
        /// </summary>
        private GUIStyle GetLabelStyle(int fontSize = 14, Color? color = null)
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = fontSize;
            style.normal.textColor = color ?? Color.white;
            return style;
        }

        /// <summary>
        /// 绘制演示区域Gizmos
        /// </summary>
        private void DrawDemoGizmos()
        {
            // 绘制演示区域
            Gizmos.color = Color.green;
            Vector3 center = demoArea != null ? demoArea.position : transform.position;
            Gizmos.DrawWireCube(center, new Vector3(5f, 2f, 3f));

            // 绘制发球高度线
            if (serveValidator != null)
            {
                Gizmos.color = Color.yellow;
                float minHeight = serveValidator.MinThrowHeight;
                Gizmos.DrawWireCube(center + Vector3.up * minHeight, new Vector3(1f, 0.02f, 1f));
            }

            // 绘制坐标轴
            Gizmos.color = Color.red;
            Gizmos.DrawLine(center, center + Vector3.right * 0.5f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(center, center + Vector3.up * 0.5f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(center, center + Vector3.forward * 0.5f);
        }
        #endregion

        #region Editor输入处理
        /// <summary>
        /// 订阅Editor输入事件
        /// </summary>
        private void SubscribeToEditorInputEvents()
        {
            PongHub.Input.EditorInputSimulator.OnLeftPaddleHit += HandleLeftPaddleHit;
            PongHub.Input.EditorInputSimulator.OnRightPaddleHit += HandleRightPaddleHit;
            PongHub.Input.EditorInputSimulator.OnServeAction += HandleServeAction;
            PongHub.Input.EditorInputSimulator.OnMoveInput += HandleMoveInput;
            PongHub.Input.EditorInputSimulator.OnMenuPressed += HandleMenuPressed;
            PongHub.Input.EditorInputSimulator.OnPositionReset += HandlePositionReset;
        }

        /// <summary>
        /// 取消订阅Editor输入事件
        /// </summary>
        private void UnsubscribeFromEditorInputEvents()
        {
            PongHub.Input.EditorInputSimulator.OnLeftPaddleHit -= HandleLeftPaddleHit;
            PongHub.Input.EditorInputSimulator.OnRightPaddleHit -= HandleRightPaddleHit;
            PongHub.Input.EditorInputSimulator.OnServeAction -= HandleServeAction;
            PongHub.Input.EditorInputSimulator.OnMoveInput -= HandleMoveInput;
            PongHub.Input.EditorInputSimulator.OnMenuPressed -= HandleMenuPressed;
            PongHub.Input.EditorInputSimulator.OnPositionReset -= HandlePositionReset;
        }

        /// <summary>
        /// 处理左拍击球
        /// </summary>
        private void HandleLeftPaddleHit(bool isPressed)
        {
            if (isPressed)
            {
                Debug.Log("[GameDemo] Editor输入: 左拍击球");
                // 这里可以触发左拍击球逻辑
            }
        }

        /// <summary>
        /// 处理右拍击球
        /// </summary>
        private void HandleRightPaddleHit(bool isPressed)
        {
            if (isPressed)
            {
                Debug.Log("[GameDemo] Editor输入: 右拍击球");
                // 这里可以触发右拍击球逻辑
            }
        }

        /// <summary>
        /// 处理发球动作
        /// </summary>
        private void HandleServeAction(Vector3 position, Vector3 velocity)
        {
            Debug.Log($"[GameDemo] Editor输入: 发球动作 - 位置: {position}, 速度: {velocity}");
            // 这里可以触发发球逻辑
        }

        /// <summary>
        /// 处理移动输入
        /// </summary>
        private void HandleMoveInput(Vector2 moveInput)
        {
            // 可以在这里处理移动逻辑
            if (moveInput != Vector2.zero)
            {
                Debug.Log($"[GameDemo] Editor输入: 移动 - {moveInput}");
            }
        }

        /// <summary>
        /// 处理菜单按键
        /// </summary>
        private void HandleMenuPressed(bool isPressed)
        {
            Debug.Log($"[GameDemo] Editor输入: 菜单按键 - {isPressed}");
        }

        /// <summary>
        /// 处理位置重置
        /// </summary>
        private void HandlePositionReset()
        {
            Debug.Log("[GameDemo] Editor输入: 位置重置");
            ResetDemo();
        }
        #endregion

        #region 公共接口
        /// <summary>
        /// 获取演示状态信息
        /// </summary>
        public string GetDemoInfo()
        {
            return $"演示状态:\n" +
                   $"已初始化: {isInitialized}\n" +
                   $"自动演示: {autoStartDemo}\n" +
                   $"键盘控制: {enableKeyboardControls}\n" +
                   $"调试显示: {showDebugInfo}\n" +
                   $"游戏模式控制器: {(gameModeController != null ? "已连接" : "未连接")}\n" +
                   $"发球验证器: {(serveValidator != null ? "已连接" : "未连接")}\n" +
                   $"Editor输入模拟器: {(editorInputSimulator != null ? "已连接" : "未连接")}\n" +
                   $"比赛管理器: {(matchManager != null ? "已连接" : "未连接")}\n" +
                   $"分数管理器: {(scoreManager != null ? "已连接" : "未连接")}";
        }

        /// <summary>
        /// 设置演示配置
        /// </summary>
        public void SetDemoConfig(bool autoStart, bool keyboardControls, bool debugInfo)
        {
            autoStartDemo = autoStart;
            enableKeyboardControls = keyboardControls;
            showDebugInfo = debugInfo;

            Debug.Log($"[GameDemo] 演示配置已更新");
        }
        #endregion
    }
}