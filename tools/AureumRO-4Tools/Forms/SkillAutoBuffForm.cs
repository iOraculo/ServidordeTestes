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
        private readonly List<BuffContainer> skillContainers = new List<BuffContainer>();
        private readonly BuffRenderer renderer;
        private TextBox txtSearch;
        private NumericUpDown numMinSp;
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
            this.renderer.doRender(64);
            this.renderer.SetPreRenewalOnly(this.cbPreRenewal != null && this.cbPreRenewal.Checked);
            this.VisibleChanged += (s, e) => SyncAutobuffOptionsFromProfile();
            subject.Attach(this);
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
                Name = "txtSkillSearch",
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
                Name = "numSkillMinSp",
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
                Name = "numSkillCooldown",
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.numCooldown.ValueChanged += OnAutobuffOptionsChanged;

            this.btnClearSkills = new Button
            {
                Location = new Point(438, 9),
                Size = new Size(104, 22),
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

            AppSettings currentSettings = AppSettings.Load();
            this.cbPreRenewal = new CheckBox
            {
                AutoSize = true,
                Cursor = Cursors.Hand,
                Location = new Point(8, 37),
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
                Location = new Point(154, 37),
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
                Location = new Point(290, 37),
                Name = "cbAutoSelfClick",
                Text = "Auto-click self (target)",
                Checked = true,
                UseVisualStyleBackColor = true,
                Tag = FormUtils.SKIP_RESET_TAG
            };
            this.cbAutoSelfClick.CheckedChanged += OnAutobuffOptionsChanged;

            this.Controls.Add(lblSearch);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(lblMinSp);
            this.Controls.Add(this.numMinSp);
            this.Controls.Add(lblCooldown);
            this.Controls.Add(this.numCooldown);
            this.Controls.Add(this.btnClearSkills);
            this.Controls.Add(this.cbPreRenewal);
            this.Controls.Add(this.cbPauseWhenSitting);
            this.Controls.Add(this.cbAutoSelfClick);

            RefreshLocalizedTooltips();
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

            AppSettings settings = AppSettings.Load();
            settings.PreRenewalMode = preRenewal;
            AppSettings.Save(settings);
        }

        private void RefreshLocalizedTooltips()
        {
            FormUtils.ConfigureHelpToolTip(this.toolTip1);

            if (this.txtSearch != null)
            {
                this.toolTip1.SetToolTip(this.txtSearch, LocalizationManager.T("tooltip.buff_search"));
            }
            if (this.btnClearSkills != null)
            {
                this.toolTip1.SetToolTip(this.btnClearSkills, LocalizationManager.T("tooltip.buff_clear_skills"));
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