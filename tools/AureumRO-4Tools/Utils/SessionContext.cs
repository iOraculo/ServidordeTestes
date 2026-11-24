using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Input;
using _4RTools.Model;

namespace _4RTools.Utils
{
    /// <summary>
    /// Isolates all per-account runtime state (Client, Profile, ChatPause, ActionPriority, Hold-to-Pause,
    /// and @refresh chat-send lock) so multiple Ragnarok accounts can run simultaneously inside one process.
    /// </summary>
    public sealed class SessionContext
    {
        public sealed class ActionPriorityState
        {
            private static readonly Stopwatch Clock = Stopwatch.StartNew();

            public long LastPlayerInputMs = -1;
            public long LastYggActionMs = -1;
            public long LastPotionActionMs = -1;
            public int YggEmergencyActive = 0;

            public int PlayerInputYieldMs = ActionPriorityCoordinator.DEFAULT_PLAYER_INPUT_YIELD_MS;
            public int YggPriorityWindowMs = ActionPriorityCoordinator.DEFAULT_YGG_PRIORITY_WINDOW_MS;
            public int PotionPriorityWindowMs = ActionPriorityCoordinator.DEFAULT_POTION_PRIORITY_WINDOW_MS;
            public int HoldPauseKeyCode = (int)Key.None;
            public int HoldPauseIncludeAutopot = 0;

            public static long NowMs
            {
                get { return Clock.ElapsedMilliseconds; }
            }

            public void Reset()
            {
                Interlocked.Exchange(ref this.LastPlayerInputMs, -1);
                Interlocked.Exchange(ref this.LastYggActionMs, -1);
                Interlocked.Exchange(ref this.LastPotionActionMs, -1);
                Volatile.Write(ref this.YggEmergencyActive, 0);
                Volatile.Write(ref this.PlayerInputYieldMs, ActionPriorityCoordinator.DEFAULT_PLAYER_INPUT_YIELD_MS);
                Volatile.Write(ref this.YggPriorityWindowMs, ActionPriorityCoordinator.DEFAULT_YGG_PRIORITY_WINDOW_MS);
                Volatile.Write(ref this.PotionPriorityWindowMs, ActionPriorityCoordinator.DEFAULT_POTION_PRIORITY_WINDOW_MS);
                Volatile.Write(ref this.HoldPauseKeyCode, (int)Key.None);
                Volatile.Write(ref this.HoldPauseIncludeAutopot, 0);
            }
        }

        private sealed class ContextScope : IDisposable
        {
            private readonly SessionContext previous;
            private bool disposed;

            public ContextScope(SessionContext next)
            {
                this.previous = threadSession;
                threadSession = next;
            }

            public void Dispose()
            {
                if (!this.disposed)
                {
                    this.disposed = true;
                    threadSession = this.previous;
                }
            }
        }

        private static readonly object RegistryLock = new object();
        private static readonly List<SessionContext> RegisteredSessions = new List<SessionContext>();

        public static readonly SessionContext Default = new SessionContext(0);

        private static SessionContext activeUiSession = Default;

        [ThreadStatic]
        private static SessionContext threadSession;

        private readonly object clientLock = new object();
        private readonly object profileLock = new object();
        private Client client;
        private Profile profile;
        private int chattingFlag;
        private int sendingCommandFlag;
        private int automationOnFlag;

        public int SlotIndex { get; private set; }
        public ActionPriorityState Priority { get; private set; }

        public event EventHandler ChatStateChanged;

        public SessionContext(int slotIndex)
        {
            this.SlotIndex = slotIndex;
            this.profile = new Profile("Default");
            this.Priority = new ActionPriorityState();
        }

        public static SessionContext Current
        {
            get
            {
                return threadSession ?? Volatile.Read(ref activeUiSession) ?? Default;
            }
        }

        public static SessionContext ActiveUiSession
        {
            get { return Volatile.Read(ref activeUiSession) ?? Default; }
            set { Volatile.Write(ref activeUiSession, value ?? Default); }
        }

