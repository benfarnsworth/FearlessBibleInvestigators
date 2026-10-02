using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource stingerSource; // Dedicated for long/important SFX (Quest Accept, Goals)
    [SerializeField] private AudioSource musicSource;

    [Header("Quick Mute Switches")]
    [SerializeField] private bool isMusicOn = true;
    [SerializeField] private bool isSFXOn = true;

    [Header("Default Pitch Range")]
    [Range(0.5f, 1.5f)] public float defaultMinPitch = 0.85f;
    [Range(0.5f, 1.5f)] public float defaultMaxPitch = 1.15f;

    [Header("Music Fading")]
    [SerializeField] private float defaultFadeDuration = 1.0f;

    private const string MUSIC_PREF_KEY = "MusicEnabled";
    private const string SFX_PREF_KEY = "SFXEnabled";

    private Coroutine musicFadeCoroutine;
    private List<AudioSource> dynamicPitchPool = new List<AudioSource>();

    public bool IsMusicOn => isMusicOn;
    public bool IsSFXOn => isSFXOn;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeStingerSource();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeStingerSource()
    {
        if (stingerSource == null)
        {
            // Auto-create stinger source if not assigned in Inspector
            stingerSource = gameObject.AddComponent<AudioSource>();
            stingerSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        isMusicOn = PlayerPrefs.GetInt(MUSIC_PREF_KEY, isMusicOn ? 1 : 0) == 1;
        isSFXOn = PlayerPrefs.GetInt(SFX_PREF_KEY, isSFXOn ? 1 : 0) == 1;

        ApplyMusicState();
        ApplySFXState();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyMusicState();
            ApplySFXState();
        }
    }
#endif

    #region Mute & Unmute Controls

    public void ToggleMusic() => SetMusicEnabled(!isMusicOn);

    public void SetMusicEnabled(bool isEnabled)
    {
        isMusicOn = isEnabled;
        PlayerPrefs.SetInt(MUSIC_PREF_KEY, isMusicOn ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMusicState();
    }

    public void ToggleSFX() => SetSFXEnabled(!isSFXOn);

    public void SetSFXEnabled(bool isEnabled)
    {
        isSFXOn = isEnabled;
        PlayerPrefs.SetInt(SFX_PREF_KEY, isSFXOn ? 1 : 0);
        PlayerPrefs.Save();
        ApplySFXState();
    }

    private void ApplyMusicState()
    {
        if (musicSource != null) musicSource.mute = !isMusicOn;
    }

    private void ApplySFXState()
    {
        if (sfxSource != null) sfxSource.mute = !isSFXOn;
        if (stingerSource != null) stingerSource.mute = !isSFXOn;
        
        foreach (var source in dynamicPitchPool)
        {
            if (source != null) source.mute = !isSFXOn;
        }
    }

    #endregion

    #region SFX Methods

    /// <summary>
    /// Plays standard sound effects. If minPitch == maxPitch == 1.0f, plays on sfxSource.
    /// If pitch variation is requested, uses an isolated pooled AudioSource so other playing sounds aren't affected.
    /// </summary>
    public void PlaySFX(AudioClip clip, float minPitch = 1.0f, float maxPitch = 1.0f)
    {
        if (clip == null || sfxSource == null || !isSFXOn) return;

        // Standard unpitched playback
        if (Mathf.Approximately(minPitch, 1.0f) && Mathf.Approximately(maxPitch, 1.0f))
        {
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(clip);
            return;
        }

        // Variable pitch playback - use an isolated AudioSource channel from pool
        AudioSource pooledSource = GetPooledAudioSource();
        pooledSource.pitch = Random.Range(minPitch, maxPitch);
        pooledSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Plays important stingers (Quest Accept, Level Up, Goal Complete) on a dedicated channel strictly locked at 1.0 pitch.
    /// </summary>
    public void PlayStinger(AudioClip clip)
    {
        if (clip == null || stingerSource == null || !isSFXOn) return;

        stingerSource.pitch = 1.0f;
        stingerSource.PlayOneShot(clip);
    }

    private AudioSource GetPooledAudioSource()
    {
        foreach (var source in dynamicPitchPool)
        {
            if (!source.isPlaying)
            {
                source.mute = !isSFXOn;
                return source;
            }
        }

        // Create new pooled source if all are busy
        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        newSource.mute = !isSFXOn;
        dynamicPitchPool.Add(newSource);
        return newSource;
    }

    #endregion

    #region Music Methods

    public void PlayMusic(AudioClip musicClip, bool loop = true, float fadeDuration = -1f)
    {
        if (musicClip == null || musicSource == null) return;
        if (musicSource.clip == musicClip && musicSource.isPlaying) return;

        if (fadeDuration < 0f) fadeDuration = defaultFadeDuration;

        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }

        if (fadeDuration > 0f && musicSource.isPlaying)
        {
            musicFadeCoroutine = StartCoroutine(CrossfadeMusicRoutine(musicClip, loop, fadeDuration));
        }
        else
        {
            musicSource.clip = musicClip;
            musicSource.loop = loop;
            musicSource.pitch = 1.0f;
            musicSource.volume = 1.0f;
            musicSource.Play();
        }

        ApplyMusicState();
    }

    public void StopMusic(float fadeDuration = -1f)
    {
        if (musicSource == null || !musicSource.isPlaying) return;

        if (fadeDuration < 0f) fadeDuration = defaultFadeDuration;

        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }

        if (fadeDuration > 0f)
        {
            musicFadeCoroutine = StartCoroutine(FadeOutMusicRoutine(fadeDuration));
        }
        else
        {
            musicSource.Stop();
        }
    }

    private IEnumerator CrossfadeMusicRoutine(AudioClip newClip, bool loop, float duration)
    {
        float startVolume = musicSource.volume;
        float halfDuration = duration / 2f;
        float timer = 0f;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / halfDuration);
            yield return null;
        }

        musicSource.clip = newClip;
        musicSource.loop = loop;
        musicSource.pitch = 1.0f;
        musicSource.Play();

        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, 1.0f, timer / halfDuration);
            yield return null;
        }

        musicSource.volume = 1.0f;
    }

    private IEnumerator FadeOutMusicRoutine(float duration)
    {
        float startVolume = musicSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = startVolume;
    }

    #endregion
}