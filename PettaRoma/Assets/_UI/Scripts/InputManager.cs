using UnityEngine;

/// <summary>
/// Central input abstraction.
/// Reads keyboard/mouse in editor, receives virtual touch input from UI controls.
/// All other scripts read from InputManager.Instance.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // ── Continuous state ──
    public Vector2 MoveInput { get; private set; }
    public bool SprintHeld { get; private set; }

    // ── Camera orbit delta (degrees) ──
    public Vector2 CameraDelta { get; private set; }

    // ── One-shot flags (consumed by readers) ──
    private bool _jumpFlag;
    private bool _attackFlag;
    private bool _interactFlag;

    // ── Virtual input from touch UI ──
    private Vector2 _virtualMove;
    private bool _virtualSprint;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Called by VirtualJoystick ──
    public void SetVirtualMove(Vector2 v) { _virtualMove = v; }

    // ── Called by VirtualButton ──
    public void TriggerJump()    { _jumpFlag = true; }
    public void TriggerAttack()  { _attackFlag = true; }
    public void TriggerInteract(){ _interactFlag = true; }
    public void SetSprint(bool v){ _virtualSprint = v; }

    // ── Called by CameraFollow for touch orbit ──
    public void SetCameraDelta(Vector2 d) { CameraDelta = d; }

    // ── Consumed by PlayerController (auto-resets) ──
    public bool ConsumeJump()
    {
        bool v = _jumpFlag;
        _jumpFlag = false;
        return v;
    }

    public bool ConsumeAttack()
    {
        bool v = _attackFlag;
        _attackFlag = false;
        return v;
    }

    public bool ConsumeInteract()
    {
        bool v = _interactFlag;
        _interactFlag = false;
        return v;
    }

    void Update()
    {
        // ── Keyboard movement (editor fallback) ──
        float h = Input.GetAxisRaw("Horizontal");
        float vert = Input.GetAxisRaw("Vertical");
        Vector2 kbMove = new Vector2(h, vert);
        if (kbMove.sqrMagnitude > 1f) kbMove.Normalize();

        // Virtual joystick overrides keyboard when active
        MoveInput = _virtualMove.sqrMagnitude > 0.01f ? _virtualMove : kbMove;

        // ── Keyboard buttons ──
        if (Input.GetKeyDown(KeyCode.Space)) _jumpFlag = true;
        if (Input.GetKeyDown(KeyCode.J))     _attackFlag = true;
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F)) _interactFlag = true;
        SprintHeld = Input.GetKey(KeyCode.LeftShift) || _virtualSprint;

        // ── Mouse camera (editor: right-click drag) ──
        if (Input.GetMouseButton(1))
        {
            CameraDelta = new Vector2(
                Input.GetAxis("Mouse X") * 5f,
                Input.GetAxis("Mouse Y") * 5f
            );
        }
        else if (_virtualMove.sqrMagnitude < 0.01f)
        {
            // Reset camera delta when no virtual orbit input
            // (touch orbit is set externally by CameraFollow)
        }
    }

    void LateUpdate()
    {
        // Reset camera delta after CameraFollow has consumed it
        if (!Input.GetMouseButton(1))
            CameraDelta = Vector2.zero;
    }
}
