# SpaceEngineers OreDetector Raycast
Adds Raycast to MyOreDetector for In-game scripts.

A low level functionality that allows in-game scripts to use the ore detector to shoot out rays,
which return the name and distance of the ore it hits.

Does not detect ore patch extent.
Does not detect in surrounding sphere or give info about the base game's automatically detected patches.

Based on IMyCameraBlock's Raycast functionality, similar charge limitation. (2km/s)

# Contribute
- Install Space Engineers Mod SDK.
- Clone the repo into \AppData\Roaming\SpaceEngineers\Mods\SpaceEngineersOreDetectorRaycast.
- The game should find the mod there.
- Use the sample world save from the workshop but swap the mod in the save edit: workshop - version with the local version.
- Check Alt+F11 for mod loading errors.
- Publish mod from save edit mod selection window.
- Update existing mod? Some modinfo.sbmi is generated which could be the identifier but dont have the old one.
