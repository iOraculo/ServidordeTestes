using System;
using System.IO;
using Newtonsoft.Json;
using _4RTools.Utils;

namespace _4RTools.Model
{
    public class AppSettings
    {
        private static readonly object FileLock = new object();
        private static string settingsFilePath = "settings.json";

        public static string SettingsFilePath
        {
            get { return settingsFilePath; }
            set { settingsFilePath = value; }
        }

        public bool DarkMode { get; set; } = true;
        public bool AlwaysOnTop { get; set; } = false;
        public bool UseBuffsInTown { get; set; } = false;
        public bool PreRenewalMode { get; set; } = true;
        public bool AutoSwitchProfileByCharacter { get; set; } = true;
        public bool ShowMiniHud { get; set; } = false;
        public int MiniHudX { get; set; } = -1;
        public int MiniHudY { get; set; } = -1;
        public string Language { get; set; } = "pt-BR";

        public static AppSettings Load()
        {
            try
            {
                lock (FileLock)
                {
                    if (!File.Exists(settingsFilePath))
                    {
                        return new AppSettings();
                    }

                    string json = File.ReadAllText(settingsFilePath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return new AppSettings();
                    }

                    AppSettings loaded = JsonConvert.DeserializeObject<AppSettings>(json);
                    return loaded != null ? loaded : new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("AppSettings", "Failed to load settings.json; using defaults", ex);
                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            try
            {
                lock (FileLock)
                {
                    string dir = Path.GetDirectoryName(settingsFilePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                    string tempPath = settingsFilePath + ".tmp";
                    File.WriteAllText(tempPath, json);
                    File.Copy(tempPath, settingsFilePath, true);
                    File.Delete(tempPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("AppSettings", "Failed to save settings.json", ex);
            }
        }
    }
}
