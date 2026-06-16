using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Logs all XR controller inputs every frame (when non-zero) and on button press edges.
/// Attach to any active GameObject. Remove or disable when done debugging.
/// </summary>
public class XRInputDebugger : MonoBehaviour
{
    [Tooltip("Only log analog values above this threshold to reduce noise.")]
    [SerializeField] private float analogLogThreshold = 0.05f;

    // --- Analog axes ---
    [SerializeField] private bool enableDebugger = true;

    
private InputAction _leftTrigger;
    private InputAction _rightTrigger;
    private InputAction _leftGrip;
    private InputAction _rightGrip;
    private InputAction _leftStick;
    private InputAction _rightStick;

    // --- Buttons (logged on press edge only) ---
    private InputAction _leftPrimary;    // A / X
    private InputAction _rightPrimary;   // A
    private InputAction _leftSecondary;  // B / Y
    private InputAction _rightSecondary; // B
    private InputAction _leftMenu;
    private InputAction _rightMenu;
    private InputAction _leftStickClick;
    private InputAction _rightStickClick;
    private InputAction _leftTriggerBtn;
    private InputAction _rightTriggerBtn;
    private InputAction _leftGripBtn;
    private InputAction _rightGripBtn;

    void Awake()
    {
        // Analog
        _leftTrigger    = Bind("LeftTriggerValue",    "{LeftHand}/trigger");
        _rightTrigger   = Bind("RightTriggerValue",   "{RightHand}/trigger");
        _leftGrip       = Bind("LeftGripValue",       "{LeftHand}/grip");
        _rightGrip      = Bind("RightGripValue",      "{RightHand}/grip");
        _leftStick      = Bind2D("LeftStick",         "{LeftHand}/thumbstick");
        _rightStick     = Bind2D("RightStick",        "{RightHand}/thumbstick");

        // Buttons — press callbacks
        _leftPrimary     = BindBtn("LeftPrimaryButton",    "{LeftHand}/primaryButton");
        _rightPrimary    = BindBtn("RightPrimaryButton",   "{RightHand}/primaryButton");
        _leftSecondary   = BindBtn("LeftSecondaryButton",  "{LeftHand}/secondaryButton");
        _rightSecondary  = BindBtn("RightSecondaryButton", "{RightHand}/secondaryButton");
        _leftMenu        = BindBtn("LeftMenuButton",       "{LeftHand}/menuButton");
        _rightMenu       = BindBtn("RightMenuButton",      "{RightHand}/menuButton");
        _leftStickClick  = BindBtn("LeftStickClick",       "{LeftHand}/thumbstickClicked");
        _rightStickClick = BindBtn("RightStickClick",      "{RightHand}/thumbstickClicked");
        _leftTriggerBtn  = BindBtn("LeftTriggerButton",    "{LeftHand}/triggerPressed");
        _rightTriggerBtn = BindBtn("RightTriggerButton",   "{RightHand}/triggerPressed");
        _leftGripBtn     = BindBtn("LeftGripButton",       "{LeftHand}/gripPressed");
        _rightGripBtn    = BindBtn("RightGripButton",      "{RightHand}/gripPressed");

        EnableAll();
        RegisterCallbacks();
    }

    void OnDestroy()
    {
        DisableAll();
    }

    void Update()
    {
        // Analog — log any non-trivial value every frame
        LogAnalog("L.Trigger", _leftTrigger.ReadValue<float>());
        LogAnalog("R.Trigger", _rightTrigger.ReadValue<float>());
        LogAnalog("L.Grip",    _leftGrip.ReadValue<float>());
        LogAnalog("R.Grip",    _rightGrip.ReadValue<float>());
        LogAnalog2D("L.Stick", _leftStick.ReadValue<Vector2>());
        LogAnalog2D("R.Stick", _rightStick.ReadValue<Vector2>());
    }

    // --- Helpers ---

    void LogAnalog(string name, float val)
    {
        if (enableDebugger && Mathf.Abs(val) > analogLogThreshold)
            Debug.Log($"[XRInput] {name}: {val:F3}");
    }

    void LogAnalog2D(string name, Vector2 val)
    {
        if (enableDebugger && val.magnitude > analogLogThreshold)
            Debug.Log($"[XRInput] {name}: ({val.x:F3}, {val.y:F3})");
    }

    InputAction Bind(string name, string path) =>
        new InputAction(name, binding: $"<XRController>{path}");

    InputAction Bind2D(string name, string path)
    {
        var a = new InputAction(name, type: InputActionType.Value, expectedControlType: "Vector2");
        a.AddBinding($"<XRController>{path}");
        return a;
    }

    InputAction BindBtn(string name, string path) =>
        new InputAction(name, binding: $"<XRController>{path}", interactions: "press");

    void EnableAll()
    {
        _leftTrigger.Enable();    _rightTrigger.Enable();
        _leftGrip.Enable();       _rightGrip.Enable();
        _leftStick.Enable();      _rightStick.Enable();
        _leftPrimary.Enable();    _rightPrimary.Enable();
        _leftSecondary.Enable();  _rightSecondary.Enable();
        _leftMenu.Enable();       _rightMenu.Enable();
        _leftStickClick.Enable(); _rightStickClick.Enable();
        _leftTriggerBtn.Enable(); _rightTriggerBtn.Enable();
        _leftGripBtn.Enable();    _rightGripBtn.Enable();
    }

    void DisableAll()
    {
        _leftTrigger.Disable();    _rightTrigger.Disable();
        _leftGrip.Disable();       _rightGrip.Disable();
        _leftStick.Disable();      _rightStick.Disable();
        _leftPrimary.Disable();    _rightPrimary.Disable();
        _leftSecondary.Disable();  _rightSecondary.Disable();
        _leftMenu.Disable();       _rightMenu.Disable();
        _leftStickClick.Disable(); _rightStickClick.Disable();
        _leftTriggerBtn.Disable(); _rightTriggerBtn.Disable();
        _leftGripBtn.Disable();    _rightGripBtn.Disable();
    }

    void RegisterCallbacks()
    {
        Register(_leftPrimary,    "L.PrimaryButton");
        Register(_rightPrimary,   "R.PrimaryButton");
        Register(_leftSecondary,  "L.SecondaryButton");
        Register(_rightSecondary, "R.SecondaryButton");
        Register(_leftMenu,       "L.Menu");
        Register(_rightMenu,      "R.Menu");
        Register(_leftStickClick, "L.StickClick");
        Register(_rightStickClick,"R.StickClick");
        Register(_leftTriggerBtn, "L.TriggerBtn");
        Register(_rightTriggerBtn,"R.TriggerBtn");
        Register(_leftGripBtn,    "L.GripBtn");
        Register(_rightGripBtn,   "R.GripBtn");
    }

void Register(InputAction action, string label)
    {
        action.performed += ctx => { if (enableDebugger) Debug.Log("[XRInput] " + label + " PRESSED"); };
        action.canceled  += ctx => { if (enableDebugger) Debug.Log("[XRInput] " + label + " released"); };
    }
}
