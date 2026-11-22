using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Input;

namespace _4RTools.Utils
{
    public enum ActionPriority
    {
        PlayerInput = 1,
        Yggdrasil = 2,
        Potion = 3,
        SkillAndOther = 4
    }

    /// <summary>
    /// Enforces the 4-tier action priority order across all 4RTools automation threads:
    /// 1) Player input in the game (highest — pauses 4RTools briefly on fresh physical keypresses)
    /// 2) Yggdrasil emergency autopot and Anti-Debuffs / Panacea (takes precedence over standard potions and skills)
    /// 3) Standard potions (takes precedence over skills, buffs, and timers)
    /// 4) Skills, Auto-skill, consumables, timers, and other automations (lowest)
    /// </summary>
    public static class ActionPriorityCoordinator
    {
        public const int DEFAULT_PLAYER_INPUT_YIELD_MS = 120;
        public const int MIN_PLAYER_INPUT_YIELD_MS = 0;
        public const int MAX_PLAYER_INPUT_YIELD_MS = 1000;

        public const int DEFAULT_YGG_PRIORITY_WINDOW_MS = 250;
        public const int DEFAULT_POTION_PRIORITY_WINDOW_MS = 150;

        private static readonly Stopwatch Clock = Stopwatch.StartNew();

        private static long lastPlayerInputMs = -1;
        private static long lastYggActionMs = -1;
        private static long lastPotionActionMs = -1;
        private static int yggEmergencyActive = 0;

        private static int playerInputYieldMs = DEFAULT_PLAYER_INPUT_YIELD_MS;
        private static int yggPriorityWindowMs = DEFAULT_YGG_PRIORITY_WINDOW_MS;
        private static int potionPriorityWindowMs = DEFAULT_POTION_PRIORITY_WINDOW_MS;
        private static int holdPauseKeyCode = (int)Key.None;
        private static int holdPauseIncludeAutopot = 0;

        public static Key HoldPauseKey
        {
            get { return (Key)Volatile.Read(ref holdPauseKeyCode); }
            set { Volatile.Write(ref holdPauseKeyCode, (int)value); }
        }

        public static bool HoldPauseIncludeAutopot
        {
            get { return Volatile.Read(ref holdPauseIncludeAutopot) != 0; }
            set { Volatile.Write(ref holdPauseIncludeAutopot, value ? 1 : 0); }
        }

        public static bool IsHoldPauseActive()
        {
            Key key = HoldPauseKey;
            return FormUtils.IsValidKey(key) && KeyHelper.IsHoldPauseKeyDown(key);
        }

        public static bool IsHoldPauseBlockingAutopot()
        {
            return HoldPauseIncludeAutopot && IsHoldPauseActive();
        }

        public static int PlayerInputYieldMs
        {
            get { return Volatile.Read(ref playerInputYieldMs); }
            set
            {
                int clamped = Math.Max(MIN_PLAYER_INPUT_YIELD_MS, Math.Min(MAX_PLAYER_INPUT_YIELD_MS, value));
                Volatile.Write(ref playerInputYieldMs, clamped);
            }
        }

        public static int YggPriorityWindowMs
        {
            get { return Volatile.Read(ref yggPriorityWindowMs); }
            set { Volatile.Write(ref yggPriorityWindowMs, Math.Max(0, value)); }
        }

        public static int PotionPriorityWindowMs
        {
            get { return Volatile.Read(ref potionPriorityWindowMs); }
            set { Volatile.Write(ref potionPriorityWindowMs, Math.Max(0, value)); }
        }

        public static bool IsYggEmergencyActive
        {
            get { return Volatile.Read(ref yggEmergencyActive) != 0; }
        }

        public static long NowMs
        {
            get { return Clock.ElapsedMilliseconds; }
        }

        public static void NotifyPlayerInput(long? nowMs = null)
        {
            long timestamp = nowMs ?? Clock.ElapsedMilliseconds;
            Interlocked.Exchange(ref lastPlayerInputMs, timestamp);
        }

