using System;
using System.Threading;
using System.Windows.Input;

namespace _4RTools.Utils
{
    public static class ChatPauseState
    {
        public static event EventHandler StateChanged;

        public static bool IsChatting
        {
            get { return SessionContext.Current.IsChatting; }
        }

        public static void ToggleChatting()
        {
            SessionContext current = SessionContext.Current;
            if (current.ToggleChatting())
            {
                RaiseStateChanged(current);
            }
        }

        public static void SetChatting(bool chatting)
        {
            SessionContext current = SessionContext.Current;
            if (current.SetChatting(chatting))
            {
                RaiseStateChanged(current);
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

        private static void RaiseStateChanged(SessionContext senderSession)
        {
            EventHandler handler = StateChanged;
            if (handler != null)
            {
                try
                {
                    handler(senderSession, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Logger.Warn("ChatPauseState", "Error notifying state change listener", ex);
                }
            }
        }
    }
}
