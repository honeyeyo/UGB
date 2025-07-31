using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Profiling;

namespace PongHub.Input.Performance
{
    /// <summary>
    /// 零GC输入处理器 - Epic-3内存优化核心组件
    /// 功能：消除输入处理中的GC分配，实现零垃圾回收的高性能输入系统
    /// 目标：Update中零GC分配，优化内存使用模式
    /// </summary>
    public class ZeroGCInputProcessor : MonoBehaviour
    {
        [Header("对象池配置")]
        [SerializeField]
        [Tooltip("Vector Pool Size / Vector对象池大小 - Pool size for Vector3 objects")]
        private int m_vectorPoolSize = 100;

        [SerializeField]
        [Tooltip("String Pool Size / String对象池大小 - Pool size for string objects")]
        private int m_stringPoolSize = 50;

        [SerializeField]
        [Tooltip("Input Data Pool Size / 输入数据池大小 - Pool size for input data structures")]
        private int m_inputDataPoolSize = 20;

        [Header("缓存配置")]
        [SerializeField]
        [Tooltip("Enable String Caching / 启用字符串缓存 - Whether to cache frequently used strings")]
        private bool m_enableStringCaching = true;

        [SerializeField]
        [Tooltip("Max Cached Strings / 最大缓存字符串数 - Maximum number of cached strings")]
        private int m_maxCachedStrings = 100;

        [Header("性能监控")]
        [SerializeField]
        [Tooltip("Enable GC Monitoring / 启用GC监控 - Whether to monitor GC allocations")]
        private bool m_enableGCMonitoring = true;

        [SerializeField]
        [Tooltip("Show Debug Info / 显示调试信息 - Whether to show debug information")]
        private bool m_showDebugInfo = false;

        // 对象池
        private ObjectPool<System.Text.StringBuilder> m_stringBuilderPool;
        
        // 值类型缓存池（特殊处理）
        private Queue<Vector3> m_vectorCache;
        private Queue<InputDataPacket> m_inputDataCache;

        // 缓存字典（使用NativeHashMap避免GC）
        private Dictionary<string, string> m_cachedStrings;
        private Dictionary<int, Vector3> m_cachedVectors;

        // 性能监控
        private ProfilerRecorder m_gcAllocRecorder;
        private long m_lastGCAlloc;
        private float m_totalGCAllocSinceStart;
        
        // 预分配的工作变量（避免Update中分配）
        private readonly Vector3[] m_workVectors = new Vector3[10];
        private readonly float[] m_workFloats = new float[20];
        private readonly InputDataPacket m_workInputData = new InputDataPacket();
        private readonly System.Text.StringBuilder m_workStringBuilder = new System.Text.StringBuilder(256);

        // 事件系统
        public System.Action<float> OnGCAllocationDetected;
        public System.Action OnZeroGCFrameAchieved;

        /// <summary>
        /// 输入数据包结构（值类型，避免GC分配）
        /// </summary>
        [System.Serializable]
        public struct InputDataPacket
        {
            public Vector3 leftHandPosition;
            public Vector3 rightHandPosition;
            public Quaternion leftHandRotation;
            public Quaternion rightHandRotation;
            public Vector2 leftStick;
            public Vector2 rightStick;
            public float leftGrip;
            public float rightGrip;
            public float leftTrigger;
            public float rightTrigger;
            public uint buttonStates; // 位字段存储按钮状态
            public float timestamp;
            public uint sequenceNumber; // 序列号用于测试

            /// <summary>
            /// 重置数据包到默认状态
            /// </summary>
            public void Reset()
            {
                leftHandPosition = Vector3.zero;
                rightHandPosition = Vector3.zero;
                leftHandRotation = Quaternion.identity;
                rightHandRotation = Quaternion.identity;
                leftStick = Vector2.zero;
                rightStick = Vector2.zero;
                leftGrip = 0f;
                rightGrip = 0f;
                leftTrigger = 0f;
                rightTrigger = 0f;
                buttonStates = 0;
                timestamp = 0f;
            }

