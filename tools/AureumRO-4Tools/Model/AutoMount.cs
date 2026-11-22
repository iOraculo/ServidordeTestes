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
    /// Handles cosmetic mount management (Halter Lead / Rédea — EFST_ALL_RIDING = 613):
    /// automatically dismounts before skills are cast (both manual keypresses and 4RTools automations),
    /// optionally pauses Auto-skill while mounted, and optionally remounts after idle time.
    /// </summary>
    public class AutoMount : Action
    {
        public const string ACTION_NAME_AUTOMOUNT = "AutoMount";
        public const int EXTRA_SKILL_SLOTS = 6;
        public const int DISMOUNT_DEBOUNCE_MS = 600;

        private readonly object stateLock = new object();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private _4RThread remountThread;
        private bool hookSubscribed;
        private long lastDismountMs = -DISMOUNT_DEBOUNCE_MS;
        private long lastSkillActivityMs = 0;
        private bool dismountedByAutoMount = false;

        public bool Enabled { get; set; } = false;
        public Key MountKey { get; set; } = Key.None;
        public bool DismountOnSkill { get; set; } = true;
        public int DismountDelayMs { get; set; } = 80;
        public bool PauseAutobuffWhileMounted { get; set; } = false;
        public bool IncludeOtherTabsSkillKeys { get; set; } = true;
        public bool AutoRemountAfterIdle { get; set; } = false;
        public int RemountDelaySec { get; set; } = 5;
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<Key> ExtraSkillKeys { get; set; } = new List<Key>();

        public AutoMount()
        {
        }

        public void EnsureExtraSkillSlotCount()
        {
            lock (this.stateLock)
            {
                if (this.ExtraSkillKeys == null)
                {
                    this.ExtraSkillKeys = new List<Key>();
                }

                while (this.ExtraSkillKeys.Count < EXTRA_SKILL_SLOTS)
                {
                    this.ExtraSkillKeys.Add(Key.None);
                }

                if (this.ExtraSkillKeys.Count > EXTRA_SKILL_SLOTS)
                {
                    this.ExtraSkillKeys.RemoveRange(EXTRA_SKILL_SLOTS, this.ExtraSkillKeys.Count - EXTRA_SKILL_SLOTS);
                }
            }
        }

        public void SetExtraSkillKey(int slotIndex, Key key)
        {
            if (slotIndex < 0 || slotIndex >= EXTRA_SKILL_SLOTS)
            {
                return;
            }

            lock (this.stateLock)
            {
                EnsureExtraSkillSlotCountUnsafe();
                this.ExtraSkillKeys[slotIndex] = FormUtils.IsValidKey(key) ? key : Key.None;
            }
        }

        public Key GetExtraSkillKey(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= EXTRA_SKILL_SLOTS)
            {
                return Key.None;
            }

            lock (this.stateLock)
            {
                EnsureExtraSkillSlotCountUnsafe();
                return this.ExtraSkillKeys[slotIndex];
            }
        }

        public void ClearExtraSkillKeys()
        {
            lock (this.stateLock)
            {
                EnsureExtraSkillSlotCountUnsafe();
                for (int i = 0; i < EXTRA_SKILL_SLOTS; i++)
                {
                    this.ExtraSkillKeys[i] = Key.None;
                }
            }
        }

        private void EnsureExtraSkillSlotCountUnsafe()
        {
            if (this.ExtraSkillKeys == null)
            {
                this.ExtraSkillKeys = new List<Key>();
            }

            while (this.ExtraSkillKeys.Count < EXTRA_SKILL_SLOTS)
            {
                this.ExtraSkillKeys.Add(Key.None);
            }

            if (this.ExtraSkillKeys.Count > EXTRA_SKILL_SLOTS)
            {
                this.ExtraSkillKeys.RemoveRange(EXTRA_SKILL_SLOTS, this.ExtraSkillKeys.Count - EXTRA_SKILL_SLOTS);
            }
        }

        public bool IsActiveForDismount()
        {
            return this.Enabled && this.DismountOnSkill && FormUtils.IsValidKey(this.MountKey);
        }

        public bool IsTrackedSkillKey(Key key, Profile profile)
        {
            if (!FormUtils.IsValidKey(key) || key == this.MountKey)
            {
                return false;
            }

            // Never treat Autopot / Yggdrasil potion keys as skills requiring dismount.
            if (profile != null)
            {
                if (profile.Autopot != null && (key == profile.Autopot.hpKey || key == profile.Autopot.spKey))
                {
                    return false;
                }

                if (profile.AutopotYgg != null && (key == profile.AutopotYgg.hpKey || key == profile.AutopotYgg.spKey))
                {
                    return false;
                }
            }

            lock (this.stateLock)
            {
                if (this.ExtraSkillKeys != null)
                {
                    for (int i = 0; i < this.ExtraSkillKeys.Count; i++)
                    {
                        if (this.ExtraSkillKeys[i] == key)
                        {
                            return true;
                        }
                    }
                }
            }

            if (!this.IncludeOtherTabsSkillKeys || profile == null)
            {
                return false;
            }

            if (profile.AHK != null)
            {
                foreach (KeyValuePair<string, KeyConfig> entry in profile.AHK.GetEntriesSnapshot())
                {
                    if (entry.Value != null && entry.Value.key == key)
                    {
                        return true;
                    }
                }
            }

            if (profile.Autobuff != null)
            {
                foreach (KeyValuePair<EffectStatusIDs, Key> entry in profile.Autobuff.GetBuffMappingSnapshot())
                {
                    if (entry.Value == key)
                    {
                        return true;
                    }
                }
            }

            if (IsTimerKey(profile.AutoRefreshSpammer1, key)
                || IsTimerKey(profile.AutoRefreshSpammer2, key)
                || IsTimerKey(profile.AutoRefreshSpammer3, key)
                || IsTimerKey(profile.AutoRefreshSpammer4, key)
                || IsTimerKey(profile.AutoRefreshSpammer5, key)
                || IsTimerKey(profile.AutoRefreshSpammer6, key))
            {
                return true;
            }

            if (profile.AtkDefMode != null && profile.AtkDefMode.keySpammer == key)
            {
                return true;
            }

            if (profile.SongMacro != null && profile.SongMacro.chainConfigs != null)
            {
                foreach (ChainConfig lane in profile.SongMacro.chainConfigs)
                {
                    if (lane == null)
                    {
                        continue;
                    }

                    if (lane.trigger == key)
                    {
                        return true;
                    }

                    if (lane.macroEntries != null)
                    {
                        foreach (KeyValuePair<string, MacroKey> slot in lane.macroEntries)
                        {
                            if (slot.Value != null && slot.Value.key == key)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static bool IsTimerKey(AutoRefreshSpammer timer, Key key)
        {
            return timer != null && timer.RefreshKey == key;
        }

        public static bool IsClientMounted(Client client)
        {
            if (client == null || !client.HasMemorySupport || !client.IsProcessAlive())
            {
                return false;
            }

            byte[] rawBuffer = new byte[Constants.STATUS_BUFFER_BYTE_SIZE];
            uint[] statusBuffer = new uint[Constants.MAX_BUFF_LIST_INDEX_SIZE];
            int count = client.ReadBuffStatusBuffer(rawBuffer, statusBuffer);
            for (int i = 0; i < count; i++)
            {
                if (statusBuffer[i] == (uint)EffectStatusIDs.EFST_ALL_RIDING)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the character is currently on a Halter Lead / Rédea mount (EFST_ALL_RIDING = 613)
        /// and dismounts before a skill is cast. Debounced so back-to-back skill calls never toggle twice.
        /// </summary>
        public bool EnsureDismountedIfMounted(Client client, CancellationToken token)
        {
            if (!IsActiveForDismount() || client == null || !client.HasMemorySupport || !client.IsProcessAlive())
            {
                return false;
            }

            RecordSkillActivity();

            if (!IsClientMounted(client))
            {
                return false;
            }

            return DismountNow(client, token);
        }

        /// <summary>
        /// Sends the configured MountKey once (guarded by a 600ms debounce so concurrent skill triggers
        /// never accidentally remount) and waits DismountDelayMs for the RO server to clear EFST_ALL_RIDING.
        /// </summary>
        public bool DismountNow(Client client, CancellationToken token)
        {
            if (!IsActiveForDismount() || client == null || KeyHelper.IsAltPressed())
            {
                return false;
            }

            IntPtr hWnd = client.GetWindowHandle();
            if (hWnd == IntPtr.Zero)
            {
                return false;
            }

            Keys formsMountKey = KeyHelper.ToFormsKeys(this.MountKey);
            if (formsMountKey == Keys.None)
            {
                return false;
            }

            int waitMs = Math.Max(20, Math.Min(500, this.DismountDelayMs));
            bool shouldSendKey = false;

            lock (this.stateLock)
            {
                long now = this.clock.ElapsedMilliseconds;
                this.lastSkillActivityMs = now;
                if ((now - this.lastDismountMs) >= DISMOUNT_DEBOUNCE_MS)
                {
                    this.lastDismountMs = now;
                    this.dismountedByAutoMount = true;
                    shouldSendKey = true;
                }
            }

            if (shouldSendKey)
            {
                Interop.SendKeyPress(hWnd, formsMountKey);
            }

            _4RThread.Sleep(waitMs, token);
            return shouldSendKey;
        }

        public void RecordSkillActivity()
        {
            lock (this.stateLock)
            {
                this.lastSkillActivityMs = this.clock.ElapsedMilliseconds;
            }
        }

        public void Start()
        {
            Stop();
            Client roClient = ClientSingleton.GetClient();
            if (roClient == null)
            {
                return;
            }

            lock (this.stateLock)
            {
                this.dismountedByAutoMount = false;
                this.lastSkillActivityMs = this.clock.ElapsedMilliseconds;
                if (!this.hookSubscribed)
                {
                    KeyboardHook.PhysicalKeyDown += OnPhysicalKeyDown;
                    this.hookSubscribed = true;
                }
            }

            this.remountThread = new _4RThread(token => IdleRemountTick(roClient, token), ACTION_NAME_AUTOMOUNT);
            _4RThread.Start(this.remountThread);
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

                this.dismountedByAutoMount = false;
            }

            _4RThread.Stop(this.remountThread);
            this.remountThread = null;
        }

        private void OnPhysicalKeyDown(Keys formsKey)
        {
            if (!IsActiveForDismount() || KeyboardHook.Alt || KeyboardHook.Control)
            {
                return;
            }

            Key wpfKey = KeyHelper.FromFormsKeys(formsKey);
            Profile currentProfile = ProfileSingleton.GetCurrent();
            if (!IsTrackedSkillKey(wpfKey, currentProfile))
            {
                return;
            }

            RecordSkillActivity();

            // Run memory check + dismount + recast off the WH_KEYBOARD_LL thread so input never lags.
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    Client client = ClientSingleton.GetClient();
                    if (client == null
                        || !client.HasMemorySupport
                        || !client.IsProcessAlive()
                        || !client.IsGameWindowFocused()
                        || ChatPauseState.IsChatting)
                    {
                        return;
                    }

                    if (!IsClientMounted(client))
                    {
                        return;
                    }

                    if (DismountNow(client, CancellationToken.None))
                    {
                        IntPtr hWnd = client.GetWindowHandle();
                        if (hWnd != IntPtr.Zero)
                        {
                            Interop.SendKeyDown(hWnd, formsKey);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn("AutoMount", "Error during dismount-on-skill handler", ex);
                }
            });
        }

        private void IdleRemountTick(Client client, CancellationToken token)
        {
            if (this.Enabled
                && this.AutoRemountAfterIdle
                && FormUtils.IsValidKey(this.MountKey)
                && client.HasMemorySupport
                && client.IsProcessAlive()
                && !client.ShouldPauseAutomation()
                && !KeyHelper.IsAltPressed()
                && !ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther))
            {
                bool shouldCheckRemount;
                long delayMs = Math.Max(1, Math.Min(120, this.RemountDelaySec)) * 1000L;

                lock (this.stateLock)
                {
                    long now = this.clock.ElapsedMilliseconds;
                    shouldCheckRemount = this.dismountedByAutoMount && (now - this.lastSkillActivityMs) >= delayMs;
                }

                if (shouldCheckRemount)
                {
                    byte[] rawBuffer = new byte[Constants.STATUS_BUFFER_BYTE_SIZE];
                    uint[] statusBuffer = new uint[Constants.MAX_BUFF_LIST_INDEX_SIZE];
                    byte[] vitalsBuffer = new byte[Constants.HP_SP_BLOCK_BYTE_SIZE];

                    uint currentHp;
                    uint maxHp;
                    uint currentSp;
                    uint maxSp;
                    bool alive = client.TryReadVitals(vitalsBuffer, out currentHp, out maxHp, out currentSp, out maxSp)
                        && currentHp >= Constants.MINIMUM_HP_TO_RECOVER;

                    if (alive)
                    {
                        bool isMounted = false;
                        bool isSitting = false;
                        int count = client.ReadBuffStatusBuffer(rawBuffer, statusBuffer);
                        for (int i = 0; i < count; i++)
                        {
                            uint st = statusBuffer[i];
                            if (st == (uint)EffectStatusIDs.EFST_ALL_RIDING)
                            {
                                isMounted = true;
                            }
                            else if (st == (uint)EffectStatusIDs.EFST_SIT)
                            {
                                isSitting = true;
                            }
                        }

                        if (!isMounted && !isSitting)
                        {
                            IntPtr hWnd = client.GetWindowHandle();
                            Keys formsMountKey = KeyHelper.ToFormsKeys(this.MountKey);
                            if (hWnd != IntPtr.Zero && formsMountKey != Keys.None)
                            {
                                Interop.SendKeyPress(hWnd, formsMountKey);
                            }
                        }

                        lock (this.stateLock)
                        {
                            this.dismountedByAutoMount = false;
                        }
                    }
                }
            }

            _4RThread.Sleep(400, token);
        }

        public string GetConfiguration()
        {
            lock (this.stateLock)
            {
                EnsureExtraSkillSlotCountUnsafe();
                return JsonConvert.SerializeObject(this);
            }
        }

        public string GetActionName()
        {
            return ACTION_NAME_AUTOMOUNT;
        }
    }
}
