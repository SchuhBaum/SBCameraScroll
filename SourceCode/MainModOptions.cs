
namespace SBCameraScroll;

public class MainModOptions : OptionInterface {
    public static MainModOptions main_mod_options = new();

    //
    // parameters
    //

    private readonly float _spacing = 20f;
    private readonly float _font_height = 20f;
    private readonly int _number_of_checkboxes = 3;
    private readonly float _check_box_size = 24f;
    private float CheckBoxWithSpacing => _check_box_size + 0.25f * _spacing;

    private static readonly string[] _camera_type_keys = new string[3] { "1-position", "2-vanilla", "3-switch" };
    private static readonly string[] _camera_type_descriptions = new string[3]
    {
            "This type tries to stay close to the player. A larger distance means a faster camera.\nThe smoothing factor determines how much of the distance is covered per frame.",
            "Vanilla-style camera. You can center the camera by pressing the map button. Pressing the map button again will revert to vanilla camera positions.\nWhen the player is close to the edge of the screen the camera jumps a constant distance.",
            "You can switch between the other two camera types by pressing the map button.\nThe keybinding can be configured using the mod 'Improved Input Config'."
    };

    //
    // options
    //

    public static Configurable<string> camera_type = main_mod_options.config.Bind("cameraType", _camera_type_keys[0], new ConfigurableInfo(_camera_type_descriptions[0], null, "", "Camera Type"));

    public static Configurable<bool> full_screen_effects = main_mod_options.config.Bind("fullScreenEffects", defaultValue: true, new ConfigurableInfo("When disabled, full screen effects like fog, bloom and melt are removed.", null, "", "Full Screen Effects"));
    public static Configurable<bool> scroll_one_screen_rooms = main_mod_options.config.Bind("scrollOneScreenRooms", defaultValue: false, new ConfigurableInfo("When disabled, the camera does not scroll in rooms with only one screen.", null, "", "One Screen Rooms")); // Automatically enabled when using SplitScreenMod.
    public static Configurable<bool> ripple_trail_effect = main_mod_options.config.Bind("rippleTrailEffect", defaultValue: true, new ConfigurableInfo("Watcher's ripple trail moves with the camera. You can disable the effect as a workaround.", null, "", "Ripple Trail Effect"));

    public static Configurable<int> smoothing_factor_slider = main_mod_options.config.Bind("smoothing_factor_slider", defaultValue: 8, new ConfigurableInfo("Determines how much of the distance is covered per frame. This is used when switching cameras as well to ensure a smooth transition.", new ConfigAcceptableRange<int>(0, 35), "", "Smoothing Factor (8)"));

    //
    //
    //

    public static Configurable<int> innercameraboxx_position = main_mod_options.config.Bind("innerCameraBoxX_Position", defaultValue: 2, new ConfigurableInfo("The camera does not move when the player is closer than this.", new ConfigAcceptableRange<int>(0, 35), "", "Minimum Distance in X (2)"));
    public static Configurable<int> innercameraboxy_position = main_mod_options.config.Bind("innerCameraBoxY_Position", defaultValue: 2, new ConfigurableInfo("The camera does not move when the player is closer than this.", new ConfigAcceptableRange<int>(0, 35), "", "Minimum Distance in Y (2)"));
    public static Configurable<bool> cameraoffset_position = main_mod_options.config.Bind("cameraOffset_Position", defaultValue: false, new ConfigurableInfo("When enabled, the camera can move ahead but still stays within the minimum distance.", null, "", "Camera Offset"));
    public static Configurable<int> cameraoffsetspeedmultiplier_position = main_mod_options.config.Bind("cameraOffsetSpeedMultiplier_Position", defaultValue: 2, new ConfigurableInfo("Determines how fast the camera pulls ahead. When set to 1.0 then the offset changes as fast as the player moves.", new ConfigAcceptableRange<int>(1, 50), "", "Camera Offset Speed Multiplier (2)"));

    //
    //
    //

    public static Configurable<int> outercameraboxx_vanilla = main_mod_options.config.Bind("outerCameraBoxX_Vanilla", defaultValue: 9, new ConfigurableInfo("The camera changes position if the player is closer to the edge of the screen than this value.", new ConfigAcceptableRange<int>(0, 35), "", "Distance from the Edge in X (9)"));
    public static Configurable<int> outercameraboxy_vanilla = main_mod_options.config.Bind("outerCameraBoxY_Vanilla", defaultValue: 1, new ConfigurableInfo("The camera changes position if the player is closer to the edge of the screen than this value.", new ConfigAcceptableRange<int>(0, 35), "", "Distance to the Edge in Y (1)"));

