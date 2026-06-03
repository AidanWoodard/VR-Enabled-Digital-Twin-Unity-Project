using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.BuiltinInterfaces;
using System;
using UnityEngine.XR;

public class pubtest : MonoBehaviour
{
    [SerializeField] Transform rightController;

    ROSConnection ros;
    public string topicName = "/sgr532/vr_target_pose";
    public string teachtopicname = "/sgr532/teach_pose";
    public float publishRateHz = 20f;

    float timeElapsed = 0f;
    float calibrationDelay = 10f; // wait 10 seconds
    float startTime;
    public static bool isCalibrated = false;

    Vector3 controllerHomeFlu;
    Quaternion controllerHomeRotation; //  NEW
    static readonly Vector3 rosEEHomeInBaseLink = new Vector3(0.308f, 0.00045f, 0.304f);

    //gripper test 
    InputDevice rightHand;
    InputDevice leftHand;
    bool fullyopen = false;
    bool fullyclosed = true;
    float grippervalue = 0.0000f;
    bool rightTriggerPressed, leftTriggerPressed;
    bool rightTriggerWasPressedLastFrame = false;
    bool leftTriggerWasPressedLastFrame = false;
    bool saveButtonPressed = false;
    bool saveButtonJustPressed = false;


    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseStampedMsg>(topicName);
        ros.RegisterPublisher<PoseStampedMsg>(teachtopicname);
        ros.RegisterPublisher<Float64Msg>("/sgr532/gripper/command");

        startTime = Time.time;

        //gripper initialize
        rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
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
        CheckSaveButton();
        if (!isCalibrated) return;

        timeElapsed += Time.deltaTime;
        if (timeElapsed < 1f / publishRateHz) return;
        timeElapsed = 0f;

        if (RobotBarrier.isCollisionActive) return;
        PublishControllerPose();
    }

    void CheckSaveButton()
    {
        bool currentState = false;
        rightHand.TryGetFeatureValue(CommonUsages.menuButton, out currentState);

        if (currentState && !saveButtonPressed)
        {
            // Rising edge: just pressed
            saveButtonJustPressed = true;
            Debug.Log("[INPUT] Save button just pressed!");
        }

        saveButtonPressed = currentState; // track previous state
    }


    //gripper test
    void CheckGripperTrigger()
    {
        // Ensure both controllers are initialized
        if (!rightHand.isValid)
            rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!leftHand.isValid)
            leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        // Update gripper state flags
        fullyclosed = grippervalue <= -0.034f;
        fullyopen = grippervalue >= 0.0f;

        // RIGHT HAND trigger (close gripper)
        if (rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool rightTriggerPressed))
        {
            if (rightTriggerPressed && !rightTriggerWasPressedLastFrame && !fullyclosed)
            {
                grippervalue -= 0.0085f;
                grippervalue = Mathf.Clamp(grippervalue, -0.034f, 0.0f);
                PublishGripperCommand(grippervalue);
            }

            rightTriggerWasPressedLastFrame = rightTriggerPressed;
        }

        // LEFT HAND trigger (open gripper)
        if (leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool leftTriggerPressed))
        {
            if (leftTriggerPressed && !leftTriggerWasPressedLastFrame && !fullyopen)
            {
                grippervalue += 0.0085f;
                grippervalue = Mathf.Clamp(grippervalue, -0.034f, 0.0f);
                PublishGripperCommand(grippervalue);
            }

            leftTriggerWasPressedLastFrame = leftTriggerPressed;
        }
    }

    //gripper test
    void PublishGripperCommand(float value)
    {
        if (RobotBarrier.isCollisionActive) return;

        var msg = new Float64Msg(value);
        ros.Publish("/sgr532/gripper/command", msg);
    }


    void PublishControllerPose()
    {
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

        
        //DEBUG
        //Debug.Log($"uPos: {uPos}, FLU: {posF}");

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

        //Send Current Pose to our Teach adn Repeat Topic 'teach_pose'
        if (saveButtonJustPressed)
        {
            ros.Publish(teachtopicname, new PoseStampedMsg(header, pose));
            Debug.Log($"[TEACH] Saved pose at position {finalPos} and orientation {rosRot}");
            saveButtonJustPressed = false;
        }
    }
}


