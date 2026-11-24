using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    /// <summary>
    /// Top-level 3-in-1 multi-account shell window that hosts 3 independent AureumTools
    /// sessions (Account 1, Account 2, Account 3) in a single window with live status pills.
    /// </summary>
    public class MultiAccountShellForm : Form
    {
        public const int ACCOUNT_COUNT = 3;
        private const int TAB_BAR_HEIGHT = 40;

        private sealed class AccountTabButton : Control
        {
            private bool isActive;
            private bool isHovered;
            private bool isDarkMode;
            private Color dotColor = Color.FromArgb(158, 140, 102);

            public int SlotIndex { get; private set; }

            public bool IsActive
            {
                get { return this.isActive; }
                set
                {
                    if (this.isActive != value)
                    {
                        this.isActive = value;
                        Invalidate();
                    }
                }
            }

            public Color DotColor
            {
                get { return this.dotColor; }
                set
                {
                    if (this.dotColor != value)
                    {
                        this.dotColor = value;
                        Invalidate();
                    }
                }
            }

            public AccountTabButton(int slotIndex)
            {
                this.SlotIndex = slotIndex;
                this.DoubleBuffered = true;
                this.ResizeRedraw = true;
                this.Cursor = Cursors.Hand;
                this.Size = new Size(220, 32);
            }

            public void ApplyTheme(bool darkMode)
            {
                this.isDarkMode = darkMode;
                Invalidate();
            }

            protected override void OnTextChanged(EventArgs e)
            {
                base.OnTextChanged(e);
                Invalidate();
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                this.isHovered = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                this.isHovered = false;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Color barBg = this.isDarkMode ? Color.FromArgb(27, 19, 4) : Color.FromArgb(248, 238, 209);
                Color idleFill = this.isDarkMode ? Color.FromArgb(22, 15, 4) : Color.FromArgb(250, 244, 226);
                Color hoverFill = this.isDarkMode ? Color.FromArgb(42, 29, 8) : Color.FromArgb(242, 229, 194);
                Color activeFill = this.isDarkMode ? Color.FromArgb(56, 39, 10) : Color.FromArgb(255, 251, 238);
                Color border = this.isActive
                    ? (this.isDarkMode ? Color.FromArgb(212, 175, 55) : Color.FromArgb(166, 124, 0))
                    : (this.isDarkMode ? Color.FromArgb(72, 52, 16) : Color.FromArgb(212, 192, 152));
                Color accent = this.isDarkMode ? Color.FromArgb(244, 217, 122) : Color.FromArgb(160, 129, 35);
                Color textColor = this.isActive
                    ? (this.isDarkMode ? Color.FromArgb(255, 244, 207) : Color.FromArgb(52, 36, 8))
                    : (this.isDarkMode ? Color.FromArgb(206, 192, 158) : Color.FromArgb(88, 68, 36));

                using (SolidBrush bgOuter = new SolidBrush(barBg))
                {
                    g.FillRectangle(bgOuter, 0, 0, this.Width, this.Height);
                }

                Color fill = this.isActive ? activeFill : (this.isHovered ? hoverFill : idleFill);
                using (GraphicsPath pill = SidebarNavButton.CreateRoundedRect(new Rectangle(0, 1, this.Width - 1, this.Height - 2), 8))
                using (SolidBrush fillBrush = new SolidBrush(fill))
                using (Pen borderPen = new Pen(border, 1))
                {
                    g.FillPath(fillBrush, pill);
                    g.DrawPath(borderPen, pill);
                }

                if (this.isActive)
                {
                    using (SolidBrush accentBrush = new SolidBrush(accent))
                    {
                        g.FillRectangle(accentBrush, 10, this.Height - 3, this.Width - 20, 2);
                    }
                }

                using (SolidBrush dotBrush = new SolidBrush(this.dotColor))
                {
                    g.FillEllipse(dotBrush, 10, (this.Height - 8) / 2, 8, 8);
                }

                Rectangle textRect = new Rectangle(24, 0, this.Width - 30, this.Height - 1);
                TextRenderer.DrawText(
                    g,
                    this.Text,
                    this.isActive ? FontManager.Bold(9F) : FontManager.Regular(9F),
                    textRect,
                    textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            }
        }

        private readonly SessionContext[] sessions = new SessionContext[ACCOUNT_COUNT];
        private readonly Container[] containers = new Container[ACCOUNT_COUNT];
        private readonly AccountTabButton[] tabButtons = new AccountTabButton[ACCOUNT_COUNT];

        private Panel panelTabBar;
        private Panel panelTabDivider;
        private Panel panelHost;
        private Button btnStartAll;
        private Button btnStopAll;
        private ToolTip shellToolTip;
        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;
        private MenuItem trayCloseItem;

        private int activeTabIndex = 0;
        private bool isSyncingGlobalSettings;
        private bool isShuttingDown;

        public int ActiveAccountIndex
        {
            get { return this.activeTabIndex; }
        }

        public int AccountCount
        {
            get { return ACCOUNT_COUNT; }
        }

        public Container GetAccountContainer(int index)
        {
            return (index >= 0 && index < ACCOUNT_COUNT) ? this.containers[index] : null;
        }

        public SessionContext GetAccountSession(int index)
        {
            return (index >= 0 && index < ACCOUNT_COUNT) ? this.sessions[index] : null;
        }

        public MultiAccountShellForm()
        {
            AppSettings initialSettings = AppSettings.Load();
            LocalizationManager.SetLanguage(initialSettings.Language);

            this.Text = AppConfig.Name + " - " + AppConfig.Version;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size(868, 676 + TAB_BAR_HEIGHT);
            this.DoubleBuffered = true;
            this.Tag = ThemeManager.TAG_CANVAS;
            this.Icon = Resources._4RTools.ETCResource.logo_4rtools_off;

            this.shellToolTip = FormUtils.CreateHelpToolTip();

            BuildTabBar();
            BuildAccountContainers();
            BuildTrayIcon();

            SelectAccountTab(0);
            ApplyShellTheme(initialSettings.DarkMode);
            RefreshShellLanguage();
        }

        private void BuildTabBar()
        {
            this.panelTabBar = new Panel
            {
                Location = new Point(0, 0),
                Name = "panelAccountTabBar",
                Size = new Size(868, TAB_BAR_HEIGHT - 1),
                Tag = ThemeManager.TAG_TOPBAR
            };

            for (int i = 0; i < ACCOUNT_COUNT; i++)
            {
                int capturedIndex = i;
                AccountTabButton btn = new AccountTabButton(i)
                {
                    Location = new Point(8 + (i * 226), 4),
                    Name = "btnAccountTab" + (i + 1)
                };
                btn.Click += (s, e) => SelectAccountTab(capturedIndex);
                this.tabButtons[i] = btn;
                this.panelTabBar.Controls.Add(btn);
            }

            this.btnStartAll = new Button
            {
                Cursor = Cursors.Hand,
                Font = FontManager.Bold(8.5F),
                Location = new Point(690, 6),
                Name = "btnStartAllAccounts",
                Size = new Size(82, 27),
                Text = "Start All",
                UseVisualStyleBackColor = true
            };
            this.btnStartAll.Click += (s, e) => SetAllAccountsState(true);

            this.btnStopAll = new Button
            {
                Cursor = Cursors.Hand,
                Font = FontManager.Bold(8.5F),
                Location = new Point(778, 6),
                Name = "btnStopAllAccounts",
                Size = new Size(82, 27),
                Text = "Stop All",
                UseVisualStyleBackColor = true
            };
            this.btnStopAll.Click += (s, e) => SetAllAccountsState(false);

            this.panelTabBar.Controls.Add(this.btnStartAll);
            this.panelTabBar.Controls.Add(this.btnStopAll);

            this.panelTabDivider = new Panel
            {
                Location = new Point(0, TAB_BAR_HEIGHT - 1),
                Name = "panelTabBarDivider",
                Size = new Size(868, 1)
            };

            this.panelHost = new Panel
            {
                Location = new Point(0, TAB_BAR_HEIGHT),
                Name = "panelAccountHost",
                Size = new Size(868, 676)
            };

            this.Controls.Add(this.panelTabBar);
            this.Controls.Add(this.panelTabDivider);
            this.Controls.Add(this.panelHost);
        }

        private void BuildAccountContainers()
        {
            for (int i = 0; i < ACCOUNT_COUNT; i++)
            {
                int slotIndex = i;
                SessionContext session = new SessionContext(slotIndex);
                this.sessions[slotIndex] = session;

                Container container = new Container(session, true, () => GetAssignedProcessLabelsExcluding(slotIndex))
                {
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Location = new Point(0, 0),
                    Size = new Size(868, 676),
                    Dock = DockStyle.Fill,
                    Name = "AccountContainer" + (slotIndex + 1)
                };

                container.SessionStateChanged += (s, e) => UpdateTabHeader(slotIndex);
                container.DarkModeChangedByUser += dark => SyncThemeFromAccount(slotIndex, dark);
                container.LanguageChangedByUser += lang => SyncLanguageFromAccount(slotIndex, lang);
                container.UseBuffsInTownChangedByUser += allow => SyncUseBuffsInTownFromAccount(slotIndex, allow);
                container.VoteNotificationRequested += ShowVoteBalloonNotification;

                this.containers[slotIndex] = container;
                this.panelHost.Controls.Add(container);

                using (SessionContext.Enter(session))
                {
                    container.Show();
                    container.Visible = false;
                }
            }
        }

        private void BuildTrayIcon()
        {
            this.trayMenu = new ContextMenu();
            this.trayCloseItem = new MenuItem();
            this.trayCloseItem.Index = 0;
            this.trayCloseItem.Click += (s, e) => Close();
            this.trayMenu.MenuItems.Add(this.trayCloseItem);

            this.trayIcon = new NotifyIcon
            {
                ContextMenu = this.trayMenu,
                Icon = Resources._4RTools.ETCResource.logo_4rtools_off,
                Text = AppConfig.Name,
                Visible = true
            };
            this.trayIcon.DoubleClick += (s, e) => RestoreFromTray();
        }

        private HashSet<string> GetAssignedProcessLabelsExcluding(int excludedSlot)
        {
            HashSet<string> assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < ACCOUNT_COUNT; i++)
            {
                if (i == excludedSlot || this.containers[i] == null)
                {
                    continue;
                }

                string label = this.containers[i].SelectedProcessLabel;
                if (!string.IsNullOrWhiteSpace(label))
                {
                    assigned.Add(label);
                }
            }

            return assigned;
        }

        public void SelectAccountTab(int index)
        {
            if (index < 0 || index >= ACCOUNT_COUNT)
            {
                return;
            }

            this.activeTabIndex = index;
            SessionContext.ActiveUiSession = this.sessions[index];

            this.panelHost.SuspendLayout();
            try
            {
                for (int i = 0; i < ACCOUNT_COUNT; i++)
                {
                    bool selected = (i == index);
                    if (this.tabButtons[i] != null)
                    {
                        this.tabButtons[i].IsActive = selected;
                    }
                    if (this.containers[i] != null)
                    {
                        this.containers[i].Visible = selected;
                        if (selected)
                        {
                            this.containers[i].BringToFront();
                            if (!this.containers[i].IsAutomationOn)
                            {
                                using (SessionContext.Enter(this.sessions[i]))
                                {
                                    this.containers[i].refreshProfileList();
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                this.panelHost.ResumeLayout(true);
            }
        }

        public void SetAllAccountsState(bool turnOn)
        {
            for (int i = 0; i < ACCOUNT_COUNT; i++)
            {
                if (this.containers[i] != null)
                {
                    this.containers[i].SetAutomationState(turnOn);
                }
            }
            UpdateAllTabHeaders();
        }

        private void SyncThemeFromAccount(int sourceIndex, bool darkMode)
        {
            if (this.isSyncingGlobalSettings)
            {
                return;
            }

            this.isSyncingGlobalSettings = true;
            try
            {
                ApplyShellTheme(darkMode);
                for (int i = 0; i < ACCOUNT_COUNT; i++)
                {
                    if (i != sourceIndex && this.containers[i] != null)
                    {
                        this.containers[i].ApplyExternalTheme(darkMode);
                    }
                }
            }
            finally
            {
                this.isSyncingGlobalSettings = false;
            }
        }

        private void SyncLanguageFromAccount(int sourceIndex, string language)
        {
            if (this.isSyncingGlobalSettings)
            {
                return;
            }

            this.isSyncingGlobalSettings = true;
            try
            {
                LocalizationManager.SetLanguage(language);
                for (int i = 0; i < ACCOUNT_COUNT; i++)
                {
                    if (i != sourceIndex && this.containers[i] != null)
                    {
                        this.containers[i].ApplyExternalLanguage(language);
                    }
                }
                RefreshShellLanguage();
            }
            finally
            {
                this.isSyncingGlobalSettings = false;
            }
        }

        private void SyncUseBuffsInTownFromAccount(int sourceIndex, bool allowInTown)
        {
            if (this.isSyncingGlobalSettings)
            {
                return;
            }

            this.isSyncingGlobalSettings = true;
            try
            {
                for (int i = 0; i < ACCOUNT_COUNT; i++)
                {
                    if (i != sourceIndex && this.containers[i] != null)
                    {
                        this.containers[i].ApplyExternalUseBuffsInTown(allowInTown);
                    }
                }
            }
            finally
            {
                this.isSyncingGlobalSettings = false;
            }
        }

        private void ApplyShellTheme(bool darkMode)
        {
            Color barBg = darkMode ? Color.FromArgb(14, 9, 2) : Color.FromArgb(245, 236, 212);
            Color dividerColor = darkMode ? Color.FromArgb(89, 66, 20) : Color.FromArgb(212, 192, 152);
            Color btnBack = darkMode ? Color.FromArgb(42, 29, 8) : Color.FromArgb(253, 248, 232);
            Color btnFore = darkMode ? Color.FromArgb(244, 217, 122) : Color.FromArgb(62, 44, 20);

            this.BackColor = barBg;
            this.panelTabBar.BackColor = barBg;
            this.panelTabDivider.BackColor = dividerColor;

            for (int i = 0; i < ACCOUNT_COUNT; i++)
            {
                if (this.tabButtons[i] != null)
                {
                    this.tabButtons[i].ApplyTheme(darkMode);
                }
            }

            StyleActionButton(this.btnStartAll, btnBack, btnFore, dividerColor);
            StyleActionButton(this.btnStopAll, btnBack, btnFore, dividerColor);
        }

        private static void StyleActionButton(Button btn, Color back, Color fore, Color border)
        {
            if (btn == null)
            {
                return;
            }

            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = back;
            btn.ForeColor = fore;
            btn.FlatAppearance.BorderColor = border;
            btn.FlatAppearance.BorderSize = 1;
        }

        private void RefreshShellLanguage()
        {
            if (this.btnStartAll != null)
            {
                this.btnStartAll.Text = LocalizationManager.T("shell.start_all");
                this.shellToolTip.SetToolTip(this.btnStartAll, LocalizationManager.T("tooltip.shell_start_all"));
            }
            if (this.btnStopAll != null)
            {
                this.btnStopAll.Text = LocalizationManager.T("shell.stop_all");
                this.shellToolTip.SetToolTip(this.btnStopAll, LocalizationManager.T("tooltip.shell_stop_all"));
            }
            if (this.trayCloseItem != null)
            {
                this.trayCloseItem.Text = LocalizationManager.T("tray.close");
            }

            UpdateAllTabHeaders();
        }

        private void UpdateAllTabHeaders()
        {
            for (int i = 0; i < ACCOUNT_COUNT; i++)
            {
                UpdateTabHeader(i);
            }
        }

        private void UpdateTabHeader(int index)
        {
            if (this.IsDisposed || index < 0 || index >= ACCOUNT_COUNT)
            {
                return;
            }

            MethodInvoker apply = delegate
            {
                if (this.IsDisposed || this.tabButtons[index] == null || this.containers[index] == null)
                {
                    return;
                }

                Container c = this.containers[index];
                AccountTabButton btn = this.tabButtons[index];

                string slotLabel = LocalizationManager.T("shell.account_tab", index + 1);
                string charName = c.DetectedCharacterName;
                string statusText;

                if (c.IsAutomationOn && c.IsChatPaused)
                {
                    btn.DotColor = Color.FromArgb(245, 158, 11);
                    statusText = "CHAT";
                }
                else if (c.IsAutomationOn)
                {
                    btn.DotColor = Color.FromArgb(46, 204, 113);
                    statusText = "ON";
                }
                else
                {
                    btn.DotColor = Color.FromArgb(158, 140, 102);
                    statusText = "OFF";
                }

                btn.Text = string.IsNullOrWhiteSpace(charName)
                    ? string.Format("{0} · {1}", slotLabel, statusText)
                    : string.Format("{0}: {1} ({2})", slotLabel, charName, statusText);

                if (this.shellToolTip != null)
                {
                    this.shellToolTip.SetToolTip(btn, LocalizationManager.T("tooltip.shell_account_tab", index + 1));
                }

                UpdateTrayIconStatus();
            };

            if (this.InvokeRequired)
            {
                this.BeginInvoke(apply);
            }
            else
            {
                apply();
            }
        }

        private void UpdateTrayIconStatus()
        {
            if (this.trayIcon == null)
            {
                return;
            }

            bool anyOn = false;
            for (int i = 0; i < ACCOUNT_COUNT; i++)
            {
                if (this.containers[i] != null && this.containers[i].IsAutomationOn)
                {
                    anyOn = true;
                    break;
                }
            }

            this.trayIcon.Icon = anyOn
                ? Resources._4RTools.ETCResource.logo_4rtools_on
                : Resources._4RTools.ETCResource.logo_4rtools_off;
        }

        private void ShowVoteBalloonNotification(string message)
        {
            if (this.IsDisposed || this.trayIcon == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            try
            {
                this.trayIcon.ShowBalloonTip(
                    5000,
                    LocalizationManager.T("aureum.vote_alert_title"),
                    message,
                    ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                Logger.Warn("MultiAccountShellForm", "Failed to show vote reminder balloon", ex);
            }
        }

        private void RestoreFromTray()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_KEYMENU = 0xF100;
            if (m.Msg == WM_SYSCOMMAND && ((int)m.WParam & 0xFFF0) == SC_KEYMENU && m.LParam == IntPtr.Zero)
            {
                return;
            }

            base.WndProc(ref m);
        }

        private void ShutdownAllContainers()
        {
            if (this.isShuttingDown)
            {
                return;
            }

            this.isShuttingDown = true;
            for (int i = 0; i < ACCOUNT_COUNT; i++)
            {
                if (this.containers[i] != null && !this.containers[i].IsDisposed)
                {
                    this.containers[i].ShutdownEmbedded();
                }
            }
            KeyboardHook.Disable();
            SessionContext.ActiveUiSession = SessionContext.Default;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            ShutdownAllContainers();
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ShutdownAllContainers();
                if (this.trayIcon != null)
                {
                    this.trayIcon.Visible = false;
                    this.trayIcon.Dispose();
                    this.trayIcon = null;
                }
                if (this.trayMenu != null)
                {
                    this.trayMenu.Dispose();
                    this.trayMenu = null;
                }
                if (this.shellToolTip != null)
                {
                    this.shellToolTip.Dispose();
                    this.shellToolTip = null;
                }
                if (this.btnStartAll != null && this.btnStartAll.Font != null)
                {
                    this.btnStartAll.Font.Dispose();
                }
                if (this.btnStopAll != null && this.btnStopAll.Font != null)
                {
                    this.btnStopAll.Font.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
