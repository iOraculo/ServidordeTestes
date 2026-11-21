using System;
using System.Threading;
using System.Windows.Input;

namespace _4RTools.Utils
{
    public static class ChatPauseState
    {
        private static int chattingFlag = 0;

        public static event EventHandler StateChanged;

        public static bool IsChatting
        {
            get { return Volatile.Read(ref chattingFlag) == 1; }
        }

        public static void ToggleChatting()
        {
            int next = IsChatting ? 0 : 1;
            int prev = Interlocked.Exchange(ref chattingFlag, next);
            if (prev != next)
            {
                RaiseStateChanged();
            }
        }

        public static void SetChatting(bool chatting)
        {
            int next = chatting ? 1 : 0;
            int prev = Interlocked.Exchange(ref chattingFlag, next);
            if (prev != next)
            {
                RaiseStateChanged();
            }
        }

        public static void Resume()
        {
            SetChatting(false);
        }

        public static bool IsTypingKey(Key key)
        {
            if (key == Key.None)
            {
                return false;
            }

            // Function keys (F1-F24) execute hotbar skills in Ragnarok even while chat input is open.
            if (key >= Key.F1 && key <= Key.F24)
            {
                return false;
            }

            return true;
        }

        private static void RaiseStateChanged()
        {
            EventHandler handler = StateChanged;
            if (handler != null)
            {
                try
                {
                    handler(null, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Logger.Warn("ChatPauseState", "Error notifying state change listener", ex);
                }
            }
        }
    }
}
