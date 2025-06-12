using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameLogic.Runtime
{
  
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Background Music Clips by Scene")]
        [SerializeField] private List<SceneMusic> sceneMusics = new List<SceneMusic>();

        [Header("Character Audio Settings")]
        [SerializeField] private List<CharacterAudio> characterAudios = new List<CharacterAudio>(2);

        [Header("Skill Audio")]
        [SerializeField] private AudioClip skillClip;

        private void Awake()
        {
           
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

           
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;

           
            SceneManager.sceneLoaded += OnSceneLoaded;


            NetworkClient.RegisterHandler<PlaySoundMessage>(OnPlaySoundMessage);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (NetworkClient.active)
                NetworkClient.UnregisterHandler<PlaySoundMessage>();
        }

        #region 
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlayMusicForScene(scene.name);
        }

        private void PlayMusicForScene(string sceneName)
        {
            var entry = sceneMusics.Find(sm =>
                sm.sceneName.Equals(sceneName, StringComparison.OrdinalIgnoreCase));
            if (entry != null && entry.musicClip != null)
            {
                if (musicSource.isPlaying && musicSource.clip == entry.musicClip)
                    return;

                musicSource.clip = entry.musicClip;
                musicSource.Play();
            }
            else
            {
                musicSource.Stop();
            }
        }
        #endregion

        #region 
        private void OnPlaySoundMessage(PlaySoundMessage msg)
        {
            switch (msg.eventType)
            {
                case SoundEvent.Attack:
                    PlayLocalAttack(msg.characterIndex);
                    break;
                case SoundEvent.Hit:
                    PlayLocalHit(msg.characterIndex);
                    break;
                case SoundEvent.Skill:
                    PlayLocalSkill();
                    break;

            }
        }
        #endregion

        #region

        public void PlayLocalAttack(int characterIndex)
        {
            if (IsValidCharacter(characterIndex))
            {
                var clip = characterAudios[characterIndex].attackClip;
                if (clip != null)
                    sfxSource.PlayOneShot(clip);
            }
        }

        public void PlayLocalHit(int characterIndex)
        {
            if (IsValidCharacter(characterIndex))
            {
                var hits = characterAudios[characterIndex].hitClips;
                if (hits != null && hits.Count > 0)
                {
                    int idx = UnityEngine.Random.Range(0, hits.Count);
                    var clip = hits[idx];
                    if (clip != null)
                        sfxSource.PlayOneShot(clip);
                }
            }
        }

        public void PlayLocalSkill()
        {
            if (skillClip != null)
                sfxSource.PlayOneShot(skillClip);
        }

        private bool IsValidCharacter(int index)
        {
            return index >= 0 && index < characterAudios.Count;
        }
        #endregion
    }

    [Serializable]
    public class SceneMusic
    {
        public string sceneName;
        public AudioClip musicClip;
    }

    [Serializable]
    public class CharacterAudio
    {
        public AudioClip attackClip;
        public List<AudioClip> hitClips = new List<AudioClip>(3);
    }
}
