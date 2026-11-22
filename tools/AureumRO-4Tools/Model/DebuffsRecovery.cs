using System.Collections.Generic;
using System.Windows.Input;
using _4RTools.Utils;

namespace _4RTools.Model
{
    public class DebuffsRecovery : StatusEffectRecoveryBase
    {
        public static string ACTION_NAME_DEBUFF_RECOVERY = "DebuffsRecovery";

        public override string GetActionName()
        {
            return ACTION_NAME_DEBUFF_RECOVERY;
        }

        public new static EffectStatusIDs ResolveCanonicalStatus(EffectStatusIDs status)
        {
            return StatusEffectRecoveryBase.ResolveCanonicalStatus(status);
        }

        protected override void ProcessStatus(Client client, EffectStatusIDs status, Dictionary<EffectStatusIDs, Key> mappingSnapshot)
        {
            Key key;
            EffectStatusIDs matchedStatus;
            if (TryResolveMappedKey(status, mappingSnapshot, out key, out matchedStatus))
            {
                SendRecoveryKey(client, key);
            }
        }
    }
}