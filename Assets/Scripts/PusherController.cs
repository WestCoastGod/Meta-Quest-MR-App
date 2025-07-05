using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic; // This is required for the HashSet

// Make sure your file is named PusherController.cs to match this class name
public class PusherController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference selectAction;
    [SerializeField] private InputActionReference triggerAction;

    [Header("Forceps Parts")]
    [SerializeField] private Transform stem;
    [SerializeField] private Transform pusher;
    [SerializeField] private Transform leftLeg;
    [SerializeField] private Transform rightLeg;
    [SerializeField] private Transform grabPoint;

    [Header("Handle Parts")]
    [SerializeField] private Transform thumbRing; // Thumb ring
    [SerializeField] private Transform fingerRing; // Finger ring (if you want to animate it too)

    [Header("Animation Settings")]
    [SerializeField] private float stemMoveDistance = 0.02f;
    [SerializeField] private float legOpenAngle = 15f;
    [SerializeField] private float thumbRingMoveDistance = 0.03f; // Distance thumb ring moves towards finger ring
    [SerializeField] private float animationSpeed = 5f;
    [SerializeField][Range(0f, 1f)] private float holdingOpenAmount = 0.4f;

    [Header("Ball Spawning")]
    // [SerializeField] private GameObject ballPrefab;

    private GameObject _grabbedBall;
    private bool isClosing = true;
    private float currentOpenAmount = 0f;
    private HashSet<GameObject> grabbableObjectsInRange = new HashSet<GameObject>();
    private Vector3 stemStartPos, pusherStartPos, thumbRingStartPos;
    private Quaternion leftLegStartRot, rightLegStartRot;

    void Start()
    {
        if (stem != null) stemStartPos = stem.localPosition;
        if (pusher != null) pusherStartPos = pusher.localPosition;
        if (leftLeg != null) leftLegStartRot = leftLeg.localRotation;
        if (rightLeg != null) rightLegStartRot = rightLeg.localRotation;

        if (thumbRing != null)
        {
            thumbRingStartPos = thumbRing.localPosition;
            Debug.Log($"Thumb Ring Start Position: {thumbRingStartPos}");
        }
        else
        {
            Debug.LogWarning("Thumb Ring Transform is not assigned!");
        }

        if (selectAction != null)
        {
            selectAction.action.Enable();
            selectAction.action.performed += OnSelectPressed;
            selectAction.action.canceled += OnSelectReleased;
        }
        if (triggerAction != null)
        {
            triggerAction.action.Enable();
            triggerAction.action.performed += OnTriggerPressed;
        }
    }

    void Update()
    {
        float targetOpenAmount = isClosing ? (_grabbedBall != null ? holdingOpenAmount : 0f) : 1f;
        currentOpenAmount = Mathf.MoveTowards(currentOpenAmount, targetOpenAmount, animationSpeed * Time.deltaTime);
        AnimateForceps();

        if (_grabbedBall != null && grabPoint != null)
        {
            _grabbedBall.transform.position = grabPoint.position;
        }
    }

    // --- The two missing methods are now here and are public ---

    public void RegisterGrabbableObject(GameObject obj)
    {
        grabbableObjectsInRange.Add(obj);
    }

    public void UnregisterGrabbableObject(GameObject obj)
    {
        grabbableObjectsInRange.Remove(obj);
    }

    // --- End of the two missing methods ---

    private void TryGrab()
    {
        GameObject closestBall = null;
        float minDistance = float.MaxValue;

        foreach (GameObject ball in grabbableObjectsInRange)
        {
            if (ball == null) continue;
            float distance = Vector3.Distance(grabPoint.position, ball.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestBall = ball;
            }
        }

        if (closestBall != null)
        {
            GrabBall(closestBall);
        }
    }

    private void GrabBall(GameObject ball)
    {
        if (_grabbedBall != null) return;
        _grabbedBall = ball;
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        Debug.Log("Ball Grabbed: " + ball.name);
    }

    private void ReleaseBall()
    {
        if (_grabbedBall == null) return;
        Rigidbody rb = _grabbedBall.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;
        Debug.Log("Ball Released: " + _grabbedBall.name);
        _grabbedBall = null;
    }

    private void OnSelectPressed(InputAction.CallbackContext context)
    {
        isClosing = false;
        ReleaseBall();
        Debug.Log("Forceps opening - thumb ring should move towards finger ring");
    }

    private void OnSelectReleased(InputAction.CallbackContext context)
    {
        isClosing = true;
        TryGrab();
        Debug.Log("Forceps closing - thumb ring should return to original position");
    }

    private void OnTriggerPressed(InputAction.CallbackContext context)
    {
        // Ball spawning functionality commented out
        // if (ballPrefab != null && grabPoint != null)
        // {
        //     Instantiate(ballPrefab, grabPoint.position, Quaternion.identity);
        // }
    }

    private void AnimateForceps()
    {
        if (stem != null) stem.localPosition = stemStartPos + Vector3.forward * stemMoveDistance * currentOpenAmount;
        if (pusher != null) pusher.localPosition = pusherStartPos + Vector3.forward * stemMoveDistance * 0.8f * currentOpenAmount;
        if (leftLeg != null) leftLeg.localRotation = leftLegStartRot * Quaternion.Euler(0, 0, legOpenAngle * currentOpenAmount);
        if (rightLeg != null) rightLeg.localRotation = rightLegStartRot * Quaternion.Euler(0, 0, -legOpenAngle * currentOpenAmount);

        // Animate thumb ring - moves towards finger ring when forceps open
        if (thumbRing != null)
        {
            // When forceps opens (currentOpenAmount increases), thumb ring moves towards finger ring
            // This typically means moving "down" or "forward" depending on your model orientation
            // Try different directions based on your forceps model:

            // Option 1: Move down (most common for forceps)
            //Vector3 newPosition = thumbRingStartPos + Vector3.down * thumbRingMoveDistance * currentOpenAmount;

            // Option 2: Move forward (if rings are aligned front-to-back)
            //Vector3 newPosition = thumbRingStartPos + Vector3.forward * thumbRingMoveDistance * currentOpenAmount;

            // Option 3: Move backward (if rings are aligned front-to-back, opposite direction)
            Vector3 newPosition = thumbRingStartPos + Vector3.down * thumbRingMoveDistance * currentOpenAmount;

            thumbRing.localPosition = newPosition;

            // Debug log to see if animation is working
            if (currentOpenAmount > 0.1f)
            {
                Debug.Log($"Thumb Ring Position: {thumbRing.localPosition}, OpenAmount: {currentOpenAmount}");
            }
        }
    }

    void OnDestroy()
    {
        if (selectAction != null) { selectAction.action.Disable(); }
        if (triggerAction != null) { triggerAction.action.Disable(); }
    }
}