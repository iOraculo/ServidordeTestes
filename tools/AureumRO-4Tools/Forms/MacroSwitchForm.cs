using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class MacroSwitchForm : Form, IObserver
    {
        public static int TOTAL_MACRO_LANES = 5;
        public const int MAX_SLOTS_PER_LANE = 7;

        private const int LANE_WIDTH = 548;
        private const int LANE_HEIGHT = 158;
        private const int LANE_LEFT = 10;
        private const int LANE_TOP_START = 8;
        private const int LANE_SPACING = 10;
        private const int SLOT_START_X = 66;
        private const int SLOT_STEP_X = 68;

        private sealed class LaneUi
        {
            public int Id;
            public GroupBox Group;
            public TextBox TriggerBox;
            public TextBox[] KeyBoxes = new TextBox[MAX_SLOTS_PER_LANE];
            public NumericUpDown[] DelayInputs = new NumericUpDown[MAX_SLOTS_PER_LANE];
            public CheckBox[] ClickChecks = new CheckBox[MAX_SLOTS_PER_LANE];
            public Label[] ArrowLabels = new Label[MAX_SLOTS_PER_LANE - 1];
            public CheckBox LoopCheck;
            public Button BtnAddSlot;
            public Button BtnRemoveSlot;
            public Button BtnClearLane;
            public Button BtnRemoveLane;
            public int VisibleSlots = 1;
            public bool IsLaneVisible;
        }

        private readonly LaneUi[] lanes = new LaneUi[TOTAL_MACRO_LANES];
        private Button btnAddSwitch;
        private ToolTip switchToolTip;
        private bool isUpdatingUi;

        public MacroSwitchForm(Subject subject)
        {
            subject.Attach(this);
            InitializeComponent();
            BuildSimplifiedUi();
            updateUi();
        }

        private void BuildSimplifiedUi()
        {
            this.SuspendLayout();
            try
            {
                this.Controls.Clear();
                this.AutoScroll = true;
                this.AutoScrollMargin = new Size(0, 12);
                this.switchToolTip = FormUtils.CreateHelpToolTip(this.components);

                for (int i = 0; i < TOTAL_MACRO_LANES; i++)
                {
                    int id = i + 1;
                    LaneUi lane = CreateLaneUi(id);
                    lane.IsLaneVisible = (id == 1);
                    lane.VisibleSlots = 1;
                    this.lanes[i] = lane;
                    this.Controls.Add(lane.Group);
                    GetOrCreateChainConfig(id);
                }

                this.btnAddSwitch = new Button
                {
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Name = "btnAddSwitchLane",
                    Size = new Size(166, 25),
                    Text = "+ Add Switch",
                    UseVisualStyleBackColor = true
                };
                this.btnAddSwitch.Click += OnAddSwitchClick;
                this.Controls.Add(this.btnAddSwitch);

                RefreshTooltips();
                LayoutVisibleLanes();
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        private LaneUi CreateLaneUi(int id)
        {
            LaneUi lane = new LaneUi { Id = id };

            GroupBox group = new GroupBox
            {
                Name = "chainGroup" + id,
                Size = new Size(LANE_WIDTH, LANE_HEIGHT),
                TabStop = false,
                Text = "Switch " + id
            };
            lane.Group = group;

            // Row 0: Dedicated Trigger key + Loop + Clear / Remove Switch
            Label lblTrigger = new Label
            {
                AutoSize = false,
                Location = new Point(4, 21),
                Size = new Size(58, 18),
                Text = "Trigger:",
                TextAlign = ContentAlignment.MiddleRight
            };

            lane.TriggerBox = new TextBox
            {
                Location = new Point(66, 20),
                Name = "inTriggerSwitch" + id,
                Size = new Size(52, 20),
                Tag = id + ":Trigger",
                Text = Key.None.ToString()
            };
            lane.TriggerBox.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
            lane.TriggerBox.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
            lane.TriggerBox.TextChanged += new EventHandler(this.onTriggerTextChange);

            lane.LoopCheck = new CheckBox
            {
                AutoSize = true,
                Cursor = System.Windows.Forms.Cursors.Hand,
                Location = new Point(128, 21),
                Name = "loopMac" + id,
                Text = "Loop",
                Tag = id,
                UseVisualStyleBackColor = true
            };
            lane.LoopCheck.CheckedChanged += new EventHandler(this.onLoopCheckedChanged);

            Label lblTriggerHint = new Label
            {
                AutoSize = true,
                Location = new Point(184, 23),
                Tag = ThemeManager.TAG_MUTED_TEXT,
                Text = "(Key that triggers this switch)"
            };

            lane.BtnClearLane = new Button
            {
                Cursor = System.Windows.Forms.Cursors.Hand,
                Location = new Point(id == 1 ? 472 : 374, 19),
                Name = "btnClearMac" + id,
                Size = new Size(id == 1 ? 66 : 62, 22),
                Text = "Clear",
                Tag = id,
                UseVisualStyleBackColor = true
            };
            lane.BtnClearLane.Click += OnClearLaneClick;

            Panel headerDivider = new Panel
            {
                Location = new Point(10, 47),
                Size = new Size(LANE_WIDTH - 20, 1)
            };

            group.Controls.Add(lblTrigger);
            group.Controls.Add(lane.TriggerBox);
            group.Controls.Add(lane.LoopCheck);
            group.Controls.Add(lblTriggerHint);
            group.Controls.Add(lane.BtnClearLane);
            group.Controls.Add(headerDivider);

            if (id > 1)
            {
                lane.BtnRemoveLane = new Button
                {
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(442, 19),
                    Name = "btnRemoveLaneMac" + id,
                    Size = new Size(96, 22),
                    Text = "Remove Switch",
                    Tag = id,
                    UseVisualStyleBackColor = true
                };
                lane.BtnRemoveLane.Click += OnRemoveLaneClick;
                group.Controls.Add(lane.BtnRemoveLane);
            }

            // Sequence area below the divider
            Label lblKeys = new Label
            {
                AutoSize = false,
                Location = new Point(4, 57),
                Size = new Size(58, 18),
                Text = "Keys:",
                TextAlign = ContentAlignment.MiddleRight
            };

            Label lblDelay = new Label
            {
                AutoSize = false,
                Location = new Point(2, 82),
                Size = new Size(60, 18),
                Text = "Delays(ms):",
                TextAlign = ContentAlignment.MiddleRight
            };

            group.Controls.Add(lblKeys);
            group.Controls.Add(lblDelay);

            for (int s = 0; s < MAX_SLOTS_PER_LANE; s++)
            {
                int slotNum = s + 1;
                int x = SLOT_START_X + (s * SLOT_STEP_X);
                string keyName = "in" + slotNum + "mac" + id;

                TextBox tb = new TextBox
                {
                    Location = new Point(x, 56),
                    Name = keyName,
                    Size = new Size(50, 20),
                    Text = Key.None.ToString()
                };
                tb.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
                tb.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
                tb.TextChanged += new EventHandler(this.onTextChange);

                NumericUpDown delay = new NumericUpDown
                {
                    Location = new Point(x, 81),
                    Maximum = 99999,
                    Minimum = 0,
                    Name = keyName + "delay",
                    Size = new Size(56, 20),
                    Value = 50
                };
                delay.ValueChanged += new EventHandler(this.onDelayChange);

                CheckBox clickCheck = new CheckBox
                {
                    AutoSize = true,
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(x, 106),
                    Name = keyName + "click",
                    Text = "Click",
                    UseVisualStyleBackColor = true
                };
                clickCheck.CheckedChanged += new EventHandler(this.onCheckClickChange);

                lane.KeyBoxes[s] = tb;
                lane.DelayInputs[s] = delay;
                lane.ClickChecks[s] = clickCheck;

                group.Controls.Add(tb);
                group.Controls.Add(delay);
                group.Controls.Add(clickCheck);

                if (s < MAX_SLOTS_PER_LANE - 1)
                {
                    Label arrow = new Label
                    {
                        AutoSize = false,
                        Location = new Point(x + 51, 57),
                        Name = "arrow" + slotNum + "mac" + id,
                        Size = new Size(16, 18),
                        Tag = ThemeManager.TAG_ACCENT_TEXT,
                        Text = "→",
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    lane.ArrowLabels[s] = arrow;
                    group.Controls.Add(arrow);
                }
            }

            lane.BtnAddSlot = new Button
            {
                Cursor = System.Windows.Forms.Cursors.Hand,
                Location = new Point(66, 128),
                Name = "btnAddSlotMac" + id,
                Size = new Size(122, 22),
                Text = "+ Add Slot",
                Tag = id,
                UseVisualStyleBackColor = true
            };
            lane.BtnAddSlot.Click += OnAddSlotClick;
            group.Controls.Add(lane.BtnAddSlot);

            lane.BtnRemoveSlot = new Button
            {
                Cursor = System.Windows.Forms.Cursors.Hand,
                Location = new Point(194, 128),
                Name = "btnRemoveSlotMac" + id,
                Size = new Size(118, 22),
                Text = "- Remove Slot",
                Tag = id,
                UseVisualStyleBackColor = true
            };
            lane.BtnRemoveSlot.Click += OnRemoveSlotClick;
            group.Controls.Add(lane.BtnRemoveSlot);

            return lane;
        }

        private void RefreshTooltips()
        {
            if (this.switchToolTip == null)
            {
                return;
            }

            string triggerTip = LocalizationManager.T("tooltip.switch_trigger_dedicated");
            string stepTip = LocalizationManager.T("tooltip.switch_step_key");
            string delayTip = LocalizationManager.T("tooltip.switch_delay");
            string loopTip = LocalizationManager.T("tooltip.switch_loop");
            string clickTip = LocalizationManager.T("tooltip.switch_click");
            string addSlotTip = LocalizationManager.T("tooltip.switch_add_slot");
            string removeSlotTip = LocalizationManager.T("tooltip.switch_remove_slot");
            string clearTip = LocalizationManager.T("tooltip.switch_clear");
            string removeLaneTip = LocalizationManager.T("tooltip.switch_remove_lane");

            for (int i = 0; i < this.lanes.Length; i++)
            {
                LaneUi lane = this.lanes[i];
                if (lane == null)
                {
                    continue;
                }

                this.switchToolTip.SetToolTip(lane.TriggerBox, triggerTip);
                this.switchToolTip.SetToolTip(lane.LoopCheck, loopTip);
                this.switchToolTip.SetToolTip(lane.BtnAddSlot, addSlotTip);
                this.switchToolTip.SetToolTip(lane.BtnRemoveSlot, removeSlotTip);
                this.switchToolTip.SetToolTip(lane.BtnClearLane, clearTip);
                if (lane.BtnRemoveLane != null)
                {
                    this.switchToolTip.SetToolTip(lane.BtnRemoveLane, removeLaneTip);
                }
                for (int s = 0; s < MAX_SLOTS_PER_LANE; s++)
                {
                    this.switchToolTip.SetToolTip(lane.KeyBoxes[s], stepTip);
                    this.switchToolTip.SetToolTip(lane.DelayInputs[s], delayTip);
                    this.switchToolTip.SetToolTip(lane.ClickChecks[s], clickTip);
                }
            }

            if (this.btnAddSwitch != null)
            {
                this.switchToolTip.SetToolTip(this.btnAddSwitch, LocalizationManager.T("tooltip.switch_add_lane"));
            }
        }

        private void RefreshLaneSlots(LaneUi lane)
        {
            if (lane == null)
            {
                return;
            }

            int visible = Math.Max(1, Math.Min(MAX_SLOTS_PER_LANE, lane.VisibleSlots));
            lane.VisibleSlots = visible;

            lane.Group.SuspendLayout();
            try
            {
                for (int s = 0; s < MAX_SLOTS_PER_LANE; s++)
                {
                    bool showSlot = s < visible;
                    lane.KeyBoxes[s].Visible = showSlot;
                    lane.DelayInputs[s].Visible = showSlot;
                    lane.ClickChecks[s].Visible = showSlot;

                    if (s < MAX_SLOTS_PER_LANE - 1)
                    {
                        lane.ArrowLabels[s].Visible = (s + 1) < visible;
                    }
                }

                lane.BtnAddSlot.Visible = visible < MAX_SLOTS_PER_LANE;
                lane.BtnAddSlot.Enabled = visible < MAX_SLOTS_PER_LANE;
                lane.BtnRemoveSlot.Visible = visible > 1;
                lane.BtnRemoveSlot.Enabled = visible > 1;
                lane.BtnRemoveSlot.Location = new Point(visible < MAX_SLOTS_PER_LANE ? 194 : 66, 128);
            }
            finally
            {
                lane.Group.ResumeLayout(true);
            }
        }

        private void LayoutVisibleLanes()
        {
            this.SuspendLayout();
            try
            {
                this.AutoScrollPosition = Point.Empty;
                int y = LANE_TOP_START;
                bool hasHiddenLane = false;

                for (int i = 0; i < this.lanes.Length; i++)
                {
                    LaneUi lane = this.lanes[i];
                    if (lane == null)
                    {
                        continue;
                    }

                    RefreshLaneSlots(lane);
                    lane.Group.Visible = lane.IsLaneVisible;
                    if (lane.IsLaneVisible)
                    {
                        lane.Group.Location = new Point(LANE_LEFT, y);
                        y += LANE_HEIGHT + LANE_SPACING;
                    }
                    else
                    {
                        hasHiddenLane = true;
                    }
                }

                if (this.btnAddSwitch != null)
                {
                    this.btnAddSwitch.Location = new Point(LANE_LEFT, y);
                    this.btnAddSwitch.Visible = hasHiddenLane;
                    this.btnAddSwitch.Enabled = hasHiddenLane;
                }
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        private LaneUi FindLaneById(int id)
        {
            if (id < 1 || id > TOTAL_MACRO_LANES)
            {
                return null;
            }

            return this.lanes[id - 1];
        }

        public void AddSlotToLane(int id)
        {
            LaneUi lane = FindLaneById(id);
            if (lane == null || lane.VisibleSlots >= MAX_SLOTS_PER_LANE)
            {
                return;
            }

            lane.VisibleSlots++;
            RefreshLaneSlots(lane);
        }

        public void RemoveLastSlotFromLane(int id)
        {
            LaneUi lane = FindLaneById(id);
            if (lane == null || lane.VisibleSlots <= 1)
            {
                return;
            }

            int slotIndex = lane.VisibleSlots - 1;
            int slotNum = slotIndex + 1;
            string entryKey = "in" + slotNum + "mac" + id;

            this.isUpdatingUi = true;
            try
            {
                lane.KeyBoxes[slotIndex].Text = Key.None.ToString();
                FormUtils.SetNumericValue(lane.DelayInputs[slotIndex], 50);
                lane.ClickChecks[slotIndex].Checked = false;
            }
            finally
            {
                this.isUpdatingUi = false;
            }

            ChainConfig chainConfig = GetOrCreateChainConfig(id);
            chainConfig.macroEntries.Remove(entryKey);
            if (!ProfileSingleton.IsSaveSuppressed)
            {
                ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().MacroSwitch);
            }

            lane.VisibleSlots--;
            RefreshLaneSlots(lane);
        }

        public void ClearLane(int id, bool hideLane = false)
        {
            LaneUi lane = FindLaneById(id);
            if (lane == null)
            {
                return;
            }

            this.isUpdatingUi = true;
            try
            {
                lane.TriggerBox.Text = Key.None.ToString();
                for (int s = 0; s < MAX_SLOTS_PER_LANE; s++)
                {
                    lane.KeyBoxes[s].Text = Key.None.ToString();
                    FormUtils.SetNumericValue(lane.DelayInputs[s], 50);
                    lane.ClickChecks[s].Checked = false;
                }
                lane.LoopCheck.Checked = false;
            }
            finally
            {
                this.isUpdatingUi = false;
            }

            ChainConfig chainConfig = GetOrCreateChainConfig(id);
            chainConfig.trigger = Key.None;
            chainConfig.infinityLoop = false;
            chainConfig.infinityLoopOn = false;
            chainConfig.macroEntries.Clear();
            if (!ProfileSingleton.IsSaveSuppressed)
            {
                ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().MacroSwitch);
            }

            lane.VisibleSlots = 1;
            if (hideLane && id > 1)
            {
                lane.IsLaneVisible = false;
            }

            LayoutVisibleLanes();
        }

        public bool AddNextSwitchLane()
        {
            for (int i = 1; i < this.lanes.Length; i++)
            {
                LaneUi lane = this.lanes[i];
                if (lane != null && !lane.IsLaneVisible)
                {
                    lane.IsLaneVisible = true;
                    lane.VisibleSlots = 1;
                    LayoutVisibleLanes();
                    return true;
                }
            }

            return false;
        }

        public int GetVisibleSlotCount(int id)
        {
            LaneUi lane = FindLaneById(id);
            return lane != null ? lane.VisibleSlots : 0;
        }

        public bool IsLaneVisible(int id)
        {
            LaneUi lane = FindLaneById(id);
            return lane != null && lane.IsLaneVisible;
        }

        private void OnAddSlotClick(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag is int)
            {
                AddSlotToLane((int)btn.Tag);
            }
        }

        private void OnRemoveSlotClick(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag is int)
            {
                RemoveLastSlotFromLane((int)btn.Tag);
            }
        }

        private void OnClearLaneClick(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag is int)
            {
                ClearLane((int)btn.Tag, false);
            }
        }

        private void OnRemoveLaneClick(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag is int)
            {
                ClearLane((int)btn.Tag, true);
            }
        }

        private void OnAddSwitchClick(object sender, EventArgs e)
        {
            AddNextSwitchLane();
        }

        public void Update(ISubject subject)
        {
            Subject subj = subject as Subject;
            if (subj == null || subj.Message == null)
            {
                return;
            }

            switch (subj.Message.code)
            {
                case MessageCode.PROFILE_CHANGED:
                    updateUi();
                    break;
                case MessageCode.LANGUAGE_CHANGED:
                    RefreshTooltips();
                    break;
                case MessageCode.TURN_ON:
                    ProfileSingleton.GetCurrent().MacroSwitch.Start();
                    break;
                case MessageCode.TURN_OFF:
                    ProfileSingleton.GetCurrent().MacroSwitch.Stop();
                    break;
            }
        }

        private void UpdatePanelData(int id)
        {
            this.isUpdatingUi = true;
            try
            {
                LaneUi lane = FindLaneById(id);
                if (lane == null)
                {
                    return;
                }

                Macro macroSwitch = ProfileSingleton.GetCurrent().MacroSwitch;
                ChainConfig existing = macroSwitch.chainConfigs.Find(cfg => cfg.id == id);
                ChainConfig chainConfig = existing != null ? new ChainConfig(existing) : new ChainConfig(id);

                lane.TriggerBox.Text = chainConfig.trigger.ToString();
                for (int s = 0; s < MAX_SLOTS_PER_LANE; s++)
                {
                    lane.KeyBoxes[s].Text = Key.None.ToString();
                    FormUtils.SetNumericValue(lane.DelayInputs[s], 50);
                    lane.ClickChecks[s].Checked = false;
                }
                lane.LoopCheck.Checked = chainConfig.infinityLoop;

                int highestSlot = 0;
                for (int s = 0; s < MAX_SLOTS_PER_LANE; s++)
                {
                    int slotNum = s + 1;
                    string entryKey = "in" + slotNum + "mac" + id;
                    MacroKey entry;
                    if (chainConfig.macroEntries != null && chainConfig.macroEntries.TryGetValue(entryKey, out entry) && entry != null)
                    {
                        lane.KeyBoxes[s].Text = entry.key.ToString();
                        FormUtils.SetNumericValue(lane.DelayInputs[s], entry.delay);
                        lane.ClickChecks[s].Checked = entry.hasClick;

                        if (entry.key != Key.None)
                        {
                            highestSlot = slotNum;
                        }
                    }
                }

                lane.IsLaneVisible = (id == 1) || chainConfig.trigger != Key.None || highestSlot > 0 || chainConfig.infinityLoop;
                lane.VisibleSlots = Math.Max(1, highestSlot);
            }
            catch (Exception ex)
            {
                Logger.Warn("MacroSwitchForm", "Failed to update lane " + id, ex);
            }
            finally
            {
                this.isUpdatingUi = false;
            }
        }

        private ChainConfig GetOrCreateChainConfig(int chainID)
        {
            Macro macroSwitch = ProfileSingleton.GetCurrent().MacroSwitch;
            ChainConfig chainConfig = macroSwitch.chainConfigs.Find(config => config.id == chainID);
            if (chainConfig == null)
            {
                chainConfig = new ChainConfig(chainID, Key.None);
                macroSwitch.chainConfigs.Add(chainConfig);
            }

            return chainConfig;
        }

        private void onTriggerTextChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            TextBox textBox = sender as TextBox;
            if (textBox == null || textBox.Tag == null)
            {
                return;
            }

            string[] parts = textBox.Tag.ToString().Split(new[] { ":" }, StringSplitOptions.None);
            if (parts.Length < 1)
            {
                return;
            }

            int chainID;
            if (!int.TryParse(parts[0], out chainID))
            {
                return;
            }

            Key key;
            if (!KeyHelper.TryParseKey(textBox.Text, out key))
            {
                return;
            }

            ChainConfig chainConfig = GetOrCreateChainConfig(chainID);
            chainConfig.trigger = key;
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().MacroSwitch);
        }

        private void onTextChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            TextBox textBox = sender as TextBox;
            if (textBox == null || textBox.Parent == null)
            {
                return;
            }

            int chainID = short.Parse(textBox.Parent.Name.Split(new[] { "chainGroup" }, StringSplitOptions.None)[1]);
            GroupBox group = (GroupBox)textBox.Parent;
            ChainConfig chainConfig = GetOrCreateChainConfig(chainID);

            Key key;
            if (!KeyHelper.TryParseKey(textBox.Text, out key))
            {
                return;
            }

            int delayVal = 50;
            Control[] delayControls = group.Controls.Find(textBox.Name + "delay", true);
            if (delayControls.Length > 0)
            {
                delayVal = decimal.ToInt32(((NumericUpDown)delayControls[0]).Value);
            }

            bool hasClick = false;
            Control[] clickControls = group.Controls.Find(textBox.Name + "click", true);
            if (clickControls.Length > 0)
            {
                hasClick = ((CheckBox)clickControls[0]).Checked;
            }

            chainConfig.macroEntries[textBox.Name] = new MacroKey(key, delayVal, hasClick);
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().MacroSwitch);
        }

        private void onDelayChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            NumericUpDown delayInput = sender as NumericUpDown;
            if (delayInput == null || delayInput.Parent == null)
            {
                return;
            }

            int chainID = short.Parse(delayInput.Parent.Name.Split(new[] { "chainGroup" }, StringSplitOptions.None)[1]);
            ChainConfig chainConfig = GetOrCreateChainConfig(chainID);

            string cbName = delayInput.Name.Split(new[] { "delay" }, StringSplitOptions.None)[0];
            int newDelay = decimal.ToInt32(delayInput.Value);
            MacroKey existing;
            if (chainConfig.macroEntries.TryGetValue(cbName, out existing) && existing != null)
            {
                existing.delay = newDelay;
            }
            else
            {
                chainConfig.macroEntries[cbName] = new MacroKey(Key.None, newDelay);
            }

            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().MacroSwitch);
        }

        private void onCheckClickChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            CheckBox checkInput = sender as CheckBox;
            if (checkInput == null || checkInput.Parent == null)
            {
                return;
            }

            int chainID = short.Parse(checkInput.Parent.Name.Split(new[] { "chainGroup" }, StringSplitOptions.None)[1]);
            ChainConfig chainConfig = GetOrCreateChainConfig(chainID);

            string cbName = checkInput.Name.Split(new[] { "click" }, StringSplitOptions.None)[0];
            MacroKey existing;
            if (chainConfig.macroEntries.TryGetValue(cbName, out existing) && existing != null)
            {
                existing.hasClick = checkInput.Checked;
            }
            else
            {
                chainConfig.macroEntries[cbName] = new MacroKey(Key.None, 50, checkInput.Checked);
            }

            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().MacroSwitch);
        }

        private void onLoopCheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            CheckBox loopCheck = sender as CheckBox;
            if (loopCheck == null || !(loopCheck.Tag is int))
            {
                return;
            }

            int chainID = (int)loopCheck.Tag;
            ChainConfig chainConfig = GetOrCreateChainConfig(chainID);
            chainConfig.infinityLoop = loopCheck.Checked;
            if (!loopCheck.Checked)
            {
                chainConfig.infinityLoopOn = false;
            }

            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().MacroSwitch);
        }

        private void updateUi()
        {
            for (int i = 1; i <= TOTAL_MACRO_LANES; i++)
            {
                UpdatePanelData(i);
            }

            LayoutVisibleLanes();
        }
    }
}
