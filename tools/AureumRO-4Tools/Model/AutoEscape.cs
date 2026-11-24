using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using Newtonsoft.Json;
using _4RTools.Utils;

namespace _4RTools.Model
{
    /// <summary>
    /// Emergency Auto-Escape (Fly Wing / Butterfly Wing / Teleport skill):
    /// automatically presses the configured escape key when HP drops below the critical threshold,
    /// with a configurable cooldown to avoid burning multiple wings during teleport load.
    /// </summary>
    public class AutoEscape : Action
    {
        public const string ACTION_NAME = "AutoEscape";
        public const int DEFAULT_HP_PERCENT = 20;
        public const int MIN_HP_PERCENT = 1;
        public const int MAX_HP_PERCENT = 95;
        public const int DEFAULT_COOLDOWN_MS = 2000;
        public const int MIN_COOLDOWN_MS = 500;
        public const int MAX_COOLDOWN_MS = 30000;
        public const int TELEPORT_ENTER_DELAY_MS = 80;

        private readonly object stateLock = new object();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private _4RThread thread;
        private long lastTriggerMs = -MAX_COOLDOWN_MS;

        public bool Enabled { get; set; } = false;
        public Key EscapeKey { get; set; } = Key.None;
        public int HpPercent { get; set; } = DEFAULT_HP_PERCENT;
        public int CooldownMs { get; set; } = DEFAULT_COOLDOWN_MS;
        public bool ConfirmWithEnter { get; set; } = false;
        public bool PauseInTown { get; set; } = true;

        public int GetEffectiveHpPercent()
        {
            return Math.Max(MIN_HP_PERCENT, Math.Min(MAX_HP_PERCENT, this.HpPercent));
        }

        public int GetEffectiveCooldownMs()
        {
            return Math.Max(MIN_COOLDOWN_MS, Math.Min(MAX_COOLDOWN_MS, this.CooldownMs));
        }

        public bool ShouldTriggerEscape(uint currentHp, uint maxHp, long nowMs, bool isInTown = false)
        {
            if (!this.Enabled || !FormUtils.IsValidKey(this.EscapeKey))
            {
                return false;
            }

            if (this.PauseInTown && isInTown)
            {
                return false;
            }

            // Never attempt to escape when dead (HP == 0) or before vitals are initialized.
            if (currentHp == 0 || maxHp == 0)
            {
                return false;
            }

            if (!Client.IsBelowPercent(currentHp, maxHp, GetEffectiveHpPercent()))
            {
                return false;
            }

            int cooldown = GetEffectiveCooldownMs();
            lock (this.stateLock)
            {
                return (nowMs - this.lastTriggerMs) >= cooldown;
            }
        }

        public void RecordEscapeTriggered(long nowMs)
        {
            lock (this.stateLock)
            {
                this.lastTriggerMs = nowMs;
            }
        }

        public void ResetCooldown()
        {
            lock (this.stateLock)
            {
                this.lastTriggerMs = -MAX_COOLDOWN_MS;
            }
        }

        public void Start()
        {
            Stop();
            ResetCooldown();

            if (!this.Enabled || !FormUtils.IsValidKey(this.EscapeKey))
            {
                return;
            }

            Client roClient = ClientSingleton.GetClient();
            if (roClient != null)
            {
                this.thread = CreateEscapeThread(roClient);
                _4RThread.Start(this.thread);
            }
        }

        public void Stop()
        {
            _4RThread.Stop(this.thread);
            this.thread = null;
        }

        private _4RThread CreateEscapeThread(Client client)
        {
            byte[] vitalsBuffer = new byte[Constants.HP_SP_BLOCK_BYTE_SIZE];

            return new _4RThread(token =>
            {
                if (this.Enabled
                    && FormUtils.IsValidKey(this.EscapeKey)
                    && client.HasMemorySupport
                    && client.IsProcessAlive()
                    && !client.ShouldPauseForFocus()
                    && !ActionPriorityCoordinator.IsHoldPauseBlockingAutopot()
                    && !AutoRefreshCommand.IsSendingCommand
                    && !ActionPriorityCoordinator.ShouldYield(ActionPriority.Yggdrasil))
                {
                    bool inTown = this.PauseInTown && client.IsInTown();
                    uint currentHp;
                    uint maxHp;
                    uint currentSp;
                    uint maxSp;
                    if (!inTown && client.TryReadVitals(vitalsBuffer, out currentHp, out maxHp, out currentSp, out maxSp))
                    {
                        long nowMs = this.clock.ElapsedMilliseconds;
                        if (ShouldTriggerEscape(currentHp, maxHp, nowMs, inTown))
                        {
                            IntPtr hWnd = client.GetWindowHandle();
                            Keys formsKey = KeyHelper.ToFormsKeys(this.EscapeKey);
                            if (hWnd != IntPtr.Zero && formsKey != Keys.None)
                            {
                                if (Autopot.NeedsChatCloseToPot(this.EscapeKey, ChatPauseState.IsChatting))
                                {
                                    if (ChatPauseState.TryConsumeChatting())
                                    {
                                        Interop.SendKeyPress(hWnd, Keys.Return);
                                    }

                                    if (!_4RThread.Sleep(35, token))
                                    {
                                        return;
                                    }
                                }

                                RecordEscapeTriggered(nowMs);
                                ActionPriorityCoordinator.NotifyEmergencyAction();
                                Interop.SendKeyPress(hWnd, formsKey);

                                if (this.ConfirmWithEnter && !ChatPauseState.IsChatting)
                                {
                                    if (_4RThread.Sleep(TELEPORT_ENTER_DELAY_MS, token) && !ChatPauseState.IsChatting)
                                    {
                                        Interop.SendKeyPress(hWnd, Keys.Return);
                                    }
                                }
                            }
                        }
                    }
                }

                _4RThread.Sleep(40, token);
            }, ACTION_NAME);
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
