using UnityEngine;

public class PlatformStickyForce : MonoBehaviour
{
    [SerializeField] private float stickyForce = 20f; // Adjust based on platform speed
    private Rigidbody playerRb;

    //private void OnCollisionEnter(Collision collision)
    //{
    //    // Check if the object has a Rigidbody (the player)
    //    if (collision.gameObject.CompareTag("Player"))
    //    {
    //        playerRb = collision.rigidbody;
    //    }
    //}

    //private void OnCollisionExit(Collision collision)
    //{
    //    if (collision.gameObject.CompareTag("Player"))
    //    {
    //        playerRb = null;
    //    }
    //}

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object has a Rigidbody (the player)
        if (other.gameObject.CompareTag("Player"))
        {
            playerRb = other.attachedRigidbody;
        }
    }

   private void OnTriggerExit(Collider other)
   {
        if (other.gameObject.CompareTag("Player"))
        {
            playerRb = null;
        }
    }

    private void FixedUpdate()
    {
        // If the player is on us, apply a downward force relative to the platform
        if (playerRb != null)
        {
            // ForceMode.Acceleration ignores mass, making it consistent
            // regardless of player weight.
            playerRb.AddForce(Vector3.down * stickyForce, ForceMode.Acceleration);
        }
    }
}