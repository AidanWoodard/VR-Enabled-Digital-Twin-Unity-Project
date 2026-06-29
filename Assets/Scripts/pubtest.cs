using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.BuiltinInterfaces;
using System;
using UnityEngine.XR;
using UnityEngine.InputSystem;

public class pubtest : MonoBehaviour
{
    [SerializeField] Transform rightController;

    [Header("Input System Action References")]
    [SerializeField] private InputActionReference aButtonAction;    // primary button
    [SerializeField] private InputActionReference bButtonAction;    // secondary button

    ROSConnection ros;
    public string topicName = "/sgr532/vr_target_pose";
    public string teachtopicname = "/sgr532/teach_pose";
    public float publishRateHz = 20f;

    float timeElapsed = 0f;
    float calibrationDelay = 10f; // wait 10 seconds
    float startTime;
    public static bool isCalibrated = false;

    Vector3 controllerHomeFlu;
    Quaternion controllerHomeRotation;

    [Header("Home Position (ROS base_link frame)")]
    [SerializeField] private Vector3 rosEEHomeInBaseLink = new Vector3(0.308f, 0.00045f, 0.304f);

    [Header("Reset Controls")]
    [SerializeField] private InputActionReference leftBButtonAction;
    [SerializeField] private float resetHoldDuration = 1f;
    private float resetHoldTimer = 0f;
    public static event System.Action OnResetOrigin;

    //gripper control (A = close, B = open, right controller only)
    bool gripperOpenFull = false;
    bool gripperClosedFull = true;
    float gripperVal = 0.0000f;
    private const float MIN_GRIPPER = -0.035f;              // Fully closed
    private const float MAX_GRIPPER = 0.0f;                 // Fully opened
    [Header("Gripper Speed")]
    [SerializeField] private float gripperSpeed = 0.0085f;

    //// save position feat. (not in use, legacy)
    //bool saveButtonPressed = false;
    //bool saveButtonJustPressed = false;
    //InputDevice rightHand;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseStampedMsg>(topicName);
        ros.RegisterPublisher<PoseStampedMsg>(teachtopicname);
        ros.RegisterPublisher<Float64Msg>("/sgr532/gripper/command");

        // reset claw
        PublishGripperCommand(MAX_GRIPPER);

        startTime = Time.time;

        ////gripper initialize
        //rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    void Update()
    {
        if (!isCalibrated && Time.time - startTime > calibrationDelay)
        {
            Debug.Log("Connected, starting to control...");
            Vector3 uHome = rightController.position;
            var homeF = uHome.To<FLU>();
            controllerHomeFlu = new Vector3(homeF.x, homeF.y, homeF.z);
            //controllerHomeRotation = rightController.rotation; // NEW
            var homeQuat = rightController.rotation.To<FLU>();
            controllerHomeRotation = new Quaternion(homeQuat.x, homeQuat.y, homeQuat.z, homeQuat.w);
            isCalibrated = true;
            //DEBUG
            //Debug.Log($"[CALIBRATED] Controller FLU home = {controllerHomeFlu}");
        }
        //gripper trigger
        CheckGripperTrigger();
        CheckResetInput();
        //CheckSaveButton();        // omit, just use live hand tracking
        if (!isCalibrated) return;

        timeElapsed += Time.deltaTime;
        if (timeElapsed < 1f / publishRateHz) return;
        timeElapsed = 0f;

        if (RobotBarrier.isCollisionActive) return;
        PublishControllerPose();
    }

    //void CheckSaveButton()
    //{
    //    bool currentState = false;
    //    rightHand.TryGetFeatureValue(CommonUsages.menuButton, out currentState);

    //    if (currentState && !saveButtonPressed)
    //    {
    //        // Rising edge: just pressed
    //        saveButtonJustPressed = true;
    //        Debug.Log("[INPUT] Save button just pressed!");
    //    }

    //    saveButtonPressed = currentState; // track previous state
    //}

