using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using PongHub.UI.Settings.Core;
using PongHub.UI.ModeSelection;

namespace PongHub.UI.Settings.Components
{
    /// <summary>
    /// 按键绑定设置组件
    /// Key binding setting component for remapping controls
    /// </summary>
    public class SettingKeyBinding : SettingComponentBase
    {
        [Header("按键绑定配置")]
        [SerializeField]
        [Tooltip("当前按键显示文本")]
        private TextMeshProUGUI currentKeyText;

        [SerializeField]
        [Tooltip("绑定按钮")]
        private Button bindButton;

        [SerializeField]
        [Tooltip("重置按钮")]
        private Button resetButton;

        [SerializeField]
        [Tooltip("等待输入提示")]
        private GameObject waitingForInputIndicator;

        [Header("按键绑定设置")]
        [SerializeField]
        [Tooltip("绑定的动作名称")]
        private string actionName;

        [SerializeField]
        [Tooltip("默认按键")]
        private KeyCode defaultKey = KeyCode.None;

        [SerializeField]
        [Tooltip("允许的按键类型")]
        private KeyType allowedKeyTypes = KeyType.All;

        [SerializeField]
        [Tooltip("冲突检测")]
        private bool detectConflicts = true;

        [Header("视觉反馈")]
        [SerializeField]
        [Tooltip("正常状态颜色")]
        private Color normalColor = Color.white;

        [SerializeField]
        [Tooltip("等待输入颜色")]
        private Color waitingColor = Color.yellow;

        [SerializeField]
        [Tooltip("冲突状态颜色")]
        private Color conflictColor = Color.red;

        [SerializeField]
        [Tooltip("无效按键颜色")]
        private Color invalidColor = Color.gray;

        // 按键类型枚举
        [Flags]
        public enum KeyType
        {
            None = 0,
            Keyboard = 1,
            Mouse = 2,
            Gamepad = 4,
            VR = 8,
            All = Keyboard | Mouse | Gamepad | VR
        }

        // 内部状态
        private KeyCode currentKey;
        private bool isWaitingForInput = false;
        private bool hasConflict = false;
        private Coroutine inputWaitCoroutine;
        private Image backgroundImage;

        // 事件
        public event Action<string, KeyCode> OnKeyBindingChanged;
        public event Action<string, KeyCode> OnConflictDetected;

        #region 重写基类方法

        protected override void SetupUI()
        {
            // 获取组件引用
            if (bindButton == null)
                bindButton = GetComponentInChildren<Button>();

            if (currentKeyText == null)
                currentKeyText = GetComponentInChildren<TextMeshProUGUI>();

            if (resetButton == null)
            {
                var buttons = GetComponentsInChildren<Button>();
                if (buttons.Length > 1)
                    resetButton = buttons[1];
            }

            backgroundImage = GetComponent<Image>();

            // 设置按钮事件
            if (bindButton != null)
            {
                bindButton.onClick.AddListener(StartKeyBinding);
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(ResetToDefault);
            }

            // 隐藏等待输入指示器
            if (waitingForInputIndicator != null)
            {
                waitingForInputIndicator.SetActive(false);
            }

            // 加载当前按键绑定
            LoadCurrentBinding();
            UpdateUI();
        }

        protected override void RefreshValue()
        {
            LoadCurrentBinding();
        }

        protected override void ApplyValue(object newValue)
        {
            if (newValue is KeyCode keyCode)
            {
                SetKeyBinding(keyCode);
            }
        }

        protected override void UpdateUI()
        {
            UpdateKeyDisplay();
            UpdateVisualState();
        }

        protected override bool ValidateValue(object value)
        {
            if (value is KeyCode keyCode)
            {
                return IsValidKey(keyCode);
            }
            return false;
        }

        public override void ResetToDefault()
        {
            SetKeyBinding(defaultKey);
            PlayHapticFeedback();
        }

        #endregion

        #region UI更新

