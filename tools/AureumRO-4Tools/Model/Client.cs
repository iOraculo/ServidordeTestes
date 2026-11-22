using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using _4RTools.Utils;

namespace _4RTools.Model
{
    public class ClientDTO
    {
        public int index { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string hpAddress { get; set; }
        public string nameAddress { get; set; }
        public string mapAddress { get; set; }
        public string statusOffset { get; set; }

        public int hpAddressPointer { get; set; }
        public int nameAddressPointer { get; set; }
        public int mapAddressPointer { get; set; }
        public int statusOffsetValue { get; set; }

        public ClientDTO() { }

        public ClientDTO(string name, string description, string hpAddress, string nameAddress, string mapAddress = null, string statusOffset = null)
        {
            this.name = name;
            this.description = description;
            this.hpAddress = hpAddress;
            this.nameAddress = nameAddress;
            this.mapAddress = mapAddress;
            this.statusOffset = statusOffset;

            this.hpAddressPointer = Convert.ToInt32(hpAddress, 16);
            this.nameAddressPointer = Convert.ToInt32(nameAddress, 16);
            this.mapAddressPointer = TryParseOptionalHex(mapAddress);
            this.statusOffsetValue = TryParseOptionalHex(statusOffset);
        }

        internal static int TryParseOptionalHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
            {
                return 0;
            }

            try
            {
                return Convert.ToInt32(hex.Trim(), 16);
            }
            catch
            {
                return 0;
            }
        }
    }

    public sealed class ClientListSingleton
    {
        private static readonly object SyncRoot = new object();
        private static readonly List<Client> clients = new List<Client>();

        public static void AddClient(Client c)
        {
            if (c == null)
            {
                return;
            }

            lock (SyncRoot)
            {
                clients.Add(c);
            }
        }

        public static void RemoveClient(Client c)
        {
            if (c == null)
            {
                return;
            }

            lock (SyncRoot)
            {
                clients.Remove(c);
            }
        }

        public static List<Client> GetAll()
        {
            lock (SyncRoot)
            {
                return new List<Client>(clients);
            }
        }

