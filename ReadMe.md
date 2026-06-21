## SBCameraScroll
###### Version: 3.2.8
This is a mod for Rain World v1.11.

### Description
Creates a smooth, scrolling camera that moves with the slugcat. Based on pipi toki's CameraScroll mod.  
  
Here is a youtube video showing Rain World v1.5 + SBCameraScroll (+ other mods) in action:  
https://www.youtube.com/watch?v=ePJbY4GSMck

This mod includes additional cameras:
- (Vanilla Type Camera) Behaves almost identical to the vanilla camera -- with one major difference. You can center the camera on the slugcat by pressing the map button. The keybinding can be configured using the mod [Improved Input Config](https://steamcommunity.com/sharedfiles/filedetails/?id=2944727862).  
- (Switch Type Camera) Allows you to switch between the other two camera types by pressing the map button. The keybinding can be configured using the mod [Improved Input Config](https://steamcommunity.com/sharedfiles/filedetails/?id=2944727862).

### Incompatibilities
- The zoom camera option in the Slugcat Eyebrow Raise mod.

### Installation
0. Update Rain World to version 1.11 if needed.
1. Download the file  `SBCameraScroll.zip` from [Releases](https://github.com/SchuhBaum/SBCameraScroll/releases/tag/v3.2.8).
2. Extract its content in the folder `[Steam]\SteamApps\common\Rain World\RainWorld_Data\StreamingAssets\mods`.
3. Start the game as normal. In the main menu select `Remix` and enable the mod. 

### Bug reports, FAQ & Known Issues
See the corresponding sections on the [Steam Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=2928752589) for this mod.

### Contact
If you have feedback, you can message me on Discord `@schuhbaum` or write an email to SchuhBaum71@gmail.com.

### License
See the file LICENSE-MIT.

### Changelog
#### (Rain World v1.11)
v3.2.8:
- Add the missing change for v3.2.7 that I removed accidentally.

v3.2.7:
- Fixed a bug where glyphs would have the wrong offset.

v3.2.6:
- Fixed a bug in Five Pebbles where Mycelium would not spawn or just spawn in
  one screen.
- Potentially improved the visuals of glyphs.

v3.2.5:
- Fixed two bugs for projection glyphs. One where too few spawned initially. One
  where they would spawn too slowly.

v3.2.4:
- Fixed a visual bug where projection glyphs would not be shown on the right and
  top end of rooms. The offset was based on the texture size while the glyph
  grid size is based only on the room size.

v3.2.3:
- Fixed a visual bug where projection glyphs were stretched and only located at
  the top left of a room.

v3.2.2:
- Fixed a bug where the room texture would get pixelated in some rooms in the
  Watcher campaign.

v3.2.1:
- Blacklisted the Weaver ending room. Otherwise, the vanilla camera scroll is
  not working.

v3.2.0:
- Fixed a bug where a hook was not applied. Maybe leading to level texture not
  being shown.

#### (Rain World v1.10)
v3.1.7:
- Blacklist rooms if the graphics card cannot handle large textures. This fixes
  a bug where you get black screens in larger rooms.

v3.1.6:
- (LevelHeat shader) Fixed a bug where using portals would flicker the screen.
  And where using Watcher's ability with max karma would not change the palette.

v3.1.5:
- (reduced memory usage) Marked the temporary cache texture as non-readable.
  This means that it does not have a RAM copy anymore.

v3.1.4:
- Using the level texture instead of the ripple texture. This fixed issues where
  the empty / black ripple texture can block the view in some rooms.

v3.1.3:
- (experimental; reduced memory usage) Added this option (disabled by default).
  I haven't tested it enough to tell, how big or small the effect is. When
  enabled, it does not cache whole screen textures (1400x800 pixels each). Some
  creatures need to know the background color (like white lizards and worm
  grass). In that case, the pixel colors are fetched from the GPU and cached
  directly. 

v3.1.2:
- Remove the ripple texture that is used for portals. In vanilla, you can see
  parts of the room on the other side. But creating another larger texture
  creates memory overhead. And if the room sizes misalign then the texture is
  still stretched.
- Fixed a bug where the wrong texture would be merged if the REPLACEROOM feature
  is used.

v3.1.1:
- Fixed a bug where the render texture was not initialized correctly. The deep
  water shader would not be drawn over offscreen areas.

v3.1.0:
- (ripple trail effect) Added this option (enabled by default). Watcher's ripple
  trail moves with the camera. You can disable the effect as a workaround.
- dev note: Renamed the generic classes `Attached_Fields` to their specific
  counterpart (RoomCameraFields, AbstractRoomFields, etc.). The old versions are
  declared obsolete and not used anymore.

v3.0.9:
- Modded the shader `LevelBlend`. Now, the ripple distortion effect should be way
  less extreme and scale much better in larger rooms. I am looking at you
  `WRSA_L01`.

v3.0.8:
- Fixed a bug where the ripple effect would jump visually when transitioning
  screens with camera scroll active.

v3.0.7:
- Fixed an IO exception where the mod settings menu would not load.

v3.0.6:
- (just-in-time merging) This is now the only option. No more cache. You can
  still delete the cache using the button in the mod settings menu.
- Simplified the code base and did some refactoring.
- Potentially fixed a bug where you would get a black screen when using
  SplitScreen Co-op.

v3.0.5:
- (Watcher campaign) Fixed a bug where the overlapped ripple texture would be
  stretched.

v3.0.4:
- (not just-in-time merging) Fixed a bug where the warp room would be stretched
  when caching / merging camera textures was too slow.

v3.0.3:
- Fixed a bug where specific hooks were added multiple times.

v3.0.2:
- Fixed a bug when playing Jolly Co-op where the camera got stuck when switching
  to a player in a different room.

v3.0.1:
- Improved compatibility with the Watcher campaign. I updated the modded shaders
  and fixed some related bugs.

v3.0.0:
- Added support for Rain World v1.10. Some options are not tested yet. There
  still might be issues.

#### (Rain World v1.9)
v2.9.9:
- (just-in-time merging) Now, enabled by default. Even without fixing memory issues, this is better than the need to build a giant cache. But it slightly increases the loading times for rooms. I did not find this noticable though.

v2.9.8:
- (camera zoom) Fixed a bug where camera zoom was used while SplitScreen Co-op was enabled. This can cause screens to overlap.

v2.9.7:
- (dynamic zoom) Fixed a bug when resetting the camera zoom. Removed the workaround.
- No camera scroll does not blacklist the room anymore. This had side effects. Some mod settings would not work unless camera scroll in one-screen rooms was enabled.

v2.9.6:
- (dynamic zoom) Added this option. When enabled, each room is zoomed as needed to avoid black borders when using custom resolutions.

v2.9.5:
- Modded the DeepProcessing shader. It should be fixed / much better now (see room DM\_O2).

v2.9.4:
- (custom resolution) Added 960x540 as lower bounds. You can get a black screen
  if the resolution is too low.

v2.9.3:
- (resolution) Added a text field for custom resolutions.

v2.9.2:
- (camera zoom) Increased the maximum zoom in the mod settings to 400%.

v2.9.1:
- (just-in-time merging) Fixed a freeze. I gave the camera index -1 to a
vanilla function and got index out of bounds.

v2.9.0:
- Fixed a bug when crs changes the name of a room. It could happen that camera
textures were misaligned or not merged.

v2.8.9:
- Modded some shaders to fix stretched clouds. In vanilla the cloud light mask is used for the full level texture. For merged textures this mask gets stretched.

v2.8.8:
- (just-in-time merging) Fixed two vanilla exceptions. They happened when the rooms loaded too slowly.

v2.8.7:
- (experimental: just-in-time merging)  
  Added this option (disabled by default). When enabled, the GPU merges the camera textures just-in-time. The cache is not used. Rooms might load slightly slower.  
  Every camera texture is loaded normally and saved. Then, they are send to the GPU. Resizing the merged texture on the CPU would re-allocate memory. The camera textures have constant size. I hoped that this would fix the memory issues. It seems that it does not improve memory consumption.

v2.8.6:
- (experimental: fill empty spaces) Added this option (disabled by default). When enabled during merging, unknown pixels are set to the nearest pre-rendered pixel vertically instead of defaulting to black. You might need to clear the cache before using this.

v2.8.5:
- Fixed a bug where the water sprite would not reach to the bottom.

v2.8.4:
- (camera zoom) Fixed a vanilla bug where the water sprite would not scroll and possibly flip. This can be noticed when the camera is zoomed out.

v2.8.3:
- Fixed a bug in the LevelColor shader where the plants in farm arrays would appear less colorful.

v2.8.2:
- (merge while loading) Wrapped in a coroutine. This way the game is not completely frozen while textures are being merged. And the merging info message appears instantly.

v2.8.1:
- (resolution) Fixed a bug where the height would not be reset after changing back to `Default`.

v2.8.0:
- Modded the underwater light shader. It had the same distortion pixelation problem as the normal underwater shader.
- Fixed a bug where the wrong room would be merged or blacklisted when using the `REPLACEROOM` feature in CRS.
- Fixed another bug regarding that where the wrong room would be displayed.
- (resolution width) Added this option. Overrides the current resolution width. Can be used to zoom out with less pixelation issues compared to the option "camera zoom". Might reduce black borders on larger monitors.
- (SplitScreen Co-op compatibility) Fixed some bugs or rather added support for four player split screen.
- (position type camera) Added a minimum speed when reaching room borders. Before the last update the camera would stop when moving too slowly (in all cases). But that can leave a black border between screens when using split screen. This way it will reach the exact position as well but less slowly.
- Changed the depth calculation for shortcut symbols and such. It does not depend on the position of the camera anymore. The depth is static after all and only needs to match the pre-rendered visuals of the room.
- (SplitScreen Co-op compatibility) Changed the parameters of the functions Shader.Set..() to match the format used in v1.9.15. This might fix some visual issues when using SplitScreen Co-op.
- Always checking File.Exists() when using the function WorldLoader.FindRoomFile() (for consistency and compatibility with MergeFix).
- (SplitScreen Co-op) Fixed a bug. When the camera is zoomed out by increasing the resolution width then the camera should no longer scroll in small rooms.
- (resolution) Renamed resolution width to resolution. The height can now be increased as well. There are some side effects. The main menu is stuck at the bottom for example. Increasing the height does not work for the second screen in SplitScreen Co-op and will be ignored in that case.

v2.7.0:
- Snow on the level texture should be less pixelated now. (I am blind. I missed the snow texture in the class RoomCamera. It needs to fit the size of the level texture.)
- Adjusted some snow parameters and some DeepWater shader variables slightly (for consistency).
- Potentially fixed a bug where the level texture could be misaligned when the AboveCloudsView effect was used.
- Added an in-game message when camera textures have been merged.
- (create cache) Added the button `Create Cache` to the Remix options menu.
- (create cache) Fixed a bug where you could not use the button `Create Cache` after using `Clear Cache` and vice versa.
- (create cache) The name and description is updated while creating the cache. This gives the user more information about the progress.
- (create cache) Clicking the button again aborts the action.
- Fixed a bug where one-screen arena rooms would scroll (even with the option `One Screen Rooms` disabled) if you have SplitScreen Co-op enabled. In story mode this is intended since one screen rooms need to scroll when split.
- (create cache) The updated description is shown even when nothing is currently focused.
- (fog) Modded the fog shader. The fog effect should now scroll properly.
- (full screen effects) Now includes the fog full screen effect. The fog option is removed.

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
