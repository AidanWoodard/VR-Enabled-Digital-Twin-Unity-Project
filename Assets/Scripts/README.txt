JointStateSubscriber.cs
- Purpose: Subscribes to the ROS `/sgr532/joint_states` topic and updates the Unity robot model's joints to mirror the physical or simulated robot.
- Details: Maps ROS joint names to the corresponding Unity ArticulationBody objects and updates their target positions based on incoming messages.

VRPosePublisher.cs
- Purpose: Publishes the XR right controller's pose to the ROS `/sgr532/vr_target_pose` topic for tracking.
- Details: Includes a calibration step triggered by the 'A' button on the controller to set a home pose. Converts Unity's coordinate system to ROS's FLU (Forward-Left-Up) format and adds an offset relative to a hardcoded robot end-effector home pose before publishing.

VRPosePublisher2.cs
- Purpose: A simplified version of the VR pose publisher.
- Details: Directly publishes the XR controller's world-space position and rotation to the `/sgr532/vr_target_pose` topic without offset or calibration logic. Converts coordinates to ROS FLU format and throttles the publish rate.

pubtest.cs
- Purpose: An advanced controller script handling VR pose tracking, gripper control, and a "teach and repeat" feature.
- Details: Automatically calibrates the controller's home pose after 10 seconds. Uses VR controller triggers to publish gripper commands (open/close) to `/sgr532/gripper/command`. Saves the current pose and publishes it to `/sgr532/teach_pose` when the VR menu button is pressed.
