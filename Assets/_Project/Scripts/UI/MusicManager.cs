using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music Events")]
    [SerializeField] private AK.Wwise.Event menuMusicEvent;
    [SerializeField] private AK.Wwise.Event gameMusicEvent;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Wwise RTPCs (read by WwiseTitleController)")]
    [Tooltip("Set this to drive the title screen glow. Range 0-100.")]
    [SerializeField] private AK.Wwise.RTPC musicAmplitudeRTPC;
    [Tooltip("Set this to drive the title screen lightning. Range 0-1.")]
    [SerializeField] private AK.Wwise.RTPC kickPulseRTPC;

    [Header("Amplitude Simulation (if no real analysis available)")]
    [Tooltip("Simulates music amplitude with a sine wave so the title reacts " +
             "even before real music analysis is implemented.")]
    [SerializeField] private bool simulateAmplitude = true;
    [SerializeField] private float simulationSpeed = 0.8f;
    [SerializeField] private float kickIntervalSeconds = 0.5f;

    private uint menuPlayingID = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
    private uint gamePlayingID = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;

    private bool isMenuMusic = false;
    private float simulationTimer = 0f;
    private float kickTimer = 0f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        PlayMenuMusic();
    }

    private void Update()
    {
        if (!simulateAmplitude || !isMenuMusic) return;

        // Simulate a pulsing amplitude so the title shader reacts
        // even without real audio analysis
        simulationTimer += Time.unscaledDeltaTime * simulationSpeed;
        float amp = (Mathf.Sin(simulationTimer * Mathf.PI * 2f) * 0.5f + 0.5f) * 100f;

        if (musicAmplitudeRTPC != null)
            musicAmplitudeRTPC.SetGlobalValue(amp);

        // Simulate kick pulse on a regular interval
        kickTimer += Time.unscaledDeltaTime;
        if (kickTimer >= kickIntervalSeconds)
        {
            kickTimer = 0f;
            if (kickPulseRTPC != null)
                kickPulseRTPC.SetGlobalValue(1f);
        }
        else
        {
            // Decay the kick pulse
            if (kickPulseRTPC != null)
            {
                float decay = 1f - (kickTimer / kickIntervalSeconds);
                kickPulseRTPC.SetGlobalValue(decay);
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void PlayMenuMusic()
    {
        StopGameMusic();

        if (menuMusicEvent == null || 
            menuPlayingID != AkUnitySoundEngine.AK_INVALID_PLAYING_ID) return;

        menuPlayingID = menuMusicEvent.Post(gameObject);
        isMenuMusic = true;
    }

    public void PlayGameMusic()
    {
        StopMenuMusic();

        if (gameMusicEvent == null ||
            gamePlayingID != AkUnitySoundEngine.AK_INVALID_PLAYING_ID) return;

        gamePlayingID = gameMusicEvent.Post(gameObject);
        isMenuMusic = false;

        // Clear title shader RTPCs when entering game
        if (musicAmplitudeRTPC != null) musicAmplitudeRTPC.SetGlobalValue(0f);
        if (kickPulseRTPC != null)      kickPulseRTPC.SetGlobalValue(0f);
    }

    public void StopMenuMusic()
    {
        if (menuPlayingID == AkUnitySoundEngine.AK_INVALID_PLAYING_ID) return;
        AkUnitySoundEngine.StopPlayingID(menuPlayingID, 500);
        menuPlayingID = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
        isMenuMusic = false;
    }

    public void StopGameMusic()
    {
        if (gamePlayingID == AkUnitySoundEngine.AK_INVALID_PLAYING_ID) return;
        AkUnitySoundEngine.StopPlayingID(gamePlayingID, 500);
        gamePlayingID = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
    }

    public void StopAll()
    {
        StopMenuMusic();
        StopGameMusic();
    }

    // ── Scene detection ───────────────────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainMenuSceneName)
            PlayMenuMusic();
        else
            PlayGameMusic();
    }
}
