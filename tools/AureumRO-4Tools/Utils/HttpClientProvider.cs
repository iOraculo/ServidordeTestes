using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace _4RTools.Utils
{
    internal static class HttpClientProvider
    {
        private static readonly HttpClient SharedClient;

        static HttpClientProvider()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch
            {
                // Keep default protocol if TLS 1.2 cannot be enabled on legacy environments.
            }

            SharedClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            SharedClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            SharedClient.DefaultRequestHeaders.UserAgent.TryParseAdd("4RTools/" + AppConfig.Version);
        }

        public static HttpClient Client
        {
            get { return SharedClient; }
        }

        public static async Task<string> GetStringAsync(string url, TimeSpan timeout)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource(timeout))
            {
                using (HttpResponseMessage response = await SharedClient.GetAsync(url, cts.Token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
