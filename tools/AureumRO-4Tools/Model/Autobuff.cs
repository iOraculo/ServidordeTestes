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
    public class AutoBuff : Action
    {
        public static string ACTION_NAME_AUTOBUFF = "Autobuff";

        private static readonly HashSet<EffectStatusIDs> QuagmireBlockedBuffs = new HashSet<EffectStatusIDs>
        {
            EffectStatusIDs.CONCENTRATION,
            EffectStatusIDs.INC_AGI,
            EffectStatusIDs.TRUESIGHT,
            EffectStatusIDs.ADRENALINE,
            EffectStatusIDs.SPEARQUICKEN
        };

        private static readonly HashSet<EffectStatusIDs> IncapacitatingDebuffs = new HashSet<EffectStatusIDs>
        {
            EffectStatusIDs.SILENCE,
            EffectStatusIDs.EFST_FREEZING,
            EffectStatusIDs.EFST_STUN,
            EffectStatusIDs.EFST_DEEP_SLEEP
        };

        // Skills that require a prerequisite buff active before casting (e.g. Mirror Image requires Ninja Aura).
        private static readonly Dictionary<EffectStatusIDs, EffectStatusIDs> PrerequisiteBuffs = new Dictionary<EffectStatusIDs, EffectStatusIDs>
        {
            { EffectStatusIDs.EFST_NJ_BUNSINJYUTSU, EffectStatusIDs.AURA_NINJA }
        };

        // Alternative client EFST IDs that satisfy a configured buff (e.g. EFST_HEALTHSTATE_ANGELUS = 888 for ANGELUS = 9).
        private static readonly Dictionary<EffectStatusIDs, EffectStatusIDs> EquivalentActiveBuffs = new Dictionary<EffectStatusIDs, EffectStatusIDs>
        {
            { EffectStatusIDs.OVERTHRUSTMAX, EffectStatusIDs.OVERTHRUST },
            { EffectStatusIDs.EFST_HEALTHSTATE_ANGELUS, EffectStatusIDs.ANGELUS },
            { EffectStatusIDs.EFST_BASILICA_BUFF, EffectStatusIDs.EFST_BASILICA }
        };

        // Target-cast buffs that require clicking on your own character (center of the RO window) to self-cast.
        private static readonly HashSet<EffectStatusIDs> SelfTargetClickBuffs = new HashSet<EffectStatusIDs>
        {
            EffectStatusIDs.BLESSING,
            EffectStatusIDs.INC_AGI,
            EffectStatusIDs.ASSUMPTIO,
            EffectStatusIDs.EFST_KYRIE,
            EffectStatusIDs.EFST_SUFFRAGIUM,
            EffectStatusIDs.IMPOSITIO,
            EffectStatusIDs.ASPERSIO,
            EffectStatusIDs.PROTECTARMOR,
            EffectStatusIDs.EFST_KAAHI,
            EffectStatusIDs.EFST_KAUPE,
            EffectStatusIDs.EFST_KAITE,
            EffectStatusIDs.EFST_KAIZEL
        };

        // Minimum recast guards for skills that have long cast time or no Pre-Renewal EFST icon (e.g. Basilica).
        public const int BASILICA_NO_ICON_RECAST_MS = 25000;
        public const int BASILICA_ICON_CAST_GUARD_MS = 1200;

        private readonly object mappingLock = new object();
        private readonly Dictionary<EffectStatusIDs, long> lastCastTimesMs = new Dictionary<EffectStatusIDs, long>();
        private readonly HashSet<EffectStatusIDs> observedStatusIcons = new HashSet<EffectStatusIDs>();
        private readonly HashSet<EffectStatusIDs> previouslyActiveStatuses = new HashSet<EffectStatusIDs>();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private _4RThread thread;

        public int delay { get; set; } = 1;
        public int minSpPercent { get; set; } = 0;
        public int buffCooldownMs { get; set; } = 0;
        public bool pauseOnDebuff { get; set; } = true;
        public bool pauseWhenSitting { get; set; } = true;
        public bool autoSelfClick { get; set; } = true;
        public Dictionary<EffectStatusIDs, Key> buffMapping = new Dictionary<EffectStatusIDs, Key>();

        public static bool TryGetPrerequisite(EffectStatusIDs status, out EffectStatusIDs prerequisite)
        {
            return PrerequisiteBuffs.TryGetValue(status, out prerequisite);
        }

        public static bool TryGetEquivalentBuff(EffectStatusIDs activeStatus, out EffectStatusIDs satisfiedBuff)
        {
            return EquivalentActiveBuffs.TryGetValue(activeStatus, out satisfiedBuff);
        }

        public static bool RequiresSelfTargetClick(EffectStatusIDs status)
        {
            return SelfTargetClickBuffs.Contains(status);
        }

        public void Start()
        {
            Stop();
            ResetCooldowns();
            Client roClient = ClientSingleton.GetClient();
            if (roClient != null)
            {
                this.thread = AutoBuffThread(roClient);
                _4RThread.Start(this.thread);
            }
        }

        public _4RThread AutoBuffThread(Client c)
        {
            byte[] rawBuffer = new byte[Constants.STATUS_BUFFER_BYTE_SIZE];
            uint[] statusBuffer = new uint[Constants.MAX_BUFF_LIST_INDEX_SIZE];
            byte[] vitalsBuffer = new byte[Constants.HP_SP_BLOCK_BYTE_SIZE];
            List<uint> extractedStatuses = new List<uint>(32);
            HashSet<EffectStatusIDs> activeStatuses = new HashSet<EffectStatusIDs>();
            Dictionary<EffectStatusIDs, Key> missingBuffs = new Dictionary<EffectStatusIDs, Key>();

            return new _4RThread(token =>
            {
                if (c.HasMemorySupport && c.IsProcessAlive() && !c.ShouldPauseAutomation()
                    && !ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther))
                {
                    bool foundQuag = false;
                    bool foundBlockingDebuff = false;
                    bool isSitting = false;
                    bool isMounted = false;
                    bool isInPostDelay = false;

                    missingBuffs.Clear();
                    CopyBuffMappingInto(missingBuffs);

                    if (missingBuffs.Count > 0)
                    {
                        activeStatuses.Clear();
                        int count = c.ReadBuffStatusBuffer(rawBuffer, statusBuffer);
                        extractedStatuses.Clear();
                        Client.ExtractActiveStatuses(statusBuffer, count, extractedStatuses);

                        for (int i = 0; i < extractedStatuses.Count; i++)
                        {
                            EffectStatusIDs status = (EffectStatusIDs)extractedStatuses[i];

                            activeStatuses.Add(status);
                            MarkStatusObserved(status);

                            EffectStatusIDs equivalentBuff;
                            if (TryGetEquivalentBuff(status, out equivalentBuff))
                            {
                                activeStatuses.Add(equivalentBuff);
                                MarkStatusObserved(equivalentBuff);
                                missingBuffs.Remove(equivalentBuff);
                            }

                            missingBuffs.Remove(status);

                            if (status == EffectStatusIDs.QUAGMIRE)
                            {
                                foundQuag = true;
                            }
                            else if (status == EffectStatusIDs.EFST_SIT)
                            {
                                isSitting = true;
                            }
                            else if (status == EffectStatusIDs.EFST_ALL_RIDING)
                            {
                                isMounted = true;
                            }
                            else if (status == EffectStatusIDs.EFST_POSTDELAY)
                            {
                                isInPostDelay = true;
                            }
                            else if (IncapacitatingDebuffs.Contains(status))
                            {
                                foundBlockingDebuff = true;
                            }
                        }

                        // Clear cached recast timers for buffs whose status icon just expired (e.g. Basilica).
                        SyncActiveBuffTransitions(activeStatuses);

                        AutoMount autoMount = ProfileSingleton.GetCurrent() != null ? ProfileSingleton.GetCurrent().AutoMount : null;
                        bool pauseForMount = isMounted && autoMount != null && autoMount.Enabled && autoMount.PauseAutobuffWhileMounted;

                        if (missingBuffs.Count > 0 && !isInPostDelay && !(this.pauseWhenSitting && isSitting) && !pauseForMount)
                        {
                            uint currentHp;
                            uint maxHp;
                            uint currentSp;
                            uint maxSp;
                            bool hasVitals = c.TryReadVitals(vitalsBuffer, out currentHp, out maxHp, out currentSp, out maxSp);
                            if (hasVitals && currentHp >= Constants.MINIMUM_HP_TO_RECOVER)
                            {
                                long nowMs = this.clock.ElapsedMilliseconds;
                                List<KeyValuePair<EffectStatusIDs, Key>> orderedBuffs = OrderBuffsForCast(missingBuffs);
                                bool castAnyOtherBuff = false;

                                foreach (KeyValuePair<EffectStatusIDs, Key> item in orderedBuffs)
                                {
                                    if (token.IsCancellationRequested || ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther))
                                    {
                                        break;
                                    }

                                    // Always cast Basilica last and defer to the next cycle if another buff was cast
                                    // in this cycle (avoids locking other skills or misfiring during post-delay).
                                    if (item.Key == EffectStatusIDs.EFST_BASILICA && castAnyOtherBuff)
                                    {
                                        continue;
                                    }

                                    EffectStatusIDs prerequisite;
                                    bool prerequisiteSatisfied = !TryGetPrerequisite(item.Key, out prerequisite)
                                        || activeStatuses.Contains(prerequisite);

                                    if (!ShouldCastBuff(item.Key, nowMs, foundQuag, foundBlockingDebuff, currentSp, maxSp, isSitting, prerequisiteSatisfied, isInPostDelay))
                                    {
                                        continue;
                                    }

                                    if (isMounted && autoMount != null && autoMount.IsActiveForDismount())
                                    {
                                        autoMount.DismountNow(c, token);
                                        isMounted = false;
                                        if (token.IsCancellationRequested || ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther))
                                        {
                                            break;
                                        }
                                    }

                                    UseAutobuff(c, item.Key, item.Value, token);
                                    RecordBuffCast(item.Key, nowMs);
                                    if (item.Key != EffectStatusIDs.EFST_BASILICA)
                                    {
                                        castAnyOtherBuff = true;
                                    }
                                    if (!_4RThread.Sleep(50, token))
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                _4RThread.Sleep(150, token);
            }, ACTION_NAME_AUTOBUFF);
        }

        public bool ShouldCastBuff(
            EffectStatusIDs status,
            long nowMs,
            bool hasQuagmire,
            bool hasBlockingDebuff,
            uint currentSp,
            uint maxSp,
            bool isSitting = false,
            bool prerequisiteSatisfied = true,
            bool isInPostDelay = false)
        {
            if (isInPostDelay || !prerequisiteSatisfied)
            {
                return false;
            }

            if (this.pauseWhenSitting && isSitting)
            {
                return false;
            }

            if (this.pauseOnDebuff && hasBlockingDebuff)
            {
                return false;
            }

            if (hasQuagmire && QuagmireBlockedBuffs.Contains(status))
            {
                return false;
            }

            if (this.minSpPercent > 0 && Client.IsBelowPercent(currentSp, maxSp, this.minSpPercent))
            {
                return false;
            }

            int cooldown = GetEffectiveCooldownMs(status);
            if (cooldown > 0)
            {
                lock (this.mappingLock)
                {
                    long lastCast;
                    if (this.lastCastTimesMs.TryGetValue(status, out lastCast) && (nowMs - lastCast) < cooldown)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public int GetEffectiveCooldownMs(EffectStatusIDs status)
        {
            int cooldown = Math.Max(0, this.buffCooldownMs);
            if (status == EffectStatusIDs.EFST_BASILICA)
            {
                bool iconObserved;
                lock (this.mappingLock)
                {
                    iconObserved = this.observedStatusIcons.Contains(EffectStatusIDs.EFST_BASILICA);
                }

                int minGuard = iconObserved ? BASILICA_ICON_CAST_GUARD_MS : BASILICA_NO_ICON_RECAST_MS;
                return Math.Max(cooldown, minGuard);
            }

            return cooldown;
        }

        public void MarkStatusObserved(EffectStatusIDs status)
        {
            lock (this.mappingLock)
            {
                this.observedStatusIcons.Add(status);
            }
        }

        public static List<KeyValuePair<EffectStatusIDs, Key>> OrderBuffsForCast(Dictionary<EffectStatusIDs, Key> missingBuffs)
        {
            List<KeyValuePair<EffectStatusIDs, Key>> ordered = new List<KeyValuePair<EffectStatusIDs, Key>>();
            if (missingBuffs == null || missingBuffs.Count == 0)
            {
                return ordered;
            }

            KeyValuePair<EffectStatusIDs, Key>? basilicaEntry = null;
            foreach (KeyValuePair<EffectStatusIDs, Key> item in missingBuffs)
            {
                if (item.Key == EffectStatusIDs.EFST_BASILICA)
                {
                    basilicaEntry = item;
                }
                else
                {
                    ordered.Add(item);
                }
            }

            if (basilicaEntry.HasValue)
            {
                ordered.Add(basilicaEntry.Value);
            }

            return ordered;
        }

        public void SyncActiveBuffTransitions(HashSet<EffectStatusIDs> currentActiveStatuses)
        {
            bool basilicaActiveNow = currentActiveStatuses != null
                && (currentActiveStatuses.Contains(EffectStatusIDs.EFST_BASILICA)
                    || currentActiveStatuses.Contains(EffectStatusIDs.EFST_BASILICA_BUFF));

            lock (this.mappingLock)
            {
                bool basilicaWasActive = this.previouslyActiveStatuses.Contains(EffectStatusIDs.EFST_BASILICA)
                    || this.previouslyActiveStatuses.Contains(EffectStatusIDs.EFST_BASILICA_BUFF);

                foreach (EffectStatusIDs prev in this.previouslyActiveStatuses)
                {
                    if (currentActiveStatuses == null || !currentActiveStatuses.Contains(prev))
                    {
                        this.lastCastTimesMs.Remove(prev);
                    }
                }

                if (basilicaWasActive && !basilicaActiveNow)
                {
                    this.lastCastTimesMs.Remove(EffectStatusIDs.EFST_BASILICA);
                }

                this.previouslyActiveStatuses.Clear();
                if (currentActiveStatuses != null)
                {
                    foreach (EffectStatusIDs active in currentActiveStatuses)
                    {
                        this.previouslyActiveStatuses.Add(active);
                    }
                }
            }
        }

        public void SyncActiveStatusTransitions(HashSet<EffectStatusIDs> currentActiveStatuses)
        {
            SyncActiveBuffTransitions(currentActiveStatuses);
        }

        public void RecordBuffCast(EffectStatusIDs status, long nowMs)
        {
            lock (this.mappingLock)
            {
                this.lastCastTimesMs[status] = nowMs;
            }
        }

        public void ResetCooldowns()
        {
            lock (this.mappingLock)
            {
                this.lastCastTimesMs.Clear();
                this.observedStatusIcons.Clear();
                this.previouslyActiveStatuses.Clear();
            }
        }

        public void AddKeyToBuff(EffectStatusIDs status, Key key)
        {
            lock (this.mappingLock)
            {
                if (this.buffMapping.ContainsKey(status))
                {
                    this.buffMapping.Remove(status);
                }

                if (FormUtils.IsValidKey(key))
                {
                    this.buffMapping.Add(status, key);
                }
            }
        }

        public void ClearKeyMapping()
        {
            lock (this.mappingLock)
            {
                this.buffMapping.Clear();
                this.lastCastTimesMs.Clear();
            }
        }

        public Dictionary<EffectStatusIDs, Key> GetBuffMappingSnapshot()
        {
            lock (this.mappingLock)
            {
                return new Dictionary<EffectStatusIDs, Key>(this.buffMapping);
            }
        }

        private void CopyBuffMappingInto(Dictionary<EffectStatusIDs, Key> destination)
        {
            lock (this.mappingLock)
            {
                foreach (KeyValuePair<EffectStatusIDs, Key> pair in this.buffMapping)
                {
                    destination[pair.Key] = pair.Value;
                }
            }
        }

        public void Stop()
        {
            _4RThread.Stop(this.thread);
            this.thread = null;
            ResetCooldowns();
        }

        public string GetConfiguration()
        {
            lock (this.mappingLock)
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        public string GetActionName()
        {
            return ACTION_NAME_AUTOBUFF;
        }

        private void UseAutobuff(Client client, EffectStatusIDs status, Key key, CancellationToken token)
        {
            if (key == Key.None || KeyHelper.IsAltPressed() || client == null)
            {
                return;
            }

            IntPtr hWnd = client.GetWindowHandle();
            if (hWnd == IntPtr.Zero)
            {
                return;
            }

            Keys formsKey = KeyHelper.ToFormsKeys(key);
            Interop.SendKeyPress(hWnd, formsKey);

            if (this.autoSelfClick && RequiresSelfTargetClick(status))
            {
                if (_4RThread.Sleep(45, token))
                {
                    Interop.SendLeftClickAtCenter(hWnd);
                }
            }
        }
    }
}
