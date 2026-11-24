using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using Newtonsoft.Json;
using _4RTools.Utils;

namespace _4RTools.Model
{
    public class MacroKey
    {
        public Key key { get; set; }
        public int delay { get; set; } = 50;
        public bool hasClick { get; set; } = false;

        public MacroKey() { }

        public MacroKey(Key key, int delay)
        {
            this.key = key;
            this.delay = delay;
        }

        public MacroKey(Key key, int delay, bool hasClick)
        {
            this.key = key;
            this.delay = delay;
            this.hasClick = hasClick;
        }
    }

    public class ChainConfig
    {
        public int id;
        public Key trigger { get; set; }
        public Key daggerKey { get; set; }
        public Key instrumentKey { get; set; }
        public int delay { get; set; } = 50;
        public Dictionary<string, MacroKey> macroEntries { get; set; } = new Dictionary<string, MacroKey>();
        public bool infinityLoop { get; set; } = false;
        public bool infinityLoopOn { get; set; } = false;

        public ChainConfig() { }

        public ChainConfig(int id)
        {
            this.id = id;
            this.macroEntries = new Dictionary<string, MacroKey>();
        }

        public ChainConfig(ChainConfig macro)
        {
            this.id = macro.id;
            this.delay = macro.delay;
            this.trigger = macro.trigger;
            this.daggerKey = macro.daggerKey;
            this.instrumentKey = macro.instrumentKey;
            this.infinityLoop = macro.infinityLoop;
            this.infinityLoopOn = macro.infinityLoopOn;
            this.macroEntries = new Dictionary<string, MacroKey>();
            if (macro.macroEntries != null)
            {
                foreach (KeyValuePair<string, MacroKey> entry in macro.macroEntries)
                {
                    if (entry.Value != null)
                    {
                        this.macroEntries[entry.Key] = new MacroKey(entry.Value.key, entry.Value.delay, entry.Value.hasClick);
                    }
                }
            }
        }

        public ChainConfig(int id, Key trigger)
        {
            this.id = id;
            this.trigger = trigger;
            this.macroEntries = new Dictionary<string, MacroKey>();
        }
    }

    public class Macro : Action
    {
        public static string ACTION_NAME_SONG_MACRO = "SongMacro2.0";
        public static string ACTION_NAME_MACRO_SWITCH = "MacroSwitch2.0";
        private const int MAX_SLOTS_PER_LANE = 8;

        private readonly object configLock = new object();
        private readonly Dictionary<int, bool> lastTriggerState = new Dictionary<int, bool>();
        private _4RThread thread;

        public string actionName { get; set; }
        public List<ChainConfig> chainConfigs { get; set; } = new List<ChainConfig>();

        public Macro(string macroname, int macroLanes)
        {
            this.actionName = macroname;
            for (int i = 1; i <= macroLanes; i++)
            {
                this.chainConfigs.Add(new ChainConfig(i, Key.None));
            }
        }

        public void ResetMacro(int macroId)
        {
            lock (this.configLock)
            {
                int index = this.chainConfigs.FindIndex(c => c.id == macroId);
                if (index >= 0)
                {
                    this.chainConfigs[index] = new ChainConfig(macroId);
                }
            }
        }

        public string GetActionName()
        {
            return this.actionName;
        }

