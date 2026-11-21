using System.Collections.Generic;
using System.Windows.Input;
using _4RTools.Utils;

namespace _4RTools.Model
{
    public class DebuffsRecovery : StatusEffectRecoveryBase
    {
        public static string ACTION_NAME_DEBUFF_RECOVERY = "DebuffsRecovery";

        private static readonly Dictionary<EffectStatusIDs, EffectStatusIDs> StatusAliases = new Dictionary<EffectStatusIDs, EffectStatusIDs>
        {
            { EffectStatusIDs.EFST_BURNT, EffectStatusIDs.EFST_BURNING },
            { EffectStatusIDs.MISTY_FROST, EffectStatusIDs.EFST_FREEZING },
            { EffectStatusIDs.EFST_HEALTHSTATE_BLOODING, EffectStatusIDs.EFST_BLEEDING },
            { EffectStatusIDs.EFST_STONE_ING, EffectStatusIDs.EFST_STONE },
        };

        public override string GetActionName()
        {
            return ACTION_NAME_DEBUFF_RECOVERY;
        }

        public static EffectStatusIDs ResolveCanonicalStatus(EffectStatusIDs status)
        {
            EffectStatusIDs canonical;
            return StatusAliases.TryGetValue(status, out canonical) ? canonical : status;
        }

        protected override void ProcessStatus(Client client, EffectStatusIDs status, Dictionary<EffectStatusIDs, Key> mappingSnapshot)
        {
            Key key;
            if (mappingSnapshot.TryGetValue(status, out key))
            {
                SendRecoveryKey(client, key);
                return;
            }

            EffectStatusIDs canonical = ResolveCanonicalStatus(status);
            if (canonical != status && mappingSnapshot.TryGetValue(canonical, out key))
            {
                SendRecoveryKey(client, key);
            }
        }
    }
}