using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioManager>();
                
                if (_instance == null)
                {
                    GameObject obj = new GameObject("AudioManager");
                    _instance = obj.AddComponent<AudioManager>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }

    [SerializeField] private AudioMixerGroup masterMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixerGroup uiMixerGroup;
    [SerializeField] private AudioMixerGroup voiceMixerGroup;
    
    private AudioSource musicSource;
    private AudioSource uiSource;
    private List<AudioSource> pooledSources = new List<AudioSource>();
    private int poolSize = 20;
    
    private Dictionary<AudioClip, AudioClip> convertedClips = new Dictionary<AudioClip, AudioClip>();
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeAudioSources();
    }

    private void InitializeAudioSources()
    {
        musicSource = CreateAudioSource("MusicSource", musicMixerGroup);
        musicSource.loop = true;
        musicSource.priority = 0;
        
        uiSource = CreateAudioSource("UISource", uiMixerGroup);
        uiSource.priority = 10;
        
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = CreateAudioSource($"PooledSource_{i}", sfxMixerGroup);
            source.priority = 128;
            pooledSources.Add(source);
            source.gameObject.SetActive(false);
        }
    }
    
    private AudioSource CreateAudioSource(string name, AudioMixerGroup mixerGroup)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        
        AudioSource source = obj.AddComponent<AudioSource>();
        source.playOnAwake = false;
        
        if (mixerGroup != null)
        {
            source.outputAudioMixerGroup = mixerGroup;
        }
        
        return source;
    }
    
    public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1.0f, float pitch = 1.0f)
    {
        if (clip == null) return;
        
        AudioSource source = GetAvailableAudioSource();
        source.gameObject.SetActive(true);
        source.transform.position = position;
        source.spatialBlend = 1.0f;
        source.clip = clip.ambisonic ? ConvertAmbisonicClip(clip) : clip;
        source.volume = volume;
        source.pitch = pitch;
        source.outputAudioMixerGroup = sfxMixerGroup;
        source.Play();
        
        ReturnToPoolAfterPlay(source, clip.length);
    }
    
    public void PlayMusic(AudioClip clip, float fadeTime = 1.0f, float volume = 1.0f)
    {
        if (clip == null) return;
        
        if (fadeTime > 0 && musicSource.isPlaying)
        {
            StartCoroutine(FadeMusicCoroutine(clip, fadeTime, volume));
        }
        else
        {
            musicSource.clip = clip.ambisonic ? ConvertAmbisonicClip(clip) : clip;
            musicSource.volume = volume;
            musicSource.Play();
        }
    }
    
    public void PlayUISound(AudioClip clip, float volume = 1.0f)
    {
        if (clip == null) return;
        
        uiSource.PlayOneShot(clip.ambisonic ? ConvertAmbisonicClip(clip) : clip, volume);
    }
    
    private System.Collections.IEnumerator FadeMusicCoroutine(AudioClip newClip, float fadeTime, float targetVolume)
    {
        float startVolume = musicSource.volume;
        float timer = 0;
        
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0, timer / fadeTime);
            yield return null;
        }
        
        musicSource.Stop();
        musicSource.clip = newClip.ambisonic ? ConvertAmbisonicClip(newClip) : newClip;
        musicSource.volume = 0;
        musicSource.Play();
        
        timer = 0;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0, targetVolume, timer / fadeTime);
            yield return null;
        }
        
        musicSource.volume = targetVolume;
    }
    
    private AudioSource GetAvailableAudioSource()
    {
        foreach (AudioSource source in pooledSources)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }
        
        AudioSource newSource = CreateAudioSource($"PooledSource_{pooledSources.Count}", sfxMixerGroup);
        newSource.priority = 128;
        pooledSources.Add(newSource);
        return newSource;
    }
    
    private void ReturnToPoolAfterPlay(AudioSource source, float clipLength)
    {
        StartCoroutine(ReturnToPoolCoroutine(source, clipLength));
    }
    
    private System.Collections.IEnumerator ReturnToPoolCoroutine(AudioSource source, float clipLength)
    {
        yield return new WaitForSeconds(clipLength + 0.1f);
        source.Stop();
        source.gameObject.SetActive(false);
    }
    
    private AudioClip ConvertAmbisonicClip(AudioClip ambisonicClip)
    {
        if (!ambisonicClip.ambisonic) return ambisonicClip;
        
        if (convertedClips.TryGetValue(ambisonicClip, out AudioClip convertedClip))
        {
            return convertedClip;
        }
        
        AudioClip newClip = AudioClip.Create(
            ambisonicClip.name + "_converted",
            ambisonicClip.samples,
            ambisonicClip.channels,
            ambisonicClip.frequency,
            false
        );
        
        float[] samples = new float[ambisonicClip.samples * ambisonicClip.channels];
        ambisonicClip.GetData(samples, 0);
        newClip.SetData(samples, 0);
        
        convertedClips[ambisonicClip] = newClip;
        return newClip;
    }
    
    public void SetMasterVolume(float volume)
    {
        if (masterMixerGroup != null)
        {
            masterMixerGroup.audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Max(0.001f, volume)) * 20);
        }
    }
    
    public void SetSFXVolume(float volume)
    {
        if (sfxMixerGroup != null)
        {
            sfxMixerGroup.audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(0.001f, volume)) * 20);
        }
    }
    
    public void SetMusicVolume(float volume)
    {
        if (musicMixerGroup != null)
        {
            musicMixerGroup.audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(0.001f, volume)) * 20);
        }
    }
    
    public void StopMusic()
    {
        musicSource.Stop();
    }
    
    public void PauseMusic()
    {
        musicSource.Pause();
    }
    
    public void ResumeMusic()
    {
        musicSource.UnPause();
    }
    
    public void SetAmbisonicEnabled(bool enabled)
    {
        AudioConfiguration config = AudioSettings.GetConfiguration();
        AudioSettings.Reset(config);
    }
    
    public void RegisterAmbisonicClip(AudioClip clip)
    {
        if (clip != null && clip.ambisonic)
        {
            ConvertAmbisonicClip(clip);
        }
    }
    
    private void OnDestroy()
    {
        foreach (var kvp in convertedClips)
        {
            Destroy(kvp.Value);
        }
        convertedClips.Clear();
    }
}