using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace PongHub.Core.Audio
{
    /// <summary>
    /// AudioSource对象池
    /// 管理AudioSource的创建、回收和重用，优化性能
    /// </summary>
    public class AudioSourcePool : MonoBehaviour
    {
        [Header("对象池设置")]
        [SerializeField]
        [Tooltip("Initial Pool Size / 初始池大小 - Initial number of AudioSource objects in the pool")]
        private int m_initialPoolSize = 20;

        [SerializeField]
        [Tooltip("Max Pool Size / 最大池大小 - Maximum number of AudioSource objects in the pool")]
        private int m_maxPoolSize = 50;

        [SerializeField]
        [Tooltip("Allow Pool Expansion / 允许池扩展 - Whether to allow pool expansion beyond initial size")]
        private bool m_allowPoolExpansion = true;

        // 对象池
        private Queue<AudioSource> m_availableAudioSources = new();
        private List<AudioSource> m_allAudioSources = new();
        private Dictionary<AudioSource, float> m_activeAudioSources = new();

        // 配置引用
        private AudioConfiguration m_configuration;

        /// <summary>
        /// 当前池中可用的AudioSource数量
        /// </summary>
        public int AvailableCount => m_availableAudioSources.Count;

        /// <summary>
        /// 当前正在使用的AudioSource数量
        /// </summary>
        public int ActiveCount => m_activeAudioSources.Count;

        /// <summary>
        /// 池中总的AudioSource数量
        /// </summary>
        public int TotalCount => m_allAudioSources.Count;

        /// <summary>
        /// 初始化对象池
        /// </summary>
        public void Initialize(AudioConfiguration configuration)
        {
            m_configuration = configuration;

            if (m_configuration != null)
            {
                m_initialPoolSize = m_configuration.AudioSourcePoolSize;
                m_maxPoolSize = m_configuration.MaxConcurrentAudio;
            }

            // 预创建AudioSource
            PrewarmPool(m_initialPoolSize);

            // 启动清理协程
            StartCoroutine(CleanupCoroutine());
        }

        /// <summary>
        /// 预热对象池
        /// </summary>
        public void PrewarmPool(int count)
        {
            for (int i = 0; i < count && m_allAudioSources.Count < m_maxPoolSize; i++)
            {
                CreateNewAudioSource();
            }
        }

        /// <summary>
        /// 从池中获取AudioSource
        /// </summary>
        public AudioSource GetPooledSource(AudioCategory category)
        {
            AudioSource audioSource = null;

            // 尝试从池中获取
            if (m_availableAudioSources.Count > 0)
            {
                audioSource = m_availableAudioSources.Dequeue();
            }
            else if (m_allowPoolExpansion && m_allAudioSources.Count < m_maxPoolSize)
            {
                // 池为空但允许扩展
                audioSource = CreateNewAudioSource();
            }
            else
            {
                Debug.LogWarning("AudioSourcePool: No available AudioSource and pool expansion not allowed!");
                return null;
            }

            if (audioSource != null)
            {
                // 激活并配置AudioSource
                SetupAudioSource(audioSource, category);

                // 记录活跃状态
                m_activeAudioSources[audioSource] = Time.time;
            }

            return audioSource;
        }

        /// <summary>
        /// 将AudioSource归还到池中
        /// </summary>
        public void ReturnToPool(AudioSource audioSource)
        {
            if (audioSource == null) return;

            // 从活跃列表中移除
            m_activeAudioSources.Remove(audioSource);

            // 重置AudioSource状态
            ResetAudioSource(audioSource);

            // 返回到池中
            if (!m_availableAudioSources.Contains(audioSource))
            {
                m_availableAudioSources.Enqueue(audioSource);
            }
        }

        /// <summary>
        /// 创建新的AudioSource
        /// </summary>
        private AudioSource CreateNewAudioSource()
        {
            var audioSourceGO = new GameObject($"PooledAudioSource_{m_allAudioSources.Count}");
            audioSourceGO.transform.SetParent(transform);

            var audioSource = audioSourceGO.AddComponent<AudioSource>();

            // 基础配置
            ConfigureNewAudioSource(audioSource);

            // 添加到列表
            m_allAudioSources.Add(audioSource);
            m_availableAudioSources.Enqueue(audioSource);

            return audioSource;
        }

        /// <summary>
        /// 配置新创建的AudioSource
        /// </summary>
        private void ConfigureNewAudioSource(AudioSource audioSource)
        {
            if (m_configuration != null)
            {
                audioSource.spatialBlend = 1f; // 默认3D音效
                audioSource.rolloffMode = m_configuration.DefaultRolloffMode;
                audioSource.minDistance = m_configuration.DefaultMinDistance;
                audioSource.maxDistance = m_configuration.DefaultMaxDistance;
                audioSource.dopplerLevel = m_configuration.DefaultDopplerLevel;
                audioSource.spread = m_configuration.DefaultSpread;
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        /// <summary>
        /// 设置AudioSource用于特定分类
        /// </summary>
        private void SetupAudioSource(AudioSource audioSource, AudioCategory category)
        {
            // 激活GameObject
            audioSource.gameObject.SetActive(true);

            // 设置混音器组
            if (m_configuration != null)
            {
                audioSource.outputAudioMixerGroup = m_configuration.GetMixerGroup(category);
            }

            // 重置位置
            audioSource.transform.SetParent(transform);
            audioSource.transform.localPosition = Vector3.zero;
            audioSource.transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// 重置AudioSource状态
        /// </summary>
        private void ResetAudioSource(AudioSource audioSource)
        {
            // 停止播放
            audioSource.Stop();

            // 重置属性
            audioSource.clip = null;
            audioSource.volume = 1f;
            audioSource.pitch = 1f;
            audioSource.loop = false;
            audioSource.time = 0f;

            // 重置位置
            audioSource.transform.SetParent(transform);
            audioSource.transform.localPosition = Vector3.zero;
            audioSource.transform.localRotation = Quaternion.identity;

            // 重置3D设置
            if (m_configuration != null)
            {
                audioSource.spatialBlend = 1f;
                audioSource.rolloffMode = m_configuration.DefaultRolloffMode;
                audioSource.minDistance = m_configuration.DefaultMinDistance;
                audioSource.maxDistance = m_configuration.DefaultMaxDistance;
            }

            // 停用GameObject以节省资源
            audioSource.gameObject.SetActive(false);
        }

        /// <summary>
        /// 清理协程 - 自动回收已停止播放的AudioSource
        /// </summary>
        private IEnumerator CleanupCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f); // 每秒检查一次

                // 检查活跃的AudioSource
                var sourcesToReturn = new List<AudioSource>();

                foreach (var kvp in m_activeAudioSources)
                {
                    var audioSource = kvp.Key;
                    var startTime = kvp.Value;

                    // 检查AudioSource是否仍在播放
                    if (audioSource == null || !audioSource.isPlaying)
                    {
                        sourcesToReturn.Add(audioSource);
                    }
                    // 检查是否超时（防止泄漏）
                    else if (Time.time - startTime > 600f) // 10分钟超时
                    {
                        Debug.LogWarning($"AudioSourcePool: AudioSource timeout, force returning to pool");
                        sourcesToReturn.Add(audioSource);
                    }
                }

                // 归还停止播放的AudioSource
                foreach (var audioSource in sourcesToReturn)
                {
                    if (audioSource != null)
                    {
                        ReturnToPool(audioSource);
                    }
                }
            }
        }

        /// <summary>
        /// 清理所有AudioSource
        /// </summary>
        public void CleanupAll()
        {
            // 停止所有正在播放的音频
            foreach (var audioSource in m_activeAudioSources.Keys)
            {
                if (audioSource != null)
                {
                    audioSource.Stop();
                }
            }

            // 清空所有列表
            m_activeAudioSources.Clear();
            m_availableAudioSources.Clear();

            // 销毁所有AudioSource GameObject
            foreach (var audioSource in m_allAudioSources)
            {
                if (audioSource != null && audioSource.gameObject != null)
                {
                    DestroyImmediate(audioSource.gameObject);
                }
            }

            m_allAudioSources.Clear();
        }

        /// <summary>
        /// 获取池统计信息
        /// </summary>
        public void GetPoolStats(out int available, out int active, out int total)
        {
            available = AvailableCount;
            active = ActiveCount;
            total = TotalCount;
        }

        /// <summary>
        /// 强制回收所有AudioSource
        /// </summary>
        public void ForceReturnAll()
        {
            var activeSourcesCopy = new List<AudioSource>(m_activeAudioSources.Keys);

            foreach (var audioSource in activeSourcesCopy)
            {
                ReturnToPool(audioSource);
            }
        }

        private void OnDestroy()
        {
            CleanupAll();
        }

        #region 调试方法

        /// <summary>
        /// 获取调试信息
        /// </summary>
        public string GetDebugInfo()
        {
            return $"AudioSourcePool - Available: {AvailableCount}, Active: {ActiveCount}, Total: {TotalCount}";
        }

        /// <summary>
        /// 在Inspector中显示池状态
        /// </summary>
        private void OnValidate()
        {
            m_initialPoolSize = Mathf.Max(1, m_initialPoolSize);
            m_maxPoolSize = Mathf.Max(m_initialPoolSize, m_maxPoolSize);
        }

        #endregion
    }
}