using UnityEngine;
using System.Collections.Generic;

namespace PongHub.Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

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

        [Header("音效设置")]
        [SerializeField] private SoundEffect[] soundEffects;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip[] backgroundMusic;

        private Dictionary<string, SoundEffect> soundEffectDict;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudio();
            }
            else
            {
                Destroy(gameObject);
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
            musicSource.volume = Mathf.Clamp01(volume);
        }

        public void SetSoundVolume(float volume)
        {
            foreach (var sound in soundEffects)
            {
                sound.source.volume = Mathf.Clamp01(volume);
            }
        }

        // 乒乓球特定的音效播放方法
        public void PlayPaddleHit()
        {
            PlaySound("PaddleHit");
        }

        public void PlayTableHit()
        {
            PlaySound("TableHit");
        }

        public void PlayNetHit()
        {
            PlaySound("NetHit");
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
    }
}