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
        protected readonly object mappingLock = new object();
        private _4RThread thread;

        public Dictionary<EffectStatusIDs, Key> buffMapping = new Dictionary<EffectStatusIDs, Key>();
        public int delay { get; set; } = 10;

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
            if (key == Key.None || KeyHelper.IsAltPressed() || client == null)
            {
                return;
            }

            IntPtr hWnd = client.GetWindowHandle();
            if (hWnd != IntPtr.Zero)
            {
                Keys formsKey = KeyHelper.ToFormsKeys(key);
                Interop.SendKeyDown(hWnd, formsKey);
            }
        }

        private _4RThread CreateRecoveryThread(Client client)
        {
            byte[] rawBuffer = new byte[Constants.STATUS_BUFFER_BYTE_SIZE];
            uint[] statusBuffer = new uint[Constants.MAX_BUFF_LIST_INDEX_SIZE];

            return new _4RThread(token =>
            {
                if (client.HasMemorySupport && client.IsProcessAlive() && !client.ShouldPauseAutomation()
                    && !ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther))
                {
                    Dictionary<EffectStatusIDs, Key> snapshot = GetBuffMappingSnapshot();
                    int count = client.ReadBuffStatusBuffer(rawBuffer, statusBuffer);
                    for (int i = 0; i < count; i++)
                    {
                        if (token.IsCancellationRequested || ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther))
                        {
                            break;
                        }

                        uint rawStatus = statusBuffer[i];
                        if (rawStatus == (uint)EffectStatusIDs.IGNORE || rawStatus == (uint)EffectStatusIDs.IGNORE_2)
                        {
                            continue;
                        }

                        ProcessStatus(client, (EffectStatusIDs)rawStatus, snapshot);
                    }
                }

                _4RThread.Sleep(Math.Max(1, this.delay), token);
            }, GetActionName());
        }
    }
}
