using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class AHKForm : Form, IObserver
    {
        private bool isUpdatingUi;
        private Button btnClearAllKeys;
        private ToolTip helpToolTip;
        private GroupBox gbPlayerPriority;
        private NumericUpDown numPlayerYield;
        private Label lblPlayerYieldMs;
        private Label lblPriorityOrder;
        private Font priorityOrderFont;

        public AHKForm(Subject subject)
        {
            InitializeComponent();
            this.helpToolTip = FormUtils.CreateHelpToolTip(this.components);
            this.priorityOrderFont = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            InitializeClearAllButton();
            InitializePriorityControls();
            InitializeCheckAsThreeState();
            RefreshTooltips();
            subject.Attach(this);
        }

        private void InitializePriorityControls()
        {
            // Expand AHK Configuration card neatly to host Key Config, Spammer Delay, and Player Priority side-by-side.
            this.groupAhkConfig.Location = new Point(14, 152);
            this.groupAhkConfig.Size = new Size(556, 122);

            this.ahkCompatibility.Location = new Point(14, 28);
            this.ahkSpeedBoost.Location = new Point(14, 54);

            this.groupBox4.Location = new Point(146, 18);
            this.groupBox4.Size = new Size(128, 68);
            this.chkMouseFlick.Location = new Point(156, 36);
            this.pbUpAndDownArrow.Location = new Point(244, 36);
            this.chkNoShift.Location = new Point(156, 60);
            this.pictureBox1.Location = new Point(226, 59);

            this.groupBox2.Location = new Point(284, 18);
            this.groupBox2.Size = new Size(112, 68);

            this.gbPlayerPriority = new GroupBox
            {
                Location = new Point(406, 18),
                Name = "gbPlayerPriority",
                Size = new Size(138, 68),
                TabStop = false,
                Text = "Player Priority"
            };

            this.numPlayerYield = new NumericUpDown
            {
                Increment = 10,
                Location = new Point(14, 28),
                Maximum = ActionPriorityCoordinator.MAX_PLAYER_INPUT_YIELD_MS,
                Minimum = ActionPriorityCoordinator.MIN_PLAYER_INPUT_YIELD_MS,
                Name = "numPlayerYield",
                Size = new Size(66, 20),
                Value = ActionPriorityCoordinator.DEFAULT_PLAYER_INPUT_YIELD_MS,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.numPlayerYield.ValueChanged += OnPlayerYieldChanged;

            this.lblPlayerYieldMs = new Label
            {
                AutoSize = true,
                Font = new Font("Microsoft Sans Serif", 10F),
                Location = new Point(84, 30),
                Name = "lblPlayerYieldMs",
                Text = "ms"
            };

            this.gbPlayerPriority.Controls.Add(this.numPlayerYield);
            this.gbPlayerPriority.Controls.Add(this.lblPlayerYieldMs);

            this.lblPriorityOrder = new Label
            {
                AutoSize = false,
                Font = this.priorityOrderFont,
                Location = new Point(14, 94),
                Name = "lblPriorityOrder",
                Size = new Size(530, 18),
                Tag = ThemeManager.TAG_ACCENT_TEXT,
                TextAlign = ContentAlignment.MiddleLeft
            };

            this.groupAhkConfig.Controls.Add(this.gbPlayerPriority);
            this.groupAhkConfig.Controls.Add(this.lblPriorityOrder);
        }

        private void OnPlayerYieldChanged(object sender, EventArgs e)
        {
            int yieldMs = decimal.ToInt32(this.numPlayerYield.Value);
            ActionPriorityCoordinator.PlayerInputYieldMs = yieldMs;

            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            Profile current = ProfileSingleton.GetCurrent();
            if (current != null && current.UserPreferences != null)
            {
                current.UserPreferences.playerInputYieldMs = yieldMs;
                ProfileSingleton.SetConfiguration(current.UserPreferences);
            }
        }

        private void RefreshTooltips()
        {
            if (this.lblPriorityOrder != null)
            {
                this.lblPriorityOrder.Text = LocalizationManager.T("mount.priority_order");
            }

            if (this.helpToolTip == null)
            {
                return;
            }

            string keyTip = LocalizationManager.T("tooltip.ahk_key");
            foreach (Control c in this.Controls)
            {
                CheckBox chk = c as CheckBox;
                if (chk != null && chk.Name.StartsWith("chk", StringComparison.Ordinal))
                {
                    this.helpToolTip.SetToolTip(chk, keyTip);
                }
            }

            this.helpToolTip.SetToolTip(this.txtSpammerDelay, LocalizationManager.T("tooltip.ahk_delay"));
            this.helpToolTip.SetToolTip(this.ahkCompatibility, LocalizationManager.T("tooltip.ahk_compat"));
            this.helpToolTip.SetToolTip(this.ahkSpeedBoost, LocalizationManager.T("tooltip.ahk_speed"));
            this.helpToolTip.SetToolTip(this.chkNoShift, LocalizationManager.T("tooltip.ahk_noshift"));
            this.helpToolTip.SetToolTip(this.chkMouseFlick, LocalizationManager.T("tooltip.ahk_flick"));
            if (this.btnClearAllKeys != null)
            {
                this.helpToolTip.SetToolTip(this.btnClearAllKeys, LocalizationManager.T("tooltip.ahk_clear"));
            }
            if (this.gbPlayerPriority != null)
            {
                string yieldTip = LocalizationManager.T("tooltip.priority_player_yield");
                this.helpToolTip.SetToolTip(this.gbPlayerPriority, yieldTip);
                this.helpToolTip.SetToolTip(this.numPlayerYield, yieldTip);
                this.helpToolTip.SetToolTip(this.lblPlayerYieldMs, yieldTip);
            }
            if (this.lblPriorityOrder != null)
            {
                this.helpToolTip.SetToolTip(this.lblPriorityOrder, LocalizationManager.T("tooltip.priority_order"));
            }
            this.helpToolTip.SetToolTip(this.cbWithClick, keyTip);
            this.helpToolTip.SetToolTip(this.cbWithNoClick, keyTip);
            this.helpToolTip.SetToolTip(this.cbDeactivated, keyTip);
        }

        private void InitializeClearAllButton()
        {
            this.btnClearAllKeys = new Button
            {
                Location = new Point(426, 122),
                Name = "btnClearAllKeys",
                Size = new Size(122, 26),
                Text = "Clear All Keys",
                UseVisualStyleBackColor = true
            };
            this.btnClearAllKeys.Click += btnClearAllKeys_Click;
            this.Controls.Add(this.btnClearAllKeys);
        }

        private void btnClearAllKeys_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                    LocalizationManager.T("msg.ahk_clear_confirm"),
                    LocalizationManager.T("title.ahk_clear"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            this.isUpdatingUi = true;
            try
            {
                foreach (Control c in this.Controls)
                {
                    CheckBox check = c as CheckBox;
                    if (check != null && check.Name.StartsWith("chk", StringComparison.Ordinal))
                    {
                        check.CheckState = CheckState.Unchecked;
                    }
                }

                ProfileSingleton.GetCurrent().AHK.ClearAHKEntries();
            }
            finally
            {
                this.isUpdatingUi = false;
            }

            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AHK);
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
                    this.isUpdatingUi = true;
                    try
                    {
                        RemoveHandlers();
                        FormUtils.ResetForm(this);
                        SetLegendDefaultValues();
                        InitializeCheckAsThreeState();

                        AHK ahk = ProfileSingleton.GetCurrent().AHK;
                        RadioButton rdAhkMode = this.groupAhkConfig.Controls[ahk.ahkMode] as RadioButton;
                        if (rdAhkMode != null)
                        {
                            rdAhkMode.Checked = true;
                        }

                        FormUtils.SetNumericValue(this.txtSpammerDelay, ahk.AhkDelay);
                        this.chkNoShift.Checked = ahk.noShift;
                        this.chkMouseFlick.Checked = ahk.mouseFlick;
                        DisableControlsIfSpeedBoost();

                        Profile currentProfile = ProfileSingleton.GetCurrent();
                        int yieldMs = currentProfile != null && currentProfile.UserPreferences != null
                            ? currentProfile.UserPreferences.playerInputYieldMs
                            : ActionPriorityCoordinator.DEFAULT_PLAYER_INPUT_YIELD_MS;
                        ActionPriorityCoordinator.PlayerInputYieldMs = yieldMs;
                        if (this.numPlayerYield != null)
                        {
                            FormUtils.SetNumericValue(this.numPlayerYield, ActionPriorityCoordinator.PlayerInputYieldMs);
                        }

                        Dictionary<string, KeyConfig> ahkClones = ahk.GetEntriesSnapshot();
                        foreach (KeyValuePair<string, KeyConfig> config in ahkClones)
                        {
                            ToggleCheckboxByName(config.Key, config.Value.ClickActive);
                        }
                    }
                    finally
                    {
                        this.isUpdatingUi = false;
                    }
                    break;
                case MessageCode.TURN_ON:
                    ProfileSingleton.GetCurrent().AHK.Start();
                    break;
                case MessageCode.TURN_OFF:
                    ProfileSingleton.GetCurrent().AHK.Stop();
                    break;
                case MessageCode.LANGUAGE_CHANGED:
                    RefreshTooltips();
                    break;
            }
        }

        private void onCheckChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            CheckBox checkbox = sender as CheckBox;
            if (checkbox == null)
            {
                return;
            }

            Key key;
            if (!KeyHelper.TryParseKey(checkbox.Text, out key))
            {
                return;
            }

            bool haveMouseClick = checkbox.CheckState == CheckState.Checked;

            if (checkbox.CheckState == CheckState.Checked || checkbox.CheckState == CheckState.Indeterminate)
            {
                ProfileSingleton.GetCurrent().AHK.AddAHKEntry(checkbox.Name, new KeyConfig(key, haveMouseClick));
            }
            else
            {
                ProfileSingleton.GetCurrent().AHK.RemoveAHKEntry(checkbox.Name);
            }

            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AHK);
        }

        private void txtSpammerDelay_TextChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            try
            {
                ProfileSingleton.GetCurrent().AHK.AhkDelay = Math.Max(1, Convert.ToInt32(this.txtSpammerDelay.Value));
                ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AHK);
            }
            catch (Exception ex)
            {
                Logger.Warn("AHKForm", "Invalid spammer delay value", ex);
            }
        }

        private void ToggleCheckboxByName(string name, bool state)
        {
            Control[] matches = this.Controls.Find(name, true);
            if (matches.Length > 0)
            {
                CheckBox checkBox = matches[0] as CheckBox;
                if (checkBox != null)
                {
                    checkBox.CheckState = state ? CheckState.Checked : CheckState.Indeterminate;
                }
            }
        }

        private void RemoveHandlers()
        {
            foreach (Control c in this.Controls)
            {
                CheckBox check = c as CheckBox;
                if (check != null)
                {
                    check.CheckStateChanged -= onCheckChange;
                }
            }
        }

        private void InitializeCheckAsThreeState()
        {
            foreach (Control c in this.Controls)
            {
                CheckBox check = c as CheckBox;
                if (check != null)
                {
                    if (check.Name.StartsWith("chk", StringComparison.Ordinal))
                    {
                        check.ThreeState = true;
                    }

                    if (check.Enabled)
                    {
                        check.CheckStateChanged -= onCheckChange;
                        check.CheckStateChanged += onCheckChange;
                    }
                }
            }
        }

        private void SetLegendDefaultValues()
        {
            this.cbWithNoClick.ThreeState = true;
            this.cbWithNoClick.CheckState = CheckState.Indeterminate;
            this.cbWithNoClick.AutoCheck = false;
            this.cbWithClick.CheckState = CheckState.Checked;
            this.cbWithClick.ThreeState = true;
            this.cbWithClick.AutoCheck = false;
        }

        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            RadioButton rb = sender as RadioButton;
            if (rb != null && rb.Checked)
            {
                ProfileSingleton.GetCurrent().AHK.ahkMode = rb.Name;
                ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AHK);
                DisableControlsIfSpeedBoost();
            }
        }

        private void chkMouseFlick_CheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            CheckBox chk = sender as CheckBox;
            if (chk != null)
            {
                ProfileSingleton.GetCurrent().AHK.mouseFlick = chk.Checked;
                ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AHK);
            }
        }

        private void chkNoShift_CheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            CheckBox chk = sender as CheckBox;
            if (chk != null)
            {
                ProfileSingleton.GetCurrent().AHK.noShift = chk.Checked;
                ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AHK);
            }
        }

        private void DisableControlsIfSpeedBoost()
        {
            bool isSpeedBoost = ProfileSingleton.GetCurrent().AHK.ahkMode == AHK.SPEED_BOOST;
            this.chkMouseFlick.Enabled = !isSpeedBoost;
            this.chkNoShift.Enabled = !isSpeedBoost;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (this.priorityOrderFont != null)
            {
                this.priorityOrderFont.Dispose();
                this.priorityOrderFont = null;
            }

            base.OnFormClosed(e);
        }
    }
}
