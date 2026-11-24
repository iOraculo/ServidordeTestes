using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class ATKDEFForm : Form, IObserver
    {
        private readonly List<Tuple<Control, Control, string>> slotTooltipTargets = new List<Tuple<Control, Control, string>>();
        private readonly List<TextBox> defSlotBoxes = new List<TextBox>();
        private readonly List<TextBox> atkSlotBoxes = new List<TextBox>();
        private GroupBox gbDef;
        private GroupBox gbAtk;
        private Button btnClearDef;
        private Button btnClearAtk;
        private bool isUpdatingUi;

        public ATKDEFForm(Subject subject)
        {
            subject.Attach(this);
            InitializeComponent();
            NormalizeLabelLayout();
            ConfigureAllTenEquipmentSlots();
            SetupInputs();
            RefreshSlotTooltips();
        }

        private void NormalizeLabelLayout()
        {
            this.panelSwitch.Location = new Point(4, 2);
            this.panelSwitch.Size = new Size(576, 240);

            this.groupBoxATKxDEFConfig.Location = new Point(6, 8);
            this.groupBoxATKxDEFConfig.Size = new Size(172, 224);

            this.label2.Text = "Skill (Trigger):";
            this.label3.Text = "Delays(ms):";
            this.lblSwitchDelay.Text = "Switch(ms):";

            this.inSpammerKey.Location = new Point(98, 28);
            this.inSpammerKey.Size = new Size(62, 20);
            this.spammerDelay.Location = new Point(98, 62);
            this.spammerDelay.Size = new Size(62, 20);
            this.switchDelay.Location = new Point(98, 96);
            this.switchDelay.Size = new Size(62, 20);
            this.inSpammerClick.Location = new Point(14, 134);
            this.inSpammerClick.Cursor = System.Windows.Forms.Cursors.Hand;

            Label[] leftLabels = new Label[] { this.label2, this.label3, this.lblSwitchDelay };
            int[] yPositions = new int[] { 30, 64, 98 };
            for (int i = 0; i < leftLabels.Length; i++)
            {
                leftLabels[i].AutoSize = false;
                leftLabels[i].Location = new Point(2, yPositions[i]);
                leftLabels[i].Size = new Size(94, 17);
                leftLabels[i].TextAlign = ContentAlignment.MiddleRight;
            }
        }

        private void ConfigureAllTenEquipmentSlots()
        {
            // Hide legacy bare headers/divider and wrap DEF & ATK in consistent framed GroupBoxes.
            this.lblDef.Visible = false;
            this.label1.Visible = false;
            this.panel1.Visible = false;

            this.gbDef = new GroupBox
            {
                Location = new Point(186, 8),
                Name = "gbDefSwitch",
                Size = new Size(188, 224),
                TabStop = false,
                Text = "DEF Switch"
            };

            this.gbAtk = new GroupBox
            {
                Location = new Point(380, 8),
                Name = "gbAtkSwitch",
                Size = new Size(188, 224),
                TabStop = false,
                Text = "ATK Switch"
            };

            this.panelSwitch.Controls.Add(this.gbDef);
            this.panelSwitch.Controls.Add(this.gbAtk);

            // Create the 5 additional equipment slots for DEF (Tag = "1") and ATK (Tag = "0"):
            // Hat Top (6), Hat Mid (7), Hat Low (8), Left Accessory (9), Right Accessory (10).
            TextBox inDefTop = CreateEquipInput(this.gbDef, "inDef6", "1");
            TextBox inDefMid = CreateEquipInput(this.gbDef, "inDef7", "1");
            TextBox inDefLow = CreateEquipInput(this.gbDef, "inDef8", "1");
            TextBox inDefAccLeft = CreateEquipInput(this.gbDef, "inDef9", "1");
            TextBox inDefAccRight = CreateEquipInput(this.gbDef, "inDef10", "1");

            PictureBox pbDefTop = CreateEquipIcon(this.gbDef, "pbDefTop", Resources._4RTools.Icons.equip_hat_top);
            PictureBox pbDefMid = CreateEquipIcon(this.gbDef, "pbDefMid", Resources._4RTools.Icons.equip_hat_mid);
            PictureBox pbDefLow = CreateEquipIcon(this.gbDef, "pbDefLow", Resources._4RTools.Icons.equip_hat_low);
            PictureBox pbDefAccLeft = CreateEquipIcon(this.gbDef, "pbDefAccLeft", Resources._4RTools.Icons.equip_acc_left);
            PictureBox pbDefAccRight = CreateEquipIcon(this.gbDef, "pbDefAccRight", Resources._4RTools.Icons.equip_acc_right);

            TextBox inAtkTop = CreateEquipInput(this.gbAtk, "inAtk6", "0");
            TextBox inAtkMid = CreateEquipInput(this.gbAtk, "inAtk7", "0");
            TextBox inAtkLow = CreateEquipInput(this.gbAtk, "inAtk8", "0");
            TextBox inAtkAccLeft = CreateEquipInput(this.gbAtk, "inAtk9", "0");
            TextBox inAtkAccRight = CreateEquipInput(this.gbAtk, "inAtk10", "0");

            PictureBox pbAtkTop = CreateEquipIcon(this.gbAtk, "pbAtkTop", Resources._4RTools.Icons.equip_hat_top);
            PictureBox pbAtkMid = CreateEquipIcon(this.gbAtk, "pbAtkMid", Resources._4RTools.Icons.equip_hat_mid);
            PictureBox pbAtkLow = CreateEquipIcon(this.gbAtk, "pbAtkLow", Resources._4RTools.Icons.equip_hat_low);
            PictureBox pbAtkAccLeft = CreateEquipIcon(this.gbAtk, "pbAtkAccLeft", Resources._4RTools.Icons.equip_acc_left);
            PictureBox pbAtkAccRight = CreateEquipIcon(this.gbAtk, "pbAtkAccRight", Resources._4RTools.Icons.equip_acc_right);

            int[] rowY = new int[] { 22, 55, 88, 121, 154 };

            // DEF Switch — Column 1 (Top, Mid, Low, Armor, Weapon)
            PlaceEquipSlot(this.gbDef, pbDefTop, inDefTop, 8, rowY[0], "equip.hat_top", this.defSlotBoxes);
            PlaceEquipSlot(this.gbDef, pbDefMid, inDefMid, 8, rowY[1], "equip.hat_mid", this.defSlotBoxes);
            PlaceEquipSlot(this.gbDef, pbDefLow, inDefLow, 8, rowY[2], "equip.hat_low", this.defSlotBoxes);
            PlaceEquipSlot(this.gbDef, this.pictureBox1, this.inDef1, 8, rowY[3], "equip.armor", this.defSlotBoxes);
            PlaceEquipSlot(this.gbDef, this.pictureBox2, this.inDef2, 8, rowY[4], "equip.weapon", this.defSlotBoxes);

            // DEF Switch — Column 2 (Shield, Garment, Shoes, Left Acc, Right Acc)
            PlaceEquipSlot(this.gbDef, this.pictureBox5, this.inDef3, 98, rowY[0], "equip.shield", this.defSlotBoxes);
            PlaceEquipSlot(this.gbDef, this.pictureBox3, this.inDef4, 98, rowY[1], "equip.garment", this.defSlotBoxes);
            PlaceEquipSlot(this.gbDef, this.pictureBox4, this.inDef5, 98, rowY[2], "equip.shoes", this.defSlotBoxes);
            PlaceEquipSlot(this.gbDef, pbDefAccLeft, inDefAccLeft, 98, rowY[3], "equip.acc_left", this.defSlotBoxes);
            PlaceEquipSlot(this.gbDef, pbDefAccRight, inDefAccRight, 98, rowY[4], "equip.acc_right", this.defSlotBoxes);

            this.btnClearDef = new Button
            {
                Cursor = System.Windows.Forms.Cursors.Hand,
                Location = new Point(92, 189),
                Name = "btnClearDefSlots",
                Size = new Size(88, 25),
                Text = "Clear DEF",
                UseVisualStyleBackColor = true
            };
            this.btnClearDef.Click += (s, e) => ClearSlots(ATKDEFEnum.DEF);
            this.gbDef.Controls.Add(this.btnClearDef);

            // ATK Switch — Column 1 (Top, Mid, Low, Armor, Weapon)
            PlaceEquipSlot(this.gbAtk, pbAtkTop, inAtkTop, 8, rowY[0], "equip.hat_top", this.atkSlotBoxes);
            PlaceEquipSlot(this.gbAtk, pbAtkMid, inAtkMid, 8, rowY[1], "equip.hat_mid", this.atkSlotBoxes);
            PlaceEquipSlot(this.gbAtk, pbAtkLow, inAtkLow, 8, rowY[2], "equip.hat_low", this.atkSlotBoxes);
            PlaceEquipSlot(this.gbAtk, this.pictureBox10, this.inAtk1, 8, rowY[3], "equip.armor", this.atkSlotBoxes);
            PlaceEquipSlot(this.gbAtk, this.pictureBox9, this.inAtk2, 8, rowY[4], "equip.weapon", this.atkSlotBoxes);

            // ATK Switch — Column 2 (Shield, Garment, Shoes, Left Acc, Right Acc)
            PlaceEquipSlot(this.gbAtk, this.pictureBox6, this.inAtk3, 98, rowY[0], "equip.shield", this.atkSlotBoxes);
            PlaceEquipSlot(this.gbAtk, this.pictureBox8, this.inAtk4, 98, rowY[1], "equip.garment", this.atkSlotBoxes);
            PlaceEquipSlot(this.gbAtk, this.pictureBox7, this.inAtk5, 98, rowY[2], "equip.shoes", this.atkSlotBoxes);
            PlaceEquipSlot(this.gbAtk, pbAtkAccLeft, inAtkAccLeft, 98, rowY[3], "equip.acc_left", this.atkSlotBoxes);
            PlaceEquipSlot(this.gbAtk, pbAtkAccRight, inAtkAccRight, 98, rowY[4], "equip.acc_right", this.atkSlotBoxes);

            this.btnClearAtk = new Button
            {
                Cursor = System.Windows.Forms.Cursors.Hand,
                Location = new Point(92, 189),
                Name = "btnClearAtkSlots",
                Size = new Size(88, 25),
                Text = "Clear ATK",
                UseVisualStyleBackColor = true
            };
            this.btnClearAtk.Click += (s, e) => ClearSlots(ATKDEFEnum.ATK);
            this.gbAtk.Controls.Add(this.btnClearAtk);
        }

        private TextBox CreateEquipInput(GroupBox parent, string name, string modeTag)
        {
            TextBox tb = new TextBox
            {
                Name = name,
                Size = new Size(54, 20),
                Tag = modeTag,
                Text = Key.None.ToString()
            };
            parent.Controls.Add(tb);
            return tb;
        }

        private PictureBox CreateEquipIcon(GroupBox parent, string name, Image icon)
        {
            PictureBox pb = new PictureBox
            {
                BackColor = Color.Transparent,
                Image = ThemeManager.StripLightBackground(icon),
                Name = name,
                Size = new Size(24, 24),
                SizeMode = PictureBoxSizeMode.CenterImage,
                TabStop = false
            };
            parent.Controls.Add(pb);
            return pb;
        }

        private void PlaceEquipSlot(GroupBox parent, PictureBox pb, TextBox tb, int x, int y, string tooltipKey, List<TextBox> bucket)
        {
            if (pb.Parent != parent)
            {
                if (pb.Parent != null) pb.Parent.Controls.Remove(pb);
                parent.Controls.Add(pb);
            }
            if (tb.Parent != parent)
            {
                if (tb.Parent != null) tb.Parent.Controls.Remove(tb);
                parent.Controls.Add(tb);
            }

            pb.BackColor = Color.Transparent;
            pb.Size = new Size(24, 24);
            pb.SizeMode = PictureBoxSizeMode.CenterImage;
            pb.Location = new Point(x, y);
            if (pb.Image != null)
            {
                pb.Image = ThemeManager.StripLightBackground(pb.Image);
            }

            tb.Size = new Size(54, 20);
            tb.Location = new Point(x + 26, y + 2);

            bucket.Add(tb);
            this.slotTooltipTargets.Add(Tuple.Create<Control, Control, string>(pb, tb, tooltipKey));
        }

        public void ClearSlots(ATKDEFEnum mode)
        {
            List<TextBox> targetBoxes = mode == ATKDEFEnum.DEF ? this.defSlotBoxes : this.atkSlotBoxes;
            ATKDEFMode atkDef = ProfileSingleton.GetCurrent().AtkDefMode;

            this.isUpdatingUi = true;
            try
            {
                foreach (TextBox tb in targetBoxes)
                {
                    tb.Text = Key.None.ToString();
                    atkDef.RemoveSwitchEntry(tb.Name, mode);
                }
            }
            finally
            {
                this.isUpdatingUi = false;
            }

            if (!ProfileSingleton.IsSaveSuppressed)
            {
                ProfileSingleton.SetConfiguration(atkDef);
            }
        }

        private void RefreshSlotTooltips()
        {
            if (this.toolTip1 == null)
            {
                return;
            }

            FormUtils.ConfigureHelpToolTip(this.toolTip1);

            string triggerTip = LocalizationManager.T("tooltip.atkdef_trigger");
            string delayTip = LocalizationManager.T("tooltip.atkdef_delay");
            string switchDelayTip = LocalizationManager.T("tooltip.atkdef_switch_delay");

            this.toolTip1.SetToolTip(this.label2, triggerTip);
            this.toolTip1.SetToolTip(this.inSpammerKey, triggerTip);
            this.toolTip1.SetToolTip(this.label3, delayTip);
            this.toolTip1.SetToolTip(this.spammerDelay, delayTip);
            this.toolTip1.SetToolTip(this.lblSwitchDelay, switchDelayTip);
            this.toolTip1.SetToolTip(this.switchDelay, switchDelayTip);
            this.toolTip1.SetToolTip(this.inSpammerClick, LocalizationManager.T("tooltip.atkdef_click"));
            if (this.btnClearDef != null)
            {
                this.toolTip1.SetToolTip(this.btnClearDef, LocalizationManager.T("tooltip.atkdef_clear_def"));
            }
            if (this.btnClearAtk != null)
            {
                this.toolTip1.SetToolTip(this.btnClearAtk, LocalizationManager.T("tooltip.atkdef_clear_atk"));
            }

            foreach (Tuple<Control, Control, string> entry in this.slotTooltipTargets)
            {
                string label = LocalizationManager.T(entry.Item3);
                this.toolTip1.SetToolTip(entry.Item1, label);
                this.toolTip1.SetToolTip(entry.Item2, label);
            }
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
                    UpdateUI();
                    break;
                case MessageCode.LANGUAGE_CHANGED:
                    RefreshSlotTooltips();
                    break;
                case MessageCode.TURN_ON:
                    ProfileSingleton.GetCurrent().AtkDefMode.Start();
                    break;
                case MessageCode.TURN_OFF:
                    ProfileSingleton.GetCurrent().AtkDefMode.Stop();
                    break;
            }
        }

        private void UpdateUI()
        {
            this.isUpdatingUi = true;
            try
            {
                ATKDEFMode atkDef = ProfileSingleton.GetCurrent().AtkDefMode;
                this.inSpammerKey.Text = atkDef.keySpammer.ToString();
                FormUtils.SetNumericValue(this.spammerDelay, atkDef.ahkDelay);
                FormUtils.SetNumericValue(this.switchDelay, atkDef.switchDelay);
                this.inSpammerClick.Checked = atkDef.keySpammerWithClick;

                Dictionary<string, Key> atkKeys = atkDef.GetKeysSnapshot(ATKDEFEnum.ATK);
                Dictionary<string, Key> defKeys = atkDef.GetKeysSnapshot(ATKDEFEnum.DEF);

                foreach (TextBox tb in this.defSlotBoxes)
                {
                    tb.Text = defKeys.ContainsKey(tb.Name) ? defKeys[tb.Name].ToString() : Key.None.ToString();
                }

                foreach (TextBox tb in this.atkSlotBoxes)
                {
                    tb.Text = atkKeys.ContainsKey(tb.Name) ? atkKeys[tb.Name].ToString() : Key.None.ToString();
                }
            }
            finally
            {
                this.isUpdatingUi = false;
            }
        }

        private void onDelayChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            NumericUpDown delayInput = sender as NumericUpDown;
            if (delayInput == null)
            {
                return;
            }

            if (delayInput.Name == "spammerDelay")
            {
                ProfileSingleton.GetCurrent().AtkDefMode.ahkDelay = decimal.ToInt32(delayInput.Value);
            }
            else
            {
                ProfileSingleton.GetCurrent().AtkDefMode.switchDelay = decimal.ToInt32(delayInput.Value);
            }

            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AtkDefMode);
        }

        private void onTextChange(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            TextBox textBox = sender as TextBox;
            if (textBox == null || textBox.Tag == null)
            {
                return;
            }

            Key key;
            if (!KeyHelper.TryParseKey(textBox.Text, out key))
            {
                return;
            }

            string tagText = textBox.Tag.ToString();
            if (string.Equals(tagText, "spammerKey", StringComparison.Ordinal))
            {
                ProfileSingleton.GetCurrent().AtkDefMode.keySpammer = key;
            }
            else
            {
                short parsedMode;
                if (!short.TryParse(tagText, out parsedMode))
                {
                    return;
                }

                ATKDEFEnum mode = (ATKDEFEnum)parsedMode;
                ProfileSingleton.GetCurrent().AtkDefMode.AddSwitchItem(textBox.Name, key, mode);
            }

            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AtkDefMode);
        }

        private void ChkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (this.isUpdatingUi || ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            ProfileSingleton.GetCurrent().AtkDefMode.keySpammerWithClick = this.inSpammerClick.Checked;
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AtkDefMode);
        }

        public void SetupInputs()
        {
            foreach (Control c in FormUtils.GetAll(this, typeof(TextBox)))
            {
                TextBox textBox = c as TextBox;
                if (textBox != null)
                {
                    textBox.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
                    textBox.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
                    textBox.TextChanged += new EventHandler(this.onTextChange);
                }
            }
        }
    }
}
