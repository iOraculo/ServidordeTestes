using System;
using System.Windows.Forms;
using System.Windows.Input;

namespace _4RTools.Utils
{
    public static class KeyHelper
    {
        private static readonly KeyConverter WpfKeyConverter = new KeyConverter();

        public static Keys ToFormsKeys(Key key)
        {
            if (key == Key.None)
            {
                return Keys.None;
            }

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            if (virtualKey > 0)
            {
                return (Keys)virtualKey;
            }

            Keys parsed;
            return Enum.TryParse(key.ToString(), true, out parsed) ? parsed : Keys.None;
        }

        public static Key FromFormsKeys(Keys keys)
        {
            Keys keyCode = keys & Keys.KeyCode;
            if (keyCode == Keys.None)
            {
                return Key.None;
            }

            Key wpfKey = KeyInterop.KeyFromVirtualKey((int)keyCode);
            if (wpfKey != Key.None)
            {
                return wpfKey;
            }

            Key parsed;
            return Enum.TryParse(keyCode.ToString(), true, out parsed) ? parsed : Key.None;
        }

        public static bool TryParseKey(string text, out Key key)
        {
            key = Key.None;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string trimmed = text.Trim();

            if (Enum.TryParse(trimmed, true, out key))
            {
                return true;
            }

            Keys formsKey;
            if (Enum.TryParse(trimmed, true, out formsKey))
            {
                key = FromFormsKeys(formsKey);
                return key != Key.None || formsKey == Keys.None;
            }

            try
            {
                object converted = WpfKeyConverter.ConvertFromString(trimmed);
                if (converted is Key)
                {
                    key = (Key)converted;
                    return true;
                }
            }
            catch
            {
                // Ignore invalid key strings.
            }

            return false;
        }

        public static bool TryParseFormsKeys(string text, out Keys keys)
        {
            keys = Keys.None;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string trimmed = text.Trim();

            if (Enum.TryParse(trimmed, true, out keys))
            {
                return true;
            }

            Key wpfKey;
            if (TryParseKey(trimmed, out wpfKey))
            {
                keys = ToFormsKeys(wpfKey);
                return keys != Keys.None || wpfKey == Key.None;
            }

            return false;
        }

        public static bool IsAltPressed()
        {
            return Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
        }
    }
}
