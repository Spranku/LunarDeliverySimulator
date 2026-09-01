using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Music")]
    public AudioSource musicSource;
    public AudioClip menuMusic;

    [Header("SFX")]
    public AudioSource sfxSource;

    private float currentVolume = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.volume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
    }

    void Start()
    {
        PlayMusic();
    }

    public void PlayMusic()
    {
        if (menuMusic != null)
        {
            musicSource.clip = menuMusic;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void SetVolume(float volume)
    {
        currentVolume = Mathf.Clamp01(volume);

        if (musicSource != null)
        {
            musicSource.volume = currentVolume;
        }


        PlayerPrefs.SetFloat("MasterVolume", currentVolume);
        PlayerPrefs.Save();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }
}