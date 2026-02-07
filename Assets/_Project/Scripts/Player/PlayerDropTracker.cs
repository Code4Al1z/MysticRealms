using UnityEngine;

public class PlayerDropTracker : MonoBehaviour
{
    public Transform LastDropPoint { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DropPoint"))
        {
            LastDropPoint = other.transform;
        }
    }
}