        public static IDisposable Enter(SessionContext session)
        {
            return new ContextScope(session ?? Default);
        }

        public static void RegisterSession(SessionContext session)
        {
            if (session == null)
            {
                return;
            }

            lock (RegistryLock)
            {
                if (!RegisteredSessions.Contains(session))
                {
                    RegisteredSessions.Add(session);
                }
            }
        }

        public static void UnregisterSession(SessionContext session)
        {
            if (session == null)
            {
                return;
            }

            lock (RegistryLock)
            {
                RegisteredSessions.Remove(session);
            }
        }

        public static List<SessionContext> GetRegisteredSessions()
        {
            lock (RegistryLock)
            {
                return new List<SessionContext>(RegisteredSessions);
            }
        }

        public static bool HasMultipleActiveSessions
        {
            get
            {
                int activeCount = 0;
                lock (RegistryLock)
                {
                    for (int i = 0; i < RegisteredSessions.Count; i++)
                    {
                        if (RegisteredSessions[i].IsAutomationOn)
                        {
                            activeCount++;
                            if (activeCount > 1)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
        }

        public static SessionContext FindSessionWithFocusedGameWindow()
        {
            lock (RegistryLock)
            {
                for (int i = 0; i < RegisteredSessions.Count; i++)
                {
                    SessionContext s = RegisteredSessions[i];
                    Client c = s.Client;
                    if (c != null && c.IsProcessAlive() && c.IsGameWindowFocused())
                    {
                        return s;
                    }
                }
            }

            return null;
        }

        public bool IsAutomationOn
        {
            get { return Volatile.Read(ref this.automationOnFlag) == 1; }
            set { Volatile.Write(ref this.automationOnFlag, value ? 1 : 0); }
        }

        public Client Client
        {
            get
            {
                lock (this.clientLock)
                {
                    return this.client;
                }
            }
        }

        public void SetClient(Client newClient)
        {
            Client previous = null;
            lock (this.clientLock)
            {
                if (!ReferenceEquals(this.client, newClient))
                {
                    previous = this.client;
                    this.client = newClient;
                }
            }

            if (previous != null)
            {
                previous.Dispose();
            }
        }

        public Profile Profile
        {
            get
            {
                lock (this.profileLock)
                {
                    return this.profile;
                }
            }
            set
            {
                lock (this.profileLock)
                {
                    this.profile = value ?? new Profile("Default");
                }
            }
        }

        public bool IsChatting
        {
            get { return Volatile.Read(ref this.chattingFlag) == 1; }
        }

        public bool SetChatting(bool chatting)
        {
            int next = chatting ? 1 : 0;
            int prev = Interlocked.Exchange(ref this.chattingFlag, next);
            if (prev != next)
            {
                RaiseChatStateChanged();
                return true;
            }

            return false;
        }

        public bool ToggleChatting()
        {
            return SetChatting(!this.IsChatting);
        }

        public void ResumeChat()
        {
            SetChatting(false);
        }

        public bool TryConsumeChatting()
        {
            if (Interlocked.CompareExchange(ref this.chattingFlag, 0, 1) == 1)
            {
                RaiseChatStateChanged();
                return true;
            }

            return false;
        }

        public bool IsSendingCommand
        {
            get { return Volatile.Read(ref this.sendingCommandFlag) == 1; }
        }

        public bool TryBeginSendingCommand()
        {
            return Interlocked.CompareExchange(ref this.sendingCommandFlag, 1, 0) == 0;
        }

        public void EndSendingCommand()
        {
            Interlocked.Exchange(ref this.sendingCommandFlag, 0);
        }

        private void RaiseChatStateChanged()
        {
            EventHandler handler = this.ChatStateChanged;
            if (handler != null)
            {
                try
                {
                    handler(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Logger.Warn("SessionContext", "Error notifying session chat state listener", ex);
                }
            }
        }
    }
}
