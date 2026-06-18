using UnityEngine;

public class PersistentEventSystem : MonoBehaviour
{
    private static PersistentEventSystem _instance;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // A duplicate came in from a scene load — destroy it silently
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}