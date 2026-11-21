using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Utils;

namespace _4RTools.Model
{
    internal class BuffRenderer
    {
        private const int BUFFS_PER_ROW = 5;
        private const int DISTANCE_BETWEEN_CONTAINERS = 10;
        private const int DISTANCE_BETWEEN_ROWS = 30;
        private const int ITEM_COLUMN_WIDTH = 100;
        private const int ITEM_START_X = 10;
        private const int ITEM_START_Y = 20;

        private sealed class RenderedBuffItem
        {
            public Buff Skill { get; set; }
            public PictureBox IconBox { get; set; }
            public TextBox KeyBox { get; set; }
        }

        private readonly string _modelName;
        private readonly List<BuffContainer> _containers;
        private readonly ToolTip _toolTip;
        private readonly Dictionary<BuffContainer, List<RenderedBuffItem>> _renderedItems = new Dictionary<BuffContainer, List<RenderedBuffItem>>();
        private readonly Dictionary<string, bool> _containerVisible = new Dictionary<string, bool>(StringComparer.Ordinal);
        private int _initialTopY;
        private bool _preRenewalOnly;
        private BuffOrigin? _originFilter;
        private string _lastSearchQuery = string.Empty;

        public BuffRenderer(string model, List<BuffContainer> containers, ToolTip toolTip)
        {
            this._modelName = model;
            this._containers = containers;
            this._toolTip = toolTip;
        }

        public void doRender(int startY = -1)
        {
            if (this._containers == null || this._containers.Count == 0)
            {
                return;
            }

            this._initialTopY = startY >= 0 ? startY : this._containers[0].container.Location.Y;

            for (int i = 0; i < this._containers.Count; i++)
            {
                BuffContainer bk = this._containers[i];
                List<RenderedBuffItem> items = new List<RenderedBuffItem>();
                this._renderedItems[bk] = items;

                bk.container.SuspendLayout();
                foreach (Buff skill in bk.skills)
                {
                    PictureBox pb = new PictureBox();
                    TextBox textBox = new TextBox();

                    string displayName = LocalizationManager.GetBuffDisplayName(skill.name);
                    pb.BackColor = Color.Transparent;
                    pb.Image = ThemeManager.StripLightBackground(skill.icon);
                    pb.SizeMode = PictureBoxSizeMode.CenterImage;
                    pb.BackgroundImageLayout = ImageLayout.Center;
                    pb.Name = "pbox" + (int)skill.effectStatusID;
                    pb.Size = new Size(26, 26);
                    this._toolTip.SetToolTip(pb, displayName);

                    textBox.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
                    textBox.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
                    textBox.TextChanged += new EventHandler(onTextChange);
                    textBox.Size = new Size(55, 20);
                    textBox.Tag = (int)skill.effectStatusID;
                    textBox.Name = "in" + (int)skill.effectStatusID;
                    this._toolTip.SetToolTip(textBox, displayName);

                    bk.container.Controls.Add(textBox);
                    bk.container.Controls.Add(pb);

                    items.Add(new RenderedBuffItem
                    {
                        Skill = skill,
                        IconBox = pb,
                        KeyBox = textBox
                    });
                }
                bk.container.ResumeLayout(false);
            }

            ApplyFilter(string.Empty);
        }

        public void RefreshLocalizedTooltips()
        {
            if (this._toolTip == null)
            {
                return;
            }

            foreach (List<RenderedBuffItem> items in this._renderedItems.Values)
            {
                foreach (RenderedBuffItem item in items)
                {
                    if (item == null || item.Skill == null)
                    {
                        continue;
                    }

                    string displayName = LocalizationManager.GetBuffDisplayName(item.Skill.name);
                    if (item.IconBox != null && !item.IconBox.IsDisposed)
                    {
                        this._toolTip.SetToolTip(item.IconBox, displayName);
                    }
                    if (item.KeyBox != null && !item.KeyBox.IsDisposed)
                    {
                        this._toolTip.SetToolTip(item.KeyBox, displayName);
                    }
                }
            }
        }

        public void SetPreRenewalOnly(bool preRenewalOnly)
        {
            this._preRenewalOnly = preRenewalOnly;
            ApplyFilter(this._lastSearchQuery);
        }

        public void SetOriginFilter(BuffOrigin? origin)
        {
            this._originFilter = origin;
            ApplyFilter(this._lastSearchQuery);
        }

        public bool IsContainerVisible(string containerName)
        {
            bool visible;
            return !string.IsNullOrEmpty(containerName)
                && this._containerVisible.TryGetValue(containerName, out visible)
                && visible;
        }