    //
    //
    //

    public static Configurable<bool> reduced_memory_usage = main_mod_options.config.Bind("reduced_memory_usage", defaultValue: false, new ConfigurableInfo("When enabled, caches only pixel colors on demand (e.g. when worm grass or\nwhite lizards request them). This is slower but should reduce memory usage.", null, "", "Reduced Memory Usage"));

    public static Configurable<int> camera_zoom_slider = main_mod_options.config.Bind("camera_zoom_slider", defaultValue: 10, new ConfigurableInfo("WARNING: Can lead to visual glitches and shader issues.\nNot used when the SplitScreen Co-op mod is active.", new ConfigAcceptableRange<int>(5, 40), "", "Camera Zoom (10)"));
    public static Configurable<bool> dynamic_zoom = main_mod_options.config.Bind("dynamicZoom", defaultValue: false, new ConfigurableInfo("When enabled, the camera zoom is adjusted dynamically per room. This removes any black borders when using custom resolutions.\nEnables scrolling in one-screen rooms. Disabled when SplitScreen Coop is used.", null, "", "Dynamic Zoom"));
    public static Configurable<string> resolution = main_mod_options.config.Bind("resolution", "Default", new ConfigurableInfo("Overrides the screen resolution. Can be used to zoom out with less\npixelation issues. WARNING: Can lead to visual glitches and shader issues.", null, "", "Resolution:"));
    public static Configurable<string> custom_resolution = main_mod_options.config.Bind("customResolution", "", new ConfigurableInfo("Requires the format \"WIDTHxHEIGHT\". Needs to be at least 960x540.\nFirst, you need to select \"Custom\" in the Resolution combo box.", null, "", "Custom Resolution:"));

    //
    // variables
    //

    public int? saved_resolution_index = null;
    public Vector2? saved_resolution = null;

    private Vector2 _margin_x = new();
    private Vector2 _pos = new();

    private readonly List<float> _box_end_positions = new();

    //
    //

    private readonly List<Configurable<bool>> _check_box_configurables = new();
    private readonly List<OpLabel> _check_boxes_text_labels = new();

    private readonly List<Configurable<string>> _combo_box_configurables = new();
    private readonly List<List<ListItem>> _combo_box_lists               = new();
    private readonly List<bool> _combo_box_allow_empty                   = new();
    private readonly List<OpLabel> _combo_boxes_text_labels              = new();

    private readonly List<Configurable<int>> _slider_configurables = new();
    private readonly List<string> _slider_main_text_labels         = new();
    private readonly List<OpLabel> _slider_text_labels_left        = new();
    private readonly List<OpLabel> _slider_text_labels_right       = new();

    private readonly List<Configurable<string>> _text_box_configurables = new();
    private readonly List<OpLabel> _text_box_labels = new();

    private readonly List<OpLabel> _text_labels = new();

    //
    //

    private OpComboBox? _camera_type_combo_box = null;
    private int _last_camera_type              = 0;

    // The button is initialized later.
    private OpSimpleButton _clear_cache_button = new OpSimpleButton(new(), new());

    private OpComboBox? _resolution_combo_box      = null;
    private OpTextBox? _custom_resolution_text_box = null;

    private OpSlider?   _zoom_slider  = null;
    private OpCheckBox? _dynamic_zoom = null;

    //
    // main
    //

    private MainModOptions() {
        On.OptionInterface._SaveConfigFile -= OptionInterface_SaveConfigFile;
        On.OptionInterface._SaveConfigFile += OptionInterface_SaveConfigFile;
    }

    //
    // public
    //

