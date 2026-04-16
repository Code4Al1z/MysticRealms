using UnityEngine;

[CreateAssetMenu(fileName = "AudioSettings", menuName = "Mystic Realms/Audio Settings")]
public class AudioSettings : ScriptableObject
{
    [Range(0f, 1f)] public float masterVolume = 0.8f;
    [Range(0f, 1f)] public float musicVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 1.0f;
}