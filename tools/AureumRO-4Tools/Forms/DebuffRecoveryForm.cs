using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class DebuffRecoveryForm : Form, IObserver
    {
        private sealed class DebuffRowUi
        {
            public Buff Debuff;
            public PictureBox IconBox;
            public Label NameLabel;
            public TextBox KeyBox;
        }

        private readonly List<DebuffRowUi> debuffRows = new List<DebuffRowUi>();
        private GroupBox gbAgilityDebuffs;
        private Label lblCurableHint;
        private Label lblAgilityHint;
        private bool isUpdatingUi;

        public DebuffRecoveryForm(Subject subject)
        {
            this.KeyPreview = true;
            InitializeComponent();

            this.txtStatusKey.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
            this.txtStatusKey.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
            this.txtStatusKey.TextChanged += new EventHandler(onStatusKeyChange);

            BuildCategorizedDebuffLayout();
            subject.Attach(this);
        }

        private void BuildCategorizedDebuffLayout()
        {
            // Remove legacy Elvira Candy and Auto Stand cards (neither applies to AureumRO).
            if (this.groupBox1 != null)
            {
                this.Controls.Remove(this.groupBox1);
                this.groupBox1.Visible = false;
            }
            if (this.StatusRecoveryGP != null)
            {
                this.Controls.Remove(this.StatusRecoveryGP);
                this.StatusRecoveryGP.Visible = false;
            }

            // 1. Card 1: Panacea / Royal Jelly curable statuses (with 1-key shortcut + individual slots).
            List<Buff> panaceaDebuffs = Buff.GetPanaceaCurableDebuffs();
            int rowStep = 28;
            int panaceaRows = (panaceaDebuffs.Count + 1) / 2;
            int panaceaHeaderHeight = 46;
            int panaceaHeight = panaceaHeaderHeight + 14 + (panaceaRows * rowStep);

            this.DebuffRecoveryGP.Controls.Clear();
            this.DebuffRecoveryGP.AutoSize = false;
            this.DebuffRecoveryGP.Location = new Point(10, 8);
            this.DebuffRecoveryGP.Size = new Size(548, panaceaHeight);
            this.DebuffRecoveryGP.Text = "Panacea / Royal Jelly (Consumable Cure)";

            this.pictureBox1.Location = new Point(12, 18);
            this.pictureBox1.Size = new Size(22, 22);
            this.pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;

            this.label1.AutoSize = true;
            this.label1.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            this.label1.Location = new Point(38, 21);
            this.label1.Text = "Single key (all 6):";

            this.txtStatusKey.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            this.txtStatusKey.Location = new Point(198, 19);
            this.txtStatusKey.Size = new Size(58, 20);
            this.txtStatusKey.Text = Key.None.ToString();

            this.lblCurableHint = new Label
            {
                AutoEllipsis = true,
                AutoSize = false,
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                Location = new Point(266, 21),
                Name = "lblCurableHint",
                Size = new Size(270, 18),
                Tag = ThemeManager.TAG_MUTED_TEXT,
                Text = "Or set by status (Green Pot / Holy Water)",
                TextAlign = ContentAlignment.MiddleLeft
            };

            Panel headerDivider = new Panel
            {
                Location = new Point(12, 46),
                Size = new Size(524, 1)
            };

            this.DebuffRecoveryGP.Controls.Add(this.pictureBox1);
            this.DebuffRecoveryGP.Controls.Add(this.label1);
            this.DebuffRecoveryGP.Controls.Add(this.txtStatusKey);
            this.DebuffRecoveryGP.Controls.Add(this.lblCurableHint);
            this.DebuffRecoveryGP.Controls.Add(headerDivider);

            PopulateDebuffGrid(this.DebuffRecoveryGP, panaceaDebuffs, 54, rowStep);

            // 2. Card 2: Increase Agility / Speed (Decrease Agility counter).
            List<Buff> agilityDebuffs = Buff.GetAgilityDebuffs();
            this.gbAgilityDebuffs = new GroupBox
            {
                AutoSize = false,
                Location = new Point(10, this.DebuffRecoveryGP.Bottom + 10),
                Name = "gbAgilityDebuffs",
                Size = new Size(548, 58),
                TabStop = false,
                Text = "Increase Agility / Speed"
            };

            if (agilityDebuffs.Count > 0)
            {
                AddDebuffRow(this.gbAgilityDebuffs, agilityDebuffs[0], 12, 22);

                this.lblAgilityHint = new Label
                {
                    AutoEllipsis = true,
                    AutoSize = false,
                    Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                    Location = new Point(266, 25),
                    Name = "lblAgilityHint",
                    Size = new Size(270, 18),
                    Tag = ThemeManager.TAG_MUTED_TEXT,
                    Text = "Inc. AGI / AGI Scroll / Guyak / Guarana",
                    TextAlign = ContentAlignment.MiddleLeft
                };
                this.gbAgilityDebuffs.Controls.Add(this.lblAgilityHint);
            }

            this.Controls.Add(this.gbAgilityDebuffs);
            this.AutoScrollMinSize = new Size(0, this.gbAgilityDebuffs.Bottom + 12);
            RefreshLocalizedDebuffLabels();
        }

        private void PopulateDebuffGrid(GroupBox container, List<Buff> debuffs, int startY, int rowStep)
        {
            int rowsPerCol = (debuffs.Count + 1) / 2;

            Panel midDivider = new Panel
            {
                Location = new Point(272, startY),
                Size = new Size(1, Math.Max(48, rowsPerCol * rowStep - 4))
            };
            container.Controls.Add(midDivider);

            for (int i = 0; i < debuffs.Count; i++)
            {
                int col = i / rowsPerCol;
                int row = i % rowsPerCol;
                int baseX = col == 0 ? 12 : 284;
                int y = startY + (row * rowStep);
                AddDebuffRow(container, debuffs[i], baseX, y);
            }
        }

        private void AddDebuffRow(GroupBox container, Buff debuff, int baseX, int y)
        {
            PictureBox pb = new PictureBox
            {
                BackColor = Color.Transparent,
                Image = ThemeManager.StripLightBackground(debuff.icon),
                Location = new Point(baseX, y),
                Name = "pbox" + (int)debuff.effectStatusID,
                Size = new Size(24, 24),
                SizeMode = PictureBoxSizeMode.Zoom,
                TabStop = false
            };

            Label lblName = new Label
            {
                AutoEllipsis = true,
                AutoSize = false,
                Location = new Point(baseX + 30, y + 2),
                Name = "lblDebuff" + (int)debuff.effectStatusID,
                Size = new Size(152, 20),
                Text = LocalizationManager.GetBuffShortDisplayName(debuff.name),
                TextAlign = ContentAlignment.MiddleLeft
            };

            TextBox tb = new TextBox
            {
                Location = new Point(baseX + 186, y + 2),
                Name = "in" + (int)debuff.effectStatusID,
                Size = new Size(58, 20),
                Tag = (int)debuff.effectStatusID,
                Text = Key.None.ToString()
            };
            tb.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
            tb.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
            tb.TextChanged += new EventHandler(onDebuffKeyChange);

            container.Controls.Add(pb);
            container.Controls.Add(lblName);
            container.Controls.Add(tb);

            this.debuffRows.Add(new DebuffRowUi
            {
                Debuff = debuff,
                IconBox = pb,
                NameLabel = lblName,
                KeyBox = tb
            });
        }

        private static string GetDebuffCureHint(EffectStatusIDs statusId)
        {
            bool pt = LocalizationManager.IsPortuguese;
            switch (statusId)
            {
                case EffectStatusIDs.POISON:
                    return pt ? "Curado por: Panaceia, Geleia Real, Poção Verde, Erva Verde ou Desintoxicar." : "Cured by: Panacea, Royal Jelly, Green Potion, Green Herb, or Detoxify.";
                case EffectStatusIDs.SILENCE:
                    return pt ? "Curado por: Panaceia, Geleia Real, Poção Verde ou Medicar." : "Cured by: Panacea, Royal Jelly, Green Potion, or Cure.";
                case EffectStatusIDs.BLIND:
                    return pt ? "Curado por: Panaceia, Geleia Real, Poção Verde ou Medicar." : "Cured by: Panacea, Royal Jelly, Green Potion, or Cure.";
                case EffectStatusIDs.CURSE:
                    return pt ? "Curado por: Panaceia, Geleia Real, Água Benta ou Bênção." : "Cured by: Panacea, Royal Jelly, Holy Water, or Blessing.";
                case EffectStatusIDs.CONFUSION:
                    return pt ? "Curado por: Panaceia, Geleia Real ou Medicar." : "Cured by: Panacea, Royal Jelly, or Cure.";
                case EffectStatusIDs.HALLUCINATION:
                    return pt ? "Curado por: Panaceia ou Geleia Real." : "Cured by: Panacea or Royal Jelly.";
                case EffectStatusIDs.EFST_DECREASE_AGI:
                    return pt ? "Removido por: Aumentar Agilidade, Pergaminho de Agilidade, Poção do Vento/Guyak ou Bala de Guaraná." : "Countered by: Increase Agility, AGI Scroll, Speed Potion/Guyak, or Guarana Candy.";
                default:
                    return string.Empty;
            }
        }

        private void RefreshLocalizedDebuffLabels()
        {
            FormUtils.ConfigureHelpToolTip(this.toolTip1);

            string panaceaTip = LocalizationManager.IsPortuguese
                ? "Define uma única tecla (Panaceia ou Geleia Real) para curar Veneno, Silêncio, Cegueira, Maldição, Confusão e Alucinação automaticamente."
                : "Sets one key (Panacea or Royal Jelly) to automatically cure Poison, Silence, Blind, Curse, Confusion, and Hallucination.";
            this.toolTip1.SetToolTip(this.pictureBox1, panaceaTip);
            this.toolTip1.SetToolTip(this.label1, panaceaTip);
            this.toolTip1.SetToolTip(this.txtStatusKey, panaceaTip);
            if (this.lblCurableHint != null)
            {
                this.toolTip1.SetToolTip(this.lblCurableHint, panaceaTip);
            }
            if (this.lblAgilityHint != null)
            {
                this.toolTip1.SetToolTip(this.lblAgilityHint, GetDebuffCureHint(EffectStatusIDs.EFST_DECREASE_AGI));
            }

            foreach (DebuffRowUi row in this.debuffRows)
            {
                if (row == null || row.Debuff == null)
                {
                    continue;
                }

                string shortName = LocalizationManager.GetBuffShortDisplayName(row.Debuff.name);
                string hint = GetDebuffCureHint(row.Debuff.effectStatusID);
                string fullTip = string.IsNullOrEmpty(hint)
                    ? LocalizationManager.GetBuffDisplayName(row.Debuff.name)
                    : LocalizationManager.GetBuffDisplayName(row.Debuff.name) + Environment.NewLine + hint;

                row.NameLabel.Text = shortName;
                this.toolTip1.SetToolTip(row.IconBox, fullTip);
                this.toolTip1.SetToolTip(row.NameLabel, fullTip);
                this.toolTip1.SetToolTip(row.KeyBox, fullTip);
            }
        }

        private void onDebuffKeyChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            TextBox tb = sender as TextBox;
            if (tb == null || tb.Tag == null)
            {
                return;
            }

            Key k;
            if (!KeyHelper.TryParseKey(tb.Text, out k))
            {
                return;
            }

            EffectStatusIDs statusId = (EffectStatusIDs)(int)tb.Tag;
            DebuffsRecovery debuffsRecovery = ProfileSingleton.GetCurrent().DebuffsRecovery;
            debuffsRecovery.AddKeyToBuff(statusId, k);
            ProfileSingleton.SetConfiguration(debuffsRecovery);
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
                        Dictionary<EffectStatusIDs, Key> debuffMap = ProfileSingleton.GetCurrent().DebuffsRecovery.GetBuffMappingSnapshot();
                        foreach (DebuffRowUi row in this.debuffRows)
                        {
                            Key mapped;
                            row.KeyBox.Text = debuffMap.TryGetValue(row.Debuff.effectStatusID, out mapped)
                                ? mapped.ToString()
                                : Key.None.ToString();
                        }

                        StatusRecovery statusRec = ProfileSingleton.GetCurrent().StatusRecovery;
                        Dictionary<EffectStatusIDs, Key> statusMap = statusRec.GetBuffMappingSnapshot();

                        this.txtStatusKey.Text = statusMap.ContainsKey(EffectStatusIDs.SILENCE)
                            ? statusMap[EffectStatusIDs.SILENCE].ToString()
                            : Key.None.ToString();
                    }
                    finally
                    {
                        this.isUpdatingUi = false;
                    }
                    break;
                case MessageCode.LANGUAGE_CHANGED:
                    RefreshLocalizedDebuffLabels();
                    break;
                case MessageCode.TURN_OFF:
                    ProfileSingleton.GetCurrent().StatusRecovery.Stop();
                    ProfileSingleton.GetCurrent().DebuffsRecovery.Stop();
                    break;
                case MessageCode.TURN_ON:
                    ProfileSingleton.GetCurrent().StatusRecovery.Start();
                    ProfileSingleton.GetCurrent().DebuffsRecovery.Start();
                    break;
            }
        }

        private void onStatusKeyChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            Key k;
            if (!KeyHelper.TryParseKey(this.txtStatusKey.Text, out k))
            {
                return;
            }

            StatusRecovery statusRec = ProfileSingleton.GetCurrent().StatusRecovery;
            statusRec.AddKeyToBuff(EffectStatusIDs.POISON, k);
            statusRec.AddKeyToBuff(EffectStatusIDs.SILENCE, k);
            statusRec.AddKeyToBuff(EffectStatusIDs.BLIND, k);
            statusRec.AddKeyToBuff(EffectStatusIDs.CONFUSION, k);
            statusRec.AddKeyToBuff(EffectStatusIDs.HALLUCINATIONWALK, k);
            statusRec.AddKeyToBuff(EffectStatusIDs.HALLUCINATION, k);
            statusRec.AddKeyToBuff(EffectStatusIDs.CURSE, k);

            ProfileSingleton.SetConfiguration(statusRec);
        }
    }
}
