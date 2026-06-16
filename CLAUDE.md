# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Sam's Robot Shop** — Unity 6 (6000.0.51f1) VR teleoperation system for a Sagittarius SGR532 robotic arm. A VR user controls the arm's end-effector pose in real time over ROS via a TCP bridge. The Unity side runs on a Windows PC; the ROS side (Noetic) runs on a separate Linux machine connected via Ethernet.

**Hardware:** HTC Vive headset + Valve Index controllers. SteamVR must be running as the active OpenXR runtime before entering Play mode.

## Unity Editor Workflow

This project has no CLI build or test pipeline. All development happens in the Unity Editor:

- **Open project:** `D:/Aidan/REU2026/Samuel/Samuel/Sam's Robot Shop/`
- **Play mode:** requires SteamVR running and the ROS TCP server active on the other machine (`roslaunch ros_tcp_endpoint endpoint.launch`)
- **MCP server** (`com.coplaydev.unity-mcp`) is installed — Claude Code can control the editor via MCP tools directly

## Architecture

### ROS Communication
`ROSConnection` (singleton, auto-created) manages the TCP socket. All publishers register in `Start()` and publish on a timer. Three active topics:
- `/sgr532/vr_target_pose` — `PoseStampedMsg` at 20 Hz (controller pose → robot EE target)
- `/sgr532/gripper/command` — `Float64Msg` (gripper open/close value)
- `/sgr532/teach_pose` — `PoseStampedMsg` (currently unused/commented out)

Publishing is gated by two static flags that must both be clear:
- `RobotBarrier.isCollisionActive` — set by collision detection
- `ROSPublishToggle.IsPublishingEnabled` — toggled by dual-trigger hold gesture

### Calibration Flow
`pubtest.cs` waits 10 seconds after `Start` before capturing the controller's home position/rotation as the reference frame. `pubtest.isCalibrated` (static bool) gates pose publishing. The delta between current and home pose is added to a hardcoded ROS EE home position (`rosEEHomeInBaseLink`).

### XR Input Setup
- `XR Origin (XR Rig)` → `InputActionManager` holds `XRI Default Input Actions.inputactions` (enables the asset at runtime)
- `EventSystem` → `XRUIInputModule` (`activeInputMode: 1` = XR Input) drives VR UI clicking
- `Dashboard` canvas → `TrackedDeviceGraphicRaycaster` handles ray-based UI hit detection

**Critical Input System rule:** Always use `InputActionType.Value` (not the default `Button`) when creating `InputAction` instances for analog triggers. `Button` type applies a press threshold and `ReadValue<float>()` returns 0 below it.

**Valve Index binding pattern:** SteamVR/OpenXR registers Index controllers as `ValveIndexController` layout. Always add two bindings for triggers:
```csharp
action.AddBinding("<XRController>{LeftHand}/trigger");
action.AddBinding("<ValveIndexController>{LeftHand}/trigger");
```

### UI Button Clicking
UI clicks flow: trigger press → `XRI Left/Right Interaction / UI Press` action → `XRUIInputModule` → `Button.onClick`. The `UI Press` action in `XRI Default Input Actions.inputactions` must have `<XRController>{Hand}/{TriggerButton}` bound; `UI Press Value` needs `<XRController>{Hand}/trigger`. These were previously empty and have been fixed.

`Select` (grip button) is for grabbing XR interactables, not UI. `UI Press` (trigger) is for clicking UI buttons.

### OpenXR Configuration
Settings live in `Assets/XR/Settings/OpenXR Package Settings.asset`. For the Vive+Index setup, required enabled features on Standalone:
- `ValveIndexControllerProfile Standalone` — controller input
- `HTCViveControllerProfile Standalone` — Vive wand fallback
- **`MockRuntime Standalone` must be `m_enabled: 0`** — if enabled, it intercepts all OpenXR calls and blocks real hardware entirely

HMD tracking (Vive headset) does not require its own interaction profile; SteamVR exposes it via the OpenXR view reference space automatically.

## Key Scripts

| Script | Location | Purpose |
|---|---|---|
| `pubtest.cs` | `Assets/Scripts/` | Main controller pose + gripper publisher |
| `ROSPublishToggle.cs` | `Assets/Scripts/` | Static `IsPublishingEnabled` flag; hold both triggers 1 s to toggle |
| `XRInputDebugger.cs` | `Assets/Scripts/` | Debug logger for all XR inputs; has `enableDebugger` Inspector checkbox |
| `RobotBarrier.cs` | `Assets/Scripts/` | Collision detection; sets `isCollisionActive` |
| `JointStateSubscriber.cs` | `Assets/Scripts/` | Subscribes to joint states from ROS |
| `RobotDashboard.cs` | `Assets/Scripts/` | Legacy placeholder — superseded by `CommandSlotDashboard.cs` |
| `CommandSlotDashboard.cs` | `Assets/Scripts/` | 5-slot record/play/clear dashboard; manages slot state machine, ROS service calls, radial hold-to-clear mechanic |
| `VRPosePublisher.cs` / `VRPosePublisher2.cs` | `Assets/Scripts/` | Additional pose publishers (alternate or legacy) |

## Dashboard Architecture

### Slot State Machine
`CommandSlotDashboard.cs` tracks 5 independent slots. Each slot cycles: `Empty → HasRecording → Recording / Playing → HasRecording`.
- Only one slot can be Recording or Playing at a time (`activeSlot` index).
- **Record**: keeps live publishing enabled (user puppets arm; ROS records the pose stream).
- **Play**: sets `ROSPublishToggle.IsPublishingEnabled = false` so the bag drives the arm; re-enables on stop.
- **Clear**: hold CLEAR button 1 s (radial fill overlay) → calls `DashboardClear` service → ROS zeroes the bag file → slot → Empty.

### ROS Services (unity_vr_control package)
| Service | .srv file | Purpose |
|---|---|---|
| `dashboard/record` | `DashboardRecord.srv` | `start=true/false` — launch/stop `rosbag record` subprocess |
| `dashboard/playback` | `DashboardPlayback.srv` | `start=true/false` — launch/stop `rosbag play` subprocess |
| `dashboard/query_slots` | `DashboardQuerySlots.srv` | Returns `bool[5]` — whether each slot's bag file has data |
| `dashboard/clear` | `DashboardClear.srv` | Truncates `slot_N.bag` to 0 bytes |

ROS node: `unity_vr_control/scripts/dashboard_controller.py`. Bags stored in `~/dashboard_bags/slot_N.bag`.

C# message classes live in `Assets/RosMessages/Dashboard/srv/` under namespace `RosMessageTypes.Dashboard`.

### UI Prefab (Dashboard scene)
5 `SlotRow` children inside a Vertical Layout Group. Each row has:
- `statusDot` Image (color = state)
- `recordButton`, `playButton`
- `stopClearButton` with a child `clearFillOverlay` Image (`fillMethod=Radial360`, orange tint)
All slot refs wired in the Inspector on the `CommandSlotDashboard` component.

## Coordinate System
ROS uses FLU (Forward-Left-Up). Unity uses RUF (Right-Up-Forward). All pose conversions go through `.To<FLU>()` extension methods from `Unity.Robotics.ROSTCPConnector.ROSGeometry`. Quaternions must be manually reconstructed after conversion — the `.To<FLU>()` return type is not a `UnityEngine.Quaternion`.
