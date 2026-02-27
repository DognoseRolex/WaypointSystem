cite the source when use: He, P. (2025). WaypointSystem (Version 1.0) [Computer software]. GitHub. https://github.com/DognoseRolex/WaypointSystem.git

## 1) WaypointPath.cs (Route container + editor gizmos)

WaypointPath stores an ordered array of waypoint Transforms:

Points (Transform[]): the waypoint sequence NPCs will follow.

Convenience: If Points is not assigned, OnValidate() auto-fills it using the GameObject’s children (in hierarchy order).

Visualization: OnDrawGizmos() draws cyan spheres and lines between waypoints in the Scene view, making the route easy to inspect and debug.
<img width="266" height="535" alt="image" src="https://github.com/user-attachments/assets/ae454ee1-9647-43ac-99e6-9e0d853723e5" />
<img width="421" height="422" alt="image" src="https://github.com/user-attachments/assets/a3e00e41-04e3-4932-a6fe-2e7abc1c4b51" />

# Recommended workflow:

Create an empty GameObject, e.g., Path_CityLoop.

Add WaypointPath to it.

Create child empties as waypoints: WP_01, WP_02, … in the desired order.

Move each waypoint onto the road centerline (or lane centerline).

The Points array will auto-populate from children.

## 2) WaypointCar is the movement controller that follows a WaypointPath.
<img width="972" height="729" alt="image" src="https://github.com/user-attachments/assets/e2cd592b-e216-43bd-9a53-37d405e29e34" />

## 3) WaypointBlinkInstruction.cs (Per-waypoint indicator command)

Attach WaypointBlinkInstruction to any waypoint Transform to set the NPC’s blinker state when it arrives at that waypoint.
<img width="902" height="469" alt="image" src="https://github.com/user-attachments/assets/19d14ec4-eb5a-431e-861d-f96a7c617e18" />


## Quick Start

Create a path

Empty GameObject → add WaypointPath

Create children waypoints in order (this becomes Points[])

Create an NPC

Add a Rigidbody + Box Collider + WaypointCar + Wheel Visual Animator + Blinker Light
<img width="1315" height="762" alt="image" src="https://github.com/user-attachments/assets/84b32817-3bb7-4f9f-b9ed-2b4e9e73b0f2" />


Assign:

path (your WaypointPath)

sensorOrigin (front bumper empty)

obstacleLayers (NPC + obstacles layers)

Add turn signals

On selected waypoints (e.g., before turning), add WaypointBlinkInstruction

Set mode (Left/Right) and autoClearSeconds (e.g., 2–4 s)

