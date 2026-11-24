using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using Newtonsoft.Json;
using _4RTools.Utils;

namespace _4RTools.Model
{
    public class KeyConfig
    {
        public Key key { get; set; }
        public bool ClickActive { get; set; }

        public KeyConfig() { }

        public KeyConfig(Key key, bool clickActive)
        {
            this.key = key;
            this.ClickActive = clickActive;
        }
    }

    public class AHK : Action
    {
        private const string ACTION_NAME = "AHK20";
        private readonly object entriesLock = new object();
        private volatile KeyConfig[] cachedEntriesSnapshot;
        private _4RThread thread;

        public const string COMPATIBILITY = "ahkCompatibility";
        public const string SPEED_BOOST = "ahkSpeedBoost";

        public Dictionary<string, KeyConfig> AhkEntries { get; set; } = new Dictionary<string, KeyConfig>();
        public int AhkDelay { get; set; } = 10;
        public bool mouseFlick { get; set; } = false;
        public bool noShift { get; set; } = false;
        public string ahkMode { get; set; } = COMPATIBILITY;

        public AHK()
        {
        }

        public void Start()
        {
            Stop();
            InvalidateEntriesCache();
            Client roClient = ClientSingleton.GetClient();
            if (roClient != null)
            {
                this.thread = new _4RThread(token => AHKThreadExecution(roClient, token), ACTION_NAME);
                _4RThread.Start(this.thread);
            }
        }

        private void InvalidateEntriesCache()
        {
            this.cachedEntriesSnapshot = null;
        }

        private KeyConfig[] GetCachedEntriesSnapshot()
        {
            KeyConfig[] snapshot = this.cachedEntriesSnapshot;
            if (snapshot != null)
            {
                return snapshot;
            }

            lock (this.entriesLock)
            {
                snapshot = this.AhkEntries.Values.ToArray();
                this.cachedEntriesSnapshot = snapshot;
                return snapshot;
            }
        }

