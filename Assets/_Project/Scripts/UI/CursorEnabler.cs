using UnityEngine;
using UnityEngine.InputSystem;

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

    private void Update()
    {
        if (_activeCount == 0)
            UpdateCursor();
    }

    private static void UpdateCursor()
    {
        if (_activeCount > 0)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        Cursor.visible = !IsCursorInsideWindow();
        Cursor.lockState = CursorLockMode.None;
    }

    private static bool IsCursorInsideWindow()
    {
        if (Mouse.current == null) return false;

        Vector2 pos = Mouse.current.position.ReadValue();
        return pos.x >= 0 && pos.x <= Screen.width
            && pos.y >= 0 && pos.y <= Screen.height;
    }
}