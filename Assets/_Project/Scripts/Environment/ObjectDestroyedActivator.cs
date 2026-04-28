using UnityEngine;
using System.Collections.Generic;

public class ObjectDestroyedActivator : MonoBehaviour
{
    [Tooltip("The object to watch. When it is destroyed, the objects below will be enabled.")]
    [SerializeField] private GameObject watchedObject;

    [Tooltip("GameObjects to enable when the watched object is destroyed.")]
    [SerializeField] private List<GameObject> objectsToEnable;

    private void Start()
    {
        foreach (GameObject obj in objectsToEnable)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    private void Update()
    {
        if (watchedObject != null) return;

        foreach (GameObject obj in objectsToEnable)
        {
            if (obj != null)
                obj.SetActive(true);
        }

        enabled = false;
    }
}
