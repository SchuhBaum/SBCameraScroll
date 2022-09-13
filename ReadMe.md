## SBCameraScroll
###### Version: 0.63
This is a mod for Rain World v1.5.

### Description
Creates a smooth, scrolling camera that moves with the slugcat. Based on bee's CameraScroll mod, credit to bee for that.

### Dependencies
- ConfigMachine.dll.
- (optional) CustomRegions.dll

### Installation
1. (ModLoader) `BepInEx` and `BOI` can be downloaded from [RainDB](https://www.raindb.net/) under `Tools`.  
  **NOTE:** Rain World's BepInEx is a modified version. Don't download it from GitHub.
2. (Dependency) The mod `ConfigMachine` can be downloaded from [RainDB](https://www.raindb.net/) under `Tools`.
3. Download the file  `SBCameraScroll.dll` from [Releases](https://github.com/SchuhBaum/SBCameraScroll/releases) and place it in the folder `[Steam]\SteamApps\common\Rain World\Mods`.
4. Start `[Steam]\SteamApps\common\Rain World\BOI\BlepOutIn.exe`.
5. Click `Select path` and enter the game's path `[Steam]\SteamApps\common\Rain World`. Enable the mod `SBCameraScroll.dll` and its dependencies. Then launch the game as normal. 

### Contact
If you have feedback, you can message me on Discord `@SchuhBaum#7246` or write an email to SchuhBaum71@gmail.com.

### License
There are two licenses available - MIT and Unlicense. You can choose which one you want to use.  
**NOTE:** WeakTables use their own license. This license is included in the folder SourceCode/WeakTables. Website: https://github.com/Dual-Iron/weak-tables

### Changelog
v0.3:
- Major overhaul. Restructured code.
- Merges camera texture at runtime.
- Added support for custom regions. You need the CustomRegion mod (CRS). When found, the "Mods\CustomResources" folder is searched as well for camera textures.
- (merge while loading) Added this option. When enabled (default), the camera textures for each room are merged when the region gets loaded. When disabled, camera textures are merged for each room on demand.
 Merging happens only once and the files are stored in your "Mods\SBCameraScroll" folder. This process can take a while. Merging all rooms in Deserted Wastelands took me around three minutes.
 If room cameras get visually changed then you need to delete the corresponding merged files in order to update them.
- Overhaul of the camera logic. Main features are: 1) No movement when close to the camera (box-type camera). 2) Smoothed acceleration when further away. 3) Matches player speed when a certain distance is reached. 4) Stops at room borders (unless you are in the final room).
- (extended debug logs) Added this option (disabled by default). Add logs when the game skips frames. In this case, the camera can feel laggy. Happens quite frequently for me :(. A scrolling camera makes them more noticeable.

v0.31:
- Camera parameters can be configured now.
- Slightly simplified camera logic. Camera only moves when the player moves. The camera also does not move when the player changes direction.
- (velocity type) Added this option (disabled by default). This is more a mario-type camera. The camera smoothly matches the players velocity. It fully matches it when OuterCameraBox is reached. Gives the camera a more rigid feel to it. Did not work too well for me (given frame skips and detailed textures but let me know what your experience is like).
- (full screen effects) Added this option (enabled by default). When disabled, full screen effect like fog and bloom are not shown. In particular, fog can noticeably move with the screen.

v0.40:
- Simplified implementation of camera logic when reaching borders.
- Trying to reduce effects of floating point imprecision by rounding the player and camera position to full pixel.
- Removed: "Camera only moves when the player moves." -- This made ledge climbing and small collisions kill all the camera momentum. Felt too rigid. This still happens (by design) when the velocity type option is used.
- Small adjustments. Changed default parameters.
- Fixed a bug with SL_C01 (which has five cameras apparently), where two cameras are located in outer space or something, and trying to merge them did lead to an out of memory exception.
- I tested that (this version of) Unity limits the texture size to 10000x10000. Textures get cut off and might look bad. So far SB_J03 seem to be the only example.
- Rooms are blacklisted when the merging textures fails or could not be written to disk. In this case the vanilla camera is used.
- Restructured code.
- Fixed a bug, where rooms could be blacklisted prematurely.
- Cropping merged texture for room SB_J03 in order to (barely) fit the maximum texture size.
- Updated worm grass loading logic. Each Worm grass patch checks its tiles and their distances. A worm grass tile gets loaded when close to the camera.
- (vanilla camera type) Added this option. Vanilla-style camera. Adds the ability to center the camera by using the map button.
- Fixed some bugs when loading an arena session.

v0.50:
- Fixed a bug when using fast travel.
- Fixed a bug where the directory for custom regions would not be used when a custom region pack contained more than one region.
(-Fixed a bug where a non-vanilla function would be called for blacklisted rooms. This might resulted in side-effects.) Reverted and only slightly changed. I remembered why I had to do this.
- Merging textures is not attempted again after it failed once. Skips the function when a room got blacklisted. Improves loading times.
- The smoothing factor is now directly set in the options.
- Added smooth transitions for various cases. For example when switching camera inside the same room.
- Fixed a bug that caused black outlines around slugcat and other sprites.
- Various changes to improve support for multiple cameras. This does not mean that it is compatible with the split screen mod. I look into that at some point later.
- (vanilla type) Removed the offset. The camera can only be centered on slugcat.
- (vanilla type) You can now switch between vanilla camera positions and the vanilla type camera. When entering a room, the vanilla camera positions are used by default.

v0.60:
- Improved transitions by checking for borders first.
- Fixed a bug where the room texture could become squashed when using the split screen mod.
- Fixed a bug where sprite could vibrate a little bit. Reverted rounding changes. I attempted to fix this before with these changes.
- (vanilla type) Fixed a bug where the vanilla camera positions were not used after entering a new room.
- Fixed a bug that could freeze the hunter cutscene.
- When SplitScreenMod is used then scrolling one-screen rooms is always enabled.
- Changed texture names to LevelTexture, LevelTexture1, LevelTexture2, ... which are used in SplitScreenMod without requiring external patching.
- When position camera type is used then the camera slows down before reaching room borders. This is most noticable when scrolling one-screen rooms.
- Fixed a bug where room textures were not merged when consisting of both vanilla and modded screens. (Example: Arid Barren's shore line SL_D06)
- Empty merged texture files are ignored and will be overwritten. In rare cases the file was created but not written to.
- Void sea is treated as being blacklisted. The camera should no longer lag behind and the screen shake should be applied normally.
- Simplified implementation.
- Fixed a memory leak caused by calling www.texture which created texture copies.
- Split the fullscreen effect option into two options. One for fog and the other for the rest.
- Fixed three bugs where a variable was cleared too early.

v0.63
- Restructured code. Using weak tables. Fixed a bug where the texture offset of region gates were not initialized. Restructured CheckBorders() + included patch from SplitScreenMod. 
- (vanilla type) Fixed a bug where the camera would slightly move after a screen transition when using vanilla camera positions. The lean effect now mimics vanilla lean effect instead of scaling with the camera box. This removes one parameter from the options menu.
- Slightly increase minimum speed for transitions and the position type camera. Camera moves at least one pixel per frame.
- Restructured code.
- (vanilla type) Map inputs for switching between centering and vanilla camera positions can be used during transitions. 
- Switched to BepInEx plugin. Changed priority (GUID) to be lower than SplitScreenMod. Otherwise curCamera is not updated during RoomCamera_DrawUpdate calls.

### Known issues
- Motion sickness might be a problem. I wonder how much is due to noise (details) in the textures. You can play around with the parameters and see what works for you.
- The underwater shaders do not work correctly. In large rooms they can smear sprites.
- Shadows at the edge of the screen seem to flicker or stretch in some rooms. I can see the same thing without camera scroll. Maybe a vanilla bug. 