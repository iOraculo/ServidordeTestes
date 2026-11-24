using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class Container : Form, IObserver
    {
        private readonly Subject subject = new Subject();
        private readonly SessionContext session;
        private readonly bool isEmbeddedTab;
        private readonly Timer clientMonitorTimer;
        private readonly Timer vitalsTimer;
        private readonly byte[] vitalsBuffer = new byte[Constants.HP_SP_BLOCK_BYTE_SIZE];
        private readonly byte[] statusRawBuffer = new byte[Constants.STATUS_BUFFER_BYTE_SIZE];
        private readonly uint[] statusCodeBuffer = new uint[Constants.MAX_BUFF_LIST_INDEX_SIZE];
        private readonly Dictionary<string, Form> sectionForms = new Dictionary<string, Form>(StringComparer.Ordinal);
        private readonly Dictionary<string, SidebarNavButton> navButtons = new Dictionary<string, SidebarNavButton>(StringComparer.Ordinal);
        private readonly List<Font> ownedFonts = new List<Font>();

        private AppSettings appSettings;
        private ToggleApplicationStateForm toggleForm;
        private ThemeToggleButton btnThemeToggle;
        private CheckBox cbUseBuffsInTown;
        private ComboBox cbLanguage;
        private VitalBar hpBar;
        private VitalBar spBar;
        private MiniHudForm miniHud;
        private ToolTip headerToolTip;
        private ProfileForm profileForm;
        private AureumHubForm aureumHubForm;
        private Func<HashSet<string>> assignedProcessProvider;

        public SessionContext Session { get { return this.session; } }
        public bool IsAutomationOn { get { return this.isAutomationOn; } }
        public bool IsChatPaused { get { return this.session.IsChatting; } }
        public string DetectedCharacterName { get { return this.lastDetectedCharacter; } }
        public string SelectedProcessLabel { get { return this.processCB != null ? (this.processCB.SelectedItem as string) : string.Empty; } }

        public event EventHandler SessionStateChanged;
        public event Action<bool> DarkModeChangedByUser;
        public event Action<string> LanguageChangedByUser;
        public event Action<bool> UseBuffsInTownChangedByUser;
        public event Action<string> VoteNotificationRequested;

        // Modern layout shell controls
        private Panel panelTopBar;
        private PictureBox pbBrandLogo;
        private Bitmap brandLogoOffBitmap;
        private Bitmap brandLogoOnBitmap;
        private Label lblBrandTitle;
        private Label lblBrandVersion;
        private Label lblTestServerBadge;
        private Panel panelSidebar;
        private ModernCardPanel cardQuickStatus;
        private StatusDot sessionStatusDot;
        private Label lblSessionStatus;
        private Label lblSessionProfile;
        private Label lblSessionClient;
        private ModernCardPanel cardAutopot;
        private Panel autopotHostPanel;
        private Panel autopotYggHostPanel;
        private AutopotForm autopotForm;
        private AutopotForm autopotYggForm;
        private ModernCardPanel cardStatus;
        private StatusDot statusDot;
        private ModernCardPanel cardSection;
        private Label lblSectionTitle;
        private Label lblSectionSubtitle;
        private Panel sectionHostPanel;
        private Panel panelModernFooter;
        private LinkLabel lblLinkWebsite;
        private LinkLabel lblLinkWiki;
        private LinkLabel lblLinkGithub;

        private string activeSectionKey = "spammer";
        private string currentProfile;
        private string lastDetectedCharacter = string.Empty;
        private bool lastPausedForTown;
        private bool isAutomationOn;
        private bool isApplyingSettings;
        private bool isShuttingDown;

        public Container()
            : this(SessionContext.Default, false, null)
        {
        }

        public Container(SessionContext session, bool isEmbeddedTab, Func<HashSet<string>> assignedProcessProvider = null)
        {
            this.session = session ?? SessionContext.Default;
            this.isEmbeddedTab = isEmbeddedTab;
            this.assignedProcessProvider = assignedProcessProvider;
            SessionContext.RegisterSession(this.session);

            using (SessionContext.Enter(this.session))
            {
                this.subject.Attach(this);
                this.appSettings = AppSettings.Load();
                LocalizationManager.SetLanguage(this.appSettings.Language);

                InitializeComponent();
                this.Text = AppConfig.Name + " - " + AppConfig.Version;
                this.TransparencyKey = Color.Empty;
                this.DoubleBuffered = true;
                this.Tag = ThemeManager.TAG_CANVAS;

                BuildModernLayout();

                // Monitor active Ragnarok client so macros stop automatically if the game closes.
                this.clientMonitorTimer = new Timer();
                this.clientMonitorTimer.Interval = 1500;
                this.clientMonitorTimer.Tick += ClientMonitorTimer_Tick;

                // Live HP/SP bars and character-change detection for profile auto-switch.
                this.vitalsTimer = new Timer();
                this.vitalsTimer.Interval = 300;
                this.vitalsTimer.Tick += VitalsTimer_Tick;

                this.session.ChatStateChanged += OnChatPauseStateChanged;
                KeyboardHook.PhysicalKeyDown += OnPhysicalPlayerKeyDown;

                // Embed all child forms inside the modern dashboard cards.
                SetToggleApplicationStateWindow();
                SetAutopotWindow();
                SetAutopotYggWindow();

                SetAHKWindow();
                SetDebuffRecoveryWindow();
                SetAutobuffSkillWindow();
                SetAutobuffStuffWindow();
                SetSkillTimerWindow();
                SetMacrosWindow();
                SetMountWindow();
                SetUtilitiesWindow();
                SetAureumHubWindow();
                SetProfileWindow();
                SetGuideWindow();

                SelectSection("spammer");
                ApplyAppSettingsToUi();
                UpdateSessionSummaryUi();
            }
        }

        public void SetAutomationState(bool turnOn)
        {
            if (this.toggleForm != null && !this.toggleForm.IsDisposed)
            {
                this.toggleForm.SetAutomationState(turnOn);
            }
        }

        public void ApplyExternalTheme(bool darkMode)
        {
            using (SessionContext.Enter(this.session))
            {
                this.isApplyingSettings = true;
                try
                {
                    this.appSettings.DarkMode = darkMode;
                    if (this.btnThemeToggle != null)
                    {
                        this.btnThemeToggle.IsDarkMode = darkMode;
                    }
                    ThemeManager.ApplyTheme(this, darkMode);
                    if (this.aureumHubForm != null && !this.aureumHubForm.IsDisposed)
                    {
                        this.aureumHubForm.ApplyCustomThemeStyling();
                    }
                    if (this.miniHud != null && !this.miniHud.IsDisposed)
                    {
                        this.miniHud.ApplyHudTheme(darkMode);
                    }
                }
                finally
                {
                    this.isApplyingSettings = false;
                }
            }
        }

        public void ApplyExternalLanguage(string language)
        {
            using (SessionContext.Enter(this.session))
            {
                this.isApplyingSettings = true;
                try
                {
                    this.appSettings.Language = language;
                    LocalizationManager.SetLanguage(language);
                    if (this.cbLanguage != null)
                    {
                        this.cbLanguage.SelectedItem = LocalizationManager.IsPortuguese ? "PT-BR" : "EN";
                    }
                    ApplyLanguageToUi();
                }
                finally
                {
                    this.isApplyingSettings = false;
                }
            }
        }

        public void ApplyExternalUseBuffsInTown(bool allowInTown)
        {
            using (SessionContext.Enter(this.session))
            {
                this.isApplyingSettings = true;
                try
                {
                    this.appSettings.UseBuffsInTown = allowInTown;
                    Client.AllowBuffsInTown = allowInTown;
                    if (this.cbUseBuffsInTown != null)
                    {
                        this.cbUseBuffsInTown.Checked = !allowInTown;
                    }
                    UpdateSessionSummaryUi();
                }
                finally
                {
                    this.isApplyingSettings = false;
                }
            }
        }

        public void ShutdownEmbedded()
        {
            ShutdownApplication(false);
        }

        private void RaiseSessionStateChanged()
        {
            EventHandler handler = this.SessionStateChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private Font TrackFont(string familyName, float emSize, FontStyle style)
        {
            return FontManager.Get(emSize, style);
        }

        private static Panel CreateDivider(Point location, Size size)
        {
            return new Panel
            {
                Location = location,
                Size = size
            };
        }

        private void BuildModernLayout()
        {
            this.SuspendLayout();
            try
            {
                this.ClientSize = new Size(868, 676);
                this.headerToolTip = FormUtils.CreateHelpToolTip();

                // Remove legacy Designer layout containers while keeping reusable Designer controls intact.
                HideLegacyControl(this.atkDefMode);
                HideLegacyControl(this.tabControlAutopot);
                HideLegacyControl(this.panel4);
                HideLegacyControl(this.panel5);
                HideLegacyControl(this.panelFooter);
                HideLegacyControl(this.panelDiscImage);
                HideLegacyControl(this.label1);

                Font brandTitleFont = TrackFont("Segoe UI", 12F, FontStyle.Bold);
                Font brandVersionFont = TrackFont("Segoe UI", 8.5F, FontStyle.Regular);
                Font uiRegularFont = TrackFont("Segoe UI", 9F, FontStyle.Regular);
                Font uiBoldFont = TrackFont("Segoe UI", 8.5F, FontStyle.Bold);
                Font sidebarGroupFont = TrackFont("Segoe UI", 8F, FontStyle.Bold);
                Font sectionTitleFont = TrackFont("Segoe UI", 11.5F, FontStyle.Bold);
                Font sectionSubtitleFont = TrackFont("Segoe UI", 8.75F, FontStyle.Regular);
                Font charNameFont = TrackFont("Segoe UI", 9.5F, FontStyle.Bold);
                Font footerFont = TrackFont("Segoe UI", 8.25F, FontStyle.Regular);

                LoadBrandLogos();

                // 1. Topbar (0, 0, 868 x 64)
                this.panelTopBar = new Panel
                {
                    Location = new Point(0, 0),
                    Name = "panelTopBar",
                    Size = new Size(868, 64),
                    Tag = ThemeManager.TAG_TOPBAR
                };

                this.pbBrandLogo = new PictureBox
                {
                    BackColor = Color.Transparent,
                    Image = this.brandLogoOffBitmap,
                    Location = new Point(12, 13),
                    Name = "pbBrandLogo",
                    Size = new Size(38, 38),
                    SizeMode = PictureBoxSizeMode.Zoom
                };

                this.lblBrandTitle = new Label
                {
                    AutoSize = true,
                    Font = brandTitleFont,
                    Location = new Point(56, 10),
                    Name = "lblBrandTitle",
                    Text = AppConfig.Name,
                    Tag = ThemeManager.TAG_ACCENT_TEXT
                };

                this.lblBrandVersion = new Label
                {
                    AutoSize = true,
                    Font = brandVersionFont,
                    Location = new Point(57, 37),
                    Name = "lblBrandVersion",
                    Text = AppConfig.Version,
                    Tag = ThemeManager.TAG_MUTED_TEXT
                };

                this.cbLanguage = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = uiRegularFont,
                    FormattingEnabled = true,
                    Location = new Point(116, 33),
                    Name = "cbLanguage",
                    Size = new Size(72, 23),
                    Tag = FormUtils.SKIP_RESET_TAG
                };
                this.cbLanguage.Items.Add("PT-BR");
                this.cbLanguage.Items.Add("EN");
                this.cbLanguage.SelectedIndexChanged += OnLanguageSelectedIndexChanged;

                // Reparent client selector controls
                this.lblProcessName.Font = uiBoldFont;
                this.lblProcessName.Location = new Point(222, 7);

                this.lblTestServerBadge = new Label
                {
                    AutoSize = false,
                    BackColor = Color.FromArgb(236, 200, 97),
                    ForeColor = Color.FromArgb(18, 12, 3),
                    Font = TrackFont("Segoe UI", 6.75F, FontStyle.Bold),
                    Location = new Point(358, 6),
                    Name = "lblTestServerBadge",
                    Size = new Size(90, 16),
                    Text = "MODO TESTE",
                    TextAlign = ContentAlignment.MiddleCenter,
                    Visible = false,
                    Tag = ThemeManager.PRESERVE_COLORS_TAG
                };

                this.processCB.DropDownStyle = ComboBoxStyle.DropDownList;
                this.processCB.Font = uiRegularFont;
                this.processCB.Location = new Point(222, 27);
                this.processCB.Size = new Size(194, 24);

                this.btnRefresh.Cursor = Cursors.Hand;
                this.btnRefresh.ImageAlign = ContentAlignment.MiddleCenter;
                this.btnRefresh.Location = new Point(420, 26);
                this.btnRefresh.Size = new Size(28, 25);

                // Reparent profile selector controls
                this.labelProfile.Font = uiBoldFont;
                this.labelProfile.Location = new Point(460, 7);

                this.profileCB.DropDownStyle = ComboBoxStyle.DropDownList;
                this.profileCB.Font = uiRegularFont;
                this.profileCB.Location = new Point(460, 27);
                this.profileCB.Size = new Size(172, 24);

                // Compact preferences block
                this.cbUseBuffsInTown = new CheckBox
                {
                    AutoSize = true,
                    Checked = true,
                    Cursor = Cursors.Hand,
                    Font = uiRegularFont,
                    Location = new Point(650, 30),
                    Name = "cbUseBuffsInTown",
                    Text = "Pause in towns",
                    UseVisualStyleBackColor = true,
                    Tag = FormUtils.SKIP_RESET_TAG
                };
                this.cbUseBuffsInTown.CheckedChanged += OnUseBuffsInTownCheckedChanged;

                this.btnThemeToggle = new ThemeToggleButton
                {
                    Location = new Point(818, 22),
                    Name = "btnThemeToggle",
                    Size = new Size(34, 32),
                    Tag = FormUtils.SKIP_RESET_TAG
                };
                this.btnThemeToggle.Click += OnThemeToggleClicked;
                CenterPauseInTownCheckbox();

                this.panelTopBar.Controls.Add(this.pbBrandLogo);
                this.panelTopBar.Controls.Add(this.lblBrandTitle);
                this.panelTopBar.Controls.Add(this.lblBrandVersion);
                this.panelTopBar.Controls.Add(this.cbLanguage);
                this.panelTopBar.Controls.Add(CreateDivider(new Point(207, 10), new Size(1, 44)));
                this.panelTopBar.Controls.Add(this.lblProcessName);
                this.panelTopBar.Controls.Add(this.lblTestServerBadge);
                this.panelTopBar.Controls.Add(this.processCB);
                this.panelTopBar.Controls.Add(this.btnRefresh);
                this.panelTopBar.Controls.Add(this.labelProfile);
                this.panelTopBar.Controls.Add(this.profileCB);
                this.panelTopBar.Controls.Add(this.cbUseBuffsInTown);
                this.panelTopBar.Controls.Add(this.btnThemeToggle);
                this.panelTopBar.Controls.Add(CreateDivider(new Point(0, 63), new Size(868, 1)));

                // 2. Left Sidebar (0, 64, 208 x 582)
                this.panelSidebar = new Panel
                {
                    Location = new Point(0, 64),
                    Name = "panelSidebar",
                    Size = new Size(208, 582),
                    Tag = ThemeManager.TAG_SIDEBAR
                };

                Label lblGroupCombat = new Label
                {
                    AutoSize = true,
                    Font = sidebarGroupFont,
                    Location = new Point(14, 12),
                    Name = "lblGroupCombat",
                    Text = "COMBAT & AUTOMATION",
                    Tag = ThemeManager.TAG_MUTED_TEXT
                };
                this.panelSidebar.Controls.Add(lblGroupCombat);

                int y = 32;
                AddSidebarButton("spammer", "Skill Spammer", SidebarIconKind.Spammer, ref y);
                AddSidebarButton("debuff", "Anti-Debuffs", SidebarIconKind.Debuffs, ref y);
                AddSidebarButton("buff_skills", "Auto-skill", SidebarIconKind.BuffSkills, ref y);
                AddSidebarButton("buff_stuffs", "Consumables", SidebarIconKind.BuffItems, ref y);
                AddSidebarButton("timers", "Skill timers", SidebarIconKind.Timers, ref y);
                AddSidebarButton("macros", "Macros", SidebarIconKind.MacroSwitch, ref y);
                AddSidebarButton("mount", "Mount", SidebarIconKind.Mount, ref y);
                AddSidebarButton("utilities", "Utilities", SidebarIconKind.Utilities, ref y);

                this.panelSidebar.Controls.Add(CreateDivider(new Point(14, 312), new Size(180, 1)));

                Label lblGroupManagement = new Label
                {
                    AutoSize = true,
                    Font = sidebarGroupFont,
                    Location = new Point(14, 320),
                    Name = "lblGroupManagement",
                    Text = "MANAGEMENT",
                    Tag = ThemeManager.TAG_MUTED_TEXT
                };
                this.panelSidebar.Controls.Add(lblGroupManagement);

                y = 339;
                AddSidebarButton("aureum", "Aureum Hub", SidebarIconKind.AureumHub, ref y);
                AddSidebarButton("profiles", "Profiles", SidebarIconKind.Profiles, ref y);
                AddSidebarButton("guide", "Guide / Help", SidebarIconKind.Guide, ref y);

                // Live Session Summary Card anchored at the bottom of the sidebar
                this.cardQuickStatus = new ModernCardPanel
                {
                    Location = new Point(10, 476),
                    Name = "cardQuickStatus",
                    Size = new Size(188, 92)
                };

                this.sessionStatusDot = new StatusDot
                {
                    Location = new Point(11, 15),
                    Name = "sessionStatusDot"
                };

                this.lblSessionStatus = new Label
                {
                    AutoEllipsis = true,
                    AutoSize = false,
                    Font = uiBoldFont,
                    Location = new Point(26, 11),
                    Name = "lblSessionStatus",
                    Size = new Size(152, 19),
                    Text = "Ready (OFF)"
                };

                this.lblSessionProfile = new Label
                {
                    AutoEllipsis = true,
                    AutoSize = false,
                    Font = uiRegularFont,
                    Location = new Point(10, 42),
                    Name = "lblSessionProfile",
                    Size = new Size(168, 19),
                    Text = "Profile: Default"
                };

                this.lblSessionClient = new Label
                {
                    AutoEllipsis = true,
                    AutoSize = false,
                    Font = footerFont,
                    Location = new Point(10, 64),
                    Name = "lblSessionClient",
                    Size = new Size(168, 18),
                    Text = "Client: not selected",
                    Tag = ThemeManager.TAG_MUTED_TEXT
                };

                this.cardQuickStatus.Controls.Add(this.sessionStatusDot);
                this.cardQuickStatus.Controls.Add(this.lblSessionStatus);
                this.cardQuickStatus.Controls.Add(CreateDivider(new Point(10, 34), new Size(168, 1)));
                this.cardQuickStatus.Controls.Add(this.lblSessionProfile);
                this.cardQuickStatus.Controls.Add(this.lblSessionClient);

                this.panelSidebar.Controls.Add(this.cardQuickStatus);
                this.panelSidebar.Controls.Add(CreateDivider(new Point(207, 0), new Size(1, 582)));

                // 3. Dashboard Row - Card 1: Unified Autopot & Yggdrasil (222, 76, 346 x 198)
                this.cardAutopot = new ModernCardPanel
                {
                    Location = new Point(222, 76),
                    Name = "cardAutopot",
                    Size = new Size(346, 198)
                };

                this.autopotHostPanel = new Panel
                {
                    Location = new Point(8, 6),
                    Name = "autopotHostPanel",
                    Size = new Size(330, 90)
                };

                this.autopotYggHostPanel = new Panel
                {
                    Location = new Point(8, 102),
                    Name = "autopotYggHostPanel",
                    Size = new Size(330, 90)
                };

                this.cardAutopot.Controls.Add(this.autopotHostPanel);
                this.cardAutopot.Controls.Add(CreateDivider(new Point(14, 99), new Size(318, 1)));
                this.cardAutopot.Controls.Add(this.autopotYggHostPanel);

                // 4. Dashboard Row - Card 2: ON/OFF & Live Vitals (578, 76, 276 x 198)
                this.cardStatus = new ModernCardPanel
                {
                    Location = new Point(578, 76),
                    Name = "cardStatus",
                    Size = new Size(276, 198)
                };

                this.OnOffPanel.Location = new Point(12, 6);
                this.OnOffPanel.Size = new Size(252, 116);

                this.statusDot = new StatusDot
                {
                    Location = new Point(14, 136),
                    Name = "statusDot"
                };

                this.lblCharacterName.AutoSize = true;
                this.lblCharacterName.Font = uiRegularFont;
                this.lblCharacterName.Location = new Point(28, 132);

                this.characterName.AutoEllipsis = true;
                this.characterName.AutoSize = false;
                this.characterName.Font = charNameFont;
                this.characterName.Location = new Point(110, 131);
                this.characterName.Size = new Size(152, 19);

                this.hpBar = new VitalBar("HP", Color.FromArgb(220, 76, 76))
                {
                    Location = new Point(14, 154),
                    Name = "hpBar",
                    Size = new Size(248, 17)
                };

                this.spBar = new VitalBar("SP", Color.FromArgb(66, 135, 245))
                {
                    Location = new Point(14, 175),
                    Name = "spBar",
                    Size = new Size(248, 17)
                };

                this.cardStatus.Controls.Add(this.OnOffPanel);
                this.cardStatus.Controls.Add(CreateDivider(new Point(14, 126), new Size(248, 1)));
                this.cardStatus.Controls.Add(this.statusDot);
                this.cardStatus.Controls.Add(this.lblCharacterName);
                this.cardStatus.Controls.Add(this.characterName);
                this.cardStatus.Controls.Add(this.hpBar);
                this.cardStatus.Controls.Add(this.spBar);

                // 5. Active Section Card (222, 284, 632 x 352)
                this.cardSection = new ModernCardPanel
                {
                    Location = new Point(222, 284),
                    Name = "cardSection",
                    Size = new Size(632, 352)
                };

                this.lblSectionTitle = new Label
                {
                    AutoEllipsis = true,
                    AutoSize = false,
                    Font = sectionTitleFont,
                    Location = new Point(14, 9),
                    Name = "lblSectionTitle",
                    Size = new Size(604, 22)
                };

                this.lblSectionSubtitle = new Label
                {
                    AutoEllipsis = true,
                    AutoSize = false,
                    Font = sectionSubtitleFont,
                    Location = new Point(14, 32),
                    Name = "lblSectionSubtitle",
                    Size = new Size(604, 18),
                    Tag = ThemeManager.TAG_MUTED_TEXT
                };

                this.sectionHostPanel = new Panel
                {
                    Location = new Point(8, 55),
                    Name = "sectionHostPanel",
                    Size = new Size(616, 289)
                };

                this.cardSection.Controls.Add(this.lblSectionTitle);
                this.cardSection.Controls.Add(this.lblSectionSubtitle);
                this.cardSection.Controls.Add(CreateDivider(new Point(14, 52), new Size(604, 1)));
                this.cardSection.Controls.Add(this.sectionHostPanel);

                // 6. Modern Footer (0, 646, 868 x 30)
                this.panelModernFooter = new Panel
                {
                    Location = new Point(0, 646),
                    Name = "panelModernFooter",
                    Size = new Size(868, 30),
                    Tag = ThemeManager.TAG_FOOTER
                };

                this.lbPowered.AutoSize = true;
                this.lbPowered.Font = footerFont;
                this.lbPowered.Location = new Point(14, 7);
                this.lbPowered.Tag = ThemeManager.TAG_MUTED_TEXT;

                this.lblLinkWebsite = new LinkLabel
                {
                    AutoSize = true,
                    Font = uiRegularFont,
                    LinkBehavior = LinkBehavior.HoverUnderline,
                    Location = new Point(628, 7),
                    Name = "lblLinkWebsite",
                    Text = "Site"
                };
                this.lblLinkWebsite.LinkClicked += websiteLinkLabel_LinkClicked;

                this.lblLinkWiki = new LinkLabel
                {
                    AutoSize = true,
                    Font = uiRegularFont,
                    LinkBehavior = LinkBehavior.HoverUnderline,
                    Location = new Point(688, 7),
                    Name = "lblLinkWiki",
                    Text = "Wiki"
                };
                this.lblLinkWiki.LinkClicked += lblLinkWiki_LinkClicked;

                this.lblLinkDiscord.AutoSize = true;
                this.lblLinkDiscord.Font = uiRegularFont;
                this.lblLinkDiscord.LinkBehavior = LinkBehavior.HoverUnderline;
                this.lblLinkDiscord.Location = new Point(734, 7);
                this.lblLinkDiscord.Text = "Discord";

                this.lblLinkGithub = new LinkLabel
                {
                    AutoSize = true,
                    Font = uiRegularFont,
                    LinkBehavior = LinkBehavior.HoverUnderline,
                    Location = new Point(798, 7),
                    Name = "lblLinkGithub",
                    Text = "GitHub"
                };
                this.lblLinkGithub.LinkClicked += lblLinkGithub_LinkClicked;

                this.panelModernFooter.Controls.Add(CreateDivider(new Point(0, 0), new Size(868, 1)));
                this.panelModernFooter.Controls.Add(this.lbPowered);
                this.panelModernFooter.Controls.Add(this.lblLinkWebsite);
                this.panelModernFooter.Controls.Add(this.lblLinkWiki);
                this.panelModernFooter.Controls.Add(this.lblLinkDiscord);
                this.panelModernFooter.Controls.Add(this.lblLinkGithub);

                this.Controls.Add(this.panelTopBar);
                this.Controls.Add(this.panelSidebar);
                this.Controls.Add(this.cardAutopot);
                this.Controls.Add(this.cardStatus);
                this.Controls.Add(this.cardSection);
                this.Controls.Add(this.panelModernFooter);
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        private void HideLegacyControl(Control control)
        {
            if (control == null)
            {
                return;
            }

            control.Visible = false;
            if (control.Parent != null)
            {
                control.Parent.Controls.Remove(control);
            }
        }

        private void LoadBrandLogos()
        {
            try
            {
                this.Icon = Resources._4RTools.ETCResource.logo_4rtools_on;
                using (Icon offIcon = Resources._4RTools.ETCResource.logo_4rtools_off)
                {
                    this.brandLogoOffBitmap = offIcon.ToBitmap();
                }
                using (Icon onIcon = Resources._4RTools.ETCResource.logo_4rtools_on)
                {
                    this.brandLogoOnBitmap = onIcon.ToBitmap();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Container", "Failed to load brand logo icons", ex);
            }
        }

        private void AddSidebarButton(string sectionKey, string label, SidebarIconKind iconKind, ref int y)
        {
            SidebarNavButton btn = new SidebarNavButton(sectionKey, label, iconKind)
            {
                Location = new Point(10, y),
                Name = "nav_" + sectionKey
            };
            btn.Click += delegate { SelectSection(sectionKey); };

            this.navButtons[sectionKey] = btn;
            this.panelSidebar.Controls.Add(btn);
            y += 34;
        }

        private void SelectSection(string sectionKey)
        {
            Form target;
            if (!this.sectionForms.TryGetValue(sectionKey, out target))
            {
                return;
            }

            this.activeSectionKey = sectionKey;
            this.sectionHostPanel.SuspendLayout();
            try
            {
                foreach (KeyValuePair<string, Form> pair in this.sectionForms)
                {
                    bool isTarget = string.Equals(pair.Key, sectionKey, StringComparison.Ordinal);
                    if (isTarget)
                    {
                        pair.Value.Visible = true;
                        pair.Value.BringToFront();
                    }
                    else if (pair.Value.Visible)
                    {
                        pair.Value.Visible = false;
                    }
                }

                foreach (KeyValuePair<string, SidebarNavButton> pair in this.navButtons)
                {
                    pair.Value.IsActive = string.Equals(pair.Key, sectionKey, StringComparison.Ordinal);
                }

                UpdateSectionHeader();
            }
            finally
            {
                this.sectionHostPanel.ResumeLayout(true);
            }
        }

        private void UpdateSectionHeader()
        {
            if (this.lblSectionTitle == null || this.lblSectionSubtitle == null)
            {
                return;
            }

            this.lblSectionTitle.Text = LocalizationManager.T("section." + this.activeSectionKey + ".title");
            this.lblSectionSubtitle.Text = LocalizationManager.T("section." + this.activeSectionKey + ".subtitle");
        }

        private void PositionCharacterNameLabel()
        {
            if (this.lblCharacterName == null || this.characterName == null || this.cardStatus == null)
            {
                return;
            }

            int startX = this.lblCharacterName.Right + 4;
            int availableWidth = Math.Max(40, this.cardStatus.Width - 14 - startX);
            this.characterName.Location = new Point(startX, 131);
            this.characterName.Size = new Size(availableWidth, 19);
        }

        private void UpdateSessionSummaryUi()
        {
            if (this.pbBrandLogo != null)
            {
                this.pbBrandLogo.Image = this.isAutomationOn ? this.brandLogoOnBitmap : this.brandLogoOffBitmap;
            }

            using (SessionContext.Enter(this.session))
            {
                Client activeClient = ClientSingleton.GetClient();
                bool hasClient = activeClient != null && activeClient.IsProcessAlive();

                if (this.statusDot != null)
                {
                    this.statusDot.IsConnected = hasClient;
                }

                if (this.sessionStatusDot != null && this.lblSessionStatus != null)
                {
                    if (this.isAutomationOn && this.session.IsChatting)
                    {
                        this.sessionStatusDot.CustomColor = Color.FromArgb(245, 158, 11);
                        this.lblSessionStatus.Text = LocalizationManager.T("sidebar.status_chat");
                    }
                    else if (this.isAutomationOn && hasClient && activeClient.ShouldPauseForTown())
                    {
                        this.sessionStatusDot.CustomColor = Color.FromArgb(244, 217, 122);
                        this.lblSessionStatus.Text = LocalizationManager.T("sidebar.status_town");
                    }
                    else if (this.isAutomationOn)
                    {
                        this.sessionStatusDot.CustomColor = Color.FromArgb(46, 204, 113);
                        this.lblSessionStatus.Text = LocalizationManager.T("sidebar.status_on");
                    }
                    else
                    {
                        this.sessionStatusDot.CustomColor = Color.FromArgb(158, 140, 102);
                        this.lblSessionStatus.Text = LocalizationManager.T("sidebar.status_off");
                    }
                }

                if (this.lblSessionProfile != null)
                {
                    string profName = !string.IsNullOrWhiteSpace(this.currentProfile)
                        ? this.currentProfile
                        : ProfileSingleton.GetCurrent().Name;
                    this.lblSessionProfile.Text = LocalizationManager.T("sidebar.profile_prefix", profName);
                }

                if (this.lblTestServerBadge != null)
                {
                    this.lblTestServerBadge.Visible = hasClient && ClientUpdaterForm.IsBuiltInTestServerName(activeClient.processName);
                }

                if (this.lblSessionClient != null)
                {
                    if (hasClient)
                    {
                        string currentMap = activeClient.ReadCurrentMap();
                        string clientLabel = string.IsNullOrWhiteSpace(currentMap)
                            ? activeClient.processName
                            : string.Format("{0} ({1})", activeClient.processName, currentMap);
                        this.lblSessionClient.Text = LocalizationManager.T("sidebar.client_connected", clientLabel);
                    }
                    else
                    {
                        this.lblSessionClient.Text = LocalizationManager.T("sidebar.client_none");
                    }
                }
            }

            RaiseSessionStateChanged();
        }

        private void ApplyAppSettingsToUi()
        {
            this.isApplyingSettings = true;
            try
            {
                LocalizationManager.SetLanguage(this.appSettings.Language);
                this.cbLanguage.SelectedItem = LocalizationManager.IsPortuguese ? "PT-BR" : "EN";
                if (this.btnThemeToggle != null)
                {
                    this.btnThemeToggle.IsDarkMode = this.appSettings.DarkMode;
                }
                this.cbUseBuffsInTown.Checked = !this.appSettings.UseBuffsInTown;
                if (this.toggleForm != null)
                {
                    this.toggleForm.SetMiniHudChecked(this.appSettings.ShowMiniHud);
                }
                Client.AllowBuffsInTown = this.appSettings.UseBuffsInTown;
                ApplyLanguageToUi();
                ThemeManager.ApplyTheme(this, this.appSettings.DarkMode);
                if (this.aureumHubForm != null && !this.aureumHubForm.IsDisposed)
                {
                    this.aureumHubForm.ApplyCustomThemeStyling();
                }
                SetMiniHudVisible(this.appSettings.ShowMiniHud);
            }
            finally
            {
                this.isApplyingSettings = false;
            }
        }

        private void ApplyLanguageToUi()
        {
            LocalizationManager.ApplyToControlTree(this);
            CenterPauseInTownCheckbox();
            UpdateSectionHeader();
            UpdateSessionSummaryUi();
            PositionCharacterNameLabel();
            if (this.lbPowered != null)
            {
                this.lbPowered.Text = LocalizationManager.T("footer.powered");
            }
            if (this.lblLinkWebsite != null)
            {
                this.lblLinkWebsite.Text = LocalizationManager.T("footer.website");
            }
            if (this.lblLinkWiki != null)
            {
                this.lblLinkWiki.Text = LocalizationManager.T("footer.wiki");
            }
            RefreshHeaderTooltips();
            if (this.profileForm != null)
            {
                this.profileForm.RefreshCharacterBindingUi();
                this.profileForm.RefreshTooltips();
            }
            this.subject.Notify(new Utils.Message(MessageCode.LANGUAGE_CHANGED, LocalizationManager.CurrentLanguage));
        }

        private void RefreshHeaderTooltips()
        {
            if (this.headerToolTip == null)
            {
                return;
            }

            this.headerToolTip.SetToolTip(this.processCB, LocalizationManager.T("tooltip.process_select"));
            this.headerToolTip.SetToolTip(this.lblProcessName, LocalizationManager.T("tooltip.process_select"));
            this.headerToolTip.SetToolTip(this.profileCB, LocalizationManager.T("tooltip.profile_select"));
            this.headerToolTip.SetToolTip(this.labelProfile, LocalizationManager.T("tooltip.profile_select"));
            if (this.hpBar != null)
            {
                this.headerToolTip.SetToolTip(this.hpBar, LocalizationManager.T("tooltip.vital_hp"));
            }
            if (this.spBar != null)
            {
                this.headerToolTip.SetToolTip(this.spBar, LocalizationManager.T("tooltip.vital_sp"));
            }
            if (this.cardQuickStatus != null)
            {
                string sessionTip = LocalizationManager.T("tooltip.session_card");
                this.headerToolTip.SetToolTip(this.cardQuickStatus, sessionTip);
                this.headerToolTip.SetToolTip(this.lblSessionStatus, sessionTip);
                this.headerToolTip.SetToolTip(this.lblSessionProfile, sessionTip);
                this.headerToolTip.SetToolTip(this.lblSessionClient, sessionTip);
            }

            foreach (KeyValuePair<string, SidebarNavButton> nav in this.navButtons)
            {
                this.headerToolTip.SetToolTip(nav.Value, LocalizationManager.T("tooltip.nav_" + nav.Key));
            }

            if (this.btnThemeToggle != null)
            {
                this.headerToolTip.SetToolTip(this.btnThemeToggle, LocalizationManager.T("tooltip.dark_mode"));
            }
            this.headerToolTip.SetToolTip(this.cbUseBuffsInTown, LocalizationManager.T("tooltip.use_buffs_in_town"));
            this.headerToolTip.SetToolTip(this.cbLanguage, LocalizationManager.T("tooltip.language"));
            this.headerToolTip.SetToolTip(this.btnRefresh, LocalizationManager.T("tooltip.refresh_clients"));
            if (this.lblLinkWebsite != null)
            {
                this.headerToolTip.SetToolTip(this.lblLinkWebsite, LocalizationManager.T("tooltip.website"));
            }
            if (this.lblLinkWiki != null)
            {
                this.headerToolTip.SetToolTip(this.lblLinkWiki, LocalizationManager.T("tooltip.wiki"));
            }
            this.headerToolTip.SetToolTip(this.lblLinkDiscord, LocalizationManager.T("tooltip.discord"));
            if (this.lblLinkGithub != null)
            {
                this.headerToolTip.SetToolTip(this.lblLinkGithub, LocalizationManager.T("tooltip.github"));
            }
        }

        private void OnLanguageSelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.isApplyingSettings || this.cbLanguage.SelectedItem == null)
            {
                return;
            }

            string selected = this.cbLanguage.SelectedItem.ToString();
            this.appSettings.Language = string.Equals(selected, "EN", StringComparison.OrdinalIgnoreCase)
                ? LocalizationManager.LANG_EN_US
                : LocalizationManager.LANG_PT_BR;

            LocalizationManager.SetLanguage(this.appSettings.Language);
            ApplyLanguageToUi();
            AppSettings.Save(this.appSettings);
            Action<string> langChanged = this.LanguageChangedByUser;
            if (langChanged != null)
            {
                langChanged(this.appSettings.Language);
            }
        }

        public void ReloadAppSettings()
        {
            this.appSettings = AppSettings.Load();
            ApplyAppSettingsToUi();
            if (this.profileForm != null)
            {
                this.profileForm.RefreshCharacterBindingUi();
            }
        }

        public void NotifyServersRestored()
        {
            this.subject.Notify(new Utils.Message(MessageCode.SERVER_LIST_CHANGED, "Servers Restored"));
        }

        private void OnThemeToggleClicked(object sender, EventArgs e)
        {
            if (this.isApplyingSettings || this.btnThemeToggle == null)
            {
                return;
            }

            this.appSettings.DarkMode = !this.appSettings.DarkMode;
            this.btnThemeToggle.IsDarkMode = this.appSettings.DarkMode;
            ThemeManager.ApplyTheme(this, this.appSettings.DarkMode);
            if (this.aureumHubForm != null && !this.aureumHubForm.IsDisposed)
            {
                this.aureumHubForm.ApplyCustomThemeStyling();
            }
            if (this.miniHud != null && !this.miniHud.IsDisposed)
            {
                this.miniHud.ApplyHudTheme(this.appSettings.DarkMode);
            }
            AppSettings.Save(this.appSettings);
            Action<bool> darkChanged = this.DarkModeChangedByUser;
            if (darkChanged != null)
            {
                darkChanged(this.appSettings.DarkMode);
            }
        }

        private void CenterPauseInTownCheckbox()
        {
            if (this.cbUseBuffsInTown == null || this.profileCB == null || this.btnThemeToggle == null)
            {
                return;
            }

            Size pref = this.cbUseBuffsInTown.PreferredSize;
            int leftBound = this.profileCB.Right;
            int rightBound = this.btnThemeToggle.Left;
            int centeredX = leftBound + Math.Max(6, (rightBound - leftBound - pref.Width) / 2);
            int centeredY = this.profileCB.Top + ((this.profileCB.Height - pref.Height) / 2);
            this.cbUseBuffsInTown.Location = new Point(centeredX, centeredY);
        }

        private void OnUseBuffsInTownCheckedChanged(object sender, EventArgs e)
        {
            if (this.isApplyingSettings)
            {
                return;
            }

            this.appSettings.UseBuffsInTown = !this.cbUseBuffsInTown.Checked;
            Client.AllowBuffsInTown = this.appSettings.UseBuffsInTown;
            UpdateSessionSummaryUi();
            SyncMiniHudState(this.session.Client);
            AppSettings.Save(this.appSettings);
            Action<bool> townChanged = this.UseBuffsInTownChangedByUser;
            if (townChanged != null)
            {
                townChanged(this.appSettings.UseBuffsInTown);
            }
        }

        private void OnMiniHudToggled(bool isVisible)
        {
            if (this.isApplyingSettings)
            {
                return;
            }

            this.appSettings.ShowMiniHud = isVisible;
            SetMiniHudVisible(this.appSettings.ShowMiniHud);
            AppSettings.Save(this.appSettings);
        }

        private void SyncMiniHudState(Client client)
        {
            if (this.miniHud == null || this.miniHud.IsDisposed)
            {
                return;
            }

            bool hasLiveClient = client != null && client.HasMemorySupport && client.IsProcessAlive();
            string currentMap = hasLiveClient ? client.ReadCurrentMap() : string.Empty;
            int activeBuffs = hasLiveClient ? client.CountActiveBuffs(this.statusRawBuffer, this.statusCodeBuffer) : 0;
            bool isTownPaused = hasLiveClient && client.ShouldPauseForTown();
            bool isUnfocused = hasLiveClient && client.ShouldPauseForFocus();

            this.miniHud.UpdateStatus(this.isAutomationOn, this.session.IsChatting, isTownPaused, isUnfocused);
            this.miniHud.UpdateTelemetry(currentMap, activeBuffs);
        }

        private void SetMiniHudVisible(bool visible)
        {
            if (visible)
            {
                if (this.miniHud == null || this.miniHud.IsDisposed)
                {
                    this.miniHud = new MiniHudForm();
                    this.miniHud.CloseRequested += OnMiniHudCloseRequested;
                    this.miniHud.PositionChanged += OnMiniHudPositionChanged;
                    this.miniHud.SetSavedPosition(this.appSettings.MiniHudX, this.appSettings.MiniHudY);
                }

                this.miniHud.ApplyHudTheme(this.appSettings.DarkMode);
                Client activeClient = this.session.Client;
                this.miniHud.UpdateIdentity(this.currentProfile ?? ProfileSingleton.GetCurrent().Name, this.characterName.Text);
                SyncMiniHudState(activeClient);
                if (!this.miniHud.Visible)
                {
                    this.miniHud.Show();
                }

                UpdateVitalsDisplay(activeClient);
            }
            else if (this.miniHud != null && !this.miniHud.IsDisposed && this.miniHud.Visible)
            {
                this.miniHud.Hide();
            }
        }

        private void OnMiniHudCloseRequested(object sender, EventArgs e)
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (this.toggleForm != null && !this.toggleForm.IsDisposed)
            {
                this.toggleForm.SetMiniHudChecked(false);
            }
            OnMiniHudToggled(false);
        }

        private void OnMiniHudPositionChanged(object sender, EventArgs e)
        {
            if (this.miniHud == null || this.miniHud.IsDisposed || this.appSettings == null)
            {
                return;
            }

            this.appSettings.MiniHudX = this.miniHud.Location.X;
            this.appSettings.MiniHudY = this.miniHud.Location.Y;
            AppSettings.Save(this.appSettings);
        }

        private void OnChatPauseStateChanged(object sender, EventArgs e)
        {
            if (this.IsDisposed || !this.IsHandleCreated)
            {
                return;
            }

            MethodInvoker updateUi = delegate
            {
                UpdateSessionSummaryUi();
                SyncMiniHudState(this.session.Client);
            };

            if (this.InvokeRequired)
            {
                this.BeginInvoke(updateUi);
            }
            else
            {
                updateUi();
            }
        }

        public bool GetAutoSwitchByCharacter()
        {
            return this.appSettings != null && this.appSettings.AutoSwitchProfileByCharacter;
        }

        public void SetAutoSwitchByCharacter(bool enabled)
        {
            if (this.appSettings == null)
            {
                return;
            }

            this.appSettings.AutoSwitchProfileByCharacter = enabled;
            AppSettings.Save(this.appSettings);
        }

        public void TryAutoSwitchProfileForCharacter(string charName)
        {
            if (this.appSettings == null || !this.appSettings.AutoSwitchProfileByCharacter)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(charName) || !this.profileCB.Enabled)
            {
                return;
            }

            string matchedProfile = ProfileSingleton.FindProfileForCharacter(charName);
            if (string.IsNullOrEmpty(matchedProfile)
                || string.Equals(matchedProfile, this.currentProfile, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!this.profileCB.Items.Contains(matchedProfile))
            {
                this.refreshProfileList();
            }

            if (this.profileCB.Items.Contains(matchedProfile))
            {
                this.profileCB.SelectedItem = matchedProfile;
            }
        }

        private void VitalsTimer_Tick(object sender, EventArgs e)
        {
            if (this.isShuttingDown)
            {
                return;
            }

            using (SessionContext.Enter(this.session))
            {
                Client client = ClientSingleton.GetClient();
                if (client == null || !client.IsProcessAlive())
                {
                    this.vitalsTimer.Stop();
                    this.hpBar.ClearValues();
                    this.spBar.ClearValues();
                    if (this.miniHud != null && !this.miniHud.IsDisposed)
                    {
                        this.miniHud.ClearVitals();
                    }
                    this.lastDetectedCharacter = string.Empty;
                    UpdateSessionSummaryUi();
                    return;
                }

                if (this.session.IsChatting && !client.IsGameWindowFocused())
                {
                    // Auto-clear stuck CHAT state whenever the game window loses focus (e.g. Alt+Tab or clicking 4Tools).
                    this.session.ResumeChat();
                }

                UpdateVitalsDisplay(client);
                SyncMiniHudState(client);

                if (client.HasMemorySupport)
                {
                    bool pausedForTownNow = this.isAutomationOn && client.ShouldPauseForTown();
                    if (pausedForTownNow != this.lastPausedForTown)
                    {
                        this.lastPausedForTown = pausedForTownNow;
                        UpdateSessionSummaryUi();
                    }

                    string detected = client.ReadCharacterName();
                    string normalized = string.IsNullOrWhiteSpace(detected) ? string.Empty : detected.Trim();
                    if (!string.Equals(normalized, this.lastDetectedCharacter, StringComparison.Ordinal))
                    {
                        this.lastDetectedCharacter = normalized;
                        this.characterName.Text = string.IsNullOrEmpty(normalized) ? "- -" : normalized;
                        RaiseSessionStateChanged();
                        if (this.miniHud != null && !this.miniHud.IsDisposed)
                        {
                            this.miniHud.UpdateIdentity(this.currentProfile ?? ProfileSingleton.GetCurrent().Name, this.characterName.Text);
                        }

                        if (!string.IsNullOrEmpty(normalized) && this.profileCB.Enabled)
                        {
                            TryAutoSwitchProfileForCharacter(normalized);
                        }
                    }
                }
            }
        }

        private void UpdateVitalsDisplay(Client client)
        {
            if (client != null && client.HasMemorySupport && client.IsProcessAlive())
            {
                uint currentHp;
                uint maxHp;
                uint currentSp;
                uint maxSp;
                if (client.TryReadVitals(this.vitalsBuffer, out currentHp, out maxHp, out currentSp, out maxSp))
                {
                    this.hpBar.UpdateValues(currentHp, maxHp);
                    this.spBar.UpdateValues(currentSp, maxSp);
                    if (this.miniHud != null && !this.miniHud.IsDisposed)
                    {
                        this.miniHud.UpdateVitals(currentHp, maxHp, currentSp, maxSp);
                    }
                    return;
                }
            }

            this.hpBar.ClearValues();
            this.spBar.ClearValues();
            if (this.miniHud != null && !this.miniHud.IsDisposed)
            {
                this.miniHud.ClearVitals();
            }
        }

        private void ClientMonitorTimer_Tick(object sender, EventArgs e)
        {
            using (SessionContext.Enter(this.session))
            {
                Client client = ClientSingleton.GetClient();
                if (client != null && !client.IsProcessAlive())
                {
                    this.clientMonitorTimer.Stop();
                    this.vitalsTimer.Stop();
                    this.isAutomationOn = false;
                    this.session.IsAutomationOn = false;
                    this.subject.Notify(new Utils.Message(MessageCode.TURN_OFF, null));
                    this.subject.Notify(new Utils.Message(MessageCode.CLIENT_DISCONNECTED, null));
                    ClientSingleton.Clear();
                    this.lastDetectedCharacter = string.Empty;
                    this.characterName.Text = "- -";
                    this.hpBar.ClearValues();
                    this.spBar.ClearValues();
                    UpdateSessionSummaryUi();
                    if (this.miniHud != null && !this.miniHud.IsDisposed)
                    {
                        this.miniHud.ClearVitals();
                        this.miniHud.UpdateIdentity(this.currentProfile ?? ProfileSingleton.GetCurrent().Name, "- -");
                        this.miniHud.UpdateStatus(false, false);
                    }
                    this.refreshProcessList();
                }
            }
        }

        private void processCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.processCB.SelectedItem == null)
            {
                return;
            }

            using (SessionContext.Enter(this.session))
            {
                Client client = new Client(this.processCB.SelectedItem.ToString());
                ClientSingleton.Instance(client);

                if (!client.IsSupported)
                {
                    MessageBox.Show(
                        LocalizationManager.T("msg.client_unsupported"),
                        LocalizationManager.T("title.warning"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                this.subject.Notify(new Utils.Message(Utils.MessageCode.PROCESS_CHANGED, null));
            }
        }

        private void Container_Load(object sender, EventArgs e)
        {
            using (SessionContext.Enter(this.session))
            {
                ProfileSingleton.Create("Default");
                this.refreshProcessList();
                this.refreshProfileList("Default");
                ApplyLanguageToUi();
                ThemeManager.ApplyTheme(this, this.appSettings.DarkMode);
                UpdateSessionSummaryUi();
            }
        }

        public void refreshProfileList(string selectProfile = null)
        {
            MethodInvoker updateAction = delegate
            {
                string previousSelection = selectProfile ?? (this.profileCB.SelectedItem as string) ?? this.currentProfile;
                this.profileCB.Items.Clear();

                foreach (string p in Profile.ListAll())
                {
                    this.profileCB.Items.Add(p);
                }

                if (!string.IsNullOrEmpty(previousSelection) && this.profileCB.Items.Contains(previousSelection))
                {
                    this.profileCB.SelectedItem = previousSelection;
                }
                else if (this.profileCB.Items.Contains("Default"))
                {
                    this.profileCB.SelectedItem = "Default";
                }
            };

            if (this.InvokeRequired)
            {
                this.Invoke(updateAction);
            }
            else
            {
                updateAction();
            }
        }

        public void OnProfileRenamed(string oldName, string newName)
        {
            if (string.Equals(this.currentProfile, oldName, StringComparison.OrdinalIgnoreCase))
            {
                this.currentProfile = newName;
                this.refreshProfileList(newName);
            }
            else
            {
                this.refreshProfileList();
            }
            UpdateSessionSummaryUi();
        }

        public void OnProfileDeleted(string deletedName)
        {
            if (string.Equals(this.currentProfile, deletedName, StringComparison.OrdinalIgnoreCase))
            {
                this.currentProfile = null;
                this.refreshProfileList("Default");
            }
            else
            {
                this.refreshProfileList();
            }
            UpdateSessionSummaryUi();
        }

        private void refreshProcessList()
        {
            MethodInvoker updateAction = delegate
            {
                string previousSelection = this.processCB.SelectedItem as string;
                this.processCB.Items.Clear();

                Process[] allProcesses = Process.GetProcesses();
                foreach (Process p in allProcesses)
                {
                    try
                    {
                        bool hasWindow = !string.IsNullOrEmpty(p.MainWindowTitle)
                            || p.MainWindowHandle != IntPtr.Zero
                            || ClientUpdaterForm.IsBuiltInTestServerName(p.ProcessName);
                        if (hasWindow && ClientListSingleton.ExistsByProcessName(p.ProcessName))
                        {
                            this.processCB.Items.Add(string.Format("{0}.exe - {1}", p.ProcessName, p.Id));
                        }
                    }
                    catch
                    {
                        // Ignore inaccessible system processes.
                    }
                    finally
                    {
                        p.Dispose();
                    }
                }

                if (!string.IsNullOrEmpty(previousSelection) && this.processCB.Items.Contains(previousSelection))
                {
                    this.processCB.SelectedItem = previousSelection;
                }
                else
                {
                    HashSet<string> assigned = this.assignedProcessProvider != null ? this.assignedProcessProvider() : null;
                    if (assigned != null && assigned.Count > 0)
                    {
                        for (int i = 0; i < this.processCB.Items.Count; i++)
                        {
                            string candidate = this.processCB.Items[i] as string;
                            if (!string.IsNullOrEmpty(candidate) && !assigned.Contains(candidate))
                            {
                                this.processCB.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    else if (this.processCB.Items.Count == 1)
                    {
                        this.processCB.SelectedIndex = 0;
                    }
                }
            };

            if (this.InvokeRequired)
            {
                this.Invoke(updateAction);
            }
            else
            {
                updateAction();
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            this.refreshProcessList();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            ShutdownApplication(false);
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            DisposeOwnedGdiResources();
        }

        private void DisposeOwnedGdiResources()
        {
            if (this.pbBrandLogo != null && !this.pbBrandLogo.IsDisposed)
            {
                this.pbBrandLogo.Image = null;
            }
            if (this.brandLogoOffBitmap != null)
            {
                this.brandLogoOffBitmap.Dispose();
                this.brandLogoOffBitmap = null;
            }
            if (this.brandLogoOnBitmap != null)
            {
                this.brandLogoOnBitmap.Dispose();
                this.brandLogoOnBitmap = null;
            }
            foreach (Font font in this.ownedFonts)
            {
                font.Dispose();
            }
            this.ownedFonts.Clear();
        }

        private void ShutdownApplication(bool requestClose)
        {
            if (this.isShuttingDown)
            {
                return;
            }

            this.isShuttingDown = true;

            try
            {
                using (SessionContext.Enter(this.session))
                {
                    KeyboardHook.PhysicalKeyDown -= OnPhysicalPlayerKeyDown;
                    this.session.ChatStateChanged -= OnChatPauseStateChanged;
                    ActionPriorityCoordinator.SetYggEmergencyActive(false);
                    this.clientMonitorTimer.Stop();
                    this.clientMonitorTimer.Dispose();
                    this.vitalsTimer.Stop();
                    this.vitalsTimer.Dispose();
                    if (this.miniHud != null && !this.miniHud.IsDisposed)
                    {
                        this.miniHud.CloseRequested -= OnMiniHudCloseRequested;
                        this.miniHud.PositionChanged -= OnMiniHudPositionChanged;
                        this.miniHud.Close();
                        this.miniHud.Dispose();
                        this.miniHud = null;
                    }
                    if (this.headerToolTip != null)
                    {
                        this.headerToolTip.Dispose();
                        this.headerToolTip = null;
                    }
                    if (this.toggleForm != null && !this.toggleForm.IsDisposed)
                    {
                        this.toggleForm.Shutdown();
                    }
                    this.session.ResumeChat();
                    this.session.IsAutomationOn = false;
                    if (!this.isEmbeddedTab)
                    {
                        KeyboardHook.Disable();
                    }
                    this.subject.Notify(new Utils.Message(MessageCode.TURN_OFF, null));
                    ClientSingleton.Clear();
                    SessionContext.UnregisterSession(this.session);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Container", "Error during shutdown", ex);
            }

            if (requestClose && !this.isEmbeddedTab && !this.IsDisposed)
            {
                this.Close();
            }
        }

        private void lblLinkGithub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenExternalUrl(AppConfig.GithubLink);
        }

        private void lblLinkDiscord_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenExternalUrl(AppConfig.DiscordLink);
        }

        private void lblLinkWiki_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenExternalUrl(AppConfig.WikiLink);
        }

        private void websiteLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenExternalUrl(AppConfig.Website);
        }

        private static void OpenExternalUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Warn("Container", "Failed to open URL: " + url, ex);
            }
        }

        public void ReloadCurrentProfile()
        {
            string active = !string.IsNullOrWhiteSpace(this.currentProfile)
                ? this.currentProfile
                : ProfileSingleton.GetCurrent().Name;

            if (string.IsNullOrWhiteSpace(active))
            {
                return;
            }

            try
            {
                using (SessionContext.Enter(this.session))
                {
                    ProfileSingleton.Load(active);
                    using (ProfileSingleton.SuppressSave())
                    {
                        this.subject.Notify(new Utils.Message(MessageCode.PROFILE_CHANGED, null));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Container", "Failed to reload active profile '" + active + "'", ex);
            }
        }

        private void profileCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = this.profileCB.Text;
            if (string.IsNullOrWhiteSpace(selected) || string.Equals(selected, this.currentProfile, StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                using (SessionContext.Enter(this.session))
                {
                    ProfileSingleton.Load(selected);
                    this.currentProfile = selected;
                    using (ProfileSingleton.SuppressSave())
                    {
                        this.subject.Notify(new Utils.Message(MessageCode.PROFILE_CHANGED, null));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Container", "Error loading profile '" + selected + "'", ex);
                MessageBox.Show(
                    LocalizationManager.T("msg.profile_load_error", ex.Message),
                    LocalizationManager.T("title.error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public void Update(ISubject subject)
        {
            Subject subj = subject as Subject;
            if (subj == null || subj.Message == null)
            {
                return;
            }

            using (SessionContext.Enter(this.session))
            {
                switch (subj.Message.code)
                {
                    case MessageCode.PROCESS_CHANGED:
                        Client activeClient = ClientSingleton.GetClient();
                        UpdateCharacterNameLabel(true);
                        UpdateVitalsDisplay(activeClient);
                        UpdateSessionSummaryUi();
                        if (activeClient != null && activeClient.IsProcessAlive())
                        {
                            this.vitalsTimer.Start();
                        }
                        else
                        {
                            this.vitalsTimer.Stop();
                        }
                        break;
                    case MessageCode.PROFILE_CHANGED:
                        UpdateCharacterNameLabel(false);
                        UpdateSessionSummaryUi();
                        if (this.profileForm != null)
                        {
                            this.profileForm.RefreshCharacterBindingUi();
                        }
                        if (this.miniHud != null && !this.miniHud.IsDisposed)
                        {
                            this.miniHud.UpdateIdentity(this.currentProfile ?? ProfileSingleton.GetCurrent().Name, this.characterName.Text);
                        }
                        break;
                    case MessageCode.TURN_OFF:
                        this.isAutomationOn = false;
                        this.session.IsAutomationOn = false;
                        ActionPriorityCoordinator.SetYggEmergencyActive(false);
                        this.clientMonitorTimer.Stop();
                        this.profileCB.Enabled = true;
                        this.processCB.Enabled = true;
                        UpdateSessionSummaryUi();
                        SyncMiniHudState(ClientSingleton.GetClient());
                        break;
                    case MessageCode.TURN_ON:
                        this.isAutomationOn = true;
                        this.session.IsAutomationOn = true;
                        this.profileCB.Enabled = false;
                        this.processCB.Enabled = false;
                        UpdateCharacterNameLabel(false);
                        UpdateSessionSummaryUi();
                        this.clientMonitorTimer.Start();
                        this.vitalsTimer.Start();
                        SyncMiniHudState(ClientSingleton.GetClient());
                        break;
                    case MessageCode.CLIENT_DISCONNECTED:
                        UpdateSessionSummaryUi();
                        break;
                    case MessageCode.SERVER_LIST_CHANGED:
                        this.refreshProcessList();
                        break;
                    case MessageCode.CLICK_ICON_TRAY:
                        if (!this.isEmbeddedTab)
                        {
                            this.Show();
                            this.WindowState = FormWindowState.Normal;
                            this.Activate();
                        }
                        break;
                    case MessageCode.SHUTDOWN_APPLICATION:
                        ShutdownApplication(true);
                        break;
                }
            }
        }

        private void UpdateCharacterNameLabel(bool allowAutoSwitch)
        {
            string previousDetected = this.lastDetectedCharacter;
            Client client = ClientSingleton.GetClient();
            if (client != null && client.IsProcessAlive())
            {
                string name = client.ReadCharacterName();
                string normalized = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
                this.lastDetectedCharacter = normalized;
                this.characterName.Text = string.IsNullOrEmpty(normalized) ? "- -" : normalized;

                if (allowAutoSwitch && !string.IsNullOrEmpty(normalized))
                {
                    TryAutoSwitchProfileForCharacter(normalized);
                }
            }
            else
            {
                this.lastDetectedCharacter = string.Empty;
                this.characterName.Text = "- -";
            }

            if (this.miniHud != null && !this.miniHud.IsDisposed)
            {
                this.miniHud.UpdateIdentity(this.currentProfile ?? ProfileSingleton.GetCurrent().Name, this.characterName.Text);
            }

            if (!string.Equals(previousDetected, this.lastDetectedCharacter, StringComparison.Ordinal))
            {
                RaiseSessionStateChanged();
            }
        }

        private void containerResize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        #region Embedded Section & Dashboard Forms

        private void RegisterSectionForm(string sectionKey, Form frm)
        {
            frm.TopLevel = false;
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Dock = DockStyle.Fill;
            this.sectionHostPanel.Controls.Add(frm);
            this.sectionForms[sectionKey] = frm;
            frm.Show();
            frm.Visible = false;
        }

        public void SetToggleApplicationStateWindow()
        {
            ToggleApplicationStateForm frm = new ToggleApplicationStateForm(this.subject, this.session, this.isEmbeddedTab);
            frm.TopLevel = false;
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Dock = DockStyle.Fill;
            frm.MiniHudCheckedChanged += OnMiniHudToggled;
            this.toggleForm = frm;
            this.OnOffPanel.Controls.Add(frm);
            frm.Show();
        }

        public void SetAutopotWindow()
        {
            AutopotForm frm = new AutopotForm(this.subject, false);
            frm.TopLevel = false;
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Dock = DockStyle.Fill;
            this.autopotForm = frm;
            this.autopotHostPanel.Controls.Add(frm);
            frm.Show();
        }

        public void SetAutopotYggWindow()
        {
            AutopotForm frm = new AutopotForm(this.subject, true);
            frm.TopLevel = false;
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Dock = DockStyle.Fill;
            this.autopotYggForm = frm;
            this.autopotYggHostPanel.Controls.Add(frm);
            frm.Show();
        }

        public void SetSkillTimerWindow()
        {
            RegisterSectionForm("timers", new SkillTimerForm(this.subject));
        }

        public void SetProfileWindow()
        {
            ProfileForm frm = new ProfileForm(this);
            this.profileForm = frm;
            RegisterSectionForm("profiles", frm);
        }

        public void SetAHKWindow()
        {
            RegisterSectionForm("spammer", new AHKForm(this.subject));
        }

        public void SetAutobuffSkillWindow()
        {
            RegisterSectionForm("buff_skills", new SkillAutoBuffForm(this.subject));
        }

        public void SetAutobuffStuffWindow()
        {
            RegisterSectionForm("buff_stuffs", new StuffAutoBuffForm(this.subject));
        }

        public void SetDebuffRecoveryWindow()
        {
            RegisterSectionForm("debuff", new DebuffRecoveryForm(this.subject));
        }

        public void SetMacrosWindow()
        {
            RegisterSectionForm("macros", new MacrosHubForm(this.subject));
        }

        public void SetMountWindow()
        {
            RegisterSectionForm("mount", new MountForm(this.subject));
        }

        public void SetUtilitiesWindow()
        {
            RegisterSectionForm("utilities", new UtilitiesForm(this.subject));
        }

        public void SetAureumHubWindow()
        {
            bool enableAlerts = !this.isEmbeddedTab || this.session.SlotIndex == 0;
            AureumHubForm frm = new AureumHubForm(this.subject, enableAlerts);
            frm.VoteNotificationRequested += msg =>
            {
                Action<string> handler = this.VoteNotificationRequested;
                if (handler != null)
                {
                    handler(msg);
                }
            };
            this.aureumHubForm = frm;
            RegisterSectionForm("aureum", frm);
        }

        public void SetGuideWindow()
        {
            RegisterSectionForm("guide", new GuideForm(this.subject));
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_KEYMENU = 0xF100;
            // Ignore standalone Alt key menu activation on this borderless/menu-free WinForms window so tapping Alt never traps focus.
            if (m.Msg == WM_SYSCOMMAND && ((int)m.WParam & 0xFFF0) == SC_KEYMENU && m.LParam == IntPtr.Zero)
            {
                return;
            }

            base.WndProc(ref m);
        }

        private void OnPhysicalPlayerKeyDown(Keys key)
        {
            using (SessionContext.Enter(this.session))
            {
                if (!this.isAutomationOn || KeyboardHook.Alt || KeyboardHook.Control)
                {
                    return;
                }

                Profile prof = ProfileSingleton.GetCurrent();
                if (prof != null && prof.UserPreferences != null)
                {
                    Keys toggleKey;
                    if (KeyHelper.TryParseFormsKeys(prof.UserPreferences.toggleStateKey, out toggleKey) && key == toggleKey)
                    {
                        return;
                    }
                }

                Client activeClient = ClientSingleton.GetClient();
                if (activeClient != null && activeClient.IsProcessAlive() && activeClient.IsGameWindowFocused())
                {
                    ActionPriorityCoordinator.NotifyPlayerInput();
                }
            }
        }

        #endregion
    }
}