using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using _4RTools.Model;

namespace _4RTools.Utils
{
    public sealed class AureumProgressData
    {
        public HashSet<string> CollectedCardKeys { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> CompletedMapWarps { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, DateTime> LastVoteUtcBySite { get; set; } = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, DateTime> LastNotifiedVoteUtcBySite { get; set; } = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        public bool VoteReminderEnabled { get; set; } = true;
        public bool VoteReminderSound { get; set; } = true;

        public void EnsureCollections()
        {
            if (this.CollectedCardKeys == null)
            {
                this.CollectedCardKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            else if (this.CollectedCardKeys.Comparer != StringComparer.OrdinalIgnoreCase)
            {
                this.CollectedCardKeys = new HashSet<string>(this.CollectedCardKeys, StringComparer.OrdinalIgnoreCase);
            }

            if (this.CompletedMapWarps == null)
            {
                this.CompletedMapWarps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            else if (this.CompletedMapWarps.Comparer != StringComparer.OrdinalIgnoreCase)
            {
                this.CompletedMapWarps = new HashSet<string>(this.CompletedMapWarps, StringComparer.OrdinalIgnoreCase);
            }

            if (this.LastVoteUtcBySite == null)
            {
                this.LastVoteUtcBySite = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            }
            else if (this.LastVoteUtcBySite.Comparer != StringComparer.OrdinalIgnoreCase)
            {
                this.LastVoteUtcBySite = new Dictionary<string, DateTime>(this.LastVoteUtcBySite, StringComparer.OrdinalIgnoreCase);
            }

            if (this.LastNotifiedVoteUtcBySite == null)
            {
                this.LastNotifiedVoteUtcBySite = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            }
            else if (this.LastNotifiedVoteUtcBySite.Comparer != StringComparer.OrdinalIgnoreCase)
            {
                this.LastNotifiedVoteUtcBySite = new Dictionary<string, DateTime>(this.LastNotifiedVoteUtcBySite, StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    public static class AureumProgressStore
    {
        private static readonly object StoreLock = new object();
        internal static string ProgressFilePath = "aureum_progress.json";

        public static event EventHandler ProgressChanged;

        public static AureumProgressData Load()
        {
            lock (StoreLock)
            {
                try
                {
                    if (File.Exists(ProgressFilePath))
                    {
                        string json = File.ReadAllText(ProgressFilePath, Encoding.UTF8);
                        AureumProgressData parsed = JsonConvert.DeserializeObject<AureumProgressData>(json);
                        if (parsed != null)
                        {
                            parsed.EnsureCollections();
                            return parsed;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn("AureumProgressStore", "Failed to load Aureum progress, using defaults", ex);
                }

                return new AureumProgressData();
            }
        }

        public static void Save(AureumProgressData data)
        {
            if (data == null)
            {
                return;
            }

            lock (StoreLock)
            {
                try
                {
                    data.EnsureCollections();
                    string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                    string dir = Path.GetDirectoryName(ProgressFilePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    string tempPath = ProgressFilePath + ".tmp";
                    File.WriteAllText(tempPath, json, Encoding.UTF8);
                    if (File.Exists(ProgressFilePath))
                    {
                        File.Replace(tempPath, ProgressFilePath, null);
                    }
                    else
                    {
                        File.Move(tempPath, ProgressFilePath);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn("AureumProgressStore", "Failed to save Aureum progress", ex);
                }
            }

            RaiseProgressChanged();
        }

        public static bool IsCardCollected(AureumProgressData data, int collectionIndex, int cardId)
        {
            if (data == null || data.CollectedCardKeys == null)
            {
                return false;
            }

            return data.CollectedCardKeys.Contains(AureumCardItem.BuildProgressKey(collectionIndex, cardId));
        }

        public static bool ToggleCardCollected(AureumProgressData data, int collectionIndex, int cardId)
        {
            if (data == null)
            {
                return false;
            }

            data.EnsureCollections();
            string key = AureumCardItem.BuildProgressKey(collectionIndex, cardId);
            bool nowCollected;
            if (data.CollectedCardKeys.Contains(key))
            {
                data.CollectedCardKeys.Remove(key);
                nowCollected = false;
            }
            else
            {
                data.CollectedCardKeys.Add(key);
                nowCollected = true;
            }

            Save(data);
            return nowCollected;
        }

        public static void SetCardCollectionCollected(AureumProgressData data, AureumCardCollection collection, bool collected)
        {
            if (data == null || collection == null || collection.Cards == null)
            {
                return;
            }

            data.EnsureCollections();
            foreach (AureumCardItem card in collection.Cards)
            {
                string key = card.GetProgressKey(collection.Index);
                if (collected)
                {
                    data.CollectedCardKeys.Add(key);
                }
                else
                {
                    data.CollectedCardKeys.Remove(key);
                }
            }

            Save(data);
        }

        public static int CountCollectedInCollection(AureumProgressData data, AureumCardCollection collection)
        {
            if (data == null || collection == null || collection.Cards == null)
            {
                return 0;
            }

            int count = 0;
            foreach (AureumCardItem card in collection.Cards)
            {
                if (IsCardCollected(data, collection.Index, card.Id))
                {
                    count++;
                }
            }

            return count;
        }

        public static int GetActiveTierLevel(int collectedCount)
        {
            if (collectedCount >= 5) return 5;
            if (collectedCount >= 3) return 3;
            if (collectedCount >= 1) return 1;
            return 0;
        }

        public static bool IsMapCompleted(AureumProgressData data, string warpCode)
        {
            if (data == null || data.CompletedMapWarps == null || string.IsNullOrWhiteSpace(warpCode))
            {
                return false;
            }

            return data.CompletedMapWarps.Contains(warpCode.Trim());
        }

        public static bool ToggleMapCompleted(AureumProgressData data, string warpCode)
        {
            if (data == null || string.IsNullOrWhiteSpace(warpCode))
            {
                return false;
            }

            data.EnsureCollections();
            string normalized = warpCode.Trim();
            bool nowDone;
            if (data.CompletedMapWarps.Contains(normalized))
            {
                data.CompletedMapWarps.Remove(normalized);
                nowDone = false;
            }
            else
            {
                data.CompletedMapWarps.Add(normalized);
                nowDone = true;
            }

            Save(data);
            return nowDone;
        }

        public static void SetMapsCompleted(AureumProgressData data, IEnumerable<string> warpCodes, bool completed)
        {
            if (data == null || warpCodes == null)
            {
                return;
            }

            data.EnsureCollections();
            foreach (string warp in warpCodes)
            {
                if (string.IsNullOrWhiteSpace(warp))
                {
                    continue;
                }

                string normalized = warp.Trim();
                if (completed)
                {
                    data.CompletedMapWarps.Add(normalized);
                }
                else
                {
                    data.CompletedMapWarps.Remove(normalized);
                }
            }

            Save(data);
        }

        public static TimeSpan GetRemainingVoteCooldown(AureumProgressData data, AureumVoteSite site, DateTime utcNow)
        {
            if (data == null || site == null || string.IsNullOrWhiteSpace(site.Code) || data.LastVoteUtcBySite == null)
            {
                return TimeSpan.Zero;
            }

            DateTime lastVoteUtc;
            if (!data.LastVoteUtcBySite.TryGetValue(site.Code, out lastVoteUtc))
            {
                return TimeSpan.Zero;
            }

            int hours = site.CooldownHours > 0 ? site.CooldownHours : 12;
            DateTime readyAtUtc = DateTime.SpecifyKind(lastVoteUtc, DateTimeKind.Utc).AddHours(hours);
            TimeSpan remaining = readyAtUtc - utcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        public static bool IsVoteReady(AureumProgressData data, AureumVoteSite site, DateTime utcNow)
        {
            return GetRemainingVoteCooldown(data, site, utcNow) <= TimeSpan.Zero;
        }

        public static void RecordVote(AureumProgressData data, string siteCode, DateTime utcNow)
        {
            if (data == null || string.IsNullOrWhiteSpace(siteCode))
            {
                return;
            }

            data.EnsureCollections();
            DateTime normalizedUtc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
            data.LastVoteUtcBySite[siteCode.Trim()] = normalizedUtc;
            data.LastNotifiedVoteUtcBySite.Remove(siteCode.Trim());
            Save(data);
        }

        public static void ResetVote(AureumProgressData data, string siteCode)
        {
            if (data == null || string.IsNullOrWhiteSpace(siteCode))
            {
                return;
            }

            data.EnsureCollections();
            data.LastVoteUtcBySite.Remove(siteCode.Trim());
            data.LastNotifiedVoteUtcBySite.Remove(siteCode.Trim());
            Save(data);
        }

        public static DateTime? GetLastVoteLocalTime(AureumProgressData data, string siteCode)
        {
            if (data == null || string.IsNullOrWhiteSpace(siteCode) || data.LastVoteUtcBySite == null)
            {
                return null;
            }

            DateTime lastVoteUtc;
            if (!data.LastVoteUtcBySite.TryGetValue(siteCode.Trim(), out lastVoteUtc))
            {
                return null;
            }

            return DateTime.SpecifyKind(lastVoteUtc, DateTimeKind.Utc).ToLocalTime();
        }

        /// <summary>
        /// Parses a user-entered local time ("HH:mm", "H:mm", or "HHmm") for the last vote.
        /// If the time today is still in the future, it resolves to yesterday at that time.
        /// </summary>
        public static bool TryParseLastVoteLocalTime(string input, DateTime localNow, out DateTime resolvedUtc)
        {
            resolvedUtc = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            string clean = input.Trim();
            int hour;
            int minute;

            if (clean.Contains(":"))
            {
                string[] parts = clean.Split(':');
                if (parts.Length != 2
                    || !int.TryParse(parts[0].Trim(), out hour)
                    || !int.TryParse(parts[1].Trim(), out minute))
                {
                    return false;
                }
            }
            else if (clean.Length == 4)
            {
                if (!int.TryParse(clean.Substring(0, 2), out hour)
                    || !int.TryParse(clean.Substring(2, 2), out minute))
                {
                    return false;
                }
            }
            else if (clean.Length == 3)
            {
                if (!int.TryParse(clean.Substring(0, 1), out hour)
                    || !int.TryParse(clean.Substring(1, 2), out minute))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            if (hour < 0 || hour > 23 || minute < 0 || minute > 59)
            {
                return false;
            }

            DateTime normalizedNow = DateTime.SpecifyKind(localNow, DateTimeKind.Local);
            DateTime candidateLocal = new DateTime(normalizedNow.Year, normalizedNow.Month, normalizedNow.Day, hour, minute, 0, DateTimeKind.Local);
            if (candidateLocal > normalizedNow)
            {
                candidateLocal = candidateLocal.AddDays(-1);
            }

            resolvedUtc = candidateLocal.ToUniversalTime();
            return true;
        }

        /// <summary>
        /// Returns sites whose cooldown has just expired after a recorded vote and have not yet
        /// triggered a ready notification for that vote cycle.
        /// </summary>
        public static List<AureumVoteSite> CheckNewlyReadyVoteSites(AureumProgressData data, IEnumerable<AureumVoteSite> sites, DateTime utcNow)
        {
            List<AureumVoteSite> newlyReady = new List<AureumVoteSite>();
            if (data == null || sites == null || !data.VoteReminderEnabled)
            {
                return newlyReady;
            }

            data.EnsureCollections();
            bool changed = false;
            foreach (AureumVoteSite site in sites)
            {
                if (site == null || string.IsNullOrWhiteSpace(site.Code))
                {
                    continue;
                }

                DateTime lastVoteUtc;
                if (!data.LastVoteUtcBySite.TryGetValue(site.Code, out lastVoteUtc))
                {
                    continue;
                }

                if (GetRemainingVoteCooldown(data, site, utcNow) > TimeSpan.Zero)
                {
                    continue;
                }

                DateTime alreadyNotifiedForVote;
                if (data.LastNotifiedVoteUtcBySite.TryGetValue(site.Code, out alreadyNotifiedForVote)
                    && alreadyNotifiedForVote == lastVoteUtc)
                {
                    continue;
                }

                data.LastNotifiedVoteUtcBySite[site.Code] = lastVoteUtc;
                newlyReady.Add(site);
                changed = true;
            }

            if (changed)
            {
                Save(data);
            }

            return newlyReady;
        }

        private static void RaiseProgressChanged()
        {
            EventHandler handler = ProgressChanged;
            if (handler != null)
            {
                try
                {
                    handler(null, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Logger.Warn("AureumProgressStore", "Error notifying progress listener", ex);
                }
            }
        }
    }
}
