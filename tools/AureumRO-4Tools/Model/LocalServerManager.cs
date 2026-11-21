using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using _4RTools.Utils;

namespace _4RTools.Model
{
    public class LocalServerManager
    {
        private static readonly object FileLock = new object();
        private static string localServerName = "supported_servers.json";

        public static string LocalServerFilePath
        {
            get { return localServerName; }
            set { localServerName = value; }
        }

        public static void AddServer(string hpAddress, string nameAddress, string processName)
        {
            AddServer(hpAddress, nameAddress, processName, null);
        }

        public static void AddServer(string hpAddress, string nameAddress, string processName, string mapAddress)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                throw new ArgumentException("Process name cannot be empty.");
            }

            string normalizedHp = NormalizeHexAddress(hpAddress);
            if (normalizedHp == null)
            {
                throw new ArgumentException("HP Address is Invalid. Please type a valid Hex value.");
            }

            string normalizedName = NormalizeHexAddress(nameAddress);
            if (normalizedName == null)
            {
                throw new ArgumentException("Name Address is Invalid. Please type a valid Hex value.");
            }

            string normalizedMap = null;
            if (!string.IsNullOrWhiteSpace(mapAddress))
            {
                normalizedMap = NormalizeHexAddress(mapAddress);
                if (normalizedMap == null)
                {
                    throw new ArgumentException("Map Address is Invalid. Please type a valid Hex value.");
                }
            }

            ClientDTO dto = new ClientDTO(processName.Trim(), null, normalizedHp, normalizedName, normalizedMap);
            ClientListSingleton.AddClient(new Client(dto));

            lock (FileLock)
            {
                List<ClientDTO> clients = GetLocalClients();
                clients.Add(dto);
                OverwriteLocalFile(clients);
            }
        }

        public static void RemoveClient(ClientDTO dto)
        {
            if (dto == null)
            {
                return;
            }

            lock (FileLock)
            {
                List<ClientDTO> clients = GetLocalClients();
                if (dto.index >= 0 && dto.index < clients.Count)
                {
                    clients.RemoveAt(dto.index);
                }
                else
                {
                    int idx = clients.FindIndex(c =>
                        string.Equals(c.name, dto.name, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(c.hpAddress, dto.hpAddress, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(c.nameAddress, dto.nameAddress, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0)
                    {
                        clients.RemoveAt(idx);
                    }
                }

                OverwriteLocalFile(clients);
            }

            Client matched = Client.FromDTO(dto);
            if (matched != null)
            {
                ClientListSingleton.RemoveClient(matched);
            }
        }

        private static void OverwriteLocalFile(List<ClientDTO> clients)
        {
            string output = JsonConvert.SerializeObject(clients, Formatting.Indented);
            string tempPath = localServerName + ".tmp";
            File.WriteAllText(tempPath, output);
            File.Copy(tempPath, localServerName, true);
            File.Delete(tempPath);
        }

        private static string LoadLocalServerFile()
        {
            lock (FileLock)
            {
                if (!File.Exists(localServerName))
                {
                    const string startJson = "[]";
                    File.WriteAllText(localServerName, startJson);
                    return startJson;
                }

                return File.ReadAllText(localServerName);
            }
        }

        public static List<ClientDTO> GetLocalClients()
        {
            string localServers = LoadLocalServerFile();

            if (string.IsNullOrWhiteSpace(localServers))
            {
                return new List<ClientDTO>();
            }

            try
            {
                List<ClientDTO> deserialized = JsonConvert.DeserializeObject<List<ClientDTO>>(localServers);
                return deserialized != null ? deserialized : new List<ClientDTO>();
            }
            catch (Exception ex)
            {
                Logger.Warn("LocalServerManager", "Failed to parse local servers JSON", ex);
                return new List<ClientDTO>();
            }
        }

        public static string NormalizeHexAddress(string rawAddress)
        {
            if (string.IsNullOrWhiteSpace(rawAddress))
            {
                return null;
            }

            string trimmed = rawAddress.Trim();
            if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(2);
            }

            if (trimmed.Length == 0 || trimmed.Length > 8 || !IsHex(trimmed))
            {
                return null;
            }

            return trimmed.PadLeft(8, '0').ToUpperInvariant();
        }

        public static bool IsHex(IEnumerable<char> chars)
        {
            if (chars == null)
            {
                return false;
            }

            foreach (char c in chars)
            {
                bool isHex = (c >= '0' && c <= '9') ||
                             (c >= 'a' && c <= 'f') ||
                             (c >= 'A' && c <= 'F');

                if (!isHex)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
