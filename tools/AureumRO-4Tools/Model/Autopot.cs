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

        private bool IsYggdrasil
        {
            get { return string.Equals(GetActionName(), ACTION_NAME_AUTOPOT_YGG, StringComparison.Ordinal); }
        }

        private void AutopotThreadExecution(Client roClient, byte[] vitalsBuffer, ref int hpPotCount, CancellationToken token)
        {
            bool isYgg = this.IsYggdrasil;
            ActionPriority priority = isYgg ? ActionPriority.Yggdrasil : ActionPriority.Potion;
            bool yggEmergency = false;

            if (roClient.HasMemorySupport && roClient.IsProcessAlive() && !roClient.ShouldPauseForFocus() && !roClient.ShouldPauseForTown())
            {
                uint currentHp;
                uint maxHp;
                uint currentSp;
                uint maxSp;

                if (roClient.TryReadVitals(vitalsBuffer, out currentHp, out maxHp, out currentSp, out maxSp)
                    && currentHp > 0 && maxHp > 0)
                {
                    bool needsHp = this.hpKey != Key.None && Client.IsBelowPercent(currentHp, maxHp, this.hpPercent);
                    bool needsSp = this.spKey != Key.None && Client.IsBelowPercent(currentSp, maxSp, this.spPercent);

                    if (isYgg)
                    {
                        yggEmergency = needsHp || needsSp;
                        ActionPriorityCoordinator.SetYggEmergencyActive(yggEmergency);
                    }

                    if (!ActionPriorityCoordinator.ShouldYield(priority))
                    {
                        bool pottedSpThisTick = false;

                        // Prioritize HP recovery first; every 3 consecutive HP pots also check SP to prevent SP starvation.
                        if (needsHp)
                        {
                            Pot(roClient, this.hpKey, isYgg);
                            hpPotCount++;

                            if (hpPotCount >= 3)
                            {
                                hpPotCount = 0;
                                if (needsSp && !ActionPriorityCoordinator.ShouldYield(priority))
                                {
                                    Pot(roClient, this.spKey, isYgg);
                                    pottedSpThisTick = true;
                                }
                            }
                        }
                        else
                        {
                            hpPotCount = 0;
                        }

                        if (!pottedSpThisTick && needsSp && !ActionPriorityCoordinator.ShouldYield(priority))
                        {
                            Pot(roClient, this.spKey, isYgg);
                        }
                    }
                }
                else if (isYgg)
                {
                    ActionPriorityCoordinator.SetYggEmergencyActive(false);
                }
            }
            else if (isYgg)
            {
                ActionPriorityCoordinator.SetYggEmergencyActive(false);
            }

            _4RThread.Sleep(Math.Max(1, this.delay), token);
        }

        private void Pot(Client roClient, Key key, bool isYgg)
        {
            if (key == Key.None || KeyHelper.IsAltPressed() || roClient == null || roClient.ShouldPauseAutopotKey(key))
            {
                return;
            }

            Keys formsKey = KeyHelper.ToFormsKeys(key);
            if (formsKey != Keys.None)
            {
                if (isYgg)
                {
                    ActionPriorityCoordinator.NotifyYggAction();
                }
                else
                {
                    ActionPriorityCoordinator.NotifyPotionAction();
                }

                Interop.SendKeyPress(roClient.GetWindowHandle(), formsKey);
            }
        }

        public void Stop()
        {
            _4RThread.Stop(this.thread);
            this.thread = null;
            if (this.IsYggdrasil)
            {
                ActionPriorityCoordinator.SetYggEmergencyActive(false);
            }
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
