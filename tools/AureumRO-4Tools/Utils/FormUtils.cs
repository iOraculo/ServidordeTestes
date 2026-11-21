using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;

namespace _4RTools.Utils
{
    public class FormUtils
    {
        public const string SKIP_RESET_TAG = "skipReset";

        public static ToolTip CreateHelpToolTip(System.ComponentModel.IContainer container = null)
        {
            ToolTip tip = container != null ? new ToolTip(container) : new ToolTip();
            ConfigureHelpToolTip(tip);
            return tip;
        }

        public static void ConfigureHelpToolTip(ToolTip tip)
        {
            if (tip == null)
            {
                return;
            }

            tip.AutoPopDelay = 20000;
            tip.InitialDelay = 250;
            tip.ReshowDelay = 120;
            tip.ShowAlways = true;
        }

        public static void OnKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            try
            {
                TextBox textBox = sender as TextBox;
                if (textBox == null)
                {
                    return;
                }

                Key thisk = KeyHelper.FromFormsKeys(e.KeyCode);

                switch (thisk)
                {
                    case Key.Escape:
                    case Key.Back:
                        textBox.Text = Key.None.ToString();
                        break;
                    default:
                        if (thisk != Key.None)
                        {
                            textBox.Text = thisk.ToString();
                        }
                        break;
                }

                if (textBox.Parent != null)
                {
                    textBox.Parent.Focus();
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            catch (Exception ex)
            {
                Logger.Warn("FormUtils", "Failed to handle OnKeyDown", ex);
            }
        }

        public static bool IsValidKey(Key key)
        {
            return key != Key.Back && key != Key.Escape && key != Key.None;
        }

        public static void OnKeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        public static void SetNumericValue(NumericUpDown control, int value)
        {
            if (control == null)
            {
                return;
            }

            decimal target = value;
            if (target < control.Minimum)
            {
                target = control.Minimum;
            }
            else if (target > control.Maximum)
            {
                target = control.Maximum;
            }

            control.Value = target;
        }

        public static IEnumerable<Control> GetAll(Control control, Type type)
        {
            var controls = control.Controls.Cast<Control>();

            return controls.SelectMany(ctrl => GetAll(ctrl, type))
                           .Concat(controls)
                           .Where(c => c.GetType() == type);
        }

        private static void resetForm(Control control)
        {
            IEnumerable<Control> texts = GetAll(control, typeof(TextBox));
            IEnumerable<Control> checks = GetAll(control, typeof(CheckBox));
            IEnumerable<Control> combos = GetAll(control, typeof(ComboBox));

            foreach (Control c in texts)
            {
                TextBox textBox = (TextBox)c;
                if (string.Equals(textBox.Tag as string, SKIP_RESET_TAG, StringComparison.Ordinal))
                {
                    continue;
                }
                textBox.Text = Key.None.ToString();
            }

            foreach (Control c in checks)
            {
                CheckBox checkBox = (CheckBox)c;
                if (string.Equals(checkBox.Tag as string, SKIP_RESET_TAG, StringComparison.Ordinal))
                {
                    continue;
                }
                checkBox.Checked = false;
            }

            foreach (Control c in combos)
            {
                ComboBox comboBox = (ComboBox)c;
                if (string.Equals(comboBox.Tag as string, SKIP_RESET_TAG, StringComparison.Ordinal))
                {
                    continue;
                }
                if (comboBox.Items.Count > 0)
                {
                    comboBox.SelectedIndex = 0;
                }
            }
        }

        public static void ResetForm(Form form)
        {
            resetForm(form);
        }

        public static void ResetForm(Control form)
        {
            resetForm(form);
        }

        public static void ResetForm(Panel panel)
        {
            resetForm(panel);
        }

        public static void ResetForm(GroupBox group)
        {
            resetForm(group);
        }
    }
}
