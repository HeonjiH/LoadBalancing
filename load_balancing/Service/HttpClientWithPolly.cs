using Polly;
using Polly.Wrap;
using System.Net;

namespace load_balancing.Service
{
    public class HttpClientWithPolly
    {
        private static readonly string[] Proxies = { "https://localhost:8080" };
        private static int currentProxyIndex = 0;
        private static HttpClient httpClient;
        private static AsyncPolicyWrap<HttpResponseMessage> policyWrap;

        public HttpClientWithPolly()
        {
            httpClient = CreateHttpClientWithProxy(Proxies[currentProxyIndex]);

            var retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(response => !response.IsSuccessStatusCode)
            .RetryAsync(3, (exception, retryCount, context) =>
            {
                Console.WriteLine($"Retry {retryCount} for proxy {Proxies[currentProxyIndex]}.");
                SwitchProxy();
            });

            var fallbackPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(response => !response.IsSuccessStatusCode)
                .FallbackAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable), onFallbackAsync: async (response, context) =>
                {
                    Console.WriteLine("Fallback triggered. All proxies are down.");
                    await Task.Delay(5000); // 5초 후 다시 시도
                    SwitchProxy();
                });

            policyWrap = Policy.WrapAsync(retryPolicy, fallbackPolicy);
        }

        private static HttpClient CreateHttpClientWithProxy(string proxyUrl)
        {
            var httpClientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy(proxyUrl),
                UseProxy = true
            };

            var httpClient = new HttpClient(httpClientHandler);
            httpClient.BaseAddress = new Uri(proxyUrl);
            return httpClient;
        }

        private static void SwitchProxy()
        {
            currentProxyIndex = (currentProxyIndex + 1) % Proxies.Length;
            httpClient = CreateHttpClientWithProxy(Proxies[currentProxyIndex]);
            Console.WriteLine($"Switched to proxy {Proxies[currentProxyIndex]}");
        }

        public async Task<string> GetDataASync(string url)
        {
            var response = await policyWrap.ExecuteAsync(() => httpClient.GetAsync(url));
            return await response.Content.ReadAsStringAsync();
        }
    }
}
