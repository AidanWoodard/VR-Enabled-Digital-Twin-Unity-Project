using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hold both controller triggers for holdDuration seconds to toggle ROS publishing on/off.
/// Attach to any active GameObject in the scene.
/// </summary>
public class ROSPublishToggle : MonoBehaviour
{
    public static bool IsPublishingEnabled = true;

    [Tooltip("Seconds both triggers must be held to toggle publishing.")]
    [SerializeField] private float holdDuration = 1.0f;

    [Tooltip("Analog threshold (0-1) above which a trigger counts as held.")]
    [SerializeField] private float triggerThreshold = 0.85f;

    [Tooltip("Log trigger values every frame for debugging.")]
    [SerializeField] private bool debugLog = true;

    private InputAction _leftTrigger;
    private InputAction _rightTrigger;

    private float _bothHeldTime = 0f;
    private bool _toggleFired = false;

    void Awake()
    {
        // InputActionType.Value so ReadValue<float>() returns the raw analog axis,
        // not a binary button state with a press threshold applied.
        _leftTrigger  = new InputAction("LeftTrigger",  type: InputActionType.Value, expectedControlType: "Axis");
        _rightTrigger = new InputAction("RightTrigger", type: InputActionType.Value, expectedControlType: "Axis");

        // Generic XRController covers most devices; ValveIndexController is the
        // specific layout SteamVR/OpenXR registers for Index controllers.
        _leftTrigger.AddBinding("<XRController>{LeftHand}/trigger");
        _leftTrigger.AddBinding("<ValveIndexController>{LeftHand}/trigger");
        _rightTrigger.AddBinding("<XRController>{RightHand}/trigger");
        _rightTrigger.AddBinding("<ValveIndexController>{RightHand}/trigger");

        _leftTrigger.Enable();
        _rightTrigger.Enable();
    }

    void OnDestroy()
    {
        _leftTrigger?.Disable();
        _rightTrigger?.Disable();
        _leftTrigger?.Dispose();
        _rightTrigger?.Dispose();
    }

    void Update()
    {
        float lVal = _leftTrigger.ReadValue<float>();
        float rVal = _rightTrigger.ReadValue<float>();
        bool bothHeld = lVal > triggerThreshold && rVal > triggerThreshold;

        if (debugLog)
            Debug.Log("[ROSPublishToggle] L=" + lVal.ToString("F2") +
                      " R=" + rVal.ToString("F2") +
                      " bothHeld=" + bothHeld +
                      " heldTime=" + _bothHeldTime.ToString("F2") +
                      " publishing=" + IsPublishingEnabled);

        if (bothHeld)
        {
            _bothHeldTime += Time.deltaTime;
            if (_bothHeldTime >= holdDuration && !_toggleFired)
            {
                IsPublishingEnabled = !IsPublishingEnabled;
                _toggleFired = true;
                Debug.Log("[ROSPublishToggle] Publishing " + (IsPublishingEnabled ? "ENABLED" : "DISABLED"));
            }
        }
        else
        {
            _bothHeldTime = 0f;
            _toggleFired = false;
        }
    }
}
