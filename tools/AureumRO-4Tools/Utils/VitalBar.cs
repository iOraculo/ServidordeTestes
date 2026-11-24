using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace _4RTools.Utils
{
    public class VitalBar : Control
    {
        private uint currentValue;
        private uint maxValue;
        private readonly string labelPrefix;
        private readonly Color fillColor;
        private Color trackColor;
        private Color borderColor;
        private Color textColor;

        public VitalBar(string prefix, Color barColor)
        {
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            this.labelPrefix = prefix ?? string.Empty;
            this.fillColor = barColor;
            this.Font = FontManager.Bold(8F);
            ApplyTheme(false);
        }

        public void ApplyTheme(bool darkMode)
        {
            this.trackColor = darkMode ? Color.FromArgb(42, 29, 8) : Color.FromArgb(234, 224, 194);
            this.borderColor = darkMode ? Color.FromArgb(110, 83, 26) : Color.FromArgb(200, 178, 132);
            this.textColor = darkMode ? Color.FromArgb(243, 234, 208) : Color.FromArgb(42, 30, 14);
            Invalidate();
        }

        public void UpdateValues(uint current, uint max)
        {
            if (this.currentValue == current && this.maxValue == max)
            {
                return;
            }

            this.currentValue = current;
            this.maxValue = max;
            Invalidate();
        }

        public void ClearValues()
        {
            UpdateValues(0, 0);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Rectangle bounds = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color outerBg = this.Parent != null ? this.Parent.BackColor : ThemeManager.GetCardColor(ThemeManager.IsDarkMode);
            using (SolidBrush outerBrush = new SolidBrush(outerBg))
            {
                g.FillRectangle(outerBrush, this.ClientRectangle);
            }

            using (GraphicsPath trackPath = SidebarNavButton.CreateRoundedRect(bounds, 5))
            {
                using (SolidBrush trackBrush = new SolidBrush(this.trackColor))
                {
                    g.FillPath(trackBrush, trackPath);
                }

                string text;
                bool mostlyFilled = false;
                if (this.maxValue > 0)
                {
                    double ratio = Math.Min(1.0, Math.Max(0.0, (double)this.currentValue / (double)this.maxValue));
                    mostlyFilled = ratio >= 0.45;
                    int fillWidth = (int)Math.Round(bounds.Width * ratio);
                    if (fillWidth > 2)
                    {
                        Rectangle fillRect = new Rectangle(1, 1, Math.Max(2, fillWidth - 1), Math.Max(2, bounds.Height - 1));
                        using (GraphicsPath fillPath = SidebarNavButton.CreateRoundedRect(fillRect, 4))
                        using (SolidBrush fillBrush = new SolidBrush(this.fillColor))
                        {
                            g.FillPath(fillBrush, fillPath);
                        }
                    }

                    int percent = (int)Math.Round(ratio * 100.0);
                    text = string.Format("{0}: {1:N0} / {2:N0} ({3}%)", this.labelPrefix, this.currentValue, this.maxValue, percent);
                }
                else
                {
                    text = this.labelPrefix + ": -- / --";
                }

                using (Pen borderPen = new Pen(this.borderColor, 1))
                {
                    g.DrawPath(borderPen, trackPath);
                }

                Color renderedTextColor = mostlyFilled ? Color.White : this.textColor;
                TextRenderer.DrawText(
                    g,
                    text,
                    FontManager.Bold(8F),
                    bounds,
                    renderedTextColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            }
        }
    }
}
