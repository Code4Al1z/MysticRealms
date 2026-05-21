using UnityEngine;

/// <summary>
/// ScriptableObject holding the shared Wwise events for all menu buttons.
/// Create one asset via Assets -> Create -> Mystic Realms -> Menu Button Sounds.
/// </summary>
[CreateAssetMenu(fileName = "MenuButtonSounds",
                 menuName = "Mystic Realms/Menu Button Sounds")]
public class MenuButtonSounds : ScriptableObject
{
    [Header("Wwise Events")]
    public AK.Wwise.Event hoverEvent;
    public AK.Wwise.Event clickEvent;
}
