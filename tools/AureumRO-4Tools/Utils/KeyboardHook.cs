using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace _4RTools.Utils
{
    public static class KeyboardHook
    {
        #region interop
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnhookWindowsHookEx(IntPtr idHook);
        #endregion

        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static readonly object SyncRoot = new object();
        private static IntPtr hHook = IntPtr.Zero;
        // Keep delegate alive for the lifetime of the hook to prevent GC collection.
        private static readonly HookProc hookproc = new HookProc(Filter);

        public static bool Control = false;
        public static bool Shift = false;
        public static bool Alt = false;
        public static bool Win = false;

        public delegate bool KeyPressed();

        private static readonly Dictionary<Keys, KeyPressed> handledKeysDown = new Dictionary<Keys, KeyPressed>();
        private static readonly Dictionary<Keys, KeyPressed> handledKeysUp = new Dictionary<Keys, KeyPressed>();

        public delegate bool KeyboardHookHandler(Keys key);
        public static KeyboardHookHandler KeyDown;

        private static bool Enabled;

        public static bool Enable()
        {
            lock (SyncRoot)
            {
                if (Enabled)
                {
                    return false;
                }

                try
                {
                    using (Process curProcess = Process.GetCurrentProcess())
                    using (ProcessModule curModule = curProcess.MainModule)
                    {
                        hHook = SetWindowsHookEx(Constants.WH_KEYBOARD_LL, hookproc, GetModuleHandle(curModule.ModuleName), 0);
                    }

                    if (hHook == IntPtr.Zero)
                    {
                        Logger.Error("KeyboardHook", "SetWindowsHookEx failed with error " + Marshal.GetLastWin32Error());
                        Enabled = false;
                        return false;
                    }

                    Enabled = true;
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error("KeyboardHook", "Failed to enable keyboard hook", ex);
                    Enabled = false;
                    hHook = IntPtr.Zero;
                    return false;
                }
            }
        }

        public static bool Disable()
        {
            lock (SyncRoot)
            {
                if (!Enabled || hHook == IntPtr.Zero)
                {
                    Enabled = false;
                    return false;
                }

                try
                {
                    UnhookWindowsHookEx(hHook);
                    hHook = IntPtr.Zero;
                    Enabled = false;
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error("KeyboardHook", "Failed to disable keyboard hook", ex);
                    return false;
                }
            }
        }

        private static IntPtr Filter(int nCode, IntPtr wParam, IntPtr lParam)
        {
            bool result = true;

            if (nCode >= 0)
            {
                try
                {
                    if (wParam == (IntPtr)Constants.WM_KEYDOWN_MSG_ID || wParam == (IntPtr)Constants.WM_SYSKEYDOWN)
                    {
                        Keys key = (Keys)Marshal.ReadInt32(lParam);
                        if (key == Keys.LControlKey || key == Keys.RControlKey)
                            Control = true;
                        else if (key == Keys.LShiftKey || key == Keys.RShiftKey)
                            Shift = true;
                        else if (key == Keys.RMenu || key == Keys.LMenu)
                            Alt = true;
                        else if (key == Keys.RWin || key == Keys.LWin)
                            Win = true;
                        else
                            result = OnKeyDown(key);
                    }
                    else if (wParam == (IntPtr)Constants.WM_KEYUP_MSG_ID || wParam == (IntPtr)Constants.WM_SYSKEYUP)
                    {
                        Keys key = (Keys)Marshal.ReadInt32(lParam);
                        if (key == Keys.LControlKey || key == Keys.RControlKey)
                            Control = false;
                        else if (key == Keys.LShiftKey || key == Keys.RShiftKey)
                            Shift = false;
                        else if (key == Keys.RMenu || key == Keys.LMenu)
                            Alt = false;
                        else if (key == Keys.RWin || key == Keys.LWin)
                            Win = false;
                        else
                            result = OnKeyUp(key);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("KeyboardHook", "Error processing keyboard hook event", ex);
                    result = true;
                }
            }

            return result ? CallNextHookEx(hHook, nCode, wParam, lParam) : new IntPtr(1);
        }

        public static bool AddKeyDown(Keys key, KeyPressed callback)
        {
            if (key == Keys.None || callback == null)
            {
                return false;
            }

            lock (SyncRoot)
            {
                KeyDown = null;
                handledKeysDown[key] = callback;
                return true;
            }
        }

        public static bool AddKeyUp(Keys key, KeyPressed callback)
        {
            if (key == Keys.None || callback == null)
            {
                return false;
            }

            lock (SyncRoot)
            {
                handledKeysUp[key] = callback;
                return true;
            }
        }

        public static bool Add(Keys key, KeyPressed callback)
        {
            return AddKeyDown(key, callback);
        }

        public static bool RemoveDown(Keys key)
        {
            lock (SyncRoot)
            {
                return handledKeysDown.Remove(key);
            }
        }

        public static bool RemoveUp(Keys key)
        {
            lock (SyncRoot)
            {
                return handledKeysUp.Remove(key);
            }
        }

        public static bool Remove(Keys key)
        {
            return RemoveDown(key);
        }

        private static bool OnKeyDown(Keys key)
        {
            KeyboardHookHandler globalHandler;
            KeyPressed callback = null;

            lock (SyncRoot)
            {
                globalHandler = KeyDown;
                if (globalHandler == null)
                {
                    handledKeysDown.TryGetValue(key, out callback);
                }
            }

            if (globalHandler != null)
            {
                return globalHandler(key);
            }

            return callback != null ? callback() : true;
        }

        private static bool OnKeyUp(Keys key)
        {
            KeyPressed callback;
            lock (SyncRoot)
            {
                handledKeysUp.TryGetValue(key, out callback);
            }

            return callback != null ? callback() : true;
        }

        public static string KeyToString(Keys key)
        {
            return (Control ? "Ctrl + " : "") +
                   (Alt ? "Alt + " : "") +
                   (Shift ? "Shift + " : "") +
                   (Win ? "Win + " : "") +
                   key.ToString();
        }
    }
}