    public void Apply_And_Log_All_Options() {
        // 0: Position type, 1: Vanilla type
        RoomCameraMod.camera_type = (RoomCameraMod.CameraType)Array.IndexOf(_camera_type_keys, camera_type.Value);
        Debug.Log($"{mod_id}: cameraType {RoomCameraMod.camera_type}");

        Debug.Log($"{mod_id}: Option_FullScreenEffects {Option_FullScreenEffects}");
        Debug.Log($"{mod_id}: Option_ScrollOneScreenRooms {Option_ScrollOneScreenRooms}");

        camera_zoom = 0.1f * camera_zoom_slider.Value;
        Set_Resolution(resolution.Value);
        smoothing_factor = smoothing_factor_slider.Value / 50f;

        Debug.Log($"{mod_id}: camera_zoom {camera_zoom}");
        Debug.Log($"{mod_id}: resolution_width {resolution.Value}");
        Debug.Log($"{mod_id}: smoothing_factor {smoothing_factor}");

        if (RoomCameraMod.camera_type is RoomCameraMod.CameraType.Position or RoomCameraMod.CameraType.Switch) {
            camera_box_x = 20f * innercameraboxx_position.Value;
            camera_box_y = 20f * innercameraboxy_position.Value;
            Debug.Log($"{mod_id}: camera_box_x {camera_box_x}");
            Debug.Log($"{mod_id}: camera_box_y {camera_box_y}");

            offset_speed_multiplier = 0.1f * cameraoffsetspeedmultiplier_position.Value;
            Debug.Log($"{mod_id}: Option_CameraOffset {Option_CameraOffset}");
            Debug.Log($"{mod_id}: offset_speed_multiplier {offset_speed_multiplier}");
        }

        if (RoomCameraMod.camera_type is RoomCameraMod.CameraType.Vanilla or RoomCameraMod.CameraType.Switch) {
            camera_box_from_border_x = 20f * outercameraboxx_vanilla.Value;
            camera_box_from_border_y = 20f * outercameraboxy_vanilla.Value;
            Debug.Log($"{mod_id}: camera_box_from_border_x {camera_box_from_border_x}");
            Debug.Log($"{mod_id}: camera_box_from_border_y {camera_box_from_border_y}");
        }

        Debug.Log($"{mod_id}: Option_ReducedMemoryUsage {Option_ReducedMemoryUsage}");
        if (Option_ReducedMemoryUsage) {

        }
    }

    public void ClearCacheButton_OnClick(UIfocusable _) {
        try {
            Directory.Delete($"{mod_directory_path}world", recursive: true);
        } catch {}
        try {
            Directory.Delete($"{mod_directory_path}levels", recursive: true);
        } catch {}

        ClearCacheButton_UpdateColor();
    }

    public void ClearCacheButton_UpdateColor() {
        bool is_levels_empty = true;
        bool is_world_empty  = true;
        try {
            is_levels_empty = Directory.GetFiles($"{mod_directory_path}levels", "*.*", SearchOption.AllDirectories).Length == 0;
        } catch {}
        try {
            is_world_empty = Directory.GetFiles($"{mod_directory_path}world", "*.*", SearchOption.AllDirectories).Length == 0;
        } catch {}

        _clear_cache_button.colorEdge = new Color(1f, 1f, 1f, 1f);

        if (is_levels_empty && is_world_empty) {
            _clear_cache_button.colorFill = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            _clear_cache_button.greyedOut = true;
            return;
        }

        _clear_cache_button.colorFill = new Color(1f, 0.0f, 0.0f, 0.5f);
        _clear_cache_button.greyedOut = false;
    }

    public void ReInitialize_Futile() {
        //
        // the hook for FScreen.ctor is applied after the FScreen in Futile is created;
        // therefore, re-initialize some of the things;
        //

        Futile? futile = Futile.instance;
        if (futile == null) return;
        Futile.screen = new FScreen(futile._futileParams);
        futile.InitCamera(futile._camera, 1);

        if (futile.splitScreen) {
            futile.InitCamera(futile._camera2, 2);
        }

        if (Display.main.systemWidth < 1366 || Display.main.systemHeight < 768) {
            Futile.screen.renderTexture.filterMode = FilterMode.Bilinear;
        } else {
            Futile.screen.renderTexture.filterMode = FilterMode.Point;
        }

        futile._cameraImage.texture = Futile.screen.renderTexture;
        futile.UpdateCameraPosition();
    }

