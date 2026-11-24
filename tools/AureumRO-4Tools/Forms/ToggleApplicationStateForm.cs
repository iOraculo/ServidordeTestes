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
        private readonly SessionContext session;
        private readonly bool isEmbeddedTab;
        private ContextMenu contextMenu;
        private MenuItem menuItem;
        private CheckBox cbFocusOnly;
        private CheckBox cbChatPause;
        private CheckBox cbMiniHud;
        private ToolTip optionsToolTip;
        private SoundPlayer soundOnPlayer;
        private SoundPlayer soundOffPlayer;

        public event Action<bool> MiniHudCheckedChanged;

        // Store key used for last profile so it can be cleanly removed when switching profiles.
        private Keys lastKey = Keys.None;
        private bool isUpdatingUi;
        private bool isApplicationOn;

        public ToggleApplicationStateForm(Subject subject)
            : this(subject, null, false)
        {
        }

        public ToggleApplicationStateForm(Subject subject, SessionContext session, bool isEmbeddedTab)
        {
            this.session = session ?? SessionContext.Current;
            this.isEmbeddedTab = isEmbeddedTab;

            InitializeComponent();

            // Keep green/red/orange ON/OFF/CHAT status button colors untouched by ThemeManager.
            this.btnStatusToggle.Tag = ThemeManager.PRESERVE_COLORS_TAG;
            this.notifyIconTray.Text = AppConfig.Name;
            if (this.isEmbeddedTab)
            {
                this.notifyIconTray.Visible = false;
            }

            // Layout: ON/OFF button & hotkey on the left; all 4 option checkboxes stacked cleanly on the right.
            this.groupBox1.Size = new Size(248, 114);
            this.btnStatusToggle.Location = new Point(14, 18);
            this.btnStatusToggle.Size = new Size(108, 34);
            this.txtStatusToggleKey.Location = new Point(14, 57);
            this.txtStatusToggleKey.Size = new Size(108, 24);

            this.cbAudio.Location = new Point(138, 15);
            this.cbAudio.Cursor = Cursors.Hand;

            // Center status prompt across the bottom of the groupbox so translated PT-BR strings never clip.
            this.lblStatusToggle.MaximumSize = Size.Empty;
            this.lblStatusToggle.AutoSize = false;
            this.lblStatusToggle.Location = new Point(6, 91);
            this.lblStatusToggle.Size = new Size(236, 18);
            this.lblStatusToggle.TextAlign = ContentAlignment.MiddleCenter;

            this.subject = subject;
            this.subject.Attach(this);
            KeyboardHook.Enable();

            InitializeAudioPlayers();
            InitializeOptionCheckboxes();
            InitializeContextualMenu();

            this.session.ChatStateChanged += OnChatPauseStateChanged;
            RegisterChatPauseHooks();

            this.cbAudio.CheckedChanged += OnAudioCheckedChanged;

            using (SessionContext.Enter(this.session))
            {
                this.txtStatusToggleKey.Text = ProfileSingleton.GetCurrent().UserPreferences.toggleStateKey;
            }
            this.txtStatusToggleKey.KeyDown += new KeyEventHandler(FormUtils.OnKeyDown);
            this.txtStatusToggleKey.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
            this.txtStatusToggleKey.TextChanged += new EventHandler(this.onStatusToggleKeyChange);
        }

        public void SetAutomationState(bool turnOn)
        {
            using (SessionContext.Enter(this.session))
            {
                if (turnOn && !this.isApplicationOn)
                {
                    ToggleStatus();
                }
                else if (!turnOn && this.isApplicationOn)
                {
                    ToggleStatus();
                }
            }
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

        public void SetMiniHudChecked(bool isChecked)
        {
            if (this.cbMiniHud == null)
            {
                return;
            }

            bool prevUpdating = this.isUpdatingUi;
            this.isUpdatingUi = true;
            try
            {
                this.cbMiniHud.Checked = isChecked;
            }
            finally
            {
                this.isUpdatingUi = prevUpdating;
            }
        }

        private void InitializeOptionCheckboxes()
        {
            this.optionsToolTip = FormUtils.CreateHelpToolTip(this.components);

            this.cbFocusOnly = new CheckBox
            {
                AutoSize = true,
                Cursor = Cursors.Hand,
                Location = new Point(138, 33),
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
                Cursor = Cursors.Hand,
                Location = new Point(138, 51),
                Name = "cbChatPause",
                Text = "Chat",
                UseVisualStyleBackColor = true,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.cbChatPause.CheckedChanged += OnChatPauseCheckedChanged;
            this.groupBox1.Controls.Add(this.cbChatPause);

            this.cbMiniHud = new CheckBox
            {
                AutoSize = true,
                Cursor = Cursors.Hand,
                Location = new Point(138, 69),
                Name = "cbMiniHud",
                Text = "Mini HUD",
                UseVisualStyleBackColor = true,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.cbMiniHud.CheckedChanged += OnMiniHudCheckedChanged;
            this.groupBox1.Controls.Add(this.cbMiniHud);

            RefreshLocalizedTooltipsAndMenu();
        }

        private void OnMiniHudCheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || this.cbMiniHud == null)
            {
                return;
            }

            Action<bool> handler = this.MiniHudCheckedChanged;
            if (handler != null)
            {
                handler(this.cbMiniHud.Checked);
            }
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
                if (this.cbMiniHud != null)
                {
                    this.optionsToolTip.SetToolTip(this.cbMiniHud, LocalizationManager.T("tooltip.mini_hud"));
                }
            }

            if (this.menuItem != null)
            {
                this.menuItem.Text = LocalizationManager.T("tray.close");
            }
        }

        private bool chatHookSubscribed;

        private void RegisterChatPauseHooks()
        {
            if (!this.chatHookSubscribed)
            {
                // Subscribe to PhysicalKeyDown (fires once per fresh physical non-injected, non-repeat key press)
                // so holding Enter (auto-repeat) or synthetic Enter keys can never desync the CHAT state.
                KeyboardHook.PhysicalKeyDown += OnPhysicalKeyDownForChat;
                this.chatHookSubscribed = true;
            }
        }

        private void OnPhysicalKeyDownForChat(Keys key)
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (this.lastKey != Keys.None && key == this.lastKey)
            {
                if (!ShouldHandleToggleHotkey())
                {
                    return;
                }

                MethodInvoker invokeToggle = delegate
                {
                    if (!this.IsDisposed)
                    {
                        using (SessionContext.Enter(this.session))
                        {
                            OnToggleHotkeyPressed();
                        }
                    }
                };

                if (this.InvokeRequired)
                {
                    this.BeginInvoke(invokeToggle);
                }
                else
                {
                    invokeToggle();
                }
                return;
            }

            using (SessionContext.Enter(this.session))
            {
                if (AutoRefreshCommand.IsSendingCommand)
                {
                    return;
                }

                if (key == Keys.Return)
                {
                    OnChatEnterKeyPressed();
                }
                else if (key == Keys.Escape)
                {
                    OnChatEscapeKeyPressed();
                }
            }
        }

        private bool ShouldHandleToggleHotkey()
        {
            if (!this.isEmbeddedTab)
            {
                return true;
            }

            SessionContext focusedGameSession = SessionContext.FindSessionWithFocusedGameWindow();
            if (focusedGameSession != null)
            {
                if (ReferenceEquals(focusedGameSession, this.session))
                {
                    return true;
                }

                Keys focusedToggleKey;
                Profile focusedProf = focusedGameSession.Profile;
                if (focusedProf != null && focusedProf.UserPreferences != null
                    && KeyHelper.TryParseFormsKeys(focusedProf.UserPreferences.toggleStateKey, out focusedToggleKey)
                    && focusedToggleKey == this.lastKey)
                {
                    return false;
                }

                return true;
            }

            SessionContext activeUi = SessionContext.ActiveUiSession;
            if (ReferenceEquals(activeUi, this.session))
            {
                return true;
            }

            Keys activeToggleKey;
            Profile activeProf = activeUi != null ? activeUi.Profile : null;
            if (activeProf != null && activeProf.UserPreferences != null
                && KeyHelper.TryParseFormsKeys(activeProf.UserPreferences.toggleStateKey, out activeToggleKey)
                && activeToggleKey == this.lastKey)
            {
                return false;
            }

            return true;
        }

        private bool OnChatEnterKeyPressed()
        {
            if (this.isApplicationOn
                && this.cbChatPause != null
                && this.cbChatPause.Checked
                && !KeyboardHook.Alt
                && !KeyboardHook.Control
                && !AutoRefreshCommand.IsSendingCommand)
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

                        this.txtStatusToggleKey.Text = currentToggleKey.ToString();
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
            using (SessionContext.Enter(this.session))
            {
                if (this.isApplicationOn && ChatPauseState.IsChatting)
                {
                    // Clicking the orange CHAT button resumes ON immediately instead of turning 4Tools off.
                    ChatPauseState.Resume();
                    return;
                }

                ToggleStatus();
            }
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

            using (SessionContext.Enter(this.session))
            {
                ProfileSingleton.GetCurrent().UserPreferences.toggleStateKey = currentToggleKey.ToString();
                ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().UserPreferences);
            }

            this.lastKey = currentToggleKey;
            RegisterChatPauseHooks();
        }

        private void OnAudioCheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            using (SessionContext.Enter(this.session))
            {
                ProfileSingleton.GetCurrent().UserPreferences.soundEnabled = this.cbAudio.Checked;
                ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().UserPreferences);
            }
        }

        private void OnFocusOnlyCheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            using (SessionContext.Enter(this.session))
            {
                ProfileSingleton.GetCurrent().UserPreferences.pauseWhenUnfocused = this.cbFocusOnly.Checked;
                ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().UserPreferences);
            }
        }

        private void OnChatPauseCheckedChanged(object sender, EventArgs e)
        {
            using (SessionContext.Enter(this.session))
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
        }

        private bool ToggleStatus()
        {
            using (SessionContext.Enter(this.session))
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
                        this.session.ResumeChat();
                        this.isApplicationOn = true;
                        this.session.IsAutomationOn = true;
                        if (!this.isEmbeddedTab)
                        {
                            this.notifyIconTray.Icon = Resources._4RTools.ETCResource.logo_4rtools_on;
                        }
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
            }

            return true;
        }

        private void ApplyOnVisualState()
        {
            if (this.session.IsChatting)
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
            this.session.ResumeChat();
            this.isApplicationOn = false;
            this.session.IsAutomationOn = false;
            this.btnStatusToggle.BackColor = Color.Red;
            this.btnStatusToggle.Text = "OFF";
            if (!this.isEmbeddedTab)
            {
                this.notifyIconTray.Icon = Resources._4RTools.ETCResource.logo_4rtools_off;
            }
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

        public void Shutdown()
        {
            if (this.chatHookSubscribed)
            {
                KeyboardHook.PhysicalKeyDown -= OnPhysicalKeyDownForChat;
                this.chatHookSubscribed = false;
            }
            this.session.ChatStateChanged -= OnChatPauseStateChanged;
            this.session.ResumeChat();
            this.session.IsAutomationOn = false;
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
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Shutdown();
            base.OnFormClosed(e);
        }
    }
}