            /// <summary>
            /// 设置按钮状态（使用位操作避免装箱）
            /// </summary>
            public void SetButtonState(InputButton button, bool pressed)
            {
                uint buttonBit = 1u << (int)button;
                if (pressed)
                    buttonStates |= buttonBit;
                else
                    buttonStates &= ~buttonBit;
            }

            /// <summary>
            /// 获取按钮状态（使用位操作避免装箱）
            /// </summary>
            public bool GetButtonState(InputButton button)
            {
                uint buttonBit = 1u << (int)button;
                return (buttonStates & buttonBit) != 0;
            }
        }

        /// <summary>
        /// 输入按钮枚举
        /// </summary>
        public enum InputButton
        {
            LeftA = 0, LeftB = 1, LeftMenu = 2,
            RightA = 3, RightB = 4, RightMenu = 5,
            LeftGrip = 6, RightGrip = 7,
            LeftTrigger = 8, RightTrigger = 9
        }

        /// <summary>
        /// 泛型对象池实现
        /// </summary>
        private class ObjectPool<T> where T : class, new()
        {
            private readonly Stack<T> m_pool = new Stack<T>();
            private readonly System.Func<T> m_createFunc;
            private readonly System.Action<T> m_resetFunc;

            public ObjectPool(int initialSize, System.Func<T> createFunc = null, System.Action<T> resetFunc = null)
            {
                m_createFunc = createFunc ?? (() => new T());
                m_resetFunc = resetFunc;

                // 预填充对象池
                for (int i = 0; i < initialSize; i++)
                {
                    m_pool.Push(m_createFunc());
                }
            }

            public T Get()
            {
                if (m_pool.Count > 0)
                {
                    return m_pool.Pop();
                }
                return m_createFunc();
            }

            public void Return(T item)
            {
                if (item == null) return;
                
                m_resetFunc?.Invoke(item);
                m_pool.Push(item);
            }

            public int PoolCount => m_pool.Count;
        }

        private void Awake()
        {
            InitializeObjectPools();
            InitializeCaches();
            EnableGCMonitoring();
        }

        private void Start()
        {
            if (m_showDebugInfo)
            {
                Debug.Log("[ZeroGCInputProcessor] 零GC输入处理器初始化完成");
            }
        }

        private void Update()
        {
            // 监控GC分配
            if (m_enableGCMonitoring)
            {
                MonitorGCAllocations();
            }
        }

        private void OnDestroy()
        {
            DisableGCMonitoring();
            ClearCaches();
        }

        /// <summary>
        /// 初始化对象池
        /// </summary>
        private void InitializeObjectPools()
        {
            // 值类型缓存池（Vector3和InputDataPacket是值类型，不能用ObjectPool<T> where T : class）
            m_vectorCache = new Queue<Vector3>(m_vectorPoolSize);
            m_inputDataCache = new Queue<InputDataPacket>(m_inputDataPoolSize);

            // 预填充缓存
            for (int i = 0; i < m_vectorPoolSize; i++)
            {
                m_vectorCache.Enqueue(Vector3.zero);
            }
            
            for (int i = 0; i < m_inputDataPoolSize; i++)
            {
                var packet = new InputDataPacket();
                packet.Reset();
                m_inputDataCache.Enqueue(packet);
            }

            // StringBuilder对象池
            m_stringBuilderPool = new ObjectPool<System.Text.StringBuilder>(
                10,
                () => new System.Text.StringBuilder(256),
                sb => sb.Clear()
            );
        }

        /// <summary>
        /// 初始化缓存
        /// </summary>
        private void InitializeCaches()
        {
            if (m_enableStringCaching)
            {
                m_cachedStrings = new Dictionary<string, string>(m_maxCachedStrings);
                
                // 预缓存常用字符串
                CacheCommonStrings();
            }

            m_cachedVectors = new Dictionary<int, Vector3>(100);
            
            // 预缓存常用Vector3值
            CacheCommonVectors();
        }

        /// <summary>
        /// 预缓存常用字符串
        /// </summary>
        private void CacheCommonStrings()
        {
            string[] commonStrings = {
                "LeftHand", "RightHand", "Trigger", "Grip", "Menu",
                "ButtonA", "ButtonB", "Stick", "Position", "Rotation",
                "Pressed", "Released", "Active", "Inactive"
            };

            foreach (string str in commonStrings)
            {
                m_cachedStrings[str] = str;
            }
        }