    public void Set_Resolution(string resolution_string) {
        if (resolution_string == "Custom") {
            resolution_string = custom_resolution.Value;
        }

        // there are some visual bugs and menues are misplaced; like the main menu 
        // for example is stuck at the bottom; the jollycoop menu is stuck at the 
        // top; you can move and zoom the game objects; but doing that on the main
        // futile game object has side effects for the in-game rendered room_camera
        // stuff; for the menues too; the jollycoop menu might be placed offscreen;

        string[] split_string = resolution_string.Split('x');
        if (split_string.Length < 2) {
            Reset_Resolution();
            return;
        }

        if (!int.TryParse(split_string[0], out int resolution_width) || !int.TryParse(split_string[1], out int resolution_height)) {
            Reset_Resolution();
            return;
        }

        if (resolution_width < 960 || resolution_height < 540) {
            Reset_Resolution();
            return;
        }

        Reset_Resolution(apply_immediately: false);
        Options options        = rainWorld.options;
        saved_resolution_index = options.resolution;
        saved_resolution       = Options.screenResolutions[(int)saved_resolution_index];

        // I am confused. I assumed that the height is hardcoded. But it is not.
        //
        // Observations:
        // Let (width_old, height_old) := (1368, 768) and (monitor_width,
        // monitor_height) := (1920, 1200).
        //
        // 16/10 but width_new < width_old (1229x768):
        //     This is the default in vanilla. It just zooms in. But no black
        //     borders anymore.
        // 16/10 but height_new > height_old (1368x855):
        // no 16/10 and height_new = monitor_height (1368x1200):
        // no 16/10 and width_new > width_old (1600x768):
        //     This is fine. It just zooms out.
        // no 16/10 and height_new > monitor_height (1368x1800):
        // no 16/10 and width_new > monitor_width (2880x768):
        //     This distorts the sprites. The sprites are squashed. But so far,
        //     they are still aligned. No missing sprites.
        // 16/10 but width_new > monitor_width and height_new > monitor_height (2880x1800):
        //     This is fine. But it is extremely zoomed out.
        //
        // Conclusion:
        //     You neither need 16/9 nor 16/10, nor be within the bounds of the monitor
        //     resolution.

        // the second screen does not get initialized correctly in split screen coop 
        // when the height is larger than 768f; the zoom does not match;
        Options.screenResolutions[(int)saved_resolution_index] = is_split_screen_coop_enabled ? new(resolution_width, 768f) : new(resolution_width, resolution_height);

        ReInitialize_Futile();
        rainWorld.options.OnLoadFinished();
    }

