using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[System.Serializable]
public class Sound
{
    public string id;
    public AudioClip clip;
    public bool loop;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Loop SFX Pool")]
    public int loopSourceCount = 3;
    private List<AudioSource> loopSources = new List<AudioSource>();

    [Header("BGM List")]
    public Sound[] bgmList;

    [Header("SFX List")]
    public Sound[] sfxList;

    private Dictionary<string, Sound> bgmDict;
    private Dictionary<string, Sound> sfxDict;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Initialize()
    {
        bgmDict = new Dictionary<string, Sound>();
        sfxDict = new Dictionary<string, Sound>();

        foreach (Sound s in bgmList)
        {
            if (!bgmDict.ContainsKey(s.id))
                bgmDict.Add(s.id, s);
        }

        foreach (Sound s in sfxList)
        {
            if (!sfxDict.ContainsKey(s.id))
                sfxDict.Add(s.id, s);
        }

        // Create loop audio sources
        for (int i = 0; i < loopSourceCount; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.loop = true;
            loopSources.Add(src);
        }
    }

    // =========================
    // BGM
    // =========================
    public void PlayBGM(string id)
    {
        if (!bgmDict.TryGetValue(id, out Sound sound))
        {
            Debug.LogWarning("BGM not found: " + id);
            return;
        }

        if (bgmSource.clip == sound.clip) return;

        bgmSource.clip = sound.clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    // =========================
    // SFX
    // =========================
    public void PlaySFX(string id)
    {
        if (!sfxDict.TryGetValue(id, out Sound sound))
        {
            Debug.LogWarning("SFX not found: " + id);
            return;
        }

        if (sound.loop)
        {
            PlayLoopSFX(sound);
        }
        else
        {
            sfxSource.PlayOneShot(sound.clip);
        }
    }

    void PlayLoopSFX(Sound sound)
    {
        // cek apakah sudah diputar
        foreach (var src in loopSources)
        {
            if (src.clip == sound.clip && src.isPlaying)
                return;
        }

        // cari slot kosong
        foreach (var src in loopSources)
        {
            if (!src.isPlaying)
            {
                src.clip = sound.clip;
                src.Play();
                return;
            }
        }

        Debug.LogWarning("No available loop audio source!");
    }

    public void StopLoopSFX(string id)
    {
        if (!sfxDict.TryGetValue(id, out Sound sound)) return;

        foreach (var src in loopSources)
        {
            if (src.clip == sound.clip)
            {
                src.Stop();
                return;
            }
        }
    }
}
