using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class SkillAutoBuffForm : Form, IObserver
    {
        private sealed class ClassFilterOption
        {
            public string ContainerName { get; private set; }
            public string DisplayText { get; private set; }

            public ClassFilterOption(string containerName, string displayText)
            {
                this.ContainerName = containerName ?? string.Empty;
                this.DisplayText = displayText ?? string.Empty;
            }

            public override string ToString()
            {
                return this.DisplayText;
            }
        }

        private readonly List<BuffContainer> skillContainers = new List<BuffContainer>();
        private readonly BuffRenderer renderer;
        private Label lblClass;
        private ComboBox cbClassFilter;
        private Label lblSearch;
        private TextBox txtSearch;
        private Label lblMinSp;
        private NumericUpDown numMinSp;
        private Label lblCooldown;
        private NumericUpDown numCooldown;
        private Button btnClearSkills;
        private CheckBox cbPreRenewal;
        private CheckBox cbPauseWhenSitting;
        private CheckBox cbAutoSelfClick;
        private bool isUpdatingOptions;

        public SkillAutoBuffForm(Subject subject)
        {
            this.KeyPreview = true;
            InitializeComponent();

            this.skillContainers.Add(new BuffContainer(this.ArcherSkillsGP, Buff.GetArcherSkills()));
            this.skillContainers.Add(new BuffContainer(this.SwordmanSkillGP, Buff.GetSwordmanSkill()));
            this.skillContainers.Add(new BuffContainer(this.MageSkillGP, Buff.GetMageSkills()));
            this.skillContainers.Add(new BuffContainer(this.MerchantSkillsGP, Buff.GetMerchantSkills()));
            this.skillContainers.Add(new BuffContainer(this.ThiefSkillsGP, Buff.GetThiefSkills()));
            this.skillContainers.Add(new BuffContainer(this.AcolyteSkillsGP, Buff.GetAcolyteSkills()));
            this.skillContainers.Add(new BuffContainer(this.TKSkillGroupBox, Buff.GetTaekwonSkills()));
            this.skillContainers.Add(new BuffContainer(this.NinjaSkillsGP, Buff.GetNinjaSkills()));
            this.skillContainers.Add(new BuffContainer(this.GunsSkillsGP, Buff.GetGunsSkills()));
            this.skillContainers.Add(new BuffContainer(this.summonerSkillsGP, Buff.GetSummonerSkills()));
            this.skillContainers.Add(new BuffContainer(this.SoulAsceticSkillsGP, Buff.GetSoulAsceticSkills()));
            this.skillContainers.Add(new BuffContainer(this.NightWatchSkillsGP, Buff.GetNightWatchSkills()));
            this.skillContainers.Add(new BuffContainer(this.HyperNoviceSkillsGP, Buff.GetHyperNoviceSkills()));
            this.skillContainers.Add(new BuffContainer(this.HomunculusSkillsGP, Buff.GetHomunculusBuffs()));

            this.renderer = new BuffRenderer("Autobuff", this.skillContainers, this.toolTip1);
            InitializeSearchBar();
            this.renderer.doRender(66);
            this.renderer.SetPreRenewalOnly(this.cbPreRenewal != null && this.cbPreRenewal.Checked);
            PopulateClassFilterOptions();
            this.VisibleChanged += (s, e) => SyncAutobuffOptionsFromProfile();
            subject.Attach(this);
        }

        private void InitializeSearchBar()
        {
            this.lblClass = new Label
            {
                AutoSize = true,
                Location = new Point(8, 12),
                Name = "lblSkillClass",
                Text = "Class:"
            };

            this.cbClassFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FormattingEnabled = true,
                Location = new Point(58, 8),
                Name = "cbSkillClassFilter",
                Size = new Size(156, 23),
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.cbClassFilter.SelectedIndexChanged += OnClassFilterChanged;

            this.lblSearch = new Label
            {
                AutoSize = true,
                Location = new Point(222, 12),
                Name = "lblSkillSearch",
                Text = "Filter:"
            };

            this.txtSearch = new TextBox
            {
                Location = new Point(270, 9),
                Size = new Size(134, 22),
                Name = "txtSkillSearch",
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.txtSearch.TextChanged += (s, e) => this.renderer.ApplyFilter(this.txtSearch.Text);

            this.lblMinSp = new Label
            {
                AutoSize = true,
                Location = new Point(412, 12),
                Name = "lblSkillMinSp",
                Text = "SP%:"
            };

            this.numMinSp = new NumericUpDown
            {
                Location = new Point(450, 9),
                Size = new Size(46, 22),
                Minimum = 0,
                Maximum = 99,
                Name = "numSkillMinSp",
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.numMinSp.ValueChanged += OnAutobuffOptionsChanged;

            this.lblCooldown = new Label
            {
                AutoSize = true,
                Location = new Point(504, 12),
                Name = "lblSkillCooldown",
                Text = "CD:"
            };

            this.numCooldown = new NumericUpDown
            {
                Location = new Point(534, 9),
                Size = new Size(58, 22),
                Minimum = 0,
                Maximum = 30000,
                Increment = 250,
                Name = "numSkillCooldown",
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.numCooldown.ValueChanged += OnAutobuffOptionsChanged;

            AppSettings currentSettings = AppSettings.Load();
            this.cbPreRenewal = new CheckBox
            {
                AutoSize = true,
                Cursor = Cursors.Hand,
                Location = new Point(8, 39),
                Name = "cbPreRenewal",
                Text = "Pre-Renewal Mode",
                Checked = currentSettings.PreRenewalMode,
                UseVisualStyleBackColor = true,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.cbPreRenewal.CheckedChanged += OnPreRenewalModeChanged;

            this.cbPauseWhenSitting = new CheckBox
            {
                AutoSize = true,
                Cursor = Cursors.Hand,
                Location = new Point(152, 39),
                Name = "cbSkillPauseWhenSitting",
                Text = "Pause when sitting",
                Checked = true,
                UseVisualStyleBackColor = true,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.cbPauseWhenSitting.CheckedChanged += OnAutobuffOptionsChanged;

            this.cbAutoSelfClick = new CheckBox
            {
                AutoSize = true,
                Cursor = Cursors.Hand,
                Location = new Point(288, 39),
                Name = "cbAutoSelfClick",
                Text = "Auto-click self (target)",
                Checked = true,
                UseVisualStyleBackColor = true,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.cbAutoSelfClick.CheckedChanged += OnAutobuffOptionsChanged;

            this.btnClearSkills = new Button
            {
                Location = new Point(474, 35),
                Size = new Size(118, 25),
                Name = "btnClearSkills",
                Text = "Clear Skills",
                UseVisualStyleBackColor = true
            };
            this.btnClearSkills.Click += (s, e) =>
            {
                if (MessageBox.Show(
                        LocalizationManager.T("msg.clear_skills_confirm"),
                        LocalizationManager.T("title.confirm"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    this.renderer.ClearAll();
                }
            };

            this.Controls.Add(this.lblClass);
            this.Controls.Add(this.cbClassFilter);
            this.Controls.Add(this.lblSearch);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.lblMinSp);
            this.Controls.Add(this.numMinSp);
            this.Controls.Add(this.lblCooldown);
            this.Controls.Add(this.numCooldown);
            this.Controls.Add(this.cbPreRenewal);
            this.Controls.Add(this.cbPauseWhenSitting);
            this.Controls.Add(this.cbAutoSelfClick);
            this.Controls.Add(this.btnClearSkills);

            RefreshLocalizedTooltips();
        }

        private void PopulateClassFilterOptions()
        {
            if (this.cbClassFilter == null)
            {
                return;
            }

            ClassFilterOption previous = this.cbClassFilter.SelectedItem as ClassFilterOption;
            string previousContainer = previous != null ? previous.ContainerName : string.Empty;
            bool preRenewalOnly = this.cbPreRenewal != null && this.cbPreRenewal.Checked;

            bool prevUpdating = this.isUpdatingOptions;
            this.isUpdatingOptions = true;
            try
            {
                this.cbClassFilter.BeginUpdate();
                this.cbClassFilter.Items.Clear();
                this.cbClassFilter.Items.Add(new ClassFilterOption(string.Empty, LocalizationManager.T("skill.all_classes")));

                int selectedIndex = 0;
                foreach (BuffContainer bc in this.skillContainers)
                {
                    if (bc == null || bc.container == null || bc.skills == null)
                    {
                        continue;
                    }

                    bool hasAny = false;
                    foreach (Buff skill in bc.skills)
                    {
                        if (!preRenewalOnly || skill.IsPreRenewal)
                        {
                            hasAny = true;
                            break;
                        }
                    }

                    if (!hasAny)
                    {
                        continue;
                    }

                    string label = LocalizationManager.TranslateText(bc.container.Text);
                    int idx = this.cbClassFilter.Items.Add(new ClassFilterOption(bc.container.Name, label));
                    if (!string.IsNullOrEmpty(previousContainer)
                        && string.Equals(bc.container.Name, previousContainer, StringComparison.Ordinal))
                    {
                        selectedIndex = idx;
                    }
                }

                this.cbClassFilter.SelectedIndex = selectedIndex;
                this.cbClassFilter.EndUpdate();

                ClassFilterOption active = this.cbClassFilter.SelectedItem as ClassFilterOption;
                if (this.renderer != null)
                {
                    this.renderer.SetContainerFilter(active != null ? active.ContainerName : string.Empty);
                }
            }
            finally
            {
                this.isUpdatingOptions = prevUpdating;
            }
        }

        private void OnClassFilterChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingOptions || this.cbClassFilter == null || this.renderer == null)
            {
                return;
            }

            ClassFilterOption option = this.cbClassFilter.SelectedItem as ClassFilterOption;
            this.renderer.SetContainerFilter(option != null ? option.ContainerName : string.Empty);
        }

        private void OnPreRenewalModeChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingOptions || this.cbPreRenewal == null)
            {
                return;
            }

            bool preRenewal = this.cbPreRenewal.Checked;
            if (this.renderer != null)
            {
                this.renderer.SetPreRenewalOnly(preRenewal);
            }
            PopulateClassFilterOptions();

            AppSettings settings = AppSettings.Load();
            settings.PreRenewalMode = preRenewal;
            AppSettings.Save(settings);
        }

        private void RefreshLocalizedTooltips()
        {
            FormUtils.ConfigureHelpToolTip(this.toolTip1);

            if (this.cbClassFilter != null)
            {
                string classTip = LocalizationManager.T("tooltip.skill_class_filter");
                this.toolTip1.SetToolTip(this.cbClassFilter, classTip);
                if (this.lblClass != null)
                {
                    this.toolTip1.SetToolTip(this.lblClass, classTip);
                }
            }
            if (this.txtSearch != null)
            {
                string searchTip = LocalizationManager.T("tooltip.buff_search");
                this.toolTip1.SetToolTip(this.txtSearch, searchTip);
                if (this.lblSearch != null)
                {
                    this.toolTip1.SetToolTip(this.lblSearch, searchTip);
                }
            }
            if (this.btnClearSkills != null)
            {
                this.toolTip1.SetToolTip(this.btnClearSkills, LocalizationManager.T("tooltip.buff_clear_skills"));
            }
            if (this.numMinSp != null)
            {
                string minSpTip = LocalizationManager.T("tooltip.min_sp");
                this.toolTip1.SetToolTip(this.numMinSp, minSpTip);
                if (this.lblMinSp != null)
                {
                    this.toolTip1.SetToolTip(this.lblMinSp, minSpTip);
                }
            }
            if (this.numCooldown != null)
            {
                string cdTip = LocalizationManager.T("tooltip.buff_cd");
                this.toolTip1.SetToolTip(this.numCooldown, cdTip);
                if (this.lblCooldown != null)
                {
                    this.toolTip1.SetToolTip(this.lblCooldown, cdTip);
                }
            }
            if (this.cbPreRenewal != null)
            {
                this.toolTip1.SetToolTip(this.cbPreRenewal, LocalizationManager.T("tooltip.pre_renewal"));
            }
            if (this.cbPauseWhenSitting != null)
            {
                this.toolTip1.SetToolTip(this.cbPauseWhenSitting, LocalizationManager.T("tooltip.pause_sitting"));
            }
            if (this.cbAutoSelfClick != null)
            {
                this.toolTip1.SetToolTip(this.cbAutoSelfClick, LocalizationManager.T("tooltip.auto_self_click"));
            }
            PopulateClassFilterOptions();
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
                if (this.cbAutoSelfClick != null)
                {
                    this.cbAutoSelfClick.Checked = autobuff.autoSelfClick;
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
            if (this.cbAutoSelfClick != null)
            {
                autobuff.autoSelfClick = this.cbAutoSelfClick.Checked;
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
            }
        }
    }
}