using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.BuiltinInterfaces;
using System;

public class VRPosePublisher2 : MonoBehaviour
{
    [Header("XR Controller")]
    [Tooltip("Drag your right?hand controller Transform here")]
    [SerializeField] Transform rightController;

    [Header("ROS Settings")]
    [Tooltip("Topic to publish target end-effector poses to")]
    [SerializeField] string topicName = "/sgr532/vr_target_pose";

    [Tooltip("How many times per second to publish")]
    [SerializeField] float publishRateHz = 30f;

    ROSConnection ros;
    float timeElapsed = 0f;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseStampedMsg>(topicName);
    }

    void Update()
    {
        // throttle publish rate
        timeElapsed += Time.deltaTime;
        if (timeElapsed < 1f / publishRateHz) return;
        timeElapsed = 0f;

        PublishControllerPose();
    }

    void PublishControllerPose()
    {
        if (rightController == null)
        {
            Debug.LogWarning("VRPosePublisher: Right controller not assigned.");
            return;
        }

        // 1) Read Unity world?space pose
        Vector3 uPos = rightController.position;
        Quaternion uRot = rightController.rotation;

        // 2) Convert to ROS FLU (Forward?Left?Up) coordinate system
        var posFLU = uPos.To<FLU>();
        var rotFLU = uRot.To<FLU>();

        // 3) Build a ROS Header with the base_link frame
        double now = Time.realtimeSinceStartup;
        uint secs = (uint)Math.Floor(now);
        uint nsecs = (uint)((now - secs) * 1e9);
        var header = new HeaderMsg
        {
            stamp = new TimeMsg(secs, nsecs),
            frame_id = "sgr532/base_link"
        };

        // 4) Construct the PoseStamped message
        var pose = new PoseMsg(
            new PointMsg(posFLU.x, posFLU.y, posFLU.z),
            new QuaternionMsg(rotFLU.x, rotFLU.y, rotFLU.z, rotFLU.w)
        );
        var msg = new PoseStampedMsg(header, pose);

        // 5) Publish to ROS
        ros.Publish(topicName, msg);

        // 6) Debug log so you can see exactly what’s sent
        Debug.Log($"[VR?ROS] Pos=({posFLU.x:F3},{posFLU.y:F3},{posFLU.z:F3}) "
                  + $"Rot=({rotFLU.x:F3},{rotFLU.y:F3},{rotFLU.z:F3},{rotFLU.w:F3})");
    }
}

