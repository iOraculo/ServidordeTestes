using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using Newtonsoft.Json;
using _4RTools.Utils;

namespace _4RTools.Model
{
    /// <summary>
    /// Anti-Position Bug helper that dispatches the server's @refresh chat command
    /// either on demand via a 1-tap hotkey or periodically on a configurable interval.
    /// </summary>
    public class AutoRefreshCommand : Action
    {
        public const string ACTION_NAME = "AutoRefreshCommand";
        public const string DEFAULT_COMMAND = "@refresh";
        public const int DEFAULT_INTERVAL_SECONDS = 30;
        public const int MIN_INTERVAL_SECONDS = 3;
        public const int MAX_INTERVAL_SECONDS = 3600;
        public const int MANUAL_DEBOUNCE_MS = 1500;

        private static int sendingCommandFlag = 0;

        public static bool IsSendingCommand
        {
            get { return Volatile.Read(ref sendingCommandFlag) == 1; }
        }

        private readonly object stateLock = new object();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private _4RThread periodicThread;
        private bool hookSubscribed;
        private long lastManualTriggerMs = -MANUAL_DEBOUNCE_MS;
        private long lastRefreshMs = -MANUAL_DEBOUNCE_MS;

        public bool Enabled { get; set; } = false;
        public Key Hotkey { get; set; } = Key.None;
        public bool AutoIntervalEnabled { get; set; } = false;
        public int IntervalSeconds { get; set; } = DEFAULT_INTERVAL_SECONDS;
        public string CommandText { get; set; } = DEFAULT_COMMAND;
        public bool PauseWhenSitting { get; set; } = true;

        public static string NormalizeCommand(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return DEFAULT_COMMAND;
            }

            string cleaned = raw.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
            return string.IsNullOrEmpty(cleaned) ? DEFAULT_COMMAND : cleaned;
        }

        public int GetEffectiveIntervalSeconds()
        {
            return Math.Max(MIN_INTERVAL_SECONDS, Math.Min(MAX_INTERVAL_SECONDS, this.IntervalSeconds));
        }

        public bool CanTriggerManual(long nowMs)
        {
            if (!this.Enabled || !FormUtils.IsValidKey(this.Hotkey))
            {
                return false;
            }

            lock (this.stateLock)
            {
                return (nowMs - this.lastManualTriggerMs) >= MANUAL_DEBOUNCE_MS;
            }
        }

        public bool ShouldTriggerPeriodic(long nowMs, bool isSitting = false)
        {
            if (!this.Enabled || !this.AutoIntervalEnabled)
            {
                return false;
            }

            if (this.PauseWhenSitting && isSitting)
            {
                return false;
            }

            long intervalMs = GetEffectiveIntervalSeconds() * 1000L;
            lock (this.stateLock)
            {
                return (nowMs - this.lastRefreshMs) >= intervalMs;
            }
        }

        public void RecordRefreshExecuted(long nowMs, bool isManual = false)
        {
            lock (this.stateLock)
            {
                this.lastRefreshMs = nowMs;
                if (isManual)
                {
                    this.lastManualTriggerMs = nowMs;
                }
            }
        }

        public void ResetTimer()
        {
            lock (this.stateLock)
            {
                this.lastManualTriggerMs = -MANUAL_DEBOUNCE_MS;
                this.lastRefreshMs = -MANUAL_DEBOUNCE_MS;
            }
        }

        public void Start()
        {
            Stop();

            if (!this.Enabled)
            {
                return;
            }

            Client roClient = ClientSingleton.GetClient();
            if (roClient == null)
            {
                return;
            }

            lock (this.stateLock)
            {
                // Manual hotkey works immediately after turning ON, while periodic countdown waits one full interval.
                this.lastManualTriggerMs = -MANUAL_DEBOUNCE_MS;
                this.lastRefreshMs = this.clock.ElapsedMilliseconds;
                if (FormUtils.IsValidKey(this.Hotkey) && !this.hookSubscribed)
                {
                    KeyboardHook.PhysicalKeyDown += OnPhysicalKeyDown;
                    this.hookSubscribed = true;
                }
            }

            if (this.AutoIntervalEnabled)
            {
                this.periodicThread = CreatePeriodicThread(roClient);
                _4RThread.Start(this.periodicThread);
            }
        }

