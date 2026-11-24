using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class AutopotForm : Form, IObserver
    {
        private Autopot autopot;
        private readonly bool isYgg;
        private readonly ToolTip helpToolTip;
        private readonly Label lblSectionTitle;
        private readonly Font sectionFont;
        private bool isUpdatingUi;

        public AutopotForm(Subject subject, bool isYgg)
        {
            InitializeComponent();
            this.isYgg = isYgg;
            this.ClientSize = new Size(330, 90);
            this.helpToolTip = FormUtils.CreateHelpToolTip(this.components);

            this.sectionFont = FontManager.Bold(9.25F);
            this.lblSectionTitle = new Label
            {
                AutoSize = true,
                Font = this.sectionFont,
                Location = new Point(10, 4),
                Name = isYgg ? "lblYggSectionTitle" : "lblAutopotSectionTitle",
                Text = isYgg ? "Yggdrasil" : "Autopot",
                Tag = ThemeManager.TAG_ACCENT_TEXT
            };
            this.Controls.Add(this.lblSectionTitle);

            if (isYgg)
            {
                this.picBoxHP.Image = Resources._4RTools.ETCResource.Yggdrasil;
                this.picBoxSP.Image = Resources._4RTools.ETCResource.Yggdrasil;
            }
            else
            {
                // Authentic in-game White Potion (504) & Blue Potion (505) sprites.
                this.picBoxHP.Image = Resources._4RTools.Icons.white_potion;
                this.picBoxSP.Image = Resources._4RTools.Icons.blue_potion;
            }

            this.picBoxHP.BackColor = Color.Transparent;
            this.picBoxSP.BackColor = Color.Transparent;
            this.picBoxHP.Image = ThemeManager.StripLightBackground(this.picBoxHP.Image);
            this.picBoxSP.Image = ThemeManager.StripLightBackground(this.picBoxSP.Image);

            // Compact unified layout so Autopot and Yggdrasil sit stacked in a single dashboard card.
            this.label2.AutoSize = false;
            this.label2.Font = FontManager.Regular(8.75F);
            this.label2.Location = new Point(160, 4);
            this.label2.Size = new Size(68, 19);
            this.label2.TextAlign = ContentAlignment.MiddleRight;

            this.txtAutopotDelay.Font = FontManager.Regular(8.75F);
            this.txtAutopotDelay.Location = new Point(232, 2);
            this.txtAutopotDelay.Size = new Size(54, 22);

            this.label1.Font = FontManager.Regular(8.75F);
            this.label1.Location = new Point(290, 5);

            this.picBoxHP.Location = new Point(12, 28);
            this.picBoxHP.Size = new Size(26, 26);
            this.label3.Font = FontManager.Regular(9F);
            this.label3.Location = new Point(44, 33);
            this.txtHpKey.Font = FontManager.Regular(9F);
            this.txtHpKey.Location = new Point(74, 29);
            this.txtHpKey.Size = new Size(108, 24);
            this.txtHPpct.Font = FontManager.Regular(9F);
            this.txtHPpct.Location = new Point(190, 29);
            this.txtHPpct.Size = new Size(60, 24);
            this.labelHP.Font = FontManager.Regular(9F);
            this.labelHP.Location = new Point(254, 33);

            this.picBoxSP.Location = new Point(12, 58);
            this.picBoxSP.Size = new Size(26, 26);
            this.label4.Font = FontManager.Regular(9F);
            this.label4.Location = new Point(44, 63);
            this.txtSPKey.Font = FontManager.Regular(9F);
            this.txtSPKey.Location = new Point(74, 59);
            this.txtSPKey.Size = new Size(108, 24);
            this.txtSPpct.Font = FontManager.Regular(9F);
            this.txtSPpct.Location = new Point(190, 59);
            this.txtSPpct.Size = new Size(60, 24);
            this.labelSP.Font = FontManager.Regular(9F);
            this.labelSP.Location = new Point(254, 63);

            // Subscribe once in constructor to avoid stacking duplicate handlers on profile switch.
            this.txtHpKey.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
            this.txtHpKey.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
            this.txtHpKey.TextChanged += new EventHandler(this.onHpTextChange);
            this.txtSPKey.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
            this.txtSPKey.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
            this.txtSPKey.TextChanged += new EventHandler(this.onSpTextChange);

            RefreshTooltips();
            subject.Attach(this);
        }

        private void RefreshTooltips()
        {
            if (this.helpToolTip == null)
            {
                return;
            }

            string hpKeyTip = LocalizationManager.T(this.isYgg ? "tooltip.autopot_ygg_hp_key" : "tooltip.autopot_hp_key");
            string hpPctTip = LocalizationManager.T(this.isYgg ? "tooltip.autopot_ygg_hp_pct" : "tooltip.autopot_hp_pct");
            string spKeyTip = LocalizationManager.T(this.isYgg ? "tooltip.autopot_ygg_sp_key" : "tooltip.autopot_sp_key");
            string spPctTip = LocalizationManager.T(this.isYgg ? "tooltip.autopot_ygg_sp_pct" : "tooltip.autopot_sp_pct");
            string delayTip = LocalizationManager.T("tooltip.autopot_delay");

            this.helpToolTip.SetToolTip(this.lblSectionTitle, LocalizationManager.T(this.isYgg ? "tooltip.tab_ygg" : "tooltip.tab_autopot"));
            this.helpToolTip.SetToolTip(this.picBoxHP, hpKeyTip);
            this.helpToolTip.SetToolTip(this.label3, hpKeyTip);
            this.helpToolTip.SetToolTip(this.txtHpKey, hpKeyTip);
            this.helpToolTip.SetToolTip(this.txtHPpct, hpPctTip);
            this.helpToolTip.SetToolTip(this.picBoxSP, spKeyTip);
            this.helpToolTip.SetToolTip(this.label4, spKeyTip);
            this.helpToolTip.SetToolTip(this.txtSPKey, spKeyTip);
            this.helpToolTip.SetToolTip(this.txtSPpct, spPctTip);
            this.helpToolTip.SetToolTip(this.label2, delayTip);
            this.helpToolTip.SetToolTip(this.txtAutopotDelay, delayTip);
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
                    this.autopot = this.isYgg ? ProfileSingleton.GetCurrent().AutopotYgg : ProfileSingleton.GetCurrent().Autopot;
                    InitializeApplicationForm();
                    break;
                case MessageCode.LANGUAGE_CHANGED:
                    RefreshTooltips();
                    break;
                case MessageCode.TURN_OFF:
                    if (this.autopot != null)
                    {
                        this.autopot.Stop();
                    }
                    break;
                case MessageCode.TURN_ON:
                    if (this.autopot != null)
                    {
                        this.autopot.Start();
                    }
                    break;
            }
        }

        private void InitializeApplicationForm()
        {
            if (this.autopot == null)
            {
                return;
            }

            this.isUpdatingUi = true;
            try
            {
                this.txtHpKey.Text = this.autopot.hpKey.ToString();
                this.txtSPKey.Text = this.autopot.spKey.ToString();
                FormUtils.SetNumericValue(this.txtHPpct, this.autopot.hpPercent);
                FormUtils.SetNumericValue(this.txtSPpct, this.autopot.spPercent);
                this.txtAutopotDelay.Text = this.autopot.delay.ToString();
            }
            finally
            {
                this.isUpdatingUi = false;
            }
        }

        private void onHpTextChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed || this.autopot == null)
            {
                return;
            }

            Key key;
            if (KeyHelper.TryParseKey(this.txtHpKey.Text, out key))
            {
                this.autopot.hpKey = key;
                ProfileSingleton.SetConfiguration(this.autopot);
            }
        }

        private void onSpTextChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed || this.autopot == null)
            {
                return;
            }

            Key key;
            if (KeyHelper.TryParseKey(this.txtSPKey.Text, out key))
            {
                this.autopot.spKey = key;
                ProfileSingleton.SetConfiguration(this.autopot);
            }
        }

        private void txtAutopotDelayTextChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed || this.autopot == null)
            {
                return;
            }

            short parsedDelay;
            if (short.TryParse(this.txtAutopotDelay.Text, out parsedDelay) && parsedDelay >= 1)
            {
                this.autopot.delay = parsedDelay;
                ProfileSingleton.SetConfiguration(this.autopot);
            }
        }

        private void txtHPpctTextChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed || this.autopot == null)
            {
                return;
            }

            this.autopot.hpPercent = decimal.ToInt32(this.txtHPpct.Value);
            ProfileSingleton.SetConfiguration(this.autopot);
        }

        private void txtSPpctTextChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed || this.autopot == null)
            {
                return;
            }

            this.autopot.spPercent = decimal.ToInt32(this.txtSPpct.Value);
            ProfileSingleton.SetConfiguration(this.autopot);
        }
    }
}
