using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public class UtilitiesForm : Form, IObserver
    {
        private readonly ToolTip helpToolTip;
        private readonly Font hintFont;

        private bool isUpdatingUi;

        // Card 1: Anti-Position Bug (@refresh)
        private GroupBox gbAntiPositionBug;
        private CheckBox chkRefreshEnabled;
        private Label lblRefreshCmd;
        private TextBox txtRefreshCmd;
        private Button btnTestRefresh;
        private Label lblRefreshHotkey;
        private TextBox txtRefreshHotkey;
        private Button btnClearRefreshHotkey;
        private CheckBox chkRefreshAutoInterval;
        private NumericUpDown numRefreshInterval;
        private Label lblRefreshIntervalSec;
        private CheckBox chkRefreshPauseSit;
        private Label lblRefreshHint;

        // Card 2: Emergency Auto-Escape (Fly Wing / Teleport)
        private GroupBox gbAutoEscape;
        private CheckBox chkEscapeEnabled;
        private Label lblEscapeKey;
        private TextBox txtEscapeKey;
        private Button btnClearEscapeKey;
        private Label lblEscapeHp;
        private NumericUpDown numEscapeHp;
        private Label lblEscapeHpPct;
        private Label lblEscapeCooldown;
        private NumericUpDown numEscapeCooldown;
        private CheckBox chkEscapeConfirmEnter;
        private CheckBox chkEscapePauseTown;
        private Label lblEscapeHint;

        public UtilitiesForm(Subject subject)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Name = "UtilitiesForm";
            this.Text = "Utilities";
            this.Size = new Size(584, 281);
            this.helpToolTip = FormUtils.CreateHelpToolTip();
            this.hintFont = new Font("Segoe UI", 7.75F, FontStyle.Regular);

            BuildUi();
            RefreshTooltipsAndDynamicText();
            SyncFromProfile();

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
                // Card 1: Anti-Position Bug (@refresh) (y = 4..136)
                this.gbAntiPositionBug = new GroupBox
                {
                    Location = new Point(8, 4),
                    Name = "gbAntiPositionBug",
                    Size = new Size(568, 132),
                    TabStop = false,
                    Text = "Anti-Position Bug (@refresh)"
                };

                this.chkRefreshEnabled = new CheckBox
                {
                    AutoSize = true,
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(12, 21),
                    Name = "chkRefreshEnabled",
                    Text = "Enable @refresh",
                    UseVisualStyleBackColor = true
                };
                this.chkRefreshEnabled.CheckedChanged += OnRefreshSettingsChanged;

                this.lblRefreshCmd = new Label
                {
                    AutoSize = true,
                    Location = new Point(156, 22),
                    Name = "lblRefreshCmd",
                    Text = "Command:"
                };

                this.txtRefreshCmd = new TextBox
                {
                    Location = new Point(226, 19),
                    MaxLength = 40,
                    Name = "txtRefreshCmd",
                    Size = new Size(116, 20),
                    Tag = FormUtils.SKIP_RESET_TAG,
                    Text = AutoRefreshCommand.DEFAULT_COMMAND
                };
                this.txtRefreshCmd.Leave += OnRefreshCommandLeave;
                this.txtRefreshCmd.TextChanged += OnRefreshSettingsChanged;

                this.btnTestRefresh = new Button
                {
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(472, 18),
                    Name = "btnTestRefresh",
                    Size = new Size(82, 23),
                    Text = "Test",
                    UseVisualStyleBackColor = true
                };
                this.btnTestRefresh.Click += OnTestRefreshClick;

                this.lblRefreshHotkey = new Label
                {
                    AutoSize = true,
                    Location = new Point(12, 52),
                    Name = "lblRefreshHotkey",
                    Text = "Hotkey (1-tap):"
                };

                this.txtRefreshHotkey = new TextBox
                {
                    Location = new Point(124, 49),
                    Name = "txtRefreshHotkey",
                    Size = new Size(68, 20),
                    Text = Key.None.ToString(),
                    TextAlign = HorizontalAlignment.Center
                };
                this.txtRefreshHotkey.KeyDown += FormUtils.OnKeyDown;
                this.txtRefreshHotkey.KeyPress += FormUtils.OnKeyPress;
                this.txtRefreshHotkey.TextChanged += OnRefreshHotkeyChanged;

                this.btnClearRefreshHotkey = new Button
                {
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(198, 48),
                    Name = "btnClearRefreshHotkey",
                    Size = new Size(62, 22),
                    Text = "Clear",
                    UseVisualStyleBackColor = true
                };
                this.btnClearRefreshHotkey.Click += (s, e) =>
                {
                    this.txtRefreshHotkey.Text = Key.None.ToString();
                };

                this.chkRefreshAutoInterval = new CheckBox
                {
                    AutoSize = true,
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(12, 81),
                    Name = "chkRefreshAutoInterval",
                    Text = "Auto every:",
                    UseVisualStyleBackColor = true
                };
                this.chkRefreshAutoInterval.CheckedChanged += OnRefreshSettingsChanged;

                this.numRefreshInterval = new NumericUpDown
                {
                    Location = new Point(124, 79),
                    Maximum = AutoRefreshCommand.MAX_INTERVAL_SECONDS,
                    Minimum = AutoRefreshCommand.MIN_INTERVAL_SECONDS,
                    Name = "numRefreshInterval",
                    Size = new Size(68, 20),
                    Tag = FormUtils.SKIP_RESET_TAG,
                    Value = AutoRefreshCommand.DEFAULT_INTERVAL_SECONDS
                };
                this.numRefreshInterval.ValueChanged += OnRefreshSettingsChanged;

                this.lblRefreshIntervalSec = new Label
                {
                    AutoSize = true,
                    Location = new Point(198, 82),
                    Name = "lblRefreshIntervalSec",
                    Text = "sec"
                };

                this.chkRefreshPauseSit = new CheckBox
                {
                    AutoSize = true,
                    Checked = true,
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(256, 81),
                    Name = "chkRefreshPauseSit",
                    Text = "Pause when sitting",
                    UseVisualStyleBackColor = true
                };
                this.chkRefreshPauseSit.CheckedChanged += OnRefreshSettingsChanged;

                this.lblRefreshHint = new Label
                {
                    AutoSize = false,
                    Font = this.hintFont,
                    Location = new Point(12, 107),
                    Name = "lblRefreshHint",
                    Size = new Size(544, 18),
                    Tag = ThemeManager.TAG_ACCENT_TEXT,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                this.gbAntiPositionBug.Controls.Add(this.chkRefreshEnabled);
                this.gbAntiPositionBug.Controls.Add(this.lblRefreshCmd);
                this.gbAntiPositionBug.Controls.Add(this.txtRefreshCmd);
                this.gbAntiPositionBug.Controls.Add(this.btnTestRefresh);
                this.gbAntiPositionBug.Controls.Add(this.lblRefreshHotkey);
                this.gbAntiPositionBug.Controls.Add(this.txtRefreshHotkey);
                this.gbAntiPositionBug.Controls.Add(this.btnClearRefreshHotkey);
                this.gbAntiPositionBug.Controls.Add(this.chkRefreshAutoInterval);
                this.gbAntiPositionBug.Controls.Add(this.numRefreshInterval);
                this.gbAntiPositionBug.Controls.Add(this.lblRefreshIntervalSec);
                this.gbAntiPositionBug.Controls.Add(this.chkRefreshPauseSit);
                this.gbAntiPositionBug.Controls.Add(this.lblRefreshHint);

                // Card 2: Emergency Auto-Escape (Fly Wing / Teleport) (y = 142..274)
                this.gbAutoEscape = new GroupBox
                {
                    Location = new Point(8, 142),
                    Name = "gbAutoEscape",
                    Size = new Size(568, 132),
                    TabStop = false,
                    Text = "Emergency Auto-Escape (Fly Wing / Teleport)"
                };

                this.chkEscapeEnabled = new CheckBox
                {
                    AutoSize = true,
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(12, 21),
                    Name = "chkEscapeEnabled",
                    Text = "Enable Auto-Escape",
                    UseVisualStyleBackColor = true
                };
                this.chkEscapeEnabled.CheckedChanged += OnEscapeSettingsChanged;

                this.lblEscapeKey = new Label
                {
                    AutoSize = true,
                    Location = new Point(164, 22),
                    Name = "lblEscapeKey",
                    Text = "Escape Key:"
                };

                this.txtEscapeKey = new TextBox
                {
                    Location = new Point(242, 19),
                    Name = "txtEscapeKey",
                    Size = new Size(68, 20),
                    Text = Key.None.ToString(),
                    TextAlign = HorizontalAlignment.Center
                };
                this.txtEscapeKey.KeyDown += FormUtils.OnKeyDown;
                this.txtEscapeKey.KeyPress += FormUtils.OnKeyPress;
                this.txtEscapeKey.TextChanged += OnEscapeKeyChanged;

                this.btnClearEscapeKey = new Button
                {
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(316, 18),
                    Name = "btnClearEscapeKey",
                    Size = new Size(62, 22),
                    Text = "Clear",
                    UseVisualStyleBackColor = true
                };
                this.btnClearEscapeKey.Click += (s, e) =>
                {
                    this.txtEscapeKey.Text = Key.None.ToString();
                };

                this.lblEscapeHp = new Label
                {
                    AutoSize = true,
                    Location = new Point(12, 53),
                    Name = "lblEscapeHp",
                    Text = "Trigger HP <"
                };

                this.numEscapeHp = new NumericUpDown
                {
                    Location = new Point(104, 50),
                    Maximum = AutoEscape.MAX_HP_PERCENT,
                    Minimum = AutoEscape.MIN_HP_PERCENT,
                    Name = "numEscapeHp",
                    Size = new Size(54, 20),
                    Tag = FormUtils.SKIP_RESET_TAG,
                    Value = AutoEscape.DEFAULT_HP_PERCENT
                };
                this.numEscapeHp.ValueChanged += OnEscapeSettingsChanged;

                this.lblEscapeHpPct = new Label
                {
                    AutoSize = true,
                    Location = new Point(162, 53),
                    Name = "lblEscapeHpPct",
                    Text = "%"
                };

                this.lblEscapeCooldown = new Label
                {
                    AutoSize = true,
                    Location = new Point(206, 53),
                    Name = "lblEscapeCooldown",
                    Text = "Cooldown (ms):"
                };

                this.numEscapeCooldown = new NumericUpDown
                {
                    Increment = 250,
                    Location = new Point(302, 50),
                    Maximum = AutoEscape.MAX_COOLDOWN_MS,
                    Minimum = AutoEscape.MIN_COOLDOWN_MS,
                    Name = "numEscapeCooldown",
                    Size = new Size(70, 20),
                    Tag = FormUtils.SKIP_RESET_TAG,
                    Value = AutoEscape.DEFAULT_COOLDOWN_MS
                };
                this.numEscapeCooldown.ValueChanged += OnEscapeSettingsChanged;

                this.chkEscapeConfirmEnter = new CheckBox
                {
                    AutoSize = true,
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(12, 81),
                    Name = "chkEscapeConfirmEnter",
                    Text = "Press Enter after key (Teleport skill)",
                    UseVisualStyleBackColor = true
                };
                this.chkEscapeConfirmEnter.CheckedChanged += OnEscapeSettingsChanged;

                this.chkEscapePauseTown = new CheckBox
                {
                    AutoSize = true,
                    Checked = true,
                    Cursor = System.Windows.Forms.Cursors.Hand,
                    Location = new Point(302, 81),
                    Name = "chkEscapePauseTown",
                    Text = "Disable in towns",
                    UseVisualStyleBackColor = true
                };
                this.chkEscapePauseTown.CheckedChanged += OnEscapeSettingsChanged;

                this.lblEscapeHint = new Label
                {
                    AutoSize = false,
                    Font = this.hintFont,
                    Location = new Point(12, 107),
                    Name = "lblEscapeHint",
                    Size = new Size(544, 18),
                    Tag = ThemeManager.TAG_ACCENT_TEXT,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                this.gbAutoEscape.Controls.Add(this.chkEscapeEnabled);
                this.gbAutoEscape.Controls.Add(this.lblEscapeKey);
                this.gbAutoEscape.Controls.Add(this.txtEscapeKey);
                this.gbAutoEscape.Controls.Add(this.btnClearEscapeKey);
                this.gbAutoEscape.Controls.Add(this.lblEscapeHp);
                this.gbAutoEscape.Controls.Add(this.numEscapeHp);
                this.gbAutoEscape.Controls.Add(this.lblEscapeHpPct);
                this.gbAutoEscape.Controls.Add(this.lblEscapeCooldown);
                this.gbAutoEscape.Controls.Add(this.numEscapeCooldown);
                this.gbAutoEscape.Controls.Add(this.chkEscapeConfirmEnter);
                this.gbAutoEscape.Controls.Add(this.chkEscapePauseTown);
                this.gbAutoEscape.Controls.Add(this.lblEscapeHint);

                this.Controls.Add(this.gbAntiPositionBug);
                this.Controls.Add(this.gbAutoEscape);
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        private void OnRefreshSettingsChanged(object sender, EventArgs e)
        {
            if (this.numRefreshInterval != null && this.chkRefreshAutoInterval != null)
            {
                this.numRefreshInterval.Enabled = this.chkRefreshAutoInterval.Checked;
            }

            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            Profile current = ProfileSingleton.GetCurrent();
            if (current == null || current.AutoRefreshCommand == null)
            {
                return;
            }

            current.AutoRefreshCommand.Enabled = this.chkRefreshEnabled.Checked;
            current.AutoRefreshCommand.AutoIntervalEnabled = this.chkRefreshAutoInterval.Checked;
            current.AutoRefreshCommand.IntervalSeconds = decimal.ToInt32(this.numRefreshInterval.Value);
            current.AutoRefreshCommand.PauseWhenSitting = this.chkRefreshPauseSit.Checked;
            current.AutoRefreshCommand.CommandText = AutoRefreshCommand.NormalizeCommand(this.txtRefreshCmd.Text);
            ProfileSingleton.SetConfiguration(current.AutoRefreshCommand);
        }

        private void OnRefreshCommandLeave(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || this.txtRefreshCmd == null)
            {
                return;
            }

            string normalized = AutoRefreshCommand.NormalizeCommand(this.txtRefreshCmd.Text);
            if (!string.Equals(this.txtRefreshCmd.Text, normalized, StringComparison.Ordinal))
            {
                this.isUpdatingUi = true;
                try
                {
                    this.txtRefreshCmd.Text = normalized;
                }
                finally
                {
                    this.isUpdatingUi = false;
                }
            }
        }

        private void OnRefreshHotkeyChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            Profile current = ProfileSingleton.GetCurrent();
            if (current == null || current.AutoRefreshCommand == null)
            {
                return;
            }

            Key key;
            if (KeyHelper.TryParseKey(this.txtRefreshHotkey.Text, out key))
            {
                current.AutoRefreshCommand.Hotkey = key;
                ProfileSingleton.SetConfiguration(current.AutoRefreshCommand);
            }
        }

        private void OnEscapeSettingsChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            Profile current = ProfileSingleton.GetCurrent();
            if (current == null || current.AutoEscape == null)
            {
                return;
            }

            current.AutoEscape.Enabled = this.chkEscapeEnabled.Checked;
            current.AutoEscape.HpPercent = decimal.ToInt32(this.numEscapeHp.Value);
            current.AutoEscape.CooldownMs = decimal.ToInt32(this.numEscapeCooldown.Value);
            current.AutoEscape.ConfirmWithEnter = this.chkEscapeConfirmEnter.Checked;
            current.AutoEscape.PauseInTown = this.chkEscapePauseTown.Checked;
            ProfileSingleton.SetConfiguration(current.AutoEscape);
        }

        private void OnEscapeKeyChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            Profile current = ProfileSingleton.GetCurrent();
            if (current == null || current.AutoEscape == null)
            {
                return;
            }

            Key key;
            if (KeyHelper.TryParseKey(this.txtEscapeKey.Text, out key))
            {
                current.AutoEscape.EscapeKey = key;
                ProfileSingleton.SetConfiguration(current.AutoEscape);
            }
        }

        private void OnTestRefreshClick(object sender, EventArgs e)
        {
            Client roClient = ClientSingleton.GetClient();
            if (roClient == null || !roClient.IsProcessAlive())
            {
                MessageBox.Show(
                    LocalizationManager.T("msg.select_client_first"),
                    LocalizationManager.T("title.warning"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            Profile current = ProfileSingleton.GetCurrent();
            AutoRefreshCommand cmd = current != null && current.AutoRefreshCommand != null
                ? current.AutoRefreshCommand
                : new AutoRefreshCommand();
            cmd.CommandText = AutoRefreshCommand.NormalizeCommand(this.txtRefreshCmd != null ? this.txtRefreshCmd.Text : AutoRefreshCommand.DEFAULT_COMMAND);
            System.Threading.ThreadPool.QueueUserWorkItem(_ => cmd.SendRefreshCommand(roClient, true));
        }

        private void RefreshTooltipsAndDynamicText()
        {
            if (this.lblRefreshHint != null)
            {
                this.lblRefreshHint.Text = LocalizationManager.T("refresh.hint");
            }

            if (this.lblEscapeHint != null)
            {
                this.lblEscapeHint.Text = LocalizationManager.T("escape.hint");
            }

            if (this.helpToolTip == null)
            {
                return;
            }

            this.helpToolTip.SetToolTip(this.chkRefreshEnabled, LocalizationManager.T("tooltip.refresh_enabled"));
            this.helpToolTip.SetToolTip(this.txtRefreshCmd, LocalizationManager.T("tooltip.refresh_cmd"));
            this.helpToolTip.SetToolTip(this.chkRefreshPauseSit, LocalizationManager.T("tooltip.refresh_pause_sit"));
            this.helpToolTip.SetToolTip(this.txtRefreshHotkey, LocalizationManager.T("tooltip.refresh_hotkey"));
            this.helpToolTip.SetToolTip(this.btnClearRefreshHotkey, LocalizationManager.T("tooltip.refresh_clear"));
            this.helpToolTip.SetToolTip(this.chkRefreshAutoInterval, LocalizationManager.T("tooltip.refresh_auto"));
            this.helpToolTip.SetToolTip(this.numRefreshInterval, LocalizationManager.T("tooltip.refresh_interval"));
            this.helpToolTip.SetToolTip(this.btnTestRefresh, LocalizationManager.T("tooltip.refresh_test"));

            this.helpToolTip.SetToolTip(this.chkEscapeEnabled, LocalizationManager.T("tooltip.escape_enabled"));
            this.helpToolTip.SetToolTip(this.txtEscapeKey, LocalizationManager.T("tooltip.escape_key"));
            this.helpToolTip.SetToolTip(this.btnClearEscapeKey, LocalizationManager.T("tooltip.escape_clear"));
            this.helpToolTip.SetToolTip(this.numEscapeHp, LocalizationManager.T("tooltip.escape_hp"));
            this.helpToolTip.SetToolTip(this.numEscapeCooldown, LocalizationManager.T("tooltip.escape_cooldown"));
            this.helpToolTip.SetToolTip(this.chkEscapeConfirmEnter, LocalizationManager.T("tooltip.escape_confirm_enter"));
            this.helpToolTip.SetToolTip(this.chkEscapePauseTown, LocalizationManager.T("tooltip.escape_pause_town"));
        }

        private void SyncFromProfile()
        {
            this.isUpdatingUi = true;
            try
            {
                Profile current = ProfileSingleton.GetCurrent();
                if (current != null && current.AutoRefreshCommand != null)
                {
                    AutoRefreshCommand refresh = current.AutoRefreshCommand;
                    this.chkRefreshEnabled.Checked = refresh.Enabled;
                    this.txtRefreshCmd.Text = AutoRefreshCommand.NormalizeCommand(refresh.CommandText);
                    this.chkRefreshPauseSit.Checked = refresh.PauseWhenSitting;
                    this.txtRefreshHotkey.Text = refresh.Hotkey.ToString();
                    this.chkRefreshAutoInterval.Checked = refresh.AutoIntervalEnabled;
                    FormUtils.SetNumericValue(this.numRefreshInterval, refresh.GetEffectiveIntervalSeconds());
                    this.numRefreshInterval.Enabled = refresh.AutoIntervalEnabled;
                }

                if (current != null && current.AutoEscape != null)
                {
                    AutoEscape escape = current.AutoEscape;
                    this.chkEscapeEnabled.Checked = escape.Enabled;
                    this.txtEscapeKey.Text = escape.EscapeKey.ToString();
                    FormUtils.SetNumericValue(this.numEscapeHp, escape.GetEffectiveHpPercent());
                    FormUtils.SetNumericValue(this.numEscapeCooldown, escape.GetEffectiveCooldownMs());
                    this.chkEscapeConfirmEnter.Checked = escape.ConfirmWithEnter;
                    this.chkEscapePauseTown.Checked = escape.PauseInTown;
                }
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
                    if (currentOn != null)
                    {
                        if (currentOn.AutoRefreshCommand != null)
                        {
                            currentOn.AutoRefreshCommand.Start();
                        }
                        if (currentOn.AutoEscape != null)
                        {
                            currentOn.AutoEscape.Start();
                        }
                    }
                    break;
                case MessageCode.TURN_OFF:
                    Profile currentOff = ProfileSingleton.GetCurrent();
                    if (currentOff != null)
                    {
                        if (currentOff.AutoRefreshCommand != null)
                        {
                            currentOff.AutoRefreshCommand.Stop();
                        }
                        if (currentOff.AutoEscape != null)
                        {
                            currentOff.AutoEscape.Stop();
                        }
                    }
                    break;
                case MessageCode.LANGUAGE_CHANGED:
                    RefreshTooltipsAndDynamicText();
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.helpToolTip != null)
                {
                    this.helpToolTip.Dispose();
                }
                if (this.hintFont != null)
                {
                    this.hintFont.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
