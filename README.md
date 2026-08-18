# **VR Interface for 6-DOF Robot Arm Control**
---
# **Purpose**

![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)

This repo is a virtual reality interface powered by `Unity` to control a 6-DOF `NXROBO Sagittarius` robot arm and it's digital twin. It's for owners of the arm who want to control the robot through a virtual reality workspace.

This is repo attempts to **democratize technology,** where any operator unlimited by skill or experience can contribute to useful work. Although the US would find this is worth addressing, any nation can benefit from interfaces, particularly intuitive and affordable VR interfaces, that lower the barrier to entry for meaningful contribution in robotics and tech.

This project was a 10-week **REU** (Research Experience for Undergraduates) program at **Kent State University** within Kent State's XR Lab and is funded by grants from the National Science Foundation.

<img src="Images/digital-twin.png" width="450"> <img src="Images/ref-video.gif" width="350">

> (Above) Physical **Sagittarius-532** 6-DOF robot arm and digital twin within VR workspace.

## Getting Started

Requirements:
> `Unity 6000.0.51f1`  
> `SteamVR`

Clone repository:
```
git clone https://github.com/AidanWoodard/VR-Enabled-Digital-Twin-Unity-Project.git
```

Open and run in `Unity`.

> See **https://github.com/AidanWoodard/VR-Enabled-Digital-Twin** for all `ROS` nodes and robot setup.

## Features

### Robot VR Dashboard

The robot dashboard, visible in the VR Unity scene as an interactive dashboard, allows the operator to easily record, save, and playback robot motion in `ROS`-friendly `.bag` file format.

<img src="Images/dashboard.png" width="700">

> (Above) Dashboard with command options to **Record**, **Clear**, **Stop**, and **Play** saved commands.

### **Robust Safety Features**

To prevent self-collision and collision with surfaces, the `Unity` scene uses bounding boxes set to `IsTrigger` to detect potential collision. `ROS` messages will pause until a collision prevention is confirmed. This prevents the operator from accidentally damaging the robot or injuring nearby operators.

<img src="Images/boundries-clear.png" width="400"> <img src="Images/boundries-red.png" width="400">

### **Remote Capabilities**

By changing the target IP address in the `ROS` TCP endpoint to be a separate computer running the VR Unity scene, the operator can effectively control the robot remotely through the VR interface. In practice, this was accomplished with a physical ethernet Cat-6 cable connecting the two computers, and fully virtual operation has not yet been tested. *(see Future Work below)*

### Live Camera Feeds

Two live webcam feeds controlled by individual `ROS` nodes output a live feed that is listened for in the Unity VR scene. The camera startup sequence is automatically staggered to prevent crowding the bus when porting from Windows or MacOS into `WSL2`.

<img src="Images/empty-workspace.png" width="400">  <img src="Images/workspace.png" width="400">

> See **https://github.com/AidanWoodard/VR-Enabled-Digital-Twin** for all `ROS` nodes and robot setup.

## **Future Work**

### **Additions to Remote Capabilities**

Operating remotely in a democratized workspace is the final goal of this project, as any operator regardless of skill or location could control and program a 6-DOF robot arm with an internet connection, computer, and VR headset/controls. 

Though achieved with an ethernet cable, this has yet to be done wirelessly.

### **More Safety Redundancy**

Further efforts should be implemented on the `ROS` side of development to prevent self-collision and collision with any floor surfaces. This is partly accomplished with the `MoveIt` library but could be more properly implemented.

## **Where to Go Next**

See extra documentation under **docs/** folder.

## **Credits**

Previous work included in this repository:

> Base ROS packaging, robot URDF descriptions, and `MoveIt` config by **NXROBO**:  
> https://github.com/NXROBO/sagittarius_ws/tree/main  
> Digital Twin through VR interaction by **Samuel Staciewicz**:  
> https://github.com/samuelstasiewicz/VR-Enabled-Teleoperation  
 
---
Thank you to **Kent State University** and the **College of Aeronautics and Engineering** for this REU program opportunity. Thanks also to Dr. Benjamin Kwasa for his guidance in the Kent State XR Laboratory.