    //gripper control - A button (close), B button (open), right controller only
    void CheckGripperTrigger()
    {
        if (aButtonAction == null || bButtonAction == null)
        {
            Debug.LogWarning("ERROR: Missing references to Unity Input System Action. Make sure to apply in Editor.");
            return;
        }

        bool aPressed = aButtonAction.action.IsPressed();
        bool bPressed = bButtonAction.action.IsPressed();       // (race condition handled in elif)
        float previousVal = gripperVal;

        if (aPressed && !gripperClosedFull)
        {
            gripperVal -= gripperSpeed * Time.deltaTime;
        }
        else if (bPressed && !gripperOpenFull)
        {
            gripperVal += gripperSpeed * Time.deltaTime;
        }

        //Debug.Log("[DEBUG] Gripper position: " + gripperVal);

        // Update gripper state flags
        gripperVal = Mathf.Clamp(gripperVal, MIN_GRIPPER, MAX_GRIPPER);
        gripperClosedFull = gripperVal <= MIN_GRIPPER;
        gripperOpenFull = gripperVal >= MAX_GRIPPER;

        if (!Mathf.Approximately(gripperVal, previousVal))
        {
            PublishGripperCommand(gripperVal);
        }
    }

    void CheckResetInput()
    {
        if (!isCalibrated) return;
        if (leftBButtonAction == null) return;

        bool leftB = leftBButtonAction.action.IsPressed();
        bool rightB = bButtonAction.action.IsPressed();

        if (leftB && rightB)
        {
            resetHoldTimer += Time.deltaTime;
            if (resetHoldTimer >= resetHoldDuration)
            {
                ResetControllerOrigin();
                resetHoldTimer = 0f;
            }
        }
        else
        {
            resetHoldTimer = 0f;
        }
    }

    void ResetControllerOrigin()
    {
        Vector3 uHome = rightController.position;
        var homeF = uHome.To<FLU>();
        controllerHomeFlu = new Vector3(homeF.x, homeF.y, homeF.z);
        var homeQuat = rightController.rotation.To<FLU>();
        controllerHomeRotation = new Quaternion(homeQuat.x, homeQuat.y, homeQuat.z, homeQuat.w);

        if (ROSPublishToggle.IsPublishingEnabled)
        {
            double now = Time.realtimeSinceStartup;
            uint secs = (uint)System.Math.Floor(now);
            uint nsecs = (uint)((now - secs) * 1e9);
            var header = new HeaderMsg { stamp = new TimeMsg(secs, nsecs), frame_id = "sgr532/base_link" };
            var pose = new PoseMsg(
                new PointMsg(rosEEHomeInBaseLink.x, rosEEHomeInBaseLink.y, rosEEHomeInBaseLink.z),
                new QuaternionMsg(0, 0, 0, 1)
            );
            ros.Publish(topicName, new PoseStampedMsg(header, pose));
        }

        OnResetOrigin?.Invoke();
        Debug.Log("[pubtest] Controller origin reset.");
    }

void PublishGripperCommand(float value)
    {
        if (!ROSPublishToggle.IsPublishingEnabled) return;
        if (RobotBarrier.isCollisionActive) return;

        var msg = new Float64Msg(value);
        ros.Publish("/sgr532/gripper/command", msg);
    }

void PublishControllerPose()
    {
        if (!ROSPublishToggle.IsPublishingEnabled) return;
        if (rightController == null)
        {
            Debug.LogWarning("Right controller not assigned.");
            return;
        }

        Vector3 uPos = rightController.position;
        //Quaternion currentRot = rightController.rotation; //  NEW

        var rosQuat = rightController.rotation.To<FLU>();//test
        Quaternion currentRot = new Quaternion(rosQuat.x, rosQuat.y, rosQuat.z, rosQuat.w);//test

        var posF = uPos.To<FLU>();
        Vector3 rosPos = new Vector3(posF.x, posF.y, posF.z);
        Vector3 delta = rosPos - controllerHomeFlu;
        Vector3 finalPos = rosEEHomeInBaseLink + delta;

        // Compute orientation delta from calibration
        Quaternion deltaRot = currentRot * Quaternion.Inverse(controllerHomeRotation);
        var rosRot = new Quaternion(deltaRot.x, deltaRot.y, deltaRot.z, deltaRot.w);

        double now = Time.realtimeSinceStartup;
        uint secs = (uint)Math.Floor(now);
        uint nsecs = (uint)((now - secs) * 1e9);

        var header = new HeaderMsg
        {
            stamp = new TimeMsg(secs, nsecs),
            frame_id = "sgr532/base_link"
        };

        var pose = new PoseMsg(
            new PointMsg(finalPos.x, finalPos.y, finalPos.z),
            new QuaternionMsg(rosRot.x, rosRot.y, rosRot.z, rosRot.w)
        );

        ros.Publish(topicName, new PoseStampedMsg(header, pose));
    }
}


