using System;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using Newtonsoft.Json;
using _4RTools.Utils;

namespace _4RTools.Model
{
    public class AutoRefreshSpammer : Action
    {
        private _4RThread thread;
        public string ActionName { get; set; }
        public int RefreshDelay { get; set; } = 5;
        public Key RefreshKey { get; set; }

        public AutoRefreshSpammer(string actionName)
        {
            this.ActionName = actionName;
        }

        public void Start()
        {
            Stop();
            Client roClient = ClientSingleton.GetClient();
            if (roClient != null)
            {
                const int defaultDelayMs = 1000;
                int delayMs = this.RefreshDelay > 0 ? this.RefreshDelay * 1000 : defaultDelayMs;
                this.thread = new _4RThread(token => AutorefreshThreadExecution(roClient, delayMs, token), this.ActionName);
                _4RThread.Start(this.thread);
            }
        }

        private void AutorefreshThreadExecution(Client roClient, int delayMs, CancellationToken token)
        {
            if (this.RefreshKey != Key.None
                && roClient.IsProcessAlive()
                && !roClient.ShouldPauseAutomation()
                && !KeyHelper.IsAltPressed())
            {
                Keys formsKey = KeyHelper.ToFormsKeys(this.RefreshKey);
                Interop.SendKeyDown(roClient.GetWindowHandle(), formsKey);
            }

            _4RThread.Sleep(delayMs, token);
        }

        public void Stop()
        {
            _4RThread.Stop(this.thread);
            this.thread = null;
        }

        public string GetConfiguration()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string GetActionName()
        {
            return this.ActionName;
        }
    }
}
