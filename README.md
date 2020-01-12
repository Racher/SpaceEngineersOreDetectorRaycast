# SpaceEngineersOreDetectorRaycast
Adds Raycast to MyOreDetector for In-game scripts.

A low level functionality that allows in-game scripts to use the ore detector to shoot out rays,
which return the name and distance of the ore it hits.
Does not detect ore patch extent.
Does not detect in surrounding sphere or give info about the base game's automatically detected patches.

Based on IMyCameraBlock's Raycast functionality, similar charge limitation. (2km/s)

Sample script: https://steamcommunity.com/workshop/filedetails/?id=1967149949
Test world: https://steamcommunity.com/sharedfiles/filedetails/?id=1967196238
Source: Racher/SpaceEngineersOreDetectorRaycast

Use:
(block as IMyOreDetector).SetValue("RaycastTarget", block.GetPosition() + block.WorldMatrix.Forward * 50);
var result = (block as IMyOreDetector).GetValue<MyDetectedEntityInfo>("RaycastResult");

pseudo interface IMyOreDetector
{
//! Ore names that will be ignored by the ray.
string OreBlacklist { get; set; }

//! Function parameter for Raycast. Does nothing on its own.
Vector3D RaycastTarget { get; set; }

//! Return value of Raycast. Calculated on every get call. Can be called multiple times per update.
MyDetectedEntityInfo RaycastResult { get; }

//! Charge that increases by 2km/s consumed by Raycast.
double AvailableScanRange { get; }
}
