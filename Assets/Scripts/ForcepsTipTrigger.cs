using UnityEngine;

public class ForcepsTipTrigger : MonoBehaviour
{
    public PusherController forcepsController;

    // RENAMED from OnTriggerEnter to OnCollisionEnter
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log(gameObject.name + " collided with " + collision.gameObject.name);

        // We now get the GameObject from the 'collision' object
        if (collision.gameObject.CompareTag("Grabbable") && forcepsController != null)
        {
            forcepsController.RegisterGrabbableObject(collision.gameObject);
        }
    }

    // RENAMED from OnTriggerExit to OnCollisionExit
    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Grabbable") && forcepsController != null)
        {
            forcepsController.UnregisterGrabbableObject(collision.gameObject);
        }
    }
}