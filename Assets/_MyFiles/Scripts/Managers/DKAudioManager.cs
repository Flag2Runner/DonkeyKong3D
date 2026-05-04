using UnityEngine;

namespace _MyFiles.Scripts.Managers
{
    public class DKAudioManager : MonoBehaviour
    {
        public static DKAudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [Tooltip("The AudioSource that plays background music.")]
        [SerializeField] private AudioSource musicSource;
        [Tooltip("The AudioSource that plays one-shot sound effects.")]
        [SerializeField] private AudioSource sfxSource;

        [Header("Music Clips")]
        public AudioClip musicAttract;
        public AudioClip musicLevel1;
        public AudioClip musicHammer;

        [Header("SFX Clips")]
        public AudioClip sfxIntro;
        public AudioClip sfxStartLevel;
        public AudioClip sfxWin;
        public AudioClip sfxDeath;
        public AudioClip sfxSmash;
        public AudioClip sfxScore;
        public AudioClip sfxPause;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null) return;
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip);
        }

        public void StopMusic()
        {
            musicSource.Stop();
        }
    }
}