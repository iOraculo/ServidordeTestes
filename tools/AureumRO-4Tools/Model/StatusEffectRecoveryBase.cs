using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using Newtonsoft.Json;
using _4RTools.Utils;

namespace _4RTools.Model
{
    public abstract class StatusEffectRecoveryBase : Action
    {
        private static readonly Dictionary<EffectStatusIDs, EffectStatusIDs> SharedStatusAliases = new Dictionary<EffectStatusIDs, EffectStatusIDs>
        {
            { EffectStatusIDs.EFST_BURNT, EffectStatusIDs.EFST_BURNING },
            { EffectStatusIDs.MISTY_FROST, EffectStatusIDs.EFST_FREEZING },
            { EffectStatusIDs.EFST_HEALTHSTATE_BLOODING, EffectStatusIDs.EFST_BLEEDING },
            { EffectStatusIDs.EFST_STONE_ING, EffectStatusIDs.EFST_STONE },
            { EffectStatusIDs.EFST_GVG_BLIND, EffectStatusIDs.BLIND },
            { EffectStatusIDs.EFST_HANDICAPSTATE_DEEPBLIND, EffectStatusIDs.BLIND },
            { EffectStatusIDs.EFST_GVG_CURSE, EffectStatusIDs.CURSE },
            { EffectStatusIDs.EFST_GVG_SILENCE, EffectStatusIDs.SILENCE },
            { EffectStatusIDs.EFST_HANDICAPSTATE_DEEPSILENCE, EffectStatusIDs.SILENCE },
            { EffectStatusIDs.EFST_HEALTHSTATE_HEAVYPOISON, EffectStatusIDs.POISON },
            { EffectStatusIDs.EFST_HANDICAPSTATE_DEADLYPOISON, EffectStatusIDs.POISON },
        };

        protected readonly object mappingLock = new object();
        private _4RThread thread;

        public Dictionary<EffectStatusIDs, Key> buffMapping = new Dictionary<EffectStatusIDs, Key>();
        public int delay { get; set; } = 10;

        public static EffectStatusIDs ResolveCanonicalStatus(EffectStatusIDs status)
        {
            EffectStatusIDs canonical;
            return SharedStatusAliases.TryGetValue(status, out canonical) ? canonical : status;
        }

        protected static bool TryResolveMappedKey(
            EffectStatusIDs status,
            Dictionary<EffectStatusIDs, Key> mappingSnapshot,
            out Key key,
            out EffectStatusIDs matchedStatus)
        {
            if (mappingSnapshot.TryGetValue(status, out key))
            {
                matchedStatus = status;
                return true;
            }

            EffectStatusIDs canonical = ResolveCanonicalStatus(status);
            if (canonical != status && mappingSnapshot.TryGetValue(canonical, out key))
            {
                matchedStatus = canonical;
                return true;
            }

            matchedStatus = status;
            key = Key.None;
            return false;
        }

        public abstract string GetActionName();

        public void Start()
        {
            Stop();
            Client roClient = ClientSingleton.GetClient();
            if (roClient != null)
            {
                this.thread = CreateRecoveryThread(roClient);
                _4RThread.Start(this.thread);
            }
        }

        public void Stop()
        {
            _4RThread.Stop(this.thread);
            this.thread = null;
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
            }
        }

        public Dictionary<EffectStatusIDs, Key> GetBuffMappingSnapshot()
        {
            lock (this.mappingLock)
            {
                return new Dictionary<EffectStatusIDs, Key>(this.buffMapping);
            }
        }

        public string GetConfiguration()
        {
            lock (this.mappingLock)
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        protected virtual void ProcessStatus(Client client, EffectStatusIDs status, Dictionary<EffectStatusIDs, Key> mappingSnapshot)
        {
            Key key;
            if (mappingSnapshot.TryGetValue(status, out key))
            {
                SendRecoveryKey(client, key);
            }
        }

        protected void SendRecoveryKey(Client client, Key key)
        {
            if (key == Key.None || client == null)
            {
                return;
            }

            IntPtr hWnd = client.GetWindowHandle();
            if (hWnd != IntPtr.Zero)
            {
                Keys formsKey = KeyHelper.ToFormsKeys(key);
                if (formsKey != Keys.None)
                {
                    ActionPriorityCoordinator.NotifyAntiDebuffAction();
                    Interop.SendKeyPress(hWnd, formsKey);
                }
            }
        }

        private _4RThread CreateRecoveryThread(Client client)
        {
            byte[] rawBuffer = new byte[Constants.STATUS_BUFFER_BYTE_SIZE];
            uint[] statusBuffer = new uint[Constants.MAX_BUFF_LIST_INDEX_SIZE];
            List<uint> activeStatuses = new List<uint>(32);

            return new _4RThread(token =>
            {
                // Anti-debuffs / Panacea shares Tier 2 priority with Yggdrasil (yields only to Tier 1 player input).
                if (client.HasMemorySupport && client.IsProcessAlive() && !client.ShouldPauseAutomation()
                    && !ActionPriorityCoordinator.ShouldYield(ActionPriority.Yggdrasil))
                {
                    Dictionary<EffectStatusIDs, Key> snapshot = GetBuffMappingSnapshot();
                    int count = client.ReadBuffStatusBuffer(rawBuffer, statusBuffer);
                    activeStatuses.Clear();
                    Client.ExtractActiveStatuses(statusBuffer, count, activeStatuses);

                    for (int i = 0; i < activeStatuses.Count; i++)
                    {
                        if (token.IsCancellationRequested || ActionPriorityCoordinator.ShouldYield(ActionPriority.Yggdrasil))
                        {
                            break;
                        }

                        ProcessStatus(client, (EffectStatusIDs)activeStatuses[i], snapshot);
                    }
                }

                _4RThread.Sleep(Math.Max(1, this.delay), token);
            }, GetActionName());
        }
    }
}
