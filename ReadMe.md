## SBCameraScroll
###### Version: 2.6.7
This is a mod for Rain World v1.9.

### Description
Creates a smooth, scrolling camera that moves with the slugcat. Based on pipi toki's CameraScroll mod.  
  
Here is a youtube video showing Rain World v1.5 + SBCameraScroll (+ other mods) in action:  
https://www.youtube.com/watch?v=ePJbY4GSMck

This mod generates (i.e. merges) camera textures for each room with multiple cameras. These textures are cached in the folders `levels` and `world` inside the folder:  
'Steam\SteamApps\common\Rain World\RainWorld_Data\StreamingAssets\mods\SBCameraScroll\'

If you want to manually generate all merged textures then you can go to the mod's options menu (in the Remix menu click on the name for this mod) and press the button `Create Cache`. In addition, there is the option `Region Mods` (enabled by default) that should update cached textures when mods modify existing rooms.  
If you want to delete all merged textures and let them be generated again then you can press the button `Clear Cache`.

This mod includes additional cameras:
- (Vanilla Type Camera) Behaves almost identical to the vanilla camera -- with one major difference. You can center the camera on the slugcat by pressing the map button. The keybinding can be configured using the mod [Improved Input Config](https://steamcommunity.com/sharedfiles/filedetails/?id=2944727862).  
- (Switch Type Camera) Allows you to switch between the other two camera types by pressing the map button. The keybinding can be configured using the mod [Improved Input Config](https://steamcommunity.com/sharedfiles/filedetails/?id=2944727862).

### Incompatibilities
- The zoom camera option in the Slugcat Eyebrow Raise mod.

### Installation
0. Update Rain World to version 1.9 if needed.
1. Download the file  `SBCameraScroll.zip` from [Releases](https://github.com/SchuhBaum/SBCameraScroll/releases/tag/v2.6.7).
2. Extract its content in the folder `[Steam]\SteamApps\common\Rain World\RainWorld_Data\StreamingAssets\mods`.
3. Start the game as normal. In the main menu select `Remix` and enable the mod. 

### Bug reports, FAQ & Known Issues
See the corresponding sections on the [Steam Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=2928752589) for this mod.

### Contact
If you have feedback, you can message me on Discord `@schuhbaum` or write an email to SchuhBaum71@gmail.com.

### License
There are two licenses available - MIT and Unlicense. You can choose which one you want to use.  

### Changelog
#### (Rain World v1.9)
v2.6.7:
- Snow on the level texture should be less pixelated now. (I am blind. I missed the snow texture in the class RoomCamera. It needs to fit the size of the level texture.)
- Adjusted some snow parameters and some DeepWater shader variables slightly (for consistency).
- Potentially fixed a bug where the level texture could be misaligned when the AboveCloudsView effect was used.
- Added an in-game message when camera textures have been merged.
- Added the button `Create Cache` to the Remix options menu.
- Fixed a bug where you could not use the button `Create Cache` after using `Clear Cache` and vice versa.
- The name and description of the `Create Cache` button is updated when used. This gives the user more information about the progress.
- Clicking the button `Create Cache` again aborts the action.

v2.6.0:
- (vanilla type camera) The button for centering the camera can be configured using the mod `Improved Input Config`.
- IL hooks should no longer log multiple times when other mods add these IL hooks as well.
- (vanilla type camera) Made transitions between centering and vanilla camera positions faster.
- Restored original mod id.
- (switch type camera) Added this camera type. You can switch between the position and vanilla type camera by pressing the map key. The keybinding for switching the camera type can be configured using the mod `Improved Input Config`.
- Fixed a bug where custom keybindings would be initialized more than once.
- Potentially fixed bad initialization.
- (improved input config) Added some conflict handling since it can be confusing and inconsistent otherwise. For example, using the map keybinding should be handled the same way as having no custom keybinding.
- (camera zoom) Added this option. Experimental. Set to 100% (10) by default. Works for the most part, but when zoomed out shaders like the water shaders are cut off. Can be more pixelated and sometimes the the sprites misalign slightly with their "shadows".
- (vanilla type camera) Fixed a bug where the variable camera_box_from_border_y was not set correctly.
- Use only one smoothing_factor. It feels weird to have different step sizes.
- (vanilla type camera) Changed smooth transition logic slightly to make sure that the step size is the same in x and y.
- ~~Increased the shader variable _screenSize in larger rooms. This variable is used to sample the level texture in steps. This makes the underwater shader less blocky and pixelated. I haven't found side effects. Does not help with the snow shader.~~ Reverted. This messes with the map otherwise.
- Added a pdb file for debugging.
- Modified the underwater shader. This should improve the water distortion effect in larger rooms.

v2.5.0:
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
- Blacklisted Moon's room SL_AI as a workaround.  
Context: There is an issue with the shader MoonProjection. It seems that it is set to the middle of the current room texture. Since the merged textures are larger it is misaligned and the projections are not placed correctly. I can move the projections but the shader stays in place. This cuts off part of the projected image.
- Changed implementation for logging the mod options. I saw some logs that didn't contain these for some reason. Should be more reliable now.

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

#### (Rain World v1.5)
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

v0.31:
- Camera parameters can be configured now.
- Slightly simplified camera logic. Camera only moves when the player moves. The camera also does not move when the player changes direction.
- (velocity type) Added this option (disabled by default). This is more a mario-type camera. The camera smoothly matches the players velocity. It fully matches it when OuterCameraBox is reached. Gives the camera a more rigid feel to it. Did not work too well for me (given frame skips and detailed textures but let me know what your experience is like).
- (full screen effects) Added this option (enabled by default). When disabled, full screen effect like fog and bloom are not shown. In particular, fog can noticeably move with the screen.

v0.3:
- Major overhaul. Restructured code.
- Merges camera texture at runtime.
- Added support for custom regions. You need the CustomRegion mod (CRS). When found, the "Mods\CustomResources" folder is searched as well for camera textures.
- (merge while loading) Added this option. When enabled (default), the camera textures for each room are merged when the region gets loaded. When disabled, camera textures are merged for each room on demand.  
 Merging happens only once and the files are stored in your "Mods\SBCameraScroll" folder. This process can take a while. Merging all rooms in Deserted Wastelands took me around three minutes.
 If room cameras get visually changed then you need to delete the corresponding merged files in order to update them.
- Overhaul of the camera logic. Main features are: 1) No movement when close to the camera (box-type camera). 2) Smoothed acceleration when further away. 3) Matches player speed when a certain distance is reached. 4) Stops at room borders (unless you are in the final room).
- (extended debug logs) Added this option (disabled by default). Add logs when the game skips frames. In this case, the camera can feel laggy. Happens quite frequently for me :(. A scrolling camera makes them more noticeable.