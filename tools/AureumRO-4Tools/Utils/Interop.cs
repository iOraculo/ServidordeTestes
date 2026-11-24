using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace _4RTools.Utils
{
    internal static class Interop
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct INPUTUNION
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct INPUT
        {
            public uint type;
            public INPUTUNION U;
        }

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP_FLAG = 0x0002;
        private const uint KEYEVENTF_UNICODE_FLAG = 0x0004;
        private const int SW_RESTORE = 9;

        // PINVOKES
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, int Msg, Keys wParam, int lParam);

        [DllImport("user32.dll", EntryPoint = "PostMessage", SetLastError = true)]
        public static extern bool PostMessageRaw(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static void SendKeyDown(IntPtr hWnd, Keys key)
        {
            if (hWnd != IntPtr.Zero && key != Keys.None)
            {
                PostMessage(hWnd, Constants.WM_KEYDOWN_MSG_ID, key, 0);
            }
        }

        public static void SendKeyPress(IntPtr hWnd, Keys key)
        {
            if (hWnd != IntPtr.Zero && key != Keys.None)
            {
                PostMessage(hWnd, Constants.WM_KEYDOWN_MSG_ID, key, 0);
                PostMessage(hWnd, Constants.WM_KEYUP_MSG_ID, key, 0);
            }
        }

        public static void SendLeftClick(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                return;
            }

            PostMessage(hWnd, Constants.WM_LBUTTONDOWN, Keys.None, 0);
            Thread.Sleep(1);
            PostMessage(hWnd, Constants.WM_LBUTTONUP, Keys.None, 0);
        }

        public static int MakeLParam(int x, int y)
        {
            return ((y & 0xFFFF) << 16) | (x & 0xFFFF);
        }

        public static void SendLeftClickAtCenter(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                return;
            }

            RECT rect;
            if (!GetClientRect(hWnd, out rect))
            {
                SendLeftClick(hWnd);
                return;
            }

            int cx = Math.Max(0, (rect.Right - rect.Left) / 2);
            int cy = Math.Max(0, (rect.Bottom - rect.Top) / 2);
            int lParam = MakeLParam(cx, cy);

            PostMessage(hWnd, Constants.WM_MOUSEMOVE, Keys.None, lParam);
            Thread.Sleep(15);
            PostMessage(hWnd, Constants.WM_LBUTTONDOWN, (Keys)Constants.MK_LBUTTON, lParam);
            Thread.Sleep(20);
            PostMessage(hWnd, Constants.WM_LBUTTONUP, Keys.None, lParam);
        }

        public static bool SendUnicodeChar(IntPtr hWnd, char ch)
        {
            INPUT[] inputs = new INPUT[2];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki.wVk = 0;
            inputs[0].U.ki.wScan = ch;
            inputs[0].U.ki.dwFlags = KEYEVENTF_UNICODE_FLAG;

            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].U.ki.wVk = 0;
            inputs[1].U.ki.wScan = ch;
            inputs[1].U.ki.dwFlags = KEYEVENTF_UNICODE_FLAG | KEYEVENTF_KEYUP_FLAG;

            uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            if (sent == 0 && hWnd != IntPtr.Zero)
            {
                // Fallback to direct WM_CHAR if SendInput was blocked.
                return PostMessageRaw(hWnd, Constants.WM_CHAR, (IntPtr)ch, IntPtr.Zero);
            }

            return sent > 0;
        }

        public static bool SendChatCommand(IntPtr hWnd, string command, bool chatAlreadyOpen = false, bool focusWindowIfNeeded = false)
        {
            if (hWnd == IntPtr.Zero || string.IsNullOrWhiteSpace(command))
            {
                return false;
            }

            string clean = command.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
            if (string.IsNullOrEmpty(clean))
            {
                return false;
            }

            if (GetForegroundWindow() != hWnd)
            {
                if (!focusWindowIfNeeded)
                {
                    return false;
                }

                if (IsIconic(hWnd))
                {
                    ShowWindow(hWnd, SW_RESTORE);
                }
                SetForegroundWindow(hWnd);
                Thread.Sleep(90);

                if (GetForegroundWindow() != hWnd)
                {
                    return false;
                }
            }

            // Open RO chat box via PostMessage (never triggers WH_KEYBOARD_LL ChatPauseState),
            // type characters via SendInput(KEYEVENTF_UNICODE) so Ragexe 2022's focused Edit/IME control receives them,
            // then submit and close chat with Return.
            if (!chatAlreadyOpen)
            {
                SendKeyPress(hWnd, Keys.Return);
                Thread.Sleep(75);
            }

            for (int i = 0; i < clean.Length; i++)
            {
                SendUnicodeChar(hWnd, clean[i]);
                Thread.Sleep(6);
            }

            Thread.Sleep(45);
            SendKeyPress(hWnd, Keys.Return);
            return true;
        }
    }
}
