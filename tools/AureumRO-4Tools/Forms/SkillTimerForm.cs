using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class SkillTimerForm : Form, IObserver
    {
        public const int TOTAL_TIMER_SLOTS = 6;

        private const int CARD_WIDTH = 186;
        private const int CARD_HEIGHT = 126;
        private const int COLS_PER_ROW = 3;
        private const int GRID_START_X = 12;
        private const int GRID_START_Y = 10;
        private const int COL_STEP_X = 196;
        private const int ROW_STEP_Y = 136;

        private sealed class TimerSlotUi
        {
            public int SlotNumber;
            public GroupBox Group;
            public TextBox KeyBox;
            public NumericUpDown DelayBox;
            public Button BtnClear;
            public Button BtnRemove;
            public bool IsVisible;
        }

        private readonly TimerSlotUi[] slots = new TimerSlotUi[TOTAL_TIMER_SLOTS];
        private Button btnAddTimer;
        private ToolTip timerToolTip;
        private Font labelFont;
        private Font inputFont;
        private bool isUpdatingUi;

        public SkillTimerForm(Subject subject)
        {
            InitializeComponent();
            BuildSimplifiedUi();
            subject.Attach(this);
            SyncFromProfile();
        }

        private void BuildSimplifiedUi()
        {
            this.SuspendLayout();
            try
            {
                this.Controls.Clear();
                this.AutoScroll = true;
                this.timerToolTip = FormUtils.CreateHelpToolTip(this.components);
                this.labelFont = new Font("Microsoft Sans Serif", 8.5F);
                this.inputFont = new Font("Microsoft Sans Serif", 10F);

                for (int i = 0; i < TOTAL_TIMER_SLOTS; i++)
                {
                    int slotNum = i + 1;
                    TimerSlotUi slot = CreateTimerSlotUi(slotNum);
                    slot.IsVisible = (slotNum == 1);
                    this.slots[i] = slot;
                    this.Controls.Add(slot.Group);
                }

                this.btnAddTimer = new Button
                {
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Name = "btnAddSkillTimer",
                    Size = new Size(156, 25),
                    Text = "+ Add Timer",
                    UseVisualStyleBackColor = true
                };
                this.btnAddTimer.Click += (s, e) => AddNextTimer();
                this.Controls.Add(this.btnAddTimer);

                RefreshTooltips();
                LayoutVisibleTimers();
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        private void RefreshTooltips()
        {
            if (this.timerToolTip == null)
            {
                return;
            }

            string delayTip = LocalizationManager.T("tooltip.timer_delay");
            string keyTip = LocalizationManager.T("tooltip.timer_key");
            string clearTip = LocalizationManager.T("tooltip.timer_clear");
            string removeTip = LocalizationManager.T("tooltip.timer_remove");

            for (int i = 0; i < this.slots.Length; i++)
            {
                TimerSlotUi slot = this.slots[i];
                if (slot == null)
                {
                    continue;
                }

                this.timerToolTip.SetToolTip(slot.DelayBox, delayTip);
                this.timerToolTip.SetToolTip(slot.KeyBox, keyTip);
                this.timerToolTip.SetToolTip(slot.BtnClear, clearTip);
                if (slot.BtnRemove != null)
                {
                    this.timerToolTip.SetToolTip(slot.BtnRemove, removeTip);
                }
            }

            if (this.btnAddTimer != null)
            {
                this.timerToolTip.SetToolTip(this.btnAddTimer, LocalizationManager.T("tooltip.timer_add"));
            }
        }

        private TimerSlotUi CreateTimerSlotUi(int slotNumber)
        {
            TimerSlotUi slot = new TimerSlotUi { SlotNumber = slotNumber };

            GroupBox group = new GroupBox
            {
                Name = "groupBox" + slotNumber,
                Size = new Size(CARD_WIDTH, CARD_HEIGHT),
                TabStop = false,
                Text = "Skill timer " + slotNumber
            };
            slot.Group = group;

            Label lblDelay = new Label
            {
                AutoSize = false,
                Font = this.labelFont,
                Location = new Point(4, 26),
                Name = "lblAutoRefreshDelay" + slotNumber,
                Size = new Size(52, 19),
                Text = "Delay",
                TextAlign = ContentAlignment.MiddleRight
            };

            NumericUpDown delayBox = new NumericUpDown
            {
                Font = this.inputFont,
                Location = new Point(60, 24),
                Maximum = new decimal(new int[] { 10000, 0, 0, 0 }),
                Minimum = 0,
                Name = slotNumber == 1 ? "txtAutoRefreshDelay" : "txtAutoRefreshDelay" + slotNumber,
                Size = new Size(58, 23),
                Value = 5
            };
            delayBox.ValueChanged += (s, e) => OnTimerDelayChanged(delayBox, GetSpammer(slotNumber));

            Label lblSec = new Label
            {
                AutoSize = true,
                Font = this.labelFont,
                Location = new Point(122, 28),
                Name = "lblAutoRefreshSec" + slotNumber,
                Text = "sec"
            };

            Label lblKey = new Label
            {
                AutoSize = false,
                Font = this.labelFont,
                Location = new Point(4, 56),
                Name = "lblAutoRefreshKey" + slotNumber,
                Size = new Size(52, 19),
                Text = "Key",
                TextAlign = ContentAlignment.MiddleRight
            };

            TextBox keyBox = new TextBox
            {
                Font = this.inputFont,
                Location = new Point(60, 54),
                Name = slotNumber == 1 ? "txtSkillTimerKey" : "txtSkillTimerKey" + slotNumber,
                Size = new Size(58, 23),
                Text = Key.None.ToString()
            };
            keyBox.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
            keyBox.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
            keyBox.TextChanged += (s, e) => OnTimerKeyChanged(keyBox, GetSpammer(slotNumber));

            Button btnClear = new Button
            {
                Cursor = System.Windows.Forms.Cursors.Hand,
                Location = new Point(10, 89),
                Name = "btnClearTimer" + slotNumber,
                Size = new Size(slotNumber == 1 ? 166 : 66, 25),
                Text = "Clear",
                UseVisualStyleBackColor = true
            };
            btnClear.Click += (s, e) => ClearTimer(slotNumber, false);

            slot.DelayBox = delayBox;
            slot.KeyBox = keyBox;
            slot.BtnClear = btnClear;

            group.Controls.Add(lblDelay);
            group.Controls.Add(delayBox);
            group.Controls.Add(lblSec);
            group.Controls.Add(lblKey);
            group.Controls.Add(keyBox);
            group.Controls.Add(btnClear);

            if (slotNumber > 1)
            {
                Button btnRemove = new Button
                {
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(82, 89),
                    Name = "btnRemoveTimer" + slotNumber,
                    Size = new Size(94, 25),
                    Text = "Remove Timer",
                    UseVisualStyleBackColor = true
                };
                btnRemove.Click += (s, e) => ClearTimer(slotNumber, true);
                slot.BtnRemove = btnRemove;
                group.Controls.Add(btnRemove);
            }

            return slot;
        }

        private void LayoutVisibleTimers()
        {
            this.SuspendLayout();
            try
            {
                int visibleIdx = 0;
                bool hasHidden = false;

                for (int i = 0; i < this.slots.Length; i++)
                {
                    TimerSlotUi slot = this.slots[i];
                    if (slot == null)
                    {
                        continue;
                    }

                    slot.Group.Visible = slot.IsVisible;
                    if (slot.IsVisible)
                    {
                        int row = visibleIdx / COLS_PER_ROW;
                        int col = visibleIdx % COLS_PER_ROW;
                        slot.Group.Location = new Point(GRID_START_X + (col * COL_STEP_X), GRID_START_Y + (row * ROW_STEP_Y));
                        visibleIdx++;
                    }
                    else
                    {
                        hasHidden = true;
                    }
                }

                if (this.btnAddTimer != null)
                {
                    int addRow = visibleIdx / COLS_PER_ROW;
                    int addCol = visibleIdx % COLS_PER_ROW;
                    int btnX = GRID_START_X + (addCol * COL_STEP_X);
                    int btnY = GRID_START_Y + (addRow * ROW_STEP_Y) + 46;
                    this.btnAddTimer.Location = new Point(btnX, btnY);
                    this.btnAddTimer.Visible = hasHidden;
                    this.btnAddTimer.Enabled = hasHidden;
                }
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        private static AutoRefreshSpammer GetSpammer(int slotNumber)
        {
            Profile profile = ProfileSingleton.GetCurrent();
            switch (slotNumber)
            {
                case 1: return profile.AutoRefreshSpammer1;
                case 2: return profile.AutoRefreshSpammer2;
                case 3: return profile.AutoRefreshSpammer3;
                case 4: return profile.AutoRefreshSpammer4;
                case 5: return profile.AutoRefreshSpammer5;
                case 6: return profile.AutoRefreshSpammer6;
                default: return null;
            }
        }

        public bool AddNextTimer()
        {
            for (int i = 1; i < this.slots.Length; i++)
            {
                TimerSlotUi slot = this.slots[i];
                if (slot != null && !slot.IsVisible)
                {
                    slot.IsVisible = true;
                    LayoutVisibleTimers();
                    return true;
                }
            }

            return false;
        }

        public void ClearTimer(int slotNumber, bool hideSlot = false)
        {
            if (slotNumber < 1 || slotNumber > TOTAL_TIMER_SLOTS)
            {
                return;
            }

            TimerSlotUi slot = this.slots[slotNumber - 1];
            AutoRefreshSpammer spammer = GetSpammer(slotNumber);

            this.isUpdatingUi = true;
            try
            {
                slot.KeyBox.Text = Key.None.ToString();
                FormUtils.SetNumericValue(slot.DelayBox, 5);
            }
            finally
            {
                this.isUpdatingUi = false;
            }

            if (spammer != null)
            {
                spammer.RefreshKey = Key.None;
                spammer.RefreshDelay = 5;
                if (!ProfileSingleton.IsSaveSuppressed)
                {
                    ProfileSingleton.SetConfiguration(spammer);
                }
            }

            if (hideSlot && slotNumber > 1)
            {
                slot.IsVisible = false;
                LayoutVisibleTimers();
            }
        }

        public bool IsTimerVisible(int slotNumber)
        {
            if (slotNumber < 1 || slotNumber > TOTAL_TIMER_SLOTS)
            {
                return false;
            }

            return this.slots[slotNumber - 1] != null && this.slots[slotNumber - 1].IsVisible;
        }

        private void SyncFromProfile()
        {
            this.isUpdatingUi = true;
            try
            {
                for (int i = 0; i < TOTAL_TIMER_SLOTS; i++)
                {
                    int slotNum = i + 1;
                    TimerSlotUi slot = this.slots[i];
                    AutoRefreshSpammer spammer = GetSpammer(slotNum);
                    if (slot == null || spammer == null)
                    {
                        continue;
                    }

                    slot.KeyBox.Text = spammer.RefreshKey.ToString();
                    FormUtils.SetNumericValue(slot.DelayBox, spammer.RefreshDelay);
                    slot.IsVisible = (slotNum == 1) || (spammer.RefreshKey != Key.None);
                }

                LayoutVisibleTimers();
            }
            finally
            {
                this.isUpdatingUi = false;
            }
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
                    SyncFromProfile();
                    break;
                case MessageCode.TURN_ON:
                    Profile currentOn = ProfileSingleton.GetCurrent();
                    currentOn.AutoRefreshSpammer1.Start();
                    currentOn.AutoRefreshSpammer2.Start();
                    currentOn.AutoRefreshSpammer3.Start();
                    currentOn.AutoRefreshSpammer4.Start();
                    currentOn.AutoRefreshSpammer5.Start();
                    currentOn.AutoRefreshSpammer6.Start();
                    break;
                case MessageCode.TURN_OFF:
                    Profile currentOff = ProfileSingleton.GetCurrent();
                    currentOff.AutoRefreshSpammer1.Stop();
                    currentOff.AutoRefreshSpammer2.Stop();
                    currentOff.AutoRefreshSpammer3.Stop();
                    currentOff.AutoRefreshSpammer4.Stop();
                    currentOff.AutoRefreshSpammer5.Stop();
                    currentOff.AutoRefreshSpammer6.Stop();
                    break;
                case MessageCode.LANGUAGE_CHANGED:
                    RefreshTooltips();
                    break;
            }
        }

        private void OnTimerKeyChanged(TextBox keyBox, AutoRefreshSpammer spammer)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed || spammer == null)
            {
                return;
            }

            Key key;
            if (KeyHelper.TryParseKey(keyBox.Text, out key))
            {
                spammer.RefreshKey = key;
                ProfileSingleton.SetConfiguration(spammer);
            }
        }

        private void OnTimerDelayChanged(NumericUpDown delayBox, AutoRefreshSpammer spammer)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed || spammer == null)
            {
                return;
            }

            spammer.RefreshDelay = decimal.ToInt32(delayBox.Value);
            ProfileSingleton.SetConfiguration(spammer);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (this.labelFont != null)
            {
                this.labelFont.Dispose();
                this.labelFont = null;
            }
            if (this.inputFont != null)
            {
                this.inputFont.Dispose();
                this.inputFont = null;
            }
            base.OnFormClosed(e);
        }
    }
}
