using UnityEngine;

public class RobotTargetVisualizer : MonoBehaviour
{
    [Header("Controller Tracking References")]
    [Tooltip("The robot_barrier_detector object (with its child trigger points).")]
    public Transform robotBarrierDetector;

    [Tooltip("The sgr532/link_grasping_frame object on the robot.")]
    public Transform linkGraspingFrame;

    [Tooltip("The right VR controller.")]
    public Transform rightController;

    [Header("Barrier Reference")]
    [Tooltip("The RobotBarrier component whose child colliders define the restricted zones.")]
    public RobotBarrier robotBarrier;

    [Header("Settings (Optional)")]
    [Tooltip("The saved local position offset relative to link_grasping_frame.")]
    [SerializeField] private Vector3 localPositionOffset;

    [Tooltip("The saved local rotation offset relative to link_grasping_frame.")]
    [SerializeField] private Quaternion localRotationOffset = Quaternion.identity;

    // Controller tracking state
    private Vector3 initialControllerPosition;
    private Vector3 initialDetectorPosition;
    private Quaternion initialControllerRotation;
    private Quaternion initialDetectorRotation;
    private bool isTracking = false;

    // World-space snapshot of the detector's position at scene start
    private Vector3 startDetectorWorldPosition;
    private Quaternion startDetectorWorldRotation;

    private float ScaleFactor => transform.parent != null ? transform.parent.localScale.x : 1f;

    // Trigger point transforms (children of robotBarrierDetector)
    private Transform triggerPt1;
    private Transform triggerPt2;
    private Transform triggerPt3;

    [ContextMenu("Save Current Relative Position")]
    public void SaveCurrentRelativePosition()
    {
        if (linkGraspingFrame != null && robotBarrierDetector != null)
        {
            localPositionOffset = linkGraspingFrame.InverseTransformPoint(robotBarrierDetector.position);
            localRotationOffset = Quaternion.Inverse(linkGraspingFrame.rotation) * robotBarrierDetector.rotation;
            Debug.Log("[RobotTargetVisualizer] Saved relative position and rotation offset from link_grasping_frame.");
        }
        else
        {
            Debug.LogWarning("[RobotTargetVisualizer] Please assign both linkGraspingFrame and robotBarrierDetector before saving.");
        }
    }

    void OnEnable()  { pubtest.OnResetOrigin += HandleResetOrigin; }
    void OnDisable() { pubtest.OnResetOrigin -= HandleResetOrigin; }

    void Start()
    {
        if (robotBarrierDetector == null || linkGraspingFrame == null || rightController == null)
        {
            Debug.LogError("[RobotTargetVisualizer] Missing references! Please assign RobotBarrierDetector, LinkGraspingFrame, and RightController in the inspector.");
            return;
        }

        if (robotBarrier == null)
        {
            Debug.LogError("[RobotTargetVisualizer] Missing RobotBarrier reference! Please assign it in the inspector.");
            return;
        }

        // Find trigger point children on the detector
        triggerPt1 = robotBarrierDetector.Find("Triggers").transform.Find("TriggerPt1");
        triggerPt2 = robotBarrierDetector.Find("Triggers").transform.Find("TriggerPt2");
        triggerPt3 = robotBarrierDetector.Find("Triggers").transform.Find("TriggerPt3");

        if (triggerPt1 == null) Debug.LogWarning("[RobotTargetVisualizer] Could not find child 'TriggerPt1' on robotBarrierDetector.");
        if (triggerPt2 == null) Debug.LogWarning("[RobotTargetVisualizer] Could not find child 'TriggerPt2' on robotBarrierDetector.");
        if (triggerPt3 == null) Debug.LogWarning("[RobotTargetVisualizer] Could not find child 'TriggerPt3' on robotBarrierDetector.");

        // Snap the detector to its saved relative position at startup
        robotBarrierDetector.position = linkGraspingFrame.TransformPoint(localPositionOffset);
        robotBarrierDetector.rotation = linkGraspingFrame.rotation * localRotationOffset;
        robotBarrierDetector.localScale = Vector3.one * ScaleFactor;

        // Cache world-space start position for use by the reset feature
        startDetectorWorldPosition = robotBarrierDetector.position;
        startDetectorWorldRotation = robotBarrierDetector.rotation;

        Debug.Log("[RobotTargetVisualizer] Waiting for pubtest calibration before locking controller origin...");
    }

    void Update()
    {
        // ── 1. Wait for pubtest calibration before starting controller tracking ──
        if (!isTracking)
        {
            if (!pubtest.isCalibrated) return;

            initialControllerPosition = rightController.position;
            initialDetectorPosition   = robotBarrierDetector.position;
            initialControllerRotation = rightController.rotation;
            initialDetectorRotation   = robotBarrierDetector.rotation;
            isTracking = true;

            Debug.Log("[RobotTargetVisualizer] Calibration detected. Controller origin locked. Tracking started.");
        }

        // ── 2. Map controller position and rotation delta to the detector ──
        Vector3 controllerDelta = rightController.position - initialControllerPosition;
        robotBarrierDetector.position = initialDetectorPosition + controllerDelta * ScaleFactor;
        robotBarrierDetector.localScale = Vector3.one * ScaleFactor;

        Quaternion controllerRotDelta = rightController.rotation * Quaternion.Inverse(initialControllerRotation);
        robotBarrierDetector.rotation = controllerRotDelta * initialDetectorRotation;

        // ── 3. Collect trigger point world positions ──
        Vector3[] triggerPoints = new Vector3[]
        {
            triggerPt1.position,
            triggerPt2.position,
            triggerPt3.position,
            //triggerPt1 != null ? triggerPt1.position : Vector3.zero,
            //triggerPt2 != null ? triggerPt2.position : Vector3.zero,
            //triggerPt3 != null ? triggerPt3.position : Vector3.zero,
        };

        //Debug.Log($"X position, zero vector: {triggerPoints[0] == Vector3.zero}");

        // ── 4. Delegate barrier detection to RobotBarrier ──
        // RobotBarrier.CheckPoints() finds all child colliders, checks each point against
        // each collider using bounds.Contains(), respects the robot_barrier /
        // robot_barrier_inverted tags, updates RobotBarrier.isCollisionActive, and returns
        // whether any violation is currently active.
        robotBarrier.CheckPoints(triggerPoints);
    }

    void HandleResetOrigin()
    {
        robotBarrierDetector.position = startDetectorWorldPosition;
        robotBarrierDetector.rotation = startDetectorWorldRotation;

        initialControllerPosition = rightController.position;
        initialDetectorPosition   = robotBarrierDetector.position;
        initialControllerRotation = rightController.rotation;
        initialDetectorRotation   = robotBarrierDetector.rotation;

        Debug.Log("[RobotTargetVisualizer] Origin reset. Detector snapped to scene-start position.");
    }
}
