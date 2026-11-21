using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using _4RTools.Model;

namespace _4RTools.Utils
{
    public class BackupRestoreResult
    {
        public int ProfilesRestored { get; set; }
        public bool ServersRestored { get; set; }
        public bool SettingsRestored { get; set; }
    }

    public static class ProfileBackupManager
    {
        private const string PROFILE_PREFIX = "Profile/";
        private const string SERVERS_ENTRY = "supported_servers.json";
        private const string SETTINGS_ENTRY = "settings.json";

        public static int ExportBackupZip(string destinationZipPath)
        {
            if (string.IsNullOrWhiteSpace(destinationZipPath))
            {
                throw new ArgumentException("Backup destination path cannot be empty.");
            }

            string dir = Path.GetDirectoryName(destinationZipPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string tempZipPath = destinationZipPath + ".tmp";
            if (File.Exists(tempZipPath))
            {
                File.Delete(tempZipPath);
            }

            int filesExported = 0;
            try
            {
                using (FileStream zipStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                {
                    foreach (string profileName in Profile.ListAll())
                    {
                        string profilePath = ProfileSingleton.GetProfilePath(profileName);
                        if (File.Exists(profilePath))
                        {
                            AddTextFileEntry(archive, PROFILE_PREFIX + profileName + ".json", File.ReadAllText(profilePath));
                            filesExported++;
                        }
                    }

                    if (File.Exists(LocalServerManager.LocalServerFilePath))
                    {
                        AddTextFileEntry(archive, SERVERS_ENTRY, File.ReadAllText(LocalServerManager.LocalServerFilePath));
                        filesExported++;
                    }

                    if (File.Exists(AppSettings.SettingsFilePath))
                    {
                        AddTextFileEntry(archive, SETTINGS_ENTRY, File.ReadAllText(AppSettings.SettingsFilePath));
                        filesExported++;
                    }
                }

                File.Copy(tempZipPath, destinationZipPath, true);
                File.Delete(tempZipPath);
                return filesExported;
            }
            catch
            {
                try
                {
                    if (File.Exists(tempZipPath))
                    {
                        File.Delete(tempZipPath);
                    }
                }
                catch
                {
                    // Ignore temp file cleanup error.
                }

                throw;
            }
        }

        public static BackupRestoreResult ImportBackupZip(string sourceZipPath)
        {
            if (string.IsNullOrWhiteSpace(sourceZipPath) || !File.Exists(sourceZipPath))
            {
                throw new FileNotFoundException("Backup archive not found.", sourceZipPath);
            }

            BackupRestoreResult result = new BackupRestoreResult();

            using (ZipArchive archive = ZipFile.OpenRead(sourceZipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.Name))
                    {
                        continue;
                    }

                    string normalized = entry.FullName.Replace('\\', '/').TrimStart('/');
                    if (normalized.Contains(".."))
                    {
                        throw new InvalidDataException("Archive contains an unsafe path entry: " + entry.FullName);
                    }

                    if (normalized.StartsWith(PROFILE_PREFIX, StringComparison.OrdinalIgnoreCase)
                        && normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        string relativeName = normalized.Substring(PROFILE_PREFIX.Length);
                        if (relativeName.Contains("/"))
                        {
                            continue;
                        }

                        string profileName = ProfileSingleton.ValidateProfileName(Path.GetFileNameWithoutExtension(relativeName));
                        string json = ReadEntryText(entry);
                        JObject.Parse(json);

                        if (!Directory.Exists(AppConfig.ProfileFolder))
                        {
                            Directory.CreateDirectory(AppConfig.ProfileFolder);
                        }

                        string targetPath = ProfileSingleton.GetProfilePath(profileName);
                        WriteTextAtomic(targetPath, json);
                        result.ProfilesRestored++;
                    }
                    else if (string.Equals(normalized, SERVERS_ENTRY, StringComparison.OrdinalIgnoreCase))
                    {
                        string json = ReadEntryText(entry);
                        List<ClientDTO> servers = JsonConvert.DeserializeObject<List<ClientDTO>>(json);
                        if (servers != null)
                        {
                            string targetDir = Path.GetDirectoryName(LocalServerManager.LocalServerFilePath);
                            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                            {
                                Directory.CreateDirectory(targetDir);
                            }

                            WriteTextAtomic(LocalServerManager.LocalServerFilePath, json);
                            foreach (ClientDTO dto in servers)
                            {
                                if (dto != null && Client.FromDTO(dto) == null)
                                {
                                    ClientListSingleton.AddClient(new Client(dto));
                                }
                            }
                            result.ServersRestored = true;
                        }
                    }
                    else if (string.Equals(normalized, SETTINGS_ENTRY, StringComparison.OrdinalIgnoreCase))
                    {
                        string json = ReadEntryText(entry);
                        AppSettings restoredSettings = JsonConvert.DeserializeObject<AppSettings>(json);
                        if (restoredSettings != null)
                        {
                            AppSettings.Save(restoredSettings);
                            result.SettingsRestored = true;
                        }
                    }
                }
            }

            return result;
        }

        private static void AddTextFileEntry(ZipArchive archive, string entryName, string content)
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using (Stream entryStream = entry.Open())
            using (StreamWriter writer = new StreamWriter(entryStream, new UTF8Encoding(false)))
            {
                writer.Write(content ?? string.Empty);
            }
        }

        private static string ReadEntryText(ZipArchiveEntry entry)
        {
            using (Stream entryStream = entry.Open())
            using (StreamReader reader = new StreamReader(entryStream, Encoding.UTF8, true))
            {
                return reader.ReadToEnd();
            }
        }

        private static void WriteTextAtomic(string targetPath, string content)
        {
            string tempPath = targetPath + ".tmp";
            File.WriteAllText(tempPath, content);
            File.Copy(tempPath, targetPath, true);
            File.Delete(tempPath);
        }
    }
}
