using System;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using Newtonsoft.Json;
using _4RTools.Utils;

namespace _4RTools.Model
{
    public class Autopot : Action
    {
        public static string ACTION_NAME_AUTOPOT = "Autopot";
        public static string ACTION_NAME_AUTOPOT_YGG = "AutopotYgg";

        public Key hpKey { get; set; }
        public int hpPercent { get; set; }
        public Key spKey { get; set; }
        public int spPercent { get; set; }
        public int delay { get; set; } = 15;
        public int delayYgg { get; set; } = 50;

        public string actionName { get; set; }
        private _4RThread thread;

        public Autopot() { }

        public Autopot(string actionName)
        {
            this.actionName = actionName;
        }

        public Autopot(Key hpKey, int hpPercent, int delay, Key spKey, int spPercent)
        {
            this.delay = delay;
            this.hpKey = hpKey;
            this.hpPercent = hpPercent;
            this.spKey = spKey;
            this.spPercent = spPercent;
        }

        public void Start()
        {
            Stop();
            Client roClient = ClientSingleton.GetClient();
            if (roClient != null)
            {
                int hpPotCount = 0;
                byte[] vitalsBuffer = new byte[Constants.HP_SP_BLOCK_BYTE_SIZE];
                this.thread = new _4RThread(token =>
                {
                    AutopotThreadExecution(roClient, vitalsBuffer, ref hpPotCount, token);
                }, GetActionName());
                _4RThread.Start(this.thread);
            }
        }

        private void AutopotThreadExecution(Client roClient, byte[] vitalsBuffer, ref int hpPotCount, CancellationToken token)
        {
            if (roClient.HasMemorySupport && roClient.IsProcessAlive() && !roClient.ShouldPauseForFocus() && !roClient.ShouldPauseForTown())
            {
                uint currentHp;
                uint maxHp;
                uint currentSp;
                uint maxSp;

                if (roClient.TryReadVitals(vitalsBuffer, out currentHp, out maxHp, out currentSp, out maxSp)
                    && currentHp > 0 && maxHp > 0)
                {
                    bool pottedSpThisTick = false;

                    // Prioritize HP recovery first; every 3 consecutive HP pots also check SP to prevent SP starvation.
                    if (Client.IsBelowPercent(currentHp, maxHp, this.hpPercent))
                    {
                        Pot(roClient, this.hpKey);
                        hpPotCount++;

                        if (hpPotCount >= 3)
                        {
                            hpPotCount = 0;
                            if (Client.IsBelowPercent(currentSp, maxSp, this.spPercent))
                            {
                                Pot(roClient, this.spKey);
                                pottedSpThisTick = true;
                            }
                        }
                    }
                    else
                    {
                        hpPotCount = 0;
                    }

                    if (!pottedSpThisTick && Client.IsBelowPercent(currentSp, maxSp, this.spPercent))
                    {
                        Pot(roClient, this.spKey);
                    }
                }
            }

            _4RThread.Sleep(Math.Max(1, this.delay), token);
        }

        private void Pot(Client roClient, Key key)
        {
            if (key == Key.None || KeyHelper.IsAltPressed() || roClient == null || roClient.ShouldPauseAutopotKey(key))
            {
                return;
            }

            Keys formsKey = KeyHelper.ToFormsKeys(key);
            if (formsKey != Keys.None)
            {
                Interop.SendKeyPress(roClient.GetWindowHandle(), formsKey);
            }
        }

        public void Stop()
        {
            _4RThread.Stop(this.thread);
            this.thread = null;
        }

        public string GetConfiguration()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string GetActionName()
        {
            return this.actionName != null ? this.actionName : ACTION_NAME_AUTOPOT;
        }
    }
}
