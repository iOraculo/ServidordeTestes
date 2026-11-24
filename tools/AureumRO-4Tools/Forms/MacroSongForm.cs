using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class MacroSongForm : Form, IObserver
    {
        public static int TOTAL_MACRO_LANES_FOR_SONGS = 4;
        public const int MAX_SONG_SLOTS_PER_LANE = 8;

        private const int LANE_WIDTH = 574;
        private const int LANE_HEIGHT_ONE_ROW = 128;
        private const int LANE_HEIGHT_TWO_ROWS = 154;
        private const int LANE_LEFT = 10;
        private const int LANE_TOP_START = 8;
        private const int LANE_SPACING = 10;

        private sealed class SongLaneUi
        {
            public int Id;
            public GroupBox Group;
            public TextBox TriggerBox;
            public NumericUpDown DelayInput;
            public PictureBox DaggerIconBox;
            public TextBox DaggerBox;
            public PictureBox InstrumentIconBox;
            public TextBox InstrumentBox;
            public TextBox[] SongBoxes = new TextBox[MAX_SONG_SLOTS_PER_LANE];
            public Label[] ArrowLabels = new Label[MAX_SONG_SLOTS_PER_LANE - 1];
            public Button BtnAddSlot;
            public Button BtnRemoveSlot;
            public Button BtnResetLane;
            public Button BtnRemoveLane;
            public int VisibleSlots = 1;
            public bool IsLaneVisible;
        }

        private readonly SongLaneUi[] lanes = new SongLaneUi[TOTAL_MACRO_LANES_FOR_SONGS];
        private Button btnAddSongMacro;
        private ToolTip songToolTip;
        private Image daggerIcon;
        private Image instrumentIcon;
        private bool isUpdatingUi;

        public MacroSongForm(Subject subject)
        {
            subject.Attach(this);
            InitializeComponent();
            CaptureEquipmentIcons();
            BuildSimplifiedUi();
            updateUi();
        }

        private void CaptureEquipmentIcons()
        {
            if (this.pictureBox5 != null && this.pictureBox5.BackgroundImage != null)
            {
                this.daggerIcon = ThemeManager.StripLightBackground(this.pictureBox5.BackgroundImage);
            }
            if (this.pictureBox6 != null && this.pictureBox6.BackgroundImage != null)
            {
                this.instrumentIcon = ThemeManager.StripLightBackground(this.pictureBox6.BackgroundImage);
            }
        }

        private void BuildSimplifiedUi()
        {
            this.SuspendLayout();
            try
            {
                this.Controls.Clear();
                this.AutoScroll = true;
                this.AutoScrollMargin = new Size(0, 12);
                this.songToolTip = FormUtils.CreateHelpToolTip(this.components);

                for (int i = 0; i < TOTAL_MACRO_LANES_FOR_SONGS; i++)
                {
                    int id = i + 1;
                    SongLaneUi lane = CreateLaneUi(id);
                    lane.IsLaneVisible = (id == 1);
                    lane.VisibleSlots = 1;
                    this.lanes[i] = lane;
                    this.Controls.Add(lane.Group);
                    GetOrCreateChainConfig(id);
                }

                this.btnAddSongMacro = new Button
                {
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Name = "btnAddSongMacroLane",
                    Size = new Size(186, 25),
                    Text = "+ Add Song Macro",
                    UseVisualStyleBackColor = true
                };
                this.btnAddSongMacro.Click += OnAddSongMacroClick;
                this.Controls.Add(this.btnAddSongMacro);

                RefreshTooltips();
                LayoutVisibleLanes();
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        private SongLaneUi CreateLaneUi(int id)
        {
            SongLaneUi lane = new SongLaneUi { Id = id };

            GroupBox group = new GroupBox
            {
                Name = "panelMacro" + id,
                Size = new Size(LANE_WIDTH, LANE_HEIGHT_ONE_ROW),
                TabStop = false,
                Text = "Song Macro " + id
            };
            lane.Group = group;

            // Standardized Row 0 Header: Trigger + Delay(ms) + Dagger + Instrument + Clear / Remove Macro
            Label lblTrigger = new Label
            {
                AutoSize = false,
                Location = new Point(4, 21),
                Size = new Size(54, 18),
                Text = "Trigger:",
                TextAlign = ContentAlignment.MiddleRight
            };

            lane.TriggerBox = new TextBox
            {
                Location = new Point(62, 20),
                Name = "inTriggerMacro" + id,
                Size = new Size(48, 20),
                Tag = id + ":Trigger",
                Text = Key.None.ToString()
            };
            AttachKeyHandlers(lane.TriggerBox);

            Label lblDelay = new Label
            {
                AutoSize = false,
                Location = new Point(114, 21),
                Size = new Size(60, 18),
                Text = "Delays(ms):",
                TextAlign = ContentAlignment.MiddleRight
            };

            lane.DelayInput = new NumericUpDown
            {
                Location = new Point(176, 20),
                Maximum = 99999,
                Minimum = 0,
                Name = "delayMac" + id,
                Size = new Size(52, 20),
                Value = 50
            };
            lane.DelayInput.ValueChanged += new EventHandler(this.onDelayChange);

            PictureBox pbDagger = new PictureBox
            {
                BackColor = Color.Transparent,
                Image = this.daggerIcon,
                Location = new Point(236, 18),
                Name = "pbDagger" + id,
                Size = new Size(22, 22),
                SizeMode = PictureBoxSizeMode.Zoom,
                TabStop = false
            };

            lane.DaggerBox = new TextBox
            {
                Location = new Point(260, 20),
                Name = "inDaggerMacro" + id,
                Size = new Size(44, 20),
                Tag = id + ":Dagger",
                Text = Key.None.ToString()
            };
            AttachKeyHandlers(lane.DaggerBox);

            PictureBox pbInstrument = new PictureBox
            {
                BackColor = Color.Transparent,
                Image = this.instrumentIcon,
                Location = new Point(310, 18),
                Name = "pbInstrument" + id,
                Size = new Size(22, 22),
                SizeMode = PictureBoxSizeMode.Zoom,
                TabStop = false
            };

            lane.InstrumentBox = new TextBox
            {
                Location = new Point(334, 20),
                Name = "inInstrumentMacro" + id,
                Size = new Size(44, 20),
                Tag = id + ":Instrument",
                Text = Key.None.ToString()
            };
            AttachKeyHandlers(lane.InstrumentBox);

            lane.BtnResetLane = new Button
            {
                Cursor = System.Windows.Forms.Cursors.Hand,
                Location = new Point(id == 1 ? 492 : 396, 17),
                Name = "btnResMac" + id,
                Size = new Size(id == 1 ? 72 : 60, 25),
                Text = "Clear",
                Tag = id,
                UseVisualStyleBackColor = true
            };
            lane.BtnResetLane.Click += OnResetLaneClick;

            Panel headerDivider = new Panel
            {
                Location = new Point(10, 47),
                Size = new Size(LANE_WIDTH - 20, 1)
            };

            lane.DaggerIconBox = pbDagger;
            lane.InstrumentIconBox = pbInstrument;

            group.Controls.Add(lblTrigger);
            group.Controls.Add(lane.TriggerBox);
            group.Controls.Add(lblDelay);
            group.Controls.Add(lane.DelayInput);
            group.Controls.Add(pbDagger);
            group.Controls.Add(lane.DaggerBox);
            group.Controls.Add(pbInstrument);
            group.Controls.Add(lane.InstrumentBox);
            group.Controls.Add(lane.BtnResetLane);
            group.Controls.Add(headerDivider);

            if (id > 1)
            {
                lane.BtnRemoveLane = new Button
                {
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(462, 17),
                    Name = "btnRemoveSongLane" + id,
                    Size = new Size(102, 25),
                    Text = "Remove Macro",
                    Tag = id,
                    UseVisualStyleBackColor = true
                };
                lane.BtnRemoveLane.Click += OnRemoveLaneClick;
                group.Controls.Add(lane.BtnRemoveLane);
            }

            // Below divider: Songs sequence (2 rows of 4 slots, 1..8)
            Label lblSongs = new Label
            {
                AutoSize = false,
                Location = new Point(4, 57),
                Size = new Size(54, 18),
                Text = "Songs:",
                TextAlign = ContentAlignment.MiddleRight
            };
            group.Controls.Add(lblSongs);

            for (int s = 0; s < MAX_SONG_SLOTS_PER_LANE; s++)
            {
                int slotNum = s + 1;
                int row = s / 4;
                int col = s % 4;
                int x = 62 + (col * 72);
                int y = row == 0 ? 56 : 84;

                TextBox tb = new TextBox
                {
                    Location = new Point(x, y),
                    Name = "in" + slotNum + "mac" + id,
                    Size = new Size(52, 20),
                    Text = Key.None.ToString()
                };
                AttachKeyHandlers(tb);
                lane.SongBoxes[s] = tb;
                group.Controls.Add(tb);

                if (s < MAX_SONG_SLOTS_PER_LANE - 1)
                {
                    Label arrow;
                    if (s == 3)
                    {
                        arrow = new Label
                        {
                            AutoSize = false,
                            Location = new Point(x + 54, y + 1),
                            Name = "arrow" + slotNum + "mac" + id,
                            Size = new Size(16, 18),
                            Tag = ThemeManager.TAG_ACCENT_TEXT,
                            Text = "↴",
                            TextAlign = ContentAlignment.MiddleCenter
                        };
                    }
                    else
                    {
                        arrow = new Label
                        {
                            AutoSize = false,
                            Location = new Point(x + 54, y + 1),
                            Name = "arrow" + slotNum + "mac" + id,
                            Size = new Size(16, 18),
                            Tag = ThemeManager.TAG_ACCENT_TEXT,
                            Text = "→",
                            TextAlign = ContentAlignment.MiddleCenter
                        };
                    }

                    lane.ArrowLabels[s] = arrow;
                    group.Controls.Add(arrow);
                }
            }

            lane.BtnAddSlot = new Button
            {
                Cursor = System.Windows.Forms.Cursors.Hand,
                Location = new Point(62, 88),
                Name = "btnAddSongSlot" + id,
                Size = new Size(136, 25),
                Text = "+ Add Song",
                Tag = id,
                UseVisualStyleBackColor = true
            };
            lane.BtnAddSlot.Click += OnAddSlotClick;
            group.Controls.Add(lane.BtnAddSlot);

            lane.BtnRemoveSlot = new Button
            {
                Cursor = System.Windows.Forms.Cursors.Hand,
                Location = new Point(206, 88),
                Name = "btnRemoveSongSlot" + id,
                Size = new Size(132, 25),
                Text = "- Remove Song",
                Tag = id,
                UseVisualStyleBackColor = true
            };
            lane.BtnRemoveSlot.Click += OnRemoveSlotClick;
            group.Controls.Add(lane.BtnRemoveSlot);

            return lane;
        }

        private void AttachKeyHandlers(TextBox textBox)
        {
            textBox.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
            textBox.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
            textBox.TextChanged += new EventHandler(this.onTextChange);
        }

        private void RefreshTooltips()
        {
            if (this.songToolTip == null)
            {
                return;
            }

            string triggerTip = LocalizationManager.T("tooltip.song_trigger");
            string delayTip = LocalizationManager.T("tooltip.song_delay");
            string stepTip = LocalizationManager.T("tooltip.song_step");
            string daggerTip = LocalizationManager.T("tooltip.song_dagger");
            string instrumentTip = LocalizationManager.T("tooltip.song_instrument");
            string addSlotTip = LocalizationManager.T("tooltip.song_add_slot");
            string removeSlotTip = LocalizationManager.T("tooltip.song_remove_slot");
            string clearTip = LocalizationManager.T("tooltip.song_clear");
            string removeLaneTip = LocalizationManager.T("tooltip.song_remove_lane");

            for (int i = 0; i < this.lanes.Length; i++)
            {
                SongLaneUi lane = this.lanes[i];
                if (lane == null)
                {
                    continue;
                }

                this.songToolTip.SetToolTip(lane.TriggerBox, triggerTip);
                this.songToolTip.SetToolTip(lane.DelayInput, delayTip);
                if (lane.DaggerIconBox != null) this.songToolTip.SetToolTip(lane.DaggerIconBox, daggerTip);
                this.songToolTip.SetToolTip(lane.DaggerBox, daggerTip);
                if (lane.InstrumentIconBox != null) this.songToolTip.SetToolTip(lane.InstrumentIconBox, instrumentTip);
                this.songToolTip.SetToolTip(lane.InstrumentBox, instrumentTip);
                this.songToolTip.SetToolTip(lane.BtnAddSlot, addSlotTip);
                this.songToolTip.SetToolTip(lane.BtnRemoveSlot, removeSlotTip);
                this.songToolTip.SetToolTip(lane.BtnResetLane, clearTip);
                if (lane.BtnRemoveLane != null)
                {
                    this.songToolTip.SetToolTip(lane.BtnRemoveLane, removeLaneTip);
                }

                for (int s = 0; s < MAX_SONG_SLOTS_PER_LANE; s++)
                {
                    this.songToolTip.SetToolTip(lane.SongBoxes[s], stepTip);
                }
            }

            if (this.btnAddSongMacro != null)
            {
                this.songToolTip.SetToolTip(this.btnAddSongMacro, LocalizationManager.T("tooltip.song_add_lane"));
            }
        }

        private void RefreshLaneSlots(SongLaneUi lane)
        {
            if (lane == null)
            {
                return;
            }

            int visible = Math.Max(1, Math.Min(MAX_SONG_SLOTS_PER_LANE, lane.VisibleSlots));
            lane.VisibleSlots = visible;
            bool twoRows = visible > 4;
            int actionY = twoRows ? 114 : 88;
            int targetHeight = twoRows ? LANE_HEIGHT_TWO_ROWS : LANE_HEIGHT_ONE_ROW;

            lane.Group.SuspendLayout();
            try
            {
                for (int s = 0; s < MAX_SONG_SLOTS_PER_LANE; s++)
                {
                    lane.SongBoxes[s].Visible = s < visible;
                    if (s < MAX_SONG_SLOTS_PER_LANE - 1)
                    {
                        lane.ArrowLabels[s].Visible = (s + 1) < visible;
                    }
                }

                lane.BtnAddSlot.Location = new Point(62, actionY);
                lane.BtnAddSlot.Visible = visible < MAX_SONG_SLOTS_PER_LANE;
                lane.BtnAddSlot.Enabled = visible < MAX_SONG_SLOTS_PER_LANE;

                lane.BtnRemoveSlot.Visible = visible > 1;
                lane.BtnRemoveSlot.Enabled = visible > 1;
                lane.BtnRemoveSlot.Location = new Point(visible < MAX_SONG_SLOTS_PER_LANE ? 206 : 62, actionY);

                lane.Group.Size = new Size(LANE_WIDTH, targetHeight);
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
                    SongLaneUi lane = this.lanes[i];
                    if (lane == null)
                    {
                        continue;
                    }

                    RefreshLaneSlots(lane);
                    lane.Group.Visible = lane.IsLaneVisible;
                    if (lane.IsLaneVisible)
                    {
                        lane.Group.Location = new Point(LANE_LEFT, y);
                        y += lane.Group.Height + LANE_SPACING;
                    }
                    else
                    {
                        hasHiddenLane = true;
                    }
                }

                if (this.btnAddSongMacro != null)
                {
                    this.btnAddSongMacro.Location = new Point(LANE_LEFT, y);
                    this.btnAddSongMacro.Visible = hasHiddenLane;
                    this.btnAddSongMacro.Enabled = hasHiddenLane;
                }
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        private SongLaneUi FindLaneById(int id)
        {
            if (id < 1 || id > TOTAL_MACRO_LANES_FOR_SONGS)
            {
                return null;
            }

            return this.lanes[id - 1];
        }

        public void AddSlotToLane(int id)
        {
            SongLaneUi lane = FindLaneById(id);
            if (lane == null || lane.VisibleSlots >= MAX_SONG_SLOTS_PER_LANE)
            {
                return;
            }

            lane.VisibleSlots++;
            LayoutVisibleLanes();
        }

        public void RemoveLastSlotFromLane(int id)
        {
            SongLaneUi lane = FindLaneById(id);
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
                lane.SongBoxes[slotIndex].Text = Key.None.ToString();
            }
            finally
            {
                this.isUpdatingUi = false;
            }

            ChainConfig chainConfig = GetOrCreateChainConfig(id);
            chainConfig.macroEntries.Remove(entryKey);
            if (!ProfileSingleton.IsSaveSuppressed)
            {
                ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().SongMacro);
            }

            lane.VisibleSlots--;
            LayoutVisibleLanes();
        }

        public void ResetLane(int id, bool hideLane = false)
        {
            SongLaneUi lane = FindLaneById(id);
            if (lane == null)
            {
                return;
            }

            Macro songMacro = ProfileSingleton.GetCurrent().SongMacro;
            songMacro.ResetMacro(id);
            if (!ProfileSingleton.IsSaveSuppressed)
            {
                ProfileSingleton.SetConfiguration(songMacro);
            }

            UpdatePanelData(id);
            lane.VisibleSlots = 1;
            if (hideLane && id > 1)
            {
                lane.IsLaneVisible = false;
            }

            LayoutVisibleLanes();
        }

        public bool AddNextSongLane()
        {
            for (int i = 1; i < this.lanes.Length; i++)
            {
                SongLaneUi lane = this.lanes[i];
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
            SongLaneUi lane = FindLaneById(id);
            return lane != null ? lane.VisibleSlots : 0;
        }

        public bool IsLaneVisible(int id)
        {
            SongLaneUi lane = FindLaneById(id);
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

        private void OnResetLaneClick(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag is int)
            {
                ResetLane((int)btn.Tag, false);
            }
        }

        private void OnRemoveLaneClick(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag is int)
            {
                ResetLane((int)btn.Tag, true);
            }
        }

        private void OnAddSongMacroClick(object sender, EventArgs e)
        {
            AddNextSongLane();
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
                    ProfileSingleton.GetCurrent().SongMacro.Start();
                    break;
                case MessageCode.TURN_OFF:
                    ProfileSingleton.GetCurrent().SongMacro.Stop();
                    break;
            }
        }

        private ChainConfig GetOrCreateChainConfig(int id)
        {
            Macro songMacro = ProfileSingleton.GetCurrent().SongMacro;
            ChainConfig chainConfig = songMacro.chainConfigs.Find(cfg => cfg.id == id);
            if (chainConfig == null)
            {
                chainConfig = new ChainConfig(id, Key.None);
                songMacro.chainConfigs.Add(chainConfig);
            }

            return chainConfig;
        }

        private void UpdatePanelData(int id)
        {
            this.isUpdatingUi = true;
            try
            {
                SongLaneUi lane = FindLaneById(id);
                if (lane == null)
                {
                    return;
                }

                Macro songMacro = ProfileSingleton.GetCurrent().SongMacro;
                ChainConfig existing = songMacro.chainConfigs.Find(cfg => cfg.id == id);
                ChainConfig chainConfig = existing != null ? new ChainConfig(existing) : new ChainConfig(id);

                lane.TriggerBox.Text = chainConfig.trigger.ToString();
                lane.DaggerBox.Text = chainConfig.daggerKey.ToString();
                lane.InstrumentBox.Text = chainConfig.instrumentKey.ToString();
                FormUtils.SetNumericValue(lane.DelayInput, chainConfig.delay);

                int highestSlot = 0;
                for (int s = 0; s < MAX_SONG_SLOTS_PER_LANE; s++)
                {
                    int slotNum = s + 1;
                    string entryKey = "in" + slotNum + "mac" + id;
                    MacroKey entry;
                    if (chainConfig.macroEntries != null && chainConfig.macroEntries.TryGetValue(entryKey, out entry) && entry != null)
                    {
                        lane.SongBoxes[s].Text = entry.key.ToString();
                        if (entry.key != Key.None)
                        {
                            highestSlot = slotNum;
                        }
                    }
                    else
                    {
                        lane.SongBoxes[s].Text = Key.None.ToString();
                    }
                }

                bool hasAnyBinding = highestSlot > 0
                    || chainConfig.trigger != Key.None
                    || chainConfig.daggerKey != Key.None
                    || chainConfig.instrumentKey != Key.None;

                lane.IsLaneVisible = (id == 1) || hasAnyBinding;
                lane.VisibleSlots = Math.Max(1, highestSlot);
            }
            catch (Exception ex)
            {
                Logger.Warn("MacroSongForm", "Failed to update panel " + id, ex);
            }
            finally
            {
                this.isUpdatingUi = false;
            }
        }

        private void onTextChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            Macro songMacro = ProfileSingleton.GetCurrent().SongMacro;
            TextBox textBox = sender as TextBox;
            if (textBox == null)
            {
                return;
            }

            Key key;
            if (!KeyHelper.TryParseKey(textBox.Text, out key))
            {
                return;
            }

            if (textBox.Tag != null)
            {
                string[] inputTag = textBox.Tag.ToString().Split(new[] { ":" }, StringSplitOptions.None);
                if (inputTag.Length < 2)
                {
                    return;
                }

                int macroid = short.Parse(inputTag[0]);
                string type = inputTag[1];
                ChainConfig chainConfig = GetOrCreateChainConfig(macroid);

                switch (type)
                {
                    case "Dagger":
                        chainConfig.daggerKey = key;
                        break;
                    case "Instrument":
                        chainConfig.instrumentKey = key;
                        break;
                    case "Trigger":
                        chainConfig.trigger = key;
                        break;
                }
            }
            else
            {
                int macroID = short.Parse(textBox.Name.Split(new[] { "mac" }, StringSplitOptions.None)[1]);
                ChainConfig chainConfig = GetOrCreateChainConfig(macroID);
                chainConfig.macroEntries[textBox.Name] = new MacroKey(key, chainConfig.delay);
            }

            ProfileSingleton.SetConfiguration(songMacro);
        }

        private void onDelayChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            Macro songMacro = ProfileSingleton.GetCurrent().SongMacro;
            NumericUpDown delayInput = sender as NumericUpDown;
            if (delayInput == null)
            {
                return;
            }

            int macroID = short.Parse(delayInput.Name.Split(new[] { "delayMac" }, StringSplitOptions.None)[1]);
            ChainConfig chainConfig = GetOrCreateChainConfig(macroID);
            chainConfig.delay = decimal.ToInt32(delayInput.Value);

            foreach (MacroKey entry in chainConfig.macroEntries.Values)
            {
                if (entry != null)
                {
                    entry.delay = chainConfig.delay;
                }
            }

            ProfileSingleton.SetConfiguration(songMacro);
        }

        private void updateUi()
        {
            for (int i = 1; i <= TOTAL_MACRO_LANES_FOR_SONGS; i++)
            {
                UpdatePanelData(i);
            }

            LayoutVisibleLanes();
        }
    }
}
