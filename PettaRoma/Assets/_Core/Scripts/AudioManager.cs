using UnityEngine;

/// <summary>
/// Simple audio manager singleton.
/// Plays SFX (coins, steps, jump) and ambient background.
/// Uses Unity's built-in audio — no external assets needed.
/// Generates simple procedural beep/blip sounds at runtime.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [Header("Volumes")]
    [SerializeField] private float sfxVolume = 0.7f;
    [SerializeField] private float musicVolume = 0.3f;

    // ── Procedural clips (generated at runtime) ──
    private AudioClip coinClip;
    private AudioClip jumpClip;
    private AudioClip hurtClip;
    private AudioClip checkpointClip;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create audio sources if not assigned
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
        }

        // Generate procedural sound effects
        coinClip = GenerateTone(880f, 0.1f, 0.15f, true);    // high ping
        jumpClip = GenerateTone(440f, 0.08f, 0.12f, true);   // medium blip
        hurtClip = GenerateTone(220f, 0.15f, 0.3f, false);   // low buzz
        checkpointClip = GenerateChime();                      // happy chime
    }

    void OnEnable()
    {
        EventBus.OnCoinChanged += OnCoinCollected;
        EventBus.OnCheckpointReached += OnCheckpoint;
        EventBus.OnPlayerDied += OnPlayerDied;
    }

    void OnDisable()
    {
        EventBus.OnCoinChanged -= OnCoinCollected;
        EventBus.OnCheckpointReached -= OnCheckpoint;
        EventBus.OnPlayerDied -= OnPlayerDied;
    }

    // ── Public API ──
    public void PlayCoin() => PlaySFX(coinClip);
    public void PlayJump() => PlaySFX(jumpClip);
    public void PlayHurt() => PlaySFX(hurtClip);
    public void PlayCheckpoint() => PlaySFX(checkpointClip);

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    // ── Event handlers ──
    void OnCoinCollected(int total) => PlayCoin();
    void OnCheckpoint(int id) => PlayCheckpoint();
    void OnPlayerDied() => PlayHurt();

    // ═══════════════════════════════════════════════════════
    //  PROCEDURAL AUDIO GENERATION
    // ═══════════════════════════════════════════════════════

    /// <summary>Generate a simple sine wave tone.</summary>
    static AudioClip GenerateTone(float frequency, float duration, float fadeOut, bool ascending)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float freq = ascending ? frequency + (t / duration) * 200f : frequency;
            float envelope = 1f - (t / duration); // linear fade
            envelope = Mathf.Pow(envelope, 2f);    // exponential fade
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.5f;
        }

        var clip = AudioClip.Create("Tone", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>Generate a happy two-note chime.</summary>
    static AudioClip GenerateChime()
    {
        int sampleRate = 44100;
        float duration = 0.4f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float half = duration / 2f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float freq = t < half ? 523.25f : 659.25f; // C5 then E5
            float localT = t < half ? t : t - half;
            float envelope = 1f - (localT / half);
            envelope = Mathf.Pow(envelope, 1.5f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.4f;
        }

        var clip = AudioClip.Create("Chime", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