        public string GetConfiguration()
        {
            lock (this.configLock)
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        private void MacroExecutionThread(Client roClient, CancellationToken token)
        {
            if (!roClient.IsProcessAlive()
                || roClient.ShouldPauseAutomation()
                || (SessionContext.HasMultipleActiveSessions && !roClient.IsGameWindowFocused()))
            {
                _4RThread.Sleep(50, token);
                return;
            }

            IntPtr hWnd = roClient.GetWindowHandle();
            if (hWnd == IntPtr.Zero)
            {
                _4RThread.Sleep(50, token);
                return;
            }

            bool altPressed = KeyHelper.IsAltPressed();
            List<ChainConfig> chainsToRun = null;

            lock (this.configLock)
            {
                for (int i = 0; i < this.chainConfigs.Count; i++)
                {
                    ChainConfig cfg = this.chainConfigs[i];
                    if (cfg == null || cfg.trigger == Key.None)
                    {
                        continue;
                    }

                    bool triggerDown = !altPressed && KeyHelper.IsKeyDown(cfg.trigger);
                    bool shouldExecute = false;

                    if (cfg.infinityLoop)
                    {
                        bool wasDown = false;
                        this.lastTriggerState.TryGetValue(cfg.id, out wasDown);
                        this.lastTriggerState[cfg.id] = triggerDown;

                        if (triggerDown && !wasDown)
                        {
                            cfg.infinityLoopOn = !cfg.infinityLoopOn;
                        }

                        shouldExecute = cfg.infinityLoopOn;
                    }
                    else
                    {
                        shouldExecute = triggerDown;
                    }

                    if (shouldExecute)
                    {
                        if (chainsToRun == null)
                        {
                            chainsToRun = new List<ChainConfig>(2);
                        }
                        chainsToRun.Add(new ChainConfig(cfg));
                    }
                }
            }

            if (chainsToRun != null)
            {
                for (int i = 0; i < chainsToRun.Count; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    ExecuteChain(roClient, hWnd, chainsToRun[i], token);
                }
            }

            _4RThread.Sleep(100, token);
        }

        private void ExecuteChain(Client roClient, IntPtr hWnd, ChainConfig chainConfig, CancellationToken token)
        {
            Dictionary<string, MacroKey> macro = chainConfig.macroEntries;
            if (macro == null || macro.Count == 0)
            {
                return;
            }

            if (string.Equals(this.actionName, ACTION_NAME_SONG_MACRO, StringComparison.Ordinal))
            {
                AutoMount autoMount = ProfileSingleton.GetCurrent() != null ? ProfileSingleton.GetCurrent().AutoMount : null;
                if (autoMount != null && autoMount.IsActiveForDismount())
                {
                    autoMount.EnsureDismountedIfMounted(roClient, token);
                }
            }

            int maxSlot = Math.Max(MAX_SLOTS_PER_LANE, macro.Count);
            Keys instrumentKey = KeyHelper.ToFormsKeys(chainConfig.instrumentKey);
            Keys daggerKey = KeyHelper.ToFormsKeys(chainConfig.daggerKey);

            for (int i = 1; i <= maxSlot; i++)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                if (ActionPriorityCoordinator.ShouldYield(ActionPriority.SkillAndOther, null, false))
                {
                    if (!_4RThread.Sleep(25, token))
                    {
                        break;
                    }
                }

                MacroKey macroKey;
                if (!macro.TryGetValue("in" + i + "mac" + chainConfig.id, out macroKey) || macroKey == null || macroKey.key == Key.None)
                {
                    continue;
                }

                if (instrumentKey != Keys.None)
                {
                    Interop.SendKeyDown(hWnd, instrumentKey);
                    if (!_4RThread.Sleep(30, token))
                    {
                        break;
                    }
                }

                if (!_4RThread.Sleep(Math.Max(0, macroKey.delay), token))
                {
                    break;
                }

                Keys formsKey = KeyHelper.ToFormsKeys(macroKey.key);
                Interop.SendKeyDown(hWnd, formsKey);

                if (macroKey.hasClick)
                {
                    Interop.SendLeftClick(hWnd);
                }

                if (daggerKey != Keys.None)
                {
                    Interop.SendKeyDown(hWnd, daggerKey);
                    if (!_4RThread.Sleep(30, token))
                    {
                        break;
                    }
                }
            }
        }

        private bool GetLaneLoopState(int laneId)
        {
            lock (this.configLock)
            {
                ChainConfig cfg = this.chainConfigs.Find(c => c.id == laneId);
                return cfg != null && cfg.infinityLoopOn;
            }
        }

        private void SetLaneLoopState(int laneId, bool enabled)
        {
            lock (this.configLock)
            {
                ChainConfig cfg = this.chainConfigs.Find(c => c.id == laneId);
                if (cfg != null)
                {
                    cfg.infinityLoopOn = enabled;
                }
            }
        }

        public void Start()
        {
            Stop();
            Client roClient = ClientSingleton.GetClient();
            if (roClient != null)
            {
                this.lastTriggerState.Clear();
                this.thread = new _4RThread(token => MacroExecutionThread(roClient, token), this.actionName);
                _4RThread.Start(this.thread);
            }
        }

        public void Stop()
        {
            _4RThread.Stop(this.thread);
            this.thread = null;

            lock (this.configLock)
            {
                foreach (ChainConfig cfg in this.chainConfigs)
                {
                    cfg.infinityLoopOn = false;
                }
            }

            this.lastTriggerState.Clear();
        }
    }
}
