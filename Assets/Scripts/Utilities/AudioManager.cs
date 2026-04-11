using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    public bool loop = false;
}

public class AudioManager : Singleton<AudioManager>
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    
    [Header("Sound Library")]
    [SerializeField] private Sound[] sounds;
    
    [Header("Volume Settings")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1f;
    
    private Dictionary<string, Sound> soundDictionary;
    private List<AudioSource> sfxSourcePool;
    private const int POOL_SIZE = 10;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Initialize audio sources if not assigned
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
        }
        
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
        }
        
        // Initialize sound dictionary
        soundDictionary = new Dictionary<string, Sound>();
        foreach (Sound sound in sounds)
        {
            if (!soundDictionary.ContainsKey(sound.name))
            {
                soundDictionary.Add(sound.name, sound);
            }
        }
        
        // Initialize SFX pool
        sfxSourcePool = new List<AudioSource>();
        for (int i = 0; i < POOL_SIZE; i++)
        {
            GameObject poolObj = new GameObject($"SFXPool_{i}");
            poolObj.transform.SetParent(transform);
            AudioSource source = poolObj.AddComponent<AudioSource>();
            sfxSourcePool.Add(source);
        }
        
        ApplyVolumeSettings();
    }
    
    #region Music Control
    
    /// <summary>
    /// Play background music by name
    /// </summary>
    public void PlayMusic(string soundName, bool fadeIn = false)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            if (fadeIn)
            {
                musicSource.volume = 0;
                musicSource.clip = sound.clip;
                musicSource.pitch = sound.pitch;
                musicSource.loop = sound.loop;
                musicSource.Play();
                FadeMusic(musicVolume * masterVolume * sound.volume, fadeDuration);
            }
            else
            {
                musicSource.clip = sound.clip;
                musicSource.volume = musicVolume * masterVolume * sound.volume;
                musicSource.pitch = sound.pitch;
                musicSource.loop = sound.loop;
                musicSource.Play();
            }
        }
        else
        {
            Debug.LogWarning($"Sound '{soundName}' not found!");
        }
    }
    
    /// <summary>
    /// Stop music
    /// </summary>
    public void StopMusic(bool fadeOut = false)
    {
        if (fadeOut)
        {
            FadeMusic(0f, fadeDuration, () => musicSource.Stop());
        }
        else
        {
            musicSource.Stop();
        }
    }
    
    /// <summary>
    /// Pause music
    /// </summary>
    public void PauseMusic()
    {
        musicSource.Pause();
    }
    
    /// <summary>
    /// Resume music
    /// </summary>
    public void ResumeMusic()
    {
        musicSource.UnPause();
    }
    
    /// <summary>
    /// Fade music to target volume
    /// </summary>
    private void FadeMusic(float targetVolume, float duration, System.Action onComplete = null)
    {
        LeanTween.cancel(musicSource.gameObject);
        LeanTween.value(musicSource.gameObject, musicSource.volume, targetVolume, duration)
            .setOnUpdate((float val) => musicSource.volume = val)
            .setOnComplete(onComplete);
    }
    
    #endregion
    
    #region SFX Control
    
    /// <summary>
    /// Play sound effect by name
    /// </summary>
    public void PlaySFX(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            AudioSource source = GetAvailableSource();
            source.clip = sound.clip;
            source.volume = sfxVolume * masterVolume * sound.volume;
            source.pitch = sound.pitch;
            source.loop = sound.loop;
            source.Play();
        }
        else
        {
            Debug.LogWarning($"Sound '{soundName}' not found!");
        }
    }
    
    /// <summary>
    /// Play sound effect with random pitch variation
    /// </summary>
    public void PlaySFXWithPitchVariation(string soundName, float pitchVariation = 0.1f)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            AudioSource source = GetAvailableSource();
            source.clip = sound.clip;
            source.volume = sfxVolume * masterVolume * sound.volume;
            source.pitch = sound.pitch + Random.Range(-pitchVariation, pitchVariation);
            source.loop = sound.loop;
            source.Play();
        }
        else
        {
            Debug.LogWarning($"Sound '{soundName}' not found!");
        }
    }
    
    /// <summary>
    /// Play one shot sound effect (doesn't interrupt current sound)
    /// </summary>
    public void PlaySFXOneShot(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            sfxSource.PlayOneShot(sound.clip, sfxVolume * masterVolume * sound.volume);
        }
        else
        {
            Debug.LogWarning($"Sound '{soundName}' not found!");
        }
    }
    
    /// <summary>
    /// Stop all sound effects
    /// </summary>
    public void StopAllSFX()
    {
        sfxSource.Stop();
        foreach (AudioSource source in sfxSourcePool)
        {
            source.Stop();
        }
    }
    
    /// <summary>
    /// Get available audio source from pool
    /// </summary>
    private AudioSource GetAvailableSource()
    {
        foreach (AudioSource source in sfxSourcePool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }
        return sfxSource; // Fallback to main SFX source
    }
    
    #endregion
    
    #region Volume Control
    
    /// <summary>
    /// Set master volume
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
    }
    
    /// <summary>
    /// Set music volume
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
    }
    
    /// <summary>
    /// Set SFX volume
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
    }
    
    /// <summary>
    /// Apply volume settings to audio sources
    /// </summary>
    private void ApplyVolumeSettings()
    {
        if (musicSource != null)
        {
            musicSource.volume = musicVolume * masterVolume;
        }
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume * masterVolume;
        }
    }
    
    /// <summary>
    /// Get current master volume
    /// </summary>
    public float GetMasterVolume() => masterVolume;
    
    /// <summary>
    /// Get current music volume
    /// </summary>
    public float GetMusicVolume() => musicVolume;
    
    /// <summary>
    /// Get current SFX volume
    /// </summary>
    public float GetSFXVolume() => sfxVolume;
    
    #endregion
    
    #region Utility
    
    /// <summary>
    /// Check if music is playing
    /// </summary>
    public bool IsMusicPlaying() => musicSource.isPlaying;
    
    /// <summary>
    /// Mute/unmute all audio
    /// </summary>
    public void SetMute(bool mute)
    {
        AudioListener.volume = mute ? 0 : 1;
    }
    
    #endregion
}
