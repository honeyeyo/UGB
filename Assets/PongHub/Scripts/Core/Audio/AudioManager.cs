using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace PongHub.Core.Audio
{
    /// <summary>
    /// PongHub游戏音频管理器
    /// 专注于游戏特定的音频功能和乒乓球相关音效
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager s_instance;
        public static AudioManager Instance => s_instance;

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

        [Header("乒乓球音效")]
        [SerializeField]
        [Tooltip("Ball Hit Paddle Sounds / 球拍击球音效 - Audio clips for ball hitting paddle")]
        private AudioClip[] m_ballHitPaddleSounds;

        [SerializeField]
        [Tooltip("Ball Hit Table Sounds / 球桌击球音效 - Audio clips for ball hitting table")]
        private AudioClip[] m_ballHitTableSounds;

        [SerializeField]
        [Tooltip("Ball Hit Net Sounds / 球网击球音效 - Audio clips for ball hitting net")]
        private AudioClip[] m_ballHitNetSounds;

        [SerializeField]
        [Tooltip("Ball Bounce Sounds / 球弹跳音效 - Audio clips for ball bouncing")]
        private AudioClip[] m_ballBounceSounds;

        [SerializeField]
        [Tooltip("Ball Out Of Bounds / 球出界音效 - Audio clip for ball going out of bounds")]
        private AudioClip m_ballOutOfBounds;

        [Header("比赛音效")]
        [SerializeField]
        [Tooltip("Match Start / 比赛开始音效 - Audio clip for match start")]
        private AudioClip m_matchStart;

        [SerializeField]
        [Tooltip("Match End / 比赛结束音效 - Audio clip for match end")]
        private AudioClip m_matchEnd;

        [SerializeField]
        [Tooltip("Point Scored / 得分音效 - Audio clip for scoring a point")]
        private AudioClip m_pointScored;

        [SerializeField]
        [Tooltip("Game Won / 游戏胜利音效 - Audio clip for winning a game")]
        private AudioClip m_gameWon;

        [SerializeField]
        [Tooltip("Set Won / 局胜利音效 - Audio clip for winning a set")]
        private AudioClip m_setWon;

        [SerializeField]
        [Tooltip("Match Won / 比赛胜利音效 - Audio clip for winning a match")]
        private AudioClip m_matchWon;

        [Header("观众音效")]
        [SerializeField]
        [Tooltip("Crowd Cheer / 观众欢呼音效 - Audio clip for crowd cheering")]
        private AudioClip m_crowdCheer;

        [SerializeField]
        [Tooltip("Crowd Applause / 观众鼓掌音效 - Audio clip for crowd applause")]
        private AudioClip m_crowdApplause;

        [SerializeField]
        [Tooltip("Crowd Boo / 观众嘘声音效 - Audio clip for crowd booing")]
        private AudioClip m_crowdBoo;

        [SerializeField]
        [Tooltip("Crowd Ambient / 观众环境音效 - Audio clip for crowd ambient sound")]
        private AudioClip m_crowdAmbient;

        [Header("环境音效")]
        [SerializeField]
        [Tooltip("Lobby Ambient / 大厅环境音效 - Audio clip for lobby ambient sound")]
        private AudioClip m_lobbyAmbient;

        [SerializeField]
        [Tooltip("Arena Ambient / 竞技场环境音效 - Audio clip for arena ambient sound")]
        private AudioClip m_arenaAmbient;

        [SerializeField]
        [Tooltip("Footstep Sounds / 脚步声音效 - Audio clips for footstep sounds")]
        private AudioClip[] m_footstepSounds;

        [SerializeField]
        [Tooltip("Voice Chat Notifications / 语音聊天通知音效 - Audio clips for voice chat notifications")]
        private AudioClip[] m_voiceChatNotifications;

        [Header("音频设置")]
        [SerializeField]
        [Tooltip("Ball Sound Variation / 球音效变化 - Variation in ball sound pitch and volume")]
        private float m_ballSoundVariation = 0.2f;
        // [SerializeField] private float m_maxBallSoundDistance = 50f; // 暂时没用到,先注释了

        [SerializeField]
        [Tooltip("Enable Dynamic Volume / 启用动态音量 - Whether to enable dynamic volume adjustment")]
        private bool m_enableDynamicVolume = true;

        [SerializeField]
        [Tooltip("Enable Crowd Reactions / 启用观众反应 - Whether to enable crowd reaction system")]
        private bool m_enableCrowdReactions = true;

        // 音频状态管理
        private AudioHandle m_currentCrowdAmbient;
        private AudioHandle m_currentEnvironmentAmbient;
        private Dictionary<string, AudioHandle> m_loopingAudio = new();

        // 球音效状态
        private float m_lastBallHitTime = 0f;
        private const float BALL_HIT_COOLDOWN = 0.05f; // 防止快速连击产生太多音效

        // 观众反应系统
        private CrowdReactionState m_crowdState = CrowdReactionState.Neutral;
        private float m_crowdExcitementLevel = 0.5f;
        private Coroutine m_crowdReactionCoroutine;

        /// <summary>
        /// 音频服务引用
        /// </summary>
        private AudioService AudioService => AudioService.Instance;

        #region 音量控制方法

        /// <summary>
        /// 设置主音量
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            if (AudioService != null)
            {
                AudioService.SetCategoryVolume(AudioCategory.Master, volume);
            }
        }

        /// <summary>
        /// 设置音乐音量
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            if (AudioService != null)
            {
                AudioService.SetCategoryVolume(AudioCategory.Music, volume);
            }
        }

        /// <summary>
        /// 设置音效音量
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            if (AudioService != null)
            {
                AudioService.SetCategoryVolume(AudioCategory.SFX, volume);
            }
        }

        /// <summary>
        /// 设置音效音量（兼容旧的方法名）
        /// </summary>
        public void SetSoundVolume(float volume)
        {
            SetSFXVolume(volume);
        }

        /// <summary>
        /// 初始化异步（兼容旧接口）
        /// </summary>
        public async System.Threading.Tasks.Task InitializeAsync()
        {
            // 等待AudioService初始化
            while (AudioService == null || !AudioService.IsInitialized)
            {
                await System.Threading.Tasks.Task.Delay(100);
            }
        }

        /// <summary>
        /// 清理资源（兼容旧接口）
        /// </summary>
        public void Cleanup()
        {
            // 停止所有循环音频
            StopAllLoopingAudio();
        }

        #endregion

        #region 游戏专用音效方法

        /// <summary>
        /// 播放球拍击球音效
        /// </summary>
        public void PlayPaddleHit(Vector3 position, float volume = 1f)
        {
            PlayBallHitPaddle(position, volume);
        }

        /// <summary>
        /// 播放桌子击球音效
        /// </summary>
        public void PlayTableHit(Vector3 position, float volume = 1f)
        {
            PlayBallHitTable(position, volume);
        }

        /// <summary>
        /// 播放网击球音效
        /// </summary>
        public void PlayNetHit(Vector3 position, float volume = 1f)
        {
            PlayBallHitNet(position, volume);
        }

        /// <summary>
        /// 播放边缘击球音效
        /// </summary>
        public void PlayEdgeHit(Vector3 position, float volume = 1f)
        {
            // 使用桌子击球音效的变体
            PlayBallHitTable(position, volume * 0.8f);
        }

        /// <summary>
        /// 播放得分音效
        /// </summary>
        public void PlayScore()
        {
            PlayPointScored(true);
        }

        #endregion



        /// <summary>
        /// 当前观众反应状态
        /// </summary>
        public CrowdReactionState CrowdState => m_crowdState;

        /// <summary>
        /// 观众兴奋度等级 (0-1)
        /// </summary>
        public float CrowdExcitementLevel
        {
            get => m_crowdExcitementLevel;
            set => m_crowdExcitementLevel = Mathf.Clamp01(value);
        }

        private void Start()
        {
            // 等待AudioService初始化
            StartCoroutine(WaitForAudioServiceAndInitialize());
        }

        private void OnDestroy()
        {
            StopAllLoopingAudio();
        }

        #region 初始化

        /// <summary>
        /// 等待AudioService初始化
        /// </summary>
        private IEnumerator WaitForAudioServiceAndInitialize()
        {
            while (AudioService == null || !AudioService.IsInitialized)
            {
                yield return new WaitForSeconds(0.1f);
            }

            Debug.Log("AudioManager: AudioService ready, initializing game audio...");

            // 验证音频资源
            ValidateAudioClips();

            Debug.Log("AudioManager: Game audio initialization complete");
        }

        /// <summary>
        /// 验证音频资源
        /// </summary>
        private void ValidateAudioClips()
        {
            if (m_ballHitPaddleSounds == null || m_ballHitPaddleSounds.Length == 0)
                Debug.LogWarning("AudioManager: Ball hit paddle sounds not assigned!");

            if (m_ballHitTableSounds == null || m_ballHitTableSounds.Length == 0)
                Debug.LogWarning("AudioManager: Ball hit table sounds not assigned!");

            if (m_crowdAmbient == null)
                Debug.LogWarning("AudioManager: Crowd ambient sound not assigned!");
        }

        #endregion

        #region 乒乓球音效

        /// <summary>
        /// 播放球拍击球音效
        /// </summary>
        public void PlayBallHitPaddle(Vector3 position, float intensity = 1f)
        {
            if (!CanPlayBallSound()) return;

            var clip = GetRandomClip(m_ballHitPaddleSounds);
            if (clip == null) return;

            var handle = AudioService.PlayOneShot(clip, position, AudioCategory.SFX, intensity);
            ApplyBallSoundEffects(handle, intensity);

            UpdateLastBallHitTime();
        }

        /// <summary>
        /// 播放球桌碰撞音效
        /// </summary>
        public void PlayBallHitTable(Vector3 position, float intensity = 1f)
        {
            if (!CanPlayBallSound()) return;

            var clip = GetRandomClip(m_ballHitTableSounds);
            if (clip == null) return;

            var handle = AudioService.PlayOneShot(clip, position, AudioCategory.SFX, intensity * 0.8f);
            ApplyBallSoundEffects(handle, intensity);

            UpdateLastBallHitTime();
        }

        /// <summary>
        /// 播放球网碰撞音效
        /// </summary>
        public void PlayBallHitNet(Vector3 position, float intensity = 1f)
        {
            if (!CanPlayBallSound()) return;

            var clip = GetRandomClip(m_ballHitNetSounds);
            if (clip == null) return;

            var handle = AudioService.PlayOneShot(clip, position, AudioCategory.SFX, intensity * 0.6f);
            ApplyBallSoundEffects(handle, intensity);

            UpdateLastBallHitTime();
        }

        /// <summary>
        /// 播放球反弹音效
        /// </summary>
        public void PlayBallBounce(Vector3 position, float intensity = 1f)
        {
            if (!CanPlayBallSound()) return;

            var clip = GetRandomClip(m_ballBounceSounds);
            if (clip == null) return;

            var handle = AudioService.PlayOneShot(clip, position, AudioCategory.SFX, intensity * 0.4f);
            ApplyBallSoundEffects(handle, intensity);

            UpdateLastBallHitTime();
        }

        /// <summary>
        /// 播放球出界音效
        /// </summary>
        public void PlayBallOutOfBounds(Vector3 position)
        {
            if (m_ballOutOfBounds == null) return;

            AudioService.PlayOneShot(m_ballOutOfBounds, position, AudioCategory.SFX, 0.8f);
        }

        /// <summary>
        /// 检查是否可以播放球音效（防止过于频繁）
        /// </summary>
        private bool CanPlayBallSound()
        {
            return Time.time - m_lastBallHitTime > BALL_HIT_COOLDOWN;
        }

        /// <summary>
        /// 更新最后球击打时间
        /// </summary>
        private void UpdateLastBallHitTime()
        {
            m_lastBallHitTime = Time.time;
        }

        /// <summary>
        /// 应用球音效的特殊效果
        /// </summary>
        private void ApplyBallSoundEffects(AudioHandle handle, float intensity)
        {
            if (handle == null || !handle.IsValid) return;

            // 根据强度调整音调
            float pitchVariation = Random.Range(-m_ballSoundVariation, m_ballSoundVariation);
            float pitch = 1f + (intensity - 1f) * 0.3f + pitchVariation;
            handle.SetPitch(Mathf.Clamp(pitch, 0.5f, 2f));
        }

        #endregion

        #region 比赛音效

        /// <summary>
        /// 播放比赛开始音效
        /// </summary>
        public void PlayMatchStart()
        {
            if (m_matchStart == null) return;

            AudioService.PlayOneShot(m_matchStart, AudioCategory.SFX, 1f);
            TriggerCrowdReaction(CrowdReactionType.Excited, 0.8f);
        }

        /// <summary>
        /// 播放比赛结束音效
        /// </summary>
        public void PlayMatchEnd()
        {
            if (m_matchEnd == null) return;

            AudioService.PlayOneShot(m_matchEnd, AudioCategory.SFX, 1f);
        }

        /// <summary>
        /// 播放得分音效
        /// </summary>
        public void PlayPointScored(bool isPlayerPoint = true)
        {
            if (m_pointScored == null) return;

            AudioService.PlayOneShot(m_pointScored, AudioCategory.SFX, 0.8f);

            if (isPlayerPoint)
            {
                TriggerCrowdReaction(CrowdReactionType.Cheer, 0.6f);
            }
        }

        /// <summary>
        /// 播放游戏获胜音效
        /// </summary>
        public void PlayGameWon(bool isPlayerWin = true)
        {
            if (m_gameWon == null) return;

            AudioService.PlayOneShot(m_gameWon, AudioCategory.SFX, 0.9f);

            if (isPlayerWin)
            {
                TriggerCrowdReaction(CrowdReactionType.Cheer, 0.8f);
            }
            else
            {
                TriggerCrowdReaction(CrowdReactionType.Disappointed, 0.5f);
            }
        }

        /// <summary>
        /// 播放局获胜音效
        /// </summary>
        public void PlaySetWon(bool isPlayerWin = true)
        {
            if (m_setWon == null) return;

            AudioService.PlayOneShot(m_setWon, AudioCategory.SFX, 1f);

            if (isPlayerWin)
            {
                TriggerCrowdReaction(CrowdReactionType.Applause, 0.9f);
            }
        }

        /// <summary>
        /// 播放比赛获胜音效
        /// </summary>
        public void PlayMatchWon(bool isPlayerWin = true)
        {
            if (m_matchWon == null) return;

            AudioService.PlayOneShot(m_matchWon, AudioCategory.SFX, 1f);

            if (isPlayerWin)
            {
                TriggerCrowdReaction(CrowdReactionType.Celebration, 1f);
            }
            else
            {
                TriggerCrowdReaction(CrowdReactionType.Boo, 0.7f);
            }
        }

        #endregion

        #region 观众音效

        /// <summary>
        /// 开始播放观众环境音
        /// </summary>
        public void StartCrowdAmbient()
        {
            if (m_crowdAmbient == null || !m_enableCrowdReactions) return;

            StopLoopingAudio("crowd_ambient");

            m_currentCrowdAmbient = AudioService.PlayLooped(m_crowdAmbient, AudioCategory.Crowd, 0.5f);
            if (m_currentCrowdAmbient != null)
            {
                m_loopingAudio["crowd_ambient"] = m_currentCrowdAmbient;
            }
        }

        /// <summary>
        /// 停止观众环境音
        /// </summary>
        public void StopCrowdAmbient()
        {
            StopLoopingAudio("crowd_ambient");
            m_currentCrowdAmbient = null;
        }

        /// <summary>
        /// 触发观众反应
        /// </summary>
        public void TriggerCrowdReaction(CrowdReactionType reactionType, float intensity = 1f)
        {
            if (!m_enableCrowdReactions) return;

            AudioClip clip = reactionType switch
            {
                CrowdReactionType.Cheer => m_crowdCheer,
                CrowdReactionType.Applause => m_crowdApplause,
                CrowdReactionType.Boo => m_crowdBoo,
                _ => null
            };

            if (clip == null) return;

            float volume = intensity * m_crowdExcitementLevel;
            AudioService.PlayOneShot(clip, AudioCategory.Crowd, volume);

            // 启动观众反应状态机
            if (m_crowdReactionCoroutine != null)
            {
                StopCoroutine(m_crowdReactionCoroutine);
            }
            m_crowdReactionCoroutine = StartCoroutine(CrowdReactionCoroutine(reactionType, intensity));
        }

        /// <summary>
        /// 观众反应协程
        /// </summary>
        private IEnumerator CrowdReactionCoroutine(CrowdReactionType reactionType, float intensity)
        {
            var oldState = m_crowdState;

            // 切换到新反应状态
            m_crowdState = reactionType switch
            {
                CrowdReactionType.Cheer or CrowdReactionType.Celebration => CrowdReactionState.Excited,
                CrowdReactionType.Applause => CrowdReactionState.Pleased,
                CrowdReactionType.Boo or CrowdReactionType.Disappointed => CrowdReactionState.Disappointed,
                _ => CrowdReactionState.Neutral
            };

            // 调整兴奋度
            float targetExcitement = reactionType switch
            {
                CrowdReactionType.Celebration => 1f,
                CrowdReactionType.Cheer => 0.8f,
                CrowdReactionType.Applause => 0.7f,
                CrowdReactionType.Excited => 0.6f,
                CrowdReactionType.Disappointed => 0.3f,
                CrowdReactionType.Boo => 0.2f,
                _ => 0.5f
            };

            // 平滑调整兴奋度
            float duration = 2f;
            float elapsed = 0f;
            float startExcitement = m_crowdExcitementLevel;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                m_crowdExcitementLevel = Mathf.Lerp(startExcitement, targetExcitement, t);
                yield return null;
            }

            m_crowdExcitementLevel = targetExcitement;

            // 等待一段时间后回归中性状态
            yield return new WaitForSeconds(3f);

            // 平滑回归中性状态
            elapsed = 0f;
            startExcitement = m_crowdExcitementLevel;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                m_crowdExcitementLevel = Mathf.Lerp(startExcitement, 0.5f, t);
                yield return null;
            }

            m_crowdState = CrowdReactionState.Neutral;
            m_crowdExcitementLevel = 0.5f;

            m_crowdReactionCoroutine = null;
        }

        #endregion

        #region 环境音效

        /// <summary>
        /// 开始播放大厅环境音
        /// </summary>
        public void StartLobbyAmbient()
        {
            if (m_lobbyAmbient == null) return;

            StopLoopingAudio("environment_ambient");

            m_currentEnvironmentAmbient = AudioService.PlayLooped(m_lobbyAmbient, AudioCategory.Ambient, 0.3f);
            if (m_currentEnvironmentAmbient != null)
            {
                m_loopingAudio["environment_ambient"] = m_currentEnvironmentAmbient;
            }
        }

        /// <summary>
        /// 开始播放竞技场环境音
        /// </summary>
        public void StartArenaAmbient()
        {
            if (m_arenaAmbient == null) return;

            StopLoopingAudio("environment_ambient");

            m_currentEnvironmentAmbient = AudioService.PlayLooped(m_arenaAmbient, AudioCategory.Ambient, 0.4f);
            if (m_currentEnvironmentAmbient != null)
            {
                m_loopingAudio["environment_ambient"] = m_currentEnvironmentAmbient;
            }
        }

        /// <summary>
        /// 停止环境音
        /// </summary>
        public void StopEnvironmentAmbient()
        {
            StopLoopingAudio("environment_ambient");
            m_currentEnvironmentAmbient = null;
        }

        /// <summary>
        /// 播放脚步声
        /// </summary>
        public void PlayFootstep(Vector3 position, float intensity = 1f)
        {
            var clip = GetRandomClip(m_footstepSounds);
            if (clip == null) return;

            AudioService.PlayOneShot(clip, position, AudioCategory.SFX, intensity * 0.5f);
        }

        /// <summary>
        /// 播放语音聊天通知音
        /// </summary>
        public void PlayVoiceChatNotification(VoiceChatNotificationType type)
        {
            if (m_voiceChatNotifications == null || m_voiceChatNotifications.Length == 0) return;

            int index = (int)type % m_voiceChatNotifications.Length;
            var clip = m_voiceChatNotifications[index];

            if (clip != null)
            {
                AudioService.PlayOneShot(clip, AudioCategory.Voice, 0.7f);
            }
        }

        #endregion

        #region UI音效方法（兼容接口）

        /// <summary>
        /// 播放UI音效
        /// </summary>
        public void PlayUISound(string soundName, float volume = 1f)
        {
            if (AudioService == null) return;

            // 这里可以根据soundName查找对应的AudioClip
            // 目前使用简单的映射，实际项目中应该有更完善的资源管理
            AudioClip clip = GetUISound(soundName);
            if (clip != null)
            {
                AudioService.PlayOneShot(clip, AudioCategory.UI, volume);
            }
        }

        /// <summary>
        /// 播放音效（通用接口）
        /// </summary>
        public void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (AudioService == null || clip == null) return;

            AudioService.PlayOneShot(clip, AudioCategory.SFX, volume);
        }

        /// <summary>
        /// 获取UI音效剪辑（临时实现）
        /// </summary>
        private AudioClip GetUISound(string soundName)
        {
            // 临时实现，返回按钮点击音效
            // 实际项目中应该有专门的UI音效资源管理
            switch (soundName.ToLower())
            {
                case "button_click":
                    return m_pointScored; // 临时使用现有音效
                case "button_hover":
                    return m_ballHitPaddleSounds?.Length > 0 ? m_ballHitPaddleSounds[0] : null;
                default:
                    return null;
            }
        }

        #endregion

        #region 循环音频管理

        /// <summary>
        /// 停止指定的循环音频
        /// </summary>
        private void StopLoopingAudio(string key)
        {
            if (m_loopingAudio.TryGetValue(key, out AudioHandle handle))
            {
                if (handle != null && handle.IsValid)
                {
                    handle.Stop();
                }
                m_loopingAudio.Remove(key);
            }
        }

        /// <summary>
        /// 停止所有循环音频
        /// </summary>
        private void StopAllLoopingAudio()
        {
            foreach (var kvp in m_loopingAudio)
            {
                if (kvp.Value != null && kvp.Value.IsValid)
                {
                    kvp.Value.Stop();
                }
            }
            m_loopingAudio.Clear();

            m_currentCrowdAmbient = null;
            m_currentEnvironmentAmbient = null;
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 从数组中随机获取音频剪辑
        /// </summary>
        private AudioClip GetRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        /// <summary>
        /// 获取调试信息
        /// </summary>
        public string GetDebugInfo()
        {
            var info = "AudioManager Debug Info:\n";
            info += $"- Crowd State: {m_crowdState}\n";
            info += $"- Crowd Excitement: {m_crowdExcitementLevel:F2}\n";
            info += $"- Looping Audio Count: {m_loopingAudio.Count}\n";
            info += $"- Last Ball Hit: {Time.time - m_lastBallHitTime:F2}s ago\n";
            info += $"- Crowd Reactions Enabled: {m_enableCrowdReactions}\n";
            info += $"- Dynamic Volume Enabled: {m_enableDynamicVolume}\n";

            return info;
        }

        #endregion
    }

    /// <summary>
    /// 观众反应类型
    /// </summary>
    public enum CrowdReactionType
    {
        Cheer,          // 欢呼
        Applause,       // 鼓掌
        Boo,           // 嘘声
        Excited,       // 兴奋
        Disappointed,  // 失望
        Celebration    // 庆祝
    }

    /// <summary>
    /// 观众反应状态
    /// </summary>
    public enum CrowdReactionState
    {
        Neutral,       // 中性
        Excited,       // 兴奋
        Pleased,       // 满意
        Disappointed   // 失望
    }

    /// <summary>
    /// 语音聊天通知类型
    /// </summary>
    public enum VoiceChatNotificationType
    {
        JoinChannel,   // 加入频道
        LeaveChannel,  // 离开频道
        Muted,         // 静音
        Unmuted,       // 取消静音
        Speaking       // 正在说话
    }
}