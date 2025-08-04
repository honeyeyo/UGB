// Copyright (c) Meta Platforms, Inc. and affiliates.

#if HAS_META_AVATARS

using Oculus.Avatar2;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using PongHub.Core;
using PongHub.VR;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Meta.Utilities.Input
{
    /// <summary>
    /// 增强的XR输入管理器
    /// 在原有XRInputManager基础上添加Hand Tracking支持
    /// 支持手势识别、输入模式切换和通用VR交互手势
    /// </summary>
    public class EnhancedXRInputManager : XRInputManager
    {
        /// <summary>
        /// VR输入模式枚举
        /// </summary>
        public enum VRInputMode
        {
            Controller,     // 控制器模式
            HandTracking,   // 手部追踪模式
            Hybrid          // 混合模式（同时支持）
        }

        /// <summary>
        /// 手势类型枚举
        /// </summary>
        public enum HandGesture
        {
            None,
            Pinch,          // 捏取 - UI交互和小物体抓取  
            Point,          // 指向 - 射线交互和选择
            Fist,           // 握拳 - 通用抓取手势
            OpenHand,       // 张开 - 释放和展示
            ThumbsUp,       // 点赞 - 确认操作
            MenuGesture     // 菜单手势 - 打开/关闭菜单
        }

        [Header("Hand Tracking Settings")]
        [SerializeField]
        [Tooltip("是否启用手部追踪")]
        private bool m_enableHandTracking = true;

        [SerializeField]
        [Tooltip("手部追踪置信度阈值 (0-1)")]
        [Range(0f, 1f)]
        private float m_handTrackingConfidenceThreshold = 0.7f;

        [SerializeField]
        [Tooltip("手势识别置信度阈值 (0-1)")]
        [Range(0.5f, 1f)]
        private float m_gestureRecognitionThreshold = 0.8f;

        [SerializeField]
        [Tooltip("输入模式自动切换")]
        private bool m_autoSwitchInputMode = true;

        [SerializeField]
        [Tooltip("手势识别更新频率 (每秒次数)")]
        [Range(10f, 60f)]
        private float m_gestureUpdateRate = 30f;

        [Header("Hand Tracking References")]
        [SerializeField]
        [Tooltip("左手OVRHand组件引用")]
        private OVRHand m_leftHand;

        [SerializeField]
        [Tooltip("右手OVRHand组件引用")]
        private OVRHand m_rightHand;

        [SerializeField]
        [Tooltip("左手骨骼组件引用")]
        private OVRSkeleton m_leftHandSkeleton;

        [SerializeField]
        [Tooltip("右手骨骼组件引用")]
        private OVRSkeleton m_rightHandSkeleton;

        // 内部状态
        private VRInputMode m_currentInputMode = VRInputMode.Controller;
        private Dictionary<bool, float> m_handTrackingConfidence = new Dictionary<bool, float>();
        private Dictionary<bool, HandGesture> m_currentHandGestures = new Dictionary<bool, HandGesture>();
        private Dictionary<HandGesture, System.Action<bool, bool>> m_gestureCallbacks = new Dictionary<HandGesture, System.Action<bool, bool>>();
        
        // 手势识别相关
        private HandGestureRecognizer m_gestureRecognizer;
        private Coroutine m_gestureUpdateCoroutine;
        
        // 性能监控
        private float m_lastGestureUpdateTime;
        private int m_gestureRecognitionCount;

        // 事件
        public System.Action<VRInputMode, VRInputMode> OnInputModeChanged;
        public System.Action<HandGesture, bool, bool> OnGestureRecognized; // gesture, isLeftHand, started

        /// <summary>
        /// 当前输入模式
        /// </summary>
        public VRInputMode CurrentInputMode => m_currentInputMode;

        /// <summary>
        /// 手部追踪是否可用
        /// </summary>
        public bool IsHandTrackingAvailable
        {
            get
            {
                return m_enableHandTracking &&
                       m_leftHand != null && m_leftHand.IsTracked &&
                       m_rightHand != null && m_rightHand.IsTracked;
            }
        }

        /// <summary>
        /// 获取手部追踪置信度
        /// </summary>
        public float GetHandTrackingConfidence(bool isLeftHand)
        {
            return m_handTrackingConfidence.TryGetValue(isLeftHand, out float confidence) ? confidence : 0f;
        }

        /// <summary>
        /// 获取当前手势
        /// </summary>
        public HandGesture GetCurrentHandGesture(bool isLeftHand)
        {
            return m_currentHandGestures.TryGetValue(isLeftHand, out HandGesture gesture) ? gesture : HandGesture.None;
        }

        protected override void Awake()
        {
            base.Awake();
            InitializeHandTracking();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            StartHandGestureRecognition();
        }

        protected override void OnDisable()
        {
            StopHandGestureRecognition();
            base.OnDisable();
        }

        private void Update()
        {
            UpdateHandTrackingStatus();
            if (m_autoSwitchInputMode)
            {
                UpdateInputModeAutoSwitching();
            }
        }

        private void InitializeHandTracking()
        {
            // 初始化手势识别器
            m_gestureRecognizer = new HandGestureRecognizer();
            m_gestureRecognizer.SetConfidenceThreshold(m_gestureRecognitionThreshold);

            // 初始化手部状态
            m_handTrackingConfidence[true] = 0f;  // 左手
            m_handTrackingConfidence[false] = 0f; // 右手
            m_currentHandGestures[true] = HandGesture.None;
            m_currentHandGestures[false] = HandGesture.None;

            // 尝试自动查找Hand组件（如果没有手动分配）
            if (m_leftHand == null || m_rightHand == null)
            {
                AutoFindHandComponents();
            }

            Debug.Log($"[EnhancedXRInputManager] Hand Tracking initialized. Left Hand: {m_leftHand != null}, Right Hand: {m_rightHand != null}");
        }

        private void AutoFindHandComponents()
        {
            var ovrHands = FindObjectsOfType<OVRHand>();
            foreach (var hand in ovrHands)
            {
                if (hand.HandType == OVRHand.Hand.HandLeft && m_leftHand == null)
                {
                    m_leftHand = hand;
                    m_leftHandSkeleton = hand.GetComponent<OVRSkeleton>();
                    Debug.Log("[EnhancedXRInputManager] Auto-found left hand component");
                }
                else if (hand.HandType == OVRHand.Hand.HandRight && m_rightHand == null)
                {
                    m_rightHand = hand;
                    m_rightHandSkeleton = hand.GetComponent<OVRSkeleton>();
                    Debug.Log("[EnhancedXRInputManager] Auto-found right hand component");
                }
            }
        }

        private void StartHandGestureRecognition()
        {
            if (m_gestureUpdateCoroutine == null && m_enableHandTracking)
            {
                m_gestureUpdateCoroutine = StartCoroutine(GestureRecognitionLoop());
            }
        }

        private void StopHandGestureRecognition()
        {
            if (m_gestureUpdateCoroutine != null)
            {
                StopCoroutine(m_gestureUpdateCoroutine);
                m_gestureUpdateCoroutine = null;
            }
        }

        private IEnumerator GestureRecognitionLoop()
        {
            float updateInterval = 1f / m_gestureUpdateRate;

            while (true)
            {
                yield return new WaitForSeconds(updateInterval);

                if (m_enableHandTracking && m_gestureRecognizer != null)
                {
                    UpdateHandGestureRecognition();
                    m_gestureRecognitionCount++;
                }
            }
        }

        private void UpdateHandGestureRecognition()
        {
            m_lastGestureUpdateTime = Time.time;

            // 识别左手手势
            if (m_leftHand != null && m_leftHand.IsDataValid)
            {
                var leftGesture = m_gestureRecognizer.RecognizeGesture(m_leftHand, m_leftHandSkeleton);
                UpdateHandGesture(true, leftGesture);
            }

            // 识别右手手势
            if (m_rightHand != null && m_rightHand.IsDataValid)
            {
                var rightGesture = m_gestureRecognizer.RecognizeGesture(m_rightHand, m_rightHandSkeleton);
                UpdateHandGesture(false, rightGesture);
            }
        }

        private void UpdateHandGesture(bool isLeftHand, HandGesture newGesture)
        {
            var previousGesture = m_currentHandGestures[isLeftHand];
            
            if (previousGesture != newGesture)
            {
                m_currentHandGestures[isLeftHand] = newGesture;

                // 触发手势事件
                OnGestureRecognized?.Invoke(newGesture, isLeftHand, true);

                // 触发手势回调
                if (m_gestureCallbacks.TryGetValue(newGesture, out var callback))
                {
                    callback?.Invoke(isLeftHand, true);
                }

                // 触发之前手势的结束事件
                if (previousGesture != HandGesture.None && m_gestureCallbacks.TryGetValue(previousGesture, out var endCallback))
                {
                    endCallback?.Invoke(isLeftHand, false);
                }

                Debug.Log($"[EnhancedXRInputManager] Hand gesture changed: {(isLeftHand ? "Left" : "Right")} hand {previousGesture} -> {newGesture}");
            }
        }

        private void UpdateHandTrackingStatus()
        {
            // 更新左手置信度
            if (m_leftHand != null)
            {
                float leftConfidence = m_leftHand.IsDataHighConfidence ? 1f : (m_leftHand.IsDataValid ? 0.5f : 0f);
                m_handTrackingConfidence[true] = leftConfidence;
            }

            // 更新右手置信度
            if (m_rightHand != null)
            {
                float rightConfidence = m_rightHand.IsDataHighConfidence ? 1f : (m_rightHand.IsDataValid ? 0.5f : 0f);
                m_handTrackingConfidence[false] = rightConfidence;
            }
        }

        private void UpdateInputModeAutoSwitching()
        {
            bool hasHighConfidenceHandTracking = 
                GetHandTrackingConfidence(true) >= m_handTrackingConfidenceThreshold ||
                GetHandTrackingConfidence(false) >= m_handTrackingConfidenceThreshold;

            bool hasControllerConnected = IsControllerConnected(true) || IsControllerConnected(false);

            VRInputMode newMode = m_currentInputMode;

            // 自动切换逻辑
            if (hasHighConfidenceHandTracking && !hasControllerConnected && m_currentInputMode != VRInputMode.HandTracking)
            {
                newMode = VRInputMode.HandTracking;
            }
            else if (hasControllerConnected && !hasHighConfidenceHandTracking && m_currentInputMode != VRInputMode.Controller)
            {
                newMode = VRInputMode.Controller;
            }
            else if (hasControllerConnected && hasHighConfidenceHandTracking && m_currentInputMode != VRInputMode.Hybrid)
            {
                newMode = VRInputMode.Hybrid;
            }

            if (newMode != m_currentInputMode)
            {
                SwitchToMode(newMode);
            }
        }

        /// <summary>
        /// 切换到指定输入模式
        /// </summary>
        public void SwitchToMode(VRInputMode mode)
        {
            var previousMode = m_currentInputMode;
            m_currentInputMode = mode;

            OnInputModeChanged?.Invoke(mode, previousMode);

            Debug.Log($"[EnhancedXRInputManager] Input mode switched: {previousMode} -> {mode}");
        }

        /// <summary>
        /// 检查控制器是否连接
        /// </summary>
        public bool IsControllerConnected(bool isLeftHand)
        {
            var controllerNode = isLeftHand ? UnityEngine.XR.XRNode.LeftHand : UnityEngine.XR.XRNode.RightHand;
            var devices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(controllerNode, devices);
            return devices.Count > 0 && devices[0].isValid;
        }

        /// <summary>
        /// 注册手势回调
        /// </summary>
        public void RegisterGestureCallback(HandGesture gesture, System.Action<bool, bool> callback)
        {
            m_gestureCallbacks[gesture] = callback;
        }

        /// <summary>
        /// 取消注册手势回调
        /// </summary>
        public void UnregisterGestureCallback(HandGesture gesture)
        {
            m_gestureCallbacks.Remove(gesture);
        }

        /// <summary>
        /// 启用/禁用手部追踪
        /// </summary>
        public void SetHandTrackingEnabled(bool enabled)
        {
            m_enableHandTracking = enabled;
            
            if (enabled)
            {
                StartHandGestureRecognition();
            }
            else
            {
                StopHandGestureRecognition();
                SwitchToMode(VRInputMode.Controller);
            }
        }

        /// <summary>
        /// 设置手势识别置信度阈值
        /// </summary>
        public void SetGestureRecognitionThreshold(float threshold)
        {
            m_gestureRecognitionThreshold = Mathf.Clamp01(threshold);
            if (m_gestureRecognizer != null)
            {
                m_gestureRecognizer.SetConfidenceThreshold(threshold);
            }
        }

        /// <summary>
        /// 获取手势识别统计信息
        /// </summary>
        public string GetGestureRecognitionStats()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== Hand Gesture Recognition Stats ===");
            stats.AppendLine($"Hand Tracking Enabled: {m_enableHandTracking}");
            stats.AppendLine($"Current Input Mode: {m_currentInputMode}");
            stats.AppendLine($"Left Hand Confidence: {GetHandTrackingConfidence(true):F2}");
            stats.AppendLine($"Right Hand Confidence: {GetHandTrackingConfidence(false):F2}");
            stats.AppendLine($"Left Hand Gesture: {GetCurrentHandGesture(true)}");
            stats.AppendLine($"Right Hand Gesture: {GetCurrentHandGesture(false)}");
            stats.AppendLine($"Recognition Count: {m_gestureRecognitionCount}");
            stats.AppendLine($"Last Update: {(Time.time - m_lastGestureUpdateTime):F2}s ago");
            return stats.ToString();
        }

        /// <summary>
        /// 获取手部位置（世界坐标）
        /// </summary>
        public Vector3 GetHandPosition(bool isLeftHand)
        {
            var hand = isLeftHand ? m_leftHand : m_rightHand;
            return hand != null && hand.IsDataValid ? hand.transform.position : Vector3.zero;
        }

        /// <summary>
        /// 获取手部旋转（世界坐标）
        /// </summary>
        public Quaternion GetHandRotation(bool isLeftHand)
        {
            var hand = isLeftHand ? m_leftHand : m_rightHand;
            return hand != null && hand.IsDataValid ? hand.transform.rotation : Quaternion.identity;
        }

        /// <summary>
        /// 获取指针位置（用于射线交互）
        /// </summary>
        public Vector3 GetPointerPosition(bool isLeftHand)
        {
            var hand = isLeftHand ? m_leftHand : m_rightHand;
            return hand != null && hand.IsPointerPoseValid ? hand.PointerPose.position : Vector3.zero;
        }

        /// <summary>
        /// 获取指针方向（用于射线交互）
        /// </summary>
        public Vector3 GetPointerDirection(bool isLeftHand)
        {
            var hand = isLeftHand ? m_leftHand : m_rightHand;
            return hand != null && hand.IsPointerPoseValid ? hand.PointerPose.forward : Vector3.forward;
        }
    }
}

#endif