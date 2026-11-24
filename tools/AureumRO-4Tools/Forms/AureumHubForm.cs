using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public sealed class AureumHubForm : Form, IObserver
    {
        private readonly Subject subject;
        private readonly bool enableVoteAlertDispatch;
        private readonly AureumCatalogData catalog;
        private List<AureumVoteSite> activeTopsites;
        private AureumProgressData progress;

        private readonly List<Font> ownedFonts = new List<Font>();
        private Font regularFont;
        private Font boldFont;
        private Font smallFont;
        private Font smallBoldFont;
        private Font strikeFont;
        private Font gridStrikeFont;

        private ToolTip hubToolTip;
        private System.Windows.Forms.Timer voteTickTimer;
        private System.Windows.Forms.Timer copyFeedbackTimer;
        private Button copyFeedbackButton;
        private string copyFeedbackOriginalText;

        // Top sub-tabs
        private SegmentedTabButton btnSubVote;
        private SegmentedTabButton btnSubCollection;
        private SegmentedTabButton btnSubCards;
        private SegmentedTabButton btnSubMaps;

        // Content hosts
        private Panel panelVote;
        private Panel panelCards;
        private Panel panelMaps;

        // Voting controls
        private Button btnOpenVotePage;
        private Button btnMarkAllVoted;
        private Button btnRefreshTopsites;
        private Panel panelTopsitesList;
        private CheckBox chkVoteReminder;
        private CheckBox chkVoteSound;
        private Label lblVoteStatusInfo;
        private readonly List<VoteSiteCardControls> voteCardControls = new List<VoteSiteCardControls>();

        // Card collection controls
        private ComboBox cbCardCategory;
        private ComboBox cbCardStatus;
        private TextBox txtCardSearch;
        private Button btnCopyCardNpc;
        private Label lblCardProgress;
        private CheckBox chkHideCollectedCards;
        private Button btnMarkVisibleCards;
        private Button btnClearVisibleCards;
        private Panel panelCardsList;
        private List<AureumCardCollection> currentlyVisibleCardCollections = new List<AureumCardCollection>();

        // Map collection controls
        private ComboBox cbMapGroupMode;
        private ComboBox cbMapGroupFilter;
        private ComboBox cbMapStatus;
        private TextBox txtMapSearch;
        private Label lblMapProgress;
        private Button btnMarkVisibleMaps;
        private Button btnClearVisibleMaps;
        private DataGridView dgvMaps;
        private Label lblMapSelectedHeader;
        private TextBox txtMapSelectedItems;
        private List<AureumMapEntry> currentlyVisibleMaps = new List<AureumMapEntry>();

        private string activeMainSubTab = "vote"; // "vote" | "collection"
        private string activeCollectionSubTab = "cards"; // "cards" | "maps"
        private bool isPopulatingUi;
        private bool isRefreshingRemoteTopsites;

        public string ActiveMainSubTab { get { return this.activeMainSubTab; } }
        public string ActiveCollectionSubTab { get { return this.activeCollectionSubTab; } }
        public int VisibleCardCollectionCount { get { return this.currentlyVisibleCardCollections.Count; } }
        public int VisibleMapCount { get { return this.currentlyVisibleMaps.Count; } }

        public event Action<string> VoteNotificationRequested;

        private sealed class VoteSiteCardControls
        {
            public AureumVoteSite Site;
            public Panel CardPanel;
            public StatusDot Dot;
            public Label TitleLabel;
            public Label SubLabel;
            public Label CountdownLabel;
            public Button VoteButton;
            public Button ResetButton;
            public Label LastVoteLabel;
            public TextBox LastVoteTimeBox;
            public Button ApplyTimeButton;
        }

        public AureumHubForm(Subject subject, bool enableVoteAlertDispatch = true)
        {
            this.subject = subject;
            this.enableVoteAlertDispatch = enableVoteAlertDispatch;
            this.catalog = AureumCatalog.Load();
            this.activeTopsites = new List<AureumVoteSite>(this.catalog.Topsites);
            this.progress = AureumProgressStore.Load();

            this.FormBorderStyle = FormBorderStyle.None;
            this.ClientSize = new Size(584, 281);
            this.DoubleBuffered = true;

            this.regularFont = TrackFont("Segoe UI", 8.5F, FontStyle.Regular);
            this.boldFont = TrackFont("Segoe UI", 8.5F, FontStyle.Bold);
            this.smallFont = TrackFont("Segoe UI", 7.8F, FontStyle.Regular);
            this.smallBoldFont = TrackFont("Segoe UI", 7.8F, FontStyle.Bold);
            this.strikeFont = TrackFont("Segoe UI", 7.8F, FontStyle.Strikeout);
            this.gridStrikeFont = TrackFont("Segoe UI", 8.2F, FontStyle.Strikeout);

            this.hubToolTip = FormUtils.CreateHelpToolTip();

            BuildSubTabBar();
            BuildVotePanel();
            BuildCardsPanel();
            BuildMapsPanel();

            this.voteTickTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            this.voteTickTimer.Tick += (s, e) => RefreshVoteCountdowns(true);
            this.voteTickTimer.Start();

            this.copyFeedbackTimer = new System.Windows.Forms.Timer { Interval = 1400 };
            this.copyFeedbackTimer.Tick += OnCopyFeedbackTick;

            AureumProgressStore.ProgressChanged += OnExternalProgressChanged;
            if (this.subject != null)
            {
                this.subject.Attach(this);
            }

            PopulateCardFilters();
            PopulateMapFilters();
            ApplyLocalizedTexts();
            SelectMainSubTab("vote");
            ApplyCustomThemeStyling();
        }

        private Font TrackFont(string family, float size, FontStyle style)
        {
            return FontManager.Get(size, style);
        }

        private void BuildSubTabBar()
        {
            this.btnSubVote = new SegmentedTabButton("Hora da Votação")
            {
                Location = new Point(6, 4),
                Name = "btnAureumSubVote",
                Size = new Size(148, 25),
                IsActive = true
            };
            this.btnSubVote.Click += (s, e) => SelectMainSubTab("vote");

            this.btnSubCollection = new SegmentedTabButton("Coleção")
            {
                Location = new Point(160, 4),
                Name = "btnAureumSubCollection",
                Size = new Size(120, 25),
                IsActive = false
            };
            this.btnSubCollection.Click += (s, e) => SelectMainSubTab("collection");

            this.btnSubCards = new SegmentedTabButton("Cartas (28)")
            {
                Location = new Point(316, 4),
                Name = "btnAureumSubCards",
                Size = new Size(128, 25),
                IsActive = true,
                Visible = false
            };
            this.btnSubCards.Click += (s, e) => SelectCollectionSubTab("cards");

            this.btnSubMaps = new SegmentedTabButton("Mapas (311)")
            {
                Location = new Point(450, 4),
                Name = "btnAureumSubMaps",
                Size = new Size(128, 25),
                IsActive = false,
                Visible = false
            };
            this.btnSubMaps.Click += (s, e) => SelectCollectionSubTab("maps");

            this.Controls.Add(this.btnSubVote);
            this.Controls.Add(this.btnSubCollection);
            this.Controls.Add(this.btnSubCards);
            this.Controls.Add(this.btnSubMaps);
        }

        private void BuildVotePanel()
        {
            this.panelVote = new Panel
            {
                Location = new Point(0, 33),
                Name = "panelAureumVote",
                Size = new Size(584, 248)
            };

            this.btnOpenVotePage = new Button
            {
                Cursor = Cursors.Hand,
                Font = this.smallBoldFont,
                Location = new Point(6, 4),
                Name = "btnOpenVotePage",
                Size = new Size(216, 25),
                Text = "Abrir aureumro.com/votar",
                UseMnemonic = false,
                UseVisualStyleBackColor = true
            };
            this.btnOpenVotePage.Click += (s, e) => OpenExternalUrl(this.catalog.VotePageUrl);

            this.btnMarkAllVoted = new Button
            {
                Cursor = Cursors.Hand,
                Font = this.smallBoldFont,
                Location = new Point(228, 4),
                Name = "btnMarkAllVoted",
                Size = new Size(192, 25),
                Text = "Marcar Todos (12h)",
                UseMnemonic = false,
                UseVisualStyleBackColor = true
            };
            this.btnMarkAllVoted.Click += (s, e) => MarkAllSitesVoted();

            this.btnRefreshTopsites = new Button
            {
                Cursor = Cursors.Hand,
                Font = this.smallFont,
                Location = new Point(426, 4),
                Name = "btnRefreshTopsites",
                Size = new Size(152, 25),
                Text = "Sincronizar",
                UseMnemonic = false,
                UseVisualStyleBackColor = true
            };
            this.btnRefreshTopsites.Click += (s, e) => RefreshRemoteTopsitesAsync();

            this.panelTopsitesList = new Panel
            {
                AutoScroll = true,
                Location = new Point(6, 36),
                Name = "panelTopsitesList",
                Size = new Size(572, 148)
            };

            this.chkVoteReminder = new CheckBox
            {
                AutoSize = true,
                Checked = this.progress.VoteReminderEnabled,
                Cursor = Cursors.Hand,
                Font = this.regularFont,
                Location = new Point(8, 190),
                Name = "chkVoteReminder",
                Text = "Avisar quando liberar voto",
                UseMnemonic = false,
                UseVisualStyleBackColor = true
            };
            this.chkVoteReminder.CheckedChanged += OnVoteReminderSettingChanged;

            this.chkVoteSound = new CheckBox
            {
                AutoSize = true,
                Checked = this.progress.VoteReminderSound,
                Cursor = Cursors.Hand,
                Font = this.regularFont,
                Location = new Point(236, 190),
                Name = "chkVoteSound",
                Text = "Tocar som no lembrete",
                UseMnemonic = false,
                UseVisualStyleBackColor = true
            };
            this.chkVoteSound.CheckedChanged += OnVoteReminderSettingChanged;

            this.lblVoteStatusInfo = new Label
            {
                AutoSize = false,
                Font = this.smallFont,
                Location = new Point(8, 213),
                Name = "lblVoteStatusInfo",
                Size = new Size(568, 30),
                Tag = ThemeManager.TAG_MUTED_TEXT,
                UseMnemonic = false
            };

            this.panelVote.Controls.Add(this.btnOpenVotePage);
            this.panelVote.Controls.Add(this.btnMarkAllVoted);
            this.panelVote.Controls.Add(this.btnRefreshTopsites);
            this.panelVote.Controls.Add(this.panelTopsitesList);
            this.panelVote.Controls.Add(this.chkVoteReminder);
            this.panelVote.Controls.Add(this.chkVoteSound);
            this.panelVote.Controls.Add(this.lblVoteStatusInfo);
            this.Controls.Add(this.panelVote);

            RebuildVoteCards();
        }

        private void BuildCardsPanel()
        {
            this.panelCards = new Panel
            {
                Location = new Point(0, 33),
                Name = "panelAureumCards",
                Size = new Size(584, 248),
                Visible = false
            };

            this.cbCardCategory = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = this.smallFont,
                Location = new Point(6, 4),
                Name = "cbCardCategory",
                Size = new Size(170, 21)
            };
            this.cbCardCategory.SelectedIndexChanged += (s, e) =>
            {
                if (!this.isPopulatingUi) RebuildCardCollectionList();
            };

            this.cbCardStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = this.smallFont,
                Location = new Point(182, 4),
                Name = "cbCardStatus",
                Size = new Size(92, 21)
            };
            this.cbCardStatus.SelectedIndexChanged += (s, e) =>
            {
                if (!this.isPopulatingUi) RebuildCardCollectionList();
            };

            this.txtCardSearch = new TextBox
            {
                Font = this.smallFont,
                Location = new Point(280, 4),
                Name = "txtCardSearch",
                Size = new Size(158, 21)
            };
            this.txtCardSearch.TextChanged += (s, e) =>
            {
                if (!this.isPopulatingUi) RebuildCardCollectionList();
            };

            this.btnCopyCardNpc = new Button
            {
                Cursor = Cursors.Hand,
                Font = this.smallFont,
                Location = new Point(444, 3),
                Name = "btnCopyCardNpc",
                Size = new Size(134, 23),
                Text = "/navi Colecionador",
                UseMnemonic = false,
                UseVisualStyleBackColor = true
            };
            this.btnCopyCardNpc.Click += (s, e) => CopyToClipboardWithFeedback(this.catalog.CardNpcNavi, this.btnCopyCardNpc);

            this.lblCardProgress = new Label
            {
                AutoEllipsis = true,
                AutoSize = false,
                Font = this.smallBoldFont,
                Location = new Point(8, 32),
                Name = "lblCardProgress",
                Size = new Size(216, 17),
                UseMnemonic = false
            };

            this.chkHideCollectedCards = new CheckBox
            {
                AutoSize = true,
                Cursor = Cursors.Hand,
                Font = this.smallFont,
                Location = new Point(226, 31),
                Name = "chkHideCollectedCards",
                Text = "Ocultar obtidas",
                UseMnemonic = false,
                UseVisualStyleBackColor = true
            };
            this.chkHideCollectedCards.CheckedChanged += (s, e) =>
            {
                if (!this.isPopulatingUi) RebuildCardCollectionList();
            };

            this.btnMarkVisibleCards = new Button
            {
                Cursor = Cursors.Hand,
                Font = this.smallFont,
                Location = new Point(358, 29),
                Name = "btnMarkVisibleCards",
                Size = new Size(106, 22),
                Text = "Marcar visíveis",
                UseMnemonic = false,
                UseVisualStyleBackColor = true
            };
            this.btnMarkVisibleCards.Click += (s, e) => SetVisibleCardsCollected(true);

            this.btnClearVisibleCards = new Button
            {
                Cursor = Cursors.Hand,
                Font = this.smallFont,
                Location = new Point(470, 29),
                Name = "btnClearVisibleCards",
                Size = new Size(108, 22),
                Text = "Desmarcar visív.",
                UseMnemonic = false,
                UseVisualStyleBackColor = true
            };
            this.btnClearVisibleCards.Click += (s, e) => SetVisibleCardsCollected(false);

            this.panelCardsList = new Panel
            {
                AutoScroll = true,
                Location = new Point(6, 55),
                Name = "panelCardsList",
                Size = new Size(572, 189)
            };

            this.panelCards.Controls.Add(this.cbCardCategory);
            this.panelCards.Controls.Add(this.cbCardStatus);
            this.panelCards.Controls.Add(this.txtCardSearch);
            this.panelCards.Controls.Add(this.btnCopyCardNpc);
            this.panelCards.Controls.Add(this.lblCardProgress);
            this.panelCards.Controls.Add(this.chkHideCollectedCards);
            this.panelCards.Controls.Add(this.btnMarkVisibleCards);
            this.panelCards.Controls.Add(this.btnClearVisibleCards);
            this.panelCards.Controls.Add(this.panelCardsList);
            this.Controls.Add(this.panelCards);
        }

        private void BuildMapsPanel()
        {
            this.panelMaps = new Panel
            {
                Location = new Point(0, 33),
                Name = "panelAureumMaps",
                Size = new Size(584, 248),
                Visible = false
            };

            this.cbMapGroupMode = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = this.smallFont,
                Location = new Point(6, 4),
                Name = "cbMapGroupMode",
                Size = new Size(124, 21)
            };
            this.cbMapGroupMode.SelectedIndexChanged += (s, e) =>
            {
                if (!this.isPopulatingUi)
                {
                    PopulateMapGroupFilterItems();
                    RebuildMapGrid();
                }
            };

            this.cbMapGroupFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = this.smallFont,
                Location = new Point(136, 4),
                Name = "cbMapGroupFilter",
                Size = new Size(166, 21)
            };
            this.cbMapGroupFilter.SelectedIndexChanged += (s, e) =>
            {
                if (!this.isPopulatingUi) RebuildMapGrid();
            };

            this.cbMapStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = this.smallFont,
                Location = new Point(308, 4),
                Name = "cbMapStatus",
                Size = new Size(88, 21)
            };
            this.cbMapStatus.SelectedIndexChanged += (s, e) =>
            {
                if (!this.isPopulatingUi) RebuildMapGrid();
            };

            this.txtMapSearch = new TextBox
            {
                Font = this.smallFont,
                Location = new Point(402, 4),
                Name = "txtMapSearch",
                Size = new Size(176, 21)
            };
            this.txtMapSearch.TextChanged += (s, e) =>
            {
                if (!this.isPopulatingUi) RebuildMapGrid();
            };

            this.lblMapProgress = new Label
            {
                AutoEllipsis = true,
                AutoSize = false,
                Font = this.smallBoldFont,
                Location = new Point(8, 32),
                Name = "lblMapProgress",
                Size = new Size(340, 17),
                UseMnemonic = false
            };

            this.btnMarkVisibleMaps = new Button
            {
                Cursor = Cursors.Hand,
                Font = this.smallFont,
                Location = new Point(358, 29),
                Name = "btnMarkVisibleMaps",
                Size = new Size(106, 22),
                Text = "Marcar visíveis",
                UseMnemonic = false,
                UseVisualStyleBackColor = true
            };
            this.btnMarkVisibleMaps.Click += (s, e) => SetVisibleMapsCompleted(true);

            this.btnClearVisibleMaps = new Button
            {
                Cursor = Cursors.Hand,
                Font = this.smallFont,
                Location = new Point(470, 29),
                Name = "btnClearVisibleMaps",
                Size = new Size(108, 22),
                Text = "Desmarcar visív.",
                UseMnemonic = false,
                UseVisualStyleBackColor = true
            };
            this.btnClearVisibleMaps.Click += (s, e) => SetVisibleMapsCompleted(false);

            this.dgvMaps = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                BorderStyle = BorderStyle.FixedSingle,
                ColumnHeadersHeight = 22,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false,
                Font = this.smallFont,
                Location = new Point(6, 54),
                MultiSelect = false,
                Name = "dgvAureumMaps",
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Size = new Size(572, 118),
                Tag = ThemeManager.PRESERVE_COLORS_TAG
            };
            EnableDoubleBuffering(this.dgvMaps);

            this.dgvMaps.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "colDone",
                HeaderText = "✓",
                Width = 24,
                Resizable = DataGridViewTriState.False,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            this.dgvMaps.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colGroup",
                HeaderText = "Ref.",
                Width = 74,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            this.dgvMaps.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colMapName",
                HeaderText = "Mapa",
                Width = 122,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            this.dgvMaps.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colWarp",
                HeaderText = "Código",
                Width = 74,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            this.dgvMaps.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDirections",
                HeaderText = "Como chegar",
                Width = 94,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
            this.dgvMaps.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colBonus",
                HeaderText = "Bônus",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            this.dgvMaps.CellClick += OnMapGridCellClick;
            this.dgvMaps.SelectionChanged += (s, e) => UpdateSelectedMapDetailBar();

            this.lblMapSelectedHeader = new Label
            {
                AutoEllipsis = true,
                AutoSize = false,
                Font = this.smallBoldFont,
                Location = new Point(8, 175),
                Name = "lblMapSelectedHeader",
                Size = new Size(568, 17),
                UseMnemonic = false
            };

            this.txtMapSelectedItems = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                Font = this.smallFont,
                Location = new Point(6, 194),
                Multiline = true,
                Name = "txtMapSelectedItems",
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Size = new Size(572, 50),
                TabStop = false,
                WordWrap = true,
                Tag = ThemeManager.PRESERVE_COLORS_TAG
            };

            this.panelMaps.Controls.Add(this.cbMapGroupMode);
            this.panelMaps.Controls.Add(this.cbMapGroupFilter);
            this.panelMaps.Controls.Add(this.cbMapStatus);
            this.panelMaps.Controls.Add(this.txtMapSearch);
            this.panelMaps.Controls.Add(this.lblMapProgress);
            this.panelMaps.Controls.Add(this.btnMarkVisibleMaps);
            this.panelMaps.Controls.Add(this.btnClearVisibleMaps);
            this.panelMaps.Controls.Add(this.dgvMaps);
            this.panelMaps.Controls.Add(this.lblMapSelectedHeader);
            this.panelMaps.Controls.Add(this.txtMapSelectedItems);
            this.Controls.Add(this.panelMaps);
        }

        private static void EnableDoubleBuffering(Control control)
        {
            try
            {
                PropertyInfo prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                if (prop != null)
                {
                    prop.SetValue(control, true, null);
                }
            }
            catch
            {
                // Non-critical optimization.
            }
        }

        public void SelectMainSubTab(string subTab)
        {
            bool isCollection = string.Equals(subTab, "collection", StringComparison.OrdinalIgnoreCase);
            this.activeMainSubTab = isCollection ? "collection" : "vote";

            this.btnSubVote.IsActive = !isCollection;
            this.btnSubCollection.IsActive = isCollection;
            this.btnSubCards.Visible = isCollection;
            this.btnSubMaps.Visible = isCollection;

            this.panelVote.Visible = !isCollection;
            this.panelCards.Visible = isCollection && this.activeCollectionSubTab == "cards";
            this.panelMaps.Visible = isCollection && this.activeCollectionSubTab == "maps";

            if (isCollection)
            {
                if (this.activeCollectionSubTab == "cards")
                {
                    RebuildCardCollectionList();
                }
                else
                {
                    RebuildMapGrid();
                }
            }
            else
            {
                RefreshVoteCountdowns(false);
            }
        }

        public void SelectCollectionSubTab(string subTab)
        {
            bool isMaps = string.Equals(subTab, "maps", StringComparison.OrdinalIgnoreCase);
            this.activeCollectionSubTab = isMaps ? "maps" : "cards";
            this.btnSubCards.IsActive = !isMaps;
            this.btnSubMaps.IsActive = isMaps;

            if (this.activeMainSubTab != "collection")
            {
                SelectMainSubTab("collection");
                return;
            }

            this.panelCards.Visible = !isMaps;
            this.panelMaps.Visible = isMaps;
            if (isMaps)
            {
                RebuildMapGrid();
            }
            else
            {
                RebuildCardCollectionList();
            }
        }

        public void SetCardCategoryFilter(string category)
        {
            if (this.cbCardCategory == null) return;
            for (int i = 0; i < this.cbCardCategory.Items.Count; i++)
            {
                if (string.Equals(this.cbCardCategory.Items[i].ToString(), category, StringComparison.OrdinalIgnoreCase))
                {
                    this.cbCardCategory.SelectedIndex = i;
                    return;
                }
            }
        }

        public void SetCardSearchText(string query)
        {
            if (this.txtCardSearch != null)
            {
                this.txtCardSearch.Text = query ?? string.Empty;
            }
        }

        public void SetMapGroupMode(bool groupByBonusType)
        {
            if (this.cbMapGroupMode != null && this.cbMapGroupMode.Items.Count >= 2)
            {
                this.cbMapGroupMode.SelectedIndex = groupByBonusType ? 1 : 0;
            }
        }

        public void SetMapGroupFilter(string filterText)
        {
            if (this.cbMapGroupFilter == null) return;
            for (int i = 0; i < this.cbMapGroupFilter.Items.Count; i++)
            {
                if (string.Equals(this.cbMapGroupFilter.Items[i].ToString(), filterText, StringComparison.OrdinalIgnoreCase))
                {
                    this.cbMapGroupFilter.SelectedIndex = i;
                    return;
                }
            }
        }

        public void SetMapSearchText(string query)
        {
            if (this.txtMapSearch != null)
            {
                this.txtMapSearch.Text = query ?? string.Empty;
            }
        }

        private void RebuildVoteCards()
        {
            this.panelTopsitesList.SuspendLayout();
            try
            {
                this.panelTopsitesList.Controls.Clear();
                this.voteCardControls.Clear();

                int y = 2;
                int cardWidth = this.activeTopsites.Count > 2 ? 550 : 566;

                for (int i = 0; i < this.activeTopsites.Count; i++)
                {
                    AureumVoteSite site = this.activeTopsites[i];

                    Panel card = new Panel
                    {
                        BorderStyle = BorderStyle.FixedSingle,
                        Location = new Point(2, y),
                        Name = "cardVoteSite_" + site.Code,
                        Size = new Size(cardWidth, 64),
                        Tag = ThemeManager.PRESERVE_COLORS_TAG
                    };

                    StatusDot dot = new StatusDot
                    {
                        Location = new Point(11, 14),
                        Name = "dotVote_" + site.Code
                    };

                    Label lblTitle = new Label
                    {
                        AutoSize = false,
                        Font = this.boldFont,
                        Location = new Point(26, 8),
                        Size = new Size(204, 18),
                        Text = site.Name,
                        UseMnemonic = false
                    };

                    Label lblSub = new Label
                    {
                        AutoEllipsis = true,
                        AutoSize = false,
                        Font = this.smallFont,
                        Location = new Point(26, 30),
                        Size = new Size(208, 28),
                        Text = LocalizationManager.T("aureum.vote_site_meta", site.Points, site.CooldownHours),
                        UseMnemonic = false
                    };

                    Label lblCountdown = new Label
                    {
                        AutoSize = false,
                        Font = this.boldFont,
                        Location = new Point(236, 18),
                        Size = new Size(118, 26),
                        TextAlign = ContentAlignment.MiddleCenter,
                        UseMnemonic = false,
                        Tag = ThemeManager.PRESERVE_COLORS_TAG
                    };

                    AureumVoteSite capturedSite = site;
                    Button btnVote = new Button
                    {
                        Cursor = Cursors.Hand,
                        Font = this.smallBoldFont,
                        Location = new Point(cardWidth - 196, 6),
                        Name = "btnVoteSite_" + site.Code,
                        Size = new Size(124, 24),
                        Text = LocalizationManager.T("aureum.vote_now", site.CooldownHours),
                        UseMnemonic = false,
                        UseVisualStyleBackColor = true
                    };
                    btnVote.Click += (s, e) => OnVoteSiteClicked(capturedSite);

                    Button btnReset = new Button
                    {
                        Cursor = Cursors.Hand,
                        Font = this.smallFont,
                        Location = new Point(cardWidth - 66, 6),
                        Name = "btnResetVote_" + site.Code,
                        Size = new Size(58, 24),
                        Text = LocalizationManager.T("aureum.vote_reset"),
                        UseMnemonic = false,
                        UseVisualStyleBackColor = true
                    };
                    btnReset.Click += (s, e) =>
                    {
                        AureumProgressStore.ResetVote(this.progress, capturedSite.Code);
                        RefreshVoteCountdowns(false);
                    };

                    Label lblLastVote = new Label
                    {
                        AutoSize = false,
                        Font = this.smallFont,
                        Location = new Point(cardWidth - 196, 37),
                        Size = new Size(80, 18),
                        Text = LocalizationManager.T("aureum.vote_last_time"),
                        TextAlign = ContentAlignment.MiddleRight,
                        UseMnemonic = false
                    };

                    DateTime? savedLocal = AureumProgressStore.GetLastVoteLocalTime(this.progress, site.Code);
                    TextBox txtLastVote = new TextBox
                    {
                        BorderStyle = BorderStyle.FixedSingle,
                        Font = this.smallFont,
                        Location = new Point(cardWidth - 112, 35),
                        MaxLength = 5,
                        Name = "txtLastVote_" + site.Code,
                        Size = new Size(46, 20),
                        Text = savedLocal.HasValue ? savedLocal.Value.ToString("HH:mm") : string.Empty,
                        TextAlign = HorizontalAlignment.Center,
                        Tag = ThemeManager.PRESERVE_COLORS_TAG
                    };
                    this.hubToolTip.SetToolTip(txtLastVote, LocalizationManager.T("aureum.vote_last_time_tip"));

                    Button btnApplyTime = new Button
                    {
                        Cursor = Cursors.Hand,
                        Font = this.smallFont,
                        Location = new Point(cardWidth - 62, 34),
                        Name = "btnApplyVoteTime_" + site.Code,
                        Size = new Size(54, 22),
                        Text = LocalizationManager.T("aureum.vote_apply_time"),
                        UseMnemonic = false,
                        UseVisualStyleBackColor = true
                    };
                    this.hubToolTip.SetToolTip(btnApplyTime, LocalizationManager.T("aureum.vote_last_time_tip"));

                    VoteSiteCardControls cardRef = new VoteSiteCardControls
                    {
                        Site = site,
                        CardPanel = card,
                        Dot = dot,
                        TitleLabel = lblTitle,
                        SubLabel = lblSub,
                        CountdownLabel = lblCountdown,
                        VoteButton = btnVote,
                        ResetButton = btnReset,
                        LastVoteLabel = lblLastVote,
                        LastVoteTimeBox = txtLastVote,
                        ApplyTimeButton = btnApplyTime
                    };

                    btnApplyTime.Click += (s, e) => ApplyManualLastVoteTime(cardRef);
                    txtLastVote.KeyDown += (s, e) =>
                    {
                        if (e.KeyCode == Keys.Enter)
                        {
                            e.SuppressKeyPress = true;
                            ApplyManualLastVoteTime(cardRef);
                        }
                    };

                    card.Controls.Add(dot);
                    card.Controls.Add(lblTitle);
                    card.Controls.Add(lblSub);
                    card.Controls.Add(lblCountdown);
                    card.Controls.Add(btnVote);
                    card.Controls.Add(btnReset);
                    card.Controls.Add(lblLastVote);
                    card.Controls.Add(txtLastVote);
                    card.Controls.Add(btnApplyTime);
                    this.panelTopsitesList.Controls.Add(card);

                    this.voteCardControls.Add(cardRef);

                    y += 70;
                }
            }
            finally
            {
                this.panelTopsitesList.ResumeLayout(true);
            }

            ApplyVoteCardsTheme();
            RefreshVoteCountdowns(false);
        }

        private void ApplyManualLastVoteTime(VoteSiteCardControls card)
        {
            if (card == null || card.Site == null || card.LastVoteTimeBox == null) return;

            DateTime resolvedUtc;
            if (!AureumProgressStore.TryParseLastVoteLocalTime(card.LastVoteTimeBox.Text, DateTime.Now, out resolvedUtc))
            {
                MessageBox.Show(
                    LocalizationManager.T("aureum.vote_invalid_time"),
                    LocalizationManager.T("title.warning"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                card.LastVoteTimeBox.Focus();
                card.LastVoteTimeBox.SelectAll();
                return;
            }

            AureumProgressStore.RecordVote(this.progress, card.Site.Code, resolvedUtc);
            card.LastVoteTimeBox.Text = resolvedUtc.ToLocalTime().ToString("HH:mm");
            RefreshVoteCountdowns(false);
        }

        private void OnVoteSiteClicked(AureumVoteSite site)
        {
            string url = !string.IsNullOrWhiteSpace(site.VoteUrl) ? site.VoteUrl : this.catalog.VotePageUrl;
            OpenExternalUrl(url);
            AureumProgressStore.RecordVote(this.progress, site.Code, DateTime.UtcNow);
            RefreshVoteCountdowns(false);
        }

        private void MarkAllSitesVoted()
        {
            DateTime now = DateTime.UtcNow;
            foreach (AureumVoteSite site in this.activeTopsites)
            {
                AureumProgressStore.RecordVote(this.progress, site.Code, now);
            }
            RefreshVoteCountdowns(false);
        }

        private void OnVoteReminderSettingChanged(object sender, EventArgs e)
        {
            if (this.isPopulatingUi) return;
            this.progress.VoteReminderEnabled = this.chkVoteReminder.Checked;
            this.progress.VoteReminderSound = this.chkVoteSound.Checked;
            AureumProgressStore.Save(this.progress);
        }

        private void RefreshVoteCountdowns(bool checkAlerts)
        {
            if (this.IsDisposed) return;

            DateTime now = DateTime.UtcNow;
            bool dark = ThemeManager.IsDarkMode;
            Color readyColor = dark ? Color.FromArgb(88, 224, 134) : Color.FromArgb(22, 138, 66);
            Color waitColor = dark ? Color.FromArgb(245, 190, 74) : Color.FromArgb(182, 108, 12);

            for (int i = 0; i < this.voteCardControls.Count; i++)
            {
                VoteSiteCardControls ctrl = this.voteCardControls[i];
                TimeSpan rem = AureumProgressStore.GetRemainingVoteCooldown(this.progress, ctrl.Site, now);
                DateTime? lastLocal = AureumProgressStore.GetLastVoteLocalTime(this.progress, ctrl.Site.Code);

                if (ctrl.LastVoteTimeBox != null && !ctrl.LastVoteTimeBox.Focused)
                {
                    string expectedText = lastLocal.HasValue ? lastLocal.Value.ToString("HH:mm") : string.Empty;
                    if (!string.Equals(ctrl.LastVoteTimeBox.Text, expectedText, StringComparison.Ordinal))
                    {
                        ctrl.LastVoteTimeBox.Text = expectedText;
                    }
                }

                if (rem <= TimeSpan.Zero)
                {
                    ctrl.Dot.CustomColor = readyColor;
                    ctrl.CountdownLabel.Text = LocalizationManager.T("aureum.vote_ready");
                    ctrl.CountdownLabel.ForeColor = readyColor;
                    ctrl.SubLabel.Text = LocalizationManager.T("aureum.vote_site_meta", ctrl.Site.Points, ctrl.Site.CooldownHours);
                }
                else
                {
                    ctrl.Dot.CustomColor = waitColor;
                    int totalHours = (int)rem.TotalHours;
                    ctrl.CountdownLabel.Text = string.Format("{0:00}h {1:00}m {2:00}s", totalHours, rem.Minutes, rem.Seconds);
                    ctrl.CountdownLabel.ForeColor = waitColor;
                    if (lastLocal.HasValue)
                    {
                        int cdHours = ctrl.Site.CooldownHours > 0 ? ctrl.Site.CooldownHours : 12;
                        DateTime nextLocal = lastLocal.Value.AddHours(cdHours);
                        ctrl.SubLabel.Text = LocalizationManager.T(
                            "aureum.vote_site_schedule",
                            lastLocal.Value.ToString("HH:mm"),
                            nextLocal.ToString("HH:mm"));
                    }
                }
            }

            if (checkAlerts && this.enableVoteAlertDispatch && this.progress.VoteReminderEnabled)
            {
                List<AureumVoteSite> newlyReady = AureumProgressStore.CheckNewlyReadyVoteSites(this.progress, this.activeTopsites, now);
                if (newlyReady.Count > 0)
                {
                    if (this.progress.VoteReminderSound)
                    {
                        try { SystemSounds.Exclamation.Play(); } catch { }
                    }

                    string names = string.Join(", ", newlyReady.Select(s => s.Name));
                    string alertMsg = LocalizationManager.T("aureum.vote_alert_body", names);
                    Action<string> notify = this.VoteNotificationRequested;
                    if (notify != null)
                    {
                        notify(alertMsg);
                    }
                }
            }
        }

        private void RefreshRemoteTopsitesAsync()
        {
            if (this.isRefreshingRemoteTopsites) return;
            this.isRefreshingRemoteTopsites = true;
            this.btnRefreshTopsites.Enabled = false;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                List<AureumVoteSite> fetched = AureumCatalog.TryFetchRemoteTopsites(this.catalog.TopsitesApiUrl, this.catalog.VotePageUrl);
                if (this.IsDisposed || !this.IsHandleCreated)
                {
                    this.isRefreshingRemoteTopsites = false;
                    return;
                }

                this.BeginInvoke((MethodInvoker)delegate
                {
                    this.isRefreshingRemoteTopsites = false;
                    if (this.IsDisposed) return;

                    this.btnRefreshTopsites.Enabled = true;
                    if (fetched != null && fetched.Count > 0)
                    {
                        this.activeTopsites = fetched;
                        RebuildVoteCards();
                    }
                });
            });
        }

        private void PopulateCardFilters()
        {
            this.isPopulatingUi = true;
            try
            {
                int prevCatIdx = Math.Max(0, this.cbCardCategory.SelectedIndex);
                int prevStatusIdx = Math.Max(0, this.cbCardStatus.SelectedIndex);

                this.cbCardCategory.Items.Clear();
                this.cbCardCategory.Items.Add(LocalizationManager.T("aureum.all_categories"));
                foreach (string cat in AureumCatalog.GetCardCategories(this.catalog))
                {
                    this.cbCardCategory.Items.Add(cat);
                }
                this.cbCardCategory.SelectedIndex = Math.Min(prevCatIdx, this.cbCardCategory.Items.Count - 1);

                this.cbCardStatus.Items.Clear();
                this.cbCardStatus.Items.Add(LocalizationManager.T("aureum.status_all"));
                this.cbCardStatus.Items.Add(LocalizationManager.T("aureum.status_pending"));
                this.cbCardStatus.Items.Add(LocalizationManager.T("aureum.status_complete"));
                this.cbCardStatus.SelectedIndex = Math.Min(prevStatusIdx, this.cbCardStatus.Items.Count - 1);
            }
            finally
            {
                this.isPopulatingUi = false;
            }
        }

        private void PopulateMapFilters()
        {
            this.isPopulatingUi = true;
            try
            {
                int prevMode = Math.Max(0, this.cbMapGroupMode.SelectedIndex);
                int prevStatus = Math.Max(0, this.cbMapStatus.SelectedIndex);

                this.cbMapGroupMode.Items.Clear();
                this.cbMapGroupMode.Items.Add(LocalizationManager.T("aureum.maps_by_city"));
                this.cbMapGroupMode.Items.Add(LocalizationManager.T("aureum.maps_by_bonus"));
                this.cbMapGroupMode.SelectedIndex = Math.Min(prevMode, 1);

                this.cbMapStatus.Items.Clear();
                this.cbMapStatus.Items.Add(LocalizationManager.T("aureum.status_all"));
                this.cbMapStatus.Items.Add(LocalizationManager.T("aureum.status_pending"));
                this.cbMapStatus.Items.Add(LocalizationManager.T("aureum.status_complete"));
                this.cbMapStatus.SelectedIndex = Math.Min(prevStatus, 2);

                PopulateMapGroupFilterItems();
            }
            finally
            {
                this.isPopulatingUi = false;
            }
        }

        private void PopulateMapGroupFilterItems()
        {
            bool prevPopulating = this.isPopulatingUi;
            this.isPopulatingUi = true;
            try
            {
                bool byBonus = this.cbMapGroupMode.SelectedIndex == 1;
                this.cbMapGroupFilter.Items.Clear();
                this.cbMapGroupFilter.Items.Add(byBonus
                    ? LocalizationManager.T("aureum.all_bonuses")
                    : LocalizationManager.T("aureum.all_cities"));

                List<string> groups = byBonus
                    ? AureumCatalog.GetMapBonusCategories(this.catalog)
                    : AureumCatalog.GetMapCities(this.catalog);

                foreach (string g in groups)
                {
                    this.cbMapGroupFilter.Items.Add(g);
                }

                this.cbMapGroupFilter.SelectedIndex = 0;

                if (this.dgvMaps != null && this.dgvMaps.Columns["colGroup"] != null)
                {
                    this.dgvMaps.Columns["colGroup"].HeaderText = LocalizationManager.T("aureum.col_ref");
                }
            }
            finally
            {
                this.isPopulatingUi = prevPopulating;
            }
        }

        private void RebuildCardCollectionList()
        {
            if (this.IsDisposed || this.panelCardsList == null) return;

            string selectedCategory = this.cbCardCategory.SelectedIndex > 0 ? this.cbCardCategory.SelectedItem.ToString() : null;
            int statusFilter = this.cbCardStatus.SelectedIndex; // 0=all, 1=pending, 2=complete
            string query = NormalizeSearch(this.txtCardSearch.Text);
            bool hideCollected = this.chkHideCollectedCards.Checked;

            List<AureumCardCollection> filtered = new List<AureumCardCollection>();
            int totalCards = 0;
            int totalCollectedCards = 0;
            int totalCompletedCollections = 0;

            foreach (AureumCardCollection col in this.catalog.CardCollections)
            {
                int colCollected = AureumProgressStore.CountCollectedInCollection(this.progress, col);
                totalCards += col.Cards.Count;
                totalCollectedCards += colCollected;
                if (col.Cards.Count > 0 && colCollected >= col.Cards.Count)
                {
                    totalCompletedCollections++;
                }

                if (!string.IsNullOrEmpty(selectedCategory)
                    && !string.Equals(col.Category, selectedCategory, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (statusFilter == 1 && colCollected >= col.Cards.Count)
                {
                    continue;
                }
                if (statusFilter == 2 && colCollected < col.Cards.Count)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(query))
                {
                    bool matches = NormalizeSearch(col.Name).Contains(query)
                        || NormalizeSearch(col.Category).Contains(query)
                        || NormalizeSearch(col.TotalBonus).Contains(query)
                        || col.Cards.Any(c => NormalizeSearch(c.Name).Contains(query) || NormalizeSearch(c.FullName).Contains(query));
                    if (!matches)
                    {
                        continue;
                    }
                }

                filtered.Add(col);
            }

            this.currentlyVisibleCardCollections = filtered;
            this.lblCardProgress.Text = LocalizationManager.T(
                "aureum.cards_progress",
                totalCollectedCards,
                totalCards,
                totalCompletedCollections,
                this.catalog.CardCollections.Count);

            bool dark = ThemeManager.IsDarkMode;
            Color cardBg = dark ? Color.FromArgb(30, 21, 7) : Color.FromArgb(255, 252, 243);
            Color cardDoneBg = dark ? Color.FromArgb(24, 34, 20) : Color.FromArgb(240, 250, 240);
            Color cardText = dark ? Color.FromArgb(244, 234, 206) : Color.FromArgb(46, 33, 12);
            Color accentText = dark ? Color.FromArgb(244, 217, 122) : Color.FromArgb(148, 102, 12);
            Color mutedText = dark ? Color.FromArgb(182, 165, 128) : Color.FromArgb(115, 96, 68);
            Color chipIdleBg = dark ? Color.FromArgb(46, 33, 11) : Color.FromArgb(248, 240, 218);
            Color chipIdleBorder = dark ? Color.FromArgb(95, 70, 24) : Color.FromArgb(204, 182, 136);
            Color chipDoneBg = dark ? Color.FromArgb(28, 52, 34) : Color.FromArgb(222, 244, 226);
            Color chipDoneFore = dark ? Color.FromArgb(132, 214, 152) : Color.FromArgb(36, 116, 62);
            Color chipDoneBorder = dark ? Color.FromArgb(62, 134, 82) : Color.FromArgb(128, 194, 146);
            Color miniBtnBg = dark ? Color.FromArgb(52, 38, 14) : Color.FromArgb(246, 238, 216);

            int scrollPos = this.panelCardsList.VerticalScroll.Value;
            this.panelCardsList.SuspendLayout();
            try
            {
                this.panelCardsList.Controls.Clear();
                int y = 2;
                int cardW = 550;

                foreach (AureumCardCollection col in filtered)
                {
                    int colCollected = AureumProgressStore.CountCollectedInCollection(this.progress, col);
                    bool isComplete = col.Cards.Count > 0 && colCollected >= col.Cards.Count;
                    int activeTier = AureumProgressStore.GetActiveTierLevel(colCollected);

                    List<AureumCardItem> visibleCards = hideCollected
                        ? col.Cards.Where(c => !AureumProgressStore.IsCardCollected(this.progress, col.Index, c.Id)).ToList()
                        : col.Cards;

                    int cardHeight = visibleCards.Count == 0 ? 54 : 84;

                    Panel colCard = new Panel
                    {
                        BackColor = isComplete ? cardDoneBg : cardBg,
                        BorderStyle = BorderStyle.FixedSingle,
                        Location = new Point(2, y),
                        Name = "cardCol_" + col.Index,
                        Size = new Size(cardW, cardHeight),
                        Tag = ThemeManager.PRESERVE_COLORS_TAG
                    };

                    Label lblHead = new Label
                    {
                        AutoEllipsis = true,
                        AutoSize = false,
                        BackColor = Color.Transparent,
                        Font = this.boldFont,
                        ForeColor = cardText,
                        Location = new Point(8, 5),
                        Size = new Size(340, 17),
                        Text = string.Format("#{0:00} {1} · {2}", col.Index, col.Name, col.Category),
                        UseMnemonic = false,
                        Tag = ThemeManager.PRESERVE_COLORS_TAG
                    };

                    Label lblCount = new Label
                    {
                        AutoSize = false,
                        BackColor = Color.Transparent,
                        Font = this.smallBoldFont,
                        ForeColor = isComplete ? chipDoneFore : accentText,
                        Location = new Point(352, 5),
                        Size = new Size(76, 17),
                        TextAlign = ContentAlignment.MiddleRight,
                        Text = string.Format("{0}/{1} ({2})", colCollected, col.Cards.Count, activeTier > 0 ? "Nv." + (activeTier == 1 ? 1 : (activeTier == 3 ? 2 : 3)) : "-"),
                        UseMnemonic = false,
                        Tag = ThemeManager.PRESERVE_COLORS_TAG
                    };

                    AureumCardCollection capturedCol = col;
                    Button btnToggleCol = new Button
                    {
                        BackColor = miniBtnBg,
                        Cursor = Cursors.Hand,
                        FlatStyle = FlatStyle.Flat,
                        Font = this.smallFont,
                        ForeColor = cardText,
                        Location = new Point(434, 3),
                        Size = new Size(108, 21),
                        Text = isComplete
                            ? LocalizationManager.T("aureum.unmark_collection")
                            : LocalizationManager.T("aureum.mark_collection"),
                        UseMnemonic = false,
                        Tag = ThemeManager.PRESERVE_COLORS_TAG
                    };
                    btnToggleCol.FlatAppearance.BorderColor = chipIdleBorder;
                    btnToggleCol.Click += (s, e) =>
                    {
                        AureumProgressStore.SetCardCollectionCollected(this.progress, capturedCol, !isComplete);
                        RebuildCardCollectionList();
                    };

                    string tiersSummary = string.Join("  |  ", col.Tiers.Select(t => t.Threshold + ": " + t.Effect));
                    Label lblBonus = new Label
                    {
                        AutoEllipsis = true,
                        AutoSize = false,
                        BackColor = Color.Transparent,
                        Font = this.smallFont,
                        ForeColor = isComplete ? chipDoneFore : mutedText,
                        Location = new Point(8, 25),
                        Size = new Size(534, 17),
                        Text = col.TotalBonus + "  (" + tiersSummary + ")",
                        UseMnemonic = false,
                        Tag = ThemeManager.PRESERVE_COLORS_TAG
                    };
                    this.hubToolTip.SetToolTip(lblBonus, col.TotalBonus + "\n" + string.Join("\n", col.Tiers.Select(t => t.Threshold + " → " + t.Effect)));

                    colCard.Controls.Add(lblHead);
                    colCard.Controls.Add(lblCount);
                    colCard.Controls.Add(btnToggleCol);
                    colCard.Controls.Add(lblBonus);

                    if (visibleCards.Count > 0)
                    {
                        int chipW = 104;
                        int chipGap = 4;
                        for (int ci = 0; ci < visibleCards.Count; ci++)
                        {
                            AureumCardItem item = visibleCards[ci];
                            bool collected = AureumProgressStore.IsCardCollected(this.progress, col.Index, item.Id);
                            int capturedColIndex = col.Index;
                            int capturedCardId = item.Id;

                            Button chip = new Button
                            {
                                AutoEllipsis = true,
                                BackColor = collected ? chipDoneBg : chipIdleBg,
                                Cursor = Cursors.Hand,
                                FlatStyle = FlatStyle.Flat,
                                Font = collected ? this.strikeFont : this.smallFont,
                                ForeColor = collected ? chipDoneFore : cardText,
                                Location = new Point(8 + ci * (chipW + chipGap), 48),
                                Name = string.Format("btnCard_{0:00}_{1}", col.Index, item.Id),
                                Size = new Size(chipW, 28),
                                Text = collected ? ("✓ " + item.Name) : item.Name,
                                TextAlign = ContentAlignment.MiddleCenter,
                                UseMnemonic = false,
                                Tag = ThemeManager.PRESERVE_COLORS_TAG
                            };
                            chip.FlatAppearance.BorderColor = collected ? chipDoneBorder : chipIdleBorder;
                            chip.FlatAppearance.BorderSize = 1;
                            chip.Click += (s, e) =>
                            {
                                AureumProgressStore.ToggleCardCollected(this.progress, capturedColIndex, capturedCardId);
                                RebuildCardCollectionList();
                            };

                            this.hubToolTip.SetToolTip(
                                chip,
                                string.Format("{0} (ID {1})\n{2}", item.FullName, item.Id, LocalizationManager.T("aureum.card_click_hint")));

                            colCard.Controls.Add(chip);
                        }
                    }

                    this.panelCardsList.Controls.Add(colCard);
                    y += cardHeight + 6;
                }

                if (filtered.Count == 0)
                {
                    Label empty = new Label
                    {
                        AutoSize = false,
                        Font = this.regularFont,
                        ForeColor = mutedText,
                        Location = new Point(12, 24),
                        Size = new Size(540, 24),
                        Text = LocalizationManager.T("aureum.no_results"),
                        TextAlign = ContentAlignment.MiddleCenter,
                        UseMnemonic = false
                    };
                    this.panelCardsList.Controls.Add(empty);
                }
            }
            finally
            {
                this.panelCardsList.ResumeLayout(true);
                if (scrollPos > 0 && this.panelCardsList.VerticalScroll.Maximum > 0)
                {
                    this.panelCardsList.AutoScrollPosition = new Point(0, Math.Min(scrollPos, this.panelCardsList.VerticalScroll.Maximum));
                }
            }
        }

        private void SetVisibleCardsCollected(bool collected)
        {
            if (this.currentlyVisibleCardCollections.Count == 0) return;

            this.progress.EnsureCollections();
            foreach (AureumCardCollection col in this.currentlyVisibleCardCollections)
            {
                foreach (AureumCardItem card in col.Cards)
                {
                    string key = card.GetProgressKey(col.Index);
                    if (collected)
                    {
                        this.progress.CollectedCardKeys.Add(key);
                    }
                    else
                    {
                        this.progress.CollectedCardKeys.Remove(key);
                    }
                }
            }

            AureumProgressStore.Save(this.progress);
            RebuildCardCollectionList();
        }

        private void RebuildMapGrid()
        {
            if (this.IsDisposed || this.dgvMaps == null) return;

            bool byBonus = this.cbMapGroupMode.SelectedIndex == 1;
            string selectedGroup = this.cbMapGroupFilter.SelectedIndex > 0 ? this.cbMapGroupFilter.SelectedItem.ToString() : null;
            int statusFilter = this.cbMapStatus.SelectedIndex; // 0=all, 1=pending, 2=complete
            string query = NormalizeSearch(this.txtMapSearch.Text);

            int totalCompleted = 0;
            List<AureumMapEntry> filtered = new List<AureumMapEntry>();

            foreach (AureumMapEntry map in this.catalog.MapCollections)
            {
                bool done = AureumProgressStore.IsMapCompleted(this.progress, map.Warp);
                if (done) totalCompleted++;

                string groupValue = byBonus ? map.BonusCategory : map.City;
                if (!string.IsNullOrEmpty(selectedGroup)
                    && !string.Equals(groupValue, selectedGroup, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (statusFilter == 1 && done) continue;
                if (statusFilter == 2 && !done) continue;

                if (!string.IsNullOrEmpty(query))
                {
                    bool matches = NormalizeSearch(map.Name).Contains(query)
                        || NormalizeSearch(map.Warp).Contains(query)
                        || NormalizeSearch(map.City).Contains(query)
                        || NormalizeSearch(map.Bonus).Contains(query)
                        || NormalizeSearch(map.BonusCategory).Contains(query)
                        || map.Items.Any(it => NormalizeSearch(it.Name).Contains(query) || NormalizeSearch(it.Drops).Contains(query));
                    if (!matches) continue;
                }

                filtered.Add(map);
            }

            if (byBonus)
            {
                filtered = filtered
                    .OrderBy(m => m.BonusCategory, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(m => m.Bonus, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(m => m.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }

            this.currentlyVisibleMaps = filtered;
            this.lblMapProgress.Text = LocalizationManager.T(
                "aureum.maps_progress",
                totalCompleted,
                this.catalog.MapCollections.Count,
                filtered.Count);

            int firstRow = this.dgvMaps.FirstDisplayedScrollingRowIndex;
            string selectedWarp = GetSelectedMapEntry() != null ? GetSelectedMapEntry().Warp : null;

            this.dgvMaps.SuspendLayout();
            try
            {
                this.dgvMaps.Rows.Clear();
                foreach (AureumMapEntry map in filtered)
                {
                    bool done = AureumProgressStore.IsMapCompleted(this.progress, map.Warp);
                    int rowIdx = this.dgvMaps.Rows.Add(
                        done,
                        map.ReferenceCity,
                        map.Name,
                        map.MapCode,
                        map.Directions ?? string.Empty,
                        map.Bonus);

                    DataGridViewRow row = this.dgvMaps.Rows[rowIdx];
                    row.Tag = map;
                    ApplyMapRowVisualStyle(row, done);
                }

                if (!string.IsNullOrEmpty(selectedWarp))
                {
                    for (int i = 0; i < this.dgvMaps.Rows.Count; i++)
                    {
                        AureumMapEntry entry = this.dgvMaps.Rows[i].Tag as AureumMapEntry;
                        if (entry != null && string.Equals(entry.Warp, selectedWarp, StringComparison.OrdinalIgnoreCase))
                        {
                            this.dgvMaps.Rows[i].Selected = true;
                            break;
                        }
                    }
                }
                else if (this.dgvMaps.Rows.Count > 0)
                {
                    this.dgvMaps.Rows[0].Selected = true;
                }

                if (firstRow >= 0 && firstRow < this.dgvMaps.Rows.Count)
                {
                    this.dgvMaps.FirstDisplayedScrollingRowIndex = firstRow;
                }
            }
            finally
            {
                this.dgvMaps.ResumeLayout(true);
            }

            UpdateSelectedMapDetailBar();
        }

        private void ApplyMapRowVisualStyle(DataGridViewRow row, bool completed)
        {
            bool dark = ThemeManager.IsDarkMode;
            if (completed)
            {
                row.DefaultCellStyle.Font = this.gridStrikeFont;
                row.DefaultCellStyle.ForeColor = dark ? Color.FromArgb(132, 214, 152) : Color.FromArgb(46, 124, 68);
                row.DefaultCellStyle.BackColor = dark ? Color.FromArgb(24, 38, 26) : Color.FromArgb(234, 248, 236);
            }
            else
            {
                row.DefaultCellStyle.Font = this.smallFont;
                row.DefaultCellStyle.ForeColor = dark ? Color.FromArgb(240, 230, 200) : Color.FromArgb(46, 33, 12);
                row.DefaultCellStyle.BackColor = dark ? Color.FromArgb(26, 18, 6) : Color.FromArgb(255, 253, 246);
            }
        }

        private void OnMapGridCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= this.dgvMaps.Rows.Count) return;
            DataGridViewRow row = this.dgvMaps.Rows[e.RowIndex];
            AureumMapEntry entry = row.Tag as AureumMapEntry;
            if (entry == null) return;

            if (e.ColumnIndex == 0)
            {
                bool nowDone = AureumProgressStore.ToggleMapCompleted(this.progress, entry.Warp);
                row.Cells[0].Value = nowDone;
                ApplyMapRowVisualStyle(row, nowDone);
                UpdateMapProgressLabelOnly();
            }
        }

        private void UpdateMapProgressLabelOnly()
        {
            int totalCompleted = this.catalog.MapCollections.Count(m => AureumProgressStore.IsMapCompleted(this.progress, m.Warp));
            this.lblMapProgress.Text = LocalizationManager.T(
                "aureum.maps_progress",
                totalCompleted,
                this.catalog.MapCollections.Count,
                this.currentlyVisibleMaps.Count);
        }

        private void SetVisibleMapsCompleted(bool completed)
        {
            if (this.currentlyVisibleMaps.Count == 0) return;
            AureumProgressStore.SetMapsCompleted(this.progress, this.currentlyVisibleMaps.Select(m => m.Warp), completed);
            RebuildMapGrid();
        }

        private AureumMapEntry GetSelectedMapEntry()
        {
            if (this.dgvMaps == null || this.dgvMaps.SelectedRows.Count == 0)
            {
                return null;
            }
            return this.dgvMaps.SelectedRows[0].Tag as AureumMapEntry;
        }

        private void UpdateSelectedMapDetailBar()
        {
            AureumMapEntry entry = GetSelectedMapEntry();
            if (entry == null)
            {
                this.lblMapSelectedHeader.Text = LocalizationManager.T("aureum.map_select_hint");
                this.txtMapSelectedItems.Text = string.Empty;
                return;
            }

            this.lblMapSelectedHeader.Text = string.Format(
                "{0} ({1}) · Ref: {2} ({3})  |  {4}",
                entry.Name,
                entry.MapCode,
                entry.ReferenceCity,
                entry.Directions ?? "-",
                entry.Bonus);

            string itemsMultiline = entry.Items != null && entry.Items.Count > 0
                ? string.Join(
                    Environment.NewLine,
                    entry.Items.Select(it => string.IsNullOrWhiteSpace(it.Drops)
                        ? ("• " + it.Name)
                        : string.Format("• {0}: {1}", it.Name, it.Drops)))
                : "-";

            this.txtMapSelectedItems.Text = itemsMultiline;

            string routeLine = !string.IsNullOrWhiteSpace(entry.RoutePath)
                ? string.Format("{0}: {1}  ({2})", entry.ReferenceCity, entry.Directions, entry.RoutePath)
                : string.Format("{0}: {1}", entry.ReferenceCity, entry.Directions);

            string tooltipText = string.Format(
                "{0} [{1}]\n{2}\n{3}\n\n{4}",
                entry.Name,
                entry.MapCode,
                routeLine,
                entry.Bonus,
                itemsMultiline);

            this.hubToolTip.SetToolTip(this.lblMapSelectedHeader, tooltipText);
            this.hubToolTip.SetToolTip(this.txtMapSelectedItems, tooltipText);
        }

        private void CopyToClipboardWithFeedback(string text, Button button)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                Logger.Warn("AureumHubForm", "Failed to copy text to clipboard", ex);
            }

            if (button == null || button.IsDisposed) return;

            if (this.copyFeedbackButton != null && !this.copyFeedbackButton.IsDisposed && !ReferenceEquals(this.copyFeedbackButton, button))
            {
                this.copyFeedbackButton.Text = this.copyFeedbackOriginalText;
            }

            this.copyFeedbackTimer.Stop();
            if (!ReferenceEquals(this.copyFeedbackButton, button))
            {
                this.copyFeedbackOriginalText = button.Text;
                this.copyFeedbackButton = button;
            }
            button.Text = LocalizationManager.T("aureum.copied");
            this.copyFeedbackTimer.Start();
        }

        private void OnCopyFeedbackTick(object sender, EventArgs e)
        {
            this.copyFeedbackTimer.Stop();
            if (this.copyFeedbackButton != null && !this.copyFeedbackButton.IsDisposed && !string.IsNullOrEmpty(this.copyFeedbackOriginalText))
            {
                this.copyFeedbackButton.Text = this.copyFeedbackOriginalText;
            }
            this.copyFeedbackButton = null;
            this.copyFeedbackOriginalText = null;
        }

        private static void OpenExternalUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Logger.Warn("AureumHubForm", "Failed to open URL: " + url, ex);
            }
        }

        private static string NormalizeSearch(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string formD = value.Trim().Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder(formD.Length);
            foreach (char ch in formD)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(char.ToLowerInvariant(ch));
                }
            }
            return sb.ToString();
        }

        private void OnExternalProgressChanged(object sender, EventArgs e)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;

            MethodInvoker reload = delegate
            {
                if (this.IsDisposed) return;
                this.progress = AureumProgressStore.Load();
                this.isPopulatingUi = true;
                try
                {
                    if (this.chkVoteReminder != null) this.chkVoteReminder.Checked = this.progress.VoteReminderEnabled;
                    if (this.chkVoteSound != null) this.chkVoteSound.Checked = this.progress.VoteReminderSound;
                }
                finally
                {
                    this.isPopulatingUi = false;
                }

                RefreshVoteCountdowns(false);
                if (this.activeMainSubTab == "collection")
                {
                    if (this.activeCollectionSubTab == "cards")
                    {
                        RebuildCardCollectionList();
                    }
                    else
                    {
                        RebuildMapGrid();
                    }
                }
            };

            if (this.InvokeRequired)
            {
                this.BeginInvoke(reload);
            }
            else
            {
                reload();
            }
        }

        public void ApplyCustomThemeStyling()
        {
            bool dark = ThemeManager.IsDarkMode;
            ApplyVoteCardsTheme();

            if (this.dgvMaps != null)
            {
                Color bg = dark ? Color.FromArgb(26, 18, 6) : Color.FromArgb(255, 253, 246);
                Color gridColor = dark ? Color.FromArgb(72, 52, 18) : Color.FromArgb(218, 200, 162);
                Color headerBg = dark ? Color.FromArgb(46, 33, 10) : Color.FromArgb(242, 229, 194);
                Color headerFore = dark ? Color.FromArgb(244, 217, 122) : Color.FromArgb(62, 44, 16);
                Color selBg = dark ? Color.FromArgb(74, 54, 16) : Color.FromArgb(236, 216, 162);
                Color selFore = dark ? Color.FromArgb(255, 246, 214) : Color.FromArgb(42, 28, 8);

                this.dgvMaps.BackgroundColor = bg;
                this.dgvMaps.GridColor = gridColor;
                this.dgvMaps.ColumnHeadersDefaultCellStyle.BackColor = headerBg;
                this.dgvMaps.ColumnHeadersDefaultCellStyle.ForeColor = headerFore;
                this.dgvMaps.ColumnHeadersDefaultCellStyle.Font = this.smallBoldFont;
                this.dgvMaps.DefaultCellStyle.SelectionBackColor = selBg;
                this.dgvMaps.DefaultCellStyle.SelectionForeColor = selFore;

                foreach (DataGridViewRow row in this.dgvMaps.Rows)
                {
                    AureumMapEntry entry = row.Tag as AureumMapEntry;
                    bool done = entry != null && AureumProgressStore.IsMapCompleted(this.progress, entry.Warp);
                    ApplyMapRowVisualStyle(row, done);
                }
            }

            if (this.txtMapSelectedItems != null)
            {
                this.txtMapSelectedItems.BackColor = dark ? Color.FromArgb(24, 16, 5) : Color.FromArgb(252, 247, 234);
                this.txtMapSelectedItems.ForeColor = dark ? Color.FromArgb(236, 224, 192) : Color.FromArgb(52, 38, 16);
            }

            if (this.activeMainSubTab == "collection" && this.activeCollectionSubTab == "cards")
            {
                RebuildCardCollectionList();
            }
        }

        private void ApplyVoteCardsTheme()
        {
            bool dark = ThemeManager.IsDarkMode;
            Color cardBg = dark ? Color.FromArgb(30, 21, 7) : Color.FromArgb(255, 252, 243);
            Color titleColor = dark ? Color.FromArgb(250, 240, 212) : Color.FromArgb(46, 33, 12);
            Color subColor = dark ? Color.FromArgb(182, 165, 128) : Color.FromArgb(115, 96, 68);
            Color btnBg = dark ? Color.FromArgb(52, 38, 14) : Color.FromArgb(248, 240, 218);
            Color btnBorder = dark ? Color.FromArgb(112, 84, 28) : Color.FromArgb(196, 172, 124);
            Color inputBg = dark ? Color.FromArgb(20, 14, 4) : Color.White;

            foreach (VoteSiteCardControls ctrl in this.voteCardControls)
            {
                ctrl.CardPanel.BackColor = cardBg;
                ctrl.TitleLabel.ForeColor = titleColor;
                ctrl.SubLabel.ForeColor = subColor;

                ctrl.VoteButton.FlatStyle = FlatStyle.Flat;
                ctrl.VoteButton.BackColor = btnBg;
                ctrl.VoteButton.ForeColor = titleColor;
                ctrl.VoteButton.FlatAppearance.BorderColor = btnBorder;

                ctrl.ResetButton.FlatStyle = FlatStyle.Flat;
                ctrl.ResetButton.BackColor = btnBg;
                ctrl.ResetButton.ForeColor = subColor;
                ctrl.ResetButton.FlatAppearance.BorderColor = btnBorder;

                if (ctrl.LastVoteLabel != null)
                {
                    ctrl.LastVoteLabel.ForeColor = subColor;
                }
                if (ctrl.LastVoteTimeBox != null)
                {
                    ctrl.LastVoteTimeBox.BackColor = inputBg;
                    ctrl.LastVoteTimeBox.ForeColor = titleColor;
                }
                if (ctrl.ApplyTimeButton != null)
                {
                    ctrl.ApplyTimeButton.FlatStyle = FlatStyle.Flat;
                    ctrl.ApplyTimeButton.BackColor = btnBg;
                    ctrl.ApplyTimeButton.ForeColor = titleColor;
                    ctrl.ApplyTimeButton.FlatAppearance.BorderColor = btnBorder;
                }
            }
        }

        public void ApplyLocalizedTexts()
        {
            this.btnSubVote.Text = LocalizationManager.T("aureum.sub_vote");
            this.btnSubCollection.Text = LocalizationManager.T("aureum.sub_collection");
            this.btnSubCards.Text = LocalizationManager.T("aureum.sub_cards", this.catalog.CardCollections.Count);
            this.btnSubMaps.Text = LocalizationManager.T("aureum.sub_maps", this.catalog.MapCollections.Count);

            this.btnOpenVotePage.Text = LocalizationManager.T("aureum.open_vote_page");
            this.btnMarkAllVoted.Text = LocalizationManager.T("aureum.mark_all_voted");
            this.btnRefreshTopsites.Text = LocalizationManager.T("aureum.sync_topsites");
            this.chkVoteReminder.Text = LocalizationManager.T("aureum.vote_reminder");
            this.chkVoteSound.Text = LocalizationManager.T("aureum.vote_sound");
            this.lblVoteStatusInfo.Text = LocalizationManager.T("aureum.vote_info");

            this.btnCopyCardNpc.Text = LocalizationManager.T("aureum.copy_card_npc");
            this.chkHideCollectedCards.Text = LocalizationManager.T("aureum.hide_collected");
            this.btnMarkVisibleCards.Text = LocalizationManager.T("aureum.mark_visible");
            this.btnClearVisibleCards.Text = LocalizationManager.T("aureum.clear_visible");

            this.btnMarkVisibleMaps.Text = LocalizationManager.T("aureum.mark_visible");
            this.btnClearVisibleMaps.Text = LocalizationManager.T("aureum.clear_visible");

            if (this.dgvMaps != null)
            {
                this.dgvMaps.Columns["colGroup"].HeaderText = LocalizationManager.T("aureum.col_ref");
                this.dgvMaps.Columns["colMapName"].HeaderText = LocalizationManager.T("aureum.col_map");
                this.dgvMaps.Columns["colWarp"].HeaderText = LocalizationManager.T("aureum.col_code");
                this.dgvMaps.Columns["colDirections"].HeaderText = LocalizationManager.T("aureum.col_directions");
                this.dgvMaps.Columns["colBonus"].HeaderText = LocalizationManager.T("aureum.col_bonus");
            }

            this.hubToolTip.SetToolTip(this.btnSubVote, LocalizationManager.T("tooltip.aureum_sub_vote"));
            this.hubToolTip.SetToolTip(this.btnSubCollection, LocalizationManager.T("tooltip.aureum_sub_collection"));
            this.hubToolTip.SetToolTip(this.btnSubCards, LocalizationManager.T("tooltip.aureum_sub_cards"));
            this.hubToolTip.SetToolTip(this.btnSubMaps, LocalizationManager.T("tooltip.aureum_sub_maps"));
            this.hubToolTip.SetToolTip(this.btnOpenVotePage, LocalizationManager.T("tooltip.aureum_open_vote"));
            this.hubToolTip.SetToolTip(this.btnMarkAllVoted, LocalizationManager.T("tooltip.aureum_mark_all_voted"));
            this.hubToolTip.SetToolTip(this.btnRefreshTopsites, LocalizationManager.T("tooltip.aureum_sync_topsites"));
            this.hubToolTip.SetToolTip(this.chkVoteReminder, LocalizationManager.T("tooltip.aureum_vote_reminder"));
            this.hubToolTip.SetToolTip(this.chkVoteSound, LocalizationManager.T("tooltip.aureum_vote_sound"));
            this.hubToolTip.SetToolTip(this.cbCardCategory, LocalizationManager.T("tooltip.aureum_card_cat"));
            this.hubToolTip.SetToolTip(this.cbCardStatus, LocalizationManager.T("tooltip.aureum_card_status"));
            this.hubToolTip.SetToolTip(this.txtCardSearch, LocalizationManager.T("aureum.cards_search_tip"));
            this.hubToolTip.SetToolTip(this.btnCopyCardNpc, this.catalog.CardNpcNavi);
            this.hubToolTip.SetToolTip(this.chkHideCollectedCards, LocalizationManager.T("tooltip.aureum_hide_cards"));
            this.hubToolTip.SetToolTip(this.btnMarkVisibleCards, LocalizationManager.T("tooltip.aureum_mark_vis_cards"));
            this.hubToolTip.SetToolTip(this.btnClearVisibleCards, LocalizationManager.T("tooltip.aureum_clear_vis_cards"));
            this.hubToolTip.SetToolTip(this.cbMapGroupMode, LocalizationManager.T("tooltip.aureum_map_mode"));
            this.hubToolTip.SetToolTip(this.cbMapGroupFilter, LocalizationManager.T("tooltip.aureum_map_filter"));
            this.hubToolTip.SetToolTip(this.cbMapStatus, LocalizationManager.T("tooltip.aureum_map_status"));
            this.hubToolTip.SetToolTip(this.txtMapSearch, LocalizationManager.T("aureum.maps_search_tip"));
            this.hubToolTip.SetToolTip(this.btnMarkVisibleMaps, LocalizationManager.T("tooltip.aureum_mark_vis_maps"));
            this.hubToolTip.SetToolTip(this.btnClearVisibleMaps, LocalizationManager.T("tooltip.aureum_clear_vis_maps"));

            PopulateCardFilters();
            PopulateMapFilters();
            RebuildVoteCards();
            RebuildCardCollectionList();
            RebuildMapGrid();
        }

        public void Update(ISubject subject)
        {
            Subject subj = subject as Subject;
            if (subj == null || subj.Message == null) return;

            if (subj.Message.code == MessageCode.LANGUAGE_CHANGED)
            {
                ApplyLocalizedTexts();
            }
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            ApplyCustomThemeStyling();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                AureumProgressStore.ProgressChanged -= OnExternalProgressChanged;
                if (this.voteTickTimer != null)
                {
                    this.voteTickTimer.Stop();
                    this.voteTickTimer.Dispose();
                    this.voteTickTimer = null;
                }
                if (this.copyFeedbackTimer != null)
                {
                    this.copyFeedbackTimer.Stop();
                    this.copyFeedbackTimer.Dispose();
                    this.copyFeedbackTimer = null;
                }
                if (this.hubToolTip != null)
                {
                    this.hubToolTip.Dispose();
                    this.hubToolTip = null;
                }
                for (int i = 0; i < this.ownedFonts.Count; i++)
                {
                    this.ownedFonts[i].Dispose();
                }
                this.ownedFonts.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
