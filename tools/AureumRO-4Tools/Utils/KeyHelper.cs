using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;

namespace _4RTools.Utils
{
    public static class KeyHelper
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        public const int VK_LSHIFT = 0xA0;
        public const int VK_RSHIFT = 0xA1;
        public const int VK_LCONTROL = 0xA2;
        public const int VK_RCONTROL = 0xA3;
        public const int VK_LMENU = 0xA4;
        public const int VK_RMENU = 0xA5;
        public const int VK_LWIN = 0x5B;
        public const int VK_RWIN = 0x5C;

        private static readonly KeyConverter WpfKeyConverter = new KeyConverter();

        // Optional test hook allowing deterministic key-state verification in unit tests.
        internal static Func<int, bool> VirtualKeyDownOverride;

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

            // Generic WinForms modifier KeyCodes do not map via KeyInterop.KeyFromVirtualKey, so normalize them.
            if (keyCode == Keys.ControlKey)
            {
                return Key.LeftCtrl;
            }
            if (keyCode == Keys.ShiftKey)
            {
                return Key.LeftShift;
            }
            if (keyCode == Keys.Menu)
            {
                return Key.LeftAlt;
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

        public static bool IsVirtualKeyDown(int virtualKey)
        {
            if (virtualKey <= 0)
            {
                return false;
            }

            Func<int, bool> overrideFn = VirtualKeyDownOverride;
            if (overrideFn != null)
            {
                return overrideFn(virtualKey);
            }

            // GetAsyncKeyState queries real-time hardware key state (high bit 0x8000) without depending on
            // a per-thread Windows message pump — prevents Alt / keys from getting stuck on background threads after Alt+Tab.
            return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
        }

        public static bool IsKeyDown(Key key)
        {
            if (key == Key.None)
            {
                return false;
            }

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            if (virtualKey > 0)
            {
                return IsVirtualKeyDown(virtualKey);
            }

            Keys formsKey = ToFormsKeys(key);
            return formsKey != Keys.None && IsVirtualKeyDown((int)(formsKey & Keys.KeyCode));
        }

        public static bool IsAltPressed()
        {
            return IsVirtualKeyDown(VK_LMENU) || IsVirtualKeyDown(VK_RMENU);
        }

        public static bool IsControlPressed()
        {
            return IsVirtualKeyDown(VK_LCONTROL) || IsVirtualKeyDown(VK_RCONTROL);
        }

        public static bool IsShiftPressed()
        {
            return IsVirtualKeyDown(VK_LSHIFT) || IsVirtualKeyDown(VK_RSHIFT);
        }

        public static bool IsWinPressed()
        {
            return IsVirtualKeyDown(VK_LWIN) || IsVirtualKeyDown(VK_RWIN);
        }

        public static bool IsHoldPauseKeyDown(Key key)
        {
            if (key == Key.None)
            {
                return false;
            }

            // Treat Left/Right modifier bindings symmetrically so either Ctrl, Shift, or Alt pauses smoothly.
            if (key == Key.LeftCtrl || key == Key.RightCtrl)
            {
                return IsControlPressed();
            }
            if (key == Key.LeftShift || key == Key.RightShift)
            {
                return IsShiftPressed();
            }
            if (key == Key.LeftAlt || key == Key.RightAlt)
            {
                return IsAltPressed();
            }

            return IsKeyDown(key);
        }
    }
}
