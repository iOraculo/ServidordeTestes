using Newtonsoft.Json;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Utils;

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
        public int playerInputYieldMs { get; set; } = ActionPriorityCoordinator.DEFAULT_PLAYER_INPUT_YIELD_MS;
        public Key holdPauseKey { get; set; } = Key.None;
        public bool holdPauseIncludeAutopot { get; set; } = false;

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
