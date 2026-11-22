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
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct GUITHREADINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rcCaret;
        }

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const ushort VK_RETURN = 0x0D;
        private const ushort SCAN_RETURN = 0x1C;
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
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        public static void FocusWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                return;
            }

            if (IsIconic(hWnd))
            {
                ShowWindow(hWnd, SW_RESTORE);
            }

            SetForegroundWindow(hWnd);
        }

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

        private static void SendInputReturnKey()
        {
            INPUT[] inputs = new INPUT[2];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki.wVk = VK_RETURN;
            inputs[0].U.ki.wScan = SCAN_RETURN;
            inputs[0].U.ki.dwFlags = 0;

            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].U.ki.wVk = VK_RETURN;
            inputs[1].U.ki.wScan = SCAN_RETURN;
            inputs[1].U.ki.dwFlags = KEYEVENTF_KEYUP;

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private static void SendInputUnicodeChar(char ch)
        {
            INPUT[] inputs = new INPUT[2];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].U.ki.wVk = 0;
            inputs[0].U.ki.wScan = ch;
            inputs[0].U.ki.dwFlags = KEYEVENTF_UNICODE;

            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].U.ki.wVk = 0;
            inputs[1].U.ki.wScan = ch;
            inputs[1].U.ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private static IntPtr FindChatEditWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            uint pid;
            uint threadId = GetWindowThreadProcessId(hWnd, out pid);
            if (threadId != 0)
            {
                GUITHREADINFO gui = new GUITHREADINFO();
                gui.cbSize = Marshal.SizeOf(typeof(GUITHREADINFO));
                if (GetGUIThreadInfo(threadId, ref gui) && gui.hwndFocus != IntPtr.Zero && gui.hwndFocus != hWnd)
                {
                    return gui.hwndFocus;
                }
            }

            return FindWindowEx(hWnd, IntPtr.Zero, "EDIT", null);
        }

        public static bool SendChatCommand(IntPtr hWnd, string command, bool bringToFront = false)
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

            if (bringToFront && GetForegroundWindow() != hWnd)
            {
                FocusWindow(hWnd);
                Thread.Sleep(90);
            }

            // Preferred path when the RO window is focused: use SendInput (hardware scan-code Enter +
            // KEYEVENTF_UNICODE characters), which works across all Ragexe 2022+ clients, ABNT2/US layouts,
            // and CapsLock states — routing directly to Ragexe's active chat input control.
            if (GetForegroundWindow() == hWnd)
            {
                SendInputReturnKey();
                Thread.Sleep(65);

                for (int i = 0; i < clean.Length; i++)
                {
                    SendInputUnicodeChar(clean[i]);
                    Thread.Sleep(5);
                }

                Thread.Sleep(45);
                SendInputReturnKey();
                return true;
            }

            // Background fallback: open chat on main window, target the child Win32 EDIT window for WM_CHAR,
            // and submit via Return.
            const int returnDownLParam = (SCAN_RETURN << 16) | 1;
            const int returnUpLParam = unchecked((int)(0xC0000001u | ((uint)SCAN_RETURN << 16)));

            PostMessage(hWnd, Constants.WM_KEYDOWN_MSG_ID, Keys.Return, returnDownLParam);
            PostMessage(hWnd, Constants.WM_KEYUP_MSG_ID, Keys.Return, returnUpLParam);
            Thread.Sleep(65);

            IntPtr editHwnd = FindChatEditWindow(hWnd);
            IntPtr charTarget = editHwnd != IntPtr.Zero ? editHwnd : hWnd;

            for (int i = 0; i < clean.Length; i++)
            {
                PostMessageRaw(charTarget, Constants.WM_CHAR, (IntPtr)clean[i], (IntPtr)1);
                Thread.Sleep(5);
            }

            Thread.Sleep(45);
            PostMessage(charTarget, Constants.WM_KEYDOWN_MSG_ID, Keys.Return, returnDownLParam);
            PostMessage(charTarget, Constants.WM_KEYUP_MSG_ID, Keys.Return, returnUpLParam);
            return true;
        }
    }
}