        public void Stop()
        {
            lock (this.stateLock)
            {
                if (this.hookSubscribed)
                {
                    KeyboardHook.PhysicalKeyDown -= OnPhysicalKeyDown;
                    this.hookSubscribed = false;
                }
            }

            _4RThread.Stop(this.periodicThread);
            this.periodicThread = null;
        }

        private void OnPhysicalKeyDown(Keys formsKey)
        {
            if (!this.Enabled || ChatPauseState.IsChatting || KeyHelper.IsAltPressed())
            {
                return;
            }

            Key pressedKey = KeyHelper.FromFormsKeys(formsKey);
            if (pressedKey == Key.None || pressedKey != this.Hotkey)
            {
                return;
            }

            Client roClient = ClientSingleton.GetClient();
            if (roClient == null || !roClient.IsProcessAlive() || !roClient.IsGameWindowFocused())
            {
                return;
            }

            long nowMs = this.clock.ElapsedMilliseconds;
            if (!CanTriggerManual(nowMs))
            {
                return;
            }

            RecordRefreshExecuted(nowMs, true);
            ThreadPool.QueueUserWorkItem(_ => SendRefreshCommand(roClient));
        }

        private _4RThread CreatePeriodicThread(Client client)
        {
            byte[] rawBuffer = new byte[Constants.STATUS_BUFFER_BYTE_SIZE];
            uint[] statusBuffer = new uint[Constants.MAX_BUFF_LIST_INDEX_SIZE];
            List<uint> activeStatuses = new List<uint>(32);

            return new _4RThread(token =>
            {
                if (this.Enabled
                    && this.AutoIntervalEnabled
                    && client.IsProcessAlive()
                    && !client.ShouldPauseAutomation()
                    && !KeyHelper.IsAltPressed()
                    && !ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther))
                {
                    bool isSitting = false;
                    if (this.PauseWhenSitting && client.HasMemorySupport)
                    {
                        int count = client.ReadBuffStatusBuffer(rawBuffer, statusBuffer);
                        activeStatuses.Clear();
                        Client.ExtractActiveStatuses(statusBuffer, count, activeStatuses);
                        for (int i = 0; i < activeStatuses.Count; i++)
                        {
                            if ((EffectStatusIDs)activeStatuses[i] == EffectStatusIDs.EFST_SIT)
                            {
                                isSitting = true;
                                break;
                            }
                        }
                    }

                    long nowMs = this.clock.ElapsedMilliseconds;
                    if (ShouldTriggerPeriodic(nowMs, isSitting))
                    {
                        RecordRefreshExecuted(nowMs);
                        SendRefreshCommand(client);
                    }
                }

                _4RThread.Sleep(250, token);
            }, ACTION_NAME);
        }

        public bool SendRefreshCommand(Client client)
        {
            if (client == null || !client.IsProcessAlive() || ChatPauseState.IsChatting)
            {
                return false;
            }

            IntPtr hWnd = client.GetWindowHandle();
            if (hWnd == IntPtr.Zero)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref sendingCommandFlag, 1, 0) != 0)
            {
                return false;
            }

            try
            {
                string cmd = NormalizeCommand(this.CommandText);
                return Interop.SendChatCommand(hWnd, cmd);
            }
            catch (Exception ex)
            {
                Logger.Warn("AutoRefreshCommand", "Failed to send chat command", ex);
                return false;
            }
            finally
            {
                Interlocked.Exchange(ref sendingCommandFlag, 0);
            }
        }

        public string GetConfiguration()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string GetActionName()
        {
            return ACTION_NAME;
        }
    }
}