        private void AHKThreadExecution(Client roClient, CancellationToken token)
        {
            if (!roClient.IsProcessAlive()
                || roClient.ShouldPauseAutomation()
                || (SessionContext.HasMultipleActiveSessions && !roClient.IsGameWindowFocused()))
            {
                return;
            }

            KeyConfig[] entriesSnapshot = GetCachedEntriesSnapshot();
            if (entriesSnapshot.Length == 0)
            {
                _4RThread.Sleep(50, token);
                return;
            }

            bool isCompatibility = string.Equals(this.ahkMode, COMPATIBILITY, StringComparison.Ordinal);

            if (isCompatibility)
            {
                if (KeyHelper.IsAltPressed())
                {
                    return;
                }

                foreach (KeyConfig config in entriesSnapshot)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (config == null || config.key == Key.None || !KeyHelper.IsKeyDown(config.key))
                    {
                        continue;
                    }

                    Keys thisk = KeyHelper.ToFormsKeys(config.key);
                    if (thisk == Keys.None)
                    {
                        continue;
                    }

                    AutoMount autoMount = ProfileSingleton.GetCurrent() != null ? ProfileSingleton.GetCurrent().AutoMount : null;
                    if (autoMount != null && autoMount.IsActiveForDismount())
                    {
                        autoMount.EnsureDismountedIfMounted(roClient, token);
                    }

                    if (config.ClickActive)
                    {
                        bool shiftPressed = false;
                        try
                        {
                            if (this.noShift)
                            {
                                Interop.keybd_event(Constants.VK_SHIFT, 0x45, Constants.KEYEVENTF_EXTENDEDKEY, 0);
                                shiftPressed = true;
                            }

                            AHKCompatibility(roClient, config, thisk, token);
                        }
                        finally
                        {
                            if (shiftPressed)
                            {
                                Interop.keybd_event(Constants.VK_SHIFT, 0x45, Constants.KEYEVENTF_EXTENDEDKEY | Constants.KEYEVENTF_KEYUP, 0);
                            }
                        }
                    }
                    else
                    {
                        AHKNoClick(roClient, config, thisk, token);
                    }
                }
            }
            else
            {
                foreach (KeyConfig config in entriesSnapshot)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (config == null || config.key == Key.None || !KeyHelper.IsKeyDown(config.key))
                    {
                        continue;
                    }

                    Keys thisk = KeyHelper.ToFormsKeys(config.key);
                    if (thisk != Keys.None)
                    {
                        AutoMount autoMount = ProfileSingleton.GetCurrent() != null ? ProfileSingleton.GetCurrent().AutoMount : null;
                        if (autoMount != null && autoMount.IsActiveForDismount())
                        {
                            autoMount.EnsureDismountedIfMounted(roClient, token);
                        }

                        AHKSpeedBoost(roClient, config, thisk, token);
                    }
                }
            }
        }

        private void AHKCompatibility(Client roClient, KeyConfig config, Keys thisk, CancellationToken token)
        {
            IntPtr hWnd = roClient.GetWindowHandle();
            if (hWnd == IntPtr.Zero)
            {
                return;
            }

            int delay = Math.Max(1, this.AhkDelay);

            if (this.mouseFlick)
            {
                while (!token.IsCancellationRequested && KeyHelper.IsKeyDown(config.key))
                {
                    if (ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther, null, false))
                    {
                        if (!_4RThread.Sleep(Math.Max(10, delay), token))
                        {
                            break;
                        }
                        continue;
                    }

                    Interop.SendKeyDown(hWnd, thisk);
                    Point current = System.Windows.Forms.Cursor.Position;
                    System.Windows.Forms.Cursor.Position = new Point(
                        current.X - Constants.MOUSE_DIAGONAL_MOVIMENTATION_PIXELS_AHK,
                        current.Y - Constants.MOUSE_DIAGONAL_MOVIMENTATION_PIXELS_AHK);
                    Interop.SendLeftClick(hWnd);
                    Point shifted = System.Windows.Forms.Cursor.Position;
                    System.Windows.Forms.Cursor.Position = new Point(
                        shifted.X + Constants.MOUSE_DIAGONAL_MOVIMENTATION_PIXELS_AHK,
                        shifted.Y + Constants.MOUSE_DIAGONAL_MOVIMENTATION_PIXELS_AHK);

                    if (!_4RThread.Sleep(delay, token))
                    {
                        break;
                    }
                }
            }
            else
            {
                while (!token.IsCancellationRequested && KeyHelper.IsKeyDown(config.key))
                {
                    if (ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther, null, false))
                    {
                        if (!_4RThread.Sleep(Math.Max(10, delay), token))
                        {
                            break;
                        }
                        continue;
                    }

                    Interop.SendKeyDown(hWnd, thisk);
                    Interop.SendLeftClick(hWnd);

                    if (!_4RThread.Sleep(delay, token))
                    {
                        break;
                    }
                }
            }
        }

        private void AHKSpeedBoost(Client roClient, KeyConfig config, Keys thisk, CancellationToken token)
        {
            IntPtr hWnd = roClient.GetWindowHandle();
            if (hWnd == IntPtr.Zero)
            {
                return;
            }

            int delay = Math.Max(1, this.AhkDelay);

            while (!token.IsCancellationRequested && !KeyHelper.IsAltPressed() && KeyHelper.IsKeyDown(config.key))
            {
                if (ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther, null, false))
                {
                    if (!_4RThread.Sleep(Math.Max(10, delay), token))
                    {
                        break;
                    }
                    continue;
                }

                Point cursorPos = System.Windows.Forms.Cursor.Position;
                Interop.SendKeyDown(hWnd, thisk);
                Interop.mouse_event(Constants.MOUSEEVENTF_LEFTDOWN, cursorPos.X, cursorPos.Y, 0, 0);
                Thread.Sleep(1);
                Interop.mouse_event(Constants.MOUSEEVENTF_LEFTUP, cursorPos.X, cursorPos.Y, 0, 0);

                if (!_4RThread.Sleep(delay, token))
                {
                    break;
                }
            }
        }

        private void AHKNoClick(Client roClient, KeyConfig config, Keys thisk, CancellationToken token)
        {
            IntPtr hWnd = roClient.GetWindowHandle();
            if (hWnd == IntPtr.Zero)
            {
                return;
            }

            int delay = Math.Max(1, this.AhkDelay);

            while (!token.IsCancellationRequested && KeyHelper.IsKeyDown(config.key))
            {
                if (ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther, null, false))
                {
                    if (!_4RThread.Sleep(Math.Max(10, delay), token))
                    {
                        break;
                    }
                    continue;
                }

                Interop.SendKeyDown(hWnd, thisk);

                if (!_4RThread.Sleep(delay, token))
                {
                    break;
                }
            }
        }

        public void AddAHKEntry(string chkboxName, KeyConfig value)
        {
            lock (this.entriesLock)
            {
                this.AhkEntries[chkboxName] = value;
                this.cachedEntriesSnapshot = null;
            }
        }

        public void RemoveAHKEntry(string chkboxName)
        {
            lock (this.entriesLock)
            {
                this.AhkEntries.Remove(chkboxName);
                this.cachedEntriesSnapshot = null;
            }
        }

        public void ClearAHKEntries()
        {
            lock (this.entriesLock)
            {
                this.AhkEntries.Clear();
                this.cachedEntriesSnapshot = null;
            }
        }

        public Dictionary<string, KeyConfig> GetEntriesSnapshot()
        {
            lock (this.entriesLock)
            {
                return new Dictionary<string, KeyConfig>(this.AhkEntries);
            }
        }

        public void Stop()
        {
            _4RThread.Stop(this.thread);
            this.thread = null;
        }

        public string GetConfiguration()
        {
            lock (this.entriesLock)
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        public string GetActionName()
        {
            return ACTION_NAME;
        }
    }
}