        public static void SetYggEmergencyActive(bool active)
        {
            Volatile.Write(ref yggEmergencyActive, active ? 1 : 0);
        }

        public static void NotifyYggAction(long? nowMs = null)
        {
            long timestamp = nowMs ?? Clock.ElapsedMilliseconds;
            Interlocked.Exchange(ref lastYggActionMs, timestamp);
        }

        public static void NotifyAntiDebuffAction(long? nowMs = null)
        {
            NotifyYggAction(nowMs);
        }

        public static void NotifyEmergencyAction(long? nowMs = null)
        {
            NotifyYggAction(nowMs);
        }

        public static void NotifyPotionAction(long? nowMs = null)
        {
            long timestamp = nowMs ?? Clock.ElapsedMilliseconds;
            Interlocked.Exchange(ref lastPotionActionMs, timestamp);
        }

        /// <summary>
        /// Returns true if an action at the requested priority tier must yield to a higher-priority action.
        /// Set <paramref name="includePlayerInput"/> to false for player-driven held spammers so they still
        /// yield to Yggdrasil and Potions without delaying the player's own held spam key.
        /// </summary>
        public static bool ShouldYield(ActionPriority priority, long? nowMs = null, bool includePlayerInput = true)
        {
            if (priority <= ActionPriority.PlayerInput)
            {
                return false;
            }

            long now = nowMs ?? Clock.ElapsedMilliseconds;

            // Tier 1: Player input takes priority over everything (Ygg, Potions, Skills & others).
            if (includePlayerInput)
            {
                int yieldWindow = Volatile.Read(ref playerInputYieldMs);
                if (yieldWindow > 0)
                {
                    long lastPlayer = Interlocked.Read(ref lastPlayerInputMs);
                    if (lastPlayer >= 0)
                    {
                        long elapsed = now - lastPlayer;
                        if (elapsed >= 0 && elapsed < yieldWindow)
                        {
                            return true;
                        }
                    }
                }
            }

            if (priority == ActionPriority.Yggdrasil)
            {
                return false;
            }

            // Tier 2: Yggdrasil emergency / recent Ygg cast takes priority over Potions and Skills.
            if (Volatile.Read(ref yggEmergencyActive) != 0)
            {
                return true;
            }

            int yggWindow = Volatile.Read(ref yggPriorityWindowMs);
            if (yggWindow > 0)
            {
                long lastYgg = Interlocked.Read(ref lastYggActionMs);
                if (lastYgg >= 0)
                {
                    long elapsed = now - lastYgg;
                    if (elapsed >= 0 && elapsed < yggWindow)
                    {
                        return true;
                    }
                }
            }

            if (priority == ActionPriority.Potion)
            {
                return false;
            }

            // Tier 3: Standard potions take priority over Skills and all other background automations.
            int potWindow = Volatile.Read(ref potionPriorityWindowMs);
            if (potWindow > 0)
            {
                long lastPot = Interlocked.Read(ref lastPotionActionMs);
                if (lastPot >= 0)
                {
                    long elapsed = now - lastPot;
                    if (elapsed >= 0 && elapsed < potWindow)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void Reset()
        {
            Interlocked.Exchange(ref lastPlayerInputMs, -1);
            Interlocked.Exchange(ref lastYggActionMs, -1);
            Interlocked.Exchange(ref lastPotionActionMs, -1);
            Volatile.Write(ref yggEmergencyActive, 0);
            Volatile.Write(ref playerInputYieldMs, DEFAULT_PLAYER_INPUT_YIELD_MS);
            Volatile.Write(ref yggPriorityWindowMs, DEFAULT_YGG_PRIORITY_WINDOW_MS);
            Volatile.Write(ref potionPriorityWindowMs, DEFAULT_POTION_PRIORITY_WINDOW_MS);
            Volatile.Write(ref holdPauseKeyCode, (int)Key.None);
            Volatile.Write(ref holdPauseIncludeAutopot, 0);
        }
    }
}
