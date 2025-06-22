using UnityEngine;
using UnityEngine.InputSystem;

public class TweezerController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField]
    private InputActionReference _activateAction;

    [Header("Tweezers")]
    [SerializeField]
    private Transform _leftArmPivot;
    [SerializeField]
    private Transform _rightArmPivot;
    [SerializeField]
    private Transform _leftArm;
    [SerializeField]
    private Transform _rightArm;

    [Header("Settings")]
    [SerializeField]
    private float _openAngle = 20f;
    [SerializeField]
    private float _closedAngle = 2f;
    [SerializeField]
    private float _speed = 3f;

    [Header("Fix Structure")]
    [SerializeField]
    private bool _autoFixStructure = true;

    private bool _isClosing = false;
    private GameObject _grabbedBall;

    void Start()
    {
        if (_autoFixStructure)
        {
            FixTweezerStructure();
        }

        SetupColliders();

        _activateAction.action.Enable();
        _activateAction.action.performed += ctx => _isClosing = true;
        _activateAction.action.canceled += ctx => { _isClosing = false; ReleaseBall(); };

        Debug.Log("Tweezer structure has been fixed!");
    }

    private void FixTweezerStructure()
    {
        Debug.Log("=== Fixing Tweezer Structure ===");

        // 1. Set both pivots at the same back contact point
        Vector3 backPoint = transform.TransformPoint(0, 0, -0.08f);
        _leftArmPivot.position = backPoint;
        _rightArmPivot.position = backPoint;

        Debug.Log($"Back contact point set to: {backPoint}");

        // 2. Set left arm initial angle and position
        _leftArmPivot.localRotation = Quaternion.Euler(90, 0, -_openAngle);
        _leftArm.localPosition = new Vector3(0, 0.06f, 0);  // Forward relative to pivot

        // 3. Set right arm initial angle and position  
        _rightArmPivot.localRotation = Quaternion.Euler(90, 0, _openAngle);
        _rightArm.localPosition = new Vector3(0, 0.06f, 0);  // Forward relative to pivot

        // 4. Verify fix results
        Vector3 newLeftTip = _leftArm.TransformPoint(0, 0.06f, 0);
        Vector3 newRightTip = _rightArm.TransformPoint(0, 0.06f, 0);
        Vector3 newLeftBack = _leftArm.TransformPoint(0, -0.06f, 0);
        Vector3 newRightBack = _rightArm.TransformPoint(0, -0.06f, 0);

        float backDist = Vector3.Distance(newLeftBack, newRightBack);
        float tipDist = Vector3.Distance(newLeftTip, newRightTip);

        Debug.Log($"After fix - Back distance: {backDist:F3}, Tip distance: {tipDist:F3}");

        if (backDist < tipDist)
        {
            Debug.Log("✓ Successfully fixed to V-shaped tweezers!");
        }
        else
        {
            Debug.LogError("✗ Fix failed, please adjust manually");
        }
    }

    private void SetupColliders()
    {
        SetupArmCollider(_leftArm);
        SetupArmCollider(_rightArm);
    }

    private void SetupArmCollider(Transform arm)
    {
        // Clear old colliders
        Collider[] old = arm.GetComponents<Collider>();
        foreach (var c in old) DestroyImmediate(c);

        // Add precise collider
        CapsuleCollider col = arm.gameObject.AddComponent<CapsuleCollider>();
        col.radius = 0.02f;     // Slightly larger than cylinder
        col.height = 0.12f;     // Full length
        col.direction = 1;      // Y-axis
        col.center = Vector3.zero;
        col.isTrigger = false;

        // Rigidbody
        Rigidbody rb = arm.GetComponent<Rigidbody>();
        if (rb == null) rb = arm.gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Update()
    {
        UpdateTweezerAnimation();

        if (_isClosing && _grabbedBall == null)
        {
            TryGrabBall();
        }

        if (_grabbedBall != null)
        {
            MaintainGrip();
        }
    }

    private void UpdateTweezerAnimation()
    {
        float leftTarget = _isClosing ? -_closedAngle : -_openAngle;
        float rightTarget = _isClosing ? _closedAngle : _openAngle;

        Quaternion leftRot = Quaternion.Euler(90, 0, leftTarget);
        Quaternion rightRot = Quaternion.Euler(90, 0, rightTarget);

        _leftArmPivot.localRotation = Quaternion.Slerp(_leftArmPivot.localRotation, leftRot, Time.deltaTime * _speed);
        _rightArmPivot.localRotation = Quaternion.Slerp(_rightArmPivot.localRotation, rightRot, Time.deltaTime * _speed);
    }

    private void TryGrabBall()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, 0.2f);

        foreach (var col in nearby)
        {
            if (col.CompareTag("Grabbable"))
            {
                Vector3 ballPos = col.transform.position;
                Vector3 leftTip = _leftArm.TransformPoint(0, 0.06f, 0);
                Vector3 rightTip = _rightArm.TransformPoint(0, 0.06f, 0);

                float leftDist = Vector3.Distance(leftTip, ballPos);
                float rightDist = Vector3.Distance(rightTip, ballPos);

                if (leftDist < 0.06f && rightDist < 0.06f)
                {
                    GrabBall(col.gameObject);
                    Debug.Log($"Ball grabbed! Left dist: {leftDist:F3}, Right dist: {rightDist:F3}");
                    break;
                }
            }
        }
    }

    private void GrabBall(GameObject ball)
    {
        _grabbedBall = ball;
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    private void MaintainGrip()
    {
        Vector3 leftTip = _leftArm.TransformPoint(0, 0.06f, 0);
        Vector3 rightTip = _rightArm.TransformPoint(0, 0.06f, 0);
        Vector3 center = (leftTip + rightTip) * 0.5f;
        _grabbedBall.transform.position = center;
    }

    private void ReleaseBall()
    {
        if (_grabbedBall != null)
        {
            Rigidbody rb = _grabbedBall.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;
            _grabbedBall = null;
        }
    }

    void OnDrawGizmos()
    {
        if (_leftArm == null || _rightArm == null) return;

        Vector3 leftTip = _leftArm.TransformPoint(0, 0.06f, 0);
        Vector3 rightTip = _rightArm.TransformPoint(0, 0.06f, 0);

        // Back contact point
        if (_leftArmPivot != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_leftArmPivot.position, 0.02f);
        }

        // Tweezer tips
        Gizmos.color = _grabbedBall != null ? Color.green : Color.red;
        Gizmos.DrawWireSphere(leftTip, 0.025f);
        Gizmos.DrawWireSphere(rightTip, 0.025f);

        // V-shape structure lines
        Gizmos.color = Color.yellow;
        if (_leftArmPivot != null)
        {
            Gizmos.DrawLine(_leftArmPivot.position, leftTip);
            Gizmos.DrawLine(_rightArmPivot.position, rightTip);
        }

        // Tip connection line
        Gizmos.color = Color.white;
        Gizmos.DrawLine(leftTip, rightTip);

        // Detection range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }

    void OnDestroy()
    {
        _activateAction?.action?.Disable();
    }
}