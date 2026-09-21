using UnityEngine;

namespace GemmaRainbowSeeker
{
    /// <summary>
    /// Central audio manager for Gemma Beaker: Rainbow Seeker.
    /// Manages continuous background music playback (seamless looping across level transitions)
    /// and SFX playback (colour-tuned gem pickups, chimes, and completion fanfares).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Background Music")]
        [Tooltip("The default background music loop.")]
        [SerializeField] private AudioClip backgroundMusic;

        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.65f;

        [SerializeField] private bool playMusicOnStart = true;
        [SerializeField] private bool persistAcrossScenes = true;

        [Header("Gem Pickups SFX (1-8)")]
        [Tooltip("Ordered pickup sounds for rainbow colours (0=Red to 6=Violet), and index 7=Rainbow Completion.")]
        [SerializeField] private AudioClip[] gemPickupSounds;

        [Header("SFX Tuning")]
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 0.85f;

        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        private GameSession _connectedSession;

        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = Mathf.Clamp01(value);
                if (musicSource != null) musicSource.volume = musicVolume;
            }
        }

        public float SfxVolume
        {
            get => sfxVolume;
            set => sfxVolume = Mathf.Clamp01(value);
        }

        public AudioClip BackgroundMusic
        {
            get => backgroundMusic;
            set => backgroundMusic = value;
        }

        public AudioClip[] GemPickupSounds
        {
            get => gemPickupSounds;
            set => gemPickupSounds = value;
        }

        public bool IsMusicPlaying => musicSource != null && musicSource.isPlaying;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Preserve the currently running instance across scene transitions
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (persistAcrossScenes && Application.isPlaying)
            {
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                DontDestroyOnLoad(gameObject);
            }

            EnsureAudioSources();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            DisconnectFromSession();
        }

        private void Start()
        {
            if (playMusicOnStart && Application.isPlaying)
            {
                PlayMusic(backgroundMusic);
            }

            ConnectToSession(GameSession.Active);
        }

        private void Update()
        {
            // Re-hook if a new scene initialized GameSession.Active
            if (GameSession.Active != null && GameSession.Active != _connectedSession)
            {
                ConnectToSession(GameSession.Active);
            }
        }

        public void ConnectToSession(GameSession session)
        {
            if (session == null || session == _connectedSession) return;

            DisconnectFromSession();

            _connectedSession = session;
            _connectedSession.OnRainbowCompleted += HandleRainbowCompleted;
        }

        public void DisconnectFromSession()
        {
            if (_connectedSession != null)
            {
                _connectedSession.OnRainbowCompleted -= HandleRainbowCompleted;
                _connectedSession = null;
            }
        }

        private void HandleRainbowCompleted()
        {
            PlayRainbowCompletedSound();
        }

        public void EnsureAudioSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.spatialBlend = 0f; // 2D stereo
                musicSource.volume = musicVolume;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f; // 2D stereo
                sfxSource.volume = sfxVolume;
            }
        }

        /// <summary>
        /// Plays or loops the specified music clip. If clip is null, uses backgroundMusic.
        /// If the requested clip is already playing, continues smoothly.
        /// </summary>
        public void PlayMusic(AudioClip clip = null, bool loop = true)
        {
            if (!Application.isPlaying) return;
            EnsureAudioSources();

            AudioClip target = clip != null ? clip : backgroundMusic;
            if (target == null) return;

            if (musicSource.clip == target && musicSource.isPlaying)
            {
                return; // already playing this music loop
            }

            musicSource.clip = target;
            musicSource.loop = loop;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        public void PauseMusic()
        {
            if (musicSource != null)
            {
                musicSource.Pause();
            }
        }

        public void ResumeMusic()
        {
            if (musicSource != null && !musicSource.isPlaying)
            {
                musicSource.UnPause();
            }
        }

        /// <summary>
        /// Plays a one-shot SFX clip in 2D space.
        /// </summary>
        public void PlaySfx(AudioClip clip, float volumeScale = 1.0f)
        {
            if (!Application.isPlaying || clip == null) return;
            EnsureAudioSources();

            sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
        }

        /// <summary>
        /// Plays colour-matched gem pickup sound (Gem_Pickup_01 for Red through Gem_Pickup_07 for Violet).
        /// </summary>
        public void PlayGemPickup(RainbowColour colour)
        {
            int index = (int)colour;
            if (gemPickupSounds != null && index >= 0 && index < gemPickupSounds.Length)
            {
                PlaySfx(gemPickupSounds[index]);
            }
            else if (gemPickupSounds != null && gemPickupSounds.Length > 0 && gemPickupSounds[0] != null)
            {
                PlaySfx(gemPickupSounds[0]);
            }
        }

        /// <summary>
        /// Plays the completion chime (Gem_Pickup_08) when all rainbow gems are collected.
        /// </summary>
        public void PlayRainbowCompletedSound()
        {
            if (gemPickupSounds != null && gemPickupSounds.Length >= 8 && gemPickupSounds[7] != null)
            {
                PlaySfx(gemPickupSounds[7], 1.15f);
            }
        }

        /// <summary>
        /// Convenient static helper to play one-shot SFX through the active manager or fallback.
        /// </summary>
        public static void PlayClip(AudioClip clip, float volume = 1f)
        {
            if (!Application.isPlaying || clip == null) return;
            if (Instance != null)
            {
                Instance.PlaySfx(clip, volume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, volume);
            }
        }
    }
}
