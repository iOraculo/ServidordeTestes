using System;
using System.Collections.Generic;

namespace _4RTools.Utils
{
    public interface IObserver
    {
        void Update(ISubject subject);
    }

    public interface ISubject
    {
        void Attach(IObserver observer);
        void Detach(IObserver observer);
        void Notify(Message message);
    }

    public enum MessageCode
    {
        PROCESS_CHANGED,
        PROFILE_CHANGED,
        PROFILE_INPUT_CHANGE,
        TURN_ON,
        TURN_OFF,
        SHUTDOWN_APPLICATION,
        CLICK_ICON_TRAY,
        SERVER_LIST_CHANGED,
        CLIENT_DISCONNECTED,
        LANGUAGE_CHANGED
    }

    public class Message
    {
        public MessageCode code { get; }
        public object data { get; set; }

        public Message() { }

        public Message(MessageCode code, object data)
        {
            this.code = code;
            this.data = data;
        }
    }

    public class Subject : ISubject
    {
        private readonly object _lock = new object();
        private readonly List<IObserver> _observers = new List<IObserver>();

        public Message Message { get; set; } = new Message();

        public void Attach(IObserver observer)
        {
            if (observer == null)
            {
                return;
            }

            lock (this._lock)
            {
                if (!this._observers.Contains(observer))
                {
                    this._observers.Add(observer);
                }
            }
        }

        public void Detach(IObserver observer)
        {
            if (observer == null)
            {
                return;
            }

            lock (this._lock)
            {
                this._observers.Remove(observer);
            }
        }

        public void Notify(Message message)
        {
            IObserver[] snapshot;
            lock (this._lock)
            {
                this.Message = message;
                snapshot = this._observers.ToArray();
            }

            foreach (IObserver observer in snapshot)
            {
                try
                {
                    observer.Update(this);
                }
                catch (Exception ex)
                {
                    Logger.Error("Subject", "Observer update failed for " + message.code, ex);
                }
            }
        }
    }
}
