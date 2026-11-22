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
        private Label lblPlayerYieldTitle;
        private NumericUpDown numPlayerYield;
        private Label lblPlayerYieldMs;
        private Label lblHoldPause;
        private TextBox txtHoldPauseKey;
        private CheckBox chkHoldPauseAutopot;
        private Label lblPriorityOrder;
        private Font priorityOrderFont;

        public AHKForm(Subject subject)
        {
            InitializeComponent();
            this.helpToolTip = FormUtils.CreateHelpToolTip(this.components);
            this.priorityOrderFont = new Font("Segoe UI", 7.75F, FontStyle.Bold);
            InitializeClearAllButton();
            InitializePriorityControls();
            InitializeCheckAsThreeState();
            RefreshTooltips();
            subject.Attach(this);
        }

        private void InitializePriorityControls()
        {
            // Align Example card and Clear button flush with the bottom cards' right edge (x = 570).
            this.groupBox3.Location = new Point(426, 10);
            this.groupBox3.Size = new Size(144, 100);
            if (this.btnClearAllKeys != null)
            {
                this.btnClearAllKeys.Location = new Point(426, 115);
                this.btnClearAllKeys.Size = new Size(144, 26);
            }

            // Compact Spammer Configuration card so Action Priority sits in its own unified card right below.
            this.groupAhkConfig.Location = new Point(14, 146);
            this.groupAhkConfig.Size = new Size(556, 63);

            this.ahkCompatibility.Location = new Point(14, 19);
            this.ahkSpeedBoost.Location = new Point(14, 39);

            this.groupBox4.Location = new Point(142, 12);
            this.groupBox4.Size = new Size(242, 44);
            this.chkMouseFlick.Location = new Point(152, 28);
            this.pbUpAndDownArrow.Location = new Point(238, 28);
            this.chkNoShift.Location = new Point(268, 28);
            this.pictureBox1.Location = new Point(344, 26);

            this.groupBox2.Location = new Point(394, 12);
            this.groupBox2.Size = new Size(150, 44);
            this.txtSpammerDelay.Location = new Point(14, 16);
            this.txtSpammerDelay.Size = new Size(64, 23);
            this.label1.Location = new Point(82, 19);

            // Unified Action Priority card: groups player-input pause control and priority hierarchy tightly together.
            this.gbPlayerPriority = new GroupBox
            {
                Location = new Point(14, 213),
                Name = "gbPlayerPriority",
                Size = new Size(556, 62),
                TabStop = false,
                Text = "Action Priority"
            };

            this.lblPlayerYieldTitle = new Label
            {
                AutoSize = true,
                Location = new Point(12, 19),
                Name = "lblPlayerYieldTitle",
                Text = "Player pause:"
            };
            this.lblPlayerYieldTitle.SizeChanged += (s, e) => AlignPriorityControls();

            this.numPlayerYield = new NumericUpDown
            {
                Increment = 10,
                Location = new Point(102, 16),
                Maximum = ActionPriorityCoordinator.MAX_PLAYER_INPUT_YIELD_MS,
                Minimum = ActionPriorityCoordinator.MIN_PLAYER_INPUT_YIELD_MS,
                Name = "numPlayerYield",
                Size = new Size(52, 20),
                Value = ActionPriorityCoordinator.DEFAULT_PLAYER_INPUT_YIELD_MS,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.numPlayerYield.ValueChanged += OnPlayerYieldChanged;

            this.lblPlayerYieldMs = new Label
            {
                AutoSize = true,
                Location = new Point(158, 19),
                Name = "lblPlayerYieldMs",
                Text = "ms"
            };

            this.lblHoldPause = new Label
            {
                AutoSize = true,
                Location = new Point(196, 19),
                Name = "lblHoldPause",
                Text = "Hold pause:"
            };
            this.lblHoldPause.SizeChanged += (s, e) => AlignPriorityControls();

            this.txtHoldPauseKey = new TextBox
            {
                Location = new Point(286, 16),
                Name = "txtHoldPauseKey",
                Size = new Size(64, 20),
                Text = Key.None.ToString(),
                TextAlign = HorizontalAlignment.Center,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.txtHoldPauseKey.PreviewKeyDown += OnHoldPausePreviewKeyDown;
            this.txtHoldPauseKey.KeyDown += FormUtils.OnKeyDown;
            this.txtHoldPauseKey.KeyPress += FormUtils.OnKeyPress;
            this.txtHoldPauseKey.TextChanged += OnHoldPauseKeyChanged;

            this.chkHoldPauseAutopot = new CheckBox
            {
                AutoSize = true,
                Cursor = System.Windows.Forms.Cursors.Hand,
                Location = new Point(358, 18),
                Name = "chkHoldPauseAutopot",
                Text = "Pause Autopot",
                UseVisualStyleBackColor = true,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.chkHoldPauseAutopot.CheckedChanged += OnHoldPauseAutopotChanged;

            this.lblPriorityOrder = new Label
            {
                AutoSize = false,
                Font = this.priorityOrderFont,
                Location = new Point(12, 38),
                Name = "lblPriorityOrder",
                Size = new Size(534, 19),
                Tag = ThemeManager.TAG_ACCENT_TEXT,
                TextAlign = ContentAlignment.MiddleLeft
            };

            this.gbPlayerPriority.Controls.Add(this.lblPlayerYieldTitle);
            this.gbPlayerPriority.Controls.Add(this.numPlayerYield);
            this.gbPlayerPriority.Controls.Add(this.lblPlayerYieldMs);
            this.gbPlayerPriority.Controls.Add(this.lblHoldPause);
            this.gbPlayerPriority.Controls.Add(this.txtHoldPauseKey);
            this.gbPlayerPriority.Controls.Add(this.chkHoldPauseAutopot);
            this.gbPlayerPriority.Controls.Add(this.lblPriorityOrder);

            this.Controls.Add(this.gbPlayerPriority);
            AlignPriorityControls();
        }

        private void AlignPriorityControls()
        {
            if (this.lblPlayerYieldTitle == null || this.numPlayerYield == null || this.lblPlayerYieldMs == null)
            {
                return;
            }

            int titleRight = this.lblPlayerYieldTitle.Left + Math.Max(this.lblPlayerYieldTitle.Width, this.lblPlayerYieldTitle.PreferredWidth);
            this.numPlayerYield.Location = new Point(titleRight + 5, 16);
            this.lblPlayerYieldMs.Location = new Point(this.numPlayerYield.Right + 3, 19);

            if (this.lblHoldPause != null && this.txtHoldPauseKey != null && this.chkHoldPauseAutopot != null)
            {
                int msRight = this.lblPlayerYieldMs.Left + Math.Max(this.lblPlayerYieldMs.Width, this.lblPlayerYieldMs.PreferredWidth);
                this.lblHoldPause.Location = new Point(msRight + 16, 19);
                int holdRight = this.lblHoldPause.Left + Math.Max(this.lblHoldPause.Width, this.lblHoldPause.PreferredWidth);
                this.txtHoldPauseKey.Location = new Point(holdRight + 5, 16);
                this.chkHoldPauseAutopot.Location = new Point(this.txtHoldPauseKey.Right + 10, 18);
            }
        }

        private void OnHoldPausePreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
            {
                e.IsInputKey = true;
            }
        }

        private void OnHoldPauseKeyChanged(object sender, EventArgs e)
        {
            Key key;
            if (!KeyHelper.TryParseKey(this.txtHoldPauseKey.Text, out key))
            {
                return;
            }

            ActionPriorityCoordinator.HoldPauseKey = key;

            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            Profile current = ProfileSingleton.GetCurrent();
            if (current != null && current.UserPreferences != null)
            {
                current.UserPreferences.holdPauseKey = key;
                ProfileSingleton.SetConfiguration(current.UserPreferences);
            }
        }

        private void OnHoldPauseAutopotChanged(object sender, EventArgs e)
        {
            bool includeAutopot = this.chkHoldPauseAutopot.Checked;
            ActionPriorityCoordinator.HoldPauseIncludeAutopot = includeAutopot;

            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            Profile current = ProfileSingleton.GetCurrent();
            if (current != null && current.UserPreferences != null)
            {
                current.UserPreferences.holdPauseIncludeAutopot = includeAutopot;
                ProfileSingleton.SetConfiguration(current.UserPreferences);
            }
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
            AlignPriorityControls();

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
                if (this.lblPlayerYieldTitle != null)
                {
                    this.helpToolTip.SetToolTip(this.lblPlayerYieldTitle, yieldTip);
                }
                this.helpToolTip.SetToolTip(this.numPlayerYield, yieldTip);
                this.helpToolTip.SetToolTip(this.lblPlayerYieldMs, yieldTip);
            }
            if (this.txtHoldPauseKey != null)
            {
                string holdTip = LocalizationManager.T("tooltip.hold_pause_key");
                this.helpToolTip.SetToolTip(this.lblHoldPause, holdTip);
                this.helpToolTip.SetToolTip(this.txtHoldPauseKey, holdTip);
            }
            if (this.chkHoldPauseAutopot != null)
            {
                this.helpToolTip.SetToolTip(this.chkHoldPauseAutopot, LocalizationManager.T("tooltip.hold_pause_autopot"));
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

                        Key holdKey = currentProfile != null && currentProfile.UserPreferences != null
                            ? currentProfile.UserPreferences.holdPauseKey
                            : Key.None;
                        bool holdIncludeAutopot = currentProfile != null && currentProfile.UserPreferences != null
                            && currentProfile.UserPreferences.holdPauseIncludeAutopot;
                        ActionPriorityCoordinator.HoldPauseKey = holdKey;
                        ActionPriorityCoordinator.HoldPauseIncludeAutopot = holdIncludeAutopot;
                        if (this.txtHoldPauseKey != null)
                        {
                            this.txtHoldPauseKey.Text = holdKey.ToString();
                        }
                        if (this.chkHoldPauseAutopot != null)
                        {
                            this.chkHoldPauseAutopot.Checked = holdIncludeAutopot;
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
