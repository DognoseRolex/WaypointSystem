cite the source when use: He, P. (2025). WaypointSystem (Version 1.0) [Computer software]. GitHub. https://github.com/DognoseRolex/WaypointSystem.git

## 1) WaypointPath.cs (Route container + editor gizmos)

WaypointPath stores an ordered array of waypoint Transforms:

Points (Transform[]): the waypoint sequence NPCs will follow.

Convenience: If Points is not assigned, OnValidate() auto-fills it using the GameObject’s children (in hierarchy order).

Visualization: OnDrawGizmos() draws cyan spheres and lines between waypoints in the Scene view, making the route easy to inspect and debug.

# Recommended workflow:

Create an empty GameObject, e.g., Path_CityLoop.

Add WaypointPath to it.

Create child empties as waypoints: WP_01, WP_02, … in the desired order.

Move each waypoint onto the road centerline (or lane centerline).

The Points array will auto-populate from children.

## 2) WaypointCar is the movement controller that follows a WaypointPath.
## 3) WaypointBlinkInstruction.cs (Per-waypoint indicator command)

Attach WaypointBlinkInstruction to any waypoint Transform to set the NPC’s blinker state when it arrives at that waypoint.

## Quick Start

Create a path

Empty GameObject → add WaypointPath

Create children waypoints in order (this becomes Points[])

Create an NPC

Add a Rigidbody + WaypointCar

Assign:

path (your WaypointPath)

sensorOrigin (front bumper empty)

obstacleLayers (NPC + obstacles layers)

Add turn signals

On selected waypoints (e.g., before turning), add WaypointBlinkInstruction

Set mode (Left/Right) and autoClearSeconds (e.g., 2–4 s)

