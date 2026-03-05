using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Roblox-style third-person orbit camera.
///
/// - Follows the player from behind at configurable distance/height.
/// - Orbit: right-click drag (editor) or touch drag on right half (mobile).
/// - Smooth position follow with configurable damping.
/// - Simple wall collision avoidance (raycast).
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Orbit")]
    [SerializeField] private float distance = 7f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float height = 2f;
    [SerializeField] private float orbitSensitivity = 4f;
    [SerializeField] private float minPitch = -15f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Smoothing")]
    [SerializeField] private float positionSmooth = 8f;
    [SerializeField] private float lookSmooth = 12f;

    [Header("Wall Avoidance")]
    [SerializeField] private float wallOffset = 0.3f;
    [SerializeField] private LayerMask wallLayers = ~0;

    // ── Internal ──
    private float yaw;
    private float pitch = 20f;
    private int orbitFingerId = -1;
    private Vector2 lastTouchPos;

    void Start()
    {
        if (target != null)
            yaw = target.eulerAngles.y;

        // Lock cursor in editor for better mouse control
        #if UNITY_EDITOR
        // Don't lock — we use right-click to orbit
        #endif
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleOrbitInput();

        // ── Compute desired position ──
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = orbitRotation * Vector3.back * distance;
        Vector3 lookPoint = target.position + Vector3.up * height;
        Vector3 desiredPos = lookPoint + offset;

        // ── Wall avoidance ──
        float actualDist = distance;
        RaycastHit hit;
        if (Physics.Raycast(lookPoint, (desiredPos - lookPoint).normalized,
            out hit, distance, wallLayers))
        {
            actualDist = Mathf.Max(minDistance, hit.distance - wallOffset);
            desiredPos = lookPoint + orbitRotation * Vector3.back * actualDist;
        }

        // ── Smooth follow ──
        transform.position = Vector3.Lerp(transform.position, desiredPos,
            Time.deltaTime * positionSmooth);

        // ── Smooth look at target ──
        Quaternion desiredLook = Quaternion.LookRotation(lookPoint - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredLook,
            Time.deltaTime * lookSmooth);
    }

    void HandleOrbitInput()
    {
        // ── Orbit: right-click drag OR middle-click drag OR Alt+left-click ──
        bool orbitActive = Input.GetMouseButton(1)                           // right click
                        || Input.GetMouseButton(2)                           // middle click (3-finger tap on trackpad)
                        || (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0));  // Alt + left click

        if (orbitActive)
        {
            yaw   += Input.GetAxis("Mouse X") * orbitSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * orbitSensitivity;
            pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // ── Keyboard orbit fallback: Q/E to rotate camera ──
        if (Input.GetKey(KeyCode.Q))
            yaw -= orbitSensitivity * 15f * Time.deltaTime;
        if (Input.GetKey(KeyCode.E))
            yaw += orbitSensitivity * 15f * Time.deltaTime;

        // ── Mobile: touch on right half of screen ──
        HandleTouchOrbit();
    }

    void HandleTouchOrbit()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            // Start tracking a touch on the right half (not over UI)
            if (touch.phase == TouchPhase.Began &&
                touch.position.x > Screen.width * 0.4f)
            {
                if (EventSystem.current != null &&
                    EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    continue;

                orbitFingerId = touch.fingerId;
                lastTouchPos = touch.position;
            }

            if (touch.fingerId != orbitFingerId) continue;

            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.position - lastTouchPos;
                yaw   += delta.x * orbitSensitivity * 0.08f;
                pitch -= delta.y * orbitSensitivity * 0.08f;
                pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);
                lastTouchPos = touch.position;
            }

            if (touch.phase == TouchPhase.Ended ||
                touch.phase == TouchPhase.Canceled)
            {
                orbitFingerId = -1;
            }
        }
    }

    /// <summary>
    /// Snap camera behind a target instantly (e.g., on respawn).
    /// </summary>
    public void SnapBehindTarget()
    {
        if (target == null) return;
        yaw = target.eulerAngles.y;
        pitch = 20f;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 lookPoint = target.position + Vector3.up * height;
        transform.position = lookPoint + rot * Vector3.back * distance;
        transform.LookAt(lookPoint);
    }

    /// <summary>
    /// Set the follow target at runtime.
    /// </summary>
    public void SetTarget(Transform t)
    {
        target = t;
        if (t != null) SnapBehindTarget();
    }
}
