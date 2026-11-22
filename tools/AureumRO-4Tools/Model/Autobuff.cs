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
            EffectStatusIDs.ASPERSIO
        };

        // Minimum recast guards for skills that have long cast time or no Pre-Renewal EFST icon (e.g. Basilica).
        public const int BASILICA_NO_ICON_RECAST_MS = 25000;
        public const int BASILICA_ICON_CAST_GUARD_MS = 1200;

        private readonly object mappingLock = new object();
        private readonly Dictionary<EffectStatusIDs, long> lastCastTimesMs = new Dictionary<EffectStatusIDs, long>();
        private readonly HashSet<EffectStatusIDs> observedStatusIcons = new HashSet<EffectStatusIDs>();
        private readonly HashSet<EffectStatusIDs> previouslyActiveBuffs = new HashSet<EffectStatusIDs>();
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
                    Dictionary<EffectStatusIDs, Key> missingBuffs = GetBuffMappingSnapshot();

                    if (missingBuffs.Count > 0)
                    {
                        HashSet<EffectStatusIDs> activeStatuses = new HashSet<EffectStatusIDs>();
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

                        // If an icon-tracked buff (e.g. Basilica) was active and just ended,
                        // clear its cast-guard timer so it reactivates cleanly once post-delay ends.
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
                                Key deferredBasilicaKey = Key.None;
                                bool castNonBasilicaBuff = false;

                                foreach (KeyValuePair<EffectStatusIDs, Key> item in missingBuffs)
                                {
                                    if (token.IsCancellationRequested || ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther))
                                    {
                                        break;
                                    }

                                    // Always cast Basilica LAST (after other buffs are up) so it neither blocks
                                    // other Pre-Renewal buffs nor gets eaten by another skill's after-cast delay.
                                    if (item.Key == EffectStatusIDs.EFST_BASILICA)
                                    {
                                        deferredBasilicaKey = item.Value;
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
                                    castNonBasilicaBuff = true;
                                    if (!_4RThread.Sleep(50, token))
                                    {
                                        break;
                                    }
                                }

                                if (deferredBasilicaKey != Key.None
                                    && !castNonBasilicaBuff
                                    && !token.IsCancellationRequested
                                    && !ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther)
                                    && ShouldCastBuff(EffectStatusIDs.EFST_BASILICA, nowMs, foundQuag, foundBlockingDebuff, currentSp, maxSp, isSitting, true, isInPostDelay))
                                {
                                    if (isMounted && autoMount != null && autoMount.IsActiveForDismount())
                                    {
                                        autoMount.DismountNow(c, token);
                                        isMounted = false;
                                    }

                                    if (!token.IsCancellationRequested && !ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther))
                                    {
                                        UseAutobuff(c, EffectStatusIDs.EFST_BASILICA, deferredBasilicaKey, token);
                                        RecordBuffCast(EffectStatusIDs.EFST_BASILICA, nowMs);
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
            if (isInPostDelay)
            {
                return false;
            }

            if (!prerequisiteSatisfied)
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

        public void SyncActiveBuffTransitions(HashSet<EffectStatusIDs> activeStatuses)
        {
            if (activeStatuses == null)
            {
                return;
            }

            lock (this.mappingLock)
            {
                if (this.previouslyActiveBuffs.Count > 0)
                {
                    List<EffectStatusIDs> expired = null;
                    foreach (EffectStatusIDs prev in this.previouslyActiveBuffs)
                    {
                        if (!activeStatuses.Contains(prev))
                        {
                            if (expired == null)
                            {
                                expired = new List<EffectStatusIDs>();
                            }
                            expired.Add(prev);
                        }
                    }

                    if (expired != null)
                    {
                        for (int i = 0; i < expired.Count; i++)
                        {
                            EffectStatusIDs ended = expired[i];
                            this.previouslyActiveBuffs.Remove(ended);
                            if (ended == EffectStatusIDs.EFST_BASILICA)
                            {
                                this.lastCastTimesMs.Remove(ended);
                            }
                        }
                    }
                }

                if (activeStatuses.Contains(EffectStatusIDs.EFST_BASILICA))
                {
                    this.previouslyActiveBuffs.Add(EffectStatusIDs.EFST_BASILICA);
                }
            }
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
                this.previouslyActiveBuffs.Clear();
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
