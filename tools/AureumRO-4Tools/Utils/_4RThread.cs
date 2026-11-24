using System;
using System.Threading;

namespace _4RTools.Utils
{
    public class _4RThread
    {
        private readonly Thread thread;
        private readonly CancellationTokenSource cts;
        private readonly object stateLock = new object();
        private bool started;
        private bool stopped;

        public _4RThread(Func<int, int> toRun, string name = null)
            : this((CancellationToken _) => { toRun(0); }, name)
        {
        }

        public _4RThread(Action<CancellationToken> toRun, string name = null)
        {
            if (toRun == null)
            {
                throw new ArgumentNullException(nameof(toRun));
            }

            this.cts = new CancellationTokenSource();
            CancellationToken token = this.cts.Token;
            string threadName = string.IsNullOrWhiteSpace(name) ? "4RWorker" : name;
            SessionContext capturedSession = SessionContext.Current;

            this.thread = new Thread(() =>
            {
                using (SessionContext.Enter(capturedSession))
                {
                    try
                    {
                        while (!token.IsCancellationRequested)
                        {
                            try
                            {
                                toRun(token);
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("_4RThread", "Error while executing thread method (" + threadName + ")", ex);
                            }

                            if (token.IsCancellationRequested)
                            {
                                break;
                            }

                            token.WaitHandle.WaitOne(5);
                        }
                    }
                    finally
                    {
                        lock (this.stateLock)
                        {
                            this.stopped = true;
                            this.cts.Dispose();
                        }
                    }
                }
            });

            this.thread.Name = threadName;
            this.thread.IsBackground = true;
            this.thread.SetApartmentState(ApartmentState.STA);
        }

        public bool IsRunning
        {
            get
            {
                lock (this.stateLock)
                {
                    return this.started && !this.stopped && this.thread.IsAlive;
                }
            }
        }

        public static bool Sleep(int milliseconds, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return false;
            }

            if (milliseconds <= 0)
            {
                return true;
            }

            return !token.WaitHandle.WaitOne(milliseconds);
        }

        public static void Start(_4RThread instance)
        {
            if (instance == null)
            {
                return;
            }

            lock (instance.stateLock)
            {
                if (instance.started || instance.stopped)
                {
                    return;
                }

                instance.started = true;
                instance.thread.Start();
            }
        }

        public static void Stop(_4RThread instance)
        {
            if (instance == null)
            {
                return;
            }

            bool shouldJoin = false;
            lock (instance.stateLock)
            {
                if (instance.stopped)
                {
                    return;
                }

                try
                {
                    instance.cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    return;
                }

                if (!instance.started)
                {
                    instance.stopped = true;
                    instance.cts.Dispose();
                    return;
                }

                shouldJoin = Thread.CurrentThread.ManagedThreadId != instance.thread.ManagedThreadId;
            }

            if (shouldJoin && instance.thread.IsAlive)
            {
                try
                {
                    instance.thread.Join(300);
                }
                catch (Exception ex)
                {
                    Logger.Warn("_4RThread", "Could not join worker thread cleanly", ex);
                }
            }
        }
    }
}
