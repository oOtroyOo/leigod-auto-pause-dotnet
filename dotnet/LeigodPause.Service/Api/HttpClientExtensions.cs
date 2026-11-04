namespace LeigodPause.Service.Api;

/// <summary>
/// HttpClient 扩展：封装雷神(leigod)接口所需的默认请求头与注册方式。
/// </summary>
public static class HttpClientExtensions
{
    public const string LeigodClientName = "leigod";
    public const string UserAgent = "Mozilla/5.0 (Linux; Android 10; SM-G973F) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Mobile Safari/537.36 EdgA/110.0.1587.63";

    /// <summary>
    /// 以 "leigod" 命名注册一个带默认请求头的 HttpClient（基于 HttpClientFactory）。
    /// </summary>
    public static IServiceCollection AddLeigodHttpClient(
        this IServiceCollection services,
        string name = LeigodClientName)
    {
        services.AddHttpClient(name, client => client.ApplyLeigodDefaultHeaders())
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip
                                         | DecompressionMethods.Deflate
            });
        return services;
    }

    /// <summary>
    /// 代理解析优先级：
    ///   1) 显式 proxyUrl（如本地代理 http://127.0.0.1:7890，建议从 config.ini 读取）
    ///   2) 环境变量 HTTPS_PROXY / HTTP_PROXY（容器/CI 常用）
    ///   3) 均未提供时 Proxy 保持 null，HttpClientHandler.UseProxy=true 自动使用 Windows 系统代理
    ///      （控制面板/设置中的代理，含本地代理）。
    /// 注意：Windows 服务(Session 0)运行账户通常没有用户级代理设置，若需让服务走本地代理，
    ///       请显式传入 proxyUrl。
    /// </summary>
    public static IServiceCollection AddHttpClientWithProxy(
        this IServiceCollection services,
        string? proxyUrl = null)
    {
        services.AddHttpClient("proxy", client => client.ApplyDefaultHeaders())
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip
                                             | DecompressionMethods.Deflate,
                    UseProxy = true,
                };

                var proxyAddress = proxyUrl
                                   ?? Environment.GetEnvironmentVariable("HTTPS_PROXY")
                                   ?? Environment.GetEnvironmentVariable("HTTP_PROXY");

                if (!string.IsNullOrWhiteSpace(proxyAddress))
                {
                    handler.Proxy = new WebProxy(proxyAddress.Trim())
                    {
                        BypassProxyOnLocal = true,
                        UseDefaultCredentials = true,
                    };
                }

                return handler;
            });
        return services;
    }

    public static void ApplyDefaultHeaders(this HttpClient client)
    {
        var h = client.DefaultRequestHeaders;

        h.UserAgent.Clear();
        h.Accept.Clear();
        h.AcceptEncoding.Clear();
        h.AcceptLanguage.Clear();

        h.UserAgent.TryParseAdd(UserAgent);
        h.Accept.TryParseAdd("application/json, text/javascript, */*; q=0.01");
        h.AcceptEncoding.TryParseAdd("gzip, deflate");
        h.AcceptLanguage.TryParseAdd("zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
    }

    /// <summary>
    /// 为指定 HttpClient 应用 leigod 默认请求头。
    /// 方法幂等：多次调用不会重复追加请求头。
    /// </summary>
    private static void ApplyLeigodDefaultHeaders(this HttpClient client)
    {
        client.ApplyDefaultHeaders();
        var h = client.DefaultRequestHeaders;
        h.Remove("DNT");
        h.Add("DNT", "1");

        h.Referrer = new Uri("https://www.leigod.com/");

        h.Remove("Sec-Fetch-Dest");
        h.Add("Sec-Fetch-Dest", "empty");
        h.Remove("Sec-Fetch-Mode");
        h.Add("Sec-Fetch-Mode", "cors");
        h.Remove("Sec-Fetch-Site");
        h.Add("Sec-Fetch-Site", "same-site");
    }
}