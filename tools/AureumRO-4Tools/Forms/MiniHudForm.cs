using System;
using System.Drawing;
using System.Windows.Forms;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public class MiniHudForm : Form
    {
        private readonly Label lblStatusBadge;
        private readonly Label lblHeaderInfo;
        private readonly Button btnClose;
        private readonly VitalBar hpBar;
        private readonly VitalBar spBar;
        private readonly Label lblMapInfo;
        private readonly Label lblBuffCount;
        private readonly Font badgeFont;
        private readonly Font headerFont;
        private readonly Font closeFont;
        private readonly Font telemetryFont;

        private Color borderColor = Color.FromArgb(170, 175, 185);
        private bool isDragging;
        private Point dragStartCursor;
        private Point dragStartLocation;

        public event EventHandler CloseRequested;
        public event EventHandler PositionChanged;

        public string StatusBadgeText => this.lblStatusBadge != null ? this.lblStatusBadge.Text : string.Empty;
        public string MapInfoText => this.lblMapInfo != null ? this.lblMapInfo.Text : string.Empty;
        public string BuffCountText => this.lblBuffCount != null ? this.lblBuffCount.Text : string.Empty;

        public MiniHudForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.Opacity = 0.92;
            this.DoubleBuffered = true;
            this.ClientSize = new Size(252, 92);
            this.Text = "AureumRO 4Tools Mini HUD";

            this.badgeFont = new Font("Microsoft Sans Serif", 7.25F, FontStyle.Bold);
            this.headerFont = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold);
            this.closeFont = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold);
            this.telemetryFont = new Font("Microsoft Sans Serif", 7.25F, FontStyle.Regular);

            this.lblStatusBadge = new Label
            {
                Location = new Point(6, 6),
                Name = "lblHudStatusBadge",
                Size = new Size(52, 18),
                Font = this.badgeFont,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(200, 45, 45),
                Text = "OFF",
                Tag = ThemeManager.PRESERVE_COLORS_TAG,
                Cursor = Cursors.SizeAll
            };

            this.lblHeaderInfo = new Label
            {
                Location = new Point(62, 7),
                Name = "lblHudHeaderInfo",
                Size = new Size(162, 16),
                Font = this.headerFont,
                AutoEllipsis = true,
                Text = "Default",
                Cursor = Cursors.SizeAll
            };

            this.btnClose = new Button
            {
                Location = new Point(228, 5),
                Name = "btnHudClose",
                Size = new Size(18, 18),
                FlatStyle = FlatStyle.Flat,
                Font = this.closeFont,
                Text = "x",
                TabStop = false,
                Padding = new Padding(0),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.Click += OnCloseButtonClick;

            this.hpBar = new VitalBar("HP", Color.FromArgb(220, 76, 76))
            {
                Location = new Point(6, 29),
                Name = "hudHpBar",
                Size = new Size(240, 16),
                Cursor = Cursors.SizeAll
            };

            this.spBar = new VitalBar("SP", Color.FromArgb(66, 135, 245))
            {
                Location = new Point(6, 49),
                Name = "hudSpBar",
                Size = new Size(240, 16),
                Cursor = Cursors.SizeAll
            };

            this.lblMapInfo = new Label
            {
                Location = new Point(6, 70),
                Name = "lblHudMapInfo",
                Size = new Size(164, 16),
                Font = this.telemetryFont,
                AutoEllipsis = true,
                Text = "Mapa: --",
                Tag = ThemeManager.TAG_MUTED_TEXT,
                Cursor = Cursors.SizeAll
            };

            this.lblBuffCount = new Label
            {
                Location = new Point(172, 70),
                Name = "lblHudBuffCount",
                Size = new Size(74, 16),
                Font = this.telemetryFont,
                TextAlign = ContentAlignment.MiddleRight,
                Text = "Buffs: 0",
                Tag = ThemeManager.TAG_ACCENT_TEXT,
                Cursor = Cursors.SizeAll
            };

            this.Controls.Add(this.lblStatusBadge);
            this.Controls.Add(this.lblHeaderInfo);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.hpBar);
            this.Controls.Add(this.spBar);
            this.Controls.Add(this.lblMapInfo);
            this.Controls.Add(this.lblBuffCount);

            AttachDragHandlers(this);
            AttachDragHandlers(this.lblStatusBadge);
            AttachDragHandlers(this.lblHeaderInfo);
            AttachDragHandlers(this.hpBar);
            AttachDragHandlers(this.spBar);
            AttachDragHandlers(this.lblMapInfo);
            AttachDragHandlers(this.lblBuffCount);
        }

        private void AttachDragHandlers(Control control)
        {
            control.MouseDown += OnDragMouseDown;
            control.MouseMove += OnDragMouseMove;
            control.MouseUp += OnDragMouseUp;
        }

        private void OnDragMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            this.isDragging = true;
            this.dragStartCursor = Cursor.Position;
            this.dragStartLocation = this.Location;
        }

        private void OnDragMouseMove(object sender, MouseEventArgs e)
        {
            if (!this.isDragging)
            {
                return;
            }

            Point current = Cursor.Position;
            int dx = current.X - this.dragStartCursor.X;
            int dy = current.Y - this.dragStartCursor.Y;
            this.Location = new Point(this.dragStartLocation.X + dx, this.dragStartLocation.Y + dy);
        }

        private void OnDragMouseUp(object sender, MouseEventArgs e)
        {
            if (!this.isDragging)
            {
                return;
            }

            this.isDragging = false;
            EventHandler handler = PositionChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void OnCloseButtonClick(object sender, EventArgs e)
        {
            EventHandler handler = CloseRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
            else
            {
                this.Hide();
            }
        }

        public void SetSavedPosition(int x, int y)
        {
            if (x >= 0 && y >= 0)
            {
                Rectangle candidate = new Rectangle(x, y, this.Width, this.Height);
                foreach (Screen screen in Screen.AllScreens)
                {
                    if (screen.WorkingArea.IntersectsWith(candidate))
                    {
                        this.Location = new Point(x, y);
                        return;
                    }
                }
            }

            Rectangle workArea = Screen.PrimaryScreen != null
                ? Screen.PrimaryScreen.WorkingArea
                : new Rectangle(0, 0, 1280, 720);
            this.Location = new Point(Math.Max(0, workArea.Right - this.Width - 20), workArea.Top + 20);
        }

        public void UpdateStatus(bool isOn, bool isChatPaused)
        {
            UpdateStatus(isOn, isChatPaused, false, false);
        }

        public void UpdateStatus(bool isOn, bool isChatPaused, bool isTownPaused, bool isUnfocused)
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (!isOn)
            {
                this.lblStatusBadge.Text = "OFF";
                this.lblStatusBadge.BackColor = Color.FromArgb(200, 45, 45);
                this.lblStatusBadge.ForeColor = Color.White;
            }
            else if (isChatPaused)
            {
                this.lblStatusBadge.Text = "CHAT";
                this.lblStatusBadge.BackColor = Color.DarkOrange;
                this.lblStatusBadge.ForeColor = Color.White;
            }
            else if (isTownPaused)
            {
                this.lblStatusBadge.Text = "CIDADE";
                this.lblStatusBadge.BackColor = Color.FromArgb(236, 200, 97);
                this.lblStatusBadge.ForeColor = Color.FromArgb(18, 12, 3);
            }
            else if (isUnfocused)
            {
                this.lblStatusBadge.Text = "FOCO";
                this.lblStatusBadge.BackColor = Color.FromArgb(100, 112, 130);
                this.lblStatusBadge.ForeColor = Color.White;
            }
            else
            {
                this.lblStatusBadge.Text = "ON";
                this.lblStatusBadge.BackColor = Color.FromArgb(40, 155, 70);
                this.lblStatusBadge.ForeColor = Color.White;
            }
        }

        public void UpdateTelemetry(string mapName, int activeBuffCount)
        {
            if (this.IsDisposed)
            {
                return;
            }

            string normalizedMap = string.IsNullOrWhiteSpace(mapName) ? "--" : mapName.Trim();
            this.lblMapInfo.Text = "Mapa: " + normalizedMap;
            this.lblBuffCount.Text = "Buffs: " + Math.Max(0, activeBuffCount);
        }

        public void UpdateIdentity(string profileName, string characterName)
        {
            if (this.IsDisposed)
            {
                return;
            }

            string prof = string.IsNullOrWhiteSpace(profileName) ? "Default" : profileName.Trim();
            string ch = string.IsNullOrWhiteSpace(characterName) || string.Equals(characterName, "- -", StringComparison.Ordinal)
                ? string.Empty
                : characterName.Trim();

            this.lblHeaderInfo.Text = string.IsNullOrEmpty(ch) ? prof : prof + " - " + ch;
        }

        public void UpdateVitals(uint currentHp, uint maxHp, uint currentSp, uint maxSp)
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.hpBar.UpdateValues(currentHp, maxHp);
            this.spBar.UpdateValues(currentSp, maxSp);
        }

        public void ClearVitals()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.hpBar.ClearValues();
            this.spBar.ClearValues();
            this.lblMapInfo.Text = "Mapa: --";
            this.lblBuffCount.Text = "Buffs: 0";
        }

        public void ApplyHudTheme(bool darkMode)
        {
            if (this.IsDisposed)
            {
                return;
            }

            ThemeManager.ApplyTheme(this, darkMode);
            this.borderColor = darkMode ? Color.FromArgb(180, 138, 44) : Color.FromArgb(168, 145, 105);
            this.btnClose.FlatStyle = FlatStyle.Flat;
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.BackColor = this.BackColor;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen pen = new Pen(this.borderColor, 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.ClientSize.Width - 1, this.ClientSize.Height - 1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.badgeFont != null)
                {
                    this.badgeFont.Dispose();
                }
                if (this.headerFont != null)
                {
                    this.headerFont.Dispose();
                }
                if (this.closeFont != null)
                {
                    this.closeFont.Dispose();
                }
                if (this.telemetryFont != null)
                {
                    this.telemetryFont.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
