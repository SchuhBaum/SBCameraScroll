## SBCameraScroll
###### Version: 2.4.9
This is a mod for Rain World v1.9.

### Description
Creates a smooth, scrolling camera that moves with the slugcat. Based on pipi toki's CameraScroll mod.  
  
Here is a youtube video showing Rain World v1.5 + SBCameraScroll (+ other mods) in action:  
https://www.youtube.com/watch?v=ePJbY4GSMck

This mod generates files for each room with multiple cameras. These files are saved in the folder:  
'Steam\SteamApps\common\Rain World\RainWorld_Data\StreamingAssets\mods\SBCameraScroll\world'

If rooms textures are changed then you need to generate these files again. This might happen when you use custom regions. Simply delete everything or specifically what you need in that folder.

### Incompatibilities
- The zoom camera option in the Slugcat Eyebrow Raise mod.

### Installation
0. Update Rain World to version 1.9 if needed.
1. Download the file  `SBCameraScroll.zip` from [Releases](https://github.com/SchuhBaum/SBCameraScroll/releases/tag/v2.4.9).
2. Extract its content in the folder `[Steam]\SteamApps\common\Rain World\RainWorld_Data\StreamingAssets\mods`.
3. Start the game as normal. In the main menu select `Remix` and enable the mod. 

### Bug reports
Please post bugs on the [Rain World Discord](https://discord.gg/rainworld) server in the channel #modding-support. If possible provide (drag & drop) the files consoleLog.txt and exceptionLog.txt (if it exists) from your Rain World folder. Both logs get cleared when you launch the game. Send them after you encounter the problem.

### FAQ
Q: The mod doesn't work. I can't open the mod's option menu. How to fix this?  
A: One thing that helped multiple people is to delete the 'Rain World\BepInEx' folder and then verifying the integrity of the game files in steam.

Q: The mod still doesn't work. I still can't open the mod's option menu. How to fix this?  
A: Maybe this more extensive strategy works for you:
- Delete RW completely.
- Then deactivate downpour on steam client so it won't download.
- Install RW again.
- Apply all mods that you want in remix and test them
- Continue if they work now
- Activate downpour on steam so it would download
- Activate downpour mods in remix
- Reboot the game

Q: The mod doesn't seem to work on SteamDeck. What can I do?  
A: You can try to add 'WINEDLLOVERRIDES="winhttp=n,b" %command%' to the launch options (in Steam: right click >> Properties >> General). If that doesn't work there are other threads about Linux and SteamDeck on the [Rain World Discord](https://discord.gg/rainworld) server.

Q: The game freezes when entering a region. How to fix this?  
A: The game might actually not be frozen. The mod needs to generate files for each region. This can take time (>1 minute). Wait a bit. If the game actually crashed then you have an exceptionLog.txt in your Rain World folder. If that is the case then proceed with the section `Bug reports`.

Q: My game actually crashed. What happened?  
A: Your game might have run out of memory. So far I haven't found a solution to this. As a workaround, I recommend that you restart your game from time to time (see `Known issues`).  

### Known issues
- Snow shaders don't work correctly. Snow on objects is more pixelated and in some places displays more snow than vanilla and in other places less. Falling snow can "jump" when you would have changed screens.
- There are blue outlines in certain rooms (shader issues?).
- This mod increases memory consumption. When the game runs low on memory the performance will decline and the game might crash. This might take a while (90+ minutes). Merging room texture accelerates this. This needs to happen once for every room with multiple screens. The textures are saved to disk (cached) and reused. It is recommended to restart the game every now and then.  
 CONTEXT: There might be memory leaks or memory fragmentation. A memory leak would mean that memory is not released when it is not used anymore. Fragmentation might happen because most roomCamera textures are now fairly large and change in size often. This means that they need to be re-allocated in memory often and they require a chunk of memory without "holes".
- The underwater shaders do not work correctly. In large rooms they can smear sprites.
- Shadows at the edge of the screen seem to flicker or stretch in some rooms. I can see the same thing without camera scroll. Maybe a vanilla bug.
- Motion sickness might be a problem. I wonder how much is due to noise (details) in the textures. You can play around with the parameters and see what works for you.

### Contact
If you have feedback, you can message me on Discord `@SchuhBaum#7246` or write an email to SchuhBaum71@gmail.com.

### License
There are two licenses available - MIT and Unlicense. You can choose which one you want to use.  

### Changelog
#### (Rain World v1.9)
v2.1.0:
- Added support for Rain World 1.9.
- Removed AutoUpdate.
- Disabled this mod for Safari mode for now.
- Fixed a bug where the base color of textures was not the correct "black". This could lead to lights freaking out at the borders. This does not effect textures that are already generated.
- Increaded maximum texture size to 16384x16384. Rain World v1.5's Unity version had a limit of 10000x10000. This means that certain room textures in Subterrainian are no longer cropped.
- Restructured code.
- Fixed a bug where one screen room textures would be stretched.
- Blacklisted the room "RM_AI" since the room textures cannot be merged correctly at this point. The room needs to be re-rendered first.
- Fixed a bug where graphics of some objects would be visible too late (resulting in pop-ins).
- (position type camera) Added new options (disabled by default). The camera can catch up with the player can even move ahead of the player when configured.
- (position type camera) Offset no longer depends on player inputs. Offset reverses when at border.
- (position type camera) Added an option for zeroG (disabled by default). If enabled, the camera will focus directly on the player during zeroG ignoring other position type parameters.
- Fixed a bug where clouds were not aligned.
- Changed implementation for RoomCamera_DrawUpdate() to an IL-Hook. This should improve compatibility with other mods.

v2.2.0:
- Fixed a bug where you would lose access to the options menu when the mod was disabled (without applying) and enabled immediately again.
- There are visual issues with snow (pop ins) that I couldn't figure out. As a workaround, snow is invisible now.
- Blacklisted Artificer's dream rooms.
- Added a transition for fade palettes.
- Fixed a bug where screen shakes were ignored.
- Fixed a bug where worm grass was unloaded too early.
- Re-enabled falling snow. Snow can "jump" visibly from camera to camera. I can prevent that but then I can't cover the whole room. :/ (Snow on the ground is still disabled.)
- Blacklisted the room 'SB_E05SAINT'.
- Removed the room 'GW_E02_PAST' from blacklisted rooms. The Artificer dream sequence is working for me. This room is also used as a regular room.

v2.3.0:
- Restructured code.
- Added support for the Safari mode.
- Simplified implementation of fade palettes.
- Reverted clouds such that they scroll. Added an offset instead such the edges are not visible in certain rooms.
- Added support for SplitScreen Co-op.
- Added support for multi-screen arena challenges.
- Fixed a bug where you couldn't access the options menu.
- Re-enabled and improving snow showing on room textures.
- Fixed a bug where the camera offset was not updated when the speed multiplier was set too low.
- Using RenderTextures (if possible) to do some of the work on the GPU when merging textures. This does not seem to speed things up much but reduces the memory consumption significantly during merging.
- Fixed a bug where a room would not get blacklisted correctly.

v2.4.0:
- Potentially fixed a bug where the palette transition would interfere with the day-night cycle.
- Added checks if the graphics cards supports the size of merged room textures. If these fail then the room gets blacklisted.
- Fixed a bug where in Safari mode the camera would vibrate when focusing on a shortcut by holding throw.
- Added a (still unused) setup for overriding shaders.
- Restructured code.
- Fixed a bug where snow would fall through the ceiling. As a downside, you have additional jumps for falling snow when changing screens in y.
- Added a section incompatibilities to the ingame mod description.
- (camera offset) Reworked implementation. The camera moves ahead but stay close enough. This way, turning around does not increase the distance that the camera "lags" behind compared to when this option is turned off.
- Restructured code.
- (camera offset) Still not content with it. I lowered the maximum a bit. Otherwise turning will instantly move the camera at maximum offset.
- Some small tweaks to how snow is displayed.
- (camera offset) Added some conditions besides player inputs to make resetting the camera offset less unintentional.

v2.4.9:
- Fixed a bug where the JollyCoop's player arrows would be misaligned when using non-default screen resolutions.
- Added a "Clear Cache" button to the options menu.
- (vanilla type camera) Added some changes to improve compatibility with SplitScreen Co-op.
- Restructured code.
- Fixed two bugs where the camera was not centered. Changed implementation.
- (region mods) Added this option (enabled by default). Clears the corresponding cached room textures when region mods get enabled or disabled. This is meant to make it easier when region mods change existing rooms.
- Restructured code.
- (region mods) Updates merged textures when the dimension of the room texture changes. Clears only room textures when at least one camera texture is shared with other mods or vanilla (more conservative).
- (region mods) Some small changes to make it more conservative.
- (region mods) Updated description to make clear that textures are also updated during gameplay and not just when region mods are enabled/disabled.
- Removed (or rather ignore) DeathFallFocus objects in rooms. These objects are used to change the height of pit fall indicators. I tried making it a full screen effect instead that moves with the camera. Even then the indicator pops in and out. This way the indicator is stuck at the bottom of the room.

#### (Rain World v1.5)
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
- Fixed a memory leak caused by calling `www.texture` which created texture copies.
- Split the fullscreen effect option into two options. One for fog and the other for the rest.
- Fixed three bugs where a variable was cleared too early.

v0.70:
- Restructured code. Using weak tables. Fixed a bug where the texture offset of region gates were not initialized. Restructured CheckBorders() + included patch from SplitScreenMod. 
- (vanilla type) Fixed a bug where the camera would slightly move after a screen transition when using vanilla camera positions. The lean effect now mimics vanilla lean effect instead of scaling with the camera box. This removes one parameter from the options menu.
- Slightly increase minimum speed for transitions and the position type camera. Camera moves at least one pixel per frame.
- Restructured code.
- (vanilla type) Map inputs for switching between centering and vanilla camera positions can be used during transitions. 
- Switched to BepInEx plugin. Changed priority (GUID) to be lower than SplitScreenMod. Otherwise curCamera is not updated during RoomCamera_DrawUpdate calls.
- Clouds account horizontally for a moving camera.
- Restructured code. Worm grass is only added when at least one patch is included.
- Switched to RGB24 since vanilla texture files are stored in RGB24 format. (Only loaded textures have an alpha channel.)
- Added a button for deleting all merged textures.
- Fixed a bug with Realm where optional dependencies were not recognized.
- Extended try-catch when merging room textures. This might help in case of out-of-memory exceptions during merging.
- Potentially reduced memory consumption when merging large regions at once. There are still other unresolved memory issues (fragmentation?).