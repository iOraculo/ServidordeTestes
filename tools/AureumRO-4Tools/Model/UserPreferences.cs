using Newtonsoft.Json;
using System.Windows.Forms;

namespace _4RTools.Model
{
    public class UserPreferences : Action
    {
        private const string ACTION_NAME = "UserPreferences";
        public string toggleStateKey { get; set; } = Keys.End.ToString();
        public bool soundEnabled { get; set; } = true;
        public bool pauseWhenUnfocused { get; set; } = false;
        public bool pauseOnChat { get; set; } = false;
        public string boundCharacter { get; set; } = string.Empty;

        public UserPreferences()
        {
        }

        public void Start() { }

        public void Stop() { }

        public string GetConfiguration()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string GetActionName()
        {
            return ACTION_NAME;
        }
    }
}
