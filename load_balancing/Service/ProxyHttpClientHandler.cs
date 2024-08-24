using System.Net;

namespace load_balancing.Service
{
    /// <summary>
    /// 프록시 상태 확인용 핸들러
    /// </summary>
    public class ProxyHttpClientHandler : DelegatingHandler
    {
        private readonly ProxyManager _proxyManager;
        public ProxyHttpClientHandler(ProxyManager proxyManager)
        {
            _proxyManager = proxyManager;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {            
            try
            {
                // Polly 정책을 적용하여 요청 전송 (각 재시도 시 HttpRequestMessage를 새로 생성)
                return await _proxyManager.PolicyWrap.ExecuteAsync(async ct =>
                {
                    // 새 HttpRequestMessage 인스턴스를 생성
                    using var newRequest = new HttpRequestMessage(request.Method, request.RequestUri)
                    {
                        Content = request.Content, // 기존 요청의 내용을 복사합니다.
                    };

                    var proxy = _proxyManager.GetAvailableProxy();
                    if (proxy == null)
                    {
                        throw new HttpRequestException("No available proxiex");
                    }

                    using var handler = new HttpClientHandler
                    {
                        Proxy = new WebProxy(proxy),
                        UseProxy = true
                    };

                    using var client = new HttpClient(handler, disposeHandler: true);

                    return await client.SendAsync(newRequest, ct);
                }, cancellationToken);
            }
            catch (HttpRequestException e)
            {
                // 프록시가 응답하지 않거나 오류가 발생한 경우
                _proxyManager.MarkProxyUnavaliable();
                throw;
            }
        }
    }
}
