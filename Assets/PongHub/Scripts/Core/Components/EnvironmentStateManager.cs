using UnityEngine;
using System.Collections.Generic;

namespace PongHub.Core.Components
{
    /// <summary>
    /// 环境状态管理器
    /// 负责在游戏模式切换时保持环境状态的一致性
    /// </summary>
    public class EnvironmentStateManager : MonoBehaviour, IGameModeComponent
    {
        [Header("环境状态设置")]
        [SerializeField]
        [Tooltip("Enable State Persistence / 启用状态持久化 - Preserve environment state across mode changes")]
        private bool m_enableStatePersistence = true;

        [SerializeField]
        [Tooltip("Environment Objects / 环境对象 - Objects that should maintain state across mode changes")]
        private GameObject[] m_environmentObjects;

        [SerializeField]
        [Tooltip("Light Settings / 灯光设置 - Lights that should maintain settings across mode changes")]
        private Light[] m_environmentLights;

        [SerializeField]
        [Tooltip("Audio Sources / 音频源 - Audio sources that should maintain settings across mode changes")]
        private AudioSource[] m_environmentAudio;

        [Header("调试设置")]
        [SerializeField]
        [Tooltip("Debug Mode / 调试模式 - Enable debug logging for environment state operations")]
        private bool m_debugMode = false;

        // 保存的状态
        private Dictionary<int, TransformState> m_savedTransforms = new Dictionary<int, TransformState>();
        private Dictionary<int, LightState> m_savedLights = new Dictionary<int, LightState>();
        private Dictionary<int, AudioState> m_savedAudio = new Dictionary<int, AudioState>();

        // 状态结构体
        private struct TransformState
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 localScale;
            public bool activeState;
        }

        private struct LightState
        {
            public Color color;
            public float intensity;
            public bool enabled;
        }

        private struct AudioState
        {
            public float volume;
            public bool isPlaying;
            public bool enabled;
        }

        #region Unity 生命周期

        private void Start()
        {
            // 注册到GameModeManager
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.RegisterComponent(this);
            }
            else
            {
                Debug.LogWarning("[EnvironmentStateManager] GameModeManager实例不存在，延迟注册");
                Invoke(nameof(RegisterWithDelay), 0.5f);
            }

