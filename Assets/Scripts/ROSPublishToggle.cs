using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hold both controller triggers for holdDuration seconds to toggle ROS publishing on/off.
/// Uses generic XR controller bindings — works with any OpenXR-compatible controller.
/// Attach this to any active GameObject in the scene.
/// </summary>
public class ROSPublishToggle : MonoBehaviour
{
    public static bool IsPublishingEnabled = true;

    [Tooltip("Seconds both triggers must be held to toggle publishing.")]
    [SerializeField] private float holdDuration = 1.0f;

    [Tooltip("Analog threshold above which a trigger counts as 'pressed'.")]
    [SerializeField] private float triggerThreshold = 0.8f;

    private InputAction _leftTrigger;
    private InputAction _rightTrigger;

    private float _bothHeldTime = 0f;
    private bool _toggleFired = false;

    void Awake()
    {
        _leftTrigger  = new InputAction("LeftTrigger",  binding: "<XRController>{LeftHand}/trigger");
        _rightTrigger = new InputAction("RightTrigger", binding: "<XRController>{RightHand}/trigger");
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
        bool bothHeld = _leftTrigger.ReadValue<float>()  > triggerThreshold
                     && _rightTrigger.ReadValue<float>() > triggerThreshold;

        if (bothHeld)
        {
            _bothHeldTime += Time.deltaTime;
            if (_bothHeldTime >= holdDuration && !_toggleFired)
            {
                IsPublishingEnabled = !IsPublishingEnabled;
                _toggleFired = true;
                Debug.Log($"[ROSPublishToggle] Publishing {(IsPublishingEnabled ? "ENABLED" : "DISABLED")}");
            }
        }
        else
        {
            _bothHeldTime = 0f;
            _toggleFired = false;
        }
    }
}
