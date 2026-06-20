# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Sam's Robot Shop** — Unity 6 (6000.0.51f1) VR teleoperation system for a Sagittarius SGR532 robotic arm. A VR user controls the arm's end-effector pose in real time over ROS via a TCP bridge. The Unity side runs on a Windows PC; the ROS side (Noetic) runs on a separate Linux machine connected via Ethernet.

**Hardware:** HTC Vive headset + Valve Index controllers. SteamVR must be running as the active OpenXR runtime before entering Play mode.

**ROS workspace location:** The actual in-use ROS workspace is `~/../../ROS_Files/sagittarius_ws/` on the Linux ROS machine. The `REU2026\OldROSFiles\sagittarius_ws\` folder in this repo is **out of date** — do not treat it as the source of truth for ROS-side code.

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

### Barrier Visual Suppression (added 2026-06-18)
`RobotBarrier.cs` no longer shows the red barrier-violation `MeshRenderer` while `CommandSlotDashboard.IsRecording` is true or while `ROSPublishToggle.IsPublishingEnabled` is false (active control paused via dual-trigger hold). This is visual-only — `isCollisionActive` and the ROS publish-gating logic are unaffected, so the arm is still blocked from publishing on a real violation even when the visual is suppressed. `CommandSlotDashboard.IsRecording` is a new public static bool set `true` in `StartRecording`'s success callback and `false` unconditionally in `StopActiveSlot`'s Recording-branch callback (mirrors the existing `activeSlot = -1` always-clear pattern from the stuck-button fix below).

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
| `RobotBarrier.cs` | `Assets/Scripts/` | Collision detection; sets `isCollisionActive`; visual flash suppressed during recording/paused control |
| `JointStateSubscriber.cs` | `Assets/Scripts/` | Subscribes to joint states from ROS |
| `RobotDashboard.cs` | `Assets/Scripts/` | Legacy placeholder — superseded by `CommandSlotDashboard.cs` |
| `CommandSlotDashboard.cs` | `Assets/Scripts/` | 5-slot record/play/clear dashboard; self-discovers rows, manages slot state machine, ROS service calls, radial hold-to-clear mechanic; exposes static `IsRecording` flag |
| `SlotRowBinding.cs` | `Assets/Scripts/` | Per-row reference holder (`slotIndex`, `statusDot`, `recordButton`, `playButton`, `actionLabel`, `clearFillOverlay`); tagged `command_ui` on the prefab |
| `VRPosePublisher.cs` / `VRPosePublisher2.cs` | `Assets/Scripts/` | Additional pose publishers (alternate or legacy) |

## Dashboard Architecture

### Slot Discovery (no manual Inspector wiring)
`CommandSlotDashboard.cs` no longer has a hand-wired `SlotRow[]` Inspector array. In `Awake()` it calls `GetComponentsInChildren<SlotRowBinding>()`, filters/warns on the `command_ui` tag, and sorts by each binding's `slotIndex` (0–4) to build its internal `slots` array. To add/rewire a row in the prefab: add a `SlotRowBinding` component to the row's GameObject, tag it `command_ui`, set `slotIndex`, and wire its 5 fields locally — no changes needed on the `CommandSlotDashboard` component itself.

### Slot State Machine
`CommandSlotDashboard.cs` tracks 5 independent slots. Each slot cycles: `Empty → HasRecording → Recording / Playing → HasRecording`.
- Only one slot can be Recording or Playing at a time (`activeSlot` index).
- **Record**: `StartRecording()` now sets `ROSPublishToggle.IsPublishingEnabled = true` immediately on press (2026-06-20) — no longer requires the dual-trigger hold gesture first. On a successful Stop, `StopActiveSlot()` leaves `ROSPublishToggle.IsPublishingEnabled` untouched (live teleop continues uninterrupted after Stop — no forced disable, unlike Play).
- **Play**: sets `ROSPublishToggle.IsPublishingEnabled = false` so the bag drives the arm; stays `false` after a successful Stop (no longer re-enables) — re-enable via the dual-trigger hold gesture. **Auto-resets without a Stop press** when the bag finishes on its own — see "Playback Auto-Finish Detection" below.
- **Clear**: hold CLEAR button 1 s (radial fill overlay) → calls `DashboardClear` service → ROS zeroes the bag file → slot → Empty. `ClearHoldRoutine()` never references `ROSPublishToggle` and never has — confirmed by code search and live Unity Editor inspection (2026-06-20): `RecordButton`/`PlayButton`'s `Button.onClick` lists are empty in the scene, the only listeners are the runtime-added `EventTrigger` callbacks in `WireMorphButton()`. If a publisher toggle is ever observed coinciding with a Clear-hold, it's the independent dual-trigger gesture in `ROSPublishToggle.cs` firing on its own 1 s timer, not Clear itself — both gestures happen to share the same hold duration.

**Change (2026-06-20):** Previously, a successful Stop after Record *also* forced `ROSPublishToggle.IsPublishingEnabled = false` (same as after Play). This was changed so Record-stop no longer touches the flag at all — only Play-stop disables publishing. Rationale: pausing a Record shouldn't interrupt live teleop the way finishing a Playback should.

### Morphing Record/Stop/Clear Button
There is no separate `stopClearButton` GameObject anymore. `recordButton` itself morphs in place via `EventTrigger` PointerDown/Up (not `Button.onClick`, to avoid double-firing with the hold-to-clear gesture):
- `Empty` → label `REC`, instant action on press: start recording.
- `Recording` / `Playing` → label `STOP` (dot color distinguishes which), instant action: stop the active slot.
- `HasRecording` → label `CLEAR`, requires the 1 s hold (radial `clearFillOverlay`, now parented under `recordButton` instead of a dedicated button).
`playButton` is untouched by this — it stays a separate button, interactable only when `HasRecording`.

### ROS Services (unity_vr_control package)
| Service | .srv file | Purpose |
|---|---|---|
| `dashboard/record` | `DashboardRecord.srv` | `start=true/false` — launch/stop `rosbag record` subprocess |
| `dashboard/playback` | `DashboardPlayback.srv` | `start=true/false` — launch/stop `rosbag play` subprocess |
| `dashboard/query_slots` | `DashboardQuerySlots.srv` | Returns `bool[5]` — whether each slot's bag file has data |
| `dashboard/clear` | `DashboardClear.srv` | Truncates `slot_N.bag` to 0 bytes |

Plus one topic (not a service): `dashboard/playback_finished` (`std_msgs/Int32`, payload = 1-based `slot_id`) — pushed by ROS the instant a `rosbag play` subprocess exits on its own. See "Playback Auto-Finish Detection" below.

ROS node: `unity_vr_control/scripts/dashboard_controller.py`. Bags stored in `~/dashboard_bags/slot_N.bag`.

C# message classes live in `Assets/RosMessages/Dashboard/srv/` under namespace `RosMessageTypes.Dashboard`.

### UI Prefab (`Assets/Prefabs/Dashboard.prefab`)
5 `Slot{N}Row` children (tagged `command_ui`) inside a Vertical Layout Group, each with a `SlotRowBinding` component. Per row:
- `StatusDot` Image (color = state)
- `RecordButton` — the morphing Record/Stop/Clear button, with children `Label` (text) and `ClearFillOverlay` Image (`fillMethod=Radial360`, orange tint, inactive by default)
- `PlayButton` — separate, untouched by the morph mechanic

### Playback Auto-Finish Detection (added 2026-06-20)
The 2026-06-18 fix (below) only made the *manual* Stop path idempotent — the user still had to press Stop after a bag finished naturally. This was a separate gap: the whole dashboard link was pure request/response, so ROS had no way to proactively tell Unity a bag ended. Fix: ROS now pushes a topic, `dashboard/playback_finished` (`std_msgs/Int32`, payload = 1-based `slot_id`), the moment a `rosbag play` subprocess exits on its own (background thread calls `proc.wait()` after `Popen`, then publishes). Implemented on the live Linux node by a separate agent session — verify it's still present if `dashboard_controller.py` is ever rewritten.

Unity side, `CommandSlotDashboard.cs`: subscribes once in `Start()` via `ros.Subscribe<Int32Msg>("dashboard/playback_finished", OnPlaybackFinishedFromROS)` (`Int32Msg` ships built-in in the ROS-TCP-Connector package under `RosMessageTypes.Std` — no message generation needed). `OnPlaybackFinishedFromROS` converts the 1-based ROS slot id to Unity's 0-based index, marshals onto `mainThreadQueue`, and only resets (`activeSlot = -1`, `states[slot] = HasRecording`, `UpdateSlotUI(slot)`) if `activeSlot == slot && states[slot] == Playing` — guards against a race with a near-simultaneous manual Stop press (whichever runs first wins; the second is a no-op).

## Coordinate System
ROS uses FLU (Forward-Left-Up). Unity uses RUF (Right-Up-Forward). All pose conversions go through `.To<FLU>()` extension methods from `Unity.Robotics.ROSTCPConnector.ROSGeometry`. Quaternions must be manually reconstructed after conversion — the `.To<FLU>()` return type is not a `UnityEngine.Quaternion`.

## TODO (resolved)

### Stop button gets permanently stuck after a bag finishes playing to completion — FIXED 2026-06-18

**Symptom:** The morphing `recordButton` (showing `STOP` during playback) works fine right after Record, but becomes unresponsive specifically once a `.bag` plays through to its natural end without the user pressing Stop first. After that: the slot's status dot stays green (`Playing`) forever, pressing Stop on that slot does nothing, other slots' buttons keep working normally, and starting playback on a *different* slot leaves the original slot's dot stuck green instead of resetting.

**Investigation already done (do not re-derive — verified by direct code reads, not just static guessing):**
- Ruled out the Unity state machine logic in `CommandSlotDashboard.cs` — `OnMorphButtonDown()` → `StopActiveSlot()` is correctly wired and identical for `Recording` and `Playing` states.
- Ruled out ROS connectivity/timeouts — confirmed ROS connected, Unity console clean during repro.
- Ruled out any script gating the XR ray/raycaster/EventSystem off `ROSPublishToggle.IsPublishingEnabled` — grepped the whole `Assets/Scripts` folder; no such script exists. `IsPublishingEnabled` only gates ROS message publishing in `pubtest.cs`.
- Confirmed via user re-hover test that the button itself correctly receives VR pointer input — the failure is not an input/EventTrigger/raycaster issue.

**Confirmed root cause (server-side):** `dashboard_controller.py`, `handle_playback()`'s stop branch:

```python
proc = playback_procs.pop(slot, None)
if proc is None or proc.poll() is not None:
    return DashboardPlaybackResponse(success=False, message=f"Slot {slot} not playing")