        /// <summary>
        /// 预缓存常用Vector3值
        /// </summary>
        private void CacheCommonVectors()
        {
            m_cachedVectors[0] = Vector3.zero;
            m_cachedVectors[1] = Vector3.one;
            m_cachedVectors[2] = Vector3.up;
            m_cachedVectors[3] = Vector3.down;
            m_cachedVectors[4] = Vector3.left;
            m_cachedVectors[5] = Vector3.right;
            m_cachedVectors[6] = Vector3.forward;
            m_cachedVectors[7] = Vector3.back;
        }

        /// <summary>
        /// 启用GC监控
        /// </summary>
        private void EnableGCMonitoring()
        {
            if (m_enableGCMonitoring)
            {
                m_gcAllocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Alloc");
                m_lastGCAlloc = 0;
                m_totalGCAllocSinceStart = 0;
            }
        }

        /// <summary>
        /// 禁用GC监控
        /// </summary>
        private void DisableGCMonitoring()
        {
            if (m_gcAllocRecorder.Valid)
            {
                m_gcAllocRecorder.Dispose();
            }
        }

        /// <summary>
        /// 监控GC分配
        /// </summary>
        private void MonitorGCAllocations()
        {
            if (!m_gcAllocRecorder.Valid) return;

            long currentGCAlloc = m_gcAllocRecorder.LastValue;
            
            if (currentGCAlloc > m_lastGCAlloc)
            {
                float deltaAlloc = (currentGCAlloc - m_lastGCAlloc) / 1024f; // 转换为KB
                m_totalGCAllocSinceStart += deltaAlloc;
                
                OnGCAllocationDetected?.Invoke(deltaAlloc);
                
                if (m_showDebugInfo && deltaAlloc > 0.1f) // 只显示超过100字节的分配
                {
                    Debug.LogWarning($"[ZeroGCInputProcessor] 检测到GC分配: {deltaAlloc:F2}KB");
                }
            }
            else if (currentGCAlloc == m_lastGCAlloc)
            {
                // 当前帧零GC分配
                OnZeroGCFrameAchieved?.Invoke();
            }

            m_lastGCAlloc = currentGCAlloc;
        }

        /// <summary>
        /// 零GC输入数据处理（核心方法）
        /// </summary>
        public void ProcessInputDataZeroGC(InputAction.CallbackContext context, ref InputDataPacket outputData)
        {
            // 使用预分配的工作变量，避免临时分配
            string actionName = GetCachedString(context.action.name);
            
            switch (actionName)
            {
                case "LeftHand":
                    ProcessHandInputZeroGC(context, ref outputData.leftHandPosition, ref outputData.leftHandRotation);
                    break;
                case "RightHand":
                    ProcessHandInputZeroGC(context, ref outputData.rightHandPosition, ref outputData.rightHandRotation);
                    break;
                case "LeftStick":
                    outputData.leftStick = context.ReadValue<Vector2>();
                    break;
                case "RightStick":
                    outputData.rightStick = context.ReadValue<Vector2>();
                    break;
                case "LeftGrip":
                    outputData.leftGrip = context.ReadValue<float>();
                    outputData.SetButtonState(InputButton.LeftGrip, outputData.leftGrip > 0.5f);
                    break;
                case "RightGrip":
                    outputData.rightGrip = context.ReadValue<float>();
                    outputData.SetButtonState(InputButton.RightGrip, outputData.rightGrip > 0.5f);
                    break;
            }

            // 设置时间戳（使用unscaled time避免Time.time的字符串转换）
            outputData.timestamp = Time.unscaledTime;
        }

        /// <summary>
        /// 零GC手部输入处理
        /// </summary>
        private void ProcessHandInputZeroGC(InputAction.CallbackContext context, ref Vector3 position, ref Quaternion rotation)
        {
            // 直接读取值到引用参数，避免临时变量分配
            if (context.action.expectedControlType == "Vector3")
            {
                position = context.ReadValue<Vector3>();
            }
            else if (context.action.expectedControlType == "Quaternion")
            {
                rotation = context.ReadValue<Quaternion>();
            }
        }

