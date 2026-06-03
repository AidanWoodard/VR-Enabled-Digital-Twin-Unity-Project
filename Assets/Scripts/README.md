# Unity ROS Scripts Documentation

This folder contains scripts used to interface the Unity XR application with a ROS backend for the SGR532 robot. Below is a summary of each script's purpose and functionality.

## Scripts Overview

### `JointStateSubscriber.cs`
- **Purpose**: Subscribes to the ROS `/sgr532/joint_states` topic and updates the Unity robot model's joints to mirror the physical (or simulated) robot in ROS.
- **Details**: Maps ROS joint names (e.g., `joint1`, `joint_gripper_right`) to the corresponding Unity `ArticulationBody` objects (e.g., `sgr532/link1`, `sgr532/link_gripper_right`). When a `JointStateMsg` is received, it updates the `xDrive.target` of each mapped articulation body using the converted joint position.

### `VRPosePublisher.cs`
- **Purpose**: Publishes the XR right controller's pose to the ROS `/sgr532/vr_target_pose` topic for tracking.
- **Details**: Includes a calibration step triggered by the 'A' button on the controller. Upon calibration, it saves the initial controller position and calculates subsequent poses as an offset relative to a hardcoded robot end-effector home pose (`rosEEHomeInBaseLink`). It converts Unity's coordinate system to ROS's FLU (Forward-Left-Up) format before publishing.

### `VRPosePublisher2.cs`
- **Purpose**: A simplified version of the VR pose publisher.
- **Details**: Directly publishes the XR controller's world-space position and rotation to the `/sgr532/vr_target_pose` topic without any offset or calibration logic. It performs the necessary conversion to ROS FLU coordinates and handles throttling to a specific publish rate.

### `pubtest.cs`
- **Purpose**: An advanced controller script that handles VR pose tracking, gripper control, and a "teach and repeat" saving feature.
- **Details**: 
  - **Pose Calibration**: Automatically calibrates the controller's home pose after a 10-second delay on startup.
  - **Gripper Control**: Uses the right and left VR controller triggers to close and open the gripper, respectively, publishing values to the `/sgr532/gripper/command` topic.
  - **Teach and Repeat**: Listens for the VR menu button press. When pressed, it saves the current pose and publishes it to the `/sgr532/teach_pose` topic.