```

When `rosbag play` reaches end-of-bag, the subprocess exits on its own and `proc.poll()` becomes non-`None`. Unity is never told playback ended, so it still thinks the slot is `Playing`. When the user later presses Stop, this handler sees the process already dead and returns `success=False` ("Slot N not playing") instead of treating "already stopped" as a successful no-op.

Back in `CommandSlotDashboard.cs::StopActiveSlot()` (~lines 176-187), the state reset (`activeSlot = -1`, `states[slot] = HasRecording`, `ROSPublishToggle.IsPublishingEnabled = true`, `UpdateSlotUI(slot)`) only runs `if (resp.success)`. Since the response is `false`, the slot is stuck permanently. This also explains the "stuck dot when switching slots" symptom: in `OnPlayClicked`'s redirect path, `StopActiveSlot(onComplete)`'s `onComplete?.Invoke()` (which starts the new slot's playback) fires unconditionally regardless of `resp.success` (~line 184), so the new slot proceeds while the old slot's UI is abandoned mid-update.

**Note on file locations:** Per this file's own ROS workspace note above, the *live* `dashboard_controller.py` is at `~/../../ROS_Files/sagittarius_ws/src/sagittarius_arm_ros/unity_vr_control/scripts/dashboard_controller.py` on the separate Linux ROS machine — not reachable from this Windows filesystem. The local mirror at `D:\Aidan\REU2026\OldROSFiles\sagittarius_ws\src\sagittarius_arm_ros\unity_vr_control\scripts\dashboard_controller.py` is out of date and not what actually runs, but should still get the same fix applied for reference/consistency.

**Fix — ROS side (primary), `handle_playback()`:** Treat "stop requested but process already exited on its own" as success, not failure:

```python
else:
    proc = playback_procs.pop(slot, None)
    if proc is None:
        return DashboardPlaybackResponse(success=False, message=f"Slot {slot} not playing")
    if proc.poll() is not None:
        # Bag already finished on its own — stopping is a no-op, but it IS stopped.
        return DashboardPlaybackResponse(success=True, message=f"Slot {slot} playback already finished")

    proc.send_signal(signal.SIGINT)
    try:
        proc.wait(timeout=5)
    except subprocess.TimeoutExpired:
        proc.kill()
    rospy.loginfo(f"[Dashboard] Playback stopped for slot {slot}")
    return DashboardPlaybackResponse(success=True, message=f"Playback stopped for slot {slot}")