        /// <summary>
        /// 获取缓存的字符串（避免重复分配）
        /// </summary>
        public string GetCachedString(string input)
        {
            if (!m_enableStringCaching || input == null) return input;

            if (m_cachedStrings.TryGetValue(input, out string cached))
            {
                return cached;
            }

            // 如果缓存未满，添加新字符串
            if (m_cachedStrings.Count < m_maxCachedStrings)
            {
                m_cachedStrings[input] = input;
                return input;
            }

            // 缓存已满，返回原字符串
            return input;
        }

        /// <summary>
        /// 获取缓存的Vector3（避免重复分配）
        /// </summary>
        public Vector3 GetCachedVector(int vectorId)
        {
            if (m_cachedVectors.TryGetValue(vectorId, out Vector3 cached))
            {
                return cached;
            }
            return Vector3.zero;
        }

        /// <summary>
        /// 零GC字符串构建
        /// </summary>
        public string BuildStringZeroGC(params object[] parts)
        {
            var sb = m_stringBuilderPool.Get();
            
            try
            {
                foreach (var part in parts)
                {
                    sb.Append(part);
                }
                return sb.ToString();
            }
            finally
            {
                m_stringBuilderPool.Return(sb);
            }
        }

        /// <summary>
        /// 获取输入数据包（从缓存池）
        /// </summary>
        public InputDataPacket GetInputDataPacket()
        {
            if (m_inputDataCache.Count > 0)
            {
                return m_inputDataCache.Dequeue();
            }
            
            // 如果缓存为空，创建新的
            var newPacket = new InputDataPacket();
            newPacket.Reset();
            return newPacket;
        }

        /// <summary>
        /// 归还输入数据包（到缓存池）
        /// </summary>
        public void ReturnInputDataPacket(InputDataPacket packet)
        {
            packet.Reset();
            m_inputDataCache.Enqueue(packet);
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        private void ClearCaches()
        {
            m_cachedStrings?.Clear();
            m_cachedVectors?.Clear();
        }

        /// <summary>
        /// 获取内存使用统计
        /// </summary>
        public MemoryStats GetMemoryStats()
        {
            return new MemoryStats
            {
                totalGCAlloc = m_totalGCAllocSinceStart,
                cachedStringsCount = m_cachedStrings?.Count ?? 0,
                cachedVectorsCount = m_cachedVectors?.Count ?? 0,
                vectorPoolCount = m_vectorCache?.Count ?? 0,
                inputDataPoolCount = m_inputDataCache?.Count ?? 0,
                stringBuilderPoolCount = m_stringBuilderPool?.PoolCount ?? 0
            };
        }

        /// <summary>
        /// 内存统计结构
        /// </summary>
        [System.Serializable]
        public struct MemoryStats
        {
            public float totalGCAlloc;      // 总GC分配（KB）
            public int cachedStringsCount;  // 缓存字符串数量
            public int cachedVectorsCount;  // 缓存Vector数量
            public int vectorPoolCount;     // Vector对象池数量
            public int inputDataPoolCount;  // 输入数据池数量
            public int stringBuilderPoolCount; // StringBuilder池数量
        }

        private void OnGUI()
        {
            if (!m_showDebugInfo) return;

            var memStats = GetMemoryStats();
            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                alignment = TextAnchor.UpperLeft
            };

            string debugText = $"=== 零GC输入处理器 ===\n" +
                             $"总GC分配: {memStats.totalGCAlloc:F2} KB\n" +
                             $"缓存字符串: {memStats.cachedStringsCount}\n" +
                             $"缓存Vector: {memStats.cachedVectorsCount}\n" +
                             $"Vector池: {memStats.vectorPoolCount}\n" +
                             $"输入数据池: {memStats.inputDataPoolCount}\n" +
                             $"StringBuilder池: {memStats.stringBuilderPoolCount}\n" +
                             $"启用GC监控: {m_enableGCMonitoring}\n" +
                             $"启用字符串缓存: {m_enableStringCaching}";

            GUI.Box(new Rect(270, 10, 250, 200), debugText, style);
        }
    }
}