using UnityEngine;

public class CursorEnabler : MonoBehaviour
{
    private static int _activeCount = 0;

    private void OnEnable()
    {
        _activeCount++;
        UpdateCursor();
    }

    private void OnDisable()
    {
        _activeCount = Mathf.Max(0, _activeCount - 1);
        UpdateCursor();
    }

    private static void UpdateCursor()
    {
        bool show = _activeCount > 0;
        Cursor.visible = show;
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
    }
}