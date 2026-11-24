using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using _4RTools.Forms;
using _4RTools.Utils;

namespace _4RTools.Model
{
    public class ProfileSingleton
    {
        private static readonly object FileLock = new object();
        private static int suppressSaveDepth = 0;

        public static Profile profile
        {
            get { return SessionContext.Current.Profile; }
            set { SessionContext.Current.Profile = value; }
        }

        private sealed class SaveSuppressionScope : IDisposable
        {
            private bool disposed;

            public SaveSuppressionScope()
            {
                Interlocked.Increment(ref suppressSaveDepth);
            }

            public void Dispose()
            {
                if (!this.disposed)
                {
                    this.disposed = true;
                    Interlocked.Decrement(ref suppressSaveDepth);
                }
            }
        }

        public static IDisposable SuppressSave()
        {
            return new SaveSuppressionScope();
        }

        public static bool IsSaveSuppressed
        {
            get { return Volatile.Read(ref suppressSaveDepth) > 0; }
        }

        public static string GetProfilePath(string profileName)
        {
            return Path.Combine(AppConfig.ProfileFolder, profileName + ".json");
        }

        public static string ValidateProfileName(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                throw new ArgumentException("Profile name cannot be empty.");
            }

            string trimmed = profileName.Trim();
            char[] invalidChars = Path.GetInvalidFileNameChars();
            if (trimmed.IndexOfAny(invalidChars) >= 0 || trimmed.Contains("..") || trimmed.Contains("/") || trimmed.Contains("\\"))
            {
                throw new ArgumentException("Profile name contains invalid characters.");
            }

            return trimmed;
        }

        public static void Load(string profileName)
        {
            try
            {
                string validName = ValidateProfileName(profileName);
                string filePath = GetProfilePath(validName);
                string json;
                lock (FileLock)
                {
                    json = File.ReadAllText(filePath);
                }

                JObject rawObject = string.IsNullOrWhiteSpace(json) ? new JObject() : JObject.Parse(json);

                // Always start from a fresh Profile so missing sections do not leak settings from the previous profile.
                Profile loaded = new Profile(validName);
                loaded.UserPreferences = DeserializeAction(rawObject, loaded.UserPreferences);
                loaded.AHK = DeserializeAction(rawObject, loaded.AHK);
                loaded.Autopot = DeserializeAction(rawObject, loaded.Autopot);
                loaded.AutopotYgg = DeserializeAction(rawObject, loaded.AutopotYgg);
                loaded.StatusRecovery = DeserializeAction(rawObject, loaded.StatusRecovery);
                loaded.AutoRefreshSpammer1 = DeserializeAction(rawObject, loaded.AutoRefreshSpammer1);
                loaded.AutoRefreshSpammer2 = DeserializeAction(rawObject, loaded.AutoRefreshSpammer2);
                loaded.AutoRefreshSpammer3 = DeserializeAction(rawObject, loaded.AutoRefreshSpammer3);
                loaded.AutoRefreshSpammer4 = DeserializeAction(rawObject, loaded.AutoRefreshSpammer4);
                loaded.AutoRefreshSpammer5 = DeserializeAction(rawObject, loaded.AutoRefreshSpammer5);
                loaded.AutoRefreshSpammer6 = DeserializeAction(rawObject, loaded.AutoRefreshSpammer6);
                loaded.Autobuff = DeserializeAction(rawObject, loaded.Autobuff);
                loaded.SongMacro = DeserializeAction(rawObject, loaded.SongMacro);
                loaded.AtkDefMode = DeserializeAction(rawObject, loaded.AtkDefMode);
                loaded.MacroSwitch = DeserializeAction(rawObject, loaded.MacroSwitch);
                loaded.DebuffsRecovery = DeserializeAction(rawObject, loaded.DebuffsRecovery);
                loaded.AutoMount = DeserializeAction(rawObject, loaded.AutoMount);
                loaded.AutoMount.EnsureExtraSkillSlotCount();
                loaded.AutoRefreshCommand = DeserializeAction(rawObject, loaded.AutoRefreshCommand);
                loaded.AutoEscape = DeserializeAction(rawObject, loaded.AutoEscape);

                profile = loaded;
            }
            catch (Exception ex)
            {
                Logger.Error("Profile", "Error loading profile '" + profileName + "'", ex);
                throw new Exception("Houve um problema ao carregar o perfil. Delete a pasta Profile e tente novamente.", ex);
            }
        }

