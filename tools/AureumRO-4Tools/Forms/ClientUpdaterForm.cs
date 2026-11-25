using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class ClientUpdaterForm : Form
    {
        public ClientUpdaterForm()
        {
            InitializeComponent();
            LocalizationManager.ApplyToControlTree(this);
            ThemeManager.ApplyTheme(this, AppSettings.Load().DarkMode);
            this.Shown += ClientUpdaterForm_Shown;
        }

        private async void ClientUpdaterForm_Shown(object sender, EventArgs e)
        {
            await StartUpdateAsync();
        }

        private async Task StartUpdateAsync()
        {
            List<KeyValuePair<ClientDTO, bool>> clients = new List<KeyValuePair<ClientDTO, bool>>();

            try
            {
                // 1. Highest priority: remote supported_servers.json from the official repository (allows live address updates).
                try
                {
                    string remoteServersRaw = await HttpClientProvider.GetStringAsync(AppConfig._4RClientsURL, TimeSpan.FromSeconds(5));
                    List<ClientDTO> remoteClients = JsonConvert.DeserializeObject<List<ClientDTO>>(remoteServersRaw);
                    if (remoteClients != null)
                    {
                        foreach (ClientDTO remoteClient in remoteClients)
                        {
                            clients.Add(new KeyValuePair<ClientDTO, bool>(remoteClient, false));
                        }
                    }
                }
                catch (Exception remoteEx)
                {
                    Logger.Warn("ClientUpdaterForm", "Cannot load remote supported_servers.json; using embedded servers", remoteEx);
                }

                // 2. Always include embedded servers (AureumRO + built-in Test Server) so standalone .exe works offline and stale local files never override official addresses.
                try
                {
                    List<ClientDTO> resourceClients = JsonConvert.DeserializeObject<List<ClientDTO>>(LoadResourceServerFile());
                    if (resourceClients != null)
                    {
                        foreach (ClientDTO resourceClient in resourceClients)
                        {
                            clients.Add(new KeyValuePair<ClientDTO, bool>(resourceClient, false));
                        }
                    }
                }
                catch (Exception resEx)
                {
                    Logger.Error("ClientUpdaterForm", "Failed to load embedded supported_servers resource", resEx);
                }

                // 3. Guaranteed built-in fallback for AureumRO Servidor de Testes (FroggoClient / Ragexe 2022-04-06).
                clients.Add(new KeyValuePair<ClientDTO, bool>(
                    new ClientDTO("FroggoClient", "AureumRO Servidor de Testes (FroggoClient)", "0x01138DDC", "0x0113B7B0", "0x00D2A3BC", "0x480"),
                    false));
                clients.Add(new KeyValuePair<ClientDTO, bool>(
                    new ClientDTO("Ragexe", "AureumRO Servidor de Testes (Ragexe)", "0x01138DDC", "0x0113B7B0", "0x00D2A3BC", "0x480"),
                    false));

                // 4. Fallback only: local supported_servers.json next to the executable (cannot override official AureumRO or built-in test server entries).
                foreach (ClientDTO localClient in LocalServerManager.GetLocalClients())
                {
                    clients.Add(new KeyValuePair<ClientDTO, bool>(localClient, true));
                }
            }
            finally
            {
                LoadServers(clients);
                if (!this.IsDisposed)
                {
                    this.Close();
                }
            }
        }

        public const string EXCLUSIVE_SERVER_NAME = "AureumRO";

        private static readonly HashSet<string> BuiltInTestServerNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "FroggoClient",
            "Ragexe"
        };

        public static bool IsExclusiveServer(ClientDTO clientDTO)
        {
            return clientDTO != null
                && !string.IsNullOrWhiteSpace(clientDTO.name)
                && string.Equals(clientDTO.name.Trim(), EXCLUSIVE_SERVER_NAME, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsBuiltInTestServerName(string processName)
        {
            return !string.IsNullOrWhiteSpace(processName)
                && BuiltInTestServerNames.Contains(processName.Trim());
        }

        public static bool IsBuiltInTestServer(ClientDTO clientDTO)
        {
            return clientDTO != null && IsBuiltInTestServerName(clientDTO.name);
        }

        public static bool IsAllowedServer(ClientDTO clientDTO, bool isLocalOverride = false)
        {
            if (clientDTO == null || string.IsNullOrWhiteSpace(clientDTO.name))
            {
                return false;
            }

            return isLocalOverride || IsExclusiveServer(clientDTO) || IsBuiltInTestServer(clientDTO);
        }

        public static string LoadResourceServerFile()
        {
            return Resources._4RTools.ETCResource.supported_servers;
        }

        private void LoadServers(List<KeyValuePair<ClientDTO, bool>> clients)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            this.pbSupportedServer.Maximum = Math.Max(1, clients.Count);
            this.pbSupportedServer.Value = 0;

            foreach (KeyValuePair<ClientDTO, bool> entry in clients)
            {
                ClientDTO clientDTO = entry.Key;
                bool isLocalOverride = entry.Value;

                try
                {
                    if (!IsAllowedServer(clientDTO, isLocalOverride))
                    {
                        continue;
                    }

                    string normalizedName = clientDTO.name.Trim();
                    if (seen.Add(normalizedName))
                    {
                        ClientListSingleton.AddClient(new Client(clientDTO));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn("ClientUpdaterForm", "Skipping invalid server entry: " + (clientDTO != null ? clientDTO.name : "null"), ex);
                }

                if (this.pbSupportedServer.Value < this.pbSupportedServer.Maximum)
                {
                    this.pbSupportedServer.Increment(1);
                }
            }
        }
    }
}
