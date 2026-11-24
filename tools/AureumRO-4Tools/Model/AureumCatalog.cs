using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using _4RTools.Utils;

namespace _4RTools.Model
{
    public sealed class AureumVoteSite
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("voteUrl")]
        public string VoteUrl { get; set; }

        [JsonProperty("directUrl")]
        public string DirectUrl { get; set; }

        [JsonProperty("points")]
        public int Points { get; set; }

        [JsonProperty("cooldownHours")]
        public int CooldownHours { get; set; }
    }

    public sealed class AureumCardTier
    {
        [JsonProperty("threshold")]
        public string Threshold { get; set; }

        [JsonProperty("effect")]
        public string Effect { get; set; }
    }

    public sealed class AureumCardItem
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("fullName")]
        public string FullName { get; set; }

        public string GetProgressKey(int collectionIndex)
        {
            return BuildProgressKey(collectionIndex, this.Id);
        }

        public static string BuildProgressKey(int collectionIndex, int cardId)
        {
            return string.Format("{0:00}:{1}", collectionIndex, cardId);
        }
    }

    public sealed class AureumCardCollection
    {
        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("totalBonus")]
        public string TotalBonus { get; set; }

        [JsonProperty("tiers")]
        public List<AureumCardTier> Tiers { get; set; }

        [JsonProperty("cards")]
        public List<AureumCardItem> Cards { get; set; }
    }

    public sealed class AureumMapDropItem
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("drops")]
        public string Drops { get; set; }
    }

    public sealed class AureumMapEntry
    {
        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("warp")]
        public string Warp { get; set; }

        [JsonProperty("bonus")]
        public string Bonus { get; set; }

        [JsonProperty("bonusCategory")]
        public string BonusCategory { get; set; }

        [JsonProperty("refCity")]
        public string RefCity { get; set; }

        [JsonProperty("directions")]
        public string Directions { get; set; }

        [JsonProperty("routePath")]
        public string RoutePath { get; set; }

        [JsonProperty("items")]
        public List<AureumMapDropItem> Items { get; set; }

        public string MapCode
        {
            get { return string.IsNullOrWhiteSpace(this.Warp) ? string.Empty : this.Warp.Trim(); }
        }

        public string ReferenceCity
        {
            get { return !string.IsNullOrWhiteSpace(this.RefCity) ? this.RefCity : (this.City ?? string.Empty); }
        }
    }

    public sealed class AureumCatalogData
    {
        [JsonProperty("votePageUrl")]
        public string VotePageUrl { get; set; }

        [JsonProperty("voteShopNavi")]
        public string VoteShopNavi { get; set; }

        [JsonProperty("cardNpcNavi")]
        public string CardNpcNavi { get; set; }

        [JsonProperty("topsitesApiUrl")]
        public string TopsitesApiUrl { get; set; }

        [JsonProperty("topsites")]
        public List<AureumVoteSite> Topsites { get; set; }

        [JsonProperty("cardCollections")]
        public List<AureumCardCollection> CardCollections { get; set; }

        [JsonProperty("mapCollections")]
        public List<AureumMapEntry> MapCollections { get; set; }
    }

    public static class AureumCatalog
    {
        private sealed class RemoteTopsitesEnvelope
        {
            [JsonProperty("topsites")]
            public List<RemoteTopsiteItem> Topsites { get; set; }
        }

        private sealed class RemoteTopsiteItem
        {
            [JsonProperty("code")]
            public string Code { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("vote_url")]
            public string VoteUrl { get; set; }

            [JsonProperty("points")]
            public int Points { get; set; }

            [JsonProperty("cooldown_hours")]
            public int CooldownHours { get; set; }
        }

        private static readonly object LoadLock = new object();
        private static AureumCatalogData cached;

        public static AureumCatalogData Load()
        {
            lock (LoadLock)
            {
                if (cached != null)
                {
                    return cached;
                }

                Assembly asm = Assembly.GetExecutingAssembly();
                string resourceName = asm.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith("AureumCatalog.json", StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(resourceName))
                {
                    using (Stream stream = asm.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                            {
                                string json = reader.ReadToEnd();
                                AureumCatalogData parsed = JsonConvert.DeserializeObject<AureumCatalogData>(json);
                                if (parsed != null)
                                {
                                    Normalize(parsed);
                                    cached = parsed;
                                    return cached;
                                }
                            }
                        }
                    }
                }

                cached = CreateEmptyFallback();
                return cached;
            }
        }

        public static List<string> GetCardCategories(AureumCatalogData data)
        {
            if (data == null || data.CardCollections == null)
            {
                return new List<string>();
            }

            return data.CardCollections
                .Select(c => c.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static List<string> GetMapCities(AureumCatalogData data)
        {
            if (data == null || data.MapCollections == null)
            {
                return new List<string>();
            }

            return data.MapCollections
                .Select(m => m.City)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static List<string> GetMapBonusCategories(AureumCatalogData data)
        {
            if (data == null || data.MapCollections == null)
            {
                return new List<string>();
            }

            return data.MapCollections
                .Select(m => m.BonusCategory)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Optionally fetches the latest topsites list from the public AureumRO API,
        /// returning null if offline or unreachable so embedded defaults stay active.
        /// </summary>
        public static List<AureumVoteSite> TryFetchRemoteTopsites(string apiUrl, string votePageUrl)
        {
            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                return null;
            }

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(apiUrl);
                req.Method = "GET";
                req.Timeout = 5000;
                req.ReadWriteTimeout = 5000;
                req.UserAgent = AppConfig.Name + "/" + AppConfig.Version;

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (Stream stream = resp.GetResponseStream())
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        string json = reader.ReadToEnd();
                        RemoteTopsitesEnvelope env = JsonConvert.DeserializeObject<RemoteTopsitesEnvelope>(json);
                        if (env == null || env.Topsites == null || env.Topsites.Count == 0)
                        {
                            return null;
                        }

                        List<AureumVoteSite> result = new List<AureumVoteSite>();
                        foreach (RemoteTopsiteItem item in env.Topsites)
                        {
                            if (item == null || string.IsNullOrWhiteSpace(item.Code) || string.IsNullOrWhiteSpace(item.Name))
                            {
                                continue;
                            }

                            result.Add(new AureumVoteSite
                            {
                                Code = item.Code.Trim(),
                                Name = item.Name.Trim(),
                                VoteUrl = string.IsNullOrWhiteSpace(votePageUrl) ? "https://aureumro.com/votar/" : votePageUrl,
                                DirectUrl = item.VoteUrl ?? string.Empty,
                                Points = item.Points > 0 ? item.Points : 1,
                                CooldownHours = item.CooldownHours > 0 ? item.CooldownHours : 12
                            });
                        }

                        return result.Count > 0 ? result : null;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("AureumCatalog", "Failed to fetch remote topsites (using embedded defaults)", ex);
                return null;
            }
        }

        private static void Normalize(AureumCatalogData data)
        {
            if (string.IsNullOrWhiteSpace(data.VotePageUrl))
            {
                data.VotePageUrl = "https://aureumro.com/votar/";
            }
            if (string.IsNullOrWhiteSpace(data.VoteShopNavi))
            {
                data.VoteShopNavi = "/navi prontera 183/135";
            }
            if (string.IsNullOrWhiteSpace(data.CardNpcNavi))
            {
                data.CardNpcNavi = "/navi prontera 153/177";
            }
            if (data.Topsites == null)
            {
                data.Topsites = new List<AureumVoteSite>();
            }
            if (data.CardCollections == null)
            {
                data.CardCollections = new List<AureumCardCollection>();
            }
            if (data.MapCollections == null)
            {
                data.MapCollections = new List<AureumMapEntry>();
            }

            foreach (AureumCardCollection col in data.CardCollections)
            {
                if (col.Tiers == null) col.Tiers = new List<AureumCardTier>();
                if (col.Cards == null) col.Cards = new List<AureumCardItem>();
            }

            foreach (AureumMapEntry map in data.MapCollections)
            {
                if (map.Items == null) map.Items = new List<AureumMapDropItem>();
            }
        }

        private static AureumCatalogData CreateEmptyFallback()
        {
            AureumCatalogData fallback = new AureumCatalogData();
            Normalize(fallback);
            return fallback;
        }
    }
}
