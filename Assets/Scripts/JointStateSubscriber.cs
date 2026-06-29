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
    [SerializeField] private GameObject robotCalibratingUI;
    private bool wasCalibrated = false;
    private float calibratedTime = 0f;
    private bool isReadyToMap = false;

    [Header("Drive Settings")]
    [SerializeField] private float driveStiffness = 3000f;
    [SerializeField] private float driveDamping = 500f;
    [SerializeField] private float driveForceLimit = 1000f;

    [Header("No-Data Timeout")]
    [SerializeField] private float noDataTimeout = 0.5f;
    private float lastMessageTime = 0f;
    private bool isDrivesFrozen = false;
    private const float frozenDamping = 50f;

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

        // Enforce stable PD gains on all joints at runtime.
        // Prismatic (gripper) joints are imported with stiffness=0 by the URDF importer;
        // revolute joints may have stale scene-file values. Setting all drives here guarantees
        // consistent, numerically stable parameters regardless of what the scene file contains.
        foreach (var kvp in jointMap)
        {
            ArticulationBody ab = kvp.Value;
            var drive = ab.xDrive;
            drive.stiffness = driveStiffness;
            drive.damping = driveDamping;
            drive.forceLimit = driveForceLimit;
            ab.xDrive = drive;
        }
    }

    void Update()
    {
        if (!pubtest.isCalibrated)
        {
            wasCalibrated = false;
            if (isReadyToMap && robotCalibratingUI != null)
                robotCalibratingUI.SetActive(true);
            isReadyToMap = false;
            isDrivesFrozen = false;
            lastMessageTime = 0f;
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
                    if (!isReadyToMap)
                    {
                        isReadyToMap = true;
                        if (robotCalibratingUI != null)
                            robotCalibratingUI.SetActive(false);
                    }
                }
            }
            else
            {
                if (!isReadyToMap)
                {
                    isReadyToMap = true;
                    if (robotCalibratingUI != null)
                        robotCalibratingUI.SetActive(false);
                }
            }

            // No-data deadman: if ready but no message has arrived within the timeout,
            // zero revolute stiffness so the arm holds position via damping only.
            // Gravity is disabled on all links, so zero stiffness is safe (no sag).
            if (isReadyToMap && (Time.time - lastMessageTime > noDataTimeout))
            {
                if (!isDrivesFrozen)
                {
                    FreezeRevoluteDrives();
                    isDrivesFrozen = true;
                }
            }
        }
    }

    void JointStateCallback(JointStateMsg msg)
    {
        if (!isReadyToMap) return;

        lastMessageTime = Time.time;

        if (isDrivesFrozen)
        {
            RestoreRevoluteDrives();
            isDrivesFrozen = false;
        }

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

    private void FreezeRevoluteDrives()
    {
        foreach (var kvp in jointMap)
        {
            ArticulationBody ab = kvp.Value;
            if (ab.jointType == ArticulationJointType.RevoluteJoint)
            {
                var drive = ab.xDrive;
                drive.stiffness = 0f;
                drive.damping = frozenDamping;
                ab.xDrive = drive;
            }
        }
        Debug.LogWarning("[JointStateSubscriber] No data for " + noDataTimeout + "s — revolute drives frozen (stiffness=0, damping=" + frozenDamping + ").");
    }

    private void RestoreRevoluteDrives()
    {
        foreach (var kvp in jointMap)
        {
            ArticulationBody ab = kvp.Value;
            if (ab.jointType == ArticulationJointType.RevoluteJoint)
            {
                var drive = ab.xDrive;
                drive.stiffness = driveStiffness;
                drive.damping = driveDamping;
                drive.forceLimit = driveForceLimit;
                ab.xDrive = drive;
            }
        }
        Debug.Log("[JointStateSubscriber] Data received — revolute drives restored (stiffness=" + driveStiffness + ", damping=" + driveDamping + ").");
    }

}
