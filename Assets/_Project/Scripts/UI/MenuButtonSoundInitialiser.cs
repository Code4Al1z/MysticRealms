using UnityEngine;

/// <summary>
/// Place one instance of this on any GameObject in each scene that has
/// menu buttons (MainMenu, pause menu, game over panel, victory panel etc).
///
/// On Awake it finds every MenuButton in the scene — including those on
/// disabled GameObjects — and assigns the MenuButtonSounds ScriptableObject
/// automatically. No manual assignment needed on individual buttons.
/// </summary>
public class MenuButtonSoundInitialiser : MonoBehaviour
{
    [Tooltip("The shared sounds asset to assign to all MenuButtons in this scene.")]
    [SerializeField] private MenuButtonSounds sounds;

    private void Awake()
    {
        if (sounds == null)
        {
            Debug.LogWarning("[MenuButtonSoundInitialiser] No MenuButtonSounds asset assigned.");
            return;
        }

        MenuButton[] buttons = FindObjectsByType<MenuButton>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (MenuButton button in buttons)
            button.SetSounds(sounds);

        Debug.Log($"[MenuButtonSoundInitialiser] Assigned sounds to {buttons.Length} buttons.");
    }
}