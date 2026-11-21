using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class StuffAutoBuffForm : Form, IObserver
    {
        private readonly List<BuffContainer> stuffContainers = new List<BuffContainer>();
        private readonly BuffRenderer renderer;
        private TextBox txtSearch;
        private NumericUpDown numMinSp;
        private NumericUpDown numCooldown;
        private Button btnClearStuffs;
        private CheckBox cbPreRenewal;
        private ComboBox cbOriginFilter;
        private CheckBox cbPauseWhenSitting;
        private bool isUpdatingOptions;

        public StuffAutoBuffForm(Subject subject)
        {
            InitializeComponent();
            BuildCategorizedContainers();

            this.renderer = new BuffRenderer("Autobuff", this.stuffContainers, this.toolTip1);
            InitializeSearchBar();
            this.renderer.doRender(66);
            this.renderer.SetPreRenewalOnly(this.cbPreRenewal != null && this.cbPreRenewal.Checked);
            this.VisibleChanged += (s, e) => SyncAutobuffOptionsFromProfile();

            subject.Attach(this);
        }

        private void BuildCategorizedContainers()
        {
            this.Controls.Clear();

            AddCategoryGroup("CommonPotionsGP", "Common Items — Potions & Utilities", Buff.GetCommonPotionsAndUtilities());
            AddCategoryGroup("CommonElementalsGP", "Common Items — Elementals & Resists", Buff.GetCommonElementals());
            AddCategoryGroup("CommonFoodsBoxesGP", "Common Items — Foods & Boxes", Buff.GetCommonFoodsAndBoxes());
            AddCategoryGroup("RopsCombatGP", "ROPs Items — Combat & Potions", Buff.GetRopsCombatAndPotions());
            AddCategoryGroup("RopsFoodsExpGP", "ROPs Items — Foods & EXP", Buff.GetRopsFoodsAndExp());
            AddCategoryGroup("RopsScrollsGP", "ROPs Items — Scrolls", Buff.GetRopsScrolls());
            AddCategoryGroup("RenewalOtherGP", "Renewal / Other (Runes, 3rd/4th, Candies)", Buff.GetRenewalAndCustomConsumables());
        }

        private void AddCategoryGroup(string name, string title, List<Buff> buffs)
        {
            GroupBox gp = new GroupBox
            {
                AutoSize = true,
                Location = new Point(10, 66),
                Name = name,
                Size = new Size(520, 32),
                TabStop = false,
                Text = title
            };
            this.Controls.Add(gp);
            this.stuffContainers.Add(new BuffContainer(gp, buffs));
        }

        private void InitializeSearchBar()
        {
            Label lblSearch = new Label
            {
                AutoSize = true,
                Location = new Point(8, 14),
                Text = "Filter:"
            };

            this.txtSearch = new TextBox
            {
                Location = new Point(54, 10),
                Size = new Size(142, 20),
                Name = "txtStuffSearch",
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.txtSearch.TextChanged += (s, e) => this.renderer.ApplyFilter(this.txtSearch.Text);

            Label lblMinSp = new Label
            {
                AutoSize = true,
                Location = new Point(204, 14),
                Text = "Min SP%:"
            };

            this.numMinSp = new NumericUpDown
            {
                Location = new Point(264, 10),
                Size = new Size(44, 20),
                Minimum = 0,
                Maximum = 99,
                Name = "numStuffMinSp",
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.numMinSp.ValueChanged += OnAutobuffOptionsChanged;

            Label lblCooldown = new Label
            {
                AutoSize = true,
                Location = new Point(316, 14),
                Text = "CD(ms):"
            };

            this.numCooldown = new NumericUpDown
            {
                Location = new Point(368, 10),
                Size = new Size(62, 20),
                Minimum = 0,
                Maximum = 30000,
                Increment = 250,
                Name = "numStuffCooldown",
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.numCooldown.ValueChanged += OnAutobuffOptionsChanged;

            this.btnClearStuffs = new Button
            {
                Location = new Point(438, 9),
                Size = new Size(104, 22),
                Text = "Clear Stuffs",
                UseVisualStyleBackColor = true
            };
            this.btnClearStuffs.Click += (s, e) =>
            {
                if (MessageBox.Show(
                        LocalizationManager.T("msg.clear_stuffs_confirm"),
                        LocalizationManager.T("title.confirm"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    this.renderer.ClearAll();
                }
            };

            AppSettings currentSettings = AppSettings.Load();
            this.cbPreRenewal = new CheckBox
            {
                AutoSize = true,
                Cursor = Cursors.Hand,
                Location = new Point(8, 38),
                Name = "cbStuffPreRenewal",
                Text = "Pre-Renewal Mode",
                Checked = currentSettings.PreRenewalMode,
                UseVisualStyleBackColor = true,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.cbPreRenewal.CheckedChanged += OnPreRenewalModeChanged;

            this.cbOriginFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FormattingEnabled = true,
                Location = new Point(150, 36),
                Name = "cbStuffOriginFilter",
                Size = new Size(182, 21),
                Tag = FormUtils.SKIP_RESET_TAG
            };
            PopulateOriginFilterItems(0);
            this.cbOriginFilter.SelectedIndexChanged += OnOriginFilterChanged;

            this.cbPauseWhenSitting = new CheckBox
            {
                AutoSize = true,
                Cursor = Cursors.Hand,
                Location = new Point(342, 38),
                Name = "cbStuffPauseWhenSitting",
                Text = "Pause when sitting",
                Checked = true,
                UseVisualStyleBackColor = true,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.cbPauseWhenSitting.CheckedChanged += OnAutobuffOptionsChanged;

            this.Controls.Add(lblSearch);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(lblMinSp);
            this.Controls.Add(this.numMinSp);
            this.Controls.Add(lblCooldown);
            this.Controls.Add(this.numCooldown);
            this.Controls.Add(this.btnClearStuffs);
            this.Controls.Add(this.cbPreRenewal);
            this.Controls.Add(this.cbOriginFilter);
            this.Controls.Add(this.cbPauseWhenSitting);

            RefreshLocalizedTooltips();
        }

        private void PopulateOriginFilterItems(int selectedIndex)
        {
            if (this.cbOriginFilter == null)
            {
                return;
            }

            this.isUpdatingOptions = true;
            try
            {
                this.cbOriginFilter.Items.Clear();
                this.cbOriginFilter.Items.Add(LocalizationManager.T("filter.items_all"));
                this.cbOriginFilter.Items.Add(LocalizationManager.T("filter.items_common"));
                this.cbOriginFilter.Items.Add(LocalizationManager.T("filter.items_rops"));
                this.cbOriginFilter.SelectedIndex = Math.Max(0, Math.Min(2, selectedIndex));
            }
            finally
            {
                this.isUpdatingOptions = false;
            }
        }

        private void OnPreRenewalModeChanged(object sender, EventArgs e)
        {
            if (this.cbPreRenewal == null || this.renderer == null)
            {
                return;
            }

            bool preRenewalOnly = this.cbPreRenewal.Checked;
            this.renderer.SetPreRenewalOnly(preRenewalOnly);

            if (!this.isUpdatingOptions)
            {
                AppSettings settings = AppSettings.Load();
                settings.PreRenewalMode = preRenewalOnly;
                AppSettings.Save(settings);
            }
        }

        private void OnOriginFilterChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingOptions || this.cbOriginFilter == null || this.renderer == null)
            {
                return;
            }

            ApplyOriginFilterIndex(this.cbOriginFilter.SelectedIndex);
        }

        public void SetPreRenewalFilter(bool preRenewalOnly)
        {
            if (this.cbPreRenewal != null)
            {
                this.cbPreRenewal.Checked = preRenewalOnly;
            }
        }

        public void ApplyOriginFilterIndex(int index)
        {
            if (this.renderer == null)
            {
                return;
            }

            if (index == 1)
            {
                this.renderer.SetOriginFilter(BuffOrigin.Common);
            }
            else if (index == 2)
            {
                this.renderer.SetOriginFilter(BuffOrigin.Rops);
            }
            else
            {
                this.renderer.SetOriginFilter(null);
            }
        }

        public bool IsGroupVisible(string groupName)
        {
            return this.renderer != null && this.renderer.IsContainerVisible(groupName);
        }

        private void RefreshLocalizedTooltips()
        {
            FormUtils.ConfigureHelpToolTip(this.toolTip1);

            if (this.txtSearch != null)
            {
                this.toolTip1.SetToolTip(this.txtSearch, LocalizationManager.T("tooltip.buff_search"));
            }
            if (this.btnClearStuffs != null)
            {
                this.toolTip1.SetToolTip(this.btnClearStuffs, LocalizationManager.T("tooltip.buff_clear_stuffs"));
            }
            if (this.numMinSp != null)
            {
                this.toolTip1.SetToolTip(this.numMinSp, LocalizationManager.T("tooltip.min_sp"));
            }
            if (this.numCooldown != null)
            {
                this.toolTip1.SetToolTip(this.numCooldown, LocalizationManager.T("tooltip.buff_cd"));
            }
            if (this.cbPreRenewal != null)
            {
                this.toolTip1.SetToolTip(this.cbPreRenewal, LocalizationManager.T("tooltip.pre_renewal_items"));
            }
            if (this.cbOriginFilter != null)
            {
                this.toolTip1.SetToolTip(this.cbOriginFilter, LocalizationManager.T("tooltip.item_origin"));
                PopulateOriginFilterItems(this.cbOriginFilter.SelectedIndex);
            }
            if (this.cbPauseWhenSitting != null)
            {
                this.toolTip1.SetToolTip(this.cbPauseWhenSitting, LocalizationManager.T("tooltip.pause_sitting"));
            }
            if (this.renderer != null)
            {
                this.renderer.RefreshLocalizedTooltips();
                if (this.txtSearch != null && !string.IsNullOrWhiteSpace(this.txtSearch.Text))
                {
                    this.renderer.ApplyFilter(this.txtSearch.Text);
                }
            }
        }

        private void SyncAutobuffOptionsFromProfile()
        {
            if (this.numMinSp == null || this.numCooldown == null)
            {
                return;
            }

            this.isUpdatingOptions = true;
            try
            {
                AutoBuff autobuff = ProfileSingleton.GetCurrent().Autobuff;
                FormUtils.SetNumericValue(this.numMinSp, autobuff.minSpPercent);
                FormUtils.SetNumericValue(this.numCooldown, autobuff.buffCooldownMs);
                if (this.cbPauseWhenSitting != null)
                {
                    this.cbPauseWhenSitting.Checked = autobuff.pauseWhenSitting;
                }
                if (this.cbPreRenewal != null)
                {
                    bool preRenewal = AppSettings.Load().PreRenewalMode;
                    if (this.cbPreRenewal.Checked != preRenewal)
                    {
                        this.cbPreRenewal.Checked = preRenewal;
                        this.renderer.SetPreRenewalOnly(preRenewal);
                    }
                }
            }
            finally
            {
                this.isUpdatingOptions = false;
            }
        }

        private void OnAutobuffOptionsChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingOptions || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            AutoBuff autobuff = ProfileSingleton.GetCurrent().Autobuff;
            autobuff.minSpPercent = Convert.ToInt32(this.numMinSp.Value);
            autobuff.buffCooldownMs = Convert.ToInt32(this.numCooldown.Value);
            if (this.cbPauseWhenSitting != null)
            {
                autobuff.pauseWhenSitting = this.cbPauseWhenSitting.Checked;
            }
            ProfileSingleton.SetConfiguration(autobuff);
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
                    BuffRenderer.doUpdate(ProfileSingleton.GetCurrent().Autobuff.GetBuffMappingSnapshot(), this);
                    SyncAutobuffOptionsFromProfile();
                    break;
                case MessageCode.LANGUAGE_CHANGED:
                    RefreshLocalizedTooltips();
                    break;
                case MessageCode.TURN_OFF:
                    ProfileSingleton.GetCurrent().Autobuff.Stop();
                    break;
                case MessageCode.TURN_ON:
                    ProfileSingleton.GetCurrent().Autobuff.Start();
                    break;
            }
        }
    }
}