```

Apply this on the live Linux file first, then mirror it into the local out-of-date copy. For full consistency, `handle_record()`'s stop branch has the identical pattern (process-already-exited treated as failure) — same idempotent-stop treatment should apply there too, though lower priority since recordings don't end "on their own" the way bag playback does.

**Fix — Unity side (defensive resilience, secondary/optional):** In `CommandSlotDashboard.cs::StopActiveSlot()`, consider always clearing `activeSlot = -1` in the response callback regardless of `resp.success`, and only conditionally restoring richer state (`states[slot] = HasRecording`, re-enabling publishing) on success. This prevents any future "ROS returns failure on stop" scenario from permanently stranding a slot, independent of this specific bag-completion case.

**Verification steps:**
1. Apply the ROS-side fix on the live Linux file, restart `dashboard_controller.py` (or the relevant roslaunch).
2. In Unity Play mode (SteamVR running, ROS TCP endpoint active): Record a short clip in a slot, Play it, and let it play to completion without touching Stop.
3. Confirm the slot's dot returns to `HasRecording` (cyan) automatically or via Stop press, and the button becomes responsive again.
4. Confirm starting playback on a different slot no longer leaves the first slot's dot stuck green.
5. Check Unity Console and ROS terminal for `[Dashboard] Playback stopped for slot N` — no more `success=False` "not playing" warnings after natural bag completion.

Full plan also saved at `C:\Users\xrlab23\.claude\plans\i-m-having-issues-with-greedy-emerson.md`.

**Resolution (2026-06-18):** Applied on the live ROS file (`~/ROS_Files/sagittarius_ws/src/unity_vr_control/scripts/dashboard_controller.py`, edited via WSL), the local mirrors, and `CommandSlotDashboard.cs::StopActiveSlot()`. `handle_playback()` and `handle_record()` now both return `success=True` ("already finished") when the subprocess already exited on its own, instead of `success=False`. `StopActiveSlot()` now always clears `activeSlot` regardless of `resp.success`, only restoring `HasRecording`/re-enabling publishing on success — so a slot can never get permanently stranded. **The live `dashboard_controller.py` node must be restarted (re-run its roslaunch/script) for this fix to take effect.**
