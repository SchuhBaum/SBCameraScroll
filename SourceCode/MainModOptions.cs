using System;
using System.Collections.Generic;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace SBCameraScroll
{
    public class MainModOptions : OptionInterface
    {
        public static MainModOptions instance = new();

        //
        // options
        //

        public static Configurable<bool> fogFullScreenEffect = instance.config.Bind("fogFullScreenEffect", defaultValue: true, new ConfigurableInfo("When disabled, the full screen fog effect is removed. It depends on the camera position and can noticeably move with the screen.", null, "", "Fog Effect"));
        public static Configurable<bool> mergeWhileLoading = instance.config.Bind("mergeWhileLoading", defaultValue: true, new ConfigurableInfo("When enabled, the camera textures for each room are merged when the region gets loaded.\nWhen disabled, camera textures are merged for each room on demand. Merging happens only once and might take a while.", null, "", "Merge While Loading")); //Merging happens only once and the files are stored inside the folder \"Mods/SBCameraScroll/\".\nThis process can take a while. Merging all rooms in Deserted Wastelands took me around three minutes.
        public static Configurable<bool> otherFullScreenEffects = instance.config.Bind("otherFullScreenEffects", defaultValue: true, new ConfigurableInfo("When disabled, full screen effects (except fog) like bloom and melt are removed.", null, "", "Full Screen Effects"));
        public static Configurable<bool> scrollOneScreenRooms = instance.config.Bind("scrollOneScreenRooms", defaultValue: false, new ConfigurableInfo("When disabled, the camera does not scroll in rooms with only one screen.", null, "", "One Screen Rooms")); // Automatically enabled when using SplitScreenMod.

        public static Configurable<int> innerCameraBoxX_Position = instance.config.Bind("innerCameraBoxX_Position", defaultValue: 2, new ConfigurableInfo("The camera does not move when the player is closer than this.", new ConfigAcceptableRange<int>(0, 35), "", "Minimum Distance in X (2)"));
        public static Configurable<int> innerCameraBoxY_Position = instance.config.Bind("innerCameraBoxY_Position", defaultValue: 2, new ConfigurableInfo("The camera does not move when the player is closer than this.", new ConfigAcceptableRange<int>(0, 35), "", "Minimum Distance in Y (2)"));
        public static Configurable<int> outerCameraBoxX_Vanilla = instance.config.Bind("outerCameraBoxX_Vanilla", defaultValue: 9, new ConfigurableInfo("The camera changes position if the player is closer to the edge of the screen than this value.", new ConfigAcceptableRange<int>(0, 35), "", "Distance from the Edge in X (9)"));
        public static Configurable<int> outerCameraBoxY_Vanilla = instance.config.Bind("outerCameraBoxY_Vanilla", defaultValue: 1, new ConfigurableInfo("The camera changes position if the player is closer to the edge of the screen than this value.", new ConfigAcceptableRange<int>(0, 35), "", "Distance to the Edge in Y (1)"));

        public static Configurable<int> smoothingFactorX = instance.config.Bind("smoothingFactorX", defaultValue: 8, new ConfigurableInfo("The smoothing factor determines how much of the distance is covered per frame. This is used when switching cameras as well to ensure a smooth transition.", new ConfigAcceptableRange<int>(0, 35), "", "Smoothing Factor for X (8)"));
        public static Configurable<int> smoothingFactorY = instance.config.Bind("smoothingFactorY", defaultValue: 8, new ConfigurableInfo("The smoothing factor determines how much of the distance is covered per frame. This is used when switching cameras as well to ensure a smooth transition.", new ConfigAcceptableRange<int>(0, 35), "", "Smoothing Factor for Y (8)"));

        //
        // parameters
        //

        private readonly float spacing = 20f;
        private readonly float fontHeight = 20f;
        private readonly int numberOfCheckboxes = 3;
        private readonly float checkBoxSize = 24f;

        private readonly string[] cameraTypeKeys = new string[3] { "position", "vanilla", "velocity" };
        private readonly string[] cameraTypeDescriptions = new string[3]
        {
            "This type tries to stay close to the player. A larger distance means a faster camera.\nThe smoothing factor determines how much of the distance is covered per frame.",
            "Vanilla-style camera. You can center the camera by pressing the map button. Pressing the map button again will revert to vanilla camera positions.\nWhen the player is close to the edge of the screen the camera jumps a constant distance.",
            "Mario-style camera. This type tries to match the player's speed.\nWhen OuterCameraBox is reached, the camera moves as fast as the player."
        };

        //
        // variables
        //

        private Vector2 marginX = new();
        private Vector2 pos = new();

        private readonly List<float> boxEndPositions = new();

        private readonly List<Configurable<bool>> checkBoxConfigurables = new();
        private readonly List<OpLabel> checkBoxesTextLabels = new();

        public static Configurable<string> cameraType = instance.config.Bind("cameraType", "position", new ConfigurableInfo("This type tries to stay close to the player. A larger distance means a faster camera.\nThe smoothing factor determines how much of the distance is covered per frame.", null, "", "Camera Type"));
        private OpComboBox? cameraTypeComboBox = null;
        private int lastCameraType = 0;

        // private OpSimpleButton? clearCacheButton = null;

        // private readonly List<OpComboBox> comboBoxes = new();
        private readonly List<Configurable<string>> comboBoxConfigurables = new();
        private readonly List<List<ListItem>> comboBoxLists = new();
        private readonly List<bool> comboBoxAllowEmpty = new();
        private readonly List<OpLabel> comboBoxesTextLabels = new();

        private readonly List<Configurable<int>> sliderConfigurables = new();
        private readonly List<string> sliderMainTextLabels = new();
        private readonly List<OpLabel> sliderTextLabelsLeft = new();
        private readonly List<OpLabel> sliderTextLabelsRight = new();

        private readonly List<OpLabel> textLabels = new();

        private float CheckBoxWithSpacing => checkBoxSize + 0.25f * spacing;

        //
        // main
        //

        public MainModOptions()
        {
            // ambiguity error // why? TODO
            // OnConfigChanged += MainModOptions_OnConfigChanged;
        }

        //
        // public
        //

        public override void Initialize()
        {
            base.Initialize();
            Tabs = new OpTab[4];

            //-------------//
            // general tab //
            //-------------//

            int tabIndex = 0;
            Tabs[tabIndex] = new OpTab(this, "General");
            InitializeMarginAndPos();

            // Title
            AddNewLine();
            AddTextLabel("SBCameraScroll Mod", bigText: true);
            DrawTextLabels(ref Tabs[tabIndex]);

            // Subtitle
            AddNewLine(0.5f);
            AddTextLabel("Version " + MainMod.version, FLabelAlignment.Left);
            AddTextLabel("by " + MainMod.author, FLabelAlignment.Right);
            DrawTextLabels(ref Tabs[tabIndex]);

            // Content //
            AddNewLine();
            AddBox();

            List<ListItem> _cameraTypes = new()
            {
                new ListItem(cameraTypeKeys[0], "Position (Default)") { desc = cameraTypeDescriptions[0] },
                new ListItem(cameraTypeKeys[1], "Vanilla") { desc = cameraTypeDescriptions[1] }
                // new ListItem(cameraTypeKeys[2], "Velocity") { desc = cameraTypeDescriptions[2] }
            };
            AddComboBox(cameraType, _cameraTypes, "Camera Type");
            DrawComboBoxes(ref Tabs[tabIndex]);

            AddNewLine(1.25f);

            AddCheckBox(fogFullScreenEffect, "Fog Effect");
            AddCheckBox(otherFullScreenEffects, "Full Screen Effects");
            AddCheckBox(mergeWhileLoading, "Merge While Loading");
            AddCheckBox(scrollOneScreenRooms, "One Screen Rooms");
            DrawCheckBoxes(ref Tabs[tabIndex]);

            AddNewLine();

            AddSlider(smoothingFactorX, (string)smoothingFactorX.info.Tags[0], "0%", "70%");
            DrawSliders(ref Tabs[tabIndex]);

            AddNewLine(2f);

            AddSlider(smoothingFactorY, (string)smoothingFactorY.info.Tags[0], "0%", "70%");
            DrawSliders(ref Tabs[tabIndex]);

            AddNewLine(3f);

            //TODO
            // clearCacheButton = new(new Vector2(pos.x + (marginX.y - marginX.x) / 2f - 55f, pos.y), new Vector2(110f, 30f), "CLEAR CACHE") // same size as apply / back button in ConfigMachine
            // {
            //     colorEdge = new Color(1f, 1f, 1f, 1f),
            //     colorFill = new Color(1f, 0.0f, 0.0f, 0.5f),
            //     description = "WARNING: Deletes all merged textures inside the folder \"Mods/SBCameraScroll/\".\nAdding custom regions might change existing room textures. These textures need to be merged again."
            // };
            // clearCacheButton.OnClick += ClearCacheButton_OnClick;
            // Tabs[tabIndex].AddItems(clearCacheButton);

            // if (clearCacheButton != null && Directory.GetFiles(MainMod.modDirectoryPath + Path.DirectorySeparatorChar + "world", "*.*", SearchOption.AllDirectories).Length == 0)
            // {
            //     clearCacheButton.colorEdge.a = 0.1f;
            //     clearCacheButton.greyedOut = true;
            // }

            DrawBox(ref Tabs[tabIndex]);

            //-------------------//
            // position type tab //
            //-------------------//

            tabIndex++;
            Tabs[tabIndex] = new OpTab(this, "Position");
            InitializeMarginAndPos();

            // Title
            AddNewLine();
            AddTextLabel("SBCameraScroll Mod", bigText: true);
            DrawTextLabels(ref Tabs[tabIndex]);

            // Subtitle
            AddNewLine(0.5f);
            AddTextLabel("Version " + MainMod.version, FLabelAlignment.Left);
            AddTextLabel("by " + MainMod.author, FLabelAlignment.Right);
            DrawTextLabels(ref Tabs[tabIndex]);

            // Content //
            AddNewLine();
            AddBox();

            AddTextLabel("Position Type Camera:", FLabelAlignment.Left);
            DrawTextLabels(ref Tabs[tabIndex]);

            AddNewLine();

            AddSlider(innerCameraBoxX_Position, (string)innerCameraBoxX_Position.info.Tags[0], "0 tiles", "35 tiles");
            DrawSliders(ref Tabs[tabIndex]);

            AddNewLine(2f);

            AddSlider(innerCameraBoxY_Position, (string)innerCameraBoxY_Position.info.Tags[0], "0 tiles", "35 tiles");
            DrawSliders(ref Tabs[tabIndex]);

            DrawBox(ref Tabs[tabIndex]);

            //------------------//
            // vanilla type tab //
            //------------------//

            tabIndex++;
            Tabs[tabIndex] = new OpTab(this, "Vanilla");
            InitializeMarginAndPos();

            // Title
            AddNewLine();
            AddTextLabel("SBCameraScroll Mod", bigText: true);
            DrawTextLabels(ref Tabs[tabIndex]);

            // Subtitle
            AddNewLine(0.5f);
            AddTextLabel("Version " + MainMod.version, FLabelAlignment.Left);
            AddTextLabel("by " + MainMod.author, FLabelAlignment.Right);
            DrawTextLabels(ref Tabs[tabIndex]);

            // Content //
            AddNewLine();
            AddBox();

            AddTextLabel("Vanilla Type Camera:", FLabelAlignment.Left);
            DrawTextLabels(ref Tabs[tabIndex]);

            AddNewLine();

            AddSlider(outerCameraBoxX_Vanilla, (string)outerCameraBoxX_Vanilla.info.Tags[0], "0 tiles", "35 tiles"); // default is 9 => 180f // I think 188f would be exactly vanilla
            DrawSliders(ref Tabs[tabIndex]);

            AddNewLine(2f);

            AddSlider(outerCameraBoxY_Vanilla, (string)outerCameraBoxY_Vanilla.info.Tags[0], "0 tiles", "35 tiles"); // default is 1 => 20f // I think 18f would be exactly vanilla
            DrawSliders(ref Tabs[tabIndex]);

            DrawBox(ref Tabs[tabIndex]);

            //-------------------//
            // velocity type tab //
            //-------------------//

            // tabIndex++;
            // Tabs[tabIndex] = new OpTab(this, "Velocity");
            // InitializeMarginAndPos();

            // // Title
            // AddNewLine();
            // AddTextLabel("SBCameraScroll Mod", bigText: true);
            // DrawTextLabels(ref Tabs[tabIndex]);

            // // Subtitle
            // AddNewLine(0.5f);
            // AddTextLabel("Version " + MainMod.version, FLabelAlignment.Left);
            // AddTextLabel("by " + MainMod.author, FLabelAlignment.Right);
            // DrawTextLabels(ref Tabs[tabIndex]);

            // // Content //
            // AddNewLine();
            // AddBox();

            // AddTextLabel("Velocity Type Camera:", FLabelAlignment.Left);
            // DrawTextLabels(ref Tabs[tabIndex]);

            // AddNewLine();

            // AddSlider("innerCameraBoxX_Velocity", "Minimum Distance in X (3)", "The camera does not move when the player is closer than this.", new IntVector2(0, 35), defaultValue: 3, "0 tiles", "35 tiles");
            // AddSlider("outerCameraBoxX_Velocity", "Maximum Distance in X (6)", "When this distance is reached the camera always moves as fast as the player.", new IntVector2(0, 35), defaultValue: 6, "0 tiles", "35 tiles");
            // DrawSliders(this, ref Tabs[tabIndex]);

            // AddNewLine(2f);

            // AddSlider("innerCameraBoxY_Velocity", "Minimum Distance in Y (4)", "The camera does not move when the player is closer than this.", new IntVector2(0, 35), defaultValue: 4, "0 tiles", "35 tiles");
            // AddSlider("outerCameraBoxY_Velocity", "Maximum Distance in Y (7)", "When this distance is reached the camera always moves as fast as the player.", new IntVector2(0, 35), defaultValue: 7, "0 tiles", "35 tiles");
            // DrawSliders(this, ref Tabs[tabIndex]);

            // DrawBox(ref Tabs[tabIndex]);

            // save UI elements in variables for Update() function
            foreach (UIelement uiElement in Tabs[0].items)
            {
                if (uiElement is OpComboBox opComboBox && opComboBox.Key == "cameraType")
                {
                    cameraTypeComboBox = opComboBox;
                }
            }
        }

        public override void Update()
        {
            base.Update();
            if (cameraTypeComboBox != null)
            {
                int _cameraType = Array.IndexOf(cameraTypeKeys, cameraTypeComboBox.value);
                if (lastCameraType != _cameraType)
                {
                    lastCameraType = _cameraType;
                    cameraTypeComboBox.description = cameraTypeDescriptions[_cameraType];
                }
            }
        }


        public void MainModOptions_OnConfigChanged()
        {
            RoomCameraMod.cameraType = (CameraType)Array.IndexOf(cameraTypeKeys, cameraType.Value); // 0: Position type, 1: Vanilla type, 2: Velocity type

            Debug.Log("SBCameraScroll: cameraType " + RoomCameraMod.cameraType);
            Debug.Log("SBCameraScroll: Option_FogFullScreenEffect " + MainMod.Option_FogFullScreenEffect);
            Debug.Log("SBCameraScroll: Option_OtherFullScreenEffects " + MainMod.Option_OtherFullScreenEffects);
            Debug.Log("SBCameraScroll: Option_MergeWhileLoading " + MainMod.Option_MergeWhileLoading);
            Debug.Log("SBCameraScroll: Option_ScrollOneScreenRooms " + MainMod.Option_ScrollOneScreenRooms);


            RoomCameraMod.smoothingFactorX = smoothingFactorX.Value / 50f;
            RoomCameraMod.smoothingFactorY = smoothingFactorY.Value / 50f;

            Debug.Log("SBCameraScroll: smoothingFactorX " + RoomCameraMod.smoothingFactorX);
            Debug.Log("SBCameraScroll: smoothingFactorY " + RoomCameraMod.smoothingFactorY);

            switch (RoomCameraMod.cameraType)
            {
                case CameraType.Position:
                    RoomCameraMod.innerCameraBoxX = 20f * innerCameraBoxX_Position.Value;
                    RoomCameraMod.innerCameraBoxY = 20f * innerCameraBoxY_Position.Value;

                    Debug.Log("SBCameraScroll: innerCameraBoxX " + RoomCameraMod.innerCameraBoxX);
                    Debug.Log("SBCameraScroll: innerCameraBoxY " + RoomCameraMod.innerCameraBoxY);
                    break;
                case CameraType.Vanilla:
                    RoomCameraMod.outerCameraBoxX = 20f * outerCameraBoxX_Vanilla.Value;
                    RoomCameraMod.outerCameraBoxY = 20f * outerCameraBoxX_Vanilla.Value;

                    Debug.Log("SBCameraScroll: outerCameraBoxX " + RoomCameraMod.outerCameraBoxX);
                    Debug.Log("SBCameraScroll: outerCameraBoxY " + RoomCameraMod.outerCameraBoxY);
                    break;
                    // case CameraType.Velocity:
                    //     RoomCameraMod.innerCameraBoxX = 20f * float.Parse(ConvertToString("innerCameraBoxX_Velocity"));
                    //     RoomCameraMod.outerCameraBoxX = 20f * float.Parse(ConvertToString("outerCameraBoxX_Velocity"));
                    //     RoomCameraMod.innerCameraBoxY = 20f * float.Parse(ConvertToString("innerCameraBoxY_Velocity"));
                    //     RoomCameraMod.outerCameraBoxY = 20f * float.Parse(ConvertToString("outerCameraBoxY_Velocity"));

                    //     Debug.Log("SBCameraScroll: innerCameraBoxX " + RoomCameraMod.innerCameraBoxX);
                    //     Debug.Log("SBCameraScroll: innerCameraBoxY " + RoomCameraMod.innerCameraBoxY);
                    //     Debug.Log("SBCameraScroll: outerCameraBoxX " + RoomCameraMod.outerCameraBoxX);
                    //     Debug.Log("SBCameraScroll: outerCameraBoxY " + RoomCameraMod.outerCameraBoxY);
                    //     break;
            }
        }

        // public void ClearCacheButton_OnClick(UIfocusable _)
        // {
        //     // if (signal == "clearCache")
        //     // {
        //     FileInfo[] files = new DirectoryInfo(MainMod.modDirectoryPath).GetFiles("*.*", SearchOption.AllDirectories);
        //     for (int fileIndex = files.Length - 1; fileIndex >= 0; --fileIndex)
        //     {
        //         files[fileIndex].Delete();
        //     }
        //     // }

        //     if (clearCacheButton != null && Directory.GetFiles(MainMod.modDirectoryPath, "*.*", SearchOption.AllDirectories).Length == 0)
        //     {
        //         clearCacheButton.colorEdge.a = 0.1f;
        //         clearCacheButton.colorFill = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        //         clearCacheButton.greyedOut = true;
        //         // clearCacheButton.OnChange(); //TODO
        //     }
        // }

        //
        // private
        //

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

        private void AddCheckBox(Configurable<bool> configurable, string text)
        {
            checkBoxConfigurables.Add(configurable);
            checkBoxesTextLabels.Add(new OpLabel(new Vector2(), new Vector2(), text, FLabelAlignment.Left));
        }

        private void DrawCheckBoxes(ref OpTab tab) // changes pos.y but not pos.x
        {
            if (checkBoxConfigurables.Count != checkBoxesTextLabels.Count) return;

            float width = marginX.y - marginX.x;
            float elementWidth = (width - (numberOfCheckboxes - 1) * 0.5f * spacing) / numberOfCheckboxes;
            pos.y -= checkBoxSize;
            float _posX = pos.x;

            for (int checkBoxIndex = 0; checkBoxIndex < checkBoxConfigurables.Count; ++checkBoxIndex)
            {
                Configurable<bool> configurable = checkBoxConfigurables[checkBoxIndex];
                OpCheckBox checkBox = new(configurable, new Vector2(_posX, pos.y))
                {
                    description = configurable.info?.description ?? ""
                };
                tab.AddItems(checkBox);
                _posX += CheckBoxWithSpacing;

                OpLabel checkBoxLabel = checkBoxesTextLabels[checkBoxIndex];
                checkBoxLabel.pos = new Vector2(_posX, pos.y + 2f);
                checkBoxLabel.size = new Vector2(elementWidth - CheckBoxWithSpacing, fontHeight);
                tab.AddItems(checkBoxLabel);

                if (checkBoxIndex < checkBoxConfigurables.Count - 1)
                {
                    if ((checkBoxIndex + 1) % numberOfCheckboxes == 0)
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

            checkBoxConfigurables.Clear();
            checkBoxesTextLabels.Clear();
        }

        private void AddComboBox(Configurable<string> configurable, List<ListItem> list, string text, bool allowEmpty = false)
        {
            OpLabel opLabel = new(new Vector2(), new Vector2(0.0f, fontHeight), text, FLabelAlignment.Center, false);
            comboBoxesTextLabels.Add(opLabel);
            comboBoxConfigurables.Add(configurable);
            comboBoxLists.Add(list);
            comboBoxAllowEmpty.Add(allowEmpty);
        }

        private void DrawComboBoxes(ref OpTab tab)
        {
            if (comboBoxConfigurables.Count != comboBoxesTextLabels.Count) return;
            if (comboBoxConfigurables.Count != comboBoxLists.Count) return;
            if (comboBoxConfigurables.Count != comboBoxAllowEmpty.Count) return;

            float offsetX = (marginX.y - marginX.x) * 0.1f;
            float width = (marginX.y - marginX.x) * 0.4f;

            for (int comboBoxIndex = 0; comboBoxIndex < comboBoxConfigurables.Count; ++comboBoxIndex)
            {
                AddNewLine(1.25f);
                pos.x += offsetX;

                OpLabel opLabel = comboBoxesTextLabels[comboBoxIndex];
                opLabel.pos = pos;
                opLabel.size += new Vector2(width, 2f); // size.y is already set
                pos.x += width;

                Configurable<string> configurable = comboBoxConfigurables[comboBoxIndex];
                OpComboBox comboBox = new(configurable, pos, width, comboBoxLists[comboBoxIndex])
                {
                    allowEmpty = comboBoxAllowEmpty[comboBoxIndex],
                    description = configurable.info?.description ?? ""
                };
                tab.AddItems(opLabel, comboBox);

                // don't add a new line on the last element
                if (comboBoxIndex < comboBoxConfigurables.Count - 1)
                {
                    AddNewLine();
                    pos.x = marginX.x;
                }
            }

            comboBoxesTextLabels.Clear();
            comboBoxConfigurables.Clear();
            comboBoxLists.Clear();
            comboBoxAllowEmpty.Clear();
        }

        private void AddSlider(Configurable<int> configurable, string text, string sliderTextLeft = "", string sliderTextRight = "")
        {
            sliderConfigurables.Add(configurable);
            sliderMainTextLabels.Add(text);
            sliderTextLabelsLeft.Add(new OpLabel(new Vector2(), new Vector2(), sliderTextLeft, alignment: FLabelAlignment.Right)); // set pos and size when drawing
            sliderTextLabelsRight.Add(new OpLabel(new Vector2(), new Vector2(), sliderTextRight, alignment: FLabelAlignment.Left));
        }

        private void DrawSliders(ref OpTab tab)
        {
            if (sliderConfigurables.Count != sliderMainTextLabels.Count) return;
            if (sliderConfigurables.Count != sliderTextLabelsLeft.Count) return;
            if (sliderConfigurables.Count != sliderTextLabelsRight.Count) return;

            float width = marginX.y - marginX.x;
            float sliderCenter = marginX.x + 0.5f * width;
            float sliderLabelSizeX = 0.2f * width;
            float sliderSizeX = width - 2f * sliderLabelSizeX - spacing;

            for (int sliderIndex = 0; sliderIndex < sliderConfigurables.Count; ++sliderIndex)
            {
                AddNewLine(2f);

                OpLabel opLabel = sliderTextLabelsLeft[sliderIndex];
                opLabel.pos = new Vector2(marginX.x, pos.y + 5f);
                opLabel.size = new Vector2(sliderLabelSizeX, fontHeight);
                tab.AddItems(opLabel);

                Configurable<int> configurable = sliderConfigurables[sliderIndex];
                OpSlider slider = new(configurable, new Vector2(sliderCenter - 0.5f * sliderSizeX, pos.y), (int)sliderSizeX)
                {
                    size = new Vector2(sliderSizeX, fontHeight),
                    description = configurable.info?.description ?? ""
                };
                tab.AddItems(slider);

                opLabel = sliderTextLabelsRight[sliderIndex];
                opLabel.pos = new Vector2(sliderCenter + 0.5f * sliderSizeX + 0.5f * spacing, pos.y + 5f);
                opLabel.size = new Vector2(sliderLabelSizeX, fontHeight);
                tab.AddItems(opLabel);

                AddTextLabel(sliderMainTextLabels[sliderIndex]);
                DrawTextLabels(ref tab);

                if (sliderIndex < sliderConfigurables.Count - 1)
                {
                    AddNewLine();
                }
            }

            sliderConfigurables.Clear();
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

            OpLabel textLabel = new(new Vector2(), new Vector2(20f, textHeight), text, alignment, bigText) // minimal size.x = 20f
            {
                autoWrap = true
            };
            textLabels.Add(textLabel);
        }

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