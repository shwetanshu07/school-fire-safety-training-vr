using UnityEngine;

public class SceneAudioManager : MonoBehaviour
{
    public AudioSource alarmSource;
    public AudioSource fireCrackleSource;

    // this instance allows other scripts to find this manager easily
    public static SceneAudioManager instance;

    void Awake()
    {
        // tink this specific object to instance
        instance = this;
    }

    void Start()
    {
        // just a preventive check
        if (alarmSource != null && !alarmSource.isPlaying) alarmSource.Play();
        if (fireCrackleSource != null && !fireCrackleSource.isPlaying) fireCrackleSource.Play();
    }

    public void StopAllEnvironmentSounds()
    {
        if (alarmSource != null) alarmSource.Stop();
        if (fireCrackleSource != null) fireCrackleSource.Stop();
        Debug.Log("Environment sounds stopped.");
    }
}