    public override void Initialize() {
        base.Initialize();
        int number_of_tabs = 4;
        Tabs = new OpTab[number_of_tabs];

        //-------------//
        // general tab //
        //-------------//

        int tab_index = 0;
        Tabs[tab_index] = new OpTab(this, "General");
        InitializeMarginAndPos();

        // Title
        AddNewLine();
        AddTextLabel("SBCameraScroll Mod", big_text: true);
        DrawTextLabels(ref Tabs[tab_index]);

        // Subtitle
        AddNewLine(0.5f);
        AddTextLabel($"Version {version}", FLabelAlignment.Left);
        AddTextLabel($"by {author}", FLabelAlignment.Right);
        DrawTextLabels(ref Tabs[tab_index]);

        // Content //
        AddNewLine();
        AddBox();

        AddTextLabel("General:", FLabelAlignment.Left);
        DrawTextLabels(ref Tabs[tab_index]);

        AddNewLine();

        List<ListItem> camera_types = new()
        {
            new ListItem(_camera_type_keys[0], "Position (Default)") { desc = _camera_type_descriptions[0] },
            new ListItem(_camera_type_keys[1], "Vanilla") { desc = _camera_type_descriptions[1] },
            new ListItem(_camera_type_keys[2], "Switch") { desc = _camera_type_descriptions[2] }
        };
        AddComboBox(camera_type, camera_types, (string)camera_type.info.Tags[0]);
        DrawComboBoxes(ref Tabs[tab_index]);

        AddNewLine(1.25f);

        // the comboBox keeps overlapping;
        AddNewLine(3f);

        AddCheckBox(full_screen_effects, (string)full_screen_effects.info.Tags[0]);
        AddCheckBox(scroll_one_screen_rooms, (string)scroll_one_screen_rooms.info.Tags[0]);
        AddCheckBox(ripple_trail_effect, (string)ripple_trail_effect.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[tab_index]);

        AddNewLine();

        AddSlider(smoothing_factor_slider, (string)smoothing_factor_slider.info.Tags[0], "0%", "70%");
        DrawSliders(ref Tabs[tab_index]);

        AddNewLine(3f);

        // This button has the same size as apply and back button in
        // ConfigMachine.
        _clear_cache_button = new(new(_pos.x + (_margin_x.y - _margin_x.x) / 2f - 55f + 65f, _pos.y), new(110f, 30f), "CLEAR CACHE") {
            description = "The cache is not used anymore. Just clear it."
        };
        ClearCacheButton_UpdateColor();
        System.Reflection.EventInfo event_info = _clear_cache_button.GetType().GetEvent("OnClick");
        Delegate event_handler = Delegate.CreateDelegate(event_info.EventHandlerType, this, "ClearCacheButton_OnClick");
        event_info.AddEventHandler(_clear_cache_button, event_handler);

        Tabs[tab_index].AddItems(_clear_cache_button);

        DrawBox(ref Tabs[tab_index]);

        //-------------------//
        // position type tab //
        //-------------------//

        tab_index++;
        Tabs[tab_index] = new OpTab(this, "Position");
        InitializeMarginAndPos();

        // Title
        AddNewLine();
        AddTextLabel("SBCameraScroll Mod", big_text: true);
        DrawTextLabels(ref Tabs[tab_index]);

        // Subtitle
        AddNewLine(0.5f);
        AddTextLabel($"Version {version}", FLabelAlignment.Left);
        AddTextLabel($"by {author}", FLabelAlignment.Right);
        DrawTextLabels(ref Tabs[tab_index]);

        // Content //
        AddNewLine();
        AddBox();

        AddTextLabel("Position Type Camera:", FLabelAlignment.Left);
        DrawTextLabels(ref Tabs[tab_index]);

        AddNewLine();

        AddSlider(innercameraboxx_position, (string)innercameraboxx_position.info.Tags[0], "0 tiles", "35 tiles");
        DrawSliders(ref Tabs[tab_index]);

        AddNewLine(2f);

        AddSlider(innercameraboxy_position, (string)innercameraboxy_position.info.Tags[0], "0 tiles", "35 tiles");
        DrawSliders(ref Tabs[tab_index]);

        AddNewLine();

        AddCheckBox(cameraoffset_position, (string)cameraoffset_position.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[tab_index]);

        AddNewLine();

        AddSlider(cameraoffsetspeedmultiplier_position, (string)cameraoffsetspeedmultiplier_position.info.Tags[0], "0.1", "5.0");
        DrawSliders(ref Tabs[tab_index]);

        DrawBox(ref Tabs[tab_index]);

        //------------------//
        // vanilla type tab //
        //------------------//

        tab_index++;
        Tabs[tab_index] = new OpTab(this, "Vanilla");
        InitializeMarginAndPos();

        // Title
        AddNewLine();
        AddTextLabel("SBCameraScroll Mod", big_text: true);
        DrawTextLabels(ref Tabs[tab_index]);

        // Subtitle
        AddNewLine(0.5f);
        AddTextLabel($"Version {version}", FLabelAlignment.Left);
        AddTextLabel($"by {author}", FLabelAlignment.Right);
        DrawTextLabels(ref Tabs[tab_index]);

        // Content //
        AddNewLine();
        AddBox();

        AddTextLabel("Vanilla Type Camera:", FLabelAlignment.Left);
        DrawTextLabels(ref Tabs[tab_index]);

        AddNewLine();

        AddSlider(outercameraboxx_vanilla, (string)outercameraboxx_vanilla.info.Tags[0], "0 tiles", "35 tiles"); // default is 9 => 180f // I think 188f would be exactly vanilla
        DrawSliders(ref Tabs[tab_index]);

        AddNewLine(2f);

        AddSlider(outercameraboxy_vanilla, (string)outercameraboxy_vanilla.info.Tags[0], "0 tiles", "35 tiles"); // default is 1 => 20f // I think 18f would be exactly vanilla
        DrawSliders(ref Tabs[tab_index]);

        DrawBox(ref Tabs[tab_index]);

        //------------------//
        // experimental tab //
        //------------------//

        tab_index++;
        Tabs[tab_index] = new OpTab(this, "Experimental");
        InitializeMarginAndPos();

        // Title
        AddNewLine();
        AddTextLabel("SBCameraScroll Mod", big_text: true);
        DrawTextLabels(ref Tabs[tab_index]);

        // Subtitle
        AddNewLine(0.5f);
        AddTextLabel($"Version {version}", FLabelAlignment.Left);
        AddTextLabel($"by {author}", FLabelAlignment.Right);
        DrawTextLabels(ref Tabs[tab_index]);

        // Content //
        AddNewLine();
        AddBox();

        AddTextLabel("Experimental:", FLabelAlignment.Left);
        DrawTextLabels(ref Tabs[tab_index]);

        AddNewLine();

        AddCheckBox(reduced_memory_usage, (string)reduced_memory_usage.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[tab_index]);

        AddNewLine();

        AddSlider(camera_zoom_slider, (string)camera_zoom_slider.info.Tags[0], "50%", "400%");
        DrawSliders(ref Tabs[tab_index]);

        AddNewLine();

        AddCheckBox(dynamic_zoom, (string)dynamic_zoom.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[tab_index]);

        AddNewLine();

        AddTextBox(custom_resolution, (string)custom_resolution.info.Tags[0]);
        DrawTextBoxes(ref Tabs[tab_index], offset_x_percent: 0f, width_label_percent: 0.3f, width_text_box_percent: 0.7f);

        AddNewLine();

        List<ListItem> resolution_item_list = new() { new ListItem("Default", "Default", 0) { desc = "Resets the screen resolution." } };
        foreach (Resolution resolution in UnityEngine.Screen.resolutions) {
            ListItem item = new($"{resolution.width.ToString()} x {resolution.height.ToString()}", resolution.width) { desc = $"Sets the screen resolution to {resolution} pixels." };
            if (resolution_item_list.Contains(item)) continue;
            resolution_item_list.Add(item);
        }
        resolution_item_list.Add(new ListItem("Custom", "Custom", 9999) { desc = $"Uses the resolution from the text box `{(string)custom_resolution.info.Tags[0]}`." });

        AddComboBox(resolution, resolution_item_list, (string)resolution.info.Tags[0]);
        // DrawComboBoxes(ref Tabs[tab_index]);
        DrawComboBoxes(ref Tabs[tab_index], offset_x_percent: 0f, width_label_percent: 0.3f, width_combo_box_percent: 0.7f);

        AddNewLine(5.25f);

        DrawBox(ref Tabs[tab_index]);

        //
        //
        //

        // save UI elements in variables for Update() function
        foreach (OpTab tab in Tabs) {
            foreach (UIelement ui_element in tab.items) {
                if (ui_element is OpComboBox op_combo_box) {
                    if (op_combo_box.Key == "cameraType") {
                        _camera_type_combo_box = op_combo_box;
                    } else if (op_combo_box.Key == "resolution") {
                        _resolution_combo_box = op_combo_box;
                    }
                } else if (ui_element is OpTextBox op_text_box && op_text_box.Key == "customResolution") {
                    _custom_resolution_text_box = op_text_box;
                } else if (ui_element is OpCheckBox op_check_box && op_check_box.Key == "dynamicZoom") {
                    _dynamic_zoom = op_check_box;
                } else if (ui_element is OpSlider op_slider && op_slider.Key == "camera_zoom_slider") {
                    _zoom_slider = op_slider;
                }
            }
        }
    }

