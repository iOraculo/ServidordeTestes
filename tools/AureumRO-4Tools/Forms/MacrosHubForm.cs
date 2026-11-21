using System;
using System.Drawing;
using System.Windows.Forms;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public class MacrosHubForm : Form, IObserver
    {
        private readonly SegmentedTabButton btnTabSwitch;
        private readonly SegmentedTabButton btnTabSongs;
        private readonly SegmentedTabButton btnTabAtkDef;
        private readonly Panel contentHostPanel;
        private readonly MacroSwitchForm switchForm;
        private readonly MacroSongForm songForm;
        private readonly ATKDEFForm atkDefForm;
        private readonly ToolTip hubToolTip;
        private string activeTab = "switch";

        public MacrosHubForm(Subject subject)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Name = "MacrosHubForm";
            this.Text = "Macros";
            this.Size = new Size(584, 281);
            this.hubToolTip = FormUtils.CreateHelpToolTip();

            Panel tabBar = new Panel
            {
                Location = new Point(0, 0),
                Name = "panelMacrosTabBar",
                Size = new Size(584, 34)
            };

            this.btnTabSwitch = new SegmentedTabButton("Switch Macro")
            {
                Location = new Point(10, 4),
                Name = "btnMacroTabSwitch",
                Size = new Size(180, 26),
                IsActive = true
            };
            this.btnTabSwitch.Click += delegate { SelectTab("switch"); };

            this.btnTabSongs = new SegmentedTabButton("Song Macro")
            {
                Location = new Point(196, 4),
                Name = "btnMacroTabSongs",
                Size = new Size(180, 26),
                IsActive = false
            };
            this.btnTabSongs.Click += delegate { SelectTab("songs"); };

            this.btnTabAtkDef = new SegmentedTabButton("ATK x DEF Macro")
            {
                Location = new Point(382, 4),
                Name = "btnMacroTabAtkDef",
                Size = new Size(188, 26),
                IsActive = false
            };
            this.btnTabAtkDef.Click += delegate { SelectTab("atkdef"); };

            Panel divider = new Panel
            {
                Location = new Point(0, 33),
                Size = new Size(584, 1)
            };

            tabBar.Controls.Add(this.btnTabSwitch);
            tabBar.Controls.Add(this.btnTabSongs);
            tabBar.Controls.Add(this.btnTabAtkDef);
            tabBar.Controls.Add(divider);

            this.contentHostPanel = new Panel
            {
                Location = new Point(0, 35),
                Name = "macrosContentHostPanel",
                Size = new Size(584, 246)
            };

            this.switchForm = new MacroSwitchForm(subject);
            this.songForm = new MacroSongForm(subject);
            this.atkDefForm = new ATKDEFForm(subject);

            EmbedChild(this.switchForm);
            EmbedChild(this.songForm);
            EmbedChild(this.atkDefForm);

            this.Controls.Add(tabBar);
            this.Controls.Add(this.contentHostPanel);

            RefreshTooltips();
            if (subject != null)
            {
                subject.Attach(this);
            }

            SelectTab("switch");
        }

        private void RefreshTooltips()
        {
            if (this.hubToolTip == null)
            {
                return;
            }

            this.hubToolTip.SetToolTip(this.btnTabSwitch, LocalizationManager.T("tooltip.macro_tab_switch"));
            this.hubToolTip.SetToolTip(this.btnTabSongs, LocalizationManager.T("tooltip.macro_tab_songs"));
            this.hubToolTip.SetToolTip(this.btnTabAtkDef, LocalizationManager.T("tooltip.macro_tab_atkdef"));
        }

        public void Update(ISubject subject)
        {
            Subject subj = subject as Subject;
            if (subj != null && subj.Message != null && subj.Message.code == MessageCode.LANGUAGE_CHANGED)
            {
                RefreshTooltips();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.hubToolTip != null)
            {
                this.hubToolTip.Dispose();
            }
            base.Dispose(disposing);
        }

        public string ActiveTab
        {
            get { return this.activeTab; }
        }

        private void EmbedChild(Form frm)
        {
            frm.TopLevel = false;
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Dock = DockStyle.Fill;
            this.contentHostPanel.Controls.Add(frm);
            frm.Show();
            frm.Visible = false;
        }

        public void SelectTab(string tabKey)
        {
            this.activeTab = tabKey;

            bool isSwitch = string.Equals(tabKey, "switch", StringComparison.Ordinal);
            bool isSongs = string.Equals(tabKey, "songs", StringComparison.Ordinal);
            bool isAtkDef = string.Equals(tabKey, "atkdef", StringComparison.Ordinal);

            this.btnTabSwitch.IsActive = isSwitch;
            this.btnTabSongs.IsActive = isSongs;
            this.btnTabAtkDef.IsActive = isAtkDef;

            this.switchForm.Visible = isSwitch;
            this.songForm.Visible = isSongs;
            this.atkDefForm.Visible = isAtkDef;

            if (isSwitch)
            {
                this.switchForm.BringToFront();
            }
            else if (isSongs)
            {
                this.songForm.BringToFront();
            }
            else if (isAtkDef)
            {
                this.atkDefForm.BringToFront();
            }
        }
    }
}
