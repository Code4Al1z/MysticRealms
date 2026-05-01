using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class WwiseTitleController : MonoBehaviour
{
    [Header("Wwise Settings")]
    public string musicAmplitudeRTPC = "Music_Amplitude";
    public string kickRTPC = "Kick_Pulse";
    public bool useWwise = true;

    [Header("Lightning & Wave Settings")]
    public float lightningSensitivity = 2.0f;
    public float waveSpeed = 0.7f;
    [Range(0.01f, 1f)] public float smoothing = 0.1f;

    [Header("Idle Settings")]
    [Tooltip("How often lightning flickers when silent.")]
    public float idleFlickerRate = 0.1f;
    public float minimumGlow = 0.4f;

    private TMP_Text _textMesh;
    private Material _textMaterial;
    private float _smoothedKick;
    private float _smoothedAmp;
    private float _currentWavePos;

    void Start()
    {
        _textMesh = GetComponent<TMP_Text>();
        _textMaterial = _textMesh.fontMaterial;
    }

    void Update()
    {
        float rawKick = 0;
        float rawAmp = 0;

        if (useWwise && AkUnitySoundEngine.IsInitialized())
        {
            int type = (int)AkQueryRTPCValue.RTPCValue_Global;
            AkUnitySoundEngine.GetRTPCValue(kickRTPC, gameObject, 0, out rawKick, ref type);
            AkUnitySoundEngine.GetRTPCValue(musicAmplitudeRTPC, gameObject, 0, out rawAmp, ref type);
        }

        // --- LIGHTNING LOGIC ---
        // Create a random flicker if Wwise is silent
        float lightningIdle = (Random.value > (1.0f - idleFlickerRate)) ? Random.Range(0.5f, 1f) : 0f;
        float finalKick = (rawKick > 0) ? rawKick : lightningIdle;
        _smoothedKick = Mathf.Lerp(_smoothedKick, finalKick, smoothing);

        // --- GLOW LOGIC ---
        float finalAmp = Mathf.Max(rawAmp / 100f, minimumGlow);
        _smoothedAmp = Mathf.Lerp(_smoothedAmp, finalAmp, smoothing);

        // --- WAVE LOGIC ---
        _currentWavePos += Time.deltaTime * waveSpeed;
        if (_currentWavePos > 1.4f) _currentWavePos = -0.4f;

        // --- APPLY TO SHADER ---
        if (_textMaterial != null)
        {
            _textMaterial.SetFloat("_KickIntensity", _smoothedKick * lightningSensitivity);
            _textMaterial.SetFloat("_MagicBrightness", _smoothedAmp);
            _textMaterial.SetFloat("_ShimmerPos", _currentWavePos);
        }
    }
}