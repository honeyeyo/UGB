// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System.Collections;
using PongHub.Arena.Gameplay;
using PongHub.Arena.Services;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PongHub.Arena.Crowd
{
    /// <summary>
    /// 控制看台上的NPC观众。主要功能包括:
    /// 1. 初始化观众成员并设置其队伍颜色
    /// 2. 根据比赛比分和阶段控制观众音效
    /// 3. 管理观众的欢呼、嘘声和闲置声音
    /// 4. 根据比赛状态调整观众数量
    /// </summary>
    public class CrowdController : NetworkBehaviour, IGamePhaseListener
    {
        // 着色器中附件颜色属性的ID
        private static readonly int s_attachmentColorID = Shader.PropertyToID("_Attachment_Color");

        /// <summary>
        /// 观众数量级别枚举
        /// </summary>
        public enum CrowdLevel
        {
            Full,       // 满员(100%)
            Pct75,      // 75%容量
            Half,       // 半满(50%)
            Quarter,    // 四分之一(25%)
            None,       // 空场(0%)
        }

        [SerializeField] private CrowdNPC[] m_teamACrowd;              // A队观众数组
        [SerializeField] private CrowdNPC[] m_teamBCrowd;              // B队观众数组
        [SerializeField] private Material m_teamAAccessoriesAndItemsMat;// A队配件和物品材质
        [SerializeField] private Material m_teamBAccessoriesAndItemsMat;// B队配件和物品材质

        [SerializeField] private AudioSource m_crowdAAudioSource;       // A队观众音源
        [SerializeField] private AudioSource m_crowdBAudioSource;       // B队观众音源

        [SerializeField] private AudioClip[] m_idleSounds;             // 闲置声音片段数组

        [SerializeField] private AudioClip[] m_hitReactionSounds;      // 击中反应声音片段数组
        [SerializeField] private AudioClip m_booSound;                 // 嘘声音效
        [SerializeField] private AudioClip m_chantSound;               // 欢呼声音效

        [SerializeField] private int m_booingDifferential = 3;         // 触发嘘声的分数差值

        [SerializeField] private GameManager m_gameManager;            // 游戏管理器引用

        private float m_nextBooTimeTeamA = 0;                         // A队下次嘘声时间
        private float m_nextBooTimeTeamB = 0;                         // B队下次嘘声时间

        private float m_nextChantTimeTeamA = 0;                       // A队下次欢呼时间
        private float m_nextChantTimeTeamB = 0;                       // B队下次欢呼时间

        // 记录比分
        private int m_scoreA = -1;                                    // A队得分
        private int m_scoreB = -1;                                    // B队得分

        /// <summary>
        /// 初始化观众控制器,设置队伍颜色并开始播放闲置声音
        /// </summary>
        private void Start()
        {
            Initialize(m_teamACrowd, m_teamAAccessoriesAndItemsMat);
            Initialize(m_teamBCrowd, m_teamBAccessoriesAndItemsMat);
            m_gameManager.RegisterPhaseListener(this);

            StartIdleSound();
        }

        /// <summary>
        /// 更新观众声音,处理欢呼声的播放时机
        /// </summary>
        private void Update()
        {
            if (m_nextChantTimeTeamA > 0 && Time.realtimeSinceStartup >= m_nextChantTimeTeamA)
            {
                PlayChant(0);
                m_nextChantTimeTeamA += Random.Range(30f, 50f);
            }

            if (m_nextChantTimeTeamB > 0 && Time.realtimeSinceStartup >= m_nextChantTimeTeamB)
            {
                PlayChant(1);
                m_nextChantTimeTeamB += Random.Range(30f, 50f);
            }
        }

        /// <summary>
        /// 清理资源,注销事件监听
        /// </summary>
        public override void OnDestroy()
        {
            m_gameManager.UnregisterPhaseListener(this);
            base.OnDestroy();
        }

        /// <summary>
        /// 游戏阶段改变时的回调
        /// </summary>
        public void OnPhaseChanged(GameManager.GamePhase phase)
        {
            if (phase == GameManager.GamePhase.PostGame)
            {
                m_nextChantTimeTeamA = 0;
                m_nextChantTimeTeamB = 0;
            }
        }

        /// <summary>
        /// 游戏阶段时间更新的回调
        /// </summary>
        public void OnPhaseTimeUpdate(double timeLeft)
        {
            // Nothing   
        }

        /// <summary>
        /// 队伍颜色更新时的回调
        /// </summary>
        public void OnTeamColorUpdated(TeamColor teamColorA, TeamColor teamColorB)
        {
            SetAttachmentColor(teamColorA, teamColorB);
        }

        /// <summary>
        /// 网络对象生成时的回调
        /// </summary>
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                GameState.Instance.Score.OnScoreUpdated += OnScoreUpdated;
            }
        }

        /// <summary>
        /// 网络对象销毁时的回调
        /// </summary>
        public override void OnNetworkDespawn()
        {
            GameState.Instance.Score.OnScoreUpdated -= OnScoreUpdated;
            base.OnNetworkDespawn();
        }

        /// <summary>
        /// 设置两队的附件颜色
        /// </summary>
        public void SetAttachmentColor(TeamColor teamAColor, TeamColor teamBColor)
        {
            m_teamAAccessoriesAndItemsMat.SetColor(s_attachmentColorID, TeamColorProfiles.Instance.GetColorForKey(teamAColor));
            m_teamBAccessoriesAndItemsMat.SetColor(s_attachmentColorID, TeamColorProfiles.Instance.GetColorForKey(teamBColor));
        }

        /// <summary>
        /// 根据指定的观众级别设置观众数量
        /// </summary>
        public void SetCrowdLevel(CrowdLevel crowdLevel)
        {
            var pct = 100;
            switch (crowdLevel)
            {
                case CrowdLevel.Full:
                    pct = 100;
                    break;
                case CrowdLevel.Pct75:
                    pct = 75;
                    break;
                case CrowdLevel.Half:
                    pct = 50;
                    break;
                case CrowdLevel.Quarter:
                    pct = 25;
                    break;
                case CrowdLevel.None:
                    pct = 0;
                    break;
                default:
                    break;
            }

            UpdateCrowdLevel(m_teamACrowd, pct);
            UpdateCrowdLevel(m_teamBCrowd, pct);
        }

        /// <summary>
        /// 根据百分比更新观众数量
        /// </summary>
        private void UpdateCrowdLevel(CrowdNPC[] crowd, int pct)
        {
            var activeCount = pct >= 100 ? crowd.Length :
                pct <= 0 ? 0 : Mathf.FloorToInt(crowd.Length * pct / 100f);
            for (var i = 0; i < crowd.Length; ++i)
            {
                crowd[i].gameObject.SetActive(i < activeCount);
            }
        }

        /// <summary>
        /// 初始化观众,随机设置面部表情和动画参数
        /// </summary>
        private void Initialize(CrowdNPC[] crowd, Material accessoryAndItemsMat)
        {
            foreach (var npc in crowd)
            {
                // 3x3 faces
                var faceSwap = new Vector2(Random.Range(0, 3), Random.Range(0, 3));
                npc.Init(Random.Range(0f, 1f), Random.Range(0.9f, 1.1f), faceSwap);
            }
        }

        /// <summary>
        /// 设置指定队伍的观众颜色
        /// </summary>
        private void SetTeamColor(CrowdNPC[] crowd, TeamColor teamColor)
        {
            var color = TeamColorProfiles.Instance.GetColorForKey(teamColor);
            foreach (var npc in crowd)
            {
                npc.SetColor(color);
            }
        }

        /// <summary>
        /// 比分更新时的回调,处理观众反应
        /// </summary>
        private void OnScoreUpdated(int teamAScore, int teamBScore)
        {
            if (m_scoreA < 0 || m_scoreB < 0)
            {
                m_scoreA = teamAScore;
                m_scoreB = teamBScore;
                return;
            }

            var scoredA = teamAScore > m_scoreA;
            var scoredB = teamBScore > m_scoreB;

            if (scoredA)
            {
                PlayHitReaction(0);
                if (teamAScore > teamBScore && m_nextChantTimeTeamA <= 0)
                {
                    m_nextChantTimeTeamA = Time.realtimeSinceStartup + Random.Range(0, 10);
                }

                if (teamAScore >= teamBScore + m_booingDifferential)
                {
                    if (m_nextBooTimeTeamA <= Time.realtimeSinceStartup)
                    {
                        PlayBoo(1);
                        m_nextBooTimeTeamA = Time.realtimeSinceStartup + Random.Range(12f, 20f);
                    }
                }
            }

            if (scoredB)
            {
                PlayHitReaction(1);
                if (teamBScore > teamAScore && m_nextChantTimeTeamA <= 0)
                {
                    m_nextChantTimeTeamB = Time.realtimeSinceStartup + Random.Range(0, 10);
                }

                if (teamBScore >= teamAScore + m_booingDifferential)
                {
                    if (m_nextBooTimeTeamB <= Time.realtimeSinceStartup)
                    {
                        PlayBoo(0);
                        m_nextBooTimeTeamB = Time.realtimeSinceStartup + Random.Range(12f, 20f);
                    }
                }
            }

            m_scoreA = teamAScore;
            m_scoreB = teamBScore;
        }

        /// <summary>
        /// 播放击中反应音效
        /// </summary>
        private void PlayHitReaction(int team)
        {
            PlaySoundClientRpc(new SoundParametersMessage(
                team, AudioEvents.HitReaction, Random.Range(0, m_hitReactionSounds.Length)));
        }

        /// <summary>
        /// 播放嘘声音效
        /// </summary>
        private void PlayBoo(int team)
        {
            PlaySoundClientRpc(new SoundParametersMessage(
                team, AudioEvents.Boo));
        }

        /// <summary>
        /// 播放欢呼声音效
        /// </summary>
        private void PlayChant(int team)
        {
            PlaySoundClientRpc(new SoundParametersMessage(
                team, AudioEvents.Chant));
        }

        /// <summary>
        /// 在所有客户端播放指定的音效
        /// </summary>
        [ClientRpc]
        private void PlaySoundClientRpc(SoundParametersMessage soundMsg)
        {
            var audioSource = soundMsg.Team == 0 ? m_crowdAAudioSource : m_crowdBAudioSource;
            switch (soundMsg.AudioEvent)
            {
                case AudioEvents.HitReaction:
                    audioSource.PlayOneShot(m_hitReactionSounds[soundMsg.AudioEventIndex]);
                    break;

                case AudioEvents.Boo:
                    audioSource.PlayOneShot(m_booSound);
                    break;

                case AudioEvents.Chant:
                    audioSource.PlayOneShot(m_chantSound);
                    break;
                case AudioEvents.Idle:
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 开始播放闲置声音
        /// </summary>
        private void StartIdleSound()
        {
            _ = StartCoroutine(PlayIdleCoroutine(0));
            _ = StartCoroutine(PlayIdleCoroutine(1));
        }

        /// <summary>
        /// 循环播放闲置声音的协程
        /// </summary>
        private IEnumerator PlayIdleCoroutine(int team)
        {
            var audioSource = team == 0 ? m_crowdAAudioSource : m_crowdBAudioSource;

            while (true)
            {
                var idleIndex = Random.Range(0, m_idleSounds.Length);
                var clip = m_idleSounds[idleIndex];
                audioSource.clip = clip;
                audioSource.loop = true;
                audioSource.time = Random.Range(0, clip.length);
                audioSource.Play();
                // wait a full audio loop
                yield return new WaitForSeconds(clip.length);
            }
        }

        /// <summary>
        /// 音效类型枚举
        /// </summary>
        internal enum AudioEvents
        {
            Idle,           // 闲置声音
            HitReaction,    // 击中反应
            Boo,            // 嘘声
            Chant,          // 欢呼声
        }

        /// <summary>
        /// 音效参数消息结构体,用于网络传输
        /// </summary>
        private struct SoundParametersMessage : INetworkSerializable
        {
            public int Team;                // 队伍编号
            public AudioEvents AudioEvent;  // 音效类型
            public int AudioEventIndex;     // 音效索引

            internal SoundParametersMessage(int team, AudioEvents audioEvent, int index = 0)
            {
                Team = team;
                AudioEvent = audioEvent;
                AudioEventIndex = index;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Team);
                serializer.SerializeValue(ref AudioEvent);
                serializer.SerializeValue(ref AudioEventIndex);
            }
        }
    }
}