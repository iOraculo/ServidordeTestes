using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;
using _4RTools.Utils;

namespace _4RTools.Model
{
    public class StatusRecovery : StatusEffectRecoveryBase
    {
        public static string ACTION_NAME_STATUS_AUTOBUFF = "StatusRecovery";

        private static readonly HashSet<EffectStatusIDs> ElviraStatuses = new HashSet<EffectStatusIDs>
        {
            EffectStatusIDs.CRITICALWOUND,
            EffectStatusIDs.PROPERTYUNDEAD,
            EffectStatusIDs.BLOODING,
            EffectStatusIDs.EFST_HEALTHSTATE_BLOODING,
            EffectStatusIDs.EFST_FREEZING,
            EffectStatusIDs.MISTY_FROST,
            EffectStatusIDs.EFST_BURNING,
            EffectStatusIDs.EFST_BURNT,
            EffectStatusIDs.OVERHEAT,
            EffectStatusIDs.EFST_MANDRAGORA
        };

        private readonly Stopwatch elviraCooldownWatch = new Stopwatch();
        private bool elviraUsedOnce = false;

        public bool autoStand { get; set; } = false;
        public int newStatusDelay { get; set; } = 0;

        public override string GetActionName()
        {
            return ACTION_NAME_STATUS_AUTOBUFF;
        }

        protected override void ProcessStatus(Client client, EffectStatusIDs status, Dictionary<EffectStatusIDs, Key> mappingSnapshot)
        {
            Key key;
            if (mappingSnapshot.TryGetValue(status, out key))
            {
                if (ElviraStatuses.Contains(status) && this.newStatusDelay > 0)
                {
                    if (!this.elviraUsedOnce || this.elviraCooldownWatch.ElapsedMilliseconds >= this.newStatusDelay)
                    {
                        SendRecoveryKey(client, key);
                        this.elviraUsedOnce = true;
                        this.elviraCooldownWatch.Restart();
                    }
                }
                else
                {
                    SendRecoveryKey(client, key);
                }
            }

            if (this.autoStand && status == EffectStatusIDs.EFST_SIT)
            {
                SendRecoveryKey(client, Key.Insert);
            }
        }
    }
}