using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Sensor;

public class JointStateSubscriber : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "/sgr532/joint_states";

    // Map joint names to GameObjects
    private Dictionary<string, ArticulationBody> jointMap = new Dictionary<string, ArticulationBody>();

    [Header("Calibration Settings")]
    public bool useBufferCalibrationDelay = true;

    [SerializeField] private float calibrationWaitTime = 2.0f;
    private bool wasCalibrated = false;
    private float calibratedTime = 0f;
    private bool isReadyToMap = false;

    // ROS to Unity joint name mapping
    private string[] rosJointNames = new string[] {
        "joint1", "joint2", "joint3", "joint4", "joint5", "joint6", "joint_gripper_right", "joint_gripper_left"
    };
    private string[] unityJointNames = new string[] {
        "sgr532/link1", "sgr532/link2", "sgr532/link3",
        "sgr532/link4", "sgr532/link5", "sgr532/link6", "sgr532/link_gripper_right", "sgr532/link_gripper_left"
    };

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<JointStateMsg>(topicName, JointStateCallback);
        Debug.Log("Subscribing to topic: " + topicName);


        // Initialize the joint map
        for (int i = 0; i < rosJointNames.Length; i++)
        {
            string unityName = unityJointNames[i];
            GameObject jointObj = GameObject.Find(unityName);
            if (jointObj != null)
            {
                ArticulationBody ab = jointObj.GetComponent<ArticulationBody>();
                if (ab != null)
                {
                    jointMap[rosJointNames[i]] = ab;
                }
                else
                {
                    Debug.LogWarning($"No ArticulationBody found on {unityName}");
                }
            }
            else
            {
                Debug.LogWarning($"GameObject not found: {unityName}");
            }
        }

        // Prismatic joints (grippers) are imported without stiffness/damping, so set them here.
        // Without stiffness > 0, xDrive.target has no physical effect.
        foreach (var kvp in jointMap)
        {
            ArticulationBody ab = kvp.Value;
            if (ab.jointType == ArticulationJointType.PrismaticJoint)
            {
                var drive = ab.xDrive;
                drive.stiffness = 10000f;
                drive.damping = 100f;
                ab.xDrive = drive;
            }
        }
    }

    void Update()
    {
        if (!pubtest.isCalibrated)
        {
            wasCalibrated = false;
            isReadyToMap = false;
        }
        else
        {
            if (!wasCalibrated)
            {
                wasCalibrated = true;
                calibratedTime = Time.time;
            }

            if (useBufferCalibrationDelay)
            {
                if (Time.time - calibratedTime >= calibrationWaitTime)
                {
                    isReadyToMap = true;
                }
            }
            else
            {
                isReadyToMap = true;
            }
        }
    }

    void JointStateCallback(JointStateMsg msg)
    {
        if (!isReadyToMap) return;

        Debug.Log($"[ROS to Unity] Received JointState with {msg.name.Length} joints");

        for (int i = 0; i < msg.name.Length; i++)
        {
            string jointName = msg.name[i];

            if (!jointMap.ContainsKey(jointName))
            {
                Debug.LogWarning($"Joint name not mapped: {jointName}");
                continue;
            }

            ArticulationBody joint = jointMap[jointName];

            // Prismatic joints (grippers) publish positions in meters; revolute joints in radians.
            // Unity xDrive.target expects meters for prismatic and degrees for revolute.
            float jointPosition = joint.jointType == ArticulationJointType.PrismaticJoint
                ? (float)msg.position[i]
                : (float)msg.position[i] * Mathf.Rad2Deg;

            //Debug.Log($"Updating {jointName} | Target: {jointPosition}");

            var drive = joint.xDrive;
            drive.target = jointPosition;
            joint.xDrive = drive;
        }
    }

}