    public void Reset_Resolution(bool apply_immediately = true) {
        if (saved_resolution_index == null) return;
        if (saved_resolution == null) return;

        Options.screenResolutions[(int)saved_resolution_index] = (Vector2)saved_resolution;
        if (!apply_immediately) return;
        ReInitialize_Futile();
        rainWorld.options.OnLoadFinished();
    }

    public override void Update() {
        base.Update();

        if (_camera_type_combo_box != null) {
            int camera_type = Array.IndexOf(_camera_type_keys, _camera_type_combo_box.value);
            if (_last_camera_type != camera_type) {
                _last_camera_type = camera_type;
                _camera_type_combo_box.description = _camera_type_descriptions[camera_type];
            }
        }

        if (_resolution_combo_box is OpComboBox rcb && _custom_resolution_text_box is OpTextBox rtb) {
            rtb.greyedOut = rcb.value != "Custom";
        }

        if (_zoom_slider != null && _dynamic_zoom != null) {
            _zoom_slider.greyedOut = _dynamic_zoom.value == "true";
        }
    }

    //
    // private
    //

    private void OptionInterface_SaveConfigFile(On.OptionInterface.orig__SaveConfigFile orig, OptionInterface option_interface) {
        //
        // the event OnConfigChange is triggered too often;
        // it is triggered when you click on the mod name in the
        // remix menu;
        // initializing the hooks takes like half a second;
        // I don't want to do that too often;
        //
        orig(option_interface);
        if (option_interface != main_mod_options) return;
        Debug.Log("SBCameraScroll: Save_Config_File.");
        Apply_And_Log_All_Options();
    }

    //
    //

    private void InitializeMarginAndPos() {
        _margin_x = new Vector2(50f, 550f);
        _pos = new Vector2(50f, 600f);
    }

    private void AddNewLine(float spacing_modifier = 1f) {
        _pos.x = _margin_x.x; // left margin
        _pos.y -= spacing_modifier * _spacing;
    }

    private void AddBox() {
        _margin_x += new Vector2(_spacing, -_spacing);
        _box_end_positions.Add(_pos.y); // end position > start position
        AddNewLine();
    }

    private void DrawBox(ref OpTab tab) {
        _margin_x += new Vector2(-_spacing, _spacing);
        AddNewLine();

        float box_width = _margin_x.y - _margin_x.x;
        int last_index = _box_end_positions.Count - 1;

        tab.AddItems(new OpRect(_pos, new Vector2(box_width, _box_end_positions[last_index] - _pos.y)));
        _box_end_positions.RemoveAt(last_index);
    }

    private void AddCheckBox(Configurable<bool> configurable, string text) {
        _check_box_configurables.Add(configurable);
        _check_boxes_text_labels.Add(new OpLabel(new Vector2(), new Vector2(), text, FLabelAlignment.Left));
    }

