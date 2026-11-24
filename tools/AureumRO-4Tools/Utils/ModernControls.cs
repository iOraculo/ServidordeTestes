using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace _4RTools.Utils
{
    public enum SidebarIconKind
    {
        Spammer,
        Debuffs,
        BuffSkills,
        BuffItems,
        Timers,
        MacroSwitch,
        MacroSongs,
        AtkDef,
        Mount,
        Utilities,
        AureumHub,
        Profiles,
        Servers,
        Guide
    }

    public class ModernCardPanel : Panel
    {
        private Color borderColor = Color.FromArgb(228, 228, 234);
        private int cornerRadius = 10;

        public Color BorderColor
        {
            get { return this.borderColor; }
            set
            {
                if (this.borderColor != value)
                {
                    this.borderColor = value;
                    Invalidate();
                }
            }
        }

        public int CornerRadius
        {
            get { return this.cornerRadius; }
            set
            {
                if (this.cornerRadius != value)
                {
                    this.cornerRadius = Math.Max(0, value);
                    Invalidate();
                }
            }
        }

        public ModernCardPanel()
        {
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            this.BackColor = Color.FromArgb(255, 255, 255);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Color outerBg = this.Parent != null ? this.Parent.BackColor : ThemeManager.GetCanvasColor(ThemeManager.IsDarkMode);
            using (SolidBrush outerBrush = new SolidBrush(outerBg))
            {
                e.Graphics.FillRectangle(outerBrush, this.ClientRectangle);
            }

            if (this.Width <= 2 || this.Height <= 2)
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using (GraphicsPath path = SidebarNavButton.CreateRoundedRect(rect, this.cornerRadius))
            using (SolidBrush fillBrush = new SolidBrush(this.BackColor))
            {
                e.Graphics.FillPath(fillBrush, path);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (this.Width <= 2 || this.Height <= 2)
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using (GraphicsPath path = SidebarNavButton.CreateRoundedRect(rect, this.cornerRadius))
            using (Pen pen = new Pen(this.borderColor, 1))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }
    }

    public class SidebarNavButton : Control
    {
        private bool isActive;
        private bool isHovered;
        private bool isDarkMode;

        public string SectionKey { get; set; }
        public SidebarIconKind IconKind { get; set; }

        public bool IsActive
        {
            get { return this.isActive; }
            set
            {
                if (this.isActive != value)
                {
                    this.isActive = value;
                    Invalidate();
                }
            }
        }

        public SidebarNavButton(string sectionKey, string label, SidebarIconKind iconKind)
        {
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            this.SectionKey = sectionKey;
            this.Text = label;
            this.IconKind = iconKind;
            this.Cursor = Cursors.Hand;
            this.Size = new Size(188, 32);
        }

        public void ApplyTheme(bool darkMode)
        {
            this.isDarkMode = darkMode;
            Invalidate();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.isHovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color idleBack = this.isDarkMode ? Color.FromArgb(18, 12, 3) : Color.FromArgb(250, 244, 224);
            Color hoverBack = this.isDarkMode ? Color.FromArgb(38, 26, 6) : Color.FromArgb(242, 229, 194);
            Color activeBack = this.isDarkMode ? Color.FromArgb(54, 37, 9) : Color.FromArgb(237, 224, 182);
            Color activeBorder = this.isDarkMode ? Color.FromArgb(148, 112, 34) : Color.FromArgb(180, 148, 86);
            Color accent = this.isDarkMode ? Color.FromArgb(244, 217, 122) : Color.FromArgb(160, 129, 35);
            Color idleText = this.isDarkMode ? Color.FromArgb(228, 216, 185) : Color.FromArgb(62, 44, 20);
            Color activeText = this.isDarkMode ? Color.FromArgb(255, 244, 207) : Color.FromArgb(61, 48, 8);

            Color fill = this.isActive ? activeBack : (this.isHovered ? hoverBack : idleBack);
            using (SolidBrush outerBg = new SolidBrush(idleBack))
            {
                g.FillRectangle(outerBg, 0, 0, this.Width, this.Height);
            }

            if (this.isActive || this.isHovered)
            {
                using (GraphicsPath pill = CreateRoundedRect(new Rectangle(1, 1, this.Width - 2, this.Height - 2), 8))
                using (SolidBrush bg = new SolidBrush(fill))
                {
                    g.FillPath(bg, pill);
                    if (this.isActive)
                    {
                        using (Pen pillBorder = new Pen(activeBorder, 1))
                        {
                            g.DrawPath(pillBorder, pill);
                        }
                    }
                }
            }

            if (this.isActive)
            {
                using (GraphicsPath bar = CreateRoundedRect(new Rectangle(2, 7, 3, this.Height - 14), 2))
                using (SolidBrush accentBrush = new SolidBrush(accent))
                {
                    g.FillPath(accentBrush, bar);
                }
            }

            Color iconColor = this.isActive ? accent : (this.isDarkMode ? Color.FromArgb(179, 160, 121) : Color.FromArgb(113, 90, 59));
            DrawVectorIcon(g, this.IconKind, new Rectangle(13, (this.Height - 14) / 2, 14, 14), iconColor);

            Rectangle textBounds = new Rectangle(35, 0, this.Width - 41, this.Height);
            TextRenderer.DrawText(
                g,
                this.Text,
                this.isActive ? FontManager.Bold(9.5F) : FontManager.Regular(9.5F),
                textBounds,
                this.isActive ? activeText : idleText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        }

        internal static GraphicsPath CreateRoundedRect(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = Math.Max(2, radius * 2);
            if (diameter > bounds.Width)
            {
                diameter = bounds.Width;
            }
            if (diameter > bounds.Height)
            {
                diameter = bounds.Height;
            }

            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void DrawVectorIcon(Graphics g, SidebarIconKind kind, Rectangle r, Color color)
        {
            using (Pen pen = new Pen(color, 1.5F))
            using (SolidBrush brush = new SolidBrush(color))
            {
                int cx = r.X + r.Width / 2;
                int cy = r.Y + r.Height / 2;

                switch (kind)
                {
                    case SidebarIconKind.Spammer:
                        // Lightning bolt
                        Point[] bolt = new Point[]
                        {
                            new Point(cx + 1, r.Y + 1),
                            new Point(r.X + 3, cy + 1),
                            new Point(cx, cy + 1),
                            new Point(cx - 1, r.Bottom - 1),
                            new Point(r.Right - 3, cy - 1),
                            new Point(cx, cy - 1)
                        };
                        g.FillPolygon(brush, bolt);
                        break;

                    case SidebarIconKind.Debuffs:
                        // Shield
                        Point[] shield = new Point[]
                        {
                            new Point(cx, r.Y + 1),
                            new Point(r.Right - 2, r.Y + 3),
                            new Point(r.Right - 3, cy + 2),
                            new Point(cx, r.Bottom - 1),
                            new Point(r.X + 3, cy + 2),
                            new Point(r.X + 2, r.Y + 3)
                        };
                        g.DrawPolygon(pen, shield);
                        break;

                    case SidebarIconKind.BuffSkills:
                        // Plus / sparkle cross
                        g.DrawLine(pen, cx, r.Y + 1, cx, r.Bottom - 1);
                        g.DrawLine(pen, r.X + 1, cy, r.Right - 1, cy);
                        g.DrawLine(pen, r.X + 3, r.Y + 3, r.Right - 3, r.Bottom - 3);
                        g.DrawLine(pen, r.Right - 3, r.Y + 3, r.X + 3, r.Bottom - 3);
                        break;

                    case SidebarIconKind.BuffItems:
                        // Potion flask
                        g.DrawLine(pen, cx - 2, r.Y + 2, cx + 2, r.Y + 2);
                        g.DrawEllipse(pen, r.X + 2, r.Y + 5, r.Width - 4, r.Height - 6);
                        break;

                    case SidebarIconKind.Timers:
                        // Clock circle with hands
                        g.DrawEllipse(pen, r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2);
                        g.DrawLine(pen, cx, cy, cx, r.Y + 4);
                        g.DrawLine(pen, cx, cy, r.Right - 4, cy);
                        break;

                    case SidebarIconKind.MacroSwitch:
                        // Swap arrows
                        g.DrawLine(pen, r.X + 2, cy - 3, r.Right - 2, cy - 3);
                        g.DrawLine(pen, r.Right - 5, cy - 6, r.Right - 2, cy - 3);
                        g.DrawLine(pen, r.X + 2, cy + 3, r.Right - 2, cy + 3);
                        g.DrawLine(pen, r.X + 5, cy + 6, r.X + 2, cy + 3);
                        break;

                    case SidebarIconKind.MacroSongs:
                        // Musical note
                        g.FillEllipse(brush, r.X + 2, r.Bottom - 6, 5, 4);
                        g.DrawLine(pen, r.X + 6, r.Bottom - 4, r.X + 6, r.Y + 2);
                        g.DrawLine(pen, r.X + 6, r.Y + 2, r.Right - 2, r.Y + 4);
                        break;

                    case SidebarIconKind.AtkDef:
                        // Crossed blades
                        g.DrawLine(pen, r.X + 2, r.Y + 2, r.Right - 2, r.Bottom - 2);
                        g.DrawLine(pen, r.Right - 2, r.Y + 2, r.X + 2, r.Bottom - 2);
                        break;

                    case SidebarIconKind.Mount:
                        // Horseshoe / bridle arc with foot tips
                        g.DrawArc(pen, r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4, 150, 240);
                        g.FillEllipse(brush, r.X + 2, r.Bottom - 4, 3, 3);
                        g.FillEllipse(brush, r.Right - 5, r.Bottom - 4, 3, 3);
                        break;

                    case SidebarIconKind.Utilities:
                        // Refresh / sync circular arrow
                        g.DrawArc(pen, r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4, 35, 285);
                        g.DrawLine(pen, r.Right - 3, r.Y + 2, r.Right - 3, r.Y + 6);
                        g.DrawLine(pen, r.Right - 7, r.Y + 6, r.Right - 3, r.Y + 6);
                        break;

                    case SidebarIconKind.Profiles:
                        // User silhouette
                        g.DrawEllipse(pen, cx - 3, r.Y + 1, 6, 6);
                        g.DrawArc(pen, r.X + 2, cy + 1, r.Width - 4, 8, 180, 180);
                        break;

                    case SidebarIconKind.Servers:
                        // Stacked server bars
                        g.DrawRectangle(pen, r.X + 2, r.Y + 2, r.Width - 4, 4);
                        g.DrawRectangle(pen, r.X + 2, r.Y + 8, r.Width - 4, 4);
                        break;

                    case SidebarIconKind.AureumHub:
                        // 4-point Aureum star / emblem
                        Point[] star = new Point[]
                        {
                            new Point(cx, r.Y),
                            new Point(cx + 2, cy - 2),
                            new Point(r.Right, cy),
                            new Point(cx + 2, cy + 2),
                            new Point(cx, r.Bottom),
                            new Point(cx - 2, cy + 2),
                            new Point(r.X, cy),
                            new Point(cx - 2, cy - 2)
                        };
                        g.FillPolygon(brush, star);
                        break;

                    case SidebarIconKind.Guide:
                        // Help circle with '?'
                        g.DrawEllipse(pen, r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2);
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            g.DrawString("?", FontManager.Bold(7.5F), brush, new RectangleF(r.X, r.Y + 0.5F, r.Width, r.Height), sf);
                        }
                        break;
                }
            }
        }
    }

    public class SegmentedTabButton : Control
    {
        private bool isActive;
        private bool isHovered;
        private bool isDarkMode;

        public bool IsActive
        {
            get { return this.isActive; }
            set
            {
                if (this.isActive != value)
                {
                    this.isActive = value;
                    Invalidate();
                }
            }
        }

        public SegmentedTabButton(string text)
        {
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            this.Text = text;
            this.Cursor = Cursors.Hand;
        }

        public void ApplyTheme(bool darkMode)
        {
            this.isDarkMode = darkMode;
            Invalidate();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.isHovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color cardBack = this.Parent != null ? this.Parent.BackColor : ThemeManager.GetCardColor(this.isDarkMode);
            Color idleFill = this.isDarkMode ? Color.FromArgb(42, 29, 8) : Color.FromArgb(242, 232, 204);
            Color hoverFill = this.isDarkMode ? Color.FromArgb(62, 43, 12) : Color.FromArgb(234, 220, 182);
            Color activeFill = this.isDarkMode ? Color.FromArgb(66, 48, 14) : Color.FromArgb(248, 236, 198);
            Color border = this.isActive
                ? (this.isDarkMode ? Color.FromArgb(228, 190, 82) : Color.FromArgb(166, 126, 26))
                : (this.isDarkMode ? Color.FromArgb(108, 80, 26) : Color.FromArgb(196, 174, 128));
            Color textColor = this.isActive
                ? (this.isDarkMode ? Color.FromArgb(255, 240, 194) : Color.FromArgb(72, 50, 8))
                : (this.isDarkMode ? Color.FromArgb(228, 216, 185) : Color.FromArgb(62, 44, 20));

            using (SolidBrush outer = new SolidBrush(cardBack))
            {
                g.FillRectangle(outer, 0, 0, this.Width, this.Height);
            }

            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            int radius = Math.Min(12, Math.Max(6, this.Height / 2));

            using (GraphicsPath pill = SidebarNavButton.CreateRoundedRect(rect, radius))
            using (SolidBrush brush = new SolidBrush(this.isActive ? activeFill : (this.isHovered ? hoverFill : idleFill)))
            using (Pen pen = new Pen(border, 1))
            {
                g.FillPath(brush, pill);
                g.DrawPath(pen, pill);
            }

            TextRenderer.DrawText(
                g,
                this.Text,
                this.isActive ? FontManager.Bold(9F) : FontManager.Regular(9F),
                rect,
                textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }

    public class StatusDot : Control
    {
        private bool isConnected;
        private Color? customColor;

        public bool IsConnected
        {
            get { return this.isConnected; }
            set
            {
                if (this.isConnected != value)
                {
                    this.isConnected = value;
                    Invalidate();
                }
            }
        }

        public Color? CustomColor
        {
            get { return this.customColor; }
            set
            {
                if (this.customColor != value)
                {
                    this.customColor = value;
                    Invalidate();
                }
            }
        }

        public StatusDot()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.BackColor = Color.Transparent;
            this.DoubleBuffered = true;
            this.Size = new Size(10, 10);
            this.Tag = ThemeManager.PRESERVE_COLORS_TAG;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color dotColor = this.customColor.HasValue
                ? this.customColor.Value
                : (this.isConnected ? Color.FromArgb(46, 204, 113) : Color.FromArgb(158, 140, 102));
            using (SolidBrush brush = new SolidBrush(dotColor))
            {
                e.Graphics.FillEllipse(brush, 1, 1, this.Width - 3, this.Height - 3);
            }
        }
    }

    /// <summary>
    /// Compact vector Sun / Moon icon toggle button for switching Light & Dark themes.
    /// </summary>
    public class ThemeToggleButton : Control
    {
        private bool isDarkMode;
        private bool isHovered;

        public bool IsDarkMode
        {
            get { return this.isDarkMode; }
            set
            {
                if (this.isDarkMode != value)
                {
                    this.isDarkMode = value;
                    Invalidate();
                }
            }
        }

        public ThemeToggleButton()
        {
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            this.Cursor = Cursors.Hand;
            this.Size = new Size(34, 26);
            this.Tag = ThemeManager.PRESERVE_COLORS_TAG;
        }

        public void ApplyTheme(bool darkMode)
        {
            this.isDarkMode = darkMode;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            this.isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.isHovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color parentBg = this.Parent != null
                ? this.Parent.BackColor
                : (this.isDarkMode ? Color.FromArgb(27, 19, 4) : Color.FromArgb(248, 238, 209));
            Color fill = this.isDarkMode
                ? (this.isHovered ? Color.FromArgb(56, 39, 12) : Color.FromArgb(38, 26, 7))
                : (this.isHovered ? Color.FromArgb(240, 226, 188) : Color.FromArgb(253, 248, 232));
            Color border = this.isDarkMode
                ? (this.isHovered ? Color.FromArgb(244, 217, 122) : Color.FromArgb(128, 96, 30))
                : (this.isHovered ? Color.FromArgb(160, 129, 35) : Color.FromArgb(204, 180, 132));
            Color iconColor = this.isDarkMode ? Color.FromArgb(244, 217, 122) : Color.FromArgb(118, 86, 22);

            using (SolidBrush bgOuter = new SolidBrush(parentBg))
            {
                g.FillRectangle(bgOuter, 0, 0, this.Width, this.Height);
            }

            using (GraphicsPath pill = SidebarNavButton.CreateRoundedRect(new Rectangle(0, 0, this.Width - 1, this.Height - 1), 8))
            using (SolidBrush fillBrush = new SolidBrush(fill))
            using (Pen borderPen = new Pen(border, 1))
            {
                g.FillPath(fillBrush, pill);
                g.DrawPath(borderPen, pill);
            }

            float cx = this.Width / 2.0F;
            float cy = this.Height / 2.0F;

            if (this.isDarkMode)
            {
                // Draw golden Sun icon (clicking switches to Light Mode)
                using (SolidBrush sunBrush = new SolidBrush(iconColor))
                using (Pen rayPen = new Pen(iconColor, 1.5F))
                {
                    g.FillEllipse(sunBrush, cx - 3.6F, cy - 3.6F, 7.2F, 7.2F);
                    for (int i = 0; i < 8; i++)
                    {
                        double rad = i * Math.PI / 4.0;
                        float x1 = cx + (float)(Math.Cos(rad) * 5.6);
                        float y1 = cy + (float)(Math.Sin(rad) * 5.6);
                        float x2 = cx + (float)(Math.Cos(rad) * 8.2);
                        float y2 = cy + (float)(Math.Sin(rad) * 8.2);
                        g.DrawLine(rayPen, x1, y1, x2, y2);
                    }
                }
            }
            else
            {
                // Draw crescent Moon icon (clicking switches to Dark Mode)
                using (SolidBrush moonBrush = new SolidBrush(iconColor))
                using (SolidBrush cutoutBrush = new SolidBrush(fill))
                {
                    g.FillEllipse(moonBrush, cx - 6.0F, cy - 6.0F, 12.0F, 12.0F);
                    g.FillEllipse(cutoutBrush, cx - 2.0F, cy - 7.0F, 10.5F, 10.5F);
                }
            }
        }
    }
}