        public void ApplyFilter(string searchQuery)
        {
            if (this._containers == null || this._containers.Count == 0)
            {
                return;
            }

            string query = string.IsNullOrWhiteSpace(searchQuery) ? string.Empty : searchQuery.Trim();
            this._lastSearchQuery = query;
            ScrollableControl parentForm = this._containers[0].container.Parent as ScrollableControl;

            if (parentForm != null)
            {
                parentForm.SuspendLayout();
                parentForm.AutoScrollPosition = Point.Empty;
            }

            int currentY = this._initialTopY;

            foreach (BuffContainer bk in this._containers)
            {
                List<RenderedBuffItem> items;
                if (!this._renderedItems.TryGetValue(bk, out items))
                {
                    continue;
                }

                bool groupMatches = query.Length > 0
                    && LocalizationManager.MatchesSearchText(bk.container.Text, query);

                bk.container.SuspendLayout();

                int colCount = 0;
                int rowY = ITEM_START_Y;
                int visibleCount = 0;

                foreach (RenderedBuffItem item in items)
                {
                    bool allowedByMode = !this._preRenewalOnly || item.Skill.IsPreRenewal;
                    bool allowedByOrigin = !this._originFilter.HasValue || item.Skill.Origin == this._originFilter.Value;
                    bool visible = allowedByMode && allowedByOrigin && (query.Length == 0
                        || groupMatches
                        || LocalizationManager.MatchesBuffSearch(item.Skill.name, query));

                    item.IconBox.Visible = visible;
                    item.KeyBox.Visible = visible;

                    if (visible)
                    {
                        int x = ITEM_START_X + (colCount * ITEM_COLUMN_WIDTH);
                        item.IconBox.Location = new Point(x, rowY);
                        item.KeyBox.Location = new Point(x + 35, rowY + 3);

                        visibleCount++;
                        colCount++;
                        if (colCount == BUFFS_PER_ROW)
                        {
                            colCount = 0;
                            rowY += DISTANCE_BETWEEN_ROWS;
                        }
                    }
                }

                bk.container.Visible = visibleCount > 0;
                if (!string.IsNullOrEmpty(bk.container.Name))
                {
                    this._containerVisible[bk.container.Name] = visibleCount > 0;
                }
                if (visibleCount > 0)
                {
                    int rows = (visibleCount + BUFFS_PER_ROW - 1) / BUFFS_PER_ROW;
                    int containerHeight = ITEM_START_Y + (rows * DISTANCE_BETWEEN_ROWS) + 10;
                    bk.container.Location = new Point(bk.container.Location.X, currentY);
                    bk.container.MinimumSize = new Size(520, containerHeight);
                    bk.container.Size = new Size(520, containerHeight);
                    currentY += containerHeight + DISTANCE_BETWEEN_CONTAINERS;
                }

                bk.container.ResumeLayout(true);
            }

            if (parentForm != null)
            {
                parentForm.ResumeLayout(true);
            }
        }

        public void ClearAll()
        {
            if (this._containers == null || this._containers.Count == 0)
            {
                return;
            }

            using (ProfileSingleton.SuppressSave())
            {
                foreach (BuffContainer bk in this._containers)
                {
                    List<RenderedBuffItem> items;
                    if (!this._renderedItems.TryGetValue(bk, out items))
                    {
                        continue;
                    }

                    foreach (RenderedBuffItem item in items)
                    {
                        item.KeyBox.Text = Key.None.ToString();
                        RemoveKeyFromModel(item.Skill.effectStatusID);
                    }
                }
            }

            PersistModel();
        }

        private void RemoveKeyFromModel(EffectStatusIDs statusID)
        {
            switch (this._modelName)
            {
                case "Autobuff":
                    ProfileSingleton.GetCurrent().Autobuff.AddKeyToBuff(statusID, Key.None);
                    break;
                case "DebuffsRecovery":
                    ProfileSingleton.GetCurrent().DebuffsRecovery.AddKeyToBuff(statusID, Key.None);
                    break;
            }
        }

        private void PersistModel()
        {
            switch (this._modelName)
            {
                case "Autobuff":
                    ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().Autobuff);
                    break;
                case "DebuffsRecovery":
                    ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().DebuffsRecovery);
                    break;
            }
        }

        private void onTextChange(object sender, EventArgs e)
        {
            if (ProfileSingleton.IsSaveSuppressed)
            {
                return;
            }

            try
            {
                TextBox txtBox = sender as TextBox;
                if (txtBox == null || string.IsNullOrWhiteSpace(txtBox.Text))
                {
                    return;
                }

                Key key;
                if (!KeyHelper.TryParseKey(txtBox.Text, out key))
                {
                    return;
                }

                EffectStatusIDs statusID;
                if (txtBox.Tag is int)
                {
                    statusID = (EffectStatusIDs)(int)txtBox.Tag;
                }
                else
                {
                    statusID = (EffectStatusIDs)uint.Parse(txtBox.Name.Substring(2));
                }

                switch (this._modelName)
                {
                    case "Autobuff":
                        ProfileSingleton.GetCurrent().Autobuff.AddKeyToBuff(statusID, key);
                        ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().Autobuff);
                        break;
                    case "DebuffsRecovery":
                        ProfileSingleton.GetCurrent().DebuffsRecovery.AddKeyToBuff(statusID, key);
                        ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().DebuffsRecovery);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("BuffRenderer", "Error handling key binding change", ex);
            }
        }

        public static void doUpdate(Dictionary<EffectStatusIDs, Key> autobuffDict, Control control)
        {
            using (ProfileSingleton.SuppressSave())
            {
                FormUtils.ResetForm(control);
                foreach (KeyValuePair<EffectStatusIDs, Key> entry in autobuffDict)
                {
                    Control[] controls = control.Controls.Find("in" + (int)entry.Key, true);
                    foreach (Control found in controls)
                    {
                        TextBox textBox = found as TextBox;
                        if (textBox != null)
                        {
                            textBox.Text = entry.Value.ToString();
                        }
                    }
                }
            }
        }
    }
}