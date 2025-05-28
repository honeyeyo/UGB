using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Audio;

namespace PongHub.Core
{
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager s_instance;
        public static AudioManager Instance => s_instance;

        [System.Serializable]
        public class SoundEffect
        {
            public string name;
            public AudioClip clip;
            [Range(0f, 1f)]
            public float volume = 1f;
            [Range(0.1f, 3f)]
            public float pitch = 1f;
            public bool loop = false;
            [HideInInspector]
            public AudioSource source;
        }

        [Header("音频混合器")]
        [SerializeField] private AudioMixer m_audioMixer;

        [Header("音频设置")]
        [SerializeField] private float m_masterVolume = 1f;
        [SerializeField] private float m_musicVolume = 1f;
        [SerializeField] private float m_sfxVolume = 1f;

        [Header("音效设置")]
        [SerializeField] private SoundEffect[] soundEffects;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip[] backgroundMusic;

        [Header("音效")]
        [SerializeField] private AudioClip m_paddleHitSound;
        [SerializeField] private AudioClip m_tableHitSound;
        [SerializeField] private AudioClip m_netHitSound;
        [SerializeField] private AudioClip m_edgeHitSound;

        private Dictionary<string, SoundEffect> soundEffectDict;
        private AudioSource m_audioSource;

        private void Awake()
        {
            if (s_instance == null)
            {
                s_instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudio();
            }
            else
            {
                Destroy(gameObject);
            }

            m_audioSource = GetComponent<AudioSource>();
            if (m_audioSource == null)
            {
                m_audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void InitializeAudio()
        {
            soundEffectDict = new Dictionary<string, SoundEffect>();

            // 为每个音效创建AudioSource
            foreach (var sound in soundEffects)
            {
                sound.source = gameObject.AddComponent<AudioSource>();
                sound.source.clip = sound.clip;
                sound.source.volume = sound.volume;
                sound.source.pitch = sound.pitch;
                sound.source.loop = sound.loop;

                soundEffectDict[sound.name] = sound;
            }
        }

        public async Task InitializeAsync()
        {
            await Task.Yield();
            LoadAudioSettings();
        }

        public void Cleanup()
        {
            // 清理音频资源
        }

        private void LoadAudioSettings()
        {
            m_masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            m_musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            m_sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);

            SetMasterVolume(m_masterVolume);
            SetMusicVolume(m_musicVolume);
            SetSFXVolume(m_sfxVolume);
        }

        public void PlaySound(string name)
        {
            if (soundEffectDict.TryGetValue(name, out SoundEffect sound))
            {
                sound.source.Play();
            }
            else
            {
                Debug.LogWarning($"Sound {name} not found!");
            }
        }

        public void StopSound(string name)
        {
            if (soundEffectDict.TryGetValue(name, out SoundEffect sound))
            {
                sound.source.Stop();
            }
        }

        public void PlayMusic(int index)
        {
            if (index >= 0 && index < backgroundMusic.Length)
            {
                musicSource.clip = backgroundMusic[index];
                musicSource.Play();
            }
        }

        public void StopMusic()
        {
            musicSource.Stop();
        }

        public void SetMusicVolume(float volume)
        {
            m_musicVolume = Mathf.Clamp01(volume);
            m_audioMixer?.SetFloat("MusicVolume", Mathf.Log10(m_musicVolume) * 20f);
        }

        public void SetSoundVolume(float volume)
        {
            foreach (var sound in soundEffects)
            {
                sound.source.volume = Mathf.Clamp01(volume);
            }
        }

        // 乒乓球特定的音效播放方法
        public void PlayPaddleHit(Vector3 position, float volume = 1f)
        {
            PlaySoundAtPosition(m_paddleHitSound, position, volume);
        }

        public void PlayTableHit(Vector3 position, float volume = 1f)
        {
            PlaySoundAtPosition(m_tableHitSound, position, volume);
        }

        public void PlayNetHit(Vector3 position, float volume = 1f)
        {
            PlaySoundAtPosition(m_netHitSound, position, volume);
        }

        public void PlayEdgeHit(Vector3 position, float volume = 1f)
        {
            PlaySoundAtPosition(m_edgeHitSound, position, volume);
        }

        private void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (clip != null)
            {
                m_audioSource.PlayOneShot(clip, volume * m_sfxVolume * m_masterVolume);
            }
        }

        private void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, position, volume * m_sfxVolume * m_masterVolume);
            }
        }

        public void SetMasterVolume(float volume)
        {
            m_masterVolume = Mathf.Clamp01(volume);
            m_audioMixer?.SetFloat("MasterVolume", Mathf.Log10(m_masterVolume) * 20f);
        }

        public void SetSFXVolume(float volume)
        {
            m_sfxVolume = Mathf.Clamp01(volume);
            m_audioMixer?.SetFloat("SFXVolume", Mathf.Log10(m_sfxVolume) * 20f);
        }

        public void PlayScore()
        {
            PlaySound("Score");
        }

        public void PlayGameStart()
        {
            PlaySound("GameStart");
        }

        public void PlayGameOver()
        {
            PlaySound("GameOver");
        }

        public void PlayBallHit(Vector3 position, float volume)
        {
            // TODO: 实现球击打音效
        }
    }
}