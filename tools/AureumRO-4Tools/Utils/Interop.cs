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

        // PINVOKES
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, int Msg, Keys wParam, int lParam);

        [DllImport("user32.dll", EntryPoint = "PostMessage", SetLastError = true)]
        public static extern bool PostMessageRaw(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

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

        public static bool SendChatCommand(IntPtr hWnd, string command)
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

            // Open RO chat box, send exact characters via WM_CHAR (keyboard-layout independent), and submit.
            SendKeyPress(hWnd, Keys.Return);
            Thread.Sleep(35);

            for (int i = 0; i < clean.Length; i++)
            {
                PostMessageRaw(hWnd, Constants.WM_CHAR, (IntPtr)clean[i], IntPtr.Zero);
                Thread.Sleep(3);
            }

            Thread.Sleep(25);
            SendKeyPress(hWnd, Keys.Return);
            return true;
        }
    }
}