        private void UpdateKeyDisplay()
        {
            if (currentKeyText == null) return;

            string displayText = GetKeyDisplayName(currentKey);
            currentKeyText.text = displayText;

            // 更新本地化
            if (!string.IsNullOrEmpty(localizationKey))
            {
                UpdateLocalizedText();
            }
        }

        private void UpdateVisualState()
        {
            Color targetColor = normalColor;

            if (isWaitingForInput)
            {
                targetColor = waitingColor;
            }
            else if (hasConflict)
            {
                targetColor = conflictColor;
            }
            else if (!IsValidKey(currentKey))
            {
                targetColor = invalidColor;
            }

            // 更新背景颜色
            if (backgroundImage != null)
            {
                backgroundImage.color = targetColor;
            }

            // 更新等待输入指示器
            if (waitingForInputIndicator != null)
            {
                waitingForInputIndicator.SetActive(isWaitingForInput);
            }

            // 更新按钮状态
            if (bindButton != null)
            {
                bindButton.interactable = !isWaitingForInput;
            }
        }

        private string GetKeyDisplayName(KeyCode key)
        {
            if (key == KeyCode.None)
                return "未绑定";

            // 特殊按键的中文显示
            switch (key)
            {
                case KeyCode.LeftControl: return "左Ctrl";
                case KeyCode.RightControl: return "右Ctrl";
                case KeyCode.LeftShift: return "左Shift";
                case KeyCode.RightShift: return "右Shift";
                case KeyCode.LeftAlt: return "左Alt";
                case KeyCode.RightAlt: return "右Alt";
                case KeyCode.Space: return "空格";
                case KeyCode.Return: return "回车";
                case KeyCode.Escape: return "ESC";
                case KeyCode.Tab: return "Tab";
                case KeyCode.Backspace: return "退格";
                case KeyCode.Delete: return "删除";
                case KeyCode.UpArrow: return "↑";
                case KeyCode.DownArrow: return "↓";
                case KeyCode.LeftArrow: return "←";
                case KeyCode.RightArrow: return "→";
                case KeyCode.Mouse0: return "鼠标左键";
                case KeyCode.Mouse1: return "鼠标右键";
                case KeyCode.Mouse2: return "鼠标中键";
                case KeyCode.Mouse3: return "鼠标侧键1";
                case KeyCode.Mouse4: return "鼠标侧键2";
                default: return key.ToString();
            }
        }

        #endregion

        #region 按键绑定逻辑

        private void LoadCurrentBinding()
        {
            if (SettingsManager.Instance == null) return;

            var controlSettings = SettingsManager.Instance.GetControlSettings();
            if (controlSettings.keyBindings != null &&
                controlSettings.keyBindings.ContainsKey(actionName))
            {
                currentKey = controlSettings.keyBindings[actionName];
            }
            else
            {
                currentKey = defaultKey;
            }

            currentValue = currentKey;
            CheckForConflicts();
        }

        private void StartKeyBinding()
        {
            if (isWaitingForInput) return;

            isWaitingForInput = true;
            PlayHapticFeedback();

            // 开始等待输入
            inputWaitCoroutine = StartCoroutine(WaitForKeyInput());
            UpdateUI();
        }