        public static bool ExistsByProcessName(string processName)
        {
            lock (SyncRoot)
            {
                return clients.Exists(client => string.Equals(client.processName, processName, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    public sealed class ClientSingleton
    {
        private static readonly object SyncRoot = new object();
        private static Client client;

        private ClientSingleton(Client newClient)
        {
            Client previous = null;
            lock (SyncRoot)
            {
                if (!ReferenceEquals(client, newClient))
                {
                    previous = client;
                    client = newClient;
                }
            }

            if (previous != null)
            {
                previous.Dispose();
            }
        }

        public static ClientSingleton Instance(Client newClient)
        {
            return new ClientSingleton(newClient);
        }

        public static Client GetClient()
        {
            lock (SyncRoot)
            {
                return client;
            }
        }

        public static void Clear()
        {
            Client previous;
            lock (SyncRoot)
            {
                previous = client;
                client = null;
            }

            if (previous != null)
            {
                previous.Dispose();
            }
        }
    }

    public class Client : IDisposable
    {
        public static volatile bool AllowBuffsInTown = false;

        private static readonly HashSet<string> TownMaps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "prontera", "morocc", "geffen", "payon", "alberta", "izlude", "aldebaran",
            "xmas", "comodo", "yuno", "amatsu", "gonryun", "umbala", "niflheim",
            "louyang", "ayothaya", "einbroch", "einbech", "lighthalzen", "hugel",
            "rachel", "veins", "moscovia", "mid_camp", "manuk", "splendide", "brasilis",
            "dicastes01", "mora", "dewata", "malangdo", "malaya", "eclage", "lasagna",
            "moc_para01", "paramk", "jawaii", "iz_int", "new_1-1",
            "prt_in", "prontera_in", "prt_church", "prt_castle", "alberta_in", "alb2trea",
            "aldeba_in", "ama_in01", "ama_in02", "ayo_in01", "ayo_in02", "cmd_in01",
            "cmd_in02", "ein_in01", "gef_tower", "geffen_in", "gon_in", "hu_in01",
            "izlude_in", "lhz_in01", "lhz_in02", "lhz_in03", "lou_in01", "lou_in02",
            "morocc_in", "mosk_in", "nif_in", "payon_in01", "payon_in02", "payon_in03",
            "ra_in01", "ra_temple", "um_in", "ve_in", "ve_in02", "xmas_in",
            "yuno_in01", "yuno_in02", "yuno_in03", "yuno_in04", "yuno_in05",
            "bra_in01", "dic_in01", "dew_in01", "mal_in01", "mal_in02", "ma_in01",
            "ecl_in01", "lasagna_in"
        };

        public Process process { get; private set; }
        public string processName { get; private set; }
        private ProcessMemoryReader PMR { get; set; }
        public int currentNameAddress { get; set; }
        public int currentHPBaseAddress { get; set; }
        public int currentMapAddress { get; set; }
        private int statusBufferAddress { get; set; }
        private readonly Stopwatch mapCacheClock = Stopwatch.StartNew();
        private long lastMapReadMs = -10000;
        private string cachedMapName = string.Empty;
        public bool IsSupported { get; private set; }
        private bool disposed = false;

        public bool HasMemorySupport
        {
            get
            {
                return !this.disposed
                    && this.currentHPBaseAddress != 0
                    && this.PMR != null
                    && this.PMR.IsOpen;
            }
        }

        public const int DEFAULT_STATUS_BUFFER_OFFSET = 0x474;

        public Client(string processName, int currentHPBaseAddress, int currentNameAddress)
        {
            this.currentNameAddress = currentNameAddress;
            this.currentHPBaseAddress = currentHPBaseAddress;
            this.processName = processName;
            this.statusBufferAddress = currentHPBaseAddress + DEFAULT_STATUS_BUFFER_OFFSET;
            this.IsSupported = currentHPBaseAddress != 0;
        }

        public Client(ClientDTO dto)
        {
            this.processName = dto.name;
            this.currentHPBaseAddress = Convert.ToInt32(dto.hpAddress, 16);
            this.currentNameAddress = Convert.ToInt32(dto.nameAddress, 16);
            this.currentMapAddress = ClientDTO.TryParseOptionalHex(dto.mapAddress);
            int customStatusOffset = ClientDTO.TryParseOptionalHex(dto.statusOffset);
            this.statusBufferAddress = this.currentHPBaseAddress + (customStatusOffset > 0 ? customStatusOffset : DEFAULT_STATUS_BUFFER_OFFSET);
            this.IsSupported = this.currentHPBaseAddress != 0;
        }

        public Client(string processDisplayName)
        {
            this.PMR = new ProcessMemoryReader();
            this.IsSupported = false;

            if (string.IsNullOrWhiteSpace(processDisplayName))
            {
                return;
            }

            const string separator = ".exe - ";
            int sepIndex = processDisplayName.LastIndexOf(separator, StringComparison.OrdinalIgnoreCase);
            if (sepIndex <= 0)
            {
                Logger.Warn("Client", "Unexpected process entry format: " + processDisplayName);
                return;
            }

            string rawProcessName = processDisplayName.Substring(0, sepIndex);
            string pidText = processDisplayName.Substring(sepIndex + separator.Length);
            int chosenPID;
            if (!int.TryParse(pidText, out chosenPID))
            {
                Logger.Warn("Client", "Could not parse PID from process entry: " + processDisplayName);
                return;
            }

            this.processName = rawProcessName;
            Process[] matchingProcesses = Process.GetProcessesByName(rawProcessName);

            foreach (Process candidate in matchingProcesses)
            {
                if (chosenPID == candidate.Id && this.process == null)
                {
                    this.process = candidate;
                    this.PMR.ReadProcess = candidate;
                    this.PMR.OpenProcess();

                    Client matchedTemplate = GetClientByProcess(rawProcessName);
                    if (matchedTemplate != null)
                    {
                        this.currentHPBaseAddress = matchedTemplate.currentHPBaseAddress;
                        this.currentNameAddress = matchedTemplate.currentNameAddress;
                        this.currentMapAddress = matchedTemplate.currentMapAddress;
                        this.statusBufferAddress = matchedTemplate.statusBufferAddress;
                        this.IsSupported = true;
                    }
                    else
                    {
                        // Do not block spammers/macros for non-supported versions.
                        this.currentHPBaseAddress = 0;
                        this.currentNameAddress = 0;
                        this.statusBufferAddress = 0;
                        this.IsSupported = false;
                    }
                }
                else
                {
                    candidate.Dispose();
                }
            }
        }

        public bool IsProcessAlive()
        {
            if (this.disposed || this.process == null)
            {
                return false;
            }

            try
            {
                return !this.process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        public IntPtr GetWindowHandle()
        {
            if (!IsProcessAlive())
            {
                return IntPtr.Zero;
            }

            try
            {
                return this.process.MainWindowHandle;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        public bool IsGameWindowFocused()
        {
            IntPtr hWnd = GetWindowHandle();
            if (hWnd == IntPtr.Zero)
            {
                return false;
            }

            return Interop.GetForegroundWindow() == hWnd;
        }

        public bool ShouldPauseForFocus()
        {
            Profile currentProfile = ProfileSingleton.GetCurrent();
            if (currentProfile != null && currentProfile.UserPreferences != null && currentProfile.UserPreferences.pauseWhenUnfocused)
            {
                return !IsGameWindowFocused();
            }

            return false;
        }

        public static string NormalizeMapName(string rawMap)
        {
            if (string.IsNullOrWhiteSpace(rawMap))
            {
                return string.Empty;
            }

            string normalized = rawMap.Trim().ToLowerInvariant();
            if (normalized.EndsWith(".rsw", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(0, normalized.Length - 4);
            }
            else if (normalized.EndsWith(".gat", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(0, normalized.Length - 4);
            }

            return normalized;
        }

        public static bool IsTownMap(string mapName)
        {
            string normalized = NormalizeMapName(mapName);
            if (string.IsNullOrEmpty(normalized))
            {
                return false;
            }

            return TownMaps.Contains(normalized);
        }

        public string ReadCurrentMap()
        {
            if (this.currentMapAddress == 0 || !HasMemorySupport)
            {
                return string.Empty;
            }

            long now = this.mapCacheClock.ElapsedMilliseconds;
            if (now - this.lastMapReadMs < 500)
            {
                return this.cachedMapName;
            }

            this.cachedMapName = NormalizeMapName(ReadMemoryAsString(this.currentMapAddress));
            this.lastMapReadMs = now;
            return this.cachedMapName;
        }

        public bool IsInTown()
        {
            return this.currentMapAddress != 0 && IsTownMap(ReadCurrentMap());
        }

        public bool ShouldPauseForTown()
        {
            return !AllowBuffsInTown && IsInTown();
        }

        public bool ShouldPauseAutomation()
        {
            return ShouldPauseForFocus() || ChatPauseState.IsChatting || ShouldPauseForTown();
        }

        public bool ShouldPauseAutopotKey(System.Windows.Input.Key key)
        {
            if (ShouldPauseForFocus() || ShouldPauseForTown())
            {
                return true;
            }

            if (ChatPauseState.IsChatting && ChatPauseState.IsTypingKey(key))
            {
                return true;
            }

            return false;
        }

        private string ReadMemoryAsString(int address)
        {
            if (this.PMR == null || !this.PMR.IsOpen || address == 0)
            {
                return string.Empty;
            }

            byte[] bytes = new byte[40];
            int bytesRead;
            if (!this.PMR.ReadProcessMemory((IntPtr)address, bytes, bytes.Length, out bytesRead))
            {
                return string.Empty;
            }

            int length = 0;
            while (length < bytesRead && bytes[length] != 0)
            {
                length++;
            }

            return length > 0 ? Encoding.Default.GetString(bytes, 0, length) : string.Empty;
        }

        private uint ReadMemory(int address)
        {
            if (this.PMR == null || !this.PMR.IsOpen || address == 0)
            {
                return 0;
            }

            byte[] buffer = new byte[4];
            int bytesRead;
            if (!this.PMR.ReadProcessMemory((IntPtr)address, buffer, 4, out bytesRead) || bytesRead < 4)
            {
                return 0;
            }

            return BitConverter.ToUInt32(buffer, 0);
        }

        public bool TryReadVitals(byte[] scratchBuffer16, out uint currentHp, out uint maxHp, out uint currentSp, out uint maxSp)
        {
            currentHp = 0;
            maxHp = 0;
            currentSp = 0;
            maxSp = 0;

            if (!HasMemorySupport || scratchBuffer16 == null || scratchBuffer16.Length < Constants.HP_SP_BLOCK_BYTE_SIZE)
            {
                return false;
            }

            int bytesRead;
            if (!this.PMR.ReadProcessMemory((IntPtr)this.currentHPBaseAddress, scratchBuffer16, Constants.HP_SP_BLOCK_BYTE_SIZE, out bytesRead)
                || bytesRead < Constants.HP_SP_BLOCK_BYTE_SIZE)
            {
                return false;
            }

            currentHp = BitConverter.ToUInt32(scratchBuffer16, 0);
            maxHp = BitConverter.ToUInt32(scratchBuffer16, 4);
            currentSp = BitConverter.ToUInt32(scratchBuffer16, 8);
            maxSp = BitConverter.ToUInt32(scratchBuffer16, 12);
            return true;
        }

        public int ReadBuffStatusBuffer(byte[] rawBuffer, uint[] destination)
        {
            if (!HasMemorySupport || this.statusBufferAddress == 0 || rawBuffer == null || destination == null)
            {
                return 0;
            }

            int count = Math.Min(destination.Length, Constants.MAX_BUFF_LIST_INDEX_SIZE);
            int byteSize = count * sizeof(uint);
            if (rawBuffer.Length < byteSize)
            {
                return 0;
            }

            int bytesRead;
            if (!this.PMR.ReadProcessMemory((IntPtr)this.statusBufferAddress, rawBuffer, byteSize, out bytesRead) || bytesRead < sizeof(uint))
            {
                return 0;
            }

            int entriesRead = bytesRead / sizeof(uint);
            Buffer.BlockCopy(rawBuffer, 0, destination, 0, entriesRead * sizeof(uint));
            return entriesRead;
        }

        private const uint MAX_VALID_EFST_CODE = 100000u;

        private static bool IsIgnoreStatusCode(uint code)
        {
            return code == (uint)EffectStatusIDs.IGNORE || code == (uint)EffectStatusIDs.IGNORE_2;
        }

        private static bool IsNonEfstSentinel(uint code)
        {
            return !IsIgnoreStatusCode(code) && code > MAX_VALID_EFST_CODE;
        }

        /// <summary>
        /// Some 2022+ Ragexe clients place the status buffer 12 bytes (3 uint32 slots) later
        /// (+0x480 instead of +0x474). When read from +0x474, slot 0 holds a CSession field
        /// that collides with ANGELUS (9), slot 1 holds a large non-EFST value (e.g. 1004240168),
        /// and slot 2 holds 1 (colliding with ENDURE). Detecting the non-EFST sentinel at slot 1
        /// lets us skip the 3-slot header cleanly on any such client.
        /// </summary>
        public static int DetectLeadingHeaderSkip(uint[] statusBuffer, int limit)
        {
            if (statusBuffer == null || limit < 4)
            {
                return 0;
            }

            return IsNonEfstSentinel(statusBuffer[1]) ? 3 : 0;
        }

        /// <summary>
        /// Extracts active EFST codes from a raw status buffer while skipping any 3-slot leading
        /// CSession header and stopping after a run of consecutive IGNORE (0xFFFFFFFF) slots.
        /// This tolerates small gaps in active buff lists without scanning past the end of the
        /// client's status array into unrelated CSession memory.
        /// </summary>
        public static int ExtractActiveStatuses(uint[] statusBuffer, int count, List<uint> activeOutput)
        {
            if (statusBuffer == null || activeOutput == null || count <= 0)
            {
                return 0;
            }

            int limit = Math.Min(count, statusBuffer.Length);
            int startIndex = DetectLeadingHeaderSkip(statusBuffer, limit);
            int consecutiveIgnores = 0;
            int added = 0;

            for (int i = startIndex; i < limit; i++)
            {
                uint code = statusBuffer[i];
                if (IsIgnoreStatusCode(code))
                {
                    consecutiveIgnores++;
                    if (consecutiveIgnores >= Constants.STATUS_END_CONSECUTIVE_IGNORES)
                    {
                        break;
                    }
                    continue;
                }

                if (IsNonEfstSentinel(code))
                {
                    continue;
                }

                consecutiveIgnores = 0;
                activeOutput.Add(code);
                added++;
            }

            return added;
        }

        public int CountActiveBuffs(byte[] rawBuffer, uint[] destination)
        {
            int count = ReadBuffStatusBuffer(rawBuffer, destination);
            if (count <= 0)
            {
                return 0;
            }

            List<uint> activeStatuses = new List<uint>(32);
            return ExtractActiveStatuses(destination, count, activeStatuses);
        }

        public string BuildActiveStatusReport()
        {
            if (!HasMemorySupport || !IsProcessAlive())
            {
                return LocalizationManager.T("msg.select_client_first");
            }

            byte[] rawBuffer = new byte[Constants.STATUS_BUFFER_BYTE_SIZE];
            uint[] statusBuffer = new uint[Constants.MAX_BUFF_LIST_INDEX_SIZE];
            int count = ReadBuffStatusBuffer(rawBuffer, statusBuffer);
            int startIndex = DetectLeadingHeaderSkip(statusBuffer, count);

            StringBuilder sb = new StringBuilder();
            string charName = ReadCharacterName();
            string mapName = ReadCurrentMap();
            sb.AppendFormat(
                LocalizationManager.IsPortuguese
                    ? "Personagem: {0}  |  Mapa: {1}"
                    : "Character: {0}  |  Map: {1}",
                string.IsNullOrWhiteSpace(charName) ? "-" : charName.Trim(),
                string.IsNullOrWhiteSpace(mapName) ? "-" : mapName);
            sb.AppendLine();
            sb.AppendLine(new string('-', 44));

            int found = 0;
            int consecutiveIgnores = 0;
            int stopIndex = count;
            for (int i = startIndex; i < count; i++)
            {
                uint code = statusBuffer[i];
                if (IsIgnoreStatusCode(code))
                {
                    consecutiveIgnores++;
                    if (consecutiveIgnores >= Constants.STATUS_END_CONSECUTIVE_IGNORES)
                    {
                        stopIndex = i + 1;
                        break;
                    }
                    continue;
                }

                if (IsNonEfstSentinel(code))
                {
                    continue;
                }

                consecutiveIgnores = 0;
                string enumName = Enum.IsDefined(typeof(EffectStatusIDs), code)
                    ? ((EffectStatusIDs)code).ToString()
                    : (LocalizationManager.IsPortuguese ? "(desconhecido)" : "(unknown)");

                sb.AppendFormat("Slot {0:00}:  ID {1}  —  {2}", i - startIndex, code, enumName);
                sb.AppendLine();
                found++;
            }

            if (found == 0)
            {
                sb.AppendLine(LocalizationManager.T("status_inspect.empty"));
            }
            else
            {
                sb.AppendLine(new string('-', 44));
                sb.AppendFormat(LocalizationManager.T("status_inspect.total"), found);
                sb.AppendLine();
            }

            List<string> trailingLines = new List<string>();
            for (int i = stopIndex; i < count; i++)
            {
                uint code = statusBuffer[i];
                if (code == (uint)EffectStatusIDs.IGNORE || code == (uint)EffectStatusIDs.IGNORE_2)
                {
                    continue;
                }

                string enumName = Enum.IsDefined(typeof(EffectStatusIDs), code)
                    ? ((EffectStatusIDs)code).ToString()
                    : "-";
                trailingLines.Add(string.Format("Slot {0:00}:  ID {1}  ({2})", i, code, enumName));
            }

            if (trailingLines.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(LocalizationManager.IsPortuguese
                    ? "[Ignorado — após fim do buffer de status]"
                    : "[Ignored — past status buffer end]");
                for (int i = 0; i < Math.Min(trailingLines.Count, 12); i++)
                {
                    sb.AppendLine(trailingLines[i]);
                }
            }

            return sb.ToString().TrimEnd();
        }

        public static bool IsBelowPercent(uint current, uint max, int percent)
        {
            if (max == 0 || percent <= 0)
            {
                return false;
            }

            return (ulong)current * 100UL < (ulong)percent * (ulong)max;
        }

        public bool IsHpBelow(int percent)
        {
            return IsBelowPercent(ReadCurrentHp(), ReadMaxHp(), percent);
        }

        public bool IsSpBelow(int percent)
        {
            return IsBelowPercent(ReadCurrentSp(), ReadMaxSp(), percent);
        }

        public uint ReadCurrentHp()
        {
            return ReadMemory(this.currentHPBaseAddress);
        }

        public uint ReadCurrentSp()
        {
            return ReadMemory(this.currentHPBaseAddress + 8);
        }

        public uint ReadMaxHp()
        {
            return ReadMemory(this.currentHPBaseAddress + 4);
        }

        public string ReadCharacterName()
        {
            return ReadMemoryAsString(this.currentNameAddress);
        }

        public uint ReadMaxSp()
        {
            return ReadMemory(this.currentHPBaseAddress + 12);
        }

        public uint CurrentBuffStatusCode(int effectStatusIndex)
        {
            if (this.statusBufferAddress == 0)
            {
                return 0;
            }

            return ReadMemory(this.statusBufferAddress + effectStatusIndex * 4);
        }

        public Client GetClientByProcess(string procName)
        {
            Client fallbackMatch = null;
            foreach (Client c in ClientListSingleton.GetAll())
            {
                if (string.Equals(c.processName, procName, StringComparison.OrdinalIgnoreCase))
                {
                    if (fallbackMatch == null)
                    {
                        fallbackMatch = c;
                    }

                    uint hpBaseValue = ReadMemory(c.currentHPBaseAddress);
                    if (hpBaseValue > 0)
                    {
                        return c;
                    }
                }
            }

            // Allow selecting the client even on login/character-select screen (when HP == 0).
            return fallbackMatch;
        }

        public static Client FromDTO(ClientDTO dto)
        {
            if (dto == null)
            {
                return null;
            }

            int hpPtr = dto.hpAddressPointer != 0 ? dto.hpAddressPointer : Convert.ToInt32(dto.hpAddress, 16);
            int namePtr = dto.nameAddressPointer != 0 ? dto.nameAddressPointer : Convert.ToInt32(dto.nameAddress, 16);

            return ClientListSingleton.GetAll()
                .Where(c => string.Equals(c.processName, dto.name, StringComparison.OrdinalIgnoreCase))
                .Where(c => c.currentHPBaseAddress == hpPtr)
                .Where(c => c.currentNameAddress == namePtr)
                .FirstOrDefault();
        }

        public static ServerAddressTestResult TestMemoryAddresses(string processName, string rawHpAddress, string rawNameAddress, string rawMapAddress = null)
        {
            ServerAddressTestResult result = new ServerAddressTestResult();

            if (string.IsNullOrWhiteSpace(processName))
            {
                result.Message = LocalizationManager.T("test.empty_process");
                return result;
            }

            string normalizedHp = LocalServerManager.NormalizeHexAddress(rawHpAddress);
            if (normalizedHp == null)
            {
                result.Message = LocalizationManager.T("test.invalid_hp");
                return result;
            }

            string normalizedName = LocalServerManager.NormalizeHexAddress(rawNameAddress);
            if (normalizedName == null)
            {
                result.Message = LocalizationManager.T("test.invalid_name");
                return result;
            }

            int mapAddress = ClientDTO.TryParseOptionalHex(rawMapAddress);

            string cleanProcessName = processName.Trim();
            if (cleanProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                cleanProcessName = cleanProcessName.Substring(0, cleanProcessName.Length - 4);
            }

            int hpAddress = Convert.ToInt32(normalizedHp, 16);
            int nameAddress = Convert.ToInt32(normalizedName, 16);

            Process[] matching = Process.GetProcessesByName(cleanProcessName);
            if (matching.Length == 0)
            {
                result.Message = LocalizationManager.T("test.process_not_running", cleanProcessName);
                return result;
            }

            Process target = null;
            try
            {
                for (int i = 0; i < matching.Length; i++)
                {
                    if (matching[i].MainWindowHandle != IntPtr.Zero)
                    {
                        target = matching[i];
                        break;
                    }
                }

                if (target == null)
                {
                    target = matching[0];
                }

                using (ProcessMemoryReader reader = new ProcessMemoryReader())
                {
                    reader.ReadProcess = target;
                    if (!reader.OpenProcess())
                    {
                        result.Message = LocalizationManager.T("test.open_failed", cleanProcessName);
                        return result;
                    }

                    byte[] vitalsBytes = new byte[Constants.HP_SP_BLOCK_BYTE_SIZE];
                    int bytesRead;
                    if (reader.ReadProcessMemory((IntPtr)hpAddress, vitalsBytes, vitalsBytes.Length, out bytesRead)
                        && bytesRead >= Constants.HP_SP_BLOCK_BYTE_SIZE)
                    {
                        result.CurrentHp = BitConverter.ToUInt32(vitalsBytes, 0);
                        result.MaxHp = BitConverter.ToUInt32(vitalsBytes, 4);
                        result.CurrentSp = BitConverter.ToUInt32(vitalsBytes, 8);
                        result.MaxSp = BitConverter.ToUInt32(vitalsBytes, 12);
                    }

                    byte[] nameBytes = new byte[40];
                    if (reader.ReadProcessMemory((IntPtr)nameAddress, nameBytes, nameBytes.Length, out bytesRead) && bytesRead > 0)
                    {
                        int length = 0;
                        while (length < bytesRead && nameBytes[length] != 0)
                        {
                            length++;
                        }

                        result.CharacterName = length > 0 ? Encoding.Default.GetString(nameBytes, 0, length).Trim() : string.Empty;
                    }

                    if (mapAddress != 0)
                    {
                        byte[] mapBytes = new byte[40];
                        if (reader.ReadProcessMemory((IntPtr)mapAddress, mapBytes, mapBytes.Length, out bytesRead) && bytesRead > 0)
                        {
                            int length = 0;
                            while (length < bytesRead && mapBytes[length] != 0)
                            {
                                length++;
                            }

                            string rawMap = length > 0 ? Encoding.Default.GetString(mapBytes, 0, length).Trim() : string.Empty;
                            result.CurrentMap = NormalizeMapName(rawMap);
                            result.IsInTown = IsTownMap(result.CurrentMap);
                        }
                    }

                    byte[] statusRaw = new byte[Constants.STATUS_BUFFER_BYTE_SIZE];
                    if (reader.ReadProcessMemory((IntPtr)(hpAddress + 0x474), statusRaw, statusRaw.Length, out bytesRead)
                        && bytesRead >= sizeof(uint))
                    {
                        int entries = bytesRead / sizeof(uint);
                        int activeCount = 0;
                        for (int i = 0; i < entries; i++)
                        {
                            uint code = BitConverter.ToUInt32(statusRaw, i * sizeof(uint));
                            if (code != 0 && code != (uint)EffectStatusIDs.IGNORE && code != (uint)EffectStatusIDs.IGNORE_2)
                            {
                                activeCount++;
                            }
                        }
                        result.ActiveBuffCount = activeCount;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Client", "Failed testing memory addresses for '" + cleanProcessName + "'", ex);
                result.Message = LocalizationManager.T("test.read_error", ex.Message);
                return result;
            }
            finally
            {
                for (int i = 0; i < matching.Length; i++)
                {
                    matching[i].Dispose();
                }
            }

            bool hpLooksValid = result.MaxHp > 0 && result.MaxHp < 100000000u && result.CurrentHp <= result.MaxHp;
            bool nameLooksValid = !string.IsNullOrWhiteSpace(result.CharacterName);

            result.Success = hpLooksValid || nameLooksValid;
            if (result.Success)
            {
                result.Message = LocalizationManager.T(
                    "test.ok_report",
                    nameLooksValid ? result.CharacterName : LocalizationManager.T("test.not_detected"),
                    result.CurrentHp,
                    result.MaxHp,
                    result.CurrentSp,
                    result.MaxSp,
                    result.ActiveBuffCount);
                if (!string.IsNullOrWhiteSpace(result.CurrentMap))
                {
                    result.Message += Environment.NewLine + "Map: " + result.CurrentMap + (result.IsInTown ? " (Town)" : " (Field)");
                }
            }
            else
            {
                result.Message = LocalizationManager.T("test.empty_values", result.CurrentHp, result.MaxHp);
            }

            return result;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;

            if (disposing)
            {
                if (this.PMR != null)
                {
                    this.PMR.Dispose();
                    this.PMR = null;
                }

                if (this.process != null)
                {
                    this.process.Dispose();
                    this.process = null;
                }
            }
        }
    }

    public class ServerAddressTestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public uint CurrentHp { get; set; }
        public uint MaxHp { get; set; }
        public uint CurrentSp { get; set; }
        public uint MaxSp { get; set; }
        public string CharacterName { get; set; } = string.Empty;
        public string CurrentMap { get; set; } = string.Empty;
        public bool IsInTown { get; set; }
        public int ActiveBuffCount { get; set; }
    }
}
