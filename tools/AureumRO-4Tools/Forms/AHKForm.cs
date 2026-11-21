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

        public AHKForm(Subject subject)
        {
            InitializeComponent();
            this.helpToolTip = FormUtils.CreateHelpToolTip(this.components);
            this.pictureBox1.Left = 266;
            this.pbUpAndDownArrow.Left = 282;
            InitializeClearAllButton();
            InitializeCheckAsThreeState();
            RefreshTooltips();
            subject.Attach(this);
        }

        private void RefreshTooltips()
        {
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
    }
}