        private IEnumerator WaitForKeyInput()
        {
            float timeout = 10f; // 10秒超时
            float elapsed = 0f;

            while (elapsed < timeout && isWaitingForInput)
            {
                // 检测键盘输入
                if (allowedKeyTypes.HasFlag(KeyType.Keyboard))
                {
                    foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
                    {
                        if (UnityEngine.Input.GetKeyDown(key) && IsKeyboardKey(key) && IsValidKey(key))
                        {
                            SetKeyBinding(key);
                            yield break;
                        }
                    }
                }

                // 检测鼠标输入
                if (allowedKeyTypes.HasFlag(KeyType.Mouse))
                {
                    for (int i = 0; i < 7; i++)
                    {
                        if (UnityEngine.Input.GetMouseButtonDown(i))
                        {
                            KeyCode mouseKey = (KeyCode)((int)KeyCode.Mouse0 + i);
                            if (IsValidKey(mouseKey))
                            {
                                SetKeyBinding(mouseKey);
                                yield break;
                            }
                        }
                    }
                }

                // 检测游戏手柄输入
                if (allowedKeyTypes.HasFlag(KeyType.Gamepad))
                {
                    // 检测游戏手柄按键
                    CheckGamepadInput();
                }

                // 检测VR控制器输入
                if (allowedKeyTypes.HasFlag(KeyType.VR) && UnityEngine.XR.XRSettings.enabled)
                {
                    CheckVRInput();
                }

                // ESC键取消绑定
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    CancelKeyBinding();
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 超时取消
            if (isWaitingForInput)
            {
                CancelKeyBinding();
            }
        }

        private void CheckGamepadInput()
        {
            // 检测游戏手柄按键
            string[] joystickButtons = {
                "joystick button 0", "joystick button 1", "joystick button 2", "joystick button 3",
                "joystick button 4", "joystick button 5", "joystick button 6", "joystick button 7",
                "joystick button 8", "joystick button 9", "joystick button 10", "joystick button 11",
                "joystick button 12", "joystick button 13", "joystick button 14", "joystick button 15"
            };

            for (int i = 0; i < joystickButtons.Length; i++)
            {
                if (UnityEngine.Input.GetKeyDown(joystickButtons[i]))
                {
                    KeyCode gamepadKey = (KeyCode)((int)KeyCode.JoystickButton0 + i);
                    if (IsValidKey(gamepadKey))
                    {
                        SetKeyBinding(gamepadKey);
                        return;
                    }
                }
            }
        }

        private void CheckVRInput()
        {
            // 检测VR控制器输入
            // 这里可以集成VR SDK的按键检测
            var inputDevices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                UnityEngine.XR.InputDeviceCharacteristics.Controller, inputDevices);

            foreach (var device in inputDevices)
            {
                // 检测触发器按钮
                if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool triggerPressed) && triggerPressed)
                {
                    // 映射到特定的KeyCode
                    SetKeyBinding(KeyCode.Space); // 临时映射
                    return;
                }

