using OptionalUI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SBCameraScroll
{
    public class MainModOptions : OptionInterface
    {
        private Vector2 marginX = new Vector2();
        private Vector2 pos = new Vector2();
        private readonly float spacing = 20f;

        private readonly List<float> boxEndPositions = new List<float>();

        private readonly int numberOfCheckboxes = 3;
        private readonly float checkBoxSize = 24f;
        private readonly List<OpCheckBox> checkBoxes = new List<OpCheckBox>();
        private readonly List<OpLabel> checkBoxesTextLabels = new List<OpLabel>();

        private OpComboBox? cameraType = null;
        private int lastCameraType = 0;
        private readonly string[] cameraTypeKeys = new string[3] { "position", "velocity", "vanilla" };
        private readonly string[] cameraTypeDescriptions = new string[3]
        {
            "This type tries to stay close to the player. A larger distance means a faster camera.\nThe smoothing factor determines how much of the distance is covered per frame.",
            "Mario-style camera. This type tries to match the player's speed.\nWhen OuterCameraBox is reached, the camera moves as fast as the player.",
            "Vanilla-style camera. You can center the camera by pressing the map button. Pressing the map button again will revert to vanilla camera positions.\nWhen the player is close to the edge of the screen the camera jumps a constant distance."
        };
        private readonly List<OpComboBox> comboBoxes = new List<OpComboBox>();
        private readonly List<OpLabel> comboBoxesTextLabels = new List<OpLabel>();

        private readonly List<string> sliderKeys = new List<string>();
        private readonly List<IntVector2> sliderRanges = new List<IntVector2>();
        private readonly List<int> sliderDefaultValues = new List<int>();
        private readonly List<string> sliderDescriptions = new List<string>();
        private readonly List<string> sliderMainTextLabels = new List<string>();
        private readonly List<OpLabel> sliderTextLabelsLeft = new List<OpLabel>();
        private readonly List<OpLabel> sliderTextLabelsRight = new List<OpLabel>();

        private readonly float fontHeight = 20f;
        private readonly List<OpLabel> textLabels = new List<OpLabel>();

        private float CheckBoxWithSpacing => checkBoxSize + 0.25f * spacing;

        public MainModOptions() : base(MainMod.instance)
        {
        }

        public override void Initialize()
        {
            base.Initialize();
            Tabs = new OpTab[4];

            // general tab
            int tabIndex = 0;
            Tabs[tabIndex] = new OpTab("General");
            InitializeMarginAndPos();

            // Title
            AddNewLine();
            AddTextLabel("SBCameraScroll Mod", bigText: true);
            DrawTextLabels(ref Tabs[tabIndex]);

            // Subtitle
            AddNewLine(0.5f);
            AddTextLabel("Version " + MainMod.instance?.Version, FLabelAlignment.Left);
            AddTextLabel("by " + MainMod.instance?.author, FLabelAlignment.Right);
            DrawTextLabels(ref Tabs[tabIndex]);

            // Content //
            AddNewLine();
            AddBox();

            List<ListItem> _cameraTypes = new List<ListItem> // { "Position", "Velocity", "Vanilla" };
            {
                new ListItem(cameraTypeKeys[0], "Position (Default)") { desc = cameraTypeDescriptions[0] },
                new ListItem(cameraTypeKeys[1], "Velocity") { desc = cameraTypeDescriptions[1] },
                new ListItem(cameraTypeKeys[2], "Vanilla") { desc = cameraTypeDescriptions[2] }
            };
            AddComboBox(_cameraTypes, "cameraType", "Camera Type", "", "Position");
            DrawComboBoxes(ref Tabs[tabIndex]);

            AddNewLine(1.25f);

            AddCheckBox("fogFullScreenEffect", "Fog Effect", "When disabled, the full screen fog effect is removed. It depends on the camera position and can noticeably move with the screen.", defaultBool: true);
            AddCheckBox("otherFullScreenEffects", "Full Screen Effects", "When disabled, full screen effects (except fog) like bloom and melt are removed.", defaultBool: true);
            AddCheckBox("mergeWhileLoading", "Merge While Loading", "When enabled, the camera textures for each room are merged when the region gets loaded.\nWhen disabled, camera textures are merge for each room on demand. Merging happens only once and the files are stored in your Mods\\SBCameraScroll folder.\nThis process can take a while. Merging all rooms in Deserted Wastelands took me around three minutes.", defaultBool: true);
            AddCheckBox("scrollOneScreenRooms", "One Screen Rooms", "When disabled, the camera does not scroll in rooms with only one screen. Automatically enabled when using SplitScreenMod.", defaultBool: false);
            DrawCheckBoxes(ref Tabs[tabIndex]);

            AddNewLine();

            AddSlider("smoothingFactorX", "Smoothing Factor for X (8)", "The smoothing factor determines how much of the distance is covered per frame. This is used when switching cameras as well to ensure a smooth transition.", new IntVector2(0, 35), defaultValue: 8, "0%", "70%");
            DrawSliders(ref Tabs[tabIndex]);

            AddNewLine(2f);

            AddSlider("smoothingFactorY", "Smoothing Factor for Y (8)", "The smoothing factor determines how much of the distance is covered per frame. This is used when switching cameras as well to ensure a smooth transition.", new IntVector2(0, 35), defaultValue: 8, "0%", "70%");
            DrawSliders(ref Tabs[tabIndex]);

            DrawBox(ref Tabs[tabIndex]);

            //-------------------//
            // position type tab //
            //-------------------//

            tabIndex++;
            Tabs[tabIndex] = new OpTab("Position");
            InitializeMarginAndPos();

            // Title
            AddNewLine();
            AddTextLabel("SBCameraScroll Mod", bigText: true);
            DrawTextLabels(ref Tabs[tabIndex]);

            // Subtitle
            AddNewLine(0.5f);
            AddTextLabel("Version " + MainMod.instance?.Version, FLabelAlignment.Left);
            AddTextLabel("by " + MainMod.instance?.author, FLabelAlignment.Right);
            DrawTextLabels(ref Tabs[tabIndex]);

            // Content //
            AddNewLine();
            AddBox();

            AddTextLabel("Position Type Camera:", FLabelAlignment.Left);
            DrawTextLabels(ref Tabs[tabIndex]);

            AddNewLine();

            AddSlider("innerCameraBoxX_Position", "Minimum Distance in X (2)", "The camera does not move when the player is closer than this.", new IntVector2(0, 35), defaultValue: 2, "0 tiles", "35 tiles");
            DrawSliders(ref Tabs[tabIndex]);

            AddNewLine(2f);

            AddSlider("innerCameraBoxY_Position", "Minimum Distance in Y (2)", "The camera does not move when the player is closer than this.", new IntVector2(0, 35), defaultValue: 2, "0 tiles", "35 tiles");
            DrawSliders(ref Tabs[tabIndex]);

            DrawBox(ref Tabs[tabIndex]);

            //------------------//
            // vanilla type tab //
            //------------------//

            tabIndex++;
            Tabs[tabIndex] = new OpTab("Vanilla");
            InitializeMarginAndPos();

            // Title
            AddNewLine();
            AddTextLabel("SBCameraScroll Mod", bigText: true);
            DrawTextLabels(ref Tabs[tabIndex]);

            // Subtitle
            AddNewLine(0.5f);
            AddTextLabel("Version " + MainMod.instance?.Version, FLabelAlignment.Left);
            AddTextLabel("by " + MainMod.instance?.author, FLabelAlignment.Right);
            DrawTextLabels(ref Tabs[tabIndex]);

            // Content //
            AddNewLine();
            AddBox();

            AddTextLabel("Vanilla Type Camera:", FLabelAlignment.Left);
            DrawTextLabels(ref Tabs[tabIndex]);

            AddNewLine();

            AddSlider("innerCameraBoxX_Vanilla", "Minimum Distance from the Edge in X (7)", "The camera starts to tilt if the player is closer to the edge of the screen than this value.", new IntVector2(0, 35), defaultValue: 7, "0 tiles", "35 tiles");
            AddSlider("outerCameraBoxX_Vanilla", "Maximum Distance from the Edge in X (5)", "The camera changes position if the player is closer to the edge of the screen than this value.", new IntVector2(0, 35), defaultValue: 5, "0 tiles", "35 tiles");
            DrawSliders(ref Tabs[tabIndex]);

            AddNewLine(2f);

            AddSlider("innerCameraBoxY_Vanilla", "Minimum Distance from the Edge in Y (3)", "The camera starts to tilt if the player is closer to the edge of the screen than this value.", new IntVector2(0, 35), defaultValue: 3, "0 tiles", "35 tiles");
            AddSlider("outerCameraBoxY_Vanilla", "Maximum Distance to the Edge in Y (1)", "The camera changes position if the player is closer to the edge of the screen than this value.", new IntVector2(0, 35), defaultValue: 1, "0 tiles", "35 tiles");
            DrawSliders(ref Tabs[tabIndex]);

            DrawBox(ref Tabs[tabIndex]);

            //-------------------//
            // velocity type tab //
            //-------------------//

            tabIndex++;
            Tabs[tabIndex] = new OpTab("Velocity");
            InitializeMarginAndPos();

            // Title
            AddNewLine();
            AddTextLabel("SBCameraScroll Mod", bigText: true);
            DrawTextLabels(ref Tabs[tabIndex]);

            // Subtitle
            AddNewLine(0.5f);
            AddTextLabel("Version " + MainMod.instance?.Version, FLabelAlignment.Left);
            AddTextLabel("by " + MainMod.instance?.author, FLabelAlignment.Right);
            DrawTextLabels(ref Tabs[tabIndex]);

            // Content //
            AddNewLine();
            AddBox();

            AddTextLabel("Velocity Type Camera:", FLabelAlignment.Left);
            DrawTextLabels(ref Tabs[tabIndex]);

            AddNewLine();

            AddSlider("innerCameraBoxX_Velocity", "Minimum Distance in X (3)", "The camera does not move when the player is closer than this.", new IntVector2(0, 35), defaultValue: 3, "0 tiles", "35 tiles");
            AddSlider("outerCameraBoxX_Velocity", "Maximum Distance in X (6)", "When this distance is reached the camera always moves as fast as the player.", new IntVector2(0, 35), defaultValue: 6, "0 tiles", "35 tiles");
            DrawSliders(ref Tabs[tabIndex]);

            AddNewLine(2f);

            AddSlider("innerCameraBoxY_Velocity", "Minimum Distance in Y (4)", "The camera does not move when the player is closer than this.", new IntVector2(0, 35), defaultValue: 4, "0 tiles", "35 tiles");
            AddSlider("outerCameraBoxY_Velocity", "Maximum Distance in Y (7)", "When this distance is reached the camera always moves as fast as the player.", new IntVector2(0, 35), defaultValue: 7, "0 tiles", "35 tiles");
            DrawSliders(ref Tabs[tabIndex]);

            DrawBox(ref Tabs[tabIndex]);

            // save UI elements in variables for Update() function
            foreach (UIelement uiElement in Tabs[0].items)
            {
                if (uiElement is OpComboBox opComboBox && opComboBox.key == "cameraType")
                {
                    cameraType = opComboBox;
                }
            }
        }

        public override void Update(float dt)
        {
            base.Update(dt);
            if (cameraType != null)
            {
                int _cameraType = Array.IndexOf(cameraTypeKeys, cameraType.value);
                if (lastCameraType != _cameraType)
                {
                    lastCameraType = _cameraType;
                    cameraType.description = cameraTypeDescriptions[_cameraType];
                }
            }
        }
        
        public override void ConfigOnChange()
        {
            base.ConfigOnChange();

            RoomCameraMod.cameraType = Array.IndexOf(cameraTypeKeys, config["cameraType"]);

            MainMod.isFogFullScreenEffectOptionEnabled = bool.Parse(config["fogFullScreenEffect"]);
            MainMod.isOtherFullScreenEffectsOptionEnabled = bool.Parse(config["otherFullScreenEffects"]);
            MainMod.isMergeWhileLoadingOptionEnabled = bool.Parse(config["mergeWhileLoading"]);
            MainMod.isScrollOneScreenRoomsOptionEnabled = bool.Parse(config["scrollOneScreenRooms"]) || MainMod.isSplitScreenModEnabled; // automatically enable when using SplitScreenMod

            Debug.Log("SBCameraScroll: cameraType " + config["cameraType"]);
            Debug.Log("SBCameraScroll: isFogFullScreenEffectOptionEnabled " + MainMod.isFogFullScreenEffectOptionEnabled);
            Debug.Log("SBCameraScroll: isOtherFullScreenEffectsOptionEnabled " + MainMod.isOtherFullScreenEffectsOptionEnabled);
            Debug.Log("SBCameraScroll: isMergeWhileLoadingOptionEnabled " + MainMod.isMergeWhileLoadingOptionEnabled);
            Debug.Log("SBCameraScroll: isScrollOneScreenOptionEnabled " + MainMod.isScrollOneScreenRoomsOptionEnabled);


            RoomCameraMod.smoothingFactorX = float.Parse(config["smoothingFactorX"]) / 50f;
            RoomCameraMod.smoothingFactorY = float.Parse(config["smoothingFactorY"]) / 50f;

            Debug.Log("SBCameraScroll: smoothingFactorX " + RoomCameraMod.smoothingFactorX);
            Debug.Log("SBCameraScroll: smoothingFactorY " + RoomCameraMod.smoothingFactorY);

            if (RoomCameraMod.cameraType == 0) // position type
            {
                RoomCameraMod.innerCameraBoxX = 20f * float.Parse(config["innerCameraBoxX_Position"]);
                RoomCameraMod.innerCameraBoxY = 20f * float.Parse(config["innerCameraBoxY_Position"]);
            }
            else if (RoomCameraMod.cameraType == 1) // velocity type
            {
                RoomCameraMod.innerCameraBoxX = 20f * float.Parse(config["innerCameraBoxX_Velocity"]);
                RoomCameraMod.outerCameraBoxX = 20f * float.Parse(config["outerCameraBoxX_Velocity"]);
                RoomCameraMod.innerCameraBoxY = 20f * float.Parse(config["innerCameraBoxY_Velocity"]);
                RoomCameraMod.outerCameraBoxY = 20f * float.Parse(config["outerCameraBoxY_Velocity"]);

                Debug.Log("SBCameraScroll: outerCameraBoxX " + RoomCameraMod.outerCameraBoxX);
                Debug.Log("SBCameraScroll: outerCameraBoxY " + RoomCameraMod.outerCameraBoxY);
            }
            else
            {
                RoomCameraMod.innerCameraBoxX = 20f * float.Parse(config["innerCameraBoxX_Vanilla"]);
                RoomCameraMod.outerCameraBoxX = 20f * float.Parse(config["outerCameraBoxX_Vanilla"]);
                RoomCameraMod.innerCameraBoxY = 20f * float.Parse(config["innerCameraBoxY_Vanilla"]);
                RoomCameraMod.outerCameraBoxY = 20f * float.Parse(config["outerCameraBoxY_Vanilla"]);

                Debug.Log("SBCameraScroll: outerCameraBoxX " + RoomCameraMod.outerCameraBoxX);
                Debug.Log("SBCameraScroll: outerCameraBoxY " + RoomCameraMod.outerCameraBoxY);
            }

            Debug.Log("SBCameraScroll: innerCameraBoxX " + RoomCameraMod.innerCameraBoxX);
            Debug.Log("SBCameraScroll: innerCameraBoxY " + RoomCameraMod.innerCameraBoxY);
        }

        // ----------------- //
        // private functions //
        // ----------------- //

        private void InitializeMarginAndPos()
        {
            marginX = new Vector2(50f, 550f);
            pos = new Vector2(50f, 600f);
        }

        private void AddNewLine(float spacingModifier = 1f)
        {
            pos.x = marginX.x; // left margin
            pos.y -= spacingModifier * spacing;
        }

        private void AddBox()
        {
            marginX += new Vector2(spacing, -spacing);
            boxEndPositions.Add(pos.y); // end position > start position
            AddNewLine();
        }

        private void DrawBox(ref OpTab tab)
        {
            marginX += new Vector2(-spacing, spacing);
            AddNewLine();

            float boxWidth = marginX.y - marginX.x;
            int lastIndex = boxEndPositions.Count - 1;

            tab.AddItems(new OpRect(pos, new Vector2(boxWidth, boxEndPositions[lastIndex] - pos.y)));
            boxEndPositions.RemoveAt(lastIndex);
        }

        //private OpScrollBox AddScrollBox(ref OpTab tab, float spacingModifier = 1f)
        //{
        //    float boxWidth = marginX.y - marginX.x;
        //    float marginY = spacingModifier * spacing;
        //    boxEndPositions.Add(pos.y);

        //    OpScrollBox scrollBox = new OpScrollBox(new Vector2(marginX.x, marginY), new Vector2(boxWidth, Math.Max(pos.y - marginY, spacing)), boxWidth);
        //    tab.AddItems(scrollBox);

        //    marginX -= new Vector2(30f, 70f);
        //    AddNewLine(0.5f);
        //    return scrollBox;
        //}

        //private void DrawScrollBox(ref OpScrollBox scrollBox)
        //{
        //    AddNewLine(1.5f);
        //    int lastIndex = boxEndPositions.Count - 1;

        //    scrollBox.SetContentSize(boxEndPositions[lastIndex] - pos.y);
        //    scrollBox.ScrollToTop();
        //    boxEndPositions.RemoveAt(lastIndex);
        //}

        private void AddCheckBox(string key, string text, string description, bool? defaultBool = null)
        {
            OpCheckBox opCheckBox = new OpCheckBox(new Vector2(), key, defaultBool: defaultBool ?? false)
            {
                description = description
            };

            checkBoxes.Add(opCheckBox);
            checkBoxesTextLabels.Add(new OpLabel(new Vector2(), new Vector2(), text, FLabelAlignment.Left));
        }

        private void DrawCheckBoxes(ref OpTab tab) // changes pos.y but not pos.x
        {
            if (checkBoxes.Count != checkBoxesTextLabels.Count)
            {
                return;
            }

            float width = marginX.y - marginX.x;
            float elementWidth = (width - (numberOfCheckboxes - 1) * 0.5f * spacing) / numberOfCheckboxes;
            pos.y -= checkBoxSize;
            float _posX = pos.x;

            for (int index = 0; index < checkBoxes.Count; ++index)
            {
                OpCheckBox checkBox = checkBoxes[index];
                checkBox.pos = new Vector2(_posX, pos.y);
                tab.AddItems(checkBox);
                _posX += CheckBoxWithSpacing;

                OpLabel checkBoxLabel = checkBoxesTextLabels[index];
                checkBoxLabel.pos = new Vector2(_posX, pos.y + 2f);
                checkBoxLabel.size = new Vector2(elementWidth - CheckBoxWithSpacing, fontHeight);
                tab.AddItems(checkBoxLabel);

                if (index < checkBoxes.Count - 1)
                {
                    if ((index + 1) % numberOfCheckboxes == 0)
                    {
                        AddNewLine();
                        pos.y -= checkBoxSize;
                        _posX = pos.x;
                    }
                    else
                    {
                        _posX += elementWidth - CheckBoxWithSpacing + 0.5f * spacing;
                    }
                }
            }

            checkBoxes.Clear();
            checkBoxesTextLabels.Clear();
        }

        private void AddComboBox(List<ListItem> list, string key, string text, string description, string defaultName = "", bool allowEmpty = false)
        {
            OpLabel opLabel = new OpLabel(new Vector2(), new Vector2(0.0f, fontHeight), text, FLabelAlignment.Center, false);
            comboBoxesTextLabels.Add(opLabel);

            OpComboBox opComboBox = new OpComboBox(new Vector2(), 200f, key, list, defaultName)
            {
                allowEmpty = allowEmpty,
                description = description
            };
            comboBoxes.Add(opComboBox);
        }

        private void DrawComboBoxes(ref OpTab tab)
        {
            if (comboBoxes.Count == 0 || comboBoxes.Count != comboBoxesTextLabels.Count)
            {
                return;
            }

            float offsetX = (marginX.y - marginX.x) * 0.1f;
            float width = (marginX.y - marginX.x) * 0.4f;

            for (int comboBoxIndex = 0; comboBoxIndex < comboBoxes.Count; ++comboBoxIndex)
            {
                AddNewLine(1.25f);
                pos.x += offsetX;

                OpLabel opLabel = comboBoxesTextLabels[comboBoxIndex];
                opLabel.pos = pos;
                opLabel.size += new Vector2(width, 2f); // size.y is already set
                pos.x += width;

                OpComboBox comboBox = comboBoxes[comboBoxIndex];
                OpComboBox newComboBox = new OpComboBox(pos, width, comboBox.key, comboBox.GetItemList().ToList(), defaultName: comboBox.defaultValue)
                {
                    allowEmpty = comboBox.allowEmpty,
                    description = comboBox.description,
                };
                tab.AddItems(opLabel, newComboBox);

                if (comboBoxIndex < checkBoxes.Count - 1)
                {
                    AddNewLine();
                    pos.x = marginX.x;
                }
            }

            comboBoxesTextLabels.Clear();
            comboBoxes.Clear();
        }

        private void AddSlider(string key, string text, string description, IntVector2 range, int defaultValue, string? sliderTextLeft = null, string? sliderTextRight = null)
        {
            sliderTextLeft ??= range.x.ToString();
            sliderTextRight ??= range.y.ToString();

            sliderMainTextLabels.Add(text);
            sliderTextLabelsLeft.Add(new OpLabel(new Vector2(), new Vector2(), sliderTextLeft, alignment: FLabelAlignment.Right)); // set pos and size when drawing
            sliderTextLabelsRight.Add(new OpLabel(new Vector2(), new Vector2(), sliderTextRight, alignment: FLabelAlignment.Left));

            sliderKeys.Add(key);
            sliderRanges.Add(range);
            sliderDefaultValues.Add(defaultValue);
            sliderDescriptions.Add(description);
        }

        //private void DrawSliders(ref OpScrollBox scrollBox)
        //{
        //    if (sliderKeys.Count != sliderRanges.Count || sliderKeys.Count != sliderDefaultValues.Count || sliderKeys.Count != sliderDescriptions.Count || sliderKeys.Count != sliderMainTextLabels.Count || sliderKeys.Count != sliderTextLabelsLeft.Count || sliderKeys.Count != sliderTextLabelsRight.Count)
        //    {
        //        return;
        //    }

        //    float width = marginX.y - marginX.x;
        //    float sliderCenter = marginX.x + 0.5f * width;
        //    float sliderLabelSizeX = 0.2f * width;
        //    float sliderSizeX = width - 2f * sliderLabelSizeX - spacing;

        //    for (int sliderIndex = 0; sliderIndex < sliderKeys.Count; ++sliderIndex)
        //    {
        //        AddNewLine(2f);

        //        OpLabel opLabel = sliderTextLabelsLeft[sliderIndex];
        //        opLabel.pos = new Vector2(marginX.x, pos.y + 5f);
        //        opLabel.size = new Vector2(sliderLabelSizeX, fontHeight);
        //        scrollBox.AddItems(opLabel);

        //        OpSlider slider = new OpSlider(new Vector2(sliderCenter - 0.5f * sliderSizeX, pos.y), sliderKeys[sliderIndex], sliderRanges[sliderIndex], length: (int)sliderSizeX, defaultValue: sliderDefaultValues[sliderIndex])
        //        {
        //            size = new Vector2(sliderSizeX, fontHeight),
        //            description = sliderDescriptions[sliderIndex]
        //        };
        //        scrollBox.AddItems(slider);

        //        opLabel = sliderTextLabelsRight[sliderIndex];
        //        opLabel.pos = new Vector2(sliderCenter + 0.5f * sliderSizeX + 0.5f * spacing, pos.y + 5f);
        //        opLabel.size = new Vector2(sliderLabelSizeX, fontHeight);
        //        scrollBox.AddItems(opLabel);

        //        AddTextLabel(sliderMainTextLabels[sliderIndex]);
        //        DrawTextLabels(ref scrollBox);

        //        if (sliderIndex < sliderKeys.Count - 1)
        //        {
        //            AddNewLine();
        //        }
        //    }

        //    sliderKeys.Clear();
        //    sliderRanges.Clear();
        //    sliderDefaultValues.Clear();
        //    sliderDescriptions.Clear();

        //    sliderMainTextLabels.Clear();
        //    sliderTextLabelsLeft.Clear();
        //    sliderTextLabelsRight.Clear();
        //}

        private void DrawSliders(ref OpTab tab)
        {
            if (sliderKeys.Count != sliderRanges.Count || sliderKeys.Count != sliderDefaultValues.Count || sliderKeys.Count != sliderDescriptions.Count || sliderKeys.Count != sliderMainTextLabels.Count || sliderKeys.Count != sliderTextLabelsLeft.Count || sliderKeys.Count != sliderTextLabelsRight.Count)
            {
                return;
            }

            float width = marginX.y - marginX.x;
            float sliderCenter = marginX.x + 0.5f * width;
            float sliderLabelSizeX = 0.2f * width;
            float sliderSizeX = width - 2f * sliderLabelSizeX - spacing;

            for (int sliderIndex = 0; sliderIndex < sliderKeys.Count; ++sliderIndex)
            {
                AddNewLine(2f);

                OpLabel opLabel = sliderTextLabelsLeft[sliderIndex];
                opLabel.pos = new Vector2(marginX.x, pos.y + 5f);
                opLabel.size = new Vector2(sliderLabelSizeX, fontHeight);
                tab.AddItems(opLabel);

                OpSlider slider = new OpSlider(new Vector2(sliderCenter - 0.5f * sliderSizeX, pos.y), sliderKeys[sliderIndex], sliderRanges[sliderIndex], length: (int)sliderSizeX, defaultValue: sliderDefaultValues[sliderIndex])
                {
                    size = new Vector2(sliderSizeX, fontHeight),
                    description = sliderDescriptions[sliderIndex]
                };
                tab.AddItems(slider);

                opLabel = sliderTextLabelsRight[sliderIndex];
                opLabel.pos = new Vector2(sliderCenter + 0.5f * sliderSizeX + 0.5f * spacing, pos.y + 5f);
                opLabel.size = new Vector2(sliderLabelSizeX, fontHeight);
                tab.AddItems(opLabel);

                AddTextLabel(sliderMainTextLabels[sliderIndex]);
                DrawTextLabels(ref tab);

                if (sliderIndex < sliderKeys.Count - 1)
                {
                    AddNewLine();
                }
            }

            sliderKeys.Clear();
            sliderRanges.Clear();
            sliderDefaultValues.Clear();
            sliderDescriptions.Clear();

            sliderMainTextLabels.Clear();
            sliderTextLabelsLeft.Clear();
            sliderTextLabelsRight.Clear();
        }

        private void AddTextLabel(string text, FLabelAlignment alignment = FLabelAlignment.Center, bool bigText = false)
        {
            float textHeight = (bigText ? 2f : 1f) * fontHeight;
            if (textLabels.Count == 0)
            {
                pos.y -= textHeight;
            }

            OpLabel textLabel = new OpLabel(new Vector2(), new Vector2(20f, textHeight), text, alignment, bigText) // minimal size.x = 20f
            {
                autoWrap = true
            };
            textLabels.Add(textLabel);
        }

        //private void DrawTextLabels(ref OpScrollBox scrollBox)
        //{
        //    if (textLabels.Count == 0)
        //    {
        //        return;
        //    }

        //    float width = (marginX.y - marginX.x) / textLabels.Count;
        //    foreach (OpLabel textLabel in textLabels)
        //    {
        //        textLabel.pos = pos;
        //        textLabel.size += new Vector2(width - 20f, 0.0f);
        //        scrollBox.AddItems(new UIelement[] { textLabel });
        //        pos.x += width;
        //    }

        //    pos.x = marginX.x;
        //    textLabels.Clear();
        //}

        private void DrawTextLabels(ref OpTab tab)
        {
            if (textLabels.Count == 0)
            {
                return;
            }

            float width = (marginX.y - marginX.x) / textLabels.Count;
            foreach (OpLabel textLabel in textLabels)
            {
                textLabel.pos = pos;
                textLabel.size += new Vector2(width - 20f, 0.0f);
                tab.AddItems(textLabel);
                pos.x += width;
            }

            pos.x = marginX.x;
            textLabels.Clear();
        }
    }
}