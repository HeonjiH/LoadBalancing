using System.Net;
using Polly;
using Polly.Wrap;

public class ProxyManager
{
    public AsyncPolicyWrap<HttpResponseMessage> PolicyWrap { get; private set; }

    private string[] _proxies;
    private int currentProxyIndex = 0;

    public ProxyManager(string[] proxies)
    {
        _proxies = proxies;
        ConfigurePolicies();
    }

    private void ConfigurePolicies()
    {
        // 재시도 정책 정의
        var retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => !r.IsSuccessStatusCode)
            .RetryAsync(3, onRetry: (outcome, retryCount, context) =>
            {
                Console.WriteLine($"Retry {retryCount} for proxy {_proxies[currentProxyIndex]}.");
                SwitchProxy(); // 재시도 시 프록시 변경
            });

        // 폴백 정책 정의
        var fallbackPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(r => !r.IsSuccessStatusCode)
            .FallbackAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable), onFallbackAsync: async (response, context) =>
            {
                Console.WriteLine("Fallback triggered. All proxies are down.");
                await Task.Delay(5000); // 5초 후 다시 시도
                SwitchProxy();
            });

        // 정책 래핑
        PolicyWrap = Policy.WrapAsync(fallbackPolicy, retryPolicy);
    }

    private void SwitchProxy()
    {
        currentProxyIndex = (currentProxyIndex + 1) % _proxies.Length;
        Console.WriteLine($"Switched to proxy {_proxies[currentProxyIndex]}");
    }

    public string GetAvailableProxy()
    {
        return _proxies[currentProxyIndex];
    }
    public void MarkProxyUnavaliable()
    {
        // 프록시가 응답하지 않는 경우 호출
        SwitchProxy();
    }
}