    private void DrawCheckBoxes(ref OpTab tab) // changes pos.y but not pos.x
    {
        if (_check_box_configurables.Count != _check_boxes_text_labels.Count) return;

        float width = _margin_x.y - _margin_x.x;
        float element_width = (width - (_number_of_checkboxes - 1) * 0.5f * _spacing) / _number_of_checkboxes;
        _pos.y -= _check_box_size;
        float pos_x = _pos.x;

        for (int check_box_index = 0; check_box_index < _check_box_configurables.Count; ++check_box_index) {
            Configurable<bool> configurable = _check_box_configurables[check_box_index];
            OpCheckBox check_box = new(configurable, new Vector2(pos_x, _pos.y)) {
                description = configurable.info?.description ?? ""
            };
            tab.AddItems(check_box);
            pos_x += CheckBoxWithSpacing;

            OpLabel check_box_label = _check_boxes_text_labels[check_box_index];
            check_box_label.pos = new Vector2(pos_x, _pos.y + 2f);
            check_box_label.size = new Vector2(element_width - CheckBoxWithSpacing, _font_height);
            tab.AddItems(check_box_label);

            if (check_box_index < _check_box_configurables.Count - 1) {
                if ((check_box_index + 1) % _number_of_checkboxes == 0) {
                    AddNewLine();
                    _pos.y -= _check_box_size;
                    pos_x = _pos.x;
                } else {
                    pos_x += element_width - CheckBoxWithSpacing + 0.5f * _spacing;
                }
            }
        }

        _check_box_configurables.Clear();
        _check_boxes_text_labels.Clear();
    }

    private void AddComboBox(Configurable<string> configurable, List<ListItem> list, string text, bool allow_empty = false) {
        OpLabel op_label = new(new Vector2(), new Vector2(0.0f, _font_height), text, FLabelAlignment.Left, false);
        _combo_boxes_text_labels.Add(op_label);
        _combo_box_configurables.Add(configurable);
        _combo_box_lists.Add(list);
        _combo_box_allow_empty.Add(allow_empty);
    }

    private void DrawComboBoxes(ref OpTab tab, ushort list_height = 5, float offset_x_percent = 0.1f, float width_label_percent = 0.4f, float width_combo_box_percent = 0.4f) {
        if (_combo_box_configurables.Count != _combo_boxes_text_labels.Count) return;
        if (_combo_box_configurables.Count != _combo_box_lists.Count) return;
        if (_combo_box_configurables.Count != _combo_box_allow_empty.Count) return;

        float offset_x        = (_margin_x.y - _margin_x.x) * offset_x_percent;
        float width_label     = (_margin_x.y - _margin_x.x) * width_label_percent;
        float width_combo_box = (_margin_x.y - _margin_x.x) * width_combo_box_percent;

        for (int combo_box_index = 0; combo_box_index < _combo_box_configurables.Count; ++combo_box_index) {
            AddNewLine(1.25f);
            _pos.x += offset_x;

            OpLabel op_label = _combo_boxes_text_labels[combo_box_index];
            op_label.pos = _pos;
            op_label.size += new Vector2(width_label, 2f); // size.y is already set
            _pos.x += width_label;

            Configurable<string> configurable = _combo_box_configurables[combo_box_index];
            OpComboBox combo_box = new(configurable, _pos, width_combo_box, _combo_box_lists[combo_box_index]) {
                allowEmpty = _combo_box_allow_empty[combo_box_index],
                description = configurable.info?.description ?? "",
                listHeight = list_height
            };
            tab.AddItems(op_label, combo_box);

            // don't add a new line on the last element
            if (combo_box_index < _combo_box_configurables.Count - 1) {
                AddNewLine();
                _pos.x = _margin_x.x;
            }
        }

        _combo_boxes_text_labels.Clear();
        _combo_box_configurables.Clear();
        _combo_box_lists.Clear();
        _combo_box_allow_empty.Clear();
    }

    private void AddSlider(Configurable<int> configurable, string text, string slider_text_left = "", string slider_text_right = "") {
        _slider_configurables.Add(configurable);
        _slider_main_text_labels.Add(text);
        _slider_text_labels_left.Add(new OpLabel(new Vector2(), new Vector2(), slider_text_left, alignment: FLabelAlignment.Right)); // set pos and size when drawing
        _slider_text_labels_right.Add(new OpLabel(new Vector2(), new Vector2(), slider_text_right, alignment: FLabelAlignment.Left));
    }

