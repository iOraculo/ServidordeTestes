using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public class MountForm : Form, IObserver
    {
        private readonly ToolTip helpToolTip;
        private readonly TextBox[] extraSkillBoxes = new TextBox[AutoMount.EXTRA_SKILL_SLOTS];

        private AutoMount autoMount;
        private bool isUpdatingUi;

        private GroupBox gbMountMain;
        private CheckBox cbMountEnabled;
        private Label lblMountKey;
        private TextBox txtMountKey;
        private Label lblDismountDelay;
        private NumericUpDown numDismountDelay;
        private CheckBox cbDismountOnSkill;
        private CheckBox cbPauseAutobuffMounted;
        private CheckBox cbAutoRemount;
        private Label lblRemountDelay;
        private NumericUpDown numRemountDelay;

        private GroupBox gbMountSkills;
        private CheckBox cbIncludeOtherTabs;
        private Label lblExtraSkills;
        private Button btnClearExtraSkills;

        public MountForm(Subject subject)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Name = "MountForm";
            this.Text = "Mount";
            this.Size = new Size(584, 281);
            this.helpToolTip = FormUtils.CreateHelpToolTip();

            BuildUi();
            RefreshTooltipsAndDynamicText();

            if (subject != null)
            {
                subject.Attach(this);
            }
        }

        private void BuildUi()
        {
            this.SuspendLayout();
            try
            {
                // Card 1: Mount (Halter Lead)
                this.gbMountMain = new GroupBox
                {
                    Location = new Point(8, 4),
                    Name = "gbMountMain",
                    Size = new Size(568, 108),
                    TabStop = false,
                    Text = "Mount (Halter Lead)"
                };

                this.cbMountEnabled = new CheckBox
                {
                    AutoSize = true,
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(12, 23),
                    Name = "cbMountEnabled",
                    Text = "Enable Mount",
                    UseVisualStyleBackColor = true
                };
                this.cbMountEnabled.CheckedChanged += OnMountSettingChanged;

                this.lblMountKey = new Label
                {
                    AutoSize = false,
                    Location = new Point(146, 23),
                    Name = "lblMountKey",
                    Size = new Size(126, 18),
                    Text = "Mount Key:",
                    TextAlign = ContentAlignment.MiddleRight
                };

                this.txtMountKey = new TextBox
                {
                    Location = new Point(276, 21),
                    Name = "txtMountKey",
                    Size = new Size(68, 20),
                    Text = "None",
                    TextAlign = HorizontalAlignment.Center
                };
                this.txtMountKey.KeyDown += FormUtils.OnKeyDown;
                this.txtMountKey.KeyPress += FormUtils.OnKeyPress;
                this.txtMountKey.TextChanged += OnMountKeyChanged;

                this.lblDismountDelay = new Label
                {
                    AutoSize = false,
                    Location = new Point(350, 23),
                    Name = "lblDismountDelay",
                    Size = new Size(136, 18),
                    Text = "Dismount delay(ms):",
                    TextAlign = ContentAlignment.MiddleRight
                };

                this.numDismountDelay = new NumericUpDown
                {
                    Increment = 10,
                    Location = new Point(490, 21),
                    Maximum = 500,
                    Minimum = 20,
                    Name = "numDismountDelay",
                    Size = new Size(64, 20),
                    Value = 80
                };
                this.numDismountDelay.ValueChanged += OnMountSettingChanged;

                this.cbDismountOnSkill = new CheckBox
                {
                    AutoSize = true,
                    Checked = true,
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(12, 52),
                    Name = "cbDismountOnSkill",
                    Text = "Dismount when using skill",
                    UseVisualStyleBackColor = true
                };
                this.cbDismountOnSkill.CheckedChanged += OnMountSettingChanged;

                this.cbPauseAutobuffMounted = new CheckBox
                {
                    AutoSize = true,
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(302, 52),
                    Name = "cbPauseAutobuffMounted",
                    Text = "Pause Auto-skill while mounted",
                    UseVisualStyleBackColor = true
                };
                this.cbPauseAutobuffMounted.CheckedChanged += OnMountSettingChanged;

                this.cbAutoRemount = new CheckBox
                {
                    AutoSize = true,
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(12, 79),
                    Name = "cbAutoRemount",
                    Text = "Auto remount after idle",
                    UseVisualStyleBackColor = true
                };
                this.cbAutoRemount.CheckedChanged += OnMountSettingChanged;

                this.lblRemountDelay = new Label
                {
                    AutoSize = false,
                    Location = new Point(218, 79),
                    Name = "lblRemountDelay",
                    Size = new Size(80, 18),
                    Text = "After(ms):",
                    TextAlign = ContentAlignment.MiddleRight
                };

                this.numRemountDelay = new NumericUpDown
                {
                    Increment = 100,
                    Location = new Point(302, 78),
                    Maximum = AutoMount.MAX_REMOUNT_DELAY_MS,
                    Minimum = AutoMount.MIN_REMOUNT_DELAY_MS,
                    Name = "numRemountDelay",
                    Size = new Size(74, 20),
                    Value = AutoMount.DEFAULT_REMOUNT_DELAY_MS
                };
                this.numRemountDelay.ValueChanged += OnMountSettingChanged;

                this.gbMountMain.Controls.Add(this.cbMountEnabled);
                this.gbMountMain.Controls.Add(this.lblMountKey);
                this.gbMountMain.Controls.Add(this.txtMountKey);
                this.gbMountMain.Controls.Add(this.lblDismountDelay);
                this.gbMountMain.Controls.Add(this.numDismountDelay);
                this.gbMountMain.Controls.Add(this.cbDismountOnSkill);
                this.gbMountMain.Controls.Add(this.cbPauseAutobuffMounted);
                this.gbMountMain.Controls.Add(this.cbAutoRemount);
                this.gbMountMain.Controls.Add(this.lblRemountDelay);
                this.gbMountMain.Controls.Add(this.numRemountDelay);

                // Card 2: Skill Keys for Auto Dismount
                this.gbMountSkills = new GroupBox
                {
                    Location = new Point(8, 118),
                    Name = "gbMountSkills",
                    Size = new Size(568, 86),
                    TabStop = false,
                    Text = "Skill Keys (Auto Dismount)"
                };

                this.cbIncludeOtherTabs = new CheckBox
                {
                    AutoSize = true,
                    Checked = true,
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(12, 21),
                    Name = "cbIncludeOtherTabs",
                    Text = "Include keys from Spammer, Auto-skill, Skill Timers and Macros",
                    UseVisualStyleBackColor = true
                };
                this.cbIncludeOtherTabs.CheckedChanged += OnMountSettingChanged;

                this.lblExtraSkills = new Label
                {
                    AutoSize = false,
                    Location = new Point(12, 51),
                    Name = "lblExtraSkills",
                    Size = new Size(92, 18),
                    Text = "Extra skill keys:",
                    TextAlign = ContentAlignment.MiddleLeft
                };

                this.gbMountSkills.Controls.Add(this.cbIncludeOtherTabs);
                this.gbMountSkills.Controls.Add(this.lblExtraSkills);

                for (int i = 0; i < AutoMount.EXTRA_SKILL_SLOTS; i++)
                {
                    int slotIndex = i;
                    TextBox tb = new TextBox
                    {
                        Location = new Point(108 + (i * 62), 50),
                        Name = "txtExtraSkill" + (i + 1),
                        Size = new Size(56, 20),
                        Text = "None",
                        TextAlign = HorizontalAlignment.Center
                    };
                    tb.KeyDown += FormUtils.OnKeyDown;
                    tb.KeyPress += FormUtils.OnKeyPress;
                    tb.TextChanged += (s, e) => OnExtraSkillKeyChanged(slotIndex, tb.Text);

                    this.extraSkillBoxes[i] = tb;
                    this.gbMountSkills.Controls.Add(tb);
                }

                this.btnClearExtraSkills = new Button
                {
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(482, 48),
                    Name = "btnClearExtraSkills",
                    Size = new Size(76, 25),
                    Text = "Clear",
                    UseVisualStyleBackColor = true
                };
                this.btnClearExtraSkills.Click += OnClearExtraSkillsClick;
                this.gbMountSkills.Controls.Add(this.btnClearExtraSkills);

                this.Controls.Add(this.gbMountMain);
                this.Controls.Add(this.gbMountSkills);
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        private void RefreshTooltipsAndDynamicText()
        {
            if (this.helpToolTip == null)
            {
                return;
            }

            this.helpToolTip.SetToolTip(this.cbMountEnabled, LocalizationManager.T("tooltip.mount_enabled"));
            this.helpToolTip.SetToolTip(this.lblMountKey, LocalizationManager.T("tooltip.mount_key"));
            this.helpToolTip.SetToolTip(this.txtMountKey, LocalizationManager.T("tooltip.mount_key"));
            this.helpToolTip.SetToolTip(this.lblDismountDelay, LocalizationManager.T("tooltip.mount_dismount_delay"));
            this.helpToolTip.SetToolTip(this.numDismountDelay, LocalizationManager.T("tooltip.mount_dismount_delay"));
            this.helpToolTip.SetToolTip(this.cbDismountOnSkill, LocalizationManager.T("tooltip.mount_dismount_on_skill"));
            this.helpToolTip.SetToolTip(this.cbPauseAutobuffMounted, LocalizationManager.T("tooltip.mount_pause_autobuff"));
            this.helpToolTip.SetToolTip(this.cbAutoRemount, LocalizationManager.T("tooltip.mount_auto_remount"));
            this.helpToolTip.SetToolTip(this.lblRemountDelay, LocalizationManager.T("tooltip.mount_remount_delay"));
            this.helpToolTip.SetToolTip(this.numRemountDelay, LocalizationManager.T("tooltip.mount_remount_delay"));
            this.helpToolTip.SetToolTip(this.cbIncludeOtherTabs, LocalizationManager.T("tooltip.mount_include_tabs"));
            this.helpToolTip.SetToolTip(this.btnClearExtraSkills, LocalizationManager.T("tooltip.mount_clear_extra"));

            string extraTip = LocalizationManager.T("tooltip.mount_extra_skill");
            for (int i = 0; i < this.extraSkillBoxes.Length; i++)
            {
                if (this.extraSkillBoxes[i] != null)
                {
                    this.helpToolTip.SetToolTip(this.extraSkillBoxes[i], extraTip);
                }
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
                    LoadFromCurrentProfile();
                    break;
                case MessageCode.LANGUAGE_CHANGED:
                    RefreshTooltipsAndDynamicText();
                    break;
                case MessageCode.TURN_ON:
                    if (this.autoMount != null)
                    {
                        this.autoMount.Start();
                    }
                    break;
                case MessageCode.TURN_OFF:
                    if (this.autoMount != null)
                    {
                        this.autoMount.Stop();
                    }
                    break;
            }
        }

        private void LoadFromCurrentProfile()
        {
            Profile current = ProfileSingleton.GetCurrent();
            if (current == null)
            {
                return;
            }

            if (current.AutoMount == null)
            {
                current.AutoMount = new AutoMount();
            }

            this.autoMount = current.AutoMount;
            this.autoMount.EnsureExtraSkillSlotCount();

            this.isUpdatingUi = true;
            try
            {
                this.cbMountEnabled.Checked = this.autoMount.Enabled;
                this.txtMountKey.Text = this.autoMount.MountKey.ToString();
                FormUtils.SetNumericValue(this.numDismountDelay, this.autoMount.DismountDelayMs);
                this.cbDismountOnSkill.Checked = this.autoMount.DismountOnSkill;
                this.cbPauseAutobuffMounted.Checked = this.autoMount.PauseAutobuffWhileMounted;
                this.cbAutoRemount.Checked = this.autoMount.AutoRemountAfterIdle;
                FormUtils.SetNumericValue(this.numRemountDelay, this.autoMount.GetEffectiveRemountDelayMs());
                this.cbIncludeOtherTabs.Checked = this.autoMount.IncludeOtherTabsSkillKeys;

                for (int i = 0; i < this.extraSkillBoxes.Length; i++)
                {
                    this.extraSkillBoxes[i].Text = this.autoMount.GetExtraSkillKey(i).ToString();
                }

                UpdateRemountControlState();
            }
            finally
            {
                this.isUpdatingUi = false;
            }
        }

        private void UpdateRemountControlState()
        {
            this.numRemountDelay.Enabled = this.cbAutoRemount.Checked;
        }

        private void OnMountSettingChanged(object sender, EventArgs e)
        {
            UpdateRemountControlState();

            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed || this.autoMount == null)
            {
                return;
            }

            this.autoMount.Enabled = this.cbMountEnabled.Checked;
            this.autoMount.DismountOnSkill = this.cbDismountOnSkill.Checked;
            this.autoMount.DismountDelayMs = decimal.ToInt32(this.numDismountDelay.Value);
            this.autoMount.PauseAutobuffWhileMounted = this.cbPauseAutobuffMounted.Checked;
            this.autoMount.AutoRemountAfterIdle = this.cbAutoRemount.Checked;
            this.autoMount.RemountDelayMs = decimal.ToInt32(this.numRemountDelay.Value);
            this.autoMount.IncludeOtherTabsSkillKeys = this.cbIncludeOtherTabs.Checked;

            ProfileSingleton.SetConfiguration(this.autoMount);
        }

        private void OnMountKeyChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed || this.autoMount == null)
            {
                return;
            }

            Key parsed;
            if (KeyHelper.TryParseKey(this.txtMountKey.Text, out parsed))
            {
                this.autoMount.MountKey = FormUtils.IsValidKey(parsed) ? parsed : Key.None;
                ProfileSingleton.SetConfiguration(this.autoMount);
            }
        }

        private void OnExtraSkillKeyChanged(int slotIndex, string keyText)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed || this.autoMount == null)
            {
                return;
            }

            Key parsed;
            if (KeyHelper.TryParseKey(keyText, out parsed))
            {
                this.autoMount.SetExtraSkillKey(slotIndex, parsed);
                ProfileSingleton.SetConfiguration(this.autoMount);
            }
        }

        private void OnClearExtraSkillsClick(object sender, EventArgs e)
        {
            if (this.autoMount == null)
            {
                return;
            }

            this.isUpdatingUi = true;
            try
            {
                this.autoMount.ClearExtraSkillKeys();
                for (int i = 0; i < this.extraSkillBoxes.Length; i++)
                {
                    this.extraSkillBoxes[i].Text = Key.None.ToString();
                }
            }
            finally
            {
                this.isUpdatingUi = false;
            }

            ProfileSingleton.SetConfiguration(this.autoMount);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.helpToolTip != null)
                {
                    this.helpToolTip.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
