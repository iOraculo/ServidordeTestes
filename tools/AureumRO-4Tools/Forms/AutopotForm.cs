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
        private bool isUpdatingUi;

        public AutopotForm(Subject subject, bool isYgg)
        {
            InitializeComponent();
            this.helpToolTip = FormUtils.CreateHelpToolTip(this.components);
            if (isYgg)
            {
                this.picBoxHP.Image = Resources._4RTools.ETCResource.Yggdrasil;
                this.picBoxSP.Image = Resources._4RTools.ETCResource.Yggdrasil;
            }

            this.picBoxHP.BackColor = Color.Transparent;
            this.picBoxSP.BackColor = Color.Transparent;
            this.picBoxHP.Image = ThemeManager.StripLightBackground(this.picBoxHP.Image);
            this.picBoxSP.Image = ThemeManager.StripLightBackground(this.picBoxSP.Image);

            // Prevent translated "Atraso" label from overlapping the Delay NumericUpDown box.
            this.label2.AutoSize = false;
            this.label2.Location = new Point(98, 86);
            this.label2.Size = new Size(64, 19);
            this.label2.TextAlign = ContentAlignment.MiddleRight;

            this.isYgg = isYgg;

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

            this.helpToolTip.SetToolTip(this.picBoxHP, hpKeyTip);
            this.helpToolTip.SetToolTip(this.txtHpKey, hpKeyTip);
            this.helpToolTip.SetToolTip(this.txtHPpct, hpPctTip);
            this.helpToolTip.SetToolTip(this.picBoxSP, spKeyTip);
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