                // 检测手柄按钮
                if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool gripPressed) && gripPressed)
                {
                    SetKeyBinding(KeyCode.LeftControl); // 临时映射
                    return;
                }
            }
        }

        private void SetKeyBinding(KeyCode newKey)
        {
            if (!IsValidKey(newKey))
            {
                Debug.LogWarning($"Invalid key for binding: {newKey}");
                CancelKeyBinding();
                return;
            }

            var oldKey = currentKey;
            currentKey = newKey;
            currentValue = newKey;

            // 检查冲突
            if (detectConflicts)
            {
                CheckForConflicts();
            }

            // 保存到设置
            SaveKeyBinding();

            // 停止等待输入
            StopWaitingForInput();

            // 触发事件
            OnKeyBindingChanged?.Invoke(actionName, newKey);

            // 播放反馈
            PlayHapticFeedback();

            // 更新UI
            UpdateUI();

            Debug.Log($"Key binding changed: {actionName} = {newKey} (was {oldKey})");
        }

        private void CancelKeyBinding()
        {
            StopWaitingForInput();
            UpdateUI();
        }

        private void StopWaitingForInput()
        {
            isWaitingForInput = false;

            if (inputWaitCoroutine != null)
            {
                StopCoroutine(inputWaitCoroutine);
                inputWaitCoroutine = null;
            }
        }

        private void SaveKeyBinding()
        {
            if (SettingsManager.Instance == null) return;

            var controlSettings = SettingsManager.Instance.GetControlSettings();
            if (controlSettings.keyBindings == null)
            {
                controlSettings.keyBindings = new System.Collections.Generic.Dictionary<string, KeyCode>();
            }

            controlSettings.keyBindings[actionName] = currentKey;
            SettingsManager.Instance.SaveControlSettings(controlSettings);
        }

        #endregion

        #region 验证和冲突检测

        private bool IsValidKey(KeyCode key)
        {
            if (key == KeyCode.None) return false;

            // 检查是否在允许的按键类型中
            if (IsKeyboardKey(key) && !allowedKeyTypes.HasFlag(KeyType.Keyboard)) return false;
            if (IsMouseKey(key) && !allowedKeyTypes.HasFlag(KeyType.Mouse)) return false;
            if (IsGamepadKey(key) && !allowedKeyTypes.HasFlag(KeyType.Gamepad)) return false;

            // 禁止的按键
            KeyCode[] forbiddenKeys = {
                KeyCode.None,
                KeyCode.Menu,
                KeyCode.Print,
                KeyCode.SysReq,
                KeyCode.Break,
                KeyCode.Pause
            };

            return Array.IndexOf(forbiddenKeys, key) == -1;
        }

        private bool IsKeyboardKey(KeyCode key)
        {
            return (int)key >= (int)KeyCode.Backspace && (int)key <= (int)KeyCode.Menu;
        }

        private bool IsMouseKey(KeyCode key)
        {
            return key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6;
        }

        private bool IsGamepadKey(KeyCode key)
        {
            return key >= KeyCode.JoystickButton0 && key <= KeyCode.Joystick8Button19;
        }

        private void CheckForConflicts()
        {
            hasConflict = false;

            if (!detectConflicts || SettingsManager.Instance == null)
                return;

            var controlSettings = SettingsManager.Instance.GetControlSettings();
            if (controlSettings.keyBindings == null)
                return;

            // 检查是否有其他动作使用了相同的按键
            foreach (var binding in controlSettings.keyBindings)
            {
                if (binding.Key != actionName && binding.Value == currentKey && currentKey != KeyCode.None)
                {
                    hasConflict = true;
                    OnConflictDetected?.Invoke(binding.Key, currentKey);
                    Debug.LogWarning($"Key binding conflict: {actionName} and {binding.Key} both use {currentKey}");
                    break;
                }
            }
        }

        #endregion

        #region 工具方法

        private void PlayHapticFeedback()
        {
            if (enableHapticFeedback && hapticFeedback != null)
            {
                hapticFeedback.PlayHaptic(VRHapticFeedback.HapticType.Selection);
            }
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 设置动作名称
        /// </summary>
        /// <param name="action">动作名称</param>
        public void SetActionName(string action)
        {
            actionName = action;
            LoadCurrentBinding();
            UpdateUI();
        }

        /// <summary>
        /// 设置默认按键
        /// </summary>
        /// <param name="key">默认按键</param>
        public void SetDefaultKey(KeyCode key)
        {
            defaultKey = key;
        }

        /// <summary>
        /// 获取当前绑定的按键
        /// </summary>
        /// <returns>当前按键</returns>
        public KeyCode GetCurrentKey()
        {
            return currentKey;
        }

        /// <summary>
        /// 强制设置按键绑定
        /// </summary>
        /// <param name="key">要设置的按键</param>
        public void ForceSetKey(KeyCode key)
        {
            SetKeyBinding(key);
        }

        /// <summary>
        /// 检查是否有冲突
        /// </summary>
        /// <returns>是否有冲突</returns>
        public bool HasConflict()
        {
            return hasConflict;
        }

        /// <summary>
        /// 清除按键绑定
        /// </summary>
        public void ClearBinding()
        {
            SetKeyBinding(KeyCode.None);
        }

        /// <summary>
        /// 开始按键绑定（公共接口）
        /// </summary>
        public void StartBinding()
        {
            StartKeyBinding();
        }

        /// <summary>
        /// 取消按键绑定（公共接口）
        /// </summary>
        public void CancelBinding()
        {
            CancelKeyBinding();
        }

        #endregion

        #region Unity事件

        private void OnDisable()
        {
            // 如果正在等待输入，取消绑定
            if (isWaitingForInput)
            {
                CancelKeyBinding();
            }
        }

        protected override void OnDestroy()
        {
            // 清理协程
            if (inputWaitCoroutine != null)
            {
                StopCoroutine(inputWaitCoroutine);
            }

            // 调用基类的OnDestroy
            base.OnDestroy();
        }

        #endregion
    }
}