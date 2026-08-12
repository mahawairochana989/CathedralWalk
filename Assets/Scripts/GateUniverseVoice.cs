using UnityEngine;

/// <summary>
/// Low cosmic voice with echo when temple gates open.
/// </summary>
public class GateUniverseVoice : MonoBehaviour
{
    public float volume = 1f;
    public float pitch = 0.78f; // slightly faster + still deep

    AudioSource source;
    bool playing;

    static GateUniverseVoice instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (instance != null) return;
        var go = new GameObject("GateUniverseVoice");
        Object.DontDestroyOnLoad(go);
        instance = go.AddComponent<GateUniverseVoice>();
        instance.SetupAudio();
    }

    void SetupAudio()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = volume;
        source.pitch = pitch;
        source.priority = 16;

        var echo = gameObject.AddComponent<AudioEchoFilter>();
        echo.delay = 480f;
        echo.decayRatio = 0.45f;
        echo.wetMix = 0.45f;
        echo.dryMix = 1f;

        var reverb = gameObject.AddComponent<AudioReverbFilter>();
        reverb.reverbPreset = AudioReverbPreset.Cave;
        reverb.dryLevel = 0f;
        reverb.room = -1200f;
    }

    public static void PlayWelcome()
    {
        if (instance == null) Boot();
        if (instance != null) instance.PlayNow();
    }

    void PlayNow()
    {
        if (source == null) SetupAudio();

        var clip = Resources.Load<AudioClip>("Audio/GateWelcome_Universe");
        if (clip == null)
        {
            Debug.LogWarning("GateUniverseVoice: clip Audio/GateWelcome_Universe not found");
            return;
        }

        // Restart if already speaking
        source.Stop();
        source.clip = clip;
        source.pitch = pitch;
        source.volume = volume;
        source.Play();
        playing = true;
        Debug.Log("GateUniverseVoice: welcome spoken");
    }
}
