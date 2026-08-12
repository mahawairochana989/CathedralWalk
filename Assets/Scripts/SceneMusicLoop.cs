using UnityEngine;

/// <summary>
/// Plays looping background music for the cathedral walk scene.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SceneMusicLoop : MonoBehaviour
{
    public AudioClip clip;
    public float volume = 0.45f;

    AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Object.FindFirstObjectByType<SceneMusicLoop>() != null) return;

        var go = new GameObject("SceneMusic");
        Object.DontDestroyOnLoad(go);
        var music = go.AddComponent<SceneMusicLoop>();
        music.Invoke(nameof(StartMusic), 0.15f);
    }

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f; // 2D
        source.volume = volume;
        source.priority = 0;
    }

    public void StartMusic()
    {
        if (clip == null)
            clip = Resources.Load<AudioClip>("Audio/Sarabanda_Handel");

        if (clip == null)
        {
            // Fallback: load any clip under Resources/Audio
            var all = Resources.LoadAll<AudioClip>("Audio");
            if (all != null && all.Length > 0) clip = all[0];
        }

        if (clip == null)
        {
            Debug.LogWarning("SceneMusicLoop: Sarabanda clip not found in Resources/Audio");
            return;
        }

        source.clip = clip;
        source.loop = true;
        source.volume = volume;
        if (!source.isPlaying) source.Play();
        Debug.Log("SceneMusicLoop: playing " + clip.name);
    }
}
