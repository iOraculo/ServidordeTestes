using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;

namespace _4RTools.Utils
{
    public class KeybindConflict
    {
        public Key Key { get; private set; }
        public List<string> Usages { get; private set; }

        public KeybindConflict(Key key, List<string> usages)
        {
            this.Key = key;
            this.Usages = usages ?? new List<string>();
        }
    }

    public static class KeybindConflictDetector
    {
        public static List<KeybindConflict> DetectConflicts(Profile profile)
        {
            List<KeybindConflict> conflicts = new List<KeybindConflict>();
            if (profile == null)
            {
                return conflicts;
            }

            Dictionary<Key, List<string>> usagesByKey = new Dictionary<Key, List<string>>();

            if (profile.UserPreferences != null && !string.IsNullOrWhiteSpace(profile.UserPreferences.toggleStateKey))
            {
                Keys toggleFormsKey;
                if (KeyHelper.TryParseFormsKeys(profile.UserPreferences.toggleStateKey, out toggleFormsKey) && toggleFormsKey != Keys.None)
                {
                    AddUsage(usagesByKey, KeyHelper.FromFormsKeys(toggleFormsKey), LocalizationManager.T("usage.toggle"));
                }
            }

            if (profile.Autopot != null)
            {
                AddUsage(usagesByKey, profile.Autopot.hpKey, LocalizationManager.T("usage.autopot_hp"));
                AddUsage(usagesByKey, profile.Autopot.spKey, LocalizationManager.T("usage.autopot_sp"));
            }

            if (profile.AutopotYgg != null)
            {
                AddUsage(usagesByKey, profile.AutopotYgg.hpKey, LocalizationManager.T("usage.ygg_hp"));
                AddUsage(usagesByKey, profile.AutopotYgg.spKey, LocalizationManager.T("usage.ygg_sp"));
            }

            if (profile.AHK != null)
            {
                foreach (KeyValuePair<string, KeyConfig> entry in profile.AHK.GetEntriesSnapshot())
                {
                    if (entry.Value != null)
                    {
                        string modeLabel = entry.Value.ClickActive
                            ? LocalizationManager.T("usage.ahk_click")
                            : LocalizationManager.T("usage.ahk_noclick");
                        AddUsage(usagesByKey, entry.Value.key, modeLabel);
                    }
                }
            }

            if (profile.Autobuff != null)
            {
                foreach (KeyValuePair<EffectStatusIDs, Key> entry in profile.Autobuff.GetBuffMappingSnapshot())
                {
                    AddUsage(usagesByKey, entry.Value, LocalizationManager.T("usage.autobuff", entry.Key));
                }
            }

            // Multiple status ailments intentionally share one cure item (e.g. Panacea / Green Potion),
            // so collapse duplicate uses within the same recovery group to avoid false positives.
            if (profile.StatusRecovery != null)
            {
                foreach (KeyValuePair<EffectStatusIDs, Key> entry in profile.StatusRecovery.GetBuffMappingSnapshot())
                {
                    AddUsage(usagesByKey, entry.Value, LocalizationManager.T("usage.status_recovery"));
                }
            }

            if (profile.DebuffsRecovery != null)
            {
                foreach (KeyValuePair<EffectStatusIDs, Key> entry in profile.DebuffsRecovery.GetBuffMappingSnapshot())
                {
                    AddUsage(usagesByKey, entry.Value, LocalizationManager.T("usage.debuff_recovery"));
                }
            }

            if (profile.AutoRefreshSpammer1 != null)
            {
                AddUsage(usagesByKey, profile.AutoRefreshSpammer1.RefreshKey, LocalizationManager.T("usage.skill_timer", 1));
            }

            if (profile.AutoRefreshSpammer2 != null)
            {
                AddUsage(usagesByKey, profile.AutoRefreshSpammer2.RefreshKey, LocalizationManager.T("usage.skill_timer", 2));
            }

            if (profile.AutoRefreshSpammer3 != null)
            {
                AddUsage(usagesByKey, profile.AutoRefreshSpammer3.RefreshKey, LocalizationManager.T("usage.skill_timer", 3));
            }

            if (profile.AutoRefreshSpammer4 != null)
            {
                AddUsage(usagesByKey, profile.AutoRefreshSpammer4.RefreshKey, LocalizationManager.T("usage.skill_timer", 4));
            }

            if (profile.AutoRefreshSpammer5 != null)
            {
                AddUsage(usagesByKey, profile.AutoRefreshSpammer5.RefreshKey, LocalizationManager.T("usage.skill_timer", 5));
            }

            if (profile.AutoRefreshSpammer6 != null)
            {
                AddUsage(usagesByKey, profile.AutoRefreshSpammer6.RefreshKey, LocalizationManager.T("usage.skill_timer", 6));
            }

            if (profile.AtkDefMode != null)
            {
                AddUsage(usagesByKey, profile.AtkDefMode.keySpammer, LocalizationManager.T("usage.atkdef_spam"));
                foreach (KeyValuePair<string, Key> entry in profile.AtkDefMode.GetKeysSnapshot(ATKDEFEnum.ATK))
                {
                    AddUsage(usagesByKey, entry.Value, LocalizationManager.T("usage.atkdef_atk"));
                }
                foreach (KeyValuePair<string, Key> entry in profile.AtkDefMode.GetKeysSnapshot(ATKDEFEnum.DEF))
                {
                    AddUsage(usagesByKey, entry.Value, LocalizationManager.T("usage.atkdef_def"));
                }
            }

            CollectMacroUsages(usagesByKey, profile.SongMacro, LocalizationManager.T("usage.song_macro"));
            CollectMacroUsages(usagesByKey, profile.MacroSwitch, LocalizationManager.T("usage.macro_switch"));

            if (profile.AutoMount != null && profile.AutoMount.Enabled)
            {
                AddUsage(usagesByKey, profile.AutoMount.MountKey, LocalizationManager.T("usage.mount_key"));
            }

            foreach (KeyValuePair<Key, List<string>> pair in usagesByKey.OrderBy(p => p.Key.ToString(), StringComparer.OrdinalIgnoreCase))
            {
                if (pair.Value.Count > 1)
                {
                    conflicts.Add(new KeybindConflict(pair.Key, pair.Value));
                }
            }

            return conflicts;
        }

