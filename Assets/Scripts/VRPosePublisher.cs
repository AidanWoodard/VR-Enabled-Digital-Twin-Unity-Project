using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;                       // for HeaderMsg & TimeMsg
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.BuiltinInterfaces;
using System;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class VRPosePublisher : MonoBehaviour
{

    // Drag your XR Rig’s “Right Hand Controller (Action Based)” here:
    [SerializeField] InputActionReference aButtonAction;
    [SerializeField] Transform rightController;

    ROSConnection ros;
    public string topicName = "/sgr532/vr_target_pose";
    public float publishRateHz = 30f;
    float timeElapsed = 0f;
 


    //calibration data
    bool isCalibrated = false;
    public Vector3 controllerHomeFlu;
    public Vector3 controllerHome;
    static readonly Vector3 rosEEHomeInBaseLink = new Vector3(0.308f, 0.00045f, 0.304f);


    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseStampedMsg>(topicName);

        // ** Subscribe to the controller’s “activate” action (the A-button) **
        aButtonAction.action.performed += OnCalibrate;
    }

    void OnDestroy()
    {
        // ** Always clean up your subscription **
        aButtonAction.action.performed -= OnCalibrate;
    }

    void OnCalibrate(InputAction.CallbackContext _)
    {
        controllerHome = rightController.transform.position;

        // convert to FLU-wrapped and then pull out x,y,z into a plain Vector3
        var homeF = controllerHome.To<FLU>();
        controllerHomeFlu = new Vector3(homeF.x, homeF.y, homeF.z);

        isCalibrated = true;
        Debug.Log("Calibration complete. Controller FLU home = " + controllerHomeFlu);
    }

    void Update()
    {
        timeElapsed += Time.deltaTime;
        if (timeElapsed < 1f / publishRateHz) return;
        timeElapsed = 0f;

        if (!isCalibrated) return;
        PublishControllerPose();
    }


    void PublishControllerPose()
    {
        if (rightController == null)
        {
            Debug.LogWarning("Right controller not assigned.");
            return;
        }

        // 1. Read Unity pose
        Vector3 uPos = rightController.transform.position;
        Quaternion uRot = rightController.transform.rotation;

        // 2. Convert to ROS FLU, then extract back to Vector3
        var posF = uPos.To<FLU>();
        var rosRot = uRot.To<FLU>();
        var rosPos = new Vector3(posF.x, posF.y, posF.z);

        // 3. Offset from calibration position
        Vector3 delta = rosPos - controllerHomeFlu;
        Vector3 finalPos = rosEEHomeInBaseLink + delta;

        Debug.Log($"Publishing Pose -- Offset: {delta}, Final (Base_Link): {finalPos}");

        // 4. Build the header with time
        double now = Time.realtimeSinceStartup;
        uint secs = (uint)Math.Floor(now);
        uint nsecs = (uint)((now - secs) * 1e9);

        var header = new HeaderMsg
        {
            stamp = new TimeMsg(secs, nsecs),
            frame_id = "sgr532/base_link"
        };

        // 5. Build the Pose
        var pose = new PoseMsg(
            new PointMsg(finalPos.x, finalPos.y, finalPos.z),
            new QuaternionMsg(rosRot.x, rosRot.y, rosRot.z, rosRot.w)
        );

        // 6. Combine and publish
        var msg = new PoseStampedMsg(header, pose);
        ros.Publish(topicName, msg);
    }
}

