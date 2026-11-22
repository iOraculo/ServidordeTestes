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

        private readonly object mappingLock = new object();
        private readonly Dictionary<EffectStatusIDs, long> lastCastTimesMs = new Dictionary<EffectStatusIDs, long>();
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

            return new _4RThread(token =>
            {
                if (c.HasMemorySupport && c.IsProcessAlive() && !c.ShouldPauseAutomation()
                    && !ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther))
                {
                    bool foundQuag = false;
                    bool foundBlockingDebuff = false;
                    bool isSitting = false;
                    bool isMounted = false;
                    Dictionary<EffectStatusIDs, Key> missingBuffs = GetBuffMappingSnapshot();

                    if (missingBuffs.Count > 0)
                    {
                        HashSet<EffectStatusIDs> activeStatuses = new HashSet<EffectStatusIDs>();
                        int count = c.ReadBuffStatusBuffer(rawBuffer, statusBuffer);
                        for (int i = 0; i < count; i++)
                        {
                            EffectStatusIDs status = (EffectStatusIDs)statusBuffer[i];

                            if (status == EffectStatusIDs.IGNORE || status == EffectStatusIDs.IGNORE_2)
                            {
                                continue;
                            }

                            activeStatuses.Add(status);

                            if (status == EffectStatusIDs.OVERTHRUSTMAX)
                            {
                                missingBuffs.Remove(EffectStatusIDs.OVERTHRUST);
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
                            else if (IncapacitatingDebuffs.Contains(status))
                            {
                                foundBlockingDebuff = true;
                            }
                        }

                        AutoMount autoMount = ProfileSingleton.GetCurrent() != null ? ProfileSingleton.GetCurrent().AutoMount : null;
                        bool pauseForMount = isMounted && autoMount != null && autoMount.Enabled && autoMount.PauseAutobuffWhileMounted;

                        if (missingBuffs.Count > 0 && !(this.pauseWhenSitting && isSitting) && !pauseForMount)
                        {
                            uint currentHp;
                            uint maxHp;
                            uint currentSp;
                            uint maxSp;
                            bool hasVitals = c.TryReadVitals(vitalsBuffer, out currentHp, out maxHp, out currentSp, out maxSp);
                            if (hasVitals && currentHp >= Constants.MINIMUM_HP_TO_RECOVER)
                            {
                                long nowMs = this.clock.ElapsedMilliseconds;
                                foreach (KeyValuePair<EffectStatusIDs, Key> item in missingBuffs)
                                {
                                    if (token.IsCancellationRequested || ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther))
                                    {
                                        break;
                                    }

                                    EffectStatusIDs prerequisite;
                                    bool prerequisiteSatisfied = !TryGetPrerequisite(item.Key, out prerequisite)
                                        || activeStatuses.Contains(prerequisite);

                                    if (!ShouldCastBuff(item.Key, nowMs, foundQuag, foundBlockingDebuff, currentSp, maxSp, isSitting, prerequisiteSatisfied))
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
                                    if (!_4RThread.Sleep(10, token))
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                _4RThread.Sleep(300, token);
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
            bool prerequisiteSatisfied = true)
        {
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

            int cooldown = this.buffCooldownMs;
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
            Interop.SendKeyDown(hWnd, formsKey);

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