        private static T DeserializeAction<T>(JObject rawObject, T defaultAction) where T : Action
        {
            string rawSection = Profile.GetByAction(rawObject, defaultAction);
            T deserialized = JsonConvert.DeserializeObject<T>(rawSection);
            return deserialized != null ? deserialized : defaultAction;
        }

        public static void Create(string profileName)
        {
            string validName = ValidateProfileName(profileName);
            string jsonFileName = GetProfilePath(validName);

            lock (FileLock)
            {
                if (!Directory.Exists(AppConfig.ProfileFolder))
                {
                    Directory.CreateDirectory(AppConfig.ProfileFolder);
                }

                if (!File.Exists(jsonFileName))
                {
                    Profile newProfile = new Profile(validName);
                    string output = JsonConvert.SerializeObject(newProfile, Formatting.Indented);
                    WriteAllTextAtomic(jsonFileName, output);
                }
            }

            Load(validName);
        }

        public static void Delete(string profileName)
        {
            try
            {
                string validName = ValidateProfileName(profileName);
                if (string.Equals(validName, "Default", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                lock (FileLock)
                {
                    string path = GetProfilePath(validName);
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Profile", "Failed to delete profile '" + profileName + "'", ex);
            }
        }

        public static void Rename(string oldProfileName, string newProfileName)
        {
            string validOld = ValidateProfileName(oldProfileName);
            string validNew = ValidateProfileName(newProfileName);

            if (string.Equals(validOld, "Default", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Cannot rename the Default profile.");
            }

            if (string.Equals(validOld, validNew, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string oldPath = GetProfilePath(validOld);
            string newPath = GetProfilePath(validNew);

            lock (FileLock)
            {
                if (!File.Exists(oldPath))
                {
                    throw new FileNotFoundException("Profile not found.", oldPath);
                }

                if (File.Exists(newPath))
                {
                    throw new IOException("A profile named '" + validNew + "' already exists.");
                }

                File.Move(oldPath, newPath);
            }

            if (profile != null && string.Equals(profile.Name, validOld, StringComparison.OrdinalIgnoreCase))
            {
                profile.Name = validNew;
            }
        }

        public static string Copy(string profileName)
        {
            string validName = ValidateProfileName(profileName);
            if (string.Equals(validName, "Default", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Cannot copy the Default profile.");
            }

            lock (FileLock)
            {
                string sourcePath = GetProfilePath(validName);
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException("Profile not found.", sourcePath);
                }

                string copyName = validName + " Copy";
                string destinationPath = GetProfilePath(copyName);
                int suffix = 2;

                while (File.Exists(destinationPath))
                {
                    copyName = validName + " Copy (" + suffix + ")";
                    destinationPath = GetProfilePath(copyName);
                    suffix++;
                }

                File.Copy(sourcePath, destinationPath);
                return copyName;
            }
        }

        public static void Export(string profileName, string destinationFilePath)
        {
            string validName = ValidateProfileName(profileName);
            if (string.IsNullOrWhiteSpace(destinationFilePath))
            {
                throw new ArgumentException("Destination path cannot be empty.");
            }

            lock (FileLock)
            {
                string sourcePath = GetProfilePath(validName);
                if (!File.Exists(sourcePath))
                {
                    throw new FileNotFoundException("Profile not found.", sourcePath);
                }

                File.Copy(sourcePath, destinationFilePath, true);
            }
        }

        public static string Import(string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException("Source profile file not found.", sourceFilePath);
            }

            string json = File.ReadAllText(sourceFilePath);
            // Validate JSON before copying into Profile folder.
            JObject.Parse(json);

            string baseName = ValidateProfileName(Path.GetFileNameWithoutExtension(sourceFilePath));
            if (string.Equals(baseName, "Default", StringComparison.OrdinalIgnoreCase))
            {
                baseName = "Default Imported";
            }

            lock (FileLock)
            {
                if (!Directory.Exists(AppConfig.ProfileFolder))
                {
                    Directory.CreateDirectory(AppConfig.ProfileFolder);
                }

                string targetName = baseName;
                string targetPath = GetProfilePath(targetName);
                int suffix = 2;

                while (File.Exists(targetPath))
                {
                    targetName = baseName + " (" + suffix + ")";
                    targetPath = GetProfilePath(targetName);
                    suffix++;
                }

                WriteAllTextAtomic(targetPath, json);
                return targetName;
            }
        }

        public static string GetBoundCharacter(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                return string.Empty;
            }

            try
            {
                string validName = ValidateProfileName(profileName);
                string filePath = GetProfilePath(validName);
                string json;

                lock (FileLock)
                {
                    if (!File.Exists(filePath))
                    {
                        return string.Empty;
                    }

                    json = File.ReadAllText(filePath);
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    return string.Empty;
                }

                JObject rawObject = JObject.Parse(json);
                UserPreferences prefs = DeserializeAction(rawObject, new UserPreferences());
                return prefs != null && prefs.boundCharacter != null ? prefs.boundCharacter.Trim() : string.Empty;
            }
            catch (Exception ex)
            {
                Logger.Warn("Profile", "Failed to read bound character for '" + profileName + "'", ex);
                return string.Empty;
            }
        }

        public static void SetBoundCharacter(string profileName, string characterName)
        {
            string validName = ValidateProfileName(profileName);
            string normalizedChar = string.IsNullOrWhiteSpace(characterName) ? string.Empty : characterName.Trim();
            string filePath = GetProfilePath(validName);

            lock (FileLock)
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("Profile not found.", filePath);
                }

                string jsonData = File.ReadAllText(filePath);
                JObject jsonObj = string.IsNullOrWhiteSpace(jsonData) ? new JObject() : JObject.Parse(jsonData);
                UserPreferences prefs = DeserializeAction(jsonObj, new UserPreferences());
                prefs.boundCharacter = normalizedChar;

                jsonObj[prefs.GetActionName()] = prefs.GetConfiguration();
                WriteAllTextAtomic(filePath, jsonObj.ToString(Formatting.Indented));
            }

            if (profile != null && string.Equals(profile.Name, validName, StringComparison.OrdinalIgnoreCase))
            {
                profile.UserPreferences.boundCharacter = normalizedChar;
            }
        }

        public static string FindProfileForCharacter(string characterName)
        {
            if (string.IsNullOrWhiteSpace(characterName))
            {
                return null;
            }

            string target = characterName.Trim();
            foreach (string profileName in Profile.ListAll())
            {
                string bound = GetBoundCharacter(profileName);
                if (!string.IsNullOrEmpty(bound) && string.Equals(bound, target, StringComparison.OrdinalIgnoreCase))
                {
                    return profileName;
                }
            }

            return null;
        }

        public static void SetConfiguration(Action action)
        {
            if (IsSaveSuppressed || profile == null || action == null)
            {
                return;
            }

            try
            {
                lock (FileLock)
                {
                    if (!Directory.Exists(AppConfig.ProfileFolder))
                    {
                        Directory.CreateDirectory(AppConfig.ProfileFolder);
                    }

                    string filePath = GetProfilePath(profile.Name);
                    JObject jsonObj;
                    if (File.Exists(filePath))
                    {
                        string jsonData = File.ReadAllText(filePath);
                        jsonObj = string.IsNullOrWhiteSpace(jsonData) ? new JObject() : JObject.Parse(jsonData);
                    }
                    else
                    {
                        jsonObj = JObject.FromObject(profile);
                    }

                    jsonObj[action.GetActionName()] = action.GetConfiguration();
                    string output = jsonObj.ToString(Formatting.Indented);
                    WriteAllTextAtomic(filePath, output);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Profile", "Failed to save configuration for " + action.GetActionName(), ex);
            }
        }

        private static void WriteAllTextAtomic(string targetPath, string content)
        {
            string tempPath = targetPath + ".tmp";
            File.WriteAllText(tempPath, content);
            File.Copy(tempPath, targetPath, true);
            File.Delete(tempPath);
        }

        public static Profile GetCurrent()
        {
            return profile;
        }
    }

    public class Profile
    {
        public string Name { get; set; }
        public UserPreferences UserPreferences { get; set; }
        public AHK AHK { get; set; }
        public Autopot Autopot { get; set; }
        public Autopot AutopotYgg { get; set; }
        public AutoRefreshSpammer AutoRefreshSpammer1 { get; set; }
        public AutoRefreshSpammer AutoRefreshSpammer2 { get; set; }
        public AutoRefreshSpammer AutoRefreshSpammer3 { get; set; }
        public AutoRefreshSpammer AutoRefreshSpammer4 { get; set; }
        public AutoRefreshSpammer AutoRefreshSpammer5 { get; set; }
        public AutoRefreshSpammer AutoRefreshSpammer6 { get; set; }
        public AutoBuff Autobuff { get; set; }
        public StatusRecovery StatusRecovery { get; set; }
        public Macro SongMacro { get; set; }
        public Macro MacroSwitch { get; set; }
        public ATKDEFMode AtkDefMode { get; set; }
        public DebuffsRecovery DebuffsRecovery { get; set; }
        public AutoMount AutoMount { get; set; }
        public AutoRefreshCommand AutoRefreshCommand { get; set; }
        public AutoEscape AutoEscape { get; set; }

        public Profile(string name)
        {
            this.Name = name;

            this.UserPreferences = new UserPreferences();
            this.AHK = new AHK();
            this.Autopot = new Autopot(Autopot.ACTION_NAME_AUTOPOT);
            this.AutopotYgg = new Autopot(Autopot.ACTION_NAME_AUTOPOT_YGG);
            this.AutoRefreshSpammer1 = new AutoRefreshSpammer(actionName: "AutoRefreshSpammer01");
            this.AutoRefreshSpammer2 = new AutoRefreshSpammer(actionName: "AutoRefreshSpammer02");
            this.AutoRefreshSpammer3 = new AutoRefreshSpammer(actionName: "AutoRefreshSpammer03");
            this.AutoRefreshSpammer4 = new AutoRefreshSpammer(actionName: "AutoRefreshSpammer04");
            this.AutoRefreshSpammer5 = new AutoRefreshSpammer(actionName: "AutoRefreshSpammer05");
            this.AutoRefreshSpammer6 = new AutoRefreshSpammer(actionName: "AutoRefreshSpammer06");
            this.Autobuff = new AutoBuff();
            this.StatusRecovery = new StatusRecovery();
            this.SongMacro = new Macro(Macro.ACTION_NAME_SONG_MACRO, MacroSongForm.TOTAL_MACRO_LANES_FOR_SONGS);
            this.MacroSwitch = new Macro(Macro.ACTION_NAME_MACRO_SWITCH, MacroSwitchForm.TOTAL_MACRO_LANES);
            this.AtkDefMode = new ATKDEFMode();
            this.DebuffsRecovery = new DebuffsRecovery();
            this.AutoMount = new AutoMount();
            this.AutoRefreshCommand = new AutoRefreshCommand();
            this.AutoEscape = new AutoEscape();
        }

        public static string GetByAction(JObject obj, Action action)
        {
            if (obj != null && action != null)
            {
                JToken token = obj[action.GetActionName()];
                if (token != null && token.Type != JTokenType.Null)
                {
                    return token.Type == JTokenType.String ? token.Value<string>() : token.ToString(Formatting.None);
                }
            }

            return action.GetConfiguration();
        }

        public static List<string> ListAll()
        {
            List<string> profiles = new List<string>();
            try
            {
                if (!Directory.Exists(AppConfig.ProfileFolder))
                {
                    return profiles;
                }

                string[] files = Directory.GetFiles(AppConfig.ProfileFolder, "*.json");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);

                foreach (string filePath in files)
                {
                    string profileName = Path.GetFileNameWithoutExtension(filePath);
                    if (!string.IsNullOrWhiteSpace(profileName))
                    {
                        profiles.Add(profileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Profile", "Failed to list profiles", ex);
            }

            return profiles;
        }
    }
}