    private void DrawSliders(ref OpTab tab) {
        if (_slider_configurables.Count != _slider_main_text_labels.Count) return;
        if (_slider_configurables.Count != _slider_text_labels_left.Count) return;
        if (_slider_configurables.Count != _slider_text_labels_right.Count) return;

        float width = _margin_x.y - _margin_x.x;
        float slider_center = _margin_x.x + 0.5f * width;
        float slider_label_size_x = 0.2f * width;
        float slider_size_x = width - 2f * slider_label_size_x - _spacing;

        for (int slider_index = 0; slider_index < _slider_configurables.Count; ++slider_index) {
            AddNewLine(2f);

            OpLabel op_label = _slider_text_labels_left[slider_index];
            op_label.pos = new Vector2(_margin_x.x, _pos.y + 5f);
            op_label.size = new Vector2(slider_label_size_x, _font_height);
            tab.AddItems(op_label);

            Configurable<int> configurable = _slider_configurables[slider_index];
            OpSlider slider = new(configurable, new Vector2(slider_center - 0.5f * slider_size_x, _pos.y), (int)slider_size_x) {
                size = new Vector2(slider_size_x, _font_height),
                description = configurable.info?.description ?? ""
            };
            tab.AddItems(slider);

            op_label = _slider_text_labels_right[slider_index];
            op_label.pos = new Vector2(slider_center + 0.5f * slider_size_x + 0.5f * _spacing, _pos.y + 5f);
            op_label.size = new Vector2(slider_label_size_x, _font_height);
            tab.AddItems(op_label);

            AddTextLabel(_slider_main_text_labels[slider_index]);
            DrawTextLabels(ref tab);

            if (slider_index < _slider_configurables.Count - 1) {
                AddNewLine();
            }
        }

        _slider_configurables.Clear();
        _slider_main_text_labels.Clear();
        _slider_text_labels_left.Clear();
        _slider_text_labels_right.Clear();
    }

    private void AddTextBox(Configurable<string> configurable, string text) {
        OpLabel op_label = new OpLabel(new Vector2(), new Vector2(0.0f, _font_height), text, FLabelAlignment.Left, false);
        _text_box_labels.Add(op_label);
        _text_box_configurables.Add(configurable);
    }

    private void DrawTextBoxes(ref OpTab tab, ushort list_height = 5, float offset_x_percent = 0.1f, float width_label_percent = 0.4f, float width_text_box_percent = 0.4f) {
        if (_text_box_configurables.Count != _text_box_labels.Count) return;

        float offset_x       = (_margin_x.y - _margin_x.x) * offset_x_percent;
        float width_label    = (_margin_x.y - _margin_x.x) * width_label_percent;
        float width_text_box = (_margin_x.y - _margin_x.x) * width_text_box_percent;

        for (int i = 0; i < _text_box_configurables.Count; ++i) {
            AddNewLine(1.25f);
            _pos.x += offset_x;

            OpLabel op_label = _text_box_labels[i];
            op_label.pos = _pos;
            op_label.size += new Vector2(width_label, 2f); // size.y is already set
            _pos.x += width_label;

            Configurable<string> c = _text_box_configurables[i];
            OpTextBox tb = new OpTextBox(c, _pos, width_text_box) {
                // accept = OpTextBox.Accept.Int,
                allowSpace = false,
                description = c.info?.description ?? ""
            };
            tab.AddItems(op_label, tb);

            // don't add a new line on the last element
            if (i < _text_box_configurables.Count - 1) {
                AddNewLine();
                _pos.x = _margin_x.x;
            }
        }

        _text_box_labels.Clear();
        _text_box_configurables.Clear();
    }

    private void AddTextLabel(string text, FLabelAlignment alignment = FLabelAlignment.Center, bool big_text = false) {
        float text_height = (big_text ? 2f : 1f) * _font_height;
        if (_text_labels.Count == 0) {
            _pos.y -= text_height;
        }

        OpLabel text_label = new(new Vector2(), new Vector2(20f, text_height), text, alignment, big_text) // minimal size.x = 20f
        {
            autoWrap = true
        };
        _text_labels.Add(text_label);
    }

    private void DrawTextLabels(ref OpTab tab) {
        if (_text_labels.Count == 0) {
            return;
        }

        float width = (_margin_x.y - _margin_x.x) / _text_labels.Count;
        foreach (OpLabel text_label in _text_labels) {
            text_label.pos = _pos;
            text_label.size += new Vector2(width - 20f, 0.0f);
            tab.AddItems(text_label);
            _pos.x += width;
        }

        _pos.x = _margin_x.x;
        _text_labels.Clear();
    }
}
