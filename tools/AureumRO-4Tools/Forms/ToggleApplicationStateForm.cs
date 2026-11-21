using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class ToggleApplicationStateForm : Form, IObserver
    {
        private readonly Subject subject;
        private ContextMenu contextMenu;
        private MenuItem menuItem;
        private CheckBox cbFocusOnly;
        private CheckBox cbChatPause;
        private ToolTip optionsToolTip;
        private SoundPlayer soundOnPlayer;
        private SoundPlayer soundOffPlayer;

        // Store key used for last profile so it can be cleanly removed when switching profiles.
        private Keys lastKey = Keys.None;
        private bool isUpdatingUi;
        private bool isApplicationOn;

        public ToggleApplicationStateForm(Subject subject)
        {
            InitializeComponent();

            // Keep green/red/orange ON/OFF/CHAT status button colors untouched by ThemeManager.
            this.btnStatusToggle.Tag = ThemeManager.PRESERVE_COLORS_TAG;
            this.notifyIconTray.Text = AppConfig.Name;

            // Center status prompt across the groupbox so translated PT-BR strings never clip.
            this.lblStatusToggle.AutoSize = false;
            this.lblStatusToggle.Location = new Point(6, 80);
            this.lblStatusToggle.Size = new Size(210, 17);
            this.lblStatusToggle.TextAlign = ContentAlignment.MiddleCenter;

            this.subject = subject;
            this.subject.Attach(this);
            KeyboardHook.Enable();

            InitializeAudioPlayers();
            InitializeOptionCheckboxes();
            InitializeContextualMenu();

            ChatPauseState.StateChanged += OnChatPauseStateChanged;
            RegisterChatPauseHooks();

            this.cbAudio.CheckedChanged += OnAudioCheckedChanged;

            this.txtStatusToggleKey.Text = ProfileSingleton.GetCurrent().UserPreferences.toggleStateKey;
            this.txtStatusToggleKey.KeyDown += new KeyEventHandler(FormUtils.OnKeyDown);
            this.txtStatusToggleKey.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
            this.txtStatusToggleKey.TextChanged += new EventHandler(this.onStatusToggleKeyChange);
        }

        private void InitializeAudioPlayers()
        {
            try
            {
                this.soundOnPlayer = new SoundPlayer(Resources._4RTools.ETCResource.Speech_On);
                this.soundOffPlayer = new SoundPlayer(Resources._4RTools.ETCResource.Speech_Off);
            }
            catch (Exception ex)
            {
                Logger.Warn("ToggleApplicationStateForm", "Failed to initialize sound players", ex);
            }
        }

        private void InitializeOptionCheckboxes()
        {
            this.optionsToolTip = FormUtils.CreateHelpToolTip(this.components);

            this.cbFocusOnly = new CheckBox
            {
                AutoSize = true,
                Location = new Point(156, 53),
                Name = "cbFocusOnly",
                Text = "Focus",
                UseVisualStyleBackColor = true,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.cbFocusOnly.CheckedChanged += OnFocusOnlyCheckedChanged;
            this.groupBox1.Controls.Add(this.cbFocusOnly);

            this.cbChatPause = new CheckBox
            {
                AutoSize = true,
                Location = new Point(10, 53),
                Name = "cbChatPause",
                Text = "Chat",
                UseVisualStyleBackColor = true,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.cbChatPause.CheckedChanged += OnChatPauseCheckedChanged;
            this.groupBox1.Controls.Add(this.cbChatPause);

            RefreshLocalizedTooltipsAndMenu();
        }

        private void RefreshLocalizedTooltipsAndMenu()
        {
            if (this.optionsToolTip != null)
            {
                this.optionsToolTip.SetToolTip(this.btnStatusToggle, LocalizationManager.T("tooltip.toggle_btn"));
                this.optionsToolTip.SetToolTip(this.txtStatusToggleKey, LocalizationManager.T("tooltip.toggle_key"));
                this.optionsToolTip.SetToolTip(this.cbAudio, LocalizationManager.T("tooltip.toggle_audio"));
                if (this.cbFocusOnly != null)
                {
                    this.optionsToolTip.SetToolTip(this.cbFocusOnly, LocalizationManager.T("tooltip.focus_only"));
                }
                if (this.cbChatPause != null)
                {
                    this.optionsToolTip.SetToolTip(this.cbChatPause, LocalizationManager.T("tooltip.chat_pause"));
                }
            }

            if (this.menuItem != null)
            {
                this.menuItem.Text = LocalizationManager.T("tray.close");
            }
        }

        private void RegisterChatPauseHooks()
        {
            if (this.lastKey != Keys.Return)
            {
                KeyboardHook.Add(Keys.Return, new KeyboardHook.KeyPressed(this.OnChatEnterKeyPressed));
            }

            if (this.lastKey != Keys.Escape)
            {
                KeyboardHook.Add(Keys.Escape, new KeyboardHook.KeyPressed(this.OnChatEscapeKeyPressed));
            }
        }

        private bool OnChatEnterKeyPressed()
        {
            if (this.isApplicationOn
                && this.cbChatPause != null
                && this.cbChatPause.Checked
                && !KeyboardHook.Alt
                && !KeyboardHook.Control)
            {
                Client client = ClientSingleton.GetClient();
                if (client != null && client.IsGameWindowFocused())
                {
                    ChatPauseState.ToggleChatting();
                }
            }

            // Always allow Enter to pass through to Ragnarok and other applications.
            return true;
        }

        private bool OnChatEscapeKeyPressed()
        {
            if (ChatPauseState.IsChatting)
            {
                ChatPauseState.Resume();
            }

            // Always allow Escape to pass through to Ragnarok and other applications.
            return true;
        }

        private void OnChatPauseStateChanged(object sender, EventArgs e)
        {
            if (this.IsDisposed || !this.IsHandleCreated)
            {
                return;
            }

            MethodInvoker updateVisuals = delegate
            {
                if (this.IsDisposed || !this.isApplicationOn)
                {
                    return;
                }

                ApplyOnVisualState();
            };

            if (this.InvokeRequired)
            {
                this.BeginInvoke(updateVisuals);
            }
            else
            {
                updateVisuals();
            }
        }

        private void InitializeContextualMenu()
        {
            this.contextMenu = new ContextMenu();
            this.menuItem = new MenuItem();

            this.contextMenu.MenuItems.AddRange(new MenuItem[] { this.menuItem });

            this.menuItem.Index = 0;
            this.menuItem.Text = LocalizationManager.T("tray.close");
            this.menuItem.Click += new EventHandler(this.notifyShutdownApplication);

            this.notifyIconTray.ContextMenu = this.contextMenu;
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
                        ChatPauseState.Resume();
                        UserPreferences prefs = ProfileSingleton.GetCurrent().UserPreferences;
                        Keys currentToggleKey;
                        if (!KeyHelper.TryParseFormsKeys(prefs.toggleStateKey, out currentToggleKey))
                        {
                            currentToggleKey = Keys.End;
                        }

                        KeyboardHook.Remove(this.lastKey);
                        this.txtStatusToggleKey.Text = currentToggleKey.ToString();
                        if (currentToggleKey != Keys.None)
                        {
                            KeyboardHook.Add(currentToggleKey, new KeyboardHook.KeyPressed(this.OnToggleHotkeyPressed));
                        }
                        this.lastKey = currentToggleKey;
                        RegisterChatPauseHooks();

                        this.cbAudio.Checked = prefs.soundEnabled;
                        this.cbFocusOnly.Checked = prefs.pauseWhenUnfocused;
                        this.cbChatPause.Checked = prefs.pauseOnChat;
                    }
                    finally
                    {
                        this.isUpdatingUi = false;
                    }
                    break;

                case MessageCode.TURN_OFF:
                    ChatPauseState.Resume();
                    if (this.isApplicationOn)
                    {
                        ApplyOffVisualState(LocalizationManager.T("status.press_to_start"), Color.Empty, false);
                    }
                    break;

                case MessageCode.CLIENT_DISCONNECTED:
                    ApplyOffVisualState(LocalizationManager.T("status.client_closed"), ThemeManager.IsDarkMode ? Color.Salmon : Color.DarkRed, true);
                    break;

                case MessageCode.LANGUAGE_CHANGED:
                    RefreshLocalizedTooltipsAndMenu();
                    if (this.isApplicationOn)
                    {
                        ApplyOnVisualState();
                    }
                    break;
            }
        }

        private void btnToggleStatusHandler(object sender, EventArgs e)
        {
            ToggleStatus();
        }

        private bool OnToggleHotkeyPressed()
        {
            // Do not toggle application state while the user is typing a new hotkey.
            if (this.txtStatusToggleKey.Focused)
            {
                return true;
            }

            return ToggleStatus();
        }

        private void onStatusToggleKeyChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            Keys currentToggleKey;
            if (!KeyHelper.TryParseFormsKeys(this.txtStatusToggleKey.Text, out currentToggleKey))
            {
                currentToggleKey = Keys.None;
            }

            KeyboardHook.Remove(this.lastKey);
            if (currentToggleKey != Keys.None)
            {
                KeyboardHook.Add(currentToggleKey, new KeyboardHook.KeyPressed(this.OnToggleHotkeyPressed));
            }

            ProfileSingleton.GetCurrent().UserPreferences.toggleStateKey = currentToggleKey.ToString();
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().UserPreferences);

            this.lastKey = currentToggleKey;
            RegisterChatPauseHooks();
        }

        private void OnAudioCheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            ProfileSingleton.GetCurrent().UserPreferences.soundEnabled = this.cbAudio.Checked;
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().UserPreferences);
        }

        private void OnFocusOnlyCheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            ProfileSingleton.GetCurrent().UserPreferences.pauseWhenUnfocused = this.cbFocusOnly.Checked;
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().UserPreferences);
        }

        private void OnChatPauseCheckedChanged(object sender, EventArgs e)
        {
            if (!this.cbChatPause.Checked)
            {
                ChatPauseState.Resume();
            }

            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            ProfileSingleton.GetCurrent().UserPreferences.pauseOnChat = this.cbChatPause.Checked;
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().UserPreferences);
        }

        private bool ToggleStatus()
        {
            if (this.isApplicationOn)
            {
                ApplyOffVisualState(LocalizationManager.T("status.press_to_start"), Color.Empty, true);
                this.subject.Notify(new Utils.Message(MessageCode.TURN_OFF, null));
            }
            else
            {
                Client client = ClientSingleton.GetClient();
                if (client != null && client.IsProcessAlive())
                {
                    ChatPauseState.Resume();
                    this.isApplicationOn = true;
                    this.notifyIconTray.Icon = Resources._4RTools.ETCResource.logo_4rtools_on;
                    ApplyOnVisualState();
                    this.subject.Notify(new Utils.Message(MessageCode.TURN_ON, null));

                    PlayAudio(this.soundOnPlayer);
                }
                else
                {
                    this.lblStatusToggle.Text = LocalizationManager.T("status.select_client");
                    this.lblStatusToggle.ForeColor = ThemeManager.IsDarkMode ? Color.Salmon : Color.Red;
                }
            }

            return true;
        }

        private void ApplyOnVisualState()
        {
            if (ChatPauseState.IsChatting)
            {
                this.btnStatusToggle.BackColor = Color.DarkOrange;
                this.btnStatusToggle.Text = "CHAT";
                this.lblStatusToggle.Text = LocalizationManager.T("status.chat_paused");
                this.lblStatusToggle.ForeColor = Color.DarkOrange;
            }
            else
            {
                this.btnStatusToggle.BackColor = Color.Green;
                this.btnStatusToggle.Text = "ON";
                this.lblStatusToggle.Text = LocalizationManager.T("status.press_to_stop");
                this.lblStatusToggle.ForeColor = ThemeManager.GetDefaultTextColor();
            }
        }

        private void ApplyOffVisualState(string message, Color textColor, bool playOffSound)
        {
            ChatPauseState.Resume();
            this.isApplicationOn = false;
            this.btnStatusToggle.BackColor = Color.Red;
            this.btnStatusToggle.Text = "OFF";
            this.notifyIconTray.Icon = Resources._4RTools.ETCResource.logo_4rtools_off;
            this.lblStatusToggle.Text = message;
            this.lblStatusToggle.ForeColor = textColor == Color.Empty ? ThemeManager.GetDefaultTextColor() : textColor;

            if (playOffSound)
            {
                PlayAudio(this.soundOffPlayer);
            }
        }

        private void PlayAudio(SoundPlayer player)
        {
            if (!this.cbAudio.Checked || player == null)
            {
                return;
            }

            try
            {
                player.Play();
            }
            catch (Exception ex)
            {
                Logger.Warn("ToggleApplicationStateForm", "Failed to play toggle sound", ex);
            }
        }

        private void notifyIconDoubleClick(object sender, MouseEventArgs e)
        {
            this.subject.Notify(new Utils.Message(MessageCode.CLICK_ICON_TRAY, null));
        }

        private void notifyShutdownApplication(object sender, EventArgs e)
        {
            this.subject.Notify(new Utils.Message(MessageCode.SHUTDOWN_APPLICATION, null));
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ChatPauseState.StateChanged -= OnChatPauseStateChanged;
            ChatPauseState.Resume();
            KeyboardHook.Remove(this.lastKey);
            KeyboardHook.Remove(Keys.Return);
            KeyboardHook.Remove(Keys.Escape);
            if (this.soundOnPlayer != null)
            {
                this.soundOnPlayer.Dispose();
                this.soundOnPlayer = null;
            }
            if (this.soundOffPlayer != null)
            {
                this.soundOffPlayer.Dispose();
                this.soundOffPlayer = null;
            }
            if (this.optionsToolTip != null)
            {
                this.optionsToolTip.Dispose();
                this.optionsToolTip = null;
            }
            if (this.notifyIconTray != null)
            {
                this.notifyIconTray.Visible = false;
            }
            base.OnFormClosed(e);
        }
    }
}