            // 初始化状态
            if (m_enableStatePersistence)
            {
                SaveAllStates();
            }
        }

        private void OnDestroy()
        {
            // 从GameModeManager注销
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.UnregisterComponent(this);
            }
        }

        #endregion

        #region IGameModeComponent 实现

        public void OnGameModeChanged(GameMode newMode, GameMode previousMode)
        {
            if (!m_enableStatePersistence)
            {
                return;
            }

            if (m_debugMode)
            {
                Debug.Log($"[EnvironmentStateManager] 模式切换: {previousMode} -> {newMode}");
            }

            // 在模式切换时保存和恢复状态
            if (previousMode != GameMode.Menu && newMode != GameMode.Menu)
            {
                // 先保存当前状态
                SaveAllStates();

                // 短暂延迟后恢复状态，确保其他组件有时间进行必要的更改
                Invoke(nameof(RestoreAllStates), 0.1f);
            }
        }

        public bool IsActiveInMode(GameMode mode)
        {
            // 环境状态管理器在所有模式下都处于活动状态
            return true;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 延迟注册到GameModeManager
        /// </summary>
        private void RegisterWithDelay()
        {
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.RegisterComponent(this);
            }
            else
            {
                Debug.LogError("[EnvironmentStateManager] 无法找到GameModeManager实例");
            }
        }

        /// <summary>
        /// 保存所有状态
        /// </summary>
        private void SaveAllStates()
        {
            if (m_debugMode)
            {
                Debug.Log("[EnvironmentStateManager] 保存所有环境状态");
            }

            // 保存环境对象变换
            SaveTransformStates();

            // 保存灯光状态
            SaveLightStates();

            // 保存音频状态
            SaveAudioStates();
        }

        /// <summary>
        /// 恢复所有状态
        /// </summary>
        private void RestoreAllStates()
        {
            if (m_debugMode)
            {
                Debug.Log("[EnvironmentStateManager] 恢复所有环境状态");
            }

            // 恢复环境对象变换
            RestoreTransformStates();

            // 恢复灯光状态
            RestoreLightStates();

            // 恢复音频状态
            RestoreAudioStates();
        }

        /// <summary>
        /// 保存变换状态
        /// </summary>
        private void SaveTransformStates()
        {
            m_savedTransforms.Clear();

            foreach (var obj in m_environmentObjects)
            {
                if (obj != null)
                {
                    int id = obj.GetInstanceID();

                    m_savedTransforms[id] = new TransformState
                    {
                        position = obj.transform.position,
                        rotation = obj.transform.rotation,
                        localScale = obj.transform.localScale,
                        activeState = obj.activeSelf
                    };
                }
            }
        }

        /// <summary>
        /// 恢复变换状态
        /// </summary>
        private void RestoreTransformStates()
        {
            foreach (var obj in m_environmentObjects)
            {
                if (obj != null)
                {
                    int id = obj.GetInstanceID();

                    if (m_savedTransforms.TryGetValue(id, out TransformState state))
                    {
                        obj.transform.position = state.position;
                        obj.transform.rotation = state.rotation;
                        obj.transform.localScale = state.localScale;
                        obj.SetActive(state.activeState);
                    }
                }
            }
        }

        /// <summary>
        /// 保存灯光状态
        /// </summary>
        private void SaveLightStates()
        {
            m_savedLights.Clear();

            foreach (var light in m_environmentLights)
            {
                if (light != null)
                {
                    int id = light.GetInstanceID();

                    m_savedLights[id] = new LightState
                    {
                        color = light.color,
                        intensity = light.intensity,
                        enabled = light.enabled
                    };
                }
            }
        }

        /// <summary>
        /// 恢复灯光状态
        /// </summary>
        private void RestoreLightStates()
        {
            foreach (var light in m_environmentLights)
            {
                if (light != null)
                {
                    int id = light.GetInstanceID();

                    if (m_savedLights.TryGetValue(id, out LightState state))
                    {
                        light.color = state.color;
                        light.intensity = state.intensity;
                        light.enabled = state.enabled;
                    }
                }
            }
        }

        /// <summary>
        /// 保存音频状态
        /// </summary>
        private void SaveAudioStates()
        {
            m_savedAudio.Clear();

            foreach (var audio in m_environmentAudio)
            {
                if (audio != null)
                {
                    int id = audio.GetInstanceID();

                    m_savedAudio[id] = new AudioState
                    {
                        volume = audio.volume,
                        isPlaying = audio.isPlaying,
                        enabled = audio.enabled
                    };
                }
            }
        }

        /// <summary>
        /// 恢复音频状态
        /// </summary>
        private void RestoreAudioStates()
        {
            foreach (var audio in m_environmentAudio)
            {
                if (audio != null)
                {
                    int id = audio.GetInstanceID();

                    if (m_savedAudio.TryGetValue(id, out AudioState state))
                    {
                        audio.volume = state.volume;
                        audio.enabled = state.enabled;

                        // 恢复播放状态
                        if (state.isPlaying && !audio.isPlaying)
                        {
                            audio.Play();
                        }
                        else if (!state.isPlaying && audio.isPlaying)
                        {
                            audio.Stop();
                        }
                    }
                }
            }
        }

        #endregion

        #region 公共API

        /// <summary>
        /// 手动保存当前环境状态
        /// </summary>
        public void SaveCurrentState()
        {
            if (!m_enableStatePersistence)
            {
                return;
            }

            SaveAllStates();
        }

        /// <summary>
        /// 手动恢复环境状态
        /// </summary>
        public void RestoreState()
        {
            if (!m_enableStatePersistence)
            {
                return;
            }

            RestoreAllStates();
        }

        /// <summary>
        /// 设置状态持久化启用状态
        /// </summary>
        public void SetStatePersistenceEnabled(bool enabled)
        {
            m_enableStatePersistence = enabled;

            if (m_debugMode)
            {
                Debug.Log($"[EnvironmentStateManager] 设置状态持久化: {enabled}");
            }

            if (m_enableStatePersistence)
            {
                SaveAllStates();
            }
        }

        /// <summary>
        /// 添加环境对象
        /// </summary>
        public void AddEnvironmentObject(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            // 检查是否已存在
            foreach (var existingObj in m_environmentObjects)
            {
                if (existingObj == obj)
                {
                    return;
                }
            }

            // 添加到数组
            System.Array.Resize(ref m_environmentObjects, m_environmentObjects.Length + 1);
            m_environmentObjects[m_environmentObjects.Length - 1] = obj;

            // 如果启用了状态持久化，保存新对象的状态
            if (m_enableStatePersistence)
            {
                int id = obj.GetInstanceID();

                m_savedTransforms[id] = new TransformState
                {
                    position = obj.transform.position,
                    rotation = obj.transform.rotation,
                    localScale = obj.transform.localScale,
                    activeState = obj.activeSelf
                };
            }
        }

        #endregion
    }
}