using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using Newtonsoft.Json;
using _4RTools.Utils;

namespace _4RTools.Model
{
    public enum ATKDEFEnum
    {
        ATK = 0,
        DEF = 1,
    }

    public class ATKDEFMode : Action
    {
        public static string ACTION_NAME_ATKDEF = "ATKDEFMode";
        private readonly object keysLock = new object();
        private _4RThread thread;

        public int ahkDelay { get; set; } = 10;
        public int switchDelay { get; set; } = 50;
        public Key keySpammer { get; set; }
        public bool keySpammerWithClick { get; set; } = true;
        public Dictionary<string, Key> defKeys { get; set; } = new Dictionary<string, Key>();
        public Dictionary<string, Key> atkKeys { get; set; } = new Dictionary<string, Key>();

        public string GetActionName()
        {
            return ACTION_NAME_ATKDEF;
        }

        public string GetConfiguration()
        {
            lock (this.keysLock)
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        public void Start()
        {
            Stop();
            Client roClient = ClientSingleton.GetClient();
            if (roClient != null)
            {
                this.thread = new _4RThread(token => AHKThreadExecution(roClient, token), ACTION_NAME_ATKDEF);
                _4RThread.Start(this.thread);
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

            Key spammer = this.keySpammer;
            if (spammer == Key.None || KeyHelper.IsAltPressed() || !KeyHelper.IsKeyDown(spammer))
            {
                return;
            }

            IntPtr hWnd = roClient.GetWindowHandle();
            if (hWnd == IntPtr.Zero)
            {
                return;
            }

            Keys thisk = KeyHelper.ToFormsKeys(spammer);
            Key[] atkSnapshot;
            Key[] defSnapshot;

            lock (this.keysLock)
            {
                atkSnapshot = this.atkKeys.Values.ToArray();
                defSnapshot = this.defKeys.Values.ToArray();
            }

            int equipDelay = Math.Max(0, this.switchDelay);
            int spamDelay = Math.Max(1, this.ahkDelay);

            // Equip ATK items
            foreach (Key key in atkSnapshot)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                Interop.SendKeyDown(hWnd, KeyHelper.ToFormsKeys(key));
                if (!_4RThread.Sleep(equipDelay, token))
                {
                    return;
                }
            }

            AutoMount autoMount = ProfileSingleton.GetCurrent() != null ? ProfileSingleton.GetCurrent().AutoMount : null;
            if (autoMount != null && autoMount.IsActiveForDismount())
            {
                autoMount.EnsureDismountedIfMounted(roClient, token);
            }

            if (this.keySpammerWithClick)
            {
                while (!token.IsCancellationRequested && KeyHelper.IsKeyDown(spammer))
                {
                    if (ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther, null, false))
                    {
                        if (!_4RThread.Sleep(Math.Max(10, spamDelay), token))
                        {
                            break;
                        }
                        continue;
                    }

                    Interop.SendKeyDown(hWnd, thisk);
                    Interop.SendLeftClick(hWnd);
                    if (!_4RThread.Sleep(spamDelay, token))
                    {
                        break;
                    }
                }
            }
            else
            {
                while (!token.IsCancellationRequested && KeyHelper.IsKeyDown(spammer))
                {
                    if (ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther, null, false))
                    {
                        if (!_4RThread.Sleep(Math.Max(10, spamDelay), token))
                        {
                            break;
                        }
                        continue;
                    }

                    Interop.SendKeyDown(hWnd, thisk);
                    if (!_4RThread.Sleep(spamDelay, token))
                    {
                        break;
                    }
                }
            }

            // Equip DEF items after releasing spammer key
            foreach (Key key in defSnapshot)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                Interop.SendKeyDown(hWnd, KeyHelper.ToFormsKeys(key));
                if (!_4RThread.Sleep(equipDelay, token))
                {
                    return;
                }
            }
        }

        public void AddSwitchItem(string dictKey, Key k, ATKDEFEnum type)
        {
            lock (this.keysLock)
            {
                Dictionary<string, Key> target = type == ATKDEFEnum.DEF ? this.defKeys : this.atkKeys;

                if (target.ContainsKey(dictKey))
                {
                    target.Remove(dictKey);
                }

                if (FormUtils.IsValidKey(k))
                {
                    target.Add(dictKey, k);
                }
            }
        }

        public void RemoveSwitchEntry(string dictKey, ATKDEFEnum type)
        {
            lock (this.keysLock)
            {
                Dictionary<string, Key> target = type == ATKDEFEnum.DEF ? this.defKeys : this.atkKeys;
                target.Remove(dictKey);
            }
        }

        public Dictionary<string, Key> GetKeysSnapshot(ATKDEFEnum type)
        {
            lock (this.keysLock)
            {
                Dictionary<string, Key> source = type == ATKDEFEnum.DEF ? this.defKeys : this.atkKeys;
                return new Dictionary<string, Key>(source);
            }
        }

        public void Stop()
        {
            _4RThread.Stop(this.thread);
            this.thread = null;
        }
    }
}
