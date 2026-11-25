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
        private ClientSingleton(Client newClient)
        {
            SessionContext.Current.SetClient(newClient);
        }

        public static ClientSingleton Instance(Client newClient)
        {
            return new ClientSingleton(newClient);
        }

        public static Client GetClient()
        {
            return SessionContext.Current.Client;
        }

        public static void Clear()
        {
            SessionContext.Current.SetClient(null);
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

        private static readonly string[] KnownMapPrefixes = new string[]
        {
            "prt_", "moc_", "gef_", "pay_", "alde_", "alb_", "iz_", "lhz_",
            "ra_", "ve_", "ein_", "hu_", "yuno_", "cmd_", "xmas_", "ama_",
            "gon_", "lou_", "ayo_", "um_", "nif_", "mosk_", "bra_", "dic_",
            "man_", "spl_", "gl_", "mjo_", "mag_", "c_tower", "tur_", "abyss_",
            "odin_", "thor_", "abbey", "ice_", "juperos_", "tha_", "kh_",
            "orcsdun", "anthell", "beach_", "in_sphinx", "guild_", "pvp_",
            "aldeg_", "gefg_", "payg_", "prtg_", "arug_", "schg_", "sec_",
            "nameless_", "nyd_", "que_", "job_", "new_", "bat_", "quiz_",
            "turbo_", "mal_", "ecl_", "dew_", "ma_", "lasagna", "paramk"
        };

        private static readonly Lazy<HashSet<string>> KnownMapNames = new Lazy<HashSet<string>>(BuildKnownMapNames);

        private static HashSet<string> BuildKnownMapNames()
        {
            HashSet<string> set = new HashSet<string>(TownMaps, StringComparer.OrdinalIgnoreCase);
            try
            {
                AureumCatalogData catalog = AureumCatalog.Load();
                if (catalog != null && catalog.MapCollections != null)
                {
                    foreach (AureumMapEntry entry in catalog.MapCollections)
                    {
                        if (entry != null && !string.IsNullOrWhiteSpace(entry.MapCode))
                        {
                            set.Add(NormalizeMapName(entry.MapCode));
                        }
                    }
                }
            }
            catch
            {
                // Catalog is optional; TownMaps + KnownMapPrefixes still provide full coverage.
            }

            return set;
        }

        public const int DEFAULT_STATUS_BUFFER_OFFSET = 0x474;
        public const int RAGEXE_2022_STATUS_BUFFER_OFFSET = 0x480;

        public Process process { get; private set; }
        public string processName { get; private set; }
        private ProcessMemoryReader PMR { get; set; }
        public int currentNameAddress { get; set; }
        public int currentHPBaseAddress { get; set; }
        public int currentMapAddress { get; set; }
        private int statusBufferAddress { get; set; }
        public int StatusBufferAddress { get { return this.statusBufferAddress; } }
        private readonly Stopwatch mapCacheClock = Stopwatch.StartNew();
        private long lastMapReadMs = -10000;
        private long lastMapScanMs = -10000;
        private string cachedMapName = string.Empty;
        private bool mapAddressAutoDetected = false;
        private readonly object mapDetectLock = new object();
        private readonly List<KeyValuePair<int, string>> detectedMapCandidates = new List<KeyValuePair<int, string>>();
        private readonly object processCacheLock = new object();
        private long lastProcessProbeMs = -10000;
        private bool cachedProcessAlive = false;
        private IntPtr cachedWindowHandle = IntPtr.Zero;
        public bool IsSupported { get; private set; }
        private volatile bool disposed = false;

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

        public Client(string processName, int currentHPBaseAddress, int currentNameAddress, int statusOffset = DEFAULT_STATUS_BUFFER_OFFSET)
        {
            this.currentNameAddress = currentNameAddress;
            this.currentHPBaseAddress = currentHPBaseAddress;
            this.processName = processName;
            int resolvedOffset = statusOffset > 0 ? statusOffset : DEFAULT_STATUS_BUFFER_OFFSET;
            this.statusBufferAddress = currentHPBaseAddress + resolvedOffset;
            this.IsSupported = currentHPBaseAddress != 0;
        }

        public Client(ClientDTO dto)
        {
            this.processName = dto.name;
            this.currentHPBaseAddress = Convert.ToInt32(dto.hpAddress, 16);
            this.currentNameAddress = Convert.ToInt32(dto.nameAddress, 16);
            this.currentMapAddress = ClientDTO.TryParseOptionalHex(dto.mapAddress);
            int parsedStatusOffset = dto.statusOffsetValue > 0
                ? dto.statusOffsetValue
                : ClientDTO.TryParseOptionalHex(dto.statusOffset);
            int resolvedOffset = parsedStatusOffset > 0 ? parsedStatusOffset : DEFAULT_STATUS_BUFFER_OFFSET;
            this.statusBufferAddress = this.currentHPBaseAddress + resolvedOffset;
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

        private void RefreshProcessCacheIfNeeded(bool requireWindowHandle)
        {
            long now = this.mapCacheClock.ElapsedMilliseconds;
            lock (this.processCacheLock)
            {
                if (this.disposed || this.process == null)
                {
                    this.cachedProcessAlive = false;
                    this.cachedWindowHandle = IntPtr.Zero;
                    return;
                }

                bool withinWindow = (now - this.lastProcessProbeMs) < 250;
                if (withinWindow && (!requireWindowHandle || this.cachedWindowHandle != IntPtr.Zero || !this.cachedProcessAlive))
                {
                    return;
                }

                try
                {
                    bool alive = !this.process.HasExited;
                    this.cachedProcessAlive = alive;
                    this.cachedWindowHandle = alive ? this.process.MainWindowHandle : IntPtr.Zero;
                }
                catch
                {
                    this.cachedProcessAlive = false;
                    this.cachedWindowHandle = IntPtr.Zero;
                }

                this.lastProcessProbeMs = now;
            }
        }

        public bool IsProcessAlive()
        {
            if (this.disposed || this.process == null)
            {
                return false;
            }

            RefreshProcessCacheIfNeeded(false);
            return this.cachedProcessAlive;
        }

        public IntPtr GetWindowHandle()
        {
            if (this.disposed || this.process == null)
            {
                return IntPtr.Zero;
            }

            RefreshProcessCacheIfNeeded(true);
            return this.cachedProcessAlive ? this.cachedWindowHandle : IntPtr.Zero;
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

        public static bool IsLikelyRagnarokMapName(string rawMap)
        {
            string normalized = NormalizeMapName(rawMap);
            if (normalized.Length < 3 || normalized.Length > 24)
            {
                return false;
            }

            for (int i = 0; i < normalized.Length; i++)
            {
                char ch = normalized[i];
                bool valid = (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_' || ch == '-';
                if (!valid)
                {
                    return false;
                }
            }

            if (KnownMapNames.Value.Contains(normalized))
            {
                return true;
            }

            for (int i = 0; i < KnownMapPrefixes.Length; i++)
            {
                if (normalized.StartsWith(KnownMapPrefixes[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsRawMapFileString(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.Length < 7 || raw.Length > 28)
            {
                return false;
            }

            string trimmed = raw.Trim();
            bool hasMapExt = trimmed.EndsWith(".rsw", StringComparison.OrdinalIgnoreCase)
                || trimmed.EndsWith(".gat", StringComparison.OrdinalIgnoreCase);
            if (!hasMapExt)
            {
                return false;
            }

            return IsLikelyRagnarokMapName(trimmed);
        }

        private static bool IsDisallowedPrecedingMapByte(byte before)
        {
            // Reject alphanumeric/identifier chars (name exceeded max length) and ASCII path/text/quote delimiters
            // (e.g. "data\prontera.rsw", "load prontera.rsw", "map=prontera.rsw"), while allowing null (0x00)
            // and arbitrary binary C++ struct field bytes (integers, floats, pointers).
            if ((before >= (byte)'a' && before <= (byte)'z')
                || (before >= (byte)'A' && before <= (byte)'Z')
                || (before >= (byte)'0' && before <= (byte)'9')
                || before == (byte)'_'
                || before == (byte)'-')
            {
                return true;
            }

            switch (before)
            {
                case (byte)'\\':
                case (byte)'/':
                case (byte)':':
                case (byte)'.':
                case (byte)' ':
                case (byte)'\t':
                case (byte)'"':
                case (byte)'\'':
                case (byte)'=':
                case (byte)',':
                case (byte)';':
                case (byte)'(':
                case (byte)'[':
                case (byte)'{':
                case (byte)'|':
                    return true;
                default:
                    return false;
            }
        }

        public static List<int> ScanBufferForMapOffsets(byte[] buffer, int validLength)
        {
            List<int> offsets = new List<int>();
            if (buffer == null || validLength < 8)
            {
                return offsets;
            }

            int limit = Math.Min(validLength, buffer.Length);
            for (int i = 3; i <= limit - 5; i++)
            {
                if (buffer[i] != (byte)'.')
                {
                    continue;
                }

                byte b1 = buffer[i + 1];
                byte b2 = buffer[i + 2];
                byte b3 = buffer[i + 3];
                byte b4 = buffer[i + 4];
                bool isRsw = (b1 == (byte)'r' || b1 == (byte)'R')
                    && (b2 == (byte)'s' || b2 == (byte)'S')
                    && (b3 == (byte)'w' || b3 == (byte)'W')
                    && b4 == 0;
                bool isGat = (b1 == (byte)'g' || b1 == (byte)'G')
                    && (b2 == (byte)'a' || b2 == (byte)'A')
                    && (b3 == (byte)'t' || b3 == (byte)'T')
                    && b4 == 0;
                if (!isRsw && !isGat)
                {
                    continue;
                }

                int start = i;
                int minStart = Math.Max(0, i - 24);
                while (start > minStart)
                {
                    byte prev = buffer[start - 1];
                    bool validChar = (prev >= (byte)'a' && prev <= (byte)'z')
                        || (prev >= (byte)'A' && prev <= (byte)'Z')
                        || (prev >= (byte)'0' && prev <= (byte)'9')
                        || prev == (byte)'_'
                        || prev == (byte)'-';
                    if (!validChar)
                    {
                        break;
                    }
                    start--;
                }

                int nameLen = i - start;
                if (nameLen < 3 || nameLen > 24)
                {
                    continue;
                }

                if (start > 0 && IsDisallowedPrecedingMapByte(buffer[start - 1]))
                {
                    continue;
                }

                string bareName = Encoding.ASCII.GetString(buffer, start, nameLen).ToLowerInvariant();
                if (IsLikelyRagnarokMapName(bareName))
                {
                    offsets.Add(start);
                }
            }

            return offsets;
        }

        /// <summary>
        /// Scans a small memory buffer near CSession (hpAddress/nameAddress) for null-delimited bare RO map names
        /// (e.g. "\0prontera\0" or "\0prt_fild08\0") matching the exact known town/catalog map set.
        /// </summary>
        public static List<int> ScanBufferForBareMapOffsets(byte[] buffer, int validLength)
        {
            List<int> offsets = new List<int>();
            if (buffer == null || validLength < 6)
            {
                return offsets;
            }

            int limit = Math.Min(validLength, buffer.Length);
            int i = 0;
            while (i < limit)
            {
                if (buffer[i] != 0)
                {
                    i++;
                    continue;
                }

                int start = i + 1;
                int pos = start;
                int maxEnd = Math.Min(limit, start + 20);
                while (pos < maxEnd)
                {
                    byte b = buffer[pos];
                    bool validChar = (b >= (byte)'a' && b <= (byte)'z')
                        || (b >= (byte)'A' && b <= (byte)'Z')
                        || (b >= (byte)'0' && b <= (byte)'9')
                        || b == (byte)'_'
                        || b == (byte)'-';
                    if (!validChar)
                    {
                        break;
                    }
                    pos++;
                }

                int len = pos - start;
                if (len >= 4 && len <= 20 && pos < limit && buffer[pos] == 0)
                {
                    string candidate = Encoding.ASCII.GetString(buffer, start, len).ToLowerInvariant();
                    if (KnownMapNames.Value.Contains(candidate))
                    {
                        offsets.Add(start);
                    }
                }

                i = pos > i ? pos : i + 1;
            }

            return offsets;
        }

        /// <summary>
        /// Filters out candidates that belong to static/Lua map lists in memory (clusters where 3+ distinct
        /// map names sit within +/-2048 bytes of each other), keeping isolated live CSession/CModeMgr slots.
        /// </summary>
        public static List<KeyValuePair<int, string>> FilterClusteredMapTableCandidates(List<KeyValuePair<int, string>> candidates)
        {
            List<KeyValuePair<int, string>> filtered = new List<KeyValuePair<int, string>>();
            if (candidates == null || candidates.Count == 0)
            {
                return filtered;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                int addr = candidates[i].Key;
                HashSet<string> nearbyDistinctMaps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                nearbyDistinctMaps.Add(candidates[i].Value);

                for (int j = 0; j < candidates.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    if (Math.Abs((long)candidates[j].Key - addr) <= 2048L)
                    {
                        nearbyDistinctMaps.Add(candidates[j].Value);
                        if (nearbyDistinctMaps.Count >= 3)
                        {
                            break;
                        }
                    }
                }

                if (nearbyDistinctMaps.Count < 3)
                {
                    filtered.Add(candidates[i]);
                }
            }

            return filtered;
        }

        private List<KeyValuePair<int, string>> ScanLiveMapCandidates(
            long minAddr,
            long maxAddr,
            long maxBytesToRead,
            long maxDurationMs,
            bool includeBareNearHp,
            out int regionsScanned,
            out int rawMatchesTotal,
            out List<KeyValuePair<int, string>> bareNearHpCandidates)
        {
            regionsScanned = 0;
            rawMatchesTotal = 0;
            bareNearHpCandidates = new List<KeyValuePair<int, string>>();
            List<KeyValuePair<int, string>> isolatedAll = new List<KeyValuePair<int, string>>();

            if (this.PMR == null || !this.PMR.IsOpen || this.currentHPBaseAddress <= 0)
            {
                return isolatedAll;
            }

            long hpAddr = this.currentHPBaseAddress;
            List<ProcessMemoryReader.MemoryRegion> regions = this.PMR.EnumerateWritableRegions(minAddr, maxAddr);
            if (regions.Count == 0)
            {
                long fallbackMin = Math.Max(0x00400000L, hpAddr - 0x40000L);
                long fallbackMax = hpAddr + 0x40000L;
                regions.Add(new ProcessMemoryReader.MemoryRegion
                {
                    BaseAddress = (IntPtr)fallbackMin,
                    RegionSize = (int)(fallbackMax - fallbackMin)
                });
            }

            // Prefer writable regions closest to hpAddress (where Ragexe static CSession / CModeMgr live) before heap.
            regions.Sort((a, b) =>
            {
                long distA = Math.Abs(a.BaseAddress.ToInt64() - hpAddr);
                long distB = Math.Abs(b.BaseAddress.ToInt64() - hpAddr);
                return distA.CompareTo(distB);
            });

            const int chunkSize = 65536;
            const int overlap = 32;
            const int maxIsolatedTotal = 64;
            const int maxRawPerRegion = 512;
            byte[] buffer = new byte[chunkSize];
            HashSet<int> seenAddrs = new HashSet<int>();
            HashSet<int> seenBareAddrs = new HashSet<int>();
            List<KeyValuePair<int, string>> bareRaw = new List<KeyValuePair<int, string>>();
            long bytesReadTotal = 0;
            long bareMin = Math.Max(0x00400000L, hpAddr - 0x80000L);
            long bareMax = hpAddr + 0x80000L;
            Stopwatch scanWatch = Stopwatch.StartNew();

            for (int r = 0; r < regions.Count && isolatedAll.Count < maxIsolatedTotal; r++)
            {
                if (bytesReadTotal >= maxBytesToRead || scanWatch.ElapsedMilliseconds >= maxDurationMs)
                {
                    break;
                }

                long regStart = regions[r].BaseAddress.ToInt64();
                long regEnd = regStart + regions[r].RegionSize;
                long cursor = regStart;
                regionsScanned++;

                List<KeyValuePair<int, string>> regionRaw = new List<KeyValuePair<int, string>>();

                while (cursor < regEnd && regionRaw.Count < maxRawPerRegion)
                {
                    if (bytesReadTotal >= maxBytesToRead || scanWatch.ElapsedMilliseconds >= maxDurationMs)
                    {
                        break;
                    }

                    int toRead = (int)Math.Min(chunkSize, regEnd - cursor);
                    int bytesRead;
                    if (this.PMR.ReadProcessMemory((IntPtr)cursor, buffer, toRead, out bytesRead) && bytesRead >= 8)
                    {
                        bytesReadTotal += bytesRead;
                        List<int> matches = ScanBufferForMapOffsets(buffer, bytesRead);
                        for (int m = 0; m < matches.Count && regionRaw.Count < maxRawPerRegion; m++)
                        {
                            long absAddr = cursor + matches[m];
                            if (absAddr > int.MaxValue)
                            {
                                continue;
                            }

                            int addr32 = (int)absAddr;
                            if (!seenAddrs.Add(addr32))
                            {
                                continue;
                            }

                            string raw = ReadMemoryAsString(addr32);
                            if (IsRawMapFileString(raw))
                            {
                                rawMatchesTotal++;
                                regionRaw.Add(new KeyValuePair<int, string>(addr32, NormalizeMapName(raw)));
                            }
                        }

                        if (includeBareNearHp && cursor < bareMax && (cursor + bytesRead) > bareMin)
                        {
                            List<int> bareOffsets = ScanBufferForBareMapOffsets(buffer, bytesRead);
                            for (int b = 0; b < bareOffsets.Count && bareRaw.Count < 128; b++)
                            {
                                long absAddr = cursor + bareOffsets[b];
                                if (absAddr < bareMin || absAddr > bareMax || absAddr > int.MaxValue)
                                {
                                    continue;
                                }

                                int addr32 = (int)absAddr;
                                if (!seenBareAddrs.Add(addr32))
                                {
                                    continue;
                                }

                                string rawBare = ReadMemoryAsString(addr32);
                                string normBare = NormalizeMapName(rawBare);
                                if (!string.IsNullOrEmpty(normBare) && KnownMapNames.Value.Contains(normBare))
                                {
                                    bareRaw.Add(new KeyValuePair<int, string>(addr32, normBare));
                                }
                            }
                        }
                    }

                    if (toRead < chunkSize)
                    {
                        break;
                    }

                    cursor += chunkSize - overlap;
                }

                if (regionRaw.Count > 0)
                {
                    // Filter static/Lua map tables per region so a single huge table never crowds out isolated live slots.
                    List<KeyValuePair<int, string>> regionIsolated = FilterClusteredMapTableCandidates(regionRaw);
                    for (int k = 0; k < regionIsolated.Count && isolatedAll.Count < maxIsolatedTotal; k++)
                    {
                        isolatedAll.Add(regionIsolated[k]);
                    }
                }
            }

            if (isolatedAll.Count > 1)
            {
                isolatedAll = FilterClusteredMapTableCandidates(isolatedAll);
                isolatedAll.Sort((a, b) =>
                {
                    bool aStatic = a.Key < 0x03000000;
                    bool bStatic = b.Key < 0x03000000;
                    if (aStatic != bStatic)
                    {
                        return aStatic ? -1 : 1;
                    }

                    return Math.Abs((long)a.Key - hpAddr).CompareTo(Math.Abs((long)b.Key - hpAddr));
                });
            }

            if (bareRaw.Count > 0)
            {
                bareNearHpCandidates = FilterClusteredMapTableCandidates(bareRaw);
                bareNearHpCandidates.Sort((a, b) => Math.Abs((long)a.Key - hpAddr).CompareTo(Math.Abs((long)b.Key - hpAddr)));
            }

            return isolatedAll;
        }

        private void TryAutoDetectMapCandidates()
        {
            if (this.PMR == null || !this.PMR.IsOpen || this.currentHPBaseAddress <= 0)
            {
                return;
            }

            int regionsScanned;
            int rawMatches;
            List<KeyValuePair<int, string>> bareUnused;
            List<KeyValuePair<int, string>> selected = ScanLiveMapCandidates(
                0x00400000L,
                0x7FFE0000L,
                64L * 1024L * 1024L,
                150L,
                false,
                out regionsScanned,
                out rawMatches,
                out bareUnused);

            if (selected.Count > 0)
            {
                this.detectedMapCandidates.Clear();
                this.detectedMapCandidates.AddRange(selected);
            }
        }

        public string ReadCurrentMap()
        {
            if (!HasMemorySupport)
            {
                return string.Empty;
            }

            long now = this.mapCacheClock.ElapsedMilliseconds;
            if (now - this.lastMapReadMs < 500)
            {
                return this.cachedMapName;
            }

            lock (this.mapDetectLock)
            {
                now = this.mapCacheClock.ElapsedMilliseconds;
                if (now - this.lastMapReadMs < 500)
                {
                    return this.cachedMapName;
                }

                if (this.currentMapAddress != 0)
                {
                    string raw = ReadMemoryAsString(this.currentMapAddress);
                    if (this.mapAddressAutoDetected && !IsRawMapFileString(raw))
                    {
                        this.currentMapAddress = 0;
                        this.mapAddressAutoDetected = false;
                        this.detectedMapCandidates.Clear();
                    }
                    else
                    {
                        this.cachedMapName = NormalizeMapName(raw);
                        this.lastMapReadMs = now;
                        return this.cachedMapName;
                    }
                }

                if (this.detectedMapCandidates.Count == 0 && (now - this.lastMapScanMs) >= 3000)
                {
                    this.lastMapScanMs = now;
                    TryAutoDetectMapCandidates();
                }

                if (this.currentMapAddress != 0)
                {
                    this.cachedMapName = NormalizeMapName(ReadMemoryAsString(this.currentMapAddress));
                    this.lastMapReadMs = now;
                    return this.cachedMapName;
                }

                if (this.detectedMapCandidates.Count > 0)
                {
                    bool anyAlive = false;
                    for (int i = 0; i < this.detectedMapCandidates.Count; i++)
                    {
                        int addr = this.detectedMapCandidates[i].Key;
                        string initialMap = this.detectedMapCandidates[i].Value;
                        string raw = ReadMemoryAsString(addr);
                        if (!IsRawMapFileString(raw))
                        {
                            continue;
                        }

                        anyAlive = true;
                        string liveMap = NormalizeMapName(raw);
                        if (!string.Equals(liveMap, initialMap, StringComparison.OrdinalIgnoreCase))
                        {
                            // Candidate updated live after a map change -> lock directly onto it!
                            this.currentMapAddress = addr;
                            this.mapAddressAutoDetected = true;
                            this.detectedMapCandidates.Clear();
                            this.cachedMapName = liveMap;
                            this.lastMapReadMs = now;
                            return this.cachedMapName;
                        }
                    }

                    if (!anyAlive)
                    {
                        this.detectedMapCandidates.Clear();
                    }
                }

                // Never treat an unconfirmed static memory string as the active town map (avoids false pauses in fields).
                this.cachedMapName = string.Empty;
                this.lastMapReadMs = now;
                return this.cachedMapName;
            }
        }

        public bool IsInTown()
        {
            return IsTownMap(ReadCurrentMap());
        }

        public bool ShouldPauseForTown()
        {
            return !AllowBuffsInTown && IsInTown();
        }

        public bool ShouldPauseAutomation()
        {
            return ShouldPauseForFocus()
                || ChatPauseState.IsChatting
                || AutoRefreshCommand.IsSendingCommand
                || ActionPriorityCoordinator.IsHoldPauseActive()
                || ShouldPauseForTown();
        }

        public bool ShouldPauseAutopotKey(System.Windows.Input.Key key)
        {
            if (ShouldPauseForFocus() || ShouldPauseForTown() || ActionPriorityCoordinator.IsHoldPauseBlockingAutopot())
            {
                return true;
            }

            if ((ChatPauseState.IsChatting || AutoRefreshCommand.IsSendingCommand) && ChatPauseState.IsTypingKey(key))
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

        /// <summary>
        /// Detects the 3-uint (12-byte) leading header present on Ragexe 2022+ (e.g. FroggoClient) when
        /// reading from the legacy +0x474 offset instead of +0x480:
        /// [Slot 0: fixed int (e.g. 9), Slot 1: out-of-range EFST value (&gt; 100000, e.g. 1004240168), Slot 2: fixed int (e.g. 1)].
        /// Valid Ragnarok Online EFST IDs are always &lt;= 100000 (except IGNORE/IGNORE_2 sentinels).
        /// </summary>
        public static int DetectLeadingHeaderSkip(uint[] statusBuffer, int limit)
        {
            if (statusBuffer == null || limit < 4 || statusBuffer.Length < 4)
            {
                return 0;
            }

            uint slot1 = statusBuffer[1];
            bool slot1OutOfEfstRange = slot1 > 100000u
                && slot1 != (uint)EffectStatusIDs.IGNORE
                && slot1 != (uint)EffectStatusIDs.IGNORE_2;

            return slot1OutOfEfstRange ? 3 : 0;
        }

        /// <summary>
        /// Extracts active EFST codes from a raw status buffer while skipping any Ragexe 2022+ leading
        /// 3-slot header, ignoring isolated out-of-range values (&gt; 100000), and stopping after a run
        /// of consecutive IGNORE (0xFFFFFFFF) slots.
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
                if (code == (uint)EffectStatusIDs.IGNORE || code == (uint)EffectStatusIDs.IGNORE_2)
                {
                    consecutiveIgnores++;
                    if (consecutiveIgnores >= Constants.STATUS_END_CONSECUTIVE_IGNORES)
                    {
                        break;
                    }
                    continue;
                }

                consecutiveIgnores = 0;
                if (code > 100000u)
                {
                    continue;
                }

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
            string mapSourceTag = this.currentMapAddress == 0
                ? (LocalizationManager.IsPortuguese ? "pendente" : "pending")
                : (this.mapAddressAutoDetected ? "auto" : "config");
            string mapRaw = this.currentMapAddress != 0 ? ReadMemoryAsString(this.currentMapAddress) : string.Empty;
            string mapDisplay;
            if (string.IsNullOrWhiteSpace(mapName))
            {
                mapDisplay = LocalizationManager.IsPortuguese ? "- [pendente]" : "- [pending]";
            }
            else
            {
                string zoneTag = IsTownMap(mapName)
                    ? (LocalizationManager.IsPortuguese ? "Cidade" : "Town")
                    : (LocalizationManager.IsPortuguese ? "Campo" : "Field");
                mapDisplay = this.currentMapAddress != 0
                    ? string.Format("{0} [{1} · 0x{2:X8} · {3}]", mapName, zoneTag, this.currentMapAddress, mapSourceTag)
                    : string.Format("{0} [{1}]", mapName, zoneTag);
            }
            sb.AppendFormat(
                LocalizationManager.IsPortuguese
                    ? "Personagem: {0}  |  Mapa: {1}"
                    : "Character: {0}  |  Map: {1}",
                string.IsNullOrWhiteSpace(charName) ? "-" : charName.Trim(),
                mapDisplay);
            sb.AppendLine();
            sb.AppendFormat(
                "HP=0x{0:X8} | Nome=0x{1:X8} | Status=0x{2:X8} | MapaAddr=0x{3:X8} ({4}) | Raw=\"{5}\"",
                this.currentHPBaseAddress,
                this.currentNameAddress,
                this.statusBufferAddress,
                this.currentMapAddress,
                mapSourceTag,
                string.IsNullOrEmpty(mapRaw) ? "-" : mapRaw.Trim());
            sb.AppendLine();
            sb.AppendLine(new string('-', 44));

            int found = 0;
            int consecutiveIgnores = 0;
            int stopIndex = count;
            for (int i = startIndex; i < count; i++)
            {
                uint code = statusBuffer[i];
                if (code == (uint)EffectStatusIDs.IGNORE || code == (uint)EffectStatusIDs.IGNORE_2)
                {
                    consecutiveIgnores++;
                    if (consecutiveIgnores >= Constants.STATUS_END_CONSECUTIVE_IGNORES)
                    {
                        stopIndex = i + 1;
                        break;
                    }
                    continue;
                }

                consecutiveIgnores = 0;
                if (code > 100000u)
                {
                    continue;
                }

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

            int diagRegions;
            int diagRawMatches;
            List<KeyValuePair<int, string>> bareNearHp;
            List<KeyValuePair<int, string>> liveMapCandidates = ScanLiveMapCandidates(
                0x00400000L,
                0x7FFE0000L,
                128L * 1024L * 1024L,
                400L,
                true,
                out diagRegions,
                out diagRawMatches,
                out bareNearHp);

            sb.AppendLine();
            sb.AppendLine(LocalizationManager.IsPortuguese
                ? string.Format("[Diagnóstico de Mapa — Regiões: {0} | Brutos .rsw/.gat: {1} | Isolados: {2}]", diagRegions, diagRawMatches, liveMapCandidates.Count)
                : string.Format("[Map Diagnostics — Regions: {0} | Raw .rsw/.gat: {1} | Isolated: {2}]", diagRegions, diagRawMatches, liveMapCandidates.Count));

            if (liveMapCandidates.Count == 0)
            {
                sb.AppendLine(LocalizationManager.IsPortuguese
                    ? "Nenhum candidato .rsw/.gat isolado encontrado."
                    : "No isolated .rsw/.gat candidates found.");
            }
            else
            {
                int limitDiag = Math.Min(liveMapCandidates.Count, 15);
                for (int i = 0; i < limitDiag; i++)
                {
                    int addr = liveMapCandidates[i].Key;
                    string liveRaw = ReadMemoryAsString(addr);
                    string scopeTag = addr < 0x03000000 ? "static" : "heap";
                    sb.AppendFormat("0x{0:X8} ({1}) -> {2}", addr, scopeTag, string.IsNullOrWhiteSpace(liveRaw) ? "-" : liveRaw.Trim());
                    sb.AppendLine();
                }
            }

            if (bareNearHp.Count > 0)
            {
                sb.AppendLine(LocalizationManager.IsPortuguese
                    ? "[Nomes de Mapa perto de HP (±0x80000)]"
                    : "[Map Names near HP (±0x80000)]");
                int limitBare = Math.Min(bareNearHp.Count, 12);
                for (int i = 0; i < limitBare; i++)
                {
                    sb.AppendFormat("0x{0:X8} -> {1}", bareNearHp[i].Key, bareNearHp[i].Value);
                    sb.AppendLine();
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
                    if (reader.ReadProcessMemory((IntPtr)(hpAddress + DEFAULT_STATUS_BUFFER_OFFSET), statusRaw, statusRaw.Length, out bytesRead)
                        && bytesRead >= sizeof(uint))
                    {
                        int entries = bytesRead / sizeof(uint);
                        uint[] parsedStatuses = new uint[entries];
                        Buffer.BlockCopy(statusRaw, 0, parsedStatuses, 0, entries * sizeof(uint));
                        List<uint> activeStatuses = new List<uint>(32);
                        ExtractActiveStatuses(parsedStatuses, entries, activeStatuses);
                        result.ActiveBuffCount = activeStatuses.Count;
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
            lock (this.processCacheLock)
            {
                this.cachedProcessAlive = false;
                this.cachedWindowHandle = IntPtr.Zero;
            }

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