        public static string FormatReport(string profileName, List<KeybindConflict> conflicts)
        {
            string safeName = string.IsNullOrWhiteSpace(profileName) ? "Default" : profileName;
            if (conflicts == null || conflicts.Count == 0)
            {
                return LocalizationManager.T("conflicts.none", safeName);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(LocalizationManager.T("conflicts.header", conflicts.Count, safeName));
            sb.AppendLine();

            foreach (KeybindConflict conflict in conflicts)
            {
                sb.AppendFormat("- [{0}]: {1}", conflict.Key, string.Join("  |  ", conflict.Usages));
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        private static void CollectMacroUsages(Dictionary<Key, List<string>> usagesByKey, Macro macro, string macroLabel)
        {
            if (macro == null || macro.chainConfigs == null)
            {
                return;
            }

            foreach (ChainConfig lane in macro.chainConfigs)
            {
                if (lane == null)
                {
                    continue;
                }

                AddUsage(usagesByKey, lane.trigger, LocalizationManager.T("usage.macro_trigger", macroLabel, lane.id));
                AddUsage(usagesByKey, lane.instrumentKey, LocalizationManager.T("usage.macro_instrument", macroLabel));
                AddUsage(usagesByKey, lane.daggerKey, LocalizationManager.T("usage.macro_dagger", macroLabel));

                if (lane.macroEntries != null)
                {
                    foreach (KeyValuePair<string, MacroKey> slot in lane.macroEntries)
                    {
                        if (slot.Value != null)
                        {
                            AddUsage(usagesByKey, slot.Value.key, LocalizationManager.T("usage.macro_slot", macroLabel, lane.id));
                        }
                    }
                }
            }
        }

        private static void AddUsage(Dictionary<Key, List<string>> usagesByKey, Key key, string label)
        {
            if (!FormUtils.IsValidKey(key) || string.IsNullOrWhiteSpace(label))
            {
                return;
            }

            List<string> list;
            if (!usagesByKey.TryGetValue(key, out list))
            {
                list = new List<string>();
                usagesByKey[key] = list;
            }

            if (!list.Contains(label))
            {
                list.Add(label);
            }
        }
    }
}
